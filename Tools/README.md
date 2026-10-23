# Checking the port from WSL

These scripts run the Unity Editor installed on Windows against this project while the repository lives in WSL. The Windows Editor refuses to open a project on the case-sensitive WSL file system, so `unity-windows.sh` copies the project to `C:\Users\<user>\redrunner-unity-check\RedRunner` and runs Unity there in batch mode. The `com.fomoxa.networking` package from the `unity` repository is copied next to it, to `redrunner-unity-check\unity\com.fomoxa.networking`, and the `com.fomoxa.networking.rapier` package from the `networking-rapier` repository to `redrunner-unity-check\networking-rapier\com.fomoxa.networking.rapier`. `Packages/manifest.json` in this repository installs both packages from their Git URLs; in the copy, the script replaces those entries with `file:../../unity/com.fomoxa.networking` and `file:../../networking-rapier/com.fomoxa.networking.rapier` and drops both packages from `packages-lock.json`, so the checks run against your local packages, including changes you have not pushed. The two package files are not copied back.

You do not need any of this to play or build the game. Open the project in the Unity Editor as usual.

## Requirements

- Unity 6000.5.7f1 installed through Unity Hub on Windows. Change `UNITY_VERSION` at the top of the script for another version.
- The Windows user name in `WIN_USER` (default `admin`).
- The `unity` and `networking-rapier` repositories cloned next to this one.
- For `play-console`: the .NET 9 SDK installed on Windows, at `C:\Program Files\dotnet\dotnet.exe`.

## Commands

```bash
./Tools/unity-windows.sh check
./Tools/unity-windows.sh run RedRunner.PortChecks.PortSetup.Networking
./Tools/unity-windows.sh play RedRunner.PortChecks.PortPlay.Connection
./Tools/unity-windows.sh play-server RedRunner.PortChecks.PortPlay.Join
./Tools/unity-windows.sh play-console RedRunner.PortChecks.PortPlay.Join
./Tools/unity-windows.sh shots
```

Unity writes its log to `unity-<command>.log` in the Windows copy, one file per command, so a log another program keeps open does not block the next run.

`check` opens the project, compiles it and quits. `run` calls an Editor method and quits. `play` calls an Editor method that enters Play Mode; the check running in Play Mode quits the Editor itself, and the script stops it after 240 seconds if it does not. `play-server` first starts the player built by `PortBuild.Player` as a dedicated server (`-batchmode -nographics -server -port 7777`, log in `server.log`), adds the words of the `SERVER_ARGS` environment variable to its command line, waits for its `server listening` line, runs the Play Mode check, then stops the server and copies its log to `Logs/unity-windows-server.log`. `play-console` does the same with the console server of `Server~/` in place of the player: it builds the Windows copy of `Server~` with the Windows `dotnet.exe` (target framework changed to `net9.0` in the copy, `com.fomoxa.networking` taken from `redrunner-unity-check\unity`, build log in `Logs/unity-windows-console-build.log`) and starts it with `-port 7777 -logFile server.log` and the words of `SERVER_ARGS`. Every check that runs with `play-server` also runs with `play-console`.

Each command writes the Unity log to `Logs/unity-windows-<command>.log` and prints the compile errors and the `PORT-CHECK` lines it finds there. Exit code 0 with a `PORT-CHECK ... PASS` line means the check passed.

After Unity exits, the script copies every file whose content changed in the Windows copy back into the repository, so generated code, upgraded project settings and saved scenes come back. Review `git status` afterwards: Unity sometimes re-saves assets you did not touch.

## Check scripts

`Tools/PortChecks/` holds the Editor methods and the Play Mode checks. The script copies the folder into the Windows copy as `Assets/PortChecks/`, so the game project in the repository never compiles them.

