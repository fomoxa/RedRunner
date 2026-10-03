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

The first run also showed what happens when a client quits. Over UDP the server gets no signal: Fomoxa's UDP transport has no close message, and the server learns that the peer is gone from the heartbeat, after 5 seconds of silence and 15 more seconds without an answer. Until then the package applies the player's last input again on every tick, marked as repeated, so a player who quit while running kept running and dying for 20 seconds.

`NetworkPlayer.Apply` now counts repeated ticks and drops the axis after `m_MaxRepeatedTicks` of them (10, which is 0.2 seconds at 50 ticks per second). A short gap in a player's input still repeats the run, and a player who left stands still until the server despawns the character:

```csharp
private void Apply(PlayerInput input, InputContext context)
{
    float tickDelta = (float)m_Game.Manager.TimeManager.TickDelta;
    bool fresh = !context.Repeated;
    m_RepeatedTicks = fresh ? 0 : m_RepeatedTicks + 1;
    float move = m_RepeatedTicks > m_MaxRepeatedTicks ? 0f : input.Move;
    m_Character.Simulate(move, fresh && input.Jump, fresh && input.Roll, tickDelta, !context.Replaying);
}
```

## Known limits

> The character of a player who left stays in the level, standing still, for about 20 seconds.

> When a character respawns, the other clients interpolate it from where it died to the start over one tick (20 ms), so one frame may show it halfway. `NetworkTransform` has no way yet to mark a move as a teleport.

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
- has not been pushed while the bot's character ran through it.
- sees the bot's character stand still for more than 10 seconds before it despawns.

In the last run the Editor client saw the bot's character run to x 24, die and respawn, with an animator speed of at most 21. After the bot left, its character stood still for 17.2 seconds until the server despawned it, and the Editor client's own character never moved.
