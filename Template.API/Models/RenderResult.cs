namespace Template.API.Models
{
    public record RenderedItem(string FileName, string Html);
    public class RenderResult
    {
        public string TemplateName { get; set; } = string.Empty;
        public List<RenderedItem> Items { get; set; } = new();
        public int Count => Items.Count;
        public string UniqueFileName { get; set; }
    }
}
