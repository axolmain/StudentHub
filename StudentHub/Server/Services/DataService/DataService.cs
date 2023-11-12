using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.EntityFrameworkCore;
using StudentHub.Server.Data;

// Assuming your models are defined here

namespace StudentHub.Server.Services.DataService;

public class DataService : IDataService
{
    private const string ContainerName = "data"; // It could be a more general name or specified in the configuration
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ApplicationDbContext _dbContext; // EF Core DB context

    public DataService(IConfiguration configuration, ApplicationDbContext dbContext)
    {
        _blobServiceClient = new BlobServiceClient(configuration.GetConnectionString("AzureBlobConnectionString1"));
        _dbContext = dbContext;
    }

    public async Task UploadFileAsync(string fileName, string studySessionId, string userId, Stream fileStream)
    {
        var containerName = "data";
        var blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await blobContainerClient.CreateIfNotExistsAsync();

        var id = Guid.NewGuid().ToString();

        var blobClient = blobContainerClient.GetBlobClient($"{userId}/{studySessionId}/content/{id}");

        var fileType = Path.GetExtension(fileName);

        var uploadOptions = new BlobUploadOptions
        {
            Metadata = new Dictionary<string, string> { { "fileType", fileType } }
        };

        await blobClient.UploadAsync(fileStream, uploadOptions);
        fileStream.Close();

        // Save file metadata to your database instead of Cosmos DB
        var document = new UserDocument
        {
            id = id,
            FileName = fileName,
            FileId = blobClient.Uri.AbsoluteUri,
            SessionId = studySessionId,
            UserId = userId
        };

        _dbContext.UserDocuments.Add(document);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<string> CreateStudySession(string studySessionName, string userId)
    {
        var id = Guid.NewGuid().ToString();

        var studySession = new StudySession
        {
            Name = studySessionName,
            id = id,
            UserId = userId
        };

        _dbContext.StudySessions.Add(studySession);
        await _dbContext.SaveChangesAsync();

        return id;
    }

    public async Task<IEnumerable<StudySession>> GetStudySessions(string userId)
    {
        return await _dbContext.StudySessions
            .Where(s => s.UserId == userId)
            .ToListAsync();
    }

    public async Task<IEnumerable<UserDocument>> GetSessionDocuments(string? userId, string studySessionId)
    {
        return await _dbContext.UserDocuments
            .Where(d => d.SessionId == studySessionId)
            .ToListAsync();
    }

    public async Task<(Stream File, string FileType)> GetFile(string? userId, string studySessionId, string fileId)
    {
        var blobContainerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);

        // Include userId in the blob's path
        var blobClient = blobContainerClient.GetBlobClient($"{userId}/{studySessionId}/content/{fileId}");

        Response<BlobDownloadInfo>? response = await blobClient.DownloadAsync();
        IDictionary<string, string>? metadata = blobClient.GetPropertiesAsync().Result.Value.Metadata;

        var fileType = metadata["fileType"]; // assuming you have stored file type in metadata

        var memoryStream = new MemoryStream();
        await response.Value.Content.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        return (memoryStream, fileType);
    }
}