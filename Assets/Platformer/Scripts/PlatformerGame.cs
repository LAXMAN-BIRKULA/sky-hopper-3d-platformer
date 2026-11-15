using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SkyHopper
{
    /// <summary>Run state: score, coin count, fall death, final score overlay and restart.</summary>
    public class PlatformerGame : MonoBehaviour
    {
        public static PlatformerGame I { get; private set; }
        public static bool IsDead => I != null && I.dead;
        public static bool IsWon => I != null && I.won;

        [Header("Rules")]
        public float killY = -12f;
        /// <summary>Below every walkable surface. Dropping past this means the player is falling off the course.</summary>
        public float fallDeathY = 0.2f;
        public int totalCoins = 12;

        [Header("Scene references")]
        public PlatformerPlayer player;

        [Header("HUD")]
        public Text scoreText;
        public Text coinText;

        [Header("Death screen")]
        public GameObject deathPanel;
        public Text finalScoreText;
        public Text deathHintText;

        [Header("Win screen")]
        public GameObject winPanel;
        public Text winSubtitleText;
        public Text winScoreText;
        public Text winHintText;

        public int Score { get; private set; }
        public int CoinsCollected { get; private set; }

        bool dead;
        bool won;
        bool deathAnimPlayed;

        void Awake()
        {
            I = this;
            if (player == null) player = FindAnyObjectByType<PlatformerPlayer>();
        }

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (deathPanel != null) deathPanel.SetActive(false);
            if (winPanel != null) winPanel.SetActive(false);
            RefreshHud();
        }

        void Update()
        {
            // The win screen is owned by LevelCompleteMenu (buttons + shortcuts).
            if (won) return;

            if (!dead)
            {
                if (player != null)
                {
                    // The moment he drops below the course, start the death animation so
                    // it plays visibly during the fall. The screen comes at killY.
                    if (!deathAnimPlayed && player.transform.position.y < fallDeathY)
                    {
                        deathAnimPlayed = true;
                        player.PlayDeathAnimation();
                    }

                    if (player.transform.position.y < killY) Die();
                }
                return;
            }

            if (RestartPressed()) Restart();
        }

        public void CollectCoin(int points)
        {
            if (dead || won) return;
            Score += points;
            CoinsCollected++;
            RefreshHud();

            if (CoinsCollected >= totalCoins) Win();
        }

        public void Die()
        {
            if (dead || won) return;
            dead = true;

            if (player != null)
            {
                // Fallback for any death that did not pass through the fall threshold.
                player.PlayDeathAnimation();
                player.InputEnabled = false;
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (deathPanel != null) deathPanel.SetActive(true);
            if (finalScoreText != null)
                finalScoreText.text = "FINAL SCORE   " + Score;
            if (deathHintText != null)
                deathHintText.text = "Press  R  or  Space  to restart";

            Sfx.Play(Sfx.DeathClip, transform.position, 0.7f);
        }

        public void Win()
        {
            if (dead || won) return;
            won = true;

            if (player != null) player.InputEnabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (winPanel != null) winPanel.SetActive(true);
            if (winSubtitleText != null) winSubtitleText.text = "ALL " + totalCoins + " COINS COLLECTED";
            if (winScoreText != null) winScoreText.text = "FINAL SCORE   " + Score;
            // winHintText is filled in by LevelCompleteMenu when the panel is enabled.

            Sfx.Play(Sfx.WinClip, transform.position, 0.7f);
        }

        public void Restart()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0) SceneManager.LoadScene(scene.buildIndex);
            else SceneManager.LoadScene(scene.name);
        }

        void RefreshHud()
        {
            if (scoreText != null) scoreText.text = Score.ToString();
            if (coinText != null) coinText.text = CoinsCollected + " / " + totalCoins;
        }

        static bool RestartPressed()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return false;
            return kb.rKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame;
        }
    }
}
