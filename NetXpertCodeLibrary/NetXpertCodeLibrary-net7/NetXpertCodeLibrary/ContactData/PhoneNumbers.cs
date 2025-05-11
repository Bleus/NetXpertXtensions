using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using NetXpertCodeLibrary.Extensions;
using NetXpertExtensions;

namespace NetXpertCodeLibrary.ContactData
{
	/// <summary>Implements a class for managing North-American-Numbering (NAN) system phone numbers.</summary>
	/// <remarks>NAN phone numbers come in the form: +ccc (aaa) eee-nnnn where:<br/>'c' is the country code,<br/>'a' is the area code,<br/>'e' is the exchange id,<br/>and<br/>'n' is the phone number within the exchange.</remarks>
	public sealed class PhoneNumber : BasicTypedDataFoundation<PhoneNumber.PhoneType>
	{
		#region Properties
		[Flags] public enum CompareUsing : byte { Nbrs = 0, Base = 1, Exchange = 2, AreaCode = 4, Basic = 7, CountryCode = 8, Extension = 16, Exact = 255 }
		[Flags] public enum PhoneType : byte { Unknown = 0, Home = 1, Work = 2, Mobile = 4, Fax = 8, Data = 16, All = 255 };

		private const string DEFAULT_VALIDATION_STRICT_PATTERN = /* language=regex */
			@"(?<phoneNbr>(?<country>[+]?[01]+)?[-. ]?(?<areaCode>[(]?[2-9][\d]{2}[)]?)[-. ]?(?<exchange>[23456789][\d]{2})[-. ]?(?<base>[\d]{4})[ \t]*(?<exBreak>[ex#t.,]*)?(?<ext>[\d,#*]{2,})?)";
		private const string DEFAULT_VALIDATION_BASIC_PATTERN =  /* language=regex */
			@"(?<phoneNbr>(?<country>[+]?[01]+)?[-. ]?(?<areaCode>\(?[2-9a-z][\da-z]{2}\)?)?[-. ]?(?<exchange>[23456789a-z][\da-z]{2})[-. ]?(?<base>[\da-z]{4})[ \t]*(?<exBreak>[ex#t.,]*)?(?<ext>[\d,#*a-z]{2,})?)";
		private const RegexOptions DEFAULT_OPTIONS = RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant;

		private string _base;
		private string _exchange;
		private string _areaCode;
		private string _countryCode;
		private string _extBreak;
		private string _extension;
		#endregion

		#region Constructors
		public PhoneNumber( string source = null, PhoneType type = PhoneType.Unknown ) : base( type )
		{
			if ( string.IsNullOrWhiteSpace( source ) )
			{
				this._base = "0000";
				this._exchange = "000";
				this._areaCode = "000";
				this._countryCode = "0";
				this._extension = "";
				this._extBreak = "";
			}
			else
				this[ "phoneNumber" ] = source.Trim();
		}

		public PhoneNumber( XmlNode source ) : base( source )
		{
			string work = source.InnerText.XmlDecode();
			if ( IsPhoneNumber( work ) )
				this[ "phoneNumber" ] = work;
			else
			{
				this._base = "0000";
				this._exchange = "000";
				this._areaCode = "000";
				this._countryCode = "0";
				this._extension = "";
				this._extBreak = "";
			}
		}
		#endregion

		#region Operators
		public static bool operator !=( PhoneNumber left, PhoneNumber right ) => !(left == right);
		public static bool operator ==( PhoneNumber left, PhoneNumber right )
		{
			if ( left is null ) return (right is null);
			if ( right is null ) return false;
			return left.Equals( right, CompareUsing.Base | CompareUsing.Exchange | CompareUsing.AreaCode );
		}

		public static bool operator ==( PhoneNumber left, string right ) => (left == Parse( right ));
		public static bool operator !=( PhoneNumber left, string right ) => !(left == Parse( right ));

		public static bool operator ==( string left, PhoneNumber right ) => (right == left);
		public static bool operator !=( string left, PhoneNumber right ) { return !(right == left); }

