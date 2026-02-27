using System.Threading;
using Project.Dto.Http.Company;

namespace Project.Integration.Tests.Factories;

public static class CompanyObjectFabric
{
    private static int _counter;

    public static CreateCompanyDto CreateCompanyDto(string? title = null, string? email = null)
    {
        var idx = Interlocked.Increment(ref _counter);
        return new CreateCompanyDto(
            title ?? $"Integration Company {idx}",
            new DateOnly(2010, 1, 1),
            $"+7499{idx:D7}",
            email ?? $"company{idx}@test.local",
            $"{9000000000 + idx:D10}",
            $"{900000000 + idx:D9}",
            $"{9000000000000 + idx:D13}",
            $"Moscow, Test street, {idx}");
    }

    public static UpdateCompanyDto UpdateCompanyDto(
        string title = "Updated Integration Company",
        string email = "updated.company@test.local")
    {
        return new UpdateCompanyDto(
            title,
            new DateOnly(2011, 2, 2),
            "+74990000011",
            email,
            "1234567890",
            "123456789",
            "1234567890123",
            "Moscow, Updated street, 1");
    }
}

