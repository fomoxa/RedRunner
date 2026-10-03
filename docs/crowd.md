# Play with two clients

Every chapter so far was checked with one client. This chapter runs a server and two clients and looks at what only shows up when players see each other: another player's character running, animating and turning, characters passing through each other, another player's death and respawn, and a character disappearing when its player leaves.

## Run a server and two clients

Build a Windows player, then start the server and two clients:

```
RedRunner.exe -batchmode -nographics -server -port 7777 -logFile server.log
RedRunner.exe
RedRunner.exe
```

Join `localhost` from both clients, or use the Editor as one of them. Each client shows its own character, predicted, and the other one, interpolated from the server.

![Two players running from the start block while a third falls into the water](.gitbook/assets/two-players.png)

Things to look for:

- Both characters start on the start block and overlap without pushing each other.
- When one player runs, the other client sees the character run with its running animation and face the way it runs.
- When one player dies, the other client sees the skeleton, then the character back at the start two seconds later.
- When one client quits, its character disappears from the other client.

## Animate a character that teleports

The first run of the check below found one problem. A character you do not control animates from how far it moved since the last frame (see the chapter on moving the player). When it respawns, its `NetworkTransform` carries it from where it died back to the start within a few frames, and that distance divided by a frame time gave the animator a running speed in the thousands. `LateUpdate` now treats a move longer than `m_TeleportDistance` (2 units by default) as a teleport, and limits the speed to what the character can reach by running and jumping:

```csharp
void LateUpdate ()
{
	Vector2 velocity = m_Rigidbody2D.linearVelocity;
	if ( m_Rigidbody2D.bodyType == RigidbodyType2D.Kinematic && Time.deltaTime > 0f )
	{
		Vector2 moved = transform.position - m_LastPosition;
		velocity = moved.magnitude > m_TeleportDistance ? Vector2.zero : Vector2.ClampMagnitude ( moved / Time.deltaTime, m_MaxRunSpeed + m_JumpStrength );
	}
	m_LastPosition = transform.position;
	m_Speed = new Vector2 ( Mathf.Abs ( velocity.x ), Mathf.Abs ( velocity.y ) );
	m_Animator.SetFloat ( "Speed", m_Speed.x );
	m_Animator.SetFloat ( "VelocityX", m_Speed.x );
	m_Animator.SetFloat ( "VelocityY", velocity.y );
```

## Stop a character whose player left

The first run also showed what happens when a client quits. Over UDP the server gets no signal from the transport: Fomoxa's UDP transport has no close message, and the server learns that the peer is gone from the heartbeat, after 5 seconds of silence and 15 more seconds without an answer. Until then the package applied the player's last input again on every tick, so a player who quit while running kept running and dying for 20 seconds.

The package now handles a client that stops on purpose. `ClientManager.StopConnection`, which `NetworkGame.Leave` calls and which `NetworkManager` also calls when the game quits, sends a `PeerLeave` message three times before it closes the connection. The server ends that peer at once and despawns its character.

A client that disappears without stopping (a crash, a lost network) still leaves the server waiting for the heartbeat. `NetworkManager` has Handshake Timeout Seconds, Heartbeat Interval Seconds and Heartbeat Timeout Seconds for that wait; Red Runner keeps the defaults. During the wait the package repeats the last input and tells `Apply` how far back that input is with `InputContext.RepeatedTicks`. `NetworkPlayer.Apply` drops the axis once the input is more than `m_MaxRepeatedTicks` ticks old (10, which is 0.2 seconds at 50 ticks per second), so a short gap still repeats the run and a vanished player stands still:

```csharp
private void Apply(PlayerInput input, InputContext context)
{
    float tickDelta = (float)m_Game.Manager.TimeManager.TickDelta;
    bool fresh = !context.Repeated;
    float move = context.RepeatedTicks > m_MaxRepeatedTicks ? 0f : input.Move;
    m_Character.Simulate(move, fresh && input.Jump, fresh && input.Roll, tickDelta, !context.Replaying);
}
```

## Mark the respawn as a teleport

A respawn moves the character from where it died to the start in one tick. `NetworkTransform` sent that move like any other, and the other clients kept the death point and the start next to each other in their interpolation buffer, ready to draw the character sliding across the level. `NetworkPlayer.Respawn` now calls `NetworkTransform.Teleport()` right after it moves the body. The server then sends the new position as a settle that starts a new teleport generation, and a client that receives it clears its buffer and places the character at the start. If that message is lost, the first update of the new generation does the same.

## Known limits

> When a client crashes or loses its network, its character stays in the level, standing still, until the heartbeat ends the session: about 20 seconds with the default settings.


## Check it

This repository runs the session from WSL:

```bash
./Tools/unity-windows.sh play-server RedRunner.PortChecks.PortPlay.Crowd
```

The Editor joins the server process and stands still at the start. It then launches the player build a second time as a bot client (`-bot 127.0.0.1 -bot-seconds 45`, handled by `BotPlayer` in `Tools/PortChecks`), which runs right and jumps for 45 seconds and leaves. The check passes when the Editor client:

- first sees the bot's character at the spawn point, as a kinematic body;
- sees it run more than 5 units to the right, facing right, with an animator speed above 1;
- sees it die and respawn;
- sees it despawn after the bot leaves;
- has not been pushed while the bot's character ran through it;
- sees the bot's character despawn less than 2 seconds after it last moved, because the bot leaves with `PeerLeave`;
- never has two neighbouring snapshots more than 2 units apart in the bot's `NetworkTransform` buffer, which would mean interpolating across a respawn.

In the last run the Editor client saw the bot's character run to x 24, die and respawn nine times without a single buffered jump, with an animator speed of at most 11.5. Without the `Teleport()` call, the same check counted 268 frames with the death point and the start next to each other in the buffer. The server despawned the bot's character as the bot left, 45.2 seconds after it appeared, and the Editor client's own character never moved.

A second check covers a client that vanishes:

```bash
./Tools/unity-windows.sh play-server RedRunner.PortChecks.PortPlay.Crash
```

It starts the bot the same way, kills its process after eight seconds so that no `PeerLeave` is sent, and passes when, from 3.5 seconds after the kill (past a pending respawn), the bot's character no longer moves sideways and does not die again, and the server despawns it more than 10 seconds after the kill. In the last run the character stood still and was despawned 20.0 seconds after the kill. With the `RepeatedTicks` rule removed from `Apply`, the same check saw it run 11.4 units further and die three more times.
