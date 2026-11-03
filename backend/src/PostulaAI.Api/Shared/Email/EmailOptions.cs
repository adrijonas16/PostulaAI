namespace PostulaAI.Api.Shared.Email;

/// <summary>Bound from "Email". SMTP credentials come from environment variables or a secret store, never from Git.</summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string SmtpHost { get; set; } = string.Empty;

    public int SmtpPort { get; set; } = 587;

    public string? SmtpUser { get; set; }

    public string? SmtpPassword { get; set; }

    /// <summary>STARTTLS. Required for real providers; off for the local Mailpit catcher.</summary>
    public bool UseSsl { get; set; } = true;

    public string From { get; set; } = "PostulaAI <no-reply@postulaai.local>";

    /// <summary>Public URL of the web app, used to build links in e-mails (e.g. https://postulaai.example).</summary>
    public string AppBaseUrl { get; set; } = "http://localhost:5173";

    /// <summary>
    /// Require a confirmed e-mail before using AI features. Only enforced when SMTP is configured:
    /// without a way to send the e-mail nobody could confirm it.
    /// </summary>
    public bool RequireVerification { get; set; } = true;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(SmtpHost);

    public bool VerificationEnforced => RequireVerification && IsConfigured;
}
