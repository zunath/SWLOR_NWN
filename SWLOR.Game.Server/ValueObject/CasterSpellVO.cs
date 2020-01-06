using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.ValueObject
{
    public class CasterSpellVO
    {
        public NWCreature Caster { get; set; }
        public NWObject Target { get; set; }
        public string EffectName { get; set; }
        public CustomEffectType CustomEffectID { get; set; }
        public int EffectiveLevel { get; set; }
        public string Data { get; set; }
    }

}
