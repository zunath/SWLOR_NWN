#:package Microsoft.CodeAnalysis.CSharp@5.6.0
// P2-CB1: splits SWLOR.Game.Server/Service/Combat.cs into partial-class files, mechanically.
//
//   dotnet run tools/Refactor/SplitCombat.cs -- --source-rev <rev> [--range-rev 7aacf4b85] [--write]
//
// Reads Service/Combat.cs as it is in <rev> (the unsplit file), maps the old line ranges of the plan's "Parts"
// table (tools/Refactor/CombatParts.txt, taken at --range-rev) onto it through `git diff`, assigns each member of
// `Combat` to the part whose range holds the member's first token, and writes Service/Combat.cs (shared header)
// plus Service/CombatParts/Combat.<Part>.cs. Without --write it only prints the report (cut members, new members,
// static-initialisation findings). Members added after --range-rev are placed by tools/Refactor/CombatNewMembers.txt
// (Name=Part). The only member whose text changes shape is RemoveStatTriggerCooldowns, which becomes calls to one
// Forget* method per part that owns the tables it clears. Run from the repository root.
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

string? sourceRev = null, rangeRev = "7aacf4b85";
var write = false;
for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--source-rev") sourceRev = args[++i];
    else if (args[i] == "--range-rev") rangeRev = args[++i];
    else if (args[i] == "--write") write = true;
}
if (sourceRev == null) { Console.Error.WriteLine("--source-rev is required"); return 2; }

const string ServiceRel = "SWLOR.Game.Server/Service/Combat.cs";
var repo = Run("git", "rev-parse --show-toplevel").Trim();
var text = Run("git", $"show {sourceRev}:{ServiceRel}");
var crlf = text.Contains("\r\n");
var nl = crlf ? "\r\n" : "\n";

// ---- ranges (old line numbers) ----
var ranges = new List<(int From, int To, string Part)>();
foreach (var line in File.ReadAllLines(Path.Combine(repo, "tools/Refactor/CombatParts.txt")))
{
    if (line.StartsWith('#') || string.IsNullOrWhiteSpace(line)) continue;
    var bar = line.Split('|');
    foreach (var r in bar[1].Split(',', StringSplitOptions.RemoveEmptyEntries))
    {
        var se = r.Split('-');
        ranges.Add((int.Parse(se[0]), int.Parse(se[^1]), bar[0]));
    }
}
var partNames = ranges.Select(r => r.Part).Distinct().ToList();

// ---- new line -> old line through git diff ----
var hunks = new List<(int OldStart, int OldCount, int NewStart, int NewCount)>();
foreach (Match m in Regex.Matches(Run("git", $"diff -U0 {rangeRev} {sourceRev} -- {ServiceRel}"), @"^@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@", RegexOptions.Multiline))
    hunks.Add((int.Parse(m.Groups[1].Value), m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : 1,
               int.Parse(m.Groups[3].Value), m.Groups[4].Success ? int.Parse(m.Groups[4].Value) : 1));

(int Old, bool Inserted) ToOld(int n)
{
    var delta = 0;
    foreach (var h in hunks)
    {
        if (h.NewCount > 0)
        {
            if (n < h.NewStart) break;
            if (n < h.NewStart + h.NewCount)
            {
                var idx = n - h.NewStart;
                return idx < h.OldCount ? (h.OldStart + idx, false) : (Math.Max(1, h.OldStart + h.OldCount - 1), true);
            }
        }
        else if (n <= h.NewStart) break;
        delta += h.OldCount - h.NewCount;
    }
    return (n + delta, false);
}
string? PartOfOld(int old) => ranges.FirstOrDefault(r => old >= r.From && old <= r.To).Part;

// ---- parse ----
var tree = CSharpSyntaxTree.ParseText(text, new CSharpParseOptions(LanguageVersion.Latest));
var root = (CompilationUnitSyntax)tree.GetRoot();
var ns = (BaseNamespaceDeclarationSyntax)root.Members.Single();
var cls = ns.Members.OfType<ClassDeclarationSyntax>().Single(c => c.Identifier.Text == "Combat");
if (ns.Members.Count != 1) throw new Exception("Unexpected extra members in the namespace.");
if (root.DescendantTrivia().Any(t => t.IsDirective)) throw new Exception("Preprocessor directives are not handled.");
if (cls.Members.OfType<ConstructorDeclarationSyntax>().Any(c => c.Modifiers.Any(SyntaxKind.StaticKeyword)))
    Console.WriteLine("STATIC CONSTRUCTOR present.");
