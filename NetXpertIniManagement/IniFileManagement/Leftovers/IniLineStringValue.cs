using System.Text.RegularExpressions;
using NetXpertExtensions;

namespace IniFileManagement
{
	public abstract partial class IniLineStringValue : IniLineValueFoundation<string>
	{
		#region Properties
		/// <summary>Specifies what the wrapping character set will be used when parsing/writing this value.</summary>
		protected QuoteTypes _quoteType = QuoteTypes.None;

		/// <summary>If the defined <seealso cref="QuoteTypes"/> value is <seealso cref="QuoteTypes.Custom"/>, this holds
		/// the values of the custom wrapping characters to use.</summary>
		protected char[] _customQuotes = Wrapper( QuoteTypes.None );

		/// <summary>Holds the actual data associated with this value, as a string.</summary>
		/// <remarks>This is <i>always</i> stored <b><i><u>unencrypted</u></i></b>!</remarks>
		protected string _value = "";

		public enum QuoteTypes { None, DoubleQuote, SingleQuote, BackTick, SquareBrackets, BraceBrackets, RoundBrackets, AngleBrackets, DoubleAngleBrackets, Custom }
		public enum DataTypes { Unknown, String, Boolean, Number, Date, Base64Encoded, Encrypted }
		#endregion

		#region Constructors
		protected IniLineValueFoundation( QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
		{
			this._quoteType = quoteType;
			if ( (quoteChars is not null) && (quoteChars.Length > 0) )
				this.CustomQuotes = quoteChars;
		}

		protected IniLineValueFoundation( string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
		{
			this._quoteType = quoteType;
			this._value = UnwrapString( value, quoteType, customQuotes );
		}
		#endregion

		#region Accessors
		/// <summary>Facilitates getting or setting the value of this object.</summary>
		/// <remarks><b>ALWAYS</b> uses <i><u>UN</u>encrypted</i> data! Encryption when relevant is managed by the <seealso cref="ToString"/> method!</remarks>
		protected string RawValue 
		{ 
			get => this._value;
			set
			{
				if ( !string.IsNullOrEmpty( value ) )
				{
					if ( this._quoteType == QuoteTypes.None )
					{
						QuoteTypes qt = SenseQuoteType( value, this._customQuotes );
						if ( qt != QuoteTypes.None )
						{
							this._quoteType = qt;
							this._value = UnwrapString( value, qt, this._customQuotes );
						}
						return;
					}

					if ( SenseQuoteType( value, this._customQuotes ) == this._quoteType )
						this._value = UnwrapString( value, this._quoteType, this._customQuotes );
				}
			}
		}

		public QuoteTypes QuoteType
		{
			get => this._quoteType;
			set => this._quoteType = value;
		}

		public int Length => this.ToString().Length;

		public char[] CustomQuotes
		{
			get => this._customQuotes;
			protected set
			{
				if ( value is not null && value.Length > 0 )
				{
					this._quoteType = QuoteTypes.Custom;
					this._customQuotes = value.Length == 1 ? new char[] { value[ 0 ], value[ 0 ] } : new char[] { value[ 0 ], value[ 1 ] };
				}
			}
		}

		protected char[] WrapChars => this._quoteType == QuoteTypes.Custom ? this._customQuotes : Wrapper( this._quoteType );
		#endregion

		#region Operators
		public static implicit operator IniLineValueFoundation(string source) => new( string.IsNullOrEmpty(source) ? "" : source );
		public static implicit operator string(IniLineValueFoundation source) => source.Value;
		#endregion

		#region Methods
		/// <summary>Reports on whether a speficied <seealso cref="Type"/> is supported.</summary>
		/// <param name="t">The C# <seealso cref="Type"/> to validate.</param>
		/// <returns><b>TRUE</b> if the provided type is supported by <seealso cref="IniLineValueFoundation"/></returns>
		public static bool IsValidDataType( Type t ) =>
			//t == typeof( string ) ||
			t == typeof( int ) ||
			t == typeof( sbyte ) ||
			t == typeof( short ) ||
			t == typeof( long ) ||
			t == typeof( uint ) ||
			t == typeof( byte ) ||
			t == typeof( ushort ) ||
			t == typeof( ulong ) ||
			t == typeof( decimal ) ||
			t == typeof( float ) ||
			t == typeof( double ) ||
			t == typeof( bool ) ||
			t == typeof( DateTime ) ||
			t == typeof( Point ) || t == typeof( Size ) ||
			t == typeof( PointF ) || t == typeof( SizeF );

		/// <summary>Reports on whether a specified generic <seealso cref="Type"/> is supported.</summary>
		/// <typeparam name="T">The generic type to validate.</typeparam>
		/// <returns><b>TRUE</b> if the type is supported by <seealso cref="IniLineValueFoundation"/></returns>
		public static bool IsValidDataType<T>() => IsValidDataType( typeof( T ) );

		public override string ToString() =>
			WrapValue( this._value, this._quoteType, this._customQuotes );

		/// <summary>Given an object and a <seealso cref="QuoteTypes"/> value, encloses the string equivalent in the appropriate wrapping characters.</summary>
		/// <param name="rawSource">An object whose <i>ToString()</i> representation is to be wrapped.</param>
		/// <param name="quoteType">A <seealso cref="QuoteTypes"/> value indicating how to wrap the string.</param>
		/// <param name="customQuotes">If the wrapper type is <seealso cref="QuoteTypes.Custom"/>, the custom character(s) are specified here.</param>
		/// <returns>The original value as a string with the appropriate wrapping characters added to the beginning and end.</returns>
		/// <remarks>If the value already contains the correct wrapping characters, nothing more is done.</remarks>
		public static string WrapValue( object? rawSource, QuoteTypes quoteType = QuoteTypes.DoubleQuote, char[]? customQuotes = null )
		{
			string source = rawSource is null ? String.Empty : rawSource.ToString();

			// If the supplied string already has the correct wrappers, there's nothing to do!
			if ( IsQuoteType( source, quoteType, customQuotes ) ) return source;

			if ( quoteType != QuoteTypes.None )
			{
				char[] quotes = Wrapper( quoteType );
				if ( quoteType == QuoteTypes.Custom )
				{
					if ( (customQuotes is null) && (customQuotes.Length > 0) )
						quotes = customQuotes.Length > 1 ? new char[] { customQuotes[ 0 ], customQuotes[ 1 ] } :
							new char[] { customQuotes[ 0 ], customQuotes[ 0 ] };
					else
						return source;
				}
				source = $"{quotes[ 0 ]}{source}{quotes[ 1 ]}";
			}
			return source;
		}

		/// <summary>Checks if the supplied string is wrapped in the specified wrapping characters and removes them if so.</summary>
		/// <param name="source">The string to work on.</param>
		/// <param name="quoteType">The <seealso cref="QuoteTypes"/> specifier to look-for / remove.</param>
		/// <param name="customQuotes">If the specified <i>quoteType</i> is <b><seealso cref="QuoteTypes.Custom"/></b>, this specifies the wrap characters to use.</param>
		/// <returns>If the passed string has the specified wrapping characters, they're removed with the remainder returned, otherwise the original string is returned.</returns>
		public static string UnwrapString( string source, QuoteTypes quoteType = QuoteTypes.DoubleQuote, char[]? customQuotes = null )
		{
			if ( string.IsNullOrWhiteSpace( source ) ) return string.Empty;
			return quoteType switch
			{
				QuoteTypes.Custom =>
					customQuotes is not null && customQuotes.Length > 0
						? (IsQuoteType( source, quoteType, customQuotes ) ? source[ 1..^1 ] : source)
						: source,
				QuoteTypes.None => source,
				_ => IsQuoteType( source, quoteType ) ? source[ 1..^1 ] : source
			};
		}

		/// <summary>Tests a given string to see if it's wrapped in the characters appropriate to the specified <i>quoteType</i>.</summary>
		/// <param name="source">The string to check.</param>
		/// <param name="quoteType">Specifies the wrapping set to check for.</param>
		/// <param name="customQuotes">If the specified <i>quoteType</i> is <b><seealso cref="QuoteTypes.Custom"/></b>, this specifies the wrap characters to use.</param>
		/// <returns><b>TRUE</b> if the provided string is wrapped in the appropriate characters for the specified type.</returns>
		public static bool IsQuoteType( string source, QuoteTypes quoteType, char[]? customQuotes = null )
		{
			if ( quoteType == QuoteTypes.None ) return true;
			if ( string.IsNullOrWhiteSpace( source ) ) return false;

			string[] c;
			if ( quoteType == QuoteTypes.Custom )
			{
				if ( (customQuotes is null) || (customQuotes.Length == 0) ) return false;
				c = customQuotes.Length == 1 ? new string[] { $"{Convert.ToByte( customQuotes[ 0 ] ):x2}", $"{Convert.ToByte( customQuotes[ 0 ] ):x2}" } :
					new string[] { $"\\x{Convert.ToByte( customQuotes[ 0 ] ):x2}", $"{Convert.ToByte( customQuotes[ 1 ] ):x2}" };
			}
			else
				c = Wrapper( quoteType ).Select( c => $"\\x{Convert.ToByte( c ):x2}" ).ToArray();

			if ( c[ 0 ] == "\x00" ) return false; // Some weird value was passed as the quoteType to check...

			return Regex.IsMatch( source, @$"^[{c[ 0 ]}][^{c[ 1 ]}]*[{c[ 1 ]}]$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant );
		}

		/// <summary>Given a specified <seealso cref="QuoteTypes"/> value, returns a character array containing the appropriate wrapping character(s),</summary>
		/// <remarks>If the specified <i>quoteType</i> value is unrecognized, or is <b><seealso cref="QuoteTypes.Custom"/></b>, the returned array contains <i>NULL</i> characters.</remarks>
		public static char[] Wrapper( QuoteTypes quoteType ) =>
			quoteType switch
			{
				QuoteTypes.DoubleQuote => new char[] { '\x22', '\x22' },    // "
				QuoteTypes.SingleQuote => new char[] { '\x27', '\x27' },    // '
				QuoteTypes.BackTick => new char[] { '\x2c', '\x2c' },       // `
				QuoteTypes.SquareBrackets => new char[] { '[', ']' },
				QuoteTypes.BraceBrackets => new char[] { '{', '}' },
				QuoteTypes.RoundBrackets => new char[] { '(', ')' },
				QuoteTypes.AngleBrackets => new char[] { '<', '>' },
				QuoteTypes.DoubleAngleBrackets => new char[] { '«', '»' },
				_ => new char[] { '\x00', '\x00' },                         // null
			};

		/// <summary>Given wrapping character(s), attempts to identify the correc <seealso cref="QuoteTypes"/> value.</summary>
		public static QuoteTypes GetQuoteTypeFromChar( params char[] chars )
		{
			switch ( chars.Length )
			{
				case 0: return QuoteTypes.None;
				case 1:
					return chars[ 0 ] switch
					{
						'\x00' => QuoteTypes.None,
						'\x22' => QuoteTypes.DoubleQuote,
						'\x27' => QuoteTypes.SingleQuote,
						'\x2c' => QuoteTypes.BackTick,
						'[' => QuoteTypes.SquareBrackets,
						'{' => QuoteTypes.BraceBrackets,
						'(' => QuoteTypes.RoundBrackets,
						'<' => QuoteTypes.AngleBrackets,
						'«' => QuoteTypes.DoubleAngleBrackets,
						_ => QuoteTypes.Custom
					};
				default:
					return $"{chars[ 0 ]}{chars[ 1 ]}" switch
					{
						"\x00\x00" => QuoteTypes.None,
						"\x22\x22" => QuoteTypes.DoubleQuote,
						"\x27\x27" => QuoteTypes.SingleQuote,
						"\x2c\x2c" => QuoteTypes.BackTick,
						"[]" => QuoteTypes.SquareBrackets,
						"{}" => QuoteTypes.BraceBrackets,
						"()" => QuoteTypes.RoundBrackets,
						"<>" => QuoteTypes.AngleBrackets,
						"«»" => QuoteTypes.DoubleAngleBrackets,
						_ => QuoteTypes.Custom
					};
			}
		}

		/// <summary>Given a <seealso cref="Regex"/> pattern string, inserts patterns for matching any valid <seealso cref="QuoteTypes"/> wrappers.</summary>
		/// <param name="source">The <seealso cref="Regex"/> pattern to modify.</param>
		/// <param name="customChars">If the pattern needs to accomodate custom wrapping characters, they are specified here.</param>
		/// <returns>A new <seealso cref="Regex"/> pattern with the added quote type validation patterns.</returns>
		/// <remarks>If the supplied pattern contains 'begin line' or 'end line' markers at the beginning or end, ("^" or "$"), they are
		/// retained in those places.</remarks>
		protected static string AddRegexQuotes( string source, char[]? customChars = null )
		{
			string front = "[", back = "[";
			foreach ( var e in Enum.GetValues<QuoteTypes>() )
			{
				string[] c = e switch
				{
					QuoteTypes.None => new string[] { "", "" },
					QuoteTypes.Custom => ((customChars is null) ? 0 : customChars.Length) switch
					{
						0 => new string[] { "", "" }, // Array.Empty<char>()
						1 => new string[] { $"\\x{Convert.ToByte( customChars[ 0 ] ):x2}", $"{Convert.ToByte( customChars[ 0 ] ):x2}" },
						_ => new string[] { $"\\x{Convert.ToByte( customChars[ 0 ] ):x2}", $"{Convert.ToByte( customChars[ 1 ] ):x2}" }
					},
					_ => Wrapper( e ).Select( c => $"\\x{Convert.ToByte( c ):x2}" ).ToArray()
				};

				if ( c.Length > 0 )
				{
					front += c[ 0 ];
					back += c[ 1 ];
				}
			}
			front += "]"; back += "]";

			string result = $"{front}{back}";
			if ( string.IsNullOrWhiteSpace( source ) )
				return result;

			if ( GenerateQuotesCheck().IsMatch( source ) )
			{
				var g = GenerateQuotesCheck().Match( source ).Groups;
				result = (g[ "pre" ].Success ? g[ "pre" ].Value : "") + front;
				result += g[ "content" ].Success ? g[ "content" ].Value : "";
				result += back + (g[ "post" ].Success ? g[ "post" ].Value : "");
			}

			return result;
		}

		/// <summary>Parses a supplied string against all known <i>QuoteTypes</i> and endeavors to detect if one applies.</summary>
		/// <param name="source">The string to parse.</param>
		/// <param name="customChars">If custom wrapping characters may be detected, they are specified here.</param>
		/// <returns>If one can be matched, the applicable <seealso cref="QuoteTypes"/> value, otherwise <b><seealso cref="QuoteTypes.None"/></b>.</returns>
		public static QuoteTypes SenseQuoteType( string source, char[]? customChars = null )
		{
			if ( !string.IsNullOrWhiteSpace( source ) )
				foreach ( var e in Enum.GetValues<QuoteTypes>() )
					if ( (e != QuoteTypes.None) && IsQuoteType( source, e, customChars ) ) return e;

			// Couldn't make a match, so assume no relevant quote characters present..
			return QuoteTypes.None;
		}

		/// <summary>Given a payload string, attempts to parse data, <seealso cref="QuoteTypes"/> and comment info.</summary>
		/// <param name="source">The unprocessed raw <i>data</i> from the original line.</param>
		/// <param name="customQuotes">If custom quotes are being used, they have to be defined here.</param>
		/// <returns>A string array with the following contents:<br></br>
		/// [ 0 ] » The entire data portion of the content, <i>including</i> wrapping chars.<br></br>
		/// [ 1 ] » Everything found on the line after the payload.<br></br>
		/// [ 2 ] » The data portion of the content, <i>excluding</i> wrapping chars.<br></br>
		/// [ 3 ] » If a comment was identified in the chaff, the comment content.<br></br>
		/// [ 4 ] » A string representation of the <seealso cref="QuoteTypes"/> value that was detected.<br></br>
		/// [ 5 ] » The complete original string that was parsed.
		/// </returns>
		public static string[] ParseLineContent( string source, char[]? customQuotes = null )
		{
			if ( !string.IsNullOrWhiteSpace( source ) )
			{
				Match m;
				foreach ( var e in Enum.GetValues<QuoteTypes>() )
					if ( e != QuoteTypes.None )
					{
						string[] wrapper = e switch
						{
							QuoteTypes.Custom => customQuotes is null ? Array.Empty<string>()
								: customQuotes.Length switch
								{
									0 => Array.Empty<string>(),
									1 => new string[] { $"{customQuotes[ 0 ]}", $"{customQuotes[ 0 ]}" },
									_ => new string[] { $"{customQuotes[ 0 ]}", $"{customQuotes[ 1 ]}" }
								},
							_ => Wrapper( e ).Select( c => $"\\x{Convert.ToByte( c ):x2}" ).ToArray()
						};

						if ( wrapper.Length == 2 )
						{
							string pattern = @$"^(?<payload>[{wrapper[ 0 ]}](?<data>[^{wrapper[ 1 ]}]*)[{wrapper[ 1 ]}])(?<chaff>[\s]+(?<comment>[#/]{{2,}}[\s\S]*))?$";
							m = Regex.Match( source, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline );
							if ( m.Success )
								return new string[] {
									m.Groups[ "payload" ].Success ? m.Groups[ "payload" ].Value : "",
									m.Groups[ "chaff" ].Success ? m.Groups[ "chaff" ].Value : "",
									m.Groups[ "data" ].Success ? m.Groups[ "data" ].Value : "",
									m.Groups[ "comment" ].Success ? m.Groups[ "comment" ].Value : "",
									$"{e}", source
								};
						}
					}

				m = QuotelessLineParser().Match( source );
				if ( m.Success )
					return new string[] {
								m.Groups[ "data" ].Success ? m.Groups[ "data" ].Value : "",
								m.Groups[ "chaff" ].Success ? m.Groups[ "chaff" ].Value : "",
								m.Groups[ "data" ].Success ? m.Groups[ "data" ].Value : "",
								m.Groups[ "comment" ].Success ? m.Groups[ "comment" ].Value : "",
								$"{QuoteTypes.None}", source
					};
			}
			return new string[] { source, "", source, "", $"{QuoteTypes.None}", source };
		}

		/// <summary>Endeavors to identify an appropriate type for the kind of data that was passed.</summary>
		/// <param name="data">A string containing the raw data to analyse.</param>
		/// <param name="goDeep">If set <b>true</b>, the routine does more thorough testing and attempts to return a more accurate/specific type. 
		/// Otherwise only superficial testing is performed and perfunctory data types are returned (<seealso cref="Decimal"/>,
		/// <seealso cref="int"/>, <seealso cref="uint"/> and <seealso cref="string"/>).</param>
		/// <returns>The best determination of the type of data that was passed, according to the depth of inspection requested.</returns>
		/// <remarks>
		/// Performing a 'Deep' inspection will determine if the value is a number or a string.<br/><br/>For numbers:
		/// First it determines if it has a decimal or not, if not, Integers are range checked and the smallest system type that's 
		/// capable of holding the passed value will be returned.<br/><br/>
		/// Negative integers will always return as a <i>signed</i> type while non-negatives will always come back as <i><u>un</u>signed</i>.<br/><br/>
		/// For decimalized values, it will recognize the following alphabetic suffixes: 'M' = <seealso cref="Decimal"/>, 'F' = <seealso cref="float"/>,
		/// 'D' = <seealso cref="double"/> (if none is specified, <seealso cref="decimal"/> will be the default).<br/><br/>
		/// If no number type can be determined, the value will be checked to see if it can be parsed into either <seealso cref="bool"/>, or <seealso cref="DateTime"/>.
		/// Finally, it will check to see if the string is <seealso cref="System.Buffers.Text.Base64"/> encoded and report the return type as 
		/// a <i>byte[]</i> if so.<br/><br/>If no specific type is able to be identified, the default result will be a <seealso cref="string"/>.
		/// </remarks>
		public static Type DetectType( string data, bool goDeep = false )
		{
			if ( !string.IsNullOrWhiteSpace( data ) )
			{
				if ( Regex.IsMatch( data.Replace(" ",""), @"^[+-]?[\d,]+$", RegexOptions.None ))
				{
					data = Regex.Replace( data, @"[^-\d]", "" );
					if ( (data.Length > 0) && (data[0]=='-') )
					{
						long v = long.Parse( data );
						return goDeep ? v switch
						{
							> int.MaxValue => typeof( long ),
							< int.MinValue => typeof( long ),
							> short.MaxValue => typeof( int ),
							< short.MinValue => typeof( int ),
							> sbyte.MaxValue => typeof( short ),
							< sbyte.MinValue => typeof( short ),
							_ => typeof( sbyte )
						} : typeof(int);
					}
					else
					{
						ulong v = ulong.Parse( data );
						return goDeep ? v switch
						{
							> uint.MaxValue => typeof( ulong ),
							> ushort.MaxValue => typeof( uint ),
							> byte.MaxValue => typeof( ushort ),
							_ => typeof( byte )
						} : typeof( uint );
					}
				}

				if ( Regex.IsMatch( data, @"^[-+]?[\d,]+(?:[.][\d]*)?[mMsSdDfF]?$", RegexOptions.None ) )
				{
					return goDeep ? data.ToLowerInvariant()[ ^1 ] switch
					{

						's' => typeof( float ),
						'f' => typeof( float ),
						'd' => typeof( double ),
						_ => typeof( decimal ),
					} : typeof( decimal );
				}

				if ( goDeep )
				{
					if ( PointValidation().IsMatch( data ) )
						return typeof( Point );

					if ( PointFValidation().IsMatch( data ) )
						return typeof( PointF );

					if ( Base64Validation().IsMatch( data ) ) return typeof( byte[] );

					if ( DateTime.TryParse( data, out _ ) ) return typeof( DateTime );

					if ( BooleanValueValidation().IsMatch( data ) ) return typeof( bool );
				}
			}
			return typeof( string );
		}

		/// <summary>Facilitates accessing the value of the object as a variety of types.</summary>
		/// <typeparam name="T">
		/// Can be any of the following: <seealso cref="sbyte"/>, <seealso cref="byte"/>, <seealso cref="short"/>, <seealso cref="ushort"/>, 
		/// <seealso cref="int"/>, <seealso cref="uint"/>, <seealso cref="long"/>, <seealso cref="ulong"/>, 
		/// <seealso cref="Int128"/>, <seealso cref="UInt128"/>, <seealso cref="double"/>, 
		/// <seealso cref="float"/>, <seealso cref="decimal"/>, <seealso cref="string"/>, <seealso cref="char"/>, <seealso cref="bool"/>, 
		/// <i>char[]</i>, <i>byte[]</i>, <seealso cref="Size"/>, <seealso cref="Point"/>, <seealso cref="SizeF"/>, <seealso cref="PointF"/>
		/// </typeparam>
		/// <returns>The value of this object as the type indicated by the <typeparamref name="T"/> parameter.<br/><br/>
		/// <b>NOTE:</b> if the data is a valid <i>Base64</i> string, and the requested type is <i>byte[]</i>, this routine <i>will</i> 
		/// decompile the string into its binary equivalent and return that.
		/// </returns>
		/// <exception cref="InvalidCastException">If the supplied type isn't recognized.</exception>
		/// <remarks>
		/// Supported types: <seealso cref="sbyte"/>, <seealso cref="byte"/>, <seealso cref="short"/>, <seealso cref="ushort"/>, 
		/// <seealso cref="int"/>, <seealso cref="uint"/>, <seealso cref="long"/>, <seealso cref="ulong"/>,
		/// <seealso cref="Int128"/>, <seealso cref="UInt128"/>, <seealso cref="double"/>, <seealso cref="float"/>, 
		/// <seealso cref="decimal"/>, <seealso cref="string"/>, <seealso cref="char"/>, <seealso cref="bool"/>, <i>char[]</i>, 
		/// <i>byte[]</i>, <seealso cref="Size"/>, <seealso cref="Point"/>, <seealso cref="SizeF"/>, <seealso cref="PointF"/>.
		/// </remarks>
		public dynamic? As<T>()
		{
			if ( typeof( T ) == typeof( string ) ) return Value;

			string value = Value;
			T var = (T)Activator.CreateInstance( typeof( T ) );
			if (var is not null)
				switch ( var )
				{
					case sbyte:
					case short:
					case int:
					case long:
					case Int128:
						value = Regex.Replace( value, @"[^-\d]", "" );
						return Regex.IsMatch( value, @"^-?[\d]+$", RegexOptions.None ) ?
							var switch
							{
								int => int.Parse( value ),
								short => short.Parse( value ),
								sbyte => sbyte.Parse( value ),
								long => long.Parse( value ),
								Int128 => Int128.Parse( value ),
								_ => default(T)
							} : default( T );
					case byte:
					case ushort:
					case uint:
					case ulong:
					case UInt128:
						value = Regex.Replace( value, @"[^\d]", "" );
						return Regex.IsMatch( value, @"^[\d]+$", RegexOptions.None ) ?
							var switch
							{
								uint => uint.Parse( value ),
								ushort => ushort.Parse( value ),
								byte => byte.Parse( value ),
								ulong => ulong.Parse( value ),
								UInt128 => UInt128.Parse( value ),
								_ => default( T )
							} : default( T );
					case decimal:
					case float:
					case double:
						value = Regex.Replace( value, @"[^-.\d]", "" );
						return Regex.IsMatch( value, @"^-?[\d]+(?:[.][\d]*)?$", RegexOptions.None ) ?
							var switch
							{
								decimal => decimal.Parse( value ),
								float => float.Parse( value ),
								double => double.Parse( value ),
								_ => default( T )
							} : default( T );
					case bool:
						return BooleanValueValidation().IsMatch( value ) && BooleanValueParser().IsMatch( value );
					case DateTime:
						return DateTime.TryParse( value, out DateTime d ) ? d: DateTime.Now;
					case char:
						return char.Parse( value );
					case char[]:
						return value.ToCharArray();
					case byte[]:
						// If the underlying string is a valid Base-64 string, convert that to a byte-array
						// and return it, otherwise just convert the unencrypted version of the value to a byte-array.
						return 
							string.IsNullOrEmpty( this.Value ) 
							? Array.Empty<byte>() 
							: (Base64Validation().IsMatch( this._value ) ? Convert.FromBase64String( this._value ) : this.Value.ToByteArray());
					case Point:
					case Size:
						Match m1 = PointValidation().Match( this.Value );
						if ( m1.Success )
						{
							int x = m1.Groups[ "x" ].Success ? int.Parse( m1.Groups[ "x" ].Value ) : int.MinValue,
								y = m1.Groups[ "y" ].Success ? int.Parse( m1.Groups[ "y" ].Value ) : int.MinValue;

							return var switch
							{
								Point => new Point( x, y ),
								Size => new Size( x, y ),
								_ => null
							};
						}
						break;
					case PointF:
					case SizeF:
						Match m2 = PointFValidation().Match( this.Value );
						if ( m2.Success )
						{
							float x = m2.Groups[ "x" ].Success ? float.Parse( m2.Groups[ "x" ].Value ) : float.MinValue,
								  y = m2.Groups[ "y" ].Success ? float.Parse( m2.Groups[ "y" ].Value ) : float.MinValue;

							return var switch
							{
								PointF => typeof( PointF ),
								SizeF => new SizeF( x, y ),
								_ => null
							};
						}
						break;
				}

			throw new InvalidCastException( $"The specified 'type' is not supported/recognized. (\x22{(typeof( T )).Name}\x22)" );
		}

		public T ToEnum<T>() where T : Enum => this._value.ToEnum<T>();

		[GeneratedRegex( @"^(?<pre>[\^])(?<content>[\s\S]+)(?<post>[$])$", RegexOptions.None )]
		private static partial Regex GenerateQuotesCheck();

		[GeneratedRegex( @"^(?<data>[\s\S]*)?(?<chaff>[\s]+(?<comment>(##|//)[\s\S]*)?)?$" )]
		private static partial Regex QuotelessLineParser();

		[GeneratedRegex( "^([y1tn0f]|On|Off|True|False|Yes|No)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant )]
		private static partial Regex BooleanValueValidation();

		[GeneratedRegex( "^([y1t]|On|True|Yes)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant )]
		private static partial Regex BooleanValueParser();

		[GeneratedRegex( "^(?:[a-z\\d+/]{4})*(?:[a-z\\d+/]{2}==|[a-z\\d+/]{3}=)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant )]
		private static partial Regex Base64Validation();

		[GeneratedRegex( @"^[(\[{][\s]*(?<x>[\d]+[.](?:[\d]*)?)[\s]*[,;][\s]*(?<y>[\d]+[.](?:[\d]*)?)[\s]*[}\])]$", RegexOptions.None )]
		private static partial Regex PointFValidation();

		[GeneratedRegex( @"^[(\[{][\s]*(?<x>[\d]+)[\s]*[,;][\s]*(?<y>[\d]+)[\s]*[}\])]$", RegexOptions.None )]
		private static partial Regex PointValidation();
		#endregion
	}

	/*
		public abstract partial class IniLineValue<T> : IniLineValueFoundation
		{
			#region Constructors
			protected IniLineValue( bool encrypted = false, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
				: base( encrypted, quoteType, quoteChars ) => ValidateGenericType();

			protected IniLineValue( string value, bool encrypted = false, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
				: base( "", encrypted, quoteType, quoteChars )
			{
				ValidateGenericType();
				if ( !ValidateForm( value ) )
					throw new ArgumentException( $"The supplied value (\x22{value}\x22) could not be converted to type {typeof( T ).Name} by the parsing engine." );

				this._value = value;
			}

			protected IniLineValue( T value, bool encrypted = false, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
				: base( "", encrypted, quoteType, quoteChars )
			{
				ValidateGenericType();
				this.Value = value;
			}
			#endregion

			#region Accessors
			new protected T Value
			{
				get => ConvertValue( this._value );
				set => this._value = value is not null ? "" : value.ToString();
			}
			#endregion

			#region Operators
			public static implicit operator T( IniLineValue<T> source ) => source is null ? default : source.Value;
			#endregion

			#region Methods
			private void ValidateGenericType()
			{
				// For any class/type that's not listed here, use String and parse it yourself!
				if (
						typeof( T ) != typeof( string ) &&
						typeof( T ) != typeof( bool ) &&
						typeof( T ) != typeof( sbyte ) &&
						typeof( T ) != typeof( short ) &&
						typeof( T ) != typeof( int ) &&
						typeof( T ) != typeof( long ) &&
						typeof( T ) != typeof( byte ) &&
						typeof( T ) != typeof( ushort ) &&
						typeof( T ) != typeof( uint ) &&
						typeof( T ) != typeof( ulong ) &&
						typeof( T ) != typeof( decimal ) &&
						typeof( T ) != typeof( float ) &&
						typeof( T ) != typeof( double ) &&
						typeof( T ) != typeof( decimal )
				   )
					throw new TypeInitializationException( typeof( T ).FullName, null );
			}

			/// <summary>Daughter classes must provide a mechanism to convert a string into a valid value.</summary>
			/// <param name="value">The string to convert the value of.</param>
			protected abstract T ConvertValue( string value );

			/// <summary>Daughter classes must provide a means to validate incoming strings.</summary>
			/// <param name="value">A proposed string to validate.</param>
			/// <returns><b>TRUE</b> if the supplied string conforms with a valid value for the daughter type.</returns>
			protected abstract bool ValidateForm( string value );

			protected string AddRegexQuotes( string source ) => AddRegexQuotes( source, this.CustomQuotes );
			#endregion
		}

		public class IniLineStringValue : IniLineValue<string>
		{
			#region Constructors
			public IniLineStringValue( bool encrypted = false )
				: base( encrypted, QuoteTypes.DoubleQuote ) { }

			/// <summary>The quoteType and quoteChars values here are purely for parameter compatibility, They have NO EFFECT!</summary>
			/// <param name="quoteType">This parameter is here purely for compatibility, it does nothing.</param>
			/// <param name="quoteChars">This parameter is here purely for compatibility, it does nothing.</param>
			public IniLineStringValue( string value, bool encrypted = false, QuoteTypes quoteType = QuoteTypes.DoubleQuote, char[]? quoteChars = null )
				: base( "", encrypted, QuoteTypes.DoubleQuote )
			{
				if ( !ValidateForm( value ) )
					throw new ArgumentException( $"The supplied string value cannot be null." );

				this._value = ConvertValue( value );
			}


			#endregion

			#region Accessors
			new public QuoteTypes QuoteType => this._quoteType;
			#endregion

			#region Operators
			public static implicit operator string( IniLineStringValue source ) => source is null ? "" : source.Value;
			public static implicit operator IniLineStringValue( string source ) => new( string.IsNullOrEmpty( source ) ? "" : source );
			#endregion

			#region Methods
			protected override string ConvertValue( string value )
			{
				value = UnwrapString( value, QuoteTypes.DoubleQuote );
				value = this._encrypted ? AES.DecryptStringToString( value ) : value;
				return value;
			}

			protected override bool ValidateForm( string value ) => (value is not null);
			#endregion
		}

		public partial class IniLineUIntValue : IniLineValue<uint>
		{
			#region Constructors
			public IniLineUIntValue( QuoteTypes quoteType = QuoteTypes.None ) : base( false, quoteType ) { }

			public IniLineUIntValue( uint value, QuoteTypes quoteType = QuoteTypes.None ) : base( value, false, quoteType ) { }

			public IniLineUIntValue( string value, bool encrypted = false, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
				: base( value, encrypted, quoteType, customQuotes ) { }
			#endregion

			#region Operators
			public static implicit operator uint( IniLineUIntValue source ) => source is null ? 0 : source.Value;
			public static implicit operator IniLineUIntValue( uint source ) => new( source );
			#endregion

			#region Methods
			protected override uint ConvertValue( string value )
			{
				if ( string.IsNullOrEmpty( value ) ) return 0;
				value = UIntValueSanitizer().Replace( value, "" );
				if ( UIntValueValidation().IsMatch( value ) )
					return uint.Parse( UIntValueSanitizer().Replace( value, "" ) );

				throw new ArgumentException( $"The supplied value (\x22{value}\x22) isn't a valid unsigned integer!" );
			}

			protected override bool ValidateForm( string value ) =>
				UIntValueValidation().IsMatch( UIntValueSanitizer().Replace( value, "" ) );

			[GeneratedRegex( @"[^\d]", RegexOptions.None )]
			private static partial Regex UIntValueSanitizer();

			[GeneratedRegex( @"^[+]?[\d]+$", RegexOptions.None )]
			private static partial Regex UIntValueValidation();
			#endregion
		}

		public partial class IniLineIntValue : IniLineValue<int>
		{
			#region Constructors
			public IniLineIntValue( QuoteTypes quoteType = QuoteTypes.None ) : base( false, quoteType ) { }

			public IniLineIntValue( int value, QuoteTypes quoteType = QuoteTypes.None ) : base( value, false, quoteType ) { }

			public IniLineIntValue( string value, bool encrypted = false, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
				: base( value, encrypted, quoteType, customQuotes ) { }
			#endregion

			#region Operators
			public static implicit operator int( IniLineIntValue source ) => source is null ? 0 : source.Value;
			public static implicit operator IniLineIntValue( int source ) => new( source );
			#endregion

			#region Methods
			protected override int ConvertValue( string value )
			{
				if ( string.IsNullOrEmpty( value ) ) return 0;
				value = IntValueSanitizer().Replace( value, "" );
				if ( IntValueValidation().IsMatch( value ) )
					return int.Parse( IntValueSanitizer().Replace( value, "" ) );

				throw new ArgumentException( $"The supplied value (\x22{value}\x22) isn't a valid integer!" );
			}

			protected override bool ValidateForm( string value ) =>
				IntValueValidation().IsMatch( IntValueSanitizer().Replace( value, "" ) );

			[GeneratedRegex( @"[^-\d]", RegexOptions.None )]
			private static partial Regex IntValueSanitizer();

			[GeneratedRegex( @"^[-+]?[\d]+$", RegexOptions.None )]
			private static partial Regex IntValueValidation();
			#endregion
		}

		public partial class IniLineDecimalValue : IniLineValue<decimal>
		{
			#region Constructors
			public IniLineDecimalValue( QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null ) : base( false, quoteType, customQuotes ) { }

			public IniLineDecimalValue( decimal value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null ) : base( value, false, quoteType, customQuotes ) { }

			public IniLineDecimalValue( string value, bool encrypted = false, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
				: base( value, encrypted, quoteType, customQuotes ) { }
			#endregion

			#region Operators
			public static implicit operator decimal( IniLineDecimalValue source ) => source is null ? 0M : source.Value;
			public static implicit operator IniLineDecimalValue( decimal source ) => new( source );
			#endregion

			#region Methods
			protected override decimal ConvertValue( string value )
			{
				if ( string.IsNullOrEmpty( value ) ) return 0M;
				value = DecimalValueSanitizer().Replace( value, "" );
				if ( DecValueValidation().IsMatch( value ) )
					return decimal.Parse( DecimalValueSanitizer().Replace( value, "" ) );

				throw new ArgumentException( $"The supplied value (\x22{value}\x22) isn't a valid integer!" );
			}

			protected override bool ValidateForm( string value ) =>
				DecValueValidation().IsMatch( DecimalValueSanitizer().Replace( value, "" ) );

			[GeneratedRegex( @"[^-\d.]", RegexOptions.None )]
			private static partial Regex DecimalValueSanitizer();

			[GeneratedRegex( @"^[+-]?[\d]+(?:[.][\d]*)?$", RegexOptions.None )]
			private static partial Regex DecValueValidation();
			#endregion
		}

		public class IniLineUrlValue : IniLineValue<string>
		{
			#region Constructors
			public IniLineUrlValue( bool encrypted = false, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
				: base( encrypted, quoteType, customQuotes ) { }

			public IniLineUrlValue( Uri value, bool encrypted = false, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
				: base( value.ToString(), encrypted, quoteType, customQuotes ) { }

			public IniLineUrlValue( string value, bool encrypted = false, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
				: base( value, encrypted, quoteType, customQuotes ) { }
			#endregion

			#region Accessors
			new public Uri Value
			{
				get => new( this._value );
				set
				{
					if ( value is not null )
						this._value = value.ToString();
				}
			}
			#endregion

			#region Operators
			public static implicit operator Uri( IniLineUrlValue source ) => source.Value;
			public static implicit operator IniLineUrlValue( Uri source ) => new( source );
			#endregion

			#region Methods
			protected override bool ValidateForm( string value )
			{
				if ( string.IsNullOrWhiteSpace( value ) ) return false;

				QuoteTypes qt = SenseQuoteType( value );
				value = UnwrapString( value, qt, this._customQuotes );

				try { Uri test = new( value ); } catch ( UriFormatException ) { return false; }

				return true;
			}

			protected override string ConvertValue( string value )
			{
				QuoteTypes qt = SenseQuoteType( value );
				value = qt == QuoteTypes.None ? value : UnwrapString( value, qt, this._customQuotes );
				Uri test = new( value );
				return test.ToString();
			}
			#endregion
		}

		public class IniLineDateTimeValue : IniLineValue<string>
		{
			#region Constructors
			public IniLineDateTimeValue( bool encrypted = false, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
				: base( encrypted, quoteType, customQuotes ) { }

			public IniLineDateTimeValue( DateTime value, bool encrypted = false, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
				: base( value.ToMySqlString(), encrypted, quoteType, customQuotes ) { }

			public IniLineDateTimeValue( string value, bool encrypted = false, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
				: base( value, encrypted, quoteType, customQuotes ) { }
			#endregion

			#region Accessors
			new public DateTime Value
			{
				get => this._value.ParseMySqlDateTime();
				set => this._value = value.ToMySqlString();
			}
			#endregion

			#region Operators
			public static implicit operator DateTime( IniLineDateTimeValue source ) => source is null ? new DateTime() : source.Value;
			public static implicit operator IniLineDateTimeValue( DateTime source ) => new( source );
			#endregion

			#region Methods
			protected override bool ValidateForm( string value )
			{
				if ( string.IsNullOrWhiteSpace( value ) ) return false;

				QuoteTypes qt = SenseQuoteType( value );
				value = UnwrapString( value, qt, this._customQuotes );
				return DateTime.TryParse( value, out _ );
			}

			protected override string ConvertValue( string value )
			{
				QuoteTypes qt = SenseQuoteType( value );
				value = qt == QuoteTypes.None ? value : UnwrapString( value, qt, this._customQuotes );

				if ( DateTime.TryParse( value, out DateTime check ) )
					value = check.ToMySqlString();

				return value;
			}
			#endregion
		}

		public partial class IniLineBooleanValue : IniLineValue<bool>
		{
			#region Constructors
			public IniLineBooleanValue( QuoteTypes quoteType = QuoteTypes.None ) : base( false, quoteType ) { }

			public IniLineBooleanValue( bool value, QuoteTypes quoteType = QuoteTypes.None ) : base( value, false, quoteType ) { }

			public IniLineBooleanValue( string value, bool encrypted = false, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
				: base( value, encrypted, quoteType, customQuotes ) { }
			#endregion

			#region Accessors
			// boolean items can't be encrypted...
			new public bool Encrypted
			{
				get => this._encrypted;
				set { } // needs to be here for compatability, but don't want to actually allow setting this value.
			}
			#endregion

			#region Operators
			public static implicit operator bool( IniLineBooleanValue source ) => source.Value;
			public static implicit operator IniLineBooleanValue( bool source ) => new( source );
			#endregion

			#region Methods
			protected override bool ValidateForm( string value )
			{
				QuoteTypes qt = SenseQuoteType( value );
				value = UnwrapString( value, qt, this._customQuotes );

				return BooleanValueValidation().IsMatch( value );
			}

			protected override bool ConvertValue( string value ) =>
				!string.IsNullOrWhiteSpace( value ) && BooleanValueParser().IsMatch( value );

			public override string ToString() =>
				WrapValue( (this.Value ? "true" : "false"), this._quoteType, this._customQuotes );


			[GeneratedRegex( "^([y1tn0f]|On|Off|True|False|Yes|No)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant )]
			private static partial Regex BooleanValueValidation();

			[GeneratedRegex( "^([y1t]|On|True|Yes)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant )]
			private static partial Regex BooleanValueParser();
			#endregion
		}

		public partial class IniLineXYValue : IniLineValue<string>
		{
			#region Constructors
			public IniLineXYValue() : base( false, QuoteTypes.RoundBrackets ) { }

			public IniLineXYValue( Point value ) : base( ParseValue( value ), false, QuoteTypes.RoundBrackets ) { }

			public IniLineXYValue( Size value ) : base( ParseValue( value ), false, QuoteTypes.RoundBrackets ) { }

			public IniLineXYValue( int x, int y ) : base( ParseValue( x, y ), false, QuoteTypes.RoundBrackets ) { }

			public IniLineXYValue( string value, bool encrypted = false, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
				: base( value, encrypted, quoteType, customQuotes ) { }
			#endregion

			#region Operators
			public static implicit operator Point( IniLineXYValue source ) => source is null ? new Point( 0, 0 ) : new Point( source.X, source.Y );
			public static implicit operator Size( IniLineXYValue source ) => source is null ? new Size( 0, 0 ) : new Size( source.X, source.Y );
			public static implicit operator IniLineXYValue( Point source ) => new( source );
			public static implicit operator IniLineXYValue( Size source ) => new( source );
			#endregion

			#region Accessors
			public int X
			{
				get => ParseValue( this._value ).X;
				set => this._value = ParseValue( value, Y );
			}

			public int Y
			{
				get => ParseValue( this._value ).Y;
				set => this._value = ParseValue( X, value );
			}

			new public bool Encrypted
			{
				get => this._encrypted;
				set { } // needs to be here for compatability, but don't want to actually allow setting this value.
			}
			#endregion

			#region Methods
			protected override string ConvertValue( string value )
			{
				QuoteTypes qt = SenseQuoteType( value );
				try
				{
					Point r = ParseValue( UnwrapString( value, qt ).Trim() );
					value = ParseValue( r );
				}
				catch ( ArgumentException ) { value = ""; }

				return value;
			}

			protected override bool ValidateForm( string value )
			{
				QuoteTypes qt = SenseQuoteType( value );
				value = UnwrapString( value, qt );
				Match m = ParseXYValue().Match( value.Trim() );
				return m.Success && m.Groups[ "xint" ].Success && !m.Groups[ "xd" ].Success && m.Groups[ "yint" ].Success && !m.Groups[ "yd" ].Success;
			}

			protected static string ParseValue( int x, int y ) => WrapValue( $"{x}, {y}", QuoteTypes.RoundBrackets );
			protected static string ParseValue( Point p ) => ParseValue( p.X, p.Y );
			protected static string ParseValue( Size s ) => ParseValue( s.Width, s.Height );

			protected static Point ParseValue( string value )
			{
				QuoteTypes qt = SenseQuoteType( value );
				value = UnwrapString( value, qt );

				Match m = ParseXYValue().Match( value.Trim() );
				if ( m.Success && m.Groups[ "xint" ].Success && m.Groups[ "yint" ].Success )
				{
					int x = int.Parse( m.Groups[ "xint" ].Value ), y = int.Parse( m.Groups[ "yint" ].Value );
					return new Point( x, y );
				}
				throw new ArgumentException( $"The supplied string could not be parsed. (\x22{value}\x22)" );
			}

			[GeneratedRegex( @"^(?<x>(?<xint>[+-]?[\d]+)(?<xd>[.][\d]*)?)[\s]*[,;xX][\s]*(?<y>(?<yint>[+-]?[\d]+)(?<yd>[.][\d]*)?)$", RegexOptions.CultureInvariant )]
			public static partial Regex ParseXYValue();
			#endregion
		}

		public partial class IniLineXfYfValue : IniLineValue<string>
		{
			#region Constructors
			public IniLineXfYfValue() : base( false, QuoteTypes.RoundBrackets ) { }

			public IniLineXfYfValue( PointF value, QuoteTypes quoteType = QuoteTypes.None ) : base( ParseValue( value ), false, quoteType ) { }

			public IniLineXfYfValue( SizeF value, QuoteTypes quoteType = QuoteTypes.None ) : base( ParseValue( value ), false, quoteType ) { }

			public IniLineXfYfValue( float x, float y, QuoteTypes quoteType = QuoteTypes.None ) : base( ParseValue( x, y ), false, quoteType ) { }

			public IniLineXfYfValue( string value, bool encrypted = false, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
				: base( value, encrypted, quoteType, customQuotes ) { }
			#endregion

			#region Operators
			public static implicit operator PointF( IniLineXfYfValue source ) => source is null ? new PointF( 0, 0 ) : new PointF( source.X, source.Y );
			public static implicit operator SizeF( IniLineXfYfValue source ) => source is null ? new SizeF( 0, 0 ) : new SizeF( source.X, source.Y );
			public static implicit operator IniLineXfYfValue( PointF source ) => new( source );
			public static implicit operator IniLineXfYfValue( SizeF source ) => new( source );
			#endregion

			#region Accessors
			public float X
			{
				get => ParseValue( this._value ).X;
				set => this._value = ParseValue( value, Y );
			}

			public float Y
			{
				get => ParseValue( this._value ).Y;
				set => this._value = ParseValue( X, value );
			}

			new public bool Encrypted
			{
				get => this._encrypted;
				set { } // needs to be here for compatability, but don't want to actually allow setting this value.
			}
			#endregion

			#region Methods
			protected override string ConvertValue( string value )
			{
				QuoteTypes qt = SenseQuoteType( value );
				try
				{
					PointF r = ParseValue( UnwrapString( value, qt ).Trim() );
					value = ParseValue( r );
				}
				catch ( ArgumentException ) { value = ""; }

				return value;
			}

			protected override bool ValidateForm( string value )
			{
				QuoteTypes qt = SenseQuoteType( value );
				value = UnwrapString( value, qt );
				Match m = IniLineXYValue.ParseXYValue().Match( value.Trim() );
				return m.Success && m.Groups[ "xint" ].Success && m.Groups[ "xd" ].Success && m.Groups[ "yint" ].Success && m.Groups[ "yd" ].Success;
			}

			protected static PointF ParseValue( string value )
			{
				QuoteTypes qt = SenseQuoteType( value );
				value = UnwrapString( value, qt ).Trim();

				Match m = IniLineXYValue.ParseXYValue().Match( value.Trim() );
				if ( m.Success && m.Groups[ "x" ].Success && m.Groups[ "y" ].Success )
				{
					float x = float.Parse( m.Groups[ "x" ].Value ), y = float.Parse( m.Groups[ "y" ].Value );
					return new PointF( x, y );
				}
				throw new ArgumentException( $"The supplied string could not be parsed. (\x22{value}\x22)" );
			}

			protected static string ParseValue( float x, float y ) => WrapValue( $"{x}, {y}", QuoteTypes.RoundBrackets );
			protected static string ParseValue( PointF p ) => ParseValue( p.X, p.Y );
			protected static string ParseValue( SizeF s ) => ParseValue( s.Width, s.Height );
			#endregion
		}

		public partial class IniLineBinaryValue : IniLineValue<string>
		{
			#region Constructors
			public IniLineBinaryValue( bool encrypt = false )
				: base( encrypt, QuoteTypes.DoubleQuote ) { }

			public IniLineBinaryValue( byte[] value, bool encrypt = false )
				: base( "", encrypt, QuoteTypes.DoubleQuote ) =>
				this.Value = value;

			public IniLineBinaryValue( string value, bool encrypted = false )
				: base( value, encrypted, QuoteTypes.DoubleQuote ) { }
			#endregion

			#region Accessors
			new public byte[] Value
			{
				get => Base64Validator().IsMatch( this._value ) ? this._value.FromBase64String() : Array.Empty<byte>();
				set => this._value = value.Length > 0 ? value.ToBase64String() : "";
			}
			#endregion

			#region Operators
			public static implicit operator byte[]( IniLineBinaryValue source ) => source is null ? Array.Empty<byte>() : source.Value;
			public static implicit operator IniLineBinaryValue( byte[] source ) => new( source );
			#endregion

			#region Methods
			protected override bool ValidateForm( string value )
			{
				QuoteTypes qt = SenseQuoteType( value );
				value = Base64Sanitizer().Replace( UnwrapString( value, qt ).Trim(), "" );
				return Base64Validator().IsMatch( value );
			}

			protected override string ConvertValue( string value )
			{
				QuoteTypes qt = SenseQuoteType( value );
				value = Base64Sanitizer().Replace( UnwrapString( value, qt ).Trim(), "" );
				return Base64Validator().IsMatch( value ) ? value : "";
			}

			[GeneratedRegex( @"^(?:[a-z\d+/]{4})*(?:[a-z\d+/]{2}==|[a-z\d+/]{3}=)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant )]
			private static partial Regex Base64Validator();

			[GeneratedRegex( "[^a-z=+A-Z\\d/]" )]
			private static partial Regex Base64Sanitizer();
			#endregion
		}
	*/
}