| Method | What it does |
|---|---|
| `PortSetup.Networking` | Creates the `FomoxaSettings` asset, runs the Fomoxa generator, puts `Play.unity` in the build scene list and adds the `Network Manager` object with a UDP transport to `Play.unity` |
| `PortSetup.Level` | Sets the generation settings to 20 middle blocks and the start block as the end block, and adds the `Network Level` object to `Play.unity` |
| `PortSetup.Joining` | Adds `NetworkObject`, `NetworkTransform` and `NetworkPlayer` to `RedRunner.prefab`, assigns prefab ids, removes the scene character, sets the `NetworkManager` to 50 ticks, physics simulation and 3 reconnect retries, stores the collected prefab and scene lists in the scene, adds `NetworkGame` and the address field, and wires the start and pause screens |
| `PortSetup.Score` | Deactivates the coin panel and the score texts of the in-game screen |
| `PortBuild.Player` | Builds a Windows player of `Play.unity` into `Builds/Windows/RedRunner.exe` in the Windows copy |
| `PortDump.Project` | Writes the `Play.unity` hierarchy, the prefabs, the generation settings, the 2D layer matrix and the input axes to `port-dump.txt`, for reading binary assets from WSL |
| `PortPlay.Connection` | Opens `Play.unity`, starts a server on the scene's `NetworkManager`, connects a second `NetworkManager` as a client over UDP on `127.0.0.1` and passes once the client reaches `Started`. From the level chapter on, the in-process client stops right after that with an `UnknownSceneObject` mismatch, because the scene's network objects are already spawned by the server in the same process; `PortPlay.Join` checks a real client |
| `PortPlay.Level` | Starts a server in `Play.unity` and checks that the level is empty before, then holds the 22 blocks of `Layout(seed)` at their positions, that the layout depends only on the seed, and that a second `Build` adds nothing |
| `PortPlay.Host` | Starts a host from `NetworkGame` and checks that the level is built, the local character spawns, is owned and stands on the start block |
| `PortPlay.Join` | Run with `play-server` or `play-console`: joins the server process at `127.0.0.1` and checks the same, plus that the client's seed matches the seed in the server log |
| `PortPlay.Move` | Run with `play-server` or `play-console`: joins through the start screen, drives virtual input (right, jump, left) and checks that the client predicts its character, that it runs, jumps and turns, and that the camera and the background follow it |
| `PortPlay.HostLife` | Starts a host, moves the host's character below the level and onto the end block, and checks the death by `Fall`, the respawn delay and the return to the start |
| `PortPlay.Life` | Run with `play-server` or `play-console`: joins, runs right and jumps until the character dies, and checks that the client shows the death only from the server's state, then the respawn |
| `PortPlay.Path` | Run with `SERVER_ARGS="-log-paths"` and `play-server` or `play-console`: compares the position of the first moving path follower on the server and the client at the same ticks for 10 seconds |
| `PortPlay.Crowd` | Run with `play-server` or `play-console`: joins, launches the player build again as a bot client (`BotPlayer`: `-bot <address> -bot-seconds <n>`), and checks how the Editor client sees the bot's character: kinematic, at the spawn point, running, animated, facing right, dying, respawning, passing through the local character, despawned less than 2 seconds after it last moved once the bot leaves, and never buffering two snapshots more than 2 units apart, which would interpolate across a respawn |
| `PortPlay.Crash` | Run with `play-server` or `play-console`: like `Crowd`, but kills the bot process after 8 seconds, so no `PeerLeave` is sent, and checks that the bot's character stops moving sideways, does not die again and is despawned by the heartbeat more than 10 seconds later |

`PathLog.cs`, `BotPlayer.cs` and `ScreenshotTaker.cs` are the scripts in `Tools/PortChecks` that are not editor-only. They end up in the player build made from the Windows copy and do nothing unless the player starts with `-log-paths`, `-bot` or `-screenshot`.

`shots` takes the screenshots used by the guide from the last player build, without opening the Editor. It starts a server, one bot without graphics and three players in batch mode with graphics: one stays on the start screen, two join as bots, and each writes a 1280 × 720 PNG after a few seconds (`-screenshot <file> -screenshot-after <seconds>`). The script copies them to `docs/.gitbook/assets/`.

A component that drives a Play Mode check must stay out of any `Editor` folder. Unity drops a MonoBehaviour from an Editor-only assembly out of the scene when it reloads the domain on entering Play Mode, without logging an error. The checks wrap their whole file in `#if UNITY_EDITOR` instead, and each check class sits in a file of the same name, which Unity needs to find a MonoBehaviour's script after the reload.

## When the copy is locked

If a run is interrupted, `Unity.exe`, the server `RedRunner.exe` or the console server `dotnet.exe` can stay alive on Windows and keep `Temp/UnityLockfile` or the server's files, and the next copy fails with `Permission denied`. The script stops any `Unity.exe`, `RedRunner.exe` or `dotnet.exe` whose command line contains `redrunner-unity-check` before each run, after a `play-server` or `play-console` run and after a run that timed out. Other Unity windows are left alone.
