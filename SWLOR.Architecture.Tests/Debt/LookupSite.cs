namespace SWLOR.Architecture.Tests.Debt;

/// <summary>One place in code that reaches a service through the static lookup.</summary>
/// <param name="Project">The project of the file.</param>
/// <param name="File">The file, relative to the repository root.</param>
/// <param name="Type">The top-level type the code is in, or null for code outside any type.</param>
/// <param name="Member">What is used: <c>GameServices.Stat</c>, <c>FacadeAccessor.Resolve</c>, the type name alone (<c>typeof(GameServices)</c>) or a <c>using</c> directive that imports the type.</param>
/// <param name="Service">The service the use reaches (the property's type, the resolved type), or null for the type name alone and an import.</param>
/// <param name="IsStaticContext">True when the code is in a static member, a static class or a static lambda; false for an import.</param>
/// <param name="IsImport">True for a <c>using static</c> or alias directive that names the lookup type.</param>
internal sealed record LookupSite(string Project, string File, string? Type, string Member, string? Service, bool IsStaticContext, bool IsImport = false);
