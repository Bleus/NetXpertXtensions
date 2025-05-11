using System.Management.Automation.Language;
using System.Numerics;
using System.Text.RegularExpressions;

namespace IniFileManagement.Values
{
	public sealed partial class IniLineULongIntValue : IniLineUnsignedIntTranslator<ulong>
	{
		#region Constructors
		public IniLineULongIntValue( IniFileMgmt root ) : base( root ) { }

		public IniLineULongIntValue( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
			: base( root, quoteType, quoteChars ) => this.Value = default;

		public IniLineULongIntValue( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }

		public IniLineULongIntValue( IniFileMgmt root, ulong value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }
		#endregion
	}

	public sealed partial class IniLineUIntValue : IniLineUnsignedIntTranslator<uint>
	{
		#region Constructors
		public IniLineUIntValue( IniFileMgmt root ) : base( root ) { }

		public IniLineUIntValue( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
			: base( root, quoteType, quoteChars ) => this.Value = default;

		public IniLineUIntValue( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }

		public IniLineUIntValue( IniFileMgmt root, uint value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }
		#endregion
	}

	public sealed partial class IniLineUShortIntValue : IniLineUnsignedIntTranslator<ushort>
	{
		#region Constructors
		public IniLineUShortIntValue( IniFileMgmt root ) : base( root ) { }

		public IniLineUShortIntValue( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
			: base( root, quoteType, quoteChars ) => this.Value = default;

		public IniLineUShortIntValue( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }

		public IniLineUShortIntValue( IniFileMgmt root, ushort value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }
		#endregion
	}

	public sealed partial class IniLineByteValue : IniLineUnsignedIntTranslator<byte>
	{
		#region Constructors
		public IniLineByteValue( IniFileMgmt root ) : base( root ) { }

		public IniLineByteValue( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
			: base( root, quoteType, quoteChars ) => this.Value = default;

		public IniLineByteValue( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }

		public IniLineByteValue( IniFileMgmt root, byte value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }
		#endregion
	}

	public sealed partial class IniLineLongIntValue : IniLineSignedIntTranslator<long>
	{
		#region Constructors
		public IniLineLongIntValue( IniFileMgmt root ) : base( root ) { }

		public IniLineLongIntValue( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
			: base( root, quoteType, quoteChars ) => this.Value = default;

		public IniLineLongIntValue( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }

		public IniLineLongIntValue( IniFileMgmt root, long value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }
		#endregion
	}

	public sealed partial class IniLineIntValue : IniLineSignedIntTranslator<int>
	{
		#region Constructors
		public IniLineIntValue( IniFileMgmt root ) : base( root ) { }

		public IniLineIntValue( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
			: base( root, quoteType, quoteChars ) => this.Value = default;

		public IniLineIntValue( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }

		public IniLineIntValue( IniFileMgmt root, int value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }
		#endregion
	}

	public partial class IniLineShortIntValue : IniLineSignedIntTranslator<short>
	{
		#region Constructors
		public IniLineShortIntValue( IniFileMgmt root ) : base( root ) { }

		public IniLineShortIntValue( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
			: base( root, quoteType, quoteChars ) => this.Value = default;

		public IniLineShortIntValue( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }

		public IniLineShortIntValue( IniFileMgmt root, short value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }
		#endregion
	}

	public sealed partial class IniLineSByteValue : IniLineSignedIntTranslator<sbyte>
	{
		#region Constructors
		public IniLineSByteValue( IniFileMgmt root ) : base( root ) { }

		public IniLineSByteValue( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
			: base( root, quoteType, quoteChars ) => this.Value = default;

		public IniLineSByteValue( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }

		public IniLineSByteValue( IniFileMgmt root, sbyte value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }
		#endregion
	}

	public sealed partial class IniLineDecimalValue : IniLineValueTranslator<decimal>
	{
		#region Properties
		/* language=Regex */ public const string PARSER = @"[+-]?[\d]+(\.[\d]*)?";
		#endregion

		#region Constructors
		public IniLineDecimalValue( IniFileMgmt root ) : base( root ) { }

		public IniLineDecimalValue( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
			: base( root, quoteType, quoteChars ) => this.Value = default;

		public IniLineDecimalValue( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }

		public IniLineDecimalValue( IniFileMgmt root, decimal value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }
		#endregion

		#region Methods
		protected override decimal Parse( string source ) =>
			ValidateSource().IsMatch( source ) ? decimal.CreateChecked( double.Parse( source ) ) : 0m;

		protected override Regex ValidateSource() => DecimalValidator_Rx();

		public static bool IsValidDataType() => IniLineValueTranslator<decimal>.IsValidDataType( typeof( decimal ) );

		[GeneratedRegex( PARSER, RegexOptions.None )] public static partial Regex DecimalValidator_Rx();
		#endregion
	}

	public sealed partial class IniLineFloatValue : IniLineFloatTranslator<float>
	{
		#region Constructors
		public IniLineFloatValue( IniFileMgmt root ) : base( root ) { }

		public IniLineFloatValue( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
			: base( root, quoteType, quoteChars ) => this.Value = default;

		public IniLineFloatValue( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }

		public IniLineFloatValue( IniFileMgmt root, float value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }
		#endregion

		#region Methods
		protected override float MyParse( string source ) => float.Parse( source );
		#endregion
	}

	public sealed partial class IniLineDoubleValue : IniLineFloatTranslator<double>
	{
		#region Constructors
		public IniLineDoubleValue( IniFileMgmt root ) : base( root ) { }

		public IniLineDoubleValue( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
			: base( root, quoteType, quoteChars ) => this.Value = default;

		public IniLineDoubleValue( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }

		public IniLineDoubleValue( IniFileMgmt root, double value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }
		#endregion

		#region Methods
		protected override double MyParse( string source ) => double.Parse( source );
		#endregion
	}

	public sealed partial class IniLineUInt128Value : IniLineUnsignedIntTranslator<UInt128>
	{
		#region Constructors
		public IniLineUInt128Value( IniFileMgmt root ) : base( root ) { }

		public IniLineUInt128Value( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
			: base( root, quoteType, quoteChars ) => this.Value = default;

		public IniLineUInt128Value( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }

		public IniLineUInt128Value( IniFileMgmt root, UInt128 value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }
		#endregion
	}

	public sealed partial class IniLineInt128Value : IniLineSignedIntTranslator<Int128>
	{
		#region Constructors
		public IniLineInt128Value( IniFileMgmt root ) : base( root ) { }

		public IniLineInt128Value( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
			: base( root, quoteType, quoteChars ) => this.Value = default;

		public IniLineInt128Value( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }

		public IniLineInt128Value( IniFileMgmt root, Int128 value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }
		#endregion
	}

	public sealed partial class IniLineBigIntValue : IniLineSignedIntTranslator<BigInteger>
	{
		#region Constructors
		public IniLineBigIntValue( IniFileMgmt root ) : base( root ) { }

		public IniLineBigIntValue( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
			: base( root, quoteType, quoteChars ) => this.Value = default;

		public IniLineBigIntValue( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }

		public IniLineBigIntValue( IniFileMgmt root, BigInteger value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }
		#endregion

		#region Methods
		protected override BigInteger Parse( string source )
		{
			if (!string.IsNullOrWhiteSpace( source ) && BigInteger.TryParse( source, out BigInteger value )) return value;
			throw CantParseException();
		}

		protected override bool Validate( string value ) =>
			!string.IsNullOrWhiteSpace( value ) && BigInteger.TryParse( value, out _ );
		#endregion
	}
}
