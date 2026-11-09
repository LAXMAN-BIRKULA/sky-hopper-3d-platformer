using System.Collections.Generic;
using UnityEngine;

namespace WheatAndCo
{
    /// <summary>Builds the crop plots and owns their growth and unlock state.</summary>
    public class FarmingSystem
    {
        readonly GameManager gm;
        readonly List<CropPlot> plots = new List<CropPlot>();
        readonly List<FarmField> fields = new List<FarmField>();

        public GameManager GameManager => gm;
        public IReadOnlyList<CropPlot> Plots => plots;

        public FarmingSystem(GameManager gameManager)
        {
            gm = gameManager;
        }

        /// <summary>Multiplier applied to growth speed by the sprinkler upgrade.</summary>
        public float GrowthMultiplier => gm.Progression.HasUpgrade(UpgradeId.Sprinkler) ? 1.25f : 1f;

        public void Build()
        {
            fields.AddRange(gm.World.Fields);

            foreach (var field in fields)
            {
                bool unlocked = gm.Progression.Level >= field.unlockLevel;
                field.SetUnlocked(unlocked);

                for (int i = 0; i < field.plotPositions.Count; i++)
                {
                    var go = new GameObject("Plot_" + field.crop + "_" + i);
                    go.transform.SetParent(gm.World.CropRoot, false);
                    go.transform.position = field.plotPositions[i] + new Vector3(0f, 0.02f, 0f);

                    var plot = go.AddComponent<CropPlot>();
                    plot.Initialize(this, field.crop, field.unlockLevel, unlocked);
                    field.plots.Add(plot);
                    plots.Add(plot);
                }
            }
        }

        public void Update(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            CheckUnlocks(false);
            for (int i = 0; i < plots.Count; i++)
                plots[i].Tick(deltaTime);
        }

        /// <summary>Public version used right after a level-up, with toast feedback.</summary>
        public void RefreshUnlocks()
        {
            CheckUnlocks(true);
        }

        void CheckUnlocks(bool showToast)
        {
            for (int f = 0; f < fields.Count; f++)
            {
                var field = fields[f];
                bool shouldUnlock = gm.Progression.Level >= field.unlockLevel;
                if (shouldUnlock == field.unlocked) continue;

                field.SetUnlocked(shouldUnlock);
                for (int i = 0; i < field.plots.Count; i++)
                    field.plots[i].SetUnlocked(shouldUnlock);

                if (showToast && shouldUnlock)
                {
                    gm.Audio.Play(Sfx.Objective, 0.7f);
                    gm.UI?.Toast(field.displayName + " field unlocked!", UITheme.Accent, 4f);
                }
            }
        }

        public void ResetAll()
        {
            for (int f = 0; f < fields.Count; f++)
            {
                var field = fields[f];
                bool unlocked = gm.Progression.Level >= field.unlockLevel;
                field.SetUnlocked(unlocked);
                for (int i = 0; i < field.plots.Count; i++)
                {
                    field.plots[i].SetUnlocked(unlocked);
                    field.plots[i].Restore(PlotStage.Empty, 0f);
                }
            }
        }

        public void ApplySave(int[] stages, float[] growth)
        {
            CheckUnlocks(false);

            for (int i = 0; i < plots.Count; i++)
            {
                int stage = stages != null && i < stages.Length ? stages[i] : 0;
                float progress = growth != null && i < growth.Length ? growth[i] : 0f;
                plots[i].Restore((PlotStage)Mathf.Clamp(stage, 0, 3), progress);
            }

            // If the loaded level unlocked fields, reflect that silently.
            CheckUnlocks(false);
        }

        public int[] SnapshotStages()
        {
            var result = new int[plots.Count];
            for (int i = 0; i < plots.Count; i++) result[i] = (int)plots[i].Stage;
            return result;
        }

        public float[] SnapshotGrowth()
        {
            var result = new float[plots.Count];
            for (int i = 0; i < plots.Count; i++) result[i] = plots[i].Growth01;
            return result;
        }
    }
}
