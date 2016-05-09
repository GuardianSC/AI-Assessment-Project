using UnityEngine;

public class PlayerController
{
    public int pid;
    public bool isActive;

    public GameObject Pawn;
    public IMoveable PawnMove;

    public void MoveForward(float value)
    {
        PawnMove.MoveForward(value);
    }
    public void MoveRight(float value)
    {
        PawnMove.TurnRight(value);
    }
}