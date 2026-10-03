# Move the player online

This chapter sends each player's keys to the server and moves the character on both sides. The client does not wait for the server: it applies its own input at once, keeps a history of where its character was, and corrects itself when the server's result differs. This is client-side prediction, and the package runs it for any behaviour that registers an input model. Other players' characters follow the positions the server sends.

## Two models

The input of one tick holds the horizontal axis and the two buttons. The motion is what the client compares with the server after each tick:

```csharp
using Fomoxa;

namespace RedRunner.Networking.Models
{
    [Network]
    [Codec("net")]
    public class PlayerInput
    {
        [Network("f32")]
        [Codec("net")]
        public float Move { get; set; }

        [Network("bool")]
        [Codec("net")]
        public bool Jump { get; set; }

        [Network("bool")]
        [Codec("net")]
        public bool Roll { get; set; }
    }
}
```

```csharp
using Fomoxa;

namespace RedRunner.Networking.Models
{
    [Network]
    [Codec("net")]
    public class PlayerMotion
    {
        [Network("f32")]
        [Codec("net")]
        public float X { get; set; }

        [Network("f32")]
        [Codec("net")]
        public float Y { get; set; }

        [Network("f32")]
        [Codec("net")]
        public float VelocityX { get; set; }

        [Network("f32")]
        [Codec("net")]
        public float VelocityY { get; set; }

        [Network("f32")]
        [Codec("net")]
        public float RunSmoothVelocity { get; set; }
    }
}
```

Neither model needs a `[NetworkChannel]`. The package carries input inside its own input frames and the motion inside its own reconcile message.

`RunSmoothVelocity` is in the motion because Red Runner accelerates past its run speed with `Mathf.SmoothDamp`, and the damping velocity carries over from one tick to the next. Without it, a client that replays a few ticks would start the acceleration from a different value than the server and drift.

As in the level chapter, save the models and let the generator write `PlayerInputNetAdapter` and `PlayerMotionNetAdapter` before writing the code that uses them.

## Simulate one tick of the character

The single-player character moved in `Update`, once per frame, with `Time.deltaTime`. Prediction needs a step that runs once per tick with the tick length, gives the same result on the server and on the client, and can run again when the client replays. `RedCharacter.Simulate` is that step:

```csharp
public virtual void Simulate ( float horizontalAxis, bool jump, bool roll, float deltaTime, bool effects )
{
	float speed = Mathf.Abs ( m_Rigidbody2D.linearVelocity.x );
	m_CurrentRunSpeed = m_RunSpeed;
	if ( speed >= m_RunSpeed )
	{
		m_CurrentRunSpeed = Mathf.SmoothDamp ( speed, m_MaxRunSpeed, ref m_CurrentSmoothVelocity, m_RunSmoothTime, Mathf.Infinity, deltaTime );
	}
	Move ( horizontalAxis );
	if ( jump )
	{
		Jump ( effects );
	}
	if ( roll )
	{
		Roll ( effects );
	}
}
```

`effects` is false when the client replays a tick it has already shown. The jump plays its animation, particles and sound only when `effects` is true, so a replay does not play them a second time:

```csharp
public virtual void Jump ( bool effects )
{
	if ( !IsDead.Value )
	{
		if ( m_GroundCheck.Check () )
		{
			Vector2 velocity = m_Rigidbody2D.linearVelocity;
			velocity.y = m_JumpStrength;
			m_Rigidbody2D.linearVelocity = velocity;
			if ( effects )
			{
				m_Animator.ResetTrigger ( "Jump" );
				m_Animator.SetTrigger ( "Jump" );
				m_JumpParticleSystem.Play ();
				AudioManager.Singleton.PlayJumpSound ( m_JumpAndGroundedAudioSource );
			}
		}
	}
}
```

The jump asks `GroundCheck.Check()` instead of reading `IsGrounded`. `IsGrounded` is updated once per frame, but during a replay the character is at an earlier position, so the ground test has to run at the moment of the jump. `GroundCheck.Update` now calls the same method:

```csharp
void Update ()
{
	bool grounded = Check ();

	if (grounded && !m_IsGrounded) {
		if (OnGrounded != null) {
			OnGrounded ();
		}
	}

	m_IsGrounded = grounded;
}
```

