using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.ContentInjection;

/// <summary>
/// The forward conversion of one file (MaintainabilityRefactorContentInjection.md, "Singleton content", "Made content", "Creation
/// sites that change"). Works on the syntax tree and changes only the text it must, so it can run again on every sync:
/// <list type="bullet">
/// <item>a converted class gets a private get-only property per service it reaches, named like the old <c>GameServices</c> property
/// (<c>&lt;Name&gt;Service</c> when the class has a member of that name), a constructor that sets them (or each existing constructor
/// gains them, forwarding a converted base's), and loses <c>GameServices.</c>; a singleton also loses <c>static</c> on the class and on
/// every member that reaches a service, and its self-qualified calls to them;</item>
/// <item><c>new X(values)</c> of a made type with services becomes <c>Content.Create&lt;X, A...&gt;(values)</c> with the arguments as
/// written; a class copying itself passes its own services; the creator gets <c>Content</c>: injected in converted content, the
/// temporary <c>GameServices.Content</c> in code not converted yet, a fixture property from its own container in a unit test;</item>
/// <item>a unit test that calls a member of a converted singleton gets a fixture property of the singleton from its container.</item>
/// </list>
/// What needs a person is refused, and a file with a refusal is left as it was.
/// </summary>
internal static class ContentRewriter
{
    public const string AccessorNamespace = "SWLOR.Game.Server.Composition";
    public const string UsingStatic = "using static " + AccessorNamespace + ".GameServices;";
    public const string ContainerType = "FacadeContainer";
    public const string TestContentNamespace = "SWLOR.Game.Server.Tests.Support";
    public const string TestContentProject = "SWLOR.Game.Server.Tests/";

    public static FileRewrite Apply(TreeFile file, ContentAnalysis analysis, bool converted, Func<TypeDirectory> types)
    {
        var text = file.Text;
        var eol = file.Source.LineEnding;
        var edits = new List<TextEdit>();
        var refusals = new List<Refusal>();
        var usings = new SortedSet<string>(StringComparer.Ordinal);
        var fixtureLines = new Dictionary<ClassDeclarationSyntax, List<string>>();
        var needsAccessorImport = false;
        var classes = converted ? analysis.Classes.Where(type => type.File == file).ToArray() : Array.Empty<ContentClass>();

        foreach (var type in classes)
        {
            refusals.AddRange(type.Refusals);
            if (type.Refusals.Count == 0)
                Convert(type, analysis, edits, usings, refusals, eol);
        }

        var made = analysis.MadeWithServices;
        if (made.Count > 0 || analysis.Singletons.Any())
            RewriteUses(file, analysis, made, classes, edits, refusals, usings, fixtureLines, ref needsAccessorImport, types);

        foreach (var (classSyntax, lines) in fixtureLines)
        {
            var indent = MemberIndent(text, classSyntax);
            var at = SyntaxHelpers.NextLineStart(text, classSyntax.OpenBraceToken.SpanStart);
            edits.Add(new TextEdit(at, 0, string.Concat(lines.Select(line => indent + line + eol + eol))));
        }

        if (refusals.Count > 0 || edits.Count == 0 && !needsAccessorImport && usings.Count == 0)
            return new FileRewrite(text, refusals, 0);

        var visible = TypeDirectory.VisibleNamespaces(file);
        var usingLines = usings.Where(space => !visible.Contains(space)).Select(space => "using " + space + ";").ToList();
        var hasAccessorImport = file.ImportsGameServicesGlobally || text.Contains(UsingStatic, StringComparison.Ordinal);
        if (needsAccessorImport && !hasAccessorImport)
            usingLines.Add(UsingStatic);

        if (classes.Any(Converts) && !StillNeedsAccessor(file, analysis, classes))
            RemoveAccessorImport(file, edits);

        if (usingLines.Count > 0)
        {
            var at = UsingInsertPoint(file);
            edits.Add(new TextEdit(at, 0, string.Concat(usingLines.Select(line => line + eol)) + (at == 0 ? eol : string.Empty)));
        }

        return new FileRewrite(TextEdit.Apply(text, edits), refusals, edits.Count);
    }

