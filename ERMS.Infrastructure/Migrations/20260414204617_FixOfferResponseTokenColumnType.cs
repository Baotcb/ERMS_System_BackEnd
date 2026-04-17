using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixOfferResponseTokenColumnType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('Offers', 'ResponseToken') IS NULL
                BEGIN
                    ALTER TABLE [Offers] ADD [ResponseToken] nvarchar(450) NULL;
                END
                ELSE
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM sys.indexes
                        WHERE name = N'IX_Offers_ResponseToken'
                          AND object_id = OBJECT_ID(N'[Offers]')
                    )
                    BEGIN
                        DROP INDEX [IX_Offers_ResponseToken] ON [Offers];
                    END;

                    IF EXISTS (
                        SELECT 1
                        FROM sys.columns c
                        JOIN sys.types t ON c.user_type_id = t.user_type_id
                        WHERE c.object_id = OBJECT_ID(N'[Offers]')
                          AND c.name = N'ResponseToken'
                          AND t.name = N'uniqueidentifier'
                    )
                    BEGIN
                        ALTER TABLE [Offers] ALTER COLUMN [ResponseToken] nvarchar(450) NULL;
                    END;
                END;
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('Offers', 'ResponseToken') IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_Offers_ResponseToken'
                      AND object_id = OBJECT_ID(N'[Offers]')
                )
                BEGIN
                    CREATE UNIQUE INDEX [IX_Offers_ResponseToken]
                        ON [Offers] ([ResponseToken])
                        WHERE [ResponseToken] IS NOT NULL;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally no-op: reverting nvarchar token column back to uniqueidentifier is unsafe
            // once non-GUID tokens exist in production data.
        }
    }
}
