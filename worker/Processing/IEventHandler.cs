namespace LinePulse.Processing;

public interface IEventHandler<in TMessage>
{
    Task HandleAsync(TMessage message, CancellationToken cancellationToken);
}
