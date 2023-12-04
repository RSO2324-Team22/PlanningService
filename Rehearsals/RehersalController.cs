using Microsoft.AspNetCore.Mvc;

namespace PlanningService.Rehearsals;

[ApiController]
[Route("rehearsals")]
public class RehearsalController : ControllerBase {
    private readonly ILogger<RehearsalController> _logger;

    public RehearsalController(ILogger<RehearsalController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "GetRehearsals")]
    public IEnumerable<Rehearsal> Get()
    {
        return new List<Rehearsal>();
    }
}
