using System.Text.Json;
using Aig.Lms.BuildingBlocks.Application.Emails;
using Aig.Lms.BuildingBlocks.Domain.Emails;

namespace Aig.Lms.BuildingBlocks.Infrastructure.Emails;

public class EmailOutboxService : IEmailOutboxService
{
    private readonly IEmailOutboxRepository _repository;

    public EmailOutboxService(IEmailOutboxRepository repository)
    {
        _repository = repository;
    }

    public async Task EnqueueAsync(string toEmail, string subject, string templateName, object payload, CancellationToken cancellationToken = default)
    {
        var payloadJson = JsonSerializer.Serialize(payload);
        var message = new EmailOutboxMessage(toEmail, subject, templateName, payloadJson);
        await _repository.InsertAsync(message, cancellationToken);
    }
}
