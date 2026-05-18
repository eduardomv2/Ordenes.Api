using System;
using System.Collections.Generic;
using System.Text;

namespace Pagos.Domain.Entities
{
    public class PAG_CargoRecurrente
    {
        public int Id { get; set; }
        public int IdUsuario { get; set; }
        public int IdMetodoPago { get; set; }
        public decimal MontoMensual { get; set; }
        public int DiaCobro { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime? UltimoCobro { get; set; }
        public DateTime? ProximoCobro { get; set; }
        public int Status { get; set; } = 1;

        public PAG_MetodoGuardado MetodoGuardado { get; set; } = null!;
    }
}
