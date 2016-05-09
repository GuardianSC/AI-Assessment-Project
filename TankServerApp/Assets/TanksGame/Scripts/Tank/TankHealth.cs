using UnityEngine;
using UnityEngine.UI;

namespace UnityGame.Tanks
{
    public class TankHealth : MonoBehaviour, IDamageable
    {
        public int m_PlayerNumber = -1;                      // Used to identify which tank belongs to which player.  This is set by this tank's manager.
        public float m_StartingHealth = 100f;               // The amount of health each tank starts with.
        public Slider m_Slider;                             // The slider to represent how much health the tank currently has.
        public Image m_FillImage;                           // The image component of the slider.
        public Color m_FullHealthColor = Color.green;       // The color the health bar will be when on full health.
        public Color m_ZeroHealthColor = Color.red;         // The color the health bar will be when on no health.
        public GameObject m_ExplosionPrefab;                // A prefab that will be instantiated in Awake, then used whenever the tank dies.
        
        
        private AudioSource m_ExplosionAudio;               // The audio source to play when the tank explodes.
        private ParticleSystem m_ExplosionParticles;        // The particle system the will play when the tank is destroyed.
        public float m_CurrentHealth { private set; get; }  // How much health the tank currently has.
        private bool m_Dead;                                // Has the tank been reduced beyond zero health yet?

        private float m_DeathAlertRadius = 1500f;
        public const float m_HealthyThreshold = 0.7f;
        public const float m_HurtThreshold = 0.3f;

        public HealthStatus m_Status
        {
            get
            {
                float remainingHealthPercentage = m_CurrentHealth / m_StartingHealth;

                return remainingHealthPercentage > m_HealthyThreshold ? HealthStatus.HEALTHY :
                       remainingHealthPercentage > m_HurtThreshold ? HealthStatus.HURT :
                       remainingHealthPercentage > 0f ? HealthStatus.CRITICAL :
                       HealthStatus.DEAD;
                        
            }
        }

        private void Awake ()
        {
            // Instantiate the explosion prefab and get a reference to the particle system on it.
            m_ExplosionParticles = Instantiate (m_ExplosionPrefab).GetComponent<ParticleSystem> ();

            // Get a reference to the audio source on the instantiated prefab.
            m_ExplosionAudio = m_ExplosionParticles.GetComponent<AudioSource> ();

            // Disable the prefab so it can be activated when it's required.
            m_ExplosionParticles.gameObject.SetActive (false);
        }


        private void OnEnable()
        {
            // When the tank is enabled, reset the tank's health and whether or not it's dead.
            m_CurrentHealth = m_StartingHealth;
            m_Dead = false;

            // Update the health slider's value and color.
            SetHealthUI();
        }


        public void TakeDamage (float amount)
        {
            // Reduce current health by the amount of damage done.
            m_CurrentHealth -= amount;

            // Change the UI elements appropriately.
            SetHealthUI ();

            // If the current health is at or below zero and it has not yet been registered, call OnDeath.
            if (m_CurrentHealth <= 0f && !m_Dead)
            {
                OnDeath ();
            }
        }


        private void SetHealthUI ()
        {
            // Set the slider's value appropriately.
            m_Slider.value = m_CurrentHealth;

            // Interpolate the color of the bar between the choosen colours based on the current percentage of the starting health.
            m_FillImage.color = Color.Lerp (m_ZeroHealthColor, m_FullHealthColor, m_CurrentHealth / m_StartingHealth);
        }


        private void OnDeath ()
        {
            // Set the flag so that this function is only called once.
            m_Dead = true;

            var objectsInRange = Physics.OverlapSphere(transform.position, m_DeathAlertRadius, ~(LayerMask.NameToLayer("Players")));

            for (int i = 0; i < objectsInRange.Length; ++i)
            {
                var receptor = objectsInRange[i].GetComponent<TankPercepts>();
                if (receptor != null && receptor.gameObject != gameObject)
                {
                    TankTacticalInfo targetRecord = receptor.GetRecord(m_PlayerNumber);

                    // write updated information
                    targetRecord.lastKnownPosition = transform.position;
                    targetRecord.lastKnownDirection = (transform.position - receptor.transform.position).normalized;

                    targetRecord.isAlive = 0;

                    receptor.WriteRecord(targetRecord);
                }
            }

            // Move the instantiated explosion prefab to the tank's position and turn it on.
            m_ExplosionParticles.transform.position = transform.position;
            m_ExplosionParticles.gameObject.SetActive (true);

            // Play the particle system of the tank exploding.
            m_ExplosionParticles.Play ();

            // Play the tank explosion sound effect.
            m_ExplosionAudio.Play();

            // Turn the tank off.
            gameObject.SetActive (false);
        }
    }
}