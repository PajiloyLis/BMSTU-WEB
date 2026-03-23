namespace Project.Dto.Http.External;

public class AgePredictionDto
{
    public AgePredictionDto(string name, int? age, int? count)
    {
        Name = name;
        Age = age;
        Count = count;
    }

    public string Name { get; set; }

    public int? Age { get; set; }

    public int? Count { get; set; }
}

