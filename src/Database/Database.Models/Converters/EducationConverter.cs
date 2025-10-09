using System.Diagnostics.CodeAnalysis;
using Project.Core.Models;
using Project.Core.Models.Education;

namespace Database.Models.Converters;

public static class EducationConverter
{
    [return: NotNullIfNotNull(nameof(education))]
    public static EducationDb? Convert(CreateEducation? education)
    {
        if (education == null)
            return null;

        return new EducationDb(Guid.NewGuid(),
            education.EmployeeId,
            education.Institution,
            (education.Level as EducationLevel?).ToStringVal(),
            education.StudyField,
            education.StartDate,
            education.EndDate
        );
    }

    [return: NotNullIfNotNull(nameof(education))]
    public static EducationDb? Convert(BaseEducation? education)
    {
        if (education == null)
            return null;

        return new EducationDb(education.Id,
            education.EmployeeId,
            education.Institution,
            (education.Level as EducationLevel?).ToStringVal(),
            education.StudyField,
            education.StartDate,
            education.EndDate);
    }

    [return: NotNullIfNotNull(nameof(education))]
    public static BaseEducation? Convert(EducationDb? education)
    {
        if (education == null)
            return null;

        return new BaseEducation(education.Id,
            education.EmployeeId,
            education.Institution,
            education.Level,
            education.StudyField,
            education.StartDate,
            education.EndDate);
    }
    
    public static EducationMongoDb ConvertMongo(CreateEducation education)
    {
        return new EducationMongoDb
        {
            Id = Guid.NewGuid(),
            EmployeeId = education.EmployeeId,
            Institution = education.Institution,
            StudyField = education.StudyField,
            Level = (education.Level as EducationLevel?).ToStringVal(),
            StartDate = education.StartDate.ToDateTime(TimeOnly.MinValue),
            EndDate = education.EndDate.Value.ToDateTime(TimeOnly.MinValue)
        };
    }

    public static BaseEducation ConvertMongo(EducationMongoDb education)
    {
        return new BaseEducation
        (
            education.Id,
            education.EmployeeId,
            education.Institution,
            education.StudyField,
            education.Level,
            DateOnly.FromDateTime(education.StartDate),
            DateOnly.FromDateTime(education.EndDate.Value)
        );
    }
}