using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// Answers the companion questions Character's services ask (<see cref="ICompanionLookup"/>). It reads the creature's
    /// own state and the droid controller item through a <see cref="DroidControllerReader"/> and takes no service, so
    /// <see cref="BeastMasteryService"/> and <see cref="DroidService"/> share it and the services below them build without a cycle.
    /// </summary>
    public sealed class CompanionLookup : ICompanionLookup
    {
        public const string BeastIdVariable = "BEAST_ID";
        public const string BeastTypeVariable = IBeastCorpseInfo.BeastTypeVariable;

        private readonly DroidControllerReader _reader = new();

        public bool IsDroid(uint creature)
        {
            return GetResRef(creature) == DroidService.DroidResref;
        }

        public bool IsPlayerBeast(uint creature)
        {
            if (GetBeastType(creature) == BeastType.Invalid)
                return false;

            var master = GetMaster(creature);
            if (!GetIsObjectValid(master) || !GetIsPC(master))
                return false;

            return true;
        }

        public BeastType GetBeastType(uint creature)
        {
            return (BeastType)GetLocalInt(creature, BeastTypeVariable);
        }

        public string GetBeastId(uint beast)
        {
            return GetLocalString(beast, BeastIdVariable);
        }

        public int GetDroidPerkLevel(uint droid, PerkType perkType)
        {
            var controller = _reader.GetControllerItem(droid);
            var droidDetails = _reader.LoadDroidItemPropertyDetails(controller);

            return droidDetails.Perks.TryGetValue(perkType, out var droidPerkLevel)
                ? droidPerkLevel
                : 0;
        }

        public int GetDroidSkillRank(uint droid, SkillType skillType)
        {
            var controller = _reader.GetControllerItem(droid);
            var droidDetails = _reader.LoadDroidItemPropertyDetails(controller);

            return droidDetails.Skills.TryGetValue(skillType, out var droidSkillRank)
                ? droidSkillRank
                : 0;
        }
    }
}
