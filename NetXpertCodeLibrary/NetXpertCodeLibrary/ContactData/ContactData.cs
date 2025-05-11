using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Xml;
using NetXpertCodeLibrary.Extensions;

namespace NetXpertCodeLibrary.ContactData
{
	/// <summary>Facilitates managing a simple Contact-information record.</summary>
	public class SimpleContactRecord
	{
		#region Properties
		protected readonly string _fullName;

		protected MailingAddresses _addresses = new( false, 5 ) { XmlTag = "addresses" };

		protected EmailAddresses _emailAddrs = new( false, 5 ) { XmlTag = "emails" };

		protected readonly DateTime _created = DateTime.Now;

		protected DateTime _updated = DateTime.Now;

		protected PhoneNumberCollection _phoneNbrs = new();

		protected Bitmap _profilePhoto = null;

		/// <summary>Specifies the width and height of the stored profile photo Bitmap.</summary>
		private static readonly Size IMAGE_SIZE = new( 256, 256 );
		#endregion

		#region Constructors
		public SimpleContactRecord( string fullName, DateTime created )
		{
			if ( string.IsNullOrEmpty( fullName ) ) throw new ArgumentNullException( "The supplied name cannot be null, or empty." );

			if ( !ValidateName( fullName ) )
				throw new ArgumentException( $"The supplied name isn't recognized as valid (\"{fullName}\")." );

			this._fullName = fullName;

			this._created = DateTime.Now.Min( (DateTime)created );
		}

		public SimpleContactRecord( string fullName, EmailAddress email = null, MailingAddress address = null, PhoneNumber phone = null, DateTime? created = null )
		{
			if ( string.IsNullOrEmpty(fullName) ) throw new ArgumentNullException( "The supplied name cannot be null, or empty." );

			if ( !ValidateName( fullName ) )
				throw new ArgumentException( $"The supplied name isn't recognized as valid (\"{fullName}\")." );

			this._fullName = fullName;

			if ( !(created is null) ) this._created = DateTime.Now.Min( (DateTime)created );
			if ( !(email is null) ) this.Add( email );
			if ( !(address is null) ) this.Add( address );
			if ( !(phone is null) ) this.Add( phone );
		}

		public SimpleContactRecord( string newName, SimpleContactRecord copyDetailsFrom, DateTime? created = null )
		{
			if ( !ValidateName( newName ) )
				throw new ArgumentException( $"The supplied name isn't recognized as valid (\"{newName}\")." );

			if ( copyDetailsFrom is null )
				throw new ArgumentNullException( "You must provide a non-null source contact record." );

			this._fullName = newName;
			this._emailAddrs = copyDetailsFrom.Emails;
			this._addresses = copyDetailsFrom.Addresses;
			this._phoneNbrs = copyDetailsFrom.PhoneNumbers;
			this._created = created is null ? copyDetailsFrom.Created : DateTime.Now.Min( (DateTime)created );
			this._updated = DateTime.Now;
		}

		public SimpleContactRecord( XmlNode source = null )
		{
			if ( source is null )
				throw new ArgumentNullException( "Ths supplied XmlNode cannot be null!" );

			if ( !source.Name.Equals( "contact", StringComparison.OrdinalIgnoreCase ) )
				throw new XmlException( $"The supplied XmlNode tag isn't recognized (\x22{source.Name}\x22)." );

			if ( !source.HasAttribute( "name" ) )
				throw new MissingFieldException( "The contact name isn't specified." );

			if ( !ValidateName( source.GetAttributeValue( "name" ).XmlDecode() ) )
				throw new FormatException( $"The specified contact name isn't recognizable (\x22{source.GetAttributeValue( "name" )}\x22)." );

			this._fullName = source.GetAttributeValue( "name" ).XmlDecode();
			this._created = source.HasAttribute( "created" ) ? source.GetAttributeValue( "created" ).ParseMySqlDateTime() : DateTime.Now;
			this._updated = source.HasAttribute( "updated" ) ? source.GetAttributeValue( "updated" ).ParseMySqlDateTime() : DateTime.Now;

			XmlNode[] work = source.GetNamedElements( "phoneNbrs" );
			foreach ( XmlNode node in work )
			{
				XmlNode[] phnbrs = node.GetNamedElements( "phone" );
				foreach ( XmlNode phNode in phnbrs )
					this.Add( new PhoneNumber( phNode.InnerText ) );
			}

			work = source.GetNamedElements( "addresses" );
			foreach ( XmlNode node in work )
			{
				XmlNode[] addresses = node.GetNamedElements( "address", "name" );
				foreach ( XmlNode addrNode in addresses )
					this.Add( new MailingAddress( addrNode ) );
			}

			work = source.GetNamedElements( "emailAddresses" );
			foreach ( XmlNode node in work )
			{
				XmlNode[] emails = node.GetNamedElements( "email" );
				foreach ( XmlNode emailNode in emails )
					this.Add( new EmailAddress( emailNode.InnerText ) );
			}

			work = source.GetNamedElements( "img" );
			if ( work.Length > 0 )
			{
				string w = Regex.Replace( work[ 0 ].GetAttributeValue( "width" ), @"[^/d]", "" ), h = Regex.Replace( work[ 0 ].GetAttributeValue( "height" ), @"[^/d]", "" );
				Size sz = (w.Length > 0) && (h.Length > 0) ? new Size( int.Parse( w ), int.Parse( h ) ) : IMAGE_SIZE;
				this._profilePhoto = work[ 0 ].InnerText.FromBase64String().CreateBitmap( sz );
			}
		}
		#endregion

