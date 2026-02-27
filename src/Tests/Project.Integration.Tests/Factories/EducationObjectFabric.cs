using System.Threading;
using Project.Dto.Http.Education;

namespace Project.Integration.Tests.Factories;

public static class EducationObjectFabric
{
    private static int _counter;

    public static CreateEducationDto CreateEducationDto(Guid employeeId, string? institution = null, string? level = null)
    {
        var idx = Interlocked.Increment(ref _counter);
        return new CreateEducationDto(
            employeeId,
            institution ?? $"Integration University {idx}",
            level ?? "Высшее (бакалавриат)",
            $"Integration Study Field {idx}",
            new DateOnly(2015, 9, 1),
            new DateOnly(2019, 6, 30));
    }

    public static UpdateEducationDto UpdateEducationDto(
        Guid employeeId,
        string institution = "Updated Integration University",
        string level = "Высшее (магистратура)")
    {
        return new UpdateEducationDto(
            employeeId,
            institution,
            level,
            "Updated Integration Field",
            new DateOnly(2016, 9, 1),
            new DateOnly(2020, 6, 30));
    }
}

