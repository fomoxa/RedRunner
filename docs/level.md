# Build the level from a seed

The original game builds an endless level while you run: `TerrainGenerator` picks the next block with `Random.value` whenever the character gets within range of the end of the level. Online, every player has to run on the same ground, and the server has to simulate that ground too. This chapter makes the level finite and builds it from a seed. The server picks the seed, the seed travels to each client as the state of a network object in the scene, and the server and every client build the whole foreground once from it. Four bytes cross the network instead of a description of every block.

The background (hills, clouds and other decoration behind the level) stays random and is built only around the camera of a client. It never touches the players, so two clients may show different hills.

## Lay out the level from a seed

A layout is a list of blocks with their x positions. `TerrainGenerationSettings` already lists the start, middle and end blocks and how many of each to use, so it gets the method that turns a seed into a layout. It draws from its own `System.Random`, seeded with the level seed, instead of `UnityEngine.Random`, which other code in the game also draws from. The same seed then gives the same layout in every process.

```csharp
public List<LevelBlock> Layout ( int seed )
{
	if ( m_StartBlocksCount <= 0 || m_MiddleBlocksCount <= 0 || m_EndBlocksCount <= 0 )
	{
		throw new System.InvalidOperationException ( name + " needs positive start, middle and end block counts to lay out a seeded level" );
	}
	System.Random random = new System.Random ( seed );
	List<LevelBlock> layout = new List<LevelBlock> ();
	float x = 0f;
	AddBlocks ( layout, m_StartBlocks, m_StartBlocksCount, LevelPart.Start, random, ref x );
	AddBlocks ( layout, m_MiddleBlocks, m_MiddleBlocksCount, LevelPart.Middle, random, ref x );
	AddBlocks ( layout, m_EndBlocks, m_EndBlocksCount, LevelPart.End, random, ref x );
	return layout;
}
```

```csharp
private static void AddBlocks ( List<LevelBlock> layout, Block[] blocks, int count, LevelPart part, System.Random random, ref float x )
{
	for ( int i = 0; i < count; i++ )
	{
		Block block = TerrainGenerator.ChooseFrom ( blocks, ( float )random.NextDouble () );
		if ( block == null )
		{
			throw new System.InvalidOperationException ( "no " + part + " block to choose from" );
		}
		layout.Add ( new LevelBlock ( block, x, part ) );
		x += block.Width;
	}
}
```

`LevelBlock` is a small value type in `TerrainGeneration/LevelBlock.cs`:

```csharp
namespace RedRunner.TerrainGeneration
{

	public enum LevelPart
	{
		Start,
		Middle,
		End
	}

	public readonly struct LevelBlock
	{

		public LevelBlock ( Block prefab, float x, LevelPart part )
		{
			Prefab = prefab;
			X = x;
			Part = part;
		}

		public Block Prefab { get; }

		public float X { get; }

		public LevelPart Part { get; }

	}

}
```

The weighted choice stays in `TerrainGenerator.ChooseFrom`. The original method keeps its signature and passes `Random.value` to a new overload that takes the random value as a parameter, so the layout can pass a value from its own generator:

```csharp
public static Block ChooseFrom ( Block[] blocks )
{
	return ChooseFrom ( blocks, Random.value );
}
```

## Build the level once

`TerrainGenerator.Generate` used to add a foreground block every frame the character came close to the end. It now handles only the background, and only when it has a character to follow. The foreground comes from a new `Build` method that creates every block of the layout and ignores later calls:

```csharp
public virtual void Build ( int seed )
{
	if ( m_Built )
	{
		return;
	}
	m_Built = true;
	foreach ( LevelBlock levelBlock in m_Settings.Layout ( seed ) )
	{
		if ( levelBlock.Part == LevelPart.End && m_EndX <= 0f )
		{
			m_EndX = levelBlock.X;
		}
		CreateBlock ( levelBlock.Prefab, new Vector3 ( levelBlock.X, 0f, 0f ) );
	}
}
```

The background loop in `Generate` is unchanged. The method now starts with a guard:

```csharp
public virtual void Generate ()
{
	if ( m_Character == null )
	{
		return;
	}
```

