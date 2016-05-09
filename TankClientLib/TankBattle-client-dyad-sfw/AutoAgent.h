#pragma once

#include "Vector2.h"
#include <iostream>
#include "Graph.h"
#include "Solver.h"
#include "Grid.h"

#include "IAgent.h"
#include "sfwdraw.h"

/*
    Example dumb agent.

    State Description:
        

        Cannon:
            SCAN:
                Turns Right
                enemy in sight? -> Set Target, FIRE
            FIRE:
                Fire weapon
                -> SCAN

        Tank:
            Explore:
                If touching Target, set random Target
                Path to Target                
*/

inline int behaviorSwitch(int min, int max)
{
	return rand() % (max - min) + min;
}

class AutoAgent : public IAgent 
{
    // Cache current battle data on each update
    tankNet::TankBattleStateData current;

    // output data that we will return at end of update
    tankNet::TankBattleCommand tbc;

    // Could use this to keep track of the environment, check out the header.
    Grid map;

	//USEFUL FUNCTIONS TO IMPLEMENT: lookToward(target), moveToward(target), turnToward(target), checkForObstacles()

    //////////////////////
    // States for turret and tank
    enum TURRET { SCAN, AIM,    FIRE,	PANICFIRE, LOOKTOWARD			} turretState = LOOKTOWARD;
    enum TANK   { SEEK, PATROL, FOLLOW, INVESTIGATE, MOVETOWARD, HALT         } tankState   = HALT;

    float randTimer = 0;
	//float behaviorSwitch = 0;

    // Active target location to pursue
    Vector2 target;

#pragma region Turret
    //////////////////////////////////////////
    //	      T   U   R   R   E   T	        //
	//////////////////////////////////////////

    void scan()
    {
        Vector2 tf = Vector2::fromXZ(current.cannonForward);  // current forward
        Vector2 cp = Vector2::fromXZ(current.position); // current position

        tbc.fireWish = false;
        // Arbitrarily look around for an enemy.
        tbc.cannonMove = tankNet::CannonMovementOptions::RIGHT;

        // Search through the possible enemy targets
        for (int aimTarget = 0; aimTarget < current.playerCount - 1; ++aimTarget)
            if (current.tacticoolData[aimTarget].inSight && current.canFire) // if in Sight and we can fire
            {
                target = Vector2::fromXZ(current.tacticoolData[aimTarget].lastKnownPosition);
             
                if(dot(tf, normal(target-cp)) > .87f)
                    turretState = FIRE;
                break;
            }
    }

	void panic() // Fires a few shots in tank forward direction
	{
		//randTimer = rand() % 4 + 2; // 2-6 second fire cooldown
		randTimer -= sfw::getDeltaTime();

		if (randTimer <= 0)
		{
			tbc.fireWish = current.canFire;
			//randTimer = rand() % 4 + 2;
		}
	 }

    void fire()
    {
        // No special logic for now, just shoot.
        tbc.fireWish = current.canFire;
        turretState = SCAN;
    }

	void lookToward(Vector2 target)
	{
		Vector2 cp = Vector2::fromXZ(current.position); // current position
		Vector2 cf = Vector2::fromXZ(current.forward);  // current forward
		target = Vector2::random() * Vector2 { 50, 50 };

		Vector2 tf = normal(target - cp);

		if (dot(cf, tf) > .87f)
		// turn right until we line up!
			tbc.cannonMove = tankNet::CannonMovementOptions::RIGHT;
	}
#pragma endregion

#pragma region Tank
	//////////////////////////////////////////
	//	         T   A   N   K     	        //
	//////////////////////////////////////////

	std::list<Vector2> path;

	// for cannon testing purposes
	void halt()
	{
		tbc.tankMove = tankNet::TankMovementOptions::HALT;
	}

	void moveToward(Vector2 target)
	{
		Vector2 cp = Vector2::fromXZ(current.position); // current position
		Vector2 cf = Vector2::fromXZ(current.forward);  // current forward

		target = Vector2::random() * Vector2 { 50, 50 };

		if (distance(target, cp) < 20)
		{
			tbc.tankMove = tankNet::TankMovementOptions::HALT;
		}

		// determine the forward to the target (which is the next waypoint in the path)
		Vector2 tf = normal(target - cp);

		if (dot(cf, tf) > .87f) // if the dot product is close to lining up, move forward
			tbc.tankMove = tankNet::TankMovementOptions::FWRD;
		else // otherwise turn right until we line up!
			tbc.tankMove = tankNet::TankMovementOptions::RIGHT;
	}

