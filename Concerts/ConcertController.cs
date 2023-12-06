using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanningService.Database;

namespace PlanningService.Concerts;

[ApiController]
[Route("concerts")]
public class ConcertController : ControllerBase {
    private readonly ILogger<ConcertController> _logger;
    private readonly PlanningDbContext _dbContext;

    public ConcertController(
            ILogger<ConcertController> logger,
            PlanningDbContext dbContext)
    {
        this._logger = logger;
        this._dbContext = dbContext;
    }

    [HttpGet(Name = "GetConcerts")]
    [Route("all")]
    [Route("")]
    public async Task<IEnumerable<Concert>> GetConcerts()
    {
        return await this._dbContext.Concerts.ToListAsync();
    }

    [HttpPost(Name = "AddConcert")]
    public async Task<string> Add([FromBody] Concert newConcert)
    {
        try
        {
            this._dbContext.Concerts.Add(newConcert);
            await this._dbContext.SaveChangesAsync();
            return "Koncert uspešno dodan.";
        }
        catch
        {
            return "Napaka pri dodajanju koncerta.";
        }
    }

    [HttpGet(Name = "GetConfirmedConcerts")]
    [Route("confirmed")]
    public async Task<IEnumerable<Concert>> GetConfirmedConcerts()
    {
        return await this._dbContext.Concerts
            .Where(concert => concert.Status == ConcertStatus.Confirmed)
            .ToListAsync();
    }
}
