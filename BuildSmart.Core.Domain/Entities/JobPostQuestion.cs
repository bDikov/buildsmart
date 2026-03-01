using BuildSmart.Core.Domain.Common;

namespace BuildSmart.Core.Domain.Entities;

public class JobPostQuestion : BaseEntity
{
    public Guid JobPostId { get; set; }
    public JobPost JobPost { get; set; } = null!;

    public Guid TradesmanProfileId { get; set; }
    public TradesmanProfile TradesmanProfile { get; set; } = null!;

    public string QuestionText { get; set; } = null!;
    
    public bool IsEdited { get; set; }
    public bool IsAnswerEdited { get; set; }

    public string? AnswerText { get; set; }
    public DateTime? AnsweredAt { get; set; }

    public bool IsAnswered => !string.IsNullOrEmpty(AnswerText);

    public void Answer(string answer)
    {
        AnswerText = answer;
        AnsweredAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateQuestionText(string newText)
    {
        if (QuestionText != newText)
        {
            QuestionText = newText;
            IsEdited = true;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void UpdateAnswerText(string newAnswer)
    {
        if (AnswerText != newAnswer)
        {
            AnswerText = newAnswer;
            IsAnswerEdited = true;
            AnsweredAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
