﻿using System;
using System.Collections.Generic;
using System.Linq;
using OfficeOpenXml;

namespace HealthParse.Standard.Health.Sheets
{
    public abstract class WorkoutBuilder : ISheetBuilder<WorkoutBuilder.WorkoutItem>
    {
        protected Dictionary<string, IEnumerable<Workout>> _workouts;
        private Func<Workout, object> _selector;
        private string _workoutKey;

        public WorkoutBuilder(Dictionary<string, IEnumerable<Workout>> workouts, string workoutKey, Func<Workout, object> selector)
        {
            _workouts = workouts;
            _selector = selector;
            _workoutKey = workoutKey;
        }

        void ISheetBuilder.Build(ExcelWorksheet sheet)
        {
            var workouts = _workouts[_workoutKey]
                 .OrderByDescending(r => r.StartDate)
                 .Select(_selector);

            sheet.WriteData(workouts);
        }

        IEnumerable<WorkoutItem> ISheetBuilder<WorkoutItem>.BuildSummary()
        {
            return _workouts[_workoutKey]
                .GroupBy(s => new { s.StartDate.Date.Year, s.StartDate.Date.Month })
                .Select(x => new WorkoutItem(x.Key.Year, x.Key.Month, x.Sum(c => c.TotalDistance ?? 0), x.Sum(c => c.Duration ?? 0)));
        }

        IEnumerable<WorkoutItem> ISheetBuilder<WorkoutItem>.BuildSummaryForDateRange(IRange<DateTime> dateRange)
        {
            return _workouts[_workoutKey]
                .Where(x => dateRange.Includes(x.StartDate))
                .GroupBy(x => x.StartDate.Date)
                .Select(x => new WorkoutItem(x.Key, x.Sum(c => c.TotalDistance) ?? 0, x.Sum(c => c.Duration) ?? 0))
                .OrderByDescending(x => x.Date);
        }

        public class WorkoutItem : DatedItem
        {
            public WorkoutItem(DateTime date, double distance, double duration) : base(date)
            {
                Distance = distance;
                Duration = duration;
            }

            public WorkoutItem(int year, int month, double distance, double duration) : base(year, month)
            {
                Distance = distance;
                Duration = duration;
            }

            public double Distance { get; }
            public double Duration { get; }
        }
    }
}