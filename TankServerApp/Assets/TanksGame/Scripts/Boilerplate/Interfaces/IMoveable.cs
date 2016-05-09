using UnityEngine;
using System.Collections;

public interface IMoveable
{
    void MoveForward(float value);
    void TurnRight(float value);

    Vector3 position { get; }
    Vector3 forward { get; }

    bool isMoving { get; }
}
