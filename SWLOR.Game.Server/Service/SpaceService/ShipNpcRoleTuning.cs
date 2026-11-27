namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The numbers of the NPC ship role behaviors (plan section 11). The Space Enemies tab's Roles table and
    /// tools/SpaceRevampBalance (model.py NPC_ROLES, checks K) repeat them; change them together.
    /// </summary>
    public static class ShipNpcRoleTuning
    {
        /// <summary>
        /// Role decisions run on the one second ship AI tick, not the quarter second toggle loop.
        /// </summary>
        public const double DecisionSeconds = 1.0;

        /// <summary>Allies within the base command range (SpaceRadii.CommandRange) count as the ship's squad.</summary>
        public const float SquadRadius = SpaceRadii.CommandRange;

        // Tank (the Gunship role: "holds threat and hardens")

        /// <summary>
        /// The tank turns on the squad's top-damage player this often: the ground Provoke II recast.
        /// </summary>
        public const double TankRetargetSeconds = 12.0;

        /// <summary>
        /// The Shield Hardener goes on below half hull, the last quarter of hull + shield. The preset gives up
        /// a quarter of the hardener's resist for it (npc_catalog HARDENER_SHARE), so the fight length holds.
        /// </summary>
        public const float TankHardenBelowHullShare = 0.5f;

        // Logistics

        /// <summary>
        /// A repairer locks onto an ally once this share of its hull or shield is missing: the NPC repairer
        /// threshold (Space.NpcRepairThreshold).
        /// </summary>
        public const float RepairMissingShare = 0.25f;

        // Electronic warfare

        /// <summary>The jam: the ground Flash status's own default Accuracy penalty.</summary>
        public const int JamAccuracyPercent = 8;

        /// <summary>
        /// The paint: Exposed at -10% Defense, below the ground's -15% default, so a matched Fighter still wins
        /// at least 95% of fights against a Fighter and an EW ship (checks K).
        /// </summary>
        public const int PaintDefensePercent = 10;

        /// <summary>A jam or paint lasts four Ion Cannon cycles (3 s each).</summary>
        public const float EwarSeconds = 12f;

        /// <summary>One jam or paint per 1.5x its duration, the hard crowd-control cooldown rule (plan section 9).</summary>
        public const double EwarCooldownSeconds = EwarSeconds * 1.5;

        // Interceptor

        /// <summary>The web (Hamstring, -20% speed) lasts as long as an Ion Cannon's slow.</summary>
        public const float WebSeconds = 6f;

        /// <summary>One web per 1.5x its duration, the hard crowd-control cooldown rule.</summary>
        public const double WebCooldownSeconds = WebSeconds * 1.5;

        /// <summary>A ship moving away below half hull is fleeing (the tank's harden line).</summary>
        public const float FleeHullShare = 0.5f;

        /// <summary>A ship is moving away when it gained this many meters since the last decision.</summary>
        public const float FleeMinimumStepMeters = 1f;

        // Sniper

        /// <summary>A sniper holds at this share of its longest weapon's range, just inside it.</summary>
        public const float KiteRangeShare = 0.9f;

        // Carrier (a Boss: one Fighter cannot beat an Elite carrier with even one wing fighter out, checks K)

        /// <summary>
        /// One fighter per wave every 40 s: three Fighters still beat a Boss carrier with a wing fighter out the whole
        /// fight in 73-97% of fights (checks K); the W0 draft's 2 every 30 s dropped that to 20-52%.
        /// </summary>
        public const int WaveSize = 1;

        public const double WaveSeconds = 40.0;

        /// <summary>The W0 draft's four waves.</summary>
        public const int MaxWaves = 4;

        /// <summary>At most two waves fly at once.</summary>
        public const int MaxAliveFighters = WaveSize * 2;

        // Minelayer (Space Enemies, Roles: the W0 draft)

        public const double MineSeconds = 20.0;

        public const float MineTriggerMeters = 5f;

        /// <summary>Three mines at the 20 s cadence: each lasts a minute.</summary>
        public const int MaxMines = 3;

        public const double MineLifetimeSeconds = MineSeconds * MaxMines;

        /// <summary>Mines check for ships twice a second so a ship cannot fly through one between checks.</summary>
        public const double MineCheckSeconds = 0.5;
    }
}
