using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalCandidateFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ResponseToken",
                table: "Offers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TokenExpiresAt",
                table: "Offers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CandidateId",
                table: "Applications",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "ExternalCandidateEmail",
                table: "Applications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalCandidateName",
                table: "Applications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalCandidatePhone",
                table: "Applications",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalResumeUrl",
                table: "Applications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsExternal",
                table: "Applications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Offers_ResponseToken",
                table: "Offers",
                column: "ResponseToken",
                unique: true,
                filter: "[ResponseToken] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Offers_ResponseToken",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "ResponseToken",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "TokenExpiresAt",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "ExternalCandidateEmail",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ExternalCandidateName",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ExternalCandidatePhone",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ExternalResumeUrl",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "IsExternal",
                table: "Applications");

            migrationBuilder.AlterColumn<Guid>(
                name: "CandidateId",
                table: "Applications",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
