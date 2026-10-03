# Install the package and add a NetworkManager

This chapter adds `com.fomoxa.unity` to Red Runner, sets up code generation and puts a `NetworkManager` in the game scene. The game does not open a connection yet; the start screen gets its address field in a later chapter. At the end of the chapter the project compiles with the generated code, and a short check connects a server and a client over UDP inside the `Play` scene.

## Open the project in Unity 6000.5

Check out Red Runner's `master` branch and open it with Unity 6000.5 or later. The project was saved with 6000.2.6f2, so Unity asks to upgrade it. When the upgrade finishes, Package Manager has moved the Unity packages in `Packages/manifest.json` to the versions that ship with your Editor, and `ProjectSettings/ProjectVersion.txt` names the new version. Commit those changes on their own if you want the upgrade separate from the networking work.

## Add the package

During development this repository points at a local copy of the package. Clone `fomoxa-unity` next to the Red Runner folder and add this line at the top of `dependencies` in `Packages/manifest.json`:

```json
"com.fomoxa.unity": "file:../../fomoxa-unity/com.fomoxa.unity",
```

The path is relative to the `Packages` folder, so the entry works on any machine that has both repositories side by side.

To install from Git instead, use Install package from git URL with the repository URL and the package folder as the `path` query:

```
https://github.com/<owner>/fomoxa-unity.git?path=/com.fomoxa.unity
```

The package depends on the built-in physics, physics 2D and animation modules, which Red Runner already has.

## Create the Fomoxa settings

Messages on the wire are plain C# classes, called models, marked with `[Network]` and `[Codec("net")]`. The package runs the `fomoxac` compiler on them to generate the codecs and the registration code. It needs to know where your models live and where to write the generated code, and it reads both from a `FomoxaSettings` asset.

1. Create the folders `Assets/Scripts/RedRunner/Networking/Models` and `Assets/Scripts/RedRunner/Networking/Generated`.
2. Right-click `Assets/Scripts/RedRunner/Networking` and choose Create > Fomoxa > Settings.
3. In the new asset, set Models Folder to `Assets/Scripts/RedRunner/Networking/Models` and Generated Folder to `Assets/Scripts/RedRunner/Networking/Generated`.

Keep exactly one `FomoxaSettings` asset in the project; the package logs an error and generates nothing when it finds two.

The models folder stays empty until the next chapter, but it has to exist, because `fomoxac` stops with `No such file or directory` when a source folder is missing. Git does not keep empty folders, so add an empty `.gitkeep` file to it. Unity skips files whose names start with a dot.

## Generate the code

The package generates code after every script compilation. To run it by hand, choose Tools > Fomoxa > Generate. A run writes these files:

| Path | Contents |
|---|---|
| `fomoxa.toml` | The `fomoxac` configuration, written from the settings asset. Edit the asset, not this file. |
| `FomoxaSystemModels/` | The package's own models (spawn, state, transform, clock, input and others), copied out of the package so that `fomoxac` can read them. The folder sits outside `Assets`, so Unity does not compile it. |
| `.fomoxa/` | The schema and build graph that `fomoxac` writes next to the code. |
| `Assets/Scripts/RedRunner/Networking/Generated/` | Codecs and adapters for every model, `NetSchema.cs`, `FomoxaAdapters.cs`, the list of network prefabs and the list of network scenes. |

Commit all four, so that a fresh clone compiles before the generator has run.

## Put the Play scene in the build

Red Runner keeps the whole game in `Assets/Scenes/Play.unity`, but the scene list in Build Profiles of the `master` branch names `Assets/Scenes/Gooose.unity`, a scene that does not exist. Open File > Build Profiles, remove the missing scene from the Scene List and add `Assets/Scenes/Play.unity`.

The package keeps its own list of network scenes in `Generated/FomoxaSceneList.asset` and refreshes it whenever the build scene list changes. The server and the client find scenes by the ids in that list.

## Add the NetworkManager

Open `Play.unity` and create an empty GameObject at the root of the scene named `Network Manager`. Add two components to it:

1. `UdpNetworkTransport`, which carries the connection over UDP.
2. `NetworkManager`. Drag the `UdpNetworkTransport` component into its Transport field.

`NetworkManager` would add a UDP transport by itself when it starts without one, but assigning it in the scene shows which transport the game uses. Leave the other fields at their defaults for now: Tick Rate 30, Timing Mode Tick, Simulate Physics off. The chapter on player movement turns on physics simulation.

The Collected Prefabs and Collected Scenes fields stay empty in the scene. The package fills them from the generated lists when it builds the scene for Play Mode or for a player build.

Save the scene. A `NetworkManager` does nothing until code calls `ServerManager.StartConnection` or `ClientManager.StartConnection`, so the game still plays as before.

## Check the connection

You can check the setup with a few lines in a throwaway component. With `server` being the `NetworkManager` in the scene, start a server, create a second manager as a client in the same Play Mode session and connect it to the server's port:

```csharp
server.ServerManager.StartConnection(0);
var clientObject = new GameObject("Check Client");
clientObject.AddComponent<UdpNetworkTransport>();
NetworkManager client = clientObject.AddComponent<NetworkManager>();
client.ClientManager.StartConnection("127.0.0.1", server.ServerManager.Port);
```

Port `0` lets the operating system pick a free port, and `ServerManager.Port` reports the one it picked. After a few frames `client.ClientManager.State` is `ConnectionState.Started` and `server.ServerManager.PeerCount` is 1.

This repository runs the same check from WSL with `./Tools/unity-windows.sh play RedRunner.PortChecks.PortPlay.Connection`. `Tools/README.md` in the repository describes the script.
