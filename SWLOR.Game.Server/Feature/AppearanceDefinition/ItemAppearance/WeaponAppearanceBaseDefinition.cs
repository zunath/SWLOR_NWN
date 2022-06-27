namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    public abstract class WeaponAppearanceBaseDefinition : IWeaponAppearanceDefinition
    {
        public abstract bool IsSimple { get; }
        public virtual int[] SimpleParts { get; } = { };
        public virtual int[] TopParts { get; } = { };
        public virtual int[] MiddleParts { get; } = { }; 
        public virtual int[] BottomParts { get; } = { };
    }
}
