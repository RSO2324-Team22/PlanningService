using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanningService.Database;
using PlanningService.Kafka;
using Swashbuckle.AspNetCore.Annotations;

namespace PlanningService.Rehearsals;

[ApiController]
[Route("[controller]")]
public class RehearsalController : ControllerBase {
    private readonly ILogger<RehearsalController> _logger;
    private readonly HttpContext _httpContext;
    private readonly PlanningDbContext _dbContext;
    private readonly IProducer<string, KafkaMessage> _kafkaProducer;

    public RehearsalController(
            ILogger<RehearsalController> logger,
            IHttpContextAccessor httpContextAccessor,
            IProducer<string, KafkaMessage> kafkaProducer,
            PlanningDbContext dbContext) {
        this._logger = logger;
        this._httpContext = httpContextAccessor.HttpContext!;
        this._dbContext = dbContext;
        this._kafkaProducer = kafkaProducer;
    }

    [HttpGet]
    [SwaggerOperation("GetRehearsals")]
    public async Task<IEnumerable<Rehearsal>> GetRehearsals()
    {
        this._logger.LogInformation("Getting all rehearsals");
        return await this._dbContext.Rehearsals.ToListAsync();
    }

    [HttpGet]
    [Route("{id}")]
    [SwaggerOperation("GetRehearsalById")]
    public async Task<ActionResult<Rehearsal>> GetRehearsalById(int id)
    {
        this._logger.LogInformation("Getting rehearsal {id}", id);
        Rehearsal? rehearsal = await this._dbContext.Rehearsals
            .Where(c => c.Id == id)
            .SingleOrDefaultAsync();

        if (rehearsal is null) {
            return NotFound();
        }

        return rehearsal;
    }

    [HttpGet]
    [Route("confirmed")]
    [SwaggerOperation("GetConfirmedRehearsals")]
    public async Task<ActionResult<IEnumerable<Rehearsal>>> GetConfirmedRehearsals()
    {
        this._logger.LogInformation("Getting rehearsals with Confirmed status");
        try
        {
            var rehearsals = await this._dbContext.Rehearsals
                .Where(rehearsal => rehearsal.Status == RehearsalStatus.Confirmed)
                .ToListAsync();
            return Ok(rehearsals);
        }
        catch (Exception e)
        {
            const string errMsg = "Error while fetching rehearsals";
            this._logger.LogError(e, errMsg);
            return BadRequest(errMsg);
        }
    }

    [HttpPost]
    [SwaggerOperation("AddRehearsal")]
    public async Task<ActionResult<Rehearsal>> AddRehearsal([FromBody] CreateRehearsalModel model)
    {
        this._logger.LogInformation("Adding new rehearsal");
        Rehearsal rehearsal = new Rehearsal {
            Title = model.Title,
            Location = model.Location,
            StartTime = model.StartTime.ToUniversalTime(),
            EndTime = model.EndTime.ToUniversalTime(),
            Notes = model.Notes,
            Status = model.Status,
            Type = model.Type
        };

        try
        {
            this._dbContext.Rehearsals.Add(rehearsal);
            await this._dbContext.SaveChangesAsync();
            Message<string, KafkaMessage> addRehearsalMessage = new Message<string, KafkaMessage>() {
                Key = "add_rehearsal",
                Value = new KafkaMessage {
                    EntityId = rehearsal.Id,
                    CorrelationId = this._httpContext.Request.Headers["X-Correlation-Id"]!
                }
            };
            this._kafkaProducer.Produce("rehearsals", addRehearsalMessage);
            this._logger.LogInformation("Added rehearsal {id}", rehearsal.Id);
            return CreatedAtAction(nameof(GetRehearsalById), 
                                   new { id = rehearsal.Id}, rehearsal);
        }
        catch (Exception e)
        {
            const string errMsg = "Error while adding rehearsal";
            this._logger.LogError(e, errMsg);
            return BadRequest(errMsg);
        }
    }

    [HttpPut]
    [Route("{id}")]
    [SwaggerOperation("EditRehearsal")]
    public async Task<ActionResult<Rehearsal>> EditRehearsal(int id, [FromBody] CreateRehearsalModel model)
    {
        this._logger.LogInformation("Editing rehearsal {id}", id);
        Rehearsal? rehearsal = await this._dbContext.Rehearsals
            .Where(c => c.Id == id)
            .SingleOrDefaultAsync();

        if (rehearsal is null)
        {
            this._logger.LogInformation("Rehearsal {id} does not exist", id);
            return NotFound();
        }

        rehearsal.Title = model.Title;
        rehearsal.Location = model.Location;
        rehearsal.StartTime = model.StartTime.ToUniversalTime();
        rehearsal.EndTime = model.EndTime.ToUniversalTime();
        rehearsal.Notes = model.Notes;
        rehearsal.Status = model.Status;
        rehearsal.Type = model.Type;

        try
        {
            await this._dbContext.SaveChangesAsync();
            Message<string, KafkaMessage> editRehearsalMessage = new Message<string, KafkaMessage>() {
                Key = "edit_rehearsal",
                Value = new KafkaMessage {
                    EntityId = rehearsal.Id,
                    CorrelationId = this._httpContext.Request.Headers["X-Correlation-Id"]!
                }
            };
            this._kafkaProducer.Produce("rehearsals", editRehearsalMessage);
            this._logger.LogInformation("Updated rehearsal {id}", id);
            return Ok(rehearsal);
        }
        catch (Exception e)
        {
            this._logger.LogError(e, "There was an error editing rehearsal with id: {id}", id);
            return BadRequest($"There was an error editing rehearsal with id: {id}");
        }
    }

    [HttpDelete]
    [Route("{id}")]
    [SwaggerOperation("DeleteRehearsal")]
    public async Task<ActionResult<Rehearsal>> DeleteRehearsal(int id)
    {
        this._logger.LogInformation("Deleting rehearsal {id}", id);
        Rehearsal? rehearsal = await this._dbContext.Rehearsals
            .Where(c => c.Id == id)
            .SingleOrDefaultAsync();

        if (rehearsal is null)
        {
            this._logger.LogInformation("Rehearsal {id} does not exist", id);
            return NotFound();
        }

        try
        {
            this._dbContext.Remove(rehearsal);
            await this._dbContext.SaveChangesAsync();
            Message<string, KafkaMessage> deleteRehearsalMessage = new Message<string, KafkaMessage>() {
                Key = "delete_rehearsal",
                Value = new KafkaMessage {
                    EntityId = rehearsal.Id,
                    CorrelationId = this._httpContext.Request.Headers["X-Correlation-Id"]!
                }
            };
            this._kafkaProducer.Produce("rehearsals", deleteRehearsalMessage);
            this._logger.LogInformation("Deleted rehearsal {id}", id);
            return Ok(rehearsal);
        }
        catch (Exception e)
        {
            this._logger.LogError(e, "There was an error deleting rehearsal with id: {id}", id);
            return BadRequest($"There was an error deleting rehearsal with id: {id}");
        }
    }
}
