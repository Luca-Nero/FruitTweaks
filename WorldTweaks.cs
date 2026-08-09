using HarmonyLib;
using FruitLib;
using UnityEngine;

namespace FruitTweaks
{
    // ── World tweaks ──────────────────────────────────────────────────────────
    internal static class WorldTweaks
    {
        private static readonly Vector3 NormalGravity = new Vector3(0f, -9.81f, 0f);
        private static bool _customGravity;

        public static void Update()
        {
            if (!FruitMenu.IsInputSuppressed && Input.GetKeyDown(Config.ToggleGravity))
            {
                _customGravity = !_customGravity;
                Physics.gravity = _customGravity
                    ? new Vector3(Config.GravityX, Config.GravityY, Config.GravityZ)
                    : NormalGravity;
            }
        }
    }

    // ── Time scale ────────────────────────────────────────────────────────────
    [HarmonyPatch(typeof(UnityEngine.Time), nameof(UnityEngine.Time.timeScale), MethodType.Setter)]
    internal static class Patch_TimeScale
    {
        private const float PauseThreshold = 0.99f;
        static bool Prefix(ref float value)
        {
            if (value < PauseThreshold && !FruitMenu.IsOpen)
                value = Config.TimeScale / 100f;
            return true;
        }
    }
}
