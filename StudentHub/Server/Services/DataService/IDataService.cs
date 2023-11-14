namespace StudentHub.Server.Services.DataService;

public interface IDataService
{
    public Task UploadFileAsync(string fileName, string studySessionId, string userId, Stream fileStream);
    public Task<string> CreateStudySession(string studySessionName, string userId);
    public Task<IEnumerable<StudySession>> GetStudySessions(string userId);
    public Task<IEnumerable<UserDocument>> GetSessionDocuments(string? userId, string studySessionId);
    public Task<string> GetStudySessionId(string userId, string sessionName);

    public Task<(Stream File, string FileType)> GetFile(string? userId, string studySessionId, string fileId);
}

public class StudySession
{
    public string Name { get; set; }
    public string id { get; set; }
    public string UserId { get; set; }
}

public class UserDocument
{
    public string id { get; set; }
    public string FileName { get; set; }
    public string FileId { get; set; }
    public string SessionId { get; set; }
    public string UserId { get; set; }
}