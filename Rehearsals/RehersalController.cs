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
        return await this._dbContext.Rehearsals.ToListAsync();
    }

    [HttpGet]
    [Route("confirmed")]
    [SwaggerOperation("GetConfirmedRehearsals")]
    public async Task<IEnumerable<Rehearsal>> GetConfirmed() {
        return await this._dbContext.Rehearsals
            .Where(r => r.Status == RehearsalStatus.Confirmed)
            .ToListAsync();
    }

    [HttpGet]
    [Route("confirmed/intensive")]
    [SwaggerOperation("GetConfirmedIntensiveRehearsals")]
    public async Task<IEnumerable<Rehearsal>> GetConfirmedIntensive() {
        return await this._dbContext.Rehearsals
            .Where(r => r.Status == RehearsalStatus.Confirmed &&
                    r.Type == RehearsalType.Intensive)
            .ToListAsync();
    }

    [HttpGet]
    [Route("confirmed/extra")]
    [SwaggerOperation("GetConfirmedExtraRehearsals")]
    public async Task<IEnumerable<Rehearsal>> GetConfirmedExtra() {
        return await this._dbContext.Rehearsals
            .Where(r => r.Status == RehearsalStatus.Confirmed &&
                    r.Type == RehearsalType.Extra)
            .ToListAsync();
    }

    [HttpPost]
    [SwaggerOperation("AddRehearsal")]
    public async Task<Rehearsal> Add([FromBody] CreateRehearsalModel model) {
        Rehearsal rehearsal = new Rehearsal {
            Title = model.Title,
            Location = model.Location,
            StartTime = model.StartTime,
            EndTime = model.EndTime,
            Notes = model.Notes,
            Status = Enum.Parse<RehearsalStatus>(model.Status ?? "Planned"),
            Type = Enum.Parse<RehearsalType>(model.Type ?? "Regular")
        };

        this._dbContext.Rehearsals.Add(rehearsal);
        Message<string, int> addRehearsalMessage = new Message<string, int>() {
            Key = "add_rehearsal",
            Value = rehearsal.Id
        };
        await this._kafkaProducer.ProduceAsync("rehearsals", addRehearsalMessage);
        await this._dbContext.SaveChangesAsync();
        return rehearsal;
    }

    [HttpPut]
    [Route("{id}")]
    [SwaggerOperation("EditRehearsal")]
    public async Task<Rehearsal> Add(int id, [FromBody] CreateRehearsalModel model) {
        Rehearsal rehearsal = await this._dbContext.Rehearsals
            .Where(r => r.Id == id)
            .SingleAsync();

        rehearsal.Title = model.Title;
        rehearsal.Location = model.Location;
        rehearsal.StartTime = model.StartTime;
        rehearsal.EndTime = model.EndTime;
        rehearsal.Notes = model.Notes;
        rehearsal.Status = Enum.Parse<RehearsalStatus>(model.Status ?? "Planned");
        rehearsal.Type = Enum.Parse<RehearsalType>(model.Type ?? "Regular");

        Message<string, int> editRehearsalMessage = new Message<string, int>() {
            Key = "edit_rehearsal",
            Value = rehearsal.Id
        };
        await this._kafkaProducer.ProduceAsync("rehearsals", editRehearsalMessage);
        await this._dbContext.SaveChangesAsync();
        return rehearsal;
    }

    [HttpDelete]
    [Route("{id}")]
    [SwaggerOperation("DeleteRehearsal")]
    public async Task<Rehearsal> Add(int id) {
        Rehearsal rehearsal = await this._dbContext.Rehearsals
            .Where(r => r.Id == id)
            .SingleAsync();

        this._dbContext.Remove(rehearsal);
        Message<string, int> deleteRehearsalMessage = new Message<string, int>() {
            Key = "delete_rehearsal",
            Value = rehearsal.Id
        };
        await this._kafkaProducer.ProduceAsync("rehearsals", deleteRehearsalMessage);
        await this._dbContext.SaveChangesAsync();
        return rehearsal;
    }
}
