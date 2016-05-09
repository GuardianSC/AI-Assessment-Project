using UnityEngine;
using System.Runtime.InteropServices;

public enum TankBattleMessage
{
    NONE,
    JOIN,
    GAME,
    QUIT
}

public enum TankMovementOptions
{
    HALT,
    FWRD,
    BACK,
    LEFT,
    RIGHT
}

public enum CannonMovementOptions
{
    HALT,
    LEFT,
    RIGHT
}

public enum HealthStatus
{
    DEAD,
    CRITICAL,
    HURT,
    HEALTHY
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct TankBattleCommand
{
    public TankBattleMessage msg;               // declare message type
    public TankMovementOptions tankMove;        // assign action for the tank treads
    public CannonMovementOptions cannonMove;    // TODO: implement rotation on tank gun

    public int fireWish;
    public int messageLength;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct TankTacticalInfo
{
    public int playerID;
    public int isAlive;

    public bool inSight;
    public Vector3 lastKnownPosition;
    public Vector3 lastKnownDirection;

    public Vector3 lastKnownTankForward;
    public Vector3 lastKnownCannonForward;

    public HealthStatus health;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct TankBattleStateData
{
    public int messageLength;

    public int playerID;
    public float currentHealth;

    public Vector3 position;
    public Vector3 forward;

    public Vector3 cannonForward;

    public int canFire;

    public int perceptCount;

    [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct, SizeConst = 3)]
    public TankTacticalInfo[] percepts;

    public int playerCount;
}
