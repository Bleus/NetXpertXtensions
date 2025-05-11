using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using NetXpertExtensions;

namespace IniFileManagement.Values
{
	public sealed class IniEncryptionKey
	{
		#region Properties
		private readonly byte[] _key = new byte[32];

		public const string CONV_MAP = "0AB7CD9EF2GH3LMN4PQR5STU6VWX6YZ9"; // J, I, O and 1 are missing!

		public readonly IniSerialNumber _serialNbr;
		#endregion

		#region Constructors
		public IniEncryptionKey( IniSerialNumber serialNbr )
		{
			this._serialNbr = serialNbr;
			this.Key = null;
		}

		public IniEncryptionKey( byte[] key, IniSerialNumber serialNbr )
		{
			this._serialNbr = serialNbr;
			this.Key = key;
		}

		//public IniEncryptionKey( params ulong[] key )
		//{
		//	if (key is null || key.Length < 4)
		//		throw new ArgumentException( "You must provide 4 values to use this constructor!" );

		//	this.Key = GenerateBinaryKey( key );
		//}

		public IniEncryptionKey( string source, IniSerialNumber serialNbr )
		{
			this._serialNbr = serialNbr;

			if (string.IsNullOrWhiteSpace( source ) || source == "00000000-00000000-00000000-00000000-00000000-00000000-00")
				this._key = GenerateBinaryKey(); // Creates a new random value!
			else if (!TryParse( source, serialNbr, out var parsed ))
				throw new FormatException( "The provided encryption key format is invalid." );
			else
				this.Key = parsed.Key;
		}
		#endregion

		#region Accessors
		public byte[] Key
		{
			get => this._key;
			set
			{
				value ??= GenerateBinaryKey();
				if (value.Length < 32 )
				{
					byte[] result = GenerateBinaryKey();
					// Copy the values we WERE given, into the new, full, key...
					for ( int i = 0; i < value.Length; i++ ) result[i] = value[i];
					// Replace the supplied value with one of the correct length.
					value = result;
				}

				// Copy the values to the internal key (b/c '_key' is a ReadOnly property, and can't be re-assigned!):
				for (int i = 0; i < 32; i++) 
					this._key[i] = value[i];
			}
		}

		public IniSerialNumber SerialNbr => this._serialNbr.Clone();
		#endregion

		#region Methods
		private byte[] GetAes256Key()
		{
			if (this._serialNbr is null)
				throw new InvalidOperationException( "Serial number has not been initialized." );

			byte[] serNo = this.SerialNbr.AsBytes;
			byte[] result = new byte[32];
			// Because the serial number is only 128 bytes long, use "& 0x7F" to only capture the lowest 7-bits!
			for (int i = 0; i < 32; i++)
				result[ i ] = serNo[ _key[ i ] & 0x7F ]; 

			return result;
		}

		/// <summary>Reversibly-encrypts a supplied string using a supplied 256-bit key.</summary>
		/// <param name="plainValue">The string to encrupt with this key.</param>
		/// <param name="serialNumber">The 128-byte serial number of the Ini file.</param>
		/// <remarks><b>NOTE</b>: Uses AES256 for reversible encruption.</remarks>
		/// <returns>A Base64 string containing the binary representation of the encoded string.</returns>
		public string Encrypt( string plainValue )
		{
			byte[] iv = new byte[ 16 ];
			byte[] array;

			using (Aes aes = Aes.Create())
			{
				aes.Key = GetAes256Key();
				aes.IV = iv;

				ICryptoTransform encryptor = aes.CreateEncryptor( aes.Key, aes.IV );

				using MemoryStream memoryStream = new();
				using CryptoStream cryptoStream = new( memoryStream, encryptor, CryptoStreamMode.Write );
				using (StreamWriter streamWriter = new( cryptoStream ))
					streamWriter.Write( plainValue );

				array = memoryStream.ToArray();
			}

			return Convert.ToBase64String( array );
		}

