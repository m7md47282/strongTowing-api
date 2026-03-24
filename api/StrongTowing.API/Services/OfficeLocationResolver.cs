using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StrongTowing.API.Options;
using StrongTowing.Infrastructure.Data;

namespace StrongTowing.API.Services;

public interface IOfficeLocationResolver
{
    Task<(double Lat, double Lng)> GetOfficeCoordinatesAsync(CancellationToken cancellationToken = default);
}

public class OfficeLocationResolver : IOfficeLocationResolver
{
    private readonly ApplicationDbContext _context;
    private readonly OfficeLocationOptions _fallbackOptions;

    public OfficeLocationResolver(
        ApplicationDbContext context,
        IOptions<OfficeLocationOptions> fallbackOptions)
    {
        _context = context;
        _fallbackOptions = fallbackOptions.Value;
    }

    public async Task<(double Lat, double Lng)> GetOfficeCoordinatesAsync(CancellationToken cancellationToken = default)
    {
        var row = await _context.SystemSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (row?.OfficeLatitude.HasValue == true && row.OfficeLongitude.HasValue == true)
            return (row.OfficeLatitude.Value, row.OfficeLongitude.Value);

        return (_fallbackOptions.Lat, _fallbackOptions.Lng);
    }
}
