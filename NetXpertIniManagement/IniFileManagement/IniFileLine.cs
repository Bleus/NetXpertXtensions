using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using IniFileManagement.Values;
using NetXpertExtensions;
using Windows.UI.Text;
//using static IniFileManagement.Values.IniLineValueFoundation;

namespace IniFileManagement
{
	public abstract partial class IniLineBase
	{
		#region Properties
		private string _key = string.Empty;         // the important parts. Use "BasicLineParser()" GeneratedRegex to disassemble...
		protected string _tail = string.Empty;      // meaningless stuff at the end of the line (comments usually)
		protected int _index = -1;                  // where it fits into the whole.
		protected string _separator = " = ";        // how the value and key are distinguished/married.
		protected readonly IniFileMgmt _root;
		protected IniLineValueFoundation _value;
		protected IniComment? _comment;

		// Private to force even child objects to use the accessors instead.
		private bool _encrypt = false;              // Private to force even child objects to use the accessors instead.

		private const char ENCR_MARKER = '\xbf';    // 0xbf = ¿ / 0xa7 = §

		/* language=regex */
		public const string GENERIC_LINE_PATTERN = @"^(?:[\s]*|[\s]*[a-z][\w]*[\xbf]?[\s]*[=:][\s]*[^\r\n]*|[\s]*[#/]{{2,}}[^\r\n]*)$";
		#endregion

		// Abstract class constructors may as well be 'protected': they're not available to non-child classes anyway!
		#region Constructor
		protected IniLineBase( IniFileMgmt root )
		{
			ArgumentNullException.ThrowIfNull( root, nameof( root ) );
			this._root = root;
			this._value = new IniLineValue( root, string.Empty );
		}

		protected IniLineBase( IniFileMgmt root, KeyValuePair<string, string> key, IniComment? comment = null, IniLineValueFoundation.QuoteTypes quoteType = IniLineValueFoundation.QuoteTypes.AutoSense, params char[]? customQuotes )
		{
			ArgumentNullException.ThrowIfNull( root, nameof( root ) );
			this._root = root;

			this.Key = key.Key;
			this.Value = IniLineValueFoundation.CreateDerivedValueClass( root, key.Value, quoteType, customQuotes );

			this._comment = comment is null ? "" : comment;
		}

		protected IniLineBase( IniFileMgmt root, string key, string value, IniComment? comment = null, IniLineValueFoundation.QuoteTypes quoteType = IniLineValueFoundation.QuoteTypes.AutoSense, params char[]? customQuotes )
		{
			ArgumentNullException.ThrowIfNull( root, nameof( root ) );
			this._root = root;

			this.Key = key;
			this.Value = IniLineValueFoundation.CreateDerivedValueClass( root, value, quoteType, customQuotes );
			this._comment = comment is null ? "" : comment;
		}

		protected IniLineBase( IniFileMgmt root, IniComment? comment )
		{
			ArgumentNullException.ThrowIfNull( root, nameof( root ) );
			this._root = root;
			this._comment = comment;
			this._value = (root, "");
		}
		#endregion

		#region Accessors
		/// <summary>Used to facilitate coherent internal and child interactions with the line's <see cref="_key"/> value.</summary>
		/// <remarks>The <see cref="Key"/> holds the name of the configuration item with which the <see cref="Value"/> is associated.</remarks>
		public string Key
		{
			get => this._key;
			set
			{
				if (string.IsNullOrWhiteSpace( value ) || !KeyValidator().IsMatch( value ))
					throw new ArgumentException( $"The value \x22{value}\x22 isn't a valid key." );

				this._key = value;
			}
		}

		/// <summary>Facilitates interactions with the configuration-line-data object.</summary>
		public virtual IniLineValueFoundation? Value
		{
			// Utilises IniLineValueFoundation implicit operator: ( IniFileMgmt, String ) <=> IniLineValueFoundation
			get => (Root, this._value is null ? "" : this._value );
			set => this._value = (Root, value is null ? string.Empty : value);
		}

		// NOTE: Use '.Value.As<T>' instead!
		//public T As<T>() => this._value is null ? default(T) : this._value.As<T>();

