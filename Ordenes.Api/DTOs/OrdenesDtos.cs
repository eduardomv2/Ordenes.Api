namespace Ordenes.Api.DTOs;

public record CrearOrdenDto(
    int IdUsuario,
    int IdDireccionEnvio,
    decimal Subtotal,
    decimal DescuentoAplicado,
    decimal TotalFinal,
    string ClaveIdempotencia,
    List<CrearDetalleDto> Detalles
);

public record CrearDetalleDto(
    int IdProducto,
    string NombreProducto,
    bool EsElectronica,
    int Cantidad,
    decimal PrecioUnitario,
    decimal DescuentoLinea
);