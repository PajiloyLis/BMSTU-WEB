namespace Project.Dto.Http.Post;

public class UpdatePostDto
{
    public UpdatePostDto(string? title, decimal? salary)
    {
        Title = title;
        Salary = salary;
    }

    public string? Title { get; set; }
    public decimal? Salary { get; set; }
}