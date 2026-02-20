namespace Project.Dto.Http.PositionHistory;

public class CurrentPositionEmployeeDto
{
    public CurrentPositionEmployeeDto(
        Guid positionId,
        Guid employeeId)
    {
        PositionId = positionId;
        EmployeeId = employeeId;
    }

    public Guid PositionId { get; set; }
    public Guid EmployeeId { get; set; }
}