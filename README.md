# FruitTweaks v2.0

A grab-bag of gameplay knobs for FRUKT: gravity, time scale, ragdoll mass, and
pistol ballistics, plus a from-scratch wound and gore pipeline for the Glock17
— raycast-based limb wounding, ejected tissue chunks, and scatter blood decals.
Everything is editable live through FruitLib's in-game menu, no restart needed.

Requires [FruitLib](../5_FruitLib) **2.0.0** or newer in your `Mods` folder.

---

## Features

### World
- **Custom gravity** — press **F6** to swap between normal Unity gravity and a
  configurable `(X, Y, Z)` vector.
- **Time scale** — set game speed as a percentage of normal (0 = frozen).
  Doesn't fight the game's own pause (menu open still stops time normally).

### Ragdoll
Per-limb-group mass sliders (Head, Torso, Left/Right Arm, Left/Right Leg) plus
an `AbsoluteWeight` total. Editing the total rescales every group proportionally
from the game's default masses; editing a single group recalculates the total.
Applied to every `Rigidbody` under a `LimbEffectorReceiver`, polled twice a
second so newly spawned ragdolls pick up the current config without a restart.

### Pistol (Glock17)
Muzzle velocity, fire delay, and native wound depth are all configurable and
apply immediately to any Glock17 already in the world when you close the menu.

### Custom Wound System
Replaces the single-point native wound with a raycast that marches through
every limb in the bullet's path, not just the first thing it hits:
- Each successive limb takes reduced wound depth and a smaller cone radius
  (`WoundFalloffDivisor` / `WoundRadiusFalloffDivisor`), so a shot loses power
  as it passes through bodies.
- Wounding stops once depth drops below `WoundMinDepth` or `WoundMaxLimbs` is
  reached, whichever comes first.
- The wound cone interpolates from `WoundConeRadius0` at entry to
  `WoundConeRadius1` at exit, giving the small-entry / large-exit channel
  shape real bullets leave.

### Exit Wound VFX
On exit, tissue-colored chunks (sampled from the voxel atlas at the exit
slice — skin, muscle, bone, or organ) are ejected with physics, can embed in
whatever they hit, and fade out after `ExitVoxelLifetime` seconds. Turn off
`ExitVoxelVisible` to keep the physics/collision behavior without rendering
them, useful for testing decals in isolation.

### Blood Decals
Wherever an ejected chunk sticks, scatter-placed blood decals are stamped onto
the surface — sized, rotated, and clipped to the surface's actual geometry
(corners without a surface behind them get trimmed instead of floating in
midair). Tiles are drawn from the `Pixelblood` atlas at runtime; grid layout
(`BloodAtlasCols` / `BloodAtlasRows`) should match whatever atlas is loaded.

### Optimizer
Chunks and decals are tracked in eviction queues. When FruitLib's performance
monitor reports frame pressure, or long-window FPS drops below `TargetFPS`,
the oldest chunks and decals are evicted first — `CullSpeed` controls how
aggressively. Active counts show up as `Chunks (active)` / `Decals (active)`
counters in FruitLib's perf overlay (**F11**).

---

## How to Install
1. Install [FruitLib](../5_FruitLib) first — FruitTweaks won't start without it.
2. Drag **FruitTweaks.dll** into your `Mods/` folder.
3. Run the game — `FruitTweaksConfig.ini` appears next to the DLL on first launch.

## How to Update
1. Drop in the new DLL — no need to delete the old config, it's rewritten on
   load with any new fields added and stale ones removed.

---

## Controls (Defaults)

| Key | Action |
|-----|--------|
| F6 | Toggle custom gravity |

Remap in `FruitTweaksConfig.ini`, or live in FruitLib's menu.

---

## Config

Every parameter lives in a sectioned, documented `.ini` file — World, Ragdoll,
Pistol, Wound, Exit Wound VFX, Blood Decals, and Optimizer. Changes made in the
in-game FruitLib menu apply live and are written back to the file when the
menu closes.

---

## Compatibility

FruitTweaks checks the installed FruitLib version on startup and refuses to
load with a clear log message instead of crashing if it's too old.
