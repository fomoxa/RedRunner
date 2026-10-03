# Red Runner online with Fomoxa

This guide turns Red Runner, a single-player 2D platformer, into an online game built on the `com.fomoxa.unity` package. Each chapter is one commit on the `feature/fomoxa-port` branch of this repository, so you can follow a chapter in the Editor and compare your project with that commit.

## The game you end up with

One dedicated server runs the game. A player opens the client, types the server address on the start screen and is placed in the level straight away; there are no lobbies or rooms. Every player runs through the same level, which the server and each client build from a seed the server picks. The level has a start block, about twenty middle blocks and an end block. Reaching the end block sends the player back to the start.

The server decides when a player dies. Saws, spikes, water and the mace kill on the server, and the client shows the skeleton when the server says so, then the player respawns at the start. Coins and chests stay on each client as decoration and there is no score. Players pass through each other.

Your own character moves as soon as you press a key: the client predicts it with `Rigidbody2D` physics and corrects it when the server's state arrives. Other players are interpolated from the server.

## Chapters

1. [Install the package and add a NetworkManager](setup.md)
2. [Build the level from a seed](level.md)
3. [Join a server](joining.md)
4. [Move the player online](player.md)
5. [Death, respawn and the end of the level](lives.md)
6. [Move the mace with the server's tick](mace.md)

## Before you start

- Unity 6000.5 or later. `com.fomoxa.unity` needs 6000.5, and Red Runner's `master` branch is on 6000.2.
- Red Runner's `master` branch, which is the original single-player game.
- Some experience with the Unity Editor and C#. The guide covers the networking and assumes you know your way around the Editor.
