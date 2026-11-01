using System.Collections.Generic;
using SWLOR.Game.Server.Feature.ShipRewardDefinition;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.SpaceObjectDefinition
{
    public class NPCSpaceObjectDefinition : ISpaceObjectListDefinition
    {
        private readonly SpaceObjectBuilder _builder = new();

        public Dictionary<string, SpaceObjectDetail> BuildSpaceObjects()
        {
            Tier1();
            Tier2();
            Tier3();
            Tier4();
            Tier5();
            RoleShips();
            TradeRiskShips();
            Aces();
            RiftRaiders();
            IncursionArmadas();

            return _builder.Build();
        }

        /// <summary>
        /// The Sith and Mandalorian Armadas (incursions and capital battles, plan sections 11 and 30): each invader's copy of
        /// the tier's role ships. Hull and weapons come from RevampNpcShipCatalog; the legacy ship item is the source ship's.
        /// </summary>
        private void IncursionArmadas()
        {
            foreach (var (tag, itemTag) in IncursionTuning.ArmadaShips)
            {
                _builder.Create(tag)
                    .ItemTag(itemTag);
            }
        }

        /// <summary>
        /// The aces (plan section 11): one named rare-spawn pilot per orbit and zone. Hull and weapons come from
        /// RevampNpcShipCatalog; the legacy ship item is the tier's ship of the ace's role.
        /// </summary>
        private void Aces()
        {
            foreach (var ace in RevampAceCatalog.Aces)
            {
                _builder.Create(ace.Tag)
                    .ItemTag(ace.ShipItemTag);
            }
        }

        private void Tier1()
        {
            _builder.Create("t1bomber")
                .ItemTag("NPC_Bomber1");

            _builder.Create("t1fighter")
                .ItemTag("NPC_Fighter1");

            _builder.Create("t1interceptor")
                .ItemTag("NPC_Interceptor1");

            _builder.Create("t1gunship")
                .ItemTag("NPC_Gunship1");

            _builder.Create("t1platform")
                .ItemTag("NPC_Platform1");

            _builder.Create("t1cargo")
                .ItemTag("NPC_Cargo1");

            _builder.Create("t1capital")
                .ItemTag("NPC_Capital1");
        }

        private void Tier2()
        {
            _builder.Create("t2bomber")
                .ItemTag("NPC_Bomber2");

            _builder.Create("t2fighter")
                .ItemTag("NPC_Fighter2");

            _builder.Create("t2interceptor")
                .ItemTag("NPC_Interceptor2");

            _builder.Create("t2gunship")
                .ItemTag("NPC_Gunship2");

            _builder.Create("t2platform")
                .ItemTag("NPC_Platform2");

            _builder.Create("t2cargo")
                .ItemTag("NPC_Cargo2");

            _builder.Create("t2capital")
                .ItemTag("NPC_Capital2");
        }

        private void Tier3()
        {
            _builder.Create("t3bomber")
                .ItemTag("NPC_Bomber3");

            _builder.Create("t3fighter")
                .ItemTag("NPC_Fighter3");

            _builder.Create("t3interceptor")
                .ItemTag("NPC_Interceptor3");

            _builder.Create("t3gunship")
                .ItemTag("NPC_Gunship3");

            _builder.Create("t3platform")
                .ItemTag("NPC_Platform3");

            _builder.Create("t3cargo")
                .ItemTag("NPC_Cargo3");

            _builder.Create("t3capital")
                .ItemTag("NPC_Capital3");
        }

        private void Tier4()
        {
            _builder.Create("t4bomber")
                .ItemTag("NPC_Bomber4");

            _builder.Create("t4fighter")
                .ItemTag("NPC_Fighter4");

            _builder.Create("t4interceptor")
                .ItemTag("NPC_Interceptor4");

            _builder.Create("t4gunship")
                .ItemTag("NPC_Gunship4");

            _builder.Create("t4platform")
                .ItemTag("NPC_Platform4");

            _builder.Create("t4cargo")
                .ItemTag("NPC_Cargo4");

            _builder.Create("t4capital")
                .ItemTag("NPC_Capital4");
        }

        private void Tier5()
        {
            _builder.Create("t5bomber")
                .ItemTag("NPC_Bomber5");

            _builder.Create("t5fighter")
                .ItemTag("NPC_Fighter5");

            _builder.Create("t5interceptor")
                .ItemTag("NPC_Interceptor5");

            _builder.Create("t5gunship")
                .ItemTag("NPC_Gunship5");

            _builder.Create("t5platform")
                .ItemTag("NPC_Platform5");

            _builder.Create("t5cargo")
                .ItemTag("NPC_Cargo5");

            _builder.Create("t5capital")
                .ItemTag("NPC_Capital5");

            _builder.Create("t6bomber")
                .ItemTag("NPC_Bomber6");

            _builder.Create("t6fighter")
                .ItemTag("NPC_Fighter6");

            _builder.Create("t6interceptor")
                .ItemTag("NPC_Interceptor6");

            _builder.Create("t6gunship")
                .ItemTag("NPC_Gunship6");

            _builder.Create("t6platform")
                .ItemTag("NPC_Platform6");

            _builder.Create("t6cargo")
                .ItemTag("NPC_Cargo6");

            _builder.Create("mandocap1")
                .ItemTag("NPC_MandoCap1");

            _builder.Create("mandocap2")
                .ItemTag("NPC_MandoCap2");

            _builder.Create("mandocap3")
                .ItemTag("NPC_MandoCap3");

            _builder.Create("sithcap1")
                .ItemTag("NPC_SithCap1");

            _builder.Create("sithcap2")
                .ItemTag("NPC_SithCap2");

            _builder.Create("sithcap3")
                .ItemTag("NPC_SithCap3");

            _builder.Create("sithcap4")
                .ItemTag("NPC_SithCap4");

            _builder.Create("sithcap5")
                .ItemTag("NPC_SithCap5");

            _builder.Create("sithcap6")
                .ItemTag("NPC_SithCap6");

            _builder.Create("sithcap7")
                .ItemTag("NPC_SithCap7");
        }

        /// <summary>
        /// The role ships of every faction tier (electronic warfare, sniper, minelayer, logistics and carrier). Their
        /// hull, weapons and support modules come from RevampNpcShipCatalog, so they list no legacy modules.
        /// </summary>
        private void RoleShips()
        {
            _builder.Create("t1ewar")
                .ItemTag("NPC_Interceptor1");

            _builder.Create("t1sniper")
                .ItemTag("NPC_Fighter1");

            _builder.Create("t1minelayer")
                .ItemTag("NPC_Bomber1");

            _builder.Create("t1logistics")
                .ItemTag("NPC_Gunship1");

            _builder.Create("t1carrier")
                .ItemTag("NPC_Gunship1");

            _builder.Create("t2ewar")
                .ItemTag("NPC_Interceptor2");

            _builder.Create("t2sniper")
                .ItemTag("NPC_Fighter2");

            _builder.Create("t2minelayer")
                .ItemTag("NPC_Bomber2");

            _builder.Create("t2logistics")
                .ItemTag("NPC_Gunship2");

            _builder.Create("t2carrier")
                .ItemTag("NPC_Gunship2");

            _builder.Create("t3ewar")
                .ItemTag("NPC_Interceptor3");

            _builder.Create("t3sniper")
                .ItemTag("NPC_Fighter3");

            _builder.Create("t3minelayer")
                .ItemTag("NPC_Bomber3");

            _builder.Create("t3logistics")
                .ItemTag("NPC_Gunship3");

            _builder.Create("t3carrier")
                .ItemTag("NPC_Gunship3");

            _builder.Create("t4ewar")
                .ItemTag("NPC_Interceptor4");

            _builder.Create("t4sniper")
                .ItemTag("NPC_Fighter4");

            _builder.Create("t4minelayer")
                .ItemTag("NPC_Bomber4");

            _builder.Create("t4logistics")
                .ItemTag("NPC_Gunship4");

            _builder.Create("t4carrier")
                .ItemTag("NPC_Gunship4");

            _builder.Create("t5ewar")
                .ItemTag("NPC_Interceptor5");

            _builder.Create("t5sniper")
                .ItemTag("NPC_Fighter5");

            _builder.Create("t5minelayer")
                .ItemTag("NPC_Bomber5");

            _builder.Create("t5logistics")
                .ItemTag("NPC_Gunship5");

            _builder.Create("t5carrier")
                .ItemTag("NPC_Gunship5");

            _builder.Create("t6ewar")
                .ItemTag("NPC_Interceptor6");

            _builder.Create("t6sniper")
                .ItemTag("NPC_Fighter6");

            _builder.Create("t6minelayer")
                .ItemTag("NPC_Bomber6");

            _builder.Create("t6logistics")
                .ItemTag("NPC_Gunship6");

            _builder.Create("t6carrier")
                .ItemTag("NPC_Gunship6");
        }

        /// <summary>
        /// The trade risk ships of every tier (plan sections 11 and 13): pirate raiders who ambush loaded ships, customs
        /// patrols of the lawful orbits and trade convoys. Hull, weapons and support modules come from RevampNpcShipCatalog.
        /// </summary>
        private void TradeRiskShips()
        {
            _builder.Create("t1pirint")
                .ItemTag("NPC_Interceptor1");

            _builder.Create("t1pirftr")
                .ItemTag("NPC_Fighter1");

            _builder.Create("t1pirgun")
                .ItemTag("NPC_Gunship1");

            _builder.Create("t2pirint")
                .ItemTag("NPC_Interceptor2");

            _builder.Create("t2pirftr")
                .ItemTag("NPC_Fighter2");

            _builder.Create("t2pirgun")
                .ItemTag("NPC_Gunship2");

            _builder.Create("t3pirint")
                .ItemTag("NPC_Interceptor3");

            _builder.Create("t3pirftr")
                .ItemTag("NPC_Fighter3");

            _builder.Create("t3pirgun")
                .ItemTag("NPC_Gunship3");

            _builder.Create("t4pirint")
                .ItemTag("NPC_Interceptor4");

            _builder.Create("t4pirftr")
                .ItemTag("NPC_Fighter4");

            _builder.Create("t4pirgun")
                .ItemTag("NPC_Gunship4");

            _builder.Create("t5pirint")
                .ItemTag("NPC_Interceptor5");

            _builder.Create("t5pirftr")
                .ItemTag("NPC_Fighter5");

            _builder.Create("t5pirgun")
                .ItemTag("NPC_Gunship5");

            _builder.Create("t6pirint")
                .ItemTag("NPC_Interceptor6");

            _builder.Create("t6pirftr")
                .ItemTag("NPC_Fighter6");

            _builder.Create("t6pirgun")
                .ItemTag("NPC_Gunship6");

            _builder.Create("t1repcusgun")
                .ItemTag("NPC_Gunship1");

            _builder.Create("t1repcusint")
                .ItemTag("NPC_Interceptor1");

            _builder.Create("t1czkcusgun")
                .ItemTag("NPC_Gunship1");

            _builder.Create("t1czkcusint")
                .ItemTag("NPC_Interceptor1");

            _builder.Create("t2repcusgun")
                .ItemTag("NPC_Gunship2");

            _builder.Create("t2repcusint")
                .ItemTag("NPC_Interceptor2");

            _builder.Create("t5repcusgun")
                .ItemTag("NPC_Gunship5");

            _builder.Create("t5repcusint")
                .ItemTag("NPC_Interceptor5");

            _builder.Create("t6sithcusgun")
                .ItemTag("NPC_Gunship6");

            _builder.Create("t6sithcusint")
                .ItemTag("NPC_Interceptor6");

            _builder.Create("t1convoy")
                .ItemTag("NPC_Cargo1");

            _builder.Create("t1escort")
                .ItemTag("NPC_Gunship1");

            _builder.Create("t2convoy")
                .ItemTag("NPC_Cargo2");

            _builder.Create("t2escort")
                .ItemTag("NPC_Gunship2");

            _builder.Create("t3convoy")
                .ItemTag("NPC_Cargo3");

            _builder.Create("t3escort")
                .ItemTag("NPC_Gunship3");

            _builder.Create("t4convoy")
                .ItemTag("NPC_Cargo4");

            _builder.Create("t4escort")
                .ItemTag("NPC_Gunship4");

            _builder.Create("t5convoy")
                .ItemTag("NPC_Cargo5");

            _builder.Create("t5escort")
                .ItemTag("NPC_Gunship5");

            _builder.Create("t6convoy")
                .ItemTag("NPC_Cargo6");

            _builder.Create("t6escort")
                .ItemTag("NPC_Gunship6");

            // Hutt Cartel enforcers on the Smuggler's Moon lanes (W11b): tier 4, Neutral until a pilot crosses the Cartel.
            _builder.Create("t4huttenfgun")
                .ItemTag("NPC_Gunship4");

            _builder.Create("t4huttenfint")
                .ItemTag("NPC_Interceptor4");
        }

        /// <summary>
        /// The Rift Raiders (Wild Space, tier 5.5, plan section 23): every W5b role and the pirate hulls at level 52. Hull,
        /// weapons and support modules come from RevampNpcShipCatalog; the legacy ship item is the tier 5 ship of the role.
        /// </summary>
        private void RiftRaiders()
        {
            _builder.Create("rrfighter")
                .ItemTag("NPC_Fighter5");

            _builder.Create("rrinterceptor")
                .ItemTag("NPC_Interceptor5");

            _builder.Create("rrbomber")
                .ItemTag("NPC_Bomber5");

            _builder.Create("rrsniper")
                .ItemTag("NPC_Fighter5");

            _builder.Create("rrewar")
                .ItemTag("NPC_Interceptor5");

            _builder.Create("rrminelayer")
                .ItemTag("NPC_Bomber5");

            _builder.Create("rrgunship")
                .ItemTag("NPC_Gunship5");

            _builder.Create("rrlogistics")
                .ItemTag("NPC_Gunship5");

            _builder.Create("rrcarrier")
                .ItemTag("NPC_Gunship5");
        }
    }
}
