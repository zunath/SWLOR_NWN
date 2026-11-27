namespace SWLOR.TypeMoves;

/// <summary>The input cannot be processed safely; the run writes nothing.</summary>
internal sealed class TypeMoveException(string message) : Exception(message);
