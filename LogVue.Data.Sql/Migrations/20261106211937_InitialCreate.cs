using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogVue.Data.Sql.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "logvue");

            migrationBuilder.CreateTable(
                name: "Entries",
                schema: "logvue",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MessageTemplate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Exception = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedQueries",
                schema: "logvue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Query = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedQueries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EntryProperties",
                schema: "logvue",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LogEntryId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntryProperties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntryProperties_Entries_LogEntryId",
                        column: x => x.LogEntryId,
                        principalSchema: "logvue",
                        principalTable: "Entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Entries_Level_Id",
                schema: "logvue",
                table: "Entries",
                columns: new[] { "Level", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Entries_Timestamp",
                schema: "logvue",
                table: "Entries",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_EntryProperties_LogEntryId_Name",
                schema: "logvue",
                table: "EntryProperties",
                columns: new[] { "LogEntryId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedQueries_Name",
                schema: "logvue",
                table: "SavedQueries",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntryProperties",
                schema: "logvue");

            migrationBuilder.DropTable(
                name: "SavedQueries",
                schema: "logvue");

            migrationBuilder.DropTable(
                name: "Entries",
                schema: "logvue");
        }
    }
}