Rolling becomes a method too. The original pushed the character along `localScale.z`, which is always 1, so it always rolled to the right; the port pushes along `localScale.x`, the way the character faces:

```csharp
public virtual void Roll ( bool effects )
{
	if ( !IsDead.Value )
	{
		m_Rigidbody2D.AddForce ( new Vector2 ( Mathf.Sign ( transform.localScale.x ) * m_RollForce, 0f ) );
		if ( effects )
		{
			m_Animator.SetTrigger ( "Roll" );
		}
	}
}
```

`RunSmoothVelocity` exposes the damping velocity for the motion model:

```csharp
public virtual float RunSmoothVelocity
{
	get
	{
		return m_CurrentSmoothVelocity;
	}
	set
	{
		m_CurrentSmoothVelocity = value;
	}
}
```

## Animate characters you do not control

`LateUpdate` feeds the animator from the `Rigidbody2D` velocity. On a client, another player's character is a kinematic body that the package moves to the positions its `NetworkTransform` receives, so its velocity is zero and it would slide across the level in its idle pose. When the body is kinematic, the velocity now comes from how far the character moved since the last frame:

```csharp
void LateUpdate ()
{
	Vector2 velocity = m_Rigidbody2D.linearVelocity;
	if ( m_Rigidbody2D.bodyType == RigidbodyType2D.Kinematic && Time.deltaTime > 0f )
	{
		velocity = ( transform.position - m_LastPosition ) / Time.deltaTime;
	}
	m_LastPosition = transform.position;
	m_Speed = new Vector2 ( Mathf.Abs ( velocity.x ), Mathf.Abs ( velocity.y ) );
	m_Animator.SetFloat ( "Speed", m_Speed.x );
	m_Animator.SetFloat ( "VelocityX", m_Speed.x );
	m_Animator.SetFloat ( "VelocityY", velocity.y );
```

## Send input and correct the prediction

`NetworkPlayer` now does the networking of the character:

```csharp
using System;
using Fomoxa.Unity;
using Fomoxa.Unity.Simulation;
using Generated;
using RedRunner.Characters;
using RedRunner.Networking.Models;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace RedRunner.Networking
{
    [RequireComponent(typeof(RedCharacter))]
    public sealed class NetworkPlayer : NetworkBehaviour
    {
        private const float PositionTolerance = 0.01f;
        private const float VelocityTolerance = 0.05f;

        private RedCharacter m_Character;
        private NetworkGame m_Game;
        private bool m_JumpPressed;
        private bool m_RollPressed;

        public RedCharacter Character => m_Character;

        private void Awake()
        {
            m_Character = GetComponent<RedCharacter>();
            m_Game = FindFirstObjectByType<NetworkGame>();
        }

        protected override void OnRegisterInput(NetworkInput input)
        {
            input.Use(PlayerInputNetAdapter.Instance, Gather, Apply);
            input.Reconcile(PlayerMotionNetAdapter.Instance, Capture, Restore, Matches);
        }

        public override void OnStartClient()
        {
            if (NetworkObject.IsOwner)
            {
                m_Game.SetLocalPlayer(this);
            }
        }

        public override void OnStopClient()
        {
            m_Game.ClearLocalPlayer(this);
        }

        private void Update()
        {
            if (NetworkObject == null || !NetworkObject.IsOwner)
            {
                return;
            }

            m_JumpPressed |= CrossPlatformInputManager.GetButtonDown("Jump");
            m_RollPressed |= CrossPlatformInputManager.GetButtonDown("Roll");
        }

        private void Gather(PlayerInput input)
        {
            bool playing = GameManager.Singleton.gameRunning;
            input.Move = playing ? CrossPlatformInputManager.GetAxis("Horizontal") : 0f;
            input.Jump = playing && m_JumpPressed;
            input.Roll = playing && m_RollPressed;
            m_JumpPressed = false;
            m_RollPressed = false;
        }

        private void Apply(PlayerInput input, InputContext context)
        {
            float tickDelta = (float)m_Game.Manager.TimeManager.TickDelta;
            bool fresh = !context.Repeated;
            m_Character.Simulate(input.Move, fresh && input.Jump, fresh && input.Roll, tickDelta, !context.Replaying);
        }

        private void Capture(PlayerMotion motion)
        {
            BodyState2D state = NetworkObject.Body2D.State;
            motion.X = state.Position.X;
            motion.Y = state.Position.Y;
            motion.VelocityX = state.Velocity.X;
            motion.VelocityY = state.Velocity.Y;
            motion.RunSmoothVelocity = m_Character.RunSmoothVelocity;
        }

        private void Restore(PlayerMotion motion)
        {
            PhysicsBody2D body = NetworkObject.Body2D;
            BodyState2D state = body.State;
            state.Position = new System.Numerics.Vector2(motion.X, motion.Y);
            state.Velocity = new System.Numerics.Vector2(motion.VelocityX, motion.VelocityY);
            body.State = state;
            m_Character.RunSmoothVelocity = motion.RunSmoothVelocity;
        }

        private static bool Matches(PlayerMotion server, PlayerMotion predicted) =>
            Math.Abs(server.X - predicted.X) <= PositionTolerance
            && Math.Abs(server.Y - predicted.Y) <= PositionTolerance
            && Math.Abs(server.VelocityX - predicted.VelocityX) <= VelocityTolerance
            && Math.Abs(server.VelocityY - predicted.VelocityY) <= VelocityTolerance
            && Math.Abs(server.RunSmoothVelocity - predicted.RunSmoothVelocity) <= VelocityTolerance;
    }
}
```

