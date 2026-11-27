using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The skill instance with its calls into unconverted code answered by the test. The seams are split into partial files by the part
/// whose code calls each one (<c>Readmes/MaintainabilityRefactorCharacterCompanionsSplit.md</c>): <c>TestSkillService.Catalog.cs</c>.
/// The part accessors name the service whose code a test reaches; <see cref="Rank"/> is the <see cref="SkillRankService"/> and <see cref="XP"/>
/// the <see cref="TestSkillXPService"/>, both built over this instance, and the catalog is this one.
/// </summary>
public sealed partial class TestSkillService : SkillService
{
    public TestSkillService(IDBService db, IGuiService gui, IPlayerFeedbackService feedback, ITwinPistolEquipmentService twin)
        : base(db, twin, ScriptedProxy.Create<ICompanionLookup>(out _))
    {
        Rank = new SkillRankService(db, this);
        XP = new TestSkillXPService(db, gui, feedback, this, Rank);
    }

    /// <summary>The service with every seam at its default body (the original calls), for the test that proves each one runs without a server.</summary>
    public static SkillService CreateWithDefaultSeams(IDBService db) =>
        new(db, ScriptedProxy.Create<ITwinPistolEquipmentService>(out _), ScriptedProxy.Create<ICompanionLookup>(out _));

    /// <summary>The catalog tables, the chart reads and the weapon skill mappings (the stay-part).</summary>
    public TestSkillService Catalog => this;

    /// <summary>The rank limit, ceiling, decay and distribution rules.</summary>
    public SkillRankService Rank { get; }

    /// <summary>The XP award path, the formulas and the limit warnings.</summary>
    public TestSkillXPService XP { get; }
}
