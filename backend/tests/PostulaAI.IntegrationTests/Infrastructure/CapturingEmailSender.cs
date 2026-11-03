using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using PostulaAI.Api.Shared.Email;

namespace PostulaAI.IntegrationTests.Infrastructure;

public sealed partial class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> sent = new();

    public IReadOnlyCollection<EmailMessage> Sent => sent;

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        sent.Enqueue(message);
        return Task.CompletedTask;
    }

    /// <summary>The token from the most recent verification link sent to <paramref name="email"/>.</summary>
    public string LatestVerificationToken(string email)
    {
        var message = sent.Where(candidate => string.Equals(candidate.To, email, StringComparison.OrdinalIgnoreCase)).LastOrDefault()
            ?? throw new InvalidOperationException("No e-mail was sent to that address.");
        var match = TokenPattern().Match(message.TextBody);
        return match.Success ? Uri.UnescapeDataString(match.Groups[1].Value) : throw new InvalidOperationException("No verification link in the e-mail.");
    }

    [GeneratedRegex(@"verificar-correo\?token=([^\s""<]+)")]
    private static partial Regex TokenPattern();
}
