using System;

namespace SWLOR.Game.Server.Service.StatusEffectService
{
    public delegate void StatusEffectAppliedDelegate(uint source, uint target, float length);
    public delegate void StatusEffectTickDelegate(uint source, uint target);
    public delegate void StatusEffectRemovedDelegate(uint target);
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
