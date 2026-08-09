using Il2CppEffectors;
using MelonLoader;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FruitTweaks
{
    internal static class RagdollTweaks
    {
        // ── Default game masses (provided by developer) ───────────────────────
        private const float DefaultHead  = 10f;
        private const float DefaultTorso = 35f;   
        private const float DefaultArm   = 17.5f;
        private const float DefaultLeg   = 45f;
        internal const float DefaultTotal = DefaultHead + DefaultTorso + DefaultArm * 2 + DefaultLeg * 2; 
        // ── Intra-group distribution ratios ───────────────────────────────────
        private const float R_Spine  = 15f / 35f;
        private const float R_Pelvis = 20f / 35f;
        private const float R_Arm     = 9f    / 17.5f;
        private const float R_Forearm = 6f    / 17.5f;
        private const float R_Hand    = 2.5f  / 17.5f;
        private const float R_Leg  = 20f / 45f;
        private const float R_Shin = 10f / 45f;
        private const float R_Foot = 15f / 45f;

        // ── State ─────────────────────────────────────────────────────────────
        private static float s_prevAbsoluteWeight;
        private static readonly HashSet<int> s_configured = new HashSet<int>();
        private static float s_pollTimer;
        private const float PollInterval = 0.5f;

        public static void InitFromConfig() => s_prevAbsoluteWeight = Config.AbsoluteWeight;
        public static void OnSceneLoaded() => s_configured.Clear();

        // ── Per-frame ─────────────────────────────────────────────────────────
        public static void Update()
        {
            s_pollTimer -= Time.unscaledDeltaTime;
            if (s_pollTimer > 0f) return;
            s_pollTimer = PollInterval;
            ApplyMasses(onlyNew: true);
        }

        // ── On menu close ─────────────────────────────────────────────────────
        public static void ApplyToLiveInstances()
        {
            SyncGroups();
            ApplyMasses(onlyNew: false);
            ConfigLoader.Write();
        }

        // ── Sync AbsoluteWeight ↔ group fields ───────────────────────────────
        internal static void SyncGroups()
        {
            float cur = Config.AbsoluteWeight;
            if (Math.Abs(cur - s_prevAbsoluteWeight) > 0.0001f)
            {
                float scale = cur / DefaultTotal;
                Config.Head     = DefaultHead  * scale;
                Config.Torso    = DefaultTorso * scale;
                Config.LeftArm  = DefaultArm   * scale;
                Config.RightArm = DefaultArm   * scale;
                Config.LeftLeg  = DefaultLeg   * scale;
                Config.RightLeg = DefaultLeg   * scale;
            }
            else
            {
                Config.AbsoluteWeight = Config.Head + Config.Torso
                                      + Config.LeftArm  + Config.RightArm
                                      + Config.LeftLeg  + Config.RightLeg;
            }
            s_prevAbsoluteWeight = Config.AbsoluteWeight;
        }

        // ── Apply config masses to live Rigidbodies ───────────────────────────
        private static void ApplyMasses(bool onlyNew)
        {
            try
            {
                foreach (var ler in UnityEngine.Object.FindObjectsOfType<LimbEffectorReceiver>(true))
                {
                    if (ler == null) continue;
                    var rb = ler.GetComponent<Rigidbody>();
                    if (rb == null) continue;

                    int id = rb.GetInstanceID();
                    if (onlyNew && s_configured.Contains(id)) continue;

                    float target = GetTargetMass(ler.transform.parent?.name);
                    if (target < 0f) continue;

                    rb.mass = Mathf.Max(0.001f, target);
                    s_configured.Add(id);
                }
            }
            catch (Exception e) { MelonLogger.Warning($"[FruitTweaks] Ragdoll mass error: {e.Message}"); }
        }
        private static float GetTargetMass(string prefabName)
        {
            switch (prefabName)
            {
                case "HeadPrefab(Clone)":         return Config.Head;

                case "Spine_1Prefab(Clone)":      return Config.Torso    * R_Spine;
                case "PelvisPrefab(Clone)":       return Config.Torso    * R_Pelvis;

                case "LeftArmPrefab(Clone)":      return Config.LeftArm  * R_Arm;
                case "LeftForearmPrefab(Clone)":  return Config.LeftArm  * R_Forearm;
                case "LeftHandPrefab(Clone)":     return Config.LeftArm  * R_Hand;

                case "RightArmPrefab(Clone)":     return Config.RightArm * R_Arm;
                case "RightForearmPrefab(Clone)": return Config.RightArm * R_Forearm;
                case "RightHandPrefab(Clone)":    return Config.RightArm * R_Hand;

                case "LeftLegPrefab(Clone)":      return Config.LeftLeg  * R_Leg;
                case "LeftKneePrefab(Clone)":     return Config.LeftLeg  * R_Shin;
                case "LeftFootPrefab(Clone)":     return Config.LeftLeg  * R_Foot;

                case "RightLegPrefab(Clone)":     return Config.RightLeg * R_Leg;
                case "RightKneePrefab(Clone)":    return Config.RightLeg * R_Shin;
                case "RightFootPrefab(Clone)":    return Config.RightLeg * R_Foot;

                default: return -1f;
            }
        }
    }
}
