using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.TimeIntervalDto
{
    public class HorarioInfoDto
    {
        public int IdHorio { get; set; }
        public string Nombre { get; set; }
        public int Tipo { get; set; }
        public string HoraEntrada { get; set; }
        public string HoraSalida { get; set; }
        public string TiempoTrabajo { get; set; }
        public int Descanso { get; set; }
        public double DiasLaboral { get; set; }
        public string TipoTrabajo { get; set; }

        public string HoraEntradaDesde { get; set; }
        public string HoraEntradaHasta { get; set; }

        public string HoraSalidaDesde { get; set; }
        public string HoraSalidaHasta { get; set; }

        public int EntradaTemprana { get; set; }
        public int EntradaTarde { get; set; }
        public int MinEntradaTemprana { get; set; }
        public int MinSalidaTarde { get; set; }

        public int Hnivel { get; set; }
        public int HNivel1 { get; set; }
        public int HNivel2 { get; set; }
        public int HNivel3 { get; set; }

        public bool MarcarEntrada { get; set; }
        public bool MarcarSalida { get; set; }

        public int PLlegadaT { get; set; }
        public int PSalidaT { get; set; }

        public int TipoIntervalo { get; set; }

        public int PeriodoMarcacion { get; set; }

        public string HCambioDia { get; set; }

        public int BasadoM { get; set; }

        public short? TotalMarcaciones { get; set; }
    }

}
