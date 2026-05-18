using Microsoft.EntityFrameworkCore;
using Pagos.Domain.Entities;



namespace Pagos.Api.Data;

public class PagosDbContext : DbContext
{
    public PagosDbContext(DbContextOptions<PagosDbContext> options)
        : base(options) { }

    public DbSet<PAG_MetodoGuardado> MetodosGuardados => Set<PAG_MetodoGuardado>();
    public DbSet<PAG_Transaccion> Transacciones => Set<PAG_Transaccion>();
    public DbSet<PAG_CargoRecurrente> CargosRecurrentes => Set<PAG_CargoRecurrente>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<PAG_MetodoGuardado>(e =>
        {
            e.ToTable("PAG_MetodoGuardado");
            e.HasKey(x => x.Id);
            e.Property(x => x.TokenIdOpenPay).IsRequired().HasMaxLength(100);
            e.Property(x => x.Ultimos4Digitos).IsRequired().HasMaxLength(4);
            e.Property(x => x.MarcaTarjeta).IsRequired().HasMaxLength(50);
        });

        m.Entity<PAG_Transaccion>(e =>
        {
            e.ToTable("PAG_Transaccion");
            e.HasKey(x => x.Id);
            e.Property(x => x.Monto).HasColumnType("decimal(10,2)");
            e.Property(x => x.EstadoPago).IsRequired().HasMaxLength(50);
            e.Property(x => x.IdTransaccionOpenPay).HasMaxLength(100);
            e.HasOne(x => x.MetodoGuardado)
             .WithMany(m => m.Transacciones)
             .HasForeignKey(x => x.IdMetodoPago)
             .OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<PAG_CargoRecurrente>(e =>
        {
            e.ToTable("PAG_CargoRecurrente");
            e.HasKey(x => x.Id);
            e.Property(x => x.MontoMensual).HasColumnType("decimal(10,2)");
            e.HasOne(x => x.MetodoGuardado)
             .WithMany(m => m.CargosRecurrentes)
             .HasForeignKey(x => x.IdMetodoPago)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}