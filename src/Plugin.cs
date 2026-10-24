using System;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;

namespace FiendGetConfigured
{
    [BepInPlugin(Id, "FiendGetConfigured", VersionInfo.Version)]
    [BepInDependency("com.TheFiend", "1.1.6")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Id = "TranslationLookasideBufferModding.FiendGetConfigured";
        private Harmony harmony;

        private void Awake()
        {
            bool enabled = Config.Bind("General", "Enabled", true,
                "Enable FiendGetConfigured. Restart the game after changing any setting. Install the same version and config on ALL players.").Value;
            Settings s = new Settings();
            s.WalkSpeed = Float("Movement", "WalkSpeed", 3f, 0f, 100f, "Movement speed while roaming or walking.");
            s.StalkSpeed = Float("Movement", "StalkSpeed", 1f, 0f, 100f, "Movement speed in the Fiend's seeking/stalking mode.");
            s.ChaseSpeed = Float("Movement", "ChaseSpeed", 5.5f, 0f, 100f, "Movement speed during a normal chase.");
            s.RageSpeed = Float("Movement", "RageSpeed", 7f, 0f, 100f, "Movement speed during a rage chase.");
            s.Damage = Integer("Combat", "Damage", 25, 0, 1000, "Damage of a normal hit. The execution threshold is configured separately.");
            s.ExecutionHealthThreshold = Integer("Combat", "ExecutionHealthThreshold", 25, -1, 100,
                "Use the original lethal grab when the victim has at most this many HP BEFORE a hit. -1 disables the execution grab; normal damage can still kill.");
            s.AttackDistance = Float("Combat", "AttackDistance", 2f, 0.1f, 10f,
                "Maximum center-to-center contact attack distance in meters. The original contact collider is still required; increasing this does not enlarge it.");
            s.AttackCooldownSeconds = Float("Combat", "AttackCooldownSeconds", 2f, 0.1f, 60f,
                "Minimum seconds between accepted attacks. Also replaces the original normal-hit cooldown. An execution animation may take longer.");
            s.KnockbackForce = Float("Combat", "KnockbackForce", 15f, 0f, 100f, "Knockback of a normal hit. Does not change death-body effects.");
            s.RequireLineOfSight = Config.Bind("Perception", "RequireLineOfSight", true,
                "Require visibility when acquiring a player and before a contact attack. The Fiend searches the LAST seen position during target memory.").Value;
            s.DetectionRange = Float("Perception", "DetectionRange", 30f, 0.5f, 40f,
                "Maximum distance for acquiring a player in meters. The v81 base game's target selector has a 40-meter visibility limit.");
            s.FieldOfViewDegrees = Float("Perception", "FieldOfViewDegrees", 140f, 10f, 360f, "Full field of view used for player acquisition.");
            s.TargetMemorySeconds = Float("Perception", "TargetMemorySeconds", 8f, 0f, 120f,
                "How long to search the last seen position after losing visibility. 0 forgets immediately. Dead players and players leaving the valid area are forgotten.");
            s.TeleportEnabled = Config.Bind("Abilities", "TeleportEnabled", false, "Allow the original random player teleport ability.").Value;
            s.ApparatusRageEnabled = Config.Bind("Abilities", "ApparatusRageEnabled", false, "Allow rage when the apparatus is removed. Overrides the original Fiend setting while this addon is enabled.").Value;
            s.TemporaryRageEnabled = Config.Bind("Abilities", "TemporaryRageEnabled", false, "Allow temporary rage triggered through the original feared/seeking behavior. Flashlight fear/stun itself is preserved.").Value;
            s.BreakDoorsEnabled = Config.Bind("Abilities", "BreakDoorsEnabled", true, "Allow the original door-breaking ability. When false, closed doors can obstruct the Fiend.").Value;
            s.BreakerBoxDestructionEnabled = Config.Bind("Abilities", "BreakerBoxDestructionEnabled", true, "Allow targeting and destroying the breaker box.").Value;
            s.LateGameBoostEnabled = Config.Bind("LateGame", "Enabled", false, "Allow the original late-game speed AND random-ability frequency multiplier.").Value;
            s.LateGameStartHour = Integer("LateGame", "StartHour", 15, 0, 24, "Original TimeOfDay.hour value that activates the boost. This is the mod's original game-time field.");
            s.LateGameMultiplier = Integer("LateGame", "Multiplier", 2, 1, 5, "Multiplier applied to the configured movement speeds and original random-ability frequency while boosted.");
            if (!enabled) { Logger.LogInfo("FiendGetConfigured is disabled."); return; }
            harmony = new Harmony(Id);
            try
            {
                PluginInfo original;
                if (!Chainloader.PluginInfos.TryGetValue("com.TheFiend", out original) || original.Metadata.Version.ToString() != "1.1.6")
                    throw new NotSupportedException("This release supports AuroraBytes-The_Fiend 1.1.6 exactly. No patches were applied.");
                Runtime.SettingsProvider = delegate { return s; };
                Runtime.Warning = delegate(string message) { Logger.LogWarning(message); };
                Runtime.Initialize(AccessTools.TypeByName("TheFiendAI"));
                Patches.Install(harmony, true);
                Logger.LogInfo("FiendGetConfigured " + VersionInfo.Version + " active for The Fiend 1.1.6: modern behavior, damage=" + s.Damage + ", chase=" + s.ChaseSpeed + ", sight=" + s.RequireLineOfSight + ". Same config required on all players; restart to change settings.");
            }
            catch (Exception error)
            {
                harmony.UnpatchSelf();
                Logger.LogError("FiendGetConfigured disabled because compatibility checks failed. The original Fiend remains active: " + error);
            }
        }

        private float Float(string section, string key, float value, float min, float max, string description)
        {
            float result = Config.Bind(section, key, value, new ConfigDescription(description + " Restart required.", new AcceptableValueRange<float>(min, max))).Value;
            return float.IsNaN(result) || float.IsInfinity(result) ? value : result;
        }

        private int Integer(string section, string key, int value, int min, int max, string description)
        {
            return Config.Bind(section, key, value, new ConfigDescription(description + " Restart required.", new AcceptableValueRange<int>(min, max))).Value;
        }

        private void OnDestroy()
        {
            if (harmony != null) harmony.UnpatchSelf();
        }
    }
}
