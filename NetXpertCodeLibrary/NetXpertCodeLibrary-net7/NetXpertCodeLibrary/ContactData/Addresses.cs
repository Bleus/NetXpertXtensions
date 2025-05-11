using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using NetXpertCodeLibrary.Extensions;
using NetXpertExtensions;
using NetXpertExtensions.Xml;

namespace NetXpertCodeLibrary.ContactData
{
	/// <summary>Facilitates managing two strings as a single entity.</summary>
	/// <remarks>Used here to manage Province/State/Country names and abbreviations. The abbreviations are constrained to two-letters according to ISO definitions.</remarks>
	public class LongNameAndAbbr
	{
		#region Properties
		private string _longName = "";
		private string _abbr = "";
		#endregion

		#region Constructors
		public LongNameAndAbbr() { }

		public LongNameAndAbbr( string name, string abbr )
		{
			this.Name = name;
			this.Abbr = abbr;
		}
		#endregion

		#region Accessors
		public string Name
		{
			get => this._longName;
			set
			{
				if ( string.IsNullOrWhiteSpace( value ) || !Regex.IsMatch( value, @"[a-zA-Z][a-zA-Z0-9 .-]*[a-zA-Z0-9]" ) )
					throw new FormatException( $"The supplied value is not acceptable as a name (\"{value}\")." );

				this._longName = value;
			}
		}

		public string Abbr
		{
			get => this._abbr;
			set
			{
				if ( !Regex.IsMatch( value, @"^[a-zA-Z]{2}$" ) )
					throw new FormatException( $"Abbreviations can only consist of two letters." );

				this._abbr = value.ToUpperInvariant();
			}
		}
		#endregion

		#region Methods
		public override string ToString() => this._longName.Length + this._abbr.Length == 0 ? "" : $"{this._longName} ({this._abbr})".Trim();

		public static LongNameAndAbbr Parse( string source )
		{
			if ( !string.IsNullOrWhiteSpace( source ) )
			{
				Match m = Regex.Match( source, @"^(?<long>[a-z][a-z0-9 .-]*[a-zA-Z0-9])( [(](?<abbr>[a-z]{2})[)])?$", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Compiled );
				if ( m.Success ) return new( m.Groups[ "long" ].Value, m.Groups[ "abbr" ].Success ? m.Groups[ "abbr" ].Value : "" );
			}
			throw new ArgumentException( $"The supplied string was null, whitespace or not in a recognizable format (ie: \"Name (NM)\"): \"{source}\"" );
		}

		public static LongNameAndAbbr GetCountry( RegionInfo region ) =>
			new( region.NativeName, region.TwoLetterISORegionName );

		/// <summary>Loads all known Windows' Regions into a 'List&lt;RegionInfo&gt;' object.</summary>
		public static List<RegionInfo> GetAllRegions( CultureTypes cultureTypes = CultureTypes.AllCultures )
		{
			List<RegionInfo> Countries = new();

			foreach ( CultureInfo culture in CultureInfo.GetCultures( cultureTypes ) )
				if ( culture.LCID != 127 ) // LCID 127 = CultureInfo.InvariantCulture
					Countries.Add( new( culture.TextInfo.CultureName ) );

			return Countries;
		}

		/// <summary>Uses Windows&apos; internal Region definitions to try and populate a new LongNameAndNumber object from the supplied country name string.</summary>
		/// <param name="name">A string to use to try and populate a new LongNameAndAbbr object with.</param>
		/// <returns>If the supplied name can be matched to a known Region, an appropriately populated LongNameAndNumber object, otherwise NULL.</returns>
		/// <remarks>
		/// This checks the supplied name against the Region's:<br />* Two-letter ISO name,<br />* Three-letter ISO name,<br />* Three-letter Windows' name,<br />* its regular name,<br />* display name,<br />* english name <br />AND<br />* native name.<br />
		/// <br />Spelling matters, case doesn't!
		/// </remarks>
		public static LongNameAndAbbr GetCountry( string name )
		{
			if ( string.IsNullOrWhiteSpace( name ) ) throw new ArgumentNullException( "You must provide a string to parse." );
			name = name.Trim();

			// Test the two most popular searches independently first:
			if ( Regex.IsMatch( name, @"^(Ca[nad]{0,4})$", RegexOptions.IgnoreCase ) ) return new( "Canada", "CA" );
			if ( Regex.IsMatch( name, @"^(U[.]? *S[.]? *([of]{0,3} )?(A[.]?)?|Am[erica]{5}|United *States *(([of]{1,3} *)?Am[erica]{2,5})?)$", RegexOptions.IgnoreCase ) )
				return new( "United States of America", "US" );

			RegionInfo[] regions = GetAllRegions().ToArray();
			int i = -1;
			while ( ++i < regions.Length )
				switch ( name.Length )
				{
					case 2:
						if ( regions[ i ].TwoLetterISORegionName.Equals( name, StringComparison.CurrentCultureIgnoreCase ) )
							return new( regions[ i ].NativeName, regions[ i ].TwoLetterISORegionName );
						break;
					case 3:
						if (
								regions[ i ].ThreeLetterISORegionName.Equals( name, StringComparison.CurrentCultureIgnoreCase ) ||
								regions[ i ].ThreeLetterWindowsRegionName.Equals( name, StringComparison.CurrentCultureIgnoreCase )
						)
							return new( regions[ i ].NativeName, regions[ i ].TwoLetterISORegionName );
						break;
					default:
						if (
								regions[ i ].Name.Equals( name, StringComparison.CurrentCultureIgnoreCase ) ||
								regions[ i ].DisplayName.Equals( name, StringComparison.CurrentCultureIgnoreCase ) ||
								regions[ i ].NativeName.Equals( name, StringComparison.CurrentCultureIgnoreCase ) ||
								regions[ i ].EnglishName.Equals( name, StringComparison.CurrentCultureIgnoreCase )
						)
							return new( regions[ i ].NativeName, regions[ i ].TwoLetterISORegionName );
						break;
				}

			return null;
		}

