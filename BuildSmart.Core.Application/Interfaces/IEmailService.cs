using System;
using System.Threading.Tasks;
using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Core.Application.Interfaces;

public interface IEmailService
{
	Task SendProjectOfferEmailAsync(Guid projectId);
	Task SendGenericEmailAsync(string toEmail, string subject, string body);
	Task SendChatNotificationEmailAsync(Guid userId, Guid notificationId);
	Task SendPostOfferFeedbackEmailAsync(Guid projectId);
	Task SendCalculatorLeadOfferEmailAsync(CalculatorLead lead);
	Task SendCalculatorLeadOfferEmailByIdAsync(Guid leadId);
}
