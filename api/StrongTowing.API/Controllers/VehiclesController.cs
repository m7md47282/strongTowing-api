using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrongTowing.Application.DTOs.Requests;
using StrongTowing.Application.DTOs.Responses;
using StrongTowing.Core.Entities;
using StrongTowing.Core.Constants;
using StrongTowing.Infrastructure.Data;

namespace StrongTowing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VehiclesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VehiclesController> _logger;

    public VehiclesController(ApplicationDbContext context, ILogger<VehiclesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get All Vehicles
    /// </summary>
    /// <returns>List of all vehicles</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<VehicleDto>>> GetAllVehicles()
    {
        try
        {
            var vehicles = await _context.Vehicles
                .Include(v => v.Owner)
                .OrderBy(v => v.Id)
                .ToListAsync();

            var vehicleDtos = vehicles.Select(v => new VehicleDto
            {
                Id = v.Id,
                VIN = v.VIN,
                Make = v.Make,
                Model = v.Model,
                Year = v.Year,
                Color = v.Color
            }).ToList();

            return Ok(vehicleDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving vehicles");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while retrieving vehicles." });
        }
    }

    /// <summary>
    /// Create Vehicle (Admin/Dispatcher only)
    /// </summary>
    /// <param name="request">Vehicle creation request</param>
    /// <returns>Created vehicle</returns>
    [HttpPost]
    [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<VehicleDto>> CreateVehicle([FromBody] CreateVehicleRequest request)
    {
        try
        {
            // Check if VIN already exists
            var existingVehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.VIN == request.VIN);

            if (existingVehicle != null)
            {
                return BadRequest(new { error = "Bad Request", message = $"A vehicle with VIN {request.VIN} already exists." });
            }

            // Verify owner exists and is a client (User role)
            var owner = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.OwnerId);
            
            if (owner == null)
            {
                return NotFound(new { error = "Not Found", message = $"Owner with ID {request.OwnerId} was not found." });
            }

            var userRoleId = UserRoles.GetRoleId(UserRoles.User);
            if (owner.RoleId != userRoleId)
            {
                return BadRequest(new { error = "Bad Request", message = "The provided OwnerId does not belong to a client (User role)." });
            }

            var vehicle = new Vehicle
            {
                VIN = request.VIN,
                Make = request.Make,
                Model = request.Model,
                Year = request.Year,
                Color = request.Color ?? string.Empty,
                OwnerId = request.OwnerId
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            var vehicleDto = new VehicleDto
            {
                Id = vehicle.Id,
                VIN = vehicle.VIN,
                Make = vehicle.Make,
                Model = vehicle.Model,
                Year = vehicle.Year,
                Color = vehicle.Color
            };

            return CreatedAtAction(nameof(GetVehicleById), new { id = vehicle.Id }, vehicleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating vehicle");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while creating the vehicle." });
        }
    }

    /// <summary>
    /// Get Vehicle by ID
    /// </summary>
    /// <param name="id">Vehicle ID</param>
    /// <returns>Vehicle details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<VehicleDto>> GetVehicleById(int id)
    {
        try
        {
            var vehicle = await _context.Vehicles
                .Include(v => v.Owner)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (vehicle == null)
            {
                return NotFound(new { error = "Not Found", message = $"Vehicle with ID {id} was not found." });
            }

            var vehicleDto = new VehicleDto
            {
                Id = vehicle.Id,
                VIN = vehicle.VIN,
                Make = vehicle.Make,
                Model = vehicle.Model,
                Year = vehicle.Year,
                Color = vehicle.Color
            };

            return Ok(vehicleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving vehicle with ID {VehicleId}", id);
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while retrieving the vehicle." });
        }
    }

    /// <summary>
    /// Get Vehicle by VIN
    /// </summary>
    /// <param name="vin">Vehicle Identification Number</param>
    /// <returns>Vehicle details</returns>
    [HttpGet("vin/{vin}")]
    public async Task<ActionResult<VehicleDto>> GetVehicleByVIN(string vin)
    {
        try
        {
            var vehicle = await _context.Vehicles
                .Include(v => v.Owner)
                .FirstOrDefaultAsync(v => v.VIN == vin);

            if (vehicle == null)
            {
                return NotFound(new { error = "Not Found", message = $"Vehicle with VIN {vin} was not found." });
            }

            var vehicleDto = new VehicleDto
            {
                Id = vehicle.Id,
                VIN = vehicle.VIN,
                Make = vehicle.Make,
                Model = vehicle.Model,
                Year = vehicle.Year,
                Color = vehicle.Color
            };

            return Ok(vehicleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving vehicle with VIN {VIN}", vin);
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while retrieving the vehicle." });
        }
    }
}

