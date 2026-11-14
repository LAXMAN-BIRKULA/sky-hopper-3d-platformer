using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkyHopper
{
    /// <summary>Main menu: pick which level to play.</summary>
    public class LevelSelect : MonoBehaviour
    {
        public void PlayLevel1()
        {
            SceneManager.LoadScene("Level1");
        }

        public void PlayLevel2()
        {
            SceneManager.LoadScene("Level2");
        }

        void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.digit1Key.wasPressedThisFrame) PlayLevel1();
            else if (keyboard.digit2Key.wasPressedThisFrame) PlayLevel2();
        }
    }
}
