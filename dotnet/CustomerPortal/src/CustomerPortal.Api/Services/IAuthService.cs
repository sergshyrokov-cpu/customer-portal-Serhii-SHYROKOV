using CustomerPortal.Api.Models.Dto;
using CustomerPortal.Api.Models.Requests;

namespace CustomerPortal.Api.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
