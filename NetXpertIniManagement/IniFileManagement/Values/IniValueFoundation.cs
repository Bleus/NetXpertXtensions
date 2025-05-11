using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using NetXpertExtensions;
using static NetExpertExtensions.Classes.Console.BoxStyle;

namespace IniFileManagement.Values
{
	public abstract partial class IniLineValueFoundation
	{
		#region Properties
		/// <summary>Specifies what the wrapping character set will be used when parsing/writing this value.</summary>
		protected QuoteTypes _quoteType = QuoteTypes.None;

		/// <summary>If the defined <seealso cref="QuoteTypes"/> value is <seealso cref="QuoteTypes.Custom"/>, this holds
		/// the values of the custom wrapping characters to use.</summary>
		protected (char lead, char tail) _customQuotes = Wrapper( QuoteTypes.None );

		/// <summary>Holds the actual data associated with this value, as a string.</summary>
		/// <remarks>This is <i>always</i> stored <b><i><u>unencrypted</u></i></b>!</remarks>
		private string _value = "";

		private static readonly Type[] _supportedTypes = GetSupportedDataTypes(IniFileMgmt.Empty);

		public enum QuoteTypes { None, DoubleQuote, SingleQuote, BackTick, SquareBrackets, BraceBrackets, RoundBrackets, AngleBrackets, DoubleAngleBrackets, Custom, AutoSense }

		public enum DataTypes { Unknown, String, Boolean, Number, Date, Base64Encoded, Encrypted }
		#endregion

		#region Constructors
		protected IniLineValueFoundation( IniFileMgmt root )
		{
			ArgumentNullException.ThrowIfNull( root, nameof( root ) );
			this.Root = root;
			this.QuoteType = QuoteTypes.None;
			this._customQuotes = Wrapper( QuoteTypes.None );
			this._value = string.Empty;
		}

		protected IniLineValueFoundation( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, params char[]? quoteChars )
		{
			ArgumentNullException.ThrowIfNull( root, nameof( root ) );
			this.Root = root;
			this.QuoteType = quoteType;
			if ( (quoteChars is not null) && (quoteChars.Length > 0) )
				this.CustomQuotes = quoteChars;
		}

		protected IniLineValueFoundation( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.AutoSense, params char[]? customQuotes )
		{
			ArgumentNullException.ThrowIfNull( root, nameof( root ) );
			this.Root = root;

			if (quoteType == QuoteTypes.AutoSense)
				quoteType = DetectQuoteType( value, customQuotes );

			this.QuoteType = quoteType;
			this._value = UnwrapString( value, quoteType, customQuotes );
		}
		#endregion

		#region Operators
		public static implicit operator IniLineValueFoundation?( (IniFileMgmt parent, string value) source  ) => CreateDerivedValueClass( source.parent, source.value );
		public static implicit operator string( IniLineValueFoundation? source ) => source is null ? string.Empty : source.RawValue;
		#endregion

		#region Accessors
		/// <summary>Facilitates getting or setting the value of this object.</summary>
		/// <remarks><b>ALWAYS</b> uses <i><u>UN</u>encrypted</i> data! Encryption when relevant is managed by the <seealso cref="ToString"/> method!</remarks>
		protected string RawValue 
		{ 
			get => this._value;
			set
			{
				if (string.IsNullOrEmpty( value ))
					this._value = string.Empty;
				else
				{
					switch ( this._quoteType )
					{
						case QuoteTypes.None: break;
						case QuoteTypes.AutoSense:
							QuoteTypes qt = DetectQuoteType( value, this.CustomQuotes );
							if (qt != QuoteTypes.None)
								this._quoteType = qt;
							goto default;
						default:
							value = UnwrapString( value, this._quoteType, this._customQuotes );
							break;
					}
					this._value = value;
				}
			}
		}

		public QuoteTypes QuoteType
		{
			get => this._quoteType;
			set
			{
				if (value != QuoteTypes.AutoSense )
					this._quoteType = value;
			}
		}

		public virtual int Length => this.ToString().Length;

		public char[] CustomQuotes
		{
			get => [ this._customQuotes.lead, this._customQuotes.tail ];
			protected set
			{
				if (value is not null && value.Length > 0)
				{
					this._quoteType = QuoteTypes.Custom;
					this._customQuotes = ( value[ 0 ], value[ Math.Min( value.Length - 1, 1 ) ] );
				}
			}
		}

