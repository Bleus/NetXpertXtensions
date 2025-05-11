using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using NetXpertCodeLibrary.Extensions;
using NetXpertExtensions;
using NetXpertExtensions.Xml;

namespace NetXpertCodeLibrary.ContactData
{
	/// <summary>A nice little class to facilitate simplified cryptographic password management.</summary>
	public sealed class Password
	{
		#region Properties
		private string _password = "";
		private string _salt = HARD_SALT;

		private const string HARD_SALT = " < --§¶ΦϾ";
		private readonly DateTime _created = DateTime.Now;
		#endregion

		#region Constructors
		/// <summary>Creates a one-way-hashed Password managament class.</summary>
		/// <param name="rawPassword">The <i><u>unhashed</u></i> password to hash and store in this object.<br/><br/>
		/// This value cannot be <i>null</i>, an empty string, or contain only whitespace!
		/// </param>
		/// <param name="salt">The salt value to use for the hashing algorithm.<br/><br/>
		/// If no salt is provided (or <i>null</i> is passed), a cryptographically-secure random salt will be created
		/// instead<br/>(which the calling function can then capture via the <i>Salt</i> accessor)jkkkkkkkkkkkkkkkkkkkkk.<br/><br/>
		/// If you don't want to use a salt value, you have to pass <i>String.Empty</i> (or <b>&quot;&quot;)</b>.
		/// </param>
		/// <remarks>If you have a raw/hashed password value, use the <b>Import( string, string )</b> function to incorporate it's values.</remarks>
		public Password( string rawPassword, string salt = null )
		{
			if ( string.IsNullOrWhiteSpace( rawPassword ) )
				throw new ArgumentException( "The supplied password cannot be null, empty or whitespace." );

			if ( salt is null ) salt = GenerateRandomSalt( 0x20 );

			// This was an interesting idea, but it necessarily results in Password objects that use the same password and 
			// salt nevertheless being incompatible. It also totally breaks Password based encryption/decryption!
			//else 
			//salt = Regex.Replace( salt.Trim(), @"[\s]", GenerateRandomSalt(1) ); // Replace whitespace with a crypto-safe random char.

			this._salt = string.IsNullOrWhiteSpace( salt ) ? "" : salt;
			this.RawPassword = rawPassword;
		}

		/// <summary>Attempts to create a new <i>Password</i> object from a properly constructed <i>XmlNode</i>.</summary>
		public Password( XmlNode node )
		{
			if ( (node is null) || !node.Name.Equals( "password", StringComparison.OrdinalIgnoreCase ) || !node.HasAttribute( "created" ) )
				throw new XmlException( "Ths supplied node is null, or unrecognized." );

			this._created = node.GetAttributeValue( "created" ).ParseMySqlDateTime().Min( DateTime.Now );

			XmlNode pass = node.GetFirstNamedElement( "dataBits" ), salt = node.GetFirstNamedElement( "modBits" );
			string useSalt = (salt is null) || !salt.GetAttributeValue( "order" ).Equals( "High", StringComparison.OrdinalIgnoreCase ) ? "" : salt.InnerText.Base64Decode( Encoding.UTF8 );

			if ( !(pass is null) )
			{
				Password pw = Import( pass.InnerXml, useSalt );
				this._password = pw._password;
				this._salt = pw._salt;
				return;
			}

			throw new ArgumentNullException( $"The supplied XmlNode does not contain both the data and modulo bitstreams! (\"{node.OuterXml}\")" );
		}

		private Password() { }
		#endregion

		#region Operators
		public static implicit operator Password( string source ) => new( source );
		public static implicit operator string( Password source ) => source.ToString();

		public static bool operator ==( Password left, string right )
		{
			if ( left is null ) return string.IsNullOrEmpty( right );
			if ( string.IsNullOrEmpty( right ) ) return left is null;
			return left.CheckPassword( right, left.Salt );
		}

