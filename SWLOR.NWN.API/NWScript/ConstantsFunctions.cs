namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScriptService
    {
        /// <summary>
        /// Invalid object constant.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public const uint OBJECT_INVALID = 0x7F000000;

        /// <summary>
        /// Number of inventory slots available.
        /// </summary>
        public int NumberOfInventorySlots => 18;
        
        /// <summary>
        /// Mathematical constant Pi.
        /// </summary>
        public float PI => 3.141592f;
        
        /// <summary>
        /// Event constant for spell cast at.
        /// </summary>
        public int EVENT_SPELL_CAST_AT => 1011;
        
        /// <summary>
        /// Invalid portrait constant.
        /// </summary>
        public int PORTRAIT_INVALID => 65535;

        /// <summary>
        /// Use creature level constant.
        /// </summary>
        public const int USE_CREATURE_LEVEL = 0;

        //  The following resrefs must match those in the tileset's set file.
        public string TILESET_RESREF_BEHOLDER_CAVES => "tib01";
        public string TILESET_RESREF_CASTLE_INTERIOR => "tic01";
        public string TILESET_RESREF_CITY_EXTERIOR => "tcn01";
        public string TILESET_RESREF_CITY_INTERIOR => "tin01";
        public string TILESET_RESREF_CRYPT => "tdc01";
        public string TILESET_RESREF_DESERT => "ttd01";
        public string TILESET_RESREF_DROW_INTERIOR => "tid01";
        public string TILESET_RESREF_DUNGEON => "tde01";
        public string TILESET_RESREF_FOREST => "ttf01";
        public string TILESET_RESREF_FROZEN_WASTES => "tti01";
        public string TILESET_RESREF_ILLITHID_INTERIOR => "tii01";
        public string TILESET_RESREF_MICROSET => "tms01";
        public string TILESET_RESREF_MINES_AND_CAVERNS => "tdm01";
        public string TILESET_RESREF_RUINS => "tdr01";
        public string TILESET_RESREF_RURAL => "ttr01";
        public string TILESET_RESREF_RURAL_WINTER => "tts01";
        public string TILESET_RESREF_SEWERS => "tds01";
        public string TILESET_RESREF_UNDERDARK => "ttu01";
        public int EVENT_HEARTBEAT => 1001;
        public int EVENT_PERCEIVE => 1002;
        public int EVENT_END_COMBAT_ROUND => 1003;
        public int EVENT_DIALOGUE => 1004;
        public int EVENT_ATTACKED => 1005;
        public int EVENT_DAMAGED => 1006;
        public int EVENT_DISTURBED => 1008;
    }
}