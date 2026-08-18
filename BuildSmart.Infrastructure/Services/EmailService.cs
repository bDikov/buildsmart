using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BuildSmart.Infrastructure.Services
{
	public class EmailService : IEmailService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ICalculatorLeadRepository _leadRepository;
		private readonly IConfiguration _config;
		private readonly ILogger<EmailService> _logger;

		public EmailService(IConfiguration config, ILogger<EmailService> logger, IUnitOfWork? unitOfWork = null, ICalculatorLeadRepository? leadRepository = null)
		{
			_unitOfWork = unitOfWork;
			_leadRepository = leadRepository;
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

			string baseUrl = _config["AppBaseUrl"]?.TrimEnd('/') ?? "https://buildsmart.bg";
			string targetUrl = $"{baseUrl}/project-detail?projectId={projectId}";
			string ctaText = lang == "bg" ? "Прегледайте офертата в платформата" : "View Proposal in Platform";

			string body = lang == "bg"
				? $@"
				<html>
				<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #1e293b; background-color: #f8fafc; padding: 24px;'>
					<div style='max-width: 600px; margin: 0 auto; background: #ffffff; padding: 32px; border-radius: 12px; border: 1px solid #e2e8f0;'>
						<h2 style='color: #0f172a; margin-top: 0;'>Здравейте, {homeowner.FirstName}!</h2>
						<p>Имаме удоволствието да Ви съобщим, че Вашата подробна количествено-стойностна оферта за проект <strong>{project.Title}</strong> е успешно генерирана от нашата платформа.</p>
						<p>Прикачен към този имейл ще намерите пълния официален PDF документ с детайлно разбиване на задачите, материалите и цените.</p>
						
						<div style='margin: 32px 0; text-align: center;'>
							<a href='{targetUrl}' target='_blank' style='background-color: #2563EB; color: #ffffff; display: inline-block; font-family: Arial, sans-serif; font-size: 15px; font-weight: 600; line-height: 44px; text-align: center; text-decoration: none; padding: 0 28px; border-radius: 8px;'>
								{ctaText}
							</a>
						</div>

						<p style='color: #64748b; font-size: 14px;'>Поздрави,<br/>Екипът на BuildSmart</p>
					</div>
				</body>
				</html>"
				: $@"
				<html>
				<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #1e293b; background-color: #f8fafc; padding: 24px;'>
					<div style='max-width: 600px; margin: 0 auto; background: #ffffff; padding: 32px; border-radius: 12px; border: 1px solid #e2e8f0;'>
						<h2 style='color: #0f172a; margin-top: 0;'>Hello, {homeowner.FirstName}!</h2>
						<p>We are pleased to inform you that your detailed project proposal for <strong>{project.Title}</strong> has been successfully generated.</p>
						<p>Attached to this email, you will find the official PDF document outlining tasks, materials, and pricing.</p>
						
						<div style='margin: 32px 0; text-align: center;'>
							<a href='{targetUrl}' target='_blank' style='background-color: #2563EB; color: #ffffff; display: inline-block; font-family: Arial, sans-serif; font-size: 15px; font-weight: 600; line-height: 44px; text-align: center; text-decoration: none; padding: 0 28px; border-radius: 8px;'>
								{ctaText}
							</a>
						</div>

						<p style='color: #64748b; font-size: 14px;'>Best regards,<br/>The BuildSmart Team</p>
					</div>
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

				// Notify Admins about the automated email dispatch
				await NotifyAdminsAsync(
					"Изпратена оферта",
					$"Автоматичен имейл с оферта бе изпратен до {homeowner.FirstName} {homeowner.LastName} ({homeowner.Email}) за проект \"{project.Title}\".",
					projectId,
					"Project"
				);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to send offer PDF email to {Email} for Project {ProjectId}", homeowner.Email, projectId);
				throw; // Let Hangfire handle retries if SMTP transient error occurs
			}
		}

		public async Task SendGenericEmailAsync(string toEmail, string subject, string body)
		{
			if (_config["Smtp:Disabled"] == "true")
			{
				_logger.LogInformation("SMTP is disabled via configuration. Skipping send to {Email}", toEmail);
				return;
			}

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
			string ctaText = lang == "bg" ? "Отворете съобщението в чата" : "Open Message in Chat";

			string baseUrl = _config["AppBaseUrl"]?.TrimEnd('/') ?? "https://buildsmart.bg";
			string targetUrl = $"{baseUrl}/project-messages";
			if ((notification.RelatedEntityType == "Chat" || notification.RelatedEntityType == "Project") && notification.RelatedEntityId.HasValue)
			{
				targetUrl = $"{baseUrl}/project-messages?projectId={notification.RelatedEntityId.Value}";
			}

			string emailBody = $@"
			<html>
			<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #1e293b; background-color: #f8fafc; padding: 24px;'>
				<div style='max-width: 600px; margin: 0 auto; background: #ffffff; padding: 32px; border-radius: 12px; border: 1px solid #e2e8f0;'>
					<h2 style='color: #0f172a; margin-top: 0;'>{greeting}</h2>
					<p style='font-size: 16px; color: #334155;'>{notification.Message}</p>
					
					<div style='margin: 32px 0; text-align: center;'>
						<a href='{targetUrl}' target='_blank' style='background-color: #2563EB; color: #ffffff; display: inline-block; font-family: Arial, sans-serif; font-size: 15px; font-weight: 600; line-height: 44px; text-align: center; text-decoration: none; padding: 0 28px; border-radius: 8px;'>
							{ctaText}
						</a>
					</div>

					<p style='color: #64748b; font-size: 14px;'>{closing}</p>
				</div>
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

		public async Task SendPostOfferFeedbackEmailAsync(Guid projectId)
		{
			_logger.LogInformation("Evaluating Post-Offer Feedback email eligibility for Project {ProjectId}", projectId);

			var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
			if (project == null)
			{
				_logger.LogWarning("Project {ProjectId} not found. Skipping feedback email.", projectId);
				return;
			}

			var homeowner = await _unitOfWork.Users.GetByIdAsync(project.HomeownerId);
			if (homeowner == null || string.IsNullOrWhiteSpace(homeowner.Email))
			{
				_logger.LogWarning("Homeowner for Project {ProjectId} not found or has no email address.", projectId);
				return;
			}

			// GUARD 1: Check if the user has already sent any chat messages for this project
			var projectMessages = await _unitOfWork.ProjectMessages.GetMessagesPaginatedAsync(projectId, 0, 100);
			bool hasUserSentChat = projectMessages != null && projectMessages.Any(m => m.SenderId == homeowner.Id);
			if (hasUserSentChat)
			{
				_logger.LogInformation("Homeowner {UserId} has already sent chat messages for Project {ProjectId}. Suppressing automated feedback email.", homeowner.Id, projectId);
				return;
			}

			// GUARD 2: Check if the user has already requested a consultation / submitted for review
			if (project.Status == Core.Domain.Enums.ProjectStatus.UnderReview || project.Status == Core.Domain.Enums.ProjectStatus.Active || project.Status == Core.Domain.Enums.ProjectStatus.Completed)
			{
				_logger.LogInformation("Project {ProjectId} is in status {Status}. Suppressing automated feedback email.", projectId, project.Status);
				return;
			}

			// All guards passed: User has NOT reached out yet. Send the feedback request email.
			string lang = homeowner.PreferredLanguage ?? project.LanguageCode ?? "bg";
			string subject = lang == "bg" 
				? $"Как Ви се стори офертата за {project.Title}?" 
				: $"How did you find your offer for {project.Title}?";

			string greeting = lang == "bg" ? $"Здравейте, {homeowner.FirstName}!" : $"Hello, {homeowner.FirstName}!";
			string closing = lang == "bg" ? "Поздрави,<br/>Бончо | BuildSmart" : "Best regards,<br/>Boncho | BuildSmart";
			string ctaText = lang == "bg" ? "Отворете съобщението в чата" : "Open Message in Chat";

			string baseUrl = _config["AppBaseUrl"]?.TrimEnd('/') ?? "https://buildsmart.bg";
			string chatUrl = $"{baseUrl}/project-messages?projectId={project.Id}";

			string emailBody = lang == "bg"
				? $@"
				<html>
				<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #1e293b; background-color: #f8fafc; padding: 24px;'>
					<div style='max-width: 600px; margin: 0 auto; background: #ffffff; padding: 32px; border-radius: 12px; border: 1px solid #e2e8f0;'>
						<h2 style='color: #0f172a; margin-top: 0;'>{greeting}</h2>
						<p>Виждам, че вече създадохте своя проект <strong>{project.Title}</strong> и офертата Ви е готова!</p>
						<p>Ако имате минутка, ще ми бъде изключително полезно да споделите как Ви се стори платформата, офертата и дали всичко Ви беше ясно. Всяко мнение ни помага много да се подобряваме!</p>
						
						<div style='margin: 32px 0; text-align: center;'>
							<a href='{chatUrl}' target='_blank' style='background-color: #2563EB; color: #ffffff; display: inline-block; font-family: Arial, sans-serif; font-size: 15px; font-weight: 600; line-height: 44px; text-align: center; text-decoration: none; padding: 0 28px; border-radius: 8px;'>
								{ctaText}
							</a>
						</div>

						<p style='color: #64748b; font-size: 14px;'>{closing}</p>
					</div>
				</body>
				</html>"
				: $@"
				<html>
				<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #1e293b; background-color: #f8fafc; padding: 24px;'>
					<div style='max-width: 600px; margin: 0 auto; background: #ffffff; padding: 32px; border-radius: 12px; border: 1px solid #e2e8f0;'>
						<h2 style='color: #0f172a; margin-top: 0;'>{greeting}</h2>
						<p>I see you have created your project <strong>{project.Title}</strong> and your proposal is ready!</p>
						<p>If you have a minute, it would be extremely helpful if you could share how you found the platform, the proposal, and whether everything was clear. Every feedback helps us immensely!</p>
						
						<div style='margin: 32px 0; text-align: center;'>
							<a href='{chatUrl}' target='_blank' style='background-color: #2563EB; color: #ffffff; display: inline-block; font-family: Arial, sans-serif; font-size: 15px; font-weight: 600; line-height: 44px; text-align: center; text-decoration: none; padding: 0 28px; border-radius: 8px;'>
								{ctaText}
							</a>
						</div>

						<p style='color: #64748b; font-size: 14px;'>{closing}</p>
					</div>
				</body>
				</html>";

			await SendGenericEmailAsync(homeowner.Email, subject, emailBody);
			_logger.LogInformation("Sent post-offer feedback email to Homeowner {UserId} for Project {ProjectId}", homeowner.Id, projectId);

			// Notify Admins about the automated feedback request dispatch
			await NotifyAdminsAsync(
				"Изпратено запитване за обратна връзка",
				$"Автоматичен имейл за обратна връзка бе изпратен до {homeowner.FirstName} {homeowner.LastName} ({homeowner.Email}) за проект \"{project.Title}\".",
				projectId,
				"Project"
			);
		}

		public async Task SendCalculatorLeadOfferEmailAsync(CalculatorLead lead)
		{
			if (lead == null || string.IsNullOrWhiteSpace(lead.Email))
			{
				_logger.LogWarning("Cannot send calculator lead email: lead or email is empty.");
				return;
			}

			_logger.LogInformation("Sending automated offer email for CalculatorLead {LeadId} to {Email}", lead.Id, lead.Email);

			string scopeText = lead.Scope == "bathroom" ? "Ремонт на Баня" : "Пълен Ремонт на Апартамент";
			string statusText = lead.BuildingStatus switch
			{
				"rough" => "Груб строеж",
				"old" => "Старо строителство (Къртене)",
				_ => "БДС / Акт 16 (Ново строителство)"
			};

			string tierText = lead.QualityTier switch
			{
				"luxury" => "ЛУКС",
				"premium" => "ПРЕМИУМ",
				_ => "СТАНДАРТ"
			};

			string subject = $"Готова Оферта BuildSmart: {scopeText} ({lead.SelectedArea} кв.м.)";
			string recipientName = !string.IsNullOrWhiteSpace(lead.Name) ? lead.Name : "Уважаеми Клиент";

			string htmlBody = $@"
				<html>
				<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #1e293b; background-color: #f8fafc; padding: 24px;'>
					<div style='max-width: 600px; margin: 0 auto; background: #ffffff; padding: 32px; border-radius: 12px; border: 1px solid #e2e8f0; box-shadow: 0 10px 15px -3px rgba(0,0,0,0.05);'>
						<div style='text-align: center; padding-bottom: 24px; border-bottom: 2px solid #2563eb;'>
							<h1 style='color: #1e293b; margin: 0; font-size: 24px;'>BuildSmart.bg</h1>
							<p style='color: #2563eb; font-weight: 600; margin: 4px 0 0 0;'>Проверена Оферта за Ремонт</p>
						</div>

						<div style='padding: 24px 0;'>
							<h2 style='color: #0f172a; margin-top: 0;'>Здравейте, {recipientName}!</h2>
							<p>Благодарим Ви, че използвахте дигиталния калкулатор за ремонти на <strong>BuildSmart.bg</strong>.</p>
							<p>Вашата оферта по фиксирани цени с <strong>0% авансово плащане</strong> е подготвена:</p>

							<div style='background: #f1f5f9; padding: 20px; border-radius: 10px; margin: 20px 0;'>
								<table style='width: 100%; border-collapse: collapse; font-size: 15px;'>
									<tr>
										<td style='padding: 6px 0; color: #64748b;'>Обхват:</td>
										<td style='padding: 6px 0; font-weight: bold; text-align: right; color: #0f172a;'>{scopeText}</td>
									</tr>
									<tr>
										<td style='padding: 6px 0; color: #64748b;'>Площ:</td>
										<td style='padding: 6px 0; font-weight: bold; text-align: right; color: #0f172a;'>{lead.SelectedArea} кв.м.</td>
									</tr>
									<tr>
										<td style='padding: 6px 0; color: #64748b;'>Състояние:</td>
										<td style='padding: 6px 0; font-weight: bold; text-align: right; color: #0f172a;'>{statusText}</td>
									</tr>
									<tr>
										<td style='padding: 6px 0; color: #64748b;'>Пакет покритие:</td>
										<td style='padding: 6px 0; font-weight: bold; text-align: right; color: #2563eb;'>{tierText}</td>
									</tr>
									<tr>
										<td style='padding: 6px 0; color: #64748b;'>Срок за изпълнение:</td>
										<td style='padding: 6px 0; font-weight: bold; text-align: right; color: #0f172a;'>{lead.EstimatedDays} Работни Дни</td>
									</tr>
									<tr style='border-top: 1px solid #cbd5e1;'>
										<td style='padding: 12px 0 6px 0; color: #0f172a; font-weight: bold;'>Бюджет (EUR):</td>
										<td style='padding: 12px 0 6px 0; font-weight: bold; font-size: 18px; text-align: right; color: #16a34a;'>€{lead.MinPriceEur:N0} - €{lead.MaxPriceEur:N0}</td>
									</tr>
									<tr>
										<td style='padding: 0 0 6px 0; color: #64748b; font-size: 13px;'>Бюджет (BGN):</td>
										<td style='padding: 0 0 6px 0; font-size: 13px; text-align: right; color: #64748b;'>{lead.MinPriceBgn:N0} - {lead.MaxPriceBgn:N0} лв.</td>
									</tr>
								</table>
							</div>

							<div style='margin: 24px 0; padding: 20px; background: #eff6ff; border-left: 4px solid #2563eb; border-radius: 6px;'>
								<h3 style='margin: 0 0 12px 0; color: #1e3a8a; font-size: 16px;'>Защо да изберете BuildSmart.bg?</h3>
								<ul style='margin: 0; padding-left: 20px; color: #1e293b; font-size: 14px;'>
									<li style='margin-bottom: 8px;'><strong>0% Авансово плащане (Капаро):</strong> Плащате на етапи – едва след като съответният етап е завършен и приет от вас.</li>
									<li style='margin-bottom: 8px;'><strong>Твърда оферта по договор:</strong> Цените по договор са фиксирани и гарантирани без &quot;непредвидени&quot; надценки.</li>
									<li style='margin-bottom: 8px;'><strong>ИИ Количествено-стойностна сметка:</strong> Количествата и цените са базирани на реални актуални формули за София.</li>
									<li><strong>Сертифициран строителен контрол:</strong> Пълна писмена гаранция за ВиК, Ел., хидроизолация и довършителни дейности.</li>
								</ul>
							</div>

							<div style='margin: 24px 0; padding: 16px; background: #f8fafc; border: 1px dashed #cbd5e1; border-radius: 8px; font-size: 13px; color: #475569;'>
								<h4 style='margin: 0 0 8px 0; color: #334155; font-size: 14px;'>📌 Общи условия и валидност</h4>
								<p style='margin: 0 0 6px 0;'>• Офертата е валидна <strong>30 календарни дни</strong> от датата на генериране.</p>
								<p style='margin: 0 0 6px 0;'>• Посочените цени са изчислени за София и включват труд, чернови и чистови материали по избрания пакет.</p>
								<p style='margin: 0;'>• При заявяване от Ваша страна, BuildSmart извършва безплатен наземен оглед за подписване на официален договор.</p>
							</div>

							<div style='margin: 32px 0; text-align: center;'>
								<a href='https://buildsmart.bg/renovation-estimator' target='_blank' style='background-color: #2563eb; color: #ffffff; display: inline-block; font-family: Arial, sans-serif; font-size: 15px; font-weight: 600; line-height: 48px; text-align: center; text-decoration: none; padding: 0 32px; border-radius: 8px;'>
									📞 Прегледайте Офертата & Заявете Оглед
								</a>
							</div>

							<p style='color: #64748b; font-size: 14px;'>С уважение,<br/><strong>Екипът на BuildSmart.bg</strong><br/>София, България</p>
						</div>
					</div>
				</body>
				</html>";

			await SendGenericEmailAsync(lead.Email, subject, htmlBody);
			_logger.LogInformation("Successfully sent calculator lead offer email to {Email}", lead.Email);

			// Send instant alert email to admin
			try
			{
				string adminSubject = $"⚡ НОВ ЛИЙД: {scopeText} ({lead.Name ?? "Анонимен"})";
				string adminBody = $@"
					<html>
					<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #1e293b; padding: 20px;'>
						<h2 style='color: #2563eb;'>НОВО ЗАПИТВАНЕ ОТ ЛИЙД МАГНЕТ</h2>
						<p>Получено е ново запитване през дигиталния калкулатор:</p>
						<ul>
							<li><strong>Име:</strong> {lead.Name ?? "Анонимен"}</li>
							<li><strong>Имейл:</strong> <a href='mailto:{lead.Email}'>{lead.Email}</a></li>
							<li><strong>Телефон:</strong> <a href='tel:{lead.Phone}'>{lead.Phone ?? "Не е посочен"}</a></li>
							<li><strong>Обхват:</strong> {scopeText} ({lead.SelectedArea} кв.м.)</li>
							<li><strong>Ориентировъчен Бюджет:</strong> €{lead.MinPriceEur:N0} - €{lead.MaxPriceEur:N0} ({lead.MinPriceBgn:N0} - {lead.MaxPriceBgn:N0} лв.)</li>
							<li><strong>Статус имейл:</strong> {lead.VerificationStatus} (Verified: {lead.IsEmailVerified})</li>
							<li><strong>Източник / Кампания:</strong> Src: {lead.UtmSource ?? "Direct"} | Cmp: {lead.UtmCampaign ?? "-"}</li>
							<li><strong>Дата:</strong> {lead.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC</li>
						</ul>
						<p><a href='https://buildsmart.bg/admin/leads' style='background: #2563eb; color: #fff; padding: 10px 18px; border-radius: 6px; text-decoration: none;'>Отворете Админ Таблото</a></p>
					</body>
					</html>";
				string adminEmail = _config["Smtp:SenderEmail"] ?? "office@buildsmart.bg";
				await SendGenericEmailAsync(adminEmail, adminSubject, adminBody);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to send admin notification email for CalculatorLead {LeadId}", lead.Id);
			}
		}

		public async Task SendCalculatorLeadOfferEmailByIdAsync(Guid leadId)
		{
			if (_leadRepository == null)
			{
				_logger.LogWarning("ICalculatorLeadRepository is null in EmailService. Cannot fetch lead by ID {LeadId}.", leadId);
				return;
			}

			var lead = await _leadRepository.GetByIdAsync(leadId);
			if (lead == null)
			{
				_logger.LogWarning("CalculatorLead {LeadId} not found in database.", leadId);
				return;
			}

			await SendCalculatorLeadOfferEmailAsync(lead);
		}

		private async Task NotifyAdminsAsync(string title, string message, Guid relatedEntityId, string relatedEntityType)
		{
			try
			{
				var allUsers = await _unitOfWork.Users.GetAllAsync();
				var adminUsers = allUsers.Where(u => u.Role == Core.Domain.Enums.UserRoleTypes.Admin).ToList();

				foreach (var admin in adminUsers)
				{
					var notification = new Core.Domain.Entities.Notification
					{
						UserId = admin.Id,
						Title = title,
						Message = message,
						RelatedEntityId = relatedEntityId,
						RelatedEntityType = relatedEntityType,
						CreatedAt = DateTime.UtcNow,
						UpdatedAt = DateTime.UtcNow
					};

					await _unitOfWork.Notifications.AddAsync(notification);
				}

				if (adminUsers.Any())
				{
					await _unitOfWork.SaveChangesAsync();
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to send admin notification for entity {EntityId}", relatedEntityId);
			}
		}
	}
}