		public static Type[] SupportedDataTypes => IniLineValueFoundation._supportedTypes;

		protected IniFileMgmt Root { get; private set; }

		protected (char,char) WrapChars => this.QuoteType == QuoteTypes.Custom ? this._customQuotes : Wrapper( this._quoteType );

		protected Type DetectedType => DetectType( this._value );

		protected virtual Type DataType => typeof( string );

		protected abstract dynamic DefaultValue { get; }
		#endregion

		#region Methods
		public override string ToString() => ToString(false);

		public string ToString( bool encrypt ) => WrapValue( encrypt && (Root.EncryptionKey is not null) ? Root.EncryptionKey.Encrypt(this._value) : this._value, this.QuoteType, this.CustomQuotes );

		protected abstract bool Validate( string value );

		/// <summary>Compiles a list of all <see cref="IniLineValueFoundation"/> derived classes from within this library.</summary>
		protected static Type[] GetAllDerivedClasses()
		{
			//Type[] classes = Assembly.GetAssembly( typeof( IniLineValueFoundation ) ).GetTypes();
			Type[] classes = Assembly.GetCallingAssembly().GetTypes();
			List<Type> result = [];
			foreach (Type type in classes)
				if (!type.IsAbstract && type.IsSealed && type.HasAncestor<IniLineValueFoundation>())
					result.Add( type );

			return [ .. result ];
		}

		/// <summary>Searches for all defined <see cref="IniLineValueFoundation"/> derived classes, and catalogs the supported data types.</summary>
		public static Type[] GetSupportedDataTypes(IniFileMgmt? root = null)
		{
			// "ValueTuple`2" is the type that's reported by the Size/SizeF and Point/PointF classes,
			// so we have to add those manually in order to support them. Also, IniLineEnumValue is a 
			// Generic class, and will be skipped by the check, so it's here too...
			List<Type> types = [ typeof(Enum), typeof(Size), typeof(SizeF), typeof(Point), typeof(PointF) ], 
					   derivedClassTypes = [ ..GetAllDerivedClasses() ];
			root ??= IniFileMgmt.Empty;

			foreach (Type type in derivedClassTypes)
				if (!type.IsAbstract && !type.IsGenericType )
				{
					IniLineValueFoundation? obj = (IniLineValueFoundation)Activator.CreateInstance( type, root );
					if ( obj is not null && !types.Contains( obj.DataType )) types.Add( obj.DataType );
				}
			return [ .. types ];
		}

		/// <summary>Seaches all defined <see cref="IniLineValueFoundation"/> derived classes for one that supports the supplied <seealso cref="Type"/>.</summary>
		/// <returns>If a class is found to support the supplied data type, the <see cref="Type"/> of that class, otherwise throws an InvalidTypeException.</returns>
		/// <exception cref="TypeInitializationException"></exception>
		public static Type FindDerivedValueClassType( Type dataType, IniFileMgmt root )
		{
			// The data detection routine identifies Base-64 strings as binary (byte[]) data, but
			// we just want to handle them as strings...
			if (dataType == typeof( string ) || dataType == typeof( byte[] ) ) return typeof( IniLineValue );
			if (dataType.IsEnum) return typeof( IniLineEnumValue<> ).MakeGenericType( dataType );

			Type[] types = GetAllDerivedClasses();
			int i = 0;
			if (types.Length > 0)
			{ 
				IniLineValueFoundation? instance = null;
				do instance = types[ i ].IsGenericType ? null : (IniLineValueFoundation)Activator.CreateInstance( types[ i ], root );
				while ( (instance is null || instance.DataType != dataType) && (++i < types.Length) );

				if (i < types.Length) return types[ i ];
			}
			throw InvalidTypeException( dataType );
		}

		/// <summary>Facilitates safely searching for an <see cref="IniLineValueFoundation"/> derived class to support the provided <seealso cref="Type"/>.</summary>
		/// <returns>
		/// <b>True</b> if such a class exists (in which case, <paramref name="instance"/> will be populated with the requisite type),
		/// otherwise <b>False</b> (and <paramref name="instance"/> will be <i>null</i>.)
		/// </returns>
		public static bool TryFindDerivedValueClassType( Type dataType, IniFileMgmt root, out Type? instance )
		{
			try { instance = FindDerivedValueClassType( dataType, root ); } 
			catch ( TypeInitializationException ) { instance = null; }
			return instance is not null;
		}

