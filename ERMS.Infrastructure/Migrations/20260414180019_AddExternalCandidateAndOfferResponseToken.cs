using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalCandidateAndOfferResponseToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('Offers', 'ResponseToken') IS NULL
                    ALTER TABLE [Offers] ADD [ResponseToken] nvarchar(450) NULL;
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('Offers', 'TokenExpiresAt') IS NULL
                    ALTER TABLE [Offers] ADD [TokenExpiresAt] datetime2 NULL;
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('Applications', 'ExternalCandidateId') IS NULL
                    ALTER TABLE [Applications] ADD [ExternalCandidateId] uniqueidentifier NULL;
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[ExternalCandidates]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [ExternalCandidates] (
                        [Id] uniqueidentifier NOT NULL,
                        [FullName] nvarchar(max) NOT NULL,
                        [Email] nvarchar(max) NOT NULL,
                        [PhoneNumber] nvarchar(max) NULL,
                        [CurrentPosition] nvarchar(max) NULL,
                        [Source] nvarchar(max) NOT NULL,
                        [EnterpriseId] uniqueidentifier NOT NULL,
                        [CreatedById] uniqueidentifier NOT NULL,
                        [IsDeleted] bit NOT NULL,
                        [DeletedAt] datetime2 NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [UpdatedAt] datetime2 NULL,
                        CONSTRAINT [PK_ExternalCandidates] PRIMARY KEY ([Id])
                    );
                END;
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_Offers_ResponseToken'
                      AND object_id = OBJECT_ID(N'[Offers]')
                )
                CREATE UNIQUE INDEX [IX_Offers_ResponseToken]
                    ON [Offers] ([ResponseToken])
                    WHERE [ResponseToken] IS NOT NULL;
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_Applications_ExternalCandidateId'
                      AND object_id = OBJECT_ID(N'[Applications]')
                )
                CREATE INDEX [IX_Applications_ExternalCandidateId]
                    ON [Applications] ([ExternalCandidateId]);
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = N'FK_Applications_ExternalCandidates_ExternalCandidateId'
                )
                ALTER TABLE [Applications]
                ADD CONSTRAINT [FK_Applications_ExternalCandidates_ExternalCandidateId]
                    FOREIGN KEY ([ExternalCandidateId])
                    REFERENCES [ExternalCandidates]([Id])
                    ON DELETE NO ACTION;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = N'FK_Applications_ExternalCandidates_ExternalCandidateId'
                )
                ALTER TABLE [Applications]
                DROP CONSTRAINT [FK_Applications_ExternalCandidates_ExternalCandidateId];
                """);

            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_Offers_ResponseToken'
                      AND object_id = OBJECT_ID(N'[Offers]')
                )
                DROP INDEX [IX_Offers_ResponseToken] ON [Offers];
                """);

            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_Applications_ExternalCandidateId'
                      AND object_id = OBJECT_ID(N'[Applications]')
                )
                DROP INDEX [IX_Applications_ExternalCandidateId] ON [Applications];
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[ExternalCandidates]', N'U') IS NOT NULL
                    DROP TABLE [ExternalCandidates];
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('Offers', 'ResponseToken') IS NOT NULL
                    ALTER TABLE [Offers] DROP COLUMN [ResponseToken];
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('Offers', 'TokenExpiresAt') IS NOT NULL
                    ALTER TABLE [Offers] DROP COLUMN [TokenExpiresAt];
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('Applications', 'ExternalCandidateId') IS NOT NULL
                    ALTER TABLE [Applications] DROP COLUMN [ExternalCandidateId];
                """);
        }
    }
}
