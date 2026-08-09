# FruitTweaks

![Version](https://img.shields.io/github/v/release/Luca-Nero/FruitTweaks?style=flat-square)
![Game Version](https://img.shields.io/badge/Game-v0.1%2B-blue?style=flat-square)
[![Ko-fi](https://img.shields.io/badge/Ko--fi-Donate-ff5e5b?style=flat-square&logo=ko-fi&logoColor=white)](https://ko-fi.com/Luca_Nero)

A grab-bag of gameplay knobs for FRUKT - gravity, time scale, ragdoll mass, and pistol ballistics - plus a from-scratch wound and gore pipeline for the Glock17 with raycast limb wounding, ejected tissue chunks, and scatter blood decals. Everything is editable live through FruitLib's in-game menu, no restart needed.

---

## Features

- **World:** Press **F6** to swap between normal Unity gravity and your own configurable `(X, Y, Z)` vector.
    - **Time Scale:** Game speed as a percentage of normal (0 = frozen). Doesn't fight the game's own pause - opening the menu still stops time normally.
- **Ragdoll Mass:** Per-limb-group sliders (Head, Torso, Left/Right Arm, Left/Right Leg) plus an `AbsoluteWeight` total. Editing the total rescales every group proportionally; editing a single group recalculates the total.
    - **Live Application:** Applied to every `Rigidbody` under a `LimbEffectorReceiver` and polled twice a second, so newly spawned ragdolls pick up the current config without a restart.
- **Pistol (Glock17):** Muzzle velocity, fire delay, and native wound depth, all applied immediately to any Glock17 already in the world when you close the menu.
- **Custom Wound System:** Replaces the single-point native wound with a raycast that marches through every limb in the bullet's path, not just the first thing it hits.
    - **Falloff:** Each successive limb takes reduced depth and a smaller cone radius, so a shot loses power as it passes through bodies. Wounding stops at `WoundMinDepth` or `WoundMaxLimbs`, whichever comes first.
    - **Channel Shape:** The cone interpolates from `WoundConeRadius0` at entry to `WoundConeRadius1` at exit, giving the small-entry / large-exit shape real bullets leave.
- **Exit Wound VFX:** Tissue-coloured chunks sampled from the voxel atlas at the exit slice (skin, muscle, bone, or organ) are ejected with physics, can embed in whatever they hit, and fade out after their lifetime.
    - **Invisible Mode:** Turn off `ExitVoxelVisible` to keep the physics and collision behaviour without rendering the chunks - useful for testing decals in isolation.
- **Blood Decals:** Wherever a chunk sticks, scatter-placed decals are stamped onto the surface - sized, rotated, and clipped to the surface's real geometry, so corners without anything behind them get trimmed instead of floating in midair.
    - **Atlas Driven:** Tiles are drawn from the `Pixelblood` atlas at runtime; set `BloodAtlasCols` / `BloodAtlasRows` to match whatever atlas is loaded.
- **Optimizer:** Chunks and decals live in eviction queues. When FruitLib's perf monitor reports frame pressure, or long-window FPS drops below `TargetFPS`, the oldest are culled first at a rate set by `CullSpeed`.
- **QoL Tweaks:** Active chunk and decal counts are exposed as counters in FruitLib's perf overlay (**F11**), and the mod checks the installed FruitLib version on startup - refusing to load with a clear log message instead of crashing if it's too old.

## Requirements & Compatibility

- **Prerequisites:** MelonLoader 0.7.2+ Installation. [Check out their Tutorial!](https://melonwiki.xyz/#/)
- **Prerequisites:** [FruitLib](https://github.com/Luca-Nero/FruitLib) **2.0.2** or newer in your `Mods/` folder - FruitTweaks will not start without it.
- **Compatibility:** No known Incompatabilities.

## Installation

1. Download the latest release from the [Releases page](../../releases/latest).
2. Extract the archive.
3. Drop the contents into your game's `Mods/` directory.

## Controls (Defaults)

| Key | Action |
|-----|--------|
| F6 | Toggle custom gravity |

## Configuration

`FruitTweaksConfig.ini` is created next to the DLL on first launch. It is sectioned and documented - Controls, World, Ragdoll, Pistol, Wound, Exit Wound VFX, Blood Decals, and Optimizer. Changes made in FruitLib's in-game menu apply live and are written back to the file when the menu closes. Dropping in a new DLL is enough on update: the config is rewritten on load with new fields added and stale ones removed.

---

## Support & Feedback

Found a bug or have a suggestion? Feel free to open an issue on the [Issues page](../../issues) or catch me on Discord.

If you enjoy my work and want to support future updates, feel free to [buy me a coffee on Ko-fi](https://ko-fi.com/Luca_Nero)!

## License

[MIT](LICENSE) © Luca Nero / Game Community
