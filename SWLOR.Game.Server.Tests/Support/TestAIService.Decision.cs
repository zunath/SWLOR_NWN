#nullable enable
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

// Part AI-4, AIDecisionService: the effect pre-check and the attackable target.
public sealed partial class TestAIService
{
    protected override bool MayHaveScriptType(uint creature, EffectTypeScript effectType) => MayHaveEffect;

    /// <summary>The context reads the static enmity tables for its target, so they are made to hold this model first.</summary>
    protected override void UseAttackableEnmityTarget(AIContext context) => RealEnmityTables.Replace(Tables);
}
