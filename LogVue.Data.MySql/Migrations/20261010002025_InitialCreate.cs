using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace LogVue.Data.MySql.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The generated ALTER DATABASE is removed: LogVue often shares the host application's
            // database, and changing its default charset is not LogVue's call. Each table below
            // declares utf8mb4 itself.

            migrationBuilder.CreateTable(
                name: "LogVueEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "longtext", nullable: false),
                    MessageTemplate = table.Column<string>(type: "longtext", nullable: false),
                    Exception = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogVueEntries", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LogVueSavedQueries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    Query = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogVueSavedQueries", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LogVueEntryProperties",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    LogEntryId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    Value = table.Column<string>(type: "longtext", nullable: true)
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
                })
                .Annotation("MySQL:Charset", "utf8mb4");

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
