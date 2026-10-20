namespace SWLOR.ContributorSetup;

internal sealed record CommandResult(int ExitCode, string Output = "", string Error = "");
