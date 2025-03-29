namespace Celeste.Mod.CelesteArchipelago
{
    internal interface IPatchable
    {
        public void Load();
        public void Unload();
    }
}