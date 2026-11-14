namespace SWLOR.EngineSeamGenerator;

/// <summary>A problem with the input that stops the generator before anything is written.</summary>
internal sealed class GeneratorException(string message) : Exception(message);
