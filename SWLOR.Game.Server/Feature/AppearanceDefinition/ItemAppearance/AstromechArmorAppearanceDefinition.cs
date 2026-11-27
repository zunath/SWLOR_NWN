namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    /// <summary>
    /// Astromech modules have no models of their own, so a worn module only brings its dyes. Every part is style
    /// 0, which draws the chassis part underneath. The torso is the plating number (AstromechBody.PlatingTorso):
    /// no body has a model under it either, and it gives Chassis Plating its inventory icon.
    /// </summary>
    public class AstromechArmorAppearanceDefinition : BaseArmorAppearanceDefinition
    {
        public override int[] Helmet { get; } = { 0 };
        public override int[] Cloak { get; } = { 0 };
        public override int[] Neck { get; } = { 0 };
        public override int[] Torso { get; } = { 240 };
        public override int[] Belt { get; } = { 0 };
        public override int[] Pelvis { get; } = { 0 };
        public override int[] Shoulder { get; } = { 0 };
        public override int[] Bicep { get; } = { 0 };
        public override int[] Forearm { get; } = { 0 };
        public override int[] Hand { get; } = { 0 };
        public override int[] Thigh { get; } = { 0 };
        public override int[] Shin { get; } = { 0 };
        public override int[] Foot { get; } = { 0 };
        public override int[] Robe { get; } = { 0 };
    }
}