		/// <summary>Attempts to generate a populated LongNameAndAbbr object from a supplied string identifier.</summary>
		/// <param name="value">A string identifier to parse into a Canadian Province LongNameAndAbbr object.</param>
		/// <returns>If the supplied string was parsed, the result, otherwise NULL.</returns>
		/// <remarks>
		/// Typically the name of the province, or, at least, a uniquely identifiable part of it can be passed.<br />
		/// French provincial names, and standard ISO two-digit abbreviations are recognized.<br />
		/// </remarks>
		public static LongNameAndAbbr GetCanadianProvice( string value )
		{
			if ( string.IsNullOrWhiteSpace( value ) ) throw new ArgumentNullException( "You must provide a Province name or abbreviation to parse!" );

			value = value.Trim();

			if ( Regex.IsMatch( value.Trim(), @"^[A-Z]{2}$" ) )
				return value.Trim().ToUpperInvariant() switch
				{
					"AB" => new( "Alberta", "AB" ),
					"BC" => new( "British Columbia", "BC" ),
					"MA" or "MB" => new( "Manitoba", "MB" ),
					"NB" => new( "New Brunswick", "NB" ),
					"NL" or "NF" => new( "Newfoundland and Labrador", "NL" ),
					"NS" => new( "Nova Scotia", "NS" ),
					"NW" or "NT" => new( "Northwest Territories", "NT" ),
					"PE" or "NU" => new( "Nunavut", "NU" ),
					"ON" => new( "Ontario", "ON" ),
					"PI" => new( "Prince Edward Island", "PE" ),
					"PQ" or "QB" or "QC" => new( "Québec", "QC" ),
					"SA" or "SK" => new( "Saskatchewan", "SK" ),
					"YU" or "YK" => new( "Yukon Territory", "YK" ),
					_ => throw new KeyNotFoundException( $"No Province could be correlated with the supplied value (\x22{value.Trim()}\x22)." ),
				};
			KeyValuePair<string, string>[] names = new KeyValuePair<string, string>[]
			{
				new( "AB", "<Al[berta]*" ),
				new( "BC", "<Bri[tish]*" ), new( "BC", "Col[umbia]*>" ), new ( "BC", "<Colo[mbie -]*" ), new( "BC", "[ -]*Britannique>" ),
				new( "MB", "<Man[itoba]*" ),
				new( "NB", "Brun[swick]{0,5}>" ), new( "NB", "<New ?Br" ), new ( "NB", "<Nou[veau]{1,4}[- ]*Br" ),
				/*"Newf[oundland]* *(and|&|+)? *Labrador",*/ 
				new( "NF", "<Newf[oundld]{0,8}" ), new( "NF", "Lab[rador]{0,5}>" ), new( "NF", "<Terre" ), new( "NF", "[ -]?Neuve[ -]" ),
				new( "NS", "<Nov(a *S)?" ), new( "NS", "Sco[tia]{1,2}>" ), new( "NS", "<Nouvelle" ), new( "NS", "[ÉE]cosse>" ),
				new( "NT", "<Nort([hwest]{0,5})" ), new( "NT", "<Territoires" ), new( "NT", "Nord[- ]*Ouest>" ),
				new( "NU", "<Nun[avut]*" ),
				new( "ON", "<Ont[ario]*" ),
				new( "PE", "<PEI" ), new( "PE", "<Pri[nce]{0,3}" ), new( "PE", "Edward" ), new( "PE", "Island" ), new( "PE", "<[ÎI]le" ), new( "PE", "Édouard>" ),
				new( "PQ", "<QUE" ), new( "PQ", "<Qu[eé][bec]{0,3}?"),
				new( "SK", "<Sask[atchewn]{0,8}" ),
				new( "AB", "<Y[ukon]{0,4}" )
			};

			int i = -1;
			while ( ++i < names.Length )
			{
				string pattern = names[ i ].Value;
				pattern = (pattern[ 0 ] == '<' ? "^" : "") + $"{pattern.Substring( 1 )}";
				pattern = pattern.EndsWith( ">" ) ? pattern.TrimEnd( '>' ) + ")$" : pattern + ")";
				if ( Regex.IsMatch( value, pattern, RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase ) ) return GetCanadianProvice( names[ i ].Key );
			}

			throw new KeyNotFoundException( $"No Province could be correlated with the supplied value (\x22{value.Trim()}\x22)." );
		}

