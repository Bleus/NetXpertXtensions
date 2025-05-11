using System.Numerics;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NetXpertExtensions;

namespace IniFileManagement.Values
{
	public abstract partial class IniLineValueTranslator<T> : IniLineValueFoundation //where T : new()
	{
		#region Constructors
		protected IniLineValueTranslator( IniFileMgmt root, T? value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, quoteType, customQuotes )
		{
			if (!IsValidDataType()) throw InvalidTypeException();
			this.Value = value is null ? DefaultValue : value;
		}

		protected IniLineValueTranslator( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes )
		{
			if (!IsValidDataType()) throw InvalidTypeException();
			this.Value = string.IsNullOrEmpty( value ) ? DefaultValue : Parse( value );
		}

		protected IniLineValueTranslator( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, quoteType, customQuotes )
		{
			if (!IsValidDataType()) throw InvalidTypeException();
			this.Value = DefaultValue;
		}

		protected IniLineValueTranslator( IniFileMgmt root ) : base(root)
		{
			if ( SupportedDataTypes is not null && !IsValidDataType()) 
				throw InvalidTypeException();
			this.Value = DefaultValue;
		}

		protected IniLineValueTranslator( IniFileMgmt root, params Type[] allowedTypes ) : base(root)
		{
			if (SupportedDataTypes is not null && !IsValidDataType(allowedTypes))
				throw InvalidTypeException();
			this.Value = DefaultValue;
		}
		#endregion

		#region Accessors
		public virtual T? Value
		{
			get => Parse( RawValue );
			set => RawValue = ValueAsString( value );
		}

		public override int Length { get => base.Length; }

		protected override Type DataType => typeof( T );

		protected override dynamic DefaultValue => default(T);
		#endregion

		#region Methods
		/// <summary>This method is responsible for properly converting the string value of the base to the type supported by the child class.</summary>
		/// <param name="value">A string value to parse.</param>
		/// <returns>If the parse succeeds, an object of the relevant type, populated from the <paramref name="value"/>.</returns>
		protected abstract T? Parse( string value );

		/// <summary>This method is responsible for converting the relevant type to a string for storage in the base value.</summary>
		/// <param name="value">An object of the source data type to convert to a string.</param>
		/// <remarks><b>NOTE</b>: This method's output string <i>should</i> be inherently compatible with the <seealso cref="Parse(string)"/> method!</remarks>
		/// <returns>A string representation of the source data.</returns>
		protected virtual string ValueAsString( T? value ) => value is null ? string.Empty : value.ToString();

		protected override bool Validate( string value ) =>
			!string.IsNullOrEmpty( value ) && ValidateSource().IsMatch( value );

		/// <summary>Reports on whether a speficied <seealso cref="Type"/> is supported.</summary>
		/// <param name="t">The C# <seealso cref="Type"/>To validate.</param>
		/// <returns><b>TRUE</b> if the provided type is supported by <seealso cref="IniLineValueFoundation"/></returns>
		public static bool IsValidDataType( params Type[] allowedTypes )
		{
			if (allowedTypes is not null && allowedTypes.Length > 0) return allowedTypes.Contains( typeof( T ) );

			if (typeof( T ).IsEnum || typeof( T ).IsGenericType || typeof( T ).IsDerivedFrom<IniLineValueFoundation>()) return false;

			return SupportedDataTypes.Contains( typeof( T ) );
			//{
			//	allowedTypes = SupportedDataTypes is null ? GetSupportedDataTypes() : SupportedDataTypes;
				//[
				//	typeof( int ),      typeof( sbyte ),    typeof( short ),		typeof( long ),     typeof( uint ),
				//	typeof( byte ),     typeof( ushort ),   typeof( ulong ),		typeof( decimal ),  typeof( float ),
				//	typeof( double ),   typeof( bool ),     typeof( DateTime ),		typeof( Point ),    typeof( Size ),
				//	typeof( PointF ),   typeof( SizeF ),	typeof( BigInteger ),	typeof( Version )
				//];
			//}
			//return allowedTypes.Contains( typeof( T ) );
		}
		#endregion
	}

