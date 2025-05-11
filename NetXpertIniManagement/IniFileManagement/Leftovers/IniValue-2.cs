using System.Numerics;

namespace IniFileManagement
{
	public abstract partial class IniLineValueFoundation<T>
	{
		#region Proeprties
		protected T? _rawValue;
		#endregion

		#region Constructors
		protected IniLineValueFoundation() => this._rawValue = default;

		protected IniLineValueFoundation( T? rawValue ) => this._rawValue = rawValue;

		protected IniLineValueFoundation( string rawValue ) =>
			this._rawValue = string.IsNullOrEmpty(rawValue) ? default : (T)Convert.ChangeType( rawValue, typeof( T ) );
		#endregion

		#region Accessors
		public abstract T Value { get; set; }


		#endregion
	}

	public class IniLineIntValue<T> : IniLineValueFoundation where T : INumber<T>
	{

	}

	public class IniLineStringValue : IniLineValueFoundation
	{

	}

	public class IniDateTimeValue : IniLineValueFoundation
	{

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
