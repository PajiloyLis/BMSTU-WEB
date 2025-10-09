using System.Diagnostics.CodeAnalysis;
using Project.Core.Models.PostHistory;

namespace Database.Models.Converters;

public static class PostHistoryConverter
{
    [return: NotNullIfNotNull(nameof(postHistory))]
    public static PostHistoryDb? Convert(CreatePostHistory? postHistory)
    {
        if (postHistory == null)
            return null;

        return new PostHistoryDb(
            postHistory.PostId,
            postHistory.EmployeeId,
            postHistory.StartDate,
            postHistory.EndDate);
    }

    [return: NotNullIfNotNull(nameof(postHistory))]
    public static PostHistoryDb? Convert(BasePostHistory? postHistory)
    {
        if (postHistory == null)
            return null;

        return new PostHistoryDb(
            postHistory.PostId,
            postHistory.EmployeeId,
            postHistory.StartDate,
            postHistory.EndDate);
    }

    [return: NotNullIfNotNull(nameof(postHistory))]
    public static BasePostHistory? Convert(PostHistoryDb? postHistory)
    {
        if (postHistory == null)
            return null;

        return new BasePostHistory(
            postHistory.PostId,
            postHistory.EmployeeId,
            postHistory.StartDate,
            postHistory.EndDate);
    }

    [return: NotNullIfNotNull(nameof(postHistory))]
    public static PostHistoryMongoDb? ConvertMongo(CreatePostHistory? postHistory)
    {
        if (postHistory == null)
            return null;

        return new PostHistoryMongoDb(
            postHistory.PostId,
            postHistory.EmployeeId,
            postHistory.StartDate.ToDateTime(TimeOnly.MinValue),
            postHistory.EndDate?.ToDateTime(TimeOnly.MinValue));
    }

    [return: NotNullIfNotNull(nameof(postHistory))]
    public static PostHistoryMongoDb? ConvertMongo(BasePostHistory? postHistory)
    {
        if (postHistory == null)
            return null;

        return new PostHistoryMongoDb(
            postHistory.PostId,
            postHistory.EmployeeId,
            postHistory.StartDate.ToDateTime(TimeOnly.MinValue),
            postHistory.EndDate?.ToDateTime(TimeOnly.MinValue));
    }

    [return: NotNullIfNotNull(nameof(postHistory))]
    public static BasePostHistory? ConvertMongo(PostHistoryMongoDb? postHistory)
    {
        if (postHistory == null)
            return null;

        return new BasePostHistory(
            postHistory.PostId,
            postHistory.EmployeeId,
            DateOnly.FromDateTime(postHistory.StartDate),
            postHistory.EndDate.HasValue ? DateOnly.FromDateTime(postHistory.EndDate.Value) : null);
    }
}