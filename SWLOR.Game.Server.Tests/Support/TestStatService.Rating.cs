using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

// Part 6, StatRatingService: Detection, Stealth, Lockpicking, Control and Craftsmanship.
public sealed partial class TestStatService
{
    protected override SkillAttribute GetSkillDetails(SkillType skillType) => _bench.SkillDetails[skillType];
}
