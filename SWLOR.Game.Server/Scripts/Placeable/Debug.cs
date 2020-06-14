using System;
using System.Collections.Generic;
using System.Text;
using NWN;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.NWNX;

namespace SWLOR.Game.Server.Scripts.Placeable
{
    public class Debug: IScript
    {
        public void SubscribeEvents()
        {
            
        }

        public void UnsubscribeEvents()
        {
            
        }

        //public void Main()
        //{
        //    Console.WriteLine("firing");

        //    var itemprop = _.ItemPropertyAttackBonus(1);

        //    Console.WriteLine($"itemprop handle = {itemprop.Handle}");

        //    Console.WriteLine("Unpacking");
        //    var unpacked = NWNXItemProperty.UnpackIP(itemprop);

        //    Console.WriteLine("Packing");
        //    var packed = NWNXItemProperty.PackIP(unpacked);

        //    Console.WriteLine($"packed handle = {packed.Handle}");

        //    Console.WriteLine("Done");

        //}


        public void Main()
        {
            var a = _.ItemPropertyAttackBonus(1);
            Console.WriteLine($"a handle = {a.Handle}");

            var b = _.ItemPropertyAttackBonus(1);
            Console.WriteLine($"b handle = {b.Handle}");

            var c = NWNXItemProperty.PackIP(NWNXItemProperty.UnpackIP(a));
            Console.WriteLine($"c handle = {c.Handle}");

            var d = NWNXItemProperty.PackIP(NWNXItemProperty.UnpackIP(b));
            Console.WriteLine($"d handle = {d.Handle}");

            var e = NWNXItemProperty.PackIP(new ItemPropertyUnpacked());
            Console.WriteLine($"e handle = {e.Handle}");

            var f = new ItemProperty(Internal.NativeFunctions.nwnxPopItemProperty());
            Console.WriteLine($"f handle = {f.Handle}");

            var g = Internal.NativeFunctions.nwnxPopItemProperty();
            Console.WriteLine($"g handle = {g}");

        }
    }
}
