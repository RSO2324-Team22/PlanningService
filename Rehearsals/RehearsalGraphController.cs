using GraphQL.AspNet.Attributes;
using GraphQL.AspNet.Controllers;
using Microsoft.EntityFrameworkCore;
using PlanningService.Database;

namespace PlanningService.Rehearsals;

public class RehearsalGraphController : GraphController
{
    private readonly ILogger<RehearsalGraphController> _logger;
    private readonly PlanningDbContext _dbContext;

    public RehearsalGraphController(
            ILogger<RehearsalGraphController> logger,
            PlanningDbContext dbContext) {
        this._logger = logger;
        this._dbContext = dbContext;
    }

    [Query]
    public async Task<IEnumerable<Rehearsal>> All() {
        this._logger.LogInformation("Getting all concerts");
        return await this._dbContext.Rehearsals.ToListAsync();
    }

    [Query]
    public async Task<Rehearsal> Rehearsal(int id) {
        this._logger.LogInformation("Getting concert {id}", id);
        return await this._dbContext.Rehearsals
            .Where(r => r.Id == id)
            .SingleAsync();
    }

    [Query]
    public async Task<IEnumerable<Rehearsal>> Rehearsals(ICollection<int> ids) {
        this._logger.LogInformation("Getting concerts {ids}", ids);
        return await this._dbContext.Rehearsals
            .Where(r => ids.Contains(r.Id))
            .ToListAsync();
    }
}
