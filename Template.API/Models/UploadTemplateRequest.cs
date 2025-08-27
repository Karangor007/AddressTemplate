using Microsoft.AspNetCore.Mvc;

namespace Template.API.Models
{
    public class UploadTemplateRequest
    {
        [FromForm(Name = "name")]
        public string Name { get; set; } = string.Empty;

        [FromForm(Name = "file")]
        public IFormFile File { get; set; } = default!;
    }
}
