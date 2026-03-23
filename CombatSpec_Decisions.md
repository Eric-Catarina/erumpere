# Combat Spec v1 - Canonical Decisions

This document records decisions for ambiguous points in `Spec_Combat_V1.md` so implementation and tests stay deterministic.

## Locked Decisions

- **Deathblow**: v1 does not execute a dedicated deathblow roll. `isDeathblowPending` is retained in the model for future extension.
- **Stealth exceptions**: no exception in v1. Direct-target skills cannot target stealth units.
- **Corruption multipliers**: loaded from `CombatBalanceConfig` data table; default values are provided and can be replaced without engine changes.
- **Defensive token consumption order**: resolve in this order: `Dodge` -> `BlockPlus` -> `Block`.
- **Movement with size 2/3**: clamp front rank to `[1, 5-size]` and run compaction after movement.
- **Compaction timing**: immediate compaction after each death and after forced movement.
- **pick_rate**: defined as `skill_uses / total_action_events`.