		public static bool operator !=( Password left, string right ) => !(left == right);
		#endregion

		#region Accessors
		public string RawPassword
		{
			get => this._password;
			internal set
			{
				if ( !string.IsNullOrEmpty( value ) && string.IsNullOrEmpty( this._password ) )
					this._password = ByteArrayToBase64String( Encrypt( value, ref this._salt ) );
			}
		}

		public string Salt
		{
			get => this._salt;
			internal set
			{
				if ( !string.IsNullOrWhiteSpace( value ) && string.IsNullOrEmpty( this._salt ) )
					this._salt = value;
			}
		}

		public DateTime Created => this._created;
		#endregion

		#region Methods
		public bool CheckPassword( string password, string salt = null )
		{
			string useSalt = SanitizeSalt( salt );
			return ByteArrayToBase64String( Encrypt( password, ref useSalt ) ).Equals( this._password );
		}

		public XmlNode ToXmlNode( string tag = "password" )
		{
			if ( string.IsNullOrWhiteSpace( tag ) || !Regex.IsMatch( tag, @"^[a-zA-Z]{2,64}$" ) )
				tag = "password";

			string 
				salt =
					((this._salt.Length > 0 ? this._salt : GenerateRandomSalt( 0x20 )).Base64Encode(Encoding.UTF8)),
				result =
					$"<{tag} created='{this._created.ToMySqlString()}'><dataBits>{this._password}</dataBits>" +
					$"<modbits order='{(this._salt.Length > 0 ? "High" : "Low")}'>{salt}</modbits></{tag}>";

			return xXmlNode.Parse( result );

			//return result.ToXmlNode();
		}

		/// <summary>Takes a byte array and XOR's it with the encoded password and salt contained in this object.</summary>
		private byte[] XorData( byte[] source )
		{
			if ( !(source is null) && (source.Length > 0) )
			{
				byte[]
					pass = $"{this._password}{this._salt}".ToByteArray(),
					output = new byte[ source.Length ];

				for ( int i = 0; i < source.Length; i++ )
					output[ i ] = (byte)(source[ i ] ^ pass[ i % pass.Length ]);

				return output;
				//result = useBase64 ? Convert.ToBase64String( output ) : Encoding.UTF8.GetString(output);
			}
			return new byte[ 0 ];
		}

		/// <summary>
		/// Encrypt the given string using AES.  The string can be decrypted using 
		/// DecryptStringAES().  The sharedSecret parameters must match.
		/// </summary>
		/// <param name="plainText">The text to encrypt.</param>
		private string EncryptStringAES( string plainText )
		{
			if ( string.IsNullOrEmpty( plainText ) )
				throw new ArgumentNullException( "plainText" );

			RijndaelManaged aesAlg = null;              // RijndaelManaged object used to encrypt the data.
			byte[] outStr;

			try
			{
				// generate the key from the shared secret and the salt
				Rfc2898DeriveBytes key = new( this._password, this._salt.ToByteArray() );

				// Create a RijndaelManaged object
				aesAlg = new RijndaelManaged();
				aesAlg.Key = key.GetBytes( aesAlg.KeySize / 8 );
				aesAlg.IV = key.GetBytes( aesAlg.BlockSize / 8 );
				aesAlg.Padding = PaddingMode.Zeros;

				// Create a decryptor to perform the stream transform.
				ICryptoTransform encryptor = aesAlg.CreateEncryptor( aesAlg.Key, aesAlg.IV );

				// Create the streams used for encryption.
				using MemoryStream msEncrypt = new();
				// prepend the IV
				msEncrypt.Write( BitConverter.GetBytes( aesAlg.IV.Length ), 0, sizeof( int ) );
				msEncrypt.Write( aesAlg.IV, 0, aesAlg.IV.Length );
				using ( CryptoStream csEncrypt = new( msEncrypt, encryptor, CryptoStreamMode.Write ) )
				{
					using StreamWriter swEncrypt = new( csEncrypt );
					//Write all data to the stream.
					swEncrypt.Write( plainText );
				}
				outStr = msEncrypt.ToArray();
			}
			finally
			{
				// Clear the RijndaelManaged object.
				if ( aesAlg != null )
					aesAlg.Clear();
			}

			// Return the encrypted bytes from the memory stream.
			return Convert.ToBase64String( outStr );
		}


