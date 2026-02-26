using Project.Core.Models.Company;
using Project.Dto.Http.Company;

namespace Project.Controller.Tests.Factories;

public static class CompanyObjectFabric
{
    public static CreateCompanyDto CreateCompanyDto(string title = "Test Company")
    {
        return new CreateCompanyDto(
            title,
            new DateOnly(2020, 1, 1),
            "+71234567890",
            "company@test.com",
            "1234567890",
            "123456789",
            "1234567890123",
            "Moscow");
    }

    public static UpdateCompanyDto UpdateCompanyDto(string title = "Updated Company")
    {
        return new UpdateCompanyDto(
            title,
            new DateOnly(2020, 1, 1),
            "+71234567890",
            "updated@test.com",
            "1234567890",
            "123456789",
            "1234567890123",
            "Saint Petersburg");
    }

    public static BaseCompany BaseCompany(Guid? companyId = null, string title = "Test Company")
    {
        return new BaseCompany(
            companyId ?? Guid.NewGuid(),
            title,
            new DateOnly(2020, 1, 1),
            "+71234567890",
            "company@test.com",
            "1234567890",
            "123456789",
            "1234567890123",
            "Moscow",
            false);
    }
}

