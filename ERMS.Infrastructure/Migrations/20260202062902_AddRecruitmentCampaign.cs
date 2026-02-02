using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecruitmentCampaign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "RecruitmentPlans",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "RecruitmentPlans",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<Guid>(
                name: "CampaignId",
                table: "RecruitmentPlans",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "RecruitmentCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnterpriseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CampaignCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FiscalYear = table.Column<int>(type: "int", nullable: false),
                    FiscalQuarter = table.Column<byte>(type: "tinyint", nullable: true),
                    SubmissionStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmissionEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TargetHireStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TargetHireEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalBudgetCeiling = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxTotalPositions = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Draft"),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecruitmentCampaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecruitmentCampaigns_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecruitmentCampaigns_Enterprises_EnterpriseId",
                        column: x => x.EnterpriseId,
                        principalTable: "Enterprises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecruitmentPlans_CampaignId",
                table: "RecruitmentPlans",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_RecruitmentCampaigns_CreatedById",
                table: "RecruitmentCampaigns",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "UQ_RC_Enterprise_Code",
                table: "RecruitmentCampaigns",
                columns: new[] { "EnterpriseId", "CampaignCode" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RecruitmentPlans_RecruitmentCampaigns_CampaignId",
                table: "RecruitmentPlans",
                column: "CampaignId",
                principalTable: "RecruitmentCampaigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecruitmentPlans_RecruitmentCampaigns_CampaignId",
                table: "RecruitmentPlans");

            migrationBuilder.DropTable(
                name: "RecruitmentCampaigns");

            migrationBuilder.DropIndex(
                name: "IX_RecruitmentPlans_CampaignId",
                table: "RecruitmentPlans");

            migrationBuilder.DropColumn(
                name: "CampaignId",
                table: "RecruitmentPlans");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "RecruitmentPlans",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "RecruitmentPlans",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
