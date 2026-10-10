using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogVue.Data.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogVueEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    MessageTemplate = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    Exception = table.Column<string>(type: "TEXT", nullable: true, collation: "NOCASE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogVueEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogVueSavedQueries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false, collation: "NOCASE"),
                    Query = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogVueSavedQueries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogVueEntryProperties",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LogEntryId = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false, collation: "NOCASE"),
                    Value = table.Column<string>(type: "TEXT", nullable: true, collation: "NOCASE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogVueEntryProperties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogVueEntryProperties_LogVueEntries_LogEntryId",
                        column: x => x.LogEntryId,
                        principalTable: "LogVueEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LogVueEntries_Level_Id",
                table: "LogVueEntries",
                columns: new[] { "Level", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_LogVueEntries_Timestamp",
                table: "LogVueEntries",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_LogVueEntryProperties_LogEntryId_Name",
                table: "LogVueEntryProperties",
                columns: new[] { "LogEntryId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_LogVueSavedQueries_Name",
                table: "LogVueSavedQueries",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogVueEntryProperties");

            migrationBuilder.DropTable(
                name: "LogVueSavedQueries");

            migrationBuilder.DropTable(
                name: "LogVueEntries");
        }
    }
}
