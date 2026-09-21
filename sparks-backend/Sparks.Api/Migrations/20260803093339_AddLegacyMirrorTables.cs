using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sparks.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLegacyMirrorTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Summary",
                table: "Tickets",
                newName: "TicketSummary");

            migrationBuilder.RenameColumn(
                name: "Priority",
                table: "Tickets",
                newName: "Gravity");

            migrationBuilder.RenameColumn(
                name: "EnglishAccepted",
                table: "Tickets",
                newName: "IsEnglishAccepted");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Tickets",
                newName: "TicketId");

            migrationBuilder.AddColumn<Guid>(
                name: "ApplicationId",
                table: "Tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Tickets",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateDateKZ",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnglishTicketSummary",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OpeningDate",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Organization",
                table: "Tickets",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "Tickets",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudyCell",
                table: "Tickets",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Applications",
                columns: table => new
                {
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PerimeterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.ApplicationId);
                });

            migrationBuilder.CreateTable(
                name: "UserTickets",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CannotSolve = table.Column<bool>(type: "bit", nullable: false),
                    AssignmentType = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsMarkedDone = table.Column<bool>(type: "bit", nullable: false),
                    SpentTime = table.Column<int>(type: "int", nullable: true),
                    ToGamify = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTickets", x => new { x.UserId, x.TicketId });
                    table.ForeignKey(
                        name: "FK_UserTickets_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserTickets_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "TicketId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserApplication",
                columns: table => new
                {
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserDefinedPriority = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserApplication", x => new { x.ApplicationId, x.UserId });
                    table.ForeignKey(
                        name: "FK_UserApplication_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "ApplicationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserApplication_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserApplication_UserId",
                table: "UserApplication",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTickets_TicketId",
                table: "UserTickets",
                column: "TicketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserApplication");

            migrationBuilder.DropTable(
                name: "UserTickets");

            migrationBuilder.DropTable(
                name: "Applications");

            migrationBuilder.DropColumn(
                name: "ApplicationId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "CreateDateKZ",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "EnglishTicketSummary",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "OpeningDate",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Organization",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Origin",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "StudyCell",
                table: "Tickets");

            migrationBuilder.RenameColumn(
                name: "TicketSummary",
                table: "Tickets",
                newName: "Summary");

            migrationBuilder.RenameColumn(
                name: "IsEnglishAccepted",
                table: "Tickets",
                newName: "EnglishAccepted");

            migrationBuilder.RenameColumn(
                name: "Gravity",
                table: "Tickets",
                newName: "Priority");

            migrationBuilder.RenameColumn(
                name: "TicketId",
                table: "Tickets",
                newName: "Id");
        }
    }
}