		/// <summary>
		/// Decrypt the given string.  Assumes the string was encrypted using 
		/// EncryptStringAES(), using an identical sharedSecret.
		/// </summary>
		/// <param name="cipherText">The text to decrypt.</param>
		private string DecryptStringAES( string rawCipherText )
		{
			if ( (rawCipherText is null) || (rawCipherText.Length == 0) )
				throw new ArgumentNullException( "rawCipherText" );

			byte[] cipherText = rawCipherText.FromBase64String();

			// Declare the RijndaelManaged object
			// used to decrypt the data.
			RijndaelManaged aesAlg = null;

			// Declare the string used to hold
			// the decrypted text.
			string plaintext = null;

			try
			{
				// generate the key from the shared secret and the salt
				Rfc2898DeriveBytes key = new( this._password, this._salt.ToByteArray() );

				// Create the streams used for decryption.                
				using MemoryStream msDecrypt = new( cipherText );

				// Create a RijndaelManaged object
				// with the specified key and IV.
				aesAlg = new RijndaelManaged();
				aesAlg.Key = key.GetBytes( aesAlg.KeySize / 8 );
				aesAlg.IV = key.GetBytes( aesAlg.BlockSize / 8 );
				aesAlg.Padding = PaddingMode.Zeros;
				// Get the initialization vector from the encrypted stream
				aesAlg.IV = ReadByteArray( msDecrypt );
				// Create a decryptor to perform the stream transform.
				ICryptoTransform decryptor = aesAlg.CreateDecryptor( aesAlg.Key, aesAlg.IV );
				using CryptoStream csDecrypt = new( msDecrypt, decryptor, CryptoStreamMode.Read );
				using StreamReader srDecrypt = new( csDecrypt );

				// Read the decrypted bytes from the decrypting stream
				// and place them in a string.
				plaintext = srDecrypt.ReadToEnd();
			}
			finally
			{
				// Clear the RijndaelManaged object.
				if ( aesAlg != null )
					aesAlg.Clear();
			}

			return plaintext;
		}

		private static byte[] ReadByteArray( Stream s )
		{
			byte[] rawLength = new byte[ sizeof( int ) ];
			if ( s.Read( rawLength, 0, rawLength.Length ) != rawLength.Length )
				throw new SystemException( "Stream did not contain properly formatted byte array" );

			byte[] buffer = new byte[ BitConverter.ToInt32( rawLength, 0 ) ];
			if ( s.Read( buffer, 0, buffer.Length ) != buffer.Length )
				throw new SystemException( "Did not read byte array properly" );

			return buffer;
		}

		/// <summary>Uses this password as a key to encrypt a provided byte array.</summary>
		/// <param name="source">A byte array containing the data to be encrypted.</param>
		/// <returns>The encrypted version of the supplied data.</returns>
		//public byte[] EncryptData( byte[] source ) => XorData( source );

		/// <summary>Uses this password as a key to decrypt a provided byte array.</summary>
		/// <param name="source">A byte array containing the encrypted data to decrypt.</param>
		/// <returns>The decrypted version of the supplied data.</returns>
		//public byte[] DecryptData( byte[] source ) => XorData( source );

		/// <summary>Uses this password as a key to encrypt a provided string.</summary>
		/// <param name="source">The plaintext source string to be encrypted.</param>
		/// <returns>A Base64-encoded representation of the encrypted string.</returns>
		public string EncryptString( string source ) =>
			string.IsNullOrWhiteSpace(source) ? "" : EncryptStringAES( source );

