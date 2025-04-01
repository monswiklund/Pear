using Pear.Models;

namespace Pear.Services;

public interface INewsletterService
{
    Task<OperationResult> SignUpForNewsletterAsync(Subscriber subscriber);
    Task<OperationResult> OptOutFromNewsletterAsync(string email);
    Task<IEnumerable<Subscriber>> GetActiveSubscribersAsync();
}