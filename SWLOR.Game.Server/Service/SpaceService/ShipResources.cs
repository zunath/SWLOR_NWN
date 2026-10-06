using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public enum ShipResource { Hull, Shield, Capacitor }

    public static class ShipResources
    {
        public static int Restore(ShipStatus status, ShipResource resource, int amount) => Change(status, resource, amount, false);
        public static int Spend(ShipStatus status, ShipResource resource, int amount) => Change(status, resource, amount, true);

        public static double Available(ShipStatus status, ShipResource resource) => Math.Max(0, (resource switch {
            ShipResource.Hull => status.Hull, ShipResource.Shield => status.Shield,
            ShipResource.Capacitor => status.Capacitor, _ => throw new ArgumentOutOfRangeException(nameof(resource))
        }) - status.FractionalResourceDeficits.GetValueOrDefault(resource));

        public static double RestorePrecise(ShipStatus status, ShipResource resource, double amount) => ChangePrecise(status, resource, amount, false);
        public static double SpendPrecise(ShipStatus status, ShipResource resource, double amount) => ChangePrecise(status, resource, amount, true);

        private static double ChangePrecise(ShipStatus status, ShipResource resource, double amount, bool spend)
        {
            ArgumentNullException.ThrowIfNull(status);
            if (!Enum.IsDefined(resource) || !double.IsFinite(amount) || amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            var deficits = status.ResourceDeficits ?? ShipResourceDeficits.Capture(status);
            var (maximum, integral) = resource switch {
                ShipResource.Hull => (status.MaxHull, deficits.HullDamage),
                ShipResource.Shield => (status.MaxShield, deficits.ShieldDamage),
                _ => (status.MaxCapacitor, deficits.CapacitorExpenditure) };
            var deficit = integral + status.FractionalResourceDeficits.GetValueOrDefault(resource);
            var alive = status.Hull > 0;
            var applied = Math.Min(amount, spend ? Available(status, resource) : deficit);
            deficit = Math.Max(0, deficit + (spend ? applied : -applied));
            // Avoid floating-point residue granting or removing a whole pool point.
            if (Math.Abs(deficit - Math.Round(deficit)) < 1e-9) deficit = Math.Round(deficit);
            var whole = checked((int)Math.Floor(deficit));
            status.FractionalResourceDeficits[resource] = deficit - whole;
            status.ResourceDeficits = resource switch {
                ShipResource.Hull => deficits with { HullDamage = whole },
                ShipResource.Shield => deficits with { ShieldDamage = whole },
                _ => deficits with { CapacitorExpenditure = whole } };
            var current = Math.Clamp(maximum - whole, 0, maximum);
            if (resource == ShipResource.Hull && alive && !spend) current = Math.Max(1, current);
            if (resource == ShipResource.Hull) status.Hull = current;
            else if (resource == ShipResource.Shield) status.Shield = current;
            else status.Capacitor = current;
            return applied;
        }

        private static int Change(ShipStatus status, ShipResource resource, int amount, bool spend)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            return checked((int)Math.Floor(ChangePrecise(status, resource, amount, spend)));
        }
    }
}
