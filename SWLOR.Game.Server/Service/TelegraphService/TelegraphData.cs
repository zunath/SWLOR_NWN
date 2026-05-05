using System.Numerics;

namespace SWLOR.Game.Server.Service.TelegraphService
{
    public class TelegraphData
    {
        public uint Creator { get; set; }
        public TelegraphType Shape { get; set; }
        public Vector3 Position { get; set; }
        public float Rotation { get; set; }
        public Vector2 Size { get; set; }
        public float Duration { get; set; }
        public bool IsHostile { get; set; }
        public ApplyTelegraphEffect Action { get; set; }
    }
}
