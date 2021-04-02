using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Player = SWLOR.Game.Server.Core.NWNX.Player;
using Skill = SWLOR.Game.Server.Service.Skill;

namespace SWLOR.Game.Server.Feature
{
    public static class CraftRefinery
    {
        private class OreDetail
        {
            public int RequiredLevel { get; }
            public string RefinedItemResref { get; }
            public int XPGranted { get; }


            public OreDetail(int requiredLevel, string refinedItemResref, int xpGranted)
            {
                RequiredLevel = requiredLevel;
                RefinedItemResref = refinedItemResref;
                XPGranted = xpGranted;
            }
        }

        private static readonly Dictionary<string, OreDetail> _ores = new Dictionary<string, OreDetail>
        {
            {"power_core", new OreDetail(1, string.Empty, 10)},
            {"raw_veldite", new OreDetail(1, "ref_veldite", 25)},
            {"raw_scordspar", new OreDetail(2, "ref_scordspar", 50)},
            {"raw_plagionite", new OreDetail(3, "ref_plagionite", 75)},
            {"raw_keromber", new OreDetail(4, "ref_keromber", 100)},
            {"raw_jasioclase", new OreDetail(5, "ref_jasioclase", 125)},
            {"raw_hemorgite", new OreDetail(99, "ref_hemorgite", 150)},
            {"raw_ochne", new OreDetail(99, "ref_ochne", 175)},
            {"raw_croknor", new OreDetail(99, "ref_croknor", 200)},
            {"raw_arkoxit", new OreDetail(99, "ref_arkoxit", 225)},
            {"raw_bisteiss", new OreDetail(99, "ref_bisteiss", 250)},
        };

        /// <summary>
        /// When an item is added to the refinery, run the proper procedure for the inserted item.
        /// </summary>
        [NWNEventHandler("refinery_dist")]
        public static void RefineryDisturbed()
        {
            var player = GetLastDisturbed();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var item = GetInventoryDisturbItem();
            var itemResref = GetResRef(item);
            var type = GetInventoryDisturbType();
            var refinery = OBJECT_SELF;
            var refineryPosition = GetPosition(refinery);
            var refineryFacing = GetFacing(refinery);
            var area = GetArea(refinery);

            if (type == DisturbType.Added)
            {
                // Not a valid item.
                if (!_ores.ContainsKey(itemResref))
                {
                    Item.ReturnItem(player, item);
                    SendMessageToPC(player, "Only power cores and raw materials may be placed inside the refinery.");
                    return;
                }

                // Player doesn't have prerequisite level.
                var perkLevel = Perk.GetEffectivePerkLevel(player, PerkType.Refining);
                if(perkLevel < _ores[itemResref].RequiredLevel)
                {
                    Item.ReturnItem(player, item);
                    SendMessageToPC(player, $"Your Refining perk level must be at least {_ores[itemResref].RequiredLevel} to refine that item.");
                    return;
                }

                // Refinery needs power.
                var activeCharges = GetLocalInt(refinery, "REFINERY_CHARGES");
                if (activeCharges <= 0 &&
                   itemResref != "power_core")
                {
                    Item.ReturnItem(player, item);
                    SendMessageToPC(player, "This refinery needs power. Insert a Power Core to turn it on.");
                    return;
                }

                // Are they already refining?
                if (GetLocalBool(player, "IS_REFINING"))
                {
                    Item.ReturnItem(player, item);
                    SendMessageToPC(player, "You are busy.");
                    return;
                }

                var refinedItem = _ores[itemResref];
                // Inserted a power core - give +18 seconds plus whatever Refinery Management gives as a bonus.
                if (itemResref == "power_core")
                {
                    var refineryManagement = Perk.GetEffectivePerkLevel(player, PerkType.RefineryManagement);
                    var bonusCharges = 3 + CalculateBonusPowerCharges(refineryManagement);
                    activeCharges += bonusCharges;

                    // Clamp active charges to last no longer than ten minutes.
                    if (activeCharges > 100)
                    {
                        activeCharges = 100;
                    }

                    SetLocalInt(refinery, "REFINERY_CHARGES", activeCharges);

                    var flames = GetLocalObject(refinery, "REFINERY_FLAMES");
                    if (!GetIsObjectValid(flames))
                    {
                        var flamePosition = BiowarePosition.GetChangedPosition(refineryPosition, 0.36f, refineryFacing);
                        var flameLocation = Location(area, flamePosition, 0.0f);
                        flames = CreateObject(ObjectType.Placeable, "forge_flame", flameLocation);
                        SetLocalObject(refinery, "REFINERY_FLAMES", flames);

                        Skill.GiveSkillXP(player, SkillType.Gathering, refinedItem.XPGranted);
                    }
                }
                // Inserted one of the raw ores - give a refined item in a few seconds.
                else
                {
                    const float DelaySeconds = 8.0f;

                    // Flag player as refining so that they can't queue up another item at the same time.
                    SetLocalBool(player, "IS_REFINING", true);

                    // Apply immobilization
                    var effect = EffectCutsceneImmobilize();
                    effect = TagEffect(effect, "REFINING_EFFECT");
                    ApplyEffectToObject(DurationType.Temporary, effect, player, DelaySeconds);

                    // Play an animation
                    AssignCommand(player, () => ActionPlayAnimation(Animation.LoopingGetMid, 1.0f, DelaySeconds));

                    // Display the timing bar and finish the process when the delay elapses.
                    Player.StartGuiTimingBar(player, DelaySeconds);
                    DelayCommand(DelaySeconds, () =>
                    {
                        // Spawn the refined item onto the player.
                        CreateItemOnObject(refinedItem.RefinedItemResref, player);
                        DeleteLocalBool(player, "IS_REFINING");

                        Skill.GiveSkillXP(player, SkillType.Gathering, refinedItem.XPGranted);
                    });
                }

                DestroyObject(item);
            }
        }

        /// <summary>
        /// When the player enters the server, remove any temporary effects or local variables related to the refining process.
        /// </summary>
        [NWNEventHandler("mod_enter")]
        public static void PlayerEnterServer()
        {
            var player = GetEnteringObject();

            RemoveEffectByTag(player, "REFINING_EFFECT");
            DeleteLocalBool(player, "IS_REFINING");
        }

        /// <summary>
        /// Determines the number of bonus charges each level of Refinery Management grants.
        /// Each charge represents six seconds.
        /// </summary>
        /// <param name="refineryManagementLevel">The level of refinery management.</param>
        /// <returns>The number of charges. If outside of range 1-6, zero will be returned.</returns>
        private static int CalculateBonusPowerCharges(int refineryManagementLevel)
        {
            switch (refineryManagementLevel)
            {
                case 1:
                    return 2;
                case 2:
                    return 3;
                case 3:
                    return 4;
                case 4:
                    return 6;
                case 5:
                    return 8;
                case 6:
                    return 10;
            }

            return 0;
        }

        /// <summary>
        /// When the refinery's heartbeat ticks, process the power core lifespan.
        /// </summary>
        [NWNEventHandler("refinery_hb")]
        public static void RefineryHeartbeat()
        {
            var refinery = OBJECT_SELF;
            var charges = GetLocalInt(refinery, "REFINERY_CHARGES");

            if (charges > 0)
            {
                charges--;
                SetLocalInt(refinery, "REFINERY_CHARGES", charges);
            }
            else
            {
                var flames = GetLocalObject(refinery, "REFINERY_FLAMES");
                DestroyObject(flames);
            }
        }


    }
}