`EndX` is where the end block starts; the chapter on death and respawn sends a player back to the start when they reach it. `IsBuilt` and `BlockCount` report what `Build` did. `Follow` sets the character the background follows, which a later chapter calls with the local player.

`Awake` no longer subscribes `Reset` to `GameManager.OnReset`. The single-player game cleared the level after each run; online, the level lives as long as the server.

## Make the level finite

Open `Assets/Generation Settings.asset` and set Middle Blocks Count to 20 and End Blocks Count to 1. The project has no end block prefab, so drag `Assets/Prefabs/Blocks/Start.prefab` into the End Blocks list as well: the level ends on the same platform it starts on. Leave Level Length at -1; `Build` does not read it.

`Layout` throws when a count is 0 or less, because the endless settings of the original game have no last block.

## Add the seed model

The seed is a model with one field. Create `LevelState.cs` in the models folder:

```csharp
using Fomoxa;
using Fomoxa.Unity;

namespace RedRunner.Networking.Models
{
    [Network]
    [Codec("net")]
    [NetworkChannel("net", Channel.ReliableOrdered)]
    public class LevelState
    {
        [Network("u32")]
        [Codec("net")]
        public uint Seed { get; set; }
    }
}
```

`[NetworkChannel("net", Channel.ReliableOrdered)]` puts the model's messages on the reliable ordered channel, which the package requires for state. Save the file and let Unity compile it before you write code that uses it: the generator then writes `LevelStateNetCodec.cs` and `LevelStateNetAdapter.cs` into the generated folder. If the behaviour below is written first, the project does not compile until the generator has run, so the order saves a round of errors.

## Keep the seed on a network object

Add `NetworkLevel.cs` next to the models folder, in `Assets/Scripts/RedRunner/Networking`:

```csharp
using Fomoxa.Unity;
using Generated;
using RedRunner.Networking.Models;
using RedRunner.TerrainGeneration;
using UnityEngine;

namespace RedRunner.Networking
{
    public sealed class NetworkLevel : NetworkBehaviour
    {
        [SerializeField]
        private TerrainGenerator m_Generator;

        private readonly LevelState m_State = new LevelState();

        public uint Seed => m_State.Seed;

        public TerrainGenerator Generator => m_Generator;

        protected override void OnRegisterState(NetworkState state)
        {
            state.Use(LevelStateNetAdapter.Instance, m_State, changed => Build());
        }

        public override void OnStartServer()
        {
            m_State.Seed = (uint)new System.Random().Next(1, int.MaxValue);
            Build();
        }

        public override void OnStartClient()
        {
            Build();
        }

        private void Build()
        {
            if (m_State.Seed != 0)
            {
                m_Generator.Build((int)m_State.Seed);
            }
        }
    }
}
```

`OnRegisterState` tells the package that `m_State` is this behaviour's state and which adapter encodes it. The server sends the state to every client when the object spawns for it, and sends changes after that.

On the server, `OnStartServer` picks the seed and builds the level. On a client, the package decodes the state into `m_State` before it calls `OnStartClient`, so the seed is already there. The `changed` callback covers a seed that arrives later. In host mode the server and the client share one instance, and the second `Build` call returns at once.

In `Play.unity`, create a root GameObject named `Network Level` and add `NetworkObject` and `NetworkLevel` to it. Drag the `Terrain Generator` object into the Generator field, then save the scene.

Saving gives the `NetworkObject` a scene object id. The package deactivates scene objects when the scene loads and activates them when they spawn: the server spawns every scene object when `ServerManager.StartConnection` runs, and a client spawns them when the server tells it to. Until a server starts, the level stays empty.

## Check the level

Nothing in the game starts a server yet; the next chapter adds that. To try the level now, start a server from a throwaway component in Play Mode, as in the previous chapter:

```csharp
server.ServerManager.StartConnection(0);
```

On the next frame `NetworkLevel.Seed` is set, the generator reports `IsBuilt`, and the scene holds 22 foreground blocks at the positions `Settings.Layout(seed)` lists. Calling `Layout` again with the same seed returns the same blocks, and another seed returns other blocks.

This repository runs those checks with `./Tools/unity-windows.sh play RedRunner.PortChecks.PortPlay.Level`.
