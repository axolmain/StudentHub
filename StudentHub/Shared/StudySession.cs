using System.ComponentModel.DataAnnotations;

namespace StudentHub.Shared;

public class StudySession
{
    public StudySession() {}

    [Key]
    public string id { get; set; }
    public string FileName { get; set; }
    public string UserId { get; set; }
    public string SessionName { get; set; }
    public string LastMessageTimeStamp { get; set; }
    public string Messages { get; set; }
}