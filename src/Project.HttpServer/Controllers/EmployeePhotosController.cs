using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Core.Exceptions;
using Project.Core.Services;
using Project.Dto.Http;
using Project.HttpServer.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace Project.HttpServer.Controllers;

[ApiController]
[Route("/api/v1/employees")]
public class EmployeePhotosController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<EmployeePhotosController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly string _photoDirectory;

    public EmployeePhotosController(
        ILogger<EmployeePhotosController> logger,
        IEmployeeService employeeService,
        IWebHostEnvironment environment)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _employeeService = employeeService ?? throw new ArgumentNullException(nameof(employeeService));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        
        // Создаем директорию для фотографий если её нет
        _photoDirectory = Path.Combine(_environment.ContentRootPath, "uploads", "photos");
        if (!Directory.Exists(_photoDirectory))
        {
            Directory.CreateDirectory(_photoDirectory);
        }
    }

    /// <summary>
    /// Загрузить фотографию сотрудника
    /// </summary>
    /// <param name="employeeId">ID сотрудника</param>
    /// <param name="photo">Файл фотографии</param>
    /// <returns>Результат загрузки</returns>
    [Authorize]
    [HttpPost("{employeeId:guid}/photo")]
    [SwaggerOperation("uploadEmployeePhoto")]
    [Consumes("multipart/form-data")]
    [SwaggerResponse(StatusCodes.Status201Created)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status404NotFound, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, type: typeof(ErrorDto))]
    public async Task<IActionResult> UploadPhoto(
        [FromRoute] [Required] Guid employeeId,
        [FromForm] PhotoUploadModel model)
    {
        try
        {
            // Проверяем, что сотрудник существует
            var employee = await _employeeService.GetEmployeeByIdAsync(employeeId);

            // Валидация файла
            if (model?.Photo == null || model.Photo.Length == 0)
            {
                return BadRequest(new ErrorDto("ValidationError", "Файл фотографии не предоставлен"));
            }

            // Проверяем тип файла
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
            if (!allowedTypes.Contains(model.Photo.ContentType.ToLower()))
            {
                return BadRequest(new ErrorDto("ValidationError", "Неподдерживаемый тип файла. Разрешены: JPEG, PNG, GIF"));
            }

            // Проверяем размер файла (максимум 5MB)
            if (model.Photo.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new ErrorDto("ValidationError", "Размер файла не должен превышать 5MB"));
            }

            // Генерируем уникальное имя файла
            var fileExtension = Path.GetExtension(model.Photo.FileName);
            var fileName = $"{employeeId}_{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(_photoDirectory, fileName);

            // Удаляем старую фотографию если есть
            if (!string.IsNullOrEmpty(employee.Photo))
            {
                var oldPhotoPath = Path.Combine(_photoDirectory, Path.GetFileName(employee.Photo));
                if (System.IO.File.Exists(oldPhotoPath))
                {
                    System.IO.File.Delete(oldPhotoPath);
                }
            }

            // Сохраняем новый файл
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.Photo.CopyToAsync(stream);
            }

            // Обновляем путь к фотографии в базе данных
            var relativePath = $"uploads/photos/{fileName}";
            await _employeeService.UpdateEmployeeAsync(employeeId, null, null, null, (DateOnly?)null, relativePath, null);

            _logger.LogInformation("Фотография успешно загружена для сотрудника {EmployeeId}: {FilePath}", employeeId, relativePath);

            return StatusCode(StatusCodes.Status201Created);
        }
        catch (EmployeeNotFoundException e)
        {
            _logger.LogWarning(e, e.Message);
            return StatusCode(StatusCodes.Status404NotFound, new ErrorDto(e.GetType().Name, e.Message));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Ошибка при загрузке фотографии для сотрудника {EmployeeId}", employeeId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorDto(e.GetType().Name, e.Message));
        }
    }

    /// <summary>
    /// Обновить фотографию сотрудника
    /// </summary>
    /// <param name="employeeId">ID сотрудника</param>
    /// <param name="model">Модель с новым файлом фотографии</param>
    /// <returns>Результат обновления</returns>
    [Authorize]
    [HttpPut("{employeeId:guid}/photo")]
    [SwaggerOperation("updateEmployeePhoto")]
    [Consumes("multipart/form-data")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status404NotFound, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, type: typeof(ErrorDto))]
    public async Task<IActionResult> UpdatePhoto(
        [FromRoute] [Required] Guid employeeId,
        [FromForm] PhotoUploadModel model)
    {
        // Обновление фотографии аналогично загрузке
        return await UploadPhoto(employeeId, model);
    }

    /// <summary>
    /// Получить фотографию сотрудника
    /// </summary>
    /// <param name="employeeId">ID сотрудника</param>
    /// <returns>Файл фотографии</returns>
    [Authorize]
    [HttpGet("{employeeId:guid}/photo")]
    [SwaggerOperation("getEmployeePhoto")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status404NotFound, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, type: typeof(ErrorDto))]
    public async Task<IActionResult> GetPhoto([FromRoute] [Required] Guid employeeId)
    {
        try
        {
            // Получаем информацию о сотруднике
            var employee = await _employeeService.GetEmployeeByIdAsync(employeeId);

            if (string.IsNullOrEmpty(employee.Photo))
            {
                return NotFound(new ErrorDto("PhotoNotFound", "У сотрудника нет фотографии"));
            }

            // Получаем полный путь к файлу
            var fileName = Path.GetFileName(employee.Photo);
            var filePath = Path.Combine(_photoDirectory, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning("Файл фотографии не найден: {FilePath} для сотрудника {EmployeeId}", filePath, employeeId);
                return NotFound(new ErrorDto("PhotoFileNotFound", "Файл фотографии не найден"));
            }

            // Определяем MIME-тип по расширению файла
            var extension = Path.GetExtension(fileName).ToLower();
            var contentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };

            // Возвращаем файл
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, contentType, fileName);
        }
        catch (EmployeeNotFoundException e)
        {
            _logger.LogWarning(e, e.Message);
            return StatusCode(StatusCodes.Status404NotFound, new ErrorDto(e.GetType().Name, e.Message));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Ошибка при получении фотографии для сотрудника {EmployeeId}", employeeId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorDto(e.GetType().Name, e.Message));
        }
    }

    /// <summary>
    /// Удалить фотографию сотрудника
    /// </summary>
    /// <param name="employeeId">ID сотрудника</param>
    /// <returns>Результат удаления</returns>
    [Authorize]
    [HttpDelete("{employeeId:guid}/photo")]
    [SwaggerOperation("deleteEmployeePhoto")]
    [SwaggerResponse(StatusCodes.Status204NoContent, type: typeof(bool))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status404NotFound, type: typeof(ErrorDto))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, type: typeof(ErrorDto))]
    public async Task<IActionResult> DeletePhoto([FromRoute] [Required] Guid employeeId)
    {
        try
        {
            var employee = await _employeeService.GetEmployeeByIdAsync(employeeId);

            if (string.IsNullOrEmpty(employee.Photo))
            {
                return NotFound(new ErrorDto("PhotoNotFound", "У сотрудника нет фотографии"));
            }

            var fileName = Path.GetFileName(employee.Photo);
            var filePath = Path.Combine(_photoDirectory, fileName);
            
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
                _logger.LogInformation("Файл фотографии удален: {FilePath}", filePath);
            }

            await _employeeService.UpdateEmployeeAsync(employeeId, null, null, null, (DateOnly?)null, null, null);

            _logger.LogInformation("Фотография успешно удалена для сотрудника {EmployeeId}", employeeId);

            return StatusCode(StatusCodes.Status204NoContent);
        }
        catch (EmployeeNotFoundException e)
        {
            _logger.LogWarning(e, e.Message);
            return StatusCode(StatusCodes.Status404NotFound, new ErrorDto(e.GetType().Name, e.Message));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Ошибка при удалении фотографии для сотрудника {EmployeeId}", employeeId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorDto(e.GetType().Name, e.Message));
        }
    }
}