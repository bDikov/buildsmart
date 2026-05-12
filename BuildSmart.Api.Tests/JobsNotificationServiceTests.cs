using Xunit;
using Moq;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Entities.JoinEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BuildSmart.Api.Services;
using MockQueryable.Moq;
using MockQueryable.EntityFrameworkCore;
using MockQueryable;

namespace BuildSmart.Api.Tests;

public class JobsNotificationServiceTests
{
	private readonly Mock<IUnitOfWork> _unitOfWorkMock;
	private readonly Mock<INotificationService> _notificationServiceMock;
	private readonly IJobsNotificationService _jobsNotificationService;

	public JobsNotificationServiceTests()
	{
		_unitOfWorkMock = new Mock<IUnitOfWork>();
		_notificationServiceMock = new Mock<INotificationService>();

		_jobsNotificationService = new JobsNotificationService(
			_unitOfWorkMock.Object,
			_notificationServiceMock.Object);
	}

	[Fact]
	public async Task NotifyTradesmenOfNewJobAsync_NotifiesMatchingTradesmen()
	{
		// Arrange
		var categoryId = Guid.NewGuid();
		var jobPost = new JobPost
		{
			Id = Guid.NewGuid(),
			Title = "Fix Leak",
			ServiceCategoryId = categoryId
		};

		var matchingTradesman1 = new TradesmanProfile
		{
			UserId = Guid.NewGuid(),
			Skills = new List<TradesmanSkill> { new TradesmanSkill { ServiceCategoryId = categoryId } }
		};
		var matchingTradesman2 = new TradesmanProfile
		{
			UserId = Guid.NewGuid(),
			Skills = new List<TradesmanSkill> { new TradesmanSkill { ServiceCategoryId = categoryId } }
		};
		var nonMatchingTradesman = new TradesmanProfile
		{
			UserId = Guid.NewGuid(),
			Skills = new List<TradesmanSkill> { new TradesmanSkill { ServiceCategoryId = Guid.NewGuid() } }
		};

		var tradesmen = new List<TradesmanProfile> { matchingTradesman1, matchingTradesman2, nonMatchingTradesman };
		var mock = tradesmen.BuildMock();

		_unitOfWorkMock.Setup(u => u.TradesmanProfiles.GetQueryable())
			.Returns(mock);

		// Act
		await _jobsNotificationService.NotifyTradesmenOfNewJobAsync(jobPost);

		// Assert
		_notificationServiceMock.Verify(n => n.SendLocalizedNotificationAsync(
		    matchingTradesman1.UserId,
		    "Title_NewJobOpportunity",
		    "Msg_NewJobOpportunity",
		    It.Is<object[]>(args => args != null && args[0].ToString() == jobPost.Title),
		    jobPost.Id,
		    "JobPost",
		    It.IsAny<object>()), Times.Once);

		_notificationServiceMock.Verify(n => n.SendLocalizedNotificationAsync(
		    matchingTradesman2.UserId,
		    "Title_NewJobOpportunity",
		    "Msg_NewJobOpportunity",
		    It.Is<object[]>(args => args != null && args[0].ToString() == jobPost.Title),
		    jobPost.Id,
		    "JobPost",
		    It.IsAny<object>()), Times.Once);

		_notificationServiceMock.Verify(n => n.SendLocalizedNotificationAsync(
		    nonMatchingTradesman.UserId,
		    It.IsAny<string>(),
		    It.IsAny<string>(),
		    It.IsAny<object[]?>(),
		    It.IsAny<Guid?>(),
		    It.IsAny<string?>(),
		    It.IsAny<object?>()), Times.Never);	}
}