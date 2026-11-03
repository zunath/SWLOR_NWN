using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.World;
using SkillType = SWLOR.Game.Server.Service.SkillService.SkillType;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The chat instance with the four calls into not-yet-converted services answered by the test: a translation marks the
/// text with the listener (the real one rolls against the database's language ranks), a chat name is a readable
/// placeholder, the per-observer name override is recorded around the send it wraps, and the space-mode check reads a
/// set. Everything else is the production code.
/// </summary>
public sealed class TestCommunicationService : CommunicationService
{
    public TestCommunicationService(
        IDBService db,
        IDMPresenceService dmPresence,
        IDMPreferenceService dmPreference,
        IPartyService party,
        IHoloComService holoCom,
        IPlanetService planet,
        IAreaService area,
        IPropertyService property)
        : base(db, dmPresence, dmPreference, party, holoCom, planet, area, property)
    {
    }

    /// <summary>Every translation asked for, in order.</summary>
    public List<(uint Speaker, uint Listener, SkillType Language, string Snippet)> Translations { get; } = new();

    /// <summary>
    /// Every name override applied around a send, in order: the observer, the target, and how many messages the chat
    /// plugin sent while the override was in place.
    /// </summary>
    public List<(uint Observer, uint Target, int SendsInside)> NameOverrides { get; } = new();

    /// <summary>Counts the messages the chat plugin has sent so far; the bench sets it.</summary>
    public Func<int> CountSends { get; set; } = () => 0;

    /// <summary>Players the space-mode check answers true for.</summary>
    public HashSet<uint> InSpace { get; } = new();

    /// <summary>What the chat name of <paramref name="target"/> looks like to <paramref name="observer"/>.</summary>
    public static string ChatName(uint observer, uint target) => $"<{target} seen by {observer}>";

    /// <summary>What a translation of <paramref name="snippet"/> for <paramref name="listener"/> looks like.</summary>
    public static string Translated(uint listener, string snippet) => $"~{listener}:{snippet}~";

    protected override string TranslateSnippetForListener(uint speaker, uint listener, SkillType language, string snippet)
    {
        Translations.Add((speaker, listener, language, snippet));
        return Translated(listener, snippet);
    }

    protected override string GetColoredChatDisplayName(uint observer, uint target) => ChatName(observer, target);

    protected override void SendChatMessageWithChatNameOverride(uint observer, uint target, Action sendMessage)
    {
        var before = CountSends();
        sendMessage();
        NameOverrides.Add((observer, target, CountSends() - before));
    }

    protected override bool IsPlayerInSpaceMode(uint player) => InSpace.Contains(player);
}
