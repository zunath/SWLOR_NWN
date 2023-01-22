using System;

namespace SWLOR.Game.Server.Service.BeastMasteryService
{
    public enum BeastRoleType
    {
        [BeastRole("Invalid", false)]
        Invalid = 0,
        [BeastRole("Bruiser", true)]
        Bruiser = 1,
        [BeastRole("Force", true)]
        Force = 2,
        [BeastRole("Evasion", true)]
        Evasion = 3,
        [BeastRole("Damage", true)]
        Damage = 4,
        [BeastRole("Tank", true)]
        Tank = 5,
        [BeastRole("Balanced", true)]
        Balanced = 6
    }

    public class BeastRoleAttribute : Attribute
    {
        public string Name { get; set; }
        public bool IsActive { get; set; }

        public BeastRoleAttribute(string name, bool isActive)
        {
            Name = name;
            IsActive = isActive;
        }
    }
}
