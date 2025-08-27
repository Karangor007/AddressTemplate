using Template.API.Models;

namespace Template.API.Interface
{
    public interface IHtmlRenderService
    {
        Task<RenderResult> RenderFromCsvAsync(Stream csvStream, RenderRequest request, CancellationToken ct = default);
        byte[] ZipToBytes(RenderResult render);
    }
}
