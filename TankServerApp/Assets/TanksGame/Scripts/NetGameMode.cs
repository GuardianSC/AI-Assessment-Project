using UnityEngine;
using System.Collections.Generic;
using System;

// HACK: Tightly coupling NetGameMode with Unity GameMode...
using UnityGame.Tanks;
using System.IO;
using System.Net.Sockets;

public class NetGameMode : MonoBehaviour
{
    private SocketListener connectionSocket;
    public int gamePort = 11000;

    // HACK: let's not couple these together
    [SerializeField]
    private GameManager gameManager;

    private List<NetworkPlayer> networkPlayers = new List<NetworkPlayer>();

    // HACK: Unity API is not thread-safe, so can't subscribe to delegates that get trigger asynchronously
    void CheckNetworkEvents()
    {
        connectionSocket.paused = true;

        int readCount = connectionSocket.events.Count;
        for (int i = 0; i < readCount; ++i)
        {
            try
            {
                var evnt = connectionSocket.events[i];

                if(evnt != null)
                    OnNetworkEvent(evnt.data, evnt.eventArgs);
                else
                {
                    Debug.LogWarning("The network event was null.");
                    // not sure why this happens

                    continue;
                }
            }
            catch (Exception ex)
            {
                if (ex is NullReferenceException)
                {
                    var nullEx = ex as NullReferenceException;
                    Debug.LogError(nullEx.Message);
                }
                else if (ex is IndexOutOfRangeException)
                {
                    var indEx = ex as IndexOutOfRangeException;
                    Debug.LogError(indEx.Message);
                }
                else if (ex is ObjectDisposedException)
                {
                    Debug.LogWarning("Disconnected player message in the network queue.");
                }
                else if (ex is SocketException)
                {
                    Debug.LogWarning("Disconnected player message in the network queue.");
                }
                else
                {
                    Debug.LogError(ex.Message);
                }
            }
        }

        // remove events processed
        try
        {
            connectionSocket.events.RemoveRange(0, readCount);
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex.InnerException + ex.StackTrace);
        }
        connectionSocket.paused = false;
    }
    void UpdateClients()
    {
        foreach(var netPlayer in networkPlayers)
        {
            // don't update players that are no longer participating
            if (!netPlayer.isActive)
                continue;

            var netPlayerController = netPlayer.playerController as TankPlayerController;
            var netPlayerPawn       = (netPlayer.playerController.PawnMove as TankMovement).gameObject;

            var percepts = netPlayerPawn.GetComponent<TankPercepts>();

            int packetSize = DataUtils.SizeOf<TankBattleStateData>();

            var stateMsg = new TankBattleStateData();
            stateMsg.messageLength  = packetSize;
            stateMsg.playerID       = netPlayerController.pid;
            stateMsg.currentHealth  = netPlayerController.m_Health.m_CurrentHealth;
            stateMsg.position       = netPlayerController.PawnMove.position;
            stateMsg.forward        = netPlayerController.PawnMove.forward;
            stateMsg.cannonForward  = netPlayerController.TankGun.forward;
            stateMsg.canFire        = netPlayerController.TankFire.CanFire() ? 1 : 0;
            stateMsg.perceptCount   = percepts.reconInfo.Count;
            stateMsg.percepts       = new TankTacticalInfo[3];
            stateMsg.playerCount    = gameManager.m_CombatantCount;

            int perceptSlot = 0;
            foreach(var reconRecord in percepts.reconInfo.Values)
            {
                // currently only space for three percepts
                if (!(perceptSlot < 3))
                    break;

                stateMsg.percepts[perceptSlot] = reconRecord;

                perceptSlot++;
            }

            // pack reconnaissance
            MemoryStream packetStream = new MemoryStream(DataUtils.SizeOf<TankBattleStateData>() +
                                  DataUtils.SizeOf<TankTacticalInfo>() * percepts.reconInfo.Count);

            packetStream.Write(DataUtils.GetBytes(stateMsg), 0, DataUtils.SizeOf<TankBattleStateData>());
            foreach (var reconRecord in percepts.reconInfo)
            {
                packetStream.Write(DataUtils.GetBytes(reconRecord.Value), 0, DataUtils.SizeOf<TankTacticalInfo>());
            }

            connectionSocket.Send(netPlayer, packetStream.GetBuffer());           
        }
    }

    private void CleanUpConnections()
    {
        connectionSocket.StopListening();
    }

    // Instantiates a new player and returns its ID
    public TankPlayerController AddPlayer(NetworkPlayer netPlayer)
    {
        // record player in array
        networkPlayers.Add(netPlayer);

        netPlayer.playerController = gameManager.AddPlayer();

        // if null, we are at the max player count
        if (netPlayer.playerController == null)
        {
            Debug.LogWarning("Server is at max capacity.");
            return null;
        }

        netPlayer.playerController.isActive = true;

        GameObject newPawn = gameManager.SpawnSingleTank();
        netPlayer.playerController.Pawn = newPawn;
        netPlayer.playerController.PawnMove = newPawn.GetComponent<TankMovement>();

        var evilDowncasting = netPlayer.playerController as TankPlayerController;
        evilDowncasting.m_Instance = newPawn;
        evilDowncasting.Setup();

        evilDowncasting.TankGun = newPawn.GetComponent<CannonMovement>();
        evilDowncasting.TankFire = newPawn.GetComponent<TankShooting>();
        evilDowncasting.TankHealth = newPawn.GetComponent<TankHealth>();

        return netPlayer.playerController as TankPlayerController;
    }

    // Removes a player from the game by their ID
    public void RemovePlayer(NetworkPlayer netPlayer)
    {
        netPlayer.isActive = false;

        // if the player was provisioned, remove them from the game
        if (netPlayer.playerController != null)
        {
            netPlayer.playerController.isActive = false;
            gameManager.RemovePlayer(netPlayer.playerController as TankPlayerController); // HACK: omg downcast i am so sorry
        }

        // close socket
        netPlayer.remoteSocket.Shutdown(System.Net.Sockets.SocketShutdown.Both);
        netPlayer.remoteSocket.Disconnect(true);
        netPlayer.remoteSocket.Close();
    }

    private void OnNetworkEvent(byte[] data, SocketEventArgs e)
    {
        switch (e.socketEvent)
        {
            case SocketEventArgs.SocketEventType.ACCEPT:
                OnNetPlayerConnected(e.endpoint);
                break;
            case SocketEventArgs.SocketEventType.READ:
                OnNetPlayerData(e.endpoint, DataUtils.FromBytes<TankBattleCommand>(data));
                break;
            case SocketEventArgs.SocketEventType.DROP:
                OnNetPlayerDisconnected(e.endpoint);
                break;
        }
    }

    private void OnNetPlayerConnected(NetworkPlayer netPlayer)
    {
        var newPlayerController = AddPlayer(netPlayer);

        // are we at the max player count?
        if (newPlayerController == null)
        {
            RemovePlayer(netPlayer);

            return;
        }

        var welcomeMsg = new TankBattleStateData();
        welcomeMsg.playerID = newPlayerController.pid;

        connectionSocket.Send(netPlayer, DataUtils.GetBytes(welcomeMsg));

        Debug.Log("New player initialized at ID " + newPlayerController.pid + ".");
    }
    private void OnNetPlayerData(NetworkPlayer netPlayer, TankBattleCommand header)
    {
        switch (header.msg)
        {
            case TankBattleMessage.QUIT:
                RemovePlayer(netPlayer);
                break;
            default:
                break;
        }

        try
        {
            var pc = netPlayer.playerController as TankPlayerController;

            // process tank movement
            switch (header.tankMove)
            {
                case TankMovementOptions.FWRD:
                    pc.MoveForward(1.0f);
                    pc.MoveRight(0.0f);
                    break;
                case TankMovementOptions.BACK:
                    pc.MoveForward(-1.0f);
                    pc.MoveRight(0.0f);
                    break;
                case TankMovementOptions.LEFT:
                    pc.MoveForward(0.0f);
                    pc.MoveRight(-1.0f);
                    break;
                case TankMovementOptions.RIGHT:
                    pc.MoveForward(0.0f);
                    pc.MoveRight(1.0f);
                    break;
                case TankMovementOptions.HALT:
                    pc.MoveForward(0.0f);
                    pc.MoveRight(0.0f);
                    break;
                default:
                    Debug.LogError("Unknown movement.");
                    break;
            }

            switch (header.cannonMove)
            {
                case CannonMovementOptions.LEFT:
                    pc.RotateRight(-1.0f);
                    break;
                case CannonMovementOptions.RIGHT:
                    pc.RotateRight(1.0f);
                    break;
                case CannonMovementOptions.HALT:
                    pc.RotateRight(0.0f);
                    break;
                default:
                    Debug.LogError("Unknown cannon movement.");
                    break;
            }

            // process tank actions
            if (header.fireWish == 1)
            {
                pc.Fire();
            }
        }
        catch (NullReferenceException ex)
        {
            Debug.LogError(ex.Message);
        }
    }
    private void OnNetPlayerDisconnected(NetworkPlayer netPlayer)
    {
        RemovePlayer(netPlayer);
    }

    void Start()
    {
        Debug.Log("Initializing NetGameMode...");

        // listen for network players
        connectionSocket = new SocketListener();
        connectionSocket.StartListening(gamePort);
    }
    void FixedUpdate()
    {
        CheckNetworkEvents();
        UpdateClients();
    }
    void OnDestroy()
    {
        CleanUpConnections();
    }
}
