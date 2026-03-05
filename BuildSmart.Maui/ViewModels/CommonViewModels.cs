using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BuildSmart.Maui.ViewModels;

public partial class JobPostViewModel : ObservableObject
{
    public IJobPostDetails JobPost { get; }
    public ObservableCollection<QuestionViewModel> Questions { get; } = new();

    public JobPostViewModel(IJobPostDetails jobPost, Func<QuestionViewModel, Task>? loadMoreRepliesAction = null)
    {
        JobPost = jobPost;
        if (jobPost.Questions != null)
        {
            foreach (var q in jobPost.Questions)
            {
                Questions.Add(new QuestionViewModel(q, loadMoreRepliesAction));
            }
        }
    }
}

public partial class QuestionViewModel : ObservableObject
{
    public IQuestionDetails Question { get; }
    
    public ObservableCollection<IQuestionReplyDetails> Replies { get; } = new();
    
    private readonly Func<QuestionViewModel, Task>? _loadMoreAction;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMoreReplies))]
    [NotifyPropertyChangedFor(nameof(ButtonText))]
    private bool _isExpanded;

    public bool HasMoreReplies => Question.ReplyCount > Replies.Count;
    
    public bool HasAnyReplies => Question.ReplyCount > 0;

    public string ButtonText => IsExpanded 
        ? (HasMoreReplies ? $"Show More ({Question.ReplyCount - Replies.Count} left)" : "Hide Conversation")
        : $"See Conversation ({Question.ReplyCount})";

    public QuestionViewModel(IQuestionDetails question, Func<QuestionViewModel, Task>? loadMoreAction = null)
    {
        Question = question;
        _loadMoreAction = loadMoreAction;
    }

    [RelayCommand]
    private async Task ToggleConversationAsync()
    {
        if (IsExpanded && !HasMoreReplies)
        {
            IsExpanded = false;
            return;
        }

        if (!IsExpanded || HasMoreReplies)
        {
            if (_loadMoreAction != null)
            {
                await _loadMoreAction(this);
            }
            IsExpanded = true;
        }
    }

    [RelayCommand]
    private async Task LoadMoreRepliesAsync()
    {
        if (_loadMoreAction != null)
        {
            await _loadMoreAction(this);
        }
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
