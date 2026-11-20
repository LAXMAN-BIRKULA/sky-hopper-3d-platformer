using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SkyHopper
{
    /// <summary>
    /// Applies a different UI style on touch devices: touch wording, bigger buttons, no
    /// QUIT GAME button (it blanks the browser tab), and a distinct level-select intro.
    /// Desktop keeps the authored scene layout untouched.
    /// </summary>
    public class UiPlatformStyle : MonoBehaviour
    {
        static UiPlatformStyle instance;
        static readonly HashSet<int> resized = new HashSet<int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (instance != null) return;
            new GameObject("UiPlatformStyle").AddComponent<UiPlatformStyle>();
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            StartCoroutine(ApplyNextFrame());
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (instance == this) instance = null;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartCoroutine(ApplyNextFrame());
        }

        IEnumerator ApplyNextFrame()
        {
            // Runtime-built menus (pause, win, death) are created during scene load.
            yield return null;
            yield return null;
            Apply();
        }

        public static void Refresh()
        {
            if (instance != null) instance.Apply();
        }

        void Apply()
        {
            bool mobile = MobileInput.IsTouchDevice;
            string sceneName = SceneManager.GetActiveScene().name;

            Text[] texts = FindObjectsByType<Text>(FindObjectsInactive.Include);
            foreach (Text text in texts)
            {
                if (text == null || text.gameObject == null) continue;

                string name = text.gameObject.name;
                string value = text.text ?? string.Empty;

                if (name == "Title" && sceneName == "LevelSelect")
                {
                    text.fontSize = mobile ? 86 : 118;
                    continue;
                }

                if (name == "Subtitle" || value.Contains("CHOOSE A LEVEL"))
                {
                    if (mobile) text.text = "TAP A LEVEL TO PLAY";
                    else if (value.Contains("TAP A LEVEL")) text.text = "CHOOSE A LEVEL";
                    continue;
                }

                if (name != "Hint" && name != "deathHintText" && name != "winHintText" &&
                    !value.Contains("Esc") && !value.Contains("Press") && !value.Contains("Tap"))
                    continue;

                if (mobile)
                {
                    if (value.Contains("Esc / P")) text.text = "Tap RESUME to continue";
                    else if (value.Contains("Press") && value.Contains("restart")) text.text = "Tap anywhere to retry";
                    else if (value.Contains("Press 1") || value.Contains("Press 2")) text.text = "Tap a level to start";
                    else if (value.Contains("WASD") || value.Contains("MOUSE")) text.text = "Drag to look  •  Tap JUMP to leap";
                }
                else
                {
                    if (value.Contains("Tap RESUME")) text.text = "Esc / P to resume";
                    else if (value.Contains("Tap anywhere to retry")) text.text = "Press R or Space to restart";
                    else if (value.Contains("Tap a level")) text.text = "Press 1 or 2 to start  •  Q to quit";
                }
            }

            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
            foreach (Button button in buttons)
            {
                if (button == null || button.gameObject == null) continue;
                // The win screen owns its own responsive column layout in LevelCompleteMenu.
                if (button.transform.parent != null && button.transform.parent.name == "WinPanel") continue;

                Text label = button.GetComponentInChildren<Text>(true);
                string labelText = label != null ? label.text ?? string.Empty : string.Empty;
                bool isQuit = button.gameObject.name.Contains("QuitGame") || labelText.Contains("QUIT GAME");

                if (mobile && isQuit)
                {
                    button.gameObject.SetActive(false);
                    continue;
                }

                if (!mobile || !resized.Add(RuntimeHelpers.GetHashCode(button))) continue;

                RectTransform rect = button.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.sizeDelta = new Vector2(
                        Mathf.Max(rect.sizeDelta.x, 700f),
                        Mathf.Max(rect.sizeDelta.y, 150f));

                    Vector2 pos = rect.anchoredPosition;
                    pos.y *= 1.12f;
                    rect.anchoredPosition = pos;
                }

                if (label != null) label.fontSize = Mathf.Max(label.fontSize, 54);
            }
        }
    }
}
