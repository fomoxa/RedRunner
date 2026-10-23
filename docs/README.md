# Red Runner online with Fomoxa

This repository turns Red Runner, a single-player 2D platformer by Bayat Games, into an online game built on the [`com.fomoxa.networking`](https://github.com/fomoxa/com.fomoxa.networking) package. The guide to the port is part of the package documentation: [Examples > Red Runner](https://github.com/fomoxa/unity-docs/blob/main/examples/redrunner/README.md). It describes the offline game, the nine steps of the migration with the commit of each step, the finished architecture and troubleshooting.

![Two players running near the start of a seeded level](.gitbook/assets/two-players.png)

## Run the game

1. Clone this repository and open it with Unity 6000.5 or later on Windows x64 or Linux x64. Package Manager installs `com.fomoxa.networking` from its Git URL.
2. Build a player with `Assets/Scenes/Play.unity` as its only scene.
3. Start a dedicated server, then start clients and click Play with the server's address in the field under the buttons:

```
RedRunner.exe -batchmode -nographics -server -port 7777 -logFile server.log
RedRunner.exe
```

`RedRunner.exe -host` runs a server and a client in one process. In the Editor, press Play and join a server the same way.

Run with the left and right arrow keys, jump with Space, W or the up arrow, and roll with Shift.

## Run the console server

`Server~/` is a dedicated server that runs without Unity, on .NET 8 on Windows x64 or Linux x64. Clients built from the Unity project join it as they join the Unity server. It builds the level from the seed with the same rules as the Unity server and runs the player on the Rapier physics backend, so deaths and respawns follow the same rules too. Unity ignores the folder because its name ends with `~`.

The project compiles the networking package from source, so clone the package and its Rapier backend next to this repository:

```
git clone https://github.com/fomoxa/com.fomoxa.networking.git
git clone https://github.com/fomoxa/networking-rapier.git
git clone https://github.com/fomoxa/RedRunner.git
```

Then build and start it with the .NET 8 SDK:

```
cd RedRunner
dotnet run --project Server~ -c Release -- -port 7777 -logFile server.log
```

The server targets .NET 8. If only a newer runtime is installed, `dotnet run` stops with "You must install or update .NET to run this application"; install the .NET 8 runtime, or set `DOTNET_ROLL_FORWARD=LatestMajor` before `dotnet run` to run it on the newer one.

`Packages/manifest.json` installs both packages from the default branch of their Git URLs, which is what these clones check out. When Unity resolves them, it records the commit of each in `Packages/packages-lock.json` (`hash`); if that commit is older than your clones, check it out in the clone, so that the server and the clients run the same package code.

`-port` is the UDP port (7777 by default), and `-logFile` copies every log line to a file. To use other locations for the two packages, pass `-p:Core=<path to com.fomoxa.networking>` and `-p:Rapier=<path to com.fomoxa.networking.rapier>` to `dotnet build` or `dotnet run`. The build copies `LevelCatalogue.bytes`, the scene file of `Play.unity` and the native Rapier libraries for Windows x64 and Linux x64 next to the server.

The server reads the level from `Assets/Scripts/RedRunner/Networking/LevelCatalogue.bytes` and the network objects of `Play.unity` from the scene file that the package's code generator writes to `Assets/Scripts/RedRunner/Networking/Generated/`. After you change a block prefab, the player prefab or `Play.unity`, export the catalogue again with `Tools > RedRunner > Export Level Catalogue` and let the package regenerate its code, then rebuild the server.

## The original game

Commit `2b675b5` is the single-player game as it is on [BayatGames/RedRunner](https://github.com/BayatGames/RedRunner). The commits after it are the port.

## Checks

`Tools/README.md` describes the automated Play Mode checks of the port, which run a Windows Unity Editor from WSL against the Unity server or the console server.