		public static implicit operator string( PhoneNumber source ) => source is null ? "" : source.ToString();
		public static implicit operator PhoneNumber( string source ) => new( source );
		public static implicit operator ulong( PhoneNumber source ) =>
			source is null ? 0 : ulong.Parse( source[ CompareUsing.Basic ] );
		public static implicit operator PhoneNumber(ulong source) => new( source.ToString() );
		#endregion

		#region Accessors
		public string this[ string part ]
		{
			get
			{
				if ( !string.IsNullOrWhiteSpace( part ) )
					switch ( part.Trim().ToLower() )
					{
						case "base":
							return this.Base;
						case "exc":
						case "exchange":
							return this.Exchange;
						case "area":
						case "areacode":
							return this.AreaCode;
						case "country":
						case "countrycode":
							return this.CountryCode;
						case "ext":
						case "extension":
						case "x":
						case "xt":
							return this.ExtensionBreak + this.Extension;
					}
				return this.ToString();
			}

			set
			{
				if ( !string.IsNullOrWhiteSpace( part ) )
					switch ( part.Trim().ToLower() )
					{
						case "base":
							this.Base = value; break;
						case "exc":
						case "exchange":
							this.Exchange = value; break;
						case "area":
						case "areacode":
							this.AreaCode = value; break;
						case "country":
						case "countrycode":
							this.CountryCode = value; break;
						case "ext":
						case "extension":
						case "x":
						case "xt":
							this.ExtensionBreak = this.Extension; break;
						default:
							PhoneNumber v = Parse( value );
							this._base = v._base;
							this._exchange = v._exchange;
							this._areaCode = v._areaCode;
							this._countryCode = v._countryCode;
							this._extension = v._extension;
							this._extBreak = v._extBreak;
							break;
					}
			}
		}

		public string this[ CompareUsing fields ]
		{
			get
			{
				string result = "";
				foreach ( string value in Enum.GetNames( typeof( CompareUsing ) ) )
					if ( !new string[] { "Unknown", "Basic", "Exact" }.Contains( value ) )
					{
						CompareUsing field = (CompareUsing)Enum.Parse( typeof( CompareUsing ), value );
						if ( fields.HasFlag( field ) ) result += $"{this[ value ]}";
					}
				return result;
			}
		}

		public string Base
		{
			get => this._base;
			set
			{
				value = Regex.Replace( ConvertAlpha( value ), @"[^\d]", "", RegexOptions.None );
				if ( value.Length < 4 )
					throw new ArgumentException( "A NAN phone number base can only contain 4 digits." );

				this._base = (value.Length >= 4) ? value.EndString( 4 ) : value;
			}
		}

		public string Extension
		{
			get => ((this._extBreak.Length > 0 ? this._extBreak : "x") + this._extension);
			set => this._extension = Regex.Replace( ConvertAlpha( value ), @"[^\d,#*]", "", RegexOptions.None );
		}

		// Local telephone Exchange
		public string Exchange
		{
			get => this._exchange;
			set
			{
				value = Regex.Replace( ConvertAlpha( value ), @"[^0-9+]", "", RegexOptions.None );
				if ( value.Length != 3 )
					throw new ArgumentException( "A telephone exchange value can only contain 3 digits." );

				this._exchange = value;
			}
		}

		public string AreaCode
		{
			get => "(" + this._areaCode + ")";
			set
			{
				value = Regex.Replace( ConvertAlpha( value ), @"[^0-9]", "", RegexOptions.None );
				if ( !string.IsNullOrWhiteSpace( value ) )
				{
					if ( value.Length != 3 )
						throw new ArgumentException( "A NAN area code can only contain 3 digits." );

					if ( (value[ 0 ] == '0') || (value[ 0 ] == '1') )
						throw new ArgumentException( "A NAN area code cannot start with either 1 or 0." );

					this._areaCode = value;
				}
			}
		}

