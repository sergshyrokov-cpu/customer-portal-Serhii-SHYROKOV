using CustomerPortal.Api.Models.Entities;

namespace CustomerPortal.Api.Services;

public interface ITokenService
{
    (string AccessToken, int ExpiresInSeconds) IssueToken(Customer customer);
}
