using System;
using System.Collections.Generic;
using System.Text;

namespace Pagos.Domain.Entities
{
    public class PAG_MetodoGuardado
    {
        public int Id { get; set; }
        public int IdUsuario { get; set; }
        public string TokenIdOpenPay { get; set; } = string.Empty;
        public string Ultimos4Digitos { get; set; } = string.Empty;
        public string MarcaTarjeta { get; set; } = string.Empty;
        public bool EsCreditoTienda { get; set; } = false;
        public bool EsPrincipal { get; set; } = false;
        public int Status { get; set; } = 1;

        public ICollection<PAG_Transaccion> Transacciones { get; set; } = [];
        public ICollection<PAG_CargoRecurrente> CargosRecurrentes { get; set; } = [];
    }
}
