using System.Net;
using System.Net.Mail;

namespace NotificationService.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string htmlBody);
}

public class EmailSettings
{
    public string SmtpServer { get; set; } = "";
    public int SmtpPort { get; set; }
    public string SenderEmail { get; set; } = "";
    public string SenderName { get; set; } = "";
    public string Password { get; set; } = "";
    public bool EnableSsl { get; set; }
}

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _settings = configuration.GetSection("EmailSettings").Get<EmailSettings>() ?? new EmailSettings();
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(toEmail) || string.IsNullOrWhiteSpace(_settings.SenderEmail))
            {
                _logger.LogWarning("Email skipped — no recipient or sender configured.");
                return;
            }

            var message = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = subject,
                Body = WrapInTemplate(subject, htmlBody),
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
            {
                Credentials = new NetworkCredential(_settings.SenderEmail, _settings.Password),
                EnableSsl = _settings.EnableSsl
            };

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent to {Email} — Subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            throw;
        }
    }

    private string WrapInTemplate(string subject, string bodyContent)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin:0;padding:0;background-color:#f1f5f9;font-family:'Segoe UI',Roboto,'Helvetica Neue',Arial,sans-serif;"">
  <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""background:#f1f5f9;padding:40px 0;"">
    <tr>
      <td align=""center"">
        <table role=""presentation"" width=""600"" cellspacing=""0"" cellpadding=""0"" style=""background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);"">
          <!-- Header -->
          <tr>
            <td style=""background:linear-gradient(135deg,#1a237e,#283593);padding:32px 40px;text-align:center;"">
              <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" style=""margin:0 auto;"">
                <tr>
                  <td style=""vertical-align:middle;padding-right:12px;"">
                    <div style=""width:40px;height:40px;background:rgba(255,255,255,0.2);border-radius:10px;display:inline-block;text-align:center;line-height:40px;color:#ffffff;font-size:20px;"">✈</div>
                  </td>
                  <td style=""vertical-align:middle;"">
                    <h1 style=""margin:0;color:#ffffff;font-size:24px;font-weight:800;letter-spacing:-0.5px;"">SkyLedger</h1>
                    <p style=""margin:2px 0 0;color:rgba(255,255,255,0.7);font-size:11px;text-transform:uppercase;letter-spacing:2px;font-weight:600;"">Airlines</p>
                  </td>
                </tr>
              </table>
            </td>
          </tr>
          <!-- Subject Banner -->
          <tr>
            <td style=""background:#eff6ff;padding:16px 40px;border-bottom:1px solid #e2e8f0;"">
              <p style=""margin:0;font-size:13px;font-weight:700;color:#1e40af;text-transform:uppercase;letter-spacing:1px;"">{subject}</p>
            </td>
          </tr>
          <!-- Body -->
          <tr>
            <td style=""padding:32px 40px;"">
              <p style=""margin:0;font-size:15px;line-height:1.7;color:#334155;"">{bodyContent}</p>
            </td>
          </tr>
          <!-- Footer -->
          <tr>
            <td style=""background:#f8fafc;padding:24px 40px;border-top:1px solid #e2e8f0;text-align:center;"">
              <p style=""margin:0;font-size:12px;color:#94a3b8;"">© {DateTime.UtcNow.Year} SkyLedger Airlines. All rights reserved.</p>
              <p style=""margin:8px 0 0;font-size:11px;color:#cbd5e1;"">This is an automated notification. Please do not reply.</p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
    }
}
