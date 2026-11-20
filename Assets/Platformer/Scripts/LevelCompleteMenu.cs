using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SkyHopper
{
    /// <summary>
    /// Options shown on the win screen: next level, restart, quit back to the menu.
    /// Lives on the win panel, so it starts working the moment the panel is shown.
    /// </summary>
    public class LevelCompleteMenu : MonoBehaviour
    {
        public const string MenuSceneName = "LevelSelect";

        [Tooltip("Scene loaded by NEXT LEVEL. Leave empty on the last level to hide that button.")]
        public string nextLevelScene = "";

        [Header("Buttons")]
        public Button nextButton;
        public Button restartButton;
        public Button quitButton;
        public Button quitGameButton;
        public Text hintText;

        bool HasNext => !string.IsNullOrEmpty(nextLevelScene);

        void OnEnable()
        {
            bool mobile = MobileInput.IsTouchDevice;

            if (nextButton != null) nextButton.gameObject.SetActive(HasNext);
            // QUIT GAME blanks the browser tab, which looks broken on touch devices.
            if (quitGameButton != null) quitGameButton.gameObject.SetActive(!mobile);

            var row = new List<Button>(4);
            if (HasNext && nextButton != null) row.Add(nextButton);
            if (restartButton != null) row.Add(restartButton);
            if (quitButton != null) row.Add(quitButton);
            if (!mobile && quitGameButton != null && quitGameButton.gameObject.activeSelf) row.Add(quitGameButton);

            if (mobile)
            {
                // A single wide column is touch-friendly and cannot overlap like a row.
                const float firstY = -120f;
                const float stepY = 130f;
                for (int i = 0; i < row.Count; i++)
                {
                    var rt = (RectTransform)row[i].transform;
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(700f, 120f);
                    rt.anchoredPosition = new Vector2(0f, firstY - i * stepY);
                }

                if (hintText != null)
                {
                    var hintRect = (RectTransform)hintText.transform;
                    hintRect.anchoredPosition = new Vector2(0f, firstY - row.Count * stepY - 10f);
                }
            }
            else
            {
                // Desktop keeps the authored horizontal row, centered by actual button width.
                const float spacing = 380f;
                for (int i = 0; i < row.Count; i++)
                {
                    var rt = (RectTransform)row[i].transform;
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2((i - (row.Count - 1) * 0.5f) * spacing, -205f);
                }

                if (hintText != null)
                {
                    var hintRect = (RectTransform)hintText.transform;
                    hintRect.anchoredPosition = new Vector2(0f, -320f);
                }
            }

            if (hintText != null)
                hintText.text = mobile
                    ? (HasNext ? "Tap NEXT  •  RESTART  •  LEVELS" : "Tap RESTART  •  LEVELS")
                    : (HasNext
                        ? "N  next      R  restart      L  levels      Q  quit"
                        : "R  restart      L  levels      Q  quit");

            // Select the first choice so arrows + Enter also work without a mouse.
            Button first = HasNext && nextButton != null ? nextButton : restartButton;
            if (EventSystem.current != null && first != null && first.gameObject.activeSelf)
                EventSystem.current.SetSelectedGameObject(first.gameObject);
        }

        void OnDisable()
        {
            var current = EventSystem.current;
            if (current == null || current.currentSelectedGameObject == null) return;

            GameObject selected = current.currentSelectedGameObject;
            if (selected == gameObject ||
                (nextButton != null && selected == nextButton.gameObject) ||
                (restartButton != null && selected == restartButton.gameObject) ||
                (quitButton != null && selected == quitButton.gameObject) ||
                (quitGameButton != null && selected == quitGameButton.gameObject))
            {
                current.SetSelectedGameObject(null);
            }
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Enter is handled by the UI Submit action on the selected button.
            if (HasNext && keyboard.nKey.wasPressedThisFrame)
                NextLevel();
            else if (keyboard.rKey.wasPressedThisFrame)
                RestartLevel();
            else if (keyboard.lKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)
                QuitToMenu();
            else if (keyboard.qKey.wasPressedThisFrame)
                QuitGame();
        }

        public void NextLevel()
        {
            if (HasNext) LoadScene(nextLevelScene);
        }

        public void RestartLevel()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0) SceneManager.LoadScene(scene.buildIndex);
            else SceneManager.LoadScene(scene.name);
        }

        public void QuitToMenu()
        {
            LoadScene(MenuSceneName);
        }

        /// <summary>Closes the whole game from the win screen.</summary>
        public void QuitGame()
        {
            AppQuit.Quit();
        }

        static void LoadScene(string sceneName)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(sceneName);
        }
    }
}
