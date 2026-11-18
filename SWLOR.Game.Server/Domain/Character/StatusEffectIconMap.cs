using System.Collections.Frozen;
using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The two icon tables of the status effects: the script effect type each icon stands for and the ability an ability-changing
    /// icon buffs or debuffs. Frozen data and two lookups, with no state, so code that has no container can read it.
    /// </summary>
    public static class StatusEffectIconMap
    {
        private static readonly FrozenDictionary<EffectIconType, AbilityType> _abilityIncreaseIconType = new Dictionary<EffectIconType, AbilityType>
        {
            { EffectIconType.AbilityIncreaseSTR, AbilityType.Might },
            { EffectIconType.AbilityDecreaseSTR, AbilityType.Might },
            { EffectIconType.AbilityIncreaseDEX, AbilityType.Perception },
            { EffectIconType.AbilityDecreaseDEX, AbilityType.Perception },
            { EffectIconType.AbilityIncreaseCON, AbilityType.Vitality },
            { EffectIconType.AbilityDecreaseCON, AbilityType.Vitality },
            { EffectIconType.AbilityIncreaseINT, AbilityType.Agility },
            { EffectIconType.AbilityDecreaseINT, AbilityType.Agility },
            { EffectIconType.AbilityIncreaseWIS, AbilityType.Willpower },
            { EffectIconType.AbilityDecreaseWIS, AbilityType.Willpower },
            { EffectIconType.AbilityIncreaseCHA, AbilityType.Social },
            { EffectIconType.AbilityDecreaseCHA, AbilityType.Social },
        }.ToFrozenDictionary();

        private static readonly FrozenDictionary<EffectIconType, EffectTypeScript> _effectIconToEffectType = new Dictionary<EffectIconType, EffectTypeScript>
        {
            { EffectIconType.Invalid, EffectTypeScript.Invalideffect },
            { EffectIconType.DamageResistance, EffectTypeScript.DamageResistance },
            { EffectIconType.Regenerate, EffectTypeScript.Regenerate },
            { EffectIconType.DamageReduction, EffectTypeScript.DamageReduction },
            { EffectIconType.TemporaryHitpoints, EffectTypeScript.TemporaryHitpoints },
            { EffectIconType.Entangle, EffectTypeScript.Entangle },
            { EffectIconType.Invulnerable, EffectTypeScript.Invulnerable },
            { EffectIconType.Fatigue, EffectTypeScript.Invalideffect },
            { EffectIconType.Deaf, EffectTypeScript.Deaf },
            { EffectIconType.Immunity, EffectTypeScript.Immunity },
            { EffectIconType.EnemyAttackBonus, EffectTypeScript.EnemyAttackBonus },
            { EffectIconType.Charmed, EffectTypeScript.Charmed },
            { EffectIconType.Confused, EffectTypeScript.Confused },
            { EffectIconType.Frightened, EffectTypeScript.Frightened },
            { EffectIconType.Dominated, EffectTypeScript.Dominated },
            { EffectIconType.Paralyze, EffectTypeScript.Paralyze },
            { EffectIconType.Dazed, EffectTypeScript.Dazed },
            { EffectIconType.Stunned, EffectTypeScript.Stunned },
            { EffectIconType.Sleep, EffectTypeScript.Sleep },
            { EffectIconType.Poison, EffectTypeScript.Poison },
            { EffectIconType.Disease, EffectTypeScript.Disease },
            { EffectIconType.Curse, EffectTypeScript.Curse },
            { EffectIconType.Silence, EffectTypeScript.Silence },
            { EffectIconType.Turned, EffectTypeScript.Turned },
            { EffectIconType.Haste, EffectTypeScript.Haste },
            { EffectIconType.Slow, EffectTypeScript.Slow },
            { EffectIconType.AbilityIncreaseSTR, EffectTypeScript.AbilityIncrease },
            { EffectIconType.AbilityDecreaseSTR, EffectTypeScript.AbilityDecrease },
            { EffectIconType.AttackIncrease, EffectTypeScript.AttackIncrease },
            { EffectIconType.AttackDecrease, EffectTypeScript.AttackDecrease },
            { EffectIconType.DamageIncrease, EffectTypeScript.DamageIncrease },
            { EffectIconType.DamageDecrease, EffectTypeScript.DamageDecrease },
            { EffectIconType.DamageImmunityIncrease, EffectTypeScript.DamageImmunityIncrease },
            { EffectIconType.DamageImmunityDecrease, EffectTypeScript.DamageImmunityDecrease },
            { EffectIconType.ACIncrease, EffectTypeScript.ACIncrease },
            { EffectIconType.ACDecrease, EffectTypeScript.ACDecrease },
            { EffectIconType.MovementSpeedIncrease, EffectTypeScript.MovementSpeedIncrease },
            { EffectIconType.MovementSpeedDecrease, EffectTypeScript.MovementSpeedDecrease },
            { EffectIconType.SpellResistanceIncrease, EffectTypeScript.SpellResistanceIncrease },
            { EffectIconType.SpellResistanceDecrease, EffectTypeScript.SpellResistanceDecrease },
            { EffectIconType.SkillIncrease, EffectTypeScript.SkillIncrease },
            { EffectIconType.SkillDecrease, EffectTypeScript.SkillDecrease },
            { EffectIconType.Invisibility, EffectTypeScript.Invisibility },
            { EffectIconType.ImprovedInvisibility, EffectTypeScript.ImprovedInvisibility },
            { EffectIconType.Darkness, EffectTypeScript.Darkness },
            { EffectIconType.DispelMagicAll, EffectTypeScript.DispelMagicAll },
            { EffectIconType.ElementalShield, EffectTypeScript.ElementalShield },
            { EffectIconType.LevelDrain, EffectTypeScript.NegativeLevel },
            { EffectIconType.Polymorph, EffectTypeScript.Polymorph },
            { EffectIconType.Sanctuary, EffectTypeScript.Sanctuary },
            { EffectIconType.TrueSeeing, EffectTypeScript.TrueSeeing },
            { EffectIconType.SeeInvisibility, EffectTypeScript.SeeInvisible },
            { EffectIconType.Timestop, EffectTypeScript.Timestop },
            { EffectIconType.Blindness, EffectTypeScript.Blindness },
            { EffectIconType.SpellLevelAbsorption, EffectTypeScript.SpellLevelAbsorption },
            { EffectIconType.DispelMagicBest, EffectTypeScript.DispelMagicBest },
            { EffectIconType.AbilityIncreaseDEX, EffectTypeScript.AbilityIncrease },
            { EffectIconType.AbilityDecreaseDEX, EffectTypeScript.AbilityDecrease },
            { EffectIconType.AbilityIncreaseCON, EffectTypeScript.AbilityIncrease },
            { EffectIconType.AbilityDecreaseCON, EffectTypeScript.AbilityDecrease },
            { EffectIconType.AbilityIncreaseINT, EffectTypeScript.AbilityIncrease },
            { EffectIconType.AbilityDecreaseINT, EffectTypeScript.AbilityDecrease },
            { EffectIconType.AbilityIncreaseWIS, EffectTypeScript.AbilityIncrease },
            { EffectIconType.AbilityDecreaseWIS, EffectTypeScript.AbilityDecrease },
            { EffectIconType.AbilityIncreaseCHA, EffectTypeScript.AbilityIncrease },
            { EffectIconType.AbilityDecreaseCHA, EffectTypeScript.AbilityDecrease },
            { EffectIconType.ImmunityAll, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityMind, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityPoison, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityDisease, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityFear, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityTrap, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityParalysis, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityBlindness, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityDeafness, EffectTypeScript.Immunity },
            { EffectIconType.ImmunitySlow, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityEntangle, EffectTypeScript.Immunity },
            { EffectIconType.ImmunitySilence, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityStun, EffectTypeScript.Immunity },
            { EffectIconType.ImmunitySleep, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityCharm, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityDominate, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityConfuse, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityCurse, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityDazed, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityAbilityDecrease, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityAttackDecrease, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityDamageDecrease, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityDamageImmunityDecrease, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityACDecrease, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityMovementSpeedDecrease, EffectTypeScript.Immunity },
            { EffectIconType.ImmunitySpellResistanceDecrease, EffectTypeScript.Immunity },
            { EffectIconType.ImmunitySkillDecrease, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityKnockdown, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityNegativeLevel, EffectTypeScript.Immunity },
            { EffectIconType.ImmunitySneakAttack, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityCriticalHit, EffectTypeScript.Immunity },
            { EffectIconType.ImmunityDeathMagic, EffectTypeScript.Immunity },
            { EffectIconType.Taunted, EffectTypeScript.Invalideffect },
            { EffectIconType.SpellImmunity, EffectTypeScript.SpellImmunity },
            { EffectIconType.Etherealness, EffectTypeScript.Ethereal },
            { EffectIconType.Concealment, EffectTypeScript.Concealment },
            { EffectIconType.Petrified, EffectTypeScript.Petrify },
            { EffectIconType.EffectSpellFailure, EffectTypeScript.SpellFailure },
            { EffectIconType.DamageImmunityMagic, EffectTypeScript.DamageImmunityIncrease },
            { EffectIconType.DamageImmunityAcid, EffectTypeScript.DamageImmunityIncrease },
            { EffectIconType.DamageImmunityCold, EffectTypeScript.DamageImmunityIncrease },
            { EffectIconType.DamageImmunityDivine, EffectTypeScript.DamageImmunityIncrease },
            { EffectIconType.DamageImmunityElectrical, EffectTypeScript.DamageImmunityIncrease },
            { EffectIconType.DamageImmunityFire, EffectTypeScript.DamageImmunityIncrease },
            { EffectIconType.DamageImmunityNegative, EffectTypeScript.DamageImmunityIncrease },
            { EffectIconType.DamageImmunityPositive, EffectTypeScript.DamageImmunityIncrease },
            { EffectIconType.DamageImmunitySonic, EffectTypeScript.DamageImmunityIncrease },
            { EffectIconType.DamageImmunityMagicDecrease, EffectTypeScript.DamageImmunityDecrease },
            { EffectIconType.DamageImmunityAcidDecrease, EffectTypeScript.DamageImmunityDecrease },
            { EffectIconType.DamageImmunityColdDecrease, EffectTypeScript.DamageImmunityDecrease },
            { EffectIconType.DamageImmunityDivineDecrease, EffectTypeScript.DamageImmunityDecrease },
            { EffectIconType.DamageImmunityElectricalDecrease, EffectTypeScript.DamageImmunityDecrease },
            { EffectIconType.DamageImmunityFireDecrease, EffectTypeScript.DamageImmunityDecrease },
            { EffectIconType.DamageImmunityNegativeDecrease, EffectTypeScript.DamageImmunityDecrease },
            { EffectIconType.DamageImmunityPositiveDecrease, EffectTypeScript.DamageImmunityDecrease },
            { EffectIconType.DamageImmunitySonicDecrease, EffectTypeScript.DamageImmunityDecrease },
            { EffectIconType.Charge, EffectTypeScript.MovementSpeedIncrease },
            { EffectIconType.Dedication, EffectTypeScript.Invalideffect },
            { EffectIconType.FrenziedShout, EffectTypeScript.Invalideffect },
            { EffectIconType.Rejuvenation, EffectTypeScript.Regenerate },
            { EffectIconType.SoldiersPrecision, EffectTypeScript.AttackIncrease },
            { EffectIconType.SoldiersSpeed, EffectTypeScript.MovementSpeedIncrease },
            { EffectIconType.SoldiersStrike, EffectTypeScript.DamageIncrease },
        }.ToFrozenDictionary();

        public static EffectTypeScript GetEffectTypeFromIcon(EffectIconType effectIcon)
        {
            return _effectIconToEffectType.TryGetValue(effectIcon, out var effectType)
                ? effectType
                : EffectTypeScript.Invalideffect;
        }

        public static AbilityType GetAbilityTypeBuffed(EffectIconType effectIcon)
        {
            return _abilityIncreaseIconType.TryGetValue(effectIcon, out var abilityType)
                ? abilityType
                : AbilityType.Invalid;
        }
    }
}
