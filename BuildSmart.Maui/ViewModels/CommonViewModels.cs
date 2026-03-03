using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace BuildSmart.Maui.ViewModels;

public partial class JobPostViewModel : ObservableObject
{
    public IJobPostDetails JobPost { get; }
    public ObservableCollection<QuestionViewModel> Questions { get; } = new();

    public JobPostViewModel(IJobPostDetails jobPost)
    {
        JobPost = jobPost;
        if (jobPost.Questions != null)
        {
            foreach (var q in jobPost.Questions)
            {
                Questions.Add(new QuestionViewModel(q));
            }
        }
    }
}

public partial class QuestionViewModel : ObservableObject
{
    public IQuestionDetails Question { get; }
    
    public ObservableCollection<IQuestionReplyDetails> Replies { get; } = new();
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMoreReplies))]
    [NotifyPropertyChangedFor(nameof(ButtonText))]
    private bool _isExpanded;

    public bool HasMoreReplies => Question.ReplyCount > Replies.Count;

    public string ButtonText => IsExpanded 
        ? (HasMoreReplies ? $"Show More ({Question.ReplyCount - Replies.Count} left)" : "Hide Conversation")
        : $"See Conversation ({Question.ReplyCount})";

    public QuestionViewModel(IQuestionDetails question)
    {
        Question = question;
    }

    public void AddReplies(IEnumerable<IQuestionReplyDetails> newReplies)
    {
        foreach (var reply in newReplies)
        {
            if (!Replies.Any(r => r.Id == reply.Id))
            {
                Replies.Add(reply);
            }
        }
        OnPropertyChanged(nameof(HasMoreReplies));
        OnPropertyChanged(nameof(ButtonText));
    }
}
