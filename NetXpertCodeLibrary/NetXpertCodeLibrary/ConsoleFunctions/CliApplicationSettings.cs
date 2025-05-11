using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using NetXpertCodeLibrary.ConfigManagement;
using NetXpertCodeLibrary.ContactData;
using NetXpertCodeLibrary.Extensions;

namespace NetXpertCodeLibrary.ConsoleFunctions
{
	public sealed class CliRegComment
	{
		public enum Type { unknown = 0, singleLineSlash = 1, singleLineHash = 2, multiLineSlash = 3, multiLineHash = 4 };

		#region Properties
		private string _comment = "";
		private readonly Type _type = Type.unknown;
		private string _prefix = "", _suffix = "";
		#endregion

		#region Accessors
		public Type DelimiterType => _type;

		public string Comment
		{
			get => this._comment;
			set => this._comment = string.IsNullOrWhiteSpace( value ) ? "" : value;
		}
		#endregion

		#region Constructors
		public CliRegComment( Type type = Type.unknown ) =>
			this._type = type;

		public CliRegComment( string data )
		{
			CliRegComment temp = CliRegComment.Parse( data );
			this._comment = temp._comment;
			this._type = temp._type;
			this._prefix = temp._prefix;
			this._suffix = temp._suffix;
		}

		public CliRegComment( string data, Type delimiterType )
		{
			this._type = delimiterType;
			this.Comment = data;
		}

		public CliRegComment( CliRegComment copy )
		{
			this._type = copy._type;
			this._comment = copy._comment;
			this._prefix = copy._prefix;
			this._suffix = copy._suffix;
		}
		#endregion

		#region Operators
		public static implicit operator string( CliRegComment source ) => source is null ? "" : source.ToString();
		public static implicit operator CliRegComment( string source ) => new CliRegComment( source );

		public static CliRegComment operator +(CliRegComment left, string right)
		{
			if ( left is null ) return new CliRegComment( right, Type.unknown );
			if ( string.IsNullOrWhiteSpace( right ) ) return left;

			CliRegComment result = new CliRegComment( left );
			result._comment += right;
			return result;
		}

		public static CliRegComment operator +( CliRegComment left, CliRegComment right )
		{
			if ( left is null ) return new CliRegComment( right, Type.unknown );
			if ( right is null ) return left;

			CliRegComment result = new CliRegComment( left );
			result._comment += right._comment;
			return result;
		}
		#endregion

		#region Methods
		public static string PreMarker( Type delimiterType )
		{
			switch ( delimiterType )
			{
				case Type.singleLineSlash: return "//";
				case Type.multiLineSlash: return "/*";
				case Type.multiLineHash:
				case Type.singleLineHash: return "##";
			}
			return "??";
		}

		public static string PostMarker( Type delimiterType )
		{
			switch ( delimiterType )
			{
				case Type.singleLineHash:
				case Type.singleLineSlash: return "";
				case Type.multiLineSlash: return "*/";
				case Type.multiLineHash: return "##";
			}
			return "??";
		}

		public override string ToString() => ToString( this._type );

		public string ToString( Type delimiterType ) =>
			this._prefix + PreMarker( delimiterType ) + this._comment + PostMarker( delimiterType ) + this._suffix;
		
		private delegate string PatternFixPrototype( Type dT );
		
		public static CliRegComment Parse( string source )
		{
			CliRegComment result;
			PatternFixPrototype PatternFix;
			
			Type delimiterType = Type.unknown;
			if ( string.IsNullOrWhiteSpace( source ) ) return new CliRegComment();

			// Attempt to detect a Multi-line comment...
			PatternFix = ( Type dT ) =>
				/* language=regex */ @"(?<prefix>[\t ]*(?<marker>$1))(?<comment>[\t\r\n \x21-\x5b\x5d-\xfe]+)(?<suffix>(?:$2)[\s]*)".Replace( new object[] { PreMarker( dT ), PostMarker( dT ) } );

			if ( Regex.IsMatch( source, PatternFix( Type.multiLineHash ), RegexOptions.Multiline ) )
				delimiterType = Type.multiLineHash;
			else
				if ( Regex.IsMatch( source, PatternFix( Type.multiLineSlash ), RegexOptions.Multiline ) )
					delimiterType = Type.multiLineSlash;

			if ( delimiterType == Type.unknown )
			{
				// okay, so try to detect a Single-line comment...
				PatternFix = ( Type dT ) =>
					/* language=regex */ @"^(?<prefix>[\t ]*(?<marker>$1))(?<comment>[\t\x20-\x5b\x5d-\xfe]+)(?<suffix>[\t ]*)$".Replace( PreMarker( dT ) );

				if ( Regex.IsMatch( source, PatternFix( Type.singleLineHash ), RegexOptions.Singleline ) )
					delimiterType = Type.singleLineHash;
				else
					if ( Regex.IsMatch( source, PatternFix( Type.singleLineSlash ), RegexOptions.Singleline ) )
						delimiterType = Type.singleLineSlash;
			}

			if ( delimiterType != Type.unknown )
			{
				MatchCollection m = new Regex( PatternFix( delimiterType ), RegexOptions.Multiline ).Matches( source );
				if ( (m.Count > 0) && (m[ 0 ].Groups[ "comment" ].Success) )
				{
					result = new CliRegComment( delimiterType );
					if ( m[ 0 ].Groups[ "prefix" ].Success )
						result._prefix = m[ 0 ].Groups[ "prefix" ].Value;
					if ( m[ 0 ].Groups[ "suffix" ].Success )
						result._prefix = m[ 0 ].Groups[ "suffix" ].Value;

					result._comment = m[ 0 ].Groups[ "comment" ].Value;
					return result;
				}
			}

			throw new InvalidDataException( $"The supplied text could not be parsed as a comment:\r\n\"{source}\"" );
		}

		public static bool IsValidComment( string sample )
		{
			bool result = !string.IsNullOrWhiteSpace( sample );
			if ( result )
				try { Parse( sample ); } catch(InvalidDataException e) { result = false; }

			return result;
		}
		#endregion
	}

	/// <summary>Provides the most elemental, data-agnostic, Registry-entry data management functions.</summary>
	public abstract class CliRegAtom
	{
		#region Properties
		protected string _comment = "";
		protected readonly DateTime _created = DateTime.Now;
		protected DateTime _updated = new DateTime( 2000, 01, 01, 00, 00, 00, DateTimeKind.Local );
		protected string _xmlTag = "value";

		private const string COMMENT_PATTERN = /* language=regex */
			@"^(?:[\t ]*(##|\/[\/*])[ \t]*)?(?<comment>[^\x00-\x1f\x22\x5c\xff]+)(?:[\t ]*|##|\*\/)[ \t]*$";
		#endregion

		#region Constructor
		protected CliRegAtom( XmlNode node )
		{
			if ( !(node is null) && node.HasAttribute("created") )
			{
				this.XmlTag = node.Name;
				this.Comment = node.GetAttributeValue( "comment" ).XmlDecode();

				DateTime work;

				string raw = node.GetAttributeValue( "created" );
				this._created = !string.IsNullOrWhiteSpace( raw ) && raw.TryParseMySqlDateTime( out work ) ? work : DateTime.Now;

				raw = node.GetAttributeValue( "updated" );
				this._updated = !string.IsNullOrWhiteSpace( raw ) && raw.TryParseMySqlDateTime( out work ) ? work : DateTime.Now;
			}
			else
				throw new ArgumentNullException( "You must supply a valid XmlNode object to use this constructor." );
		}

