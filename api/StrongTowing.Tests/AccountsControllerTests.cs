using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StrongTowing.API;
using StrongTowing.API.Controllers;
using StrongTowing.Application.DTOs.Requests;
using StrongTowing.Core.Entities;
using StrongTowing.Infrastructure.Data;

namespace StrongTowing.Tests;

public class AccountsControllerTests
{
    [Fact]
    public async Task Create_SeedsSeventeenCashCallTemplateRows_AndCreatesMissingProfiles()
    {
        await using var context = CreateContext(nameof(Create_SeedsSeventeenCashCallTemplateRows_AndCreatesMissingProfiles));
        // Empty catalog: all 17 services are created, then 17 account rates.
        var controller = new AccountsController(context, NullLogger<AccountsController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        var request = new CreateInsuranceAccountRequest
        {
            Name = "Trouble A",
            IsActive = true,
            RateAB = 0m,
            RateBC = 0m,
            RateCA = 0m
        };

        var result = await controller.Create(request);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.NotNull(created.Value);

        var account = await context.InsuranceAccounts.SingleAsync(a => a.Name == "Trouble A");
        var expectedCount = CashCallDefaultServiceTemplate.All.Count;
        var seededRows = await context.InsuranceAccountServiceRates
            .Where(x => x.InsuranceAccountId == account.Id)
            .ToListAsync();

        Assert.Equal(expectedCount, seededRows.Count);
        Assert.Equal(expectedCount, await context.ServicePricingProfiles.CountAsync());

        var deadhead = CashCallDefaultServiceTemplate.All.First(l => l.Name == "Deadhead Mileage");
        var deadheadRow = await context.InsuranceAccountServiceRates
            .Include(x => x.ServicePricingProfile)
            .SingleAsync(x => x.InsuranceAccountId == account.Id && x.ServicePricingProfile!.Name == "Deadhead Mileage");
        Assert.Equal(0m, deadheadRow.BasePrice);
        Assert.Equal(0.50m, deadheadRow.PricePerMile);

        var admin = CashCallDefaultServiceTemplate.All.First(l => l.Name == "Admin Fee");
        var adminRow = await context.InsuranceAccountServiceRates
            .SingleAsync(x => x.InsuranceAccountId == account.Id && x.BasePrice == 63m);
        Assert.Equal(63.00m, adminRow.BasePrice);
        Assert.Equal(0m, adminRow.PricePerMile);
    }

    [Fact]
    public async Task Create_ReusesExistingProfiles_ByName()
    {
        await using var context = CreateContext(nameof(Create_ReusesExistingProfiles_ByName));
        var existing = new ServicePricingProfile
        {
            Name = "Admin Fee",
            BasePrice = 0m,
            PricePerMile = 0m,
            IsAvailable = true
        };
        context.ServicePricingProfiles.Add(existing);
        await context.SaveChangesAsync();

        var controller = new AccountsController(context, NullLogger<AccountsController>.Instance);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        var request = new CreateInsuranceAccountRequest
        {
            Name = "Second Account",
            IsActive = true,
            RateAB = 0m,
            RateBC = 0m,
            RateCA = 0m
        };

        var result = await controller.Create(request);
        Assert.IsType<CreatedAtActionResult>(result.Result);

        // Still exactly one Admin Fee profile; new account's rate uses template 63/0, not 0/0 in DB
        var profiles = await context.ServicePricingProfiles.CountAsync();
        var expected = CashCallDefaultServiceTemplate.All.Count; // 16 new + 1 existing = 17 total
        Assert.Equal(expected, profiles);

        var acc = await context.InsuranceAccounts.SingleAsync(a => a.Name == "Second Account");
        var adminRate = await context.InsuranceAccountServiceRates
            .SingleAsync(x => x.InsuranceAccountId == acc.Id && x.ServicePricingProfileId == existing.Id);
        Assert.Equal(63.00m, adminRate.BasePrice);
        Assert.Equal(0m, adminRate.PricePerMile);
    }

    private static ApplicationDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new ApplicationDbContext(options);
    }
}
