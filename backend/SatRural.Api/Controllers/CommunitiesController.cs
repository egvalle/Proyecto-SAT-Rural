using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SatRural.Domain.Entities;
using SatRural.Infrastructure.Persistence;

namespace SatRural.Api.Controllers;

[ApiController]
[Route("api/communities")]
public class CommunitiesController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public CommunitiesController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // GET: api/communities
    [HttpGet]
    public async Task<ActionResult<PagedResult<CommunityResponse>>> Get(
        [FromQuery] string? search,
        [FromQuery] string? municipality,
        [FromQuery] string? department,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new
            {
                message = "Page must be greater than 0 and pageSize must be between 1 and 100."
            });
        }

        var query = _dbContext.Communities
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(community =>
                community.Name.Contains(term) ||
                community.Municipality.Contains(term) ||
                community.Department.Contains(term) ||
                community.Country.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(municipality))
        {
            var term = municipality.Trim();

            query = query.Where(community =>
                community.Municipality.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(department))
        {
            var term = department.Trim();

            query = query.Where(community =>
                community.Department.Contains(term));
        }

        if (isActive.HasValue)
        {
            query = query.Where(community =>
                community.IsActive == isActive.Value);
        }

        var totalItems = await query.CountAsync();

        var communities = await query
            .OrderBy(community => community.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(community => new CommunityResponse(
                community.Id,
                community.Name,
                community.Municipality,
                community.Department,
                community.Country,
                community.Latitude,
                community.Longitude,
                community.Description,
                community.IsActive,
                community.Sensors.Count(),
                community.CreatedAt))
            .ToListAsync();

        return Ok(new PagedResult<CommunityResponse>(
            communities,
            page,
            pageSize,
            totalItems,
            (int)Math.Ceiling(totalItems / (double)pageSize)));
    }

    // GET: api/communities/1
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CommunityResponse>> GetById(int id)
    {
        var community = await _dbContext.Communities
            .AsNoTracking()
            .Where(community => community.Id == id)
            .Select(community => new CommunityResponse(
                community.Id,
                community.Name,
                community.Municipality,
                community.Department,
                community.Country,
                community.Latitude,
                community.Longitude,
                community.Description,
                community.IsActive,
                community.Sensors.Count(),
                community.CreatedAt))
            .FirstOrDefaultAsync();

        if (community is null)
        {
            return NotFound(new
            {
                message = "Community not found."
            });
        }

        return Ok(community);
    }

    // POST: api/communities
    [HttpPost]
    public async Task<ActionResult<CommunityResponse>> Create(
        CreateCommunityRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Municipality) ||
            string.IsNullOrWhiteSpace(request.Department) ||
            string.IsNullOrWhiteSpace(request.Country))
        {
            return BadRequest(new
            {
                message = "Name, municipality, department and country are required."
            });
        }

        if (!CoordinatesAreValid(request.Latitude, request.Longitude))
        {
            return BadRequest(new
            {
                message = "Invalid geographic coordinates."
            });
        }

        var community = new Community
        {
            Name = request.Name.Trim(),
            Municipality = request.Municipality.Trim(),
            Department = request.Department.Trim(),
            Country = request.Country.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Description = CleanDescription(request.Description),
            IsActive = request.IsActive
        };

        _dbContext.Communities.Add(community);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = community.Id },
            ToResponse(community, 0));
    }

    // PUT: api/communities/1
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CommunityResponse>> Update(
        int id,
        UpdateCommunityRequest request)
    {
        var community = await _dbContext.Communities
            .Include(community => community.Sensors)
            .FirstOrDefaultAsync(community => community.Id == id);

        if (community is null)
        {
            return NotFound(new
            {
                message = "Community not found."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Municipality) ||
            string.IsNullOrWhiteSpace(request.Department) ||
            string.IsNullOrWhiteSpace(request.Country))
        {
            return BadRequest(new
            {
                message = "Name, municipality, department and country are required."
            });
        }

        if (!CoordinatesAreValid(request.Latitude, request.Longitude))
        {
            return BadRequest(new
            {
                message = "Invalid geographic coordinates."
            });
        }

        community.Name = request.Name.Trim();
        community.Municipality = request.Municipality.Trim();
        community.Department = request.Department.Trim();
        community.Country = request.Country.Trim();
        community.Latitude = request.Latitude;
        community.Longitude = request.Longitude;
        community.Description = CleanDescription(request.Description);

        await _dbContext.SaveChangesAsync();

        return Ok(ToResponse(community, community.Sensors.Count));
    }

    // PATCH: api/communities/1/status
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<CommunityResponse>> ChangeStatus(
        int id,
        ChangeCommunityStatusRequest request)
    {
        var community = await _dbContext.Communities
            .Include(community => community.Sensors)
            .FirstOrDefaultAsync(community => community.Id == id);

        if (community is null)
        {
            return NotFound(new
            {
                message = "Community not found."
            });
        }

        community.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync();

        return Ok(ToResponse(community, community.Sensors.Count));
    }

    private static bool CoordinatesAreValid(
        decimal? latitude,
        decimal? longitude)
    {
        if (latitude.HasValue &&
            (latitude.Value < -90 || latitude.Value > 90))
        {
            return false;
        }

        if (longitude.HasValue &&
            (longitude.Value < -180 || longitude.Value > 180))
        {
            return false;
        }

        return true;
    }

    private static string? CleanDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
    }

    private static CommunityResponse ToResponse(
        Community community,
        int sensorCount)
    {
        return new CommunityResponse(
            community.Id,
            community.Name,
            community.Municipality,
            community.Department,
            community.Country,
            community.Latitude,
            community.Longitude,
            community.Description,
            community.IsActive,
            sensorCount,
            community.CreatedAt);
    }
}

public sealed record CreateCommunityRequest(
    string Name,
    string Municipality,
    string Department,
    string Country,
    decimal? Latitude,
    decimal? Longitude,
    string? Description,
    bool IsActive = true);

public sealed record UpdateCommunityRequest(
    string Name,
    string Municipality,
    string Department,
    string Country,
    decimal? Latitude,
    decimal? Longitude,
    string? Description);

public sealed record ChangeCommunityStatusRequest(
    bool IsActive);

public sealed record CommunityResponse(
    int Id,
    string Name,
    string Municipality,
    string Department,
    string Country,
    decimal? Latitude,
    decimal? Longitude,
    string? Description,
    bool IsActive,
    int SensorCount,
    DateTime CreatedAt);