		public string CountryCode
		{
			get => "+" + this._countryCode;
			set
			{
				value = Regex.Replace( value, @"[^01]", "", RegexOptions.None );
				if ( string.IsNullOrWhiteSpace( value ) || (value.Length < 1) || (value.Length > 1) )
					throw new ArgumentException( "The Country Code can only be a '1' or a '0'." );

				this._countryCode = (value.Length >= 3) ? value.EndString( 3 ) : value;
			}
		}

		public string Raw
		{
			get
			{
				string result = this[ CompareUsing.CountryCode | CompareUsing.AreaCode | CompareUsing.Exchange | CompareUsing.Base ];
				if ( this._extension.Length > 0 ) result += $".{this._extension}";
				return Regex.Replace( result, @"[^\d,]", "", RegexOptions.None );
			}
		}

		public string ExtensionBreak
		{
			get => this._extBreak;
			set => this._extBreak = Regex.Replace( value, @"[^ext#.,]", "", RegexOptions.IgnoreCase ).ToLowerInvariant();
		}
		#endregion

		#region Methods
		public override string ToString()
		{
			string result = "";
			if ( (this._countryCode.Length > 0) && !Regex.IsMatch( this._countryCode, @"0+" ) )
				result = this.CountryCode + " ";

			result += (this._areaCode.Length == 3) ? $"({this._areaCode}) " : "";

			if ( (this._exchange.Length == 3) && (this._base.Length == 4) )
				result += $"{this._exchange}-{this._base}";

			if ( this._extension.Length > 0 )
				result += $" {((this._extBreak.Length > 0) ? this._extBreak : "x")}{this._extension}";

			return result;
		}

		public bool Equals( PhoneNumber left, CompareUsing compareFlags = CompareUsing.Exact )
		{
			if ( compareFlags == CompareUsing.Exact ) return this.ToString().Equals( left.ToString() );
			if ( compareFlags == CompareUsing.Nbrs ) return Raw.Equals( left.Raw );

			bool result = true;
			if ( compareFlags.HasFlag( CompareUsing.CountryCode ) ) 
				result = CountryCode.Equals( left.CountryCode, StringComparison.OrdinalIgnoreCase );

			if ( result && compareFlags.HasFlag( CompareUsing.AreaCode ) )
				result &= AreaCode.Equals( left.AreaCode, StringComparison.OrdinalIgnoreCase );

			if ( result && compareFlags.HasFlag( CompareUsing.Exchange ) )
				result &= Exchange.Equals( left.Exchange, StringComparison.OrdinalIgnoreCase );

			if ( result && compareFlags.HasFlag( CompareUsing.Base ) )
				result &= Base.Equals( left.Base, StringComparison.OrdinalIgnoreCase );

			if ( result && compareFlags.HasFlag( CompareUsing.Extension ) )
				result &= Extension.Equals( left.Extension, StringComparison.OrdinalIgnoreCase );

			return result;
		}

		public override bool Equals( object obj ) => base.Equals( (object)obj );

		public override int GetHashCode() => base.GetHashCode();

		public override bool IsEmpty() => this.ToString().Length == 0; //this.Equals( new PhoneNumber(), CompareUsing.Exact );

		public override XmlNode ToXmlNode() => base.ToXmlNode( this.ToString() );
		#endregion

		#region Comparer
		/*
		public override int Comparer<PhoneNumber>( PhoneNumber a, PhoneNumber b )
		{
			if ( a is null ) return (b is null) ? 0 : 1;
			if ( b is null ) return -1;

			string a1 = Regex.Replace( a.ToString(), @"[^0-9]", "" ), b1 = Regex.Replace( b.ToString(), @"[^0-9]", "" );
			if ( (a1.Length > 0) && (b1.Length > 0) )
				return ulong.Parse( a1 ).CompareTo( ulong.Parse( b1 ) );

			throw new ArgumentException( $"The supplied values are not enumerable (\"{a1}\" / \"{b1}\")." );
		}
		*/
		#endregion

