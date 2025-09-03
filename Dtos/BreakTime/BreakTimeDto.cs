using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.BreakTime
{
    public class BreakTimeDto
    {
        public int Id { get; set; }
        public string Alias { get; set; }
        public int Duration { get; set; }
        // Añade cualquier otra propiedad de AttBreaktime que necesites en el frontend
    }
}
