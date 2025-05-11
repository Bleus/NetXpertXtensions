using System.Text.RegularExpressions;
using NetXpertExtensions.Classes;

namespace IniFileManagement.Values
{
	public sealed partial class IniLineVersionValue : IniLineValueTranslator<VersionMgmt>
	{
		#region Constructors
		public IniLineVersionValue( IniFileMgmt root ) : base( root ) { }

		public IniLineVersionValue( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
			: base( root, quoteType, quoteChars ) => this.Value = DefaultValue;

		public IniLineVersionValue( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }

		public IniLineVersionValue( IniFileMgmt root, VersionMgmt value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }
		#endregion

		#region Accessors
		protected override VersionMgmt DefaultValue => VersionMgmt.Parse("1.0.0.0");
		#endregion

		#region Methods
		protected override VersionMgmt Parse( string source )
		{
			if (!VersionMgmt.TryParse( base.RawValue, out VersionMgmt version )) throw CantParseException();
			return version;
		}

		protected override string? ValueAsString( VersionMgmt value ) => value?.ToString();

		protected override bool Validate( string value ) => !string.IsNullOrWhiteSpace( value ) && Version.TryParse( value, out _ );

		public static bool IsValidDataType() => IniLineValueTranslator<Version>.IsValidDataType( typeof( Version ) );

		protected override Regex ValidateSource() => VersionValidator_Rx();

		[GeneratedRegex( @"^\d+(\.[\d]{1,9}}){0,3}$", RegexOptions.Compiled )] private static partial Regex VersionValidator_Rx();
		#endregion
	}
}
