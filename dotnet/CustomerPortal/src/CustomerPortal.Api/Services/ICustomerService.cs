using CustomerPortal.Api.Models.Dto;
using CustomerPortal.Api.Models.Requests;

namespace CustomerPortal.Api.Services;

public interface ICustomerService
{
    Task<CustomerResponse> RegisterAsync(RegistrationRequest request, CancellationToken cancellationToken);
}
