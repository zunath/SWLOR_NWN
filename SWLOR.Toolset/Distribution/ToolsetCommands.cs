using SWLOR.NWN.Formats.Common;
using System.Text.Json;
using SWLOR.Toolset.Domain.Script.Compile;
using SWLOR.Toolset.Domain.Distribution;
using SWLOR.Toolset.Domain.GameData.GameCode;

namespace SWLOR.Toolset.Distribution;

/// <summary>Release generation and maintainer imports use the same code paths as the desktop application.</summary>
public static class ToolsetCommands
{
    public static bool TryRun(string[] args, out int exitCode)
    {
        exitCode = 0;
        if (args.Length == 0 || !new[] { "--export-catalog", "--check-contribution", "--import-contribution", "--verify-install" }.Contains(args[0])) return false;
        try
        {
            if (args[0] == "--export-catalog" && args.Length == 4)
            {
                var index = new GameCodeIndex(Path.Combine(args[1], "SWLOR.Game.Server"));
                if (!index.IsSourceScanAvailable || index.QuestIds.Count == 0 || index.SpawnTableIds.Count == 0)
                    throw new InvalidDataException("The release editor catalog is incomplete.");
                GameCodeCatalog.Capture(index, args[2]).Save(args[3]);
            }
            else if (args[0] is "--check-contribution" or "--import-contribution" && args.Length == 3)
            {
                var count = ContributionArchive.Import(args[1], args[2], apply: args[0] == "--import-contribution");
                Console.WriteLine($"{count} contribution change(s) checked. Conversation changes require a matching server build.");
            }
            else if (args[0] == "--verify-install" && args.Length == 2)
            {
                var root = AppContext.BaseDirectory;
                var tools = Path.Combine(root, "tools", "SWLOR.CLI");
                foreach (var name in new[] { "SWLOR.CLI", "nwn_erf", "nwn_gff", "nwn_script_comp" })
                    if (NativeToolPath.FindInDirectory(tools, name) == null)
                        throw new FileNotFoundException("Incomplete toolset installation: " + NativeToolPath.ExecutableName(name));
                var library = OperatingSystem.IsWindows() ? "libnwnscriptcomp.dll" : "libnwnscriptcomp.so";
                if (!File.Exists(Path.Combine(tools, library))) throw new FileNotFoundException("Missing native compiler library: " + library);
                if (!Directory.EnumerateFiles(Path.Combine(root, "SWLOR.NWN.API", "NWN"), "nwscript*.nss").Any())
                    throw new FileNotFoundException("The NWScript engine header is missing.");
                VerifyStandalonePacking(tools);
                File.WriteAllText(args[1], JsonSerializer.Serialize(new { Success = true, ApplicationRoot = root }));
            }
            else throw new ArgumentException("Invalid arguments for " + args[0]);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            exitCode = 1;
            if (args.Length == 2 && args[0] == "--verify-install")
                File.WriteAllText(args[1], JsonSerializer.Serialize(new { Success = false, Error = exception.Message }));
        }
        return true;
    }
    private static void VerifyStandalonePacking(string tools)
    {
        var fixture = Path.Combine(Path.GetTempPath(), "swlor_install_check_" + Guid.NewGuid().ToString("N"));
        var module = Path.Combine(fixture, "Module");
        try
        {
            foreach (var folder in new[] { "are", "dlg", "fac", "gic", "git", "ifo", "itp", "jrl", "utc", "utd", "ute", "uti", "utm", "utp", "uts", "utt", "utw", "nss", "ncs" })
                Directory.CreateDirectory(Path.Combine(module, folder));
            File.WriteAllText(Path.Combine(module, "ifo", "module.ifo.json"),
                "{\"__data_type\":\"IFO \",\"__struct_id\":4294967295}");
            var header = Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "SWLOR.NWN.API", "NWN"), "nwscript*.nss").First();
            File.Copy(header, Path.Combine(module, "nss", "nwscript.nss"));
            var script = Path.Combine(module, "nss", "install_check.nss");
            File.WriteAllText(script, "void main() { object speaker = GetPCSpeaker(); }");
            var bytecode = Path.Combine(module, "ncs", "install_check.ncs");
            var compiler = NativeToolPath.FindInDirectory(tools, "nwn_script_comp")!;
            var compiled = new ScriptCompiler(compiler, new[] { Path.Combine(module, "nss") })
                .CompileAsync(script, outputPath: bytecode).GetAwaiter().GetResult();
            if (!compiled.Succeeded || !File.Exists(bytecode))
                throw new InvalidOperationException("The standalone compiler failed: " + compiled.Output);
            var result = new Services.PackService(new Workspace.OutputLogService()).PackAsync(module).GetAwaiter().GetResult();
            if (result != 0 || !Directory.EnumerateFiles(module, "*.mod").Any())
                throw new InvalidOperationException("The standalone packer could not build a fixture module.");
            if (Directory.Exists(Path.Combine(fixture, "debugserver")))
                throw new InvalidOperationException("Standalone packing unexpectedly deployed developer files.");
        }
        finally { if (Directory.Exists(fixture)) Directory.Delete(fixture, recursive: true); }
    }

}
