using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;


namespace SWLOR.Game.Server.Trigger
{
    public class AnimationExplode : IRegisteredEvent
    {
        private readonly INWScript _;

        public AnimationExplode(INWScript script)
        {
            _ = script;
        }

        public bool Run(params object[] args)
        {
            

            NWPlayer player = _.GetEnteringObject();
            if (_.GetIsPC(player) && _.GetLocalInt(Object.OBJECT_SELF, "done") = 0)
            {
                NWObject oP1c1 = _.GetNearestObjectByTag("InvisibleObject", NWScript.OBJECT_SELF);
                NWScript.EFFECT_TYPE_VISUALEFFECT eTremble = _.EffectVisualEffect(VFX_FNF_SCREEN_SHAKE);
                NWScript.EFFECT_TYPE_VISUALEFFECT evis1 = _.EffectVisualEffect(VFX_FNF_ELECTRIC_EXPLOSION);
                NWScript.EFFECT_TYPE_VISUALEFFECT eVis2 = _.EffectVisualEffect(VFX_FNF_FIREBALL);

                _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, evis1, oP1c1);
                _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, eVis2, oP1c1);
                _.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY.CompareTo eTremble, player, 3.0);
                _.SetLocalInt(NWScript.OBJECT_SELF, "done", 1);

            }
            return true;
        }
    }
}
`