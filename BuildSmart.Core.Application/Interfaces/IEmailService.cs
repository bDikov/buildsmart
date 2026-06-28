using System;
using System.Threading.Tasks;

namespace BuildSmart.Core.Application.Interfaces;

public interface IEmailService
{
	Task SendProjectOfferEmailAsync(Guid projectId);
	Task SendGenericEmailAsync(string toEmail, string subject, string body);
}
