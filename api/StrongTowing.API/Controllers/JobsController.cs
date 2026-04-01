using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrongTowing.Application.DTOs.Requests;
using StrongTowing.Application.DTOs.Responses;
using StrongTowing.Application.Exceptions;
using StrongTowing.Core.Entities;
using StrongTowing.Core.Constants;
using StrongTowing.Core.Enums;
using StrongTowing.Infrastructure.Data;
using System.Text.Json;
using StrongTowing.API.Services;
using StrongTowing.Application.Abstractions;
using System.IO;
using System.Globalization;

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
    private readonly IPaymentProvider _paymentProvider;
    private readonly IFcmNotificationService _fcmNotificationService;
    private readonly IWebHostEnvironment _environment;
    private readonly IPricingCalculatorService _pricingCalculatorService;

    public JobsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<JobsController> logger,
        IPaymentProvider paymentProvider,
        IFcmNotificationService fcmNotificationService,
        IWebHostEnvironment environment,
        IPricingCalculatorService pricingCalculatorService)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
        _paymentProvider = paymentProvider;
        _fcmNotificationService = fcmNotificationService;
        _environment = environment;
        _pricingCalculatorService = pricingCalculatorService;
    }

    /// <summary>
    /// Get All Jobs (SuperAdmin/Admin/Dispatcher only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
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

            var jobDtos = jobs.Select(j => MapToJobDto(j)).ToList();

            return Ok(jobDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving jobs");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while retrieving jobs." });
        }
    }

    /// <summary>
    /// Create Job (SuperAdmin / Admin / Dispatcher)
    /// Supports creating job with new or existing vehicle and client
    /// </summary>
    [HttpPost]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
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

                // Check if client already exists by phone number (phone number is the unique identifier)
                var existingClient = await _userManager.FindByNameAsync(request.Client.PhoneNumber);
                if (existingClient == null)
                {
                    // Also check by phone number field as fallback
                    existingClient = _context.Users.FirstOrDefault(u => u.PhoneNumber == request.Client.PhoneNumber);
                }
                
                if (existingClient != null)
                {
                    // Verify it's a User role
                    var userRoleId = UserRoles.GetRoleId(UserRoles.User);
                    if (existingClient.RoleId != userRoleId)
                    {
                        return BadRequest(new { error = "Bad Request", message = "A user with this phone number exists but is not a client." });
                    }
                    // Use existing client
                    client = existingClient;
                }
                else
                {
                    // Create new client user - phone number is the unique identifier
                    var userRoleId = UserRoles.GetRoleId(UserRoles.User);
                    var userRole = await _roleManager.FindByIdAsync(userRoleId);
                    if (userRole == null)
                    {
                        return StatusCode(500, new { error = "Server Error", message = "User role not found." });
                    }

                    client = new ApplicationUser
                    {
                        UserName = request.Client.PhoneNumber, // Phone number is used as UserName (unique identifier)
                        Email = request.Client.Email, // Email is optional
                        FullName = request.Client.FullName,
                        PhoneNumber = request.Client.PhoneNumber, // Required - main key
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
                        LicensePlate = request.Vehicle.LicensePlate,
                        LicenseState = request.Vehicle.LicenseState,
                        DriveType = request.Vehicle.DriveType,
                        VehicleType = request.Vehicle.VehicleType,
                        OwnerId = client.Id
                    };

                    _context.Vehicles.Add(vehicle);
                    await _context.SaveChangesAsync();
                }
            }

            // Step 3: Handle Driver Assignment (if provided)
            ApplicationUser? driver = null;
            if (!string.IsNullOrEmpty(request.DriverId))
            {
                driver = await _userManager.FindByIdAsync(request.DriverId);
                if (driver != null)
                {
                    var driverRoleId = UserRoles.GetRoleId(UserRoles.Driver);
                    if (driver.RoleId == driverRoleId && driver.IsActive)
                    {
                        // Driver will be assigned below
                    }
                    else
                    {
                        driver = null; // Invalid driver, ignore
                    }
                }
            }

            // Step 4: Calculate pricing server-side (authoritative).
            var pricingRequest = BuildPricingQuoteRequest(request);
            var pricingQuote = await _pricingCalculatorService.CalculateAsync(pricingRequest);

            if (request.Cost > 0)
            {
                var mismatch = Math.Abs(request.Cost - pricingQuote.GrandTotal);
                if (mismatch > pricingQuote.PricingMismatchTolerance)
                {
                    return BadRequest(new
                    {
                        error = "Bad Request",
                        message = $"Submitted total does not match calculated total. Submitted={request.Cost:0.00}, Calculated={pricingQuote.GrandTotal:0.00}."
                    });
                }
            }

            var invoiceCharges = request.InvoiceCharges ?? new InvoiceChargesData();
            invoiceCharges.UnloadedEnrouteMileage ??= new MileageChargeData();
            invoiceCharges.UnloadedEnrouteMileage.Quantity = pricingQuote.MilesAB;
            invoiceCharges.UnloadedEnrouteMileage.Price = pricingQuote.RateAB;
            invoiceCharges.UnloadedEnrouteMileage.Total = pricingQuote.ChargeAB;

            invoiceCharges.LoadedHookedMileage ??= new MileageChargeData();
            invoiceCharges.LoadedHookedMileage.Quantity = pricingQuote.MilesBC;
            invoiceCharges.LoadedHookedMileage.Price = pricingQuote.RateBC;
            invoiceCharges.LoadedHookedMileage.Total = pricingQuote.ChargeBC;

            invoiceCharges.DeadHeadMileage ??= new MileageChargeData();
            invoiceCharges.DeadHeadMileage.Quantity = pricingQuote.MilesCA;
            invoiceCharges.DeadHeadMileage.Price = pricingQuote.RateCA;
            invoiceCharges.DeadHeadMileage.Total = pricingQuote.ChargeCA;

            invoiceCharges.HookupFee = pricingQuote.HookupFee;
            invoiceCharges.Discount = pricingQuote.DiscountAmount;
            invoiceCharges.ServiceChargePercent = pricingQuote.ServiceChargePercent;
            invoiceCharges.ServiceChargeAmount = pricingQuote.ServiceChargeAmount;
            invoiceCharges.TaxPercent = pricingQuote.TaxPercent;
            invoiceCharges.Subtotal = pricingQuote.BaseSubtotal;
            invoiceCharges.Taxes = pricingQuote.TaxAmount;
            invoiceCharges.GrandTotal = pricingQuote.GrandTotal;
            invoiceCharges.TaxExempt = pricingQuote.TaxExempt;

            if (pricingQuote.ManualTotalOverrideApplied)
            {
                var adjustedBy = _userManager.GetUserId(User);
                invoiceCharges.AdjustedBy = adjustedBy;
                invoiceCharges.AdjustedAt = DateTime.UtcNow;
                invoiceCharges.AdjustmentReason = pricingQuote.ManualOverrideReason;
                invoiceCharges.ManualTotalOverride = pricingQuote.ManualTotalOverride;
                invoiceCharges.ManualOverrideReason = pricingQuote.ManualOverrideReason;
            }

            var invoiceChargesJson = JsonSerializer.Serialize(invoiceCharges);

            // Step 5: Create Job
            var job = new Job
            {
                VehicleId = vehicle.Id,
                Cost = pricingQuote.GrandTotal,
                Notes = request.Notes,
                Status = JobStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                
                // Call/Job Type
                CallType = request.CallType,
                ScheduledDate = request.ScheduledDate,
                ScheduledTime = request.ScheduledTime,
                
                // Company & Account
                CompanyName = request.CompanyName,
                Account = pricingQuote.AccountName ?? request.Account,
                CompanyOverride = request.CompanyOverride,
                
                // Contact Information
                ContactName = request.ContactName ?? request.Client?.ContactName,
                ContactPhoneNumber = request.ContactPhoneNumber ?? request.Client?.PhoneNumber,
                
                // Location
                PickupLocation = request.PickupLocation ?? request.PickupLocation,
                DestinationAddress = request.DestinationAddress ?? request.DropoffLocation,
                
                // Job Details
                Reason = request.Reason,
                Priority = request.Priority,
                InvoiceNumber = request.InvoiceNumber,
                ETA = request.ETA,
                ServiceType = request.ServiceType,
                
                // Vehicle Details (job-specific)
                LicensePlate = request.Vehicle?.LicensePlate,
                LicenseState = request.Vehicle?.LicenseState,
                DriveType = request.Vehicle?.DriveType,
                VehicleType = request.Vehicle?.VehicleType,
                Odometer = request.Vehicle?.Odometer,
                Drivable = request.Vehicle?.Drivable,
                HaveKeys = request.Vehicle?.HaveKeys ?? false,
                KeyLocation = request.Vehicle?.KeyLocation,
                
                // Assignment
                DriverId = driver?.Id,
                Driver = driver,
                TruckId = request.TruckId,
                
                // Notes
                BillingNotes = request.BillingNotes,
                IncludeBillingNotesOnReceipt = request.IncludeBillingNotesOnReceipt,
                
                // Invoice Charges
                InvoiceChargesJson = invoiceChargesJson
            };

            // Set status to Assigned if driver is provided
            if (driver != null)
            {
                job.Status = JobStatus.Assigned;
            }

            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();

            if (string.IsNullOrWhiteSpace(job.InvoiceNumber))
            {
                job.InvoiceNumber = $"INV-{job.Id:D7}";
                await _context.SaveChangesAsync();
            }

            // Load related data for response
            await _context.Entry(job)
                .Reference(j => j.Vehicle)
                .LoadAsync();
            
            await _context.Entry(job.Vehicle)
                .Reference(v => v.Owner)
                .LoadAsync();

            var jobDto = MapToJobDto(job);

            return CreatedAtAction(nameof(GetJobById), new { id = job.Id }, jobDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating job");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while creating the job." });
        }
    }

    /// <summary>
    /// Get jobs assigned to the current driver
    /// </summary>
    [HttpGet("mine")]
    [Authorize(Roles = UserRoles.Driver)]
    public async Task<ActionResult<IEnumerable<JobDto>>> GetMyJobs([FromQuery] string? status = null)
    {
        try
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new { error = "Unauthorized", message = "User not authenticated." });
            }

            var query = _context.Jobs
                .Include(j => j.Vehicle)
                    .ThenInclude(v => v.Owner)
                .Include(j => j.Driver)
                .Include(j => j.Photos)
                .Where(j => j.DriverId == currentUserId)
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

            var jobDtos = jobs.Select(j => MapToJobDto(j)).ToList();
            return Ok(jobDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving driver jobs");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while retrieving jobs." });
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

            if (User.IsInRole(UserRoles.Driver))
            {
                var currentUserId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(currentUserId) || job.DriverId != currentUserId)
                {
                    return StatusCode(403, new { error = "Forbidden", message = "You do not have access to this job." });
                }
            }

            var jobDto = MapToJobDto(job);
            return Ok(jobDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job with ID {JobId}", id);
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while retrieving the job." });
        }
    }

    /// <summary>
    /// Upload a photo for a job (assigned driver, or admin/dispatcher). Maximum 5 photos per job.
    /// </summary>
    [HttpPost("{id}/photos")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher},{UserRoles.Driver}")]
    public async Task<ActionResult<JobDto>> UploadJobPhoto(int id, IFormFile? file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "Bad Request", message = "No file was uploaded." });
            }

            const long maxBytes = 8 * 1024 * 1024;
            if (file.Length > maxBytes)
            {
                return BadRequest(new { error = "Bad Request", message = "File is too large (max 8 MB)." });
            }

            var contentType = file.ContentType?.ToLowerInvariant() ?? string.Empty;
            var ext = contentType switch
            {
                "image/jpeg" or "image/jpg" or "image/pjpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(ext))
            {
                return BadRequest(new { error = "Bad Request", message = "Only JPEG, PNG, or WebP images are allowed." });
            }

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

            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new { error = "Unauthorized", message = "User not authenticated." });
            }

            if (User.IsInRole(UserRoles.Driver))
            {
                if (job.DriverId != currentUserId)
                {
                    return StatusCode(403, new { error = "Forbidden", message = "You can only add photos to jobs assigned to you." });
                }
            }

            if (job.Photos.Count >= 5)
            {
                return BadRequest(new { error = "Bad Request", message = "This job already has the maximum of 5 photos." });
            }

            var webRoot = _environment.WebRootPath;
            if (string.IsNullOrEmpty(webRoot))
            {
                webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");
            }

            var relativeDir = Path.Combine("uploads", "job-photos", id.ToString());
            var physicalDir = Path.Combine(webRoot, relativeDir);
            Directory.CreateDirectory(physicalDir);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var physicalPath = Path.Combine(physicalDir, fileName);

            await using (var stream = new FileStream(physicalPath, FileMode.CreateNew))
            {
                await file.CopyToAsync(stream);
            }

            var publicPath = $"/uploads/job-photos/{id}/{fileName}";

            var photo = new JobPhoto
            {
                JobId = job.Id,
                PhotoUrl = publicPath,
                UploadedAt = DateTime.UtcNow
            };

            _context.JobPhotos.Add(photo);
            await _context.SaveChangesAsync();

            var reloaded = await _context.Jobs
                .Include(j => j.Vehicle)
                    .ThenInclude(v => v.Owner)
                .Include(j => j.Driver)
                .Include(j => j.Photos)
                .Include(j => j.StatusUpdatedBy)
                .FirstAsync(j => j.Id == id);

            return Ok(MapToJobDto(reloaded));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading photo for job {JobId}", id);
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while uploading the photo." });
        }
    }

    /// <summary>
    /// Assign a driver to a pending job (SuperAdmin / Admin / Dispatcher)
    /// </summary>
    [HttpPost("{id}/assign")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<AssignDriverResponseDto>> AssignDriver(int id, [FromBody] AssignDriverRequest request)
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

            if (!driver.IsAvailableForDispatch)
            {
                return BadRequest(new { error = "Bad Request", message = "That driver is off-duty and cannot receive new assignments." });
            }

            job.DriverId = driver.Id;
            job.Driver = driver;
            job.Status = JobStatus.Assigned;

            await _context.SaveChangesAsync();

            var pickup = string.IsNullOrWhiteSpace(job.PickupLocation) ? "Pickup TBD" : job.PickupLocation;
            if (pickup.Length > 120)
            {
                pickup = pickup[..117] + "...";
            }

            var notifyResult = await _fcmNotificationService.SendToUserAsync(
                driver.Id,
                "New job assigned",
                $"Job #{job.Id} — {pickup}",
                new Dictionary<string, string> { ["jobId"] = job.Id.ToString() });

            if (!notifyResult.Sent)
            {
                _logger.LogWarning(
                    "Push notification was not sent after assigning job {JobId} to driver {DriverId}: {Message}",
                    job.Id,
                    driver.Id,
                    notifyResult.Message);
            }

            var jobDto = MapToJobDto(job);
            return Ok(new AssignDriverResponseDto
            {
                Job = jobDto,
                NotificationSent = notifyResult.Sent,
                NotificationMessage = notifyResult.Sent ? null : notifyResult.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning driver to job {JobId}", id);
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while assigning the driver." });
        }
    }

    /// <summary>
    /// Update job status (SuperAdmin / Admin / Dispatcher / Driver). Tracks who updated.
    /// </summary>
    [HttpPut("{id}/status")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher},{UserRoles.Driver}")]
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

            // Business rule: drivers must upload exactly 5 photos before ReadyToRelease.
            // SuperAdmin / Administrator / Dispatcher may set this status without photos (testing, support, corrections).
            if (status == JobStatus.ReadyToRelease && job.Photos.Count != 5 && User.IsInRole(UserRoles.Driver))
            {
                return BadRequest(new { error = "Bad Request", message = "Job must have exactly 5 photos before marking as ReadyToRelease." });
            }

            // Settlement gate: job cannot be marked completed unless payment is settled.
            if (status == JobStatus.Completed && !IsPaymentSettled(job.PaymentStatus))
            {
                return BadRequest(new
                {
                    error = "Payment Not Settled",
                    message = "Job cannot be completed until payment is settled. Use admin override if required."
                });
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

            var jobDto = MapToJobDto(job);
            return Ok(jobDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating job status {JobId}", id);
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while updating the job status." });
        }
    }

    /// <summary>
    /// Admin-only override to complete a job even when payment is not settled.
    /// </summary>
    [HttpPost("{id}/complete-with-override")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator}")]
    public async Task<ActionResult<JobDto>> CompleteWithOverride(int id, [FromBody] OverrideJobCompletionRequest request)
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

            var currentUserId = _userManager.GetUserId(User);
            var currentUser = !string.IsNullOrEmpty(currentUserId)
                ? await _userManager.FindByIdAsync(currentUserId)
                : null;
            var actorName = currentUser?.FullName ?? "Unknown Admin";

            var previousStatus = job.Status;
            job.Status = JobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.StatusUpdatedAt = DateTime.UtcNow;
            job.StatusUpdatedById = currentUserId;
            job.Notes = $"{job.Notes}\n[Payment Override] Completed by {actorName} at {DateTime.UtcNow:u}. Reason: {request.Reason}".Trim();

            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "Job {JobId} completed via admin override by {UserId}. PreviousStatus={PreviousStatus}, PaymentStatus={PaymentStatus}, Reason={Reason}",
                job.Id,
                currentUserId,
                previousStatus,
                job.PaymentStatus,
                request.Reason);

            var jobDto = MapToJobDto(job);
            return Ok(jobDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error overriding completion for job {JobId}", id);
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while overriding job completion." });
        }
    }

    /// <summary>
    /// Cancel a job and apply cancellation fee policy by current job stage.
    /// </summary>
    [HttpPost("{id}/cancel-with-fee")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<JobDto>> CancelWithFee(int id, [FromBody] CancelJobWithFeeRequest request)
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

            if (job.Status == JobStatus.Completed || job.Status == JobStatus.Cancelled)
            {
                return BadRequest(new { error = "Bad Request", message = "Job is already closed and cannot be cancelled." });
            }

            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            var feePercent = request.OverrideFeePercent ?? ResolveCancellationFeePercent(job.Status, settings);
            var feeAmount = decimal.Round(job.Cost * (feePercent / 100m), 2);
            var userId = _userManager.GetUserId(User);

            var payment = await _context.Payments
                .Include(p => p.Job)
                .Where(p => p.JobId == job.Id)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (payment != null && payment.IsPreAuthorization &&
                !string.IsNullOrWhiteSpace(payment.StripePaymentIntentId))
            {
                if (feeAmount > 0)
                {
                    await _paymentProvider.CapturePaymentIntentAsync(payment.StripePaymentIntentId, feeAmount);
                    payment.CapturedAmount = feeAmount;
                    payment.CapturedAt = DateTime.UtcNow;
                    payment.CaptureStatus = PaymentLifecycle.CaptureStatuses.PartiallyCaptured;
                    payment.PaymentStatus = PaymentLifecycle.Statuses.Paid;
                    payment.IsCancellationFeePayment = true;
                    payment.CancellationFeeAmount = feeAmount;
                }
                else
                {
                    await _paymentProvider.CancelPaymentIntentAsync(payment.StripePaymentIntentId);
                    payment.CaptureStatus = PaymentLifecycle.CaptureStatuses.Released;
                    payment.ReleasedAt = DateTime.UtcNow;
                    payment.PaymentStatus = PaymentLifecycle.Statuses.Cancelled;
                }
            }

            job.CancellationFeeAmount = feeAmount;
            job.CancellationReason = request.Reason;
            job.CancelledAt = DateTime.UtcNow;
            job.CancelledBy = userId;
            job.Status = JobStatus.Cancelled;
            job.PaymentStatus = feeAmount > 0 ? PaymentLifecycle.Statuses.Paid : PaymentLifecycle.Statuses.Cancelled;
            job.Notes = $"{job.Notes}\n[Cancellation] Reason: {request.Reason}. FeePercent: {feePercent}. FeeAmount: {feeAmount}".Trim();
            job.StatusUpdatedAt = DateTime.UtcNow;
            job.StatusUpdatedById = userId;

            await _context.SaveChangesAsync();
            return Ok(MapToJobDto(job));
        }
        catch (PaymentProviderException ex)
        {
            _logger.LogError(ex, "{Provider} error during cancellation settlement for job {JobId}", ex.ProviderName, id);
            return StatusCode(502, new { error = "Payment Provider Error", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling job {JobId} with fee", id);
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while cancelling the job." });
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

    private static bool IsPaymentSettled(string? paymentStatus)
    {
        return paymentStatus == "Paid"
            || paymentStatus == "Refunded"
            || paymentStatus == "PartiallyRefunded";
    }

    private static decimal ResolveCancellationFeePercent(JobStatus status, SystemSettings? settings)
    {
        if (settings == null)
        {
            return status switch
            {
                JobStatus.Pending => 0m,
                JobStatus.Assigned => 0m,
                JobStatus.OnRoute => 30m,
                JobStatus.InProgress => 50m,
                JobStatus.ReadyToRelease => 50m,
                _ => 0m
            };
        }

        return status switch
        {
            JobStatus.Pending => settings.CancelFeeBeforeDispatchPercent,
            JobStatus.Assigned => settings.CancelFeeBeforeDispatchPercent,
            JobStatus.OnRoute => settings.CancelFeeAfterDispatchPercent,
            JobStatus.InProgress => settings.CancelFeeAfterArrivalPercent,
            JobStatus.ReadyToRelease => settings.CancelFeeAfterArrivalPercent,
            _ => settings.CancelFeeBeforeDispatchPercent
        };
    }

    private static PricingQuoteRequestDto BuildPricingQuoteRequest(CreateJobRequest request)
    {
        var charges = request.InvoiceCharges;
        var serviceItemsTotal = charges?.ServiceItems?.Sum(x => x.Total) ?? 0m;

        int? accountId = null;
        string? accountName = null;
        if (!string.IsNullOrWhiteSpace(request.Account))
        {
            var accountValue = request.Account.Trim();
            if (int.TryParse(accountValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedAccountId))
            {
                accountId = parsedAccountId;
            }
            else
            {
                accountName = accountValue;
            }
        }

        return new PricingQuoteRequestDto
        {
            AccountId = accountId,
            AccountName = accountName,
            MilesAB = charges?.UnloadedEnrouteMileage?.Quantity ?? 0m,
            MilesBC = charges?.LoadedHookedMileage?.Quantity ?? 0m,
            MilesCA = charges?.DeadHeadMileage?.Quantity ?? 0m,
            RateAB = charges?.UnloadedEnrouteMileage?.Price,
            RateBC = charges?.LoadedHookedMileage?.Price,
            RateCA = charges?.DeadHeadMileage?.Price,
            HookupFee = charges?.HookupFee,
            ServiceChargePercent = charges?.ServiceChargePercent,
            TaxPercent = charges?.TaxPercent,
            DiscountAmount = charges?.Discount ?? 0m,
            DiscountPercent = charges?.DiscountPercent,
            TaxExempt = charges == null ? null : charges.TaxExempt,
            ExtraItemsTotal = serviceItemsTotal,
            ManualTotalOverride = charges?.ManualTotalOverride,
            ManualOverrideReason = charges?.ManualOverrideReason
        };
    }

    private JobDto MapToJobDto(Job job)
    {
        // Deserialize invoice charges if present
        InvoiceChargesData? invoiceCharges = null;
        if (!string.IsNullOrEmpty(job.InvoiceChargesJson))
        {
            try
            {
                invoiceCharges = JsonSerializer.Deserialize<InvoiceChargesData>(job.InvoiceChargesJson);
            }
            catch
            {
                // Ignore deserialization errors
            }
        }

        return new JobDto
        {
            Id = job.Id,
            Status = job.Status.ToString(),
            VehicleId = job.VehicleId,
            Vehicle = job.Vehicle != null ? new VehicleDto
            {
                Id = job.Vehicle.Id,
                VIN = job.Vehicle.VIN,
                Make = job.Vehicle.Make,
                Model = job.Vehicle.Model,
                Year = job.Vehicle.Year,
                Color = job.Vehicle.Color
            } : null,
            ClientId = job.Vehicle?.OwnerId ?? string.Empty,
            ClientName = job.Vehicle?.Owner?.FullName ?? string.Empty,
            ClientEmail = job.Vehicle?.Owner?.Email ?? string.Empty,
            ClientPhoneNumber = job.Vehicle?.Owner?.PhoneNumber,
            
            // Call/Job Type
            CallType = job.CallType,
            ScheduledDate = job.ScheduledDate,
            ScheduledTime = job.ScheduledTime,
            
            // Company & Account
            CompanyName = job.CompanyName,
            Account = job.Account,
            CompanyOverride = job.CompanyOverride,
            
            // Contact Information
            ContactName = job.ContactName,
            ContactPhoneNumber = job.ContactPhoneNumber,
            
            // Location
            PickupLocation = job.PickupLocation,
            DestinationAddress = job.DestinationAddress,
            
            // Job Details
            Reason = job.Reason,
            Priority = job.Priority,
            InvoiceNumber = job.InvoiceNumber,
            ETA = job.ETA,
            ServiceType = job.ServiceType,
            
            // Vehicle Details
            LicensePlate = job.LicensePlate,
            LicenseState = job.LicenseState,
            DriveType = job.DriveType,
            VehicleType = job.VehicleType,
            Odometer = job.Odometer,
            Drivable = job.Drivable,
            HaveKeys = job.HaveKeys,
            KeyLocation = job.KeyLocation,
            
            // Assignment
            DriverId = job.DriverId,
            DriverName = job.Driver?.FullName,
            TruckId = job.TruckId,
            
            // Financials
            Cost = job.Cost,
            PaymentStatus = string.IsNullOrWhiteSpace(job.PaymentStatus) ? "Unpaid" : job.PaymentStatus,
            PaymentMethod = job.PaymentMethod,
            PaidAt = job.PaidAt,
            Notes = job.Notes,
            BillingNotes = job.BillingNotes,
            IncludeBillingNotesOnReceipt = job.IncludeBillingNotesOnReceipt,
            InvoiceCharges = invoiceCharges,
            
            PhotoCount = job.Photos?.Count ?? 0,
            Photos = job.Photos == null || job.Photos.Count == 0
                ? new List<JobPhotoDto>()
                : job.Photos.OrderBy(p => p.UploadedAt).Select(p => new JobPhotoDto
                {
                    Id = p.Id,
                    Url = p.PhotoUrl,
                    UploadedAt = p.UploadedAt
                }).ToList(),
            CreatedAt = job.CreatedAt,
            CompletedAt = job.CompletedAt,
            StatusUpdatedById = job.StatusUpdatedById,
            StatusUpdatedByName = job.StatusUpdatedBy?.FullName,
            StatusUpdatedAt = job.StatusUpdatedAt
        };
    }
}

