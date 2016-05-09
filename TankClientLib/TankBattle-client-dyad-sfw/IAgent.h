#pragma once
#include "TankBattleHeaders.h"



class IAgent
{
public:
    virtual tankNet::TankBattleCommand update(tankNet::TankBattleStateData *state) = 0;
};