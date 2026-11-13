using Npgsql;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpLogging();
var connection = builder.Configuration.GetConnectionString("Database");
if (!string.IsNullOrWhiteSpace(connection))
    builder.Services.AddSingleton(NpgsqlDataSource.Create(connection));

var app = builder.Build();
app.UseHttpLogging();
var database = app.Services.GetService<NpgsqlDataSource>();
var sample = new Equipment(1, "LAB-001", "Мультиметр");

app.MapGet("/health", () => Results.Ok(new { service = "catalog" }));
app.MapGet("/equipment", async (CancellationToken ct) =>
    Results.Ok(database is null ? [sample] : await CatalogStore.Read(database, null, ct)));

app.MapGet("/equipment/{id:int}", async (int id, CancellationToken ct) =>
{
    if (database is null)
        return id == sample.Id ? Results.Ok(sample) : Results.NotFound();
    var rows = await CatalogStore.Read(database, id, ct);
    return rows.Count == 0 ? Results.NotFound() : Results.Ok(rows[0]);
});

app.MapPost("/equipment", (EquipmentInput input, CancellationToken ct) => Save(null, input, ct));
app.MapPut("/equipment/{id:int}", (int id, EquipmentInput input, CancellationToken ct) => Save(id, input, ct));
app.MapDelete("/equipment/{id:int}", async (int id, CancellationToken ct) =>
{
    if (database is null)
        return Results.StatusCode(503);
    await using var command = database.CreateCommand("DELETE FROM equipment WHERE id = $1");
    command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = id });
    try
    {
        return await command.ExecuteNonQueryAsync(ct) == 0 ? Results.NotFound() : Results.NoContent();
    }
    catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.ForeignKeyViolation)
    {
        return Results.Conflict(new { error = "У прибора есть записи о выдаче" });
    }
});

app.Run();

async Task<IResult> Save(int? id, EquipmentInput input, CancellationToken ct)
{
    if (database is null)
        return Results.StatusCode(503);
    var code = input.Code?.Trim().ToUpperInvariant();
    var name = input.Name?.Trim();
    if (string.IsNullOrEmpty(code) || code.Length > 40 || string.IsNullOrEmpty(name) || name.Length > 120)
        return Results.BadRequest(new { error = "Укажите номер до 40 и название до 120 символов" });
    try
    {
        var item = await CatalogStore.Save(database, id, code, name, ct);
        if (item is null)
            return Results.NotFound();
        return id is null ? Results.Created($"/equipment/{item.Id}", item) : Results.Ok(item);
    }
    catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.UniqueViolation)
    {
        return Results.Conflict(new { error = "Такой инвентарный номер уже существует" });
    }
}

record Equipment(int Id, string Code, string Name);
record EquipmentInput(string? Code, string? Name);
