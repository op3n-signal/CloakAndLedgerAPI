using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.S3;
using AutoMapper;
using EncompassAPI;
using EncompassAPI.AppLayer;
using EncompassAPI.Models.Configs;
using EncompassAPI.Repos;
using EncompassAPI.Services;
using EncompassAPI.Utility.Filters;
using EncompassAPI.Utility.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TokenGenerator;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<AuthHeaderFilter>();
});

builder.Services.AddAutoMapper(typeof(MapperProfile));

builder.Services.Configure<AWSConfig>(options => builder.Configuration.GetSection(nameof(AWSConfig)).Bind(options));
builder.Services.Configure<LoggingConfig>(options => builder.Configuration.GetSection(nameof(LoggingConfig)).Bind(options));
builder.Services.Configure<FileConfig>(options => builder.Configuration.GetSection(nameof(FileConfig)).Bind(options));
builder.Services.Configure<S3Config>(options => builder.Configuration.GetSection(nameof(S3Config)).Bind(options));
builder.Services.Configure<EncompassConfig>(options => builder.Configuration.GetSection(nameof(EncompassConfig)).Bind(options));
builder.Services.Configure<SalesforceConfig>(options => builder.Configuration.GetSection(nameof(SalesforceConfig)).Bind(options));
builder.Services.Configure<DocusignConfig>(options => builder.Configuration.GetSection(nameof(DocusignConfig)).Bind(options));

builder.Services.AddScoped<IDisclosuresServiceV2, DisclosuresV2Service>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IeFolderService, eFolderService>();
builder.Services.AddScoped(typeof(LoggerService<>), typeof(LoggerService<>));
builder.Services.AddScoped(typeof(S3Service));
builder.Services.AddScoped(typeof(AWSSecretService));
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddScoped<StorageService>();
builder.Services.AddScoped<ISalesforceService, SalesforceService>();
builder.Services.AddScoped<ICandidDataRepo, CandidDataRepo>();
builder.Services.AddScoped<IUploadService, UploadService>();
builder.Services.AddScoped<ICandidDataService, CandidDataService>();
builder.Services.AddScoped<IEncompassService, DocsService>();
builder.Services.AddScoped<IDocusignService, DocusignService>();
builder.Services.AddTransient<IDisclosureAuditService, DisclosureAuditService>();
builder.Services.AddTransient<IDisclosureAuditAppService, DisclosureAuditAppService>();
builder.Services.AddTransient<IMappingAppService, MappingAppService>();
builder.Services.AddTransient<IDocumentMappingService, DocumentMappingService>();

builder.Services.AddAWSService<IAmazonDynamoDB>();
builder.Services.AddSingleton<IDynamoDBContext, DynamoDBContext>();

builder.Services.AddScoped<IDataService, DataService>(serviceProvider =>
{
    var awsOptions = serviceProvider.GetRequiredService<IOptionsSnapshot<AWSConfig>>();
    var dataService = new DataService(awsOptions);
    Task.Run(async () => await dataService.Init()).GetAwaiter().GetResult();
    return dataService;
});
builder.Services.AddScoped(f => f.GetRequiredService<IDataService>().GetConnection());

builder.Services.AddLogging(logging =>
{
    var config = builder.Configuration.GetAWSLoggingConfigSection();
    logging.AddAWSProvider(config);
    logging.SetMinimumLevel(LogLevel.Trace);
});

// Get Security keys from secrets manager
var awsConfig = builder.Configuration.GetSection("AWSConfig").Get<AWSConfig>();
var tokenProvider = new TokenProvider();
await tokenProvider.InitializeAsync(awsConfig);

IAuthCreds authCreds = SecurityKeysBuilder.Build(tokenProvider.SecurityKeys.salt, tokenProvider.SecurityKeys.signing, tokenProvider.SecurityKeys.encryption);

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
    });


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Security.Policy, policy => policy.RequireRole(Security.Role));
});

builder.Services.AddTransient<EncompassTokenHandler>();

// Client with the handler
builder.Services.AddHttpClient<IHttpService, HttpService>("EncompassClient")
       .AddHttpMessageHandler<EncompassTokenHandler>();

// Client without the handler
builder.Services.AddHttpClient<IHttpService, HttpService>("DefaultClient");

// Register two IHttpService instances via factory
builder.Services.AddScoped<IEncompassHttpService>(sp =>
{
    var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient("EncompassClient");
    return new HttpService(client, sp.GetRequiredService<ILogger<HttpService>>());
});

// If you need another service without handler, register with a different interface or named service
builder.Services.AddScoped<IDefaultHttpService>(sp =>
{
    var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient("DefaultClient");
    return new HttpService(client, sp.GetRequiredService<ILogger<HttpService>>());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger(u =>
{
    u.RouteTemplate = "swagger/{documentName}/swagger.json";
});
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EncompassAPI API V1");
    c.RoutePrefix = "";
});

app.UseMiddleware<EncompassMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
