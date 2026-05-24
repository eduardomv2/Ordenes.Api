using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Extensions;
using Pagos.Api.Data;
using Pagos.Api.DTOs;
using Pagos.Domain.Entities;
using System.Net.Http.Headers;

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

// ── OpenPay HttpClient ────────────────────────────────────────────
var merchantId = builder.Configuration["OpenPay:MerchantId"]!;
var secretKey = builder.Configuration["OpenPay:SecretKey"]!;

builder.Services.AddHttpClient("OpenPay", client =>
{
    client.BaseAddress = new Uri($"https://sandbox-api.openpay.mx/v1/{merchantId}/");
    var credentials = Convert.ToBase64String(
        System.Text.Encoding.ASCII.GetBytes($"{secretKey}:"));
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Basic", credentials);
});

var app = builder.Build();


// ── Middleware ─────
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

// ── Migración automática ────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PagosDbContext>();
    db.Database.Migrate();
}


// ENDPOINTS


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

// ── Métodos de pago ────

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

// ── Transacciones ─────

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

// ── Cargos recurrentes ──────

// POST /api/pagos/cobrar
app.MapPost("/api/pagos/cobrar", async (
    CobrarDto dto,
    PagosDbContext db,
    IHttpClientFactory httpClientFactory) =>
{
    var metodo = await db.MetodosGuardados
        .FirstOrDefaultAsync(m => m.Id == dto.IdMetodoPago && m.Status == 1);
    if (metodo is null)
        return Results.NotFound(new { error = "Método de pago no encontrado." });
    if (dto.Monto <= 0)
        return Results.BadRequest(new { error = "El monto debe ser mayor a cero." });
    int[] mesesValidos = [1, 3, 6, 12];
    if (!mesesValidos.Contains(dto.MesesSinIntereses))
        return Results.BadRequest(new
        {
            error = "Los meses sin intereses deben ser 1, 3, 6 o 12.",
            valoresPermitidos = mesesValidos
        });
    try
    {
        var openPay = httpClientFactory.CreateClient("OpenPay");

        // Paso 1: Crear token de tarjeta
        var tokenDict = new Dictionary<string, object>
        {
            ["card_number"] = dto.NumeroTarjeta,
            ["holder_name"] = dto.NombreTarjeta.Trim(),
            ["expiration_year"] = dto.AnioExpiracion.Length == 4
                ? dto.AnioExpiracion[2..] : dto.AnioExpiracion,
            ["expiration_month"] = dto.MesExpiracion,
            ["cvv2"] = dto.Cvv
        };

        var jsonToken = System.Text.Json.JsonSerializer.Serialize(tokenDict);
        var tokenContent = new StringContent(jsonToken,
            System.Text.Encoding.UTF8, "application/json");
        var tokenResponse = await openPay.PostAsync("tokens", tokenContent);
        var tokenRaw = await tokenResponse.Content.ReadAsStringAsync();
        Console.WriteLine($"Token response: {tokenRaw}");

        var tokenData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(tokenRaw);

        if (!tokenResponse.IsSuccessStatusCode || tokenData is null)
            return Results.BadRequest(new { error = "Error al tokenizar la tarjeta." });

        var sourceId = tokenData["id"].ToString();

        // Paso 2: Hacer el cargo con source_id
        var cargoDict = new Dictionary<string, object>
        {
            ["method"] = "card",
            ["source_id"] = sourceId!,
            ["amount"] = Math.Round(dto.Monto, 2),
            ["currency"] = "MXN",
            ["description"] = $"Orden #{dto.IdOrden}",
            ["order_id"] = $"orden-{Guid.NewGuid().ToString()[..8]}",
            ["device_session_id"] = "kR1MiQhz2otdIuUlQkbEyitIqVMiI16f",
            ["customer"] = new
            {
                name = dto.NombreTarjeta.Trim(),
                email = $"cliente{dto.IdUsuario}@tienda.com"
            }
        };

        if (dto.MesesSinIntereses > 1)
            cargoDict["installments"] = dto.MesesSinIntereses;

        var jsonCargo = System.Text.Json.JsonSerializer.Serialize(cargoDict);
        Console.WriteLine($"Enviando a OpenPay: {jsonCargo}");
        var content = new StringContent(jsonCargo,
            System.Text.Encoding.UTF8, "application/json");
        var response = await openPay.PostAsync("charges", content);
        var rawContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"OpenPay response: {rawContent}");

        var contenido = System.Text.Json.JsonSerializer.Deserialize<OpenPayChargeResponse>(
            rawContent, new System.Text.Json.JsonSerializerOptions
            { PropertyNameCaseInsensitive = true });

        var transaccion = new PAG_Transaccion
        {
            IdOrden = dto.IdOrden,
            IdMetodoPago = dto.IdMetodoPago,
            Monto = dto.Monto,
            MesesSinIntereses = dto.MesesSinIntereses,
            IdTransaccionOpenPay = contenido?.Id,
            EstadoPago = response.IsSuccessStatusCode ? "exitoso" : "fallido",
            EsCargoRecurrente = false,
            Status = 1
        };
        db.Transacciones.Add(transaccion);
        await db.SaveChangesAsync();

        if (response.IsSuccessStatusCode)
            return Results.Ok(new
            {
                transaccion.Id,
                transaccion.EstadoPago,
                transaccion.Monto,
                IdTransaccionOpenPay = contenido?.Id,
                Status = contenido?.Status
            });

        return Results.BadRequest(new
        {
            error = "El cobro fue rechazado.",
            detalle = contenido?.ErrorMessage
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("CobrarConOpenPay")
.WithTags("Pagos")
.WithSummary("Procesa un cobro con OpenPay");

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

record CobrarDto(
    int IdOrden,
    int IdMetodoPago,
    int IdUsuario,
    decimal Monto,
    int MesesSinIntereses,
    string NumeroTarjeta,
    string NombreTarjeta,
    string MesExpiracion,
    string AnioExpiracion,
    string Cvv);

record OpenPayChargeResponse(
    string? Id,
    string? Status,
    string? ErrorMessage);