using System.Text.Json;
using Microsoft.Extensions.Logging;
using Project.Core.Models;
using Project.Core.Models.Education;
using Project.Core.Repositories;
using Project.Core.Services;

namespace Project.Services.EducationService;

public class EducationService : IEducationService
{
    private readonly IEducationRepository _educationRepository;
    private readonly ILogger<EducationService> _logger;

    public EducationService(IEducationRepository educationRepository, ILogger<EducationService> logger)
    {
        _educationRepository = educationRepository ?? throw new ArgumentNullException(nameof(educationRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BaseEducation> AddEducationAsync(Guid employeeId, string institution, string level,
        string studyField,
        DateOnly startDate, DateOnly? endDate = null)
    {
        try
        {
            var createdEducation = await _educationRepository.AddEducationAsync(
                new CreateEducation(employeeId, institution, level, studyField, startDate, endDate));

            return createdEducation;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error creating education for employee {employeeId}");
            throw;
        }
    }

    public async Task<BaseEducation> GetEducationByIdAsync(Guid educationId)
    {
        try
        {
            var education = await _educationRepository.GetEducationByIdAsync(educationId);

            return education;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error getting education with id {educationId}");
            throw;
        }
    }

    public async Task<BaseEducation> UpdateEducationAsync(Guid educationId, Guid employeeId, string? institution = null,
        string? level = null, string? studyField = null, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        try
        {
            var education = await _educationRepository.UpdateEducationAsync(
                new UpdateEducation(educationId, employeeId, institution, level, studyField, startDate, endDate));

            return education;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error updating education with id {educationId}");
            throw;
        }
    }

    public async Task<IEnumerable<BaseEducation>> GetEducationsByEmployeeIdAsync(Guid employeeId, int pageNumber,
        int pageSize)
    {
        try
        {
            var educations = await _educationRepository.GetEducationsAsync(employeeId, pageNumber, pageSize);
            return educations;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error getting educations for employee {employeeId}");
            throw;
        }
    }

    public async Task DeleteEducationAsync(Guid educationId)
    {
        try
        {
            await _educationRepository.DeleteEducationAsync(educationId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error deleting education with id {educationId}");
            throw;
        }
    }
}