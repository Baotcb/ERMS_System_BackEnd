using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addTrainerEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Employees_TrainerId",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Courses_TrainerId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "TrainerId",
                table: "Courses");

            migrationBuilder.AddColumn<string>(
                name: "TrainerEmail",
                table: "Courses",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrainerEmail",
                table: "Courses");

            migrationBuilder.AddColumn<Guid>(
                name: "TrainerId",
                table: "Courses",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Courses_TrainerId",
                table: "Courses",
                column: "TrainerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Employees_TrainerId",
                table: "Courses",
                column: "TrainerId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
