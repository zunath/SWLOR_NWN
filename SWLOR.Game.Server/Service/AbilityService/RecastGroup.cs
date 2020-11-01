using System;

namespace SWLOR.Game.Server.Service.AbilityService
{
    // Note: Short names are what's displayed on the recast Gui element. They are limited to 14 characters.
    public enum RecastGroup
    {
        [RecastGroup("Invalid", "Invalid")]
        Invalid = 0,
        [RecastGroup("One-Hour Ability", "1-Hr Ability")] 
        OneHourAbility = 1,
        [RecastGroup("Fire", "Fire")]
        Fire = 2,
        [RecastGroup("Blizzard", "Blizzard")]
        Blizzard = 3,
        [RecastGroup("Thunder", "Thunder")]
        Thunder = 4,
        [RecastGroup("Warp", "Warp")]
        Warp = 5,
        [RecastGroup("Blaze Spikes", "Blaze Spikes")]
        BlazeSpikes = 5,
        [RecastGroup("Elemental Spread", "Elem. Spread")]
        ElementalSpread = 6,
        [RecastGroup("Sleep", "Sleep")]
        Sleep = 7,
        [RecastGroup("Subtle Blow", "Subtle Blow")]
        SubtleBlow = 8,
        [RecastGroup("Inner Healing", "Inner Healing")]
        InnerHealing = 9,
        [RecastGroup("Valor", "Valor")]
        Valor = 10,
        [RecastGroup("Chakra", "Chakra")]
        Chakra = 11,
        [RecastGroup("Electric Fist", "Elec. Fist")]
        ElectricFist = 12,
        [RecastGroup("Defender", "Defender")]
        Defender = 13,
        [RecastGroup("Ironclad", "Ironclad")]
        Ironclad = 14,
        [RecastGroup("Spiked Defense", "Spiked Def.")]
        SpikedDefense = 15,
        [RecastGroup("Provoke I", "Provoke I")]
        Provoke1 = 16,
        [RecastGroup("Provoke II", "Provoke II")]
        Provoke2 = 17,
        [RecastGroup("Flash", "Flash")]
        Flash = 18,
        [RecastGroup("Bash", "Bash")]
        Bash = 19,
        [RecastGroup("Cover", "Cover")]
        Cover = 20,
        [RecastGroup("Flee", "Flee")]
        Flee = 21,
        [RecastGroup("Hide", "Hide")]
        Hide = 22,
        [RecastGroup("Wasp Sting", "Wasp Sting")]
        WaspSting = 23,
        [RecastGroup("Shadowstitch", "Shadowstitch")]
        Shadowstitch = 24,
        [RecastGroup("Sneak Attack", "Sneak Atk.")]
        SneakAttack = 25,
        [RecastGroup("Life Steal", "Life Steal")]
        LifeSteal = 26,
        [RecastGroup("Steal", "Steal")]
        Steal = 27,
        [RecastGroup("Regen", "Regen")]
        Regen = 28,
        [RecastGroup("Raise", "Raise")]
        Raise = 29,
        [RecastGroup("Poisona", "Poisona")]
        Poisona = 30,
        [RecastGroup("Teleport", "Teleport")]
        Teleport = 31,
        [RecastGroup("Stone", "Stone")]
        Stone = 32,
        [RecastGroup("Dia", "Dia")]
        Dia = 33,
        [RecastGroup("Protect", "Protect")]
        Protect = 34,
        [RecastGroup("Protectra", "Protectra")]
        Protectra = 35,
        [RecastGroup("Cure I", "Cure I")]
        Cure1 = 36,
        [RecastGroup("Cure II", "Cure II")]
        Cure2 = 37,
        [RecastGroup("Cure III", "Cure III")]
        Cure3 = 38,
        [RecastGroup("Curaga I", "Curaga I")]
        Curaga1 = 39,
        [RecastGroup("Curaga II", "Curaga II")]
        Curaga2 = 40,
        [RecastGroup("Transfer", "Transfer")]
        Transfer = 41,
        [RecastGroup("Piercing Stab", "Piercing Stab")]
        PiercingStab = 42,
        [RecastGroup("Blind", "Blind")]
        Blind = 43,
        [RecastGroup("Recovery Stab", "Recovery Stab")]
        RecoveryStab = 44,
        [RecastGroup("Convert", "Convert")]
        Convert = 45,
        [RecastGroup("Refresh", "Refresh")]
        Refresh = 46,
        [RecastGroup("Jolt", "Jolt")]
        Jolt = 47,
        [RecastGroup("Poison Stab", "Poison Stab")]
        PoisonStab = 48,
        [RecastGroup("Shock Spikes", "Shock Spikes")]
        ShockSpikes = 49,
        [RecastGroup("Deliberate Stab", "Delib. Stab")]
        DeliberateStab = 50,
    }

    public class RecastGroupAttribute: Attribute
    {
        public string Name { get; set; }
        public string ShortName { get; set; }

        public RecastGroupAttribute(string name, string shortName)
        {
            Name = name;
            ShortName = shortName;
        }
    }
}
