namespace Project.Dto.Http.PostHistory;

public class UpdatePostHistoryDto
{
    public UpdatePostHistoryDto(
        DateOnly? startDate = null,
        DateOnly? endDate = null)
    {
        StartDate = startDate;
        EndDate = endDate;
    }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}