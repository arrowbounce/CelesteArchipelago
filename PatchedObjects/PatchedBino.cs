using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteArchipelago
{
    class PatchedBino : IPatchable
    {
        public void Load()
        {
            On.Celeste.Lookout.Interact += BinoCollect;
        }
        public void Unload()
        {
            On.Celeste.Lookout.Interact -= BinoCollect;
        }

        private static void BinoCollect(On.Celeste.Lookout.orig_Interact orig, Lookout self, Player player)
        {
            Level level = self.Scene as Level;
            Logger.Log("CelesteArchipelago", "Looking at bino");
            Logger.Log("CelesteArchipelago", $"{self.Position}, ${ArchipelagoController.Instance.PlayState}");
            orig(self, player);
        }

    }
}
