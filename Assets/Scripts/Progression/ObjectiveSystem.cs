using System;
using System.Collections.Generic;
using UnityEngine;

namespace WheatAndCo
{
    public enum ObjEvent
    {
        HarvestWheat,
        HarvestCarrot,
        HarvestPumpkin,
        PlantCorn,
        FeedCow,
        CollectMilk,
        SellProduce,
        BuyChicken,
        CollectEgg,
        ReachLevel
    }

    public class Objective
    {
        public string title;
        public ObjEvent evt;
        public int target;
        public int progress;
        public int rewardCoins;
        public int rewardXp;
        public bool completed;

        public string ProgressLabel => $"{Mathf.Min(progress, target)} / {target}";
        public float Progress01 => target <= 0 ? 1f : Mathf.Clamp01((float)progress / target);
    }

    /// <summary>
    /// A gentle, ordered goal list that teaches the loop and celebrates milestones.
    /// Only the current objective is shown, keeping the HUD calm.
    /// </summary>
    public class ObjectiveSystem
    {
        readonly GameManager gm;
        readonly List<Objective> objectives = new List<Objective>();
        int index;
        bool completing;

        public event Action Changed;

        public ObjectiveSystem(GameManager gameManager)
        {
            gm = gameManager;
            BuildList();
        }

        public Objective Current => index >= 0 && index < objectives.Count ? objectives[index] : null;
        public bool IsComplete => Current == null;
        public int Index => index;

        void BuildList()
        {
            objectives.Clear();
            objectives.Add(Obj("Harvest 5 wheat", ObjEvent.HarvestWheat, 5, 30, 40));
            objectives.Add(Obj("Feed a cow with wheat", ObjEvent.FeedCow, 1, 25, 30));
            objectives.Add(Obj("Collect a bottle of milk", ObjEvent.CollectMilk, 1, 30, 40));
            objectives.Add(Obj("Sell 8 items at the market", ObjEvent.SellProduce, 8, 50, 60));
            objectives.Add(Obj("Reach level 2", ObjEvent.ReachLevel, 2, 40, 0));
            objectives.Add(Obj("Plant 3 corn", ObjEvent.PlantCorn, 3, 40, 50));
            objectives.Add(Obj("Reach level 3", ObjEvent.ReachLevel, 3, 50, 0));
            objectives.Add(Obj("Buy a chicken", ObjEvent.BuyChicken, 1, 40, 50));
            objectives.Add(Obj("Collect 3 eggs", ObjEvent.CollectEgg, 3, 60, 60));
            objectives.Add(Obj("Reach level 4", ObjEvent.ReachLevel, 4, 80, 0));
            objectives.Add(Obj("Harvest 5 carrots", ObjEvent.HarvestCarrot, 5, 60, 70));
            objectives.Add(Obj("Reach level 6", ObjEvent.ReachLevel, 6, 100, 0));
        }

        static Objective Obj(string title, ObjEvent evt, int target, int coins, int xp)
        {
            return new Objective
            {
                title = title,
                evt = evt,
                target = target,
                rewardCoins = coins,
                rewardXp = xp
            };
        }

        public void Begin()
        {
            // Level goals may already be satisfied if a save was loaded.
            var cur = Current;
            if (cur != null && cur.evt == ObjEvent.ReachLevel)
                cur.progress = Mathf.Max(cur.progress, gm.Progression.Level);
            Changed?.Invoke();
        }

        public void Notify(ObjEvent evt, int amount)
        {
            var cur = Current;
            if (cur == null || amount <= 0) return;
            if (cur.evt == ObjEvent.ReachLevel) return;
            if (cur.evt != evt) return;
            cur.progress = Mathf.Min(cur.target, cur.progress + amount);
            CheckComplete();
        }

        public void NotifyLevel(int level)
        {
            var cur = Current;
            if (cur == null || cur.evt != ObjEvent.ReachLevel) return;
            cur.progress = Mathf.Max(cur.progress, level);
            CheckComplete();
        }

        void CheckComplete()
        {
            if (completing) return;
            completing = true;

            int guard = 0;
            while (guard++ < 30)
            {
                var cur = Current;
                if (cur == null) break;
                if (cur.evt == ObjEvent.ReachLevel)
                    cur.progress = Mathf.Max(cur.progress, gm.Progression.Level);
                if (cur.progress < cur.target) break;

                cur.completed = true;
                int coins = cur.rewardCoins;
                int xp = cur.rewardXp;
                string title = cur.title;

                gm.Economy.Add(coins, false);
                gm.Audio?.Play(Sfx.Objective);
                gm.UI?.Toast($"Goal complete: {title}   +{coins} coins", UITheme.Good, 4f);

                index++;
                if (xp > 0) gm.Progression.AddXp(xp, "Objective");
            }

            completing = false;
            Changed?.Invoke();
            gm.UI?.RefreshAll();
        }

        public void ResetNewGame()
        {
            index = 0;
            foreach (var objective in objectives) objective.progress = 0;
            Changed?.Invoke();
        }

        public void ApplySave(int savedIndex, int[] progress)
        {
            index = Mathf.Clamp(savedIndex, 0, objectives.Count);
            for (int i = 0; i < objectives.Count; i++)
            {
                objectives[i].progress = 0;
                objectives[i].completed = false;
            }

            if (progress != null)
                for (int i = 0; i < progress.Length && i < objectives.Count; i++)
                    objectives[i].progress = Mathf.Clamp(progress[i], 0, objectives[i].target);

            for (int i = 0; i < index && i < objectives.Count; i++)
                objectives[i].completed = true;

            Changed?.Invoke();
        }

        public int[] SnapshotProgress()
        {
            var result = new int[objectives.Count];
            for (int i = 0; i < objectives.Count; i++) result[i] = objectives[i].progress;
            return result;
        }
    }
}
