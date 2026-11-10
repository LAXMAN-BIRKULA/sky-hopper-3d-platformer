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

        [Header("Rules")]
        public float killY = -12f;
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

        public int Score { get; private set; }
        public int CoinsCollected { get; private set; }

        bool dead;

        void Awake()
        {
            I = this;
            if (player == null) player = FindFirstObjectByType<PlatformerPlayer>();
        }

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (deathPanel != null) deathPanel.SetActive(false);
            RefreshHud();
        }

        void Update()
        {
            if (!dead)
            {
                if (player != null && player.transform.position.y < killY) Die();
                return;
            }

            if (RestartPressed()) Restart();
        }

        public void CollectCoin(int points)
        {
            if (dead) return;
            Score += points;
            CoinsCollected++;
            RefreshHud();
        }

        public void Die()
        {
            if (dead) return;
            dead = true;

            if (player != null) player.InputEnabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (deathPanel != null) deathPanel.SetActive(true);
            if (finalScoreText != null)
                finalScoreText.text = "FINAL SCORE   " + Score;
            if (deathHintText != null)
                deathHintText.text = "Press  R  or  Space  to restart";

            Sfx.Play(Sfx.DeathClip, transform.position, 0.7f);
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
