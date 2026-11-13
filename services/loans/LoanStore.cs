using Npgsql;

sealed class LoanStore(NpgsqlDataSource db)
{
    private const string Columns = "id, equipment_id, borrower, issued_at, returned_at";

    public async Task<List<Loan>> List(CancellationToken ct)
    {
        await using var cmd = db.CreateCommand($"SELECT {Columns} FROM loans ORDER BY id");
        await using var rows = await cmd.ExecuteReaderAsync(ct);
        var loans = new List<Loan>();
        while (await rows.ReadAsync(ct))
            loans.Add(Map(rows));
        return loans;
    }

    public Task<Loan?> Find(int id, CancellationToken ct) =>
        ReadOne($"SELECT {Columns} FROM loans WHERE id = $1", id, ct);

    public Task<Loan?> Return(int id, CancellationToken ct) =>
        ReadOne($"UPDATE loans SET returned_at = coalesce(returned_at, now()) WHERE id = $1 RETURNING {Columns}", id, ct);

    public async Task<Loan> Create(int equipmentId, string borrower, CancellationToken ct)
    {
        await using var cmd = db.CreateCommand($"INSERT INTO loans (equipment_id, borrower) VALUES ($1, $2) RETURNING {Columns}");
        cmd.Parameters.Add(new NpgsqlParameter<int> { TypedValue = equipmentId });
        cmd.Parameters.Add(new NpgsqlParameter<string> { TypedValue = borrower });
        await using var rows = await cmd.ExecuteReaderAsync(ct);
        await rows.ReadAsync(ct);
        return Map(rows);
    }

    private async Task<Loan?> ReadOne(string sql, int id, CancellationToken ct)
    {
        await using var cmd = db.CreateCommand(sql);
        cmd.Parameters.Add(new NpgsqlParameter<int> { TypedValue = id });
        await using var rows = await cmd.ExecuteReaderAsync(ct);
        return await rows.ReadAsync(ct) ? Map(rows) : null;
    }

    private static Loan Map(NpgsqlDataReader r) =>
        new(r.GetInt32(0), r.GetInt32(1), r.GetString(2), r.GetDateTime(3), r.IsDBNull(4) ? null : r.GetDateTime(4));
}
