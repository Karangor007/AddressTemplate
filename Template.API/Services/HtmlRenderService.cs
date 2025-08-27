using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Template.API.Interface;
using Template.API.Models;

namespace Template.API.Services
{
    public class HtmlRenderService : IHtmlRenderService
    {
        private readonly ITemplateService _templates;
        private readonly IConfiguration _config;
        private static readonly Regex Placeholder = new(@"\$([A-Za-z0-9_]+)", RegexOptions.Compiled);
        private readonly ILogger<HtmlRenderService> _logger;
        private readonly IWebHostEnvironment _env;

        public HtmlRenderService(ITemplateService templates, IConfiguration config, ILogger<HtmlRenderService> logger, IWebHostEnvironment env)
        {
            _templates = templates;
            _config = config;
            _logger = logger;
            _env = env;
        }

        public async Task<RenderResult> RenderFromCsvAsync(Stream csvStream, RenderRequest request, CancellationToken ct = default)
        {
            string templateName = "SixtyDaysLetterPrompt.html";

            _logger.LogInformation("Starting RenderFromCsvAsync with TemplateName={Template}", templateName);

            try
            {
                // 🔹 Load template from content root
                var templatePath = Path.Combine(_env.ContentRootPath, "Templates", templateName);
                if (!File.Exists(templatePath))
                    throw new FileNotFoundException($"Template not found at {templatePath}");

                var template = await File.ReadAllTextAsync(templatePath, Encoding.UTF8, ct);

                // 🔹 Read CSV
                using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
                var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    DetectDelimiter = true,
                    IgnoreBlankLines = true,
                    TrimOptions = TrimOptions.Trim,
                    PrepareHeaderForMatch = args => args.Header?.Trim() ?? string.Empty,
                    BadDataFound = null // Ignore malformed rows
                };

                using var csv = new CsvReader(reader, cfg);
                var rows = new List<IDictionary<string, string?>>();

                if (await csv.ReadAsync())
                {
                    csv.ReadHeader();
                    var headers = csv.HeaderRecord?.Select(h => h.Trim()).ToList() ?? new List<string>();
                    _logger.LogInformation("CSV Headers: {Headers}", string.Join(",", headers));

                    while (await csv.ReadAsync())
                    {
                        var dict = headers.ToDictionary(
                            h => h,
                            h => (string?)csv.GetField(h) ?? string.Empty,
                            StringComparer.OrdinalIgnoreCase
                        );
                        rows.Add(dict);
                    }
                }

                _logger.LogInformation("Total rows parsed from CSV: {RowCount}", rows.Count);

                // 🔹 Build combined HTML
                var combinedHtml = new StringBuilder();
                combinedHtml.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'/><title>Letters</title></head><body>");

                int rowIndex = 1;
                foreach (var row in rows)
                {
                    var values = BuildValueBag(row, request.DateFormat);

                    var html = Placeholder.Replace(template, m =>
                    {
                        var key = m.Groups[1].Value;
                        if (key.Equals("CardNunber", StringComparison.OrdinalIgnoreCase))
                            key = "CardNumber";

                        return values.TryGetValue(key, out var val) ? val : string.Empty;
                    });

                    combinedHtml.AppendLine("<div style='page-break-after: always;'>");
                    combinedHtml.AppendLine(html);
                    combinedHtml.AppendLine("</div>");

                    _logger.LogDebug("Rendered letter {Index} for {FirstName} {LastName}", rowIndex, values.GetValueOrDefault("FirstName"), values.GetValueOrDefault("LastName"));
                    rowIndex++;
                }

                combinedHtml.AppendLine("</body></html>");

                var result = new RenderResult { TemplateName = templateName };
                result.Items.Add(new RenderedItem("letters.html", combinedHtml.ToString()));

                _logger.LogInformation("Finished rendering {RowCount} letters into a single HTML file", rows.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Source: {Source}, Operation: {Operation}, Message: {Message}, Request: {RequestJson}",
                    nameof(HtmlRenderService),
                    nameof(RenderFromCsvAsync),
                    ex.Message,
                    JsonConvert.SerializeObject(request));

                throw;
            }
        }


        public byte[] ZipToBytes(RenderResult render)
        {
            try
            {
                _logger.LogInformation("Creating ZIP archive for {Count} rendered HTML items", render.Items.Count);

                using var ms = new MemoryStream();
                using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
                {
                    foreach (var item in render.Items)
                    {
                        var entry = archive.CreateEntry(item.FileName, CompressionLevel.Optimal);
                        using var es = entry.Open();
                        var bytes = Encoding.UTF8.GetBytes(item.Html);
                        es.Write(bytes, 0, bytes.Length);

                        _logger.LogDebug("Added {FileName} to ZIP archive", item.FileName);
                    }
                }

                _logger.LogInformation("ZIP archive created successfully (Size={Size} bytes)", ms.Length);
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                   "Source: {Source}, Operation: {Operation}, Message: {Message}, render: {RequestJson}",
                   nameof(HtmlRenderService),
                   nameof(RenderFromCsvAsync),
                   ex.Message,
                   JsonConvert.SerializeObject(render));

                throw; // preserve stack trace
                throw;
            }
           
        }

        private Dictionary<string, string> BuildValueBag(IDictionary<string, string?> row, string? dateFormat)
        {
            string Get(string key) => row.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v) ? v!.Trim() : string.Empty;
            string Combine(params string[] keys) => string.Join(" ", keys.Select(k => Get(k)).Where(s => !string.IsNullOrWhiteSpace(s)));

            var bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["FirstName"] = Get("FirstName"),
                ["LastName"] = Get("LastName"),
                ["ContactPerson"] = Combine("FirstName", "LastName").Trim(),
                ["StreetAddress"] = Combine("Address1", "Address2", "StreetAddress").Trim(),
                ["Suburb"] = string.IsNullOrWhiteSpace(Get("Suburb")) ? Get("City") : Get("Suburb"),
                ["State"] = Get("State"),
                ["PostCode"] = string.IsNullOrWhiteSpace(Get("PostCode")) ? Get("PostalCode") : Get("PostCode"),
                ["CardNumber"] = Get("CardNumber"),
                ["ExpireDate"] = FormatDate(Get("ExpireDate"), dateFormat),
                ["MyCompanyPhoneNumber"] = _config["Company:Phone"] ?? string.Empty
            };

            // Mirror CSV headers directly as placeholders too
            foreach (var kv in row)
            {
                bag[kv.Key] = kv.Value ?? string.Empty;
            }

            return bag;
        }

        private static string FormatDate(string raw, string? format)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            if (DateTime.TryParse(raw, out var dt)) return dt.ToString(format ?? "yyyy-MM-dd");
            return raw;
        }

        // 🔹 Helper: generates safe file names
        private static string MakeFileName(IDictionary<string, string?> row, string? fileNameColumn, int index)
        {
            string Sanitize(string s)
            {
                foreach (var ch in Path.GetInvalidFileNameChars())
                    s = s.Replace(ch, '_');
                return s.Trim();
            }

            if (!string.IsNullOrWhiteSpace(fileNameColumn) &&
                row.TryGetValue(fileNameColumn, out var val) &&
                !string.IsNullOrWhiteSpace(val))
            {
                return Sanitize(val!);
            }

            var first = row.TryGetValue("FirstName", out var fn) ? fn : string.Empty;
            var last = row.TryGetValue("LastName", out var ln) ? ln : string.Empty;
            var baseName = $"{first} {last}".Trim();

            if (string.IsNullOrWhiteSpace(baseName)) baseName = $"Row-{index}";
            return Sanitize(baseName);
        }
    }
}