		/// <summary>Attempts to generate a populated LongNameAndAbbr object from a supplied string identifier.</summary>
		/// <param name="value">A string identifier to parse into an American State LongNameAndAbbr object.</param>
		/// <returns>If the supplied string was parsed, the result, otherwise NULL.</returns>
		/// <remarks>Typically the name of the state, or, at least, a uniquely identifiable part of it can be passed. The ISO two-digit standard abbreviations are all recognized.</remarks>
		public static LongNameAndAbbr GetAmericanState( string value )
		{
			if ( string.IsNullOrWhiteSpace( value ) ) throw new ArgumentNullException( "You must provide a State name or abbreviation to parse!" );

			value = value.Trim().ToUpperInvariant();

			if ( Regex.IsMatch( value, @"^[A-Z]{2}$" ) )
				return value switch
				{
					/*
					   state	US-AL	Alabama		en		
					   state	US-AK	Alaska		en		
					   outlying area	US-AS	American Samoa (see also separate country code entry under AS)		en		
					   state	US-AZ	Arizona		en		
					   state	US-AR	Arkansas		en		
					   state	US-CA	California		en		
					   state	US-CO	Colorado		en		
					   state	US-CT	Connecticut		en		
					   state	US-DE	Delaware		en		
					   district	US-DC	District of Columbia		en		
					   state	US-FL	Florida		en		
					   state	US-GA	Georgia		en		
					   outlying area	US-GU	Guam (see also separate country code entry under GU)		en		
					   state	US-HI	Hawaii		en		
					   state	US-ID	Idaho		en		
					   state	US-IL	Illinois		en		
					   state	US-IN	Indiana		en		
					   state	US-IA	Iowa		en		
					   state	US-KS	Kansas		en		
					   state	US-KY	Kentucky		en		
					   state	US-LA	Louisiana		en		
					   state	US-ME	Maine		en		
					   state	US-MD	Maryland		en		
					   state	US-MA	Massachusetts		en		
					   state	US-MI	Michigan		en		
					   state	US-MN	Minnesota		en		
					   state	US-MS	Mississippi		en		
					   state	US-MO	Missouri		en		
					   state	US-MT	Montana		en		
					   state	US-NE	Nebraska		en		
					   state	US-NV	Nevada		en		
					   state	US-NH	New Hampshire		en		
					   state	US-NJ	New Jersey		en		
					   state	US-NM	New Mexico		en		
					   state	US-NY	New York		en		
					   state	US-NC	North Carolina		en		
					   state	US-ND	North Dakota		en		
					   outlying area	US-MP	Northern Mariana Islands (see also separate country code entry under MP)		en		
					   state	US-OH	Ohio		en		
					   state	US-OK	Oklahoma		en		
					   state	US-OR	Oregon		en		
					   state	US-PA	Pennsylvania		en		
					   outlying area	US-PR	Puerto Rico (see also separate country code entry under PR)		en		
					   state	US-RI	Rhode Island		en		
					   state	US-SC	South Carolina		en		
					   state	US-SD	South Dakota		en		
					   state	US-TN	Tennessee		en		
					   state	US-TX	Texas		en		
					   outlying area	US-UM	United States Minor Outlying Islands (see also separate country code entry under UM)		en		
					   state	US-UT	Utah		en		
					   state	US-VT	Vermont		en		
					   outlying area	US-VI	Virgin Islands, U.S. (see also separate country code entry under VI)		en		
					   state	US-VA	Virginia		en		
					   state	US-WA	Washington		en		
					   state	US-WV	West Virginia		en		
					   state	US-WI	Wisconsin		en		
					   state	US-WY	Wyoming */
					"AL" => new( "Alabama", value ),
					"AK" => new( "Alaska", value ),
					"AR" => new( "Arkansas", value ),
					"AS" => new( "American Samoa", value ),
					"AZ" => new( "Arizona", value ),
					"CA" => new( "California", value ),
					"CO" => new( "Colorado", value ),
					"CT" => new( "Conneticut", value ),
					"DC" => new( "District of Columbia", value ),
					"DE" => new( "Deleware", value ),
					"FL" => new( "Florida", value ),
					"GE" or "GA" => new( "Georgia", value ),
					"GU" => new( "Guam", value ),
					"HA" or "HI" => new( "Hawaii", value ),
					"IO" or "IA" => new( "Iowa", value ),
					"ID" => new( "Idaho", value ),
					"IL" => new( "Illinois", value ),
					"IN" => new( "Indiana", value ),
					"KA" or "KS" => new( "Kansas", value ),
					"KE" or "KY" => new( "Kentucky", value ),
					"LO" or "LA" => new( "Louisiana", value ),
					"MA" => new( "Massachusetts", value ),
					"MD" => new( "Maryland", value ),
					"ME" => new( "Maine", value ),
					"MI" => new( "Michigan", value ),
					"MN" => new( "Minnesota", value ),
					"MO" => new( "Missouri", value ),
					"MP" => new( "Northern Mariana Islands", value ),
					"MS" => new( "Mississippi", value ),
					"MT" => new( "Montana", value ),
					"NC" => new( "North Carolina", value ),
					"ND" => new( "North Dakota", value ),
					"NE" => new( "Nebraska", value ),
					"NH" => new( "New Hampshire", value ),
					"NJ" => new( "New Jersey", value ),
					"NM" => new( "New Mexico", value ),
					"NY" => new( "New York", value ),
					"OH" => new( "Ohio", value ),
					"OK" => new( "Oklahoma", value ),
					"OR" => new( "Oregon", value ),
					"PE" or "PN" or "PA" => new( "Pennsylvania", value ),
					"PR" => new( "Puerto Rico", value ),
					"RI" => new( "Rhode Island", value ),
					"SC" => new( "South Carolina", value ),
					"SD" => new( "South Dakota", value ),
					"TN" => new( "Tennessee", value ),
					"TX" => new( "Texas", value ),
					"UM" => new( "United States Minor Outlying Islands", value ),
					"UT" => new( "Utah", value ),
					"VA" => new( "Virginia", value ),
					"VI" => new( "United States' Virgin Islands", value ),
					"VT" => new( "Vermont", value ),
					"WA" => new( "Washington", value ),
					"WI" => new( "Wisconsin", value ),
					"WV" => new( "West Virginia", value ),
					"WY" => new( "Wyoming", value ),
					_ => throw new KeyNotFoundException( $"No State could be correlated with the supplied value (\x22{value.Trim()}\x22)." ),
				};

			KeyValuePair<string, string>[] names = new KeyValuePair<string, string>[]
			{
				new( "AL", "<Alab[bam]{0,4}" ),
				new( "AK", "<Alas[ka]{0,2}" ),
				new( "AR", "<Ark[ans]{0,5}" ),
				new( "AS", " Samoa>" ),
				new( "AZ", "<Ari[zona]{0,4}" ),
				new( "CA", "<Cal[iforna]{0,7}" ), new( "CA", "fornia>" ),
				new( "CO", "<Col[orad]{0,5}" ),
				new( "CT", "<Con[neticu]{0,7}" ),
				new( "DC", "<Dist[rict]{0,4}" ), new( "DC", "Colu[mbia]{1,4}>" ),
				new( "DE", "<Dele[ware]{0,4}" ),
				new( "FL", "<Flo[rida]{0,4}" ),
				new( "GA", "<Geo[rgia]{0,4}" ),
				new( "GU", "<Guam" ),
				new( "HI", "<Haw[ai]{0,3}" ), new( "HI", "waii>" ),
				new( "IA", "<Iowa>" ),
				new( "ID", "<Idaho>" ),
				new( "IL", "<Ill[inos]{0,5}" ), new( "IL", "I.{0,3}nois>" ),
				new( "IN", "<Ind[ina]{0,4}" ),
				new( "KS", "<Kansas" ),
				new( "KY", "<Ken[tucky]{0,5}" ),
				new( "LA", "<Lou[isan]{0,6}" ),
				new( "MA", "<Mass[achusets]{0,10}" ), new( "MA", "[asch]{0,7}setts>" ),
				new( "MD", "<Mary[land]{0,4}" ),
				new( "ME", "<Maine>" ),
				new( "MI", "<Mic[higan]{1,5}" ),
				new( "MN", "<Min[nesota]{1,6}" ),
				new( "MO", "<Mis{1,2}ouri" ), new( "MO", "ouri>" ),
				new( "MP", "<North[ern ]{0,4}" ), new( "MP", "Mariana( ?I[lands.]{1,7})?>" ),
				new( "MS", "<M(is{1,2}){1,2}ip{1,2}?i?" ),
				new( "MT", "<Mon[tan]{0,4}" ),
				new( "NC", "<N[orth]{0,4}[. ]?Car[olina]{0,5}" ),
				new( "ND", "<N[orth]{0,4}[. ]?Dak[ota]{0,3}" ),
				new( "NE", "<Neb[raska]{0,5}>" ),
				new( "NH", "<(N[.]|New) ?H[ampshire]{0,8}" ), new( "NH", "H[ampshir]{3,7}e>" ),
				new( "NJ", "<(N[.]|New) ?J[ersy]{0,5}" ), new( "NJ", "J[ersy]{2,5}>" ),
				new( "NM", "<(N[.]|New) ?M[exico]{0,5}" ), new( "NM", "Me[xico]{2,4}>" ),
				new( "NY", "<(N[.]|New) ?Y[ork]{0,3}" ),
				new( "OH", "<[Ohio]{2,4}" ),
				new( "OK", "<Ok[lahom]{1,6}" ),
				new( "OR", "<Or[egon]{1,4}" ),
				new( "PA", "<Pen[nsylvani]{0,9}" ),
				new( "PR", "<Puerto ?" ), new( "PR", " ?Rico>"),
				new( "RI", "<R[hode]{2,4}" ),
				new( "SC", "<S([.]|[outh]{0,4} ?C[arolina]{0,7}" ),
				new( "SD", "<S([.]|[outh]{0,4} ?D[akot]{0,5}" ),
				new( "TN", "<Ten[nes]{0,6}" ),
				new( "TX", "<Tex[as]{0,2}" ),
				new( "UM", " ?Minor " ), new( "UM", " Outlying ?" ), new( "UM", "M[inor.]{1,4} O[utlying.]{1,7} I[sland.]{1,7}" ),
				new( "UT", "<U[tah]{2,3}" ),
				new( "VA", "<Vir[gina]{0,5}" ),
				new( "VI", " Virgin I[sland.]{0,7}>" ),
				new( "VT", "<Ver[mont]{0,4}" ),
				new( "WA", "<Wash[ington]{0,6}" ),
				new( "WI", "<Wis[consi]{0,6}" ),
				new( "WV", "<W([est]{3}|[.]) ?V[irgna]{0,7}" ),
				new( "WY", "<Wy[oming]{1,5}" ),
			};

			int i = -1;
			while ( ++i < names.Length )
			{
				string pattern = names[ i ].Value;
				pattern = (pattern[ 0 ] == '<' ? "^" : "") + $"{pattern.Substring( 1 )}";
				pattern = pattern.EndsWith( ">" ) ? pattern.TrimEnd( '>' ) + ")$" : pattern + ")";
				if ( Regex.IsMatch( value, pattern, RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase ) ) return GetCanadianProvice( names[ i ].Key );
			}

			throw new KeyNotFoundException( $"No State could be correlated with the supplied value (\x22{value.Trim()}\x22)." );
		}
		#endregion
	}

