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
    public async Task<IEnumerable<Concert>> GetConfirmedConcerts()
    {
        return await this._dbContext.Concerts
            .Where(concert => concert.Status == ConcertStatus.Confirmed)
            .ToListAsync();
    }

    [HttpPost]
    [SwaggerOperation("AddConcert")]
    public async Task<Concert> Add([FromBody] CreateConcertModel model)
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

        this._dbContext.Concerts.Add(concert);
        Message<string, int> addConcertMessage = new Message<string, int>() {
            Key = "add_concert",
            Value = concert.Id
        };
        await this._kafkaProducer.ProduceAsync("concerts", addConcertMessage);
        await this._dbContext.SaveChangesAsync();
        return concert;
    }

    [HttpPut]
    [Route("{id}")]
    [SwaggerOperation("EditConcert")]
    public async Task<Concert> Add(int id, [FromBody] CreateConcertModel model)
    {
        Concert concert = await this._dbContext.Concerts
            .Where(c => c.Id == id)
            .SingleAsync();

        concert.Title = model.Title;
        concert.Location = model.Location;
        concert.MeetupTime = model.MeetupTime;
        concert.SoundCheckTime = model.SoundCheckTime;
        concert.StartTime = model.StartTime;
        concert.ExpectedEndTime = model.ExpectedEndTime;
        concert.Notes = model.Notes;
        concert.Status = Enum.Parse<ConcertStatus>(model.Status ?? "Proposed");

        Message<string, int> editConcertMessage = new Message<string, int>() {
            Key = "edit_concert",
            Value = concert.Id
        };
        await this._kafkaProducer.ProduceAsync("concerts", editConcertMessage);
        await this._dbContext.SaveChangesAsync();
        return concert;
    }

    [HttpDelete]
    [Route("{id}")]
    [SwaggerOperation("DeleteConcert")]
    public async Task<Concert> Add(int id)
    {
        Concert concert = await this._dbContext.Concerts
            .Where(c => c.Id == id)
            .SingleAsync();

        this._dbContext.Remove(concert);
        Message<string, int> deleteConcertMessage = new Message<string, int>() {
            Key = "delete_concert",
            Value = concert.Id
        };
        await this._kafkaProducer.ProduceAsync("concerts", deleteConcertMessage);
        await this._dbContext.SaveChangesAsync();
        return concert;
    }
}
