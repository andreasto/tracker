using LinqToDB;
using LinqToDB.Data;

namespace tracker.App.Configuration;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabase(string connectionString, ILogger logger)
    {
        try
        {
            logger.LogInformation("Initializing Akka.Persistence.Sql database tables...");

            // Create the tables using Akka.Persistence.Sql's schema
            await using var db = new DataConnection(
                ProviderName.PostgreSQL15,
                connectionString);

            // Create journal table
            await db.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS journal (
                    ordering BIGSERIAL NOT NULL PRIMARY KEY,
                    persistence_id VARCHAR(255) NOT NULL,
                    sequence_number BIGINT NOT NULL,
                    is_deleted BOOLEAN NOT NULL DEFAULT false,
                    payload BYTEA NOT NULL,
                    manifest VARCHAR(500) NOT NULL,
                    timestamp BIGINT NOT NULL,
                    tags VARCHAR(100) NULL,
                    serializer_id INTEGER NULL,
                    CONSTRAINT journal_uq UNIQUE (persistence_id, sequence_number)
                );
                
                CREATE INDEX IF NOT EXISTS journal_persistence_id_idx ON journal(persistence_id);
                CREATE INDEX IF NOT EXISTS journal_tags_idx ON journal USING GIN(string_to_array(tags, ','));
            ");

            // Create snapshot table
            await db.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS snapshot (
                    persistence_id VARCHAR(255) NOT NULL,
                    sequence_number BIGINT NOT NULL,
                    created BIGINT NOT NULL,
                    snapshot BYTEA NOT NULL,
                    manifest VARCHAR(500) NOT NULL,
                    serializer_id INTEGER NULL,
                    CONSTRAINT snapshot_pk PRIMARY KEY (persistence_id, sequence_number)
                );
            ");

            // Create metadata table
            await db.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS metadata (
                    persistence_id VARCHAR(255) NOT NULL,
                    sequence_number BIGINT NOT NULL,
                    CONSTRAINT metadata_pk PRIMARY KEY (persistence_id)
                );
            ");

            logger.LogInformation("Akka.Persistence.Sql database tables initialized successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize database tables");
            throw;
        }
    }
}