		/// <summary>Sets/Gets whether or not the key should be encrypted when it is written.</summary>
		/// <remarks>Even if this value is <b>true</b>, it will still report false and remain inactive unless/until <see cref="EncryptionKey"/> has been set and is valid.</remarks>
		public bool Encrypted
		{
			get => this._encrypt && HasValidKey;
			set => this._encrypt = value && HasValidKey;
		}

		/// <summary>Quick means to determine if the object's encryption key is set and valid...</summary>
		protected bool HasValidKey => (this.EncryptionKey is not null);

		public IniEncryptionKey? EncryptionKey => Root.EncryptionKey;

		public IniFileMgmt Root => this._root;

		/// <summary>Used to identify what datatype the derived class manages.</summary>
		protected abstract Type DataType { get; }
		#endregion

		#region Operators
		public static implicit operator string( IniLineBase? line ) => line is null ? string.Empty : line.Compile();
		public static implicit operator IniLineBase?( (IniFileMgmt root, string data) line ) => ParseLine( line.root, line.data );
		#endregion

		#region Methods
		public override string ToString()
		{
			StringBuilder value = new();

			if (!string.IsNullOrEmpty( this._key ))
			{
				value.Append( $"\t{this._key}" );
				if (this.Encrypted) value.Append( ENCR_MARKER );
				value.Append( this._separator );

				if ( this.Value is not null )
					value.Append( this.Encrypted ? this.EncryptionKey.Encrypt( this.Value ) : this.Value.ToString() );
			}

			if ( this._comment is not null && this._comment.Length > 0 ) value.Append( this._comment.ToString() );

			return value.ToString();

			//string value = this.Value is null ? "" : (this.Encrypted ? EncryptViaAES256( Value, this.EncryptionKey ) : this._value);
			//return $"\t{this._key}{(this.Encrypted ? ENCR_MARKER : string.Empty)}{this._separator}{value}{this._tail}";
		}

		public virtual string Compile( string preamble = "\t", int valueStart = -1, int commentStart = -1 )
		{
			string result = ($"{preamble}{Key}" + (this.Encrypted ? ENCR_MARKER : string.Empty)).PadRight( Math.Max( Key.Length + 1, valueStart ), ' ' );
			result += $"{this._separator}{Value.ToString( this.Encrypted )}";

			if (this._comment is not null)
			{
				if ((commentStart > 0) && (result.Length > commentStart))
					result = $"{preamble}{this._comment}\r\n{preamble}{result}";
				else
					result = result.PadRight( Math.Max( result.Length + 1, commentStart ), ' ' ) + this._comment.ToString();
			}
			return $"{result}\r\n";
		}

		protected static bool IsValid( string source ) =>
			source is not null && IniValidateGenericLine().IsMatch( source );

		protected static Type[] GetAllDerivedClasses()
		{
			//Type[] classes = Assembly.GetAssembly( typeof( IniLineValueFoundation ) ).GetTypes();
			Type[] classes = Assembly.GetCallingAssembly().GetTypes();
			List<Type> result = [];
			foreach (Type type in classes)
				if (!type.IsAbstract && type.HasAncestor<IniLineBase>())
					result.Add( type );

			return [ .. result ];
		}

		public static Type GetLineClassType( Type dataType, IniFileMgmt root )
		{
			if (dataType == typeof( string )) return typeof( IniLine );
			//if (dataType.IsEnum) return typeof( IniLineEnumValue<> ).MakeGenericType( dataType );
			if ( IniLineValueFoundation.GetSupportedDataTypes(root).Contains( dataType ) ) return typeof(IniLine<>).MakeGenericType( dataType );

			throw IniLineValueFoundation.InvalidTypeException( dataType );
		}

		public static bool TryGetLineClassType( Type dataType, IniFileMgmt root, out Type? lineType )
		{
			try { lineType = GetLineClassType( dataType, root ); }
			catch( TypeInitializationException ) { lineType = null; }
			return lineType is not null;
		}

		public static IniLineBase? CreateDerivedValueClass( IniFileMgmt root, string key, string data, IniLineValueFoundation.QuoteTypes quoteType = IniLineValueFoundation.QuoteTypes.AutoSense, params char[] customQuotes ) =>
			CreateDerivedValueClass( root, IniLineValueFoundation.DetectType( data, true ), key, data, quoteType, customQuotes );

