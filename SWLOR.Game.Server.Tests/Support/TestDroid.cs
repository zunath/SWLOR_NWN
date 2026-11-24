#nullable enable
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.UI;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The droid instance, built either over a <see cref="DroidWorld"/> (the neighbours are its recording stand-ins) or over the facade forwarders
/// with a scripted window service. The seam of the code that reads the generated ammunition is in <c>TestDroid.Inventory.cs</c>, with the part
/// whose code calls it, so a part that leaves <see cref="DroidService"/> takes its file with it. This file is the stay-part: the controller data
/// and the constructed droid.
/// <para>
/// <see cref="Droids"/>, <see cref="Spawn"/>, <see cref="Inventory"/>, <see cref="Events"/>, <see cref="Despawn"/> and <see cref="Restore"/> are the
/// part accessors of the split (DR-1 to DR-6): each is the one instance until its part moves, and a test calls a member on the accessor of the part
/// that owns it, so the pull request that moves the part changes what the accessor returns and no test.
/// </para>
/// </summary>
public sealed partial class TestDroid : DroidService
{
    public TestDroid(DroidWorld world)
        : base(world.Ai, world.Recorder, world.Companion, world.Diagnostics, world.Gui, world.Item, world.Perk, world.Glove,
            world.Race, world.Recast, world.Resistance, world.Stat, world.StatusEffect, world.Twin)
    {
    }

    public TestDroid(IGuiService gui)
        : base(
            FacadeForwarder.For<IAIService>(),
            FacadeForwarder.For<ICombatRecorderService>(),
            FacadeForwarder.For<ICompanionControlService>(),
            FacadeForwarder.For<IDroidEquipmentDiagnosticsService>(),
            gui,
            FacadeForwarder.For<IItemService>(),
            FacadeForwarder.For<IPerkService>(),
            FacadeForwarder.For<IPowerGloveEquipmentService>(),
            FacadeForwarder.For<IRaceService>(),
            FacadeForwarder.For<IRecastService>(),
            FacadeForwarder.For<IResistanceService>(),
            FacadeForwarder.For<ICreatureStatService>(),
            FacadeForwarder.For<IStatusEffectApplicationService>(),
            FacadeForwarder.For<ITwinPistolEquipmentService>())
    {
    }

    /// <summary>DR-1, the stay-part: the controller data, the constructed droid and the item details.</summary>
    public TestDroid Droids => this;

    /// <summary>DR-2: summoning a droid.</summary>
    public TestDroid Spawn => this;

    /// <summary>DR-3: the inventory sync and the saved item snapshot.</summary>
    public TestDroid Inventory => this;

    /// <summary>DR-4: the droid event handlers, the assembly terminal and the appearance edit.</summary>
    public TestDroid Events => this;

    /// <summary>DR-5: despawn, the player exit and death.</summary>
    public TestDroid Despawn => this;

    /// <summary>DR-6: restoring the saved equipment, inventory and appearance.</summary>
    public TestDroid Restore => this;
}
