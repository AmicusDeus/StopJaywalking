using System.Collections.Generic;
using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Game.Simulation;

namespace StopJaywalking
{
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(StopJaywalking)}.{nameof(Mod)}").SetShowsErrorsInUI(false);
        public static Setting ActiveSetting;

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            ActiveSetting = new Setting(this);
            ActiveSetting.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEn(ActiveSetting));
            AssetDatabase.global.LoadSettings(nameof(StopJaywalking), ActiveSetting, new Setting(this));
            // Apply the cost the instant the user changes a setting (immediate feedback), on top of the interval re-assert.
            ActiveSetting.onSettingsApplied += OnSettingsApplied;

            updateSystem.UpdateAt<StopJaywalkingSystem>(SystemUpdatePhase.GameSimulation);
            // Keep platform achievements enabled while the mod is active.
            updateSystem.UpdateAt<AchievementEnablerSystem>(SystemUpdatePhase.GameSimulation);

            log.Info("[SelfTest] StopJaywalking loaded (jaywalk-cost deterrent).");
        }

        // Fires the instant the user changes any Stop Jaywalking setting: push the new cost immediately so the change is
        // visible right away, AND write it straight to disk so a crash / non-clean exit can't lose it. Guarded because
        // ApplyAndSave re-raises onSettingsApplied.
        private static bool s_savingReentrant;
        private static void OnSettingsApplied(Game.Settings.Setting setting)
        {
            if (s_savingReentrant)
                return;
            s_savingReentrant = true;
            try
            {
                StopJaywalkingSystem.Instance?.ApplyNow();
                ActiveSetting?.ApplyAndSave();
            }
            finally { s_savingReentrant = false; }
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
            if (ActiveSetting != null)
            {
                ActiveSetting.onSettingsApplied -= OnSettingsApplied;
                ActiveSetting.UnregisterInOptionsUI();
                ActiveSetting = null;
            }
        }
    }

    // Minimal English locale (same pipeline as the other mods).
    public class LocaleEn : IDictionarySource
    {
        private readonly Setting m_S;
        public LocaleEn(Setting setting) { m_S = setting; }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_S.GetSettingsLocaleID(), "Stop Jaywalking" },
                { m_S.GetOptionTabLocaleID(Setting.Section), "Main" },
                { m_S.GetOptionGroupLocaleID(Setting.Group), "Pedestrian crossings" },
                { m_S.GetOptionLabelLocaleID(nameof(Setting.Enabled)), "Enable Stop Jaywalking" },
                { m_S.GetOptionDescLocaleID(nameof(Setting.Enabled)), "Makes jaywalking (crossing a road away from a marked crosswalk) expensive so pedestrians funnel to zebra crossings. Off = vanilla behaviour." },
                { m_S.GetOptionLabelLocaleID(nameof(Setting.CostMultiplier)), "Jaywalk cost multiplier" },
                { m_S.GetOptionDescLocaleID(nameof(Setting.CostMultiplier)), "How much more costly an unmarked crossing is versus vanilla. Higher = pedestrians avoid jaywalking harder and detour to marked crossings. No one is ever fully stranded — if no crossing is reachable they still cross, just reluctantly." },
                { m_S.GetOptionLabelLocaleID(nameof(Setting.ReassertIntervalHours)), "Re-apply interval (in-game hours)" },
                { m_S.GetOptionDescLocaleID(nameof(Setting.ReassertIntervalHours)), "How often the mod re-applies the cost. The game can reset it after big road edits; a shorter interval re-applies sooner." },

                { m_S.GetOptionGroupLocaleID(Setting.GroupGeneral), "General" },
                { m_S.GetOptionLabelLocaleID(nameof(Setting.EnableAchievements)), "Keep achievements enabled" },
                { m_S.GetOptionDescLocaleID(nameof(Setting.EnableAchievements)), "Cities: Skylines II disables achievements whenever any mod is active. This re-enables them. Safe to leave on." },
            };
        }

        public void Unload() { }
    }
}
