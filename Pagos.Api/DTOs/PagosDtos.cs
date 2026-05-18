namespace Pagos.Api.DTOs;

public record CrearMetodoGuardadoDto(
    int IdUsuario,
    string TokenIdOpenPay,
    string Ultimos4Digitos,
    string MarcaTarjeta,
    bool EsCreditoTienda,
    bool EsPrincipal
);

public record CrearTransaccionDto(
    int IdOrden,
    int IdMetodoPago,
    decimal Monto,
    int MesesSinIntereses
);

public record CrearCargoRecurrenteDto(
    int IdUsuario,
    int IdMetodoPago,
    decimal MontoMensual,
    int DiaCobro
);