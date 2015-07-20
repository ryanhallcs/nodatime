// Copyright 2009 The Noda Time Authors. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Globalization;
using System.IO;
using NodaTime.TimeZones;
using NodaTime.Utility;

namespace NodaTime.TzdbCompiler.Tzdb
{
    /// <summary>
    /// Provides a parser for TZDB time zone description files.
    /// </summary>
    public class TzdbZoneInfoParser
    {
        /// <summary>
        /// The keyword that specifies the line defines an alias link.
        /// </summary>
        private const string KeywordLink = "Link";

        /// <summary>
        /// The keyword that specifies the line defines a daylight savings rule.
        /// </summary>
        private const string KeywordRule = "Rule";

        /// <summary>
        /// The keyword that specifies the line defines a time zone.
        /// </summary>
        private const string KeywordZone = "Zone";

        /// <summary>
        /// The days of the week names as they appear in the TZDB zone files. They are
        /// always the short name in US English.
        /// </summary>
        private static readonly string[] DaysOfWeek = { "", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

        /// <summary>
        /// The months of the year names as they appear in the TZDB zone files. They are
        /// always the short name in US English. 
        /// </summary>
        private static readonly string[] ShortMonths = { "", "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        /// <summary>
        /// ... except when they're actually the long month name, e.g. in Greece in 96d.
        /// (This is basically only for old files.)
        /// </summary>
        private static readonly string[] LongMonths =
            { "", "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

        /// <summary>
        /// Parses the next token as a month number (1-12).
        /// </summary>
        /// <param name="tokens">The tokens.</param>
        /// <param name="name">The name of the expected value, for use in the exception if no value is available.</param>
        private int NextMonth(Tokens tokens, string name)
        {
            var value = NextString(tokens, name);
            return ParseMonth(value);
        }

        /// <summary>
        /// Parses the next token as an offset.
        /// </summary>
        /// <param name="tokens">The tokens.</param>
        /// <param name="name">The name of the expected value, for use in the exception if no value is available.</param>
        private Offset NextOffset(Tokens tokens, string name)
        {
            return ParserHelper.ParseOffset(NextString(tokens, name));
        }

        /// <summary>
        /// Returns the next token, which is optional, converting "-" to null.
        /// </summary>
        /// <param name="tokens">The tokens.</param>
        /// <param name="name">The name of the expected value, for use in the exception if no value is available.</param>
        private string NextOptional(Tokens tokens, string name)
        {
            return ParserHelper.ParseOptional(NextString(tokens, name));
        }

        /// <summary>
        /// Returns the next string from the token stream.
        /// </summary>
        /// <param name="tokens">The tokens to parse from.</param>
        /// <param name="name">The name of the expected value, for use in the exception if no value is available.</param>
        private string NextString(Tokens tokens, string name)
        {
            if (!tokens.HasNextToken)
            {
                throw new InvalidDataException($"Missing zone info token: {name}");
            }
            return tokens.NextToken(name);
        }

        /// <summary>
        /// Parses the next string from the token stream as a year.
        /// </summary>
        /// <param name="tokens">The tokens.</param>
        /// <param name="defaultValue">The default value to return if the year isn't specified.</param>
        private static int NextYear(Tokens tokens, int defaultValue)
        {
            int result = defaultValue;
            string text;
            if (tokens.TryNextToken(out text))
            {
                result = ParserHelper.ParseYear(text, defaultValue);
            }
            return result;
        }

        /// <summary>
        /// Parses the TZDB time zone info file from the given stream and merges its information
        /// with the given database. The stream is not closed or disposed.
        /// </summary>
        /// <param name="input">The stream input to parse.</param>
        /// <param name="database">The database to fill.</param>
        public void Parse(Stream input, TzdbDatabase database)
        {
            Parse(new StreamReader(input, true), database);
        }

        /// <summary>
        /// Parses the TZDB time zone info file from the given reader and merges its information
        /// with the given database. The reader is not closed or disposed.
        /// </summary>
        /// <param name="reader">The reader to read.</param>
        /// <param name="database">The database to fill.</param>
        public void Parse(TextReader reader, TzdbDatabase database)
        {
            bool firstLine = true;
            for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
            {
                // Only bother with files which start with comments.
                if (firstLine && !line.StartsWith("# ", StringComparison.Ordinal))
                {
                    return;
                }
                firstLine = false;
                ParseLine(line, database);
            }
        }

        /// <summary>
        /// Parses the ZoneYearOffset for a rule or zone. This is something like "3rd Sunday of October at 2am".
        /// </summary>
        /// <remarks>
        /// IN ON AT
        /// </remarks>
        /// <param name="tokens">The tokens to parse.</param>
        /// <param name="forRule">True if this is for a Rule line, in which case ON/AT are mandatory;
        /// false for a Zone line, in which case it's part of "until" and they're optional</param>
        /// <returns>The ZoneYearOffset object.</returns>
        internal ZoneYearOffset ParseDateTimeOfYear(Tokens tokens, bool forRule)
        {
            var mode = ZoneYearOffset.StartOfYear.Mode;
            var timeOfDay = ZoneYearOffset.StartOfYear.TimeOfDay;

            int monthOfYear = NextMonth(tokens, "MonthOfYear");

            int dayOfMonth = 1;
            int dayOfWeek = 0;
            bool advanceDayOfWeek = false;
            bool addDay = false;

            if (tokens.HasNextToken || forRule)
            {
                var on = NextString(tokens, "On");
                if (on.StartsWith("last", StringComparison.Ordinal))
                {
                    dayOfMonth = -1;
                    dayOfWeek = ParseDayOfWeek(on.Substring(4));
                }
                else
                {
                    int index = on.IndexOf(">=", StringComparison.Ordinal);
                    if (index > 0)
                    {
                        dayOfMonth = Int32.Parse(on.Substring(index + 2), CultureInfo.InvariantCulture);
                        dayOfWeek = ParseDayOfWeek(on.Substring(0, index));
                        advanceDayOfWeek = true;
                    }
                    else
                    {
                        index = on.IndexOf("<=", StringComparison.Ordinal);
                        if (index > 0)
                        {
                            dayOfMonth = Int32.Parse(on.Substring(index + 2), CultureInfo.InvariantCulture);
                            dayOfWeek = ParseDayOfWeek(on.Substring(0, index));
                        }
                        else
                        {
                            try
                            {
                                dayOfMonth = Int32.Parse(on, CultureInfo.InvariantCulture);
                                dayOfWeek = 0;
                            }
                            catch (FormatException e)
                            {
                                throw new ArgumentException($"Unparsable ON token: {on}", e);
                            }
                        }
                    }
                }

                if (tokens.HasNextToken || forRule)
                {
                    var atTime = NextString(tokens, "AT");
                    if (!string.IsNullOrEmpty(atTime))
                    {
                        if (Char.IsLetter(atTime[atTime.Length - 1]))
                        {
                            char zoneCharacter = atTime[atTime.Length - 1];
                            mode = ConvertModeCharacter(zoneCharacter);
                            atTime = atTime.Substring(0, atTime.Length - 1);
                        }
                        if (atTime == "24:00")
                        {
                            timeOfDay = LocalTime.Midnight;
                            addDay = true;
                        }
                        else
                        {
                            timeOfDay = ParserHelper.ParseTime(atTime);
                        }
                    }
                }
            }
            return new ZoneYearOffset(mode, monthOfYear, dayOfMonth, dayOfWeek, advanceDayOfWeek, timeOfDay, addDay);
        }

        /// <summary>
        /// Parses the day of week.
        /// </summary>
        /// <param name="text">The text.</param>
        private static int ParseDayOfWeek(string text)
        {
            Preconditions.CheckArgument(!string.IsNullOrEmpty(text), "text", "Value must not be empty or null");
            int index = Array.IndexOf(DaysOfWeek, text, 1);
            if (index == -1)
            {
                throw new InvalidDataException($"Invalid day of week: {text}");
            }
            return index;
        }

        /// <summary>
        /// Parses a single line of an TZDB zone info file.
        /// </summary>
        /// <remarks>
        /// <para>
        /// TZDB files have a simple line based structure. Each line defines one item. Comments
        /// start with a hash or pound sign (#) and continue to the end of the line. Blank lines are
        /// ignored. Of the remaining there are four line types which are determined by the first
        /// keyword on the line.
        /// </para>
        /// <para>
        /// A line beginning with the keyword <c>Link</c> defines an alias between one time zone and
        /// another. Both time zones use the same definition but have different names.
        /// </para>
        /// <para>
        /// A line beginning with the keyword <c>Rule</c> defines a daylight savings time
        /// calculation rule.
        /// </para>
        /// <para>
        /// A line beginning with the keyword <c>Zone</c> defines a time zone.
        /// </para>
        /// <para>
        /// A line beginning with leading whitespace (an empty keyword) defines another part of the
        /// preceeding time zone. As many lines as necessary to define the time zone can be listed,
        /// but they must all be together and only the first line can have a name.
        /// </para>
        /// </remarks>
        /// <param name="line">The line to parse.</param>
        /// <param name="database">The database to fill.</param>
        internal void ParseLine(string line, TzdbDatabase database)
        {
            int index = line.IndexOf("#", StringComparison.Ordinal);
            if (index == 0)
            {
                return;
            }
            if (index > 0)
            {
                line = line.Substring(0, index - 1);
            }
            line = line.TrimEnd();
            if (line.Length == 0)
            {
                return;
            }
            var tokens = Tokens.Tokenize(line);
            var keyword = NextString(tokens, "Keyword");
            switch (keyword)
            {
                case KeywordRule:
                    database.AddRule(ParseRule(tokens));
                    break;
                case KeywordLink:
                    database.AddAlias(ParseLink(tokens));
                    break;
                case KeywordZone:
                    var name = NextString(tokens, "GetName");
                    var namedZone = ParseZone(name, tokens);
                    database.AddZone(namedZone);
                    break;
                default:
                    if (string.IsNullOrEmpty(keyword))
                    {
                        var zone = ParseZone(string.Empty, tokens);
                        database.AddZone(zone);
                    }
                    else
                    {
                        throw new InvalidDataException($"Unexpected zone database keyword: {keyword}");
                    }
                    break;
            }
        }

        /// <summary>
        /// Parses an alias link and returns the ZoneAlias object.
        /// </summary>
        /// <param name="tokens">The tokens to parse.</param>
        /// <returns>The ZoneAlias object.</returns>
        internal ZoneAlias ParseLink(Tokens tokens)
        {
            var existing = NextString(tokens, "Existing");
            var alias = NextString(tokens, "Alias");
            return new ZoneAlias(existing, alias);
        }

        /// <summary>
        /// Parses the month.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>The month number in the range 1 to 12.</returns>
        /// <exception cref="InvalidDataException">The month name can't be parsed</exception>
        internal static int ParseMonth(String text)
        {
            Preconditions.CheckArgument(!string.IsNullOrEmpty(text), "text", "Value must not be empty or null");
            int index = Array.IndexOf(ShortMonths, text, 1);
            if (index == -1)
            {
                index = Array.IndexOf(LongMonths, text, 1);
                if (index == -1)
                {
                    throw new InvalidDataException($"Invalid month: {text}");
                }
            }
            return index;
        }

        /// <summary>
        /// Parses a daylight savings rule and returns the Rule object.
        /// </summary>
        /// <remarks>
        /// # Rule    NAME    FROM    TO    TYPE    IN    ON    AT    SAVE    LETTER/S
        /// </remarks>
        /// <param name="tokens">The tokens to parse.</param>
        /// <returns>The Rule object.</returns>
        internal ZoneRule ParseRule(Tokens tokens)
        {
            var name = NextString(tokens, "GetName");
            int fromYear = NextYear(tokens, 0);
            int toYear = NextYear(tokens, fromYear);
            if (toYear < fromYear)
            {
                throw new ArgumentException($"To year cannot be before the from year in a Rule: {toYear} < {fromYear}");
            }
            var type = NextOptional(tokens, "Type");
            var yearOffset = ParseDateTimeOfYear(tokens, true);
            var savings = NextOffset(tokens, "SaveMillis");
            var daylightSavingsIndicator = NextOptional(tokens, "LetterS");
            var recurrence = new ZoneRecurrence(name, savings, yearOffset, fromYear, toYear);
            return new ZoneRule(recurrence, daylightSavingsIndicator, type);
        }

        /// <summary>
        ///   Parses a time zone definition and returns the Zone object.
        /// </summary>
        /// <remarks>
        ///   # GMTOFF RULES FORMAT [ UntilYear [ UntilMonth [ UntilDay [ UntilTime [ ZoneCharacter ] ] ] ] ]
        /// </remarks>
        /// <param name="name">The name of the zone being parsed.</param>
        /// <param name="tokens">The tokens to parse.</param>
        /// <returns>The Zone object.</returns>
        internal Zone ParseZone(string name, Tokens tokens)
        {
            var offset = NextOffset(tokens, "Gmt Offset");
            var rules = NextOptional(tokens, "Rules");
            var format = NextString(tokens, "Format");
            int year = NextYear(tokens, Int32.MaxValue);
            
            if (tokens.HasNextToken)
            {
                var until = ParseDateTimeOfYear(tokens, false);
                return new Zone(name, offset, rules, format, year, until);
            }

            return new Zone(name, offset, rules, format, year, ZoneYearOffset.StartOfYear);
        }

        /// <summary>
        /// Normalizes the transition mode characater.
        /// </summary>
        /// <param name="modeCharacter">The character to normalize.</param>
        /// <returns>The <see cref="TransitionMode"/>.</returns>
        private static TransitionMode ConvertModeCharacter(char modeCharacter)
        {
            switch (modeCharacter)
            {
                case 's':
                case 'S':
                    return TransitionMode.Standard;
                case 'u':
                case 'U':
                case 'g':
                case 'G':
                case 'z':
                case 'Z':
                    return TransitionMode.Utc;
                default:
                    return TransitionMode.Wall;
            }
        }
    }
}
