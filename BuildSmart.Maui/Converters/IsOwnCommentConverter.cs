using System.Globalization;
using BuildSmart.Maui.GraphQL;

namespace BuildSmart.Maui.Converters;

public class IsOwnCommentConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length < 2) return false;

        Guid? authorId = null;
        var item = values[0];
        var currentUserId = values[1] as Guid?;

        if (currentUserId == null) return false;

        // Cast to known interfaces from our fragments (fast, no reflection)
        if (item is IFeedbackDetails f) authorId = f.Author.Id;
        else if (item is IFeedbackReplyDetails fr) authorId = fr.Author.Id;
        else if (item is IQuestionDetails q) authorId = q.Author.Id;
        else if (item is IQuestionReplyDetails qr) authorId = qr.Author.Id;

        return authorId != null && authorId.Value == currentUserId.Value;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
