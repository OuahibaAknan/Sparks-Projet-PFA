using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sparks.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationTicketLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TicketId",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TicketId",
                table: "Notifications",
                column: "TicketId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Tickets_TicketId",
                table: "Notifications",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Tickets_TicketId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_TicketId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "TicketId",
                table: "Notifications");
        }
    }
}
