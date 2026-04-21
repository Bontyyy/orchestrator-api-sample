using OrchestratorApiSample.Api.Persistence;
using OrchestratorApiSample.Application.Interfaces;
using OrchestratorApiSample.Application.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<IWidgetRepository, InMemoryWidgetRepository>();
builder.Services.AddScoped<WidgetService>();

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();

app.Run();

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public partial class Program
{
}
