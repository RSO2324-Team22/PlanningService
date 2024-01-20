using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanningService.Database;
using Swashbuckle.AspNetCore.Annotations;

namespace PlanningService.Rehearsals;

[ApiController]
[Route("[controller]")]
public class RehearsalController : ControllerBase {
    private readonly ILogger<RehearsalController> _logger;
    private readonly PlanningDbContext _dbContext;
    private readonly IProducer<string, int> _kafkaProducer;

    public RehearsalController(
            ILogger<RehearsalController> logger,
            IProducer<string, int> kafkaProducer,
            PlanningDbContext dbContext) {
        this._logger = logger;
        this._dbContext = dbContext;
        this._kafkaProducer = kafkaProducer;
    }

    [HttpGet]
    [SwaggerOperation("GetRehearsals")]
    public async Task<IEnumerable<Rehearsal>> GetRehearsals() {
        this._logger.LogInformation("Getting rehearsals");
        return await this._dbContext.Rehearsals.ToListAsync();
    }

    [HttpGet]
    [Route("confirmed")]
    [SwaggerOperation("GetConfirmedRehearsals")]
    public async Task<IEnumerable<Rehearsal>> GetConfirmed() {
        this._logger.LogInformation("Getting rehearsals with Confirmed status");
        return await this._dbContext.Rehearsals
            .Where(r => r.Status == RehearsalStatus.Confirmed)
            .ToListAsync();
    }

    [HttpGet]
    [Route("confirmed/intensive")]
    [SwaggerOperation("GetConfirmedIntensiveRehearsals")]
    public async Task<IEnumerable<Rehearsal>> GetConfirmedIntensive() {
        this._logger.LogInformation("Getting rehearsals with Confirmed status and Intensive type");
        return await this._dbContext.Rehearsals
            .Where(r => r.Status == RehearsalStatus.Confirmed &&
                    r.Type == RehearsalType.Intensive)
            .ToListAsync();
    }

    [HttpGet]
    [Route("confirmed/extra")]
    [SwaggerOperation("GetConfirmedExtraRehearsals")]
    public async Task<IEnumerable<Rehearsal>> GetConfirmedExtra() {
        this._logger.LogInformation("Getting rehearsals with Extra type");
        return await this._dbContext.Rehearsals
            .Where(r => r.Status == RehearsalStatus.Confirmed &&
                    r.Type == RehearsalType.Extra)
            .ToListAsync();
    }

    [HttpGet]
    [Route("{id}")]
    [SwaggerOperation("GetRehearsalById")]
    public async Task<ActionResult<Rehearsal>> GetRehearsalById(int id) {
        this._logger.LogInformation("Getting rehearsal {id}", id);
        Rehearsal? rehearsal = await this._dbContext.Rehearsals
            .Where(r => r.Id == id)
            .SingleOrDefaultAsync();

        if (rehearsal is null) {
            return NotFound();
        }

        return rehearsal;
    }

    [HttpPost]
    [SwaggerOperation("AddRehearsal")]
    public async Task<IResult> AddRehearsal([FromBody] CreateRehearsalModel model) {
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
            Message<string, int> addRehearsalMessage = new Message<string, int>() {
                Key = "add_rehearsal",
                Value = rehearsal.Id
            };
            this._kafkaProducer.Produce("rehearsals", addRehearsalMessage);
            this._logger.LogInformation("Added rehearsal {id}", rehearsal.Id);
            return Results.Created(nameof(Index), rehearsal);
        }
        catch (Exception e)
        {
            const string errMsg = "Error while adding rehearsal";
            this._logger.LogError(e, errMsg);
            return Results.BadRequest(errMsg);
        }
    }

    [HttpPut]
    [Route("{id}")]
    [SwaggerOperation("EditRehearsal")]
    public async Task<ActionResult<Rehearsal>> EditRehearsal(int id, [FromBody] CreateRehearsalModel model) {
        this._logger.LogInformation("Editing rehearsal {id}", id);
        Rehearsal? rehearsal = await this._dbContext.Rehearsals
            .Where(r => r.Id == id)
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
            Message<string, int> editRehearsalMessage = new Message<string, int>() {
                Key = "edit_rehearsal",
                Value = rehearsal.Id
            };
            this._kafkaProducer.Produce("rehearsals", editRehearsalMessage);
            this._logger.LogInformation("Updated rehearsal {id}", id);
            return CreatedAtAction(nameof(GetRehearsalById), rehearsal);
        }
        catch (Exception e)
        {
            this._logger.LogError(e, "There was an error editing rehearsal {id}", id);
            return BadRequest($"There was an error editing rehearsal {id}");
        }
    }

    [HttpDelete]
    [Route("{id}")]
    [SwaggerOperation("DeleteRehearsal")]
    public async Task<ActionResult<Rehearsal>> DeleteRehearsal(int id) {
        this._logger.LogInformation("Deleting rehearsal {id}", id);
        Rehearsal? rehearsal = await this._dbContext.Rehearsals
            .Where(r => r.Id == id)
            .SingleOrDefaultAsync();

        if (rehearsal is null) {
            return NotFound();
        }

        try
        {
            this._dbContext.Remove(rehearsal);
            Message<string, int> deleteRehearsalMessage = new Message<string, int>() {
                Key = "delete_rehearsal",
                Value = rehearsal.Id
            };
            this._kafkaProducer.Produce("rehearsals", deleteRehearsalMessage);
            await this._dbContext.SaveChangesAsync();
            this._logger.LogInformation("Deleted rehearsal {id}", id);
            return Ok(rehearsal);
        }
        catch (Exception e)
        {
            this._logger.LogError(e, "There was an error deleting rehearsal {id}", id);
            return BadRequest($"There was an error deleting rehearsal {id}");
        }
    }
}
