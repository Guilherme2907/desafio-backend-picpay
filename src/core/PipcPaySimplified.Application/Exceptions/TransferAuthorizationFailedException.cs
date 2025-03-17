using System.Net;

namespace PipcPaySimplified.Application.Exceptions;

public class TransferAuthorizationFailedException : Exception
{
    public string? Reason { get; }

    public HttpStatusCode Status { get; }

    public TransferAuthorizationFailedException(
        string? message,
        Exception? innerException,
        string? reason,
        HttpStatusCode status
    ) : base(message, innerException)
    {
        Reason = reason;
        Status = status;
    }
}
