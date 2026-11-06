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
                name: "LogVue_Entries",
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
                    table.PrimaryKey("PK_LogVue_Entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogVue_SavedQueries",
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
                    table.PrimaryKey("PK_LogVue_SavedQueries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogVue_EntryProperties",
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
                    table.PrimaryKey("PK_LogVue_EntryProperties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogVue_EntryProperties_LogVue_Entries_LogEntryId",
                        column: x => x.LogEntryId,
                        principalTable: "LogVue_Entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LogVue_Entries_Level_Id",
                table: "LogVue_Entries",
                columns: new[] { "Level", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_LogVue_Entries_Timestamp",
                table: "LogVue_Entries",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_LogVue_EntryProperties_LogEntryId_Name",
                table: "LogVue_EntryProperties",
                columns: new[] { "LogEntryId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_LogVue_SavedQueries_Name",
                table: "LogVue_SavedQueries",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogVue_EntryProperties");

            migrationBuilder.DropTable(
                name: "LogVue_SavedQueries");

            migrationBuilder.DropTable(
                name: "LogVue_Entries");
        }
    }
}
