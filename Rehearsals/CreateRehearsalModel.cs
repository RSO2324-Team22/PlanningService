using PlanningService.Common;

namespace PlanningService.Rehearsals;

public class CreateRehearsalModel {
    public required string Title { get; set; }
    public required Location Location { get; set; }
    public required DateTime StartTime { get; set; }
    public required DateTime EndTime { get; set; }
    public string? Notes { get; set; }
    public string? Status { get; set; }
    public string Type { get; set; }
}
