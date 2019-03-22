using System;
using System.Reflection;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Event.Legacy
{
    public class LegacyJVMEvent: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWObject self = (Object.OBJECT_SELF);
            string script = self.GetLocalString((string) args[0]);

            using (new Profiler("LegacyJVMEvent::" + script))
            {
                Type type = Type.GetType(Assembly.GetExecutingAssembly().GetName().Name + "." + script);

                if (type == null)
                {
                    Console.WriteLine("Unable to locate type for LegacyJVMEvent: " + script);
                    return false;
                }

                App.RunEvent(type);
            }


            return true;
        }
    }
}
