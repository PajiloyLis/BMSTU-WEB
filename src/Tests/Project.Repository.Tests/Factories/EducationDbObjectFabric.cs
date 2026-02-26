using Project.Core.Models.Education;

namespace Project.Repository.Tests.Factories;

/// <summary>
/// Object Fabric для создания тестовых объектов Education.
/// </summary>
public static class EducationDbObjectFabric
{
    private static int _counter;

    /// <summary>
    /// Создаёт валидный CreateEducation для вызова репозитория.
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
    /// Создаёт CreateEducation без даты окончания (текущее обучение).
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
    /// Создаёт валидный UpdateEducation.
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

