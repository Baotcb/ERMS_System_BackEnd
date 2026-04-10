using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackReplyAnonymous : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
    name: "WorkshopConfirmations");

            migrationBuilder.AddColumn<string>(
                name: "ContentManagerEmail",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAnonymous",
                table: "CourseFeedbackReplies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReplyByAvatarUrl",
                table: "CourseFeedbackReplies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReplyByName",
                table: "CourseFeedbackReplies",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentManagerEmail",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "IsAnonymous",
                table: "CourseFeedbackReplies");

            migrationBuilder.DropColumn(
                name: "ReplyByAvatarUrl",
                table: "CourseFeedbackReplies");

            migrationBuilder.DropColumn(
                name: "ReplyByName",
                table: "CourseFeedbackReplies");

            migrationBuilder.CreateTable(
    name: "WorkshopConfirmations",
    columns: table => new
    {
        Id = table.Column<int>(nullable: false)
            .Annotation("SqlServer:Identity", "1, 1"),
        // thêm các column cũ của bạn ở đây
    },
    constraints: table =>
    {
        table.PrimaryKey("PK_WorkshopConfirmations", x => x.Id);
    });
        }
    }
}
