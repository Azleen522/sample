namespace WattWatch.Events;

public interface IEvent { DateTime OccurredAt { get; } }

public interface IEventDispatcher
{
    void Subscribe<T>(Action<T> handler) where T : IEvent;
    void Publish<T>(T evt) where T : IEvent;
}

/// <summary>Small in-process publish/subscribe dispatcher.
/// One failing handler never stops the others.</summary>
public class EventDispatcher : IEventDispatcher
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();
    private readonly object _lock = new();

    public void Subscribe<T>(Action<T> handler) where T : IEvent
    {
        lock (_lock)
        {
            if (!_handlers.TryGetValue(typeof(T), out var list))
                _handlers[typeof(T)] = list = new List<Delegate>();
            list.Add(handler);
        }
    }

    public void Publish<T>(T evt) where T : IEvent
    {
        List<Delegate> snapshot;
        lock (_lock)
        {
            if (!_handlers.TryGetValue(typeof(T), out var list)) return;
            snapshot = new List<Delegate>(list);
        }
        foreach (var h in snapshot)
        {
            try { ((Action<T>)h)(evt); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Handler failed for {typeof(T).Name}: {ex}"); }
        }
    }
}
