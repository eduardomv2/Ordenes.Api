using System;
using System.Collections.Generic;
using System.Text;

namespace Ordenes.Domain.Entities
{
    public class ORD_Detalle
    {
        public int Id { get; set; }
        public int IdOrden { get; set; }
        public int IdProducto { get; set; }
        public string NombreProducto { get; set; } = string.Empty;
        public bool EsElectronica { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal DescuentoLinea { get; set; } = 0m;
        public int Status { get; set; } = 1;

        public ORD_Orden Orden { get; set; } = null!;
    }
}
