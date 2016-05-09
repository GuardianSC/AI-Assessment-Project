# TankBattle

TankBattle provides a base project and client lib for demonstrating communication
between a server and a client for the purpose of running a networked simulation.
There are three projects provided in this repository:

- TankServerApp
- TankClientLib
  - TankBattle-net
  - TankBattle-client-dyad-sfw

**TankServerApp** contains the Unity3D project which serves as the network server as
well as the simulation. Clients will send commands to the server over TCP sockets
and the server will process them every FixedUpdate. After processing, the server
will update each client, sending everyone a new snapshot of the current state of
their tank. Its codebase is compiled against _Unity3D v5.3.2f1_.

The game rules and other specifications can be reviewed in the [documentation](GAME-RULES.md).

**TankBattle-net** contains the basic C++ networking library for sending commands to the
server and receiving state updates from the server. It exposes a specific [API](API.md)
designed to specifically interface with the TankServer. Under the hood,
the library wraps [dyad](https://github.com/rxi/dyad), a C library for handling
TCP sockets.  

**TankBattle-client-dyad-sfw** contains the example client for use in connecting with the
server. It accepts keyboard input for controlling the tank and displays a
debug output in the game window, showing what information is received from the
server.

## Getting Started

### Building and Connecting the Basic Server and Client

To test whether or not the server and client can successfully communicate on your
machine, try using the existing example projects.

1. Open the Unity3D project located in the TankServerApp folder.
2. Hit play, and verify that there are no problems with initialization by looking
at the console output.
3. Open up the solution for the TankClientLib.
4. Build the solution. The build order should be configured to build TankBattle-net
first, which is a dependency of TankBattle-dyad-sfw. Upon successfully building,
start debugging to fire up the startup project which, by default, connects to
127.0.0.1, or localhost.
5. You should be able to focus the client game window and use the WASD-keys to move
around on the server.
6. To spin up a second client, right-click on the TankBattle-dyad-sfw project in
Visual Studio's Solution Explorer and hover over the "Debug" option. There should be
a menu entry for starting a new instance. Both of these instances will be picked up
by the debugger.

### Building and Linking Against TankBattle-net

To use TankBattle-net in your own program for interfacing with the provided
server, build and link the static library to your project.

1. Open up the solution for the TankClientLib.
2. Build the TankBattle-net project. You should find its binary in the solution
folder's configuration folder. (e.g. _TankClientLib/Debug_).
3. Import the header and binary into your project.
4. Include TankBattleNet.h in your project where appropriate and begin!

Alternatively, download the release on [GitHub](.../../releases). Import the header and
link against the library in your program.

### Example Project

You can review the [example client](TankClientLib/TankBattle-client-dyad-sfw/main.cpp)
included in the repository. It attempts to connect to a server on the local machine
and will present a debug window. You may focus the window and use the WASD-keys to
move your tank around on the server.

## License

This project is licensed under the [MIT/X11 License](LICENSE.md).