`OnRegisterInput` gives the package four callbacks.

`Gather` runs on the owner's client once per tick and fills the input. Button presses are caught in `Update`, which runs every frame, and kept until the next `Gather`, so a press shorter than a tick is not lost. While the pause screen is open, the input is empty.

`Apply` runs the tick. On the owner's client it runs as soon as the input is gathered, which is the prediction. On the server it runs at the tick the input belongs to, once the input has arrived. `InputContext.Repeated` is true when the server has no input for a tick and reuses the last one; a jump or roll is not repeated, only the axis. `InputContext.Replaying` is true while the client replays.

`Capture` and `Restore` read and write the motion. After each tick the server captures the character's motion and sends it to the owner, and the owner captures its own predicted motion for the same tick. When `Matches` finds them too far apart, the client restores the 2D physics of the scene to that tick, applies the server's motion, and runs `Apply` again for every later tick it has input for. Because Simulate Physics is on, the package also steps 2D physics between those ticks.

`NetworkObject.Body2D` is the package's view of the character's `Rigidbody2D`, with positions and velocities as `System.Numerics.Vector2`.

The tolerances are 0.01 for positions and 0.05 for velocities. The server and the client run the same physics on the same blocks, so their results stay close; in the check below, six seconds of running and jumping needed one correction.

## Follow the local player

When `NetworkPlayer` reports the local player, `NetworkGame` points the camera and the background at it:

```csharp
internal void SetLocalPlayer(NetworkPlayer player)
{
    LocalPlayer = player;
    if (CameraController.Singleton != null)
    {
        CameraController.Singleton.Follow(player != null ? player.transform : null);
    }

    if (TerrainGenerator.Singleton != null)
    {
        TerrainGenerator.Singleton.Follow(player != null ? player.Character : null);
    }
    LocalPlayerChanged?.Invoke(player);
}
```

## Other players

Nothing else is needed for the other characters. The server moves them, `NetworkTransform` sends their positions and facing, and each client interpolates two ticks behind the newest position it has. They pass through your character and each other, as the joining chapter described.

## Check movement

Join a server, press the arrow keys and Space. The character runs and jumps without waiting for the server.

This repository checks it with `./Tools/unity-windows.sh play-server RedRunner.PortChecks.PortPlay.Move`. The check joins the server process through the start screen, then drives the input with `CrossPlatformInputManager` virtual axes: two seconds to the right, a jump, one second to the left. It passes when the client predicts its character, the character runs at least 5 units, rises at least 1.5 units and turns, and the camera and the background follow it. A run on the development machine moved the character 9.65 units and jumped 4.77 units high.
