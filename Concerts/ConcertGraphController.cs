using System.Threading.Tasks;
using GraphQL.AspNet.Attributes;
using GraphQL.AspNet.Controllers;
using Microsoft.EntityFrameworkCore;
using PlanningService.Database;

namespace PlanningService.Concerts;

public class ConcertGraphController : GraphController
{
    private readonly ILogger<ConcertGraphController> _logger;
    private readonly PlanningDbContext _dbContext;

    public ConcertGraphController(
            ILogger<ConcertGraphController> logger,
            PlanningDbContext dbContext) {
        this._logger = logger;
        this._dbContext = dbContext;
    }

    [Query]
    public async Task<IEnumerable<Concert>> All() {
        this._logger.LogInformation("Getting all concerts");
        return await this._dbContext.Concerts.ToListAsync();
    }

    [Query]
    public async Task<Concert> Concert(int id) {
        this._logger.LogInformation("Getting concert {id}", id);
        return await this._dbContext.Concerts
            .Where(c => c.Id == id)
            .SingleAsync();
    }
    
    [Query]
    public async Task<IEnumerable<Concert>> Concerts(ICollection<int> ids) {
        this._logger.LogInformation("Getting concerts {ids}", ids);
        return await this._dbContext.Concerts
            .Where(c => ids.Contains(c.Id))
            .ToListAsync();
    }
}
