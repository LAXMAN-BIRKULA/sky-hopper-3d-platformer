using UnityEngine;

namespace WheatAndCo
{
    /// <summary>
    /// Owns every gameplay system and drives the overall loop: boot, update,
    /// pause, autosave and the level-up hand-off. Systems stay plain C# classes
    /// so this is the only object that needs a scene reference.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameDatabase Database { get; private set; }
        public MaterialLibrary Materials { get; private set; }
        public InventorySystem Inventory { get; private set; }
        public EconomySystem Economy { get; private set; }
        public ProgressionSystem Progression { get; private set; }
        public ObjectiveSystem Objectives { get; private set; }
        public FarmingSystem Farming { get; private set; }
        public AnimalSystem Animals { get; private set; }
        public InteractionSystem Interaction { get; private set; }
        public SaveSystem Save { get; private set; }
        public WorldBuilder World { get; private set; }
        public UIManager UI { get; private set; }
        public AudioManager Audio { get; private set; }

        public PlayerController Player { get; private set; }
        public ThirdPersonCamera CameraRig { get; private set; }
        public Camera MainCamera { get; private set; }

        public bool Initialized { get; private set; }
        public bool IsPaused { get; private set; }

        /// <summary>True when the player may move and interact right now.</summary>
        public bool CanPlayerAct => Initialized && !IsPaused && UI != null && !UI.AnyModalOpen;

        float autosaveTimer;
        const float AutosaveSeconds = 45f;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        /// <summary>Builds the whole game. Called once by GameBootstrap.</summary>
        public void Initialize()
        {
            if (Initialized) return;

            Time.timeScale = 1f;
            Database = GameDatabase.LoadOrCreate();

            Materials = new MaterialLibrary();
            Materials.Initialize();

            Audio = gameObject.AddComponent<AudioManager>();
            Audio.Initialize();

            Inventory = new InventorySystem(this);
            Economy = new EconomySystem(this);
            Progression = new ProgressionSystem(this);
            Objectives = new ObjectiveSystem(this);
            Save = new SaveSystem(this);

            // Interaction must exist before the world and animals register themselves.
            Interaction = new InteractionSystem(this);

            World = new WorldBuilder(this);
            World.Build();

            Farming = new FarmingSystem(this);
            Farming.Build();

            Animals = new AnimalSystem(this);
            Animals.Build();

            var rig = PlayerFactory.Create(this, World.PlayerSpawn);
            Player = rig.player;
            CameraRig = rig.camera;
            MainCamera = rig.unityCamera;

            UI = gameObject.AddComponent<UIManager>();
            UI.Initialize(this);

            Progression.LeveledUp += OnLeveledUp;
            Objectives.Begin();
            UI.RefreshAll();

            if (!Save.TryLoad())
            {
                StartNewFarm(false);
            }
            else
            {
                UI.RefreshAll();
                UI.Toast("Welcome back to Wheat & Co.!", UITheme.Accent, 4f);
            }

            Initialized = true;
            SetPaused(false);
        }

        void Update()
        {
            if (!Initialized) return;

            // Global menus. Modal panels consume Escape first.
            if (InputReader.PausePressed)
            {
                if (UI.LevelPanelOpen) UI.AdvanceLevelPanel();
                else if (UI.ConfirmOpen) UI.CloseConfirm();
                else if (UI.ShopOpen) UI.CloseShop();
                else if (UI.PauseOpen) UI.ResumeFromPause();
                else UI.OpenPause();
            }

            if (InputReader.ShopPressed && !IsPaused && UI != null && !UI.AnyModalOpen)
                UI.OpenShop();

            Interaction?.Update();
            Farming?.Update(Time.deltaTime);
            Animals?.Update(Time.deltaTime);

            if (!IsPaused)
            {
                autosaveTimer += Time.unscaledDeltaTime;
                if (autosaveTimer >= AutosaveSeconds)
                {
                    autosaveTimer = 0f;
                    SaveGame(false);
                }
            }
        }

        void OnApplicationQuit()
        {
            if (Initialized && Save != null) Save.Save();
        }

        /// <summary>Freeze the world for a menu or popup.</summary>
        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            if (Player != null) Player.InputEnabled = !paused;
            if (CameraRig != null) CameraRig.InputEnabled = !paused;
        }

        void OnLeveledUp(int level)
        {
            // Unlock farm content immediately so the popup matches the world.
            Farming?.RefreshUnlocks();
            Animals?.RefreshLevelUnlocks();
            UI?.EnqueueLevelUp(level);
        }

        public void SaveGame(bool toast)
        {
            if (Save == null) return;
            Save.Save();
            if (toast) UI?.Toast("Farm saved.", UITheme.Good);
        }

        /// <summary>Full reset used by the pause menu.</summary>
        public void RestartFarm()
        {
            StartNewFarm(true);
            UI?.RefreshAll();
            UI?.Toast("A fresh farm awaits. Have fun!", UITheme.Accent, 4f);
        }

        void StartNewFarm(bool deleteSave)
        {
            Economy.SetCoins(60);
            Inventory.ResetToNewGame();
            Progression.SetProgress(1, 0);
            Progression.SetUpgrades(null);
            Objectives.ResetNewGame();
            Farming.ResetAll();
            Animals.ResetAll();
            Player.Teleport(World.PlayerSpawn);
            if (CameraRig != null) CameraRig.Snap();
            if (deleteSave) Save.Delete();
            UI?.RefreshAll();
        }
    }
}
