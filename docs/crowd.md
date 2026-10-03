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

## Known limits

> When a client quits, the server notices only when the connection times out, about 20 seconds later in the checks. Until then it keeps the character and applies that client's last input again on every tick, so a player who quit while running keeps running and dying.

> A respawned character slides back across the level on the other clients for the two ticks of interpolation, because the server moves it like any other position change.

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

In the last run the Editor client saw the bot's character run to x 24, die and respawn, with an animator speed of at most 21, and its own character did not move.
