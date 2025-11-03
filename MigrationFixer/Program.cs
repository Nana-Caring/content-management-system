using Npgsql;

const string connectionString = "Host=dpg-d04muamuk2gs73drrong-a.oregon-postgres.render.com;Port=5432;Database=nana_caring_ts9m;Username=nana_caring_ts9m_user;Password=hJVRlGcNxewOc0PdKIWtyI7ou1zjXOoy;SSL Mode=Require;Trust Server Certificate=true";

Console.WriteLine("Fixing migration history...");

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    var sql = @"
        INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"") VALUES 
        ('20250701152720_AddUserProfileDetails', '9.0.0'),
        ('20250701191902_AddUserBlockingSystem', '9.0.0'),
        ('20250701200559_AddUserBlockingFields', '9.0.0'),
        ('20251101212100_AddProductsTable', '9.0.0')
        ON CONFLICT (""MigrationId"") DO NOTHING;
    ";
    
    using var command = new NpgsqlCommand(sql, connection);
    var result = await command.ExecuteNonQueryAsync();
    
    Console.WriteLine($"Migration history fixed. {result} records inserted.");
    
    // Check if Products table exists
    var checkTableSql = @"
        SELECT EXISTS (
            SELECT FROM information_schema.tables 
            WHERE table_schema = 'public' 
            AND table_name = 'Products'
        );
    ";
    
    using var checkCommand = new NpgsqlCommand(checkTableSql, connection);
    var tableExists = (bool)await checkCommand.ExecuteScalarAsync();
    
    Console.WriteLine($"Products table exists: {tableExists}");
    
    if (!tableExists)
    {
        Console.WriteLine("Creating Products table...");
        var createTableSql = @"
            CREATE TABLE ""Products"" (
                ""Id"" serial PRIMARY KEY,
                ""Name"" text NOT NULL,
                ""ApiLink"" text NOT NULL,
                ""Description"" text NOT NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL
            );
        ";
        
        using var createCommand = new NpgsqlCommand(createTableSql, connection);
        await createCommand.ExecuteNonQueryAsync();
        Console.WriteLine("Products table created successfully.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

Console.WriteLine("Done.");
