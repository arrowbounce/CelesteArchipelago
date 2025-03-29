using System;
using System.Linq;

namespace Celeste.Mod.CelesteArchipelago
{
    public static class CelesteArchipelagoSettingsUI
    {
        public static void CreateMenu(TextMenu menu)
        {
            DeathlinkModeSelector(menu);
        }

        public static void DeathlinkModeSelector(TextMenu menu)
        {
            TextMenu.Option<DeathLinkMode> modeSelectionMenu = new(Dialog.Clean("archipelago_settings_deathlink_mode_label_title"));

            modeSelectionMenu.Add(Dialog.Clean($"archipelago_settings_deathlink_mode_label_room"), DeathLinkMode.Room, true);
            foreach (DeathLinkMode mode in Enum.GetValues(typeof(DeathLinkMode)))
            {
                if (mode == DeathLinkMode.Room)
                {
                    continue;
                }

                string label = "archipelago_settings_deathlink_mode_label_" + mode.ToString().ToLower();
                bool selected = mode == CelesteArchipelagoModule.Settings.DeathLinkMode;
                modeSelectionMenu.Add(Dialog.Clean(label), mode, selected);
            }

            modeSelectionMenu.Change(selectedMode => {
                CelesteArchipelagoModule.Settings.DeathLinkMode = selectedMode;
            });

            menu.Insert(menu.Items.Count - 2, modeSelectionMenu);

            // Can only add descriptions after option has been added to the menu
            foreach (DeathLinkMode mode in ((DeathLinkMode[])Enum.GetValues(typeof(DeathLinkMode))).Reverse())
            {
                modeSelectionMenu.AddDescription(menu, Dialog.Clean("archipelago_settings_deathlink_mode_description_" + mode.ToString().ToLower()));
            }
        }
    }
}