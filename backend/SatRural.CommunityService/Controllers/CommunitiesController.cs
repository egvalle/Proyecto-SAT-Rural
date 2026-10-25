using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SatRural.CommunityService.Models;
using SatRural.CommunityService.Persistence;

namespace SatRural.CommunityService.Controllers;

[ApiController]
[Route("api/communities")]
public sealed class CommunitiesController : ControllerBase
{
    private readonly CommunityDbContext _dbContext;

    public CommunitiesController(CommunityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // GET /api/communities?search=&municipality=&department=&isActive=&page=1&pageSize=10
    [HttpGet]
    public async Task<ActionResult<PagedResult<CommunityResponse>>> Get(
        [FromQuery] string? search,
        [FromQuery] string? municipality,
        [FromQuery] string? department,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
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

        var totalItems = await query.CountAsync(cancellationToken);

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
                community.CreatedAt))
            .ToListAsync(cancellationToken);

        var result = new PagedResult<CommunityResponse>(
            communities,
            page,
            pageSize,
            totalItems,
            (int)Math.Ceiling(totalItems / (double)pageSize));

        return Ok(result);
    }

    // POST /api/communities
    [HttpPost]
    public async Task<ActionResult<CommunityResponse>> Create(
        CreateCommunityRequest request,
        CancellationToken cancellationToken)
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
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = ToResponse(community);

        return CreatedAtAction(
            nameof(GetById),
            new { id = community.Id },
            response);
    }

    // PUT /api/communities/2
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CommunityResponse>> Update(
        int id,
        UpdateCommunityRequest request,
        CancellationToken cancellationToken)
    {
        var community = await _dbContext.Communities
            .FirstOrDefaultAsync(
                community => community.Id == id,
                cancellationToken);

        if (community is null)
        {
            return NotFound(new { message = "Community not found." });
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
            return BadRequest(new { message = "Invalid geographic coordinates." });
        }

        community.Name = request.Name.Trim();
        community.Municipality = request.Municipality.Trim();
        community.Department = request.Department.Trim();
        community.Country = request.Country.Trim();
        community.Latitude = request.Latitude;
        community.Longitude = request.Longitude;
        community.Description = CleanDescription(request.Description);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(community));
    }

    // PATCH /api/communities/2/status
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<CommunityResponse>> ChangeStatus(
        int id,
        ChangeCommunityStatusRequest request,
        CancellationToken cancellationToken)
    {
        var community = await _dbContext.Communities
            .FirstOrDefaultAsync(
                community => community.Id == id,
                cancellationToken);

        if (community is null)
        {
            return NotFound(new { message = "Community not found." });
        }

        community.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(community));
    }

    // GET /api/communities/1
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CommunityResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
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
                community.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (community is null)
        {
            return NotFound(new { message = "Community not found." });
        }

        return Ok(community);
    }

    private static bool CoordinatesAreValid(
        decimal? latitude,
        decimal? longitude)
    {
        return (!latitude.HasValue ||
                latitude.Value is >= -90 and <= 90) &&
               (!longitude.HasValue ||
                longitude.Value is >= -180 and <= 180);
    }

    private static string? CleanDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
    }

    private static CommunityResponse ToResponse(Community community)
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
            community.CreatedAt);
    }
}

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
    DateTime CreatedAt);

public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

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

public sealed record ChangeCommunityStatusRequest(bool IsActive);