var closeTrivia = cls.CloseBraceToken.LeadingTrivia.Where(t => !t.IsKind(SyntaxKind.WhitespaceTrivia) && !t.IsKind(SyntaxKind.EndOfLineTrivia)).ToList();
if (closeTrivia.Count > 0) throw new Exception("Comment before the class's closing brace.");

int LineOf(SyntaxToken t) => t.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
string NameOf(MemberDeclarationSyntax m) => m switch
{
    FieldDeclarationSyntax f => f.Declaration.Variables[0].Identifier.Text,
    MethodDeclarationSyntax x => x.Identifier.Text,
    BaseTypeDeclarationSyntax t => t.Identifier.Text,
    PropertyDeclarationSyntax p => p.Identifier.Text,
    DelegateDeclarationSyntax d => d.Identifier.Text,
    _ => m.Kind().ToString() + "@" + LineOf(m.GetFirstToken()),
};

// Header material the plan's "Static fields" table and the nested types give a home. "" keeps it in Combat.cs.
var placements = new Dictionary<string, string>
{
    ["_allValidDamageTypes"] = "DamageCatalog", ["_allDefenseDamageTypes"] = "DamageCatalog", ["_damageTypesCached"] = "DamageCatalog",
    ["_statTriggerCooldowns"] = "Resources", ["_damageDerivedHealingStates"] = "Resources", ["_passiveDamageHealingBudgets"] = "Resources",
    ["_defeatedEnemyHealingBudgets"] = "Resources", ["_abilityStaminaCosts"] = "Resources", ["AbilityStaminaCostState"] = "Resources",
    ["_recentDamageTargets"] = "Activity", ["_recentDamageTaken"] = "Activity", ["_lastCombatActivity"] = "Activity",
    ["_lastHostileAbilityAttemptActivity"] = "Activity", ["_lastHostileIncomingActivity"] = "Activity", ["_firstCombatAttackConsumed"] = "Activity",
    ["_lastAttackActivity"] = "Activity", ["_lastCombatAbilityUse"] = "Activity", ["_firstHostileAbilityHitCounts"] = "Activity",
    ["_stealthOpeningWindows"] = "Activity",
    ["_recentGuardedHits"] = "DamageTaken", ["_recentDeflections"] = "DamageTaken",
    ["_pendingSuppressionAbilityUses"] = "Suppression", ["_containmentNormalizationTargets"] = "Suppression", ["SuppressionAbilityUseState"] = "Suppression",
    ["_hostileAbilitySequenceStates"] = "AbilityActivation", ["HostileAbilitySequenceState"] = "AbilityActivation",
    ["_sameTargetHostileAbilityHitCounts"] = "AbilityImpact", ["_areaAbilityTargetHitSequences"] = "AbilityImpact",
    ["_criticalHitSequenceStates"] = "HitEffects", ["_autoAttackCycleCounts"] = "HitEffects", ["_sourceStatusAutoAttackCycleCounts"] = "HitEffects",
    ["_meleeAutoAttackCycleCounts"] = "HitEffects", ["CriticalHitSequenceState"] = "HitEffects",
    ["_autoAttackCycleCriticalCounts"] = "DamageModifiers", ["_repeatedTargetDamageStates"] = "DamageModifiers",
    ["_meleeRepeatedTargetDamageStates"] = "DamageModifiers", ["_rangedRepeatedTargetDamageStates"] = "DamageModifiers",
    ["_sameTargetPressureStates"] = "DamageModifiers", ["SameTargetPressureState"] = "DamageModifiers",
    ["_attackSwingDebts"] = "AttackTiming", ["_attackSwingDebtsWithoutLimitedReduction"] = "AttackTiming",
    // Shared by several parts: stays in Combat.cs.
    ["_idleSkillAbilityStatChannels"] = "", ["IdleSkillAbilityStatChannel"] = "", ["TargetHitSequenceState"] = "",
};
var newMemberPlacements = new Dictionary<string, string>();
var placementFile = Path.Combine(repo, "tools/Refactor/CombatNewMembers.txt");
if (File.Exists(placementFile))
    foreach (var l in File.ReadAllLines(placementFile).Where(l => !l.StartsWith('#') && l.Contains('=')))
        newMemberPlacements[l.Split('=')[0].Trim()] = l.Split('=')[1].Trim();

