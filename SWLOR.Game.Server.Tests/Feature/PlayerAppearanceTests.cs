using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
using SWLOR.Game.Server.Feature.ChatCommandDefinition;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class PlayerAppearanceTests
{
    [Test]
    public void AppearanceCommands_CoexistAndAllowStaffAuthorizedPlayers()
    {
        var commands = new CharacterChatCommand().BuildChatCommands();
        foreach (var name in new[] { "disguise", "disguises", "headscale" })
            commands[name].Authorization.Should().Be(AuthorizationLevel.All, name);
    }

    [Test]
    public void RacialRegistry_RetainsEveryAuthoredSpecies()
    {
        var authored = typeof(IRacialAppearanceDefinition).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface &&
                typeof(IRacialAppearanceDefinition).IsAssignableFrom(type));
        var registered = Enum.GetValues<AppearanceType>()
            .Select(appearance => RacialAppearanceRegistry.TryGet(appearance, out var definition)
                ? definition.GetType() : null)
            .Where(type => type != null).Distinct();

        registered.Should().BeEquivalentTo(authored);
    }

    [Test]
    public void LegacyPlayer_DefaultsHeadScaleWithoutChangingBodyScale()
    {
        var player = JsonConvert.DeserializeObject<Player>("{\"AppearanceScale\":0.92}")!;
        player.AppearanceScale.Should().Be(0.92f);
        player.HeadAppearanceScale.Should().Be(1f);

        player.HeadAppearanceScale = 1.08f;
        var saved = JsonConvert.DeserializeObject<Player>(JsonConvert.SerializeObject(player))!;
        saved.AppearanceScale.Should().Be(0.92f);
        saved.HeadAppearanceScale.Should().Be(1.08f);
    }
}
