using System;

namespace SWLOR.Game.Server.Service.CombatService
{
    public static class WeaponImpactDamagePartition
    {
        public static int Partition(int damage, int index, int count)
        {
            if (count < 1 || index < 0 || index >= count)
                throw new ArgumentOutOfRangeException(nameof(index));
            damage = Math.Max(0, damage);
            var totalWeight = (long)count * (count + 1L) / 2;
            var previousWeight = (long)index * (index + 1L) / 2;
            var currentWeight = (index + 1L) * (index + 2L) / 2;
            return (int)((decimal)damage * currentWeight / totalWeight) -
                   (int)((decimal)damage * previousWeight / totalWeight);
        }
    }
}
