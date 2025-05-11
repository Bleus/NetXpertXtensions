using System;
using System.Text.RegularExpressions;
using NetXpertCodeLibrary.Extensions;
using NetXpertCodeLibrary.ContactData;
using System.Drawing;
using System.Xml;

namespace NetXpertCodeLibrary.ConsoleFunctions
{
	/// <summary>Provides a basic class for managing user credentials within the console.</summary>
	public sealed class UserInfo
	{
		#region Properties
		/// <summary>The user's username.</summary>
		private readonly string _userName;

		/// <summary>Stores / manages the User's contact information.</summary>
		private SimpleContactRecord _contactInfo;

		/// <summary>The user's rank for establishing what commands they can execute.</summary>
		private RankManagement _rank = new RankManagement();

		/// <summary>Stores / manages passwords.</summary>
		private PasswordCollection _history = new PasswordCollection( 5 );

		private readonly DateTime _created = DateTime.Now;

		private DateTime _updated = DateTime.Now;
		#endregion

		#region Constructors
		public UserInfo( string userName, Ranks rank = Ranks.None, SimpleContactRecord contact = null, byte historyDepth = 5 )
		{
			if ( !ValidateUserName( userName ) ) 
				throw new ArgumentException( $"The supplied username, `{userName}` isn't a valid username." );

			this._userName = userName.ToUpperInvariant();

			this._contactInfo = contact;

			this._rank = new RankManagement( rank );

			this._history = new PasswordCollection( historyDepth );
		}

		public UserInfo( string userName, string plainName, Ranks rank = Ranks.None, EmailAddress email = null, PhoneNumber phone = null, MailingAddress address = null, byte historyDepth = 5 )
		{
			if ( !ValidateUserName( userName ) )
				throw new ArgumentException( $"The supplied username, `{userName}` isn't a valid username." );

			this._rank = new RankManagement( rank );

			this._contactInfo = new SimpleContactRecord( plainName, email, address, phone );

			this._userName = userName.ToUpperInvariant();

			this._history = new PasswordCollection( historyDepth );
		}

		public UserInfo( string userName, DateTime created, string plainName = "", Ranks rank = Ranks.None, EmailAddress email = null, PhoneNumber phone = null, MailingAddress address = null, byte historyDepth = 5 )
		{
			if ( !ValidateUserName( userName ) )
				throw new ArgumentException( $"The supplied username, `{userName}` isn't a valid username." );

			this._userName = userName.ToUpperInvariant();
			this._rank = new RankManagement( rank );
			this._contactInfo = new SimpleContactRecord( plainName, email, address, phone,  created );
			this._created = DateTime.Now.Min( created );
			this._history = new PasswordCollection( historyDepth );
		}

		// Generate an anonymous user -- only available for the "DefaultUser" static methods!
		private UserInfo( Ranks useRank )
		{
			this._userName = "UNKNOWN";
			this._contactInfo = new SimpleContactRecord( "Anonymous System Account" );
			this._rank = useRank;
		}

		public UserInfo( XmlNode source )
		{
			if ( source is null )
				throw new ArgumentNullException( "You must supply an XmlNode to parse!" );

			if ( !source.Name.Equals( "userInfo", StringComparison.OrdinalIgnoreCase ) )
				throw new XmlException( $"The supplied node is not a 'userInfo' node (\x22{source.Name}\x22)." );

			if ( UserInfo.ValidateUserName( source.GetAttributeValue( "name" ) ) )
				throw new XmlException( $"The 'userInfo' node does not specify a valid user name (\x22{source.GetAttributeValue( "name" )}\x22)." );

			this._userName = source.GetAttributeValue( "name" );

			DateTime dateParser;
			if ( !source.GetAttributeValue( "created" ).TryParseMySqlDateTime( out dateParser ) )
				dateParser = DateTime.Now;
			this._created = dateParser;

			if ( !source.GetAttributeValue( "updated" ).TryParseMySqlDateTime( out dateParser ) )
				dateParser = DateTime.Now;
			this._updated = dateParser;

			XmlNode work;
			work = source.GetFirstNamedElement( "contact", "name" );
			if ( !(work is null) )
				this._contactInfo = new SimpleContactRecord( work );

			work = source.GetFirstNamedElement( "passList", "salt" );
			this._history = work is null ? new PasswordCollection(this._history.Limit) : new PasswordCollection( work );
		}
		#endregion

		#region Operators
		public static bool operator !=(UserInfo left, string right) => !(left == right);
		
