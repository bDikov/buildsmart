namespace BuildSmart.Core.Domain.ValueObjects;

// We change this from 'record struct' to a simple 'class'
// This is much more stable and compatible with EF Core's 'OwnsOne'
public class Amount
{
	// We use 'init' setters to make the object immutable after creation.
	// EF Core can work with 'init' setters perfectly.
	public string Currency { get; init; }

	public decimal Subtotal { get; init; }
	public decimal Tax { get; init; }
	public decimal Total { get; init; }

	// EF Core needs a parameterless constructor to create the object
	// when reading from the database.
	private Amount()
	{ }

	// Static factory method for business logic
	public static Amount Create(string currency, decimal subtotal, decimal taxRate = 0.2m)
	{
		if (string.IsNullOrWhiteSpace(currency))
		{
			throw new ArgumentException("Currency cannot be empty.", nameof(currency));
		}

		if (subtotal < 0)
		{
			throw new ArgumentException("Subtotal cannot be negative.");
		}

		var tax = subtotal * taxRate;
		var total = subtotal + tax;

		return new Amount
		{
			Currency = currency,
			Subtotal = subtotal,
			Tax = tax,
			Total = total
		};
	}

	public override string ToString() => $"{Total} {Currency}";
}