		#region Static Methods
		/// <summary>Endeavours to parse a string into a legible NAN phone number.</summary>
		/// <param name="source">The string to try and parse.</param>
		/// <param name="pattern">The Regex pattern to use to perform the test.</param>
		/// <param name="options">The RegexOptions to apply to the pattern.</param>
		/// <returns>A PhoneNumber object populated from the provided string.</returns>
		/// <exception cref="ArgumentException">Thrown if the provided value cannot be parsed.</exception>
		/// <remarks>If the provided string is null, empty, whitespace, or "(000) 000-0000", an unpopulated PhoneNumber object is returned.</remarks>
		public static PhoneNumber Parse( string source, string pattern = DEFAULT_VALIDATION_BASIC_PATTERN, RegexOptions options = DEFAULT_OPTIONS )
		{
			if ( string.IsNullOrEmpty( source ) || (source == "(000) 000-0000") ) return new();

			ArgumentException exception = new ( $"\x22{source}\x22 is not recognized as a valid phone number." );
			if ( !string.IsNullOrWhiteSpace( source ) )
			{
				Regex validator = new( pattern, options );
				source = source.Trim();
				if ( validator.IsMatch( source ) )
				{
					PhoneNumber result = new();
					GroupCollection items = validator.Matches( source )[ 0 ].Groups;
					if ( items[ "phoneNbr" ].Success )
					{
						if ( items[ "country" ].Success ) result.CountryCode = items[ "country" ].Value;

						result.AreaCode = items[ "areaCode" ].Success ? items[ "areaCode" ].Value : "519";

						if ( items[ "exchange" ].Success ) result.Exchange = items[ "exchange" ].Value;
						if ( items[ "base" ].Success ) result.Base = items[ "base" ].Value;
						if ( items[ "exBreak" ].Success ) result.ExtensionBreak = items[ "exBreak" ].Value;
						if ( items[ "ext" ].Success ) result.Extension = items[ "ext" ].Value;
					}
					return result;
				}
			}
			throw exception;
		}

		/// <summary>Endeavours to parse a string into a legible NAN phone number.</summary>
		/// <param name="source">The string to try and parse.</param>
		/// <param name="pattern">The Regex pattern to use to perform the test.</param>
		/// <param name="options">The RegexOptions to apply to the pattern.</param>
		/// <returns>A PhoneNumber object populated from the provided string or null if the string could not be parsed.</returns>
		/// <remarks>If the provided string is null, empty, whitespace, or "(000) 000-0000", an unpopulated PhoneNumber object is returned.</remarks>
		public static PhoneNumber TryParse( string source, string pattern = DEFAULT_VALIDATION_BASIC_PATTERN, RegexOptions options = DEFAULT_OPTIONS )
		{
			try { return Parse( source, pattern, options ); } catch ( ArgumentException ) { /* ignored */ }
			return null;
		}

		/// <summary>Tries to validate a provided string as a NAN phone number (using the default pattern and options).</summary>
		/// <param name="source">The string to test.</param>
		/// <param name="pattern">The Regex pattern to use for validation.</param>
		/// <param name="options">The RegexOptions to apply to the validation test.</param>
		/// <returns></returns>
		public static bool IsPhoneNumber(
			string source,
			string pattern = DEFAULT_VALIDATION_BASIC_PATTERN,
			RegexOptions options = DEFAULT_OPTIONS
			)
		=> !string.IsNullOrWhiteSpace( source ) &&
			!string.IsNullOrWhiteSpace( pattern ) &&
			Regex.IsMatch( source, pattern, options );

