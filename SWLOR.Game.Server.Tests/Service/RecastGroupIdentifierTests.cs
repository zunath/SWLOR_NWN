using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AbilityService;

namespace SWLOR.Game.Server.Tests.Service;

public class RecastGroupIdentifierTests
{
    [Test]
    public void RecastGroups_HaveUniquePersistedIdentifiers()
    {
        var duplicates = Enum.GetNames<RecastGroup>()
            .GroupBy(name => (int)Enum.Parse<RecastGroup>(name))
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key}: {string.Join(", ", group)}");
        duplicates.Should().BeEmpty("unrelated cooldowns must never share a persisted recast identifier");
    }
}