    /// <summary>The edits that convert one class.</summary>
    private static void Convert(ContentClass type, ContentAnalysis analysis, List<TextEdit> edits, SortedSet<string> usings, List<Refusal> refusals, string eol)
    {
        var text = type.File.Text;
        if (type.IsSingleton)
        {
            RemoveStatic(type.Syntax.Modifiers, edits, text);
            foreach (var member in type.Members.Where(member => member.IsStatic && type.InstanceMembers.Contains(member.Name)))
                RemoveStatic(member.Node.Modifiers, edits, text);
        }

        foreach (var member in type.Members)
        {
            foreach (var access in member.ExplicitAccesses)
            {
                if (analysis.Names.TryGet(access.Name.Identifier.ValueText, out var service))
                    edits.Add(new TextEdit(access.SpanStart, access.Span.Length, type.PropertyName(service)));
            }

            if (!type.IsSingleton)
                continue;

            foreach (var reference in member.OwnReferences.Where(reference => reference.Qualified && type.InstanceMembers.Contains(reference.Member)))
                edits.Add(new TextEdit(reference.Node.SpanStart, reference.Node.Span.Length, reference.Member));
        }

        if (!type.IsSingleton && !type.IsMadeWithServices)
            return;

        var desired = type.IsSingleton
            ? type.Needed.Select(name => analysis.Names.TryGet(name, out var service) ? service : null).OfType<ServiceName>().ToList()
            : type.CtorServices;
        var own = desired.Where(service => type.Needed.Contains(service.Name)).ToList();
        var baseServices = type.Base?.CtorServices ?? new List<ServiceName>();
        var newProperties = own.Where(service => type.Injected.Values.All(held => held.Name != service.Name)).ToList();
        foreach (var service in newProperties)
            usings.Add(service.Namespace);

        var indent = MemberIndent(text, type.Syntax);
        var propertyLines = newProperties.Select(service => $"{indent}private {service.Type} {type.PropertyName(service)} {{ get; }}{eol}").ToList();
        var constructors = type.Syntax.Members.OfType<ConstructorDeclarationSyntax>().Where(constructor => !constructor.Modifiers.Any(SyntaxKind.StaticKeyword)).ToArray();

        if (constructors.Length == 0)
        {
            var block = new System.Text.StringBuilder();
            foreach (var line in propertyLines)
                block.Append(line);
            if (propertyLines.Count > 0)
                block.Append(eol);

            var access = type.Syntax.Modifiers.Any(SyntaxKind.AbstractKeyword) ? "protected" : "public";
            var parameters = string.Join(", ", desired.Select(service => $"{service.Type} {Parameter(type, service)}"));
            block.Append($"{indent}{access} {type.Name}({parameters}){eol}");
            if (baseServices.Count > 0)
                block.Append($"{indent}    : base({string.Join(", ", baseServices.Select(service => Parameter(type, service)))}){eol}");
            block.Append($"{indent}{{{eol}");
            foreach (var service in own)
                block.Append($"{indent}    {type.PropertyName(service)} = {Parameter(type, service)};{eol}");
            block.Append($"{indent}}}{eol}{eol}");
            edits.Add(new TextEdit(PropertyInsertPoint(type, text), 0, block.ToString()));
            return;
        }

        if (propertyLines.Count > 0)
            edits.Add(new TextEdit(PropertyInsertPoint(type, text), 0, string.Concat(propertyLines) + (type.Injected.Count == 0 ? eol : string.Empty)));

        foreach (var constructor in constructors)
            ExtendConstructor(type, analysis, constructor, desired, own, baseServices, edits, refusals, eol);
    }

