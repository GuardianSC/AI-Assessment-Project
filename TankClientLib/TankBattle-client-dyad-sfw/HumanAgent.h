#pragma once
#include "sfwdraw.h"
#include "IAgent.h"
#include "TankBattleHeaders.h"


const char TANK_FWRD = 'W';
const char TANK_BACK = 'S';
const char TANK_LEFT = 'A';
const char TANK_RIGT = 'D';

const char TANK_FIRE = 'F';

const char CANN_LEFT = 'Q';
const char CANN_RIGT = 'E';


// Polls all printable characters for a key press
// Returns true if a key press was detected, otherwise returns false
inline bool inputPressed()
{
    // 32 is the ASCII code for the first printable character, while 255 is the last
    for (unsigned int i = 32; i < 255; ++i)
    {
        if (sfw::getKey(i))
            return true;
    }

    return false;
}


/*
    Simple wrapper for the update function to allow human controller
*/
class HumanAgent : public IAgent
{
public:
    virtual tankNet::TankBattleCommand update(tankNet::TankBattleStateData *state)
    {
        tankNet::TankBattleCommand ex;
        ex.msg          = tankNet::TankBattleMessage::GAME;
        ex.tankMove     = tankNet::TankMovementOptions::HALT;
        ex.cannonMove   = tankNet::CannonMovementOptions::HALT;


        if (inputPressed())
        {
            ex.tankMove = sfw::getKey(TANK_FWRD) ? tankNet::TankMovementOptions::FWRD :
                sfw::getKey(TANK_BACK) ? tankNet::TankMovementOptions::BACK :
                sfw::getKey(TANK_LEFT) ? tankNet::TankMovementOptions::LEFT :
                sfw::getKey(TANK_RIGT) ? tankNet::TankMovementOptions::RIGHT :
                tankNet::TankMovementOptions::HALT;

            ex.cannonMove = sfw::getKey(CANN_LEFT) ? tankNet::CannonMovementOptions::LEFT :
                sfw::getKey(CANN_RIGT) ? tankNet::CannonMovementOptions::RIGHT :
                tankNet::CannonMovementOptions::HALT;

            ex.fireWish = sfw::getKey(TANK_FIRE);
        }

        return ex;
    }
};