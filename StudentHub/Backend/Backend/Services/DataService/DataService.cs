using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.SemanticKernel.Memory;

namespace Backend.Services.DataService;

public class DataService : IDataService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly CosmosClient _cosmosClient;
    private readonly Container _filesContainer;
    private readonly Container _sessionsContainer;

    public DataService(IConfiguration configuration)
    {
        _blobServiceClient = new BlobServiceClient(configuration.GetConnectionString("AzureBlobConnectionString1"));
        _cosmosClient = new CosmosClient(configuration.GetConnectionString("CosmosDb"));
        Database? database = _cosmosClient.GetDatabase("userDataMap");
        _filesContainer = database.GetContainer("sessionFiles");
        _sessionsContainer = database.GetContainer("studySessions");
    }

    public async Task UploadFileAsync(string fileName, string studySessionId, string userId, Stream fileStream)
    {
        string containerName = "data"; // It could be a more general name or specified in the configuration
        BlobContainerClient? blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await blobContainerClient.CreateIfNotExistsAsync();

        string id = Guid.NewGuid().ToString();

        // Include userId in the blob's path
        BlobClient? blobClient = blobContainerClient.GetBlobClient($"{userId}/{studySessionId}/content/{id}");

        // Parse the file type from the file name
        string fileType = Path.GetExtension(fileName);

        BlobUploadOptions uploadOptions = new BlobUploadOptions
        {
            Metadata = new Dictionary<string, string>
            {
                { "fileType", fileType }
            }
        };

        await blobClient.UploadAsync(fileStream, uploadOptions);

        fileStream.Close();

        UserDocument document = new UserDocument
        {
            id = id,
            FileName = fileName,
            FileId = blobClient.Uri.AbsoluteUri,
            SessionId = studySessionId,
            UserId = userId
        };

        await _filesContainer.CreateItemAsync(document, new PartitionKey(studySessionId));
    }

    public async Task<string> CreateStudySession(string studySessionName, string userId)
    {
        string id = Guid.NewGuid().ToString();

        StudySession studySession = new StudySession
        {
            Name = studySessionName,
            id = id,
            UserId = userId
        };

        await _sessionsContainer.CreateItemAsync(studySession, new PartitionKey(userId));

        return id;
    }

    public async Task<IEnumerable<StudySession>> GetStudySessions(string userId)
    {
        string sqlQueryText = "SELECT * FROM c WHERE c.UserId = @userId";
        QueryDefinition? queryDefinition = new QueryDefinition(sqlQueryText).WithParameter("@userId", userId);
        FeedIterator<StudySession>? queryResultSetIterator =
            _sessionsContainer.GetItemQueryIterator<StudySession>(queryDefinition);

        List<StudySession> studySessions = new();

        while (queryResultSetIterator.HasMoreResults)
        {
            FeedResponse<StudySession>? currentResultSet = await queryResultSetIterator.ReadNextAsync();
            foreach (StudySession? session in currentResultSet) studySessions.Add(session);
        }

        return studySessions;
    }

    public async Task<IEnumerable<UserDocument>> GetSessionDocuments(string? userId, string studySessionId)
    {
        string sqlQueryText = "SELECT * FROM c WHERE c.SessionId = @sessionId";
        QueryDefinition? queryDefinition =
            new QueryDefinition(sqlQueryText).WithParameter("@sessionId", studySessionId);
        FeedIterator<UserDocument>? queryResultSetIterator =
            _filesContainer.GetItemQueryIterator<UserDocument>(queryDefinition);

        List<UserDocument> documents = new();

        while (queryResultSetIterator.HasMoreResults)
        {
            FeedResponse<UserDocument>? currentResultSet = await queryResultSetIterator.ReadNextAsync();
            foreach (UserDocument? document in currentResultSet) documents.Add(document);
        }

        return documents;
    }
    private readonly string _containerName = "data"; // It could be a more general name or specified in the configuration

    public async Task<(Stream File, string FileType)> GetFile(string? userId, string studySessionId, string fileId)
    {
        BlobContainerClient? blobContainerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

        // Include userId in the blob's path
        BlobClient? blobClient = blobContainerClient.GetBlobClient($"{userId}/{studySessionId}/content/{fileId}");

        Azure.Response<BlobDownloadInfo>? response = await blobClient.DownloadAsync();
        IDictionary<string, string>? metadata = blobClient.GetPropertiesAsync().Result.Value.Metadata;

        string fileType = metadata["fileType"]; // assuming you have stored file type in metadata

        MemoryStream memoryStream = new MemoryStream();
        await response.Value.Content.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        return (memoryStream, fileType);
    }
}