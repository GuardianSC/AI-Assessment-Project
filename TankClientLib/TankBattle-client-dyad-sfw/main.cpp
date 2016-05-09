#include <iostream>
#include <sstream>

#include "AutoAgent.h"
#include "HumanAgent.h"

#include "TankBattleNet.h"
#include "sfwdraw.h"
#undef NONE     // sfw defines NONE as one of its colors; we won't be needing that

using std::stringstream;


// use O or P to switch between players
const char GAME_QUIT = 'L';
const char GAME_TOGGLE_AI    = 'O';
const char GAME_TOGGLE_HUMAN = 'P';

const int WINDOW_HEIGHT = 800;
const int WINDOW_WIDTH  = 400;


void printTacticalData(tankNet::TankBattleStateData * state);



unsigned g_FONT = 0;

int main(int argc, char** argv)
{
    char * serverIPAddress = "";

    // handle console arguments
    if (argc > 2)
    {
        std::cout << "Unsupported number of arguments." << std::endl;
        std::cout << "Specify the IP address of the target server. Ex: " << argv[0] << " 127.0.0.1" << std::endl;

        return EXIT_FAILURE;
    }
    else if (argc == 2)
        serverIPAddress = argv[1]; 

    // initialize networking
    if (serverIPAddress[0] == '\0') tankNet::init();
    else                            tankNet::init(serverIPAddress);


    // block further execution until the server responds
    // or until the client gives up on connecting
    while (!tankNet::isProvisioned()) tankNet::update();
    

    // if a connection was successful...
    if (tankNet::isConnected() && tankNet::isProvisioned())
    {
        auto serverData = tankNet::recieve();

        // initialize SFW and assets
        sfw::initContext(WINDOW_WIDTH, WINDOW_HEIGHT, "TankController");
        g_FONT = sfw::loadTextureMap("./res/fontmap.png", 16, 16);

        AutoAgent  aiAgent;
        HumanAgent huAgent;
        bool isHuman = false;

        ////////////////////////////////////////////////////////////
        /////////// Game Loop!
        while (sfw::stepContext() && tankNet::isConnected() && tankNet::isProvisioned() && !sfw::getKey(GAME_QUIT))
        {
            // check TCP streams via dyad
            tankNet::update();
            if (tankNet::isConnected() == false) break;

            tankNet::TankBattleStateData * state = tankNet::recieve();

            printTacticalData(state);
        
            // Toggle between human or computer player, for debug
            if (sfw::getKey(GAME_TOGGLE_AI))    isHuman = false;
            if (sfw::getKey(GAME_TOGGLE_HUMAN)) isHuman = true;

            // use human agent or AI agent to determine the TBC
            tankNet::TankBattleCommand ex = isHuman ? huAgent.update(state)
                                                    : aiAgent.update(state);

            // begin transmission
            tankNet::send(ex);
        }
    }
    else
    {
        std::cout << "\nFailed to connect to server. Is the server online and not full?";
    }

    tankNet::term();
    sfw::termContext();

    return EXIT_SUCCESS;
}







void printTacticalData(tankNet::TankBattleStateData * state)
{
    // diagnostic report of current state
    stringstream debugStrings;
    debugStrings << *state;
    debugStrings << "Tacticool Report:\n";
    for (int i = 0; i < state->tacticoolCount; ++i)
    {
        debugStrings << state->tacticoolData[i].playerID << "\n    ";
        for (int j = 0; j < 3; ++j)
        {
            debugStrings << state->tacticoolData[i].lastKnownPosition[j] << " , ";
        }
        debugStrings << "\n    ";
        for (int j = 0; j < 3; ++j)
        {
            debugStrings << state->tacticoolData[i].lastKnownDirection[j] << " , ";
        }

        debugStrings << "\ninSight: " << (state->tacticoolData[i].inSight ? "true" : "false") << "\n";
    }

    sfw::drawString(g_FONT, debugStrings.str().c_str(), 0, WINDOW_HEIGHT, 15, 15);
}