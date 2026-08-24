using HarleyLeadApi.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        path: "Logs/api_log-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddScoped<ILeadService, LeadService>();
    builder.Services.AddHttpClient<IDispositionService, DispositionService>();
    builder.Services.AddHttpClient<INotConnectedService, NotConnectedService>();
    //builder.Services.AddScoped<INotConnectedService, NotConnectedService>();
    var app = builder.Build();

    // IIS aur Development dono me Swagger chalega
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("../swagger/v1/swagger.json", "HarleyLeadApi V1");
    });

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}