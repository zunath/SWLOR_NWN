using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.ContentInjection;

/// <summary>
/// Classifies the classes of the converted content files and finds what each reaches (MaintainabilityRefactorContentInjection.md,
/// "What the tool does"): a static class, a class of static members only, or a registered one is a candidate singleton, which
/// becomes one when a member reaches a service; any other class is made content. A member reaches a service when it names one
/// (bare, through the <c>GameServices</c> import, or <c>GameServices.X</c>), calls a member of a singleton that becomes an instance
/// member, or calls one of its own members that reaches one, solved to a fixed point over every class. Then each class gets the
/// services its constructor takes, <c>Content</c> when it creates made content with services, and the refusals that need a person.
/// </summary>
internal static class ContentAnalyzer
{
    /// <summary>The most services a class takes (target 2): constructor service parameters, the factory and singletons counting one each.</summary>
    public const int MaximumServices = 10;

    public static ContentAnalysis Analyze(IReadOnlyList<TreeFile> converted, NameTable names)
    {
        var classes = new List<ContentClass>();
        foreach (var file in converted)
        {
            foreach (var syntax in file.Root.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(SyntaxHelpers.IsTopLevel))
                classes.Add(Describe(file, syntax, names));
        }

        var refusals = new List<Refusal>();
        foreach (var group in classes.GroupBy(type => type.Name, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            refusals.AddRange(group.Select(type => type.File.Refuse(type.Syntax,
                $"{type.Name} is the name of {group.Count()} converted classes; the tool finds creation sites and callers by simple name, so it converts none of them.")));
        }

        var byName = classes.GroupBy(type => type.Name, StringComparer.Ordinal).Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);
        foreach (var type in classes)
            type.Base = type.BaseName != null && byName.TryGetValue(type.BaseName, out var baseClass) ? baseClass : null;

        foreach (var type in classes.Where(type => type.IsSingletonCandidate))
            names.AddSingleton(type.FullName);

        foreach (var type in classes)
            Collect(type, names);

        Reach(classes, byName, names);
        foreach (var type in classes)
            Needs(type, byName, names);

        AddContent(classes, byName, names);
        RefuseCycles(classes, byName);
        foreach (var type in classes.Where(type => type.CtorServices.Count > MaximumServices || type.IsSingleton && type.Needed.Count > MaximumServices))
            type.Refusals.Add(type.File.Refuse(type.Syntax, $"{type.Name} would take {Math.Max(type.CtorServices.Count, type.Needed.Count)} services, more than {MaximumServices}: split it first."));

        return new ContentAnalysis(classes, names, refusals);
    }

