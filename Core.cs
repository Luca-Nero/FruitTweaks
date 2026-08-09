using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using FruitLib;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(FruitTweaks.Core), "FruitTweaks", "2.0.0", "Luca_Nero")]
[assembly: MelonGame]

namespace FruitTweaks
{
    public class Core : MelonMod
    {
        private bool _wasMenuOpen;

        public override void OnInitializeMelon()
        {
            HarmonyInstance.PatchAll();
            if (!FruitVersion.Require("FruitTweaks", 2, 0, 0)) return;
            Init();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Init()
        {
            FruitMenu.Register("FruitTweaks", ConfigLoader.IniPath, typeof(Config));
            FruitMenu.OnConfigChanged += RagdollTweaks.SyncGroups;
            ConfigLoader.Load();
            RagdollTweaks.InitFromConfig();
            FruitLib.FruitPerfMon.RegisterCounter("Chunks  (active)", () => WoundEjectVFX.ActiveChunks);
            FruitLib.FruitPerfMon.RegisterCounter("Decals  (active)", () => WoundEjectVFX.ActiveDecals);
            FruitUpdateCheck.Register("FruitTweaks", "2.0.0", "Luca-Nero", "FruitTweaks");
            LoggerInstance.Msg("FruitTweaks loaded.");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            BloodDecalAtlas.Init();
            RagdollTweaks.OnSceneLoaded();
        }

        public override void OnUpdate()
        {
            WorldTweaks.Update();
            RagdollTweaks.Update();
            WoundEjectVFX.Update();
            Patch_PistolBullet_Init.PurgeStale();
            bool isOpen = FruitMenu.IsOpen;
            if (_wasMenuOpen && !isOpen)
            {
                PistolConfig.ApplyToLiveInstances();
                RagdollTweaks.ApplyToLiveInstances();
            }
            _wasMenuOpen = isOpen;
        }
    }

    // ── Config loader ─────────────────────────────────────────────────────────
    internal static class ConfigLoader
    {
        public static string IniPath => Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "FruitTweaksConfig.ini");

        public static void Load()
        {
            try
            {
                if (!File.Exists(IniPath)) { Write(); MelonLogger.Msg("[FruitTweaks] Wrote default config."); return; }
                foreach (var line in File.ReadAllLines(IniPath))
                {
                    string t = line.Trim();
                    if (string.IsNullOrEmpty(t) || t.StartsWith("#")) continue;
                    int eq = t.IndexOf('=');
                    if (eq < 0) continue;
                    Set(t.Substring(0, eq).Trim(), t.Substring(eq + 1).Trim());
                }
                Write(); // re-write: adds new fields, removes stale ones
                MelonLogger.Msg("[FruitTweaks] Config loaded.");
            }
            catch (Exception e) { MelonLogger.Warning($"[FruitTweaks] Config load failed: {e.Message}"); }
        }

        private static void Set(string key, string raw)
        {
            var f = typeof(Config).GetField(key, BindingFlags.Public | BindingFlags.Static);
            if (f == null) return;
            try
            {
                if      (f.FieldType == typeof(float))   f.SetValue(null, float.Parse(raw, System.Globalization.CultureInfo.InvariantCulture));
                else if (f.FieldType == typeof(int))     f.SetValue(null, int.Parse(raw));
                else if (f.FieldType == typeof(bool))    f.SetValue(null, raw.ToLowerInvariant() == "true");
                else if (f.FieldType == typeof(string))  f.SetValue(null, raw);
                else if (f.FieldType == typeof(KeyCode)) f.SetValue(null, Enum.Parse(typeof(KeyCode), raw, true));
            }
            catch { }
        }

