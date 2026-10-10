using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LogVue.Data.Npgsql.Migrations
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
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    MessageTemplate = table.Column<string>(type: "text", nullable: false),
                    Exception = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogVueEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogVueSavedQueries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Query = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogVueSavedQueries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogVueEntryProperties",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LogEntryId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
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
