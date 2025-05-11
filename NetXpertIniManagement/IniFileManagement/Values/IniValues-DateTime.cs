using System.Text.RegularExpressions;

namespace IniFileManagement.Values
{
	public sealed partial class IniLineDateTimeValue : IniLineValueTranslator<DateTime>
	{
		#region Constructors
		public IniLineDateTimeValue( IniFileMgmt root ) : base( root ) { }

		public IniLineDateTimeValue( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
			: base( root, quoteType, quoteChars ) => this.Value = DateTime.Now;

		public IniLineDateTimeValue( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }

		public IniLineDateTimeValue( IniFileMgmt root, DateTime value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }
		#endregion

		#region Accessors
		protected override dynamic DefaultValue => DateTime.Now;
		#endregion

		#region Methods
		protected override DateTime Parse( string source )
		{
			if (!DateTime.TryParse( base.RawValue, out DateTime dateTime )) throw CantParseException();
			return dateTime;
		}

		protected override string ValueAsString( DateTime value ) => value.ToString( "o" );

		protected override bool Validate( string value ) => !string.IsNullOrWhiteSpace( value ) && DateTime.TryParse( value, out _ );

		protected override Regex ValidateSource() => throw new NotImplementedException();
		#endregion
	}
}
