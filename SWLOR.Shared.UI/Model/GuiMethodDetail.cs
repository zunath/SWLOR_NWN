using System.Reflection;

namespace SWLOR.Shared.UI.Model
{
    public class GuiMethodDetail
    {
        public MethodInfo Method { get; set; }
        public List<KeyValuePair<Type, object>> Arguments { get; set; }

        public GuiMethodDetail(MethodInfo method, List<KeyValuePair<Type, object>> args)
        {
            Method = method;
            Arguments = args;
        }
    }
}