		protected CliRegAtom( DateTime created ) => this._created = created.Min( DateTime.Now );

		protected CliRegAtom() => this._created = DateTime.Now;
		#endregion

		#region Accessors
		/// <summary>Gets / Sets the comment string for this object.</summary>
		public string Comment
		{
			get => this._comment;
			set { if ( IsValidComment( value ) ) this._comment = Regex.Replace( value.Trim(), COMMENT_PATTERN, "${comment}" ); }
		}

		/// <summary>Reports the DateTime when this object was created.</summary>
		public DateTime Created => this._created;

		/// <summary>Gets/Sets the Last-Updated DateTime for this object.</summary>
		/// <remarks>NOTE: If the supplied DateTime value is in the future, 'DateTime.Now' will be assigned instead!</remarks>
		public DateTime Updated
		{
			get => this._updated;
			set { if ( value > this._updated ) this._updated = DateTime.Now.Min( value ); }
		}

		/// <summary>Manages the string used to create XmlNode objects from this class.</summary>
		protected string XmlTag
		{
			get => this._xmlTag;
			set
			{
				if ( !IsValidTag( value ) )
					throw new ArgumentException( $"You must supply a valid `XmlTag` (\x22{value}\x22) to call this function." );

				this._xmlTag = value;
			}
		}
		#endregion

		#region Methods
		protected abstract string BuildGenericXmlString();

		public static bool IsValidComment( string value ) =>
			!(value is null) && (Regex.IsMatch( value, COMMENT_PATTERN ) || value.Length == 0);

		public static bool IsValidTag( string tag ) =>
			!string.IsNullOrWhiteSpace( tag ) && Regex.IsMatch( tag, @"^[a-zA-Z]{2,32}$" );
		#endregion

		#region Delegate Xml Processing functions
		public static XmlNode DataToXmlNode( CliRegAtom source ) =>
			source.BuildGenericXmlString().ToXmlNode();

		//public static CliRegAtom XmlNodeToData( XmlNode node ) { }
		#endregion
	}

	/// <summary>Provides additional functionality and type-agnostic data management features/functions.</summary>
	/// <typeparam name="T">The Type of the data that is to be managed by this class.</typeparam>
	public abstract class CliRegFoundation<T> : CliRegAtom
	{
		#region Constructor
		protected CliRegFoundation( T value = default( T ), string comment = "" )
		{
			this.Data = value;
			this.Comment = string.IsNullOrWhiteSpace( comment ) ? "" : comment;
		}

		protected CliRegFoundation( XmlNode node ) : base( node ) =>
			this.Data = ParseXmlNodeValue( node );
		#endregion

		#region Accessors
		/// <summary>Gets/Sets the plaintext Value of this object, managing encryption automatically as required.</summary>
		public T Data { get; set; } = default( T );

		/// <summary>Reports TRUE if there is a Value assigned to this object.</summary>
		public bool HasValue => !this.Data.Equals( default( T ) );
		#endregion

		#region Methods
		/// <summary>Placeholder for a function to convert a 'T' type object into a string.</summary>
		/// <remarks>NOTE: The output of the function MUST be 'Safe' for use as Xml Inner Text!</remarks>
		public abstract string Stringify( T source );

		/// <summary>Placeholder for a function that can convert the innerXml value of the provided XmlNode into a 'T' type object.</summary>
		public abstract T ParseXmlNodeValue( XmlNode source );

		protected override string BuildGenericXmlString()
		{
			if ( this._xmlTag.Length > 0 )
			{
				string result = $"<{XmlTag} created='{Created.ToMySqlString()}' updated='{Updated.ToMySqlString()}'";
				if ( Comment.Length > 0 ) result += $" comment=\x22{Comment.XmlEncode().Replace( "'", "" )}\x22";
				return $"{result}>{Stringify(this.Data)}</{XmlTag}>";
			}
			return string.Empty;
		}

		protected XmlNode ToXmlNode( params XmlAttribute[] attributes ) =>
			ToXmlNode( new List<XmlAttribute>(attributes) );

		protected XmlNode ToXmlNode( IEnumerable<XmlAttribute> attributes = null )
		{
			xXmlNode xml = BuildGenericXmlString();
			if ( !(xml is null) )
			{
				if ( !(attributes is null) )
					foreach ( XmlAttribute attr in attributes )
						xml[ attr.Name ] = attr.Value;
			}
			return (XmlNode)xml;
		}

		protected KeyValuePair<string, T> ToKeyValuePair( string name ) => 
			new KeyValuePair<string, T>( name, this.Data );
		#endregion
	}

	/// <summary>Complete Registry Data Management class for string values.</summary>
	public sealed class CliRegData : CliRegFoundation<string>
	{
		#region Properties
		private bool _encrypted = false;
		#endregion

		#region Constructors
		public CliRegData( object value = null, bool encrypted = false, string comment = "" ) : base( "", comment )
		{
			this._encrypted = encrypted;
			this.Data = (value is null) ? "" : value.ToString();
			this.XmlTag = "value";
		}

		public CliRegData( XmlNode source ) : base( source ) =>
			this._encrypted = source.GetAttributeValue( "encrypted" ) != "";
		#endregion

		#region Accessors
		/// <remarks>
		/// DON'T assign pre-encrypted data to this value! It reports and expects PLAINTEXT and manages
		/// encryption internally!
		/// </remarks>
		new public string Data
		{
			get =>
				string.IsNullOrWhiteSpace( base.Data ) ? "" : (Encrypted ? AES.DecryptStringToString( base.Data, Encoding.ASCII ) : base.Data);
			set =>
				base.Data = string.IsNullOrWhiteSpace( value ) ? "" : (Encrypted ? AES.EncryptStringToString( value, Encoding.ASCII ) : value);
		}

		/// <summary>Specifies if the data element (Value) of this class should be stored as an encrypted value.</summary>
		public bool Encrypted
		{
			get => this._encrypted;
			set
			{
				if ( value != this._encrypted )
				{
					this.Data = value ? AES.EncryptStringToString( this.Data ) : AES.DecryptStringToString( this.Data );
					this._encrypted = value;
				}
			}
		}
		#endregion

		#region Methods
		public XmlNode ToXmlNode() =>
			base.ToXmlNode( new KeyValuePair<string, object>( "encrypted", this._encrypted ).ToXmlAttribute() );

		public override string Stringify( string source ) => source.XmlEncode();

		public override string ParseXmlNodeValue( XmlNode source ) => source.InnerXml.XmlDecode();

		public override string ToString() =>
			$"Type: {{string}}; value=\x22{base.Data}\x22;{(this._encrypted ? " √" : "")}";
		#endregion
	}

	/// <summary>The basic CLI User data object.</summary>
	public sealed class CliRegUser : CliRegFoundation<UserInfo>
	{
		#region Properties
		private CliHive _data = new CliHive( CliRegistry.Hive.User, "User" );
		#endregion

		#region Constructors
		public CliRegUser( UserInfo value = null, string comment = "" ) : base( value, comment ) =>
			this.XmlTag = "user"; // value.UserName;

		public CliRegUser( XmlNode source ) : base( source ) { }
		#endregion

