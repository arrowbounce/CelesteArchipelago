using Microsoft.Xna.Framework;

namespace Celeste.Mod.CelesteArchipelago
{
    public class PatchedPlayer : IPatchable
    {
        public void Load()
        {
            On.Celeste.Player.Update += Update;
            Everest.Events.Player.OnSpawn += OnSpawn;
            Everest.Events.Player.OnDie += OnDie;
        }

        public void Unload()
        {
            On.Celeste.Player.Update -= Update;
            Everest.Events.Player.OnSpawn -= OnSpawn;
            Everest.Events.Player.OnDie -= OnDie;
        }

        private static void Update(On.Celeste.Player.orig_Update orig, Player self)
        {
            if (ArchipelagoController.Instance.DeathLinkPool.Count > 0
            && self.InControl
            && !self.SceneAs<Level>().InCutscene
            && !self.SceneAs<Level>().InCredits
            )
            {
                ArchipelagoController.Instance.FlushDeathLinkMessage();
                self.Die(Vector2.Zero, true);
            }
            orig(self);
        }

        private static void OnSpawn(Player self)
        {
            DeathAmnestyUI entity = self.SceneAs<Level>().Tracker.GetEntity<DeathAmnestyUI>();
            if (entity != null)
            {
                entity.UpdateDisplayText();
            }
        }

        private static void OnDie(Player self)
        {
            ArchipelagoController.Instance.SendDeathLinkCallback();
            ArchipelagoController.Instance.IsLocalDeath = ArchipelagoController.Instance.DeathLinkPool.Count < 1;
        }
    }
}