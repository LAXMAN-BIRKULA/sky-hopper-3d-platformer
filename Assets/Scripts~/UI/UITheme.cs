using UnityEngine;

namespace WheatAndCo
{
    /// <summary>Shared colours and font for every runtime UI element.</summary>
    public static class UITheme
    {
        public static readonly Color Background = new Color(0.10f, 0.13f, 0.11f, 0.94f);
        public static readonly Color Panel = new Color(0.15f, 0.19f, 0.16f, 0.97f);
        public static readonly Color PanelLight = new Color(0.21f, 0.26f, 0.22f, 1f);
        public static readonly Color Accent = new Color(0.98f, 0.80f, 0.36f, 1f);
        public static readonly Color Gold = Accent;
        public static readonly Color Text = new Color(0.96f, 0.94f, 0.87f, 1f);
        public static readonly Color Muted = new Color(0.73f, 0.74f, 0.66f, 0.92f);
        public static readonly Color Good = new Color(0.56f, 0.86f, 0.50f, 1f);
        public static readonly Color Bad = new Color(0.95f, 0.43f, 0.37f, 1f);
        public static readonly Color Dim = new Color(0f, 0f, 0f, 0.64f);
        public static readonly Color DarkText = new Color(0.12f, 0.14f, 0.11f, 1f);

        static Font cachedFont;

        public static Font Font
        {
            get
            {
                if (cachedFont == null)
                    cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return cachedFont;
            }
        }
    }
}
