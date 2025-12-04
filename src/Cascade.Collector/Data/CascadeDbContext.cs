using Cascade.Collector.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cascade.Collector.Data;

public class CascadeDbContext : DbContext
{
    public CascadeDbContext(DbContextOptions<CascadeDbContext> options) : base(options)
    {
    }

    public DbSet<StoredMessage> Messages => Set<StoredMessage>();
    public DbSet<StoredEndpoint> Endpoints => Set<StoredEndpoint>();
    public DbSet<StoredConnection> Connections => Set<StoredConnection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // StoredMessage configuration
        modelBuilder.Entity<StoredMessage>(entity =>
        {
            entity.ToTable("Messages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MessageId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CorrelationId).HasMaxLength(100);
            entity.Property(e => e.ConversationId).HasMaxLength(100);
            entity.Property(e => e.CausationId).HasMaxLength(100);
            entity.Property(e => e.RelatedTo).HasMaxLength(100);
            entity.Property(e => e.MessageType).HasMaxLength(500).IsRequired();
            entity.Property(e => e.MessageTypeShort).HasMaxLength(200).IsRequired();
            entity.Property(e => e.EndpointName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.HostId).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ExceptionType).HasMaxLength(500);
            entity.Property(e => e.ExceptionMessage).HasMaxLength(4000);
            entity.Property(e => e.OriginatingEndpoint).HasMaxLength(200);
            entity.Property(e => e.SagaId).HasMaxLength(100);
            entity.Property(e => e.SagaType).HasMaxLength(500);

            // Indexes for common queries
            entity.HasIndex(e => e.CorrelationId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.EndpointName);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.CorrelationId, e.Timestamp });
        });

        // StoredEndpoint configuration
        modelBuilder.Entity<StoredEndpoint>(entity =>
        {
            entity.ToTable("Endpoints");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // StoredConnection configuration
        modelBuilder.Entity<StoredConnection>(entity =>
        {
            entity.ToTable("Connections");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SourceEndpoint).HasMaxLength(200).IsRequired();
            entity.Property(e => e.TargetEndpoint).HasMaxLength(200).IsRequired();
            entity.Property(e => e.MessageType).HasMaxLength(500).IsRequired();
            entity.Property(e => e.MessageTypeShort).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => new { e.SourceEndpoint, e.TargetEndpoint, e.MessageType }).IsUnique();
        });
    }
}