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
        public static StopJaywalkingSetting ActiveSetting;

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            ActiveSetting = new StopJaywalkingSetting(this);
            ActiveSetting.RegisterInOptionsUI();
            var lm = GameManager.instance.localizationManager;
            foreach (var locale in lm.GetSupportedLocales())
                lm.AddSource(locale, new LocaleEn(ActiveSetting, locale));
            AssetDatabase.global.LoadSettings(nameof(StopJaywalking), ActiveSetting, new StopJaywalkingSetting(this));
            // Apply the cost the instant the user changes a setting (immediate feedback), on top of the interval re-assert.
            ActiveSetting.onSettingsApplied += OnSettingsApplied;

            updateSystem.UpdateAt<StopJaywalkingSystem>(SystemUpdatePhase.GameSimulation);

            log.Info("[SelfTest] StopJaywalking loaded (jaywalk-cost deterrent).");
        }

        // Fires the instant the user changes any Stop Jaywalking setting: push the new cost immediately so the change
        // is visible right away instead of waiting for the next interval re-assert.
        // NOTE: this handler deliberately does NOT persist — vanilla AutomaticSettings already calls ApplyAndSave() on
        // every options-widget change, so saving here only doubled traffic into a task queue that silently drops work
        // when over its cap of 2.
        private static void OnSettingsApplied(Game.Settings.Setting setting)
        {
            StopJaywalkingSystem.Instance?.ApplyNow();
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
        private readonly StopJaywalkingSetting m_S;
        private readonly string m_L;
        public LocaleEn(StopJaywalkingSetting setting, string locale) { m_S = setting; m_L = locale; }
        private string T(string k) => Translations.Get(k, m_L);

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_S.GetSettingsLocaleID(), "Stop Jaywalking" },
                { m_S.GetOptionTabLocaleID(StopJaywalkingSetting.Section), "Main" },
                { m_S.GetOptionGroupLocaleID(StopJaywalkingSetting.Group), T("grp.Group") },
                { m_S.GetOptionLabelLocaleID(nameof(StopJaywalkingSetting.Enabled)), T("opt.Enabled.L") },
                { m_S.GetOptionDescLocaleID(nameof(StopJaywalkingSetting.Enabled)), T("opt.Enabled.D") },
                { m_S.GetOptionLabelLocaleID(nameof(StopJaywalkingSetting.CostMultiplier)), T("opt.CostMultiplier.L") },
                { m_S.GetOptionDescLocaleID(nameof(StopJaywalkingSetting.CostMultiplier)), T("opt.CostMultiplier.D") },
                { m_S.GetOptionLabelLocaleID(nameof(StopJaywalkingSetting.ReassertIntervalHours)), T("opt.ReassertIntervalHours.L") },
                { m_S.GetOptionDescLocaleID(nameof(StopJaywalkingSetting.ReassertIntervalHours)), T("opt.ReassertIntervalHours.D") },

                { m_S.GetOptionGroupLocaleID(StopJaywalkingSetting.GroupGeneral), T("grp.GroupGeneral") },
            };
        }

        public void Unload() { }
    }
}
