using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeDepartmentID : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlanDetails_Departments_DepartmentId",
                table: "PlanDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_RecruitmentPlans_Enterprises_EnterpriseId",
                table: "RecruitmentPlans");

            migrationBuilder.DropIndex(
                name: "IX_PlanDetails_DepartmentId",
                table: "PlanDetails");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "PlanDetails");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "RecruitmentPlans",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Draft",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Pending");

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "RecruitmentPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_RecruitmentPlans_DepartmentId",
                table: "RecruitmentPlans",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_RecruitmentPlans_Departments_DepartmentId",
                table: "RecruitmentPlans",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RecruitmentPlans_Enterprises_EnterpriseId",
                table: "RecruitmentPlans",
                column: "EnterpriseId",
                principalTable: "Enterprises",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecruitmentPlans_Departments_DepartmentId",
                table: "RecruitmentPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_RecruitmentPlans_Enterprises_EnterpriseId",
                table: "RecruitmentPlans");

            migrationBuilder.DropIndex(
                name: "IX_RecruitmentPlans_DepartmentId",
                table: "RecruitmentPlans");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "RecruitmentPlans");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "RecruitmentPlans",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Pending",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Draft");

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "PlanDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PlanDetails_DepartmentId",
                table: "PlanDetails",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlanDetails_Departments_DepartmentId",
                table: "PlanDetails",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RecruitmentPlans_Enterprises_EnterpriseId",
                table: "RecruitmentPlans",
                column: "EnterpriseId",
                principalTable: "Enterprises",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
