using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanningService.Database;
using PlanningService.Kafka;
using Swashbuckle.AspNetCore.Annotations;

namespace PlanningService.Concerts;

[ApiController]
[Route("[controller]")]
public class ConcertController : ControllerBase {
    private readonly ILogger<ConcertController> _logger;
    private readonly HttpContext _httpContext;
    private readonly PlanningDbContext _dbContext;
    private readonly IProducer<string, KafkaMessage> _kafkaProducer;

    public ConcertController(
            ILogger<ConcertController> logger,
            IHttpContextAccessor httpContextAccessor,
            IProducer<string, KafkaMessage> kafkaProducer,
            PlanningDbContext dbContext) {
        this._logger = logger;
        this._httpContext = httpContextAccessor.HttpContext!;
        this._dbContext = dbContext;
        this._kafkaProducer = kafkaProducer;
    }

    [HttpGet]
    [SwaggerOperation("GetConcerts")]
    public async Task<IEnumerable<Concert>> GetConcerts()
    {
        this._logger.LogInformation("Getting all concerts");
        return await this._dbContext.Concerts.ToListAsync();
    }

    [HttpGet]
    [Route("{id}")]
    [SwaggerOperation("GetConcertById")]
    public async Task<ActionResult<Concert>> GetConcertById(int id)
    {
        this._logger.LogInformation("Getting concert {id}", id);
        Concert? concert = await this._dbContext.Concerts
            .Where(c => c.Id == id)
            .SingleOrDefaultAsync();

        if (concert is null) {
            return NotFound();
        }

        return concert;
    }

    [HttpGet]
    [Route("confirmed")]
    [SwaggerOperation("GetConfirmedConcerts")]
    public async Task<ActionResult<IEnumerable<Concert>>> GetConfirmedConcerts()
    {
        this._logger.LogInformation("Getting concerts with Confirmed status");
        try
        {
            var concerts = await this._dbContext.Concerts
                .Where(concert => concert.Status == ConcertStatus.Confirmed)
                .ToListAsync();
            return Ok(concerts);
        }
        catch (Exception e)
        {
            const string errMsg = "Error while fetching concerts";
            this._logger.LogError(e, errMsg);
            return BadRequest(errMsg);
        }
    }

    [HttpPost]
    [SwaggerOperation("AddConcert")]
    public async Task<ActionResult<Concert>> AddConcert([FromBody] CreateConcertModel model)
    {
        this._logger.LogInformation("Adding new concert");
        Concert concert = new Concert {
            Title = model.Title,
            Location = model.Location,
            MeetupTime = model.MeetupTime.ToUniversalTime(),
            SoundCheckTime = model.SoundCheckTime.ToUniversalTime(),
            StartTime = model.StartTime.ToUniversalTime(),
            ExpectedEndTime = model.ExpectedEndTime.ToUniversalTime(),
            Notes = model.Notes,
            Status = model.Status
        };

        try
        {
            this._dbContext.Concerts.Add(concert);
            await this._dbContext.SaveChangesAsync();
            Message<string, KafkaMessage> addConcertMessage = new Message<string, KafkaMessage>() {
                Key = "add_concert",
                Value = new KafkaMessage {
                    EntityId = concert.Id,
                    CorrelationId = this._httpContext.Request.Headers["X-Correlation-Id"]!
                }
            };
            this._kafkaProducer.Produce("concerts", addConcertMessage);
            this._logger.LogInformation("Added concert {id}", concert.Id);
            return CreatedAtAction(nameof(GetConcertById), 
                                   new { id = concert.Id}, concert);
        }
        catch (Exception e)
        {
            const string errMsg = "Error while adding concert";
            this._logger.LogError(e, errMsg);
            return BadRequest(errMsg);
        }
    }

    [HttpPut]
    [Route("{id}")]
    [SwaggerOperation("EditConcert")]
    public async Task<ActionResult<Concert>> EditConcert(int id, [FromBody] CreateConcertModel model)
    {
        this._logger.LogInformation("Editing concert {id}", id);
        Concert? concert = await this._dbContext.Concerts
            .Where(c => c.Id == id)
            .SingleOrDefaultAsync();

        if (concert is null)
        {
            this._logger.LogInformation("Concert {id} does not exist", id);
            return NotFound();
        }

        concert.Title = model.Title;
        concert.Location = model.Location;
        concert.MeetupTime = model.MeetupTime.ToUniversalTime();
        concert.SoundCheckTime = model.SoundCheckTime.ToUniversalTime();
        concert.StartTime = model.StartTime.ToUniversalTime();
        concert.ExpectedEndTime = model.ExpectedEndTime.ToUniversalTime();
        concert.Notes = model.Notes;
        concert.Status = model.Status;

        try
        {
            await this._dbContext.SaveChangesAsync();
            Message<string, KafkaMessage> editConcertMessage = new Message<string, KafkaMessage>() {
                Key = "edit_concert",
                Value = new KafkaMessage {
                    EntityId = concert.Id,
                    CorrelationId = this._httpContext.Request.Headers["X-Correlation-Id"]!
                }
            };
            this._kafkaProducer.Produce("concerts", editConcertMessage);
            this._logger.LogInformation("Updated concert {id}", id);
            return Ok(concert);
        }
        catch (Exception e)
        {
            this._logger.LogError(e, "There was an error editing concert with id: {id}", id);
            return BadRequest($"There was an error editing concert with id: {id}");
        }
    }

    [HttpDelete]
    [Route("{id}")]
    [SwaggerOperation("DeleteConcert")]
    public async Task<ActionResult<Concert>> DeleteConcert(int id)
    {
        this._logger.LogInformation("Deleting concert {id}", id);
        Concert? concert = await this._dbContext.Concerts
            .Where(c => c.Id == id)
            .SingleOrDefaultAsync();

        if (concert is null)
        {
            this._logger.LogInformation("Concert {id} does not exist", id);
            return NotFound();
        }

        try
        {
            this._dbContext.Remove(concert);
            await this._dbContext.SaveChangesAsync();
            Message<string, KafkaMessage> deleteConcertMessage = new Message<string, KafkaMessage>() {
                Key = "delete_concert",
                Value = new KafkaMessage {
                    EntityId = concert.Id,
                    CorrelationId = this._httpContext.Request.Headers["X-Correlation-Id"]!
                }
            };
            this._kafkaProducer.Produce("concerts", deleteConcertMessage);
            this._logger.LogInformation("Deleted concert {id}", id);
            return Ok(concert);
        }
        catch (Exception e)
        {
            this._logger.LogError(e, "There was an error deleting concert with id: {id}", id);
            return BadRequest($"There was an error deleting concert with id: {id}");
        }
    }
}
