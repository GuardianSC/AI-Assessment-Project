using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace UnityGame.Tanks
{
    public class TankShooting : MonoBehaviour, IFireable
    {
        public int m_PlayerNumber = 1;              // Used to identify the different players.
        public Rigidbody m_Shell;                   // Prefab of the shell.
        public Transform m_FireTransform;           // A child of the tank where the shells are spawned.
        public Slider m_AimSlider;                  // A child of the tank that displays the current launch force.
        public AudioSource m_ShootingAudio;         // Reference to the audio source used to play the shooting audio. NB: different to the movement audio source.
        public AudioClip m_ChargingClip;            // Audio that plays when each shot is charging up.
        public AudioClip m_FireClip;                // Audio that plays when each shot is fired.
        public float m_MinLaunchForce = 15f;        // The force given to the shell if the fire button is not held.
        public float m_MaxLaunchForce = 30f;        // The force given to the shell if the fire button is held for the max charge time.
        public float m_MaxChargeTime = 0.75f;       // How long the shell can charge for before it is fired at max force.

        private float m_CurrentLaunchForce;         // The force that will be given to the shell when the fire button is released.
        private float m_ChargeSpeed;                // How fast the launch force increases, based on the max charge time.
        private bool m_Fired;                       // Whether or not the shell has been launched with this button press.
        private bool m_FireWish;                    // Is the user intending to fire?
        private bool m_FireEvent;                   // Is there a fire event that needs to be broadcasted?
        [SerializeField]
        private float m_FireCooldown = 5.0f;        // How long is the cooldown between fires?
        private bool m_IsFireOnCooldown;            // Is the tank currently on cooldown for firing?

        private float m_NoiseRadius = 1500f;        // Radius in which to broadcast firing events.

        IEnumerator StartGunCooldown(float cooldownTimer)
        {
            yield return new WaitForSeconds(cooldownTimer);

            m_IsFireOnCooldown = false;
        }

        public bool CanFire()
        {
            return !m_IsFireOnCooldown;
        }
        public void Fire ()
        {
            m_FireWish = true;
        }

        public void Launch(bool maximumForce = false)
        {
            // Set the fired flag so only Fire is only called once.
            m_Fired = true;

            // Create an instance of the shell and store a reference to it's rigidbody.
            Rigidbody shellInstance =
                Instantiate(m_Shell, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;

            // Instantly up to max?
            if(maximumForce)
            {
                m_CurrentLaunchForce = m_MaxLaunchForce;
            }

            // Set the shell's velocity to the launch force in the fire position's forward direction.
            shellInstance.velocity = m_CurrentLaunchForce * m_FireTransform.forward;

            // Change the clip to the firing clip and play it.
            m_ShootingAudio.clip = m_FireClip;
            m_ShootingAudio.Play();

            // Reset the launch force.  This is a precaution in case of missing button events.
            m_CurrentLaunchForce = m_MinLaunchForce;

            m_FireEvent = true;
        }

        public void BroadcastEvents()
        {
            if(m_FireEvent)
            {
                m_FireEvent = false;

                var objectsInRadius = Physics.OverlapSphere(transform.position, m_NoiseRadius, ~(LayerMask.NameToLayer("Players")));

                for (int i = 0; i < objectsInRadius.Length; ++i)
                {
                    var percepts = objectsInRadius[i].GetComponent<TankPercepts>();
                    if (objectsInRadius[i].gameObject != gameObject &&
                        percepts != null)
                    {
                        TankTacticalInfo targetRecord = percepts.GetRecord(m_PlayerNumber);

                        // write updated information
                        targetRecord.lastKnownPosition = transform.position;
                        targetRecord.lastKnownDirection = (transform.position - percepts.transform.position).normalized;
                        targetRecord.lastKnownCannonForward = m_FireTransform.parent.transform.forward; // HACK: find a better way to expose the cannon

                        percepts.WriteRecord(targetRecord);
                    }
                }
            }
        }

        private void OnEnable()
        {
            // When the tank is turned on, reset the launch force and the UI
            m_CurrentLaunchForce = m_MinLaunchForce;
            m_AimSlider.value = m_MinLaunchForce;
            m_IsFireOnCooldown = false;
            m_FireWish = false;
        }
        private void Start()
        {
            // The rate that the launch force charges up is the range of possible forces by the max charge time.
            m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;
        }
        private void Update()
        {
            // The slider should have a default value of the minimum launch force.
            m_AimSlider.value = m_MinLaunchForce;

            // If a fire has been requested and we can fire...
            if (m_FireWish)
            {
                m_FireWish = false;

                if (CanFire())
                {
                    m_IsFireOnCooldown = true;

                    Launch(true);
                    StartCoroutine(StartGunCooldown(m_FireCooldown));
                }
            }
        }
        private void FixedUpdate()
        {
            BroadcastEvents();
            //Debug.Log(m_PlayerNumber + "::" + m_FireTransform.parent.forward);
        }
    }
}