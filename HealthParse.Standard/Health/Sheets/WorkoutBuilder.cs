﻿using System;
using System.Collections.Generic;
using System.Linq;
using NodaTime;
using OfficeOpenXml;
using UnitsNet;
using UNDuration = UnitsNet.Duration;

namespace HealthParse.Standard.Health.Sheets
{
    public abstract class WorkoutBuilder : ISheetBuilder<WorkoutBuilder.WorkoutItem>
    {
        private readonly IEnumerable<Workout> _workouts;
        private readonly DateTimeZone _zone;
        private readonly Func<Workout, object> _selector;
        public IEnumerable<string> Headers { get; }
        public bool HasHeaders { get; }

        protected WorkoutBuilder(IEnumerable<Workout> workouts, string workoutKey, DateTimeZone zone, Func<Workout, object> selector, params string[] columnNames)
        {
            _workouts = workouts.Where(w => w.WorkoutType == workoutKey);
            _zone = zone;
            _selector = selector;
            HasHeaders = columnNames.Any();
            Headers = columnNames ?? new string[0];
        }

        void ISheetBuilder.Customize(ExcelWorksheet _, ExcelWorkbook workbook)
        {
        }

        IEnumerable<object> ISheetBuilder.BuildRawSheet()
        {
            return _workouts
                .OrderByDescending(r => r.StartDate)
                .Select(_selector);
        }

        IEnumerable<WorkoutItem> ISheetBuilder<WorkoutItem>.BuildSummary()
        {
            return _workouts
                .GroupBy(s => new { s.StartDate.InZone(_zone).Date.Year, s.StartDate.InZone(_zone).Date.Month })
                .Select(x => new WorkoutItem(x.Key.Year, x.Key.Month, x.Sum(c => c.Distance), x.Sum(c => c.Duration)));
        }

        IEnumerable<WorkoutItem> ISheetBuilder<WorkoutItem>.BuildSummaryForDateRange(IRange<ZonedDateTime> dateRange)
        {
            return _workouts
                .Where(x => dateRange.Includes(x.StartDate.InZone(_zone), Clusivity.Inclusive))
                .GroupBy(x => x.StartDate.InZone(_zone).Date)
                .Select(x => new WorkoutItem(x.Key, x.Sum(c => c.Distance), x.Sum(c => c.Duration)))
                .OrderByDescending(x => x.Date);
        }


        public class WorkoutItem : DatedItem
        {
            public WorkoutItem(LocalDate date, Length distance, UNDuration duration) : base(date)
            {
                Distance = distance;
                Duration = duration;
            }

            public WorkoutItem(int year, int month, Length distance, UNDuration duration) : base(year, month)
            {
                Distance = distance;
                Duration = duration;
            }

            public Length Distance { get; }
            public UNDuration Duration { get; }
        }
    }
}
