using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using GreenDonut;

namespace BuildSmart.Api.GraphQL.DataLoaders;

public class FeedbacksByJobPostIdDataLoader : BatchDataLoader<Guid, IEnumerable<JobPostFeedback>>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public FeedbacksByJobPostIdDataLoader(
        IServiceScopeFactory scopeFactory,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task<IReadOnlyDictionary<Guid, IEnumerable<JobPostFeedback>>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IJobPostService>();
        var lookup = await service.GetFeedbacksBatchByJobPostIdsAsync(keys);
        return keys.ToDictionary(k => k, k => (IEnumerable<JobPostFeedback>)lookup[k]);
    }
}

public class BidsByJobPostIdDataLoader : BatchDataLoader<Guid, IEnumerable<Bid>>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public BidsByJobPostIdDataLoader(
        IServiceScopeFactory scopeFactory,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task<IReadOnlyDictionary<Guid, IEnumerable<Bid>>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IJobPostService>();
        var lookup = await service.GetBidsBatchByJobPostIdsAsync(keys);
        return keys.ToDictionary(k => k, k => (IEnumerable<Bid>)lookup[k]);
    }
}

public class JobTasksByJobPostIdDataLoader : BatchDataLoader<Guid, IEnumerable<JobTask>>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public JobTasksByJobPostIdDataLoader(
        IServiceScopeFactory scopeFactory,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task<IReadOnlyDictionary<Guid, IEnumerable<JobTask>>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IJobPostService>();
        var lookup = await service.GetJobTasksBatchByJobPostIdsAsync(keys);
        return keys.ToDictionary(k => k, k => (IEnumerable<JobTask>)lookup[k]);
    }
}

public class QuestionsByJobPostIdDataLoader : BatchDataLoader<Guid, IEnumerable<JobPostQuestion>>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public QuestionsByJobPostIdDataLoader(
        IServiceScopeFactory scopeFactory,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task<IReadOnlyDictionary<Guid, IEnumerable<JobPostQuestion>>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IJobPostService>();
        var lookup = await service.GetQuestionsBatchByJobPostIdsAsync(keys);
        return keys.ToDictionary(k => k, k => (IEnumerable<JobPostQuestion>)lookup[k]);
    }
}

public class QuestionReplyCountDataLoader : BatchDataLoader<Guid, int>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public QuestionReplyCountDataLoader(
        IServiceScopeFactory scopeFactory,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task<IReadOnlyDictionary<Guid, int>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IJobPostService>();
        var counts = await service.GetQuestionReplyCountsBatchAsync(keys);
        return keys.ToDictionary(k => k, k => counts.TryGetValue(k, out var count) ? count : 0);
    }
}