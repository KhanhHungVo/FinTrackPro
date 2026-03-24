namespace FinTrackPro.Domain.Exceptions;

public class AuthorizationException : DomainException
{
    public AuthorizationException(string message) : base(message) { }
}
