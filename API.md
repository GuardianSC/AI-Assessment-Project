# API

**TankBattleCommand**  

Byte Offset | Field                              | Description
------------|------------------------------------|-------------------------------
0           | TankBattleMessage msg              | The category or type of the message.
4           | TankMovementOptions tankMove       | Movement direction command for the treads.
8           | CannonMovementOptions cannonMove   | Movement direction command for the cannon.
12          | int fireWish                       | Is the tank attempting to fire?
16          | int messageLength                  | Number of bytes for this message. (Currently not used)
20          | int playerCount                    | Number of players currently participating in the battle.

**HealthStatus**  

Value    | Threshold
---------|-----------
HEALTHY  | > 70%
HURT     | > 30%
CRITICAL | > 0%
DEAD     | <= 0%

**TankTacticalInfo**

Byte Offset | Field                              | Description
------------|------------------------------------|-------------------------------
0           | int playerID                       | Player ID of the tank being identified.
4           | int inSight                        | Is the enemy tank in sight of the player's tank?
8           | int isAlive                        | Is the enemy tank alive and participating in the current round?
12          | Vector3 lastKnownPosition          | The last known position of the enemy tank.
24          | Vector3 lastKnownDirection         | The last known direction of the enemy tank, relative to the player's tank.
36          | Vector3 lastKnownForward           | The last known forward direction of the enemy tank.
48          | Vector3 lastKnownCannonForward     | The last known forward direction of the enemy tank's cannon.
60          | HealthStatus status                | An approximation of the enemy's health.

*Conditions*  

- Information on an enemy tank is recorded when any of the following conditions
are met.
- The last known tank _position_, _forward_, and _health_ values are changed
when one of the following is met:  
  - The enemy tank is in the view of the tank cannon.  
    - The field-of-view (FOV) for the tank cannon is 90 degrees.
    - The vision distance limit is 50m.
  - The enemy tank has fired within 1500m of your tank.  
  - The enemy tank has been defeated.
- The last known tank _direction_ value is changed when one of the following is met:  
  - Any of the conditions for updating the last known position and forward of a tank.
  - The enemy tank is moving (not turning) within a 30m radius.  
- The last known cannon _forward_ value is changed when one of the following is met:
  - Any of the conditions for updating the last known position and forward of a tank.
  - The enemy tank has fired within 1500m of your tank.
- The last known tank _health status_ is updated when any of the following
conditions are met.
    - When a tank dies.
    - When a tank is in view.

**TankBattleStateInfo**  

Byte Offset | Field                              | Description
------------|------------------------------------|-------------------------------
0           | int messageLength                  | Number of bytes for this message. (not implemented)
4           | int playerID                       | Player ID of the tank receiving input.
8           | float currentHealth                | The current health of this tank.
12          | Vector3 position                   | The current position of this tank in world-space.
24          | Vector3 forward                    | The current forward of this tank in world-space.
36          | Vector3 cannonForward              | The current forward of this tank's cannon in world-space.
48          | int canFire                        | Is the tank currently allowed to fire?
52          | int tacticalCount                  | How many tanks have been recorded into the reconnaissance log?
56          | TankTacticalInfo tacticalData[3]   | An array of tactical info.

## tankNet

### init

```C++
// Attempts to locate and form a TCP connection with the server
// - serverAddress :: IP address of server, defaults to 127.0.0.1 (AKA localhost)
// - serverPort :: listening port on server
// Returns true if the host server could be resolved, otherwise returns false
bool init(char * serverAddress="127.0.0.1", int serverPort = 11100);
```

### update

```C++
// Sends data upstream and checks the TCP queue for any information
// - timeToBlock :: Time in seconds to block execution while waiting for information.
// Returns false if an error has occurred, otherwise returns true.
bool update(double timeToBlock=0.0);
```

### send

```C++
// Enqueues a tank command for transmission to the server on the next update.
void send(TankBattleCommand command);
```

### recieve

```C++
// Pulls the latest state information recieved from the server.
// Returns a pointer to the last state data recieved. This does NOT force a state update.
TankBattleStateData * recieve();
```

### isConnected

```C++
// Returns true if tankNet is currently connected to a server.
bool isConnected();
```

### isProvisioned

```C++
// Returns true if tankNet has recieved at last one valid state update from the server.
bool isProvisioned();
```

### term

```C++
// Forcibly closes the socket to the server and executes tankNet cleanup routines.
void term();
```
