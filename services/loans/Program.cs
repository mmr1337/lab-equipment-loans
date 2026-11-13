using Npgsql;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpLogging();
builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(
    builder.Configuration.GetConnectionString("Database") ?? throw new InvalidOperationException("Database connection required")));
builder.Services.AddSingleton<LoanStore>();
builder.Services.AddHttpClient("catalog", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["CatalogUrl"] ?? throw new InvalidOperationException("CatalogUrl required"));
    client.Timeout = TimeSpan.FromSeconds(5);
});

var app = builder.Build();
app.UseHttpLogging();
app.MapGet("/health", () => Results.Ok(new { service = "loans" }));
app.MapGet("/loans", async (LoanStore store, CancellationToken ct) => Results.Ok(await store.List(ct)));
app.MapGet("/loans/{id:int}", async (int id, LoanStore store, CancellationToken ct) =>
    await store.Find(id, ct) is { } loan ? Results.Ok(loan) : Results.NotFound());
app.MapPost("/loans", async (LoanInput input, LoanStore store, IHttpClientFactory clients, CancellationToken ct) =>
{
    var borrower = input.Borrower?.Trim();
    if (input.EquipmentId < 1 || string.IsNullOrEmpty(borrower) || borrower.Length > 120)
        return Results.BadRequest(new { error = "Укажите прибор и имя получателя до 120 символов" });

    try
    {
        using var catalogResponse = await clients.CreateClient("catalog").GetAsync($"equipment/{input.EquipmentId}", ct);
        if (catalogResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            return Results.NotFound(new { error = "Прибор не найден" });
        if (!catalogResponse.IsSuccessStatusCode)
            return Results.StatusCode(502);

        var loan = await store.Create(input.EquipmentId, borrower, ct);
        return Results.Created($"/loans/{loan.Id}", loan);
    }
    catch (Exception e) when (e is HttpRequestException || (e is TaskCanceledException && !ct.IsCancellationRequested))
    {
        return Results.StatusCode(502);
    }
    catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.UniqueViolation)
    {
        return Results.Conflict(new { error = "Прибор уже выдан" });
    }
    catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.ForeignKeyViolation)
    {
        return Results.NotFound(new { error = "Прибор не найден" });
    }
});

app.MapPost("/loans/{id:int}/return", async (int id, LoanStore store, CancellationToken ct) =>
{
    var loan = await store.Return(id, ct);
    return loan is null ? Results.NotFound() : Results.Ok(loan);
});

app.Run();

record LoanInput(int EquipmentId, string? Borrower);
record Loan(int Id, int EquipmentId, string Borrower, DateTime IssuedAt, DateTime? ReturnedAt);