		#region Accessors
		public string UserName => this.Data.UserName;

		public string FullName => this.Data.FullName;

		public string FirstName => this.Data.FirstName;

		public string LastName => this.Data.LastName;

		public RankManagement Rank => this.Data.Rank;

		public EmailAddresses Emails => this.Data.Emails;

		public MailingAddresses Addresses => this.Data.Addresses;

		public PhoneNumberCollection PhoneNbrs => this.Data.Phones;

		public PasswordCollection PastPasswords => this.Data.History;

		public CliHive Settings => this._data;
		#endregion

		#region Operators
		public static implicit operator CliRegUser(UserInfo source) =>
			source is null ? null : new CliRegUser( source, "" );

		public static implicit operator UserInfo(CliRegUser source) =>
			source is null ? null : source.Data;
		#endregion

		#region Methods
		public override string Stringify( UserInfo source ) => source.ToXmlNode().OuterXml;

		public override UserInfo ParseXmlNodeValue( XmlNode source ) => new UserInfo( source );

		public XmlNode ToXmlNode()
		{
			XmlNode result = base.ToXmlNode();
			result.ImportNode( this._data.ToXmlNode() );
			return result;
		}

		public bool CheckPassword( string password, string salt = null ) => 
			this.Data.CheckPassword( password, salt );

		public void SetPassword( string password, string salt = null ) => 
			this.Data.SetPassword( password, salt );
		#endregion
	}

	/// <summary>The Registry DataTree class for managing data (string) objects.</summary>
	public sealed class CliRegObject : DataTree<CliRegData>
	{
		#region Properties
		//protected const string NAME_PATTERN = @"(?:^\[(?:" + GROUP_NAME_ROOT + @")\])";
		#endregion

		#region Constructors
		public CliRegObject( string name, object value = null, string comment = "", bool encrypt = false, CliRegObject parent = null ) : 
			base( name, new CliRegData( value, encrypt, comment ), parent ) { }

		//public CliRegObject( IniLineItem item, CliRegObject parent = null ) : 
		//	base( item.Key, new CliRegData( item.Value, item.Encrypted, item.Comment ), parent) { }

		//public CliRegObject( IniGroupItem group, CliRegObject parent = null ) : 
		//	base( group.Name, new CliRegData( null, group.Encrypted, group.Comment ), parent ) =>
		//	this.AddRange( group.ToArray() );

		public CliRegObject( XmlNode source, DataTree<CliRegData> parent = null ) : 
			base( source, source => new CliRegData(source), parent ) { }

		/*
			protected CliRegFoundation( IniLineItem item )
			{
				if ( item is null )
					throw new ArgumentNullException( "The supplied IniLineItem value cannot be null!" );

				this._value = item.Value;
				this._encrypted = item.Encrypted;
				this.Comment = item.Comment;
			}

			protected CliRegFoundation( IniGroupItem group )
			{
				if ( group is null )
					throw new ArgumentNullException( "The supplied IniGroupItem value cannot be null!" );

				this.Comment = group.Comment;
			}

			protected CliRegFoundation( XmlNode node )
			{
				if ( !(node is null) && node.HasAttribute( "name" ) )
				{
					this.Comment = node.GetAttributeValue( "comment" ).XmlDecode();

					string name = node.GetAttributeValue( "name" ), raw;
					if ( !string.IsNullOrWhiteSpace( name ) && Regex.IsMatch( name, @"^[a-zA-Z][\w]*[a-zA-Z0-9]$" ) )
					{
						this._name = name;
						DateTime work;

						raw = node.GetAttributeValue( "created" );
						this._created = !string.IsNullOrWhiteSpace( raw ) && raw.TryParseMySqlDateTime( out work ) ? work : DateTime.Now;

						raw = node.GetAttributeValue( "updated" );
						this._updated = !string.IsNullOrWhiteSpace( raw ) && raw.TryParseMySqlDateTime( out work ) ? work : DateTime.Now;

						XmlNode value = node.GetFirstNamedElement( "value" );
						if ( !(value is null) )
						{
							this._encrypted = value.GetAttributeValue( "encrypted" ).Equals( "true", StringComparison.OrdinalIgnoreCase );
							this._value = node.InnerText.XmlDecode();
						}

						return;
					}
					throw new ArgumentException( $"The specified XmlNode attribute name, could not be found, or is not acceptable (\"{name}\")." );
				}
				throw new ArgumentNullException( "You must supply a valid XmlNode object to use this constructor." );
			}
		 */
		#endregion

		#region Operators
		public static implicit operator KeyValuePair<string,string>(CliRegObject source) =>
			source is null ? new KeyValuePair<string, string>("","") : new KeyValuePair<string, string>( source.Name, source.Data );
		public static implicit operator CliRegObject(KeyValuePair<string,string> source) => new CliRegObject( source.Key, source.Value );
		#endregion

		#region Accessors
		new public CliRegObject this[ TreeGroupChain path ] => (CliRegObject)Get( path );

		new public string Data
		{
			get => base.Data.Data;
			set => base.Data.Data = string.IsNullOrWhiteSpace( value ) ? "" : value;
		}

		public CliRegData BaseData => base.Data;

		public string Comment
		{
			get => base.Data.Comment;
			set => base.Data.Comment = value;
		}

		public bool Encrypted
		{
			get => base.Data.Encrypted;
			set => base.Data.Encrypted = value;
		}

		public DateTime Created => base.Data.Created;

		public DateTime Updated
		{
			get => base.Data.Updated;
			set => base.Data.Updated = value;
		}
		#endregion

		#region Methods
		protected override DataTree<CliRegData> CreateInstance( string name, CliRegData value = null, DataTree<CliRegData> parent = null ) =>
			new CliRegObject( name, value, "", false, (CliRegObject)parent );

		protected override DataTree<CliRegData> CreateInstance( XmlNode source, XmlNodeToData xmlParser, DataTree<CliRegData> parent = null ) =>
			new CliRegObject( source, (CliRegObject)parent );

		public bool Has( TreeGroupChain name ) => !(Get( name ) is null);

		public T As<T>( string groupChain = "" )
		{
			TreeGroupChain gc = new TreeGroupChain( groupChain );
			if ( gc.Length == 0 )
			{
				if ( this.HasValue )
				{
					if ( typeof( T ) == typeof( DateTime ) )
						return (T)(this.Data.ParseMySqlDateTime() as object);

					string value = this.Data;
					if ( typeof( T ) != typeof( string ) )
					{
						if (
							CryptoRNG.IsIntegerType<T>() ||
							(typeof( T ) == typeof( decimal )) ||
							(typeof( T ) == typeof( float )) ||
							(typeof( T ) == typeof( double ))
						)
							value = string.IsNullOrWhiteSpace( this.Data ) || !Regex.IsMatch( this.Data, @"^[+-]?[\d]+(?:[.][\d]*)?$" ) ? "0" : this.Data;

						if ( typeof( T ) == typeof( bool ) )
							value = Regex.IsMatch( this.Data.Trim(), @"^(?:y[es]{0,2}|on|1|t[rue]{0,3})$", RegexOptions.IgnoreCase ).ToString();
					}

					return (T)Convert.ChangeType( value, typeof( T ) );
				}
				return default( T );
			}

			if ( gc.Count > 0 )
			{
				CliRegObject obj = (CliRegObject)Get( gc );
				if ( obj is null )
					throw new KeyNotFoundException( $"The requested key, \x22{groupChain}\x22 was not found." );

				return obj.As<T>();
			}

			throw new FormatException( $"The supplied path is not a valid GroupChain (\x22{groupChain}\x22)." );
		}

