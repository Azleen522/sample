namespace WattWatch.Events;

public record UserRegisteredEvent(int UserId, string Email) : IEvent
{ public DateTime OccurredAt { get; } = DateTime.UtcNow; }

public record UserLoggedInEvent(int UserId, string Email) : IEvent
{ public DateTime OccurredAt { get; } = DateTime.UtcNow; }

public record EnergyReadingReceivedEvent(long ReadingId, int DeviceId, decimal PowerKW, int? UserId) : IEvent
{ public DateTime OccurredAt { get; } = DateTime.UtcNow; }

public record EnergyThresholdExceededEvent(long ReadingId, int DeviceId, decimal PowerKW, decimal MaxPowerKW) : IEvent
{ public DateTime OccurredAt { get; } = DateTime.UtcNow; }

public record AlertAcknowledgedEvent(long AlertId, int UserId) : IEvent
{ public DateTime OccurredAt { get; } = DateTime.UtcNow; }

public record ThresholdUpdatedEvent(int DeviceId, decimal NewMaxPowerKW, int UserId) : IEvent
{ public DateTime OccurredAt { get; } = DateTime.UtcNow; }
