using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AchievementService;

namespace SWLOR.Game.Server.Tests.Service;

public sealed class AchievementTypeTests
{
    [Test]
    public void AchievementDefinitions_181Through210_AreActiveAndUseConsecutiveStableIds()
    {
        var achievements = Enum.GetValues<AchievementType>()
            .Where(type => (int)type is >= 181 and <= 210)
            .ToArray();

        achievements.Select(type => (int)type).Should().Equal(Enumerable.Range(181, 30));
        achievements.Should().HaveCount(30);

        foreach (var achievement in achievements)
        {
            var detail = typeof(AchievementType)
                .GetMember(achievement.ToString())
                .Single()
                .GetCustomAttribute<AchievementAttribute>();

            detail.Should().NotBeNull();
            detail!.IsActive.Should().BeTrue();
            detail.Name.Should().NotBeNullOrWhiteSpace();
            detail.Description.Should().NotBeNullOrWhiteSpace();
        }
    }
}
