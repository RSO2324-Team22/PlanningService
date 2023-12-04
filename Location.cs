using Microsoft.EntityFrameworkCore;

namespace PlanningService;

[Owned]
public class Location {
    public required double Latitude { get; init; } 
    public required double Longitude { get; init; } 
}
