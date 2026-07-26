using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Validation
{
    /// <summary>
    /// Reports blueprints and placed instances whose <c>Conversation</c> names a dialog that does
    /// not exist anywhere - not in the module, not in a hak, not in the base game.
    /// </summary>
    /// <remarks>
    /// The resource index is consulted before anything is reported, because several of these
    /// resolve from base-game or hak content rather than from <c>Module\dlg</c>. Without that check
    /// this rule would cry about doors and chairs that work perfectly well in game.
    /// </remarks>
    public sealed class DanglingConversationRule : IValidationRule
    {
        public string RuleId => "DanglingConversation";

        public IEnumerable<ValidationIssue> Validate(ValidationContext context)
        {
            foreach (var (type, resRef, conversation, source) in ConversationReferences(context))
            {
                if (string.IsNullOrWhiteSpace(conversation))
                    continue;

                if (context.ResourceExists(ResourceType.Dlg, conversation))
                    continue;

                if (context.ResolvableOutsideModule(ResourceType.Dlg, conversation))
                    continue;

                yield return new ValidationIssue(
                    ValidationSeverity.Error,
                    RuleId,
                    $"{source} points at conversation '{conversation}', which does not exist in the "
                    + "module, a hak, or the base game. Nobody will ever hear it.",
                    null,
                    resRef);
            }
        }

        internal static IEnumerable<(ResourceType Type, string ResRef, string Conversation, string Source)>
            ConversationReferences(ValidationContext context)
        {
            foreach (var type in new[]
                     {
                         ResourceType.Utc, ResourceType.Utp, ResourceType.Utd,
                         ResourceType.Uts, ResourceType.Utt
                     })
            {
                foreach (var resRef in context.ResRefsFor(type))
                {
                    var (document, _) = context.LoadBlueprint(type, resRef);
                    var conversation = document?.Fields.GetOrNull("Conversation")?.GetString();
                    if (!string.IsNullOrWhiteSpace(conversation))
                        yield return (type, resRef, conversation, $"{type.SingularDisplayName()} '{resRef}'");
                }
            }

            foreach (var areaResRef in context.AreaResRefs)
            {
                var (git, _) = context.LoadGit(areaResRef);
                if (git == null)
                    continue;

                foreach (var (listName, instances) in new[]
                         {
                             ("creature", git.Creatures),
                             ("placeable", git.Placeables),
                             ("door", git.Doors)
                         })
                {
                    foreach (var instance in instances)
                    {
                        var conversation = instance.GetOrNull("Conversation")?.GetString();
                        if (!string.IsNullOrWhiteSpace(conversation))
                        {
                            yield return (ResourceType.Area, areaResRef, conversation,
                                $"A placed {listName} in '{areaResRef}'");
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Reports hand-authored conversations that nothing points at, so nobody can ever hear them.
    /// </summary>
    /// <remarks>
    /// The 255 generated <c>dialogN</c> shells are skipped: the C# <c>Dialog</c> service assigns
    /// them at runtime, so they are unreferenced by design and reporting them would bury the
    /// handful of real findings under a wall of noise.
    /// </remarks>
    public sealed class UnreferencedConversationRule : IValidationRule
    {
        public string RuleId => "UnreferencedConversation";

        public IEnumerable<ValidationIssue> Validate(ValidationContext context)
        {
            var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (_, _, conversation, _) in DanglingConversationRule.ConversationReferences(context))
                referenced.Add(conversation);

            foreach (var resRef in context.ResRefsFor(ResourceType.Dlg))
            {
                if (IsGeneratedShell(resRef) || referenced.Contains(resRef))
                    continue;

                yield return new ValidationIssue(
                    ValidationSeverity.Warning,
                    RuleId,
                    $"Nothing in the module has conversation '{resRef}', so nobody will ever hear it.",
                    null,
                    resRef);
            }
        }

        /// <summary>
        /// Whether this is one of the numbered shells the C# <c>Dialog</c> service generates. Matched
        /// by pattern rather than by a list of 255 names nobody would keep in step.
        /// </summary>
        /// <remarks>
        /// Public because the editor asks the same question for a different reason - it refuses to
        /// open one for editing - and two copies of this predicate would eventually disagree.
        /// </remarks>
        public static bool IsGeneratedShell(string resRef)
        {
            if (!resRef.StartsWith("dialog", StringComparison.OrdinalIgnoreCase))
                return false;

            var suffix = resRef.AsSpan("dialog".Length);
            return suffix.Length > 0 && int.TryParse(suffix, out _);
        }
    }
}
