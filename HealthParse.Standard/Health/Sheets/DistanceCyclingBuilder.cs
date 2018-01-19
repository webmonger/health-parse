﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace HealthParse.Standard.Health.Sheets
{
    public class DistanceCyclingBuilder : ISheetBuilder<DistanceCyclingBuilder.CyclingItem>
    {
        private readonly IEnumerable<Record> _records;

        public DistanceCyclingBuilder(IReadOnlyDictionary<string, IEnumerable<Record>> records)
        {
            _records = records.ContainsKey(HKConstants.Records.DistanceCycling)
                ? records[HKConstants.Records.DistanceCycling]
                : Enumerable.Empty<Record>();
        }
        IEnumerable<object> ISheetBuilder.BuildRawSheet()
        {
            return _records
                .OrderByDescending(r => r.StartDate)
                .Select(r => new
                {
                    date = r.StartDate,
                    distance = r.Raw.Attribute("value").Value,
                    unit = r.Raw.Attribute("unit").Value
                });
        }

        IEnumerable<CyclingItem> ISheetBuilder<CyclingItem>.BuildSummary()
        {
            return _records
                .GroupBy(s => new { s.StartDate.Date.Year, s.StartDate.Date.Month })
                .Select(x => new CyclingItem(x.Key.Year, x.Key.Month, x.Sum(c => c.Raw.Attribute("value").ValueDouble(0) ?? 0)));
        }

        IEnumerable<CyclingItem> ISheetBuilder<CyclingItem>.BuildSummaryForDateRange(IRange<DateTime> dateRange)
        {
            return _records
                .Where(x => dateRange.Includes(x.StartDate))
                .GroupBy(x => x.StartDate.Date)
                .Select(x => new CyclingItem(x.Key, x.Sum(c => c.Raw.Attribute("value").ValueDouble(0) ?? 0)))
                .OrderBy(x => x.Date);
        }

        public class CyclingItem : DatedItem
        {
            public CyclingItem(DateTime date, double distance) : base(date)
            {
                Distance = distance;
            }

            public CyclingItem(int year, int month, double distance) : base(year, month)
            {
                Distance = distance;
            }

            public double Distance { get; }
        }
    }
}
