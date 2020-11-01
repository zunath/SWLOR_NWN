using System;

namespace SWLOR.Game.Server.Service.StatusEffectService
{
    public class StatusEffectDetail
    {
        public string Name { get; set; }
        public int EffectIconId { get; set; }
        public Action<uint, float> GrantAction { get; set; }
        public Action<uint> RemoveAction { get; set; }
        public Action<uint, uint> TickAction { get; set; }

        public StatusEffectDetail()
        {
            Name = string.Empty;
            EffectIconId = 0;
        }
    }
}
