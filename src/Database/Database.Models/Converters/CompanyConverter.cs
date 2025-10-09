using System.Diagnostics.CodeAnalysis;
using Project.Core.Models;
using Project.Core.Models.Company;

namespace Database.Models.Converters;

public static class CompanyConverter
{
    [return: NotNullIfNotNull(nameof(company))]
    public static CompanyDb? Convert(CreationCompany? company)
    {
        if (company == null)
            return null;

        return new CompanyDb(Guid.NewGuid(),
            company.Title,
            company.RegistrationDate,
            company.PhoneNumber,
            company.Email,
            company.Inn,
            company.Kpp,
            company.Ogrn,
            company.Address
        );
    }

    [return: NotNullIfNotNull(nameof(company))]
    public static CompanyDb? Convert(BaseCompany? company)
    {
        if (company == null)
            return null;

        return new CompanyDb(company.CompanyId,
            company.Title,
            company.RegistrationDate,
            company.PhoneNumber,
            company.Email,
            company.Inn,
            company.Kpp,
            company.Ogrn,
            company.Address);
    }

    [return: NotNullIfNotNull(nameof(company))]
    public static BaseCompany? Convert(CompanyDb? company)
    {
        if (company == null)
            return null;

        return new BaseCompany(company.Id,
            company.Title,
            company.RegistrationDate,
            company.PhoneNumber,
            company.Email,
            company.Inn,
            company.Kpp,
            company.Ogrn,
            company.Address);
    }
    
    [return: NotNullIfNotNull(nameof(company))]
    public static CompanyMongoDb? ConvertMongo(CreationCompany? company)
    {
        if (company == null)
            return null;

        return new CompanyMongoDb(Guid.NewGuid(),
            company.Title,
            company.RegistrationDate.ToDateTime(TimeOnly.MinValue),
            company.PhoneNumber,
            company.Email,
            company.Inn,
            company.Kpp,
            company.Ogrn,
            company.Address
        );
    }

    [return: NotNullIfNotNull(nameof(company))]
    public static CompanyMongoDb? ConvertMongo(BaseCompany? company)
    {
        if (company == null)
            return null;

        return new CompanyMongoDb(company.CompanyId,
            company.Title,
            company.RegistrationDate.ToDateTime(TimeOnly.MinValue),
            company.PhoneNumber,
            company.Email,
            company.Inn,
            company.Kpp,
            company.Ogrn,
            company.Address);
    }

    [return: NotNullIfNotNull(nameof(company))]
    public static BaseCompany? ConvertMongo(CompanyMongoDb? company)
    {
        if (company == null)
            return null;

        return new BaseCompany(company.Id,
            company.Title,
            DateOnly.FromDateTime(company.RegistrationDate),
            company.PhoneNumber,
            company.Email,
            company.Inn,
            company.Kpp,
            company.Ogrn,
            company.Address);
    }
}