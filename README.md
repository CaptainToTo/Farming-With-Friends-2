# Farming With Friends 2

Example project for the OwlTree Unity add-on. View the add-on [here](https://github.com/CaptainToTo/owl-tree-unity).


Farming With Friends 2 is a sequel to the OwlTree prototype game [Farming With Friends](https://github.com/CaptainToTo/farm-with-friends). This is
a relayed peer-to-peer minimal game.

This example contains 2 programs. The monolith relay service, which is provided by OwlTree, and the Unity client.

## Starting The Relay Service

The relay service will manage multiple sessions of any app. You can learn more about the relay service in its README. Start the relay service
by opening a terminal in the "Relay" folder and running:

```
> dotnet run
```

## Unity Client

The Unity project, located in the "Farming" folder, is a simple showcase of using the OwlTree.Unity wrappers, the NetworkStateMachine add-on for
created a character controller, the OwlTree Matchmaking API, and how to create synchronized components and gameobject.

Playing from the MainMenu, the player can choose to either host a new session, or join an existing one by typing in a session id.
In game, players can move around WASD controls, jump with SPACE, and plant or harvest with LEFT MOUSE CLICK.