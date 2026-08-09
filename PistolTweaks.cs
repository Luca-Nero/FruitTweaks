using HarmonyLib;
using Il2Cpp;
using Il2CppEffectors;
using Il2CppEffectors.ReceiveMethods.Index;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppLVA.Organs.EffectorsPerception.Collectors;
using Il2CppSpawnables.Bullets;
using Il2CppSpawnables.Weapons;
using Il2CppVoxelMeshGeneration;
using MelonLoader;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FruitTweaks
{
    // ── Live config apply ─────────────────────────────────────────────────────
    internal static class PistolConfig
    {
        public static void ApplyToLiveInstances()
        {
            foreach (var glock in UnityEngine.Object.FindObjectsOfType<Glock17>(true))
            {
                glock.m_launchSpeed = Config.LaunchSpeed;
                glock.raq           = Config.FireDelay;
            }
        }
    }

    // ── Wound system ──────────────────────────────────────────────────────────
    internal static class BulletWound
    {
        internal static bool IsLimb(GameObject obj) =>
            obj.GetComponentInParent(Il2CppType.Of<LimbEffectorReceiver>()) != null;
        public static void ApplyRaycast(Vector3 spawnPos, Vector3 dir)
        {
            try
            {
                var hits = Physics.RaycastAll(spawnPos, dir, Config.WoundRayDistance,
                    ~0, QueryTriggerInteraction.Ignore);
                if (hits == null || hits.Length == 0) return;

                Array.Sort<RaycastHit>(hits, (a, b) => a.distance.CompareTo(b.distance));

                float depthScale = Config.WoundDepthScale;
                float r0Scale    = 1f;
                int   limbs      = 0;
                var woundedRbs = new HashSet<IntPtr>();

                foreach (var hit in hits)
                {
                    if (limbs >= Config.WoundMaxLimbs) break;
                    if (depthScale < Config.WoundMinDepth) break;
                    if (hit.collider == null) continue;
                    if (!IsLimb(hit.collider.gameObject)) continue;

                    var rb = hit.collider.GetComponentInParent<Rigidbody>();
                    if (rb == null) continue;
                    if (!woundedRbs.Add(rb.Pointer)) continue;

                    ApplyCone(hit.point, dir, hit.collider.gameObject, depthScale, r0Scale);
                    limbs++;

                    depthScale /= Config.WoundFalloffDivisor;
                    r0Scale    /= Config.WoundRadiusFalloffDivisor;
                }
            }
            catch (Exception e) { MelonLogger.Warning($"[FruitTweaks] Wound error: {e.Message}"); }
        }
        private static void ApplyCone(Vector3 entryPos, Vector3 dir, GameObject hitObject,
                                      float depthScale, float radiusScale)
        {
            var rb = hitObject.GetComponentInParent<Rigidbody>();
            if (rb == null) return;

            var birComp = rb.GetComponent(Il2CppType.Of<bir>());
            if (birComp == null) return;

            var biwReceiver = birComp.TryCast<biw>();
            if (biwReceiver == null) return;

            var lerComp = hitObject.GetComponentInParent(Il2CppType.Of<LimbEffectorReceiver>());
            if (lerComp == null) return;
            var receiver = lerComp.TryCast<LimbEffectorReceiver>();
            if (receiver == null) return;

            var voxelMesh = receiver.wtb;
            if (voxelMesh == null) return;

            float r0 = Config.WoundConeRadius0 * radiusScale;
            float r1 = Config.WoundConeRadius1 * radiusScale;
            int stride = Mathf.Max(1, Config.WoundConeStep);
            int n = 0;
            for (int s = 0; s < Config.WoundMaxSteps; s++)
            {
                var chunk = ct.diz(voxelMesh, entryPos, dir, s * stride);
                if (chunk == null) break; // stepped out of the mesh
                n++;
            }

            if (n == 0) return;

            int  totalVoxels = 0;
            var  sets        = new List<(Il2CppStructArray<Vector3Int> voxels, float signal)>();
            cu   lastChunk   = null;
            var  exitColors  = new List<Color>();

            for (int s = 0; s < n; s++)
            {
                var chunk = ct.diz(voxelMesh, entryPos, dir, s * stride);
                if (chunk == null) break; 

                float t      = n > 1 ? s / (float)(n - 1) : 0f;
                float radius = Mathf.Lerp(r0, r1, t);
                int   ri     = Mathf.Max(0, Mathf.RoundToInt(radius));

                var voxels = new dh(chunk.pla, ri).dki();
                if (voxels == null || voxels.Length == 0) continue;

                float signal = -10000f * depthScale;
                sets.Add((voxels, signal));
                totalVoxels += voxels.Length;

                lastChunk = chunk;

                if (s == n - 1 && Config.ExitVoxelEnabled)
                {
                    var voxelData = voxelMesh.pjw;
                    foreach (var v in voxels)
                    {
                        if (exitColors.Count >= Config.ExitVoxelMaxCount) break;
                        try
                        {
                            var idx = fp.ecz(v);
                            var vox = voxelData[idx];
                            if (!vox.enabled) continue;
                            exitColors.Add(WoundEjectVFX.AtlasToColor(vox.color));
                        }
                        catch { exitColors.Add(WoundEjectVFX.ColMuscle); }
                    }
                }
            }

            if (totalVoxels == 0) return;

            var builder = new bjd(totalVoxels, false);
            foreach (var (voxels, signal) in sets)
                foreach (var v in voxels)
                    builder.jbq(new IndexEffectorSignal(fp.ecz(v), signal, InfluenceProcessType.Sum));

            biwReceiver.cyn(new bjb<bit>(builder));
            builder.Dispose();

            if (lastChunk != null && exitColors.Count > 0)
                WoundEjectVFX.Spawn(lastChunk.plb, dir, exitColors, rb);
        }
    }

    // ── Glock17 init ──────────────────────────────────────────────────────────
    [HarmonyPatch(typeof(Glock17), nameof(Glock17.cxh))]
    internal static class Patch_Glock17_Init
    {
        static void Postfix(Glock17 __instance)
        {
            __instance.m_launchSpeed = Config.LaunchSpeed;
            __instance.raq           = Config.FireDelay;
        }
    }

    // ── PistolBullet init — record spawn position ─────────────────────────────
    [HarmonyPatch(typeof(PistolBullet), nameof(PistolBullet.cxh))]
    internal static class Patch_PistolBullet_Init
    {
        public static readonly Dictionary<int, (Vector3 pos, float time)> SpawnPositions =
            new Dictionary<int, (Vector3 pos, float time)>();

        private const float StaleAfter = 10f;
        private const float PurgeInterval = 5f;
        private static float s_lastPurge;

        static void Postfix(PistolBullet __instance)
        {
            SpawnPositions[__instance.GetInstanceID()] = (__instance.transform.position, Time.time);
            __instance.rdt = Config.WoundDepth;
        }

        public static void PurgeStale()
        {
            if (Time.time - s_lastPurge < PurgeInterval) return;
            s_lastPurge = Time.time;

            List<int> stale = null;
            foreach (var kvp in SpawnPositions)
                if (Time.time - kvp.Value.time > StaleAfter)
                    (stale ??= new List<int>()).Add(kvp.Key);

            if (stale != null)
                foreach (var id in stale) SpawnPositions.Remove(id);
        }
    }

    // ── PistolBullet collision ────────────────────────────────────────────────
    [HarmonyPatch(typeof(PistolBullet), nameof(PistolBullet.OnCollisionEnter))]
    internal static class Patch_PistolBullet_Hit
    {
        static void Postfix(PistolBullet __instance)
        {
            if (!Config.CustomWoundEnabled) return;

            int id = __instance.GetInstanceID();
            if (!Patch_PistolBullet_Init.SpawnPositions.TryGetValue(id, out var spawn))
                return;

            Patch_PistolBullet_Init.SpawnPositions.Remove(id);

            Vector3 spawnPos = spawn.pos;
            var rb = __instance.m_rigidbody;
            Vector3 dir = rb != null && rb.linearVelocity.sqrMagnitude > 0.001f
                ? rb.linearVelocity.normalized
                : (__instance.transform.position - spawnPos).normalized;

            BulletWound.ApplyRaycast(spawnPos, dir);
        }
    }


}
