# NPC Hit Points

Required reading before adding creatures, restatting them, or changing NPC HP code.

- A stat skin's `NPCHP` is the creature's final maximum HP.
- NWN adds Vitality, Toughness, and Epic Toughness on top of a UTC's `HitPoints`. Never write `NPCHP` to UTC `HitPoints` or pass it to `ObjectPlugin.SetMaxHitPoints`.
- In the UTC, set `CurrentHitPoints` and `MaxHitPoints` to `NPCHP`, and `HitPoints` to `NPCHP` minus those bonuses.
- Apply runtime HP budgets only through `Stat.SetNPCMaxHitPoints`, after Vitality is final.
- After adding or restatting creatures, run `powershell -ExecutionPolicy Bypass -File tools/NormalizeNpcHitPoints.ps1` (`-CheckOnly` to audit). `NPCEnemyBalanceAuditTests` guards the result.
- The engine reads current plus temporary HP as a 16-bit value: totals above 32,767 wrap negative and the creature reads as dead. Keep HP budgets and temporary-HP grants under that limit.
- Native `GetCurrentHitPoints` includes temporary HP. Compare base HP with `ObjectPlugin.GetCurrentHitPoints` before calling `SetCurrentHitPoints`, or shielded creatures heal for free.