		public static bool operator ==(UserInfo left, string right)
		{
			if (left is null) return string.IsNullOrEmpty(right);
			if (string.IsNullOrEmpty(right)) return false;
			return left.UserName.Equals(right, StringComparison.OrdinalIgnoreCase);
		}

		public static bool operator !=(UserInfo left, Ranks right) => !(left == right);
		
		public static bool operator ==(UserInfo left, Ranks right) =>
			(left is null) ? false : (left.Rank == right);

		public static bool operator >(UserInfo left, Ranks right) =>
			(left is null) ? false : (left.Rank > right);

		public static bool operator <(UserInfo left, Ranks right) =>
			(left is null) ? false : (left.Rank > right);

		public static bool operator <=(UserInfo left, Ranks right) =>
			(left == right) || (left < right);

		public static bool operator >=(UserInfo left, Ranks right) =>
			(left == right) || (left > right);

		public static implicit operator SimpleContactRecord(UserInfo source) =>
			source is null ? null : source._contactInfo;
		#endregion

		#region Accessors
		public string UserName => this._userName.ToUpper();

		public string FullName
		{
			get => (this._contactInfo is null) ? "" : this._contactInfo.FullName;
			set
			{
				if ( value is null )
					this._contactInfo = null;
				else
				{
					if ( this._contactInfo is null )
						this._contactInfo = new SimpleContactRecord( value, DateTime.Now );
					else
					{
						if ( !value.Equals( this._contactInfo.FullName, StringComparison.OrdinalIgnoreCase ) )
							this._contactInfo = new SimpleContactRecord( value, this._contactInfo );
					}
				}
			}
		}

		public string FirstName => (Names.Length > 0) ? Names[0] : FullName;

		public string LastName => (Names.Length > 1) ? Names[Names.Length - 1] : "";

		public RankManagement Rank => this._rank.ToRank;

		public EmailAddresses Emails => this._contactInfo is null ? new EmailAddresses() : this._contactInfo.Emails;

		public EmailAddress Email => (this._contactInfo is null) || (this._contactInfo.Emails.Count == 0) ? new EmailAddress("") : this._contactInfo.Emails[ 0 ];

		public PhoneNumberCollection Phones => this._contactInfo is null ? new PhoneNumberCollection() : this._contactInfo.PhoneNumbers;

		public PhoneNumber Phone => (this._contactInfo is null) || (this._contactInfo.PhoneNumbers.Count == 0) ? null : this._contactInfo.PhoneNumbers[ 0 ];

		public MailingAddresses Addresses => this._contactInfo is null ? new MailingAddresses() : this._contactInfo.Addresses;

		public MailingAddress Address => (this._contactInfo is null) || (this._contactInfo.Addresses.Count == 0) ? null : this._contactInfo.Addresses[ 0 ];

		public Password Password { set { if ( !(value is null) ) this._history.Add( value ); } }

		public PasswordCollection History => this._history;

		public bool Enablbed { get; set; } = true;

		public string SystemName => System.Security.Principal.WindowsIdentity.GetCurrent().Name;

		/// <summary>Reports how many past passwords are logged.<br/>Users cannot assign a new password that matches any password in the history.</summary>
		public byte HistoryCount => (byte)this._history.Count;

		public Bitmap ProfilePic => this._contactInfo is null ? SimpleContactRecord.BlankProfileImage() : this._contactInfo.Pic;

		public DateTime CreatedOn => this._created;

		public DateTime UpdatedOn
		{
			get => this._updated;
			set => this._updated = (value > this._updated) ? value.Min( DateTime.Now ) : this._updated;
		}

		private string[] Names => 
			this.FullName.Split( new char[] { ' ', '.' }, StringSplitOptions.RemoveEmptyEntries );

		public bool IsAnonymous => 
			this.UserName.Equals( "UNKNOWN", StringComparison.OrdinalIgnoreCase );
		#endregion

		#region Methods
		public override bool Equals(object obj) => base.Equals(obj);

		public override int GetHashCode() => base.GetHashCode();

		public bool CheckPassword( string password, string salt = null ) => 
			this._history.Last.CheckPassword( password, salt );

		public void SetPassword( string password, string salt = null ) =>
			this._history.Add( new Password( password, salt ), false );

		public override string ToString() => 
			$"{this.UserName} ({this.FullName}) [{this.Rank}]: {(this.Emails.Count > 0 ? this.Email : "{none given}")}";

