using Template.API.Interface;
using Template.API.Models;

namespace Template.API.Services
{
    public class TemplateService : ITemplateService
    {
        private readonly string _root;
        private readonly Dictionary<string, TemplateInfo> _templates = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _gate = new();
        private readonly ILogger<TemplateService> _logger;

        public TemplateService(IWebHostEnvironment env, ILogger<TemplateService> logger)
        {
            _logger = logger;
            _root = Path.Combine(env.ContentRootPath, "Templates");

            try
            {
                Directory.CreateDirectory(_root);
                foreach (var path in Directory.EnumerateFiles(_root, "*.html", SearchOption.AllDirectories))
                {
                    var name = Path.GetFileNameWithoutExtension(path);
                    _templates[name] = new TemplateInfo(name, path);
                }

                _logger.LogInformation("Initialized TemplateService with {Count} templates from {Root}",
                    _templates.Count, _root);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error initializing TemplateService. Root={Root}, Message={Message}",
                    _root, ex.Message);
                throw;
            }
        }

        public IEnumerable<TemplateInfo> GetAll()
        {
            _logger.LogInformation("Retrieving all templates ({Count})", _templates.Count);
            return _templates.Values.OrderBy(t => t.Name);
        }

        public TemplateInfo? Get(string name)
        {
            var found = _templates.TryGetValue(name, out var t);
            _logger.LogInformation(found
                ? "Template {Name} found."
                : "Template {Name} not found.", name);
            return t;
        }

        public async Task<TemplateInfo> SaveAsync(string name, IFormFile file)
        {
            try
            {
                if (!name.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                    name += ".html";

                var safeName = Path.GetFileNameWithoutExtension(name);
                var fullPath = Path.Combine(_root, safeName + ".html");

                using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
                {
                    await file.CopyToAsync(fs);
                }

                var info = new TemplateInfo(safeName, fullPath);
                lock (_gate)
                {
                    _templates[safeName] = info;
                }

                _logger.LogInformation("Template {Name} saved at {Path}", safeName, fullPath);

                return info;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error saving template. Name={Name}, Message={Message}",
                    name, ex.Message);
                throw;
            }
        }

        public async Task<string> ReadAsync(string name)
        {
            try
            {
                var t = Get(name) ?? throw new FileNotFoundException($"Template '{name}' not found.");
                _logger.LogInformation("Reading template {Name} from {Path}", name, t.Path);

                return await File.ReadAllTextAsync(t.Path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error reading template. Name={Name}, Message={Message}",
                    name, ex.Message);
                throw;
            }
        }
    }
}

