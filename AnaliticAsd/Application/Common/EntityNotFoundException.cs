namespace AnaliticAsd.Application.Common;

public sealed class EntityNotFoundException(string message) : Exception(message);
