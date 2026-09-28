namespace AnaliticAsd.Application.Common;

public sealed class ExternalServiceException(string message) : Exception(message);

public sealed class ServiceConfigurationException(string message) : Exception(message);
