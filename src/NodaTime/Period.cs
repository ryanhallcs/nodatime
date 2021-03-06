// Copyright 2010 The Noda Time Authors. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using static NodaTime.NodaConstants;

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using NodaTime.Annotations;
using NodaTime.Fields;
using NodaTime.Text;
using NodaTime.Utility;

namespace NodaTime
{
    /// <summary>
    /// Represents a period of time expressed in human chronological terms: hours, days,
    /// weeks, months and so on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <see cref="Period"/> contains a set of properties such as <see cref="Years"/>, <see cref="Months"/>, and so on
    /// that return the number of each unit contained within this period. Note that these properties are not normalized in
    /// any way by default, and so a <see cref="Period"/> may contain values such as "2 hours and 90 minutes". The
    /// <see cref="Normalize"/> method will convert equivalent periods into a standard representation.
    /// </para>
    /// <para>
    /// Periods can contain negative units as well as positive units ("+2 hours, -43 minutes, +10 seconds"), but do not
    /// differentiate between properties that are zero and those that are absent (i.e. a period created as "10 years"
    /// and one created as "10 years, zero months" are equal periods; the <see cref="Months"/> property returns zero in
    /// both cases).
    /// </para>
    /// <para>
    /// <see cref="Period"/> equality is implemented by comparing each property's values individually.
    /// </para>
    /// <para>
    /// Periods operate on calendar-related types such as
    /// <see cref="LocalDateTime" /> whereas <see cref="Duration"/> operates on instants
    /// on the time line. (Note that although <see cref="ZonedDateTime" /> includes both concepts, it only supports
    /// duration-based arithmetic.)
    /// </para>
    /// </remarks>
    /// <threadsafety>This type is immutable reference type. See the thread safety section of the user guide for more information.</threadsafety>
#if !PCL
    [Serializable]
#endif
    [Immutable]
    public sealed class Period : IEquatable<Period>
#if !PCL
        , ISerializable
#endif
    {
        /// <summary>
        /// A period containing only zero-valued properties.
        /// </summary>
        public static Period Zero { get; } = new Period(0, 0, 0, 0);

        /// <summary>
        /// Returns an equality comparer which compares periods by first normalizing them - so 24 hours is deemed equal to 1 day, and so on.
        /// Note that as per the <see cref="Normalize"/> method, years and months are unchanged by normalization - so 12 months does not
        /// equal 1 year.
        /// </summary>
        /// <value>An equality comparer which compares periods by first normalizing them.</value>
        public static IEqualityComparer<Period> NormalizingEqualityComparer => NormalizingPeriodEqualityComparer.Instance;

        // The fields that make up this period.

        /// <summary>
        /// Gets the number of nanoseconds within this period.
        /// </summary>
        /// <remarks>
        /// This property returns zero both when the property has been explicitly set to zero and when the period does not
        /// contain this property.
        /// </remarks>
        /// /// <value>The number of nanoseconds within this period.</value>
        public long Nanoseconds { get; }

        /// <summary>
        /// Gets the number of ticks within this period.
        /// </summary>
        /// <remarks>
        /// This property returns zero both when the property has been explicitly set to zero and when the period does not
        /// contain this property.
        /// </remarks>
        /// <value>The number of ticks within this period.</value>
        public long Ticks { get; }

        /// <summary>
        /// Gets the number of milliseconds within this period.
        /// </summary>
        /// <remarks>
        /// This property returns zero both when the property has been explicitly set to zero and when the period does not
        /// contain this property.
        /// </remarks>
        /// <value>The number of milliseconds within this period.</value>
        public long Milliseconds { get; }

        /// <summary>
        /// Gets the number of seconds within this period.
        /// </summary>
        /// <remarks>
        /// This property returns zero both when the property has been explicitly set to zero and when the period does not
        /// contain this property.
        /// </remarks>
        /// <value>The number of seconds within this period.</value>
        public long Seconds { get; }

        /// <summary>
        /// Gets the number of minutes within this period.
        /// </summary>
        /// <remarks>
        /// This property returns zero both when the property has been explicitly set to zero and when the period does not
        /// contain this property.
        /// </remarks>
        /// <value>The number of minutes within this period.</value>
        public long Minutes { get; }
        
        /// <summary>
        /// Gets the number of hours within this period.
        /// </summary>
        /// <remarks>
        /// This property returns zero both when the property has been explicitly set to zero and when the period does not
        /// contain this property.
        /// </remarks>
        /// <value>The number of hours within this period.</value>
        public long Hours { get; }

        /// <summary>
        /// Gets the number of days within this period.
        /// </summary>
        /// <remarks>
        /// This property returns zero both when the property has been explicitly set to zero and when the period does not
        /// contain this property.
        /// </remarks>
        /// <value>The number of days within this period.</value>
        public int Days { get; }

        /// <summary>
        /// Gets the number of weeks within this period.
        /// </summary>
        /// <remarks>
        /// This property returns zero both when the property has been explicitly set to zero and when the period does not
        /// contain this property.
        /// </remarks>
        /// <value>The number of weeks within this period.</value>
        public int Weeks { get; }

        /// <summary>
        /// Gets the number of months within this period.
        /// </summary>
        /// <remarks>
        /// This property returns zero both when the property has been explicitly set to zero and when the period does not
        /// contain this property.
        /// </remarks>
        /// <value>The number of months within this period.</value>
        public int Months { get; }

        /// <summary>
        /// Gets the number of years within this period.
        /// </summary>
        /// <remarks>
        /// This property returns zero both when the property has been explicitly set to zero and when the period does not
        /// contain this property.
        /// </remarks>
        /// <value>The number of years within this period.</value>
        public int Years { get; }

        /// <summary>
        /// Creates a period with the given date values.
        /// </summary>
        private Period(int years, int months, int weeks, int days)
        {
            this.Years = years;
            this.Months = months;
            this.Weeks = weeks;
            this.Days = days;
        }

        /// <summary>
        /// Creates a period with the given time values.
        /// </summary>
        private Period(long hours, long minutes, long seconds, long milliseconds, long ticks, long nanoseconds)
        {
            this.Hours = hours;
            this.Minutes = minutes;
            this.Seconds = seconds;
            this.Milliseconds = milliseconds;
            this.Ticks = ticks;
            this.Nanoseconds = nanoseconds;
        }

        /// <summary>
        /// Creates a new period from the given values.
        /// </summary>
        internal Period(int years, int months, int weeks, int days, long hours, long minutes, long seconds,
            long milliseconds, long ticks, long nanoseconds)
        {
            this.Years = years;
            this.Months = months;
            this.Weeks = weeks;
            this.Days = days;
            this.Hours = hours;
            this.Minutes = minutes;
            this.Seconds = seconds;
            this.Milliseconds = milliseconds;
            this.Ticks = ticks;
            this.Nanoseconds = nanoseconds;
        }

        /// <summary>
        /// Creates a period representing the specified number of years.
        /// </summary>
        /// <param name="years">The number of years in the new period</param>
        /// <returns>A period consisting of the given number of years.</returns>
        [NotNull]
        public static Period FromYears(int years) => new Period(years, 0, 0, 0);

        /// <summary>
        /// Creates a period representing the specified number of months.
        /// </summary>
        /// <param name="months">The number of months in the new period</param>
        /// <returns>A period consisting of the given number of months.</returns>
        [NotNull]
        public static Period FromMonths(int months) => new Period(0, months, 0, 0);

        /// <summary>
        /// Creates a period representing the specified number of weeks.
        /// </summary>
        /// <param name="weeks">The number of weeks in the new period</param>
        /// <returns>A period consisting of the given number of weeks.</returns>
        [NotNull]
        public static Period FromWeeks(int weeks) => new Period(0, 0, weeks, 0);

        /// <summary>
        /// Creates a period representing the specified number of days.
        /// </summary>
        /// <param name="days">The number of days in the new period</param>
        /// <returns>A period consisting of the given number of days.</returns>
        [NotNull]
        public static Period FromDays(int days) => new Period(0, 0, 0, days);

        /// <summary>
        /// Creates a period representing the specified number of hours.
        /// </summary>
        /// <param name="hours">The number of hours in the new period</param>
        /// <returns>A period consisting of the given number of hours.</returns>
        [NotNull]
        public static Period FromHours(long hours) => new Period(hours, 0L, 0L, 0L, 0L, 0L);

        /// <summary>
        /// Creates a period representing the specified number of minutes.
        /// </summary>
        /// <param name="minutes">The number of minutes in the new period</param>
        /// <returns>A period consisting of the given number of minutes.</returns>
        [NotNull]
        public static Period FromMinutes(long minutes) => new Period(0L, minutes, 0L, 0L, 0L, 0L);

        /// <summary>
        /// Creates a period representing the specified number of seconds.
        /// </summary>
        /// <param name="seconds">The number of seconds in the new period</param>
        /// <returns>A period consisting of the given number of seconds.</returns>
        [NotNull]
        public static Period FromSeconds(long seconds) => new Period(0L, 0L, seconds, 0L, 0L, 0L);

        /// <summary>
        /// Creates a period representing the specified number of milliseconds.
        /// </summary>
        /// <param name="milliseconds">The number of milliseconds in the new period</param>
        /// <returns>A period consisting of the given number of milliseconds.</returns>
        [NotNull]
        public static Period FromMilliseconds(long milliseconds) => new Period(0L, 0L, 0L, milliseconds, 0L, 0L);

        /// <summary>
        /// Creates a period representing the specified number of ticks.
        /// </summary>
        /// <param name="ticks">The number of ticks in the new period</param>
        /// <returns>A period consisting of the given number of ticks.</returns>
        [NotNull]
        public static Period FromTicks(long ticks) => new Period(0L, 0L, 0L, 0L, ticks, 0L);

        /// <summary>
        /// Creates a period representing the specified number of nanooseconds.
        /// </summary>
        /// <param name="nanoseconds">The number of nanoseconds in the new period</param>
        /// <returns>A period consisting of the given number of nanoseconds.</returns>
        [NotNull]
        public static Period FromNanoseconds(long nanoseconds) => new Period(0L, 0L, 0L, 0L, 0L, nanoseconds);

        /// <summary>
        /// Adds two periods together, by simply adding the values for each property.
        /// </summary>
        /// <param name="left">The first period to add</param>
        /// <param name="right">The second period to add</param>
        /// <returns>The sum of the two periods. The units of the result will be the union of those in both
        /// periods.</returns>
        [NotNull]
        public static Period operator +([NotNull] Period left, [NotNull] Period right)
        {
            Preconditions.CheckNotNull(left, nameof(left));
            Preconditions.CheckNotNull(right, nameof(right));
            return new Period(
                left.Years + right.Years,
                left.Months + right.Months,
                left.Weeks + right.Weeks,
                left.Days + right.Days,
                left.Hours + right.Hours,
                left.Minutes + right.Minutes,
                left.Seconds + right.Seconds,
                left.Milliseconds + right.Milliseconds,
                left.Ticks + right.Ticks,
                left.Nanoseconds + right.Nanoseconds);
        }

        /// <summary>
        /// Creates an <see cref="IComparer{T}"/> for periods, using the given "base" local date/time.
        /// </summary>
        /// <remarks>
        /// Certain periods can't naturally be compared without more context - how "one month" compares to
        /// "30 days" depends on where you start. In order to compare two periods, the returned comparer
        /// effectively adds both periods to the "base" specified by <paramref name="baseDateTime"/> and compares
        /// the results. In some cases this arithmetic isn't actually required - when two periods can be
        /// converted to durations, the comparer uses that conversion for efficiency.
        /// </remarks>
        /// <param name="baseDateTime">The base local date/time to use for comparisons.</param>
        /// <returns>The new comparer.</returns>
        [NotNull]
        public static IComparer<Period> CreateComparer(LocalDateTime baseDateTime) => new PeriodComparer(baseDateTime);

        /// <summary>
        /// Subtracts one period from another, by simply subtracting each property value.
        /// </summary>
        /// <param name="minuend">The period to subtract the second operand from</param>
        /// <param name="subtrahend">The period to subtract the first operand from</param>
        /// <returns>The result of subtracting all the values in the second operand from the values in the first. The
        /// units of the result will be the union of both periods, even if the subtraction caused some properties to
        /// become zero (so "2 weeks, 1 days" minus "2 weeks" is "zero weeks, 1 days", not "1 days").</returns>
        [NotNull]
        public static Period operator -([NotNull] Period minuend, [NotNull] Period subtrahend)
        {
            Preconditions.CheckNotNull(minuend, nameof(minuend));
            Preconditions.CheckNotNull(subtrahend, nameof(subtrahend));
            return new Period(
                minuend.Years - subtrahend.Years,
                minuend.Months - subtrahend.Months,
                minuend.Weeks - subtrahend.Weeks,
                minuend.Days - subtrahend.Days,
                minuend.Hours - subtrahend.Hours,
                minuend.Minutes - subtrahend.Minutes,
                minuend.Seconds - subtrahend.Seconds,
                minuend.Milliseconds - subtrahend.Milliseconds,
                minuend.Ticks - subtrahend.Ticks,
                minuend.Nanoseconds - subtrahend.Nanoseconds);
        }

        /// <summary>
        /// Returns the period between a start and an end date/time, using only the given units.
        /// </summary>
        /// <remarks>
        /// If <paramref name="end"/> is before <paramref name="start" />, each property in the returned period
        /// will be negative. If the given set of units cannot exactly reach the end point (e.g. finding
        /// the difference between 1am and 3:15am in hours) the result will be such that adding it to <paramref name="start"/>
        /// will give a value between <paramref name="start"/> and <paramref name="end"/>. In other words,
        /// any rounding is "towards start"; this is true whether the resulting period is negative or positive.
        /// </remarks>
        /// <param name="start">Start date/time</param>
        /// <param name="end">End date/time</param>
        /// <param name="units">Units to use for calculations</param>
        /// <exception cref="ArgumentException"><paramref name="units"/> is empty or contained unknown values.</exception>
        /// <exception cref="ArgumentException"><paramref name="start"/> and <paramref name="end"/> use different calendars.</exception>
        /// <returns>The period between the given date/times, using the given units.</returns>
        [NotNull]
        public static Period Between(LocalDateTime start, LocalDateTime end, PeriodUnits units)
        {
            Preconditions.CheckArgument(units != 0, nameof(units), "Units must not be empty");
            Preconditions.CheckArgument((units & ~PeriodUnits.AllUnits) == 0, nameof(units), "Units contains an unknown value: {0}", units);
            CalendarSystem calendar = start.Calendar;
            Preconditions.CheckArgument(calendar.Equals(end.Calendar), nameof(end), "start and end must use the same calendar system");

            if (start == end)
            {
                return Zero;
            }

            // Adjust for situations like "days between 5th January 10am and 7th Janary 5am" which should be one
            // day, because if we actually reach 7th January with date fields, we've overshot.
            // The date adjustment will always be valid, because it's just moving it towards start.
            // We need this for all date-based period fields. We could potentially optimize by not doing this
            // in cases where we've only got time fields...
            LocalDate endDate = end.Date;
            if (start < end)
            {
                if (start.TimeOfDay > end.TimeOfDay)
                {
                    endDate = endDate.PlusDays(-1);
                }
            }
            else if (start > end && start.TimeOfDay < end.TimeOfDay)
            {
                endDate = endDate.PlusDays(1);
            }

            // Optimization for single field
            switch (units)
            {
                case PeriodUnits.Years: return FromYears(DatePeriodFields.YearsField.Subtract(endDate, start.Date));
                case PeriodUnits.Months: return FromMonths(DatePeriodFields.MonthsField.Subtract(endDate, start.Date));
                case PeriodUnits.Weeks: return FromWeeks(DatePeriodFields.WeeksField.Subtract(endDate, start.Date));
                case PeriodUnits.Days: return FromDays(DatePeriodFields.DaysField.Subtract(endDate, start.Date));
                case PeriodUnits.Hours: return FromHours(GetTimeBetween(start, end, TimePeriodField.Hours));
                case PeriodUnits.Minutes: return FromMinutes(GetTimeBetween(start, end, TimePeriodField.Minutes));
                case PeriodUnits.Seconds: return FromSeconds(GetTimeBetween(start, end, TimePeriodField.Seconds));
                case PeriodUnits.Milliseconds: return FromMilliseconds(GetTimeBetween(start, end, TimePeriodField.Milliseconds));
                case PeriodUnits.Ticks: return FromTicks(GetTimeBetween(start, end, TimePeriodField.Ticks));
                case PeriodUnits.Nanoseconds: return FromNanoseconds(GetTimeBetween(start, end, TimePeriodField.Nanoseconds));
            }

            // Multiple fields
            LocalDateTime remaining = start;
            int years = 0, months = 0, weeks = 0, days = 0;
            if ((units & PeriodUnits.AllDateUnits) != 0)
            {
                LocalDate remainingDate = start.Date;
                years = FieldBetween(units & PeriodUnits.Years, endDate, ref remainingDate, DatePeriodFields.YearsField);
                months = FieldBetween(units & PeriodUnits.Months, endDate, ref remainingDate, DatePeriodFields.MonthsField);
                weeks = FieldBetween(units & PeriodUnits.Weeks, endDate, ref remainingDate, DatePeriodFields.WeeksField);
                days = FieldBetween(units & PeriodUnits.Days, endDate, ref remainingDate, DatePeriodFields.DaysField);
                remaining = new LocalDateTime(remainingDate, start.TimeOfDay);
            }

            long hours = 0, minutes = 0, seconds = 0, milliseconds = 0, ticks = 0, nanoseconds = 0;
            if ((units & PeriodUnits.AllTimeUnits) != 0)
            {
                hours = FieldBetween(units & PeriodUnits.Hours, end, ref remaining, TimePeriodField.Hours);
                minutes = FieldBetween(units & PeriodUnits.Minutes, end, ref remaining, TimePeriodField.Minutes);
                seconds = FieldBetween(units & PeriodUnits.Seconds, end, ref remaining, TimePeriodField.Seconds);
                milliseconds = FieldBetween(units & PeriodUnits.Milliseconds, end, ref remaining, TimePeriodField.Milliseconds);
                ticks = FieldBetween(units & PeriodUnits.Ticks, end, ref remaining, TimePeriodField.Ticks);
                nanoseconds = FieldBetween(units & PeriodUnits.Ticks, end, ref remaining, TimePeriodField.Nanoseconds);
            }

            return new Period(years, months, weeks, days, hours, minutes, seconds, milliseconds, ticks, nanoseconds);
        }

        private static Duration GetNanosecondsBetween(LocalDateTime start, LocalDateTime end) =>
            // TODO(2.0): Optimize this for the common case of the dates being the same.
            end.ToLocalInstant().TimeSinceLocalEpoch - start.ToLocalInstant().TimeSinceLocalEpoch;

        private static int FieldBetween(PeriodUnits units, LocalDate end, ref LocalDate remaining, IDatePeriodField dateField)
        {
            if (units == 0)
            {
                return 0;
            }
            int value = dateField.Subtract(end, remaining);
            remaining = dateField.Add(remaining, value);
            return value;
        }

        private static long FieldBetween(PeriodUnits units, LocalDateTime end, ref LocalDateTime remaining, TimePeriodField timeField)
        {
            if (units == 0)
            {
                return 0;
            }
            long value = GetTimeBetween(remaining, end, timeField);
            remaining = timeField.Add(remaining, value);
            return value;
        }

        private static long FieldBetween(PeriodUnits units, LocalTime end, ref LocalTime remaining, TimePeriodField timeField)
        {
            if (units == 0)
            {
                return 0;
            }
            long value = timeField.Subtract(end, remaining);
            remaining = timeField.Add(remaining, value);
            return value;
        }

        private static long GetTimeBetween(LocalDateTime start, LocalDateTime end, TimePeriodField periodField)
        {
            int days = DatePeriodFields.DaysField.Subtract(end.Date, start.Date);
            long units = periodField.Subtract(end.TimeOfDay, start.TimeOfDay);
            return units + days * periodField.UnitsPerDay;
        }

        /// <summary>
        /// Adds the time components of this period to the given time, scaled accordingly.
        /// </summary>
        [Pure]
        internal LocalTime AddTo(LocalTime time, int scalar) =>
            time.PlusHours(Hours * scalar)
                .PlusMinutes(Minutes * scalar)
                .PlusSeconds(Seconds * scalar)
                .PlusMilliseconds(Milliseconds * scalar)
                .PlusTicks(Ticks * scalar)
                // FIXME(2.0): Cope with larger nanosecond values
                .PlusNanoseconds(Nanoseconds * scalar);

        /// <summary>
        /// Adds the date components of this period to the given time, scaled accordingly.
        /// </summary>
        [Pure]
        internal LocalDate AddTo(LocalDate date, int scalar) =>
            date.PlusYears(Years * scalar)
                .PlusMonths(Months * scalar)
                .PlusWeeks(Weeks * scalar)
                .PlusDays(Days * scalar);

        /// <summary>
        /// Adds the contents of this period to the given date and time, with the given scale (either 1 or -1, usually).
        /// </summary>
        internal LocalDateTime AddTo(LocalDate date, LocalTime time, int scalar)
        {
            date = AddTo(date, scalar);
            int extraDays = 0;
            time = TimePeriodField.Hours.Add(time, Hours * scalar, ref extraDays);
            time = TimePeriodField.Minutes.Add(time, Minutes * scalar, ref extraDays);
            time = TimePeriodField.Seconds.Add(time, Seconds * scalar, ref extraDays);
            time = TimePeriodField.Milliseconds.Add(time, Milliseconds * scalar, ref extraDays);
            time = TimePeriodField.Ticks.Add(time, Ticks * scalar, ref extraDays);
            // FIXME(2.0): Cope with larger nanosecond values
            time = TimePeriodField.Nanoseconds.Add(time, Nanoseconds * scalar, ref extraDays);
            // TODO(2.0): Investigate the performance impact of us calling PlusDays twice.
            // Could optimize by including that in a single call...
            return new LocalDateTime(date.PlusDays(extraDays), time);
        }

        /// <summary>
        /// Returns the exact difference between two date/times.
        /// </summary>
        /// <remarks>
        /// If <paramref name="end"/> is before <paramref name="start" />, each property in the returned period
        /// will be negative.
        /// </remarks>
        /// <param name="start">Start date/time</param>
        /// <param name="end">End date/time</param>
        /// <returns>The period between the two date and time values, using all units.</returns>
        [Pure]
        [NotNull]
        public static Period Between(LocalDateTime start, LocalDateTime end) => Between(start, end, PeriodUnits.DateAndTime);

        /// <summary>
        /// Returns the period between a start and an end date, using only the given units.
        /// </summary>
        /// <remarks>
        /// If <paramref name="end"/> is before <paramref name="start" />, each property in the returned period
        /// will be negative. If the given set of units cannot exactly reach the end point (e.g. finding
        /// the difference between 12th February and 15th March in months) the result will be such that adding it to <paramref name="start"/>
        /// will give a value between <paramref name="start"/> and <paramref name="end"/>. In other words,
        /// any rounding is "towards start"; this is true whether the resulting period is negative or positive.
        /// </remarks>
        /// <param name="start">Start date</param>
        /// <param name="end">End date</param>
        /// <param name="units">Units to use for calculations</param>
        /// <exception cref="ArgumentException"><paramref name="units"/> contains time units, is empty or contains unknown values.</exception>
        /// <exception cref="ArgumentException"><paramref name="start"/> and <paramref name="end"/> use different calendars.</exception>
        /// <returns>The period between the given dates, using the given units.</returns>
        [Pure]
        [NotNull]
        public static Period Between(LocalDate start, LocalDate end, PeriodUnits units)
        {
            Preconditions.CheckArgument((units & PeriodUnits.AllTimeUnits) == 0, nameof(units), "Units contains time units: {0}", units);
            Preconditions.CheckArgument(units != 0, nameof(units), "Units must not be empty");
            Preconditions.CheckArgument((units & ~PeriodUnits.AllUnits) == 0, nameof(units), "Units contains an unknown value: {0}", units);
            CalendarSystem calendar = start.Calendar;
            Preconditions.CheckArgument(calendar.Equals(end.Calendar), nameof(end), "start and end must use the same calendar system");

            if (start == end)
            {
                return Zero;
            }

            // Optimization for single field
            switch (units)
            {
                case PeriodUnits.Years: return FromYears(DatePeriodFields.YearsField.Subtract(end, start));
                case PeriodUnits.Months: return FromMonths(DatePeriodFields.MonthsField.Subtract(end, start));
                case PeriodUnits.Weeks: return FromWeeks(DatePeriodFields.WeeksField.Subtract(end, start));
                case PeriodUnits.Days: return FromDays(DatePeriodFields.DaysField.Subtract(end, start));
            }

            // Multiple fields
            LocalDate remainingDate = start;
            int years = FieldBetween(units & PeriodUnits.Years, end, ref remainingDate, DatePeriodFields.YearsField);
            int months = FieldBetween(units & PeriodUnits.Months, end, ref remainingDate, DatePeriodFields.MonthsField);
            int weeks = FieldBetween(units & PeriodUnits.Weeks, end, ref remainingDate, DatePeriodFields.WeeksField);
            int days = FieldBetween(units & PeriodUnits.Days, end, ref remainingDate, DatePeriodFields.DaysField);
            return new Period(years, months, weeks, days);
        }

        /// <summary>
        /// Returns the exact difference between two dates.
        /// </summary>
        /// <remarks>
        /// If <paramref name="end"/> is before <paramref name="start" />, each property in the returned period
        /// will be negative.
        /// </remarks>
        /// <param name="start">Start date</param>
        /// <param name="end">End date</param>
        /// <returns>The period between the two dates, using year, month and day units.</returns>
        [Pure]
        [NotNull]
        public static Period Between(LocalDate start, LocalDate end) => Between(start, end, PeriodUnits.YearMonthDay);

        /// <summary>
        /// Returns the period between a start and an end time, using only the given units.
        /// </summary>
        /// <remarks>
        /// If <paramref name="end"/> is before <paramref name="start" />, each property in the returned period
        /// will be negative. If the given set of units cannot exactly reach the end point (e.g. finding
        /// the difference between 3am and 4.30am in hours) the result will be such that adding it to <paramref name="start"/>
        /// will give a value between <paramref name="start"/> and <paramref name="end"/>. In other words,
        /// any rounding is "towards start"; this is true whether the resulting period is negative or positive.
        /// </remarks>
        /// <param name="start">Start time</param>
        /// <param name="end">End time</param>
        /// <param name="units">Units to use for calculations</param>
        /// <exception cref="ArgumentException"><paramref name="units"/> contains date units, is empty or contains unknown values.</exception>
        /// <exception cref="ArgumentException"><paramref name="start"/> and <paramref name="end"/> use different calendars.</exception>
        /// <returns>The period between the given times, using the given units.</returns>
        [Pure]
        [NotNull]
        public static Period Between(LocalTime start, LocalTime end, PeriodUnits units)
        {
            Preconditions.CheckArgument((units & PeriodUnits.AllDateUnits) == 0, nameof(units), "Units contains date units: {0}", units);
            Preconditions.CheckArgument(units != 0, nameof(units), "Units must not be empty");
            Preconditions.CheckArgument((units & ~PeriodUnits.AllUnits) == 0, nameof(units), "Units contains an unknown value: {0}", units);

            // Optimization for single field
            switch (units)
            {
                case PeriodUnits.Hours: return FromHours(TimePeriodField.Hours.Subtract(end, start));
                case PeriodUnits.Minutes: return FromMinutes(TimePeriodField.Minutes.Subtract(end, start));
                case PeriodUnits.Seconds: return FromSeconds(TimePeriodField.Seconds.Subtract(end, start));
                case PeriodUnits.Milliseconds: return FromMilliseconds(TimePeriodField.Milliseconds.Subtract(end, start));
                case PeriodUnits.Ticks: return FromTicks(TimePeriodField.Ticks.Subtract(end, start));
                case PeriodUnits.Nanoseconds: return FromNanoseconds(TimePeriodField.Nanoseconds.Subtract(end, start));
            }

            LocalTime remaining = start;
            long hours = FieldBetween(units & PeriodUnits.Hours, end, ref remaining, TimePeriodField.Hours);
            long minutes = FieldBetween(units & PeriodUnits.Minutes, end, ref remaining, TimePeriodField.Minutes);
            long seconds = FieldBetween(units & PeriodUnits.Seconds, end, ref remaining, TimePeriodField.Seconds);
            long milliseconds = FieldBetween(units & PeriodUnits.Milliseconds, end, ref remaining, TimePeriodField.Milliseconds);
            long ticks = FieldBetween(units & PeriodUnits.Ticks, end, ref remaining, TimePeriodField.Ticks);
            long nanoseconds = FieldBetween(units & PeriodUnits.Nanoseconds, end, ref remaining, TimePeriodField.Nanoseconds);

            return new Period(hours, minutes, seconds, milliseconds, ticks, nanoseconds);
        }

        /// <summary>
        /// Returns the exact difference between two times.
        /// </summary>
        /// <remarks>
        /// If <paramref name="end"/> is before <paramref name="start" />, each property in the returned period
        /// will be negative.
        /// </remarks>
        /// <param name="start">Start time</param>
        /// <param name="end">End time</param>
        /// <returns>The period between the two times, using the time period units.</returns>
        [Pure]
        [NotNull]
        public static Period Between(LocalTime start, LocalTime end) => Between(start, end, PeriodUnits.AllTimeUnits);

        /// <summary>
        /// Returns whether or not this period contains any non-zero-valued time-based properties (hours or lower).
        /// </summary>
        /// <value>true if the period contains any non-zero-valued time-based properties (hours or lower); false otherwise.</value>
        public bool HasTimeComponent => Hours != 0 || Minutes != 0 || Seconds != 0 || Milliseconds != 0 || Ticks != 0 || Nanoseconds != 0;

        /// <summary>
        /// Returns whether or not this period contains any non-zero date-based properties (days or higher).
        /// </summary>
        /// <value>true if this period contains any non-zero date-based properties (days or higher); false otherwise.</value>
        public bool HasDateComponent => Years != 0 || Months != 0 || Weeks != 0 || Days != 0;

        /// <summary>
        /// For periods that do not contain a non-zero number of years or months, returns a duration for this period
        /// assuming a standard 7-day week, 24-hour day, 60-minute hour etc.
        /// </summary>
        /// <exception cref="InvalidOperationException">The month or year property in the period is non-zero.</exception>
        /// <exception cref="OverflowException">The period doesn't have years or months, but the calculation
        /// overflows the bounds of <see cref="Duration"/>. In some cases this may occur even though the theoretical
        /// result would be valid due to balancing positive and negative values, but for simplicity there is
        /// no attempt to work around this - in realistic periods, it shouldn't be a problem.</exception>
        /// <returns>The duration of the period.</returns>
        [Pure]
        public Duration ToDuration()
        {
            if (Months != 0 || Years != 0)
            {
                throw new InvalidOperationException("Cannot construct duration of period with non-zero months or years.");
            }
            return Duration.FromNanoseconds(TotalNanoseconds);
        }

        /// <summary>
        /// Gets the total number of nanoseconds duration for the 'standard' properties (all bar years and months).
        /// </summary>
        /// <value>The total number of nanoseconds duration for the 'standard' properties (all bar years and months).</value>
        private long TotalNanoseconds =>
            // This can overflow even when it wouldn't necessarily need to. See comments elsewhere.
            // TODO(2.0): Handle big nanosecond values. (Return Nanoseconds instead...)
            Nanoseconds +
                Ticks * NanosecondsPerTick +
                Milliseconds * NanosecondsPerMillisecond +
                Seconds * NanosecondsPerSecond +
                Minutes * NanosecondsPerMinute +
                Hours * NanosecondsPerHour +
                Days * NanosecondsPerDay +
                Weeks * NanosecondsPerWeek;

        /// <summary>
        /// Creates a <see cref="PeriodBuilder"/> from this instance. The new builder
        /// is populated with the values from this period, but is then detached from it:
        /// changes made to the builder are not reflected in this period.
        /// </summary>
        /// <returns>A builder with the same values and units as this period.</returns>
        [Pure]
        [NotNull]
        public PeriodBuilder ToBuilder() => new PeriodBuilder(this);

        // FIXME(2.0): Normalize to a particular fraction-of-second type?
        /// <summary>
        /// Returns a normalized version of this period, such that equivalent (but potentially non-equal) periods are
        /// changed to the same representation.
        /// </summary>
        /// <remarks>
        /// Months and years are unchanged
        /// (as they can vary in length), but weeks are multiplied by 7 and added to the
        /// Days property, and all time properties are normalized to their natural range
        /// (where ticks are "within a millisecond"), adding to the larger property where
        /// necessary. So for example, a period of 25 hours becomes a period of 1 day
        /// and 1 hour. Aside from months and years, either all the properties
        /// end up positive, or they all end up negative.
        /// </remarks>
        /// <exception cref="OverflowException">The period doesn't have years or months, but it contains more than
        /// <see cref="Int64.MaxValue"/> ticks when the combined weeks/days/time portions are considered. Such a period
        /// could never be useful anyway, however.
        /// In some cases this may occur even though the theoretical result would be valid due to balancing positive and
        /// negative values, but for simplicity there is no attempt to work around this - in realistic periods, it
        /// shouldn't be a problem.</exception>
        /// <returns>The normalized period.</returns>
        /// <seealso cref="NormalizingEqualityComparer"/>
        [Pure]
        [NotNull]
        public Period Normalize()
        {
            // FIXME: Normalize to a Nanoseconds value instead, then go from there.
            // Simplest way to normalize: grab all the fields up to "week" and
            // sum them.
            long totalNanoseconds = TotalNanoseconds;
            int days = (int) (totalNanoseconds / NanosecondsPerDay);
            long hours = (totalNanoseconds / NanosecondsPerHour) % HoursPerDay;
            long minutes = (totalNanoseconds / NanosecondsPerMinute) % MinutesPerHour;
            long seconds = (totalNanoseconds / NanosecondsPerSecond) % SecondsPerMinute;
            long milliseconds = (totalNanoseconds / NanosecondsPerMillisecond) % MillisecondsPerSecond;
            long ticks = (totalNanoseconds / NanosecondsPerTick) % TicksPerMillisecond;
            long nanoseconds = totalNanoseconds % NanosecondsPerTick;

            return new Period(this.Years, this.Months, 0 /* weeks */, days, hours, minutes, seconds, milliseconds, ticks, nanoseconds);
        }

        #region Object overrides

        /// <summary>
        /// Returns this string formatted according to the <see cref="PeriodPattern.RoundtripPattern"/>.
        /// </summary>
        /// <returns>A formatted representation of this period.</returns>
        public override string ToString() => PeriodPattern.RoundtripPattern.Format(this);

        /// <summary>
        /// Compares the given object for equality with this one, as per <see cref="Equals(Period)"/>.
        /// </summary>
        /// <param name="other">The value to compare this one with.</param>
        /// <returns>true if the other object is a period equal to this one, consistent with <see cref="Equals(Period)"/></returns>
        public override bool Equals(object other) => Equals(other as Period);

        /// <summary>
        /// Returns the hash code for this period, consistent with <see cref="Equals(Period)"/>.
        /// </summary>
        /// <returns>The hash code for this period.</returns>
        public override int GetHashCode() =>
            HashCodeHelper.Initialize()
                .Hash(Years)
                .Hash(Months)
                .Hash(Weeks)
                .Hash(Days)
                .Hash(Hours)
                .Hash(Minutes)
                .Hash(Seconds)
                .Hash(Milliseconds)
                .Hash(Ticks)
                .Hash(Nanoseconds)
                .Value;

        /// <summary>
        /// Compares the given period for equality with this one.
        /// </summary>
        /// <remarks>
        /// Periods are equal if they contain the same values for the same properties.
        /// However, no normalization takes place, so "one hour" is not equal to "sixty minutes".
        /// </remarks>
        /// <param name="other">The period to compare this one with.</param>
        /// <returns>True if this period has the same values for the same properties as the one specified.</returns>
        public bool Equals(Period other) =>
            other != null &&
            Years == other.Years &&
            Months == other.Months &&
            Weeks == other.Weeks &&
            Days == other.Days &&
            Hours == other.Hours &&
            Minutes == other.Minutes &&
            Seconds == other.Seconds &&
            Milliseconds == other.Milliseconds &&
            Ticks == other.Ticks &&
            Nanoseconds == other.Nanoseconds;
        #endregion

#if !PCL
        #region Binary serialization
        private const string YearsSerializationName = "years";
        private const string MonthsSerializationName = "months";
        private const string WeeksSerializationName = "weeks";
        private const string DaysSerializationName = "days";
        private const string HoursSerializationName = "hours";
        private const string MinutesSerializationName = "minutes";
        private const string SecondsSerializationName = "seconds";
        private const string MillisecondsSerializationName = "milliseconds";
        private const string TicksSerializationName = "ticks";
        private const string NanosecondsSerializationName = "nanosDays";

        /// <summary>
        /// Private constructor only present for serialization.
        /// TODO(2.0): Revisit this for 2.0.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> to fetch data from.</param>
        /// <param name="context">The source for this deserialization.</param>
        private Period(SerializationInfo info, StreamingContext context)
            : this((int) info.GetInt64(YearsSerializationName),
                   (int) info.GetInt64(MonthsSerializationName),
                   (int) info.GetInt64(WeeksSerializationName),
                   (int) info.GetInt64(DaysSerializationName),
                   info.GetInt64(HoursSerializationName),
                   info.GetInt64(MinutesSerializationName),
                   info.GetInt64(SecondsSerializationName),
                   info.GetInt64(MillisecondsSerializationName),
                   info.GetInt64(TicksSerializationName),
                   info.GetInt64(NanosecondsSerializationName))
        {
        }

        /// <summary>
        /// Implementation of <see cref="ISerializable.GetObjectData"/>.
        /// TODO(2.0): Revisit this for 2.0.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> to populate with data.</param>
        /// <param name="context">The destination for this serialization.</param>
        [System.Security.SecurityCritical]
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(YearsSerializationName, (long) Years);
            info.AddValue(MonthsSerializationName, (long) Months);
            info.AddValue(WeeksSerializationName, (long) Weeks);
            info.AddValue(DaysSerializationName, (long) Days);
            info.AddValue(HoursSerializationName, Hours);
            info.AddValue(MinutesSerializationName, Minutes);
            info.AddValue(SecondsSerializationName, Seconds);
            info.AddValue(MillisecondsSerializationName, Milliseconds);
            info.AddValue(TicksSerializationName, Ticks);
            info.AddValue(NanosecondsSerializationName, Nanoseconds);
        }
        #endregion
#endif


