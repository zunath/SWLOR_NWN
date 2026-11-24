using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The skill instance with its calls into unconverted code answered by the test. The seams are split into partial files by the part
/// whose code calls each one (<c>Readmes/MaintainabilityRefactorCharacterCompanionsSplit.md</c>): <c>TestSkillService.Catalog.cs</c>
/// and <c>TestSkillService.XP.cs</c>. The part accessors name the service whose code a test reaches; each is this one instance until
/// the part moves, and the part's pull request changes only the type and the body of its accessor.
/// </summary>
public sealed partial class TestSkillService : SkillService
{
    public TestSkillService(IDBService db, IGuiService gui, IPlayerFeedbackService feedback, ITwinPistolEquipmentService twin)
        : base(db, gui, feedback, twin, FacadeForwarder.LazyFor<IStatService>(), ScriptedProxy.Create<ICompanionLookup>(out _))
    {
    }

    /// <summary>The service with every seam at its default body (the original calls), for the test that proves each one runs without a server.</summary>
    public static SkillService CreateWithDefaultSeams(IDBService db) =>
        new(db, ScriptedProxy.Create<IGuiService>(out _), ScriptedProxy.Create<IPlayerFeedbackService>(out _),
            ScriptedProxy.Create<ITwinPistolEquipmentService>(out _), FacadeForwarder.LazyFor<IStatService>(),
            ScriptedProxy.Create<ICompanionLookup>(out _));

    /// <summary>The catalog tables, the chart reads and the weapon skill mappings (the stay-part).</summary>
    public TestSkillService Catalog => this;

    /// <summary>The rank limit, ceiling, decay and distribution rules.</summary>
    public TestSkillService Rank => this;

    /// <summary>The XP award path, the formulas and the limit warnings.</summary>
    public TestSkillService XP => this;
}
