using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SatRural.Infrastructure.Persistence;

namespace SatRural.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlertsController : ControllerBase
{
    private readonly AppDbContext _context;

    public AlertsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var alerts = await _context.Alerts
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
        return Ok(alerts);
    }
}