		/// <summary>Takes in a string and converts all letters to the appropriate numbers per standard telephone alphabetic conversion.</summary>
		/// <param name="source">A potential string containing a telephone number with alphabetic elements.</param>
		/// <returns>The string with all letters converted to their numeric equivalent.</returns>
		public static string ConvertAlpha( string source )
		{
			source = Regex.Replace( source, @"[^a-z 0-9-+.]", "", RegexOptions.IgnoreCase );
			if ( Regex.IsMatch( source, @"([a-z])", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture ) )
			{
				source = Regex.Replace( source, @"[abc]", "2", RegexOptions.IgnoreCase );
				source = Regex.Replace( source, @"[def]", "3", RegexOptions.IgnoreCase );
				source = Regex.Replace( source, @"[ghi]", "4", RegexOptions.IgnoreCase );
				source = Regex.Replace( source, @"[jkl]", "5", RegexOptions.IgnoreCase );
				source = Regex.Replace( source, @"[mno]", "6", RegexOptions.IgnoreCase );
				source = Regex.Replace( source, @"[pqrs]", "7", RegexOptions.IgnoreCase );
				source = Regex.Replace( source, @"[tuv]", "8", RegexOptions.IgnoreCase );
				source = Regex.Replace( source, @"[wxyz]", "9", RegexOptions.IgnoreCase );
			}
			return source;
		}
		#endregion
	}

	/// <summary>Provides a mechanism for managing a collection of PhoneNumber objects with multiple keys, but unique values.</summary>
	public sealed class PhoneNumberCollection : BasicTypedCollection<PhoneNumber,PhoneNumber.PhoneType> // IEnumerator<PhoneNumber>
	{
		#region Properties
		#endregion

		#region Constructors
		public PhoneNumberCollection( bool sorted = false, int limit = int.MaxValue ) : base( sorted, limit ) { }

		public PhoneNumberCollection( PhoneNumber nbr, bool sorted = false, int limit = int.MaxValue ) : base( sorted, limit ) =>
			Add( nbr, sorted );

		public PhoneNumberCollection( IEnumerable<PhoneNumber> nbrs, bool sorted = false, int limit = int.MaxValue ) : base( sorted, limit ) =>
			AddRange( nbrs, sorted );

		public PhoneNumberCollection( XmlNode node ) : base( node ) { }
		#endregion

		#region Operators
		public static implicit operator PhoneNumberCollection( PhoneNumber[] data ) => 
			data is null ? new PhoneNumberCollection() : new PhoneNumberCollection( data );

		public static implicit operator PhoneNumber[]( PhoneNumberCollection data ) =>
			(data is null) ? Array.Empty<PhoneNumber>() : data.ToArray();
		#endregion

		#region Accessors
		#endregion

		#region Methods
		private int IndexOf( PhoneNumber nbr, PhoneNumber.CompareUsing compare = PhoneNumber.CompareUsing.Basic )
		{

			int i = -1;
			if ( !(nbr is null) )
				while ( (++i < Count) && !this[ i ].Equals( nbr, compare ) ) ;

			return (i < Count) ? i : -1;
		}

		public PhoneNumber Remove( PhoneNumber number, PhoneNumber.CompareUsing compare = PhoneNumber.CompareUsing.Basic )
		{
			PhoneNumber result = null;
			int i = IndexOf( number, compare );
			if ( i >= 0 )
			{
				result = this._data[ i ];
				this._data.RemoveAt( i );
			}

			return result;
		}

		protected override int Comparer( PhoneNumber a, PhoneNumber b ) =>
			string.Compare( a[ PhoneNumber.CompareUsing.Basic ], b[ PhoneNumber.CompareUsing.Basic ], true );

		public override XmlNode ToXmlNode() => CreateXmlNode( "phoneNbrs" );
		#endregion
	}
}

