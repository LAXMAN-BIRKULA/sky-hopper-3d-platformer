using UnityEngine;

namespace WheatAndCo
{
    /// <summary>
    /// Single scene entry point. Add this component to one empty GameObject in
    /// SampleScene; it creates the GameManager and the entire farm at play time.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameBootstrap : MonoBehaviour
    {
        void Awake()
        {
            if (GameManager.Instance != null) return;

            var manager = gameObject.AddComponent<GameManager>();
            manager.Initialize();
        }
    }
}