		public static IniLineBase? CreateDerivedValueClass( IniFileMgmt root, Type fromDataType, string key, string data, IniLineValueFoundation.QuoteTypes quoteType = IniLineValueFoundation.QuoteTypes.AutoSense, char[]? customQuotes = null )
		{
			IniLineBase? instance = null;
			if ( TryGetLineClassType( fromDataType, root, out Type? t ) )
				instance = (IniLineBase)Activator.CreateInstance( t, root, key, data, IniComment.Empty(), quoteType, customQuotes ); 

			return (instance is not null && instance.DataType == fromDataType) ? instance : null;
		}

		public static IniLineBase? CreateDerivedValueClass<T>( IniFileMgmt root, string key, T data, IniLineValueFoundation.QuoteTypes quoteType = IniLineValueFoundation.QuoteTypes.None, char[]? customQuotes = null ) =>
			CreateDerivedValueClass( root, typeof( T ), key, (data is null ? (default( T ) is null ? string.Empty : $"{default(T)}") : data.ToString()), quoteType, customQuotes );

		public static IniLineBase? ParseLine( IniFileMgmt root, string line, IniLineValueFoundation.QuoteTypes quoteType = IniLineValueFoundation.QuoteTypes.AutoSense, params char[]? customQuotes )
		{
			if (IniComment.WholeLineCommentParser().IsMatch( line ))
				return new IniLine( root, IniComment.ParseLine( line ) );

			if (IniValidateGenericLine().IsMatch( line ))
			{
				Match m = BasicLineParser().Match( line );
				if (m.Success)
				{
					string
						meat = m.Groups[ "value" ].Success ? m.Groups[ "value" ].Value : string.Empty,
						key = m.Groups[ "key" ].Success ? m.Groups[ "key" ].Value : string.Empty,
						sign = m.Groups[ "sign" ].Success ? m.Groups[ "sign" ].Value : string.Empty;

					bool encrypt = m.Groups[ "encr" ].Success && (m.Groups[ "encr" ].Value == $"{ENCR_MARKER}");
					if (encrypt && (root.EncryptionKey is null))
						throw new InvalidOperationException( "The given line indicates that it is encrypted, but no encryption key has been created.", new ArgumentException( line ) );

					string[] parsedMeat = IniLineValueFoundation.ParseLineContent( meat );

					var qt = quoteType == IniLineValueFoundation.QuoteTypes.AutoSense ? Enum.Parse<IniLineValueFoundation.QuoteTypes>( parsedMeat[ 4 ] ) : quoteType;

					Type lineType = IniLineValueFoundation.DetectType( encrypt ? root.EncryptionKey.Decrypt( parsedMeat[ 2 ] ) : parsedMeat[ 2 ], true );

					IniLineBase? result;
					try
					{
						result = CreateDerivedValueClass( root, lineType, key, parsedMeat[2], qt, customQuotes ); // = null;
						if (result is not null)
						{
							result._comment = string.IsNullOrWhiteSpace( parsedMeat[ 3 ] ) ? null : new IniComment( parsedMeat[ 3 ] );
							result._separator = sign.Trim();
							result._tail = parsedMeat[ 1 ];
							//result._value = IniLineValueFoundation.CreateDerivedValueClass( lineType, parsedMeat[2], qt, customQuotes );
						}

						//if (lineType == typeof( string ))
						//	result = new IniLine( key, "", "", encrKey );
						//else
						//	result = Activator.CreateInstance( (typeof( IniLine<> ).MakeGenericType( [ lineType ] )), [ key, encrKey ] );

						//						result = lineType == typeof( string ) ? new IniLine( key ) :
						//							Activator.CreateInstance( (typeof( IniLine<> ).MakeGenericType( new Type[] { lineType } )), new object[] { key, "", "", encrKey } );

						//if (result is not null)
						//{
						//	result._separator = sign.Trim();
						//	result._key = key;
						//	result._value = IniLineValueFoundation.CreateDerivedValueClass( lineType, parsedMeat[ 2 ] );
						//	result._tail = parsedMeat[ 1 ];
						//	result._comment = string.IsNullOrWhiteSpace( parsedMeat[ 3 ] ) ? null : new IniComment( parsedMeat[ 3 ] );
						//}
					}
					//catch (MissingMethodException ex)
					catch( FileNotFoundException ex )
					{
						result = new IniLine( root, key, "", new IniComment( "" ), quoteType, customQuotes )
						{
							_separator = sign.Trim(),
							_value = (root, IniLineValueFoundation.WrapValue( parsedMeat[ 2 ], qt, customQuotes )), // new IniLineValueFoundation( meat, qt ) );
							_tail = parsedMeat[ 1 ],
							_comment = new IniComment( IniComment.Marker.DoubleHash, $"{lineType}: {ex.Message}" )
						};
						//throw ex;
					}
					return result;
				}
			}
			return null;
		}