    /// <summary>Adds the services a constructor lacks: parameters after those it has, forwarded to <c>this</c> or the base, assigned to the properties.</summary>
    private static void ExtendConstructor(ContentClass type, ContentAnalysis analysis, ConstructorDeclarationSyntax constructor, IReadOnlyList<ServiceName> desired,
        IReadOnlyList<ServiceName> own, IReadOnlyList<ServiceName> baseServices, List<TextEdit> edits, List<Refusal> refusals, string eol)
    {
        var text = type.File.Text;
        var parameters = constructor.ParameterList.Parameters;
        var serviceTypes = analysis.Names.Names.Values.Select(service => service.Type).ToHashSet(StringComparer.Ordinal);
        var existing = parameters.TakeWhile(parameter => parameter.Type != null && SyntaxHelpers.SimpleName(parameter.Type) is { } name && serviceTypes.Contains(name)).ToList();
        var existingTypes = existing.Select(parameter => SyntaxHelpers.SimpleName(parameter.Type!)).ToHashSet(StringComparer.Ordinal);
        var missing = desired.Where(service => !existingTypes.Contains(service.Type)).ToList();
        if (missing.Count == 0)
            return;

        foreach (var service in missing)
        {
            var name = Parameter(type, service);
            if (parameters.Any(parameter => parameter.Identifier.ValueText == name.TrimStart('@')))
            {
                refusals.Add(type.File.Refuse(constructor, $"A constructor of {type.Name} already has a parameter named {name}: rename it first."));
                return;
            }
        }

        var added = string.Join(", ", missing.Select(service => $"{service.Type} {Parameter(type, service)}"));
        if (existing.Count > 0)
            edits.Add(new TextEdit(existing[^1].Span.End, 0, ", " + added));
        else if (parameters.Count > 0)
            edits.Add(new TextEdit(parameters[0].SpanStart, 0, added + ", "));
        else
            edits.Add(new TextEdit(constructor.ParameterList.OpenParenToken.Span.End, 0, added));

        var initializer = constructor.Initializer;
        if (initializer != null && initializer.ThisOrBaseKeyword.IsKind(SyntaxKind.ThisKeyword))
        {
            InsertArguments(initializer.ArgumentList, existing.Count, missing.Select(service => Parameter(type, service)), edits);
            return;
        }

        var baseMissing = missing.Where(service => baseServices.Any(baseService => baseService.Name == service.Name)).ToList();
        if (baseMissing.Count > 0)
        {
            if (initializer != null)
            {
                var passed = initializer.ArgumentList.Arguments.TakeWhile(argument =>
                    argument.Expression is IdentifierNameSyntax identifier && existing.Any(parameter => parameter.Identifier.ValueText == identifier.Identifier.ValueText)).Count();
                InsertArguments(initializer.ArgumentList, passed, baseMissing.Select(service => Parameter(type, service)), edits);
            }
            else
            {
                edits.Add(new TextEdit(constructor.ParameterList.Span.End, 0,
                    $" : base({string.Join(", ", baseServices.Select(service => Parameter(type, service)))})"));
            }
        }

        var assigned = missing.Where(service => own.Any(ownService => ownService.Name == service.Name)).ToList();
        if (assigned.Count == 0 || constructor.Body == null)
            return;

        var body = constructor.Body;
        var bodyIndent = SyntaxHelpers.IndentAt(text, body.OpenBraceToken.SpanStart) + "    ";
        var lastAssignment = body.Statements.TakeWhile(statement => statement is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: IdentifierNameSyntax left } } &&
                                                                    type.Injected.ContainsKey(left.Identifier.ValueText)).LastOrDefault();
        var at = SyntaxHelpers.NextLineStart(text, lastAssignment?.Span.End ?? body.OpenBraceToken.SpanStart);
        edits.Add(new TextEdit(at, 0, string.Concat(assigned.Select(service => $"{bodyIndent}{type.PropertyName(service)} = {Parameter(type, service)};{eol}"))));
    }

    private static void InsertArguments(ArgumentListSyntax list, int after, IEnumerable<string> names, List<TextEdit> edits)
    {
        var text = string.Join(", ", names);
        var arguments = list.Arguments;
        if (after > 0)
            edits.Add(new TextEdit(arguments[after - 1].Span.End, 0, ", " + text));
        else if (arguments.Count > 0)
            edits.Add(new TextEdit(arguments[0].SpanStart, 0, text + ", "));
        else
            edits.Add(new TextEdit(list.OpenParenToken.Span.End, 0, text));
    }

    /// <summary>Creation sites, Activator sites, singleton callers and subclasses of converted made types anywhere in the file.</summary>
    private static void RewriteUses(TreeFile file, ContentAnalysis analysis, IReadOnlyDictionary<string, ContentClass> made, IReadOnlyList<ContentClass> converted,
        List<TextEdit> edits, List<Refusal> refusals, SortedSet<string> usings, Dictionary<ClassDeclarationSyntax, List<string>> fixtureLines,
        ref bool needsAccessorImport, Func<TypeDirectory> types)
    {
        var text = file.Text;
        var mentions = made.Keys.Concat(analysis.Singletons.Select(type => type.Name)).Any(name => text.Contains(name, StringComparison.Ordinal));
        if (!mentions)
            return;

        var declaredHere = file.Root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Select(type => type.Identifier.ValueText).ToHashSet(StringComparer.Ordinal);
        var convertedHere = converted.Where(Converts).Select(type => type.Syntax).ToHashSet();

        foreach (var declaration in file.Root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            var baseName = declaration.BaseList?.Types.Select(baseType => SyntaxHelpers.SimpleName(baseType.Type)).FirstOrDefault();
            if (baseName != null && made.TryGetValue(baseName, out var baseType) && !convertedHere.Contains(declaration) && baseType.File != file)
                refusals.Add(file.Refuse(declaration, $"{declaration.Identifier.ValueText} derives from {baseName}, which takes services: convert it in the same wave (list its file in tools/Refactor/ContentInjection.json)."));
        }

        foreach (var creation in file.Root.DescendantNodes().OfType<BaseObjectCreationExpressionSyntax>())
        {
            var (typeText, typeName) = creation switch
            {
                ObjectCreationExpressionSyntax explicitCreation => (explicitCreation.Type.ToString(), SyntaxHelpers.SimpleName(explicitCreation.Type)),
                ImplicitObjectCreationExpressionSyntax implicitCreation => TargetType(implicitCreation),
                _ => (null, null)
            };

            if (typeName == null)
            {
                if (creation is ImplicitObjectCreationExpressionSyntax && made.Keys.Any(name => text.Contains(name, StringComparison.Ordinal)))
                    refusals.Add(file.Refuse(creation, "A target-typed new() whose type the tool cannot tell: if it builds made content with services, write Content.Create by hand."));
                continue;
            }

            if (!made.TryGetValue(typeName, out var target) || declaredHere.Contains(typeName) && target.File != file)
                continue;

            var owner = creation.Ancestors().OfType<ClassDeclarationSyntax>().LastOrDefault();
            if (owner != null && owner.Identifier.ValueText == target.Name && target.File == file)
            {
                SelfCopy(target, creation, edits, refusals);
                continue;
            }

            if (creation.ArgumentList?.Arguments.Any(argument => argument.NameColon != null) == true)
            {
                refusals.Add(file.Refuse(creation, $"new {typeName}(...) with named arguments: write Content.Create by hand."));
                continue;
            }

            if (creation.Initializer != null || creation.ArgumentList == null)
            {
                refusals.Add(file.Refuse(creation, $"new {typeName} with an object initializer: write Content.Create by hand."));
                continue;
            }

            var values = ValueTypes(target, creation.ArgumentList.Arguments.Count, file, types, out var problem);
            if (values == null)
            {
                refusals.Add(file.Refuse(creation, $"new {typeName}(...): {problem}"));
                continue;
            }

            if (!ProvideContent(file, analysis, creation, convertedHere, fixtureLines, refusals, ref needsAccessorImport))
                continue;

            // Only `new X` changes: the argument list keeps its text, so a creation inside the arguments is rewritten on its own.
            var end = creation is ObjectCreationExpressionSyntax withType ? withType.Type.Span.End : creation.NewKeyword.Span.End;
            var generic = string.Join(", ", new[] { typeText! }.Concat(values));
            edits.Add(new TextEdit(creation.SpanStart, end - creation.SpanStart, $"{ServiceName.ContentName}.Create<{generic}>"));
        }

        foreach (var invocation in file.Root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "Activator" } } activator ||
                activator.Name.Identifier.ValueText != "CreateInstance")
            {
                continue;
            }

            var createdName = activator.Name is GenericNameSyntax { TypeArgumentList.Arguments: [var argument] } ? SyntaxHelpers.SimpleName(argument)
                : invocation.ArgumentList.Arguments is [{ Expression: TypeOfExpressionSyntax typeOf }] ? SyntaxHelpers.SimpleName(typeOf.Type) : null;
            if (createdName == null || !made.ContainsKey(createdName))
                continue;

            if (file.Kind == ProjectKind.UnitTests)
            {
                refusals.Add(file.Refuse(invocation, $"Activator.CreateInstance of {createdName}: a test builds it with Content.Create by hand (the characterization project through its support helper)."));
                continue;
            }

            if (!ProvideContent(file, analysis, invocation, convertedHere, fixtureLines, refusals, ref needsAccessorImport))
                continue;

            var typeText = activator.Name is GenericNameSyntax generic2 ? generic2.TypeArgumentList.Arguments[0].ToString()
                : ((TypeOfExpressionSyntax)invocation.ArgumentList.Arguments[0].Expression).Type.ToString();
            edits.Add(new TextEdit(invocation.SpanStart, invocation.Span.Length, $"{ServiceName.ContentName}.Create<{typeText}>()"));
        }

        foreach (var access in file.Root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (access.Expression is not IdentifierNameSyntax receiver || !analysis.ByName.TryGetValue(receiver.Identifier.ValueText, out var singleton) ||
                !singleton.IsSingleton || singleton.Refusals.Count > 0 || !singleton.InstanceMembers.Contains(access.Name.Identifier.ValueText))
            {
                continue;
            }

            var owner = access.Ancestors().OfType<ClassDeclarationSyntax>().LastOrDefault();
            if (owner == null || convertedHere.Contains(owner) || owner.Members.SelectMany(SyntaxHelpers.MemberNames).Contains(singleton.Name))
                continue;
            if (access.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault() is { } member && SyntaxHelpers.DeclaredLocals(member).Contains(singleton.Name))
                continue;

            if (file.Kind == ProjectKind.UnitTests && ContainerField(owner) is { } container && !InStaticContext(access, owner))
            {
                var typeName = TypeDirectory.VisibleNamespaces(file).Contains(singleton.Namespace) ? singleton.Name : singleton.FullName;
                AddFixtureLine(fixtureLines, owner, $"private {typeName} {singleton.Name} => {container}.Get<{typeName}>();");
                continue;
            }

            refusals.Add(file.Refuse(access, $"{singleton.Name}.{access.Name.Identifier.ValueText} becomes an instance member, and this caller cannot take {singleton.Name}: convert the caller in the same wave."));
        }
    }

    /// <summary>A class copying itself passes its own services first.</summary>
    private static void SelfCopy(ContentClass type, BaseObjectCreationExpressionSyntax creation, List<TextEdit> edits, List<Refusal> refusals)
    {
        if (type.Base?.CtorServices.Count > 0)
        {
            refusals.Add(type.File.Refuse(creation, $"{type.Name} copies itself, but its base takes services it does not hold: write the copy by hand."));
            return;
        }

        if (creation.ArgumentList == null)
            return;

        var properties = type.CtorServices.Select(type.PropertyName).ToList();
        var arguments = creation.ArgumentList.Arguments;
        var passed = arguments.TakeWhile(argument => argument.Expression is IdentifierNameSyntax identifier && properties.Contains(identifier.Identifier.ValueText)).Count();
        var missing = properties.Skip(passed).ToList();
        if (missing.Count > 0)
            InsertArguments(creation.ArgumentList, passed, missing, edits);
    }

    /// <summary>
    /// Makes <c>Content</c> mean a content factory where <paramref name="site"/> is: converted content has it injected; a unit test gets
    /// a fixture property from its container; other code that is not converted uses the temporary <c>GameServices.Content</c>.
    /// </summary>
    private static bool ProvideContent(TreeFile file, ContentAnalysis analysis, SyntaxNode site, HashSet<ClassDeclarationSyntax> converted,
        Dictionary<ClassDeclarationSyntax, List<string>> fixtureLines, List<Refusal> refusals, ref bool needsAccessorImport)
    {
        var owner = site.Ancestors().OfType<ClassDeclarationSyntax>().LastOrDefault();
        if (owner != null && converted.Contains(owner))
            return true;
        if (owner != null && owner.Members.SelectMany(SyntaxHelpers.MemberNames).Contains(ServiceName.ContentName))
            return true;

        switch (file.Kind)
        {
            case ProjectKind.UnitTests:
                if (owner == null || InStaticContext(site, owner))
                {
                    refusals.Add(file.Refuse(site, "Made content is created in a static context of a test: the fixture's Content is an instance property, so convert this by hand."));
                    return false;
                }

                var factory = TypeDirectory.VisibleNamespaces(file).Contains(ServiceName.ContentNamespace)
                    ? ServiceName.ContentType
                    : ServiceName.ContentNamespace + "." + ServiceName.ContentType;
                var containers = owner.Members.OfType<FieldDeclarationSyntax>().Where(field => SyntaxHelpers.SimpleName(field.Declaration.Type) == ContainerType)
                    .SelectMany(field => field.Declaration.Variables).ToArray();
                if (containers.Length > 1)
                {
                    refusals.Add(file.Refuse(owner, $"{owner.Identifier.ValueText} has {containers.Length} containers: choose the one Content comes from by hand."));
                    return false;
                }

                if (containers.Length == 1)
                {
                    AddFixtureLine(fixtureLines, owner, $"private {factory} {ServiceName.ContentName} => {containers[0].Identifier.ValueText}.Content;");
                    return true;
                }

                if (!file.RelativePath.StartsWith(TestContentProject, StringComparison.Ordinal))
                {
                    refusals.Add(file.Refuse(owner, $"{owner.Identifier.ValueText} has no container and its project has no TestContent: give it Content by hand."));
                    return false;
                }

                var testContent = TypeDirectory.VisibleNamespaces(file).Contains(TestContentNamespace) ? "TestContent" : TestContentNamespace + ".TestContent";
                AddFixtureLine(fixtureLines, owner, $"private {factory} {ServiceName.ContentName} {{ get; }} = {testContent}.WithoutServices;");
                return true;

            case ProjectKind.Tool:
                refusals.Add(file.Refuse(site, "A tool creates made content with services: it takes IContentFactory once at its entry, by hand."));
                return false;

            default:
                if (file.IsServiceCode)
                {
                    refusals.Add(file.Refuse(site, "A class the container builds creates made content: it takes IContentFactory as _content through its constructor, by hand."));
                    return false;
                }

                needsAccessorImport = true;
                return true;
        }
    }

    private static void AddFixtureLine(Dictionary<ClassDeclarationSyntax, List<string>> fixtureLines, ClassDeclarationSyntax owner, string line)
    {
        if (!fixtureLines.TryGetValue(owner, out var lines))
            fixtureLines[owner] = lines = new List<string>();
        if (!lines.Contains(line))
            lines.Add(line);
    }

    private static string? ContainerField(ClassDeclarationSyntax owner)
    {
        var containers = owner.Members.OfType<FieldDeclarationSyntax>().Where(field => SyntaxHelpers.SimpleName(field.Declaration.Type) == ContainerType)
            .SelectMany(field => field.Declaration.Variables).ToArray();
        return containers.Length == 1 ? containers[0].Identifier.ValueText : null;
    }

    private static bool InStaticContext(SyntaxNode site, ClassDeclarationSyntax owner) =>
        owner.Modifiers.Any(SyntaxKind.StaticKeyword) ||
        site.Ancestors().OfType<MemberDeclarationSyntax>().TakeWhile(member => member != owner).Any(member => member.Modifiers.Any(SyntaxKind.StaticKeyword));

    /// <summary>The declared type a target-typed <c>new(...)</c> builds, when a declaration names it.</summary>
    private static (string? Text, string? Name) TargetType(ImplicitObjectCreationExpressionSyntax creation)
    {
        TypeSyntax? type = creation.Parent switch
        {
            EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax declaration } } => declaration.Type,
            EqualsValueClauseSyntax { Parent: PropertyDeclarationSyntax property } => property.Type,
            _ => null
        };

        if (type == null || type.IsVar)
            return (null, null);
        return (type.ToString(), SyntaxHelpers.SimpleName(type));
    }

    /// <summary>
    /// The value types of a creation with <paramref name="count"/> arguments: the types of the first values of the constructors that take
    /// that many (the rest optional), which must agree.
    /// </summary>
    private static IReadOnlyList<string>? ValueTypes(ContentClass target, int count, TreeFile creator, Func<TypeDirectory> types, out string problem)
    {
        problem = string.Empty;
        var serviceTypes = new HashSet<string>(target.CtorServices.Select(service => service.Type), StringComparer.Ordinal) { ServiceName.ContentType };
        var constructors = target.Syntax.Members.OfType<ConstructorDeclarationSyntax>().Where(constructor => !constructor.Modifiers.Any(SyntaxKind.StaticKeyword)).ToArray();
        var shapes = constructors.Length == 0
            ? new List<IReadOnlyList<ParameterSyntax>> { Array.Empty<ParameterSyntax>() }
            : constructors.Select(constructor => (IReadOnlyList<ParameterSyntax>)constructor.ParameterList.Parameters
                .SkipWhile(parameter => parameter.Type != null && SyntaxHelpers.SimpleName(parameter.Type) is { } name && serviceTypes.Contains(name)).ToList()).ToList();

        var fitting = shapes.Where(shape => shape.Count >= count && shape.Skip(count).All(parameter => parameter.Default != null)).ToList();
        var candidates = fitting.Select(shape => shape.Take(count).Select(parameter => parameter.Type!.ToString()).ToList())
            .GroupBy(values => string.Join(",", values), StringComparer.Ordinal).ToList();
        if (candidates.Count == 0)
        {
            problem = $"no constructor of {target.Name} takes {count} value(s).";
            return null;
        }

        if (candidates.Count > 1)
        {
            problem = $"constructors of {target.Name} take {count} value(s) of different types ({string.Join("; ", candidates.Select(group => group.Key))}): write Content.Create by hand.";
            return null;
        }

        var qualified = new List<string>();
        foreach (var value in candidates[0].First())
        {
            var text = SyntaxHelpers.IsPredefined(value) ? value : types().Qualify(value, target.File, creator);
            if (text == null)
            {
                problem = $"the tool cannot tell which type {value} is: write Content.Create by hand.";
                return null;
            }

            qualified.Add(text);
        }

        return qualified;
    }

    /// <summary>True when a lookup the conversion does not serve is left in the file, so the <c>GameServices</c> import stays.</summary>
    private static bool StillNeedsAccessor(TreeFile file, ContentAnalysis analysis, IReadOnlyList<ContentClass> converted)
    {
        var convertedSyntax = converted.Where(Converts).Select(type => type.Syntax).ToArray();
        foreach (var identifier in file.Root.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            if (convertedSyntax.Any(syntax => syntax.Span.Contains(identifier.Span)))
                continue;
            if (SyntaxHelpers.IsReceiver(identifier) && analysis.Names.TryGet(identifier.Identifier.ValueText, out var service) && !service.IsSingleton)
                return true;
        }

        return false;
    }

    private static void RemoveAccessorImport(TreeFile file, List<TextEdit> edits)
    {
        foreach (var directive in file.Root.DescendantNodes().OfType<UsingDirectiveSyntax>())
        {
            if (directive.GlobalKeyword.ValueText.Length > 0 || directive.StaticKeyword.ValueText.Length == 0 || directive.Name?.ToString() != AccessorNamespace + ".GameServices")
                continue;

            var start = SyntaxHelpers.LineStart(file.Text, directive.SpanStart);
            edits.Add(new TextEdit(start, SyntaxHelpers.NextLineStart(file.Text, directive.Span.End) - start, string.Empty));
        }
    }

    private static int UsingInsertPoint(TreeFile file)
    {
        var last = file.Root.DescendantNodes().OfType<UsingDirectiveSyntax>().LastOrDefault(directive => directive.GlobalKeyword.ValueText.Length == 0);
        return last == null ? 0 : SyntaxHelpers.NextLineStart(file.Text, last.Span.End);
    }

    private static int PropertyInsertPoint(ContentClass type, string text)
    {
        var lastInjected = type.Syntax.Members.OfType<PropertyDeclarationSyntax>().LastOrDefault(property => type.Injected.ContainsKey(property.Identifier.ValueText));
        return SyntaxHelpers.NextLineStart(text, lastInjected?.Span.End ?? type.Syntax.OpenBraceToken.SpanStart);
    }

    private static string MemberIndent(string text, ClassDeclarationSyntax type)
    {
        var first = type.Members.FirstOrDefault();
        return first != null ? SyntaxHelpers.IndentAt(text, first.SpanStart) : SyntaxHelpers.IndentAt(text, type.SpanStart) + "    ";
    }

    /// <summary>True when a class of a converted file changes: a singleton, or made content that takes services.</summary>
    private static bool Converts(ContentClass type) => type.Refusals.Count == 0 && (type.IsSingleton || type.IsMadeWithServices);

    private static string Parameter(ContentClass type, ServiceName service) => SyntaxHelpers.ParameterName(type.PropertyName(service));

    private static void RemoveStatic(SyntaxTokenList modifiers, List<TextEdit> edits, string text)
    {
        var token = modifiers.FirstOrDefault(modifier => modifier.IsKind(SyntaxKind.StaticKeyword));
        if (token.RawKind == 0)
            return;

        var end = token.Span.End;
        while (end < text.Length && text[end] == ' ')
            end++;
        edits.Add(new TextEdit(token.SpanStart, end - token.SpanStart, string.Empty));
    }
}
