using System.Reflection;
using EncompassAPI.Middleware;
using EncompassAPI.Models;
using EncompassAPI.Services;
using EncompassAPI.Utility.Filters;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<AuthKeyHeaderFilter>();
});

builder.Services.Configure<AWSConfig>(options => builder.Configuration.GetSection("AWSConfig").Bind(options));
builder.Services.Configure<LogSettingsConfig>(options => builder.Configuration.GetSection("LogSettingsConfig").Bind(options));

builder.Services.AddTransient<IAuthService, AuthService>();
builder.Services.AddScoped(typeof(LoggerService<>));

builder.Services.AddLogging(logging =>
{
    var config = builder.Configuration.GetAWSLoggingConfigSection();
    logging.AddAWSProvider(config);
    logging.SetMinimumLevel(LogLevel.Trace);
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
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProjectNameHere API V1"); // TODO Please replace your project name with ProjectNameHere
    c.RoutePrefix = "";
});

app.UseMiddleware<AuthKeyMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
