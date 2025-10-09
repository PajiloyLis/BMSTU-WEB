using System.Data;
using System.Data.Common;
using Dapper;
using Database.Context;
using Database.Models;
using Database.Models.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Project.Core.Exceptions;
using Project.Core.Models;
using Project.Core.Models.Company;
using Project.Core.Repositories;

namespace Database.Repositories;

public class CompanyRepository : ICompanyRepository
{
    private readonly ProjectDbContext _context;
    private readonly ILogger<CompanyRepository> _logger;

    public CompanyRepository(ProjectDbContext context, ILogger<CompanyRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BaseCompany> AddCompanyAsync(CreationCompany newCompany)
    {
        var company = CompanyConverter.Convert(newCompany);
        try
        {
            var foundCompany =
                await _context.CompanyDb
                    .Where(e => e.Id == company.Id || e.Title == company.Title ||
                                e.PhoneNumber == company.PhoneNumber ||
                                e.Email == company.Email || e.Inn == company.Inn ||
                                e.Kpp == company.Kpp || e.Ogrn == company.Ogrn).FirstOrDefaultAsync();
            if (foundCompany is not null)
                throw new CompanyAlreadyExistsException(
                    $"Company with same title - {company.Title} or phone - {company.PhoneNumber} or email - {company.Email} or inn - {company.Inn} or kpp - {company.Kpp} or ogrn - {company.Ogrn} or id - {company.Id} already exists");

            await _context.CompanyDb.AddAsync(company);
            await _context.SaveChangesAsync();

            return CompanyConverter.Convert(company);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error creating company with id - {company.Id}");
            throw;
        }
    }

    public async Task<BaseCompany> UpdateCompanyAsync(UpdateCompany company)
    {
        try
        {
            var existingCompanyCount = await _context.CompanyDb.Where(e =>
                e.Id != company.CompanyId && (e.Title == company.Title || e.PhoneNumber == company.PhoneNumber ||
                                              e.Email == company.Email || e.Inn == company.Inn ||
                                              e.Kpp == company.Kpp || e.Ogrn == company.Ogrn)).CountAsync();

            if (existingCompanyCount > 0)
                throw new CompanyAlreadyExistsException(
                    $"Company with another id, but same title - {company.Title} or phone - {company.PhoneNumber} or email - {company.Email} or inn - {company.Inn} or kpp - {company.Kpp} or ogrn - {company.Ogrn} already exists");

            var companyToUpdate = await _context.CompanyDb.FirstOrDefaultAsync(e => e.Id == company.CompanyId);
            if (companyToUpdate is null)
                throw new CompanyNotFoundException($"Company with id {company.CompanyId} not found");

            companyToUpdate.Title = company.Title ?? companyToUpdate.Title;
            companyToUpdate.RegistrationDate = company.RegistrationDate ?? companyToUpdate.RegistrationDate;
            companyToUpdate.PhoneNumber = company.PhoneNumber ?? companyToUpdate.PhoneNumber;
            companyToUpdate.Email = company.Email ?? companyToUpdate.Email;
            companyToUpdate.Inn = company.Inn ?? companyToUpdate.Inn;
            companyToUpdate.Kpp = company.Kpp ?? companyToUpdate.Kpp;
            companyToUpdate.Ogrn = company.Ogrn ?? companyToUpdate.Ogrn;
            companyToUpdate.Address = company.Address ?? companyToUpdate.Address;

            await _context.SaveChangesAsync();

            return CompanyConverter.Convert(companyToUpdate);
        }
        catch (CompanyNotFoundException e)
        {
            _logger.LogWarning(e, $"Company with id {company.CompanyId} not found");
            throw;
        }
        catch (CompanyAlreadyExistsException e)
        {
            _logger.LogWarning(e, "Company with another id, but same unique fields already exists");
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error updating company with id - {company.CompanyId}");
            throw;
        }
    }

    public async Task<BaseCompany> GetCompanyByIdAsync(Guid companyId)
    {
        try
        {
            var company = await _context.CompanyDb.FirstOrDefaultAsync(e => e.Id == companyId);
            if (company is null)
                throw new CompanyNotFoundException($"Company with id {companyId} not found");

            return CompanyConverter.Convert(company);
        }
        catch (CompanyNotFoundException e)
        {
            _logger.LogWarning(e, $"Company with id {companyId} not found");
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error getting company with id - {companyId}");
            throw;
        }
    }

    public async Task DeleteCompanyAsync(Guid companyId)
    {
        try
        {
            var company = await _context.CompanyDb.FirstOrDefaultAsync(e => e.Id == companyId);
            if (company is null)
                throw new CompanyNotFoundException($"Company with id {companyId} not found");
            company.IsDeleted = true;
            
            var posts = await _context.PostDb.Where(e => e.CompanyId == companyId).ToListAsync();
            posts.ForEach(e => e.IsDeleted=true);

            var positions = await _context.PositionDb.Where(e => e.CompanyId == companyId).ToListAsync();
            positions.ForEach(e => e.IsDeleted = true);

            var postHistories = await _context.PostHistoryDb.Where(e => posts.Select(x => x.Id).ToList().Contains(e.PostId))
                .ToListAsync();
            postHistories.ForEach(e => e.EndDate=DateOnly.FromDateTime(DateTime.Now));

            var positionHistories = await _context.PositionHistoryDb
                .Where(e => positions.Select(x => x.Id).ToList().Contains(e.PositionId)).ToListAsync();
            positionHistories.ForEach(e => e.EndDate=DateOnly.FromDateTime(DateTime.Now));
            
            await _context.SaveChangesAsync();
        }
        catch (CompanyNotFoundException e)
        {
            _logger.LogWarning(e, $"Company with id {companyId} not found for deleting");
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error deleting company with id - {companyId}");
            throw;
        }
    }

    public async Task<IEnumerable<BaseCompany>> GetCompaniesAsync()
    {
        try
        {
            var companies = await _context.CompanyDb.OrderBy(e => e.RegistrationDate).AsNoTracking().ToListAsync();
            
            return companies.Select(e => CompanyConverter.Convert(e)).ToList();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting companies");
            throw;
        }
    }
}