using Project.Core.Models.Education;

namespace Project.Service.Tests.Factories;

/// <summary>
/// Object Fabric для создания тестовых объектов Education.
/// </summary>
public static class EducationObjectFabric
{
    private static int _counter;

    /// <summary>
    /// Создаёт валидный объект CreateEducation с уникальными данными.
    /// </summary>
    public static CreateEducation CreateValidCreateEducation(Guid employeeId)
    {
        var idx = Interlocked.Increment(ref _counter);
        return new CreateEducation(
            employeeId,
            $"МГУ Институт {idx}",
            "Высшее (бакалавриат)",
            $"Информатика {idx}",
            new DateOnly(2018, 9, 1),
            new DateOnly(2022, 6, 30)
        );
    }

    /// <summary>
    /// Создаёт валидный объект CreateEducation без даты окончания.
    /// </summary>
    public static CreateEducation CreateOngoingEducation(Guid employeeId)
    {
        var idx = Interlocked.Increment(ref _counter);
        return new CreateEducation(
            employeeId,
            $"МГТУ им. Баумана {idx}",
            "Высшее (магистратура)",
            $"Программная инженерия {idx}",
            new DateOnly(2022, 9, 1)
        );
    }

    /// <summary>
    /// Создаёт валидный объект UpdateEducation.
    /// </summary>
    public static UpdateEducation CreateValidUpdateEducation(Guid educationId, Guid employeeId)
    {
        return new UpdateEducation(
            educationId,
            employeeId,
            "СПбГУ",
            "Высшее (магистратура)",
            "Программная инженерия",
            new DateOnly(2020, 9, 1),
            new DateOnly(2022, 6, 30)
        );
    }
}

