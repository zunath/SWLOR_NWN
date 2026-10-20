using NUnit.Framework;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Tests.Service;

public class StatTypeIdentityTests
{
    [Test]
    public void StatTypesHaveDistinctNumericIdentities()
    {
        var duplicates = Enum.GetNames<StatType>()
            .GroupBy(name => (int)Enum.Parse<StatType>(name))
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key}: {string.Join(", ", group)}");

        Assert.That(duplicates, Is.Empty);
    }
}