		private CliRegData GetValue( string groupChain )
		{
			CliRegObject result = this[ groupChain ];
			return (result is null) ? null : result.BaseData;
		}

		public bool SetValue( TreeGroupChain groupChain, CliRegObject value )
		{
			CliRegObject obj = this[ groupChain ];
			if ( obj is null ) return false;

			obj.Merge( value );
			return true;
		}

		public bool SetValue( TreeGroupChain groupChain, string value, bool encrypt = false, string comment = "" )
		{
			CliRegObject target = this[ groupChain ];
			if ( target is null ) return false;

			target.Merge( new CliRegObject( groupChain.Last, value, comment, encrypt ) );
			return true;
		}

		public void AddItem( IniLineItem item ) =>
			this.AddItem( new CliRegObject( item, this ) );

		public void AddItem( string name, object value = null, string comment = "", bool encrypt = false ) =>
			this.AddItem( new CliRegObject( name, value, comment, encrypt, this ) );

		public void AddItem( IniGroupItem group )
		{
			CliRegObject newGroup = new CliRegObject( group.Name, this );
			newGroup.AddRange( group.ToArray() );
			this.AddItem( newGroup );
		}

		public void AddRange( IEnumerable<IniLineItem> items )
		{
			foreach ( IniLineItem item in items )
				this.AddItem( item );
		}

		public void AddRange( IEnumerable<CliRegObject> groups ) =>
			this.AddRange( groups );

		public void AddRange( IEnumerable<string> names, bool encrypted = false )
		{
			foreach ( string s in names )
				this.AddItem( new CliRegObject( s, "", "", encrypted, this ) );
		}

		/// <summary>Recursively merges a supplied group into this group.</summary>
		public void Merge( CliRegObject newGroup )
		{
			if ( !(newGroup is null) )
			{
				this.Data = newGroup.Data;
				foreach ( CliRegObject group in newGroup )
				{
					int i = IndexOf( group.Name );
					if ( i < 0 )
						this.AddItem( group.Clone( this.Name, this ) );
					else
						this[ i ].Merge( group );
				}
			}
		}

		public void ImportNode( CliRegObject node, bool destructive = true ) => base.ImportNode( node, destructive );

		public CliRegObject Rehome( CliRegObject newParent ) =>
			(CliRegObject)base.Rehome( newParent );

		new public CliRegObject Rehome( TreeGroupChain groupChain ) =>
			(CliRegObject)base.Rehome( groupChain );

		new public CliRegObject Remove( string name ) => (CliRegObject)base.Remove( name );

		/// <summary>Creates an exact-copy CliGroup but "re-home" it to the specified parent group.</summary>
		/// <remarks>This facilitates a non-destructive object-copy to a separate group / hive.</remarks>
		public CliRegObject Clone( string name = "", CliRegObject parent = null ) =>
			(CliRegObject)base.Clone( name, parent );

		public IniLineItem ToIniLineItem( bool enabled = true ) =>
			this.HasValue ? new IniLineItem( this.Name, this.Data, this.Encrypted, this.Comment, enabled ) : null;

		/// <remarks>
		/// BEWARE: IniGroupItem objects CANNOT nest, so only groups with VALUES will be added to the result of this
		/// operation. Sub-groups will disappear!
		/// </remarks>
		public IniGroupItem ToIniGroupItem()
		{
			IniGroupItem result = new IniGroupItem( this.Name );
			if ( this.HasValue )
				result.Add( this.ToIniLineItem() );

			foreach ( CliRegObject group in this )
				if ( group.HasValue )
					result.Add( group.ToIniLineItem( true ) );

			return result;
		}

		public XmlNode ToXmlNode() => base.ToXmlNode( CliRegAtom.DataToXmlNode );

		public string ToXmlString() => ToXmlNode().OuterXml;

		public KeyValuePair<string, string> ToKeyValuePair() => 
			new KeyValuePair<string, string>( Name, base.Data.Data );

		public override string ToString() =>
			$"{this.Path}: {(this.Data.Length > 0 ? $"\x22{this.Data}\x22 " : "")}(Groups: {Count})";
		#endregion
	}

	/// <summary>The Class for managing a data (string) object hive.</summary>
	/// <remarks>
	/// Hive.User CliHives are intended to hold and manage KeyValuePair&lt;string,string&gt; style registry data!<br/>
	/// Actual User information is contained in an independent CliUserHive object within the user's CliRegUser object.
	/// </remarks>
	/// <seealso cref="CliUserHive"/>
	public sealed class CliHive : DataTreeRoot<CliRegData>
	{
		#region Properties
		private readonly CliRegistry.Hive _hive;
		#endregion

		#region Constructors
		public CliHive( CliRegistry.Hive hiveType, string name = null ) 
			: base( string.IsNullOrWhiteSpace(name) ? $"{hiveType}" : name, $"{hiveType}Hive", "group" ) =>
			this._hive = hiveType;

		public CliHive( CliRegistry.Hive hiveType, string name = null, params CliRegObject[] nodes ) 
			: base( string.IsNullOrWhiteSpace( name ) ? $"{hiveType}" : name, $"{hiveType}Hive", "group")
		{
			this._hive = hiveType;
			if ( !(nodes is null) && (nodes.Length > 0) )
				foreach ( var node in nodes )
					base.ImportNode( node, true );
		}

		public CliHive( CliRegistry.Hive hiveType, params CliRegObject[] nodes ) : base( $"{hiveType}", $"{hiveType}Hive", "group" )
		{
			this._hive = hiveType;
			if ( !(nodes is null) && (nodes.Length > 0) )
				this.AddRangeOfItems( nodes );
		}

		public CliHive( XmlNode source ) : base( source, source => new CliRegData( source ), "", "group" )
		{
			if ( !source.Name.Equals("hive", StringComparison.OrdinalIgnoreCase) || !Regex.IsMatch( source.GetAttributeValue( "Name" ), @"(User|App|System)Hive" ) )
				throw new ArgumentException( $"The supplied XmlNode isn't a recognized Hive (\x22{source.OuterXml.Replace( source.InnerXml, "..." )}\x22)." );

			this._hive = (CliRegistry.Hive)Enum.Parse( typeof( CliRegistry.Hive ), source.Name.Replace( "Hive", "" ) );
		}
		#endregion

		#region Accessors
		public CliRegistry.Hive HiveType => this._hive;

		new public CliRegObject this[ TreeGroupChain path ] => (base[ path ] as CliRegObject);
		#endregion

		#region Methods
		public XmlNode ToXmlNode() => base.ToXmlNode( source => source is null ? null : source.ToXmlNode() );

		public void ImportNode( params CliRegObject[] nodes )
		{
			foreach( CliRegObject node in nodes )
				if ( !(node is null) ) base.AddItem( node );
				else
					throw new ArgumentNullException( "You must supply a non-null 'node' to add!" );
		}

		protected override DataTree<CliRegData> CreateInstance( string name, CliRegData value = null, DataTree<CliRegData> parent = null ) =>
			new CliRegObject( name, value, "", false, null );

