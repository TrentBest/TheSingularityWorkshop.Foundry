namespace TheSingularityWorkshop.Foundry;

/// <summary>Host-owned delivery boundary for human-facing CODE RED notices.</summary>
public interface ICodeRedStatusPublisher
{
    ValueTask PublishAsync(CodeRedStatusNotice notice, CancellationToken cancellationToken = default);
}

/// <summary>Validates incident/task state before handing a notice to a host-owned delivery adapter.</summary>
public sealed class CodeRedStatusNotifier(ICodeRedStatusPublisher publisher)
{
    private readonly ICodeRedStatusPublisher _publisher =
        publisher ?? throw new ArgumentNullException(nameof(publisher));

    public ValueTask PublishActiveAsync(CodeRedIncident incident, WorkTaskSnapshot task,
        DateTimeOffset now, DateTimeOffset nextUpdateAt, CancellationToken cancellationToken = default)
    {
        var notice = CodeRedStatusNoticeFactory.CreateActive(incident, task, now, nextUpdateAt);
        return _publisher.PublishAsync(notice, cancellationToken);
    }

    public ValueTask PublishClearedAsync(CodeRedIncident incident,
        CancellationToken cancellationToken = default)
    {
        var notice = CodeRedStatusNoticeFactory.CreateCleared(incident);
        return _publisher.PublishAsync(notice, cancellationToken);
    }
}
