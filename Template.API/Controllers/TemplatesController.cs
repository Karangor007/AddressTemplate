using Microsoft.AspNetCore.Mvc;
using Template.API.Interface;
using Template.API.Models;

namespace Template.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TemplatesController : ControllerBase
    {
        private readonly ITemplateService _templates;
        private readonly ILogger<TemplatesController> _logger;

        public TemplatesController(ITemplateService templates, ILogger<TemplatesController> logger)
        {
            _templates = templates;
            _logger = logger;
        }

        [HttpGet]
        [Route("GetAll")]
        public ActionResult<IEnumerable<TemplateInfo>> GetAll()
        {
            _logger.LogInformation("Received request GetAll templates");
            try
            {
                var templates = _templates.GetAll();
                _logger.LogInformation("Returning {Count} templates", templates.Count());
                return Ok(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAll templates. Message={Message}", ex.Message);
                throw;
            }
        }

        [HttpGet]
        public async Task<ActionResult> Get(string name)
        {
            _logger.LogInformation("Received request Get template {Name}", name);
            try
            {
                var text = await _templates.ReadAsync(name);
                return Content(text, "text/html", System.Text.Encoding.UTF8);
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogWarning("Template {Name} not found. Message={Message}", name, ex.Message);
                return NotFound(new { ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Get template {Name}. Message={Message}", name, ex.Message);
                throw;
            }
        }

        [HttpPost]        
        public async Task<ActionResult<TemplateInfo>> Upload([FromForm] UploadTemplateRequest request)
        {
            _logger.LogInformation("Received request Upload template {Name}", request.Name);

            try
            {
                if (request.File == null || request.File.Length == 0)
                {
                    _logger.LogWarning("No file uploaded in Upload request for template {Name}", request.Name);
                    return BadRequest("No file uploaded.");
                }

                var info = await _templates.SaveAsync(request.Name, request.File);
                _logger.LogInformation("Template {Name} uploaded successfully", request.Name);
                return CreatedAtAction(nameof(Get), new { name = info.Name }, info);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Upload template {Name}. Message={Message}", request.Name, ex.Message);
                throw;
            }
        }
    }
}
