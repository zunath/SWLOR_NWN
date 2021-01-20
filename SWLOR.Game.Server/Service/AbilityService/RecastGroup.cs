using System;

namespace SWLOR.Game.Server.Service.AbilityService
{
    // Note: Short names are what's displayed on the recast Gui element. They are limited to 14 characters.
    public enum RecastGroup
    {
        [RecastGroup("Invalid", "Invalid")]
        Invalid = 0,
        [RecastGroup("Burst Of Speed", "Burst Of Speed")]
        BurstOfSpeed = 1,
        [RecastGroup("Force Heal", "Force Heal")]
        ForceHeal = 2,
        [RecastGroup("Force Push", "Force Push")]
        ForcePush = 3,
        [RecastGroup("Throw Lightsaber", "Throw Saber")]
        ThrowLightsaber = 4,
        [RecastGroup("Force Stun", "Force Stun")]
        ForceStun = 5,
        [RecastGroup("Battle Insight", "Battle Insight")]
        BattleInsight = 6,
        [RecastGroup("Comprehend Speech", "Comp. Speech")]
        ComprehendSpeech = 7,
        [RecastGroup("Mind Trick", "Mind Trick")]
        MindTrick = 8,
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
