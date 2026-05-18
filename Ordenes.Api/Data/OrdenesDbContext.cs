using Microsoft.EntityFrameworkCore;
using Ordenes.Domain.Entities;


namespace Ordenes.Api.Data;

public class OrdenesDbContext : DbContext
{
    public OrdenesDbContext(DbContextOptions<OrdenesDbContext> options)
        : base(options) { }

    public DbSet<ORD_Cat_EstadoOrden> EstadosOrden => Set<ORD_Cat_EstadoOrden>();
    public DbSet<ORD_Orden> Ordenes => Set<ORD_Orden>();
    public DbSet<ORD_Detalle> Detalles => Set<ORD_Detalle>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<ORD_Cat_EstadoOrden>(e =>
        {
            e.ToTable("ORD_Cat_EstadoOrden");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nombre).IsRequired().HasMaxLength(100);
        });

        m.Entity<ORD_Orden>(e =>
        {
            e.ToTable("ORD_Orden");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ClaveIdempotencia).IsUnique();
            e.Property(x => x.ClaveIdempotencia).IsRequired().HasMaxLength(100);
            e.Property(x => x.Subtotal).HasColumnType("decimal(10,2)");
            e.Property(x => x.DescuentoAplicado).HasColumnType("decimal(10,2)");
            e.Property(x => x.TotalFinal).HasColumnType("decimal(10,2)");
            e.HasOne(x => x.EstadoOrden)
             .WithMany()
             .HasForeignKey(x => x.IdEstadoOrden)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Detalles)
             .WithOne(d => d.Orden)
             .HasForeignKey(d => d.IdOrden)
             .OnDelete(DeleteBehavior.Cascade);
        });

        m.Entity<ORD_Detalle>(e =>
        {
            e.ToTable("ORD_Detalle");
            e.HasKey(x => x.Id);
            e.Property(x => x.NombreProducto).IsRequired().HasMaxLength(200);
            e.Property(x => x.PrecioUnitario).HasColumnType("decimal(10,2)");
            e.Property(x => x.DescuentoLinea).HasColumnType("decimal(10,2)");
        });
    }
}