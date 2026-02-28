using System.Globalization;

namespace BuildSmart.Maui.Converters;

public class StepVisibleConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is int currentStep && int.TryParse(parameter?.ToString(), out int targetStep))
		{
			return currentStep == targetStep;
		}
		return false;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}