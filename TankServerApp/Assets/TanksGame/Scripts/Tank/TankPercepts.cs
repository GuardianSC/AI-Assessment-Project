using UnityEngine;
using UnityGame.Tanks;
using System.Collections.Generic;

public class TankPercepts : MonoBehaviour
{
    public Dictionary<int, TankTacticalInfo> reconInfo = new Dictionary<int, TankTacticalInfo>();

    public float VisionRadius = 300f;

    [SerializeField]
    private Transform eyePoint;
    [SerializeField]
    private float eyeFOV = 90f;

    public TankTacticalInfo GetRecord(int playerID)
    {
        TankTacticalInfo record;

        // get the existing record, if any, otherwise create a new one
        if (reconInfo.ContainsKey(playerID))
        {
            record = reconInfo[playerID];
        }
        else
        {
            record = new TankTacticalInfo();
            record.playerID = playerID;
            record.isAlive = 1;
            reconInfo[playerID] = record;
        }

        return record;
    }

    public void WriteRecord(TankTacticalInfo tacticalData)
    {
        reconInfo[tacticalData.playerID] = tacticalData;
    }

    void VisionCheck(float radarRadius)
    {
        // reset vision data for each tank
        foreach (var enemyData in new List<TankTacticalInfo> (reconInfo.Values))
        {
            var revisedData = enemyData;
            revisedData.inSight = false;

            reconInfo[revisedData.playerID] = revisedData;
        }

        var radar = Physics.OverlapSphere(transform.position, VisionRadius, ~(LayerMask.NameToLayer("Players")));
        foreach (var ping in radar)
        {
            // make sure we aren't registering ourselves as an enemy
            if (ping.gameObject != gameObject)
            {
                var direction = (ping.transform.position - transform.position).normalized;
                RaycastHit hit;

                // is it within view?
                if (Vector3.Angle(eyePoint.forward, direction) < eyeFOV / 2f &&
                    Physics.Raycast(eyePoint.transform.position, direction, out hit) &&
                    hit.collider == ping)
                {
                    var tankComponent = hit.collider.GetComponent<TankMovement>();
                    var targetRecord = GetRecord(tankComponent.m_PlayerNumber);

                    // update existing information
                    targetRecord.inSight = true;
                    targetRecord.lastKnownPosition = hit.transform.position;
                    targetRecord.lastKnownDirection = (hit.transform.position - transform.position).normalized;
                    targetRecord.lastKnownTankForward = hit.transform.forward;
                    targetRecord.lastKnownCannonForward = hit.transform.GetComponent<CannonMovement>().cannon.transform.forward;    // HACK: gross
                    targetRecord.health = hit.transform.GetComponent<TankHealth>().m_Status;

                    WriteRecord(targetRecord);
                }
            }
        }
    }

    void OnEnable()
    {
        Reset();
    }
    void Reset()
    {
        reconInfo.Clear();
    }
    void FixedUpdate()
    {
        VisionCheck(VisionRadius);
    }
    void OnDrawGizmos()
    {
        // display known enemy information
        foreach (var enemy in reconInfo.Values)
        {
            Debug.DrawLine(transform.position, transform.position + (enemy.lastKnownDirection * 5f), Color.green);

            if(enemy.inSight)
            {
                Debug.DrawLine(transform.position, enemy.lastKnownPosition, Color.red);
            }

            Gizmos.color = Color.red;

            Gizmos.DrawCube(enemy.lastKnownPosition + enemy.lastKnownTankForward, Vector3.one / 2f);
            Gizmos.DrawCube(enemy.lastKnownPosition + enemy.lastKnownCannonForward, Vector3.one / 4f);
        }
    }
}
