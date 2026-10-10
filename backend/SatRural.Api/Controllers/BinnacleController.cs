using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SatRural.Domain.Entities;
using SatRural.Infrastructure.Persistence;

namespace SatRural.Api.Controllers;

[ApiController]
[Route("api/binnacle")]
[Authorize]
public class BinnacleController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public BinnacleController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BinnacleResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var entries = await _dbContext.Binnacles
            .AsNoTracking()
            .OrderByDescending(entry => entry.DateHour)
            .ThenByDescending(entry => entry.Id)
            .Select(entry => new BinnacleResponse(
                entry.Id,   
                entry.Description,
                entry.IdMovimentType,
                entry.User,
                entry.DateHour))
            .ToListAsync(cancellationToken);

        return Ok(entries);
    }

    [HttpPost]
    public async Task<ActionResult<BinnacleResponse>> Create(
        CreateBinnacleRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Description) ||
            string.IsNullOrWhiteSpace(request.User))
        {
            return BadRequest(new
            {
                message = "Description and User are required."
            });
        }

        var entry = new Binnacle
        {
            Description = request.Description.Trim(),
            IdMovimentType = request.IdMovimentType,
            User = request.User.Trim(),
            DateHour = request.DateHour
        };

        _dbContext.Binnacles.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new BinnacleResponse(
            entry.Id,
            entry.Description,
            entry.IdMovimentType,
            entry.User,
            entry.DateHour);

        return CreatedAtAction(
            nameof(GetAll),
            response);
    }
}

public sealed record CreateBinnacleRequest(
    string Description,
    int IdMovimentType,
    string User,
    DateTime DateHour);

public sealed record BinnacleResponse(
    int Id,
    string Description,
    int IdMovimentType,
    string User,
    DateTime DateHour);