    private static ContentClass Describe(TreeFile file, ClassDeclarationSyntax syntax, NameTable names)
    {
        var type = new ContentClass(file, syntax, SyntaxHelpers.NamespaceOf(syntax));
        type.BaseName = syntax.BaseList?.Types.Select(baseType => SyntaxHelpers.SimpleName(baseType.Type))
            .FirstOrDefault(name => name != null && !(name.Length > 1 && name[0] == 'I' && char.IsUpper(name[1])));

        var serviceTypes = names.Names.Values.GroupBy(service => service.Type, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        foreach (var member in syntax.Members)
        {
            foreach (var name in SyntaxHelpers.MemberNames(member))
                type.DeclaredNames.Add(name);

            if (member is PropertyDeclarationSyntax property && IsInjectedShape(property) &&
                SyntaxHelpers.SimpleName(property.Type) is { } typeName && serviceTypes.TryGetValue(typeName, out var service))
            {
                type.Injected[property.Identifier.ValueText] = service;
            }

            switch (member)
            {
                case FieldDeclarationSyntax field:
                    foreach (var variable in field.Declaration.Variables)
                        type.Members.Add(new ContentMember(member, variable.Identifier.ValueText, SyntaxHelpers.IsStatic(member)));
                    break;
                case ConstructorDeclarationSyntax constructor:
                    var isStatic = constructor.Modifiers.Any(SyntaxKind.StaticKeyword);
                    type.Members.Add(new ContentMember(member, isStatic ? ".cctor" : ".ctor", isStatic));
                    break;
                case BaseTypeDeclarationSyntax or DelegateDeclarationSyntax:
                    break;
                default:
                    var memberNames = SyntaxHelpers.MemberNames(member).ToArray();
                    type.Members.Add(new ContentMember(member, memberNames.Length > 0 ? memberNames[0] : member.Kind().ToString(), SyntaxHelpers.IsStatic(member)));
                    break;
            }
        }

        var instanceMembers = type.Members.Where(member => !member.IsStatic && !type.Injected.ContainsKey(member.Name)).ToArray();
        type.IsSingletonCandidate = syntax.Modifiers.Any(SyntaxKind.StaticKeyword) ||
                                    names.RegisteredSingletons.Contains(type.FullName) ||
                                    !syntax.Modifiers.Any(SyntaxKind.AbstractKeyword) && type.BaseName == null && type.Members.Count > 0 &&
                                    instanceMembers.All(member => member.Name == ".ctor");
        return type;
    }

    /// <summary><c>private T Name { get; }</c>: the shape of an injected service.</summary>
    private static bool IsInjectedShape(PropertyDeclarationSyntax property) =>
        property.Modifiers.Count == 1 && property.Modifiers[0].IsKind(SyntaxKind.PrivateKeyword) && property.Initializer == null &&
        property.ExpressionBody == null && property.AccessorList is { Accessors: [{ Body: null, ExpressionBody: null } accessor] } &&
        accessor.IsKind(SyntaxKind.GetAccessorDeclaration);

    /// <summary>The names each member reaches, its own members it names and the singleton members it calls.</summary>
    private static void Collect(ContentClass type, NameTable names)
    {
        foreach (var member in type.Members)
        {
            var locals = SyntaxHelpers.DeclaredLocals(member.Node);
            foreach (var identifier in member.Node.DescendantNodes().OfType<IdentifierNameSyntax>())
            {
                if (SyntaxHelpers.InsideNameof(identifier))
                    continue;

                var name = identifier.Identifier.ValueText;
                if (identifier.Parent is MemberAccessExpressionSyntax access && access.Name == identifier)
                {
                    if (SyntaxHelpers.IsAccessor(access.Expression))
                    {
                        if (names.TryGet(name, out var explicitService) && !explicitService.IsSingleton)
                        {
                            member.Lookups.Add(name);
                            member.ExplicitAccesses.Add(access);
                        }
                    }
                    else if (access.Expression is IdentifierNameSyntax receiver && !locals.Contains(receiver.Identifier.ValueText))
                    {
                        var receiverName = receiver.Identifier.ValueText;
                        if (receiverName == type.Name)
                            member.OwnReferences.Add((access, name, SyntaxHelpers.IsInvoked(access), true));
                        else if (!type.DeclaredNames.Contains(receiverName) && names.TryGet(receiverName, out var singleton) && singleton.IsSingleton)
                            member.SingletonCalls.Add((receiverName, name, access, SyntaxHelpers.IsInvoked(access)));
                    }

                    continue;
                }

                if (identifier.Parent is QualifiedNameSyntax qualified && qualified.Right == identifier || identifier.Parent is MemberBindingExpressionSyntax)
                    continue;
                if (locals.Contains(name))
                    continue;

                if (type.DeclaredNames.Contains(name))
                {
                    if (type.Injected.TryGetValue(name, out var held))
                        member.Lookups.Add(held.Name);
                    else
                        member.OwnReferences.Add((identifier, name, SyntaxHelpers.IsInvoked(identifier), false));
                    continue;
                }

                if (SyntaxHelpers.IsReceiver(identifier) && name != type.Name && names.TryGet(name, out var service) && !service.IsSingleton)
                    member.Lookups.Add(name);
            }
        }

        foreach (var nested in type.Syntax.Members.OfType<BaseTypeDeclarationSyntax>())
        {
            if (nested.DescendantNodes().OfType<IdentifierNameSyntax>().Any(identifier =>
                    SyntaxHelpers.IsReceiver(identifier) && names.TryGet(identifier.Identifier.ValueText, out _) ||
                    identifier.Parent is MemberAccessExpressionSyntax access && SyntaxHelpers.IsAccessor(access.Expression)))
            {
                type.Refusals.Add(type.File.Refuse(nested, $"{type.Name}.{nested.Identifier.ValueText} is a nested type that reaches a service: move it out, or make it a collaborator the class passes its services to."));
            }
        }
    }

    /// <summary>Which members reach a service, to a fixed point over every class; a candidate singleton's reaching members become instance members.</summary>
    private static void Reach(IReadOnlyList<ContentClass> classes, IReadOnlyDictionary<string, ContentClass> byName, NameTable names)
    {
        foreach (var type in classes.Where(type => type.IsSingletonCandidate))
        {
            foreach (var member in type.Members.Where(member => !member.IsStatic && member.Name != ".ctor" && !type.Injected.ContainsKey(member.Name)))
            {
                member.Reaches = true;
                type.InstanceMembers.Add(member.Name);
            }
        }

        bool changed;
        do
        {
            changed = false;
            foreach (var type in classes)
            {
                foreach (var member in type.Members.Where(member => !member.Reaches))
                {
                    var reaches = member.Lookups.Count > 0 ||
                                  member.SingletonCalls.Any(call => BecomesInstance(call.Singleton, call.Member, byName, names)) ||
                                  member.OwnReferences.Any(reference => type.Members.Any(other => other.Name == reference.Member && other.Reaches));
                    if (!reaches)
                        continue;

                    member.Reaches = true;
                    changed = true;
                    if (type.IsSingletonCandidate && member.Name is not ".ctor" and not ".cctor")
                        type.InstanceMembers.Add(member.Name);
                }
            }
        }
        while (changed);
    }

    /// <summary>True when <paramref name="member"/> of the singleton named <paramref name="singleton"/> is (or becomes) an instance member.</summary>
    public static bool BecomesInstance(string singleton, string member, IReadOnlyDictionary<string, ContentClass> byName, NameTable names)
    {
        if (byName.TryGetValue(singleton, out var type))
            return type.IsSingletonCandidate && type.InstanceMembers.Contains(member);

        return names.TryGet(singleton, out var service) && service.IsSingleton && names.RegisteredSingletons.Contains(service.FullType);
    }

    /// <summary>The services a class takes, and what keeps it from converting.</summary>
    private static void Needs(ContentClass type, IReadOnlyDictionary<string, ContentClass> byName, NameTable names)
    {
        foreach (var held in type.Injected.Values)
            type.Needed.Add(held.Name);

        if (type.IsSingletonCandidate)
        {
            if (!type.IsSingleton)
                return;

            foreach (var member in type.Members.Where(member => member.Reaches))
                AddReached(type, member, byName, names);

            foreach (var member in type.Members)
            {
                var node = member.Node;
                if (member.Reaches && member.IsStatic && node is FieldDeclarationSyntax)
                    type.Refusals.Add(type.File.Refuse(node, $"The static field {type.Name}.{member.Name} reads a service in its initializer: move the read to first use."));
                else if (member.Reaches && node is PropertyDeclarationSyntax { Initializer: not null })
                    type.Refusals.Add(type.File.Refuse(node, $"The static property {type.Name}.{member.Name} reads a service in its initializer: move the read to first use."));
                else if (member.Reaches && member.Name == ".cctor")
                    type.Refusals.Add(type.File.Refuse(node, $"The static constructor of {type.Name} reaches a service: move that work to first use."));
                else if (member.Reaches && node is MethodDeclarationSyntax method && method.ParameterList.Parameters.FirstOrDefault()?.Modifiers.Any(SyntaxKind.ThisKeyword) == true)
                    type.Refusals.Add(type.File.Refuse(node, $"The extension method {type.Name}.{member.Name} reaches a service: an extension method cannot be an instance member."));
                else if (member.Name == ".ctor" && !type.Injected.Any() && !names.RegisteredSingletons.Contains(type.FullName))
                    type.Refusals.Add(type.File.Refuse(node, $"{type.Name} becomes a content singleton but declares a constructor of its own: remove it first."));

                if (node is FieldDeclarationSyntax { } field && member.IsStatic && !field.Modifiers.Any(SyntaxKind.ConstKeyword) && !field.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
                    type.Refusals.Add(type.File.Refuse(node, $"{type.Name}.{member.Name} is runtime state of a content singleton: move it to a service first."));
                if (node is PropertyDeclarationSyntax { AccessorList: { } accessors } && member.IsStatic &&
                    accessors.Accessors.Any(accessor => accessor.IsKind(SyntaxKind.SetAccessorDeclaration) && accessor.Body == null && accessor.ExpressionBody == null))
                    type.Refusals.Add(type.File.Refuse(node, $"{type.Name}.{member.Name} is runtime state of a content singleton: move it to a service first."));
            }

            foreach (var member in type.Members)
            {
                foreach (var reference in member.OwnReferences.Where(reference => !reference.Invoked))
                {
                    if (type.Members.Any(other => other.Name == reference.Member && other.IsMethod && other.IsStatic && other.Reaches))
                        type.Refusals.Add(type.File.Refuse(reference.Node, $"{type.Name}.{reference.Member} becomes an instance method and is used as a method group here: write a lambda (a method group of an instance method allocates a delegate on each use)."));
                }
            }
        }
        else
        {
            foreach (var member in type.Members)
            {
                if (member.IsStatic && member.Reaches && IsPrivateHelper(member))
                {
                    // Only the class calls it, and every caller in the class reaches a service too, so it is (or becomes) an instance member.
                    type.StaticToInstance.Add(member.Name);
                    AddReached(type, member, byName, names);
                    continue;
                }

                if (member.IsStatic && member.Reaches)
                {
                    type.Refusals.Add(type.File.Refuse(member.Node, $"The static member {type.Name}.{member.Name} of made content reaches a service: move it (and its state) to a singleton or service; a handler there is renamed in HandlerRenames.txt."));
                    continue;
                }

                if (!member.IsStatic && member.Reaches && member.Node is FieldDeclarationSyntax or PropertyDeclarationSyntax { Initializer: not null })
                {
                    type.Refusals.Add(type.File.Refuse(member.Node, $"The field initializer of {type.Name}.{member.Name} reads a service: an initializer runs before the constructor sets the service; move the read into the constructor."));
                    continue;
                }

                if (!member.IsStatic)
                    AddReached(type, member, byName, names);
            }

            foreach (var member in type.Members)
            {
                foreach (var reference in member.OwnReferences.Where(reference => !reference.Invoked && type.StaticToInstance.Contains(reference.Member)))
                    type.Refusals.Add(type.File.Refuse(reference.Node, $"{type.Name}.{reference.Member} becomes an instance method and is used as a method group here: write a lambda (a method group of an instance method allocates a delegate on each use)."));
            }

            foreach (var constructor in type.Syntax.Members.OfType<ConstructorDeclarationSyntax>().Where(constructor => constructor.ExpressionBody != null))
            {
                if (type.Needed.Count > 0)
                    type.Refusals.Add(type.File.Refuse(constructor, $"An expression-bodied constructor of {type.Name}: write it with a body first."));
            }
        }
    }

    /// <summary>A private static method that is no handler and no extension method: only its own class calls it.</summary>
    private static bool IsPrivateHelper(ContentMember member) =>
        member.Node is MethodDeclarationSyntax method &&
        !method.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PublicKeyword) || modifier.IsKind(SyntaxKind.InternalKeyword) || modifier.IsKind(SyntaxKind.ProtectedKeyword)) &&
        method.AttributeLists.Count == 0 &&
        method.ParameterList.Parameters.FirstOrDefault()?.Modifiers.Any(SyntaxKind.ThisKeyword) != true;

