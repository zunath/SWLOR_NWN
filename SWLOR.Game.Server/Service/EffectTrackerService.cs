using System.Collections.Generic;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWN.NWScript;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class EffectTrackerService : IEffectTrackerService
    {
        private readonly INWScript _;
        private readonly AppState _state;

        public EffectTrackerService(INWScript script, AppState state)
        {
            _ = script;
            _state = state;
        }

        public void ProcessPCEffects(NWPlayer oPC)
        {
            HashSet<string> foundIDs = new HashSet<string>();

            foreach (Effect effect in oPC.Effects)
            {
                string pcUUID = oPC.GlobalID;
                string effectKey = pcUUID + "_" + _.GetEffectType(effect);

                if (_.GetEffectDurationType(effect) != NWScript.DURATION_TYPE_PERMANENT) continue;
                if (!string.IsNullOrWhiteSpace(_.GetEffectTag(effect))) continue;

                int ticks = _state.EffectTicks.ContainsKey(effectKey) ? _state.EffectTicks[effectKey] : 40;
                ticks--;

                if (ticks <= 0)
                {
                    _.RemoveEffect(oPC.Object, effect);
                    _state.EffectTicks.Remove(effectKey);
                }
                else
                {
                    foundIDs.Add(effectKey);
                    _state.EffectTicks[effectKey] = ticks;
                }
            }

            foreach (var effect in _state.EffectTicks)
            {
                if (!foundIDs.Contains(effect.Key))
                {
                    _state.EffectTicks.Remove(effect.Key);
                }
            }
        }
}
}