/*
namespace NetXpertCodeLibrary.ConsoleFunctions
{
	//using CobblestoneCommon;
	using System.Drawing;
	using NetXpertCodeLibrary.ContactData;

	public class PhoneNumberScreenEditField : ScreenEditField<PhoneNumber>
	{
		#region Constructors
		public PhoneNumberScreenEditField( string name, PhoneNumber data, Point location, Point workAreaHome, int size, string dataName = "", CliColor labelColor = null, CliColor dataColor = null ) :
			base( name, data, location, new Rectangle( workAreaHome.X, workAreaHome.Y, size, 1 ), dataName, dataColor, labelColor ) =>
			Initialize();

		public PhoneNumberScreenEditField( string name, PhoneNumber data, Point location, int areaX, int areaY, int size, string dataName = "", CliColor labelColor = null, CliColor dataColor = null ) :
			base( name, data, location.X, location.Y, areaX, areaY, size, dataName, dataColor, labelColor ) =>
			Initialize();

		public PhoneNumberScreenEditField( string name, PhoneNumber data, int locX, int locY, int areaX, int areaY, int size, string dataName = "", CliColor labelColor = null, CliColor dataColor = null ) :
			base( name, data, locX, locY, areaX, areaY, size, dataName, dataColor, labelColor ) =>
			Initialize();
		#endregion

		#region Accessors
		new public Regex FilterPattern
		{
			get => base.FilterPattern;
			protected set => base.FilterPattern = value;
		}

		new public Regex ValidationPattern
		{
			get => base.ValidationPattern;
			protected set => base.ValidationPattern = value;
		}

		new public bool RealTimeValidation
		{
			get => base.RealTimeValidation;
			protected set => base.RealTimeValidation = value;
		}

		new public Rectangle WorkArea
		{
			get => base.WorkArea;
			protected set => base.WorkArea = value;
		}
		#endregion

		#region Methods
		private void Initialize() // Oem1 = colon/semi-colon; Oem2 = slash/question mark
		{
			string pattern = /* language=Regex * /
				@"(?<date>(?<year>20[0-9]{2})[-/](?<month>0?[1-9]|1[0-2])[-/](?<day>[0-2]?[0-9]|3[0-1]))";

			ValidationPattern = new Regex( pattern );
			FilterPattern = new Regex( @"[^0-9a-z]", RegexOptions.IgnoreCase );
			RealTimeValidation = true;
		}

		public override Type DataType
		{
			get => base.DataType;
			set
			{
				if ( value == typeof( PhoneNumber ) )
					this._dataType = value;
				else
					base.DataType = value;
			}
		}

		public override void ProcessKeyStroke( ConsoleKeyInfo keyPressed )
		{
			if ( (keyPressed.KeyChar == '+') && (_field.InsertPosition == 0) )
			{
				_field.Insert( "+" );
				//string myValue = MyScreenValue;
				//if (InsertMode)
				//	MyScreenValue = "+" + myValue;
				//else
				//	MyScreenValue = "+" + ((myValue.Length > 1) ? myValue.Substring( 1 ) : "");
				CursorPos = new Point( CursorPos.X + 1, CursorPos.Y );
			}
			else
				base.ProcessKeyStroke( keyPressed );
		}

		public override void Write()
		{
			Point saveCurPos = CursorPos;
			string myValue = Value;
			CliColor color = IsValidData() ? DataColor : new CliColor( ConsoleColor.Black, ConsoleColor.Red );
			PhoneNumber pn = new PhoneNumber( myValue );
			ScreenEditController.WriteAt( PromptLocation, "{$1}$2:", new object[] { LabelColor, Title } );
			ScreenEditController.WriteAt( WorkArea.Location,
				"{$1}$5{B} ({$1,,<3'0'}$2{B}) {$1,,<3'0'}$3{B}-{$1,,<4'0'}$4",
				new object[] { color, pn.AreaCode.Trim( new char[] { '(', ')' } ), pn.Exchange, pn.Base, pn.CountryCode }
			);
			if ( pn.Extension.Length > 1 )
				Con.Tec( "{9} $2{$1}$3", new object[] { color, pn.ExtensionBreak, pn.Extension } );
			CursorPos = saveCurPos;
		}

		public override PhoneNumber Parse( string value ) =>
			PhoneNumber.TryParse( value );

		new public bool PatternMatch( string test ) =>
			PhoneNumber.IsPhoneNumber( test );

		public override bool IsValidData()
		{
			string myValue = Value;
			return Regex.IsMatch( myValue, @"([+][01]+)?([ -.\(]{0,2}000[\) -.]{0,2})?000[ .-]0000([ a-zA-Z]+[0-9]+)?" ) || PhoneNumber.IsPhoneNumber( Value );
		}

		public override dynamic ReadValue() =>
			new ScreenEditData<PhoneNumber>( this._dataName, Parse( Value ) );
		#endregion
	}
}
*/

