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
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMoreReplies))]
    [NotifyPropertyChangedFor(nameof(HasAnyReplies))]
    [NotifyPropertyChangedFor(nameof(ButtonText))]
    private IQuestionDetails _question;
    
    public ObservableCollection<IQuestionReplyDetails> Replies { get; } = new();
    
    private readonly Func<QuestionViewModel, Task>? _loadMoreAction;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMoreReplies))]
    [NotifyPropertyChangedFor(nameof(ButtonText))]
    private bool _isExpanded;

    public bool HasMoreReplies => Question?.ReplyCount > Replies.Count;
    
    public bool HasAnyReplies => Question?.ReplyCount > 0;

    public string ButtonText => IsExpanded 
        ? (HasMoreReplies ? $"Show More ({Question?.ReplyCount - Replies.Count} left)" : "Hide Conversation")
        : $"See Conversation ({Question?.ReplyCount})";

    public QuestionViewModel(IQuestionDetails question, Func<QuestionViewModel, Task>? loadMoreAction = null)
    {
        _question = question;
        _loadMoreAction = loadMoreAction;
    }

    public void UpdateQuestion(IQuestionDetails updatedQuestion)
    {
        Question = updatedQuestion;
    }

    public void UpdateAnswer(IQuestionDetails updatedQuestion)
    {
        Question = updatedQuestion;
    }

    public void AddReply(IQuestionReplyDetails newReply)
    {
        if (!Replies.Any(r => r.Id == newReply.Id))
        {
            Replies.Add(newReply);
            OnPropertyChanged(nameof(HasMoreReplies));
            OnPropertyChanged(nameof(ButtonText));
        }
    }

    public void UpdateReply(IQuestionReplyDetails updatedReply)
    {
        var existing = Replies.FirstOrDefault(r => r.Id == updatedReply.Id);
        if (existing != null)
        {
            var index = Replies.IndexOf(existing);
            Replies[index] = updatedReply;
        }
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
