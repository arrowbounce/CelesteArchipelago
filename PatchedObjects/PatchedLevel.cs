using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.CelesteArchipelago
{
    public class PatchedLevel : IPatchable
    {
        public void Load()
        {
            On.Celeste.Level.Update += Update;
            Everest.Events.Level.OnEnter += OnEnter;
            Everest.Events.Level.OnTransitionTo += OnTransitionTo;
            Everest.Events.Level.OnExit += OnExit;
        }

        public void Unload()
        {
            On.Celeste.Level.Update -= Update;
            Everest.Events.Level.OnEnter -= OnEnter;
            Everest.Events.Level.OnTransitionTo -= OnTransitionTo;
            Everest.Events.Level.OnExit -= OnExit;
        }

        private static void Update(On.Celeste.Level.orig_Update orig, Level self)
        {
            if (!(ArchipelagoController.Instance.DeathLinkPool.Count > 0
            && self.Tracker.GetEntity<Player>().InControl
            && !self.InCutscene
            && !self.InCredits))
            {
                // No deathlink to process or deathlink mode is set to room
                orig(self);
                return;
            }

            switch (CelesteArchipelagoModule.Settings.DeathLinkMode)
            {
                case DeathLinkMode.SubChapter:
                    ArchipelagoController.Instance.FlushDeathLinkMessage();

                    // Places user at last collected sub-chapter/checkpoint
                    self.Session.StartCheckpoint = ArchipelagoController.Instance.CheckpointState.LastHitCheckpoint;

                    RestartChapter(self);
                    break;
                case DeathLinkMode.Chapter:
                    ArchipelagoController.Instance.FlushDeathLinkMessage();

                    // Re-Lock Sub-Chapters
                    ArchipelagoController.Instance.CheckpointState.RemoveAreaCheckpoints(self.Session.Area);
                    // Places user at start of level
                    self.Session.StartCheckpoint = null;

                    RestartChapter(self);
                    break;
                case DeathLinkMode.ChapterNoGoal:
                    if (ArchipelagoController.Instance.VictoryCondition == ConvertLevelToVictoryCondition(self.Session.Area))
                    {
                        // Is Goal Level, so don't restart
                        break;
                    }
                    
                    ArchipelagoController.Instance.FlushDeathLinkMessage();

                    // Re-Lock Sub-Chapters
                    ArchipelagoController.Instance.CheckpointState.RemoveAreaCheckpoints(self.Session.Area);
                    // Places user at start of level
                    self.Session.StartCheckpoint = null;

                    RestartChapter(self);
                    break;
            }

            orig(self);
        }

        private static void RestartChapter(Level level)
        {
            level.DoScreenWipe(wipeIn: false, delegate
            {
                Monocle.Engine.Scene = new LevelExit(LevelExit.Mode.Restart, level.Session);
            });
        }

        private static VictoryConditionOptions? ConvertLevelToVictoryCondition(AreaKey area)
        {
            Dictionary<int, Dictionary<AreaMode, VictoryConditionOptions>> areaToVictoryCondition = new()
            {
                {
                    7, new Dictionary<AreaMode, VictoryConditionOptions>
                    {
                        { AreaMode.Normal, VictoryConditionOptions.CHAPTER_7_SUMMIT_A },
                        { AreaMode.BSide, VictoryConditionOptions.CHAPTER_7_SUMMIT_B },
                        { AreaMode.CSide, VictoryConditionOptions.CHAPTER_7_SUMMIT_C }
                    }
                },
                {
                    9, new Dictionary<AreaMode, VictoryConditionOptions>
                    {
                        { AreaMode.Normal, VictoryConditionOptions.CHAPTER_8_CORE_A },
                        { AreaMode.BSide, VictoryConditionOptions.CHAPTER_8_CORE_B },
                        { AreaMode.CSide, VictoryConditionOptions.CHAPTER_8_CORE_C }
                    }
                },
                {
                    10, new Dictionary<AreaMode, VictoryConditionOptions>
                    {
                        { AreaMode.Normal, VictoryConditionOptions.CHAPTER_9_FAREWELL_A }
                    }
                }
            };

            if (!areaToVictoryCondition.ContainsKey(area.ID)) 
            {
                return null;
            }

            return areaToVictoryCondition[area.ID][area.Mode];
        }

        private static void OnEnter(Session session, bool fromSaveData)
        {
            var state = new PlayState(false, session.Area, session.LevelData.Name);
            Logger.Log("CelesteArchipelago", $"Entering level. Setting PlayState to {state}");
            ArchipelagoController.Instance.PlayState = state;
            if (session.StartedFromBeginning)
            {
                ArchipelagoController.Instance.CheckpointState.LastHitCheckpoint = null;
            }
            else if (session.LevelData.HasCheckpoint)
            {
                ArchipelagoController.Instance.CheckpointState.LastHitCheckpoint = session.LevelData.Name;
            }
        }

        private static void OnTransitionTo(Level level, LevelData next, Vector2 direction)
        {
            var state = new PlayState(false, level.Session.Area, next.Name);
            Logger.Log("CelesteArchipelago", $"Transitioning level. Setting PlayState to {state}");
            ArchipelagoController.Instance.PlayState = state;

            if (next.HasCheckpoint)
            {
                ArchipelagoController.Instance.CheckpointState.LastHitCheckpoint = next.Name;
            }
        }

        private static void OnExit(Level level, LevelExit exit, LevelExit.Mode mode, Session session, HiresSnow snow)
        {
            if (mode != LevelExit.Mode.SaveAndQuit)
            {
                var state = new PlayState(true, level.Session.Area, "overworld");
                Logger.Log("CelesteArchipelago", $"Exiting level. Setting PlayState to {state}");
                ArchipelagoController.Instance.PlayState = state;
                ArchipelagoController.Instance.CheckpointState.LastHitCheckpoint = null; // Preventing aysnc shenanigans
            }
        }
    }
}