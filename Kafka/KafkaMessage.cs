namespace PlanningService.Kafka;

public class KafkaMessage {
    public required int EntityId { get; init; }
    public required string CorrelationId { get; init; }
}
