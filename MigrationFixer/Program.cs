using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Npgsql;

// Build configuration from appsettings.json, user secrets, and environment variables
var builder = Host.CreateApplicationBuilder(args);
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddUserSecrets(typeof(Program).Assembly, optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();

var configuration = builder.Configuration;

// Precedence: Env var -> ConnectionStrings:MigrationFixer -> ConnectionStrings:DefaultConnection
var connectionString = Environment.GetEnvironmentVariable("MIGRATIONFIXER_CONNECTION_STRING");
if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = configuration.GetConnectionString("MigrationFixer")
        ?? configuration.GetConnectionString("DefaultConnection")
        ?? configuration["ConnectionStrings:MigrationFixer"]
        ?? configuration["ConnectionStrings:DefaultConnection"];
}

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.WriteLine("Error: Missing database connection string.\n" +
        "Set one of the following:\n" +
        "  - Env var MIGRATIONFIXER_CONNECTION_STRING\n" +
        "  - User secret ConnectionStrings:MigrationFixer (recommended)\n" +
        "  - appsettings.json ConnectionStrings:MigrationFixer (for non-secret dev only)\n\n" +
        "Examples (PowerShell):\n" +
        "  $env:MIGRATIONFIXER_CONNECTION_STRING=\"Host=...;Port=5432;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true\"\n" +
        "  dotnet user-secrets set \"ConnectionStrings:MigrationFixer\" \"Host=...;Port=5432;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true\" --project \"c:\\Users\\princ\\OneDrive\\Documents\\TOBUN\\NANA Project\\CMS\\MigrationFixer\\MigrationFixer.csproj\"\n");
    return;
}

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
    var scalar = await checkCommand.ExecuteScalarAsync();
    var tableExists = scalar is bool b && b;
    
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
