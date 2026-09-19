using SWLOR.Toolset.Domain.Conversations;

if (args.Length != 2 || args.Any(argument => argument.StartsWith("--", StringComparison.Ordinal)))
{
    Console.Error.WriteLine("Usage: SWLOR.ConversationMigrator <input.dlg.json> <new-output.conversation.json>");
    Console.Error.WriteLine("This imports one legacy file. Existing SWLOR conversations must be edited directly; bulk regeneration and --overwrite are not supported.");
    return 1;
}

try
{
    var result = ConversationImporter.Import(Path.GetFullPath(args[0]), Path.GetFullPath(args[1]));
    foreach (var issue in result.Issues)
        Console.WriteLine($"{issue.Location}: {issue.Message}");
    Console.WriteLine($"Imported '{result.Graph.Id}' to {Path.GetFullPath(args[1])}.");
    Console.WriteLine("Author subsequent changes in the SWLOR graph. Assign its ID to the object's Conversation field and route its interaction through dialog_start.");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}
