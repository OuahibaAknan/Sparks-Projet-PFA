using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sparks.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketResolvedAtUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAtUtc",
                table: "Tickets",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResolvedAtUtc",
                table: "Tickets");
        }
    }
}
