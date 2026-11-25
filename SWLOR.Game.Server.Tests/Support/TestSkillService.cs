using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The skill instance with its calls into unconverted code answered by the test. The seams are split into partial files by the part
/// whose code calls each one (<c>Readmes/MaintainabilityRefactorCharacterCompanionsSplit.md</c>): <c>TestSkillService.Catalog.cs</c>.
/// The part accessors name the service whose code a test reaches; <see cref="XP"/> is the <see cref="TestSkillXPService"/> built over
/// this instance, the others are this one instance until the part moves, and the part's pull request changes only the type and the
/// body of its accessor.
/// </summary>
public sealed partial class TestSkillService : SkillService
{
    public TestSkillService(IDBService db, IGuiService gui, IPlayerFeedbackService feedback, ITwinPistolEquipmentService twin)
        : base(db, twin, ScriptedProxy.Create<ICompanionLookup>(out _))
    {
        XP = new TestSkillXPService(db, gui, feedback, this);
    }

    /// <summary>The service with every seam at its default body (the original calls), for the test that proves each one runs without a server.</summary>
    public static SkillService CreateWithDefaultSeams(IDBService db) =>
        new(db, ScriptedProxy.Create<ITwinPistolEquipmentService>(out _), ScriptedProxy.Create<ICompanionLookup>(out _));

    /// <summary>The catalog tables, the chart reads and the weapon skill mappings (the stay-part).</summary>
    public TestSkillService Catalog => this;

    /// <summary>The rank limit, ceiling, decay and distribution rules.</summary>
    public TestSkillService Rank => this;

    /// <summary>The XP award path, the formulas and the limit warnings.</summary>
    public TestSkillXPService XP { get; }
}
