using Npgsql;

static class CatalogStore
{
    public static async Task<List<Equipment>> Read(NpgsqlDataSource db, int? id, CancellationToken ct)
    {
        await using var command = db.CreateCommand(id is null
            ? "SELECT id, code, name FROM equipment ORDER BY id"
            : "SELECT id, code, name FROM equipment WHERE id = $1");
        if (id.HasValue)
            command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = id.Value });
        await using var reader = await command.ExecuteReaderAsync(ct);
        var items = new List<Equipment>();
        while (await reader.ReadAsync(ct))
            items.Add(Map(reader));
        return items;
    }

    public static async Task<Equipment?> Save(NpgsqlDataSource db, int? id, string code, string name, CancellationToken ct)
    {
        await using var command = db.CreateCommand(id is null
            ? "INSERT INTO equipment (code, name) VALUES ($1, $2) RETURNING id, code, name"
            : "UPDATE equipment SET code = $1, name = $2 WHERE id = $3 RETURNING id, code, name");
        command.Parameters.Add(new NpgsqlParameter<string> { TypedValue = code });
        command.Parameters.Add(new NpgsqlParameter<string> { TypedValue = name });
        if (id.HasValue)
            command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = id.Value });
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    private static Equipment Map(NpgsqlDataReader reader) => new(reader.GetInt32(0), reader.GetString(1), reader.GetString(2));
}
