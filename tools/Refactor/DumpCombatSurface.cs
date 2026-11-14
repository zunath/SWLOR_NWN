#:package System.Reflection.MetadataLoadContext@10.0.0
// P2-CB1 proof: dumps the public member set of SWLOR.Game.Server.Service.Combat and every [NWNEventHandler] method
// of an assembly, sorted, one per line, so a before and an after dump can be diffed.
//   dotnet run tools/Refactor/DumpCombatSurface.cs -- <bin folder with SWLOR.Game.Server.dll> <out file>
using System.Reflection;
var dir = args[0];
var paths = Directory.GetFiles(dir, "*.dll").Concat(Directory.GetFiles(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "*.dll")).ToList();
using var mlc = new MetadataLoadContext(new PathAssemblyResolver(paths), typeof(object).Assembly.GetName().Name);
var asm = mlc.LoadFromAssemblyPath(Path.Combine(dir, "SWLOR.Game.Server.dll"));
var lines = new List<string>();
var combat = asm.GetType("SWLOR.Game.Server.Service.Combat", true)!;
lines.Add($"type abstract={combat.IsAbstract} sealed={combat.IsSealed}");
const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
foreach (var m in combat.GetMembers(all))
{
    var vis = m switch
    {
        MethodBase mb => mb.IsPublic ? "public" : null,
        FieldInfo f => f.IsPublic ? "public" : null,
        PropertyInfo p => p.GetMethod?.IsPublic == true || p.SetMethod?.IsPublic == true ? "public" : null,
        Type t => t.IsNestedPublic ? "public" : null,
        _ => null,
    };
    if (vis != null) lines.Add($"PUBLIC {m.MemberType} {m}" + (m is FieldInfo { IsLiteral: true } lit ? " = " + lit.GetRawConstantValue() : ""));
}
foreach (var t in asm.GetTypes())
    foreach (var m in t.GetMethods(all))
        foreach (var a in m.CustomAttributes.Where(a => a.AttributeType.Name == "NWNEventHandler"))
            lines.Add($"HANDLER {t.FullName}.{m.Name} [{string.Join(",", a.ConstructorArguments.Select(c => c.Value))}]");
lines.Sort(StringComparer.Ordinal);
File.WriteAllLines(args[1], lines);
Console.WriteLine($"{lines.Count(l => l.StartsWith("PUBLIC"))} public members, {lines.Count(l => l.StartsWith("HANDLER"))} handlers (all types), {lines.Count(l => l.StartsWith("HANDLER") && l.Contains("Service.Combat."))} on Combat");
