using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.CelesteArchipelago {
    public class CelesteArchipelagoModuleSettings : EverestModuleSettings
    {
        [SettingMaxLength(30), SettingName("archipelago_settings_name_label")]
        public string Name { get; set; } = "Madeline";
        [SettingName("archipelago_settings_password_label")]
        public string Password { get; set; } = "";
        [SettingMaxLength(30), SettingName("archipelago_settings_server_label")]
        public string Server { get; set; } = "archipelago.gg";
        [SettingName("archipelago_settings_port_label")]
        public string Port { get; set; } = "38281";
        private bool _Chat = false;
        [SettingName("archipelago_settings_chat_label")]
        public bool Chat { 
            get => _Chat; 
            set {
                _Chat = value;
                if (value) {
                    ArchipelagoController.Instance.Init();
                } else {
                    ArchipelagoController.Instance.DeInit();
                }
            }
        }
        
        private bool _DeathLink = false;
        [SettingInGame(true), SettingName("archipelago_settings_deathlink_label")]
        public bool DeathLink
        {
            get => _DeathLink;
            set
            {
                if (ArchipelagoController.Instance.DeathLinkService is not null)
                {
                    if (value) {
                        ArchipelagoController.Instance.DeathLinkService.EnableDeathLink();
                    }
                    else {
                        ArchipelagoController.Instance.DeathLinkService.DisableDeathLink();
                    }
                }
                _DeathLink = value;
            }
        }

        public DeathAmnestyVisibility AmnestyVisibility = DeathAmnestyVisibility.AfterDeathAndInMenu;

        // If adding new options under this comment, do not forget to change the DeathLinkMode insert value in the settingsUI
        public DeathLinkMode DeathLinkMode = DeathLinkMode.Room;

        [DefaultButtonBinding(Buttons.Back, Keys.T), SettingName("archipelago_settings_chat_toggle_label")]
        public ButtonBinding ToggleChat { get; set; }
        [DefaultButtonBinding(Buttons.RightThumbstickUp, Keys.Q), SettingName("archipelago_settings_chat_scroll_up_label")]
        public ButtonBinding ScrollChatUp { get; set; }
        [DefaultButtonBinding(Buttons.RightThumbstickDown, Keys.Z), SettingName("archipelago_settings_chat_scroll_down_label")]
        public ButtonBinding ScrollChatDown { get; set; }
    }
}