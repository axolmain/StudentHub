using System.Collections.Concurrent;
using StudentHub.Server.Services.DataService;

namespace StudentHub.Server.Services;

public class TextEmbeddingService
{
    private readonly IDataService _dataService;

    public TextEmbeddingService(IDataService dataService)
    {
        _dataService = dataService;
    }

    public async Task<IEnumerable<Chunk>> GetChunks(string? userId, string studySessionId)
    {
        var chunks = new ConcurrentBag<Chunk>();

        var files = await _dataService.GetSessionDocuments(userId, studySessionId);

        var tasks = files.Select(async file =>
        {
            Stream? stream = null;
            try
            {
                (stream, string ext) = await _dataService.GetFile(userId, studySessionId, file.id);

                EmbeddingService es = new(stream, ext);

                // If the Paragraphs property is thread-safe or if EmbeddingService is reentrant
                Parallel.ForEach(es.Paragraphs, paragraph =>
                {
                    chunks.Add(new Chunk
                    {
                        Text = paragraph,
                        SourceFile = file.FileName
                    });
                });
            }
            catch (Exception ex)
            {
                stream?.Close();
                stream?.Dispose();
                throw;
            }
        }).ToArray();

        await Task.WhenAll(tasks);

        return chunks;
    }
}

public class Chunk
{
    public string Text { get; set; }
    public string SourceFile { get; set; }
}