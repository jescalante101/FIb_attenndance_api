// Dtos/Transactions/AsistenciaCompletaDto.cs
using System.ComponentModel.DataAnnotations.Schema;

namespace Dtos.Transactions
{
    /// <summary>
    /// DTO completo para el nuevo SP mejorado con análisis de proximidad
    /// </summary>
    public class AsistenciaCompletaDto
    {
        // Información del empleado
        
        public string Nro_Doc { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
       
        public string Apellido_Paterno { get; set; } = string.Empty;
       
        public string Apellido_Materno { get; set; } = string.Empty;
        public string AreaDescripcion { get; set; } = string.Empty;
        public string CcostoDescripcion { get; set; } = string.Empty;

        // Información de turno y horario
        public string TurnoAlias { get; set; } = string.Empty;
        public string HorarioAlias { get; set; } = string.Empty;

        // Fecha y horarios programados
        public DateTime FechaMarcacion { get; set; }
        public TimeSpan? HorarioEntrada { get; set; }
        public TimeSpan? HorarioSalida { get; set; }

        // Marcaciones reales
        public TimeSpan? HoraEntrada { get; set; }
        public TimeSpan? HoraSalida { get; set; }
        public string? TerminalEntrada { get; set; }
        public string? TerminalSalida { get; set; }

        // Análisis de marcaciones
        public int TotalMarcacionesDia { get; set; }
        public int MarcacionesEsperadas { get; set; }
        public int ContadorEntradas { get; set; }
        public int ContadorSalidas { get; set; }
        public int ContadorBreaks { get; set; }

        // Estados y análisis
        public string EstadoEntrada { get; set; } = string.Empty;
        public string EstadoDia { get; set; } = string.Empty;
        public string EstadoMarcaciones { get; set; } = string.Empty;

        // Cálculos de tiempo
        public int? MinutosTardanza { get; set; }
        public int? MinutosSalidaTemprana { get; set; }
        public int? MinutosTrabajados { get; set; }

        // Información adicional
        public int DiaSemana { get; set; }
        public string? DetalleMarcaciones { get; set; }

        // Campos calculados adicionales
        public string NombreCompleto => $"{Nombres} {Apellido_Paterno} {Apellido_Materno}".Trim();
        public string DiaSemanaTexto => DiaSemana switch
        {
            0 => "Domingo",
            1 => "Lunes",
            2 => "Martes",
            3 => "Miércoles",
            4 => "Jueves",
            5 => "Viernes",
            6 => "Sábado",
            _ => "Desconocido"
        };

        public decimal? HorasTrabajadas => MinutosTrabajados.HasValue ?
            Math.Round((decimal)MinutosTrabajados.Value / 60, 2) : null;

        public bool EsPuntual => EstadoEntrada == "Puntual";
        public bool TieneTardanza => MinutosTardanza > 0;
        public bool TieneSalidaTemprana => MinutosSalidaTemprana > 0;
        public bool AsistenciaCompleta => HoraEntrada.HasValue && HoraSalida.HasValue;
        public bool MarcacionesCompletas => TotalMarcacionesDia == MarcacionesEsperadas;
    }

    /// <summary>
    /// DTO interno para mapear con el SP (incluye TotalRecords para paginación)
    /// </summary>
    public class AsistenciaCompletaDtoWithTotal : AsistenciaCompletaDto
    {
        public int TotalRecords { get; set; }
    }
}