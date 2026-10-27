using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Characterization.Support;

/// <summary>Fixed creatures for scenarios: stock humanoid blueprints, held still so only the scenario acts on them.</summary>
public static class ScenarioActors
{
    /// <summary>The stock humanoid that both sides of every scenario are built from.</summary>
    public const string Humanoid = "nw_bandit001";

    /// <summary>
    /// Spawns a creature and removes everything that could act on its own: AI decisions, heartbeat regeneration and
    /// perception reactions. Its hit points are set to a known pool so a damage-over-time scenario never kills it.
    /// </summary>
    public static async Task<uint> SpawnQuietAsync(EngineTestContext ctx, string resref, float xOffset, float yOffset,
        int maximumHitPoints = 2000)
    {
        var creature = ctx.SpawnCreature(resref, xOffset, yOffset);
        // Spawn initialization scripts reset stats and resources; let them finish before the scenario configures anything.
        await ctx.DelaySecondsAsync(1f);
        await ctx.ExecuteInCreatureContextAsync(creature, () =>
        {
            SetAILevel(creature, AILevel.VeryLow);
            SetEventScript(creature, EventScript.Creature_OnHeartbeat, "");
            SetEventScript(creature, EventScript.Creature_OnNotice, "");
            SetEventScript(creature, EventScript.Creature_OnMeleeAttacked, "");
            SetEventScript(creature, EventScript.Creature_OnEndCombatRound, "");
            SetEventScript(creature, EventScript.Creature_OnSpellCastAt, "");
            ClearAllActions();
        });
        ctx.SuppressNPCNaturalRegen(creature);
        Stat.SetNPCMaxHitPoints(creature, maximumHitPoints, true);
        return creature;
    }

    /// <summary>Makes the creature a hostile training dummy: hostile to the scenario's attacker, unable to move or act.</summary>
    public static async Task MakeDummyAsync(EngineTestContext ctx, uint creature)
    {
        ctx.MakeHostile(creature);
        await ctx.ExecuteInCreatureContextAsync(creature, () =>
        {
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneImmobilize(), creature, 3600f);
            SetCommandable(false, creature);
        });
    }
}
