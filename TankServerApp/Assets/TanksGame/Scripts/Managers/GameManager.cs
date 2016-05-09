using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UnityGame.Tanks
{
    public class GameManager : MonoBehaviour
    {
        public int m_NumRoundsToWin = 5;            // The number of rounds a single player has to win to win the game.
        public float m_ConnectDelay = 5f;           // The delay between RoundWaiting and RoundStarting.
        public float m_StartDelay = 3f;             // The delay between the start of RoundStarting and RoundPlaying phases.
        public float m_EndDelay = 3f;               // The delay between the end of RoundPlaying and RoundEnding phases.
        public float m_RoundTime = 2f;              // The time limit for each round to finish.
        public CameraControl m_CameraControl;       // Reference to the CameraControl script for control during different phases.
        public Text m_MessageText;                  // Reference to the overlay Text to display winning text, etc.
        public Text m_RoundText;                    // Reference to the overlay Text to display time remaining per round.

        public GameObject m_TankPrefab;             // Reference to the prefab of the tank.
        private List<TankPlayerController> m_PlayerControllers = new List<TankPlayerController>();

        private int m_RoundNumber;                  // Which round the game is currently on.
        private WaitForSeconds m_StartWait;         // Used to have a delay whilst the round starts.
        private WaitForSeconds m_EndWait;           // Used to have a delay whilst the round or game ends.
        private TankPlayerController m_RoundWinner;          // Reference to the winner of the current round.  Used to make an announcement of who won.
        private TankPlayerController m_GameWinner;           // Reference to the winner of the game.  Used to make an announcement of who won.
        private bool m_RoundInProgress = false;              // Used to determine whether a match is currently occurring.
        private bool m_NewGame = true;

        [SerializeField]
        private Transform[] m_SpawnPoints;

        [SerializeField]
        private Color[] m_PlayerColors;

        public int m_MinimumPlayerCount = 2;
        public int m_ActivePlayerCount
        {
            get
            {
                return m_PlayerControllers.Count(p => p.isActive);
            }
        }
        public int m_MaximumPlayerCount = 4;
        public int m_CombatantCount { get; private set; }

        private void Start()
        {
            // Create the delays so they only have to be made once.
            m_StartWait = new WaitForSeconds (m_StartDelay);
            m_EndWait = new WaitForSeconds (m_EndDelay);

            // Once the tanks have been created and the camera is using them as targets, start the game.
            StartCoroutine (GameLoop ());
        }

        public TankPlayerController AddPlayer()
        {
            // max player count reached?
            if (m_ActivePlayerCount + 1 > m_MaximumPlayerCount)
            {
                return null;
            }

            var newPlayerController = new TankPlayerController();
            m_PlayerControllers.Add(newPlayerController);
            newPlayerController.isActive = true;
            newPlayerController.pid = m_PlayerControllers.Count - 1;
            newPlayerController.m_PlayerColor = m_PlayerColors[m_ActivePlayerCount - 1];
            newPlayerController.m_SpawnPoint = m_SpawnPoints[m_ActivePlayerCount - 1];
            

            return newPlayerController;
        }
        public void RemovePlayer(TankPlayerController controller)
        {
            controller.Kill();
        }

        public GameObject SpawnSingleTank()
        {
            var newTank = Instantiate(m_TankPrefab) as GameObject;

            // don't set the tank in play if a round is already going
            newTank.SetActive(!m_RoundInProgress);

            m_CameraControl.m_Targets.Add(newTank.transform);

            return newTank;
        }

        // This is called from start and will run each phase of the game one after another.
        private IEnumerator GameLoop ()
        {
            // Wait for players to join the game
            yield return StartCoroutine (RoundWaiting());

            // Once players have loaded, start off by running the 'RoundStarting' coroutine but don't return until it's finished.
            yield return StartCoroutine (RoundStarting ());

            // Once the 'RoundStarting' coroutine is finished, run the 'RoundPlaying' coroutine but don't return until it's finished.
            yield return StartCoroutine (RoundPlaying());

            // Once execution has returned here, run the 'RoundEnding' coroutine, again don't return until it's finished.
            yield return StartCoroutine (RoundEnding());

            // This code is not run until 'RoundEnding' has finished.  At which point, check if a game winner has been found.
            if (m_GameWinner != null)
            {
                // If there is a game winner, restart the match.
                ResetAllWins();
                m_NewGame = true;
                m_RoundNumber = 0;
            }
            
            
            // If there isn't a winner yet, restart this coroutine so the loop continues.
            // Note that this coroutine doesn't yield.  This means that the current version of the GameLoop will end.
            StartCoroutine (GameLoop ());
            
        }

        private IEnumerator RoundWaiting ()
        {
            m_RoundText.text = "";

            // Wait for enough players to connnect
            while (m_ActivePlayerCount < m_MinimumPlayerCount)
            {
                m_MessageText.text = "WAITING FOR " + (m_MinimumPlayerCount - m_ActivePlayerCount) + " PLAYERS";
                yield return null;
            }

            float timeToStart = m_ConnectDelay;
            while(timeToStart > 0.0f && m_NewGame)
            {
                timeToStart -= Time.deltaTime;

                m_MessageText.text = "MATCH START IN\n" + (int)timeToStart;
                yield return null;
            }
            
        }
        private IEnumerator RoundStarting ()
        {
            m_NewGame = false;
            m_CombatantCount = m_ActivePlayerCount;

            // As soon as the round starts reset the tanks and make sure they can't move.
            ResetAllTanks ();
            DisableTankControl ();

            // Snap the camera's zoom and position to something appropriate for the reset tanks.
            m_CameraControl.SetStartPositionAndSize ();

            // Increment the round number and display text showing the players what round it is.
            m_RoundNumber++;
            m_MessageText.text = "ROUND " + m_RoundNumber;

            var timer = TimeSpan.FromSeconds(m_RoundTime * 60f);
            m_RoundText.text = string.Format("{0}:{1}", timer.Minutes.ToString("D2"), timer.Seconds.ToString("D2"));

            // Wait for the specified length of time until yielding control back to the game loop.
            yield return m_StartWait;
        }
        private IEnumerator RoundPlaying ()
        {
            m_RoundInProgress = true;

            // As soon as the round begins playing let the players control the tanks.
            EnableTankControl ();

            // Clear the text from the screen.
            m_MessageText.text = string.Empty;

            // TODO: How are null ref errors handled in coroutines? They quit?

            float roundTime = m_RoundTime * 60;

            // While there is not one tank left...
            while (!OneTankLeft() && roundTime > 0f)
            {
                roundTime -= Time.deltaTime;

                var timer = TimeSpan.FromSeconds(roundTime);

                m_RoundText.text = string.Format("{0}:{1}", timer.Minutes.ToString("D2"), timer.Seconds.ToString("D2"));

                // ... return on the next frame.
                yield return null;
            }

            // If the round ended without a clear winner, kill everyone to force a draw.
            if(roundTime < 0f)
            {
                KillAllTanks();
                m_RoundText.text = "00:00";
            }
        }
        private IEnumerator RoundEnding ()
        {
            m_RoundInProgress = false;

            // Stop tanks from moving.
            DisableTankControl ();

            // Clear the winner from the previous round.
            m_RoundWinner = null;

            // See if there is a winner now the round is over.
            m_RoundWinner = GetRoundWinner ();

            // If there is a winner, increment their score.
            if (m_RoundWinner != null)
                m_RoundWinner.m_Wins++;

            // Now the winner's score has been incremented, see if someone has one the game.
            m_GameWinner = GetGameWinner ();

            // If all other players quit, the remaining winner wins by default.
            if(m_ActivePlayerCount < 2)
            {
                m_GameWinner = m_RoundWinner;
            }

            // Get a message based on the scores and whether or not there is a game winner and display it.
            string message = EndMessage ();
            m_MessageText.text = message;

            // Wait for the specified length of time until yielding control back to the game loop.
            yield return m_EndWait;
        }

        // This is used to check if there is one or fewer tanks remaining and thus the round should end.
        private bool OneTankLeft()
        {
            // Start the count of tanks left at zero.
            int numTanksLeft = 0;

            // Go through all the tanks...
            for (int i = 0; i < m_PlayerControllers.Count; i++)
            {
                // ... and if they are active, increment the counter.
                if (m_PlayerControllers[i].m_Instance.activeSelf)
                    numTanksLeft++;
            }

            // If there are one or fewer tanks remaining return true, otherwise return false.
            return numTanksLeft <= 1;
        }
        
        // This function is to find out if there is a winner of the round.
        // This function is called with the assumption that 1 or fewer tanks are currently active.
        private TankPlayerController GetRoundWinner()
        {
            Debug.Assert(OneTankLeft(), "GetRoundWinner was called before one tank was left standing!");

            // Go through all the tanks...
            for (int i = 0; i < m_PlayerControllers.Count; i++)
            {
                // ... and if one of them is active, it is the winner so return it.
                if (m_PlayerControllers[i].m_Instance.activeSelf)
                    return m_PlayerControllers[i];
            }

            // If none of the tanks are active it is a draw so return null.
            return null;
        }

        // This function is to find out if there is a winner of the game.
        private TankPlayerController GetGameWinner()
        {
            // Go through all the tanks...
            for (int i = 0; i < m_PlayerControllers.Count; i++)
            {
                // ... and if one of them has enough rounds to win the game, return it.
                if (m_PlayerControllers[i].m_Wins == m_NumRoundsToWin)
                    return m_PlayerControllers[i];
            }

            // If no tanks have enough rounds to win, return null.
            return null;
        }

        // Returns a string message to display at the end of each round.
        private string EndMessage()
        {
            // By default when a round ends there are no winners so the default end message is a draw.
            string message = "DRAW!";

            // If there is a winner then change the message to reflect that.
            if (m_RoundWinner != null)
                message = m_RoundWinner.m_ColoredPlayerText + " WINS THE ROUND!";

            // Add some line breaks after the initial message.
            message += "\n\n\n\n";

            // Go through all the tanks and add each of their scores to the message.
            for (int i = 0; i < m_PlayerControllers.Count; i++)
            {
                if (m_PlayerControllers[i].isActive)
                {
                    message += m_PlayerControllers[i].m_ColoredPlayerText + ": " + m_PlayerControllers[i].m_Wins + " WINS\n";
                }
            }

            // If there is a game winner, change the entire message to reflect that.
            if (m_GameWinner != null)
                message = m_GameWinner.m_ColoredPlayerText + " WINS THE GAME!";

            return message;
        }

        // This function is used to turn all the tanks back on and reset their positions and properties.
        private void ResetAllWins()
        {
            for (int i = 0; i < m_PlayerControllers.Count; i++)
            {
                if (m_PlayerControllers[i].isActive)
                {
                    m_PlayerControllers[i].m_Wins = 0;
                }
            }
        }
        private void ResetAllTanks()
        {
            for (int i = 0; i < m_PlayerControllers.Count; i++)
            {
                if (m_PlayerControllers[i].isActive)
                {
                    m_PlayerControllers[i].Reset();
                }
            }
        }
        private void EnableTankControl()
        {
            for (int i = 0; i < m_PlayerControllers.Count; i++)
            {
                if (m_PlayerControllers[i].isActive)
                {
                    m_PlayerControllers[i].EnableControl();
                }
            }
        }
        private void DisableTankControl()
        {
            for (int i = 0; i < m_PlayerControllers.Count; i++)
            {
                if (m_PlayerControllers[i].isActive)
                {
                    m_PlayerControllers[i].DisableControl();

                }
            }
        }
        private void KillAllTanks()
        {
            for (int i = 0; i < m_PlayerControllers.Count; i++)
            {
                if (m_PlayerControllers[i].isActive)
                {
                    m_PlayerControllers[i].Kill();

                }
            }
        }
    }
}