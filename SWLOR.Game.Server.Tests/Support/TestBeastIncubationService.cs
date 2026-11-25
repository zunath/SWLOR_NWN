#nullable enable
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.World;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Part BM-4, <see cref="BeastIncubationService"/>, over a <see cref="TestBeastMastery"/> with the perk read and the window answered by the
/// test: the perk levels and the recording list are the host's, so a test sets both in one place.
/// </summary>
public sealed class TestBeastIncubationService : BeastIncubationService
{
    private readonly TestBeastMastery _host;

    public TestBeastIncubationService(TestBeastMastery host, IDBService db, IPropertyService property, IIncubationFieldNoteService notes)
        : base(db, property, notes, FacadeForwarder.For<IResistanceService>(), FacadeForwarder.For<ICombatDamageCatalogService>(), FacadeForwarder.For<IPerkService>(), FacadeForwarder.For<IGuiService>(), host)
    {
        _host = host;
    }

    protected override int GetPerkLevel(uint creature, PerkType perkType) =>
        _host.PerkLevels.GetValueOrDefault((creature, perkType));

    protected override void TogglePlayerWindow(uint player, GuiWindowType windowType, GuiPayloadBase payload, uint tetherObject) =>
        _host.RecordWindow(player, windowType, payload, tetherObject);
}
