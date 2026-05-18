using System;
using System.Collections.Generic;
using System.Text;

namespace Pagos.Domain.Entities
{
    public class PAG_Transaccion
    {
        public int Id { get; set; }
        public int IdOrden { get; set; }
        public int IdMetodoPago { get; set; }
        public decimal Monto { get; set; }
        public int MesesSinIntereses { get; set; } = 1;
        public string? IdTransaccionOpenPay { get; set; }
        public string EstadoPago { get; set; } = string.Empty;
        public bool EsCargoRecurrente { get; set; } = false;
        public int Status { get; set; } = 1;

        public PAG_MetodoGuardado MetodoGuardado { get; set; } = null!;
    }
}
