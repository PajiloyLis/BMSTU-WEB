namespace Project.Dto.Http.External;

public sealed class AgePredictionDto
{
    public AgePredictionDto(string name, int? age, int count, string source)
    {
        Name = name;
        Age = age;
        Count = count;
        Source = source;
    }

    public string Name { get; set; }

    public int? Age { get; set; }

    public int Count { get; set; }

    public string Source { get; set; }
}
