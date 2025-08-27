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

        public RenderController(IHtmlRenderService renderer, ILogger<RenderController> logger)
        {
            _renderer = renderer;
            _logger = logger;
        }

        [HttpPost]        
        [Consumes("multipart/form-data")]
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

                _logger.LogInformation("RenderHtml completed with {Count} items", result.Items.Count);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RenderHtml.");
                throw;
            }
        }
    }
}
