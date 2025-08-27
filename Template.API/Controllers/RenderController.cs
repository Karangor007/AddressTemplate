using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Template.API.Interface;
using Template.API.Models;

namespace Template.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RenderController : ControllerBase
    {
        private readonly IHtmlRenderService _renderer;
        private readonly ILogger<RenderController> _logger;
        private readonly IWebHostEnvironment _env;

        public RenderController(IHtmlRenderService renderer, ILogger<RenderController> logger, IWebHostEnvironment env)
        {
            _renderer = renderer;
            _logger = logger;
            _env = env;
        }

        [HttpPost]        
        [Route("RenderHtml")]
        public async Task<ActionResult<RenderResult>> RenderHtml([FromForm] RenderHtmlRequest request)
        {
            _logger.LogInformation("Received RenderHtml with  FileNameColumn={FileNameColumn}",
                request.FileNameColumn);

            try
            {
                if (request.Csv == null || request.Csv.Length == 0)
                {
                    _logger.LogWarning("CSV file missing in RenderHtml request.");
                    return BadRequest("CSV file is required.");
                }

                var renderReq = new RenderRequest
                {
                    FileNameColumn = request.FileNameColumn
                };

                using var stream = request.Csv.OpenReadStream();
                var result = await _renderer.RenderFromCsvAsync(stream, renderReq, HttpContext.RequestAborted);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RenderHtml.");
                throw;
            }
        }

        [HttpGet("download/{fileName}")]
        public IActionResult DownloadLetter(string fileName)
        {
            try
            {
                _logger.LogInformation("DownloadLetter with  string = {fileName}",
                fileName);

                if (string.IsNullOrWhiteSpace(fileName))
                    return BadRequest("Invalid file name.");

                var filePath = Path.Combine(_env.ContentRootPath, "RenderedLetters", fileName);

                if (!System.IO.File.Exists(filePath))
                    return NotFound("File not found.");

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                return File(fileBytes, "text/html", fileName);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error in DownloadLetter.");
                throw;
            }
          
        }
    }
}
