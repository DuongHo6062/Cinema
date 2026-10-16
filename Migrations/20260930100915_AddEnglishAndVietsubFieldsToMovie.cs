using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MovieStreaming.Migrations
{
    /// <inheritdoc />
    public partial class AddEnglishAndVietsubFieldsToMovie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVietsub",
                table: "Movies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedAt",
                table: "Movies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalLanguage",
                table: "Movies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "IsVietsub", "LastSyncedAt", "OriginalLanguage" },
                values: new object[] { true, null, "English" });

            migrationBuilder.CreateIndex(
                name: "IX_Movies_Country",
                table: "Movies",
                column: "Country");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_IsVietsub",
                table: "Movies",
                column: "IsVietsub");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_OriginalLanguage",
                table: "Movies",
                column: "OriginalLanguage");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Movies_Country",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Movies_IsVietsub",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Movies_OriginalLanguage",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "IsVietsub",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "LastSyncedAt",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "OriginalLanguage",
                table: "Movies");
        }
    }
}
