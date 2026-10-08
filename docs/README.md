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

## The original game

Commit `2b675b5` is the single-player game as it is on [BayatGames/RedRunner](https://github.com/BayatGames/RedRunner). The commits after it are the port.

## Checks

`Tools/README.md` describes the automated Play Mode checks of the port, which run a Windows Unity Editor from WSL.