	void seek()
	{
		Vector2 cp = Vector2::fromXZ(current.position); // current position
		Vector2 cf = Vector2::fromXZ(current.forward);  // current forward

		randTimer -= sfw::getDeltaTime();

		// If we're pretty close to the target, get a new target           
		if (distance(target, cp) < 20 || randTimer < 0)
		{
			target = Vector2::random() * Vector2 { 50, 50 };
			randTimer = rand() % 4 + 2; // every 2-6 seconds randomly pick a new target
		}

		// determine the forward to the target (which is the next waypoint in the path)
		Vector2 tf = normal(target - cp);

		if (dot(cf, tf) > .87f) // if the dot product is close to lining up, move forward
			tbc.tankMove = tankNet::TankMovementOptions::FWRD;
		else // otherwise turn right until we line up!
			tbc.tankMove = tankNet::TankMovementOptions::RIGHT;

		for (int aimTarget = 0; aimTarget < current.playerCount - 1; ++aimTarget)
			if (!current.tacticoolData[aimTarget].inSight) // if out of Sight and 
				investigate();
	}

	void patrol() // move between two points 
	{
		Vector2 cp = Vector2::fromXZ(current.position); // current position
		Vector2 cf = Vector2::fromXZ(current.forward);  // current forward
		target = Vector2::random() * Vector2 { 50, 50 };
		Vector2 target2 = Vector2::random() * Vector2 { 50, 50 };

		/// MOVE TO FIRST POINT
		// If we're pretty close to the target, get a new target           
		if (distance(target, cp) < 20)
		{
			target = target2;
		}

		// determine the forward to the target (which is the next waypoint in the path)
		Vector2 tf = normal(target - cp); // (where you want to go - where you currently are)

		// if the dot product is close to lining up, move forward
		if (dot(cf, tf) > .87f) // (cannon forward, target forward)
			tbc.tankMove = tankNet::TankMovementOptions::FWRD;
		// otherwise turn right until we line up
		else
			tbc.tankMove = tankNet::TankMovementOptions::RIGHT;

		/// MOVE TO SECOND POINT
		if (distance(target2, cp) < 20)
		{
			target2 = target;
		}

		// determine the forward to the target (which is the next waypoint in the path)
		Vector2 tf2 = normal(target2 - cp); // (where you want to go - where you currently are)

		// if the dot product is close to lining up, move forward
		if (dot(cf, tf2) > .87f) // (cannon forward, target2 forward)
			tbc.tankMove = tankNet::TankMovementOptions::FWRD;
		// otherwise turn right until we line up
		else
			tbc.tankMove = tankNet::TankMovementOptions::RIGHT;
	}

	void follow() // follow an enemy tank
	{
		Vector2 tf = Vector2::fromXZ(current.cannonForward);  // current forward
		Vector2 cp = Vector2::fromXZ(current.position); // current position

														// Search through the possible enemy targets
		for (int moveTarget = 0; moveTarget < current.playerCount - 1; ++moveTarget)
			if (current.tacticoolData[moveTarget].inSight && current.canFire) // if in Sight and we can fire
			{
				target = Vector2::fromXZ(current.tacticoolData[moveTarget].lastKnownPosition);

				if (dot(tf, normal(target - cp)) > .87f)
					// if the dot product is close to lining up, move forward
					tbc.tankMove = tankNet::TankMovementOptions::FWRD;
				// otherwise turn right until we line up!
				else
					tbc.tankMove = tankNet::TankMovementOptions::RIGHT;
				break;
			}
	}

	void investigate() // if enemy is seen then leaves sight, identify direction and move towards it
	{
		Vector2 cp = Vector2::fromXZ(current.position); // current position
		Vector2 cf = Vector2::fromXZ(current.forward);  // current forward
		
		// determine the forward to the target (which is the next waypoint in the path)
		Vector2 tf = normal(target - cp);

		for (int aimTarget = 0; aimTarget < current.playerCount - 1; ++aimTarget)
			if (current.tacticoolData[aimTarget].inSight && current.canFire)
				target = Vector2::fromXZ(current.tacticoolData[aimTarget].lastKnownPosition);
	}
#pragma endregion

public:
    tankNet::TankBattleCommand update(tankNet::TankBattleStateData *state)
    {
        current = *state;
        tbc.msg = tankNet::TankBattleMessage::GAME;

        switch (turretState)
        {
        case SCAN: scan(); break;
        case FIRE: fire(); break;
		case PANICFIRE: panic(); break;
		case LOOKTOWARD: lookToward(target); break;
        }

        switch (tankState)
        {
		case HALT: halt(); break;
        case SEEK: seek(); break;
		case PATROL: patrol(); break;
		case FOLLOW: follow(); break;
		case INVESTIGATE: investigate(); break; 
		case MOVETOWARD: moveToward(target); break;
        }

        return tbc;
    }
};