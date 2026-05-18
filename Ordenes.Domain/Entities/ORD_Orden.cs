using System;
using System.Collections.Generic;
using System.Text;

namespace Ordenes.Domain.Entities
{
    public class ORD_Orden
    {
        public int Id { get; set; }
        public int IdUsuario { get; set; }
        public int IdDireccionEnvio { get; set; }
        public int IdEstadoOrden { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DescuentoAplicado { get; set; } = 0m;
        public decimal TotalFinal { get; set; }
        public string ClaveIdempotencia { get; set; } = string.Empty;
        public int Status { get; set; } = 1;

        public ORD_Cat_EstadoOrden EstadoOrden { get; set; } = null!;
        public ICollection<ORD_Detalle> Detalles { get; set; } = [];
    }
}