	/// <summary>Provides a facility for managing / parsing Street Addresses (strings).</summary>
	public class StreetAddress
	{
		#region Properties
		private const string PATTERN = /* language=regex */
			@"^(?<unit>(unit|suite|#|ap(partmen)?t)?[. ]*[a-z0-9]*)?[ ]+(?<nbr>(#?([0-9.]+([ ][\d]\/[\d]|[ ]?[½¾¾a-z])?)))[ ]+(?<name>[a-z][a-z'0-9.-]+ ?)(?<type>r(oa)?d|st(reet)?|av(e|enue)?|cr(es|escent)?|blvd|boulevard|pl(ace)?|row|(park)?way|pk?wy)[ ]?(?<dir>e(a?st)?|w(est)?|n(or(th|d))|s(ud|outh)?|nw|ne|sw|se|northwest|northeast|southwest|southeast)$";

		private string _street = "";

		[Flags]
		public enum Parts { None = 0, Unit = 1, Nbr = 2, Name = 4, Type = 8, Dir = 16, All = 255 };
		#endregion

		#region Constructors
		public StreetAddress( string value = "" ) => this.Raw = value;
		#endregion

		#region Operators
		public static implicit operator string( StreetAddress source ) => source is null ? "" : source.ToString();
		public static implicit operator StreetAddress( string source ) => string.IsNullOrWhiteSpace( source ) ? new() : new( source );

