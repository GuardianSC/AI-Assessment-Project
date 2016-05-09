using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using System.Collections.Generic;

using UnityEngine;

public class NetworkPlayer
{
    public Socket remoteSocket;
    public PlayerController playerController;
    public bool isActive;

    public static implicit operator Socket(NetworkPlayer p)
    {
        return p.remoteSocket;
    }
}

public class StateObject
{
    public Socket remoteSocket = null;                // socket to player

    public const int BufferSize = 4096;             // maximum amount of data buffered for a user
    public int bytesRead = 0;                       // bytes currently in the buffer

    public byte[] buffer = new byte[BufferSize];    // access to buffer
}
public class SocketEventArgs : EventArgs
{
    public enum SocketEventType
    {
        ACCEPT,
        READ,
        SEND,
        DROP
    }
    public readonly SocketEventType socketEvent;
    public readonly NetworkPlayer endpoint;

    public SocketEventArgs(SocketEventType e, NetworkPlayer remote)
    {
        socketEvent = e;
        endpoint = remote;
    }
}
public class SocketEvent
{
    public readonly SocketEventArgs eventArgs;
    public readonly byte[] data;

    public SocketEvent(SocketEventArgs e, byte[] d)
    {
        eventArgs = e;
        data = d;
    }
}
public delegate void SocketEventHandler(byte[] data, SocketEventArgs e);

// Setup a listener on a given port
public class SocketListener
{
    private Socket localListener;
    private Dictionary<Socket, NetworkPlayer> players = new Dictionary<Socket, NetworkPlayer>();

    public int port { get; private set; }
    private ManualResetEvent allDone = new ManualResetEvent(false);
    public bool paused
    {
        get
        {
            return _paused;
        }
        set
        {
            if(value)
            {
                allDone.Reset(); // block
            }
            else
            {
                allDone.Set();  // allow
            }

            _paused = value;
        }
    }
    private bool _paused;

    public List<SocketEvent> events { get; private set; }
    public void AddNetworkEvent(SocketEvent newEvent)
    {
        try
        {
            events.Add(newEvent);
        }
        catch (Exception ex)
        {
            if (ex is IndexOutOfRangeException)
            {
                Debug.LogWarning("Index was out of range while adding an event to the queue.");
            }
            else
            {
                Debug.LogWarning("UNKNOWN WARNING:" + ex.InnerException + "\n" + ex.StackTrace);
            }
        }
    }

    public SocketListener()
    {
        events = new List<SocketEvent>();
    }

    // TODO: StateObject is hard-coded to max out at 1024 bytes. Please define somewhere consistent.
    // TODO: Add RTCs for data size. Should not exceed max or be less than zero.
    // TODO: Genericize this to work with other types of data

    public void Send(Socket handler, byte[] data)
    {
        try
        {
            handler.BeginSend(data, 0, data.Length, 0,
                new AsyncCallback(SendCallback), handler);
        }
        catch (SocketException ex)
        {
            Debug.LogWarning(ex.Message);
            players[handler].isActive = false;
            DropConnection(handler);
            events.Add(new SocketEvent(new SocketEventArgs(SocketEventArgs.SocketEventType.DROP, players[handler]), null));
        }
    }
    public void Send(Socket handler, object data)
    {
        Send(handler, DataUtils.GetBytes(data));
    }

