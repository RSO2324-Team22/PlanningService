using System.Text.Json;
using GraphQL.AspNet.Common.Extensions;
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
            .HasConversion(new EnumJsonValueConverter<ConcertStatus>())
            .Metadata.SetValueComparer(new EnumValueComparer<ConcertStatus>());

        modelBuilder.Entity<Rehearsal>().OwnsOne(c => c.Location);
        
        modelBuilder.Entity<Rehearsal>()
            .Property(c => c.Status)
            .HasConversion(new EnumJsonValueConverter<RehearsalStatus>())
            .Metadata.SetValueComparer(new EnumValueComparer<RehearsalStatus>());
        
        modelBuilder.Entity<Rehearsal>()
            .Property(c => c.Type)
            .HasConversion(new EnumJsonValueConverter<RehearsalType>())
            .Metadata.SetValueComparer(new EnumValueComparer<RehearsalType>());
    }
}


class EnumJsonValueConverter<T> : ValueConverter<T, string> 
    where T : struct, Enum
{
    public EnumJsonValueConverter() : base(
        v => JsonSerializer
            .Serialize(v.ToString(), (JsonSerializerOptions) null),
        v => JsonSerializer 
            .Deserialize<string>(v, (JsonSerializerOptions) null)
            .AsEnumerable()
            .Select(e => Enum.Parse<T>(e)).Single()) {}
}

class EnumValueComparer<T> : ValueComparer<T>
{
    public EnumValueComparer() : base(
        (c1, c2) => c1.Equals(c2),
        c => c.GetHashCode(), 
        c => c) {}
}
