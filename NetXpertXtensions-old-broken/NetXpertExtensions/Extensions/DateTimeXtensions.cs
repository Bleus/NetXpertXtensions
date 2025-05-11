using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace NetXpertExtensions
{
	#nullable disable

	public static partial class NetXpertExtensions
	{
		#region DateTime related extensions for processing MySQL strings.
		/// <summary>Extends the DateTime class to provide an easy way to extract a valid MySQL DateTime formatted string.</summary>
		/// <returns>The contents of the current DateTime object expressed as a MySQL DateTime compatible string.</returns>
		public static string ToMySqlString(this DateTime source) => source.ToString("yyyy-MM-dd HH:mm:ss");

		/// <summary>Will take a MySQL DateTime value (as a string) and use it to populate the calling object.</summary>
		/// <param name="mysqlDateTime">A MySQL-formatted DATETIME string.</param>
		public static void FromMySqlString( this DateTime source, string mysqlDateTime ) =>
			source = mysqlDateTime.ParseMySqlDateTime();

		/// <summary>Extends the String class to facilitate parsing a MySQL-formatted DATETIME string into a C# DateTime object.</summary>
		/// <param name="mysqlSourceString">A MySQL-formatted DATETIME string.</param>
		/// <returns>If parsing is possible, a DateTime object populated from the supplied value, otherwise an exception will be thrown.</returns>
		/// <remarks>NOTE: If the parsing operation is successful, the source string's value will be replaced with a valid MySQL DATETIME-formatted version of it.</remarks>
		public static DateTime ParseMySqlDateTime( this string mysqlSourceString )
		{
			// Stop trying to parse null, empty or whitespace-only strings!
			if ( string.IsNullOrWhiteSpace( mysqlSourceString ) )
				throw new ArgumentNullException( "The source string cannot be null, empty or whitespace!" );

			string pattern = /* language=regex */
				@"^(1[89]|2[01])[\d]{2}[-\/](0?[1-9]|1[012])[-\/]([012]?[\d]|3[01])[ ]+([01]?[\d]|2[0123])([:][0-5][\d]){2}$",
				work = Regex.Replace( mysqlSourceString.Trim().Replace( '\t', ' ' ), @"[^ \d\/:-]", "" );

			if ( Regex.IsMatch( work, pattern, RegexOptions.ExplicitCapture ) )
			{
				mysqlSourceString = Regex.Replace( work, @"[ ]{2,}", " " );
				return DateTime.ParseExact( mysqlSourceString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture );
			}

			// Okay, who knows wtf we've been handed, try to do something with it...
			DateTime result;
			if ( DateTime.TryParse( mysqlSourceString, out result ) )
			{
				mysqlSourceString = result.ToString( "yyyy-MM-dd HH:mm:ss" );
				return result;
			}

			throw new ArgumentException( $"The value provided (\x22{mysqlSourceString}\x22) could not be parsed as a DateTime value." );
		}

		/// <summary>This attempts to parse a string value into a DateTime object using the 'ParseMySqlDateTime()' extension, but will
		/// catch any parsing-related exceptions that may be thrown during the operation (and return FALSE) instead of interrupting 
		/// execution.  
		/// </summary>
		/// <param name="mysqlSourceString">A string to try and parse; preferbly a MySQL-formatted DATETIME value.</param>
		/// <param name="value">If the parsing is successful, a properly populated DateTime object, otherwise a value equivalent to `new DateTime(0)`.</param>
		/// <returns>TRUE if the parse attempt was successful, otherwise FALSE. If this is FALSE, the returned `value` will be equivalent to 'new DateTime(0)'.</returns>
		/// <remarks>
		/// NOTE: If the parsing operation is successful, the source string's value will be replaced with a valid MySQL 
		/// DATETIME-formatted version of it.
		/// </remarks>
		/// <seealso cref="ParseMySqlDateTime(string)"/>
		public static bool TryParseMySqlDateTime( this string mySqlSourceString, out DateTime value )
		{
			try { value = mySqlSourceString.ParseMySqlDateTime(); return true; }
			catch ( Exception e )
			{ 
				// If the caught exception is one of the types that ParseMySqlDateTime( string ) can throw on it's own, catch them
				// and return false.
				switch( e.GetType().Name )
				{
					case "ArgumentException":
					case "ArgumentNullException":
					case "FormatException":
						value = new DateTime(0);
						return false;
					default:
						// Something weird just happened, act accordingly!
						throw;
				}
			}
		}
		#endregion

		/// <summary>Generates a Unix Epoch TimeStamp value for the time currently being stored.</summary>
		/// <returns>The number of seconds between Jan 1, 1970 and the source DateTime value.</returns>
		/// <remarks>If the source time value precedes Jan 1, 1970, the returned value will be zero!</remarks>
		public static UInt64 UnixTimeStamp( this DateTime source ) =>
			(UInt64)source.Subtract( new DateTime( 1970, 1, 1, 0, 0, 0 ) ).TotalSeconds;

		/// <summary>Faciltiates adding features to the DateTime.ToString() parser.</summary>
		/// <param name="format">The format string to use.</param>
		/// <param name="formatProvider">An optional <i>IFormateProvider</i> to use, if needed.</param>
		/// <remarks>
		/// The format string is initially passed to the base <i>ToString()</i> parser. The result is
		/// then scanned for additional parameters (i.e. "nn/NN" for ordinal day suffixes) and re-parsed. 
		/// The resultant string is then returned.
		/// </remarks>
		public static string ExtToString( this DateTime source, string format, IFormatProvider formatProvider = null)
		{
			string result = formatProvider is null ? source.ToString( format ) : source.ToString( format, formatProvider );

			// Extended attributes:
			if (Regex.IsMatch(result, @"(?:nn|NN)", RegexOptions.CultureInvariant))
			{
				switch (source.Day)
				{
					case 1:
					case 21:
					case 31:
						result = result.Replace( "nn", "st" );
						result = result.Replace( "NN", "ST" );
						break;
					case 2:
					case 22:
						result = result.Replace( "nn", "nd" );
						result = result.Replace( "NN", "ND" );
						break;
					case 3:
					case 23:
						result = result.Replace( "nn", "rd" );
						result = result.Replace( "NN", "RD" );
						break;
					default:
						result = result.Replace( "nn", "th" );
						result = result.Replace( "NN", "TH" );
						break;
				}
			}
			return result;
		}

		#region DateTime range comparison extensions (Min/Max)
		/// <summary>Takes supplied DateTime values, plus the calling value, and returns the greatest (i.e. 'youngest') one.</summary>
		/// <remarks>NOTE: 'youngest' in this sense means the date that is furthest into the future.</remarks>
		public static DateTime Max( this DateTime source, params DateTime[] list ) => 
			source.Max( new List<DateTime>(list) );

		/// <summary>Takes a supplied enumerable group of DateTime values, plus the calling value, and returns the greatest (i.e. 'youngest') one.</summary>
		/// <returns>The youngest DateTime value from all the values provided.</returns>
		/// <remarks>NOTE: 'youngest' in this sense means the date that is furthest into the future.</remarks>
		public static DateTime Max( this DateTime source, IEnumerable<DateTime> list )
		{
			List<DateTime> values = new( list );
			values.Add( source );
			values.Sort( ( x, y ) => x.CompareTo( y ) );
			return values[ 0 ];
		}

		/// <summary>Takes supplied DateTime values, plus the calling value, and returns the least (oldest) one.</summary>
		/// <remarks>NOTE: 'oldest' in this sense means the date that is furthest into the past.</remarks>
		public static DateTime Min( this DateTime source, params DateTime[] list ) =>
			source.Min( new List<DateTime>( list ) );

		/// <summary>Takes a supplied enumerable group of DateTime values, plus the calling value, and returns the least (i.e. 'oldest') one.</summary>
		/// <returns>The oldest DateTime value from all the values provided.</returns>
		/// <remarks>NOTE: 'oldest' in this sense means the date that is furthest into the past.</remarks>
		public static DateTime Min( this DateTime source, IEnumerable<DateTime> list )
		{
			List<DateTime> values = new( list );
			values.Add( source );
			values.Sort( ( x, y ) => x.CompareTo( y ) );
			return values[ values.Count - 1 ];
		}
		#endregion

		#region Methods to facilitate creating collections based on a specified cut-off date (OlderThan/NewerThan)
		/// <summary>Takes an IEnumerable collection of DateTime values and returns all values older than the calling value.</summary>
		/// <param name="orEqualTo">If TRUE (default), the returned collection also includes any values that match the calling value.</param>
		public static DateTime[] OlderThan( this DateTime source, IEnumerable<DateTime> list, bool orEqualTo = true )
		{
			List<DateTime> result = new List<DateTime>();
			foreach ( DateTime value in list )
				if ( (value < source) || (orEqualTo && (value == source) ) ) 
					result.Add( value );

			return result.ToArray();
		}

		/// <summary>Takes an IEnumerable collection of DateTime values and returns all values newer than the calling value.</summary>
		/// <param name="orEqualTo">If TRUE (default), the returned collection also includes any values that match the calling value.</param>
		public static DateTime[] NewerThan( this DateTime source, IEnumerable<DateTime> list, bool orEqualTo = true )
		{
			List<DateTime> result = new List<DateTime>();
			foreach ( DateTime value in list )
				if ( (value > source) || (orEqualTo && (value == source)) ) 
					result.Add( value );

			return result.ToArray();
		}
		#endregion
	}
}