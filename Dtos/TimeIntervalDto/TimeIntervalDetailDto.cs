// In TimeIntervalDetailDto.cs
using Dtos.BreakTime; // Make sure this namespace is correct
using System;
using System.Collections.Generic;

namespace Dtos.TimeIntervalDto
{
    public class TimeIntervalDetailDto
    {
        public int Id { get; set; }
        public string Alias { get; set; }

        // --- FORMATTED AND CALCULATED FIELDS ---
        public string FormattedStartTime { get; set; }
        public string ScheduledEndTime { get; set; }
        public int TotalDurationMinutes { get; set; }
        public string NormalWorkDay { get; set; }

        // --- BREAK DETAILS ---
        public List<BreakTimeDto> Breaks { get; set; }

        // --- OTHER PROPERTIES ---
        public decimal? OvertimeLv1Percentage { get; set; }
        public decimal? OvertimeLv2Percentage { get; set; }
        public decimal? OvertimeLv3Percentage { get; set; }
        public string? CompanyId { get; set; }
    }
}