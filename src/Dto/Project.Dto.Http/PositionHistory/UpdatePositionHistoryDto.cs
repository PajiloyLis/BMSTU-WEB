namespace Project.Dto.Http.PositionHistory;

public class UpdatePositionHistoryDto
{
    public UpdatePositionHistoryDto(
        DateOnly? startDate = null,
        DateOnly? endDate = null)
    {
        StartDate = startDate;
        EndDate = endDate;
    }
    
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}