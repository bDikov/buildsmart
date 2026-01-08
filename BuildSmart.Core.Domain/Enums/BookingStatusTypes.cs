namespace BuildSmart.Core.Domain.Enums;

public enum BookingStatusTypes
{
	Pending,    // Initial state, waiting for tradesman to accept
	Confirmed,  // Tradesman has accepted the booking
	Completed,  // The job is done
	Cancelled,  // Cancelled by either party
	Declined    // Tradesman declined the request
}