        /// <summary>
        /// Equality comparer which simply normalizes periods before comparing them.
        /// </summary>
        private sealed class NormalizingPeriodEqualityComparer : EqualityComparer<Period>
        {
            internal static readonly NormalizingPeriodEqualityComparer Instance = new NormalizingPeriodEqualityComparer();

            private NormalizingPeriodEqualityComparer()
            {
            }

            public override bool Equals(Period x, Period y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }
                if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                {
                    return false;
                }
                return x.Normalize().Equals(y.Normalize());
            }

            public override int GetHashCode([NotNull] Period obj) =>
                Preconditions.CheckNotNull(obj, nameof(obj)).Normalize().GetHashCode();
        }

        private sealed class PeriodComparer : Comparer<Period>
        {
            private readonly LocalDateTime baseDateTime;

            internal PeriodComparer(LocalDateTime baseDateTime)
            {
                this.baseDateTime = baseDateTime;
            }

            public override int Compare(Period x, Period y)
            {
                if (ReferenceEquals(x, y))
                {
                    return 0;
                }
                if (x == null)
                {
                    return -1;
                }
                if (y == null)
                {
                    return 1;
                }
                if (x.Months == 0 && y.Months == 0 &&
                    x.Years == 0 && y.Years == 0)
                {
                    // Note: this *could* throw an OverflowException when the normal approach
                    // wouldn't, but it's highly unlikely
                    return x.ToDuration().CompareTo(y.ToDuration());
                }
                return (baseDateTime + x).CompareTo(baseDateTime + y);
            }
        }
    }
}
