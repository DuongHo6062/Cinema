using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MovieStreaming.Migrations
{
    /// <inheritdoc />
    public partial class AddMovieSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Movies_CreatedAt",
                table: "Movies",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_MovieType",
                table: "Movies",
                column: "MovieType");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_ReleaseYear",
                table: "Movies",
                column: "ReleaseYear");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Movies_CreatedAt",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Movies_MovieType",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Movies_ReleaseYear",
                table: "Movies");
        }
    }
}
