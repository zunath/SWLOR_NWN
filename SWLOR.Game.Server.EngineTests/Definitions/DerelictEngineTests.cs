using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Derelict boarding (plan section 28) in the engine: a derelict interior instanced from each starship template holds
    /// the tier's ground enemies, its slicing terminals, debris and the strongbox; the template's computer goes dead and
    /// its exit becomes the airlock.
    /// </summary>
    public static class DerelictEngineTests
    {
        [EngineTest("A derelict interior holds the tier's enemies, terminals and strongbox, with a dead computer and an airlock", Category = "Derelict", TimeoutSeconds = 60f)]
        public static async Task InteriorIsDressedAndStockedAtItsTier(EngineTestContext ctx)
        {
            for (var template = 0; template < DerelictTuning.TemplateResrefs.Length; template++)
            {
                var tierIndex = template + 1;
                var pick = template;
                var plan = DerelictRules.PlanInterior(tierIndex, max => max == DerelictTuning.TemplateResrefs.Length ? pick : 0);
                var boarding = new DerelictBoarding
                {
                    SignatureId = $"engine-derelict/{template}",
                    Mode = DerelictMode.Boarding,
                    TierIndex = tierIndex,
                    TemplateResref = plan.TemplateResref,
                    Name = plan.Name,
                };

                var interior = Space.CreateDerelictInterior(boarding, plan);
                try
                {
                    ctx.Assert(GetIsObjectValid(interior), $"{plan.TemplateResref} instances");
                    ctx.AssertEqual(plan.Name, GetName(interior), "the instance takes the derelict's name");
                    await ctx.WaitUntilAsync(() => boarding.Enemies.All(GetIsObjectValid), 5, "the enemies spawn");
                    ctx.AssertEqual(DerelictTuning.InteriorEnemies, boarding.Enemies.Count(GetIsObjectValid), "every enemy spawns");
                    ctx.Assert(boarding.Enemies.All(x => DerelictRules.Roster(tierIndex).Contains(GetResRef(x))), "the enemies are the tier's");
                    ctx.Assert(boarding.Enemies.All(x => GetArea(x) == interior), "the enemies are aboard");

                    var terminals = 0;
                    var airlockReady = false;
                    var computerDead = false;
                    for (var obj = GetFirstObjectInArea(interior); GetIsObjectValid(obj); obj = GetNextObjectInArea(interior))
                    {
                        if (GetResRef(obj) == DerelictRules.TerminalResref(tierIndex))
                            terminals++;
                        if (GetTag(obj) == "building_exit")
                            airlockReady = GetEventScript(obj, EventScript.Placeable_OnUsed) == ScriptName.OnDerelictAirlockUsed;
                        if (GetTag(obj) == "ShipComputer")
                            computerDead = !GetUseableFlag(obj);
                    }

                    ctx.AssertEqual(DerelictTuning.InteriorTerminals, terminals, "the slicing terminals of the tier");
                    ctx.Assert(airlockReady, "the exit is the airlock");
                    ctx.Assert(computerDead, "the ship computer is dead");
                    ctx.Assert(GetIsObjectValid(boarding.Strongbox) && GetArea(boarding.Strongbox) == interior, "the strongbox is aboard");
                    ctx.AssertEqual(ScriptName.OnDerelictStrongboxUsed, GetEventScript(boarding.Strongbox, EventScript.Placeable_OnUsed),
                        "the strongbox opens through its script");
                }
                finally
                {
                    if (GetIsObjectValid(interior))
                        DestroyArea(interior);
                }
            }
        }
    }
}