		public static bool operator ==( StreetAddress left, StreetAddress right )
		{
			if ( left is null ) return right is null;
			if ( right is null ) return false;

			return left.CompareTo( right ) == 0;
		}

		public static bool operator !=( StreetAddress left, StreetAddress right ) => !(left == right);

		public static bool operator >=( StreetAddress left, StreetAddress right )
		{
			if ( left is null ) return right is null;
			if ( right is null ) return true;

			return left.CompareTo( right ) >= 0;
		}

		public static bool operator <( StreetAddress left, StreetAddress right ) => !(left >= right);

		public static bool operator <=( StreetAddress left, StreetAddress right )
		{
			if ( left is null ) return right is null;
			if ( right is null ) return false;

			return left.CompareTo( right ) <= 0;
		}

		public static bool operator >( StreetAddress left, StreetAddress right ) => !(left <= right);
		#endregion

		#region Accessors
		public string this[ Parts parts ]
		{
			get
			{
				string result = "";
				var pieces = Parse();
				Parts[] partsArray = new Parts[] { Parts.Unit, Parts.Nbr, Parts.Name, Parts.Type, Parts.Dir };
				for ( int i = 0; i < partsArray.Length; i++ )
					if ( parts.HasFlag( partsArray[ i ] ) && (pieces[ i ].Length > 0) ) result += pieces[ i ] + ' ';

				return result.TrimEnd();
			}
		}

		protected string Raw
		{
			get => this._street;
			set => this._street = ValidateStreet( value ) ? value : "";
		}

		public int Length => this._street.Length;

		public string Street => this[ Parts.All ];

		public string Number
		{
			get => this[ Parts.Nbr ].ToUpperInvariant();
			set
			{
				if ( ValidatePart( value, Parts.Nbr ) )
					this._street = Compile( value, Parts.Unit, Parts.Name | Parts.Type | Parts.Dir );
				//	this._street = ($"{this[ Parts.Unit ]}{(value.Equals(string.Empty) ? "" : $" {value} ")}{this[ Parts.Name | Parts.Type | Parts.Dir ]}").Trim();
			}
		}

		public string Unit
		{
			get => this[ Parts.Unit ].UCWords( true );
			set
			{
				if ( ValidatePart( value, Parts.Unit ) )
					this._street = Compile( value, Parts.None, Parts.Nbr | Parts.Name | Parts.Type | Parts.Dir );
				//	this._street = ((value.Equals( string.Empty ) ? "" : $"{value} ") + this[ Parts.Nbr | Parts.Name | Parts.Type | Parts.Dir ]).Trim();
			}
		}

		public string Name
		{
			get => this[ Parts.Name ].UCWords( false );
			set
			{
				if ( ValidatePart( value, Parts.Name ) )
					this._street = Compile( value, Parts.Unit | Parts.Nbr, Parts.Type | Parts.Dir );
				//	this._street = ($"{this[ Parts.Unit | Parts.Nbr ]}{(value.Equals( string.Empty ) ? "" : $" {value} ")}{this[ Parts.Type | Parts.Dir ]}").Trim();
			}
		}
		public string Type
		{
			get => this[ Parts.Type ].UCWords( true );
			set
			{
				if ( ValidatePart( value, Parts.Type ) )
					this._street = Compile( value, Parts.Unit | Parts.Nbr | Parts.Name, Parts.Dir );
				//	this._street = $"{this[ Parts.Unit | Parts.Nbr | Parts.Name ]}{(value.Equals( string.Empty ) ? "" : $" {value} ")}{this[ Parts.Dir ]}".Trim();
			}
		}