		/// <summary>Attempts to decrypt a Base64-encoded string created by <seealso cref="Encrypt(string, byte[])"/>.</summary>
		/// <param name="encryptedValue">A string containing the Base64-encoded string to decrypt.</param>
		/// <param name="serialNumber">The 128 byte serial number of the IniFile.</param>
		/// <returns>A plaintext string containing the result of decrypting the source string with the provided key.</returns>
		public string Decrypt( string encryptedValue )
		{
			byte[] iv = new byte[ 16 ];
			byte[] buffer = Convert.FromBase64String( encryptedValue );

			using Aes aes = Aes.Create();
			aes.Key = GetAes256Key();
			aes.IV = iv;
			ICryptoTransform decryptor = aes.CreateDecryptor( aes.Key, aes.IV );

			using MemoryStream memoryStream = new( buffer );
			using CryptoStream cryptoStream = new( memoryStream, decryptor, CryptoStreamMode.Read );
			using StreamReader streamReader = new( cryptoStream );

			return streamReader.ReadToEnd();
		}

		private static void Extract5BitValues( ReadOnlySpan<byte> input, ref Span<byte> output )
		{
			if (input.Length != 32) throw new ArgumentException( $"Input must be exactly 32 bytes (actual length: {input.Length}).", nameof(input) );
			if (output.Length != 48) throw new ArgumentException( $"Output must be exactly 48 bytes (actual length: {output.Length}).", nameof(output) );

			int bitBuffer = 0;
			int bitCount = 0;
			int outputIndex = 0;

			foreach (byte b in input)
			{
				bitBuffer = (bitBuffer << 8) | b;
				bitCount += 8;

				while (bitCount >= 5 && outputIndex < 48)
				{
					bitCount -= 5;
					output[ outputIndex++ ] = (byte)((bitBuffer >> bitCount) & 0b11111);
				}
			}

			// Optional: pad with last bits if needed
			if (outputIndex < 48 && bitCount > 0)
				output[ outputIndex ] = (byte)((bitBuffer << (5 - bitCount)) & 0b11111);
		}

		private static void Pack5BitValues( ReadOnlySpan<byte> input, ref byte[] output )
		{
			if (input.Length < 48)
				throw new ArgumentException( "Input must contain at least 48 5-bit values.", nameof( input ) );

			if (output.Length < 30)
				throw new ArgumentException( "Output buffer must be at least 30 bytes long.", nameof( output ) );

			const int totalBits = 240;
			int inputLength = input.Length;

			for (int i = 0, bitIndex = 0; i < 30; i++)
			{
				int byteValue = 0;

				for (int b = 0; b < 8 && bitIndex < totalBits; b++, bitIndex++)
				{
					int srcByte = bitIndex / 5;
					int srcBitOffset = bitIndex % 5;

					if (srcByte >= inputLength)
						break;

					if ((input[ srcByte ] & (1 << (4 - srcBitOffset))) != 0)
						byteValue |= (1 << (7 - b));
				}

				output[ i ] = (byte)byteValue;
			}
		}

		public override string ToString()
		{
			const int encodedLength = 53; // 6*8 chars + 5 hyphens = 53
			Span<char> output = stackalloc char[ encodedLength ];

			// Allocate temp buffer for 48 5-bit values
			Span<byte> indexes = stackalloc byte[ 48 ];
			Extract5BitValues( this._key, ref indexes );

			int outIdx = 0;
			for (int group = 0; group < 6; group++)
			{
				for (int i = 0; i < 8; i++)
					output[ outIdx++ ] = CONV_MAP[ indexes[ group * 8 + i ] ];

				if (group < 5)
					output[ outIdx++ ] = '-';
			}

			// Final 4 characters are the last 2 bytes in hex
			return new string( output ) + $"-{this._key[ 30 ]:X2}{this._key[ 31 ]:X2}"; ;
		}

