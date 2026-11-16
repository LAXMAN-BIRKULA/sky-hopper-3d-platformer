using UnityEngine;

namespace SkyHopper
{
    /// <summary>
    /// Closes the game. Standalone players quit; the editor leaves play mode; a browser
    /// build cannot close its own tab, so it clears the page to make the exit obvious.
    /// </summary>
    public static class AppQuit
    {
        public static void Quit()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
            Application.OpenURL("about:blank");
#else
            Application.Quit();
#endif
        }
    }
}
