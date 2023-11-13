using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Memory.Weaviate;
using Microsoft.SemanticKernel.Plugins.Memory;
using StudentHub.Server.Data;
using StudentHub.Server.Models;
using StudentHub.Server.Services;
using StudentHub.Server.Services.AiServices;
using StudentHub.Server.Services.DataService;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("sensitivesettings.json", true);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddIdentityServer()
    .AddApiAuthorization<ApplicationUser, ApplicationDbContext>();

builder.Services.AddAuthentication()
    .AddIdentityServerJwt()
    .AddGoogle(o =>
    {
        o.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? throw new InvalidOperationException();
        o.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ??
                         throw new InvalidOperationException();
    });

// Globally enable CORS for all origins, methods, and headers
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builderCors =>
        {
            builderCors.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});


builder.Services.AddScoped<KernelService>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<EmbeddingCacheService>();
builder.Services.AddScoped<ChatAiService>();
builder.Services.AddSingleton<IChatService, ChatService>();
builder.Services.AddScoped<MemoryBuilder>();

var skFunctionDictionary = CreateSkFunctionDictionary();
builder.Services.AddSingleton<IDictionary<string, Microsoft.SemanticKernel.ISKFunction>>(skFunctionDictionary);



builder.Services.AddSingleton<ChatHistoryService>()
    .AddScoped<IDataService, DataService>()
    .AddScoped<TextEmbeddingService>()
    .AddSingleton<IUserAuthService, UserAuthService>();


builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

IDictionary<string, ISKFunction> CreateSkFunctionDictionary()
{
    var dictionary = new Dictionary<string, ISKFunction>();
    // Populate your dictionary here
    return dictionary;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseIdentityServer();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();