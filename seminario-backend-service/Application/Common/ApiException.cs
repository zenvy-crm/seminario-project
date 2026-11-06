namespace Seminario.Application.Common;

public class ApiException : Exception
{
    public int StatusCode { get; }
    public List<string> Errors { get; }

    public ApiException(int statusCode, string message, List<string>? errors = null) : base(message)
    {
        StatusCode = statusCode;
        Errors = errors ?? [];
    }

    public ApiException(int statusCode, string message, Exception innerException, List<string>? errors = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        Errors = errors ?? [];
    }
}

public class NotFoundException : ApiException
{
    public NotFoundException(string message) : base(404, message) { }
}

public class ConflictException : ApiException
{
    public ConflictException(string message) : base(409, message) { }

    /// <summary>Conserva la excepcion original (ej. DbUpdateConcurrencyException) para el log.</summary>
    public ConflictException(string message, Exception innerException) : base(409, message, innerException) { }
}

public class ForbiddenException : ApiException
{
    public ForbiddenException(string message) : base(403, message) { }
}

public class UnauthorizedApiException : ApiException
{
    public UnauthorizedApiException(string message) : base(401, message) { }
}

public class ValidationApiException : ApiException
{
    public ValidationApiException(string message, List<string> errors) : base(422, message, errors) { }
}
