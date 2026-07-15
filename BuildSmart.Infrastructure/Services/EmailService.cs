using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using BuildSmart.Core.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BuildSmart.Infrastructure.Services
{
	public class EmailService : IEmailService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IConfiguration _config;
		private readonly ILogger<EmailService> _logger;

		public EmailService(IUnitOfWork unitOfWork, IConfiguration config, ILogger<EmailService> logger)
		{
			_unitOfWork = unitOfWork;
			_config = config;
			_logger = logger;
		}

		public async Task SendProjectOfferEmailAsync(Guid projectId)
		{
			_logger.LogInformation("Triggering email offer delivery job for Project {ProjectId}", projectId);

			var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
			if (project == null)
			{
				_logger.LogWarning("Project {ProjectId} not found. Cannot send offer email.", projectId);
				return;
			}

			if (project.MasterOfferPdf == null || project.MasterOfferPdf.Length == 0)
			{
				_logger.LogError("Project {ProjectId} has no generated Master PDF offer. Cannot send email.", projectId);
				return;
			}

			var homeowner = await _unitOfWork.Users.GetByIdAsync(project.HomeownerId);
			if (homeowner == null || string.IsNullOrWhiteSpace(homeowner.Email))
			{
				_logger.LogError("Homeowner for Project {ProjectId} not found or has no email address.", projectId);
				return;
			}

			if (!homeowner.EmailOnOfferReady)
			{
				_logger.LogInformation("Homeowner for Project {ProjectId} has disabled email notifications. Skipping send.", projectId);
				return;
			}

			// Read SMTP settings (configured from appsettings.json / secrets)
			var smtpServer = _config["Smtp:Server"] ?? throw new InvalidOperationException("SMTP Server configuration is missing (Smtp:Server).");
			var smtpPortStr = _config["Smtp:Port"] ?? throw new InvalidOperationException("SMTP Port configuration is missing (Smtp:Port).");
			var smtpUsername = _config["Smtp:Username"] ?? throw new InvalidOperationException("SMTP Username configuration is missing (Smtp:Username).");
			var smtpPassword = _config["Smtp:Password"] ?? throw new InvalidOperationException("SMTP Password configuration is missing (Smtp:Password).");
			var senderEmail = _config["Smtp:SenderEmail"] ?? throw new InvalidOperationException("SMTP SenderEmail configuration is missing (Smtp:SenderEmail).");
			var senderName = _config["Smtp:SenderName"] ?? "BuildSmart";

			if (!int.TryParse(smtpPortStr, out var smtpPort))
			{
				smtpPort = 587;
			}

			string lang = project.LanguageCode ?? "bg";
			string subject = lang == "bg" 
				? $"Оферта за Вашия проект: {project.Title}" 
				: $"Proposal for your project: {project.Title}";

			string body = lang == "bg"
				? $@"
				<html>
				<body style='font-family: Arial, sans-serif; line-height: 1.6;'>
					<h2>Здравейте, {homeowner.FirstName}!</h2>
					<p>Имаме удоволствието да Ви съобщим, че Вашата подробна количествено-стойностна оферта за проект <strong>{project.Title}</strong> е успешно генерирана от нашата платформа.</p>
					<p>Прикачен към този имейл ще намерите пълния официален PDF документ с детайлно разбиване на задачите, материалите и цените.</p>
					<br/>
					<p>Поздрави,<br/>Екипът на BuildSmart</p>
				</body>
				</html>"
				: $@"
				<html>
				<body style='font-family: Arial, sans-serif; line-height: 1.6;'>
					<h2>Hello, {homeowner.FirstName}!</h2>
					<p>We are pleased to inform you that your detailed project proposal for <strong>{project.Title}</strong> has been successfully generated.</p>
					<p>Attached to this email, you will find the official PDF document outlining tasks, materials, and pricing.</p>
					<br/>
					<p>Best regards,<br/>The BuildSmart Team</p>
				</body>
				</html>";

			try
			{
				using var mail = new MailMessage();
				mail.From = new MailAddress(senderEmail, senderName);
				mail.To.Add(new MailAddress(homeowner.Email, $"{homeowner.FirstName} {homeowner.LastName}"));
				mail.Subject = subject;
				mail.Body = body;
				mail.IsBodyHtml = true;

				// Attach the generated Master PDF offer
				using var pdfStream = new MemoryStream(project.MasterOfferPdf);
				string safeTitle = string.Join("_", project.Title.Split(Path.GetInvalidFileNameChars()));
				var attachment = new Attachment(pdfStream, $"{safeTitle}_Offer.pdf", "application/pdf");
				mail.Attachments.Add(attachment);

				using var smtp = new SmtpClient(smtpServer, smtpPort);
				smtp.UseDefaultCredentials = false;
				smtp.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
				smtp.EnableSsl = true; // Use TLS (Port 587)

				await smtp.SendMailAsync(mail);
				_logger.LogInformation("Successfully sent offer PDF email to {Email} for Project {ProjectId}", homeowner.Email, projectId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to send offer PDF email to {Email} for Project {ProjectId}", homeowner.Email, projectId);
				throw; // Let Hangfire handle retries if SMTP transient error occurs
			}
		}

		public async Task SendGenericEmailAsync(string toEmail, string subject, string body)
		{
			var smtpServer = _config["Smtp:Server"] ?? throw new InvalidOperationException("SMTP Server configuration is missing (Smtp:Server).");
			var smtpPortStr = _config["Smtp:Port"] ?? throw new InvalidOperationException("SMTP Port configuration is missing (Smtp:Port).");
			var smtpUsername = _config["Smtp:Username"] ?? throw new InvalidOperationException("SMTP Username configuration is missing (Smtp:Username).");
			var smtpPassword = _config["Smtp:Password"] ?? throw new InvalidOperationException("SMTP Password configuration is missing (Smtp:Password).");
			var senderEmail = _config["Smtp:SenderEmail"] ?? throw new InvalidOperationException("SMTP SenderEmail configuration is missing (Smtp:SenderEmail).");
			var senderName = _config["Smtp:SenderName"] ?? "BuildSmart";

			if (!int.TryParse(smtpPortStr, out var smtpPort))
			{
				smtpPort = 587;
			}

			try
			{
				using var mail = new MailMessage();
				mail.From = new MailAddress(senderEmail, senderName);
				mail.To.Add(new MailAddress(toEmail));
				mail.Subject = subject;
				mail.Body = body;
				mail.IsBodyHtml = true;

				using var smtp = new SmtpClient(smtpServer, smtpPort);
				smtp.UseDefaultCredentials = false;
				smtp.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
				smtp.EnableSsl = true;

				await smtp.SendMailAsync(mail);
				_logger.LogInformation("Successfully sent generic email to {Email}", toEmail);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to send generic email to {Email}", toEmail);
				throw;
			}
		}

		public async Task SendChatNotificationEmailAsync(Guid userId, Guid notificationId)
		{
			_logger.LogInformation("Starting SendChatNotificationEmailAsync for User {UserId}, Notification {NotificationId}", userId, notificationId);

			var user = await _unitOfWork.Users.GetByIdAsync(userId);
			if (user == null || string.IsNullOrWhiteSpace(user.Email) || !user.EmailOnNewChatMessage)
			{
				_logger.LogInformation("User {UserId} not found, has no email, or email chat notifications disabled.", userId);
				return;
			}

			var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);
			if (notification == null)
			{
				_logger.LogInformation("Notification {NotificationId} not found.", notificationId);
				return;
			}

			if (notification.IsRead)
			{
				_logger.LogInformation("Notification {NotificationId} has already been read. Skipping email notification.", notificationId);
				return;
			}

			if (user.LastChatEmailSentAt.HasValue && DateTime.UtcNow - user.LastChatEmailSentAt.Value < TimeSpan.FromHours(1))
			{
				_logger.LogInformation("User {UserId} received a chat email in the last hour. Throttling email notification.", userId);
				return;
			}

			string lang = user.PreferredLanguage ?? "bg";
			string greeting = lang == "bg" ? $"Здравейте, {user.FirstName}!" : $"Hello, {user.FirstName}!";
			string closing = lang == "bg" ? "Поздрави,<br/>Екипът на BuildSmart" : "Best regards,<br/>The BuildSmart Team";
			string emailBody = $@"
			<html>
			<body style='font-family: Arial, sans-serif; line-height: 1.6;'>
				<h2>{greeting}</h2>
				<p>{notification.Message}</p>
				<br/>
				<p>{closing}</p>
			</body>
			</html>";

			try
			{
				await SendGenericEmailAsync(user.Email, notification.Title, emailBody);

				user.LastChatEmailSentAt = DateTime.UtcNow;
				_unitOfWork.Users.Update(user);
				await _unitOfWork.SaveChangesAsync();

				_logger.LogInformation("Successfully sent chat notification email to User {UserId} for Notification {NotificationId}", userId, notificationId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to send chat notification email to User {UserId} for Notification {NotificationId}", userId, notificationId);
				throw;
			}
		}
	}
}
