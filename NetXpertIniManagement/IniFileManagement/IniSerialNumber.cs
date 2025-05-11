using System.Security.Cryptography;
using System.Text.RegularExpressions;
using NetXpertExtensions;

namespace IniFileManagement.Values
{
	public sealed class IniSerialNumber
	{
		#region Properties
		private readonly byte[] _data = new byte[ 128 ];
		#endregion

		#region Constructors
		public IniSerialNumber() => this._data = CreateSerialNbr();

		public IniSerialNumber( string base64Value ) =>
			this._data = ByteParse( base64Value );

		public IniSerialNumber( byte[] rawSerialNbr )
		{
			if ( !ValidateSerialNbr( rawSerialNbr ) )
				throw new FormatException( "This is not a valid serial number!" );

			this._data = rawSerialNbr;
		}
		#endregion

		#region Accessors
		public byte[] AsBytes => (byte[])this._data.Clone();
		#endregion

		#region Methods
		public override string ToString() => this._data.ToBase64String();

		public IniSerialNumber Clone() => new( this.AsBytes );

		/// <summary>Creates a properly-constructed serial number.</summary>
		/// <param name="offset">Allows manual designation of the offset value. Valid range: 0 - 95 (0x00 - 0x5f)</param>
		/// <returns>An array of 128 bytes specifying the serial number for the file.</returns>
		/// <remarks>Not specifying, or using an invalid value for the offset causes a random one to be generated.</remarks>
		public static byte[] CreateSerialNbr( sbyte offset = -1 )
		{
			byte[] serialNbr = RandomNumberGenerator.GetBytes( 128 );
			offset = (offset >= 0x00) && (offset < 0x60) ? offset : (sbyte)(96.Random() & 0x7f);
			serialNbr[ 8 ] = (byte)offset;
			serialNbr[ offset++ ] = 0x1d; // 29;
			serialNbr[ offset ] = 0x45;   // 69;
			return serialNbr;
		}

		private static bool ValidateSerialNbr( byte[] serialNbr )
		{
			if ( (serialNbr is null) || (serialNbr.Length != 128) ) return false;
			byte offset = serialNbr[ 8 ];
			return  (offset < 96) && (serialNbr[ offset++ ] == 29) && (serialNbr[ offset ] == 69);
		}

		private static byte[] ByteParse( string source )
		{
			ArgumentException.ThrowIfNullOrWhiteSpace( source );
			if (!IniLineSerialNoValue.SerialNumberBase64Validator_Rx().IsMatch( source ))
				throw new FormatException( "The supplied string is not a valid Base64 Serial Number!" );

			byte[] data = source.FromBase64String();
			if (!ValidateSerialNbr( data ))
				throw new FormatException( "This is not a valid serial number!" );

			return data;
		}

		public static IniSerialNumber Parse( string source ) => new( ByteParse( source ) );

		public static bool TryParse( string source, out IniSerialNumber? serialNumber )
		{
			serialNumber = null;
			if (string.IsNullOrWhiteSpace( source )) return false;
			try { serialNumber = Parse( source ); return true; }
			catch (ArgumentException) { }
			catch (FormatException) { }
			return false;
		}
		#endregion
	}

	public sealed partial class IniLineSerialNoValue : IniLineValueTranslator<IniSerialNumber>
	{
		#region Constructors
		public IniLineSerialNoValue( IniFileMgmt root ) : base( root ) { }

		public IniLineSerialNoValue( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
			: base( root, QuoteTypes.DoubleQuote, quoteChars ) { }

		public IniLineSerialNoValue( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, QuoteTypes.DoubleQuote, customQuotes )
		{
			if (!Validate( value )) throw CantParseException();
			base.Value = Parse( value );
		}

		public IniLineSerialNoValue( IniFileMgmt root, IniSerialNumber value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, QuoteTypes.DoubleQuote, customQuotes ) { }
		#endregion

		#region Methods
		protected override IniSerialNumber? Parse( string value ) => IniSerialNumber.Parse( value );

		protected override Regex ValidateSource() => SerialNumberBase64Validator_Rx();

		[GeneratedRegex( @"^([A-Z\d+/]{4}){42}[A-Z\d+/]{4}$|^([A-Z\d+/]{4}){42}[A-Z\d+/]{2}==$|^([A-Z\d+/]{4}){42}[A-Z\d+/]{3}=$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture )]
		public static partial Regex SerialNumberBase64Validator_Rx();
		#endregion
	}

}