    private void AcceptCallback(IAsyncResult ar)
    {
        allDone.Set();

        Socket listener = (Socket)ar.AsyncState;
        Socket handler = listener.EndAccept(ar);

        var endpoint = (IPEndPoint)handler.RemoteEndPoint;

        Debug.Log("Connection from " + endpoint.Address.ToString());

        StateObject state = new StateObject();
        state.remoteSocket = handler;

        NetworkPlayer netPlayer = new NetworkPlayer();
        netPlayer.isActive = true;
        netPlayer.remoteSocket = handler;

        players[handler] = netPlayer;

        try
        {
            events.Add(new SocketEvent(new SocketEventArgs(SocketEventArgs.SocketEventType.ACCEPT, netPlayer), null));
        }
        catch (Exception ex)
        {
            if (ex is IndexOutOfRangeException)
            {
                Debug.LogWarning("Index was out of range while adding an event to the queue.");
            }
            else
            {
                Debug.LogWarning("UNKNOWN WARNING:" + ex.InnerException + "\n" + ex.StackTrace);
            }
        }
        Debug.Log("Setting up to recieve from " + endpoint.Address.ToString());

        listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
            new AsyncCallback(ReadCallback), state);
    }
    private void ReadCallback(IAsyncResult ar)
    {
        StateObject state = (StateObject)ar.AsyncState;
        Socket handler = state.remoteSocket;

        int bytesRead = handler.EndReceive(ar);
        state.bytesRead += bytesRead;   // add to running total of bytesRead for this message

        try {

            if (bytesRead > 0)
            {
                // TODO: Can we add generics to callbacks?
                // How can we simplify this to be reusable?

                // do we have enough data to work from?
                while (state.bytesRead >= DataUtils.SizeOf<TankBattleCommand>())
                {
                    var header = DataUtils.FromBytes<TankBattleCommand>(state.buffer);

                    // RTC: Verify that the header has a valid message length
                    if (header.messageLength <= 0)
                    {
                        Debug.LogError("Invalid message length provided. Dropping client.");
                        DropConnection(handler);
                        break;
                    }
                    // RTC: Validate message length
                    else if (header.messageLength >= StateObject.BufferSize)
                    {
                        Debug.LogErrorFormat("Message size is slated to be larger than buffer capacity. ({0} bytes)",
                            header.messageLength);
                        DropConnection(handler);
                        break;
                    }

                    // have we recieved the full message?
                    if (state.bytesRead >= header.messageLength)
                    {
                        try
                        {
                            events.Add(new SocketEvent(new SocketEventArgs(SocketEventArgs.SocketEventType.READ, players[handler]), (byte[])state.buffer.Clone()));
                        }
                        catch (Exception ex)
                        {
                            if (ex is IndexOutOfRangeException)
                            {
                                Debug.LogWarning("Index was out of range while adding an event to the queue.");
                            }
                            else
                            {
                                Debug.LogWarning("UNKNOWN WARNING:" + ex.InnerException + "\n" + ex.StackTrace);
                            }
                        }
                        // subtract the number of bytes needed to be processed
                        state.bytesRead -= header.messageLength;

                        // remove this record from the buffer
                        state.buffer.ShiftLeft(header.messageLength);
                    }
                    else
                    {
                        Debug.Log("Need more information; expecting " + header.messageLength + " bytes of data.");
                        break;
                    }
                }
            }
            else 
            {
                // client has probably disconnected, so drop them
                handler.Close(1);
                
                events.Add(new SocketEvent(new SocketEventArgs(SocketEventArgs.SocketEventType.DROP, players[handler]), null));
                players.Remove(handler);

                return;
            }

            // set up to recieve again
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message + e.StackTrace);
        }
    }
    private void SendCallback(IAsyncResult ar)
    {
        try
        {
            Socket handler = (Socket)ar.AsyncState;
            handler.EndSend(ar);

            events.Add(new SocketEvent(new SocketEventArgs(SocketEventArgs.SocketEventType.SEND, players[handler]), null));
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message + e.StackTrace);
        }
    }

    private void DropConnection(Socket remote)
    {
        try
        {
            if (remote.Connected)
            {
                remote.Disconnect(true);
            }
        }
        catch (Exception e)
        {
            if (e is SocketException)
            {
                Debug.LogError("Socket ErrorCode:" + ((SocketException)e).SocketErrorCode);
            }
        }
    }

    public void StartListening(int port)
    {
        // bind to all local IPv4 interfaces
        IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);

        Socket listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

        listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
        listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

        localListener = listener;
        try
        {
            Debug.LogFormat("Binding to all local IPv4 interfaces.");
            listener.Bind(localEndPoint);
            listener.Listen(100);

            allDone.Reset();

            listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
            Debug.Log("Ready to accept connections.");

            Debug.Log("Socket Initialized.");
            Debug.LogFormat("Listening on port <{0}>.", port);
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
            Debug.LogError("Failed to initialize socket.");
        }
    }
    public void StopListening()
    {
        // close local socket
        localListener.Close(0);

        // close sockets to other machines
        Debug.LogFormat("Closing {0} sockets to remote machines.", players.Count);
        foreach (var connection in players)
        {
            DropConnection(connection.Key);
        }

        players.Clear();
    }
}