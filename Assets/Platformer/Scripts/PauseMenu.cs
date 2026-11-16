using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SkyHopper
{
    /// <summary>
    /// Pause overlay with RESUME / RETRY / QUIT. Created automatically on every run,
    /// works with Escape or P on a keyboard and with the on-screen pause button on touch.
    /// Freezes time while open and restores it when the level unloads.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        public static PauseMenu I { get; private set; }
        public static bool IsPaused => I != null && I.paused;

        bool paused;
        GameObject panel;
        Text titleText;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (I != null) return;
            new GameObject("PauseMenu").AddComponent<PauseMenu>();
        }

        void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }

            I = this;
            DontDestroyOnLoad(gameObject);
            Build();
        }

        void OnDestroy()
        {
            if (I == this)
            {
                I = null;
                Time.timeScale = 1f;
                AudioListener.pause = false;
            }
        }

        void Update()
        {
            Keyboard kb = Keyboard.current;
            bool pressed = (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame)) ||
                           MobileInput.PausePressed;
            MobileInput.PausePressed = false;

            if (pressed)
            {
                if (paused) Close();
                else if (CanPause()) Open();
            }

            // Escape/P is a toggle, so while paused only the panel buttons (and Enter) act.
        }

        /// <summary>Pause is only meaningful while a level is actually being played.</summary>
        public static bool CanPause()
        {
            return PlatformerGame.I != null && !PlatformerGame.IsWon && !PlatformerGame.IsDead;
        }

        public void Toggle()
        {
            if (paused) Close();
            else if (CanPause()) Open();
        }

        public void Open()
        {
            if (paused || panel == null) return;
            paused = true;
            panel.SetActive(true);
            Time.timeScale = 0f;
            AudioListener.pause = true;
            MobileInput.Reset();

            var players = FindObjectsByType<PlatformerPlayer>(FindObjectsInactive.Exclude);
            foreach (PlatformerPlayer player in players) player.InputEnabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(ResumeButton.gameObject);
        }

        public void Close()
        {
            if (!paused) return;
            paused = false;
            if (panel != null) panel.SetActive(false);
            Time.timeScale = 1f;
            AudioListener.pause = false;

            var players = FindObjectsByType<PlatformerPlayer>(FindObjectsInactive.Exclude);
            foreach (PlatformerPlayer player in players) player.InputEnabled = true;

            Cursor.lockState = MobileInput.Active ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = MobileInput.Active;

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        /// <summary>
        /// Restores time, input and cursor and hides the overlay. This object survives
        /// scene loads, so the panel must be hidden explicitly before leaving the level.
        /// </summary>
        void Teardown()
        {
            paused = false;
            if (panel != null) panel.SetActive(false);
            Time.timeScale = 1f;
            AudioListener.pause = false;
            MobileInput.Reset();

            var players = FindObjectsByType<PlatformerPlayer>(FindObjectsInactive.Exclude);
            foreach (PlatformerPlayer player in players) player.InputEnabled = true;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        public void Retry()
        {
            Teardown();
            Load(SceneManager.GetActiveScene());
        }

        public void Quit()
        {
            Teardown();
            SceneManager.LoadScene(LevelCompleteMenu.MenuSceneName);
        }

        /// <summary>Closes the whole game, not just the level.</summary>
        public void QuitGame()
        {
            Teardown();
            AppQuit.Quit();
        }

        static void Load(Scene scene)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (scene.buildIndex >= 0) SceneManager.LoadScene(scene.buildIndex);
            else SceneManager.LoadScene(scene.name);
        }

        Button ResumeButton => panel != null ? panel.transform.Find("Resume").GetComponent<Button>() : null;

        void Build()
        {
            var canvasObject = new GameObject("PauseCanvas", typeof(RectTransform));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();

            var panelRect = new GameObject("Panel", typeof(RectTransform));
            panelRect.transform.SetParent(canvasObject.transform, false);
            RectTransform rect = panelRect.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var backdrop = panelRect.AddComponent<Image>();
            backdrop.color = new Color(0.03f, 0.05f, 0.10f, 0.82f);
            backdrop.raycastTarget = true;

            titleText = NewText("Title", rect, "PAUSED", 96, TextAnchor.MiddleCenter);
            SetRect(titleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(900f, 140f));

            CreateButton(rect, "Resume", "RESUME", new Vector2(0f, 40f), Close);
            CreateButton(rect, "Retry", "RETRY LEVEL", new Vector2(0f, -75f), Retry);
            CreateButton(rect, "Quit", "LEVEL SELECT", new Vector2(0f, -190f), Quit);
            CreateButton(rect, "QuitGame", "QUIT GAME", new Vector2(0f, -305f), QuitGame);

            Text hint = NewText("Hint", rect, "Esc / P  to resume", 34, TextAnchor.MiddleCenter);
            hint.color = new Color(1f, 1f, 1f, 0.55f);
            SetRect(hint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -410f), new Vector2(900f, 60f));

            panel = panelRect;
            panel.SetActive(false);
        }

        void CreateButton(RectTransform parent, string name, string label, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            var image = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = new Color(0.16f, 0.42f, 0.85f, 0.95f);
            SetRect(image.rectTransform, new Vector2(0.5f, 0.5f), position, new Vector2(560f, 104f));

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.28f, 0.56f, 1f, 1f);
            colors.pressedColor = new Color(0.12f, 0.32f, 0.68f, 1f);
            button.colors = colors;
            button.onClick.AddListener(action);

            Text text = NewText("Label", image.rectTransform, label, 44, TextAnchor.MiddleCenter);
            text.fontStyle = FontStyle.Bold;
            SetRect(text.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 100f));
        }

        static Text NewText(string name, RectTransform parent, string value, int size, TextAnchor anchor)
        {
            var component = new GameObject(name, typeof(RectTransform)).AddComponent<Text>();
            component.transform.SetParent(parent, false);
            component.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            component.text = value;
            component.alignment = anchor;
            component.color = Color.white;
            component.fontSize = size;
            component.raycastTarget = false;
            return component;
        }

        static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
