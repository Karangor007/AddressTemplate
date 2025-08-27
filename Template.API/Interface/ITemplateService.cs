using Template.API.Models;

namespace Template.API.Interface
{
    public interface ITemplateService
    {
        IEnumerable<TemplateInfo> GetAll();
        TemplateInfo? Get(string name);
        Task<string> ReadAsync(string name);
        Task<TemplateInfo> SaveAsync(string name, IFormFile file);
    }
}
