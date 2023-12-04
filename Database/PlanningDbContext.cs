using Microsoft.EntityFrameworkCore;
using PlanningService.Concerts;
using PlanningService.Rehearsals;

namespace PlanningService.Database;

public class PlanningDbContext : DbContext {
    private readonly ILogger<PlanningDbContext> _logger;

    public DbSet<Concert> Concerts { get; private set; }
    public DbSet<Rehearsal> Rehearsals { get; private set; }

    public PlanningDbContext(
            DbContextOptions<PlanningDbContext> options,
            ILogger<PlanningDbContext> logger) : base(options) {
        this._logger = logger;
    }
}
