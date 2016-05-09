using System.Collections.Generic;
using UnityEngine;

namespace UnityGame.Tanks
{
    public class TankMovement : MonoBehaviour, IMoveable
    {
        public int m_PlayerNumber = 1;              // Used to identify which tank belongs to which player.  This is set by this tank's manager.
        public float m_Speed = 12f;                 // How fast the tank moves forward and back.
        public float m_TurnSpeed = 180f;            // How fast the tank turns in degrees per second.
        public AudioSource m_MovementAudio;         // Reference to the audio source used to play engine sounds. NB: different to the shooting audio source.
        public AudioClip m_EngineIdling;            // Audio to play when the tank isn't moving.
        public AudioClip m_EngineDriving;           // Audio to play when the tank is moving.
		public float m_PitchRange = 0.2f;           // The amount by which the pitch of the engine noises can vary.

        public enum TerrainType
        {
            NONE,
            SAND,
            GOO
        }

        private TerrainType currentTerrain;

        private Rigidbody m_Rigidbody;              // Reference used to move the tank.
        private float m_MovementInputValue;         // The current value of the movement input.
        private float m_TurnInputValue;             // The current value of the turn input.
        private float m_OriginalPitch;              // The pitch of the audio source at the start of the scene.

        public float m_NoiseRadius = 25f;          // Radius for broadcasting movement events

        private struct UserInput
        {
            public float verticalInput;
            public float horizontalInput;
        }
        private UserInput controllerInput;

        public Vector3 position
        {
            get
            {
                return transform.position;
            }
        }
        public Vector3 forward
        {
            get
            {
                return transform.forward;
            }
        }
        public bool isMoving
        {
            get
            {
                return (velocity.magnitude > 0.1f);
            }
        }

        private Vector3 prevPosition;
        private Vector3 _velocity;
        public Vector3 velocity
        {
            get
            {
                return _velocity;
            }
        }

        private void Awake ()
        {
            m_Rigidbody = GetComponent<Rigidbody> ();
        }
        private void OnEnable ()
        {
            // When the tank is turned on, make sure it's not kinematic.
            m_Rigidbody.isKinematic = false;

            // Also reset the input values.
            m_MovementInputValue = 0f;
            m_TurnInputValue = 0f;
        }
        private void Start ()
        {
            // Store the original pitch of the audio source.
            m_OriginalPitch = m_MovementAudio.pitch;
        }
        private void Update ()
        {
            // Store the value of both input axes.
            m_MovementInputValue = controllerInput.verticalInput;
            m_TurnInputValue = controllerInput.horizontalInput;

            EngineAudio ();
        }
        private void FixedUpdate()
        {
            // Adjust the rigidbodies position and orientation in FixedUpdate.
            Move();
            Turn();

            BroadcastEvents();

            _velocity = (transform.position - prevPosition) / Time.deltaTime;
            prevPosition = transform.position;
            currentTerrain = GetTerrain();

            Debug.Log(currentTerrain);
        }
        private void OnDisable()
        {
            // When the tank is turned off, set it to kinematic so it stops moving.
            m_Rigidbody.isKinematic = true;
        }

        private void EngineAudio ()
        {
            // If there is no input (the tank is stationary)...
            if (Mathf.Abs (m_MovementInputValue) < 0.1f && Mathf.Abs (m_TurnInputValue) < 0.1f)
            {
                // ... and if the audio source is currently playing the driving clip...
                if (m_MovementAudio.clip == m_EngineDriving)
                {
                    // ... change the clip to idling and play it.
                    m_MovementAudio.clip = m_EngineIdling;
                    m_MovementAudio.pitch = UnityEngine.Random.Range (m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play ();
                }
            }
            else
            {
                // Otherwise if the tank is moving and if the idling clip is currently playing...
                if (m_MovementAudio.clip == m_EngineIdling)
                {
                    // ... change the clip to driving and play.
                    m_MovementAudio.clip = m_EngineDriving;
                    m_MovementAudio.pitch = UnityEngine.Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play();
                }
            }
        }
        private TerrainType GetTerrain()
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position + (Vector3.up), -transform.up, out hit, 5f,
                ~(LayerMask.NameToLayer("Ground")), QueryTriggerInteraction.Collide))
            {
                return hit.collider.CompareTag("Goo") ? TerrainType.GOO : TerrainType.SAND;
            }
            else
            {
                return TerrainType.NONE;
            }
        }
        private float GetTerrainMoveModifier(TerrainType terrainType)
        {
            switch(terrainType)
            {
                case TerrainType.GOO:
                    return 0.3f;
                case TerrainType.SAND:
                    return 1.0f;
                default:
                    return 0.0f;
            }
        }
        private float GetTerrainTurnModifier(TerrainType terrainType)
        {
            switch (terrainType)
            {
                case TerrainType.GOO:
                    return 0.2f;
                case TerrainType.SAND:
                    return 1.0f;
                default:
                    return 0.0f;
            }
        }

        private void BroadcastEvents()
        {
            var objectsInRange = Physics.OverlapSphere(transform.position, m_NoiseRadius, ~(LayerMask.NameToLayer("Players")));

            for(int i = 0; i < objectsInRange.Length; ++i)
            {
                var receptor = objectsInRange[i].GetComponent<TankPercepts>();
                if(receptor != null && receptor.gameObject != gameObject)
                {
                    TankTacticalInfo targetRecord = receptor.GetRecord(m_PlayerNumber);

                    // write updated information
                    targetRecord.lastKnownDirection = (transform.position - receptor.transform.position).normalized;

                    receptor.WriteRecord(targetRecord);
                }
            }
        }

        private void Move ()
        {
            // Create a vector in the direction the tank is facing with a magnitude based on the input, speed and the time between frames.
            Vector3 movement = transform.forward * m_MovementInputValue * (m_Speed * GetTerrainMoveModifier(currentTerrain)) * Time.deltaTime;

            // Apply this movement to the rigidbody's position.
            m_Rigidbody.MovePosition(m_Rigidbody.position + movement);
        }
        private void Turn ()
        {
            // Determine the number of degrees to be turned based on the input, speed and time between frames.
            float turn = m_TurnInputValue * (m_TurnSpeed * GetTerrainTurnModifier(currentTerrain)) * Time.deltaTime;

            // Make this into a rotation in the y axis.
            Quaternion turnRotation = Quaternion.Euler (0f, turn, 0f);

            // Apply this rotation to the rigidbody's rotation.
            m_Rigidbody.MoveRotation (m_Rigidbody.rotation * turnRotation);
        }

        public void MoveForward(float value)
        {
            controllerInput.verticalInput = Mathf.Clamp(value, -1, 1);
        }
        public void TurnRight(float value)
        {
            controllerInput.horizontalInput = Mathf.Clamp(value, -1, 1);
        }

        private void OnDrawGizmos()
        {
            // draw movement-sound radius
            Gizmos.color = (velocity.magnitude / m_Speed) * Color.red;
            Gizmos.DrawWireSphere(transform.position, m_NoiseRadius);
        }
    }
}