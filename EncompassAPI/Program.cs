using EncompassAPI.AppLayer;
using EncompassAPI.Models.Configs;
using EncompassAPI.Services;
using TokenGenerator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<AWSConfig>(options => builder.Configuration.GetSection(nameof(AWSConfig)).Bind(options));
builder.Services.Configure<LoggingConfig>(options => builder.Configuration.GetSection(nameof(LoggingConfig)).Bind(options));
builder.Services.Configure<FileConfig>(options => builder.Configuration.GetSection(nameof(FileConfig)).Bind(options));
builder.Services.Configure<S3Config>(options => builder.Configuration.GetSection(nameof(S3Config)).Bind(options));
builder.Services.Configure<EncompassConfig>(options => builder.Configuration.GetSection(nameof(EncompassConfig)).Bind(options));

builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IDisclosuresService, DisclosuresService>();
builder.Services.AddScoped<IeFolderService, eFolderService>();
builder.Services.AddScoped(typeof(LoggerService<>));
builder.Services.AddScoped(typeof(SecretService));
builder.Services.AddScoped(typeof(S3Service));

builder.Services.AddLogging(logging =>
{
    var config = builder.Configuration.GetAWSLoggingConfigSection();
    logging.AddAWSProvider(config);
    logging.SetMinimumLevel(LogLevel.Trace);
});

//todo Get Security keys from secrets manager

/*IAuthCreds authCreds = SecurityKeysBuilder.Build();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Security.Issuer,

            ValidateAudience = true,
            ValidAudience = Security.Audience,
            ValidateLifetime = true,

            IssuerSigningKey = authCreds.SigningKey,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(2),
            TokenDecryptionKey = authCreds.EncryptionKey,
        };
    });*/


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Security.Policy, policy => policy.RequireRole(Security.Role));
});

var app = builder.Build();

// Handle logging shutdown
app.Lifetime.ApplicationStopping.Register(() =>
{
    if (app.Services.GetService<ILoggerFactory>() is IDisposable loggerFactory)
    {
        loggerFactory.Dispose();
    }
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EncompassAPI API V1");
    c.RoutePrefix = "";
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
