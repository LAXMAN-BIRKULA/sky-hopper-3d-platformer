using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SkyHopper
{
    /// <summary>
    /// Virtual gamepad state shared by the on-screen touch controls. The player and the
    /// camera read these values and merge them with the keyboard/mouse, so the same build
    /// works with a keyboard or with a touch screen.
    /// </summary>
    public static class MobileInput
    {
        /// <summary>Movement stick, same convention as WASD (x = strafe, y = forward).</summary>
        public static Vector2 Move;

        /// <summary>Jump/climb button held (same as holding Space).</summary>
        public static bool JumpHeld;

        /// <summary>Camera look drag in screen pixels, consumed once per frame by the camera.</summary>
        public static Vector2 LookDelta;

        /// <summary>Set by the on-screen pause button and consumed by the pause menu.</summary>
        public static bool PausePressed;

        public static bool Active => TouchControls.Exists;

        /// <summary>
        /// True when this run should use the touch layout. Kept separate from Active so UI
        /// styling can be applied before the touch-control object has finished booting.
        /// </summary>
        public static bool IsTouchDevice
        {
            get
            {
                string url = Application.absoluteURL ?? string.Empty;
                if (url.Contains("touch=0")) return false;
                if (url.Contains("touch=1")) return true;
                return Application.isMobilePlatform ||
                       SystemInfo.deviceType == DeviceType.Handheld ||
                       (Touchscreen.current != null && Touchscreen.current.enabled);
            }
        }

        public static void Reset()
        {
            Move = Vector2.zero;
            JumpHeld = false;
            LookDelta = Vector2.zero;
            PausePressed = false;
        }
    }

    /// <summary>
    /// Builds a small on-screen gamepad (stick, jump button, look area) when the game runs on
    /// a touch device. Created automatically, so no scene needs the component.
    /// Append ?touch=1 to the URL to force it on a desktop browser, ?touch=0 to hide it.
    /// </summary>
    public class TouchControls : MonoBehaviour
    {
        public static bool Exists { get; private set; }

        static Sprite circleSprite;

        GameObject controlsRoot;
        float sceneCheckTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (!MobileInput.IsTouchDevice) return;
            Ensure();
        }

        /// <summary>Creates the overlay if it does not exist yet.</summary>
        public static void Ensure()
        {
            if (Exists) return;
            new GameObject("TouchControls").AddComponent<TouchControls>();
        }

        void Awake()
        {
            if (Exists)
            {
                Destroy(gameObject);
                return;
            }

            Exists = true;
            MobileInput.Reset();
            DontDestroyOnLoad(gameObject);
            Build();
        }

        void OnDestroy()
        {
            if (Exists)
            {
                Exists = false;
                MobileInput.Reset();
            }
        }

        void Build()
        {
            EnsureEventSystem();

            var canvasObject = new GameObject("TouchCanvas", typeof(RectTransform));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();

            // Everything that belongs to gameplay sits under one root so it can be hidden
            // on the menu / win / pause screens without touching the canvas itself.
            var controlsObject = new GameObject("Controls", typeof(RectTransform));
            controlsObject.transform.SetParent(canvasObject.transform, false);
            RectTransform controls = controlsObject.GetComponent<RectTransform>();
            controls.anchorMin = Vector2.zero;
            controls.anchorMax = Vector2.one;
            controls.offsetMin = Vector2.zero;
            controls.offsetMax = Vector2.zero;
            controlsRoot = controlsObject;

            // Right-hand drag area rotates the camera (added first so buttons sit on top).
            CreateLookPad(controls);

            CreateJoystick(controls, new Vector2(0f, 0f), new Vector2(190f, 190f), 300f);
            CreateJumpButton(controls, new Vector2(1f, 0f), new Vector2(-190f, 190f), 230f);
            CreatePauseButton(controls, new Vector2(1f, 1f), new Vector2(-120f, -100f), 130f);

            UpdateVisibility();
        }

        void Update()
        {
            sceneCheckTimer -= Time.unscaledDeltaTime;
            if (sceneCheckTimer > 0f) return;
            sceneCheckTimer = 0.25f;
            UpdateVisibility();
        }

        /// <summary>Hide the pad on the menu, and while the win/death/pause UI is up.</summary>
        void UpdateVisibility()
        {
            if (controlsRoot == null) return;

            bool inGame = PlatformerGame.I != null && !PlatformerGame.IsWon && !PlatformerGame.IsDead
                          && !PauseMenu.IsPaused;
            if (controlsRoot.activeSelf == inGame) return;

            controlsRoot.SetActive(inGame);
            if (!inGame) MobileInput.Reset();
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var system = new GameObject("EventSystem", typeof(EventSystem));
            system.AddComponent<InputSystemUIInputModule>();
        }

        static void CreateLookPad(RectTransform parent)
        {
            var pad = NewImage("LookPad", parent, Color.clear);
            RectTransform rect = pad.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            pad.gameObject.AddComponent<LookPad>();
        }

        static void CreateJoystick(RectTransform parent, Vector2 anchor, Vector2 offset, float size)
        {
            var background = NewImage("Joystick", parent, new Color(1f, 1f, 1f, 0.16f));
            background.sprite = Circle();
            background.type = Image.Type.Simple;

            RectTransform area = background.rectTransform;
            area.anchorMin = anchor;
            area.anchorMax = anchor;
            area.pivot = new Vector2(0.5f, 0.5f);
            area.anchoredPosition = offset;
            area.sizeDelta = new Vector2(size, size);

            var handle = NewImage("Handle", area, new Color(1f, 1f, 1f, 0.5f));
            handle.sprite = Circle();
            RectTransform handleRect = handle.rectTransform;
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(size * 0.42f, size * 0.42f);
            handleRect.anchoredPosition = Vector2.zero;

            var stick = background.gameObject.AddComponent<VirtualJoystick>();
            stick.area = area;
            stick.handle = handleRect;
            stick.radius = size * 0.34f;
        }

        static void CreateJumpButton(RectTransform parent, Vector2 anchor, Vector2 offset, float size)
        {
            var background = NewImage("Jump", parent, new Color(1f, 1f, 1f, 0.24f));
            background.sprite = Circle();

            RectTransform rect = background.rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = offset;
            rect.sizeDelta = new Vector2(size, size);

            var label = new GameObject("Label", typeof(RectTransform)).AddComponent<Text>();
            label.transform.SetParent(rect, false);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = "JUMP";
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(1f, 1f, 1f, 0.85f);
            label.fontSize = 46;
            label.fontStyle = FontStyle.Bold;
            label.raycastTarget = false;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            background.gameObject.AddComponent<JumpButton>();
        }

        static void CreatePauseButton(RectTransform parent, Vector2 anchor, Vector2 offset, float size)
        {
            var background = NewImage("Pause", parent, new Color(0f, 0f, 0f, 0.35f));
            background.sprite = Circle();

            RectTransform rect = background.rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = offset;
            rect.sizeDelta = new Vector2(size, size);

            // Two bars instead of a font glyph, so it reads the same everywhere.
            for (int i = 0; i < 2; i++)
            {
                var bar = NewImage("Bar" + i, rect, new Color(1f, 1f, 1f, 0.9f));
                RectTransform barRect = bar.rectTransform;
                barRect.anchorMin = new Vector2(0.5f, 0.5f);
                barRect.anchorMax = new Vector2(0.5f, 0.5f);
                barRect.pivot = new Vector2(0.5f, 0.5f);
                barRect.sizeDelta = new Vector2(size * 0.11f, size * 0.42f);
                barRect.anchoredPosition = new Vector2((i == 0 ? -1f : 1f) * size * 0.14f, 0f);
                bar.raycastTarget = false;
            }

            background.gameObject.AddComponent<PauseButton>();
        }

        static Image NewImage(string name, RectTransform parent, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            image.raycastTarget = true;
            return image;
        }

        /// <summary>Soft-edged white circle so the controls do not need an imported sprite.</summary>
        static Sprite Circle()
        {
            if (circleSprite != null) return circleSprite;

            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.name = "TouchCircle";

            var pixels = new Color32[size * size];
            float center = size * 0.5f;
            float radius = center - 1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                    float alpha = Mathf.Clamp01(radius - distance);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            circleSprite.name = "TouchCircle";
            return circleSprite;
        }
    }

    /// <summary>Drag stick: the touch position relative to the stick centre drives movement.</summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform area;
        public RectTransform handle;
        public float radius = 100f;

        bool dragging;

        public void OnPointerDown(PointerEventData eventData)
        {
            dragging = true;
            Apply(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging) dragging = true;
            Apply(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            dragging = false;
            MobileInput.Move = Vector2.zero;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
        }

        void Apply(PointerEventData eventData)
        {
            if (area == null) return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position, null, out Vector2 local))
                return;

            Vector2 clamped = Vector2.ClampMagnitude(local, radius);
            if (handle != null) handle.anchoredPosition = clamped;
            MobileInput.Move = clamped / Mathf.Max(1f, radius);
        }
    }

    /// <summary>Drag anywhere on the right of the screen to orbit the camera.</summary>
    public class LookPad : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        public void OnPointerDown(PointerEventData eventData) { }

        public void OnPointerUp(PointerEventData eventData) { }

        public void OnDrag(PointerEventData eventData)
        {
            MobileInput.LookDelta += eventData.delta;
        }
    }

    /// <summary>Jump on press, climb while held (mirrors the Space key).</summary>
    public class JumpButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public void OnPointerDown(PointerEventData eventData)
        {
            MobileInput.JumpHeld = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            MobileInput.JumpHeld = false;
        }

        // A finger can lift outside the button, and the pause menu can hide it mid-press.
        void OnDisable()
        {
            MobileInput.JumpHeld = false;
        }
    }

    /// <summary>Top-right button that asks the pause menu to open.</summary>
    public class PauseButton : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            MobileInput.PausePressed = true;
            MobileInput.Move = Vector2.zero;
            MobileInput.JumpHeld = false;
        }
    }

    /// <summary>Tapping the death screen restarts, since a phone has no R key.</summary>
    public class TapToRestart : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            if (PlatformerGame.I != null) PlatformerGame.I.Restart();
        }
    }
}
