namespace Hotel.Api.Services;

public class TenantResolutionException : Exception
{
    public TenantResolutionException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}
