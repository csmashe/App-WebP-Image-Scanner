using Microsoft.EntityFrameworkCore;
using WebPScanner.Core.Entities;

namespace WebPScanner.Data.Context;

public class WebPScannerDbContext : DbContext
{
    public WebPScannerDbContext(DbContextOptions<WebPScannerDbContext> options) : base(options)
    {
    }

    public DbSet<ScanJob> ScanJobs => Set<ScanJob>();
    public DbSet<DiscoveredImage> DiscoveredImages => Set<DiscoveredImage>();
    public DbSet<AggregateStats> AggregateStats => Set<AggregateStats>();
    public DbSet<AggregateCategoryStat> AggregateCategoryStats => Set<AggregateCategoryStat>();
    public DbSet<AggregateImageTypeStat> AggregateImageTypeStats => Set<AggregateImageTypeStat>();
    public DbSet<ConvertedImageZip> ConvertedImageZips => Set<ConvertedImageZip>();
    public DbSet<CrawlCheckpoint> CrawlCheckpoints => Set<CrawlCheckpoint>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ScanJob>(entity =>
        {
            entity.HasKey(e => e.ScanId);

            entity.Property(e => e.TargetUrl)
                .IsRequired()
                .HasMaxLength(2048);

            entity.Property(e => e.Email)
                .HasMaxLength(256);

            entity.Property(e => e.SubmitterIp)
                .HasMaxLength(45); // IPv6 max length

            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(4096);

            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.PriorityScore);
            entity.HasIndex(e => e.SubmitterIp);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<DiscoveredImage>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ImageUrl)
                .IsRequired()
                .HasMaxLength(2048);

            entity.Property(e => e.PageUrl)
                .IsRequired()
                .HasMaxLength(2048);

            entity.Property(e => e.MimeType)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasOne(e => e.ScanJob)
                .WithMany(s => s.DiscoveredImages)
                .HasForeignKey(e => e.ScanJobId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ScanJobId);
        });

        modelBuilder.Entity<AggregateStats>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Configure RowVersion as a concurrency token
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<AggregateCategoryStat>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Category)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasOne(e => e.AggregateStats)
                .WithMany(a => a.CategoryStats)
                .HasForeignKey(e => e.AggregateStatsId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Category).IsUnique();
        });

        modelBuilder.Entity<AggregateImageTypeStat>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.MimeType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.DisplayName)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasOne(e => e.AggregateStats)
                .WithMany(a => a.ImageTypeStats)
                .HasForeignKey(e => e.AggregateStatsId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.MimeType).IsUnique();
        });

        modelBuilder.Entity<ConvertedImageZip>(entity =>
        {
            entity.HasKey(e => e.DownloadId);

            entity.Property(e => e.FilePath)
                .IsRequired()
                .HasMaxLength(1024);

            entity.Property(e => e.FileName)
                .IsRequired()
                .HasMaxLength(256);

            entity.HasOne(e => e.ScanJob)
                .WithMany()
                .HasForeignKey(e => e.ScanJobId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ScanJobId);
            entity.HasIndex(e => e.ExpiresAt);
        });

        modelBuilder.Entity<CrawlCheckpoint>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.VisitedUrlsJson)
                .IsRequired();

            entity.Property(e => e.PendingUrlsJson)
                .IsRequired();

            entity.Property(e => e.CurrentUrl)
                .HasMaxLength(2048);

            entity.HasOne(e => e.ScanJob)
                .WithMany()
                .HasForeignKey(e => e.ScanJobId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ScanJobId).IsUnique();
        });
    }
}