		protected override DataTree<CliRegData> CreateInstance( XmlNode source, XmlNodeToData xmlParser, DataTree<CliRegData> parent = null ) =>
			new CliRegObject( source, parent );

		/*
		public void Import( CliRegObject node, bool destructive = true ) => base.ImportNode( node, destructive );

		public void Import( IEnumerable<CliRegObject> nodes, bool destructive = true )
		{
			if ( !(nodes is null) )
				foreach ( var node in nodes )
					base.ImportNode( node, destructive );
		}

		public void Import( params CliRegObject[] nodes, bool destructive = true )
		{
			if ( !(nodes is null) && (nodes.Length > 0) )
				foreach (var node in nodes )
					base.ImportNode( node, destructive );
		}

		public static CliHive Parse( XmlNode source )
		{
			CliRegistry.Hive hive;
			CliHive result = null;
			if ( !(source is null) && source.Name.Equals( "hive" ) && source.HasAttribute( "name" ) )
			{
				string hiveTypeString = source.GetAttributeValue( "name" );
				if (Regex.IsMatch( hiveTypeString, @"([a-z][\w]*[a-z\d])Hive", RegexOptions.IgnoreCase ))
				{
					hiveTypeString = hiveTypeString.Substring( 0, hiveTypeString.Length - 4 );
					hive = (CliRegistry.Hive)Enum.Parse( typeof( CliRegistry.Hive ), hiveTypeString );
					result = new CliHive( hive );

					XmlNode[] nodes = source.GetNamedElements( "group", "name" );
					foreach ( XmlNode node in nodes )
						result._groups.Add( new CliGroup( hive, node ) );

					if ( source.HasAttribute( "comment" ) ) result.Comment = source.Attributes[ "comment" ].Value.Trim();
				}
			}
			return result;
		} */
		#endregion
	}

	/// <summary>The Class for managing the UserData Hive.</summary>
	public sealed class CliUserHive : IEnumerator<CliRegUser>
	{
		#region Properties
		private List<CliRegUser> _users = new List<CliRegUser>();
		private readonly CliRegUser _defaultUser = null;
		private int _position = 0;
		private int _currentUser = -1;
		private readonly DateTime _created;
		private readonly Guid _uid;
		#endregion

		#region Constructors
		public CliUserHive( UserInfo defaultUser = null )
		{
			this._defaultUser = (defaultUser is null) ? new CliRegUser( UserInfo.DefaultUser( Ranks.None ) ) : defaultUser;
			this._uid = Guid.NewGuid();
			this._created = DateTime.Now;
		}

		public CliUserHive( XmlNode source )
		{
			if (
				!(source is null) && source.HasAttribute( "created" ) && source.HasAttribute( "serial" ) &&
				source.Name.Equals( "users", StringComparison.OrdinalIgnoreCase )
				)
			{
				this._users.Clear();
				DateTime dateParser;

				this._created = source.GetAttributeValue( "created" ).TryParseMySqlDateTime( out dateParser ) ? dateParser : DateTime.Now;
				this._uid = Guid.Parse( source.GetAttributeValue( "serial" ) );

				XmlNode work = source.GetFirstNamedElement( "default" );
				this._defaultUser = (work is null) ? new CliRegUser( UserInfo.DefaultUser() ) : new CliRegUser( work );

				if ( !this.Import( source.GetFirstNamedElement( "data" ) ) )
					throw new XmlException( "The supplied node's user data is indecipherable." );
			}
			else
				throw new XmlException( "The supplied XmlNode isn't recognizable as a user hive." );
		}
		#endregion

		#region Accessors
		public CliRegUser this[ int index ] => this._users[ index ];

		public CliRegUser this[ string userName ] => Find( userName );

		///<summary>Reports the serial number of this object.</summary>
		///<remarks>Every new instance of this class receives a new unique SerialNumber. 
		///This function allows apps to read the serial number and lock their data to a
		///specific instance of the UserHive.</remarks>
		public string SerialNbr => this._uid.ToString("B");

		public int CurrentUserId
		{
			get => this._currentUser;
			private set => this._currentUser = value.InRange( Count, 0, null ) ? value : -1;
		}

		public CliRegUser DefaultUser => this._defaultUser;

		public CliRegUser CurrentUser => (this._currentUser < 0) ? null : this[ this._currentUser ];

		public CliHive Settings => this._currentUser < 0 ? new CliHive( CliRegistry.Hive.User ) : this[ this._currentUser ].Settings;

		/// <summary>Reports the number of users currently being managed.</summary>
		public int Count => this._users.Count;

		public string Name => "Users";

		CliRegUser IEnumerator<CliRegUser>.Current => this._users[ this._position ];

		object IEnumerator.Current => this._users[ this._position ];
		#endregion

		#region Operators
		#endregion

		#region Methods
		private int IndexOf( string name )
		{
			int i = -1;
			if ( !string.IsNullOrWhiteSpace( name ) )
				while ( (++i < Count) && !this._users[ i ].UserName.Equals( name, StringComparison.OrdinalIgnoreCase ) ) ;

			return i < Count ? i : -1;
		}

		public bool DeleteUser( UserInfo user ) => DeleteUser( user.UserName );

		public bool DeleteUser( string userName )
		{
			int i = IndexOf( userName );

			if ( i >= 0 )
			{
				if ( i == this._currentUser )
					throw new InvalidOperationException( $"You cannot delete the currently active user! (\x22{CurrentUser.UserName}\x22)" );

				this._users.RemoveAt( i );
			}
			// If the deletion has put the '_currentUser' pointer out of range, set it to -1 (no current user selected)
			if ( this._currentUser >= Count ) this._currentUser = -1;

			return i >= 0;
		}

		public bool HasUser( UserInfo user ) => IndexOf( user.UserName ) >= 0;

		public bool HasUser( string userName ) => IndexOf( userName ) >= 0;

		public bool AddUser( UserInfo newUser, string comment = "" ) =>
			AddUser( new CliRegUser( newUser, comment ) );

		public bool AddUser( CliRegUser user )
		{
			int i = IndexOf( user.Data.UserName );
			if ( i < 0 )
			{
				this._users.Add( user );
				this._users.Sort( ( a, b ) => a.UserName.CompareTo( b.UserName ) );
			}

			return ( i < 0 );
		}
		
		public CliRegUser Find( string name )
		{
			int i = IndexOf( name );
			return i < 0 ? null : this._users[ i ];
		}

		public bool SetCurrentUser( string name )
		{
			int i = IndexOf( name );
			if ( i >= 0 )
				this._currentUser = i;

			return (i >= 0);
		}

		public bool UpdateUser( string name, UserInfo settings )
		{
			int i = IndexOf( name );
			if ( i >= 0 )
				this._users[ i ].Data = settings;

			return (i >= 0);
		}

		public bool Logon( string userName, string password, string salt = null )
		{
			if ( this._currentUser < 0 )
			{
				int i = IndexOf( userName );
				if ( (i >= 0) && this._users[ i ].CheckPassword( password, salt ) )
				{
					this._currentUser = i;
					return true;
				}
				return false;
			}
			throw new InvalidOperationException( $"You can't change the current user while an existing user is already active! (\x22{CurrentUser.UserName}\x22)" );
		}

		public void Logoff() => this._currentUser = -1;

