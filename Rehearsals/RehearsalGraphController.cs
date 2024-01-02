using GraphQL.AspNet.Controllers;
using Microsoft.EntityFrameworkCore;
using PlanningService.Database;

namespace PlanningService.Rehearsals;

public class RehearsalsGraphController : GraphController
{
    private readonly ILogger<RehearsalsGraphController> _logger;
    private readonly PlanningDbContext _dbContext;

    public RehearsalsGraphController(
            ILogger<RehearsalsGraphController> logger,
            PlanningDbContext dbContext) {
        this._logger = logger;
        this._dbContext = dbContext;
    }

    public async Task<IEnumerable<Rehearsal>> All() {
        return await this._dbContext.Rehearsals.ToListAsync();
    }

    public async Task<Rehearsal> Rehearsal(int id) {
        return await this._dbContext.Rehearsals
            .Where(r => r.Id == id)
            .SingleAsync();
    }

    public async Task<IEnumerable<Rehearsal>> Rehearsals(ICollection<int> ids) {
        return await this._dbContext.Rehearsals
            .Where(r => ids.Contains(r.Id))
            .ToListAsync();
    }
}