		/// <summary>Uses this password as a key to decrypt a provided string.</summary>
		/// <param name="source">A Base64-encoded string to decrypt.</param>
		/// <returns>The decrypted version of the source string.</returns>
		public string DecryptString( string source ) =>
			string.IsNullOrWhiteSpace( source ) ? "" : DecryptStringAES( source );

		public override string ToString()
		{
			string result = "";
			foreach ( char c in this._password )
			{
				result += c;
				if ( result.Length % 48 == 0 ) result += "\r\n";
			}
			return result;
		}

		/// <summary>Imports a hashed password.</summary>
		/// <param name="passHash">The already-hashed password to store.</param>
		/// <param name="salt">The Salt that is needed to parse the hash.</param>
		public static Password Import( string passHash, string salt = null )
		{
			Password result = new();
			result._salt = salt is null ? HARD_SALT : salt;
			result._password = passHash;
			return result;
		}

		public bool Equals( Password pw ) => 
			!(pw is null) && $"{this._password}+{this._salt}".Equals( $"{pw._password}+{pw._salt}" );

		public override bool Equals( object obj ) => base.Equals( obj );

		public override int GetHashCode() => base.GetHashCode();

		private static string SanitizeSalt( string rawSalt = null ) =>
			(rawSalt is null) || Regex.IsMatch( rawSalt, @"^[\s]+$" ) ? HARD_SALT : Regex.Replace( rawSalt, @"[\s]", GenerateRandomSalt( 1 ) );

		/// <summary>Uses the SHA512 hashing algorithm to one-way-encrypt the supplied variable-length string.</summary>
		/// <param name="password">The plain-text, unencrupted password to hash.</param>
		/// <param name="salt">The salt value to use, if desired.</param>
		/// <returns>An array of bytes containing the SHA512-hashed version of the password + salt.</returns>
		/// <remarks>If the supplied salt is null, or whitespace, HARD_SALT is substituted instead, but<br/>if the salt
		/// is 'string.Empty' (or ""), no salt will be applied!</remarks>
		private static byte[] Encrypt( string password, ref string salt )
		{
			salt = SanitizeSalt( salt );

			byte[] hashArray;
			using ( SHA512 shaM = new SHA512Managed() )
				hashArray = shaM.ComputeHash( (password + salt).ToByteArray() );

			return hashArray;
		}

		private static string ByteArrayToBase64String( IEnumerable<byte> source ) =>
			(source is null) ? "" : Convert.ToBase64String( new List<byte>( source ).ToArray() );

		/// <summary>Creates a random salt of a specified length.</summary>
		/// <param name="length">The number of characters to put into the salt.</param>
		/// <returns>A string of the specified length containing a randomly generated series of characters.</returns>
		/// <remarks>This generator uses a cryptographically-safe RNG method.<br/>The minimum salt 'length' is 4, any specified value less than 4 is ignored..</remarks>
		public static string GenerateRandomSalt( byte length )
		{
			string result = "";
			if ( length > 0 )
				foreach ( byte b in CryptoRNG.Generate<byte>( Math.Max(length, (byte)4), 0x20, 0xff ) )
					result += (char)b;

			return result;
		}

		public class DuplicatePasswordException : Exception
		{
			public DuplicatePasswordException( string message ) : base( message ) { }
		}
		#endregion
	}

	public sealed class PasswordCollection : IEnumerator<Password>
	{
		#region Properties
		private List<Password> _passwords = new();
		private int _position = 0;
		private readonly int _limit = -1;
		#endregion

		#region Constructors
		public PasswordCollection( int limit = -1 ) => this._limit = Math.Min( limit, 255 );

		public PasswordCollection( Password password, int limit = -1 )
		{
			this._limit = Math.Min( 255, limit );
			if ( !(password is null))
				this._passwords.Add( password );
		}