		public static IniLineValueFoundation? CreateDerivedValueClass( IniFileMgmt root, string data, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
		{
			ArgumentNullException.ThrowIfNull( root, nameof( root ) );
			return CreateDerivedValueClass( root, DetectType( data, true ), data, quoteType, customQuotes );
		}

		public static IniLineValueFoundation? CreateDerivedValueClass ( IniFileMgmt root, Type fromDataType, object data, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
		{
			ArgumentNullException.ThrowIfNull( root, nameof( root ) );
			IniLineValueFoundation? instance = null;
			
			if ( TryFindDerivedValueClassType( fromDataType, root, out Type? t ) )
				instance = t is null ? null : (IniLineValueFoundation)Activator.CreateInstance( t, root, data, quoteType, customQuotes );
			else
				InvalidTypeException( fromDataType );

			return (instance is not null && instance.DataType == fromDataType) ? instance : null;
		}

		public static IniLineValueFoundation? CreateDerivedValueClass<T>( IniFileMgmt root, T data, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
		{
			ArgumentNullException.ThrowIfNull( root, nameof( root ) );
			return CreateDerivedValueClass( root, typeof( T ), (data is null ? default( T ) : data), quoteType, customQuotes );
		}

		/// <returns>"The stored value (\x22{value}\x22) cannot be parsed as a valid {t.FullName} object."</returns>
		protected FormatException CantParseException( string message = "" ) =>
			string.IsNullOrWhiteSpace(message) ? CantParseValueException( this.DataType, this.RawValue ) : CantParseValueException( message );

		//protected TypeInitializationException InvalidTypeException( string message = "" ) =>
		//	new ( DataType.FullName, new InvalidCastException( string.IsNullOrWhiteSpace(message)?$"The specified Type ({DataType.FullName}) is not supported." : message) );

		/// <returns>"The stored value (\x22{value}\x22) cannot be parsed as a valid {t.FullName} object."</returns>
		public static FormatException CantParseValueException( Type t, string value ) =>
			CantParseValueException( $"The stored value (\x22{value}\x22) cannot be parsed as a valid {t.FullName} object." );

		/// <returns>"The stored value (\x22{value}\x22) cannot be parsed as a valid {t.FullName} object."</returns>
		public static FormatException CantParseValueException( string message ) => new( message );

		/// <returns>"The specified Type ({t.FullName}) is not supported."</returns>
		public static TypeInitializationException InvalidTypeException( Type t, string message = "") =>
			new( t.FullName, new InvalidCastException( string.IsNullOrWhiteSpace( message ) ? $"The specified Type ({t.FullName}) is not supported." : message ) );

		/// <returns>"The specified Type ({t.FullName}) is not supported."</returns>
		protected TypeInitializationException InvalidTypeException() => InvalidTypeException( this.DataType );
		/// <summary>Used to validate a string for use by the desired object.</summary>

		protected abstract Regex ValidateSource();

		/// <summary>Given an object and a <seealso cref="QuoteTypes"/> value, encloses the string equivalent in the appropriate wrapping characters.</summary>
		/// <param name="rawSource">An object whose <i>ToString()</i> representation is to be wrapped.</param>
		/// <param name="quoteType">A <seealso cref="QuoteTypes"/> value indicating how to wrap the string.</param>
		/// <param name="customQuotes">If the wrapper type is <seealso cref="QuoteTypes.Custom"/>, the custom character(s) are specified here.</param>
		/// <returns>The original value as a string with the appropriate wrapping characters added to the beginning and end.</returns>
		/// <remarks>If the value already contains the correct wrapping characters, nothing more is done.</remarks>
		public static string WrapValue( object? rawSource, QuoteTypes quoteType = QuoteTypes.DoubleQuote, params char[]? customQuotes )
		{
			string source = rawSource is null ? string.Empty : rawSource.ToString();

			// If the supplied string already has the correct wrappers, there's nothing to do!
			if ( IsQuoteType( source, quoteType, customQuotes ) ) return source;

			if ( quoteType != QuoteTypes.None )
			{
				var (c1,c2) = Wrapper( quoteType, customQuotes ); char[] quotes = [ c1, c2 ];
				source = (quotes[ 0 ]=='\x00' ? "" : $"{quotes[0]}") + source + (quotes[ 1 ] == '\x00' ? "" : $"{quotes[ 1 ]}");
			}
			return source;
		}

		/// <summary>Checks if the supplied string is wrapped in the specified wrapping characters and removes them if so.</summary>
		/// <param name="source">The string to work on.</param>
		/// <param name="quoteType">The <seealso cref="QuoteTypes"/> specifier to look-for / remove.</param>
		/// <param name="customQuotes">If the specified <i>quoteType</i> is <b><seealso cref="QuoteTypes.Custom"/></b>, this specifies the wrap characters to use.</param>
		/// <returns>If the passed string has the specified wrapping characters, they're removed with the remainder returned, otherwise the original string is returned.</returns>
		protected static string UnwrapString( string source, QuoteTypes quoteType = QuoteTypes.DoubleQuote, (char lead, char tail)? customQuotes = null ) =>
			UnwrapString( source, quoteType, customQuotes is null ? null : [ customQuotes.Value.lead, customQuotes.Value.tail ] );

		/// <summary>Checks if the supplied string is wrapped in the specified wrapping characters and removes them if so.</summary>
		/// <param name="source">The string to work on.</param>
		/// <param name="quoteType">The <seealso cref="QuoteTypes"/> specifier to look-for / remove.</param>
		/// <param name="customQuotes">If the specified <i>quoteType</i> is <b><seealso cref="QuoteTypes.Custom"/></b>, this specifies the wrap characters to use.</param>
		/// <returns>If the passed string has the specified wrapping characters, they're removed with the remainder returned, otherwise the original string is returned.</returns>
		protected static string UnwrapString( string source, QuoteTypes quoteType = QuoteTypes.DoubleQuote, params char[]? customQuotes )
		{
			if ( string.IsNullOrWhiteSpace( source ) ) return string.Empty;
			var (c1, c2) = Wrapper( quoteType, customQuotes );
			return IsQuoteType( source, quoteType, customQuotes ) ? source.TrimStart( c1 ).TrimEnd( c2 ) : source;
		}

		/// <summary>Tests a given string to see if it's wrapped in the characters appropriate to the specified <paramref name="quoteType"/>.</summary>
		/// <param name="source">The string to check.</param>
		/// <param name="quoteType">Specifies the wrapping set to check for.</param>
		/// <param name="customQuotes">If the specified <i>quoteType</i> is <b><seealso cref="QuoteTypes.Custom"/></b>, this specifies the wrap characters to use.</param>
		/// <returns><b>TRUE</b> if the provided string is wrapped in the appropriate characters for the specified type.</returns>
		public static bool IsQuoteType( string source, QuoteTypes quoteType, params char[]? customQuotes )
		{

			if (quoteType == QuoteTypes.Custom && (customQuotes is null || customQuotes.Length == 0))
				return false; // No custom quotes were provided...

			if (string.IsNullOrEmpty( source )) return quoteType == QuoteTypes.None;
			source = source.Trim();

			string[] c;
			var (c1, c2) = Wrapper( quoteType, customQuotes );
			c = [ $"\\x{Convert.ToByte(c1):x2}", $"\\x{Convert.ToByte( c2 ):x2}" ]; // .. Wrapper( quoteType, customQuotes ).Select( ch => $"\\x{Convert.ToByte( ch ):x2}" ) ];
			if (c[ 0 ] == "\\x00" && c[ 1 ] == "\\x00") return false; // Some weird value was passed as the quoteType to check...

			bool matches = Regex.IsMatch( source, @$"^[{c[ 0 ]}][^{c[ 1 ]}]*[{c[ 1 ]}]$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant );

			return (quoteType == QuoteTypes.None && !matches) || matches; 
		}

		/// <summary>Given a specified <seealso cref="QuoteTypes"/> value, returns a character array containing the appropriate wrapping character(s),</summary>
		/// <remarks>If the specified <i>quoteType</i> value is unrecognized, or is <b><seealso cref="QuoteTypes.Custom"/></b>, the returned array contains <i>NULL</i> characters.</remarks>
		private static (char c1, char c2) Wrapper( QuoteTypes quoteType, params char[]? customQuotes ) =>
			quoteType switch
			{
				QuoteTypes.DoubleQuote => ('\x22', '\x22'),    // "
				QuoteTypes.SingleQuote => ('\x27', '\x27'),    // '
				QuoteTypes.BackTick => ('\x2c', '\x2c'),       // `
				QuoteTypes.SquareBrackets => ('[', '['),
				QuoteTypes.BraceBrackets => ('{', '{'),
				QuoteTypes.RoundBrackets => ('(', '('),
				QuoteTypes.AngleBrackets => ('<', '<'),
				QuoteTypes.DoubleAngleBrackets => ('«', '«'),
				QuoteTypes.Custom => 
					(customQuotes is null || customQuotes.Length == 0) ? 
						('\x00', '\x00') 
					: 
						( customQuotes[0], customQuotes[ Math.Min( customQuotes.Length - 1, 1 ) ] ), 
				_ => ('\x00', '\x00'),                        // null
			};

		/// <summary>Given wrapping character(s), attempts to identify the correc <seealso cref="QuoteTypes"/> value.</summary>
		private static QuoteTypes GetQuoteTypeFromChar( params char[] chars ) =>
			chars is null ? QuoteTypes.None :
			chars.Length switch
			{
				0 => QuoteTypes.None,
				1 => chars[ 0 ] switch
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
				},
				_ => $"{chars[ 0 ]}{chars[ 1 ]}" switch
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
				},
			};

		/// <summary>Given a <seealso cref="Regex"/> pattern string, inserts patterns for matching any valid <seealso cref="QuoteTypes"/> wrappers.</summary>
		/// <param name="source">The <seealso cref="Regex"/> pattern to modify.</param>
		/// <param name="customChars">If the pattern needs to accomodate custom wrapping characters, they are specified here.</param>
		/// <returns>A new <seealso cref="Regex"/> pattern with the added quote type validation patterns.</returns>
		/// <remarks>If the supplied pattern contains 'begin line' or 'end line' markers at the beginning or end, ("^" or "$"), they are
		/// retained in those places.</remarks>
		private static Regex AddRegexQuotes( string source, RegexOptions options, params char[]? customChars )
		{
			string front = "", back = "";
			byte cc1 = Convert.ToByte( customChars is null || customChars.Length == 0 ? '\0' : customChars[ 0 ] ),
				 cc2 = Convert.ToByte( customChars is null || customChars.Length == 0 ? '\0' : customChars[ Math.Min( customChars.Length - 1, 1 ) ] );

			foreach (var e in Enum.GetValues<QuoteTypes>())
			{
				string[] c;
				switch (e)
				{
					case QuoteTypes.None:
					case QuoteTypes.AutoSense: c = [ "\\x00", "\\x00" ]; break;
					case QuoteTypes.Custom:
						c = [ cc1 > 0 ? $"\\x{cc1:x2}" : "", cc2 > 0 ? $"\\x{cc2:x2}" : "" ];
						break;
					default:
						var (c1, c2) = Wrapper( e, customChars );
						c = [ $"\\x{Convert.ToByte( c1 ):x2}", $"\\x{Convert.ToByte( c2 ):x2}" ];
						break;
				};

				if (c[ 0 ] != "\\x00") front += c[ 0 ];
				if (c[ 1 ] != "\\x00") back += c[ 1 ];
			}

			string pattern = $"[{front}][{back}]";
			if (!string.IsNullOrEmpty( source ))
			{
				var m = GenerateQuotesCheck().Match( source );
				pattern = m.Success ?
						(m.Groups[ "pre" ].Success ? "^" : "") + $"[{front}]" +
						(m.Groups[ "content" ].Success ? m.Groups[ "content" ].Value : "") + $"[{back}]" +
						(m.Groups[ "post" ].Success ? "$" : "")
					: source;
			}
			return new( pattern, options );
		}

		[GeneratedRegex( @"^(?<pre>\^)?(?<content>[\s\S]+)(?<post>\$)?$", RegexOptions.ExplicitCapture )]
		protected static partial Regex GenerateQuotesCheck();

		/// <summary>Parses a supplied string against all known <i>QuoteTypes</i> and endeavors to detect if one applies.</summary>
		/// <param name="source">The string to parse.</param>
		/// <param name="customChars">If custom wrapping characters may be detected, they are specified here.</param>
		/// <returns>If one can be matched, the applicable <seealso cref="QuoteTypes"/> value, otherwise <b><seealso cref="QuoteTypes.None"/></b>.</returns>
		public static QuoteTypes DetectQuoteType( string source, params char[]? customChars )
		{
			if ( !string.IsNullOrWhiteSpace( source ) )
				foreach ( var e in Enum.GetValues<QuoteTypes>() )
					if ( (e != QuoteTypes.None) && (e != QuoteTypes.AutoSense) && IsQuoteType( source, e, customChars ) ) return e;

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
		public static string[] ParseLineContent( string source, params char[]? customQuotes )
		{
			if ( !string.IsNullOrWhiteSpace( source ) )
			{
				Match m;
				foreach ( var e in Enum.GetValues<QuoteTypes>() )
					if ( e != QuoteTypes.None )
					{
						var (c1,c2) = Wrapper( e, customQuotes );
						string[] wrapper = [ $"\\x{Convert.ToByte(c1):x2}", $"\\x{Convert.ToByte(c2):x2}" ];

						if ( wrapper.Length == 2 )
						{
							string pattern = @$"^(?<payload>[{wrapper[ 0 ]}](?<data>[^{wrapper[ 1 ]}]*)[{wrapper[ 1 ]}])(?<chaff>[\s]+(?<comment>[#/]{{2,}}[\s\S]*))?$";
							m = Regex.Match( source, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline );
							if ( m.Success )
								return [
									m.Groups[ "payload" ].Success ? m.Groups[ "payload" ].Value : "",
									m.Groups[ "chaff" ].Success ? m.Groups[ "chaff" ].Value : "",
									m.Groups[ "data" ].Success ? m.Groups[ "data" ].Value : "",
									m.Groups[ "comment" ].Success ? m.Groups[ "comment" ].Value : "",
									$"{e}", source
								];
						}
					}

				m = QuotelessLineParser_Rx().Match( source );
				if ( m.Success )
					return [
								m.Groups[ "data" ].Success ? m.Groups[ "data" ].Value : "",
								m.Groups[ "chaff" ].Success ? m.Groups[ "chaff" ].Value : "",
								m.Groups[ "data" ].Success ? m.Groups[ "data" ].Value : "",
								m.Groups[ "comment" ].Success ? m.Groups[ "comment" ].Value : "",
								$"{QuoteTypes.None}", source
					];
			}
			return [source, "", source, "", $"{QuoteTypes.None}", source];
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
		/// For decimalized values, it will recognize the following alphabetic suffixes: 'M' = <seealso cref="Decimal"/>, 'F' = <seealso cref="Double"/>,
		/// 'D' = <seealso cref="double"/> (if none is specified, <seealso cref="decimal"/> will be the default).<br/><br/>
		/// If no number type can be determined, the value will be checked to see if it can be parsed into either <seealso cref="bool"/>, or <seealso cref="DateTime"/>.
		/// Finally, it will check to see if the string is <seealso cref="System.Buffers.Text.Base64"/> encoded and report the return type as 
		/// a <i>byte[]</i> if so.<br/><br/>If no specific type is able to be identified, the default result will be a <seealso cref="string"/>.
		/// </remarks>
		public static Type DetectType( string data, bool goDeep = false )
		{
			if ( !string.IsNullOrWhiteSpace( data ) )
			{
				// Convert any hexadecimal value to an integer:
				if (Regex.IsMatch( data.Replace( " ", "" ), @"^(0[Xx])?[a-f\dA-F]+$", RegexOptions.Compiled ))
					data = BigInteger.Parse( Regex.Replace( data, @"(^0[xX]|[^a-f\dA-F])", "" ), System.Globalization.NumberStyles.HexNumber ).ToString();

				// Some kind of Integer:
				if ( Regex.IsMatch( data.Replace( " ", "" ), @"^[+-]?[\d,]+$", RegexOptions.Compiled ))
				{
					data = Regex.Replace( data, @"[^-\d]", "" ); // Data is a negative number...
					if ( (data.Length > 0) && (data[0]=='-') )
					{
						if (goDeep)
						{// The supplied Value has to be a negative number to hit his code!
							BigInteger v = BigInteger.Parse( data );
							if (v < Int128.MinValue) return typeof( BigInteger );
							if (v < long.MinValue) return typeof( Int128 );
							if (v < int.MinValue) return typeof( long );
							if (v < short.MinValue) return typeof( int );
							if (v < sbyte.MinValue) return typeof( short );
							return typeof( sbyte );
						}
					}
					else
					{
						BigInteger v = BigInteger.Parse( data );
						if ( goDeep )
						{
							// Not having a sign doesn't neccessarily require the value type to be unsigned,
							// so default to an adequate signed type first, unless the value exceeds the range for one...
							if (v > UInt128.MaxValue) return typeof( BigInteger );
							if (v > Int128.MaxValue) return typeof( UInt128 );
							if (v > ulong.MaxValue) return typeof( Int128 );
							if (v > long.MaxValue) return typeof( ulong );
							if (v > uint.MaxValue) return typeof( ulong );
							if (v > int.MaxValue) return typeof( uint );
							if (v > ushort.MaxValue) return typeof( int );
							if (v > short.MaxValue) return typeof( ushort );
							if (v > byte.MaxValue) return typeof( short );
							return typeof( byte );
						}
					}
					return typeof( int );
				}

				if ( Regex.IsMatch( data, @"^[-+]?[\d,]+(?:[.][\d]*)?[mMsSdDfF]?$", RegexOptions.Compiled ) )
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
					if ( Version.TryParse( data, out _ ) ) return typeof( Version );

					if (DateTime.TryParse( data, out _ )) return typeof( DateTime );

					if ( PointValidation_Rx().IsMatch( data ) ) return typeof( (int,int) );

					if ( PointFValidation_Rx().IsMatch( data ) ) return typeof( (float,float) );

					if ( Base64Validation_Rx().IsMatch( data ) ) return typeof( byte[] );

					if ( BooleanValueValidation_Rx().IsMatch( data ) ) return typeof( bool );
				}
			}
			return typeof( string );
		}

		/// <summary>Facilitates assigning a value of a specified type to this object.</summary>
		/// <typeparam name="T">The datatype being assigned.</typeparam>
		/// <param name="obj">The data itself.</param>
		public void As<T>( T obj ) // where T : new()
		{
			if (!IniLineValueTranslator<T>.IsValidDataType() || typeof(T)!=this.DataType) throw InvalidTypeException();

			IniLineValueFoundation? v = CreateDerivedValueClass<T>( Root, obj, this.QuoteType, this.CustomQuotes );
			this.RawValue = v is null ? String.Empty : v.RawValue;
		}

		/// <summary>Facilitates accessing the value of the object as a variety of types.</summary>
		/// <typeparam name="T">
		/// Can be any of the following: <seealso cref="sbyte"/>, <seealso cref="byte"/>, <seealso cref="short"/>, <seealso cref="ushort"/>, 
		/// <seealso cref="int"/>, <seealso cref="uint"/>, <seealso cref="long"/>, <seealso cref="ulong"/>, 
		/// <seealso cref="Int128"/>, <seealso cref="UInt128"/>, <seealso cref="double"/>, 
		/// <seealso cref="Double"/>, <seealso cref="decimal"/>, <seealso cref="string"/>, <seealso cref="char"/>, <seealso cref="bool"/>, 
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
		/// <seealso cref="Int128"/>, <seealso cref="UInt128"/>, <seealso cref="double"/>, <seealso cref="Double"/>, 
		/// <seealso cref="decimal"/>, <seealso cref="string"/>, <seealso cref="char"/>, <seealso cref="bool"/>, <i>char[]</i>, 
		/// <i>byte[]</i>, <seealso cref="Size"/>, <seealso cref="Point"/>, <seealso cref="SizeF"/>, <seealso cref="PointF"/>.
		/// </remarks>
		public dynamic? As<T>() // where T : new()
		{
			T var; string value;
			if (typeof( T ) == typeof( string )) return (this as IniLineValue).Value;
			if (typeof( T ).IsEnum)
			{
				value = EnumCleaner_Rx().Replace( RawValue, "" );
				if (!string.IsNullOrWhiteSpace( RawValue ) && Enum.TryParse(typeof(T), value, out object? result ))
					return result is null ? default(T) : (T)result;

				throw CantParseException();
			}

			value = RawValue;
			var = (T)Activator.CreateInstance( typeof( T ) );

			if (var is not null)
				switch (var)
				{
					case sbyte:		return (this as IniLineSByteValue).Value;
					case short:		return (this as IniLineShortIntValue).Value;
					case int:		return (this as IniLineIntValue).Value;
					case long:		return (this as IniLineLongIntValue).Value;
					case Int128:	return (this as IniLineInt128Value).Value;
					case byte:		return (this as IniLineByteValue).Value;
					case ushort:	return (this as IniLineUShortIntValue).Value;
					case uint:		return (this as IniLineUIntValue).Value;
					case ulong:		return (this as IniLineULongIntValue).Value;
					case UInt128:	return (this as IniLineUInt128Value).Value;

					case BigInteger: return (this as IniLineBigIntValue).Value;

					case decimal:	return (this as IniLineDecimalValue).Value;
					case float:		return (this as IniLineFloatValue).Value;
					case double:	return (this as IniLineDoubleValue).Value;

					case bool:		return (this as IniLineBooleanValue).Value;

					case DateTime:	return (this as IniLineDateTimeValue).Value;

					case char:		return char.Parse( value );
					case char[]:	return value.ToCharArray();
					case byte[]:
						// If the underlying string is a valid Base-64 string, convert that to a byte-array
						// and return it, otherwise just convert the unencrypted version of the value to a byte-array.
						return
							string.IsNullOrEmpty( this.RawValue )
							? Array.Empty<byte>()
							: (Base64Validation_Rx().IsMatch( this._value ) ? Convert.FromBase64String( this._value ) : this.RawValue.ToByteArray());
					case Point:		return (this as IniLinePointValue).Value;
					case Size:		return (this as IniLineSizeValue).Value;
					case PointF:	return (this as IniLinePointFValue).Value;
					case SizeF:		return (this as IniLineSizeFValue).Value;
					case Version:	return (this as IniLineVersionValue).Value;
				}

			throw new InvalidCastException( $"Exposing the value of this object as type \x22{typeof(T).FullName}\x22 is not supported/recognized." );
		}

		public T ToEnum<T>() where T : struct, IConvertible => ((IniLineEnumValue<T>)this).Value;

		[GeneratedRegex( @"^(?<data>[\s\S]*)?(?<chaff>[\s]+(?<comment>(##|//)[\s\S]*)?)?$" )]
		protected static partial Regex QuotelessLineParser_Rx();

		[GeneratedRegex( "^([y1tn0f]|On|Off|True|False|Yes|No)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant )]
		protected static partial Regex BooleanValueValidation_Rx();

		[GeneratedRegex( "^([y1t]|On|True|Yes)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant )]
		protected static partial Regex BooleanValueParser_Rx();

		[GeneratedRegex( "^(?:[a-z\\d+/]{4})*(?:[a-z\\d+/]{2}==|[a-z\\d+/]{3}=)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant )]
		protected static partial Regex Base64Validation_Rx();

		[GeneratedRegex( @"^[(\[{][\s]*(?<x>[\d]+[.](?:[\d]*)?)[\s]*[,;][\s]*(?<y>[\d]+[.](?:[\d]*)?)[\s]*[}\])]$", RegexOptions.None )]
		protected static partial Regex PointFValidation_Rx();

		[GeneratedRegex( @"^[(\[{][\s]*(?<x>[\d]+)[\s]*[,;][\s]*(?<y>[\d]+)[\s]*[}\])]$", RegexOptions.None )]
		protected static partial Regex PointValidation_Rx();

		[GeneratedRegex( @"[^\d.-]", RegexOptions.Compiled )] protected static partial Regex DecimalCleaner_Rx();

		[GeneratedRegex( @"[^\d-]", RegexOptions.Compiled )] protected static partial Regex IntCleaner_Rx();

		[GeneratedRegex( @"[^a-zA-Z\d]" )] private static partial Regex EnumCleaner_Rx();

		[GeneratedRegex( @"^\d+(\.[\d]{1,9){2,3}$" )] private static partial Regex VersionValidation_Rx();
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
						typeof( T ) != typeof( Double ) &&
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

			public IniLineXfYfValue( Double x, Double y, QuoteTypes quoteType = QuoteTypes.None ) : base( ParseValue( x, y ), false, quoteType ) { }

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
			public Double X
			{
				get => ParseValue( this._value ).X;
				set => this._value = ParseValue( value, Y );
			}

			public Double Y
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
					Double x = Double.Parse( m.Groups[ "x" ].Value ), y = Double.Parse( m.Groups[ "y" ].Value );
					return new PointF( x, y );
				}
				throw new ArgumentException( $"The supplied string could not be parsed. (\x22{value}\x22)" );
			}

			protected static string ParseValue( Double x, Double y ) => WrapValue( $"{x}, {y}", QuoteTypes.RoundBrackets );
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
