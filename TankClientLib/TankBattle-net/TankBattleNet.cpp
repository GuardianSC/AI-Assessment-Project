#include "TankBattleNet.h"

#include <cassert>
#include <ostream>
#include <iostream>

#include <vector>

#include "dyad.h"

namespace tankNet
{
    static bool _isConnected;
    static bool _isProvisioned;
    static bool _isErrored;

    static dyad_Stream * stream;

    static char * lastMessage;
    static int lastMessageLength;

    void onConnect(dyad_Event *e)
    {
        _isConnected = true;
        std::cout << "Connected.\n";
    }
    void onData(dyad_Event *e)
    {
        auto msg = (TankBattleStateData*)e->data;

		// HACK: discard this transmission
		// develop a better messaging protocol
		if (msg->messageLength <= 0 ||
            msg->messageLength != sizeof(TankBattleStateData))
			return;

        // TODO: RTCs
        if (lastMessageLength < e->size)
        {
            delete[] lastMessage;
            lastMessageLength = e->size;
            lastMessage = new char[lastMessageLength];
        }

        memcpy_s(lastMessage, lastMessageLength, e->data, e->size);

        if (!isProvisioned())
        {
            _isProvisioned = true;
            std::cout << "Provisioned.\n";
        }
    }
    void onError(dyad_Event *e)
    {
        std::cerr << "Error:" << e->msg << "\n";

        _isErrored = true;
    }
    void onClose(dyad_Event *e)
    {
        printf("Stream Close: ");
        printf("%s", e->msg);

        _isConnected = false;
        _isProvisioned = false;
    }
    
    bool init(char * serverAddress, int serverPort)
    {
        dyad_init();

        assert(stream == nullptr);

        lastMessageLength = sizeof(TankBattleStateData);
        lastMessage = new char[lastMessageLength];

        stream = dyad_newStream();

        dyad_addListener(stream, DYAD_EVENT_DATA, onData, 0);
        dyad_addListener(stream, DYAD_EVENT_ERROR, onError, 0);
        dyad_addListener(stream, DYAD_EVENT_CONNECT, onConnect, 0);
        dyad_addListener(stream, DYAD_EVENT_CLOSE, onClose, 0);

        if (dyad_connect(stream, serverAddress, serverPort) == -1)
        {
            std::cerr << "Failed to resolve server host.\n";
            return false;
        }
    }

    bool update(double timeToBlock)
    {
        if (_isErrored)
            return false;
        
        if (dyad_getStreamCount() < 1)
            _isConnected = false;

        dyad_setUpdateTimeout(timeToBlock);
        dyad_update();

        return true;
    }

    void term()
    {
        _isConnected = false;
        _isProvisioned = false;

        if (stream != nullptr)
        {
            dyad_close(stream);
        }

        delete[] lastMessage;

        dyad_shutdown();
    }

    void send(TankBattleCommand command)
    {
        if (!isConnected)
            return;

        const int msgSize = sizeof(TankBattleCommand);
        unsigned char msg[msgSize];

        memcpy_s(&msg, msgSize, &command, sizeof(TankBattleCommand));
        dyad_write(stream, &msg, msgSize);
    }

    TankBattleStateData * recieve()
    {
        TankBattleStateData * lastState = ((TankBattleStateData*)lastMessage);

        return lastState;
    }

    bool isConnected()
    {
        return _isConnected;
    }

    bool isProvisioned()
    {
        return _isProvisioned;
    }

    adjacencyMatrix getAdjacencyMatrix()
    {
        auto adjMat = adjacencyMatrix();
        adjMat.reserve(10);

        for (int i = 0; i < adjMat.size(); ++i)
        {
            adjMat[i].reserve(10);
        }

        for (int i = 0; i < adjMat.size(); ++i)
        {
            for (int j = 0; i < adjMat[i].size(); ++j)
            {
                adjMat[i][j] = 1;
            }
        }

        return adjMat;
    }

	std::ostream& operator<<(std::ostream &os, TankBattleStateData const &state)
	{
		os << "Player ID: " << state.playerID << "\n";
		os << "Health: " << state.currentHealth << "\n";
		for (int i = 0; i < 3; ++i)
		{
			os << "Position " << i << " = " << state.position[i] << "\n";
		}
		for (int i = 0; i < 3; ++i)
		{
			os << "Forward " << i << " = " << state.forward[i] << "\n";
		}
		for (int i = 0; i < 3; ++i)
		{
			os << "Cannon Forward " << i << " = " << state.cannonForward[i] << "\n";
		}
		os << "Can Fire: " << state.canFire << "\n";
		os << "Tacticool Count: " << state.tacticoolCount << "\n";

		return os;
	}
}