        public static void Write()
        {
            var sb = new System.Text.StringBuilder();
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            string F(float v) => v.ToString("0.####", ci);
            string B(bool  v) => v ? "true" : "false";

            sb.AppendLine("# ╔══════════════════════════════════════════════════════════════╗");
            sb.AppendLine("# ║       FruitTweaks v1.4.2    —    Configuration               ║");
            sb.AppendLine("# ╚══════════════════════════════════════════════════════════════╝");
            sb.AppendLine("# Changes take effect immediately via the in-game mod menu.");
            sb.AppendLine();

            sb.AppendLine("# ── Controls ───────────────────────────────────────────────────");
            sb.AppendLine("# ToggleGravity : key to switch between normal and custom gravity");
            sb.AppendLine($"ToggleGravity = {Config.ToggleGravity}");
            sb.AppendLine();

            sb.AppendLine("# ── World ───────────────────────────────────────────────────────");
            sb.AppendLine("# TimeScale : game speed as a percentage (0 = frozen, 100 = normal)");
            sb.AppendLine($"TimeScale = {F(Config.TimeScale)}");
            sb.AppendLine("# Gravity : applied when ToggleGravity is pressed. Default is normal Unity gravity.");
            sb.AppendLine($"GravityX = {F(Config.GravityX)}");
            sb.AppendLine($"GravityY = {F(Config.GravityY)}");
            sb.AppendLine($"GravityZ = {F(Config.GravityZ)}");
            sb.AppendLine();

            sb.AppendLine("# ── Ragdoll ─────────────────────────────────────────────────────");
            sb.AppendLine("# AbsoluteWeight : total body mass (default 170). Editing it rescales all");
            sb.AppendLine("#   limb groups proportionally. Editing a group updates AbsoluteWeight instead.");
            sb.AppendLine($"AbsoluteWeight = {F(Config.AbsoluteWeight)}");
            sb.AppendLine($"Head           = {F(Config.Head)}");
            sb.AppendLine($"Torso          = {F(Config.Torso)}");
            sb.AppendLine($"LeftArm        = {F(Config.LeftArm)}");
            sb.AppendLine($"RightArm       = {F(Config.RightArm)}");
            sb.AppendLine($"LeftLeg        = {F(Config.LeftLeg)}");
            sb.AppendLine($"RightLeg       = {F(Config.RightLeg)}");
            sb.AppendLine();

            sb.AppendLine("# ── Pistol (Glock17) ────────────────────────────────────────────");
            sb.AppendLine("# Bullet muzzle velocity");
            sb.AppendLine($"LaunchSpeed = {F(Config.LaunchSpeed)}");
            sb.AppendLine("# Min seconds between shots");
            sb.AppendLine($"FireDelay = {F(Config.FireDelay)}");
            sb.AppendLine("# Native wound depth (tied to bullet force)");
            sb.AppendLine($"WoundDepth = {Config.WoundDepth}");
            sb.AppendLine();

            sb.AppendLine("# ── Wound (custom raycast) ──────────────────────────────────────");
            sb.AppendLine("# Enable raycast wound: traces from bullet origin, wounds every limb in path");
            sb.AppendLine($"CustomWoundEnabled = {B(Config.CustomWoundEnabled)}");
            sb.AppendLine("# Base signal multiplier for first limb hit. 1.0 = game-equivalent depth");
            sb.AppendLine($"WoundDepthScale = {F(Config.WoundDepthScale)}");
            sb.AppendLine("# Voxel slice stride per sample (1 = every slice, 2 = every other — trade detail for perf)");
            sb.AppendLine($"WoundConeStep = {Config.WoundConeStep}");
            sb.AppendLine("# Entry wound cone radius (voxels, float)");
            sb.AppendLine($"WoundConeRadius0 = {F(Config.WoundConeRadius0)}");
            sb.AppendLine("# Exit wound cone radius — set larger than r0 for exit-wound-bigger shape");
            sb.AppendLine($"WoundConeRadius1 = {F(Config.WoundConeRadius1)}");
            sb.AppendLine("# Safety cap: maximum march steps per limb");
            sb.AppendLine($"WoundMaxSteps = {Config.WoundMaxSteps}");
            sb.AppendLine("# Divide signal by this for each successive body (e.g. 2 = half depth per body)");
            sb.AppendLine($"WoundFalloffDivisor = {F(Config.WoundFalloffDivisor)}");
            sb.AppendLine("# Divide cone radii by this for each successive body");
            sb.AppendLine($"WoundRadiusFalloffDivisor = {F(Config.WoundRadiusFalloffDivisor)}");
            sb.AppendLine("# Stop wounding when depthScale drops below this (gives WoundFalloffDivisor its teeth)");
            sb.AppendLine("# With falloff=2 and minDepth=0.15: wounds ~3 bodies. With falloff=5: wounds ~2.");
            sb.AppendLine($"WoundMinDepth = {F(Config.WoundMinDepth)}");
            sb.AppendLine("# Hard cap on bodies wounded per shot (safety net above the min-depth cutoff)");
            sb.AppendLine($"WoundMaxLimbs = {Config.WoundMaxLimbs}");
            sb.AppendLine("# Max raycast distance (world units)");
            sb.AppendLine($"WoundRayDistance = {F(Config.WoundRayDistance)}");
            sb.AppendLine();

            sb.AppendLine("# ── Exit Wound VFX ──────────────────────────────────────────────");
            sb.AppendLine("# Spawn tissue chunks at the exit wound");
            sb.AppendLine($"ExitVoxelEnabled = {B(Config.ExitVoxelEnabled)}");
            sb.AppendLine("# Render chunks (set false to test decals in isolation)");
            sb.AppendLine($"ExitVoxelVisible = {B(Config.ExitVoxelVisible)}");
            sb.AppendLine("# Max chunks ejected per limb hit");
            sb.AppendLine($"ExitVoxelMaxCount = {Config.ExitVoxelMaxCount}");
            sb.AppendLine("# Ejection speed (m/s)");
            sb.AppendLine($"ExitVoxelSpeed = {F(Config.ExitVoxelSpeed)}");
            sb.AppendLine("# Radial spread of ejection cone");
            sb.AppendLine($"ExitVoxelSpread = {F(Config.ExitVoxelSpread)}");
            sb.AppendLine("# Seconds before chunks fade and are destroyed");
            sb.AppendLine($"ExitVoxelLifetime = {F(Config.ExitVoxelLifetime)}");
            sb.AppendLine();

            sb.AppendLine("# ── Blood Decals ─────────────────────────────────────────────────");
            sb.AppendLine($"BloodDecalEnabled = {B(Config.BloodDecalEnabled)}");
            sb.AppendLine("# Number of scatter points attempted per chunk impact");
            sb.AppendLine($"BloodDecalCountPerHit = {Config.BloodDecalCountPerHit}");
            sb.AppendLine("# Scatter circle radius, and individual tile size = BloodDecalSize * 0.35");
            sb.AppendLine($"BloodDecalSize = {F(Config.BloodDecalSize)}");
            sb.AppendLine("# Seconds before decals fade");
            sb.AppendLine($"BloodDecalLifetime = {F(Config.BloodDecalLifetime)}");
            sb.AppendLine("# Atlas grid layout (adjust to match Pixelblood tile count)");
            sb.AppendLine($"BloodAtlasCols = {Config.BloodAtlasCols}");
            sb.AppendLine($"BloodAtlasRows = {Config.BloodAtlasRows}");
            sb.AppendLine();


            sb.AppendLine("# ── Optimizer ───────────────────────────────────────────────────");
            sb.AppendLine("# FPS the optimizer will try to stay at");
            sb.AppendLine($"TargetFPS = {F(Config.TargetFPS)}");
            sb.AppendLine("# How many chunks/decals are to be deleted per second per frame delta to Target");
            sb.AppendLine($"CullSpeed = {F(Config.CullSpeed)}");

            File.WriteAllText(IniPath, sb.ToString());
        }
    }
}
