using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.ShiftDto
{
    public class ShiftCreateDto
    {
        public string Alias { get; set; }
        public short CycleUnit { get; set; }
        public int ShiftCycle { get; set; }
        public bool WorkWeekend { get; set; }
        public short WeekendType { get; set; }
        public bool WorkDayOff { get; set; }
        public short DayOffType { get; set; }
        public bool AutoShift { get; set; }
        public List<ShiftDetailCreateDto> Detalles { get; set; }
    }

    public class ShiftDetailCreateDto
    {
        public int TimeIntervalId { get; set; }
        public int DayIndex { get; set; }
    }
}
