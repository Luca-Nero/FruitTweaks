using UnityEngine;

namespace FruitTweaks
{
    internal static class Config
    {
        // ── Controls ──────────────────────────────────────────────────────────
        [FruitLib.MenuCategory("Controls")] public static KeyCode ToggleGravity = KeyCode.F6;

        // ── World ─────────────────────────────────────────────────────────────
        [FruitLib.MenuCategory("World")] public static float TimeScale     = 0f;
        [FruitLib.MenuCategory("World")] public static float GravityX      = 0f;
        [FruitLib.MenuCategory("World")] public static float GravityY      = -9.81f;
        [FruitLib.MenuCategory("World")] public static float GravityZ      = 0f;

        // ── Ragdoll ───────────────────────────────────────────────────────────
        [FruitLib.MenuCategory("Ragdoll")] public static float AbsoluteWeight = 170f;
        [FruitLib.MenuCategory("Ragdoll")] public static float Head           = 10f;
        [FruitLib.MenuCategory("Ragdoll")] public static float Torso          = 35f; 
        [FruitLib.MenuCategory("Ragdoll")] public static float LeftArm        = 17.5f;
        [FruitLib.MenuCategory("Ragdoll")] public static float RightArm       = 17.5f;
        [FruitLib.MenuCategory("Ragdoll")] public static float LeftLeg        = 45f;
        [FruitLib.MenuCategory("Ragdoll")] public static float RightLeg       = 45f;

        // ── Pistol — Glock17 ──────────────────────────────────────────────────
        [FruitLib.MenuCategory("Pistol")] public static float LaunchSpeed  = 10000f;
        [FruitLib.MenuCategory("Pistol")] public static float FireDelay    = 0f;
        [FruitLib.MenuCategory("Pistol")] public static int   WoundDepth   = 1;

        // ── Wound — custom raycast ────────────────────────────────────────────
        [FruitLib.MenuCategory("Wound")] public static bool  CustomWoundEnabled        = true;
        [FruitLib.MenuCategory("Wound")] public static float WoundDepthScale           = 1f;
        [FruitLib.MenuCategory("Wound")] public static int   WoundConeStep             = 1;
        [FruitLib.MenuCategory("Wound")] public static float WoundConeRadius0          = 1f;
        [FruitLib.MenuCategory("Wound")] public static float WoundConeRadius1          = 3f;
        [FruitLib.MenuCategory("Wound")] public static int   WoundMaxSteps             = 64;
        [FruitLib.MenuCategory("Wound")] public static float WoundFalloffDivisor       = 2f;
        [FruitLib.MenuCategory("Wound")] public static float WoundRadiusFalloffDivisor = 1.5f;
        [FruitLib.MenuCategory("Wound")] public static float WoundMinDepth             = 0.15f;
        [FruitLib.MenuCategory("Wound")] public static int   WoundMaxLimbs             = 5;
        [FruitLib.MenuCategory("Wound")] public static float WoundRayDistance          = 100f;

        // ── Exit Wound VFX ────────────────────────────────────────────────────
        [FruitLib.MenuCategory("ExitVoxel")] public static bool  ExitVoxelEnabled  = true;
        [FruitLib.MenuCategory("ExitVoxel")] public static bool  ExitVoxelVisible  = true;
        [FruitLib.MenuCategory("ExitVoxel")] public static int   ExitVoxelMaxCount = 12;
        [FruitLib.MenuCategory("ExitVoxel")] public static float ExitVoxelSpeed    = 5f;
        [FruitLib.MenuCategory("ExitVoxel")] public static float ExitVoxelSpread   = 0.35f;
        [FruitLib.MenuCategory("ExitVoxel")] public static float ExitVoxelLifetime = 3f;

        // ── Blood Decals ───────────────────────────────────────────────────────
        [FruitLib.MenuCategory("BloodDecal")] public static bool  BloodDecalEnabled      = true;
        [FruitLib.MenuCategory("BloodDecal")] public static int   BloodDecalCountPerHit  = 4;
        [FruitLib.MenuCategory("BloodDecal")] public static float BloodDecalSize         = 0.15f;
        [FruitLib.MenuCategory("BloodDecal")] public static float BloodDecalLifetime     = 5f;
        [FruitLib.MenuCategory("BloodDecal")] public static int   BloodAtlasCols         = 3;
        [FruitLib.MenuCategory("BloodDecal")] public static int   BloodAtlasRows         = 3;

        // ── Optimizer ───────────────────────────────────────────────────────────

        [FruitLib.MenuCategory("Optimizer")] public static float TargetFPS = 75f;
        [FruitLib.MenuCategory("Optimizer")] public static float CullSpeed= 0.4f;

    }
}
