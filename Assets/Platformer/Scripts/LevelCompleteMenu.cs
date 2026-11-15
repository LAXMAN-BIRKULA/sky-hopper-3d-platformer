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
        public Text hintText;

        bool HasNext => !string.IsNullOrEmpty(nextLevelScene);

        void OnEnable()
        {
            if (nextButton != null) nextButton.gameObject.SetActive(HasNext);

            // Keep whichever buttons are visible centered in one row (level 2 has no NEXT).
            var row = new List<Button>(3);
            if (HasNext) row.Add(nextButton);
            row.Add(restartButton);
            row.Add(quitButton);

            const float spacing = 380f;
            for (int i = 0; i < row.Count; i++)
            {
                if (row[i] == null) continue;
                var rt = (RectTransform)row[i].transform;
                rt.anchoredPosition = new Vector2((i - (row.Count - 1) * 0.5f) * spacing, rt.anchoredPosition.y);
            }

            if (hintText != null)
                hintText.text = HasNext
                    ? "N  next level      R  restart      Q  quit"
                    : "R  restart      Q  quit";

            // Select the first choice so arrows + Enter also work without a mouse.
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject((HasNext ? nextButton : restartButton).gameObject);
        }

        void OnDisable()
        {
            var current = EventSystem.current;
            if (current == null || current.currentSelectedGameObject == null) return;

            GameObject selected = current.currentSelectedGameObject;
            if (selected == gameObject ||
                (nextButton != null && selected == nextButton.gameObject) ||
                (restartButton != null && selected == restartButton.gameObject) ||
                (quitButton != null && selected == quitButton.gameObject))
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
            else if (keyboard.qKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)
                QuitToMenu();
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

        static void LoadScene(string sceneName)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(sceneName);
        }
    }
}
