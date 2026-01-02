using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrongTowing.Application.DTOs.Requests;
using StrongTowing.Application.DTOs.Responses;
using StrongTowing.Core.Entities;
using StrongTowing.Core.Constants;
using StrongTowing.Core.Enums;
using StrongTowing.Infrastructure.Data;

namespace StrongTowing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JobsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<JobsController> _logger;

    public JobsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<JobsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    /// <summary>
    /// Get All Jobs (Admin/Dispatcher only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<IEnumerable<JobDto>>> GetAllJobs([FromQuery] string? status = null)
    {
        try
        {
            var query = _context.Jobs
                .Include(j => j.Vehicle)
                    .ThenInclude(v => v.Owner)
                .Include(j => j.Driver)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<JobStatus>(status, out var statusEnum))
                {
                    query = query.Where(j => j.Status == statusEnum);
                }
            }

            var jobs = await query
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();

            var jobDtos = jobs.Select(j => new JobDto
            {
                Id = j.Id,
                Status = j.Status.ToString(),
                VehicleId = j.VehicleId,
                Vehicle = new VehicleDto
                {
                    Id = j.Vehicle.Id,
                    VIN = j.Vehicle.VIN,
                    Make = j.Vehicle.Make,
                    Model = j.Vehicle.Model,
                    Year = j.Vehicle.Year,
                    Color = j.Vehicle.Color
                },
                ClientId = j.Vehicle.OwnerId,
                ClientName = j.Vehicle.Owner?.FullName ?? string.Empty,
                ClientEmail = j.Vehicle.Owner?.Email ?? string.Empty,
                ClientPhoneNumber = j.Vehicle.Owner?.PhoneNumber,
                Cost = j.Cost,
                Notes = j.Notes,
                DriverId = j.DriverId,
                DriverName = j.Driver?.FullName,
                PhotoCount = j.Photos.Count,
                CreatedAt = j.CreatedAt,
                CompletedAt = j.CompletedAt,
                StatusUpdatedById = j.StatusUpdatedById,
                StatusUpdatedByName = j.StatusUpdatedBy?.FullName,
                StatusUpdatedAt = j.StatusUpdatedAt
            }).ToList();

            return Ok(jobDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving jobs");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while retrieving jobs." });
        }
    }

    /// <summary>
    /// Create Job (Admin/Dispatcher only)
    /// Supports creating job with new or existing vehicle and client
    /// </summary>
    [HttpPost]
    [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<JobDto>> CreateJob([FromBody] CreateJobRequest request)
    {
        try
        {
            // Validate: Must provide either VehicleId OR Vehicle data
            if (request.VehicleId == null && request.Vehicle == null)
            {
                return BadRequest(new { error = "Bad Request", message = "Either VehicleId or Vehicle data must be provided." });
            }

            // Validate: Must provide either ClientId OR Client data
            if (string.IsNullOrEmpty(request.ClientId) && request.Client == null)
            {
                return BadRequest(new { error = "Bad Request", message = "Either ClientId or Client data must be provided." });
            }

            Vehicle vehicle;
            ApplicationUser client;

            // Step 1: Get or Create Client
            if (!string.IsNullOrEmpty(request.ClientId))
            {
                // Use existing client
                client = await _userManager.FindByIdAsync(request.ClientId) ?? throw new InvalidOperationException($"Client with ID {request.ClientId} not found");
                if (client == null)
                {
                    return NotFound(new { error = "Not Found", message = $"Client with ID {request.ClientId} was not found." });
                }

                // Verify client has User role
                var clientRoleId = UserRoles.GetRoleId(UserRoles.User);
                if (client.RoleId != clientRoleId)
                {
                    return BadRequest(new { error = "Bad Request", message = "The provided ClientId does not belong to a client (User role)." });
                }
            }
            else
            {
                // Create new client
                if (request.Client == null)
                {
                    return BadRequest(new { error = "Bad Request", message = "Client data is required when ClientId is not provided." });
                }

                // Check if client already exists by email
                var existingClient = await _userManager.FindByEmailAsync(request.Client.Email);
                if (existingClient != null)
                {
                    // Verify it's a User role
                    var userRoleId = UserRoles.GetRoleId(UserRoles.User);
                    if (existingClient.RoleId != userRoleId)
                    {
                        return BadRequest(new { error = "Bad Request", message = "A user with this email exists but is not a client." });
                    }
                    // Use existing client
                    client = existingClient;
                }
                else
                {
                    // Create new client user
                    var userRoleId = UserRoles.GetRoleId(UserRoles.User);
                    var userRole = await _roleManager.FindByIdAsync(userRoleId);
                    if (userRole == null)
                    {
                        return StatusCode(500, new { error = "Server Error", message = "User role not found." });
                    }

                    client = new ApplicationUser
                    {
                        UserName = request.Client.Email,
                        Email = request.Client.Email,
                        FullName = request.Client.FullName,
                        PhoneNumber = request.Client.PhoneNumber,
                        RoleId = userRoleId,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    // Generate a random password (client will reset it if needed)
                    var password = GenerateRandomPassword();
                    var result = await _userManager.CreateAsync(client, password);
                    
                    if (!result.Succeeded)
                    {
                        return BadRequest(new { error = "Bad Request", message = "Failed to create client.", errors = result.Errors });
                    }
                }
            }

            // Step 2: Get or Create Vehicle
            if (request.VehicleId.HasValue)
            {
                // Use existing vehicle
                vehicle = await _context.Vehicles
                    .Include(v => v.Owner)
                    .FirstOrDefaultAsync(v => v.Id == request.VehicleId.Value) 
                    ?? throw new InvalidOperationException($"Vehicle with ID {request.VehicleId.Value} not found");

                if (vehicle == null)
                {
                    return NotFound(new { error = "Not Found", message = $"Vehicle with ID {request.VehicleId.Value} was not found." });
                }

                // Verify vehicle belongs to the client
                if (vehicle.OwnerId != client.Id)
                {
                    return BadRequest(new { error = "Bad Request", message = "The vehicle does not belong to the specified client." });
                }
            }
            else
            {
                // Create new vehicle
                if (request.Vehicle == null)
                {
                    return BadRequest(new { error = "Bad Request", message = "Vehicle data is required when VehicleId is not provided." });
                }

                // Check if vehicle already exists by VIN
                var existingVehicle = await _context.Vehicles
                    .FirstOrDefaultAsync(v => v.VIN == request.Vehicle.VIN);

                if (existingVehicle != null)
                {
                    // Verify it belongs to the same client
                    if (existingVehicle.OwnerId != client.Id)
                    {
                        return BadRequest(new { error = "Bad Request", message = $"A vehicle with VIN {request.Vehicle.VIN} already exists and belongs to a different client." });
                    }
                    vehicle = existingVehicle;
                }
                else
                {
                    // Create new vehicle
                    vehicle = new Vehicle
                    {
                        VIN = request.Vehicle.VIN,
                        Make = request.Vehicle.Make,
                        Model = request.Vehicle.Model,
                        Year = request.Vehicle.Year,
                        Color = request.Vehicle.Color ?? string.Empty,
                        OwnerId = client.Id
                    };

                    _context.Vehicles.Add(vehicle);
                    await _context.SaveChangesAsync();
                }
            }

            // Step 3: Create Job
            var job = new Job
            {
                VehicleId = vehicle.Id,
                Cost = request.Cost,
                Notes = request.Notes,
                Status = JobStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();

            // Load related data for response
            await _context.Entry(job)
                .Reference(j => j.Vehicle)
                .LoadAsync();
            
            await _context.Entry(job.Vehicle)
                .Reference(v => v.Owner)
                .LoadAsync();

            var jobDto = new JobDto
            {
                Id = job.Id,
                Status = job.Status.ToString(),
                VehicleId = job.VehicleId,
                Vehicle = new VehicleDto
                {
                    Id = vehicle.Id,
                    VIN = vehicle.VIN,
                    Make = vehicle.Make,
                    Model = vehicle.Model,
                    Year = vehicle.Year,
                    Color = vehicle.Color
                },
                ClientId = client.Id,
                ClientName = client.FullName,
                ClientEmail = client.Email ?? string.Empty,
                ClientPhoneNumber = client.PhoneNumber,
                Cost = job.Cost,
                Notes = job.Notes,
                PhotoCount = 0,
                CreatedAt = job.CreatedAt
            };

            return CreatedAtAction(nameof(GetJobById), new { id = job.Id }, jobDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating job");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while creating the job." });
        }
    }

    /// <summary>
    /// Get Job by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<JobDto>> GetJobById(int id)
    {
        try
        {
            var job = await _context.Jobs
                .Include(j => j.Vehicle)
                    .ThenInclude(v => v.Owner)
                .Include(j => j.Driver)
                .Include(j => j.Photos)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null)
            {
                return NotFound(new { error = "Not Found", message = $"Job with ID {id} was not found." });
            }

            var jobDto = new JobDto
            {
                Id = job.Id,
                Status = job.Status.ToString(),
                VehicleId = job.VehicleId,
                Vehicle = new VehicleDto
                {
                    Id = job.Vehicle.Id,
                    VIN = job.Vehicle.VIN,
                    Make = job.Vehicle.Make,
                    Model = job.Vehicle.Model,
                    Year = job.Vehicle.Year,
                    Color = job.Vehicle.Color
                },
                ClientId = job.Vehicle.OwnerId,
                ClientName = job.Vehicle.Owner?.FullName ?? string.Empty,
                ClientEmail = job.Vehicle.Owner?.Email ?? string.Empty,
                ClientPhoneNumber = job.Vehicle.Owner?.PhoneNumber,
                Cost = job.Cost,
                Notes = job.Notes,
                DriverId = job.DriverId,
                DriverName = job.Driver?.FullName,
                PhotoCount = job.Photos.Count,
                CreatedAt = job.CreatedAt,
                CompletedAt = job.CompletedAt,
                StatusUpdatedById = job.StatusUpdatedById,
                StatusUpdatedByName = job.StatusUpdatedBy?.FullName,
                StatusUpdatedAt = job.StatusUpdatedAt
            };

            return Ok(jobDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job with ID {JobId}", id);
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while retrieving the job." });
        }
    }

    /// <summary>
    /// Assign a driver to a pending job (Admin/Dispatcher only)
    /// </summary>
    [HttpPost("{id}/assign")]
    [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<JobDto>> AssignDriver(int id, [FromBody] AssignDriverRequest request)
    {
        try
        {
            var job = await _context.Jobs
                .Include(j => j.Vehicle)
                    .ThenInclude(v => v.Owner)
                .Include(j => j.Driver)
                .Include(j => j.Photos)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null)
            {
                return NotFound(new { error = "Not Found", message = $"Job with ID {id} was not found." });
            }

            if (job.Status != JobStatus.Pending)
            {
                return BadRequest(new { error = "Bad Request", message = "Only pending jobs can be assigned to a driver." });
            }

            var driver = await _userManager.FindByIdAsync(request.DriverId);
            if (driver == null)
            {
                return NotFound(new { error = "Not Found", message = $"Driver with ID {request.DriverId} was not found." });
            }

            var driverRoleId = UserRoles.GetRoleId(UserRoles.Driver);
            if (driver.RoleId != driverRoleId)
            {
                return BadRequest(new { error = "Bad Request", message = "The specified user is not a driver." });
            }

            if (!driver.IsActive)
            {
                return BadRequest(new { error = "Bad Request", message = "The driver is not active and cannot be assigned." });
            }

            job.DriverId = driver.Id;
            job.Driver = driver;
            job.Status = JobStatus.Assigned;

            await _context.SaveChangesAsync();

            var jobDto = new JobDto
            {
                Id = job.Id,
                Status = job.Status.ToString(),
                VehicleId = job.VehicleId,
                Vehicle = new VehicleDto
                {
                    Id = job.Vehicle.Id,
                    VIN = job.Vehicle.VIN,
                    Make = job.Vehicle.Make,
                    Model = job.Vehicle.Model,
                    Year = job.Vehicle.Year,
                    Color = job.Vehicle.Color
                },
                ClientId = job.Vehicle.OwnerId,
                ClientName = job.Vehicle.Owner?.FullName ?? string.Empty,
                ClientEmail = job.Vehicle.Owner?.Email ?? string.Empty,
                ClientPhoneNumber = job.Vehicle.Owner?.PhoneNumber,
                Cost = job.Cost,
                Notes = job.Notes,
                DriverId = job.DriverId,
                DriverName = driver.FullName,
                PhotoCount = job.Photos.Count,
                CreatedAt = job.CreatedAt,
                CompletedAt = job.CompletedAt,
                StatusUpdatedById = job.StatusUpdatedById,
                StatusUpdatedByName = job.StatusUpdatedBy?.FullName,
                StatusUpdatedAt = job.StatusUpdatedAt
            };

            return Ok(jobDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning driver to job {JobId}", id);
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while assigning the driver." });
        }
    }

    /// <summary>
    /// Update job status (Admin/Dispatcher/Driver). Tracks who updated.
    /// </summary>
    [HttpPut("{id}/status")]
    [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Dispatcher},{UserRoles.Driver}")]
    public async Task<ActionResult<JobDto>> UpdateJobStatus(int id, [FromBody] UpdateJobStatusRequest request)
    {
        try
        {
            var job = await _context.Jobs
                .Include(j => j.Vehicle)
                    .ThenInclude(v => v.Owner)
                .Include(j => j.Driver)
                .Include(j => j.Photos)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null)
            {
                return NotFound(new { error = "Not Found", message = $"Job with ID {id} was not found." });
            }

            if (!Enum.TryParse<JobStatus>(request.Status, true, out var status))
            {
                return BadRequest(new { error = "Bad Request", message = "Invalid status value." });
            }

            // Drivers can only update their assigned jobs
            if (User.IsInRole(UserRoles.Driver))
            {
                var currentUserId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(currentUserId) || job.DriverId != currentUserId)
                {
                    return Forbid();
                }
            }

            // Business rule: ReadyToRelease requires exactly 5 photos
            if (status == JobStatus.ReadyToRelease && job.Photos.Count != 5)
            {
                return BadRequest(new { error = "Bad Request", message = "Job must have exactly 5 photos before marking as ReadyToRelease." });
            }

            // Update status and audit fields
            job.Status = status;
            job.StatusUpdatedAt = DateTime.UtcNow;
            job.StatusUpdatedById = _userManager.GetUserId(User);
            job.CompletedAt = status == JobStatus.Completed ? DateTime.UtcNow : job.CompletedAt;

            await _context.SaveChangesAsync();

            // Reload updater to populate DTO
            if (!string.IsNullOrEmpty(job.StatusUpdatedById))
            {
                job.StatusUpdatedBy = await _userManager.FindByIdAsync(job.StatusUpdatedById);
            }

            var jobDto = new JobDto
            {
                Id = job.Id,
                Status = job.Status.ToString(),
                VehicleId = job.VehicleId,
                Vehicle = new VehicleDto
                {
                    Id = job.Vehicle.Id,
                    VIN = job.Vehicle.VIN,
                    Make = job.Vehicle.Make,
                    Model = job.Vehicle.Model,
                    Year = job.Vehicle.Year,
                    Color = job.Vehicle.Color
                },
                ClientId = job.Vehicle.OwnerId,
                ClientName = job.Vehicle.Owner?.FullName ?? string.Empty,
                ClientEmail = job.Vehicle.Owner?.Email ?? string.Empty,
                ClientPhoneNumber = job.Vehicle.Owner?.PhoneNumber,
                Cost = job.Cost,
                Notes = job.Notes,
                DriverId = job.DriverId,
                DriverName = job.Driver?.FullName,
                PhotoCount = job.Photos.Count,
                CreatedAt = job.CreatedAt,
                CompletedAt = job.CompletedAt,
                StatusUpdatedById = job.StatusUpdatedById,
                StatusUpdatedByName = job.StatusUpdatedBy?.FullName,
                StatusUpdatedAt = job.StatusUpdatedAt
            };

            return Ok(jobDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating job status {JobId}", id);
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while updating the job status." });
        }
    }

    private string GenerateRandomPassword()
    {
        // Generate a random password for new clients
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}

