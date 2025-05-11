using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace NetXpertExtensions
{
#nullable disable

	public static partial class NetXpertExtensions
	{
		#region String extensions
		/// <summary>Creates an array of strings containing all of the "words" in the source.</summary>
		/// <param name="options">An optional StringSplitOptions value to specify removal or retention of empty result.</param>
		/// <returns>An array containing all of the words found in the source.</returns>
		public static string[] ToWords( this string source, StringSplitOptions options = StringSplitOptions.None )
		{
			List<string> words =
				new( (source.Trim().Length > 0) ? Regex.Split( source.Trim(), @"[\s]+", RegexOptions.Compiled ) : new string[] { source.Trim() } );

			int i = -1;
			while ( ++i < words.Count )
			{
				words[ i ] = words[ i ].Trim(); // Remove all leading/trailing whitespace.
				if ( (options == StringSplitOptions.RemoveEmptyEntries) && (words[ i ].Length == 0) )
					words.RemoveAt( i-- ); // Remove an Empty Emtry..
			}

			return words.ToArray();
		}

		/// <summary>Extends the string class to add a UCWords function (a la PHP).</summary>
		/// <param name="strict">Specifies whether the capitalization will be strict or lax.</param>
		/// <remarks>
		/// The "strict" parameter controls how capital letters WITHIN the words will be processed. If the parameter is set to TRUE, 
		/// it specifies that ONLY the first letter of each word will be capitalized in the result and all other letters will be forced
		/// to lowercase. If the parameter is FALSE, capital letters in the bodies of the words will remain unaltered in the output.
		/// Bear in mind that FALSE is a significantly more computationally burdensome process!
		/// </remarks>
		/// <returns>A copy of the original string with all of the initial letters of the individual words capitalised.</returns>
		public static string UCWords( this string source, bool strict = true )
		{
			if ( string.IsNullOrWhiteSpace( source ) ) return source;

			if ( strict )
				return CultureInfo.InvariantCulture.TextInfo.ToTitleCase( source.ToLowerInvariant() );

			string result = "";
			char[] alphabet = "abcdefghijklmnopqrstuvwxyz".ToCharArray();

			MatchCollection words = Regex.Matches( source, @"(?<spacer>[\s]+)?(?<word>[\S]*)", RegexOptions.ExplicitCapture | RegexOptions.Compiled );
			foreach ( Match block in words )
			{
				string word = block.Groups[ "word" ].Success ? block.Groups[ "word" ].Value : "",
					   spacer = block.Groups[ "spacer" ].Success ? block.Groups[ "spacer" ].Value : " ";

				if ( (word.Length > 0) && alphabet.Contains( word[ 0 ] ) )
					word = char.ToUpper( word[ 0 ] ) + ((word.Length > 1) ? word.Substring( 1 ) : "");

				if ( word.Length + spacer.Length > 0 )
					result += spacer + word;
			}

			return result;
		}

		/// <summary>Takes a string of words and condenses it into a camelCase form.</summary>
		/// <param name="strict">Specifies whether intra-word capitalization will be retained.</param>
		/// <remarks>
		/// The "strict" parameter controls how capital letters WITHIN the words will be processed. If the parameter is set to TRUE, 
		/// it specifies that ONLY the first letter of each word will be capitalized in the result and all other letters will be forced
		/// to lowercase. If the parameter is FALSE, capital letters in the bodies of the words will remain unaltered in the output.
		/// Bear in mind that FALSE is a significantly more computationally burdensome process!
		/// </remarks>
		/// <returns>A representation of the string, with all non-alphabetic characters removed and the string presented in camelCase.</returns>
		public static string ToCamelCase( this string source, bool strict = true )
		{
			if ( string.IsNullOrWhiteSpace( source ) ) return "";

			source = Regex.Replace( source.UCWords( strict ), @"[^a-z_A-Z]", "", RegexOptions.None );
			return source.Length == 0 ? "" : (char)((byte)source[ 0 ] & 0xbf) + (source.Length > 1 ? source.Substring( 1 ) : "");
		}

		/// <summary>Splits a string into an array by block size rather than using a particular identifier.</summary>
		/// <param name="blockSize">The size of the blocks to create.</param>
		/// <param name="pad">If TRUE, the final block is padded out to the blocksize, otherwise it is left "as-is".</param>
		/// <param name="padWith">If the final block is to be padded, this specifies what to pad it with (default = space)</param>
		/// <returns>An array of strings of length "blockSize".</returns>
		public static string[] Split( this string source, int blockSize, bool pad = false, char padWith = ' ' )
		{
			List<string> results = new();
			while ( source.Length > blockSize )
			{
				results.Add( source.Substring( 0, blockSize ) );
				source = source.Substring( blockSize );
			}
			if ( source.Length > 0 )
				results.Add( source + (pad ? "".PadRight( blockSize - source.Length, padWith ) : "") );

			return results.ToArray();
		}

		/// <summary>Similar to the Split extension that breaks strings at specific intervals, this splits a string at the specified location, but doesn't break up words.</summary>
		/// <param name="source">The string to split.</param>
		/// <param name="wrapAt">The maximum length of each line.</param>
		/// <param name="maxCutBack">
		/// The maximum number of characters back that the cut can take place in. If no breakable space is found inside this limit
		/// the string is split and a hyphen is added.</param>
		/// <returns>The source string split into an array containing the lines wrapped according to the specified criteria.</returns>
		/// <see cref="Split(string, int, bool, char)"/>
		public static string[] WrapSplit( this string source, int wrapAt, int maxCutBack = 5 )
		{
			List<string> lines = new();
			wrapAt = Math.Max( 10, wrapAt );
			if ( source.Length > wrapAt )
			{
				string[] workLines = source.Split( wrapAt );
				int i = 0;
				while ( i < workLines.Length )
				{
					if ( workLines[ i ].Length >= wrapAt )
					{
						string[] parts = Regex.Split( workLines[ i++ ], @"[\s](?=[\S]*$)", RegexOptions.Multiline );
						if ( parts[ 0 ].Trim().Length > 0 )
						{
							lines.Add( parts[ 0 ].Trim() );

							if ( parts[ 1 ].Trim().Length > 0 )
							{
								if ( parts[ 1 ].Length > maxCutBack )
								{
									lines[ lines.Count - 1 ] += " " + parts[ 1 ].Substring( 0, maxCutBack ) + "-";
									parts[ 1 ] = parts[ 1 ].Substring( maxCutBack );
								}

								if ( i < workLines.Length )
									workLines[ i ] = parts[ 1 ].Trim() + workLines[ i ];
								else
									lines.Add( parts[ 1 ].Trim() );
							}
						}
					}
					else lines.Add( workLines[ i++ ] );
				}
				return lines.ToArray();
			}
			return new string[] { source };
		}

		/// <summary>Removes all instances of a specified character from the string.</summary>
		/// <param name="value">A Char value to remove all instances of from this string.</param>
		/// <returns>The current string with all of the specified characters removed.</returns>
		public static string Remove( this string source, char value ) =>
			source.Replace( value.ToString(), "" );

		/// <summary>Removes all instances of a specified string from the string.</summary>
		/// <param name="value">A string value to remove all instances of from this string.</param>
		/// <returns>The current string with all of the specified string removed.</returns>
		public static string Remove( this string source, string value ) =>
			source.Replace( value, "" );

		/// <summary>Removes all instances of each element in a specified character array from the string.</summary>
		/// <param name="value">An array of Char value to remove all instances of from this string.</param>
		/// <returns>The current string with all of the specified characters removed.</returns>
		public static string Remove( this string source, char[] values )
		{
			string result = source;
			foreach ( char c in values ) result = result.Remove( c.ToString() );
			return result;
		}

		/// <summary>Replaces a specific section of text in the source string with a new string value.</summary>
		/// <param name="start">An int value specifying the start position. If the value is negative, it counts back from the end of the string.</param>
		/// <param name="length">An int value specifying the length of text to remove. If this value plus the start value exceeds the length of the line, only the remainder of the string is replaced.</param>
		/// <param name="with">The string to be inserted in place of the removed section. This CAN be an empty value to act as a delete operation.</param>
		/// <returns>A new string with the specified section replaced with the new value.</returns>
		/// <exception cref="IndexOutOfRangeException">Occurs if the "start" value ends up exceeding the source string length.</exception>
		/// <remarks>If the "length" value is less than 1, this function operates identically to a "{String}.Insert( start, with )" call.</remarks>
		public static string Replace( this string source, int start, int length, string with )
		{
			if ( start < 0 ) start = Math.Max( source.Length + start, 0 ); // negative values work back from the end of the string.
			if ( length < 1 ) return source.Insert( start, with ); // if the length is zero (or less) this is just an Insert operation.

			// If the "start" value is beyond the end of the line, throw an exception.
			if ( start > source.Length )
				throw new IndexOutOfRangeException( "The supplied \"Start\" value exceeds the length of the source string." );

			// If the length + the start exceeds the length of the string, just cut the whole remainder of the string.
			if ( start + length > source.Length ) length = source.Length - start;

			// Any value of "start" less than one starts at the beginning of the string, so only work with the remainder, if applicable.
			if ( start < 1 )
				return with + ((length >= source.Length) ? String.Empty : source.Substring( length ));

			return source.Substring( 0, start ) + with + source.Substring( start + length );
		}

		/// <summary>Replaces instances of '$n' with corresponding values from a provided array.</summary>
		/// <param name="values">An array of objects to insert into the string.</param>
		/// <returns>A string with all un-escaped instances of '$n' replaced by the corresponding value from the provided array.</returns>
		/// <remarks>
		/// "$n" refers to a string consisting of a dollar-sign ($) and a character between 1 and 9, then A-Z (excluding 0, I, L and O).<br/>
		/// If you need a token to be ignored by this operation, escape its '$' character with a backslash.
		/// </remarks>
		public static string Replace( this string source, object[] values )
		{
			if ( string.IsNullOrWhiteSpace( source ) ) return "";
			string work = source;
			if ( (values.Length > 0) && Regex.IsMatch( work, @"(?:[^\\]|^)\$[1-9A-HJKMNP-Z]", RegexOptions.IgnoreCase ) )
			{
				char[] translators = "123456789ABCDEFGHJKMNPQRSTUVWXYZ".ToCharArray();
				for ( int i = 0; i < Math.Min( translators.Length, values.Length ); i++ )
					work = Regex.Replace( work, /* language=regex */
						@"(?<prefix>[^\\]|^)(\$n)".Replace( 'n', translators[ i ] ),
						$"${{prefix}}{values[ i ]}",
						RegexOptions.IgnoreCase
					);
			}

			return work;
		}

		/// <summary>Replaces any unescaped instance of '$1' in the source string  with the string-equivalent of the supplied value.</summary>
		/// <param name="value">An object whose string equivalent will be inserted into the string at the indicated location(s).</param>
		/// <returns>A string with all instances of '$1' replaced with the string equivalent of the provided value.</returns>
		/// <remarks>If you have any instances of '$1' in the original string that need to be ignored, escape the '$' with a backslash.</remarks>
		public static string Replace( this string source, object value ) =>
			source.Replace( new object[] { value } );

		/// <summary>Alternative to Substring that performs a bunch of range-checking and corrects / adapts as possible.</summary>
		/// <param name="start">An int value specifying the start position. If the value is negative, it counts back from the end of the string.</param>
		/// <param name="length">An int value specifying the length of text to retrieve. 
		/// If this value plus the start value exceeds the length of the line, only the remainder of the string is captured.</param>
		/// <returns>As much of the original string, as defined by the start and length parameters, as possible.</returns>
		/// <exception cref="IndexOutOfRangeException">Occurs if the "start" value ends up exceeding the source string length.</exception>
		/// <remarks>If the "length" value is less than 1, this returns an empty string.</remarks>
		public static string MidString( this string source, int start, int length = int.MaxValue )
		{
			if ( start < 0 ) start = Math.Max( source.Length + start, 0 ); // negative values work back from the end of the string.

			// if the length is zero (or less), or start is beyond the end of the string, the only rational result is an empty string.
			if ( (length < 1) || (start > source.Length) ) return String.Empty;

			// If the "start" value is somehow beyond the end of the line, throw an exception.
			if ( start > source.Length )
				throw new IndexOutOfRangeException( "The supplied \"Start\" value exceeds the length of the source string." );

			// If the length + the start exceeds the length of the string, just cut the whole remainder of the string.
			if ( start + length > source.Length ) length = source.Length - start;

			// Any value of "start" less than one starts at the beginning of the string.
			if ( start < 1 )
				return ((length >= source.Length) ? source : source.Substring( 0, length ));

			return (length == int.MaxValue) || (start + length >= source.Length) ? source.Substring( start ) : source.Substring( start, length );
		}

		/// <summary>
		/// Pads a string of text such that it is centered within a field of the defined size with the specified character
		/// padding the left and right ends of the string as neccessary for this purpose. If the supplied string is longer
		/// than the specified field size, then its ends are truncated to length and an ellipsis is added between them.
		/// </summary>
		/// <param name="fieldSize">The desired size of the field.</param>
		/// <param name="withChar">A Char value indicating what the pad character is.</param>
		/// <returns>A string of the specified length containing the supplied text centered in it.</returns>
		public static string PadCenter( this string source, byte fieldSize, char withChar = ' ' )
		{
			if ( fieldSize == 0 ) return "";
			if ( string.IsNullOrEmpty( source ) ) return "".PadRight( fieldSize, withChar );
			if ( source.Length == fieldSize ) return source;

			int padSize = (int)fieldSize - source.Length,
				left = (int)Math.Floor( (decimal)((padSize < 1) ? fieldSize : padSize) / 2M );

			return
				(padSize < 0)
				?
				source.Substring( 0, left - 3 ) + "..." + source.EndString( fieldSize - left )
				:
				(source.PadLeft( left + source.Length, withChar )).PadRight( fieldSize, withChar );
		}

		/// <summary>Given a string of valid characters, filters all non-matching characters out of a string.</summary>
		/// <param name="validChars">
		/// A string of valid (permitted) characters to retain. If a regular expression is passed, the expression is used 
		/// to remove unwanted characters, rather than preserve them. Use '/' at the beginning and end of the string to
		/// indicate it should be treated as a Regular Expression.
		/// </param>
		/// <param name="ignoreCase">Specifies whether case should be ignored.</param>
		/// <returns>A string containing only the permitted characters.</returns>
		public static string Filter( this string source, string validChars, bool ignoreCase = true )
		{
			if ( (source.Length == 0) || (validChars.Length == 0) ) return "";

			if ( Regex.IsMatch( validChars, @"^[/][\s\S]+[/]$", RegexOptions.Singleline | (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None) ) )
				source = Regex.Replace( source, validChars.Trim( new char[] { ' ', '/' } ), "" );
			else
			{
				if ( ignoreCase )
				{
					validChars = validChars.ToLowerInvariant();
					foreach ( char c in validChars )
						if ( " abcdefghijklmnopqrstuvwxyz".IndexOf( c ) > 0 ) validChars += (char)(c & 223);
				}

				int i = 0;
				while ( i < source.Length )
					if ( validChars.IndexOf( source.Substring( i, 1 ) ) < 0 )
						source = source.Remove( source.Substring( i, 1 ) );
					else i++;
			}

			return source;
		}

		/// <summary>Given an array valid characters, filters all non-matching characters out of a string.</summary>
		/// <param name="validChars">An array of valid (permitted) characters to retain.</param>
		/// <param name="ignoreCase">Specifies whether case should be ignored.</param>
		/// <returns>A string containing only the permitted characters.</returns>
		public static string Filter( this string source, char[] validChars, bool ignoreCase = true ) =>
			source.Filter( new string( validChars ), ignoreCase );

		/// <summary>Filter the parent string according to a given a RegEx pattern + options.</summary>
		/// <param name="pattern">A string containing a Regular Expression pattern to base the filter on.</param>
		/// <param name="options">A RegexOptions value modifying how the pattern is applied / translated.</param>
		/// <returns>A string representing the value of the source after being filtered according to the supplied parameters.</returns>
		public static string Filter( this string source, string pattern, RegexOptions options ) =>
			Regex.Replace( source, pattern, "", options );

		/// <summary>Captures the number of characters specified from the end of a string.</summary>
		/// <param name="size">The number of characters to capture.</param>
		/// <returns>A string containing the characters requested.</returns>
		public static string EndString( this string source, int size )
		{
			if ( size > source.Length ) return source;
			if ( size == 0 ) return "";
			return source.Substring( source.Length - size );
		}

		/// <summary>Provides a fast means of extracting the last character in a string.</summary>
		/// <returns>If the string is empty, char(0), otherwise the char representation of the last character in the string.</returns>
		public static char LastChar( this string source )
		{
			if ( source.Length == 0 ) return char.MinValue;
			return source[ source.Length - 1 ];
		}

		/// <summary>Attempts to build an XmlNode object from the contents of this string.</summary>
		/// <returns>If successful, an XmlNode object populated with the content of the current string, otherwise NULL.</returns>
		public static XmlNode ToXmlNode( this string parent )
		{
			if ( !string.IsNullOrWhiteSpace( parent ) )
			{
				string work = parent.Trim( new char[] { ' ', '\t', '\r', '\n', (char)0xfeff, (char)0x200b } );
				XmlDocument doc = new() { XmlResolver = null };
				if ( !Regex.IsMatch( work, @"^([<][?]xml.*[?][>])", RegexOptions.IgnoreCase | RegexOptions.Singleline ) )
					parent = work = Xml.XML.HEADER + work;

				doc.LoadXml( work );
				return (doc.ChildNodes.Count > 0) ? doc.LastChild : null;
			}
			throw new ArgumentNullException( "You must provide a value to parse into an XmlNode!" );
		}

		/// <summary>Returns an XML-friendly, encoded, version of the source data.</summary>
		/// <param name="encodeApostrophe">If set to true, any apostrophe in the source string is converted to "&apos;", otherwise they're left as-is.</param>
		public static string XmlEncode( this string source, bool encodeApostrophe = false )
		{
			XmlDocument doc = new();
			XmlNode node = doc.CreateElement( "root" );
			node.InnerXml = encodeApostrophe ? source.Replace( "'", "&apos;" ) : source;
			return node.InnerXml;
		}

		/// <summary>Returns the plaintext (decoded) equivalent of the XML-encoded source.</summary>
		public static string XmlDecode( this string source )
		{
			try
			{
				XmlDocument doc = new();
				XmlNode node = doc.CreateElement( "root" );
				node.InnerXml = source;
				return node.InnerText;
			}
			catch ( Exception e ) { return e.Message; }
		}

		/// <summary>Converts a base string to an HTML compatible one with encoded entities.</summary>
		/// <returns>A valid HTML string with appropriately encoded entities.</returns>
		public static string EncodeHtmlEntities( this string text ) =>
			WebUtility.HtmlEncode( text );

		/// <summary>Converts a string containing HTML entities into one with only the basic ASCII characters.</summary>
		/// <returns>A string with encoded HTML entities translated back to their ASCII equivalents.</returns>
		public static string DecodeHtmlEntities( this string text ) =>
			WebUtility.HtmlDecode( text );

		/// <summary>Tests the source string against a provided Regex pattern and returns TRUE if it matches.</summary>
		/// <param name="regexPattern">A Regex Pattern string to test.</param>
		/// <param name="options">The RegularExpression options to apply.</param>
		/// <returns></returns>
		public static bool Match( this string source, string regexPattern, RegexOptions options = RegexOptions.None ) =>
			Regex.IsMatch( source, regexPattern, options );

		[Flags]
		public enum StripOuterOptions
		{
			None = 0x0000,
			DoubleQuotes = 0x0001, // "
			SingleQuotes = 0x0002, // '
			BackQuotes = 0x0004, // `
			RoundBrackets = 0x0008, // ( )
			SquareBrackets = 0x0010, // [ ]
			AngleBrackets = 0x0020, // < >
			BraceBrackets = 0x0040, // { }
			DblAnglBrackets = 0x0080, // « »
			All = 0xffff
		}

		/// <summary>This returns a copy of the original string, stripped of leading and trailing patterns as specified.</summary>
		/// <param name="leftSide">A Regex pattern string to identify the opening character to match.</param>
		/// <param name="rightSide">A Regex pattern string to identify the closing character to match.</param>
		/// <param name="options">Regular Expression Options to apply. Default: IgnoreCase | Multiline.</param>
		/// <remarks>
		/// Note that the "leftSide" and "rightSide" strings MUST be valid Regex character class definitions (designated characters enclosed 
		/// in square brackets). If the passed strings do not conform with this requirement, no work will be performed. Note that any leading 
		/// and/or trailing whitespace in the source string will be PRESERVED by this function!
		/// </remarks>
		/// <seealso cref="Unwrap(string, StripOuterOptions, bool, bool)"/>
		public static string Unwrap( this string source, string leftSide, string rightSide = null, RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Multiline )
		{
			if ( !string.IsNullOrWhiteSpace( source ) && !string.IsNullOrWhiteSpace( leftSide ) )
			{
				leftSide = leftSide.Trim();
				if ( string.IsNullOrWhiteSpace( rightSide ) ) rightSide = leftSide; ;

				if ( Regex.IsMatch( leftSide, @"^[\[][\S]+[\]]$" ) && Regex.IsMatch( rightSide, @"^[\[][\S]+[\]]$" ) )
				{
					/* language=Regex */
					string pattern = @"^([\s]*)" + leftSide + @"([\s\S]*)" + rightSide + @"([\s]*)$";
					return Regex.Replace( source, pattern, "$1$2$3", options );
				}
			}
			return source;
		}

		/// <summary>Returns a copy of the original string with any of the specified classes of enclosing characters removed.</summary>
		/// <param name="options">A StripOuterOptions enum to specify what work to perform.</param>
		/// <param name="trim">If TRUE, leading and trailing whitespace is removed from the string prior to processing, otherwise it is RETAINED.</param>
		/// <param name="all">If TRUE, all provided options will be removed, otherwise, only the first matching option will be.</param>
		/// <returns>A string with the specified enclosing </returns>
		public static string Unwrap( this string word, StripOuterOptions options, bool trim = true, bool all = false )
		{
			if ( string.IsNullOrWhiteSpace( word ) || (options == StripOuterOptions.None) )
				return word is null ? "" : word;

			string source = trim ? word.Trim() : word;
			foreach ( StripOuterOptions value in Enum.GetValues( typeof( StripOuterOptions ) ) )
			{
				if ( options.HasFlag( value ) )
					switch ( value )
					{
						case StripOuterOptions.DoubleQuotes:
							source = source.Unwrap(
								/* language=regex */ @"[\x22]"
							);
							if ( !all ) return source;
							break;
						case StripOuterOptions.SingleQuotes:
							source = source.Unwrap(
								/* language=regex */ @"[']"
							);
							if ( !all ) return source;
							break;
						case StripOuterOptions.BackQuotes:
							source = source.Unwrap(
								/* language=regex */ @"[`]"
							);
							if ( !all ) return source;
							break;
						case StripOuterOptions.RoundBrackets:
							source = source.Unwrap(
								/* language=regex */ @"[(]",
								/* language=regex */ @"[)]"
							);
							if ( !all ) return source;
							break;
						case StripOuterOptions.SquareBrackets:
							source = source.Unwrap(
								/* language=regex */ @"[\[]",
								/* language=regex */ @"[\]]"
							);
							if ( !all ) return source;
							break;
						case StripOuterOptions.AngleBrackets:
							source = source.Unwrap(
								/* language=regex */ @"[<]",
								/* language=regex */ @"[>"
							);
							if ( !all ) return source;
							break;
						case StripOuterOptions.BraceBrackets:
							source = source.Unwrap(
								/* language=regex */ @"[{]",
								/* language=regex */ @"[}]"
							);
							if ( !all ) return source;
							break;
						case StripOuterOptions.DblAnglBrackets:
							source = source.Unwrap(
								/* language=regex */ @"[«]",
								/* language=regex */ @"[»]"
							);
							if ( !all ) return source;
							break;
					}
			}
			return source;
		}

		/// <summary>Encloses the root string in the specified wrapping characters.</summary>
		/// <param name="with">A StripOuterOptions value specifying what character(s) to wrap the base string in.</param>
		/// <param name="trim">If TRUE, the supplied string is stripped of any leading or trailing whitespace BEFORE the wrapping operations are performed.</param>
		/// <returns>The original string, wrapped in the requested character(s).</returns>
		/// <remarks>As StripOuterOptions is a flagged enumeration value, if multiple values are specified, only the first match
		/// will be effected. Matches are checked in the following order: DoubleQuotes, SingleQuotes, BackQuotes, RoundBrackets,
		/// SquareBrackets, AngleBrackets, BraceBrackets then DblAnglBrackets.</remarks>
		public static string Wrap( this string word, StripOuterOptions with, bool trim = false )
		{
			if ( !string.IsNullOrEmpty( word ) )
			{
				if ( trim ) word = word.Trim();

				if ( with.HasFlag( StripOuterOptions.DoubleQuotes ) )
					return $"\x22{word}\x22";

				if ( with.HasFlag( StripOuterOptions.SingleQuotes ) )
					return $"'{word}'";

				if ( with.HasFlag( StripOuterOptions.BackQuotes ) )
					return $"`{word}`";

				if ( with.HasFlag( StripOuterOptions.RoundBrackets ) )
					return $"({word})";

				if ( with.HasFlag( StripOuterOptions.SquareBrackets ) )
					return $"[{word}]";

				if ( with.HasFlag( StripOuterOptions.AngleBrackets ) )
					return $"<{word}>";

				if ( with.HasFlag( StripOuterOptions.BraceBrackets ) )
					return $"{{{word}}}";

				if ( with.HasFlag( StripOuterOptions.DblAnglBrackets ) )
					return $"«{word}»";
			}

			return word;
		}

		/// <summary>Tries to parse a string as an XmlDocument and reports success or failure.</summary>
		/// <param name="allowHeaderlessXml">If set to TRUE (default) the test will pass valid XML, even if the header is missing.</param>
		/// <param name="resolver">Allows using a defined XmlResolver. Defaults to null.</param>
		/// <returns>TRUE if the supplied string can be parsed as XML.</returns>
		public static bool IsXml( this string source, bool allowHeaderlessXml = true, XmlResolver resolver = null )
		{
			XmlDocument doc = new() { XmlResolver = resolver };
			try { doc.LoadXml( source ); return true; } catch ( XmlException ) { };

			if ( allowHeaderlessXml )
				try { doc.LoadXml( Xml.XML.HEADER + source ); return true; } catch ( XmlException ) { };

			return false;
		}

		/// <summary>Searches an array of strings for a specified value.</summary>
		/// <param name="value">The value to search for.</param>
		/// <param name="caseSensitive">Indicates whether or not the search should be case sensitive (default = no).</param>
		/// <returns>TRUE if the specified string is found in the array.</returns>
		public static bool Contains( this string[] source, string value, bool caseSensitive = false )
		{
			if ( (source.Length == 0) || (value is null) || (value.Length == 0) ) return false;
			StringComparison compare = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
			int i = -1; while ( (++i < source.Length) && !source[ i ].Equals( value, compare ) ) ;
			return (i < source.Length);
		}

		/// <summary>Searches an array of strings for all values in a specified collection.</summary>
		/// <param name="values">An array of strings to search for in the source array.</param>
		/// <param name="caseSensitive">Indicates whether or not the search should be case sensitive (default = no).</param>
		/// <returns>TRUE if all of the specified strings are found in the array.</returns>
		public static bool ContainsAll( this string[] source, string[] values, bool caseSensitive = false )
		{
			bool result = (!(values is null) && (values.Length > 0) && (source.Length >= values.Length));
			int i = -1; while ( result && (++i < values.Length) )
				result = source.Contains( values[ i ], caseSensitive );

			return result;
		}

		/// <summary>Reports whether all elements in an array correspond to a provided Regular Expression</summary>
		/// <param name="pattern">The Regular Expression pattern to match against.</param>
		/// <param name="options">RegexOptions flags to apply.</param>
		/// <returns>TRUE if all elements of the array match the supplied Regular Expression.</returns>
		public static bool ContainsAll( this string[] source, string pattern, RegexOptions options )
		{
			bool result = !string.IsNullOrWhiteSpace( pattern ) && (source.Length > 0);
			int i = -1; while ( result && (++i < source.Length) )
				result = Regex.IsMatch( pattern, source[ i ], options );

			return result;
		}

		/// <summary>Searches an array of strings for all values that match a supplied Regular Expression pattern.</summary>
		/// <param name="pattern">The Regular Expression pattern to match against.</param>
		/// <param name="options">RegexOptions flags to apply.</param>
		/// <returns>An array of strings containing all of the matches.</returns>
		public static string[] ContainsAny( this string[] source, string pattern, RegexOptions options )
		{
			List<string> results = new();
			if ( !string.IsNullOrWhiteSpace( pattern ) && (source.Length > 0) )
			{
				foreach ( string s in source )
					if ( Regex.IsMatch( s, pattern, options ) )
						results.Add( s );
			}
			return results.ToArray();
		}

		/// <summary>Searches an array of strings for any value from a supplied collection of potentital matches.</summary>
		/// <param name="values">An array of strings to look for.</param>
		/// <param name="caseSensitive">Indicates whether or not the search should be case sensitive (default = no).</param>
		/// <returns>TRUE if any string in the supplied values is found in the source array.</returns>
		public static bool ContainsAny( this string[] source, string[] values, bool caseSensitive = false )
		{
			if ( (source.Length > 0) && !(values is null) && (values.Length > 0) )
			{
				int i = -1; while ( (++i < values.Length) && !source.Contains( values[ i ], caseSensitive ) ) ;
				return (i < values.Length);
			}
			return false;
		}

		/// <summary>
		/// Returns a string limited in length to the specified value. If the source string's length is less than the value, the
		/// entire string is returned, otherwise the string up to the specified length, plus an ellipses, is returned.</summary>
		/// <param name="length">The length to limit the returned value to.</param>
		public static string Limit( this string source, int length ) =>
			string.IsNullOrEmpty( source ) ? source :
				((source.Length > length) ? source.Substring( 0, length - 3 ) + "..." : source);

		/// <summary>
		/// Cuts a string down to a specified length by cutting out as much content as needed in the middle and replacing it with
		/// whatever is contained in "glue".
		/// </summary>
		/// <param name="length">The maximum length of the result. The minimum value for this is the length of "glue" plus four characters.</param>
		/// <param name="glue">What string to use to replace the middle of the source when condensing.</param>
		/// <remarks>If "glue" is null or empty, " ... " will be substituted.</remarks>
		public static string Condense( this string source, int length, string glue = " ... " )
		{
			if ( string.IsNullOrEmpty( glue ) ) glue = " ... ";
			length = Math.Max( glue.Length + 4, length );
			if ( source.Length < length ) return source;
			int splitSize = (length - glue.Length) / 2;
			return source.Substring( 0, splitSize ) + glue + source.EndString( splitSize );
		}

		/// <summary>Initializes every element of a String array with a provided string.</summary>
		/// <param name="with">The string value to set for each element.</param>
		/// <returns>An array of strings initialized with the provided value.</returns>
		public static string[] Seed( this string[] source, string with )
		{
			if ( source.Length > 0 )
				for ( int i = 0; i < source.Length; i++ )
					source[ i ] = with;

			return source;
		}

		/// <summary>Creates an array of strings with the specified size and seeded with the source value.</summary>
		/// <param name="size">The size of the array to create.</param>
		public static string[] Seed( this string source, int size )
		{
			List<string> items = new();
			if ( size > 0 )
				for ( int i = 0; i < size; i++ ) { items.Add( source ); }

			return items.ToArray();
		}

		/// <summary>Converts any supplied string to it's base-64 equivalent according to the specified encoding.</summary>
		/// <param name="encoding">What encoding method should be used. If none is provided, UTF8 is assumed.</param>
		/// <returns>If a string is passed, that string encoded to Base64 with the specified encoder, otherwise an empty string.</returns>
		public static string Base64Encode( this string source, System.Text.Encoding encoding = null )
		{
			if ( encoding is null ) encoding = System.Text.Encoding.UTF8;
			if ( string.IsNullOrEmpty( source ) ) return String.Empty;
			byte[] asBytes = encoding.GetBytes( source );
			return Convert.ToBase64String( asBytes );
		}

		/// <summary>Takes a string containing a Base64-encoded value and decodes it with the specified encoder.</summary>
		/// <param name="encoding">What encoding method should be used. If none is provided, UTF8 is assumed.</param>
		/// <returns>The decoded string.</returns>
		public static string Base64Decode( this string source, System.Text.Encoding encoding = null )
		{
			string result = String.Empty;
			if ( encoding is null ) encoding = System.Text.Encoding.UTF8;
			if ( !string.IsNullOrEmpty( source ) )
			{
				byte[] asBytes = Convert.FromBase64String( source );
				result = encoding.GetString( asBytes );
			}
			return result;
		}

		/// <summary>Confirms whether or not the <i>source</i> string conforms with Base64 requirements.</summary>
		/// <returns><b>TRUE</b> if the calling string has content and conforms with Base64 standards.</returns>
		public static bool IsBase64String( this string source ) =>
			!string.IsNullOrWhiteSpace( source ) && (source.Length % 4 == 0) && Regex.IsMatch( source, @"[^a-zA-Z\d\+\/]*={0,3}$" );

		/// <summary>Provides an intrinsic means of converting any string to byte[].</summary>
		/// <remarks>Uses the <seealso cref="System.Text.Encoding.UTF8"/>.GetBytes() method to generate the output array.</remarks>
		public static byte[] ToByteArray( this string source ) => System.Text.Encoding.UTF8.GetBytes( source );

		/// <summary>Extends the MatchCollection class to provide a means to extract all match Values in an array of strings.</summary>
		public static string[] ValueCollection( MatchCollection matches )
		{
			List<string> values = new();
			foreach ( Match m in matches )
				values.Add( m.Value );

			return values.ToArray();
		}

		/// <summary>Tests to see if the value of a string is a defined <i>Enum</i> value for the specified type.</summary>
		/// <typeparam name="T">The <i>Enum</i> type against which to evaluate the string.</typeparam>
		/// <returns><b>TRUE</b> if the contents of the string can be converted to the specified <i>Enum</i> type.</returns>
		public static bool IsEnumValue<T>( this string source ) where T : Enum =>
			!string.IsNullOrWhiteSpace( source ) && Regex.IsMatch( source.Trim(), $"^({string.Join( "|", typeof( T ).GetEnumNames() )})$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant );

		/// <summary>Attempts to parse the value of the calling string into the specified <i>Enum</i> type.</summary>
		/// <typeparam name="T">The <i>Enum</i> type against which to evaluate the string.</typeparam>
		/// <param name="defaultValue">Facilitates specifying the default value to assign if the parsing effort fails.</param>
		/// <param name="ignoreCase">If set <b>true</b> (the default), the parser ignores the cases of the string and <seealso cref="Enum"/>.</param>
		/// <returns>If the string represents a valid name for the specified <i>Enum</i> type, the relevant value, otherwise the <i>default</i> value for the type.</returns>
		public static T ToEnum<T>( this string source, T defaultValue = default, bool ignoreCase = true ) where T : Enum =>
			IsEnumValue<T>( source ) ? (T)Enum.Parse( typeof( T ), source.Trim(), ignoreCase ) : defaultValue;

		/// <summary>Reports on the number of instances that a given character occurs in a string.</summary>
		/// <param name="char">The character to count.</param>
		/// <returns>The number of times the specified character was found in the string.</returns>
		public static int CountChar( this string source, char @char )
		{
			int count = 0, i = -1;
			if ( !string.IsNullOrEmpty( source ) )
				while ( ++i < source.Length )
					if ( source[ i ] == @char ) count += 1;

			return count;
		}

		/// <summary>Reports on the number of instances an array of characters can be found in the provided string.</summary>
		/// <param name="chars">An array of char values to count.</param>
		/// <returns>
		/// A <seealso cref="Dictionary{char, int}"/> that contains a list of the requested characters and the corresponding 
		/// number of times they occur in the supplied string.
		/// </returns>
		public static Dictionary<char, int> CountChars( this string source, params char[] chars )
		{
			Dictionary<char, int> result = new();
			foreach ( char c in chars )
			{
				int count = source.CountChar( c );
				if ( result.ContainsKey( c ) )
					result[ c ] += count;
				else
					result.Add( c, count );
			}

			return result;
		}

		/// <summary>Facilitates compressing and uncompressing raw text via GZipStream.</summary>
		/// <remarks>Code found on StackOverflow, written by "xanatos"</remarks>
		/// <see href="https://stackoverflow.com/questions/7343465/compression-decompression-string-with-c-sharp"/>

		// Garnered from StackOverflow: 
		// https://stackoverflow.com/questions/7343465/compression-decompression-string-with-c-sharp

		/// <summary>Compresses a supplied string and returns the result as an array of bytes.</summary>
		public static byte[] Compress( this string source )
		{
			byte[] data = Encoding.UTF8.GetBytes( source );

			if ( data.Length > 0 )
			{
				using var msi = new MemoryStream( data );
				using var mso = new MemoryStream();
				using ( var gs = new GZipStream( mso, CompressionMode.Compress ) )
					msi.CopyTo( gs );

				return mso.ToArray();
			}

			return Array.Empty<byte>();
		}

		/// <summary>Decompresses the supplied binary data and returns the result as a string.</summary>
		public static string UncompressString( this byte[] bytes )
		{
			if ( bytes.Length > 0 )
			{
				using MemoryStream msi = new( bytes );
				using MemoryStream mso = new();
				using ( GZipStream gs = new( msi, CompressionMode.Decompress ) )
					gs.CopyTo( mso );

				return Encoding.UTF8.GetString( mso.ToArray() );
			}

			return string.Empty;
		}

		/// <summary>Returns the contents of the calling string 
		/// 
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		public static string UrlEncode( this string source ) => Uri.EscapeDataString( source );
		#endregion
	}
}