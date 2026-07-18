using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;

namespace StopJaywalking
{
    [FileLocation(nameof(StopJaywalking))]
    public class Setting : ModSetting
    {
        public const string Section = "Main";
        public const string Group = "Crossings";
        public const string GroupGeneral = "General";

        public Setting(IMod mod) : base(mod) { }

        // NOTE: property initializers double as the settings-migration failsafe — a key missing from an old settings
        // file keeps these values instead of defaulting to 0/false.

        // Master switch. OFF = vanilla (unmarked crossings back to their normal ~100 cost).
        [SettingsUISection(Section, Group)]
        public bool Enabled { get; set; } = true;

        // How much more costly an unmarked / jaywalk crossing is versus vanilla. Higher = pedestrians funnel harder to
        // marked zebra crossings. Applied by multiplying the game's unsafe-crosswalk pathfind cost.
        [SettingsUISlider(min = 1f, max = 30f, step = 1f, unit = "integer")]
        [SettingsUISection(Section, Group)]
        public float CostMultiplier { get; set; } = 10f;

        // How often (in-game hours) the mod re-applies the cost — the game can reset it after big road edits / a
        // prefab reload, so we re-assert periodically.
        [SettingsUISlider(min = 1f, max = 24f, step = 1f, unit = "integer")]
        [SettingsUISection(Section, Group)]
        public int ReassertIntervalHours { get; set; } = 1;

        // Keep platform achievements enabled while this mod is active (the game otherwise disables them for any mod).
        [SettingsUISection(Section, GroupGeneral)]
        public bool EnableAchievements { get; set; } = true;

        public override void SetDefaults()
        {
            Enabled = true;
            CostMultiplier = 10f;
            ReassertIntervalHours = 1;
            EnableAchievements = true;
        }
    }
}
