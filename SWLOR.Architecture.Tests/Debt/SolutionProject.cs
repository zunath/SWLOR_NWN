namespace SWLOR.Architecture.Tests.Debt;

/// <summary>A C# project of <c>SWLOR.Game.Server.sln</c> and the source files under its folder.</summary>
/// <param name="Name">The project name as the solution lists it (<c>SWLOR.Game.Server</c>).</param>
/// <param name="Directory">The project's folder, absolute.</param>
/// <param name="Files">
/// Its <c>.cs</c> files as paths relative to the repository root with forward slashes, sorted. Files under <c>bin</c>,
/// <c>obj</c> and the folder of another project of the solution are left out; files a project links from elsewhere
/// (<c>Compile Include="..\tools\..."</c>) belong to the folder they live in.
/// </param>
internal sealed record SolutionProject(string Name, string Directory, IReadOnlyList<string> Files);
