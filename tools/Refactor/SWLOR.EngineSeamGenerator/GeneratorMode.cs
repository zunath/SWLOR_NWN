namespace SWLOR.EngineSeamGenerator;

/// <summary>What a generator run does with its result.</summary>
internal enum GeneratorMode
{
    /// <summary>Write the output into the working tree.</summary>
    Apply,

    /// <summary>Write nothing; fail when the working tree differs from what the generator produces.</summary>
    Check,

    /// <summary>Write nothing; produce the output from the wrapper files at a git revision and fail when the working tree differs.</summary>
    VerifyOrigin
}
