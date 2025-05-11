using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace IniFileManagement.Values
{
	public sealed partial class IniLineBooleanValue : IniLineValueTranslator<bool>
	{
		#region Properties
		private BooleanStyle _style = BooleanStyle.Unknown;

		public enum BooleanStyle { TrueFalse, YesNo, OneZero, OnOff, Unknown };
		#endregion

		#region Constructors
		public IniLineBooleanValue( IniFileMgmt root ) : base( root ) { }

		public IniLineBooleanValue( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
			: base( root, quoteType, quoteChars ) { }

		public IniLineBooleanValue( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, quoteType, customQuotes )
		{
			if (!Validate( value )) throw CantParseException();
			Style = DetectStyle( value );
			this.Value = Parse( value );
		}

		public IniLineBooleanValue( IniFileMgmt root, bool value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) => Style = BooleanStyle.TrueFalse;
		#endregion

		#region Accessors
		public override bool Value 
		{ 
			get => Parse(base.RawValue); 
			set => base.RawValue = ValueAsString( value ); 
		}

		public BooleanStyle Style
		{
			get => this._style;
			set => this._style = value == BooleanStyle.Unknown ? DetectStyle( this.RawValue ) : value;
		}

		protected override dynamic DefaultValue => false;

		#endregion

		#region Methods
		protected override bool Parse( string source ) => 
			ValidateSource().IsMatch( WhiteSpaceRemover_Rx().Replace( source, "" ) );

		protected override string ValueAsString( bool value )
		{
			//bool v = Value;
			return Style switch { 
				BooleanStyle.OneZero => value ? "1" : "0", 
				BooleanStyle.YesNo => value ? "Yes" : "No", 
				BooleanStyle.OnOff => value ? "On" : "Off",
				_ => value ? "True" : "False" 
			};
		}

		public static BooleanStyle DetectStyle( string value )
		{
			value = string.IsNullOrWhiteSpace(value) ? "true" : WhiteSpaceRemover_Rx().Replace( value, "" );

			if (TrueFalseIdentity_Rx().IsMatch( value )) return BooleanStyle.TrueFalse;

			if (YesNoIdentity_Rx().IsMatch( value )) return BooleanStyle.YesNo;

			if (OnOffIdentity_Rx().IsMatch( value )) return BooleanStyle.OnOff;

			if (Digitalidentity_Rx().IsMatch( value )) return BooleanStyle.OneZero;

			return BooleanStyle.Unknown;
		}

		protected override bool Validate( string value ) =>
			!string.IsNullOrWhiteSpace( value ) &&
			ValidateBoolFormat_Rx().IsMatch( WhiteSpaceRemover_Rx().Replace( value, "" ) );

		protected override Regex ValidateSource() => ValidateBoolValue_Rx();

		[GeneratedRegex( @"^(T[rue]*|F[alse]*|O[nf]*|Y[es]*|No*|-?[\d]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant )]
		private static partial Regex ValidateBoolFormat_Rx();

		[GeneratedRegex( @"^(T[rue]*|Y[es]*|On?|-?[1-9][\d]*)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant )]
		private static partial Regex ValidateBoolValue_Rx();

		[GeneratedRegex( @"^(T[rue]*|F[alse]*)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant )]
		private static partial Regex TrueFalseIdentity_Rx();

		[GeneratedRegex( @"^(Y(es)?|No?)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant )]
		private static partial Regex YesNoIdentity_Rx();

		[GeneratedRegex( @"^O(n|f)*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant )]
		private static partial Regex OnOffIdentity_Rx();

		[GeneratedRegex( @"^(-?[\d]+)", RegexOptions.None )]
		private static partial Regex Digitalidentity_Rx();

		[GeneratedRegex( @"[\s]", RegexOptions.None )]
		private static partial Regex WhiteSpaceRemover_Rx();
		#endregion
	}
}