		public static IniEncryptionKey Parse( string input, IniSerialNumber? serNbr )
		{
			ArgumentNullException.ThrowIfNull( input, nameof(input) );
			ArgumentNullException.ThrowIfNull( serNbr, nameof(serNbr) );

			if (!KeyFormatRegex.IsMatch( input )) throw new FormatException( "The supplied key is not valid!" );

			Span<byte> indexes = stackalloc byte[ 48 ];
			ReadOnlySpan<char> map = CONV_MAP;

			int idx = 0;
			for (int i = 0, pos = 0; i < 6; i++, pos += 9)
				for (int j = 0; j < 8; j++)
				{
					char c = input[ pos + j ];
					int mapIndex = map.IndexOf( c );
					if (mapIndex < 0) throw new FormatException( $"An unrecognized character '{c}' was encountered at position {pos+j}!" );

					indexes[ idx++ ] = (byte)mapIndex;
				}

			// Reassemble the original 30 bytes from the 48 5-bit chunks
			byte[] work = new byte[ 32 ];
			Pack5BitValues( indexes, ref work );

			// Parse the 2-byte hex suffix
			ReadOnlySpan<char> hex = input.AsSpan( 54, 4 );
			if (!byte.TryParse( hex[ ..2 ], NumberStyles.HexNumber, null, out work[ 30 ] ) ||
						!byte.TryParse( hex[ 2.. ], NumberStyles.HexNumber, null, out work[ 31 ] ))
				throw new FormatException( "The supplied key could not be parsed." );

			return new IniEncryptionKey( work, serNbr );
		}

		public static bool TryParse( string input, IniSerialNumber serNbr, out IniEncryptionKey? result )
		{
			result = null;
			if (!string.IsNullOrWhiteSpace( input ))
				try 
				{
					result = Parse( input, serNbr );
					return true;
				}
				catch ( FormatException ) { }

			return false;
		}

		private static readonly Regex KeyFormatRegex = new( $"^[{CONV_MAP}]{8}(-[{CONV_MAP}]{8}){5}-[0-9A-F]{4}$", RegexOptions.Compiled | RegexOptions.CultureInvariant );

		/// <summary>Merges an array of (at least 8) bytes into a single <see cref="ulong"/> value.</summary>
		/// <param name="input">The (minimum of 8) byte array to assemble.</param>
		/// <remarks><b>NOTE</b>: Any bytes supplied in <paramref name="input"/>, beyond the 8th, are ignored.</remarks>
		/// <returns>
		/// If <paramref name="input"/> is <i>null</i>, or contains less than 8 bytes, a randomly-generated <see cref="ulong"/> 
		/// value.<br/>Otherwise, a new <see cref="ulong"/> value assembled from the supplied byte array.
		/// </returns>
		private static ulong CreateULong( params byte[]? input )  =>
			( input is null || input.Length < 8 ) ?
				(((ulong)int.MaxValue.Random()) << 32) | (uint)int.MaxValue.Random()
			:
				(((ulong)input[0] << 56)) | 
				((ulong)(input[1] << 48)) | 
				((ulong)(input[2] << 40)) | 
				((ulong)(input[3] << 32)) | 
				((ulong)(input[4] << 24)) | 
				((ulong)(input[5] << 16)) | 
				((ulong)(input[6] << 8)) | 
				input[7];

		/// <summary>Creates a new random, key.</summary>
		private static byte[] GenerateBinaryKey()
		{
			byte[] randomBytes = new byte[ 32 ];
			RandomNumberGenerator.Fill( randomBytes );
			return randomBytes;
		}
		#endregion
	}

	public sealed class IniLineEncrKeyValue : IniLineValueTranslator<IniEncryptionKey>
	{
		#region Constructors
		public IniLineEncrKeyValue( IniFileMgmt root ) : base( root ) { }

		public IniLineEncrKeyValue( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
			: base( root, quoteType, quoteChars ) { }

		public IniLineEncrKeyValue( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes )
		{
			if (!Validate( value )) throw CantParseException();
			base.Value = IniEncryptionKey.Parse( value, Root.SerialNbr );
		}

		public IniLineEncrKeyValue( IniFileMgmt root, IniEncryptionKey value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, value, quoteType, customQuotes ) { }
		#endregion

		#region Methods
		protected override IniEncryptionKey? Parse( string data ) => throw new NotImplementedException();

		public IniEncryptionKey? Parse( string value, IniSerialNumber serNo ) =>
			IniEncryptionKey.Parse( value, serNo );

		protected override Regex ValidateSource() =>
			new( $"^([{IniEncryptionKey.CONV_MAP}]{7}-){6}[{IniEncryptionKey.CONV_MAP}]{2}$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant );
		#endregion
	}
}
