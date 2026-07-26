using SWLOR.Game.Server.Service.DialogService;

namespace SWLOR.Toolset.Domain.GameData.GameCode
{
    /// <summary>
    /// Lists the conversations a placeable's <c>CONVERSATION</c> local can name: the concrete
    /// subclasses of <c>DialogBase</c> in SWLOR.Game.Server.
    /// </summary>
    /// <remarks>
    /// The variable holds a class name, which nothing validates today - 23 uses across the module
    /// name a conversation that does not exist, so clicking those placeables does nothing at all.
    /// Reading the real type list turns that field into a picker.
    /// <para>
    /// Only type metadata is touched, never a constructor, so no dialog's static state or NWN
    /// native dependency is initialized by asking this question.
    /// </para>
    /// </remarks>
    internal static class ReflectionDialogReader
    {
        public static IReadOnlyCollection<string> ReadDialogNames()
        {
            var names = new SortedSet<string>(StringComparer.Ordinal);

            try
            {
                foreach (var type in typeof(DialogBase).Assembly.GetTypes())
                {
                    if (type.IsAbstract || !typeof(DialogBase).IsAssignableFrom(type))
                        continue;

                    names.Add(type.Name);
                }
            }
            catch (Exception)
            {
                // A load failure in an unrelated type must not take the index with it; an empty
                // list degrades the picker to free text rather than blocking the editor.
            }

            return names;
        }
    }
}
