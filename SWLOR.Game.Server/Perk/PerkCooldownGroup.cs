using System.Linq;
using System.Reflection;

namespace SWLOR.Game.Server.Perk
{
    public enum PerkCooldownGroup
    {
        [PerkCooldown("None", 0.0f)]
        None = 0,
        [PerkCooldown("Evasiveness", 60.0f)]
        Evasiveness = 1,
        [PerkCooldown("Force Speed", 600)]
        ForceSpeed = 2,
        [PerkCooldown("Knockdown", 60)]
        Knockdown = 3,
        [PerkCooldown("Absorb Energy", 600)]
        AbsorbEnergy = 4,
        [PerkCooldown("Force Body", 600)]
        ForceBody = 5,
        [PerkCooldown("Mind Shield", 300)]
        MindShield = 6,
        [PerkCooldown("Rage", 600)]
        Rage = 7,
        [PerkCooldown("Force Persuade", 600)]
        ForcePersuade = 8,
        [PerkCooldown("Confusion", 600)]
        Confusion = 9,
        [PerkCooldown("Force Stun", 600)]
        ForceStun = 10,
        [PerkCooldown("Sith Alchemy", 600)]
        SithAlchemy = 11,
        [PerkCooldown("Throw Saber", 10)]
        ThrowSaber = 12,
        [PerkCooldown("Rest/Meditate", 300)]
        RestAndMeditate = 13,
        [PerkCooldown("Provoke", 30)]
        Provoke = 14,
        [PerkCooldown("Sneak Attack", 300)]
        SneakAttack = 15,
        [PerkCooldown("Premonition", 600)]
        Premonition = 16,
        [PerkCooldown("Comprehend Speech", 600)]
        ComprehendSpeech = 17,
        [PerkCooldown("Cross-Cut", 300)]
        CrossCut = 18,
        [PerkCooldown("Hide", 300)]
        Hide = 19,
        [PerkCooldown("Force Detection", 600)]
        ForceDetection = 20,
        [PerkCooldown("Farseeing", 600)]
        Farseeing = 21,
        [PerkCooldown("Shield Boost", 300)]
        ShieldBoost = 22,
        [PerkCooldown("Recovery Blast", 600)]
        RecoveryBlast = 23,
        [PerkCooldown("Leg Shot", 300)]
        LegShot = 24,
        [PerkCooldown("Tranquilizer", 120)]
        Tranquilizer = 25,
        [PerkCooldown("Mass Tranquilizer", 300)]
        MassTranquilizer = 26,
        [PerkCooldown("Precise Toss", 300)]
        PreciseToss = 27,
        [PerkCooldown("Battle/Force Insight", 600)]
        BattleAndForceInsight = 28,
        [PerkCooldown("Dash", 300)]
        Dash = 29,
        [PerkCooldown("Dash", 300)]
        ElectricFist = 30,
        [PerkCooldown("Plasma Cell", 300)]
        PlasmaCell = 31,
        [PerkCooldown("Chi", 300)]
        Chi = 32,
        [PerkCooldown("Stance", 600)]
        Stance = 33,
        [PerkCooldown("Animal Bond", 600)]
        AnimalBond = 34,
        [PerkCooldown("Drain Life", 12)]
        DrainLife = 35,
        [PerkCooldown("Force Lightning", 12)]
        ForceLightning = 36,
        [PerkCooldown("Force Push", 10)]
        ForcePush = 37,
        [PerkCooldown("Force Breach", 10)]
        ForceBreach = 38,
        [PerkCooldown("Skewer", 300)]
        Skewer = 40,
    }

    public static class PerkCooldownGroupExtensions
    {
        private static PerkCooldownAttribute GetAttribute(PerkCooldownGroup @group)
        {
            var enumType = typeof(PerkCooldownGroup);
            var memberInfos = enumType.GetMember(@group.ToString());
            var enumValueMemberInfo = memberInfos.First(m => m.DeclaringType == enumType);
            var valueAttributes = enumValueMemberInfo.GetCustomAttributes(typeof(PerkCooldownAttribute), false);
            return ((PerkCooldownAttribute)valueAttributes[0]);
        }

        public static float GetDelay(this PerkCooldownGroup group)
        {
            var attribute = GetAttribute(group);

            return attribute.Delay;
        }

        public static string GetName(this PerkCooldownGroup group)
        {
            var attribute = GetAttribute(group);

            return attribute.Name;
        }
    }
}
