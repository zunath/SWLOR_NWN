using System;

namespace SWLOR.Game.Server.Service.StatusEffectService
{
    public delegate void StatusEffectAppliedDelegate(uint source, uint target, float length, object effectData);
    public delegate void StatusEffectTickDelegate(uint source, uint target, object effectData);
    public delegate void StatusEffectRemovedDelegate(uint target, object effectData);
    public class StatusEffectDetail
    {
        public string Name { get; set; }
        public int EffectIconId { get; set; }
        public StatusEffectAppliedDelegate AppliedAction { get; set; }
        public StatusEffectRemovedDelegate RemoveAction { get; set; }
        public StatusEffectTickDelegate TickAction { get; set; }

        public StatusEffectDetail()
        {
            Name = string.Empty;
            EffectIconId = 0;
        }
    }
}