    private static void AddReached(ContentClass type, ContentMember member, IReadOnlyDictionary<string, ContentClass> byName, NameTable names)
    {
        foreach (var lookup in member.Lookups)
            type.Needed.Add(lookup);

        foreach (var call in member.SingletonCalls)
        {
            if (!BecomesInstance(call.Singleton, call.Member, byName, names))
                continue;

            type.Needed.Add(call.Singleton);
            if (!call.Invoked)
                type.Refusals.Add(type.File.Refuse(call.Node, $"{call.Singleton}.{call.Member} becomes an instance method and is used as a method group here: write a lambda."));
        }
    }

    /// <summary>
    /// The constructor services of every class (the base's, then its own) and <c>Content</c> for each class that creates made content
    /// with services, to a fixed point (taking <c>Content</c> can make a type one that takes services).
    /// </summary>
    private static void AddContent(IReadOnlyList<ContentClass> classes, IReadOnlyDictionary<string, ContentClass> byName, NameTable names)
    {
        bool changed;
        do
        {
            foreach (var type in classes)
                type.CtorServices.Clear();
            foreach (var type in classes)
                CtorServices(type, names, new HashSet<ContentClass>());

            changed = false;
            var made = classes.Where(type => type.IsMadeWithServices).Select(type => type.Name).ToHashSet(StringComparer.Ordinal);
            foreach (var type in classes.Where(type => !type.Needed.Contains(ServiceName.ContentName)))
            {
                var creates = type.Syntax.DescendantNodes().OfType<ObjectCreationExpressionSyntax>()
                    .Any(creation => SyntaxHelpers.SimpleName(creation.Type) is { } created && created != type.Name && made.Contains(created));
                if (!creates)
                    continue;

                if (type.IsSingleton || !type.IsSingletonCandidate)
                {
                    type.Needed.Add(ServiceName.ContentName);
                    changed = true;
                }
                else if (type.Refusals.All(refusal => !refusal.Reason.Contains("creates made content", StringComparison.Ordinal)))
                {
                    type.Refusals.Add(type.File.Refuse(type.Syntax, $"The static class {type.Name} creates made content with services but reaches no other service: convert it by hand (it becomes a singleton that takes IContentFactory)."));
                }
            }
        }
        while (changed);
    }

