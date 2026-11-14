using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Composition
{
    /// <summary>
    /// One <c>Content.Create</c> signature of the game's code: the made type and the value types the call passes
    /// (<c>Content.Create&lt;ExamineItemPayload, uint&gt;(item)</c> is <c>ExamineItemPayload</c> with <c>uint</c>).
    /// <c>tools/Refactor/SWLOR.ContentInjection</c> records them in <see cref="ContentRegistrations"/>, and the reload validation checks
    /// that each resolves to exactly one constructor.
    /// </summary>
    public sealed class ContentSignature
    {
        public ContentSignature(Type made, params Type[] values)
        {
            Made = made ?? throw new ArgumentNullException(nameof(made));
            Values = (values ?? Array.Empty<Type>()).ToArray();
        }

        /// <summary>The type the call builds.</summary>
        public Type Made { get; }

        /// <summary>The value types, in the order the call passes them.</summary>
        public IReadOnlyList<Type> Values { get; }

        public override string ToString()
        {
            return Values.Count == 0
                ? $"Create<{Made.Name}>()"
                : $"Create<{Made.Name}, {string.Join(", ", Values.Select(value => value.Name))}>(...)";
        }
    }
}
