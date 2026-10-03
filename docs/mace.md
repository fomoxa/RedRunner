# Move the mace with the server's tick

The mace and the saws that ride a track move with `PathFollower`. In the single-player game it moves in `Update` with `Time.deltaTime`, waits for the character to come in range before it starts (the mace has Smart turned on), and stops while `GameManager.gameRunning` is false. Online, that gives every process its own mace: the dedicated server never starts a game, so its mace never moves, while each client's mace starts when that client's character comes near. A player could then be crushed on screen by a mace that, on the server, was somewhere else.

This chapter turns the path into a fixed cycle measured in ticks. The position of every path follower becomes a function of the tick number, so the server and each client put it in the same place at the same tick without sending anything over the network. The mace no longer waits for a character.

## Turn the path into a cycle

`BuildCycle` runs the follower's movement rules once, tick by tick, from the first point of the path until the follower arrives at the first point again, and stores the position after every tick:

```csharp
protected virtual void BuildCycle (float tickDelta)
{
	m_CycleTickDelta = tickDelta;
	m_Cycle = new List<Vector3> ();
	IEnumerator<PathPoint> point = m_PathDefinition.GetPathEnumerator ();
	if (!point.MoveNext () || point.Current == null) {
		m_Cycle.Add (transform.position);
		return;
	}
	PathPoint first = point.Current;
	Vector3 position = first.transform.position;
	if (m_PathDefinition.Points.Count < 2) {
		m_Cycle.Add (position);
		return;
	}
	Vector3 smoothVelocity = Vector3.zero;
	float overTimeSpeed = 0f;
	int waitTicks = 0;
	bool left = false;
	int limit = Mathf.CeilToInt (600f / tickDelta);
	while (m_Cycle.Count < limit) {
		m_Cycle.Add (position);
		if (waitTicks > 0) {
			waitTicks--;
			if (waitTicks == 0) {
				overTimeSpeed = 0f;
				point.MoveNext ();
				left |= point.Current != first;
			}
			continue;
		}
		PathPoint target = point.Current;
		Vector3 destination = target.transform.position;
		if (target.moveType == PathPoint.MoveType.MoveTowards) {
			position = Vector3.MoveTowards (position, destination, tickDelta * target.speed);
		} else if (target.moveType == PathPoint.MoveType.Lerp) {
			position = Vector3.Lerp (position, destination, tickDelta * target.speed);
		} else if (target.moveType == PathPoint.MoveType.SmoothDamp) {
			position = Vector3.SmoothDamp (position, destination, ref smoothVelocity, target.smoothTime, Mathf.Infinity, tickDelta);
		} else if (target.moveType == PathPoint.MoveType.Acceleration) {
			position = Vector3.MoveTowards (position, destination, overTimeSpeed);
			overTimeSpeed = Mathf.Min (overTimeSpeed + target.acceleration, target.maxSpeed);
		}
		if ((position - destination).sqrMagnitude < m_Step * m_Step) {
			position = destination;
			smoothVelocity = Vector3.zero;
			if (left && target == first) {
				return;
			}
			waitTicks = Mathf.Max (1, Mathf.CeilToInt (DelayAt (target) / tickDelta));
		}
	}
}
```

The movement types are the same as in `Update`, with the tick length in place of the frame time. Three details differ from the frame-based version, so that the cycle closes exactly:

- On arrival the follower snaps to the point and its smoothing velocity resets.
- It holds still while it waits at a point.
- The Acceleration type gains speed once per tick instead of once per frame.

The delay at a point follows the same rules as before:

```csharp
protected virtual float DelayAt (PathPoint point)
{
	if (m_FollowDelays && m_PathDefinition.UseGlobalDelay) {
		return m_PathDefinition.GlobalDelay;
	}
	if (!m_FollowDelays) {
		return m_Delay;
	}
	return point.delay;
}
```

The loop gives up after 600 seconds of ticks, which only a path that never returns to its first point would reach.

## Place the follower for a tick

```csharp
public virtual Vector3 PositionAt (uint tick)
{
	return m_Cycle [(int)(tick % (uint)m_Cycle.Count)];
}
```

```csharp
public virtual void FollowTick (uint tick, float tickDelta)
{
	if (m_PathDefinition == null) {
		return;
	}
	if (m_Cycle == null || m_CycleTickDelta != tickDelta) {
		BuildCycle (tickDelta);
	}
	Vector3 position = PositionAt (tick);
	m_Velocity = (PositionAt (tick - 1) - position) / tickDelta;
	if (!m_Driven) {
		m_Driven = true;
		StopAllCoroutines ();
		m_Rigidbody2D = GetComponent<Rigidbody2D> ();
		transform.position = position;
		if (m_Rigidbody2D != null) {
			m_Rigidbody2D.position = position;
		}
	} else if (m_Rigidbody2D != null) {
		m_Rigidbody2D.MovePosition (position);
	} else {
		transform.position = position;
	}
}
```

The mace has a kinematic `Rigidbody2D`. `MovePosition` moves it during the next physics step, so a character the mace hits is pushed the way a moving body pushes, instead of overlapping a body that jumped. The first call places the follower directly.

`Velocity` keeps the sign the mace's collision code was written for (previous position minus current), so the mace still decides from its direction whether it slams.

Once `FollowTick` has run, `Update` returns at its first line and the frame-based movement, the range check and the `Stopped` flag no longer apply:

```csharp
void Update ()
{
	if (m_Driven) {
		return;
	}
```

## Drive the followers from the level

`NetworkLevel` already knows when the level exists. After it builds the blocks, it collects their path followers and moves them before every tick:

```csharp
private void Build()
{
    if (m_State.Seed == 0 || m_Generator.IsBuilt)
    {
        return;
    }

    m_Generator.Build((int)m_State.Seed);
    m_Followers = FindObjectsByType<PathFollower>(FindObjectsSortMode.None);
    m_Game.Manager.TimeManager.OnPreTick += MoveFollowers;
}
```

```csharp
private void MoveFollowers()
{
    TimeManager time = m_Game.Manager.TimeManager;
    uint tick = time.PredictionTick;
    float tickDelta = (float)time.TickDelta;
    foreach (PathFollower follower in m_Followers)
    {
        follower.FollowTick(tick, tickDelta);
    }
}
```

`TimeManager.OnPreTick` runs before the package steps physics. `PredictionTick` is the server's tick on the server and in host mode. On a client it is the tick the client is predicting, the same number the server uses when it applies that tick's input, so the mace stands where it will stand on the server when the local character reaches it.

`NetworkLevel` finds `NetworkGame` in `Awake` and unsubscribes in `OnDestroy`:

```csharp
private void Awake()
{
    m_Game = FindFirstObjectByType<NetworkGame>();
}
```

> When a client replays ticks to correct its character, the followers stay at the position of the current tick. A replay covers a few ticks, so the difference is small, and the server still decides whether the mace killed anyone.

## Check the mace

Join a server and watch a mace without going near it: it moves anyway, and every client shows it at the same point of its swing.

This repository checks it with:

```bash
SERVER_ARGS="-log-paths" ./Tools/unity-windows.sh play-server RedRunner.PortChecks.PortPlay.Path
```

With `-log-paths`, a component from `Tools/PortChecks` logs the position of the level's first moving follower every 10 ticks on the server. The client does the same for 10 seconds, and the check compares the two at the ticks both logged. In one run the mace was at the same position on both sides at 49 ticks, with a cycle of 436 ticks (8.7 seconds at 50 ticks per second).
