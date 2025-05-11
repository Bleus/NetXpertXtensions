using System.Text.RegularExpressions;

namespace IniFileManagement.Values
{
	public sealed partial class IniLinePointValue : IniLineValueIntTupleTranslator<Point>
	{
		#region Constructors
		public IniLinePointValue( IniFileMgmt root ) : base( root ) { }

		public IniLinePointValue( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
			: base( root, quoteType, quoteChars ) => this.Value = new( 0, 0 );

		public IniLinePointValue( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }

		public IniLinePointValue( IniFileMgmt root, Point value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, (value.X, value.Y), quoteType, customQuotes ) { }
		#endregion

		#region Accessors
		public override Point Value
		{
			get => new( I1, I2 );
			set { I1 = value.X; I2 = value.Y; }
		}

		public int X { get => I1; set => I1 = value; }

		public int Y { get => I2; set => I2 = value; }
		#endregion

		#region Methods
		protected override string ValueAsString( Point value ) => ValueAsString( value, 'X', 'Y' );

		protected override bool Validate( string value ) => !string.IsNullOrWhiteSpace( value ) && ValidateSource().IsMatch( value );

		protected override Regex ValidateSource() => PointValidator_Rx();

		[GeneratedRegex( @"[({]?([xX]:?)?(?<X>[+-]?[\d]+),([yY]:?)?(?<Y>[-+]?[\d]+)[)}]?", RegexOptions.ExplicitCapture )]
		private static partial Regex PointValidator_Rx();
		#endregion
	}

	public sealed partial class IniLinePointFValue : IniLineValueDecimalTupleTranslator<PointF>
	{
		#region Constructors
		public IniLinePointFValue( IniFileMgmt root ) : base( root ) { }

		public IniLinePointFValue( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
			: base( root, quoteType, quoteChars ) => this.Value = new( 0, 0 );

		public IniLinePointFValue( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }

		public IniLinePointFValue( IniFileMgmt root, PointF value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }
		#endregion

		#region Accessors
		public override PointF Value
		{
			get => new( F1, F2 );
			set { F1 = value.X; F2 = value.Y; }
		}

		public float X { get => F1; set => F1 = value; }

		public float Y { get => F2; set => F2 = value; }
		#endregion

		#region Methods
		protected override bool Validate( string value ) => !string.IsNullOrWhiteSpace( value ) && ValidateSource().IsMatch( value );

		protected override string ValueAsString( PointF value ) => ValueAsString( value, 'X', 'Y' );

		protected override Regex ValidateSource() => ValidatePointF_Rx();

		[GeneratedRegex( @"[({]?([xX]:?)?(?<X>[+-]?[\d]+(\.[\d]*)?),([yY]:?)?(?<Y>[-+]?[\d]+(\.[\d]*)?)[)}]?", RegexOptions.ExplicitCapture )]
		private static partial Regex ValidatePointF_Rx();
		#endregion
	}

	public sealed partial class IniLineSizeValue : IniLineValueIntTupleTranslator<Size>
	{
		#region Constructors
		public IniLineSizeValue( IniFileMgmt root ) : base( root ) { }

		public IniLineSizeValue( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
			: base( root, quoteType, quoteChars ) => this.Value = new( 0, 0 );

		public IniLineSizeValue( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }

		public IniLineSizeValue( IniFileMgmt root, Size value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, quoteType, customQuotes ) => this.Value = value;
		#endregion

		#region Accessors
		public override Size Value
		{
			get => new( I1, I2 );
			set { I1 = value.Width; I2 = value.Height; }
		}

		public int Width
		{
			get => I1;
			set => I1 = value;
		}

		public int Height
		{
			get => I2;
			set => I2 = value;
		}
		#endregion

		#region Methods
		protected override string ValueAsString( Size value ) => ValueAsString( value, 'W', 'H' );

		protected override bool Validate( string value ) => !string.IsNullOrWhiteSpace( value ) && ValidateSource().IsMatch( value );

		protected override Regex ValidateSource() => SizeValidator_Rx();

		[GeneratedRegex( @"[({]?([Ww]:?)?(?<Width>[+-]?[\d]+),([Hh]:?)?(?<Height>[-+]?[\d]+)[)}]?", RegexOptions.ExplicitCapture )]
		private static partial Regex SizeValidator_Rx();
		#endregion
	}

	public sealed partial class IniLineSizeFValue : IniLineValueDecimalTupleTranslator<SizeF>
	{
		#region Constructors
		public IniLineSizeFValue( IniFileMgmt root ) : base( root ) { }

		public IniLineSizeFValue( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
			: base( root, quoteType, quoteChars ) => this.Value = new( 0, 0 );

		public IniLineSizeFValue( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }

		public IniLineSizeFValue( IniFileMgmt root, SizeF value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }
		#endregion

		#region Accessors
		public override SizeF Value
		{
			get => new( F1, F2 );
			set { F1 = value.Width; F2 = Value.Height; }
		}

		public float Width
		{
			get => F1;
			set => F1 = value;
		}

		public float Height
		{
			get => F2;
			set => F2 = value;
		}
		#endregion

		#region Methods
		protected override string ValueAsString( SizeF value ) => ValueAsString( value, 'W', 'H' );

		protected override bool Validate( string value ) => !string.IsNullOrWhiteSpace( value ) && ValidateSource().IsMatch( value );

		protected override Regex ValidateSource() => ValidateSizeF_Rx();

		[GeneratedRegex( @"[({]?([Ww]:?)?(?<Width>[+-]?[\d]+(\.[\d]*)?),([Hh]:?)?(?<Height>[-+]?[\d]+(\.[\d]*)?)[)}]?", RegexOptions.ExplicitCapture )]
		private static partial Regex ValidateSizeF_Rx();
		#endregion
	}
}