		public XmlNode ToXmlNode()
		{
			xXmlNode wrapper = "<data></data>";
			foreach ( CliRegUser user in this )
				wrapper.AppendChild( user.ToXmlNode() );

			string xml = $"<users created='{this._created.ToMySqlString()}' serial='{SerialNbr}'><default>{this._defaultUser.ToXmlNode().OuterXml}</default></users>";
			xXmlNode result = xXmlNode.Parse( xml );
			// Encrypts the plaintext content of the 'wrapper' node:
			wrapper.InnerText = new Password( SerialNbr, result["created"] ).EncryptString( wrapper.InnerXml );
			result.AppendChild( wrapper );

			return result;
		}

		private bool Import( XmlNode source )
		{
			if ( !(source is null) && source.Name.Equals( "data", StringComparison.OrdinalIgnoreCase ) )
			{
				string rawData = new Password( SerialNbr, this._created.ToMySqlString() ).DecryptString( source.InnerText );
				if ( rawData.IsXml() )
				{
					xXmlNode[] nodes = rawData.ToXmlNode().GetNamedElements( "user", "name" );
					foreach ( xXmlNode node in nodes )
						this._users.Add( new CliRegUser( node ) );
					return true;
				}
			}
			return false;
		}

		public override string ToString() => $"UserDb: [{Count} Users]";

		#region IEnumerator support
		public IEnumerator<CliRegUser> GetEnumerator() => this._users.GetEnumerator();

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

	public sealed class CliRegistry : IDisposable
	{
		#region Properties
		private string _lastSaved = "", _fileName = "";
		private CliHive _applicationHive = new CliHive( Hive.App, "AppHive" );
		private CliHive _systemHive = new CliHive( Hive.System, "SystemHive" );
		private CliUserHive _userDb = new CliUserHive( UserInfo.DefaultUser() );

		public enum Hive { User, App, System };
		#endregion

		#region Constructors
		//public CliRegistry( string fileName ) => Load( fileName );

		public CliRegistry() { }
		#endregion

		#region Accessors
		public CliHive this[ Hive hive ]
		{
			get
			{
				switch ( hive )
				{
					case Hive.App: return this._applicationHive;
					case Hive.System: return this._systemHive;
					case Hive.User:
						return HasActiveUser ? this._userDb.CurrentUser.Settings : DefaultRegistry()._userDb.Settings;
				}
				return null;
			}
		}

		public UserInfo this[ string userName ]
		{
			get
			{
				// Provides a mechanism to retrieve any specified user's data without giving out any password information.
				UserInfo info = this._userDb.HasUser( userName ) ? this._userDb[ userName ] : null;
				return info is null ? null : 
					new UserInfo(
						info.UserName,
						info.Rank,
						(SimpleContactRecord)info,
						(byte)info.History.Limit
					);
			}
		}

		public string Name => $"{ExecutableName} Registry";

		public string SerialNbr => this._userDb.SerialNbr;

		public bool IsSaved =>
			!string.IsNullOrEmpty( this._fileName ) &&
			this._lastSaved.Equals( this.ToString() ) &&
			File.Exists( this._fileName );

		private static string ExecutableName =>
			Path.GetFileNameWithoutExtension( Process.GetCurrentProcess().MainModule.FileName );

		private static string HomePath =>
			Path.GetDirectoryName( Process.GetCurrentProcess().MainModule.FileName ) + "\\";

		public string FileName => _fileName;

		public string LastSaved => _lastSaved;

		public CliRegUser CurrentUser => this._userDb.CurrentUser;

		public bool HasActiveUser => !(CurrentUser is null);
		#endregion

		#region Methods
		public void Save( string fileName = null )
		{
			if ( string.IsNullOrWhiteSpace( fileName ) )
				fileName = HomePath + ExecutableName + ".cfg";

			// TODO: Validate that the incoming string is a valid path+file name

			xXmlNode xml = $"<CliRegistry last='{DateTime.Now}'></CliRegistry>";
			xml.ImportNodes( this._systemHive.ToXmlNode(), this._applicationHive.ToXmlNode() ); //, this._userDb.ToXmlNode() );
			xml.AppendChild( this._userDb.ToXmlNode() );
			byte[] data = TextCompress.Compress( xml.OuterXml );

			if ( File.Exists( fileName ) ) File.Delete( fileName );
			File.WriteAllBytes( fileName, data );

			this._lastSaved = xml[ "last" ];
			this._fileName = fileName;
		}

		public bool ValidateUser( string userName, string password, string salt = null ) =>
			UserInfo.ValidateUserName( userName ) && 
			!string.IsNullOrWhiteSpace( password ) && 
			this._userDb[ userName ].CheckPassword( password, salt );

		public void Logoff() => this._userDb.Logoff();

		public bool Logon( string userName, string password, string salt = "" ) =>
			this._userDb.Logon( userName, password, salt );

		public void Backup( byte version = 0 )
		{
			if ( !string.IsNullOrWhiteSpace( this._fileName ) && File.Exists( this._fileName ) )
			{
				CliRegistry backup = CliRegistry.Load( this._fileName );
				string fileName = Regex.Replace( this._fileName, @"^(.+)[.]cfg$", $"$1.cfg({version})", RegexOptions.IgnoreCase );
				backup.Save( fileName );
				File.SetAttributes( fileName, FileAttributes.Hidden | FileAttributes.Archive );
			}
		}

		public bool Recover( byte version = 0 )
		{
			string fileName = Path.GetFileNameWithoutExtension( this._fileName ) + $".cfg({version})";
			if ( File.Exists( fileName ) )
			{
				CliRegistry temp = CliRegistry.Load( fileName );
				this._lastSaved = temp._lastSaved;
				this._fileName = temp._fileName;
				this._applicationHive = temp._applicationHive;
				this._systemHive = temp._systemHive;
				this._userDb = temp._userDb;

				return true;
			}

			return false;
		}

		public UserInfo GetUser( string userName ) => 
			this._userDb[ userName ].Data;

		public bool RemoveUser( string userName ) =>
			this._userDb.DeleteUser( userName );

		public bool AddUser( UserInfo newUser, string comment = "" ) =>
			!(newUser is null) && this[ Hive.System ].HasItem( "UserDefaults" ) &&
			this._userDb.AddUser( newUser, comment ) &&
			this.Copy( Hive.System, "UserDefaults", $"{newUser.UserName}.Defaults", Hive.User );

		public bool HasUser( string userName ) =>
			this._userDb.HasUser( userName );

		/// <summary>Copies a registry node from one location to another within the same hive.</summary>
		/// <param name="hive">The hive in which both the source and destinations nodes will reside.</param>
		/// <param name="sourceNode">A dot-separated path identifying the node to be copied.</param>
		/// <param name="destinationNode">A dot-separated path identifying the node to be created.</param>
		/// <param name="destructive">If set to TRUE, original nodes will be wholly replaced with the new one, otherwise they'll be merged.</param>
		/// <returns>TRUE if the copy operation was successful, otherwise FALSE.</returns>
		/// <remarks>
		/// The SystemHive cannot be modified, so any attempt to use Hive.System with this function will be rejected, as
		/// will any attempt to copy a node onto itself (i.e. both the source and destination paths match).
		/// </remarks>
		public bool Copy( Hive hive, TreeGroupChain sourceNode, TreeGroupChain destinationNode, bool destructive = false ) =>
			( hive != Hive.System ) && Copy( hive, sourceNode, destinationNode, hive );

