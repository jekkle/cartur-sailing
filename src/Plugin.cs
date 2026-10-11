using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace CarturSailing
{
    /// <summary>
    /// A Sailing skill: faster ships under your command, tougher ships you build, and a wider map
    /// reveal while you're aboard. Inspired by Smoothbrain's Sailing (deprecated on Thunderstore in
    /// 2026); written from scratch against the game's code, none of theirs.
    ///
    /// Valheim 1.0 has no sailing skill (Skills.SkillType stops at Ride), so one is registered with
    /// Jotunn's SkillManager. Read off assembly_valheim (1.0):
    ///  - Sail thrust is Ship.GetSailForce, applied on the ship's ZDO owner in CustomFixedUpdate.
    ///    Taking the helm (ShipControlls.RPC_RequestControl) only writes s_user - it does not move
    ///    ownership - so the helmsman may not be the machine running the physics, and their level is
    ///    read from what each player publishes on their own character's ZDO.
    ///  - A ship is a WearNTear piece; its current health is the ZDO's s_health defaulting to m_health.
    ///  - The map reveals around the player through Minimap.Explore(position, m_exploreRadius).
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.jekkle.valheim.cartursailing";
        public const string PluginName = "Cartur's Sailing";
        public const string PluginVersion = "1.0.0";
        internal const string Name = PluginName;
        internal const string Version = PluginVersion;
        internal const string ServerSection = "1 - Server";

        internal static BepInEx.Logging.ManualLogSource Log;
        internal static ConfigFile Cfg;

        internal static ConfigEntry<float> SpeedBonus;
        internal static ConfigEntry<float> HealthBonus;
        internal static ConfigEntry<float> ExploreBonus;
        internal static ConfigEntry<string> ShipSpeeds;
        internal static ConfigEntry<int> HalfSailLevel;
        internal static ConfigEntry<int> FullSailLevel;
        internal static ConfigEntry<float> NudgeSpeed;
        internal static ConfigEntry<bool> EnforceVersion;
        internal static ConfigEntry<KeyboardShortcut> NudgeKey;

        internal static Skills.SkillType Skill;

        private void Awake()
        {
            Log = Logger;
            Cfg = Config;

            SpeedBonus = Config.Bind(ServerSection, "SpeedBonus", 0.5f,
                new ConfigDescription("How much faster a ship sails at Sailing 100, from the helmsman's level. 0.5 = half again as fast, 0 = vanilla.",
                    new AcceptableValueRange<float>(0f, 3f)));
            HealthBonus = Config.Bind(ServerSection, "HealthBonus", 1f,
                new ConfigDescription("Extra hull health for ships built at Sailing 100, from the builder's level when it was built. 1 = double, 0 = vanilla.",
                    new AcceptableValueRange<float>(0f, 4f)));
            ExploreBonus = Config.Bind(ServerSection, "ExploreBonus", 1f,
                new ConfigDescription("How much further the map reveals while you're on a ship, at Sailing 100. 1 = twice the radius, 0 = vanilla.",
                    new AcceptableValueRange<float>(0f, 4f)));
            ShipSpeeds = Config.Bind(ServerSection, "ShipSpeeds", "",
                "Speed per ship, by prefab name, applied on top of everything else - e.g. \"Raft=1.2, Karve=1, VikingShip=0.9\". Works for ships from other mods too. Empty = all 1.");
            HalfSailLevel = Config.Bind(ServerSection, "HalfSailLevel", 0,
                new ConfigDescription("Sailing level needed to use half sail. Below it, half sail catches no wind and the ship coasts - row instead. 0 = no lock.",
                    new AcceptableValueRange<int>(0, 100)));
            FullSailLevel = Config.Bind(ServerSection, "FullSailLevel", 0,
                new ConfigDescription("Sailing level needed to use full sail. Below it, full sail works as half sail. 0 = no lock.",
                    new AcceptableValueRange<int>(0, 100)));
            NudgeSpeed = Config.Bind(ServerSection, "NudgeSpeed", 2f,
                new ConfigDescription("How hard the ladder nudge pushes the ship, in metres per second.", new AcceptableValueRange<float>(0.5f, 6f)));
            EnforceVersion = Config.Bind(ServerSection, "EnforceVersion", true,
                "On a server: refuse clients running a different version of this mod.");
            NudgeKey = Config.Bind("2 - Keys", "NudgeKey", new KeyboardShortcut(KeyCode.LeftShift),
                "Hold while using a ship's ladder to push the ship instead of climbing aboard - for getting off a beach.");

            try
            {
                string iconPath = Path.Combine(Path.GetDirectoryName(Info.Location) ?? "", "sailing_icon.png");
                Sprite icon = File.Exists(iconPath) ? AssetUtils.LoadSpriteFromFile(iconPath) : null;
                Skill = SkillManager.Instance.AddSkill(new Jotunn.Configs.SkillConfig
                {
                    Identifier = "cartur.sailing",
                    Name = "Sailing",
                    Description = "Steering a ship under sail. Faster ships, tougher hulls you build, and a wider view of the sea.",
                    IncreaseStep = 1f,
                    Icon = icon,
                });

                Harmony.CreateAndPatchAll(typeof(Plugin).Assembly, PluginGuid);
                Log.LogInfo($"{PluginName} {PluginVersion} loaded - Sailing skill {(int)Skill}, speed, hull health, exploration and the ladder nudge.");
            }
            catch (Exception e)
            {
                Log.LogWarning($"{PluginName} failed to start, game continues without it: {e}");
            }
        }

        private float _publishAt;
        private float _raiseAt;

        private void Update()
        {
            if (Time.time >= _publishAt)
            {
                _publishAt = Time.time + 2f;
                Sailing.Publish();
            }
            if (Time.time >= _raiseAt)
            {
                _raiseAt = Time.time + 5f;
                Sailing.Practice();
            }
        }

        internal static IEnumerable<ConfigEntryBase> ServerOwnedEntries(HashSet<string> sections) =>
            Cfg.Keys.Where(k => sections.Contains(k.Section)).Select(k => Cfg[k]);

        internal static ConfigEntryBase FindEntry(string section, string key) =>
            Cfg.Keys.Where(k => k.Section == section && k.Key == key).Select(k => Cfg[k]).FirstOrDefault();

        internal static void WithoutSaving(Action change)
        {
            bool was = Cfg.SaveOnConfigSet;
            Cfg.SaveOnConfigSet = false;
            try { change(); }
            finally { Cfg.SaveOnConfigSet = was; }
        }
    }

    internal static class Sailing
    {
        internal static readonly int LevelKey = "cartur_sailing_level".GetStableHashCode();
        internal static readonly int BuilderKey = "cartur_sailing_builder".GetStableHashCode();

        internal static int LocalLevel(Player p) =>
            p != null && Plugin.Skill != Skills.SkillType.None ? Mathf.RoundToInt(p.GetSkillLevel(Plugin.Skill)) : 0;

        /// Every player's Sailing level on their own character's ZDO, so a ship's owner can read its
        /// helmsman's level.
        internal static void Publish()
        {
            Player p = Player.m_localPlayer;
            ZNetView nview = p != null ? p.GetComponent<ZNetView>() : null;
            if (nview == null || !nview.IsValid() || !nview.IsOwner())
                return;
            int level = LocalLevel(p);
            if (nview.GetZDO().GetInt(LevelKey, -1) != level)
                nview.GetZDO().Set(LevelKey, level);
        }

        /// The skill rises while you hold the helm of a ship under half or full sail and it is moving.
        internal static void Practice()
        {
            Player p = Player.m_localPlayer;
            Ship ship = Ship.GetLocalShip();
            if (p == null || ship == null || ship.m_shipControlls == null || ship.m_shipControlls.GetUser() != p.GetPlayerID())
                return;
            Ship.Speed speed = ship.GetSpeedSetting();
            if ((speed == Ship.Speed.Half || speed == Ship.Speed.Full) && ship.GetSpeed() > 1f)
                p.RaiseSkill(Plugin.Skill, speed == Ship.Speed.Full ? 1f : 0.5f);
        }

        internal static int LevelOf(long playerID)
        {
            Player p = Player.GetPlayer(playerID);
            if (p == null)
                return 0;
            if (p == Player.m_localPlayer)
                return LocalLevel(p);
            ZNetView nview = p.GetComponent<ZNetView>();
            return nview != null && nview.IsValid() ? nview.GetZDO().GetInt(LevelKey, 0) : 0;
        }

        private static string s_speedsRaw;
        private static readonly Dictionary<string, float> Speeds = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        internal static float ShipSpeed(Ship ship)
        {
            string raw = Plugin.ShipSpeeds.Value ?? "";
            if (raw != s_speedsRaw)
            {
                s_speedsRaw = raw;
                Speeds.Clear();
                foreach (string part in raw.Split(','))
                {
                    string[] kv = part.Split('=');
                    if (kv.Length == 2 && float.TryParse(kv[1].Trim(), System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float v))
                        Speeds[kv[0].Trim()] = v;
                }
            }
            return Speeds.TryGetValue(Utils.GetPrefabName(ship.gameObject), out float s) ? s : 1f;
        }
    }

    // ---- speed ---------------------------------------------------------------------------------

    // GetSailForce returns the smoothed thrust that CustomFixedUpdate pushes the hull with; scaling
    // what it returns (not its stored m_sailForce) leaves the smoothing itself untouched.
    [HarmonyPatch(typeof(Ship), "GetSailForce")]
    internal static class Speed
    {
        private static void Postfix(Ship __instance, ref Vector3 __result)
        {
            if (__instance.m_shipControlls == null)
                return;
            long helmsman = __instance.m_shipControlls.GetUser();
            int level = helmsman != 0 ? Sailing.LevelOf(helmsman) : 0;
            float factor = (1f + level / 100f * Plugin.SpeedBonus.Value) * Sailing.ShipSpeed(__instance);

            // Speed locks: full sail below FullSailLevel works as half (vanilla sailSize 1 -> 0.5),
            // half sail below HalfSailLevel gives no sail force (rowing force is only applied on
            // Slow and Back in Ship.CustomFixedUpdate, so the ship coasts until the helmsman rows).
            Ship.Speed setting = __instance.GetSpeedSetting();
            if (setting == Ship.Speed.Full && level < Plugin.FullSailLevel.Value)
                factor *= 0.5f;
            else if (setting == Ship.Speed.Half && level < Plugin.HalfSailLevel.Value)
                factor = 0f;

            __result *= factor;
        }
    }

    // ---- hull health ---------------------------------------------------------------------------

    // The builder's level is recorded when the ship is placed - WearNTear.Awake runs inside the
    // Object.Instantiate in Player.PlacePiece - and the hull's m_health raised from it on every load.
    // A new ship's ZDO has no s_health yet, so it starts at the raised maximum; an old one keeps its
    // damage and can be repaired up to the new maximum.
    [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
    internal static class PlacingLevel
    {
        internal static int Current = -1;
        private static void Prefix(Player __instance) { if (__instance == Player.m_localPlayer) Current = Sailing.LocalLevel(__instance); }
        private static void Finalizer() => Current = -1;
    }

    [HarmonyPatch(typeof(WearNTear), "Awake")]
    internal static class Hull
    {
        private static void Postfix(WearNTear __instance, ZNetView ___m_nview)
        {
            if (___m_nview == null || ___m_nview.GetZDO() == null || __instance.GetComponent<Ship>() == null)
                return;
            ZDO zdo = ___m_nview.GetZDO();
            if (PlacingLevel.Current >= 0 && ___m_nview.IsOwner() && zdo.GetInt(Sailing.BuilderKey, -1) < 0)
                zdo.Set(Sailing.BuilderKey, PlacingLevel.Current);
            int level = zdo.GetInt(Sailing.BuilderKey, 0);
            if (level > 0)
                __instance.m_health *= 1f + level / 100f * Plugin.HealthBonus.Value;
        }
    }

    // ---- exploration -----------------------------------------------------------------------------

    [HarmonyPatch(typeof(Minimap), "Explore", new[] { typeof(Vector3), typeof(float) })]
    internal static class Explore
    {
        private static void Prefix(ref float radius)
        {
            Player p = Player.m_localPlayer;
            if (p == null || Ship.GetLocalShip() == null)
                return;
            radius *= 1f + Sailing.LocalLevel(p) / 100f * Plugin.ExploreBonus.Value;
        }
    }

    // ---- ladder nudge ------------------------------------------------------------------------------

    // Holding NudgeKey while using a ship's ladder pushes the ship away from you instead of climbing
    // aboard. Prefix returning false skips Ladder.Interact on purpose - the original would move the
    // player onto the deck, which is the opposite of what the nudge is for. The push is a velocity
    // change on the hull's rigidbody, so it needs the hull's owner: taken only when nobody holds
    // the helm, so a nudge never wrests a ship from its helmsman.
    [HarmonyPatch(typeof(Ladder), nameof(Ladder.Interact))]
    internal static class Nudge
    {
        private static readonly FieldInfo Body = AccessTools.Field(typeof(Ship), "m_body");

        private static bool Prefix(Ladder __instance, Humanoid character, bool hold)
        {
            if (hold || character != Player.m_localPlayer || !Plugin.NudgeKey.Value.IsPressed())
                return true;
            Ship ship = __instance.GetComponentInParent<Ship>();
            ZNetView nview = ship != null ? ship.GetComponent<ZNetView>() : null;
            if (nview == null || !nview.IsValid() || ship.m_shipControlls == null || ship.m_shipControlls.HaveValidUser())
                return true;

            nview.ClaimOwnership();
            if (Body?.GetValue(ship) is Rigidbody body && body != null)
            {
                Vector3 away = ship.transform.position - character.transform.position;
                away.y = 0f;
                body.AddForce(away.normalized * Plugin.NudgeSpeed.Value, ForceMode.VelocityChange);
            }
            return false;
        }
    }
}
