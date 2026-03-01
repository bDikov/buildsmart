using System.Globalization;

namespace BuildSmart.Maui.Converters;

public class IsOwnQuestionConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length < 2) return false;

        var questionProfileId = values[0]?.ToString();
        var currentProfileId = values[1]?.ToString();

        System.Diagnostics.Debug.WriteLine($"[DEBUG] IsOwnQuestionConverter: QuestionOwner={questionProfileId}, CurrentUser={currentProfileId}");

        if (string.IsNullOrEmpty(questionProfileId) || string.IsNullOrEmpty(currentProfileId)) return false;

        return string.Equals(questionProfileId, currentProfileId, StringComparison.OrdinalIgnoreCase);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