		public string Direction
		{
			get
			{
				string result = this[ Parts.Name ];
				if ( result.Length == 2 ) return result.ToUpperInvariant();

				return Regex.Replace( result.Replace( " ", "" ), @"^(North|South)(East|West)$", "$1 $2", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase ).Trim().UCWords( true ).Replace( " ", "" );
			}
			set
			{
				if ( !string.IsNullOrWhiteSpace( value ) )
				{
					if ( Regex.IsMatch( value.Trim().ToUpperInvariant(), @"^([NSEW]|[NS][EW])$" ) )
					{
						this._street = Compile( value.Trim().ToUpperInvariant(), Parts.Unit | Parts.Nbr | Parts.Name | Parts.Type, Parts.None );
						return;
					}

					if ( Regex.IsMatch( value.Trim(), @"(North|South|East|West" ) )
					{
						this._street = Compile( value.Trim().ToUpperInvariant().Substring( 0, 1 ), Parts.Unit | Parts.Nbr | Parts.Name | Parts.Type, Parts.None );
						return;
					}

					if ( Regex.IsMatch( value.Trim(), @"^(North|South)[ ]?(East|West)$", RegexOptions.IgnoreCase ) )
					{
						string temp = Regex.Replace( value.Trim(), @"(North|South)[ ]?(East|West)", "$1 $2", RegexOptions.IgnoreCase ).UCWords( true );
						temp = Regex.Replace( temp, @"[^NSEW]", "" );
						this._street = Compile( value, Parts.Unit | Parts.Nbr | Parts.Name | Parts.Type, Parts.None );
					}
				}
			}
		}
		#endregion

		#region Methods
		public int CompareTo( StreetAddress b )
		{
			if ( b is null ) return -1;

			if ( this.Name.Equals( b.Name, StringComparison.OrdinalIgnoreCase ) )
			{
				// Names Match, compare numbers:
				if ( this.Number.Equals( b.Number ) )
					return this.Unit.ToLowerInvariant().CompareTo( b.Unit.ToLowerInvariant() );
				else
					return this.Number.CompareTo( b.Number );
			}

			return this.Name.CompareTo( b.Name.ToLowerInvariant() );
		}

		public override string ToString() => Regex.Replace( this[ Parts.Unit | Parts.Nbr | Parts.Name | Parts.Type | Parts.Dir ], @"[\s]{2,}", " " );

		protected string Compile( string value, Parts pre, Parts post ) =>
			((pre.Equals( Parts.None ) ? "" : this[ pre ]) + (string.IsNullOrEmpty( value ) ? "" : $" {value} ") + (post.Equals( Parts.None ) ? "" : this[ post ])).Trim();

		protected string[] Parse()
		{
			string[] result = new string[ 5 ] { "", "", "", "", "" };
			if ( ValidateStreet( this._street ) )
			{
				result = new string[ 5 ] { "unit", "nbr", "name", "type", "dir" };
				Match m = Regex.Match( this._street, PATTERN, RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase );
				if ( m.Success )
					for ( int i = 0; i < 5; i++ )
						result[ i ] = m.Groups[ result[ i ] ].Success ? m.Groups[ result[ i ] ].Value : "";
			}

			return result;
		}

		public bool Has( Parts parts )
		{
			if ( parts == Parts.None ) return false;

			var pieces = Parse();
			Parts[] partElements = new Parts[] { Parts.Unit, Parts.Nbr, Parts.Name, Parts.Type };
			for ( int i = 0; i < partElements.Length; i++ )
				if ( parts.HasFlag( partElements[ i ] ) && (pieces[ i ].Length == 0) ) return false;

			return true;
		}

		public override bool Equals( object obj ) => base.Equals( obj );

		public override int GetHashCode() => base.GetHashCode();

		public XmlNode ToXmlNode() =>
			$"<street name='{Name.XmlEncode()}'>{Raw.XmlEncode()}</street>".ToXmlNode();

		public void Import( string source ) => this.Raw = source;

		public static bool ValidateStreet( string value ) =>
			!string.IsNullOrWhiteSpace( value ) && Regex.IsMatch( value, PATTERN, RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase );

		public static bool ValidatePart( string value, Parts parts ) =>
			(value.Equals( string.Empty ) || ValidateStreet( value )) && new StreetAddress( value ).Has( parts );
		#endregion
	}

	public class MailingAddress : BasicTypedDataFoundation<MailingAddress.AddressType>
	{
		#region Properties
		protected StreetAddress _street = "";

		protected string _name = "";

		protected string _city = "";

		protected LongNameAndAbbr _province = null;

		protected LongNameAndAbbr _country = null;

		protected PostalCodeFoundation _postalCode = null;

		[Flags] public enum AddressType : byte { Unknown = 0, Home = 1, Business = 2, POBox = 128, All = 255 };
		#endregion

		#region Constructors
		protected MailingAddress( string name = "", AddressType type = AddressType.Unknown ) : base( type )
		{
			RegionInfo region = new( CultureInfo.CurrentCulture.LCID );
			this._country = new( region.NativeName, region.TwoLetterISORegionName );
			this.Name = name;
		}

