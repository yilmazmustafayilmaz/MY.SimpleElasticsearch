using Elasticsearch.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using SimpleElasticsearch.API.Data;
using System.Text;

namespace SimpleElasticsearch.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ValuesController : ControllerBase
{
    AppDbContext _context = new();

    /// <summary>
    /// Insert 10000 random data to the database
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("[action]")]
    public async Task<IActionResult> InsertData(CancellationToken cancellationToken)
    {
        Random random = new();
        List<Student> students = new();

        for (int i = 0; i < 10000; i++)
        {
            string name = new(Enumerable.Repeat("abcdefgğhıjklmnoöprsştuwyc", 5).Select(x => x[random.Next(x.Length)]).ToArray());
            string surname = new(Enumerable.Repeat("abcdefgğhıjklmnoöprsştuwyc", 5).Select(x => x[random.Next(x.Length)]).ToArray());

            StringBuilder departmentBuilder = new();
            for (int j = 0; j < 500; j++)
            {
                departmentBuilder.Append(new string(Enumerable.Repeat("abcdefgğhıjklmnoöprsştuwyc", 5).Select(x => x[random.Next(x.Length)]).ToArray()));
                if (j < 499)
                    departmentBuilder.Append(" ");
            }
            string department = departmentBuilder.ToString();

            Student student = new()
            {
                Name = name,
                Surname = surname,
                Department = department
            };
            students.Add(student);
        }

        await _context.Set<Student>().AddRangeAsync(students, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok("Data inserted successfully");
    }

    /// <summary>
    /// Get all data from the database
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    [HttpGet("[action]")]
    public async Task<IActionResult> GetAll(string filter)
    {
        IList<Student> students = await _context.Set<Student>()
            .Where(x => x.Department.Contains(filter)).AsNoTracking().ToListAsync();

        return Ok(students.Take(10));
    }

    /// <summary>
    /// Sync data to Elasticsearch
    /// </summary>
    /// <returns></returns>
    [HttpGet("[action]")]
    public async Task<IActionResult> SyncToElastic()
    {
        ConnectionConfiguration settings = new(new Uri("http://localhost:9200"));
        ElasticLowLevelClient client = new(settings);
        var students = await _context.Set<Student>().AsNoTracking().ToListAsync();

        List<Task> task = new();

        foreach (var student in students)
        {
            task.Add(client.IndexAsync<StringResponse>("students", student.Id.ToString(),
            PostData.Serializable(new
            {
                student.Id,
                student.Name,
                student.Surname,
                student.Department
            })));
        }
        await Task.WhenAll(task);
        return Ok();
    }

    /// <summary>
    /// Get all data from Elasticsearch
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [HttpGet("[action]/{value}")]
    public async Task<IActionResult> GetAllWithElasticsearch(string value)
    {
        ConnectionConfiguration settings = new(new Uri("http://localhost:9200"));
        ElasticLowLevelClient client = new(settings);
        var response = await client.SearchAsync<StringResponse>("students", PostData.Serializable(new
        {
            query = new
            {
                wildcard = new
                {
                    Department = new { value = $"*{value}*" }
                }
            }
        }));

        var result = JObject.Parse(response.Body);

        var hits = result["hits"]["hits"].ToObject<List<JObject>>();
        List<Student> students = new();

        foreach (var hit in hits)
            students.Add(hit["_source"].ToObject<Student>());

        return Ok(students.Take(10));
    }
}
