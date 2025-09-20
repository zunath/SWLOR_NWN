using System.Reflection;

namespace SWLOR.Shared.Core.Extension
{
    public static class ReflectionExtensions
    {
        public static string GetFullName(this MemberInfo member)
        {
            return member.DeclaringType != null ? $"{member.DeclaringType.FullName}.{member.Name}" : member.Name;
        }
    }
}