		/// <summary>Copies a registry node from one location to another.</summary>
		/// <param name="hive">Which hive contains the node to be copied.</param>
		/// <param name="sourceNode">A dot-separated path identifying the node to be copied.</param>
		/// <param name="destinationNode">A dot-separated path identifying the node to be created.</param>
		/// <param name="destHive">Which hive will receive the copied node. This CANNOT be Hive.System!</param>
		/// <param name="destructive">If set to TRUE, original nodes will be wholly replaced with the new one, otherwise they'll be merged.</param>
		/// <returns>TRUE if the copy operation was successful, otherwise FALSE.</returns>
		/// <remarks>
		/// The SystemHive cannot be modified, so any attempt to use Hive.System as the destination will be rejected, as
		/// will any attempt to copy a node onto itself (i.e. both the source and destination hive and node values match).
		/// </remarks>
		public bool Copy( Hive hive, TreeGroupChain sourceNode, TreeGroupChain destinationNode, Hive destHive, bool destructive = false )
		{
			if ( (hive == destHive) && sourceNode.Equals( destinationNode ) )
				return false;

			if ( !(sourceNode is null) && !(destinationNode is null) && !(destHive == Hive.System) )
			{
				CliRegObject source = GetNode( hive, sourceNode ).Clone(destinationNode.Last), dest;
				if ( !(source is null) )
				{
					if ( destHive == Hive.User )
					{
						string userName = destinationNode.First;
						destinationNode = destinationNode.Tail;
						dest = this._userDb[ userName ].Settings.TryGet( destinationNode ) as CliRegObject;
						if ( dest is null )
							dest = this._userDb[ userName ].Settings.CreateNodePath( destinationNode ) ?
								this._userDb[ userName ].Settings.Get( destinationNode ) as CliRegObject : null;
					}
					else
					{
						dest = GetNode( destHive, destinationNode );

						if ( dest is null )
							dest = this[ destHive ].CreateNodePath( destinationNode ) ? GetNode( destHive, destinationNode ) : null;
					}

					if ( !(dest is null) )
					{
						foreach ( CliRegObject item in source )
							dest.ImportNode( item, destructive );

						return true;
					}
				}
			}
			return false;
		}

		private CliRegObject GetNode( Hive hive, TreeGroupChain path ) =>
			this[ hive ].Get( path ) as CliRegObject;

		public CliRegData Get( Hive hive, string path )
		{
			CliRegObject result =  TreeGroupChain.ValidatePath( path ) ? GetNode( hive, path ) : null;
			return (result is null) ? null : result.BaseData;
		}

		/// <summary>Facilitates setting a value in the Registry.</summary>
		/// <param name="hive">Which hive is being addressed.</param>
		/// <param name="path">The dot-separated name of the node.</param>
		/// <param name="value">The string value to assign to the specified node.</param>
		/// <param name="comment">An optional comment to assign to the node.</param>
		/// <param name="encrypt">Specifies if the node data should be stored in an encrypted state.</param>
		/// <param name="suppressExceptions">If TRUE, the 'invalid path' exception will be suppressed if an invalid path is supplied.</param>
		/// <returns>TRUE if the set operation was successful.</returns>
		/// <remarks>
		/// NOTE:<br/>Assigning a NULL value to a node will DELETE the node!<br/>
		/// If you want to simply clear a node value, assign an empty string instead!
		/// </remarks>
		/// <seealso cref="SysSet(Hive, string, CliRegData, bool)"/>
		public bool Set( Hive hive, string path, object value, string comment = "", bool encrypt = false, bool suppressExceptions = false ) =>
			Set( hive, path, new CliRegData( value, encrypt, comment ), suppressExceptions );

		/// <summary>Facilitates setting a value in the Registry.</summary>
		/// <param name="hive">Which hive is being addressed.</param>
		/// <param name="path">The dot-separated name of the node.</param>
		/// <param name="data">The data being applied to the node.</param>
		/// <param name="suppressExceptions">If TRUE, the 'invalid path' exception will be suppressed if an invalid path is supplied.</param>
		/// <returns>TRUE if the set operation was successful.</returns>
		/// <remarks>
		/// NOTE:<br/>Assigning a NULL value to a node will DELETE the node!<br/>
		/// If you want to simply clear a node value, assign an empty string instead!
		/// </remarks>
		/// <seealso cref="SysSet(Hive, string, CliRegData, bool)"/>
		public bool Set( Hive hive, string path, CliRegData data, bool suppressExceptions = false ) =>
			((hive != Hive.System) || this._systemHive.HasNode( path )) && SysSet( hive, path, data, suppressExceptions );

		/// <summary>Facilitates setting a value in the Registry.</summary>
		/// <param name="hive">Which hive is being addressed.</param>
		/// <param name="path">The dot-separated name of the node.</param>
		/// <param name="data">The data being applied to the node.</param>
		/// <param name="suppressExceptions">If TRUE, the 'invalid path' exception will be suppressed if an invalid path is supplied.</param>
		/// <returns>TRUE if the set operation was successful.</returns>
		/// <remarks>
		/// This is set to Private to force external calls to use 'Set' and thereby be precluded from making changes to the
		/// System Hive while facilitating internal access to make such modifications.<br/><br />
		/// NOTE:<br/>Assigning a NULL value to a non-SystemHive node will DELETE the node!<br/>
		/// Assigning a NULL value to a SystemHive node will throw an exception, or return false.<br />
		/// If you want to simply clear a node value, assign an empty string instead!
		/// </remarks>
		/// <exception cref="InvalidOperationException">Thrown if a NULL value assignment to a SystemHive node is attempted.</exception>
		/// <exception cref="ArgumentException">If the supplied 'path' string is invalid.</exception>
		private bool SysSet( Hive hive, string path, CliRegData data, bool suppressExceptions = false )
		{
			if ( (hive == Hive.System) && (data is null) )
			{
				if ( !suppressExceptions )
					throw new InvalidOperationException( $"You cannot delete nodes from the System Hive!" );

				return false;
			}

			if ( TreeGroupChain.ValidatePath( path ) )
				return this[ hive ][ path ].Set( path, data );

			if ( !suppressExceptions )
				throw new ArgumentException( $"The supplied path (\x22{path}\x22) isn't valid." );

			return false;
		}

		/// <summary>Facilitates setting a value in the Registry.</summary>
		/// <param name="hive">Which hive is being addressed.</param>
		/// <param name="path">The dot-separated name of the node.</param>
		/// <param name="data">The data being applied to the node.</param>
		/// <param name="comment">An optional comment to attach to the node.</param>
		/// <param name="encrypt">Specifies whether or not the data should be stored in an encrypted form.</param>
		/// <param name="suppressExceptions">If TRUE, the 'invalid path' exception will be suppressed if an invalid path is supplied.</param>
		/// <returns>TRUE if the set operation was successful.</returns>
		/// <remarks>
		/// This is set to Private to force external calls to use 'Set' and thereby be precluded from making changes to the
		/// System Hive while facilitating internal access to make such modifications.<br/><br />
		/// NOTE:<br/>Assigning a NULL value to a non-SystemHive node will DELETE the node!<br/>
		/// Assigning a NULL value to a SystemHive node will throw an exception, or return false.<br />
		/// If you want to simply clear a node value, assign an empty string instead!
		/// </remarks>
		/// <exception cref="InvalidOperationException">Thrown if a NULL value assignment to a SystemHive node is attempted.</exception>
		/// <exception cref="ArgumentException">If the supplied 'path' string is invalid.</exception>
		private bool SysSet( Hive hive, string path, object data, string comment = "", bool encrypt = false, bool suppressExceptions = false ) =>
			SysSet( hive, path, new CliRegData( data, encrypt, comment ), suppressExceptions );

