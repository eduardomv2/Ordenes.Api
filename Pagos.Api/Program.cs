using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Extensions;
using Pagos.Api.Data;
using Pagos.Api.DTOs;
using Pagos.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

// ── Servicios ─────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Pagos API",
        Version = "v1",
        Description = "Microservicio de gestión de pagos y métodos de pago"
    });
});

builder.Services.AddDbContext<PagosDbContext>(opt =>
    opt.UseMySQL(builder.Configuration.GetConnectionString("PagosDb")!));

var app = builder.Build();

// ── Middleware ────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pagos API v1");
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
    var db = scope.ServiceProvider.GetRequiredService<PagosDbContext>();
    db.Database.Migrate();
}

// ══════════════════════════════════════════════════════════════════
// ENDPOINTS
// ══════════════════════════════════════════════════════════════════

// GET /health
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "Pagos API",
    timestamp = DateTime.UtcNow
}))
.WithName("Health")
.WithTags("Health")
.WithSummary("Verifica estado del microservicio");

// ── Métodos de pago ───────────────────────────────────────────────

// GET /api/pagos/metodos/{idUsuario}
app.MapGet("/api/pagos/metodos/{idUsuario:int}", async (
    int idUsuario,
    PagosDbContext db) =>
{
    var metodos = await db.MetodosGuardados
        .Where(m => m.IdUsuario == idUsuario && m.Status == 1)
        .ToListAsync();

    return Results.Ok(metodos);
})
.WithName("ObtenerMetodosPago")
.WithTags("Pagos")
.WithSummary("Obtiene los métodos de pago de un usuario");

// POST /api/pagos/metodos
app.MapPost("/api/pagos/metodos", async (
    CrearMetodoGuardadoDto dto,
    PagosDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(dto.TokenIdOpenPay))
        return Results.BadRequest(new
        { error = "El token de OpenPay es obligatorio." });

    if (dto.Ultimos4Digitos.Length != 4)
        return Results.BadRequest(new
        { error = "Los últimos 4 dígitos deben ser exactamente 4 caracteres." });

    // Si es principal quitar el flag a los demás
    if (dto.EsPrincipal)
    {
        var anteriores = await db.MetodosGuardados
            .Where(m => m.IdUsuario == dto.IdUsuario && m.EsPrincipal)
            .ToListAsync();
        anteriores.ForEach(m => m.EsPrincipal = false);
    }

    var metodo = new PAG_MetodoGuardado
    {
        IdUsuario = dto.IdUsuario,
        TokenIdOpenPay = dto.TokenIdOpenPay,
        Ultimos4Digitos = dto.Ultimos4Digitos,
        MarcaTarjeta = dto.MarcaTarjeta,
        EsCreditoTienda = dto.EsCreditoTienda,
        EsPrincipal = dto.EsPrincipal,
        Status = 1
    };

    db.MetodosGuardados.Add(metodo);
    await db.SaveChangesAsync();

    return Results.Created(
        $"/api/pagos/metodos/{metodo.Id}",
        new { metodo.Id, metodo.MarcaTarjeta, metodo.Ultimos4Digitos });
})
.WithName("AgregarMetodoPago")
.WithTags("Pagos")
.WithSummary("Registra un nuevo método de pago");

// DELETE /api/pagos/metodos/{id}
app.MapDelete("/api/pagos/metodos/{id:int}", async (
    int id,
    PagosDbContext db) =>
{
    var metodo = await db.MetodosGuardados
        .FirstOrDefaultAsync(m => m.Id == id && m.Status == 1);

    if (metodo is null)
        return Results.NotFound(new { error = "Método de pago no encontrado." });

    metodo.Status = 0;
    await db.SaveChangesAsync();

    return Results.NoContent();
})
.WithName("EliminarMetodoPago")
.WithTags("Pagos")
.WithSummary("Elimina un método de pago (borrado lógico)");

// ── Transacciones ─────────────────────────────────────────────────