		public PasswordCollection( IEnumerable<Password> passwords, int limit = -1 )
		{
			this._limit = Math.Min( 255, limit );
			if ( !(passwords is null) )
				this._passwords.AddRange( passwords );
		}

		public PasswordCollection( XmlNode source )
		{
			if ( source is null ) throw new ArgumentNullException( "The supplied XmlNode cannot be null." );
			if ( source.Name.Equals( "passList" ) && source.HasAttribute( "limit") )
			{
				if ( !Regex.IsMatch( source.GetAttributeValue( "limit" ), @"^(?:[0-1]?[\d]{1,2}|2[0-4][\d]|25[0-5])$" ) )
					throw new ArgumentOutOfRangeException( $"The supplied XmlNode doesn't have a valid 'limit' value (\x22{source.GetAttributeValue( "limit" )}\x22)." );

				this._limit = int.Parse( source.GetAttributeValue( "limit" ) );
				XmlNode[] passwords = source.GetNamedElements( "password", "created" );
				if ( passwords.Length > 0 )
					foreach ( XmlNode pw in passwords )
						this.Add( new Password( pw ) );
			}
		}
		#endregion

		#region Accessors
		public Password this[ int index ] => this._passwords[ index ];

		public int Count => this._passwords.Count;

		public int Limit => (this._limit < 1) ? 0 : this._limit;

		public Password Last => this.Count > 0 ? this._passwords[ this.Count - 1 ] : null;

		public bool IsFull => (this.Limit > 0) && (this.Count == this.Limit);

		Password IEnumerator<Password>.Current => this._passwords[ this._position ];

		object IEnumerator.Current => this._passwords[ this._position ];
		#endregion

		#region Methods
		private int IndexOf( Password pw )
		{
			int i = -1;
			while ( (++i < Count) && (!this._passwords[ i ].Equals( pw )) );
			return (i < Count) ? i : -1;
		}

		public void Add( Password pw, bool suppressException = true )
		{
			int i = IndexOf( pw );
			if ( (i >= 0) && !suppressException )
				throw new Password.DuplicatePasswordException( $"The supplied password conflicts with one already used by this user (\x22{this[ i ].GetHashCode()}:{this[ i ].Created}\x22)." );

			if ( IsFull ) this._passwords.RemoveAt( 0 );
			this._passwords.Add( pw );
		}

		public void Add( string plaintextPassword, string salt = null, bool suppressException = true ) => 
			this.Add( new( plaintextPassword, salt ), suppressException );

		public void AddRange( IEnumerable<Password> passwords, bool suppressExceptions = true )
		{
			foreach ( Password pw in passwords )
				this.Add( pw, suppressExceptions );
		}

		public bool HasPassword( Password pw ) => IndexOf( pw ) >= 0;

		public bool HasPassword( string plaintextPassword, string salt = null ) => 
			IndexOf( new( plaintextPassword, salt ) ) >= 0;

		public XmlNode ToXmlNode()
		{
			xXmlNode node = $"<passList limit='{this.Limit}'></passList>";
			foreach ( Password pw in this._passwords )
				node.AppendChild( pw.ToXmlNode() );

			return node;
		}

		public Password[] ToArray() => this._passwords.ToArray();

		#region IEnumerator support
		public IEnumerator<Password> GetEnumerator() => this._passwords.GetEnumerator();

		bool IEnumerator.MoveNext() => (++this._position) < this.Count;

		void IEnumerator.Reset() => this._position = 0;
		#endregion

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		private void Dispose( bool disposing )
		{
			if ( !disposedValue )
			{
				if ( disposing )
				{
					// TODO: dispose managed state (managed objects).
				}
				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~AppletParameters() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose( true );
			// TODO: uncomment the following line if the finalizer is overridden above.
			GC.SuppressFinalize( this );
		}
		#endregion
		#endregion
	}
}