    private static void CtorServices(ContentClass type, NameTable names, HashSet<ContentClass> visiting)
    {
        if (type.CtorServices.Count > 0 || !visiting.Add(type))
            return;

        if (type.Base != null && !type.IsSingletonCandidate)
        {
            CtorServices(type.Base, names, visiting);
            type.CtorServices.AddRange(type.Base.CtorServices);
        }

        if (type.IsSingletonCandidate && !type.IsSingleton)
            return;

        foreach (var name in type.Needed)
        {
            if (names.TryGet(name, out var service) && type.CtorServices.All(existing => existing.Name != service.Name))
                type.CtorServices.Add(service);
        }
    }

    /// <summary>Content singletons that need each other, directly or through others: the container cannot build them, so none converts.</summary>
    private static void RefuseCycles(IReadOnlyList<ContentClass> classes, IReadOnlyDictionary<string, ContentClass> byName)
    {
        var singletons = classes.Where(type => type.IsSingleton).ToArray();
        var edges = singletons.ToDictionary(type => type,
            type => type.Needed.Where(name => byName.TryGetValue(name, out var other) && other.IsSingleton).Select(name => byName[name]).ToArray());
        foreach (var cycle in CycleFinder.Find(singletons, type => edges[type]))
        {
            var names = string.Join(", ", cycle.Select(type => type.Name).OrderBy(name => name, StringComparer.Ordinal));
            foreach (var type in cycle)
                type.Refusals.Add(type.File.Refuse(type.Syntax, $"The content singletons {names} need each other, which the container cannot build: break the loop by hand first."));
        }
    }
}
