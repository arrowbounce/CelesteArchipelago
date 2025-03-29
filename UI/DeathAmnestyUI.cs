using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CelesteArchipelago
{
    [Tracked(false)]
    public class DeathAmnestyUI : Entity
    {
        private Level level;
        private MTexture APIcon;
        private MTexture SkullIcon;
        private MTexture Background;
        private float Scale = 1f;
        private MTexture _X;
        private string _text;
        private float _lerp;
        private float _width;
        private float _timer = 0f;
        private float _despawnLimit = -200f;

        public DeathAmnestyUI(Level level, bool isAPConnected = false)
        {
            this.level = level;

            Background = GFX.Gui["strawberryCountBG"];
            APIcon = GFX.Gui["archipelago/menu/start"];
            SkullIcon = SetSkullIcon(level.Session.Area);
            _X = GFX.Gui["x"];

            Y = GetYPosition();

            if (isAPConnected)
            {
                UpdateDisplayText();
            }

            Tag = Tags.HUD | Tags.Global | Tags.PauseUpdate | Tags.TransitionUpdate;
        }

        private float GetYPosition()
        {
            float posY = 230f;
            if (level.TimerHidden)
            {
                return posY;
            }

            if (Settings.Instance.SpeedrunClock == SpeedrunType.Chapter)
            {
                posY += 58f;
            }
            else if (Settings.Instance.SpeedrunClock == SpeedrunType.File)
            {
                posY += 78f;
            }

            return posY;
        }

        public void UpdateDisplayText()
        {
            _text = $"{ArchipelagoController.Instance.DeathAmnestyCount} / {ArchipelagoController.Instance.SlotData.DeathAmnestyMax}";
            _width = ActiveFont.Measure(_text).X + 144f + 6f;
            _despawnLimit = -_width + 1f;
            _timer = 3f;
        }

        private bool CanShow()
        {
            if (!CelesteArchipelagoModule.Settings.DeathLink)
            {
                return false;
            }

            switch (CelesteArchipelagoModule.Settings.AmnestyVisibility)
            {
                case DeathAmnestyVisibility.Always:
                    return true;
                case DeathAmnestyVisibility.AfterDeathAndInMenu:
                    return (level.Paused && level.PauseMainMenuOpen) || _timer > 0f;
                case DeathAmnestyVisibility.InMenu:
                    return level.Paused && level.PauseMainMenuOpen;
                case DeathAmnestyVisibility.AfterDeath:
                    return _timer > 0f;
            }

            return false;
        }

        public override void Update()
        {
            base.Update();
            Y = Calc.Approach(Y, GetYPosition(), Engine.DeltaTime * 800f);

            if (CanShow())
            {
                _lerp = Calc.Approach(_lerp, 1f, Engine.DeltaTime * 1.2f);
            }
            else
            {
                _lerp = Calc.Approach(_lerp, 0f, Engine.DeltaTime * 2f);
            }

            if (_timer > 0f)
            {
                _timer -= Engine.DeltaTime;
            }
        }

        public override void Render()
        {
            base.Render();
            Vector2 basePos = Vector2.Lerp(new Vector2(0 - _width, Y), new Vector2(0, Y), Ease.CubeOut(_lerp)).Round();

            if (basePos.X < _despawnLimit)
            {
                return;
            }

            Background.Draw(new Vector2(_width - Background.Width + basePos.X, Y));

            if (_width > Background.Width + basePos.X)
            {
                Draw.Rect(0, Y, _width - Background.Width + basePos.X, 38f, Color.Black);
            }

            APIcon.DrawCentered(new Vector2(basePos.X + 60f, Y + 19f), Color.White, Scale * 0.3f);
            SkullIcon.DrawCentered(new Vector2(basePos.X + 80f, Y + 39f), Color.White, Scale * 0.7f);

            _X.DrawCentered(new Vector2(basePos.X + 120f, Y + 9f), Color.White, Scale);

            ActiveFont.DrawOutline(_text, new(basePos.X + 145f, Y - 24f), Vector2.Zero, Vector2.One, Color.White, 2f, Color.Black);
        }

        public MTexture SetSkullIcon(AreaKey area)
        {
            if (area.SID == "Celeste/LostLevels")
            {
                return GFX.Gui["collectables/skullGold"];
            }

            switch (area.Mode)
            {
                case AreaMode.Normal:
                    return GFX.Gui["collectables/skullBlue"];
                case AreaMode.BSide:
                    return GFX.Gui["collectables/skullRed"];
                case AreaMode.CSide:
                    return GFX.Gui["collectables/skullGold"];
                default:
                    return GFX.Gui["collectables/skullBlue"];
            }
        }
    }

    public enum DeathAmnestyVisibility
    {
        Disabled,
        AfterDeath,
        InMenu,
        AfterDeathAndInMenu,
        Always,
    }
}