var assigned = new List<(MemberDeclarationSyntax Node, string Part)>();
var problems = new List<string>();
foreach (var m in cls.Members)
{
    var name = NameOf(m);
    var first = LineOf(m.GetFirstToken());
    var last = LineOf(m.GetLastToken());
    var (oldFirst, insertedFirst) = ToOld(first);
    var (oldLast, _) = ToOld(last);
    string? part;
    if (placements.TryGetValue(name, out var p))
        part = p;
    else if (newMemberPlacements.TryGetValue(name, out var np))
        part = np;
    else if (insertedFirst)
    {
        problems.Add($"NEW member {name} at line {first}: needs a placement in CombatNewMembers.txt");
        part = "?";
    }
    else
    {
        part = PartOfOld(oldFirst);
        var partLast = PartOfOld(oldLast);
        if (part == null)
        {
            if (oldFirst < 173 && m is FieldDeclarationSyntax fd && fd.Modifiers.Any(SyntaxKind.ConstKeyword)) part = "";
            else { problems.Add($"UNMAPPED {name} at line {first} (old {oldFirst})"); part = "?"; }
        }
        else if (partLast != part)
            problems.Add($"CUT {name} lines {first}-{last} (old {oldFirst}-{oldLast}): {part} to {partLast}");
    }
    assigned.Add((m, part));
}

// ---- static initialisation order ----
var memberNames = cls.Members.Select(NameOf).ToHashSet();
Console.WriteLine("== Static initialisation ==");
foreach (var f in cls.Members.OfType<FieldDeclarationSyntax>().Where(f => f.Modifiers.Any(SyntaxKind.StaticKeyword) && !f.Modifiers.Any(SyntaxKind.ConstKeyword)))
foreach (var v in f.Declaration.Variables.Where(v => v.Initializer != null))
{
    var ids = v.Initializer!.DescendantNodes().OfType<IdentifierNameSyntax>().Select(i => i.Identifier.Text)
        .Where(n => memberNames.Contains(n) && n != v.Identifier.Text).Distinct().ToList();
    var kind = v.Initializer.Value.ToString();
    Console.WriteLine($"  {v.Identifier.Text}: line {LineOf(v.Identifier)} init `{(kind.Length > 40 ? kind[..40] + "..." : kind).Replace("\r", "").Replace("\n", " ")}` reads Combat members: {(ids.Count == 0 ? "none" : string.Join(", ", ids))}");
}
foreach (var pr in cls.Members.OfType<PropertyDeclarationSyntax>().Where(x => x.Modifiers.Any(SyntaxKind.StaticKeyword) && x.Initializer != null))
    Console.WriteLine($"  STATIC PROPERTY INIT {pr.Identifier.Text}: {pr.Initializer}");
Console.WriteLine("== Placement report ==");
foreach (var g in assigned.GroupBy(a => a.Part).OrderBy(g => g.Key))
    Console.WriteLine($"  {(g.Key == "" ? "(Combat.cs)" : g.Key)}: {g.Count()} members");
foreach (var pr in problems) Console.WriteLine("  PROBLEM " + pr);
if (problems.Count > 0 || !write) return problems.Count > 0 ? 1 : 0;

