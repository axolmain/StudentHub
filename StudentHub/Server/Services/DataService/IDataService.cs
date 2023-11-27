using System.ComponentModel.DataAnnotations;
using StudentHub.Shared;

namespace StudentHub.Server.Services.DataService;

public interface IDataService
{
    public Task UploadFileAsync(string fileName, string studySessionId, string userId, Stream fileStream);
    public Task SaveChatSession(List<ChatMessage> messages, string sessionId, string userId);
    public Task<bool> DeleteStudySession(string studySessionId, string userId);
    public Task<string> CreateStudySession(string studySessionName, string userId, string fileName);
    public Task<IEnumerable<StudySession>> GetStudySessions(string userId);
    public Task<IEnumerable<UserDocument>> GetSessionDocuments(string? userId, string studySessionId);
    public Task<StudySession> GetStudySession(string userId, string sessionId);
    public Task<(Stream File, string FileType)> GetFile(string? userId, string studySessionId, string fileId);
}


public class UserDocument
{
    public string id { get; set; }
    public string FileName { get; set; }
    public string FileId { get; set; }
    public string SessionId { get; set; }
    public string UserId { get; set; }
}