		/// <summary>Attempts to decrypt a Base64-encoded string created by <seealso cref="EncryptViaAES256(string, byte[])"/>.</summary>
		/// <param name="encrKey">The 256-bit (32 byte) encryption key that was used to encrypt the string originally.</param>
		/// <returns>A string containing the result of decrypting the source string with the provided key.</returns>
		//private static string DecryptViaAES256( string source, byte[] encrKey )
		//{
		//	byte[] iv = new byte[ 16 ];
		//	byte[] buffer = Convert.FromBase64String( source );

		//	using Aes aes = Aes.Create();
		//	aes.Key = encrKey;
		//	aes.IV = iv;
		//	ICryptoTransform decryptor = aes.CreateDecryptor( aes.Key, aes.IV );

		//	using MemoryStream memoryStream = new( buffer );
		//	using CryptoStream cryptoStream = new( memoryStream, decryptor, CryptoStreamMode.Read );
		//	using StreamReader streamReader = new( cryptoStream );

		//	return streamReader.ReadToEnd();
		//}

		/// <summary>Can be used to easily generate a random encryption key.</summary>
		/// <returns>A 32-byte array of randomly generated values.</returns>
		public static byte[] GenerateEncryptionKey()
		{
			byte[] key = new byte[ 32 ];
			for (int i = 0; i < 32;)
			{
				byte[] bytes = int.MaxValue.Random().AsBytes();
				key[ i++ ] = bytes[ 0 ];
				key[ i++ ] = bytes[ 1 ];
				key[ i++ ] = bytes[ 2 ];
				key[ i++ ] = bytes[ 3 ];
			}

			return key;
		}

		[GeneratedRegex( GENERIC_LINE_PATTERN, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture )]
		public static partial Regex IniValidateGenericLine();

		[GeneratedRegex( @"^(##|//|^; )?[ //\t]+$", RegexOptions.None )]
		private static partial Regex PreambleValidation();

