using System.ComponentModel.DataAnnotations;

namespace Project.HttpServer.Models;

public class PhotoUploadModel
{
    [Required]
    public IFormFile Photo { get; set; } = null!;
}
