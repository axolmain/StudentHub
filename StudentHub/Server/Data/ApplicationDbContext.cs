using Duende.IdentityServer.EntityFramework.Options;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StudentHub.Server.Models;
using StudentHub.Server.Services.DataService;

namespace StudentHub.Server.Data;

public class ApplicationDbContext : ApiAuthorizationDbContext<ApplicationUser>
{
    public ApplicationDbContext(
        DbContextOptions options,
        IOptions<OperationalStoreOptions> operationalStoreOptions) : base(options, operationalStoreOptions)
    {
    }
    public DbSet<UserDocument> UserDocuments { get; set; } // Table for UserDocuments
    public DbSet<StudySession> StudySessions { get; set; } // Table for StudySessions
}