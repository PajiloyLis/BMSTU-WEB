using Project.Core.Models.Education;
using Project.Dto.Http.Education;

namespace Project.Controller.Tests.Factories;

public static class EducationObjectFabric
{
    public static CreateEducationDto CreateEducationDto(Guid employeeId)
    {
        return new CreateEducationDto(
            employeeId,
            "BMSTU",
            "Высшее (бакалавриат)",
            "Informatics",
            new DateOnly(2015, 9, 1),
            new DateOnly(2019, 6, 30));
    }

    public static UpdateEducationDto UpdateEducationDto(Guid employeeId, string institution = "MSU")
    {
        return new UpdateEducationDto(
            employeeId,
            institution,
            "Высшее (магистратура)",
            "Mathematics",
            new DateOnly(2019, 9, 1),
            new DateOnly(2021, 6, 30));
    }

    public static BaseEducation BaseEducation(Guid educationId, Guid employeeId, string institution = "BMSTU")
    {
        return new BaseEducation(
            educationId,
            employeeId,
            institution,
            "Высшее (бакалавриат)",
            "Informatics",
            new DateOnly(2015, 9, 1),
            new DateOnly(2019, 6, 30));
    }
}

