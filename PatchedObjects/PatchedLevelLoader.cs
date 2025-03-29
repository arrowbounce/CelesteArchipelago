namespace Celeste.Mod.CelesteArchipelago
{
    public class PatchedLevelLoader : IPatchable
    {
        public void Load()
        {
            On.Celeste.LevelLoader.StartLevel += StartLevel;
        }

        public void Unload()
        {
            On.Celeste.LevelLoader.StartLevel -= StartLevel;
        }

        private static void StartLevel(On.Celeste.LevelLoader.orig_StartLevel orig, LevelLoader self)
        {
            self.Level.Add(new DeathAmnestyUI(self.Level));
            orig(self);
        }
    }
}