// ---- RemoveStatTriggerCooldowns ----
var remove = cls.Members.OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "RemoveStatTriggerCooldowns");
var stmts = remove.Body!.Statements;
if (stmts.Count != 35) throw new Exception($"RemoveStatTriggerCooldowns has {stmts.Count} statements, expected 35.");
// statement index -> Forget group. Every dictionary Remove is pure; the two observable points (the overwatch
// marker refresh in statement 15, and the status-effect removal in statement 32) see the same cleared set as before.
var groups = new (string Part, string Method, int[] Indexes)[]
{
    ("Resources", "ForgetResourceTriggerState", new[] { 0, 1, 2 }),
    ("Activity", "ForgetActivityState", new[] { 3, 4, 7, 8, 9, 10, 11, 12 }),
    ("DamageTaken", "ForgetDamageTakenState", new[] { 5, 6 }),
    ("Suppression", "ForgetSuppressionState", new[] { 13, 14, 15 }),
    ("AbilityActivation", "ForgetActivationState", new[] { 16 }),
    ("HitEffects", "ForgetHitEffectState", new[] { 17, 20, 22, 31 }),
    ("AbilityImpact", "ForgetImpactState", new[] { 19, 25 }),
    ("Activity", "ForgetOpeningActivityState", new[] { 18, 23 }),
    ("Resources", "ForgetAbilityStaminaCostState", new[] { 24 }),
    ("AttackTiming", "ForgetAttackTimingState", new[] { 26, 27 }),
    ("DamageModifiers", "ForgetDamageModifierState", new[] { 21, 28, 29, 30, 32, 33 }),
};
if (!groups.SelectMany(g => g.Indexes).OrderBy(x => x).SequenceEqual(Enumerable.Range(0, 34))) throw new Exception("Forget groups do not cover statements 0-33.");
if (!stmts[34].ToString().StartsWith("TemporaryStatModifier.Clear(creature)")) throw new Exception("Statement 34 changed.");
string StmtText(StatementSyntax s)
{
    var lines = s.ToFullString().Split('\n').Select(l => l.TrimEnd('\r')).ToList();
    while (lines.Count > 0 && lines[0].Trim().Length == 0) lines.RemoveAt(0);
    while (lines.Count > 0 && lines[^1].Trim().Length == 0) lines.RemoveAt(lines.Count - 1);
    return string.Join(nl, lines);
}
var forgetByPart = new Dictionary<string, List<string>>();
foreach (var g in groups)
{
    foreach (var idx in g.Indexes)
    {
        var field = Regex.Match(stmts[idx].ToString(), @"_[A-Za-z]+").Value;
        var owner = field != "" && placements.TryGetValue(field, out var o) ? o : null;
        if (owner != null && owner != g.Part) throw new Exception($"Statement {idx} touches {field} owned by {owner}, not {g.Part}.");
    }
    var sb = new StringBuilder();
    sb.Append("        private static void ").Append(g.Method).Append("(uint creature)").Append(nl).Append("        {").Append(nl);
    sb.Append(string.Join(nl, g.Indexes.Select(i => StmtText(stmts[i]))));
    sb.Append(nl).Append("        }");
    if (!forgetByPart.TryGetValue(g.Part, out var list)) forgetByPart[g.Part] = list = new();
    list.Add(sb.ToString());
}
var newRemove = new StringBuilder();
newRemove.Append("        private static void RemoveStatTriggerCooldowns(uint creature)").Append(nl).Append("        {").Append(nl);
foreach (var g in groups) newRemove.Append("            ").Append(g.Method).Append("(creature);").Append(nl);
newRemove.Append("            TemporaryStatModifier.Clear(creature);").Append(nl).Append("        }");

// ---- emit ----
string MemberText(MemberDeclarationSyntax m)
{
    if (ReferenceEquals(m, remove)) return newRemove.ToString();
    var s = m.ToFullString().Replace("\r\n", "\n");
    s = Regex.Replace(s, @"\A(?:[ \t]*\n)+", "").TrimEnd();
    return s.Replace("\n", nl);
}
var usings = string.Join(nl, root.Usings.Select(u => u.ToString().Trim()));
var servicePart = Path.Combine(repo, "SWLOR.Game.Server", "Service");
var partsDir = Path.Combine(servicePart, "CombatParts");
Directory.CreateDirectory(partsDir);
void Emit(string path, string? part)
{
    var sb = new StringBuilder();
    sb.Append(usings).Append(nl).Append(nl);
    sb.Append("namespace ").Append(ns.Name).Append(nl).Append('{').Append(nl);
    sb.Append("    public static partial class Combat").Append(nl).Append("    {").Append(nl);
    var items = assigned.Where(a => a.Part == (part ?? "")).Select(a => (Text: MemberText(a.Node), IsField: a.Node is FieldDeclarationSyntax)).ToList();
    if (part != null && forgetByPart.TryGetValue(part, out var forgets)) items.AddRange(forgets.Select(f => (f, false)));
    for (var i = 0; i < items.Count; i++)
    {
        if (i > 0 && !(items[i].IsField && items[i - 1].IsField)) sb.Append(nl);
        sb.Append(items[i].Text).Append(nl);
    }
    sb.Append("    }").Append(nl).Append('}').Append(nl);
    File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
}
Emit(Path.Combine(servicePart, "Combat.cs"), null);
foreach (var part in partNames) Emit(Path.Combine(partsDir, $"Combat.{part}.cs"), part);
Console.WriteLine($"Wrote Combat.cs and {partNames.Count} part files.");
return 0;

string Run(string file, string arguments)
{
    var psi = new ProcessStartInfo(file, arguments) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, StandardOutputEncoding = new UTF8Encoding(false) };
    using var p = Process.Start(psi)!;
    var o = p.StandardOutput.ReadToEnd();
    p.WaitForExit();
    if (p.ExitCode != 0) throw new Exception($"{file} {arguments} failed: {p.StandardError.ReadToEnd()}");
    return o;
}
