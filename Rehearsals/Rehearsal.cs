namespace PlanningService.Rehearsals;

public class Rehearsal {
    public int Id { get; private set; }
    public required string Title { get; set; }
    public required Location Location { get; set; }
    public required DateTime StartTime { get; set; }
    public required DateTime EndTime { get; set; }
    public string? Notes { get; set; }
    public RehearsalStatus Status { get; set; }
    public RehearsalType Type { get; set; }
}
