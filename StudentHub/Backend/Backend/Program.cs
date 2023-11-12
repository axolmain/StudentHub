using Backend;
using Backend.Auth;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Configuration.AddJsonFile("sensitivesettings.json", true);


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddServices2();
builder.Services.AddServices();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "Firebase";
        options.DefaultChallengeScheme = "Firebase";
    })
    .AddScheme<AuthenticationSchemeOptions, FirebaseAuthenticationHandler>("Firebase", null);

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

FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromFile("firebasekey.json")
});

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use the CORS middleware here, before UseRouting
app.UseCors("AllowAllOrigins");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();