	public abstract partial class IniLineSignedIntTranslator<T> : IniLineValueTranslator<T> where T : IBinaryInteger<T>
	{
		#region Constructors
		protected IniLineSignedIntTranslator( IniFileMgmt root ) : base( root )
		{
			if (SupportedDataTypes is not null && !IsValidDataType()) throw InvalidTypeException();
		}

		protected IniLineSignedIntTranslator( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
			: base( root, quoteType, quoteChars ) 
		{
			if (!IsValidDataType()) throw InvalidTypeException();
		}

		protected IniLineSignedIntTranslator( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes )
		{
			if (!IsValidDataType()) throw InvalidTypeException();
		}

		protected IniLineSignedIntTranslator( IniFileMgmt root, T? value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value is null ? T.Zero : value, quoteType, customQuotes )
		{
			if (!IsValidDataType()) throw InvalidTypeException();
		}
		#endregion

		#region Accessors
		protected override dynamic DefaultValue => T.Zero;
		#endregion

		#region Methods
		protected override T? Parse( string source ) => (T?)Convert.ChangeType( ValidateSource().IsMatch( source ) ? Int128.Parse( source ) : 0, typeof( T ) );

		protected override string ValueAsString( T? value ) => value is null ? string.Empty : value.ToString();

		protected override bool Validate( string value ) => !string.IsNullOrWhiteSpace( value ) && ValidateSource().IsMatch( value );

		protected override Regex ValidateSource() => IntegerValidator_Rx();

		public static bool IsValidDataType() =>
			IniLineValueTranslator<T>.IsValidDataType( typeof( int ), typeof( sbyte ), typeof( short ), typeof( long ), typeof( Int128 ), typeof( BigInteger ) );

		[GeneratedRegex( @"[+-]?[\d]+", RegexOptions.None )] public static partial Regex IntegerValidator_Rx();
		#endregion
	}

	public abstract partial class IniLineUnsignedIntTranslator<T> : IniLineValueTranslator<T> where T : IBinaryInteger<T>
	{
		#region Constructors
		protected IniLineUnsignedIntTranslator( IniFileMgmt root ) : base( root )
		{
			if (SupportedDataTypes is not null && !IsValidDataType()) throw InvalidTypeException();
		}

		protected IniLineUnsignedIntTranslator( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
			: base( root, quoteType, quoteChars )
		{
			if (!IsValidDataType()) throw InvalidTypeException();
		}

		protected IniLineUnsignedIntTranslator( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes )
		{
			if (!IsValidDataType()) throw InvalidTypeException();
		}

		protected IniLineUnsignedIntTranslator( IniFileMgmt root, T value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value is null ? T.Zero : value, quoteType, customQuotes )
		{
			if (!IsValidDataType()) throw InvalidTypeException();
		}
		#endregion

		#region Accessors
		protected override dynamic DefaultValue => T.Zero;
		#endregion

		#region Methods
		protected override T? Parse( string source ) => (T?)Convert.ChangeType( ValidateSource().IsMatch( source ) ? UInt128.Parse( source ) : 0, typeof( T ) );

		protected override string ValueAsString( T? value ) => value is null ? string.Empty : value.ToString();

		protected override bool Validate( string value ) => !string.IsNullOrWhiteSpace( value ) && ValidateSource().IsMatch( value );

		protected override Regex ValidateSource() => UIntegerValidator_Rx();

		new public static bool IsValidDataType() =>
			IniLineValueTranslator<T>.IsValidDataType( typeof( uint ), typeof( byte ), typeof( ushort ), typeof( ulong ), typeof( UInt128 ) );

		[GeneratedRegex( @"\+?[\d]+", RegexOptions.None )] public static partial Regex UIntegerValidator_Rx();
		#endregion
	}

