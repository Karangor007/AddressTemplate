namespace Template.API.Models
{
    public class RenderRequest
    {
        public string TemplateName { get; set; } = "SixtyDaysLetterPrompt";
        public string? FileNameColumn { get; set; } // optional column for naming outputs
        public string? DateFormat { get; set; } = "yyyy-MM-dd"; // used when formatting dates
    }
}