// POST /api/pagos/transacciones
app.MapPost("/api/pagos/transacciones", async (
    CrearTransaccionDto dto,
    PagosDbContext db) =>
{
    var metodoExiste = await db.MetodosGuardados
        .AnyAsync(m => m.Id == dto.IdMetodoPago && m.Status == 1);

    if (!metodoExiste)
        return Results.NotFound(new
        { error = "Método de pago no encontrado." });

    if (dto.Monto <= 0)
        return Results.BadRequest(new
        { error = "El monto debe ser mayor a cero." });

    int[] mesesValidos = [1, 3, 6, 12];
    if (!mesesValidos.Contains(dto.MesesSinIntereses))
        return Results.BadRequest(new
        {
            error = "Los meses sin intereses deben ser 1, 3, 6 o 12.",
            valoresPermitidos = mesesValidos
        });

    var transaccion = new PAG_Transaccion
    {
        IdOrden = dto.IdOrden,
        IdMetodoPago = dto.IdMetodoPago,
        Monto = dto.Monto,
        MesesSinIntereses = dto.MesesSinIntereses,
        EstadoPago = "pendiente",
        EsCargoRecurrente = false,
        Status = 1
    };

    db.Transacciones.Add(transaccion);
    await db.SaveChangesAsync();

    return Results.Created(
        $"/api/pagos/transacciones/{transaccion.Id}",
        new
        {
            transaccion.Id,
            transaccion.EstadoPago,
            transaccion.Monto,
            transaccion.MesesSinIntereses
        });
})
.WithName("CrearTransaccion")
.WithTags("Pagos")
.WithSummary("Registra una nueva transacción de pago");

// GET /api/pagos/transacciones/{id}
app.MapGet("/api/pagos/transacciones/{id:int}", async (
    int id,
    PagosDbContext db) =>
{
    var transaccion = await db.Transacciones
        .Include(t => t.MetodoGuardado)
        .FirstOrDefaultAsync(t => t.Id == id);

    return transaccion is null
        ? Results.NotFound(new { error = "Transacción no encontrada." })
        : Results.Ok(transaccion);
})
.WithName("ObtenerTransaccion")
.WithTags("Pagos")
.WithSummary("Obtiene una transacción por Id");

// ── Cargos recurrentes ────────────────────────────────────────────

// POST /api/pagos/cargos
app.MapPost("/api/pagos/cargos", async (
    CrearCargoRecurrenteDto dto,
    PagosDbContext db) =>
{
    if (dto.DiaCobro < 1 || dto.DiaCobro > 28)
        return Results.BadRequest(new
        { error = "El día de cobro debe estar entre 1 y 28." });

    var metodoExiste = await db.MetodosGuardados
        .AnyAsync(m => m.Id == dto.IdMetodoPago && m.Status == 1);

    if (!metodoExiste)
        return Results.NotFound(new
        { error = "Método de pago no encontrado." });

    var hoy = DateTime.UtcNow;
    var proximoCobro = new DateTime(
        hoy.Year,
        hoy.Month,
        dto.DiaCobro);

    if (proximoCobro < hoy)
        proximoCobro = proximoCobro.AddMonths(1);

    var cargo = new PAG_CargoRecurrente
    {
        IdUsuario = dto.IdUsuario,
        IdMetodoPago = dto.IdMetodoPago,
        MontoMensual = dto.MontoMensual,
        DiaCobro = dto.DiaCobro,
        Activo = true,
        ProximoCobro = proximoCobro,
        Status = 1
    };

    db.CargosRecurrentes.Add(cargo);
    await db.SaveChangesAsync();

    return Results.Created(
        $"/api/pagos/cargos/{cargo.Id}",
        new { cargo.Id, cargo.DiaCobro, cargo.ProximoCobro });
})
.WithName("CrearCargoRecurrente")
.WithTags("Pagos")
.WithSummary("Crea un cargo recurrente mensual");

// GET /api/pagos/cargos/{idUsuario}
app.MapGet("/api/pagos/cargos/{idUsuario:int}", async (
    int idUsuario,
    PagosDbContext db) =>
{
    var cargos = await db.CargosRecurrentes
        .Where(c => c.IdUsuario == idUsuario && c.Status == 1)
        .ToListAsync();

    return Results.Ok(cargos);
})
.WithName("ObtenerCargosRecurrentes")
.WithTags("Pagos")
.WithSummary("Obtiene los cargos recurrentes de un usuario");

app.Run();