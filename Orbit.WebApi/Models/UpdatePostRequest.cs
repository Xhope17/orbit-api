namespace Orbit.WebApi.Models;

public class UpdatePostRequest
{
    public string? Content { get; set; }
    public List<IFormFile>? Media { get; set; }
}
