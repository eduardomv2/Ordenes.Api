using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Extensions;
using Ordenes.Api.Data;
using Ordenes.Api.DTOs;
using Ordenes.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

// ── Servicios ─────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Ordenes API",
        Version = "v1",
        Description = "Microservicio de gestión de órdenes"
    });
});

builder.Services.AddDbContext<OrdenesDbContext>(opt =>
    opt.UseMySQL(builder.Configuration.GetConnectionString("OrdenesDb")!));

var app = builder.Build();

// ── Middleware ────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ordenes API v1");
    c.RoutePrefix = "swagger";
});

app.UseExceptionHandler(errApp =>
{
    errApp.Run(async context =>
    {
        var error = context.Features
            .Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            status = 500,
            error = "Error interno del servidor.",
            detalle = error?.Error.Message,
            timestamp = DateTime.UtcNow
        });
    });
});

// ── Migración automática ──────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrdenesDbContext>();
    db.Database.Migrate();
}

// ══════════════════════════════════════════════════════════════════
// ENDPOINTS
// ══════════════════════════════════════════════════════════════════

// GET /health
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "Ordenes API",
    timestamp = DateTime.UtcNow
}))
.WithName("Health")
.WithTags("Health")
.WithSummary("Verifica estado del microservicio");

// GET /api/ordenes/estados
app.MapGet("/api/ordenes/estados", async (OrdenesDbContext db) =>
    Results.Ok(await db.EstadosOrden
        .Where(e => e.Status == 1)
        .ToListAsync()))
.WithName("ObtenerEstados")
.WithTags("Ordenes")
.WithSummary("Lista todos los estados de orden");

// GET /api/ordenes/{id}
app.MapGet("/api/ordenes/{id:int}", async (
    int id,
    OrdenesDbContext db) =>
{
    var orden = await db.Ordenes
        .Include(o => o.EstadoOrden)
        .Include(o => o.Detalles)
        .FirstOrDefaultAsync(o => o.Id == id && o.Status == 1);

    return orden is null
        ? Results.NotFound(new { error = "Orden no encontrada." })
        : Results.Ok(orden);
})
.WithName("ObtenerOrden")
.WithTags("Ordenes")
.WithSummary("Obtiene una orden por Id");

app.MapGet("/api/ordenes/usuario/{idUsuario:int}", async (
    int idUsuario,
    OrdenesDbContext db) =>
{
    var ordenes = await db.Ordenes
        .Include(o => o.EstadoOrden)
        .Include(o => o.Detalles)
        .Where(o => o.IdUsuario == idUsuario && o.Status == 1)
        .Select(o => new
        {
            o.Id,
            o.IdUsuario,
            o.IdDireccionEnvio,
            o.IdEstadoOrden,
            o.Subtotal,
            o.DescuentoAplicado,
            o.TotalFinal,
            o.ClaveIdempotencia,
            EstadoOrden = new
            {
                o.EstadoOrden.Id,
                o.EstadoOrden.Nombre
            },
            Detalles = o.Detalles.Select(d => new
            {
                d.Id,
                d.IdProducto,
                d.NombreProducto,
                d.EsElectronica,
                d.Cantidad,
                d.PrecioUnitario,
                d.DescuentoLinea
            }).ToList()
        })
        .ToListAsync();

    return Results.Ok(ordenes);
})
.WithName("ObtenerOrdenesPorUsuario")
.WithTags("Ordenes")
.WithSummary("Obtiene todas las órdenes de un usuario");

// POST /api/ordenes
app.MapPost("/api/ordenes", async (
    CrearOrdenDto dto,
    OrdenesDbContext db) =>
{
    // Idempotencia — verificar si ya existe una orden con esa clave
    var ordenExistente = await db.Ordenes
        .FirstOrDefaultAsync(o => o.ClaveIdempotencia == dto.ClaveIdempotencia);

    if (ordenExistente is not null)
        return Results.Ok(new
        {
            ordenExistente.Id,
            ordenExistente.TotalFinal,
            ordenExistente.ClaveIdempotencia,
            mensaje = "Orden ya existente, retornando orden original."
        });

    // Validaciones
    if (dto.Detalles is null || !dto.Detalles.Any())
        return Results.BadRequest(new
        { error = "La orden debe tener al menos un producto." });

    if (dto.TotalFinal <= 0)
        return Results.BadRequest(new
        { error = "El total de la orden debe ser mayor a cero." });

    // Estado inicial = Pendiente (Id: 1)
    var orden = new ORD_Orden
    {
        IdUsuario = dto.IdUsuario,
        IdDireccionEnvio = dto.IdDireccionEnvio,
        IdEstadoOrden = 1,
        Subtotal = dto.Subtotal,
        DescuentoAplicado = dto.DescuentoAplicado,
        TotalFinal = dto.TotalFinal,
        ClaveIdempotencia = dto.ClaveIdempotencia,
        Status = 1,
        Detalles = dto.Detalles.Select(d => new ORD_Detalle
        {
            IdProducto = d.IdProducto,
            NombreProducto = d.NombreProducto,
            EsElectronica = d.EsElectronica,
            Cantidad = d.Cantidad,
            PrecioUnitario = d.PrecioUnitario,
            DescuentoLinea = d.DescuentoLinea,
            Status = 1
        }).ToList()
    };

    db.Ordenes.Add(orden);
    await db.SaveChangesAsync();

    return Results.Created(
        $"/api/ordenes/{orden.Id}",
        new { orden.Id, orden.TotalFinal, orden.ClaveIdempotencia });
})
.WithName("CrearOrden")
.WithTags("Ordenes")
.WithSummary("Crea una nueva orden")
.WithDescription("Si la ClaveIdempotencia ya existe retorna la orden original sin duplicar.");

// PATCH /api/ordenes/{id}/estado
app.MapMethods("/api/ordenes/{id:int}/estado", ["PATCH"], async (
    int id,
    CambiarEstadoOrdenDto dto,
    OrdenesDbContext db) =>
{
    var orden = await db.Ordenes
        .FirstOrDefaultAsync(o => o.Id == id && o.Status == 1);

    if (orden is null)
        return Results.NotFound(new { error = "Orden no encontrada." });

    var estadoExiste = await db.EstadosOrden
        .AnyAsync(e => e.Id == dto.IdEstadoOrden && e.Status == 1);

    if (!estadoExiste)
        return Results.NotFound(new { error = "Estado de orden no válido." });

    orden.IdEstadoOrden = dto.IdEstadoOrden;
    await db.SaveChangesAsync();

    return Results.Ok(new { orden.Id, orden.IdEstadoOrden });
})
.WithName("CambiarEstadoOrden")
.WithTags("Ordenes")
.WithSummary("Cambia el estado de una orden");

// DELETE /api/ordenes/{id}
app.MapDelete("/api/ordenes/{id:int}", async (
    int id,
    OrdenesDbContext db) =>
{
    var orden = await db.Ordenes
        .FirstOrDefaultAsync(o => o.Id == id && o.Status == 1);

    if (orden is null)
        return Results.NotFound(new { error = "Orden no encontrada." });

    orden.Status = 0;
    await db.SaveChangesAsync();

    return Results.NoContent();
})
.WithName("CancelarOrden")
.WithTags("Ordenes")
.WithSummary("Cancela una orden (borrado lógico)");

app.Run();

record CambiarEstadoOrdenDto(int IdEstadoOrden);