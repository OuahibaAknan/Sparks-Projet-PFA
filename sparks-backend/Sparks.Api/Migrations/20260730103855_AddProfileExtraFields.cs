using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sparks.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileExtraFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AltenId",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailStellantis",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Languages",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AltenId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EmailStellantis",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Languages",
                table: "AspNetUsers");
        }
    }
}
