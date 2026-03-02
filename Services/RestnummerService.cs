using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MaterialManager_V01.Services
{
    public static class RestnummerService
    {
        private static readonly string StateFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MaterialManager_V01",
            "restnummer_state.json"
        );

        private class RestnummerState
        {
            public string LastDate { get; set; } = "";
            public int LastNumber { get; set; } = 0;
        }

        /// <summary>
        /// Generates the next Restnummer in format: KW_TAG-NR
        /// Example: 50_DI-01, 50_DI-02, ...
        /// Resets daily.
        /// </summary>
        public static string GenerateNext()
        {
            var now = DateTime.Now;
            var calendar = CultureInfo.CurrentCulture.Calendar;
            var calendarWeekRule = CultureInfo.CurrentCulture.DateTimeFormat.CalendarWeekRule;
            var firstDayOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            
            // Get calendar week (KW)
            int kw = calendar.GetWeekOfYear(now, calendarWeekRule, firstDayOfWeek);
            
            // Get day abbreviation (MO, DI, MI, DO, FR, SA, SO)
            string dayAbbrev = GetDayAbbreviation(now.DayOfWeek);
            
            // Current date as string for comparison
            string currentDate = now.ToString("yyyy-MM-dd");
            
            // Load state
            var state = LoadState();
            
            // Reset counter if it's a new day
            if (state.LastDate != currentDate)
            {
                state.LastDate = currentDate;
                state.LastNumber = 0;
            }
            
            // Increment counter
            state.LastNumber++;
            
            // Save state
            SaveState(state);
            
            // Format: KW_TAG-NR (e.g., 50_DI-01)
            return $"{kw}_{dayAbbrev}-{state.LastNumber:D2}";
        }

        private static string GetDayAbbreviation(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "MO",
                DayOfWeek.Tuesday => "DI",
                DayOfWeek.Wednesday => "MI",
                DayOfWeek.Thursday => "DO",
                DayOfWeek.Friday => "FR",
                DayOfWeek.Saturday => "SA",
                DayOfWeek.Sunday => "SO",
                _ => "XX"
            };
        }

        private static RestnummerState LoadState()
        {
            try
            {
                if (File.Exists(StateFile))
                {
                    var json = File.ReadAllText(StateFile);
                    return JsonSerializer.Deserialize<RestnummerState>(json) ?? new RestnummerState();
                }
            }
            catch { }
            
            return new RestnummerState();
        }

        private static void SaveState(RestnummerState state)
        {
            try
            {
                var dir = Path.GetDirectoryName(StateFile);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir!);
                
                var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(StateFile, json);
            }
            catch { }
        }
    }
}
