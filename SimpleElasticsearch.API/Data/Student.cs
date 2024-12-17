namespace SimpleElasticsearch.API.Data;

public sealed class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}