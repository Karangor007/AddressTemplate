using Microsoft.AspNetCore.Mvc;

namespace Template.API.Models
{
    public class RenderHtmlRequest
    {
        [FromForm(Name = "csv")]
        public IFormFile Csv { get; set; } = default!;
       

        [FromForm(Name = "fileNameColumn")]
        public string? FileNameColumn { get; set; }
    }
}