		#region Accessors
		public string FullName => this._fullName;

		public MailingAddresses Addresses => this._addresses;

		public PhoneNumberCollection PhoneNumbers => this._phoneNbrs;

		public EmailAddresses Emails => this._emailAddrs;

		public DateTime Created => this._created;

		public DateTime Updated
		{
			get => this._updated;
			set => this._updated = value > this._updated ? DateTime.Now.Min( value ) : this._updated;
		}

		public Bitmap Pic
		{
			get => this._profilePhoto is null ? new Bitmap( IMAGE_SIZE.Width, IMAGE_SIZE.Height ).Fill( Color.Black ) : this._profilePhoto;
			set => this._profilePhoto = value is null ? BlankProfileImage() : value.ResizeTo( IMAGE_SIZE.Width, IMAGE_SIZE.Height, Color.Black );
		}
		#endregion

		#region Methods
		public void Add( MailingAddress address ) => this.Addresses.Add( address );

		public void Add( EmailAddress email ) => this.Emails.Add( email );

		public void Add( PhoneNumber phNbr ) => this.PhoneNumbers.Add( phNbr );

		public void AddRange( IEnumerable<MailingAddress> addresses ) => this.Addresses.AddRange( addresses );

		public void AddRange( IEnumerable<EmailAddress> emails ) => this.Emails.AddRange( emails );

		public void AddRange( IEnumerable<PhoneNumber> phNbrs ) => this.PhoneNumbers.AddRange( phNbrs );

		public XmlNode ToXmlNode()
		{
			XmlNode result =$"<contact created='{Created.ToMySqlString()}' updated='{Updated.ToMySqlString()}' name='{FullName.XmlEncode()}'></contact>".ToXmlNode();

			if ( PhoneNumbers.Count > 0 ) result.AppendChild( this._phoneNbrs.ToXmlNode() );

			if ( Addresses.Count > 0 ) result.AppendChild( this._addresses.ToXmlNode() );

			if ( Emails.Count > 0 ) result.AppendChild( this._emailAddrs.ToXmlNode() );

			if ( !(this._profilePhoto is null) )
				result.AppendChild( $"<img width='{this._profilePhoto.Width}' height='{this._profilePhoto.Height}'>{this._profilePhoto.ToByteArray().ToBase64String()}</img>".ToXmlNode() );

			return result;
		}

		public static bool ValidateName( string name ) =>
			!string.IsNullOrWhiteSpace( name ) && Regex.IsMatch( name, @"^((Mrs?|Ms|Miss|Dr)[. ]+)?(([a-z'\x80-\xa5-]+[.]?( |$)))+", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.ExplicitCapture );

		/// <summary>Provides a global mechanism for generating default/blank images.</summary>
		/// <remarks>The 'default' blank image produced by this method is a 256x256-pixel square in the specified color.</remarks>
		public static Bitmap BlankProfileImage( Color color )
		{
			Bitmap result = new( IMAGE_SIZE.Width, IMAGE_SIZE.Height );
			Graphics g = Graphics.FromImage( result );
			g.Clear( color );
			return result;
		}

		/// <summary>Provides a global mechanism for generating default/blank images.</summary>
		/// <remarks>The 'default' blank image produced with this method is a black 256x256-pixel square.</remarks>
		public static Bitmap BlankProfileImage() => BlankProfileImage( Color.Black );
		#endregion
	}
}
