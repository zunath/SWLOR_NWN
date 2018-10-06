using System;
using System.Reflection;

namespace SWLOR.Game.Server.Event.Legacy
{
    public class LegacyJVMItemEvent: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            string script = (string) args[0];

            if (!script.StartsWith("Item."))
            {
                script = "Item." + script;
            }

            Type type = Type.GetType(Assembly.GetExecutingAssembly().GetName().Name + "." + script);

            if (type == null)
            {
                Console.WriteLine("Unable to locate type for LegacyJVMItemEvent: " + script);
                return false;
            }

            App.RunEvent(type);

            return false;
        }
    }
}
