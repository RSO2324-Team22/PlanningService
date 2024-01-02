using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PlanningService.Concerts;
using PlanningService.Rehearsals;

namespace PlanningService.Database;

public class PlanningDbContext : DbContext {
    private readonly ILogger<PlanningDbContext> _logger;

    public DbSet<Concert> Concerts { get; private set; }
    public DbSet<Rehearsal> Rehearsals { get; private set; }

    public PlanningDbContext(
            DbContextOptions<PlanningDbContext> options,
            ILogger<PlanningDbContext> logger) : base(options) {
        this._logger = logger;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Concert>().OwnsOne(c => c.Location);

        modelBuilder.Entity<Concert>()
            .Property(c => c.Status)
            .HasConversion(new EnumCollectionJsonValueConverter<ConcertStatus>())
            .Metadata.SetValueComparer(new CollectionValueComparer<ConcertStatus>());

        modelBuilder.Entity<Rehearsal>().OwnsOne(c => c.Location);
        
        modelBuilder.Entity<Rehearsal>()
            .Property(c => c.Status)
            .HasConversion(new EnumCollectionJsonValueConverter<RehearsalStatus>())
            .Metadata.SetValueComparer(new CollectionValueComparer<RehearsalStatus>());
        
        modelBuilder.Entity<Rehearsal>()
            .Property(c => c.Type)
            .HasConversion(new EnumCollectionJsonValueConverter<RehearsalType>())
            .Metadata.SetValueComparer(new CollectionValueComparer<RehearsalType>());
    }
}

class EnumCollectionJsonValueConverter<T> : ValueConverter<IEnumerable<T>, string> 
where T : struct, Enum
{
    public EnumCollectionJsonValueConverter() : base(
        v => JsonSerializer
            .Serialize(v.Select(e => e.ToString()).ToList(), (JsonSerializerOptions) null),
        v => JsonSerializer
            .Deserialize<IEnumerable<string>>(v, (JsonSerializerOptions) null)
            .Select(e => Enum.Parse<T>(e)).ToHashSet()) {}
}

class CollectionValueComparer<T> : ValueComparer<IEnumerable<T>>
{
    public CollectionValueComparer() : base(
        (c1, c2) => c1.SequenceEqual(c2),
        c => c.Aggregate(0, 
            (a, v) => HashCode.Combine(a, v.GetHashCode())), 
                c => (IEnumerable<T>)c.ToHashSet()) {}
}