		public XmlNode ToXmlNode()
		{
			if ( (this.UserName.Length == 0) || !SimpleContactRecord.ValidateName( this.UserName ) )
				throw new MissingFieldException( $"The username for this record isn't acceptable (\x22{this.UserName}\x22)." );

			xXmlNode result =
				$"<userInfo name='{this.UserName.XmlEncode()}' created='{this.CreatedOn.ToMySqlString()}' updated='{this.UpdatedOn.ToMySqlString()}'></userInfo>";

			result.AppendChild( this._contactInfo.ToXmlNode() );
			result.AppendChild( this._history.ToXmlNode() );

			return (XmlNode)result;
		}

		public Password CreateNewPassword( string password, string salt = null )
		{
			Password result = null;
			if ( !string.IsNullOrWhiteSpace( password ) )
			{
				salt = string.IsNullOrWhiteSpace( salt ) ? this._history.Last.Salt : salt;
				result = new Password( password, salt );
			}

			return result;
		}

		public bool HasPastPassword( string password, string salt ) => 
			this._history.HasPassword( password, salt );

		public bool HasPastPassword( Password password ) =>
			this._history.HasPassword( password );
		#endregion

		#region Static Methods
		/// <summary>Validates a User Name.</summary>
		public static bool ValidateUserName(string source)
		{
			if ( (source is null) || Regex.IsMatch( source.Trim(), @"^UNKNOWN$", RegexOptions.IgnoreCase ) ) return false;

			source = Regex.Replace( source, @"[^a-z0-9_]", "", RegexOptions.IgnoreCase );
			return Regex.IsMatch( source, @"^[a-z][a-z0-9_]{2,30}[a-z0-9]$", RegexOptions.IgnoreCase );
		}

		/// <summary>Sanity-Validates a person's actual name.</summary>
		public static bool ValidateName(string source)
		{
			if (source is null) return false;
			source = Regex.Replace(source, @"[^a-z]", "", RegexOptions.IgnoreCase );
			return Regex.IsMatch(source, @"[ ]*(?<name>[a-zA-Z]{3,24})[ ]*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		}

		/// <summary>Uses some basic Regex pattern matching to confirm the supplied string is a valid email FORMAT.</summary>
		/// <param name="source">A string containing the text to validate.</param>
		/// <returns>TRUE if the passed string matches a basic email regex pattern.</returns>
		
		// ( --> Deprecated with the implementation of the 'EmailAddress' class! <-- )

		//public static bool IsValidEmailForm(string source) =>
		//	string.IsNullOrWhiteSpace( source ) ? false :
		//		Regex.IsMatch( source,
		//		@"^([a-z0-9][\w-\.]*[\w])@((?:[a-zA-Z][\w-]*[a-zA-Z0-9]\.)+)([a-zA-Z][a-zA-Z0-9]+)$", 
		//		RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled 
		//	);

		public static UserInfo DefaultUser(Ranks useRank = Ranks.None, byte pwHistoryDepth = 5) => 
			new UserInfo( "default", "Default User", useRank, null, null, null, pwHistoryDepth );

		// The DefaultUser that's used by this system uses the lowest possible User Rank.
		public static string PromptParser(string source, PromptPrimitive parent, Ranks defaultUserRank = Ranks.None)
		{
			if (string.IsNullOrWhiteSpace( source ) && (parent is null)) return /* language=regex */ @"\$USER\[(?:[a-z]{4,})\]";

			if (source.Trim().Equals( "--help", StringComparison.OrdinalIgnoreCase ) && (parent is null))
				return @"{7,7}&bull; {6}$user{3}[{E}first{3}|{E}last{3}|{E}full{3}|{E}rank{3}|{E}user{3}|{E}email{3}|{E}name{3,rn}]";

			UserInfo user = (parent.Data is null) ? UserInfo.DefaultUser() : parent.GetDataAs<UserInfo>();
			MatchCollection matches = Regex.Matches(source, @"(?<cmd>\$user\[(?<opt>first|last|full|rank|user(?:name)?|email|name)\])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
			foreach (Match m in matches)
			{
				if (m.Groups["cmd"].Success && m.Groups["opt"].Success)
				{
					string replace = "$user[err]";
					switch (m.Groups["opt"].Value.ToLower())
					{
						case "first": replace = user.FirstName; break;
						case "last": replace = user.LastName; break;
						case "name":
						case "full": replace = user.FullName; break;
						case "user": replace = user.UserName; break;
						case "email": replace = user.Emails.Count > 0 ? user.Emails[0] : ""; break;
						case "rank": replace = user.Rank.ToString(); break;
					}
					source = source.Replace(m.Value, replace);
				}
			}
			return source;
		}
		#endregion
	}
}
