using UnityEngine;
using System.Collections;

public enum MatchState
{
    WAITING_FOR_PLAYERS,
    ROUND_IS_BEGINNING,
    ROUND_IN_PROGRESS,
    ROUND_IS_ENDING,
    LEVEL_IS_CHANGING
}