	public abstract partial class IniLineFloatTranslator<T> : IniLineValueTranslator<T> where T : INumber<T>
	{
		#region Constructors
		protected IniLineFloatTranslator( IniFileMgmt root ) : base( root ) => GlobalInit();

		protected IniLineFloatTranslator( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
			: base( root, quoteType, quoteChars ) => GlobalInit();

		protected IniLineFloatTranslator( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) => GlobalInit();

		protected IniLineFloatTranslator( IniFileMgmt root, T value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) => GlobalInit();

		private void GlobalInit() { if (!IsValidDataType()) throw InvalidTypeException(); }
		#endregion

		#region Accessors
		protected override dynamic DefaultValue => T.Zero;
		#endregion

		#region Methods
		protected override T? Parse( string source ) => 
			ValidateSource().IsMatch(source) ? T.CreateChecked( MyParse( source )) : T.Zero;

		protected abstract T MyParse( string source );

		protected override string ValueAsString( T? value ) => value is null ? string.Empty : value.ToString();

		protected override Regex ValidateSource() => IniLineDecimalValue.DecimalValidator_Rx();

		public static bool IsValidDataType() =>
			IniLineValueTranslator<T>.IsValidDataType( typeof( float ), typeof( double ) );
		#endregion
	}

	public abstract partial class IniLineValueIntTupleTranslator<T> : IniLineValueTranslator<(int i1, int i2)>
	{
		/* language=Regex */
		protected const string PARSER = @"[({]?([a-zA-Z]+:?)?(?<i1>[+-]?[\d]+),([a-zA-Z]+:?)?(?<i2>[-+]?[\d]+)[)}]?";

		#region Constructors
		protected IniLineValueIntTupleTranslator( IniFileMgmt root ) 
			: base( root, typeof( Size ), typeof( Point ), typeof( (int, int) ) )
		{
			if (SupportedDataTypes is not null && !IsValidDataType()) throw InvalidTypeException();
			RawValue = ValueAsString( (0, 0) );
		}

		protected IniLineValueIntTupleTranslator( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
			: base( root, quoteType, quoteChars )
		{
			if (!IsValidDataType()) throw InvalidTypeException();
			RawValue = ValueAsString( (0, 0) );
		}

		protected IniLineValueIntTupleTranslator( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, quoteType, customQuotes )
		{
			if (!IsValidDataType()) throw InvalidTypeException();
			if (!Validate( value )) throw CantParseException( $"The stored value (\x22{value}\x22) cannot be parsed as a valid {typeof( T ).FullName} object." );
			RawValue = value;
		}

		protected IniLineValueIntTupleTranslator( IniFileMgmt root, (int i1, int i2) value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, quoteType, customQuotes )
		{
			if (!IsValidDataType()) throw InvalidTypeException();
			RawValue = ValueAsString( value );
		}
		#endregion

		#region Accessors
		new public abstract T Value { get; set; }

		protected int I1
		{
			get => base.Value.i1;
			set => base.Value = (value, I2);
		}

		protected int I2
		{
			get => base.Value.i2;
			set => base.Value = (I1, value);
		}

		protected override dynamic DefaultValue => (0, 0);
		#endregion

		#region Methods
		protected override Regex ValidateSource() => IntTupleParser_Rx();

		protected override (int i1, int i2) Parse( string source )
		{
			if (Validate( source ))
			{
				Match m = ValidateSource().Match( IntCleaner_Rx().Replace( source, "" ) );
				if (m.Success)
				{
					int x = m.Groups[ "i1" ].Success ? int.Parse( m.Groups[ "i1" ].Value ) : 0,
						y = m.Groups[ "i2" ].Success ? int.Parse( m.Groups[ "i2" ].Value ) : 0;

					return (x, y);
				}
			}
			throw CantParseException();
		}

		protected override string ValueAsString( (int i1, int i2) value ) => $"({value.i1},{value.i2})";

		protected abstract string ValueAsString( T value );

		protected string ValueAsString( T value, params char[] markup )
		{
			if (markup is null || markup.Length == 0) return ValueAsString( value );
			if (markup.Length == 1) markup = [ markup[ 0 ], markup[ 0 ] ];
			return $"({markup[ 0 ]}:{I1},{markup[ 1 ]}:{I2})";
		}

		public static bool IsValidDataType() =>
			IniLineValueTranslator<T>.IsValidDataType( typeof( Size ), typeof( Point ), typeof( (int, int) ) );

		[GeneratedRegex( PARSER, RegexOptions.Compiled )]
		private static partial Regex IntTupleParser_Rx();
		#endregion
	}

