using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedProPlanAndSetSubscriptionHistoryCurrencyVnd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "SubscriptionHistories",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "VND",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM SubscriptionPlans WHERE PlanCode = 'PRO')
                BEGIN
                    INSERT INTO SubscriptionPlans
                    (
                        Id,
                        PlanName,
                        PlanCode,
                        Description,
                        MaxUsers,
                        MaxJobPostings,
                        MaxCourses,
                        PriceMonthly,
                        PriceYearly,
                        Features,
                        IsActive,
                        DisplayOrder,
                        IsDeleted,
                        CreatedAt
                    )
                    VALUES
                    (
                        '8D9F8D8A-92A2-4F4D-A0F3-3D6C5C8CFCD3',
                        N'Pro Plan',
                        'PRO',
                        N'Gói chuyên nghiệp: AI chấm điểm CV, tuyển dụng & đào tạo nâng cao',
                        50,
                        20,
                        25,
                        5000,
                        0,
                        '{"aiCvScreening": true, "aiJdSuggestion": true}',
                        1,
                        2,
                        0,
                        GETUTCDATE()
                    );
                END;

                UPDATE SubscriptionPlans
                SET Features = '{"aiCvScreening": false, "aiJdSuggestion": false}',
                    Description = N'Gói miễn phí với tính năng cơ bản',
                    DisplayOrder = 1,
                    PriceYearly = 0
                WHERE PlanCode = 'FREE'
                  AND (Features IS NULL OR Features = '');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM SubscriptionPlans
                WHERE PlanCode = 'PRO';

                UPDATE SubscriptionPlans
                SET Features = NULL,
                    Description = NULL,
                    DisplayOrder = 0,
                    PriceYearly = 0
                WHERE PlanCode = 'FREE';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "SubscriptionHistories",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldDefaultValue: "VND");
        }
    }
}
