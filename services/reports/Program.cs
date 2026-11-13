using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpLogging();
builder.Services.AddHttpClient("loans", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["LoansUrl"] ?? throw new InvalidOperationException("LoansUrl required"));
    client.Timeout = TimeSpan.FromSeconds(5);
});

var app = builder.Build();
app.UseHttpLogging();
var exportDirectory = Path.GetFullPath(app.Configuration["ExportDirectory"] ?? "exports");
Directory.CreateDirectory(exportDirectory);

app.MapGet("/health", () => Results.Ok(new { service = "reports" }));
app.MapPost("/reports", async (IHttpClientFactory clients, CancellationToken ct) =>
{
    List<LoanSnapshot>? loans;
    try
    {
        loans = await clients.CreateClient("loans").GetFromJsonAsync<List<LoanSnapshot>>("loans", ct);
    }
    catch (Exception e) when (e is HttpRequestException or JsonException || (e is TaskCanceledException && !ct.IsCancellationRequested))
    {
        return Results.StatusCode(502);
    }

    if (loans is null)
        return Results.StatusCode(502);
    var id = Guid.NewGuid();
    var active = loans.Count(loan => loan.ReturnedAt is null);
    var report = new Report(id, loans.Count, active, loans.Count - active, DateTimeOffset.UtcNow);
    var reportPath = Path.Combine(exportDirectory, $"{id:D}.json");
    var staged = reportPath + ".tmp";
    try
    {
        await File.WriteAllTextAsync(staged, JsonSerializer.Serialize(report, new JsonSerializerOptions(JsonSerializerDefaults.Web)), ct);
        File.Move(staged, reportPath);
    }
    finally
    {
        File.Delete(staged);
    }
    return Results.Created($"/reports/{id:D}", report);
});

app.MapGet("/reports/{id:guid}", (Guid id) =>
{
    var path = Path.Combine(exportDirectory, $"{id:D}.json");
    return File.Exists(path) ? Results.File(path, "application/json") : Results.NotFound();
});

app.Run();

record LoanSnapshot(DateTime? ReturnedAt);
record Report(Guid Id, int Total, int Active, int Returned, DateTimeOffset CreatedAt);