		[GeneratedRegex( @"^[\s]*(?<key>[a-z][\w]*)(?<encr>[\xbf]?)(?<sign>[\s]*[=:][\s]*)(?<value>.*)$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant )]
		public static partial Regex BasicLineParser();

		[GeneratedRegex( @"^[a-z][\w]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant )]
		private static partial Regex KeyValidator();
		#endregion
	}

	public class IniLine<T> : IniLineBase // where T : new()
	{
		#region Constructors
		public IniLine(
			IniFileMgmt root,
			string key, 
			IniLineValueFoundation.QuoteTypes quoteType = IniLineValueFoundation.QuoteTypes.AutoSense, 
			params char[]? customQuotes ) 
			: base( root, key, "", "", quoteType, customQuotes )
		{
			if (!IniLineValueTranslator<T>.IsValidDataType()) throw InvalidGenericType();

			//this._value = (IniLineValue)string.Empty;
		}

		public IniLine( 
			IniFileMgmt root,
			string key, 
			T data, 
			IniComment? comment = null, 
			IniLineValueFoundation.QuoteTypes quoteType = IniLineValueFoundation.QuoteTypes.AutoSense, 
			params char[]? customQuotes ) 
			: base(root, key, "", comment, quoteType, customQuotes )
		{
			if (!IniLineValueTranslator<T>.IsValidDataType()) throw InvalidGenericType();

			this._value = IniLineValueFoundation.CreateDerivedValueClass( root, typeof( T ), data );
		}

		public IniLine(
			IniFileMgmt root,
			string key, 
			string data, 
			IniComment? comment = null, 
			IniLineValueFoundation.QuoteTypes quoteType = IniLineValueFoundation.QuoteTypes.AutoSense, 
			params char[]? customQuotes 
			) : base( root, key, data, comment, quoteType, customQuotes )
		{
			if (!IniLineValueTranslator<T>.IsValidDataType()) throw InvalidGenericType();

			//this._value = IniLineValueFoundation.CreateDerivedValueClass( typeof( T ), data );
		}

		//protected IniLine( string key, object data, string comment, byte[]? encrKey = null ) : base( key, "", comment, encrKey )
		//{
		//	if (!IniLineValueTranslator<T>.IsValidDataType()) throw InvalidGenericType();


		//	this._value = data is null ? null : IniLineValueFoundation.CreateDerivedValueClass( typeof( T ), data );
		//}

		public IniLine( 
			IniFileMgmt root, 
			string key, 
			T data, 
			string comment, 
			IniLineValueFoundation.QuoteTypes quoteType = IniLineValueFoundation.QuoteTypes.AutoSense, 
			params char[]? customQuotes 
			) : base(root, key, "", comment, quoteType, customQuotes )
		{
			if (!IniLineValueTranslator<T>.IsValidDataType()) throw InvalidGenericType();

			this._value.As<T>( data );
		}
		#endregion

		#region Accessors
		public T? RawValue
		{
			get => base.Value?.As<T?>();
			set => base.Value?.As<T?>( value is null ? default( T ) : value );
		}

		protected override Type DataType => typeof(T);
		#endregion

		#region Operators
		public static implicit operator (string key, T? value)( IniLine<T?> line ) => (line.Key, line.RawValue);
		public static implicit operator KeyValuePair<string, T?>?( IniLine<T?> line ) => line is null ? null : new( line.Key, line.RawValue );
		//public static implicit operator IniLine<T?>?( KeyValuePair<string, T?> line ) => (IniLine<T>)CreateDerivedValueClass<T?>( line.Key, line.Value );
		//public static implicit operator IniLine<T?>( (string Key, T? Value) line ) => (IniLine<T>)CreateDerivedValueClass<T?>( line.Key, line.Value );
		#endregion

		#region Methods
		/// <returns><seealso cref="TypeInitializationException"/>: "The generic type {typeof(T).FullName} is not a valid data type."</returns>
		protected TypeInitializationException InvalidGenericType( string message = "" ) =>
			new( typeof( T ).FullName, new Exception( string.IsNullOrWhiteSpace(message)?$"The generic type {typeof( T ).FullName} is not a valid data type.":message ) );
		#endregion
	}

	public sealed class IniLine : IniLineBase
	{
		#region Constructors
		public IniLine(IniFileMgmt root ,string key, string data = "", IniComment? comment = null, IniLineValueFoundation.QuoteTypes quoteType = IniLineValueFoundation.QuoteTypes.AutoSense, params char[]? customQuotes )
			: base(root, key, data, comment, quoteType, customQuotes ) { }

		public IniLine( IniFileMgmt root, string key, string data = "", string comment = "", IniLineValueFoundation.QuoteTypes quoteType = IniLineValueFoundation.QuoteTypes.AutoSense, params char[]? customQuotes )
			: base( root, key, data, comment, quoteType, customQuotes ) { }

		public IniLine( IniFileMgmt root, IniComment comment ) : base(root) =>
			this._comment = comment;
		#endregion

		#region Accessors
		new public string Value
		{
			get => this._value is null ? "" : this._value;
			set => this._value = ( Root, string.IsNullOrEmpty( value ) ? "" : value );
		}

		protected override Type DataType => typeof(string);
		#endregion

		#region Operators
		public static implicit operator KeyValuePair<string, string>( IniLine line ) => new KeyValuePair<string, string>( line.Key, line.Value );
		//public static implicit operator IniLine( KeyValuePair<string, string> line ) => new( line.Key, line.Value, "" );
		#endregion
	}

	public partial class IniComment // : IniLineBase
	{
		#region Properties
		public enum Marker { Unknown, DoubleHash, DoubleSlash, Semicolon };

		protected Marker _marker = Marker.Unknown;

		protected string _value = string.Empty;

		/* language=regex */
		public const string COMMENT_PATTERN = @"(?<preamble>[ \t]*)(?<marker>[#\\]{2,})(?<comment>[^\r\n]*)$";
		#endregion

		#region Constructors
		public IniComment( string comment ) : base()
		{
			this.Comment = comment;
			this._marker = Marker.DoubleHash;
		}

		public IniComment( Marker marker, string comment )
		{
			this.Comment = comment;
			this._marker = marker == Marker.Unknown ? Marker.DoubleHash : marker;
		}

		private IniComment() { }
		#endregion

		#region Accessors
		public string Comment
		{
			get => this._value;
			set
			{
				if (string.IsNullOrWhiteSpace( value ))
				{
					this._value = string.Empty;
					this._marker = Marker.Unknown;
					this.Index = -1;
				}
				else
				{
					Match m = BaseCommentParser().Match( value );
					if (m.Success && m.Groups[ "comment" ].Success)
						this._value = m.Groups[ "comment" ].Value;
				}
			}
		}

		public int Index { get; protected set; } = 0;

		public int Length => this.ToString().Length;
		#endregion

		#region Operators
		public static implicit operator IniComment?( string source ) => !string.IsNullOrWhiteSpace( source ) ? ParseLine( source ) : null;
		public static implicit operator string( IniComment comment ) => comment is null ? "" : comment.ToString();
		#endregion

		#region Methods
		/// <summary>Parses the passed string for content that matches the <see cref="COMMENT_PATTERN"/>.</summary>
		/// <param name="line">A string containing the text to parse.</param>
		/// <param name="trimmedComment">
		/// If a comment is found in <paramref name="line"/>, this will contain the original passed text, 
		/// <i>with the parsed comment removed</i>.
		/// </param>
		/// <returns>If a comment is found, a new <see cref="IniComment"/> object containing the comment's content, otherwise <i>null</i>.</returns>
		public static IniComment? ParseLine( string line, out string trimmedComment )
		{
			IniComment? result = null; trimmedComment = line;
			if (!string.IsNullOrWhiteSpace( line ))
			{
				//Match m = Regex.Match( line.Trim(), @"^(?<type>##|//|; )(?<content>[\s\S]+)$", RegexOptions.None );
				var m = BaseCommentParser().Match( line.Trim() ); // CommentValidator().Match( line.Trim() );
				if (m.Success)
				{
					result = new();
					if (m.Groups[ "marker" ].Success)
						result._marker = m.Groups[ "marker" ].Value switch
						{
							"//" => Marker.DoubleSlash,
							"; " => Marker.Semicolon,
							_ => Marker.DoubleHash
						};
					if (m.Groups[ "comment" ].Success)
						result._value = m.Groups[ "comment" ].Value;

					trimmedComment = BaseCommentParser().Replace( line, "" );
				}
			}
			return result;
		}

		/// <summary>Parses the passed string for content that matches the <see cref="COMMENT_PATTERN"/>.</summary>
		/// <param name="line">A string containing the text to parse.</param>
		/// <returns>If a comment is found, a new <see cref="IniComment"/> object containing the comment's content, otherwise <i>null</i>.</returns>
		public static IniComment? ParseLine( string line ) => ParseLine( line, out _ );

		protected static string WhitespaceFill( string original, int desiredLength, bool useTabs = true, bool includeOriginal = false, byte tabSize = 3 )
		{
			string result = includeOriginal ? original : string.Empty;
			int start = original.Length, length = desiredLength - start, tabCount = (int)Math.Floor( (decimal)length / (decimal)tabSize );
			if (useTabs) //return (includeOriginal ? original : "").PadRight(length);
			{
				result += "".PadRight( tabCount, '\t' );
				length = desiredLength % 3;
			}
			return result.PadRight( length );
		}

		public override string ToString() =>
			string.IsNullOrEmpty( this.Comment ) ? string.Empty : 
			WhitespaceFill( "", this.Index, true, false, 3 ) +
			this._marker switch { Marker.DoubleSlash => "//", Marker.Semicolon => "; ", _ => "##" } +
			$"{this._value}";

		public static bool IsComment( string value ) =>
			!string.IsNullOrWhiteSpace( value ) && WholeLineCommentParser().IsMatch( value );

		public static bool HasComment( string value ) =>
			!string.IsNullOrWhiteSpace( value ) && BaseCommentParser().IsMatch( value );

		public static IniComment Empty( Marker mark = Marker.DoubleHash) => new( mark, "" );

		[GeneratedRegex( @"^[\s]*(?<marker>##|//|; )(?<comment>[\s]+[\s\S]+)$", RegexOptions.Multiline )]
		public static partial Regex WholeLineCommentParser();

		[GeneratedRegex( COMMENT_PATTERN, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant )]
		private static partial Regex BaseCommentParser();
		#endregion
	}
}