		public MailingAddress( CultureInfo culture, string name, StreetAddress street, string city, string province, PostalCodeFoundation pCode = null, AddressType type = AddressType.Unknown ) : base( type )
		{
			RegionInfo region = new( culture.LCID );
			this.Name = name;
			this._country = new( region.NativeName, region.TwoLetterISORegionName );
			this._province = LongNameAndAbbr.Parse( province );
			this.Street = street;
			this.City = city;
			this.PostalCode = pCode is null ? (region.TwoLetterISORegionName.Equals( "US" ) ? new PC_UnitedStates() : new PC_Canada()) : pCode;
		}

		public MailingAddress( RegionInfo region, string name, StreetAddress street, string city, string province, PostalCodeFoundation pCode = null, AddressType type = AddressType.Unknown ) : base( type )
		{
			this.Name = name;
			this._country = new( region.NativeName, region.TwoLetterISORegionName );
			this._province = LongNameAndAbbr.Parse( province );
			this.Street = street;
			this.City = city;
			this.PostalCode = pCode is null ? region.TwoLetterISORegionName.Equals( "US" ) ? new PC_UnitedStates() : new PC_Canada() : pCode;
		}

		public MailingAddress( string name, StreetAddress street, string city, string province, string country = "", PostalCodeFoundation pCode = null, AddressType type = AddressType.Unknown ) : base( type )
		{
			RegionInfo region = new( CultureInfo.CurrentCulture.LCID );
			this.Name = name;
			this._country = string.IsNullOrWhiteSpace( country ) ? new( region.NativeName, region.TwoLetterISORegionName ) : LongNameAndAbbr.Parse( country );
			this._province = LongNameAndAbbr.Parse( province );
			this.Street = street;
			this.City = city;
			this.PostalCode = pCode is null ? region.TwoLetterISORegionName.Equals( "US" ) ? new PC_UnitedStates() : new PC_Canada() : pCode;
		}

		public MailingAddress( XmlNode source ) : base( source )
		{
			if ( !(source is null) )
			{
				this._name = source.GetAttributeValue( "name" );
				XmlNode work = source.GetFirstNamedElement( "street" );
				if ( !(work is null) )
					this.Street = new( work.InnerXml ) { Name = work.GetAttributeValue( "Name" ) };

				work = source.GetFirstNamedElement( "City" );
				if ( !(work is null) )
					this.City = work.InnerXml;

				work = source.GetFirstNamedElement( "Province" );
				if ( !(work is null) )
					this.Province = LongNameAndAbbr.Parse( work.InnerXml );

				work = source.GetFirstNamedElement( "Country" );
				if ( !(work is null) )
					this.Country = LongNameAndAbbr.Parse( work.InnerXml );

				work = source.GetFirstNamedElement( "PostalCode" );
				if ( !(work is null) )
					this.PostalCode = new PC_Canada( work.InnerXml );
			}
		}
		#endregion

		#region Accessors
		public LongNameAndAbbr Province
		{
			get => this._province is null ? new() : this._province;
			set => this._province = value;
		}

		public LongNameAndAbbr Country
		{
			get => this._country;
			set { if ( !(value is null) ) this._country = value; }
		}

		public StreetAddress Street
		{
			get => this._street;
			set => this._street = value is null ? new() : value;
		}

		public PostalCodeFoundation PostalCode { get; set; } = null;

		public string City
		{
			get => this._city;
			set
			{
				if ( string.IsNullOrWhiteSpace( value ) || !Regex.IsMatch( value.Trim(), @"^[a-zA-Z][a-zA-Z .']+$" ) )
					throw new FormatException( $"The supplied City name doesn't appear to be valid (\"{value}\")." );

				this._city = value.Trim();
			}
		}

		public string Name
		{
			get => this._name;
			set
			{
				value = Regex.Replace( value, @"[^\x20-\xfe]", "", RegexOptions.IgnoreCase );
				this._name = string.IsNullOrWhiteSpace( value ) ? "" : value;
			}
		}
		#endregion

		#region Methods
		public override string ToString()
		{
			string result = Street.Unit.Length > 0 ? $"{Street.Unit},\r\n" : "";
			result += Street[ StreetAddress.Parts.Nbr | StreetAddress.Parts.Name | StreetAddress.Parts.Type | StreetAddress.Parts.Dir ] + "\r\n";
			result += $"{City}, {Province.Abbr}, {Country.Name}\r\n{PostalCode}";

			return result;
		}

		public override XmlNode ToXmlNode() => 
		base.ToXmlNode( 
			$"<address name='{Name.XmlEncode()}'>{Street.Street.XmlEncode()}" +
			$"<city>{City.XmlEncode()}</city>" +
			$"<province>{Province.ToString().XmlEncode()}</province>" +
			$"<country>{Country.ToString().XmlEncode()}</country>" +
			$"<postalCode>{PostalCode}</postalCode></address>"
		);

		public override bool IsEmpty() =>
			((this._street is null) || string.IsNullOrWhiteSpace( this._street.ToString() )) &&
			string.IsNullOrWhiteSpace( this._name ) && string.IsNullOrWhiteSpace( this._city ) &&
			((this._province is null) || (this._province.ToString() == "")) &&
			((this._country is null) || (this._country.ToString() == "")) &&
			(this._postalCode.Length == 0);
		#endregion
	}

