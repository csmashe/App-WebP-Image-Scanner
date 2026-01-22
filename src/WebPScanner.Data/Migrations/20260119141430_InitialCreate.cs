using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebPScanner.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AggregateStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TotalScans = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalPagesCrawled = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalImagesFound = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalOriginalSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalEstimatedWebPSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalSavingsPercentSum = table.Column<double>(type: "REAL", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AggregateStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScanJobs",
                columns: table => new
                {
                    ScanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TargetUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    QueuePosition = table.Column<int>(type: "INTEGER", nullable: false),
                    SubmitterIp = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    SubmissionCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PagesScanned = table.Column<int>(type: "INTEGER", nullable: false),
                    PagesDiscovered = table.Column<int>(type: "INTEGER", nullable: false),
                    NonWebPImagesFound = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: true),
                    PriorityScore = table.Column<long>(type: "INTEGER", nullable: false),
                    ConvertToWebP = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanJobs", x => x.ScanId);
                });

            migrationBuilder.CreateTable(
                name: "AggregateCategoryStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AggregateStatsId = table.Column<int>(type: "INTEGER", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalSavingsBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    SavingsPercentSum = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AggregateCategoryStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AggregateCategoryStats_AggregateStats_AggregateStatsId",
                        column: x => x.AggregateStatsId,
                        principalTable: "AggregateStats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AggregateImageTypeStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AggregateStatsId = table.Column<int>(type: "INTEGER", nullable: false),
                    MimeType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    PotentialSavingsBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    SavingsPercentSum = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AggregateImageTypeStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AggregateImageTypeStats_AggregateStats_AggregateStatsId",
                        column: x => x.AggregateStatsId,
                        principalTable: "AggregateStats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConvertedImageZips",
                columns: table => new
                {
                    DownloadId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScanJobId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    ImageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConvertedImageZips", x => x.DownloadId);
                    table.ForeignKey(
                        name: "FK_ConvertedImageZips_ScanJobs_ScanJobId",
                        column: x => x.ScanJobId,
                        principalTable: "ScanJobs",
                        principalColumn: "ScanId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CrawlCheckpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScanJobId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VisitedUrlsJson = table.Column<string>(type: "TEXT", nullable: false),
                    PendingUrlsJson = table.Column<string>(type: "TEXT", nullable: false),
                    PagesVisited = table.Column<int>(type: "INTEGER", nullable: false),
                    PagesDiscovered = table.Column<int>(type: "INTEGER", nullable: false),
                    NonWebPImagesFound = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrawlCheckpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrawlCheckpoints_ScanJobs_ScanJobId",
                        column: x => x.ScanJobId,
                        principalTable: "ScanJobs",
                        principalColumn: "ScanId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiscoveredImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScanJobId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    PageUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    MimeType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: true),
                    Height = table.Column<int>(type: "INTEGER", nullable: true),
                    EstimatedWebPSize = table.Column<long>(type: "INTEGER", nullable: false),
                    PotentialSavingsPercent = table.Column<double>(type: "REAL", nullable: false),
                    DiscoveredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PageUrlsJson = table.Column<string>(type: "TEXT", nullable: false),
                    PageCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscoveredImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscoveredImages_ScanJobs_ScanJobId",
                        column: x => x.ScanJobId,
                        principalTable: "ScanJobs",
                        principalColumn: "ScanId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AggregateCategoryStats_AggregateStatsId",
                table: "AggregateCategoryStats",
                column: "AggregateStatsId");

            migrationBuilder.CreateIndex(
                name: "IX_AggregateCategoryStats_Category",
                table: "AggregateCategoryStats",
                column: "Category",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AggregateImageTypeStats_AggregateStatsId",
                table: "AggregateImageTypeStats",
                column: "AggregateStatsId");

            migrationBuilder.CreateIndex(
                name: "IX_AggregateImageTypeStats_MimeType",
                table: "AggregateImageTypeStats",
                column: "MimeType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConvertedImageZips_ExpiresAt",
                table: "ConvertedImageZips",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ConvertedImageZips_ScanJobId",
                table: "ConvertedImageZips",
                column: "ScanJobId");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlCheckpoints_ScanJobId",
                table: "CrawlCheckpoints",
                column: "ScanJobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveredImages_ScanJobId",
                table: "DiscoveredImages",
                column: "ScanJobId");

            migrationBuilder.CreateIndex(
                name: "IX_ScanJobs_CreatedAt",
                table: "ScanJobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ScanJobs_PriorityScore",
                table: "ScanJobs",
                column: "PriorityScore");

            migrationBuilder.CreateIndex(
                name: "IX_ScanJobs_Status",
                table: "ScanJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ScanJobs_SubmitterIp",
                table: "ScanJobs",
                column: "SubmitterIp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AggregateCategoryStats");

            migrationBuilder.DropTable(
                name: "AggregateImageTypeStats");

            migrationBuilder.DropTable(
                name: "ConvertedImageZips");

            migrationBuilder.DropTable(
                name: "CrawlCheckpoints");

            migrationBuilder.DropTable(
                name: "DiscoveredImages");

            migrationBuilder.DropTable(
                name: "AggregateStats");

            migrationBuilder.DropTable(
                name: "ScanJobs");
        }
    }
}
