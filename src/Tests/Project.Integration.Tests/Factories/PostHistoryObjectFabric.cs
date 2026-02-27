using Project.Dto.Http.PostHistory;

namespace Project.Integration.Tests.Factories;

public static class PostHistoryObjectFabric
{
    public static CreatePostHistoryDto CreatePostHistoryDto(
        Guid postId,
        Guid employeeId,
        DateOnly? startDate = null,
        DateOnly? endDate = null)
    {
        return new CreatePostHistoryDto(
            postId,
            employeeId,
            startDate ?? new DateOnly(2019, 1, 1),
            endDate ?? new DateOnly(2020, 1, 1));
    }

    public static UpdatePostHistoryDto UpdatePostHistoryDto(
        DateOnly? startDate = null,
        DateOnly? endDate = null)
    {
        return new UpdatePostHistoryDto(
            startDate ?? new DateOnly(2018, 1, 1),
            endDate ?? new DateOnly(2019, 1, 1));
    }
}

