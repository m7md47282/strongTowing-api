namespace StrongTowing.API;

/// <summary>Default per-account cash call line items and rates when a new insurance account is created.</summary>
public static class CashCallDefaultServiceTemplate
{
    public readonly record struct Line(string Name, decimal BasePrice, decimal PricePerMile);

    /// <summary>Ordered list: mileage rows use <see cref="Line.PricePerMile"/>; flat-fee rows use <see cref="Line.BasePrice"/>.</summary>
    public static IReadOnlyList<Line> All { get; } = new Line[]
    {
        new("Deadhead Mileage", 0m, 0.50m),
        new("Loaded/Hooked Mileage", 0m, 5.00m),
        new("Unloaded/Enroute Mileage", 0m, 1.50m),
        new("Admin Fee", 63.00m, 0m),
        new("Battery Sales (Per Minute)", 6.00m, 0m),
        new("Dollies", 50.00m, 0m),
        new("FB - FLATBED", 50.00m, 0m),
        new("FD - Fuel Delivery Amount", 55.00m, 0m),
        new("GOA", 50.00m, 0m),
        new("Jump Start Service", 55.00m, 0m),
        new("Lockout Service", 100.00m, 0m),
        new("LT - LONG TOW", 66.00m, 0m),
        new("Tire Service", 110.00m, 0m),
        new("TL - TOLLS/PARKING FEE", 22.00m, 0m),
        new("Tow/Hook Fee", 150.00m, 0m),
        new("Wait Time", 65.00m, 0m),
        new("Winching", 125.00m, 0m)
    };
}
