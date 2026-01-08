using System.Globalization;

namespace BuildSmart.Maui.Converters;

public class StepToProgressConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int currentStep)
        {
            // Assuming 4 steps (0, 1, 2, 3)
            return (double)(currentStep + 1) / 4.0;
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
