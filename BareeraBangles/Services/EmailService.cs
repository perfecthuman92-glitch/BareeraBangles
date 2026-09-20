using System.Net;
using System.Net.Mail;
using BareeraBangles.Configuration;
using Microsoft.Extensions.Options;

namespace BareeraBangles.Services;

public interface IEmailService
{
    Task SendAsync(string toEmail, string subject, string htmlBody);
    bool IsConfigured { get; }
}

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<EmailSettings> settings,
        IWebHostEnvironment env,
        ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _env = env;
        _logger = logger;
    }

    public bool IsConfigured => _settings.IsConfigured;

    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(toEmail)) return;

        if (_settings.IsConfigured)
        {
            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                EnableSsl = _settings.UseSsl,
                Credentials = new NetworkCredential(_settings.Username, _settings.Password)
            };
            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);
            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
            return;
        }

        var dir = Path.Combine(_env.ContentRootPath, "App_Data", "mail");
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.html");
        var content = $"<!-- To: {toEmail} | Subject: {subject} -->\n{htmlBody}";
        await File.WriteAllTextAsync(file, content);
        _logger.LogInformation("SMTP not configured. Saved email for {Email} to {File}", toEmail, file);
    }
}
