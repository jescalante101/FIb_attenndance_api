namespace Dtos.ShiftDto
{
    // DTO para el detalle del horario diario
    public class ShiftDayDto
    {
        public int DayIndex { get; set; }
        public string TimeIntervalAlias { get; set; }
        public int TimeIntervalId { get; set; }
        public string InTime { get; set; }
        public string OutTime { get; set; } // Hora de salida calculada

        // --- CÁLCULOS POR DÍA ---
        public string WorkHours { get; set; }
        public string BreakHours { get; set; }
        public string OvertimeHours { get; set; }
        public string TotalDuration { get; set; }
    }

    // DTO principal para cada turno en la lista
    public class ShiftListDto
    {
        public int Id { get; set; }
        public string Alias { get; set; }
        public int ShiftCycle { get; set; }
        public short CycleUnit { get; set; }
        public bool AutoShift { get; set; }
        public ShiftTotalsDto Totals { get; set; }
        public List<ShiftDayDto> Horario { get; set; } = new List<ShiftDayDto>();
    }

    public class ShiftTotalsDto
    {
        public string TotalWorkHours { get; set; }
        public string TotalBreakHours { get; set; }
        public string TotalOvertimeHours { get; set; }
        public string TotalShiftHours { get; set; }
    }

}
