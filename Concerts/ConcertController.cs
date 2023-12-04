using Microsoft.AspNetCore.Mvc;

namespace PlanningService.Concerts;

[ApiController]
[Route("concerts")]
public class ConcertController : ControllerBase {
    private readonly ILogger<ConcertController> _logger;

    public ConcertController(ILogger<ConcertController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "GetConcert")]
    public IEnumerable<Concert> Get()
    {
        return new List<Concert>();
    }
}
