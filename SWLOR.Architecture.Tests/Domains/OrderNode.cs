namespace SWLOR.Architecture.Tests.Domains;

/// <summary>
/// A place in the domain order (owner decisions 10 to 13): one domain's logic or one domain's content. Content
/// (<c>Feature/</c>) is a band above all logic, so a domain has two nodes. Its text form is what the baselines show:
/// <c>Character</c> for the logic, <c>Character content</c> for the content.
/// </summary>
internal readonly record struct OrderNode(string Domain, bool IsContent)
{
    public const string ContentSuffix = " content";

    public override string ToString() => IsContent ? Domain + ContentSuffix : Domain;
}
