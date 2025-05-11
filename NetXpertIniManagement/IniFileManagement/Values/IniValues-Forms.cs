using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using NetXpertExtensions;

namespace IniFileManagement.Values
{
	public sealed partial class IniLineFormValue : IniLineValueTranslator<Form>
	{
		#region Properties
		/* language=Regex */ private const string FORM_TYPE_PARSER = @"Type:[\x22'](?<type>[a-z][a-z\d]+)[\x22'];";
		/* language=Regex */ private const string TUPLE_TYPE_PARSER = @"(?<kind>Size|Loc(ation)?):\(?([XW]:)(?<v1>[+-]?[\d]+),([YH]:)?(?<v2>[+-]?[\d]+)\)?;";
		/* language=Regex */ private const string FORM_STATE_PARSER = @"State:[\x22'](?<state>M([ax]|[in]){1,2}|N[o0r]{0,2})[mizedal]{0,6}[\x22'];";
		/* language=Regex */ private const string FORM_TITLE_PARSER = @"T(itle|ext):\x22(?<title>[^\x22\r\n]+)\x22;";
		/* language=Regex */ private const string BOOLEAN_TYPE_PARSER = @"(?<kind>Is(Top|Mdi(Child|Parent))):(?<bool>T(rue)?|Y[es]+|O[nf]+|-?[\d]+|F(alse)?|No+)";
		/* language=Regex */ private const string FORM_START_PARSER = @"Start:\x22(?<start>Man[ual]{0,3}|Scr[en]{0,3}|Par[ent]{0,3}|Loc[ation]{0,5}|Bou[nds]{0,3}|Def[ault]{0,4})\x22;";

		private const RegexOptions PARSE_OPTIONS = RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

		/* language=Regex */
		private readonly string VALIDATION_PATTERN = $"({FORM_TYPE_PARSER[..^1]}|{TUPLE_TYPE_PARSER[ ..^1 ]}|{FORM_STATE_PARSER[ ..^1 ]}|{FORM_TITLE_PARSER[ ..^1 ]}|{BOOLEAN_TYPE_PARSER[ ..^1 ]}|{FORM_START_PARSER[..^1]});";
		#endregion

		#region Constructors
		public IniLineFormValue( IniFileMgmt root ) : base( root ) { }

