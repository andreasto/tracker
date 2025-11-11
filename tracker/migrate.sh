#!/bin/bash

# Database Migration Helper Script
# This script helps you manage FluentMigrator migrations

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/src/tracker.App"

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

print_usage() {
    echo "Usage: ./migrate.sh [command] [options]"
    echo ""
    echo "Commands:"
    echo "  up              Run all pending migrations"
    echo "  down            Rollback the last migration"
    echo "  create <name>   Create a new migration with the given name"
    echo "  list            List all migrations and their status"
    echo "  version <ver>   Migrate to a specific version"
    echo "  help            Show this help message"
    echo ""
    echo "Examples:"
    echo "  ./migrate.sh up"
    echo "  ./migrate.sh create AddUserPreferences"
    echo "  ./migrate.sh down"
    echo "  ./migrate.sh version 20241111001"
}

get_timestamp() {
    date +"%Y%m%d%H%M"
}

create_migration() {
    local migration_name=$1
    
    if [ -z "$migration_name" ]; then
        echo -e "${RED}Error: Migration name is required${NC}"
        echo "Usage: ./migrate.sh create <MigrationName>"
        exit 1
    fi
    
    local timestamp=$(get_timestamp)
    local filename="${timestamp}_${migration_name}.cs"
    local filepath="$PROJECT_DIR/Migrations/$filename"
    
    # Create Migrations directory if it doesn't exist
    mkdir -p "$PROJECT_DIR/Migrations"
    
    # Create migration file
    cat > "$filepath" << EOF
using FluentMigrator;

namespace tracker.App.Migrations;

/// <summary>
/// ${migration_name}
/// </summary>
[Migration(${timestamp})]
public class ${migration_name} : Migration
{
    public override void Up()
    {
        // TODO: Implement migration
        // Example:
        // Create.Table("table_name")
        //     .WithColumn("id").AsInt32().PrimaryKey().Identity()
        //     .WithColumn("name").AsString(100).NotNullable();
    }

    public override void Down()
    {
        // TODO: Implement rollback
        // Example:
        // Delete.Table("table_name");
    }
}
EOF
    
    echo -e "${GREEN}✓ Created migration: $filename${NC}"
    echo -e "${YELLOW}  Path: $filepath${NC}"
    echo ""
    echo "Edit the migration file to implement your changes."
}

run_migrations_up() {
    echo -e "${YELLOW}Running all pending migrations...${NC}"
    cd "$PROJECT_DIR"
    dotnet run --no-build -- migrate up
    echo -e "${GREEN}✓ Migrations completed${NC}"
}

run_migrations_down() {
    echo -e "${YELLOW}Rolling back last migration...${NC}"
    cd "$PROJECT_DIR"
    dotnet run --no-build -- migrate down
    echo -e "${GREEN}✓ Rollback completed${NC}"
}

list_migrations() {
    echo -e "${YELLOW}Listing all migrations...${NC}"
    cd "$PROJECT_DIR"
    dotnet run --no-build -- migrate list
}

migrate_to_version() {
    local version=$1
    
    if [ -z "$version" ]; then
        echo -e "${RED}Error: Version number is required${NC}"
        echo "Usage: ./migrate.sh version <version_number>"
        exit 1
    fi
    
    echo -e "${YELLOW}Migrating to version $version...${NC}"
    cd "$PROJECT_DIR"
    dotnet run --no-build -- migrate version "$version"
    echo -e "${GREEN}✓ Migration to version $version completed${NC}"
}

# Main script
case "${1:-help}" in
    up)
        run_migrations_up
        ;;
    down)
        run_migrations_down
        ;;
    create)
        create_migration "$2"
        ;;
    list)
        list_migrations
        ;;
    version)
        migrate_to_version "$2"
        ;;
    help|--help|-h)
        print_usage
        ;;
    *)
        echo -e "${RED}Unknown command: $1${NC}"
        echo ""
        print_usage
        exit 1
        ;;
esac

