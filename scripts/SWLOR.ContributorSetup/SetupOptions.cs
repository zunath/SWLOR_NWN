namespace SWLOR.ContributorSetup;

internal sealed record SetupOptions(string Mode, string Profile, bool Help = false)
{
    public static SetupOptions Parse(string[] args)
    {
        var mode = "Check";
        var profile = "Toolset";
        for (var index = 0; index < args.Length; index++)
        {
            if (args[index] is "--help" or "-h") return new(mode, profile, Help: true);
            var option = args[index].ToLowerInvariant();
            if (option is not ("--mode" or "--profile") || ++index == args.Length)
                throw new ArgumentException("Use --mode Check|Setup|Toolset|Server and --profile Toolset|Server.");
            var choices = option == "--mode" ? new[] { "Check", "Setup", "Toolset", "Server" } : new[] { "Toolset", "Server" };
            var value = choices.FirstOrDefault(choice => choice.Equals(args[index], StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException("Invalid value for " + option + ": " + args[index]);
            if (option == "--mode") mode = value;
            else profile = value;
        }
        // Launch shortcuts always launch their named application; profiles select Setup/Check only.
        if (mode is "Toolset" or "Server") profile = mode;
        return new(mode, profile);
    }
}