		public IniLineFormValue( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
			: base( root, quoteType, quoteChars ) => RawValue = ValueAsString(new Form());

		public IniLineFormValue( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }

		public IniLineFormValue( IniFileMgmt root, Form value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }
		#endregion

		#region Accessors
		protected override Form DefaultValue => new();
		#endregion

		#region Methods
		protected override Form Parse( string source )
		{
			source = FormDefinitionCleaner_Rx().Replace( source ?? string.Empty, "" ).Trim();
			if (Validate( source ) )
			{
				// Extract Form Type:
				string formTypeName = FormTypeParser_Rx().Match( source ).Groups["type"].Value;

				Type formType = Type.GetType(formTypeName);
				if (formType is not null && formType.IsDerivedFrom<Form>(true))
				{
					Form form = Activator.CreateInstance( formType ) as Form;

					MatchCollection matches = FormTupleParser_Rx().Matches( source );
					foreach ( Match m in matches )
						if (m.Groups["kind"].Success)
						{
							int i1 = m.Groups[ "v1" ].Success ? (int.TryParse( m.Groups[ "v1" ].Value, out int v1 )? v1 : 0) : 0,
								i2 = m.Groups[ "v2" ].Success ? (int.TryParse( m.Groups[ "v2" ].Value, out int v2 ) ? v2 : 0) : 0;
							switch (m.Groups[ "kind" ].Value.ToUpperInvariant()[ 0 ])
							{
								case 'L': form.Location = new Point( i1, i2 ); break;
								case 'S': form.Size = new Size( i1, i2 ); break;
							}
						}

					var match = FormStateParser_Rx().Match( source );
					if ( match.Success )
					{
						string state = (match.Groups[ "state" ].Success ? match.Groups[ "state" ].Value : "Normal").ToUpperInvariant();
						form.WindowState = state[ 0 ] switch
						{
							'M' => state[ 1 ] switch { 'A' or 'X' => FormWindowState.Maximized, 'I' or 'N' => FormWindowState.Minimized, _ => FormWindowState.Normal },
							_ => FormWindowState.Normal,
						};
					}

					match = FormTitleParser_Rx().Match( source );
					if (match.Success && match.Groups[ "title" ].Success && !string.IsNullOrWhiteSpace( match.Groups[ "title" ].Value ))
						form.Text = match.Groups[ "title" ].Value.IsBase64String() ? match.Groups[ "title" ].Value.Base64Decode() : match.Groups[ "title" ].Value;

					matches = FormBooleanParser_Rx().Matches( source );
					foreach (Match m in matches)
						if (m.Groups[ "kind" ].Success)
						{
							bool val = BooleanValueParser_Rx().IsMatch( m.Groups[ "bool" ].Value );
							switch (m.Groups[ "kind" ].Value.ToUpperInvariant())
							{
								case "ISTOP":		form.TopMost = val; break;
								case "ISMDICHILD":	break; // form.IsMdiChild = !form.IsMdiContainer && val; break;
								case "ISMDIPARENT": form.IsMdiContainer = !form.IsMdiChild && val; break;
							}
						}

					match = FormStartParser_Rx().Match( source );
					if ( match.Success )
					{
						string start = (match.Groups[ "start" ].Success ? match.Groups[ "start" ].Value : "Default")[0..3].ToUpperInvariant();
						form.StartPosition = start switch
						{
							"PAR" => FormStartPosition.CenterParent,
							"SCR" => FormStartPosition.CenterScreen,
							"MAN" => FormStartPosition.Manual,
							"BOU" => FormStartPosition.WindowsDefaultBounds,
							_ => FormStartPosition.WindowsDefaultLocation,
						};
					}

					return form;
				}
			}
			return null;
		}

		protected override string ValueAsString( Form? value )
		{
			if (value is null) return string.Empty;

			var result = new StringBuilder();

			// Assign universal properties:
			result.AppendFormat( "Type:\x22{0}\x22;", value.GetType().FullName )
				  .AppendFormat( "Location:(X:{0},Y:{1});", value.Location.X, value.Location.Y )
				  .AppendFormat( "Size:(W:{0},H:{1});", value.Size.Width, value.Size.Height )
				  .AppendFormat( "Title:\x22{0}\x22;", value.Text.Base64Encode() );

			// Add WindowState if it's something other than Normal:
			if (value.WindowState != FormWindowState.Normal)
				result.AppendFormat( "State:\x22{0}\x22;", value.WindowState );

			// Add StartPosition if it's something other than WindowsDefaultLocation:
			if ( value.StartPosition != FormStartPosition.WindowsDefaultLocation )
				result.AppendFormat( "Start:\"{0}\";", value.StartPosition switch
				{
					FormStartPosition.Manual => "Manual",
					FormStartPosition.WindowsDefaultBounds => "Bounds",
					FormStartPosition.CenterParent => "Parent",
					FormStartPosition.CenterScreen => "Screen",
					_ => "Default"
				});

			// Add some other options if they're relevant:
			if ( value.IsMdiChild ) result.Append( "IsMdiChild:Yes;" );
			if ( value.IsMdiContainer ) result.Append( "IsMdiParent:Yes;" );
			if ( value.TopMost ) result.Append( "IsTop:Yes;" );

			return result.ToString();
		}

		public static bool IsValidDataType() => IsValidDataType( typeof( Form ), typeof( Control ) );

		protected override bool Validate( string value ) => 
			!string.IsNullOrWhiteSpace(value) && ValidateSource().IsMatch( value );

		// NOTE: Can't use Generated Regex here b/c VALIDATION_PATTERN isn't a constant!
		protected override Regex ValidateSource() => new( VALIDATION_PATTERN, PARSE_OPTIONS ); 

		[GeneratedRegex( @"[\t\r\n]" )] private static partial Regex FormDefinitionCleaner_Rx();

		[GeneratedRegex( FORM_TYPE_PARSER, PARSE_OPTIONS )] private static partial Regex FormTypeParser_Rx();

		[GeneratedRegex( TUPLE_TYPE_PARSER, PARSE_OPTIONS )] private static partial Regex FormTupleParser_Rx();

		[GeneratedRegex( FORM_TITLE_PARSER, PARSE_OPTIONS )] private static partial Regex FormTitleParser_Rx();

		[GeneratedRegex( FORM_STATE_PARSER, PARSE_OPTIONS )] private static partial Regex FormStateParser_Rx();


		[GeneratedRegex( BOOLEAN_TYPE_PARSER, PARSE_OPTIONS )] private static partial Regex FormBooleanParser_Rx();


		[GeneratedRegex( FORM_START_PARSER, PARSE_OPTIONS )] private static partial Regex FormStartParser_Rx();

		[GeneratedRegex( @"(T(rue)?|Y[es]+|On|-?(0x)?[1-9a-f][\da-f]*)", PARSE_OPTIONS )] private static partial Regex BooleanValueParser_Rx();
		#endregion
	}
}
