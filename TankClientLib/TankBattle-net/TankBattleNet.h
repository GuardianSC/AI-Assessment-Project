#pragma once

#include <vector>

#include "TankBattleHeaders.h"

namespace tankNet
{
    typedef std::vector<std::vector<int>> adjacencyMatrix;

    // Attempts to locate and form a TCP connection with the server
    // - serverAddress :: IP address of server, defaults to 127.0.0.1 (AKA localhost)
    // - serverPort :: listening port on server
    // Returns true if the host server could be resolved, otherwise returns false
    bool init(char * serverAddress="127.0.0.1", int serverPort = 11000);

    // Sends data upstream and checks the TCP queue for any information
    // - timeToBlock :: Time in seconds to block execution while waiting for information.
    // Returns false if an error has occurred, otherwise returns true.
    bool update(double timeToBlock=0.0);

    // Enqueues a tank command for transmission to the server on the next update.
    void send(TankBattleCommand command);

    // Pulls the latest state information recieved from the server.
    // Returns a pointer to the last state data recieved. This does NOT force a state update.
    TankBattleStateData * recieve();

    // Returns an adjacency matrix, representing obstacles on the server.
    adjacencyMatrix getAdjacencyMatrix();

    // Forcibly closes the socket to the server and executes tankNet cleanup routines.
    void term();

    // Returns true if tankNet is currently connected to a server.
    bool isConnected();

    // Returns true if tankNet has recieved at last one valid state update from the server.
    bool isProvisioned();

	std::ostream& operator<<(std::ostream &os, TankBattleStateData const &state);
}