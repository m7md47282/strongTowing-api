using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrongTowing.Application.DTOs.Requests;
using StrongTowing.Application.DTOs.Responses;
using StrongTowing.Core.Constants;
using StrongTowing.Core.Entities;
using StrongTowing.Infrastructure.Data;

namespace StrongTowing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private const int MaxInvoiceImages = 20;

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<InvoicesController> _logger;

    public InvoicesController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment,
        ILogger<InvoicesController> logger)
    {
        _context = context;
        _userManager = userManager;
        _environment = environment;
        _logger = logger;
    }

    // GET api/invoices
    [HttpGet]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var query = _context.Invoices
            .AsNoTracking()
            .Include(i => i.CreatedBy)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(i =>
                i.InvoiceNumber.ToLower().Contains(s) ||
                i.ClientName.ToLower().Contains(s) ||
                (i.ClientPhone != null && i.ClientPhone.Contains(s)) ||
                (i.ClientEmail != null && i.ClientEmail.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "All")
            query = query.Where(i => i.Status == status);

        if (from.HasValue)
            query = query.Where(i => i.IssuedDate >= from.Value);

        if (to.HasValue)
            query = query.Where(i => i.IssuedDate <= to.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new InvoiceListItemDto
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                IssuedDate = i.IssuedDate,
                DueDate = i.DueDate,
                ClientName = i.ClientName,
                ClientPhone = i.ClientPhone,
                Total = i.Total,
                Status = i.Status,
                CreatedByName = i.CreatedBy != null ? i.CreatedBy.FullName : null,
                CreatedAt = i.CreatedAt,
                LineItemCount = i.LineItems.Count
            })
            .ToListAsync();

        return Ok(new
        {
            data = items,
            pageNumber = page,
            pageSize,
            totalCount,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            hasPreviousPage = page > 1,
            hasNextPage = page * pageSize < totalCount
        });
    }

    // GET api/invoices/{id}
    [HttpGet("{id:int}")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<IActionResult> GetInvoice(int id)
    {
        var invoice = await _context.Invoices
            .AsNoTracking()
            .Include(i => i.LineItems)
            .Include(i => i.Images)
            .Include(i => i.CreatedBy)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
            return NotFound(new { message = "Invoice not found." });

        return Ok(MapToDto(invoice));
    }

    // POST api/invoices
    [HttpPost]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var currentUser = await _userManager.GetUserAsync(User);

        var invoice = new Invoice
        {
            IssuedDate = request.IssuedDate,
            DueDate = request.DueDate,
            ClientName = request.ClientName.Trim(),
            ClientPhone = request.ClientPhone?.Trim(),
            ClientEmail = request.ClientEmail?.Trim(),
            ClientAddress = request.ClientAddress?.Trim(),
            JobId = request.JobId,
            TaxRate = request.TaxRate,
            Notes = request.Notes?.Trim(),
            Status = request.Status,
            HideLogo = request.HideLogo,
            HideCompanyName = request.HideCompanyName,
            CompanyDisplayName = string.IsNullOrWhiteSpace(request.CompanyDisplayName)
                ? null
                : request.CompanyDisplayName.Trim(),
            CreatedById = currentUser?.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add line items
        int order = 0;
        foreach (var item in request.LineItems)
        {
            var lineTotal = CalculateLineItemTotal(item.UnitPrice, item.Quantity, item.Discount, item.IsDiscountPercentage);
            invoice.LineItems.Add(new InvoiceLineItem
            {
                ServiceName = item.ServiceName.Trim(),
                Details = item.Details?.Trim(),
                Category = item.Category,
                UnitType = item.UnitType?.Trim(),
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                Discount = item.Discount,
                IsDiscountPercentage = item.IsDiscountPercentage,
                IsTaxable = item.IsTaxable,
                Total = lineTotal,
                SortOrder = item.SortOrder > 0 ? item.SortOrder : order++
            });
        }

        RecalculateTotals(invoice);

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        // Set invoice number after save (so we have the Id)
        invoice.InvoiceNumber = $"INV-{invoice.Id}";
        await _context.SaveChangesAsync();

        // Reload for response
        await _context.Entry(invoice).Reference(i => i.CreatedBy).LoadAsync();

        return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, MapToDto(invoice));
    }

    // PUT api/invoices/{id}
    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<IActionResult> UpdateInvoice(int id, [FromBody] UpdateInvoiceRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var invoice = await _context.Invoices
            .Include(i => i.LineItems)
            .Include(i => i.Images)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
            return NotFound(new { message = "Invoice not found." });

        invoice.IssuedDate = request.IssuedDate;
        invoice.DueDate = request.DueDate;
        invoice.ClientName = request.ClientName.Trim();
        invoice.ClientPhone = request.ClientPhone?.Trim();
        invoice.ClientEmail = request.ClientEmail?.Trim();
        invoice.ClientAddress = request.ClientAddress?.Trim();
        invoice.JobId = request.JobId;
        invoice.TaxRate = request.TaxRate;
        invoice.Notes = request.Notes?.Trim();
        invoice.Status = request.Status;
        invoice.HideLogo = request.HideLogo;
        invoice.HideCompanyName = request.HideCompanyName;
        invoice.CompanyDisplayName = string.IsNullOrWhiteSpace(request.CompanyDisplayName)
            ? null
            : request.CompanyDisplayName.Trim();

        if (request.ClearCustomLogo && !string.IsNullOrEmpty(invoice.CustomLogoUrl))
        {
            TryDeleteInvoiceLogoPhysicalFile(invoice.CustomLogoUrl);
            invoice.CustomLogoUrl = null;
        }

        invoice.UpdatedAt = DateTime.UtcNow;

        // Replace line items
        _context.InvoiceLineItems.RemoveRange(invoice.LineItems);
        invoice.LineItems.Clear();

        int order = 0;
        foreach (var item in request.LineItems)
        {
            var lineTotal = CalculateLineItemTotal(item.UnitPrice, item.Quantity, item.Discount, item.IsDiscountPercentage);
            invoice.LineItems.Add(new InvoiceLineItem
            {
                ServiceName = item.ServiceName.Trim(),
                Details = item.Details?.Trim(),
                Category = item.Category,
                UnitType = item.UnitType?.Trim(),
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                Discount = item.Discount,
                IsDiscountPercentage = item.IsDiscountPercentage,
                IsTaxable = item.IsTaxable,
                Total = lineTotal,
                SortOrder = item.SortOrder > 0 ? item.SortOrder : order++
            });
        }

        RecalculateTotals(invoice);

        await _context.SaveChangesAsync();

        await _context.Entry(invoice).Reference(i => i.CreatedBy).LoadAsync();

        return Ok(MapToDto(invoice));
    }

    // PATCH api/invoices/{id}/status
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateInvoiceStatusRequest request)
    {
        var validStatuses = new[] { "Draft", "Sent", "Paid", "Cancelled" };
        if (!validStatuses.Contains(request.Status))
            return BadRequest(new { message = $"Invalid status. Allowed: {string.Join(", ", validStatuses)}" });

        var invoice = await _context.Invoices.FindAsync(id);
        if (invoice == null)
            return NotFound(new { message = "Invoice not found." });

        invoice.Status = request.Status;
        invoice.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { id = invoice.Id, status = invoice.Status });
    }

    // DELETE api/invoices/{id}
    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator}")]
    public async Task<IActionResult> DeleteInvoice(int id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.LineItems)
            .Include(i => i.Images)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
            return NotFound(new { message = "Invoice not found." });

        foreach (var img in invoice.Images)
            TryDeleteInvoiceImagePhysicalFile(img.ImageUrl);

        TryDeleteInvoiceLogoPhysicalFile(invoice.CustomLogoUrl);

        _context.Invoices.Remove(invoice);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Invoice deleted." });
    }

    /// <summary>Upload a gallery image for an invoice (JPEG, PNG, WebP). Max 20 images per invoice.</summary>
    [HttpPost("{id:int}/images")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<IActionResult> UploadInvoiceImage(int id, IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file was uploaded." });

        const long maxBytes = 8 * 1024 * 1024;
        if (file.Length > maxBytes)
            return BadRequest(new { message = "File is too large (max 8 MB)." });

        var contentType = file.ContentType?.ToLowerInvariant() ?? string.Empty;
        var ext = contentType switch
        {
            "image/jpeg" or "image/jpg" or "image/pjpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(ext))
            return BadRequest(new { message = "Only JPEG, PNG, or WebP images are allowed." });

        var invoice = await _context.Invoices
            .Include(i => i.Images)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
            return NotFound(new { message = "Invoice not found." });

        if (invoice.Images.Count >= MaxInvoiceImages)
            return BadRequest(new { message = $"This invoice already has the maximum of {MaxInvoiceImages} images." });

        var webRoot = _environment.WebRootPath;
        if (string.IsNullOrEmpty(webRoot))
            webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");

        var relativeDir = Path.Combine("uploads", "invoice-images", id.ToString());
        var physicalDir = Path.Combine(webRoot, relativeDir);
        Directory.CreateDirectory(physicalDir);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var physicalPath = Path.Combine(physicalDir, fileName);

        await using (var stream = new FileStream(physicalPath, FileMode.CreateNew))
        {
            await file.CopyToAsync(stream);
        }

        var publicPath = $"/uploads/invoice-images/{id}/{fileName}";
        var nextOrder = invoice.Images.Count == 0
            ? 0
            : invoice.Images.Max(x => x.SortOrder) + 1;

        var row = new InvoiceImage
        {
            InvoiceId = invoice.Id,
            ImageUrl = publicPath,
            SortOrder = nextOrder,
            UploadedAt = DateTime.UtcNow
        };

        _context.InvoiceImages.Add(row);
        invoice.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var refreshed = await _context.Invoices
            .AsNoTracking()
            .Include(i => i.LineItems)
            .Include(i => i.Images)
            .Include(i => i.CreatedBy)
            .FirstAsync(i => i.Id == id);

        return Ok(MapToDto(refreshed));
    }

    [HttpDelete("{id:int}/images/{imageId:int}")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<IActionResult> DeleteInvoiceImage(int id, int imageId)
    {
        var image = await _context.InvoiceImages
            .FirstOrDefaultAsync(img => img.Id == imageId && img.InvoiceId == id);

        if (image == null)
            return NotFound(new { message = "Image not found." });

        var url = image.ImageUrl;
        _context.InvoiceImages.Remove(image);

        var invoice = await _context.Invoices.FindAsync(id);
        if (invoice != null)
            invoice.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TryDeleteInvoiceImagePhysicalFile(url);

        return Ok(new { message = "Image removed." });
    }

    /// <summary>Upload a custom header logo (JPEG, PNG, WebP, SVG). Replaces any previous custom logo.</summary>
    [HttpPost("{id:int}/logo")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<IActionResult> UploadInvoiceLogo(int id, IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file was uploaded." });

        const long maxBytes = 8 * 1024 * 1024;
        if (file.Length > maxBytes)
            return BadRequest(new { message = "File is too large (max 8 MB)." });

        var contentType = file.ContentType?.ToLowerInvariant() ?? string.Empty;
        var ext = contentType switch
        {
            "image/jpeg" or "image/jpg" or "image/pjpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/svg+xml" => ".svg",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(ext))
            return BadRequest(new { message = "Only JPEG, PNG, WebP, or SVG images are allowed." });

        var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == id);
        if (invoice == null)
            return NotFound(new { message = "Invoice not found." });

        if (!string.IsNullOrEmpty(invoice.CustomLogoUrl))
            TryDeleteInvoiceLogoPhysicalFile(invoice.CustomLogoUrl);

        var webRoot = _environment.WebRootPath;
        if (string.IsNullOrEmpty(webRoot))
            webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");

        var relativeDir = Path.Combine("uploads", "invoice-logos", id.ToString());
        var physicalDir = Path.Combine(webRoot, relativeDir);
        Directory.CreateDirectory(physicalDir);

        var fileName = $"logo{ext}";
        var physicalPath = Path.Combine(physicalDir, fileName);

        await using (var stream = new FileStream(physicalPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var publicPath = $"/uploads/invoice-logos/{id}/{fileName}";
        invoice.CustomLogoUrl = publicPath;
        invoice.HideLogo = false;
        invoice.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var refreshed = await _context.Invoices
            .AsNoTracking()
            .Include(i => i.LineItems)
            .Include(i => i.Images)
            .Include(i => i.CreatedBy)
            .FirstAsync(i => i.Id == id);

        return Ok(MapToDto(refreshed));
    }

    /// <summary>Remove custom logo file; invoice then uses default logo unless <see cref="Invoice.HideLogo"/> is set.</summary>
    [HttpDelete("{id:int}/logo")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<IActionResult> DeleteInvoiceLogo(int id)
    {
        var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == id);
        if (invoice == null)
            return NotFound(new { message = "Invoice not found." });

        if (!string.IsNullOrEmpty(invoice.CustomLogoUrl))
        {
            TryDeleteInvoiceLogoPhysicalFile(invoice.CustomLogoUrl);
            invoice.CustomLogoUrl = null;
            invoice.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        var refreshed = await _context.Invoices
            .AsNoTracking()
            .Include(i => i.LineItems)
            .Include(i => i.Images)
            .Include(i => i.CreatedBy)
            .FirstAsync(i => i.Id == id);

        return Ok(MapToDto(refreshed));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static decimal CalculateLineItemTotal(decimal unitPrice, decimal qty, decimal discount, bool isPercent)
    {
        var gross = unitPrice * qty;
        var discountAmount = isPercent ? gross * (discount / 100m) : discount;
        return Math.Max(0, gross - discountAmount);
    }

    private static void RecalculateTotals(Invoice invoice)
    {
        var taxableSubtotal = invoice.LineItems
            .Where(li => li.IsTaxable)
            .Sum(li => li.Total);

        var nonTaxableSubtotal = invoice.LineItems
            .Where(li => !li.IsTaxable)
            .Sum(li => li.Total);

        invoice.SubTotal = taxableSubtotal + nonTaxableSubtotal;
        invoice.TaxAmount = Math.Round(taxableSubtotal * (invoice.TaxRate / 100m), 2);
        invoice.Total = invoice.SubTotal + invoice.TaxAmount;
    }

    private static InvoiceDto MapToDto(Invoice invoice) => new()
    {
        Id = invoice.Id,
        InvoiceNumber = invoice.InvoiceNumber,
        IssuedDate = invoice.IssuedDate,
        DueDate = invoice.DueDate,
        ClientName = invoice.ClientName,
        ClientPhone = invoice.ClientPhone,
        ClientEmail = invoice.ClientEmail,
        ClientAddress = invoice.ClientAddress,
        JobId = invoice.JobId,
        TaxRate = invoice.TaxRate,
        SubTotal = invoice.SubTotal,
        TaxAmount = invoice.TaxAmount,
        Total = invoice.Total,
        Notes = invoice.Notes,
        Status = invoice.Status,
        HideLogo = invoice.HideLogo,
        CustomLogoUrl = invoice.CustomLogoUrl,
        HideCompanyName = invoice.HideCompanyName,
        CompanyDisplayName = invoice.CompanyDisplayName,
        CreatedById = invoice.CreatedById,
        CreatedByName = invoice.CreatedBy?.FullName,
        CreatedAt = invoice.CreatedAt,
        UpdatedAt = invoice.UpdatedAt,
        LineItems = invoice.LineItems
            .OrderBy(li => li.SortOrder)
            .Select(li => new InvoiceLineItemDto
            {
                Id = li.Id,
                InvoiceId = li.InvoiceId,
                SortOrder = li.SortOrder,
                ServiceName = li.ServiceName,
                Details = li.Details,
                Category = li.Category,
                UnitType = li.UnitType,
                UnitPrice = li.UnitPrice,
                Quantity = li.Quantity,
                Discount = li.Discount,
                IsDiscountPercentage = li.IsDiscountPercentage,
                IsTaxable = li.IsTaxable,
                Total = li.Total
            })
            .ToList(),
        Images = (invoice.Images ?? Enumerable.Empty<InvoiceImage>())
            .OrderBy(img => img.SortOrder)
            .ThenBy(img => img.Id)
            .Select(img => new InvoiceImageDto
            {
                Id = img.Id,
                InvoiceId = img.InvoiceId,
                ImageUrl = img.ImageUrl,
                SortOrder = img.SortOrder,
                UploadedAt = img.UploadedAt
            })
            .ToList()
    };

    private void TryDeleteInvoiceImagePhysicalFile(string? imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl))
            return;

        if (!imageUrl.StartsWith("/uploads/invoice-images/", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Skipped deleting unexpected invoice image path: {Path}", imageUrl);
            return;
        }

        TryDeleteUnderUploadsPath(imageUrl);
    }

    private void TryDeleteInvoiceLogoPhysicalFile(string? logoUrl)
    {
        if (string.IsNullOrEmpty(logoUrl))
            return;

        if (!logoUrl.StartsWith("/uploads/invoice-logos/", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Skipped deleting unexpected invoice logo path: {Path}", logoUrl);
            return;
        }

        TryDeleteUnderUploadsPath(logoUrl);
    }

    private void TryDeleteUnderUploadsPath(string relativeUrlPath)
    {
        try
        {
            var webRoot = _environment.WebRootPath;
            if (string.IsNullOrEmpty(webRoot))
                webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");

            var relative = relativeUrlPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(webRoot, relative);
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete uploaded file: {Path}", relativeUrlPath);
        }
    }
}
