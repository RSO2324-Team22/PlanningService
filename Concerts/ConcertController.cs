using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanningService.Database;
using Swashbuckle.AspNetCore.Annotations;

namespace PlanningService.Concerts;

[ApiController]
[Route("[controller]")]
public class ConcertController : ControllerBase {
    private readonly ILogger<ConcertController> _logger;
    private readonly PlanningDbContext _dbContext;
    private readonly IProducer<string, int> _kafkaProducer;

    public ConcertController(
            ILogger<ConcertController> logger,
            IProducer<string, int> kafkaProducer,
            PlanningDbContext dbContext) {
        this._logger = logger;
        this._dbContext = dbContext;
        this._kafkaProducer = kafkaProducer;
    }

    [HttpGet]
    [SwaggerOperation("GetConcerts")]
    public async Task<IEnumerable<Concert>> GetConcerts()
    {
        return await this._dbContext.Concerts.ToListAsync();
    }

    [HttpGet]
    [Route("confirmed")]
    [SwaggerOperation("GetConfirmedConcerts")]
    public async Task<ActionResult<IEnumerable<Concert>>> GetConfirmedConcerts()
    {
        try
        {
            var concerts = await this._dbContext.Concerts
                .Where(concert => concert.Status == ConcertStatus.Confirmed)
                .ToListAsync();
            return Ok(concerts);
        }
        catch (Exception e)
        {
            const string errMsg = "There was a problem fetching confirmed concerts";
            this._logger.LogError(e, errMsg);
            return BadRequest(errMsg);
        }
    }

    [HttpPost]
    [SwaggerOperation("AddConcert")]
    public async Task<IResult> AddConcert([FromBody] CreateConcertModel model)
    {
        Concert concert = new Concert {
            Title = model.Title,
            Location = model.Location,
            MeetupTime = model.MeetupTime,
            SoundCheckTime = model.SoundCheckTime,
            StartTime = model.StartTime,
            ExpectedEndTime = model.ExpectedEndTime,
            Notes = model.Notes,
            Status = Enum.Parse<ConcertStatus>(model.Status ?? "Proposed")
        };

        try
        {
            this._dbContext.Concerts.Add(concert);
            await this._dbContext.SaveChangesAsync();
            Message<string, int> addConcertMessage = new Message<string, int>() {
                Key = "add_concert",
                Value = concert.Id
            };
            await this._kafkaProducer.ProduceAsync("concerts", addConcertMessage);
            this._logger.LogInformation("Added new concert");
            return Results.Created(nameof(Index), concert);
        }
        catch (Exception e)
        {
            const string errMsg = "There was a problem adding new Concert";
            this._logger.LogError(e, errMsg);
            return Results.BadRequest(errMsg);
        }
    }

    [HttpPut]
    [Route("{id}")]
    [SwaggerOperation("EditConcert")]
    public async Task<IResult> EditConcert(int id, [FromBody] CreateConcertModel model)
    {
        Concert? concert = await this._dbContext.Concerts
            .Where(c => c.Id == id)
            .SingleOrDefaultAsync();

        if (concert == null)
        {
            this._logger.LogInformation("Concert with id: {id} does not exist");
            return Results.BadRequest();
        }

        concert.Title = model.Title;
        concert.Location = model.Location;
        concert.MeetupTime = model.MeetupTime;
        concert.SoundCheckTime = model.SoundCheckTime;
        concert.StartTime = model.StartTime;
        concert.ExpectedEndTime = model.ExpectedEndTime;
        concert.Notes = model.Notes;
        concert.Status = Enum.Parse<ConcertStatus>(model.Status ?? "Proposed");

        try
        {
            await this._dbContext.SaveChangesAsync();
            Message<string, int> editConcertMessage = new Message<string, int>() {
                Key = "edit_concert",
                Value = concert.Id
            };
            await this._kafkaProducer.ProduceAsync("concerts", editConcertMessage);
            this._logger.LogInformation("Edited concert with id: {id}", id);
            return Results.Created(nameof(Index), concert);;
        }
        catch (Exception e)
        {
            this._logger.LogError(e, "There was an error editing concert with id: {id}", id);
            return Results.BadRequest($"There was an error editing concert with id: {id}");
        }
    }

    [HttpDelete]
    [Route("{id}")]
    [SwaggerOperation("DeleteConcert")]
    public async Task<IResult> DeleteConcert(int id)
    {
        Concert concert = await this._dbContext.Concerts
            .Where(c => c.Id == id)
            .SingleAsync();

        if (concert == null)
        {
            this._logger.LogInformation("Concert with given id: {id} does not exist");
            return Results.BadRequest();
        }

        try
        {
            this._dbContext.Remove(concert);
            await this._dbContext.SaveChangesAsync();
            Message<string, int> deleteConcertMessage = new Message<string, int>() {
                Key = "delete_concert",
                Value = concert.Id
            };
            await this._kafkaProducer.ProduceAsync("concerts", deleteConcertMessage);
            this._logger.LogInformation("Deleted concert with id: {id}", id);
            return Results.Ok(concert);;
        }
        catch (Exception e)
        {
            this._logger.LogError(e, "There was an error deleting concert with id: {id}", id);
            return Results.BadRequest($"There was an error deleting concert with id: {id}");
        }
    }
}
