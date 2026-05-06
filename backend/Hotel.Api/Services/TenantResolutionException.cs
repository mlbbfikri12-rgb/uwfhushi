using Microsoft.AspNetCore.Http;

namespace Hotel.Api.Services;

public class TenantResolutionException : Exception
{
    public int StatusCode { get; }
    public string ErrorCode { get; }

    public TenantResolutionException(
        string message,
        int statusCode = StatusCodes.Status400BadRequest,
        string errorCode = "TENANT_RESOLUTION_ERROR"
    ) : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }

    public static TenantResolutionException MissingBranchHeader()
        => new(
            "X-Branch-Code header is missing",
            StatusCodes.Status400BadRequest,
            "TENANT_HEADER_MISSING"
        );

    public static TenantResolutionException BranchNotFound()
        => new(
            "Branch not found",
            StatusCodes.Status404NotFound,
            "BRANCH_NOT_FOUND"
        );

    public static TenantResolutionException ForbiddenBranchAccess()
        => new(
            "You are not allowed to access this branch",
            StatusCodes.Status403Forbidden,
            "BRANCH_FORBIDDEN"
        );
}