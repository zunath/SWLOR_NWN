using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Domain.Crafting;

/// <inheritdoc cref="ICraftStyleEffectsService" />
public class CraftStyleEffectsService : ICraftStyleEffectsService
{
    private readonly IDBService _db;

    public CraftStyleEffectsService(IDBService db)
    {
        _db = db;
    }

    private static bool IsPlayer(uint creature) =>
        GetIsObjectValid(creature) && GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature);

    /// <inheritdoc />
    public void Record(uint creature, SkillType skill, PerkType perk)
    {
        if (!IsPlayer(creature))
            return;

        var record = _db.Get<Player>(GetObjectUUID(creature));
        if (record != null && CraftingStyleSelection.Set(record, skill, perk))
            _db.Set(record);
    }

    /// <inheritdoc />
    public void ClearIfSelected(uint creature, SkillType skill, PerkType perk)
    {
        if (!IsPlayer(creature))
            return;

        var record = _db.Get<Player>(GetObjectUUID(creature));
        if (record != null && CraftingStyleSelection.Selected(record, skill) == perk && CraftingStyleSelection.Set(record, skill, PerkType.Invalid))
            _db.Set(record);
    }

    /// <inheritdoc />
    public void Restore(uint creature)
    {
        if (!IsPlayer(creature) || GetIsDead(creature))
            return;

        var record = _db.Get<Player>(GetObjectUUID(creature));
        if (record == null)
            return;

        if (CraftingStyleSelection.Reconcile(record, CraftingStyleSelection.Definitions, out var active))
            _db.Set(record);

        foreach (var definition in CraftingStyleSelection.Definitions)
        {
            var isActive = HasEffect(definition.StatusEffectType, creature);
            if (active.Contains(definition))
            {
                if (!isActive)
                    ApplyPermanentEffect(definition.StatusEffectType, creature);
            }
            else if (isActive)
            {
                RemoveEffect(creature, definition.StatusEffectType);
            }
        }
    }

    /// <summary>Whether the creature has the effect. StatusEffect is not converted yet.</summary>
    protected virtual bool HasEffect(Type effect, uint creature) => StatusEffect.HasEffect(effect, creature);

    /// <summary>Applies the effect to the creature for good. StatusEffect is not converted yet.</summary>
    protected virtual void ApplyPermanentEffect(Type effect, uint creature) =>
        StatusEffect.ApplyPermanentStatusEffect(effect, creature, creature);

    /// <summary>Removes the effect from the creature. StatusEffect is not converted yet.</summary>
    protected virtual void RemoveEffect(uint creature, Type effect) => StatusEffect.RemoveStatusEffect(creature, effect);
}
