using System;
using System.Collections.Generic;
using System.Text;

namespace Ordenes.Domain.Entities
{
    public class ORD_Cat_EstadoOrden
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Status { get; set; } = 1;
    }
}