	public class MailingAddresses : BasicTypedCollection<MailingAddress, MailingAddress.AddressType>
	{
		#region Properties
		#endregion

		#region Constructors
		public MailingAddresses( bool sorted = false, int limit = int.MaxValue ) : base( sorted, limit ) { }

		public MailingAddresses( MailingAddress email, bool sorted = false, int limit = int.MaxValue ) : base( sorted, limit ) =>
			Add( email, sorted );

		public MailingAddresses( IEnumerable<MailingAddress> addresses, bool sorted = false, int limit = int.MaxValue ) : base( sorted, limit ) =>
			AddRange( addresses, sorted );

		public MailingAddresses( XmlNode node ) : base( node )
		{
			if ( !(node is null) && node.HasChildNodes )
			{
				foreach ( XmlNode child in node.ChildNodes )
					this.Add( new MailingAddress( child ) );
			}
		}
		#endregion

		#region Operators
		public static implicit operator MailingAddresses( MailingAddress[] data ) =>
			data is null ? new MailingAddresses() : new MailingAddresses( data );

		public static implicit operator MailingAddress[]( MailingAddresses data ) =>
			(data is null) ? Array.Empty<MailingAddress>() : data.ToArray();
		#endregion

		#region Accessors
		#endregion

		#region Methods
		protected override int Comparer( MailingAddress a, MailingAddress b ) =>
			string.Compare( a.ToString(), b.ToString(), true );

		public override XmlNode ToXmlNode() => base.CreateXmlNode();
		#endregion
	}

	/// <summary>This class was found on StackOverflow and facilitates getting/managing a list of known/supported Countries and Cultures.</summary>
	/// <see cref="https://stackoverflow.com/a/49313331/1542024"/>
	public sealed class CountryList
	{
		#region Properties
		private CultureTypes _allCultures;
		#endregion

		#region Constructors
		public CountryList( bool AllCultures = false )
		{
			this._allCultures = AllCultures ? CultureTypes.AllCultures : CultureTypes.SpecificCultures;
			this.Countries = GetAllCountries( this._allCultures );
		}
		#endregion

		#region Accessors
		public List<CountryInfo> Countries { get; set; }
		#endregion

		#region Methods
		public List<CountryInfo> GetCountryInfoByName( string CountryName, bool NativeName ) =>
			(NativeName) ? this.Countries.Where( info => info.Region.NativeName == CountryName ).ToList()
						 : this.Countries.Where( info => info.Region.EnglishName == CountryName ).ToList();

		public List<CountryInfo> GetCountryInfoByName( string CountryName, bool NativeName, bool IsNeutral ) =>
			(NativeName) ? this.Countries.Where( info => info.Region.NativeName == CountryName &&
														 info.Culture.IsNeutralCulture == IsNeutral ).ToList()
						 : this.Countries.Where( info => info.Region.EnglishName == CountryName &&
														 info.Culture.IsNeutralCulture == IsNeutral ).ToList();
		public string GetTwoLettersName( string CountryName, bool NativeName )
		{
			CountryInfo country = (NativeName) ? this.Countries.Where( info => info.Region.NativeName == CountryName ).FirstOrDefault()
											   : this.Countries.Where( info => info.Region.EnglishName == CountryName ).FirstOrDefault();

			return country is not null ? country.Region.TwoLetterISORegionName : string.Empty;
		}

		public string GetThreeLettersName( string CountryName, bool NativeName )
		{
			CountryInfo country = (NativeName) ? this.Countries.Where( info => info.Region.NativeName.Contains( CountryName ) ).FirstOrDefault()
											   : this.Countries.Where( info => info.Region.EnglishName.Contains( CountryName ) ).FirstOrDefault();

			return country is not null ? country.Region.ThreeLetterISORegionName : string.Empty;
		}

		public List<string> GetIetfLanguageTag( string CountryName, bool NativeName ) =>
			(NativeName) ? this.Countries.Where( info => info.Region.NativeName == CountryName )
												.Select( info => info.Culture.IetfLanguageTag ).ToList()
						 : this.Countries.Where( info => info.Region.EnglishName == CountryName )
												.Select( info => info.Culture.IetfLanguageTag ).ToList();

		public List<int> GetRegionGeoId( string CountryName, bool NativeName ) =>
			(NativeName) ? this.Countries.Where( info => info.Region.NativeName == CountryName )
												.Select( info => info.Region.GeoId ).ToList()
						 : this.Countries.Where( info => info.Region.EnglishName == CountryName )
												.Select( info => info.Region.GeoId ).ToList();

		private static List<CountryInfo> GetAllCountries( CultureTypes cultureTypes )
		{
			List<CountryInfo> Countries = new();

			foreach ( CultureInfo culture in CultureInfo.GetCultures( cultureTypes ) )
			{
				if ( culture.LCID != 127 )
					Countries.Add( new CountryInfo()
					{
						Culture = culture,
						Region = new RegionInfo( culture.TextInfo.CultureName )
					} );
			}
			return Countries;
		}
		#endregion

		#region Dependent Class -> CountryInfo
		public class CountryInfo
		{
			public CultureInfo Culture { get; set; }
			public RegionInfo Region { get; set; }
		}
		#endregion
	}
}
