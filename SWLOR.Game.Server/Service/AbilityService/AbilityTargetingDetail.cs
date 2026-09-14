using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.AbilityService
{
    public delegate float AbilityTargetingSizeResolver(uint creature, float baseSize);

    public sealed class AbilityTargetingDetail
    {
        public AbilityTargetingDetail(
            Spell spell,
            AbilityTargetingShapeType shape,
            float sizeX,
            float sizeY,
            AbilityTargetingFlags flags,
            AbilityTargetingSizeResolver sizeResolver,
            bool updatesClientTargeting = true)
        {
            Spell = spell;
            Shape = shape;
            SizeX = sizeX;
            SizeY = sizeY;
            Flags = flags;
            SizeResolver = sizeResolver;
            UpdatesClientTargeting = updatesClientTargeting;
        }

        public Spell Spell { get; }
        public AbilityTargetingShapeType Shape { get; }
        public float SizeX { get; }
        public float SizeY { get; }
        public AbilityTargetingFlags Flags { get; }
        public AbilityTargetingSizeResolver SizeResolver { get; }
        public bool UpdatesClientTargeting { get; }

        public float ResolveSizeX(uint creature, bool appliesDynamicSize)
        {
            return appliesDynamicSize && SizeResolver != null
                ? SizeResolver(creature, SizeX)
                : SizeX;
        }

        public float ResolveSizeY()
        {
            return SizeY;
        }
    }
}
