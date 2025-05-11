using System.Text.RegularExpressions;

namespace IniFileManagement.Values
{
	public sealed partial class IniLineValue : IniLineValueFoundation
	{
		#region Constructors
		public IniLineValue(IniFileMgmt root) : base(root) { }

		public IniLineValue( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
			: base( root, quoteType, quoteChars ) { }

		public IniLineValue( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }
		#endregion

		#region Operators
		public static implicit operator IniLineValue( (IniFileMgmt root, string value) source ) => new( source.root, (string.IsNullOrEmpty( source.value ) ? string.Empty : source.value) );
		public static implicit operator string( IniLineValue source ) => source is null ? string.Empty : source.Value;
		#endregion

		#region Accessors
		public string Value
		{
			get => RawValue;
			set => RawValue = string.IsNullOrEmpty( value ) ? string.Empty : value;
		}

		protected override Type DataType => typeof( string );

		protected override dynamic DefaultValue => string.Empty;
		#endregion

		#region Methods
		protected override bool Validate( string value ) => value is not null && ValidateSource().IsMatch( value );

		protected override Regex ValidateSource() => GenericStringValidator_Rx();

		[GeneratedRegex( @"[^\r\n]*", RegexOptions.Compiled )] private static partial Regex GenericStringValidator_Rx();
		#endregion
	}
}