		/*
		private bool Parse( string rawData )
		{
			CliHive hive = new CliHive( rawData.ToXmlNode() );
			if ( (hive is null) || (hive.HiveType != Hive.App) ) return false;
			this._applicationHive = hive;

			hive = new CliHive( rawData.ToXmlNode() );
			if ( (hive is null) || (hive.HiveType != Hive.System ) ) return false;
			this._systemHive = hive;

			CliUserHive userDb = new CliUserHive( rawData.ToXmlNode() );
			if ( userDb is null ) return false;
			this._userDb = userDb;

			return true;
		}
		*/

		public override string ToString() =>
			$"{Name}:\r\n* System: {this._systemHive.NodeCount} nodes.\r\n" +
			$"* Application: {this._applicationHive.NodeCount} nodes.\r\n" +
			$"* {this._userDb.Count} User{(this._userDb.Count == 1 ? "" : "s")}";

		//public string ExtractUserDb() => this._userDb.ToString();

		/// <summary>Attempts to load a Registry from disk.</summary>
		/// <param name="fileName">The filename to load. If this is null or empty, the application name will be substituted.</param>
		/// <param name="suppressExceptions">If set to TRUE, exceptions generated by this process will be suppressed.</param>
		/// <param name="customSysObjects">If you want to add custom SystemHive objects, they must be specified here.</param>
		/// <returns>If the load is successful, a new CliRegistry object populated from the requested file.</returns>
		/// <remarks>
		/// Adding custom nodes to the System Hive is only possible when the registry is originally created!<br/>
		/// If the requested file already exists, the 'customSysObjects' parameter will be ignored!
		/// </remarks>
		public static CliRegistry Load( string fileName = null, bool suppressExceptions = false, params CliRegObject[] customSysObjects )
		{
			fileName = ConfigFileName( fileName );
			if ( string.IsNullOrWhiteSpace( fileName ) )
				throw new FileNotFoundException( $"The supplied filename is invalid (\x22{fileName}\x22)." );

			if ( !File.Exists( fileName ) ) // If the registry file for this CLI doesn't exist, create it.
				DefaultRegistry( customSysObjects ).Save( fileName );

			if ( File.Exists( fileName ) )
			{
				byte[] data = File.ReadAllBytes( fileName );
				if ( data.Length > 0 )
				{
					string rawXml = $"{NetXpertExtensions.XML_HEADER}{TextCompress.TextUncompress( data )}";

					if ( rawXml.IsXml() )
					{
						XmlDocument doc = new XmlDocument();
						doc.LoadXml( rawXml );
						if ( doc.HasElement( "CliRegistry" ) )
						{
							XmlNode root = doc.GetFirstNamedElement( "CliRegistry", "last" );
							if ( !(root is null) )
							{
								CliRegistry result = new CliRegistry();
								result._fileName = fileName;
								result._lastSaved = root.GetAttributeValue( "last" );
								result._systemHive = new CliHive( root.GetFirstNamedElement( "group", "name", "system" ) );
								result._applicationHive = new CliHive( root.GetFirstNamedElement( "group", "name", "apphive" ) );
								result._userDb = new CliUserHive( root.GetFirstNamedElement( "users", "serial" ) );
								return result;
							}
						}
						/*
						{
							CliRegistry result = new CliRegistry( doc.GetFirstNamedElement( "CliRegistry" ) );
							result._fileName = fileName;
							result._lastSaved = this.ToString();
							return result;
						}
						*/
					}

					if ( !suppressExceptions )
						throw new InvalidDataException( $"The data imported from the configuration file ({rawXml.Length} bytes) is not valid." );
				}
			}
			else
				if ( !suppressExceptions )
				throw new FileNotFoundException( $"The configuration file, `{fileName}' was not found." );

			return null;
			//throw new FileNotFoundException( $"The configuration file \x22{fileName}\x22 doesn't exist." );
		}
		#endregion

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		public void Dispose( bool disposing )
		{
			if ( !disposedValue )
			{
				if ( disposing )
				{
					// TODO: dispose managed state (managed objects).
					if (!IsSaved)
						this.Save( this._fileName );
				}

				// base.Dispose( disposing );
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
			// GC.SuppressFinalize(this);
		}
		#endregion

		/// <summary>Builds a minimalist default starting registry.</summary>
		/// <remarks>
		/// If you want to create custom SystemHive objects for your application, you can specify them in the
		/// 'customSystemNodes' param lis`t. Each CliRegObject object included in this way will be added at the
		/// root level of the SystemHive.
		/// </remarks>
		public static CliRegistry DefaultRegistry( params CliRegObject[] customSystemNodes )
		{
			CliRegistry def = new CliRegistry();

			CliRegObject global = new CliRegObject( "Global", "", "Stores Global System settings." );
			global.AddItem( "Prompt", "[CS] > ", "The default system prompt.", true );
			global.AddItem( "KeyInterval", 44, "The default system Keyboard Interval value (ms)." );
			global.AddItem( "CmdInterval", 250, "The default system Command Interval value (ms)." );
			global.AddItem( "Heartbeat", 180, "The default system Heartbeat Interval value (ms)." );
			global.AddItem( "AllowSu", true, "Specifies whether or not the SU cmdlet is permitted to run." );
			global.AddItem( "BlockedCmdlets", "", "Specifies which cmdlets to disable." );
			//global.AddItem( "WindowsUser", "\x22Unknown\\Unknown\x22" );
			//global.AddItem( "CurrentUser", "\x22Unknown\x22" );
			XmlNode globalXml = global.ToXmlNode();

			CliRegObject userDefaults = new CliRegObject( "UserDefaults", "", "Stores default user registry settings" );
			userDefaults.AddItem( "Prompt", "[$user['userName']] $file['path'] > ", "The default user prompt.", true );

			def._systemHive.ImportNode( global, userDefaults );


			List<CliRegObject> sysObjects = new List<CliRegObject>();
			sysObjects.AddRange( new CliRegObject[] { global, userDefaults } );
			sysObjects.AddRange( customSystemNodes );

			def._systemHive = new CliHive( Hive.System, sysObjects.ToArray() );

			UserInfo admin = new UserInfo( "root", "System Administrator", Ranks.SuperUser ) { Password = new Password( "password" ) };
			def.AddUser( admin, "Default System Administrator Account" );

			def._userDb[ "root" ].Settings.CreateNodePath( "Applications" );
			def._userDb.SetCurrentUser( "root" );
			return def;
		}

		public static string ConfigFileName( string fileName = null )
		{
			if ( !string.IsNullOrWhiteSpace( fileName ) ) fileName = Regex.Replace( fileName, @"[^\w]", "" );
			fileName = HomePath + (string.IsNullOrWhiteSpace( fileName ) ? ExecutableName : fileName);
			return string.IsNullOrWhiteSpace( fileName ) ? "" : fileName + ".cfg";
		}
	}
}
