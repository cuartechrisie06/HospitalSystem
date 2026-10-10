using System;

namespace HospitalSystem.Models
{
    public static class DurationText
    {
        // "45 min", "3 h 10 min", "2 d 5 h"
        public static string Format(TimeSpan span)
        {
            if (span.TotalMinutes < 60) return Math.Max(0, (int)span.TotalMinutes) + " min";
            if (span.TotalHours < 24) return (int)span.TotalHours + " h " + span.Minutes + " min";
            return (int)span.TotalDays + " d " + span.Hours + " h";
        }
    }
}