	public abstract partial class IniLineValueDecimalTupleTranslator<T> : IniLineValueTranslator<(float f1, float f2)>
	{
		/* language=Regex */
		protected const string PARSER = @"[({]?([a-zA-Z]+:?)?(?<f1>[+-]?[\d]+(\.[\d]*)?),([a-zA-Z]+:?)?(?<f2>[-+]?[\d]+(\.[\d]*)?)[)}]?";

		#region Constructors
		protected IniLineValueDecimalTupleTranslator( IniFileMgmt root ) 
			: base( root, typeof( SizeF ), typeof( PointF ), typeof( (float, float ) ) )
		{
			if (SupportedDataTypes is not null && !IsValidDataType()) throw InvalidTypeException();
			RawValue = ValueAsString( (0, 0) );
		}

		protected IniLineValueDecimalTupleTranslator( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
			: base( root, quoteType, quoteChars )
		{
			if (!IsValidDataType()) throw InvalidTypeException();
			RawValue = ValueAsString( (0, 0) );
		}

		protected IniLineValueDecimalTupleTranslator( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, quoteType, customQuotes )
		{
			if (!IsValidDataType()) throw InvalidTypeException();
			if (!Validate( value )) throw CantParseException( $"The stored value (\x22{value}\x22) cannot be parsed as a valid {typeof( T ).FullName} object." );
			RawValue = value;
		}

		protected IniLineValueDecimalTupleTranslator( IniFileMgmt root, T value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, quoteType, customQuotes )
		{
			if (!IsValidDataType()) throw InvalidTypeException();
			RawValue = ValueAsString( value );
		}
		#endregion

		#region Accessors
		new public abstract T Value { get; set; }

		protected float F1
		{
			get => base.Value.f1;
			set => base.Value = (value, F2);
		}

		protected float F2
		{
			get => base.Value.f2;
			set => base.Value = (F1, value);
		}

		protected override dynamic DefaultValue => (0.0f, 0.0f);
		#endregion

		#region Methods
		protected override Regex ValidateSource() => DecimalTupleParser_Rx();

		protected override (float f1, float f2) Parse( string source )
		{
			if (Validate( source ))
			{
				Match m = ValidateSource().Match( DecimalCleaner_Rx().Replace( source, "" ) );
				if (m.Success)
				{
					int x = m.Groups[ "f1" ].Success ? int.Parse( m.Groups[ "f1" ].Value ) : 0,
						y = m.Groups[ "f2" ].Success ? int.Parse( m.Groups[ "f2" ].Value ) : 0;

					return (x, y);
				}
			}
			throw CantParseException();
		}

		//protected abstract T Parse( string value );


		protected abstract string ValueAsString( T value );

		protected override string ValueAsString( (float f1, float f2) value ) => $"({value.f1},{value.f2})";

		protected string ValueAsString( T value, params char[] markup )
		{
			if (markup is null || markup.Length == 0) return ValueAsString( value );
			if (markup.Length == 1) markup = [ markup[ 0 ], markup[ 0 ] ];
			return $"({markup[ 0 ]}:{F1},{markup[ 1 ]}:{F2})";
		}

		public static bool IsValidDataType() =>
			IniLineValueTranslator<T>.IsValidDataType(
				typeof( SizeF ),
				typeof( PointF ),
				typeof( (float, float) )
			);

		[GeneratedRegex( PARSER, RegexOptions.Compiled )]
		private static partial Regex DecimalTupleParser_Rx();
		#endregion
	}
}
