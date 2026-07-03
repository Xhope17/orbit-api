namespace Orbit.WebApi.Models;

public class CreatePostRequest
{
    public string? Content { get; set; }
    public List<IFormFile>? Media { get; set; }
}
