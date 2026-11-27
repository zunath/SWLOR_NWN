namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// How Trait Potency raises an equipped Mimicry trait's amounts. Pure, so the trait stat store (Character) and the Mimicry
    /// service's resistance read (Abilities) share it.
    /// </summary>
    public static class MimicryTraitRules
    {
        /// <summary>Raises a trait amount by the potency percent, rounded up.</summary>
        public static int ScaleTraitBonus(int amount, int potency)
        {
            return amount + (int)Math.Ceiling(amount * (potency / 100f));
        }
    }
}
