﻿using Database.Context;
using Database.Models;
using Database.Models.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Project.Core.Exceptions;
using Project.Core.Models;
using Project.Core.Models.Employee;
using Project.Core.Models.Position;
using Project.Core.Repositories;

namespace Database.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly ProjectDbContext _context;
    private readonly ILogger<EmployeeRepository> _logger;

    public EmployeeRepository(ProjectDbContext context, ILogger<EmployeeRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BaseEmployee> AddEmployeeAsync(CreationEmployee newEmployee)
    {
        var employee = EmployeeConverter.Convert(newEmployee);
        try
        {
            var foundEmployee = await _context.EmployeeDb
                .Where(u => u.Id == employee.Id || u.Email == employee.Email || u.Phone == employee.Phone)
                .FirstOrDefaultAsync();

            if (foundEmployee is not null)
                throw new EmployeeAlreadyExistsException(
                    $"Employee with email - {newEmployee.Email} or phone - {employee.Phone} or id - {employee.Id} already exists");

            await _context.EmployeeDb.AddAsync(employee);
            await _context.SaveChangesAsync();

            return EmployeeConverter.Convert(employee);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error creating employee with id - {employee.Id}");
            throw;
        }
    }

    public async Task DeleteEmployeeAsync(Guid employeeId)
    {
        try
        {
            var employee = await _context.EmployeeDb.FirstOrDefaultAsync(u => u.Id == employeeId);
            if (employee is null) throw new EmployeeNotFoundException($"Employee with id - {employeeId} not found");

            var educations = await _context.EducationDb.Where(e=>e.EmployeeId==employeeId).ToListAsync();
            _context.EducationDb.RemoveRange(educations);
            _context.EmployeeDb.Remove(employee);
            await _context.SaveChangesAsync();
        }
        catch (EmployeeNotFoundException e)
        {
            _logger.LogWarning(e, $"Error getting employee with id - {employeeId}");
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error deleting employee with id - {employeeId}");
            throw;
        }
    }

    public async Task<IEnumerable<BaseEmployee>> GetSubordinatesByDirectorIdAsync(Guid employeeId)
    {
        try
        {
            var currentPositionHistory = await _context.PositionHistoryDb
                .Where(e => e.EmployeeId == employeeId && e.EndDate == null).FirstOrDefaultAsync();
            if (currentPositionHistory is null)
                throw new PositionHistoryNotFoundException($"Current position for employee {employeeId} not found");
            
            
            List<Guid> positionsId = new List<Guid>();
            var head = await _context.PositionDb.Where(e => e.Id == currentPositionHistory.PositionId).Select(e => e.Id).FirstOrDefaultAsync();
            if (head == Guid.Empty)
                throw new PositionNotFoundException($"Position with id {currentPositionHistory.PositionId} not found");
            positionsId.Add(head);
            int i = 0;
            while (i != positionsId.Count)
            {
                var children = await _context.PositionDb.Where(e => e.ParentId == positionsId[i]).Select(e=>e.Id).ToListAsync();
                positionsId.AddRange(children);
                ++i;
            }

            var employeesId = await _context.PositionHistoryDb
                .Where(e => positionsId.Contains(e.PositionId) && e.EndDate == null).Select(e => e.EmployeeId)
                .ToListAsync();
            
            var employees = await _context.EmployeeDb.Where(e => employeesId.Contains(e.Id)).ToListAsync();

            return employees.Select(e => EmployeeConverter.Convert(e)).ToList();
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error getting current position for employee - {employeeId}");
            throw;
        }
    }

    public async Task<BaseEmployee> GetEmployeeByIdAsync(Guid employeeId)
    {
        try
        {
            var employee = await _context.EmployeeDb.AsNoTracking().FirstOrDefaultAsync(u => u.Id == employeeId);
            if (employee is null)
                throw new EmployeeNotFoundException($"Employee with id - {employeeId} not found");

            return EmployeeConverter.Convert(employee);
        }
        catch (EmployeeNotFoundException e)
        {
            _logger.LogWarning(e, $"Error getting employee with id - {employeeId}");
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error getting employee with id - {employeeId}");
            throw;
        }
    }

    public async Task<BaseEmployee> UpdateEmployeeAsync(UpdateEmployee updatedUpdateEmployee)
    {
        try
        {
            //Check for users with another Id and same Phone or Email
            var existingEmployeesCount = await _context.EmployeeDb.Where(u => u.Id != updatedUpdateEmployee.EmployeeId)
                .Where(u => u.Email == updatedUpdateEmployee.Email ||
                            (updatedUpdateEmployee.PhoneNumber != null &&
                             u.Phone == updatedUpdateEmployee.PhoneNumber)).AsNoTracking()
                .CountAsync();

            if (existingEmployeesCount > 0)
                throw new EmployeeAlreadyExistsException(
                    $"Employee with email - {updatedUpdateEmployee.Email} or phone - {updatedUpdateEmployee.PhoneNumber}");
            var employee = await _context.EmployeeDb.FirstOrDefaultAsync(u => u.Id == updatedUpdateEmployee.EmployeeId);
            if (employee is null)
                throw new EmployeeNotFoundException($"Employee with id - {updatedUpdateEmployee.EmployeeId} not found");

            employee.Email = updatedUpdateEmployee.Email ?? employee.Email;
            employee.Phone = updatedUpdateEmployee.PhoneNumber ?? employee.Phone;
            employee.FullName = updatedUpdateEmployee.FullName ?? employee.FullName;
            employee.BirthDate = updatedUpdateEmployee.BirthDate ?? employee.BirthDate;
            employee.Photo = updatedUpdateEmployee.Photo ?? employee.Photo;
            employee.Duties = updatedUpdateEmployee.Duties ?? employee.Duties;

            await _context.SaveChangesAsync();

            return EmployeeConverter.Convert(employee);
        }
        catch (EmployeeNotFoundException e)
        {
            _logger.LogWarning(e, $"Error getting employee with id - {updatedUpdateEmployee.EmployeeId}");
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error updating employee with id - {updatedUpdateEmployee.EmployeeId}");
            throw;
        }
    }
}