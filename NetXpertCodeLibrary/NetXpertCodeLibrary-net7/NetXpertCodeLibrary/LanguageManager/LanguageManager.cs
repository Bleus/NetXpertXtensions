using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using NetXpertCodeLibrary.Extensions;
using NetXpertExtensions;
using NetXpertExtensions.Xml;

namespace NetXpertCodeLibrary
{
	#region Foundation Classes
	/// <summary>Facilitates interacting with XmlNode Attributes in a more sensible/intuitive manner.</summary>
	public class AttributeCollection : IEnumerator<KeyValuePair<string,string>>
	{
		#region Properties
		private Dictionary<string,string> _attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		private int _position = 0;
		#endregion

		#region Constructors
		public AttributeCollection() { }

		public AttributeCollection( XmlNode source ) => Import( source.Attributes );

		public AttributeCollection( XmlAttributeCollection attributes ) => Import( attributes );
		#endregion

		#region Operators
		public static implicit operator AttributeCollection( XmlNode node ) => new AttributeCollection( node );
		public static implicit operator AttributeCollection( XmlAttributeCollection attrs ) => new AttributeCollection( attrs );
		public static implicit operator XmlAttributeCollection( AttributeCollection ac ) => ac.ToXmlAttributes();
		#endregion

		#region Accessors
		public int Count => _attributes.Count;

		public string this[ string key ]
		{
			get
			{
				if ( !string.IsNullOrWhiteSpace( key ) && this._attributes.ContainsKey( key ) )
					return this._attributes[ key ];

				//throw Language.Prompt.GetException( 0, new object[] { key } );
				throw new KeyNotFoundException( $"The requested attribute does not exist in this collection (\"{key}\")" );
			}
			set
			{
				if ( !string.IsNullOrWhiteSpace( key ) )
					this.Add( key, value );
			}
		}

		public KeyValuePair<string,string> this[ int index ]
		{
			get
			{
				if ( (index < 0) || (index >= this.Count) )
					throw new IndexOutOfRangeException( $"The requested index ({index}) is out of range. (0-{Count - 1})" );

				return this.ToArray()[ index ];
			}
		}

		KeyValuePair<string,string> IEnumerator<KeyValuePair<string, string>>.Current =>
			this.ToArray()[ this._position ];

		object IEnumerator.Current => this.ToArray()[ this._position ];
		#endregion

		#region Methods
		public bool HasAttribute( string name ) => 
			string.IsNullOrWhiteSpace(name) ? false : _attributes.ContainsKey( name );

		public void Add( KeyValuePair<string, string> value ) =>
			Add( value.Key, value.Value );

		public void Add( string key, string value )
		{
			if ( _attributes.ContainsKey( key ) )
				_attributes[ key ] = value;
			else
				_attributes.Add( key, value );
		}

		public void Add( XmlAttribute xmlAttr )
		{
			if ( !(xmlAttr is null) )
				Add( xmlAttr.Name, xmlAttr.Value );
		}

		public void Remove( string key )
		{
			if (!string.IsNullOrWhiteSpace(key) && _attributes.ContainsKey( key ))
			this._attributes.Remove( key );
		}

		public void Import( XmlAttributeCollection attributes )
		{
			if ( !(attributes is null) )
				foreach ( XmlAttribute xa in attributes )
					this.Add( xa );
		}

		/// <summary>Creates an XmlNode using the supplied tagname and the attributes contained in this set.</summary>
		public XmlNode ToXmlNode( string tagName )
		{
			if ( !string.IsNullOrEmpty( tagName ) && Regex.IsMatch( tagName, @"^(project|settings|languages?|class|method|namespace|prompt|data|exception)$" ) )
			{
				string xml = $"<{tagName}";
				foreach ( KeyValuePair<string, string> attribute in this._attributes )
					xml += $" {attribute.Key}='{LanguageBase.EncodeXmlAttributeValue( attribute.Value )}'";

				return (xml + " />").ToXmlNode();
			}

			//throw Language.Prompt.GetException( 0, new object[] { tagName } );
			throw new ArgumentException( $"The requested XmlNode type cannot be created with this method (\"{tagName}\")" );
		}

		/// <summary>Applies these attributes to a supplied XmlNode</summary>
		public XmlNode ToXmlNode( XmlNode source )
		{
			if ( !(source is null) )
			{
				string xml = $"<{source.Name}";
				foreach ( KeyValuePair<string, string> attribute in this._attributes )
					xml += $" {attribute.Key}='{LanguageBase.EncodeXmlAttributeValue( attribute.Value )}'";

				return (xml + $">{source.InnerXml}</{source.Name}>").ToXmlNode();
			}
			//throw Language.Prompt.GetException( 1 ); 
			throw new ArgumentNullException( "Cannot apply Attributes to a NULL XmlNode." );
		}

		public XmlAttributeCollection ToXmlAttributes() => 
			ToXmlNode( "<temp />" ).Attributes;

		public KeyValuePair<string, string>[] ToArray() =>
			new List<KeyValuePair<string, string>>( this._attributes ).ToArray();

		#region IEnumerator Support
		public IEnumerator<KeyValuePair<string, string>> GetEnumerator() =>
			this._attributes.GetEnumerator();

		public bool MoveNext() =>
			(++this._position) < this.Count;

		public void Reset() =>
			this._position = 0;

		// IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose( bool disposing )
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
			// GC.SuppressFinalize(this);
		}
		#endregion
		#endregion
	}

	/// <summary>Provides a common foundation class for tying all of this stuff together.</summary>
	public abstract class LanguageBase
	{
		#region Properties
		protected int _position = 0;
		private string _tagName = "";

		/// <summary>Regex Class to validate that a string conforms to a dot-separated list of valid names.</summary>
		public static readonly Regex PathMatch = new Regex( @"^(([a-z][\w]*[a-z0-9])[.])+([a-z0-9][\w]*[a-z0-9]|[0-9]{1,2})$", RegexOptions.IgnoreCase );
		#endregion

		#region Constructors
		public LanguageBase( string name ) => Name = name;

		protected LanguageBase() { }

		protected LanguageBase( XmlNode node ) => ParseXmlNode( node );
		#endregion

		#region Accessors
		public string Name
		{
			get => this.Attributes.HasAttribute( "name" ) ? this.Attributes[ "name" ] : "";
			set
			{
				if ( ValidateName( value ) )
					this.Attributes[ "name" ] = value;
				else
					//throw Language.Prompt.GetException( 0, new object[] { value } );
					throw new ArgumentException( $"The specified name isn't valid: \"{value}\"." );
			}
		}

		public string Comment
		{
			get => this.Attributes.HasAttribute( "name" ) ? UnencodeXmlAttributeValue( this.Attributes[ "comment" ] ) : "";
			set
			{
				if ( string.IsNullOrWhiteSpace( value ) ) value = "";

				if ( (value.Length == 0) || (value.Trim().Length > 0) )
					this.Attributes[ "comment" ] = value;
			}
		}

		protected string TagName
		{
			get => this._tagName;
			set
			{
				if ( string.IsNullOrWhiteSpace( this._tagName ) && !string.IsNullOrWhiteSpace( value ) )
				{
					value = Regex.Replace( value.ToLowerInvariant(), @"[^a-z]", "" );
					if ( Regex.IsMatch( value, @"^(project|settings|languages?|class|method|namespace|prompt|data|exception)$" ) )
						this._tagName = value;
				}
			}
		}

		public AttributeCollection Attributes { get; set; } = new AttributeCollection();

		/// <summary>Used internally for encoding/decoding XML Attribute strings.</summary>
		private static List<KeyValuePair<string, string>> TranslationTable =>
			new List<KeyValuePair<string, string>>(
				new KeyValuePair<string, string>[] {
					new KeyValuePair<string, string>( "&", "&amp;" ),
					new KeyValuePair<string, string>( "<", "&lt;" ),
					new KeyValuePair<string, string>( ">", "&gt;" ),
					new KeyValuePair<string, string>( "'", "&apos;" ),
					new KeyValuePair<string, string>( "\"", "&quot;" ),
				}
			);
		#endregion

		#region Methods
		protected XmlNode CreateXmlNode( string payload, string tag = "" )
		{
			XmlNode xml = this.Attributes.ToXmlNode( string.IsNullOrWhiteSpace( tag ) ? TagName : tag );
			xml.InnerXml = payload;
			return xml;
		}

		protected void ParseXmlNode( XmlNode node )
		{
			this._tagName = node.Name;
			this.Attributes = node.Attributes;
		}

		/// <summary>Facilitates easily checking the supplied node's name and parameters.</summary>
		/// <param name="node">The XmlNode to test.</param>
		/// <param name="tagName">The required tagname to match (Case-Insensitive).</param>
		/// <param name="attrName">Optional Attribute match value (Case-Insensitive).</param>
		/// <param name="attrValue">Optional Attribute value to match (Case-Insensitive).</param>
		/// <param name="exceptions">If this optional parameter is set to FALSE, exceptions will NOT be thrown when data does not match the specified criteria.</param>
		/// <returns>True if the passed XmlNode matches the provided parameters.</returns>
		/// <exception cref="ArgumentNullException">Thrown if the supplied XmlNode is null and "exceptions" is TRUE.</exception>
		/// <exception cref="XmlException">The supplied XmlNode does not conform with the specified criteria and "exception" is TRUE.</exception>
		public static bool PreParse( XmlNode node, string tagName, string attrName = null, string attrValue = null, bool exceptions = true )
		{
			if ( node is null )
			{
				if ( exceptions )
					//throw Language.Prompt.GetException( 0 );
					throw new ArgumentNullException( "The provided XmlNode was null." );
				else return false;
			}

			if ( !node.Name.Equals( tagName, StringComparison.OrdinalIgnoreCase ) )
			{
				if ( exceptions )
					//throw Language.Prompt.GetException( 1, new object[] { node.Name, tagName } );
					throw new XmlException( $"The supplied XmlNode name is unrecognized: \x22{node.Name}\x22 (Expecting: \"{tagName}\")." );
				else return false;
			}

			if ( !string.IsNullOrWhiteSpace( attrName ) && !node.HasAttribute( attrName ) )
			{
				if ( exceptions )
					//throw Language.Prompt.GetException( 2, new object[] { node.Name } );
					throw new XmlException( $"The supplied \x22{node.Name}\x22 node does not have a \"culture\" attribute." );
				else return false;
			}

			if ( !string.IsNullOrWhiteSpace( attrValue ) && !node.GetAttributeValue( attrName ).Equals( attrValue, StringComparison.OrdinalIgnoreCase ) )
			{
				if ( exceptions )
					//throw Language.Prompt.GetException( 3, new object[] { node.Name, attrName, attrValue } );
					throw new XmlException( $"The supplied \x22{node.Name}\x22 node's {attrName} attribute does not equal \"{attrValue}\"." );
				else return false;
			}

			return true;
		}

		public static bool PreParse( XmlNode node, string tagName, KeyValuePair<string, string>[] attrList, bool exceptions = true )
		{
			bool result = true;
			int i = -1;
			while ( result && (++i < attrList.Length) )
				result = PreParse( node, tagName, attrList[ i ].Key, attrList[ i ].Value, exceptions );

			return result;
		}

		public static bool PreParse( XmlNode node, string tagName, string[] attrList, string[] attrValues = null, bool exceptions = true )
		{
			if ( attrList is null ) attrList = new string[] { };
			if ( attrValues is null ) attrValues = new string[] { };
			bool result = true;
			int i = -1;
			while ( result && (++i < attrList.Length) )
				result = PreParse( node, tagName, attrList[ i ], (i < attrValues.Length) ? attrValues[ i ] : null, exceptions );

			return result;
		}

		/// <summary>Functionally equivalent to "PreParse", but with Exceptions turned off.</summary>
		/// <seealso cref="PreParse(XmlNode, string, string, string, bool)"/>
		public static bool TryPreParse( XmlNode node, string tagName, string attrName = null, string attrValue = null ) =>
			PreParse( node, tagName, attrName, attrValue, false );

		public static bool TryPreParse( XmlNode node, string tagName, KeyValuePair<string, string>[] attrList ) =>
			PreParse( node, tagName, attrList, false );

		public static bool TryPreParse( XmlNode node, string tagName, string[] attrList, string[] attrValues = null ) =>
			PreParse( node, tagName, attrList, attrValues, false );

		/// <summary>Provides a means to validate a Namespace/Class/Method name.</summary>
		public static bool ValidateName( string name ) =>
			!string.IsNullOrWhiteSpace( name ) && Regex.IsMatch( name, @"^[a-z][\w]*[a-z0-9]$", RegexOptions.IgnoreCase );

		/// <summary>Validates that a supplied string represents a valid Culture.</summary>
		public static bool ValidateCulture( string name )
		{
			if ( !string.IsNullOrWhiteSpace( name ) && Regex.IsMatch( name, @"^([a-zA-Z]{2}[-][a-zA-Z]{2})$" ) )
			{
				try { CultureInfo ci = CultureInfo.GetCultureInfo( name ); return true; }
				catch ( CultureNotFoundException ) { }
			}
			return false;
		}

		/// <summary>Makes a string safe for use in XML Attributes.</summary>
		public static string EncodeXmlAttributeValue( string source )
		{
			if ( string.IsNullOrWhiteSpace( source ) ) return String.Empty;

			foreach ( KeyValuePair<string, string> translate in TranslationTable )
				source = source.Replace( translate.Key, translate.Value );

			return source;
		}

		/// <summary>Takes an XML-Attribute encoded string and returns the plaintext equivalent.</summary>
		public static string UnencodeXmlAttributeValue( string source )
		{
			if ( string.IsNullOrWhiteSpace( source ) ) return String.Empty;

			foreach ( KeyValuePair<string, string> translate in TranslationTable )
				source = source.Replace( translate.Value, translate.Key );

			return source;
		}

		/// <summary>Breaks a dot-separated string of names into an array of strings.</summary>
		public static string[] DotStringPieces( string source )
		{
			if ( string.IsNullOrWhiteSpace( source ) ) return new string[] { };

			// Remove junk:
			source = Regex.Replace( source, @"[^\w.]", "" );

			if ( Regex.IsMatch( source, @"^[.]?([a-zA-Z][\w]*[a-zA-Z0-9])$", RegexOptions.Compiled ) )
				return new string[] { source.Trim( new char[] { '.' } ), "" };

			if ( LanguageBase.PathMatch.IsMatch( source ) )
				return source.Split( new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries );

			throw Language.Prompt.GetException( 0, new object[] { source } );
				// new InvalidDataException( $"The supplied string is not a valid dot-separated name. (\x22{source}\x22)." );
		}

		/// <summary>Takes in a dot-separated string and returns a KeyValuePair(string,string) comprised of the first element as the key, and the remainder as the value.</summary>
		/// <returns>Given the string: "element1.element2.element3.element4" this returns: (kvp):[ key="element1", value="element2.element3.element4" ]</returns>
		/// <remarks>If the function can't decode the supplied string, the returned kvp object will contain the unadulterated source as the key, with an empty string as the value.</remarks>
		public static KeyValuePair<string, string> SplitDotString( string source )
		{
			try
			{
				List<string> parts = new List<string>( DotStringPieces( source ) );
				switch ( parts.Count )
				{
					case 0: return new KeyValuePair<string, string>( source, "" );
					case 1: return new KeyValuePair<string, string>( parts[ 0 ], "" );
					case 2: return new KeyValuePair<string, string>( parts[ 0 ], parts[ 1 ] );
					default:
						string key = parts[ 0 ];
						parts.RemoveAt( 0 );
						return new KeyValuePair<string, string>( key, string.Join( ".", parts.ToArray() ) );
				}
			}
			catch (InvalidDataException e) { return new KeyValuePair<string, string>( source, e.Message ); }

			//if ( Regex.IsMatch( source, @"^[.]?([a-zA-Z][\w]*[a-zA-Z0-9])$", RegexOptions.Compiled ) )
			//	return new KeyValuePair<string, string>( source.Trim( new char[] { '.' } ), "" );

			//if ( Regex.IsMatch( source, @"^([a-zA-Z][\w]*[a-zA-Z0-9])[.:](.+)$", RegexOptions.Compiled ) )
			//{
			//	Match m = Regex.Match( source, @"^(?<key>[a-zA-Z][\w]*[a-zA-Z0-9])(?<cargo>[.:](.+))$" );
			//	if ( m.Groups[ "key" ].Success && m.Groups[ "cargo" ].Success )
			//		return new KeyValuePair<string, string>( m.Groups[ "key" ].Value, m.Groups[ "cargo" ].Value.Trim( new char[] { ' ', '.', '\r', '\n', '\t' } ) );
			//}

			//return new KeyValuePair<string, string>( source, "" );
		}

		/// <summary>Given a dot-separated string, returns the last item in the string.</summary>
		public static string DotStringLast( string source )
		{
			string[] parts = DotStringPieces( source );
			return ( parts.Length > 0 ) ? parts[ parts.Length - 1 ] :
				( string.IsNullOrWhiteSpace(source) ? "" : Regex.Replace( source, @"[^\w]", "" ) );
		}
		#endregion
	}

	/// <summary>Provides a foundation for managing a collection of objects accessed by their Name.</summary>
	/// <typeparam name="T">Any class derived from LangaugeBase, including other LanguageBaseCollection[T] objects.</typeparam>
	public abstract class LanguageBaseCollection<T> : LanguageBase, IEnumerator<T> where T : LanguageBase
	{
		#region Properties
		protected List<T> _data = new List<T>();
		#endregion

		#region Constructors
		public LanguageBaseCollection( string name ) : base( name ) { }

		public LanguageBaseCollection( XmlNode node ) : base( node ) => Parse( node );
		#endregion

		#region Accessors
		public T this[ string identifier ]
		{
			get
			{
				int i = IndexOf( identifier );
				if ( i >= 0 ) return _data[ i ];
				//throw Language.Prompt.GetException( 0, new object[] { identifier, Name } );
				throw new KeyNotFoundException( $"The requested item \x22{identifier}\x22 does not exist in this Class (\x22{Name}\x22)" );
			}
		}

		protected T this[ int index ] => this._data[ index ];

		public string[] Names
		{
			get
			{
				List<string> names = new List<string>();
				foreach ( T d in _data )
					names.Add( d.Name );

				return names.ToArray();
			}
		}

		public int Count => _data.Count;

		T IEnumerator<T>.Current => this._data[ this._position ];

		object IEnumerator.Current => this._data[ this._position ];
		#endregion

		#region Methods
		protected int IndexOf( string identifier )
		{
			int i = -1;
			if ( !string.IsNullOrWhiteSpace( identifier ) )
				while ( (++i < this.Count) && !this._data[ i ].Name.Equals( identifier, StringComparison.OrdinalIgnoreCase ) ) ;

			return i < Count ? i : -1;
		}

		protected bool HasItem( string item ) => IndexOf( item ) >= 0;

		/// <summary>Adds a new item to this collection</summary>
		/// <remarks>If an item already exists with the same Name, the one being added will replace it.</remarks>
		public void Add( T newEntity )
		{
			int i = IndexOf( newEntity.Name );
			if ( i < 0 )
				this._data.Add( newEntity );
			else
				this._data[ i ] = newEntity;
		}

		public void AddRange( ICollection<T> newEntities )
		{
			foreach ( T entity in newEntities )
				this.Add( entity );
		}

		/// <summary>Removes a prompt from this Collection by it's Culture</summary>
		public void Remove( string entity )
		{
			int i = IndexOf( entity );
			if ( i >= 0 ) this._data.RemoveAt( i );
		}

		public override string ToString() =>
			$"{this.Name}: {typeof( T )} (Count: {Count})";

		public T[] ToArray() => _data.ToArray();

		public abstract XmlNode ToXmlNode();

		public abstract void Parse( XmlNode node );

		protected string[] GetNames()
		{
			List<string> names = new List<string>();
			foreach ( T item in this._data )
				names.Add( $"{this.Name}.{item.Name}" );

			return names.ToArray();
		}
		#endregion

		#region IEnumerator Support
		public IEnumerator<T> GetEnumerator() =>
			this._data.GetEnumerator();

		public bool MoveNext() =>
			(++this._position) < this._data.Count;

		public void Reset() =>
			this._position = 0;

		// IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose( bool disposing )
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
			// GC.SuppressFinalize(this);
		}
		#endregion
	}

	public class LanguageTranslation
	{
		#region Properties
		/// <summary>The Culture we're translating from (i.e. en-UK, fr-FR etc)</summary>
		private CultureInfo _source = null;
		/// <summary>The Culture we're going to look up instead (i.e. en-CA or fr-CA)</summary>
		private CultureInfo _translate = null;

		private string _comment = "";
		#endregion

		#region Constructors
		public LanguageTranslation( CultureInfo translateFrom, CultureInfo translateTo = null )
		{
			this.From = translateFrom;

			if ( translateTo is null )
				_translate = _source;
			else
				this.To = translateTo;
		}

		public LanguageTranslation( string translateFrom, string translateTo )
		{
			this.FromName = translateFrom;
			this.ToName = translateTo;
		}

		public LanguageTranslation( XmlNode node ) => Parse( node );
		#endregion

		#region Operators
		public static implicit operator LanguageTranslation( CultureInfo culture ) => new LanguageTranslation( culture, null );
		public static implicit operator CultureInfo( LanguageTranslation source ) => source.From;
		#endregion

		#region Accessors
		public string Name => _source.DisplayName;

		public CultureInfo From
		{
			get => this._source;
			set
			{
				if ( value is null )
					//throw Language.Prompt.GetException( 0, new object[] { "From" } );
					throw new ArgumentNullException( "The 'From' Culture cannot be null." );

				if ( value.Equals( To ) )
					//throw Language.Prompt.GetException( 1, new object[] { value, "From", "To" } );
					throw new ArgumentException( $"The 'From' Culture cannot match the 'To' Culture (\"{value}\")" );

				if ( value.Equals( CultureInfo.InvariantCulture ) )
					//throw Language.Prompt.GetException( 2, new object[] { "From" }  );
					throw new ArgumentException( $"The 'From' Culture cannot be \"CultureInfo.InvariantCulture\"." );

				this._source = value;
			}
		}

		public string FromName
		{
			get => this._source.ToString();
			set
			{
				if ( LanguageBase.ValidateCulture( value ) )
					this.From = CultureInfo.GetCultureInfo( value );
				else
					//throw Language.Prompt.GetException( 3, new object[] { value } );
					throw new CultureNotFoundException( $"The Culture '{value}' could not be located." );
			}
		}

		public CultureInfo To
		{
			get => this._translate;
			set
			{
				if ( value is null )
					//throw Language.Prompt.GetException( 0, new object[] { "To" } );
					throw new ArgumentNullException( "The 'To' Culture cannot be null." );

				if ( value.Equals( this.To ) )
					//throw Language.Prompt.GetException( 1, new object[] { value, "To", "From" } );
					throw new ArgumentException( $"The 'To' Culture cannot match the 'From' Culture (\"{value}\")" );

				if ( value.Equals( CultureInfo.InvariantCulture ) )
					//throw Language.Prompt.GetException( 2, new object[] { "To" } );
					throw new ArgumentException( $"The 'To' Culture cannot be \"CultureInfo.InvariantCulture\"." );

				this._translate = value;
			}
		}

		public string ToName
		{
			get => this._translate.ToString();
			set
			{
				if ( LanguageBase.ValidateCulture( value ) )
					this.To = CultureInfo.GetCultureInfo( value );
				else
					//throw Language.Prompt.GetException( 3, new object[] { value } );
					throw new CultureNotFoundException( $"The Culture '{value}' could not be located." );
			}
		}

		public string Comment => _comment;

		public bool IsTranslation => this._source.Equals( this._translate );
		#endregion

		#region Methods
		public override string ToString() => $"Name: {Name} ({From} -> {To})";

		public void Parse( XmlNode node )
		{
			if ( node is null ) //throw Language.Prompt.GetException( 0 );
				throw new ArgumentNullException( "The provided XmlNode was null." );

			if ( LanguageBase.TryPreParse( node, "language", "name" ) )
			{
				if ( LanguageBase.ValidateCulture( node.GetAttributeValue( "name" ) ) )
					this._source = this._translate = CultureInfo.GetCultureInfo( node.GetAttributeValue( "name" ) );
				else
					//throw Language.Prompt.GetException( 1, new object[] { node.GetAttributeValue( "name" ) } );
					throw new ArgumentException( $"The supplied language name isn't valid. (\x22{node.GetAttributeValue( "name" )}\x22)." );
				return;
			}

			if ( LanguageBase.TryPreParse( node, "translation" ) )
			{
				if (
					node.HasAttribute( "from" ) && LanguageBase.ValidateCulture( node.GetAttributeValue( "from" ) ) &&
					node.HasAttribute( "to" ) && LanguageBase.ValidateCulture( node.GetAttributeValue( "to" ) )
				)
				{
					this.FromName = node.GetAttributeValue( "from" );
					this.ToName = node.GetAttributeValue( "to" );
					return;
				}

				if ( !node.HasAttribute( "from" ) )
					//throw Language.Prompt.GetException( 2, new object[] { "from" } );
					throw new XmlException( "The supplied \x22translation\x22 node does not have a \"from\" attribute!" );

				if ( !node.HasAttribute( "to" ) )
					//throw Language.Prompt.GetException( 2, new object[] { "to" } );
					throw new XmlException( "The supplied \x22translation\x22 node does not have a \"to\" attribute!" );

				if ( !LanguageBase.ValidateCulture( node.GetAttributeValue( "from" ) ) )
					//throw Language.Prompt.GetException( 3, new object[] { node.GetAttributeValue( "from" ), "from" } );
					throw new ArgumentException( $"The supplied from language isn't valid. (\x22{node.GetAttributeValue( "from" )}\x22)." );

				if ( !LanguageBase.ValidateCulture( node.GetAttributeValue( "name" ) ) )
					//throw Language.Prompt.GetException( 3, new object[] { node.GetAttributeValue( "to" ), "to" } );
					throw new ArgumentException( $"The supplied to language isn't valid. (\x22{node.GetAttributeValue( "to" )}\x22)." );

				return;
			}

			//throw Language.Prompt.GetException( 4, new object[] { node.Name } );
			throw new XmlException( "The supplied XmlNode name is unrecognized: \x22{node.Name}\x22." );
		}

		public XmlNode ToXmlNode() =>
			(IsTranslation
				? $"<translation from='{From}' to='{To}' name='{From.EnglishName}' />"
				: $"<language name='{From}' alt='{From.EnglishName}'>{Name}</language>"
			).ToXmlNode();
		#endregion
	}

	/// <summary>Facilitates having alternate languages return defined languages that are compatible (i.e. en-UK -> en-CA)</summary>
	public sealed class TranslationEngine : IEnumerator<LanguageTranslation>
	{
		#region Properties
		private List<LanguageTranslation> _translations = new List<LanguageTranslation>();
		private CultureInfo _defaultCulture = CultureInfo.CurrentCulture;
		private int _position = 0;
		#endregion

		#region Constructors
		public TranslationEngine( CultureInfo defaultCulture ) => DefaultCulture = defaultCulture;

		public TranslationEngine() { }
		#endregion

		#region Accessors
		public CultureInfo this[ CultureInfo from ]
		{
			get
			{
				int i = IndexOf( from );
				return i >= 0 ? _translations[ i ].To : DefaultCulture;
			}
		}

		public int Count => _translations.Count;

		public CultureInfo DefaultCulture
		{
			get => _defaultCulture;
			set
			{
				if ( !(value is null) && !value.Equals( CultureInfo.InvariantCulture ) )
					_defaultCulture = value;
			}
		}

		public string[] Languages
		{
			get
			{
				List<string> result = new List<string>();
				foreach ( LanguageTranslation lt in this._translations )
					result.Add( lt.ToString() );

				return result.ToArray();
			}
		}

		LanguageTranslation IEnumerator<LanguageTranslation>.Current => this._translations[ this._position ];

		object IEnumerator.Current => this._translations[ this._position ];
		#endregion

		#region Methods
		private int IndexOf( CultureInfo culture )
		{
			int i = -1;
			if ( !(culture is null) )
				while ( (++i < Count) && !this._translations[ i ].From.Equals( culture ) ) ;
			return i < Count ? i : -1;
		}

		public bool HasCulture( CultureInfo culture ) => IndexOf( culture ) >= 0;

		public void Add( LanguageTranslation translator )
		{
			int i = IndexOf( translator.From );
			if ( i < 0 )
				this._translations.Add( translator );
			else
				this._translations[ i ] = translator;
		}

		public void Add( TranslationEngine engine )
		{
			foreach ( LanguageTranslation line in engine.ToArray() )
				if ( !this.HasCulture( line.From ) )
					this.Add( line );
		}

		public void Add( CultureInfo from, CultureInfo to ) =>
			Add( new LanguageTranslation( from, to ) );

		public void Remove( CultureInfo from )
		{
			int i = IndexOf( from );
			if ( i >= 0 ) this._translations.RemoveAt( i );
		}

		public override string ToString() =>
			$"Translators: \"{string.Join( "\", \"", Languages )}\"";

		public void Parse( XmlNode node )
		{
			if ( !LanguageBase.TryPreParse( node, "languages" ) )
			{
				XmlNode[] nodes = node.GetNamedElements( "languages", "count" );
				if ( nodes.Length == 0 )
					//throw Language.Prompt.GetException( 0, new object[] { node.Name } );
					throw new XmlException( $"The supplied node, \"{node.Name}\" is not a Language node, and does not contain one." );

				node = nodes[ 0 ];
			}

			if ( LanguageBase.PreParse( node, "languages", "count" ) )
			{
				foreach ( XmlNode child in node.ChildNodes )
					if ( Regex.IsMatch( child.Name, @"(translation|language)", RegexOptions.IgnoreCase ) )
						this.Add( new LanguageTranslation( child ) );
			}
		}

		public XmlNode ToXmlNode()
		{
			XmlNode translator = $"<languages count='{Count}'><defaultCulture name='{DefaultCulture}'>{DefaultCulture.Name}</defaultCulture></languages>".ToXmlNode();
			foreach ( LanguageTranslation lt in this._translations )
				translator.AppendChild( lt.ToXmlNode() );
			return translator;
		}

		public LanguageTranslation[] ToArray() => this._translations.ToArray();
		#endregion

		#region IEnumerator Support
		public IEnumerator<LanguageTranslation> GetEnumerator() =>
			this._translations.GetEnumerator();

		public bool MoveNext() =>
			(++this._position) < this._translations.Count;

		public void Reset() =>
			this._position = 0;

		// IDisposable Support
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
			// GC.SuppressFinalize(this);
		}
		#endregion
	}

	public sealed class LanguageSettings : LanguageBase
	{
		#region Constructors
		public LanguageSettings() { }

		public LanguageSettings( XmlNode node ) => Parse( node );
		#endregion

		#region Accessors
		public TranslationEngine Translator { get; set; } = new TranslationEngine();

		public CultureInfo DefaultCulture
		{
			get => this.Translator.DefaultCulture;
			private set => this.Translator.DefaultCulture = value;
		}
		#endregion

		#region Methods
		public void Import( LanguageSettings settings )
		{
			foreach ( LanguageTranslation lang in settings.Translator )
				this.Translator.Add( lang );
		}

		public void Parse( XmlNode node )
		{
			if ( !(node is null) && !TryPreParse( node, "settings" ) && node.HasChildNodes )
			{
				XmlNode[] nodes = node.GetNamedElements( "settings" );
				if ( nodes.Length == 0 )
					//throw Language.Prompt.GetException( 0, new object[] { node.Name } );
					throw new XmlException( $"The supplied <{node.Name}> node is not a Settings node, and does not contain one." );

				node = nodes[ 0 ];
			}

			if ( PreParse( node, "settings" ) )
			{
				foreach ( XmlNode child in node.ChildNodes )
				{
					if ( TryPreParse( child, "defaultCulture", "name" ) && ValidateCulture( child.GetAttributeValue( "name" ) ) )
						this.DefaultCulture = CultureInfo.GetCultureInfo( child.GetAttributeValue( "name" ) );

					if ( TryPreParse( child, "languages", "count" ) )
						this.Translator.Parse( node );
				}
			}
		}

		public XmlNode ToXmlNode()
		{
			XmlNode node = "<settings></settings>".ToXmlNode();
			node.AppendChild( $"<defaultCulture name='{DefaultCulture}' />".ToXmlNode() );

			node.AppendChild( Translator.ToXmlNode() );

			return node;
		}

		public static bool HasSettingsNode( XmlNode node )
		{
			if ( !(node is null) && node.HasChildNodes )
			{
				XmlNode[] settings = node.GetNamedElements( "settings" );
				return settings.Length > 0;
			}
			return false;
		}
		#endregion
	}
	#endregion

	#region Exception Management
	/// <summary>Manages language prompts for Exceptions.</summary>
	/// <remarks>Structurally similiar to LanguagePrompt, but for exceptions.</remarks>
	/// <seealso cref="ExceptionPrompt"/>
	public sealed class ExceptionPrompt : LanguageBaseCollection<LanguageEntity>
	{
		#region Properties
		private Type _exceptionType = typeof( Exception );
		#endregion

		#region Constructors
		public ExceptionPrompt( string name ) : base( name ) { }

		public ExceptionPrompt( XmlNode node ) : base( node ) => Parse( node );
		#endregion

		#region Accessors
		public string this[ CultureInfo culture ]
		{
			get
			{
				int i = -1;
				if ( !(culture is null) )
					while ( (++i < Count) && !this._data[ i ].Culture.Equals( culture ) ) ;

				return (i >= 0) ? this._data[ i ].Payload : ""; // $"No prompt was found for this culture (\x22{culture}\x22).";
			}
		}

		public Type ExceptionType
		{
			get => _exceptionType;
			set
			{
				if ( IsException( value ) )
				{
					this._exceptionType = value;

					// Make sure that the underlying XML Attribute is updated too!
					if ( this.Attributes.HasAttribute( "type" ) )
						this.Attributes[ "type" ] = value.Name;
					else
						this.Attributes.Add( "type", value.Name );
				}			
			}
		}

		public string ExceptionName
		{
			get => ExceptionType.Name;
			set
			{
				if ( !string.IsNullOrWhiteSpace( value ) )
				{
					Type t = DeriveType( value );
					if ( !(t is null) )
						ExceptionType = t;
				}
			}
		}

		public string Id
		{
			get => base.Attributes.HasAttribute( "id" ) ? base.Attributes[ "id" ] : base.Name;
			set
			{
				if ( ValidateName( Id ) )
				{
					if ( !base.Attributes.HasAttribute( "id" ) )
						base.Attributes.Add( "id", value );
					else
						base.Attributes[ "id" ] = value;
				}
				else
					//throw Language.Prompt.GetException( 0, new object[] { value } );
					throw new ArgumentException( $"The specified Id is invalid (\x22{value}\x22)." );
			}
		}

		// Obfuscate this base class Accessor from the world.
		new private string Name => "";
		#endregion

		#region Methods
		public override XmlNode ToXmlNode()
		{
			XmlNode result = CreateXmlNode( "", "exception" ); // $"<prompt id=\"{Id}\" />".ToXmlNode();
			foreach ( LanguageEntity le in this._data )
				result.AppendChild( le.ToXmlNode() );
			return result;
		}

		public override void Parse( XmlNode node )
		{
			if ( PreParse( node, "exception", new string[] { "type", "id" } ) )
			{
				base.ParseXmlNode( node );
				if ( !IsException( node.GetAttributeValue( "type" ) ) )
					//throw Language.Prompt.GetException( 0, new object[] { node.GetAttributeValue( "type" ) } );
					throw new XmlException( $"The specified 'type' is not a valid Exception (\x22{node.GetAttributeValue( "type" )}\x22)" );

				ExceptionType = DeriveType( node.GetAttributeValue( "type" ) );


				XmlNode[] entities = node.GetNamedElements( "data", "culture" );
				foreach ( XmlNode nodes in entities )
					this.Add( new LanguageEntity( nodes ) );

				return;
			}

			if ( !LanguageEntity.ValidateName( node.GetAttributeValue( "id" ) ) )
				//throw Language.Prompt.GetException( 1, new object[] { node.GetAttributeValue( "id" ) } );
				throw new ArgumentException( $"The supplied prompt id isn't valid. (\x22{node.GetAttributeValue( "id" )}\x22)." );
		}

		/// <summary>Provides an easy means of instantiating an exception with a culture-specific message from this object.</summary>
		/// <typeparam name="T">The Exception derivative type to generate.</typeparam>
		/// <param name="culture">The Language/Culture to use.</param>
		/// <param name="data">An array of objects to insert into the prompt message.</param>
		/// <param name="innerException">If an InnerException is needed, supply it here.</param>
		/// <returns>A brand new instance of the (T)Exception populated with the supplied data.</returns>
		public T CreateException<T>( CultureInfo culture, object[] data = null, Exception innerException = null ) where T : Exception =>
			(T)CreateException( culture, data, innerException );

		/// <summary>Provides an easy means of instantiating an exception with a culture-specific message from this object.</summary>
		/// <param name="culture">The Language/Culture to use.</param>
		/// <param name="data">An array of objects to insert into the prompt message.</param>
		/// <param name="innerException">If an InnerException is needed, supply it here.</param>
		/// <returns>A brand new instance of the Exception populated with the supplied data.</returns>
		public dynamic CreateException( CultureInfo culture, object[] data = null, Exception innerException = null)
		{
			string msg = this[ culture ];
			if ( msg == "" )
			{
				msg = Language.Prompt.Get( 0 ).Replace( new object[] { Name, culture} ); 
				// $"Requested Prompt Not Found: \x22{Name}.{culture}\x22";
				if ( !(data is null) && (data.Length > 0) )
				{
					msg += " Parameters: [";
					for ( int i = 0; i < data.Length; i++ )
						msg += ((i > 0) ? ", " : "") + "\"{o}\"";

					msg += " ];";
				}
			}
			else
				if ( !(data is null) && (data.Length > 0) ) msg.Replace( data );

			object[] parameters = (innerException is null) ? new object[] { msg } : new object[] { msg, innerException };
			return Convert.ChangeType( Activator.CreateInstance( ExceptionType, parameters ), ExceptionType );
		}

		public override string ToString() =>
			$"Id: \x22{Name}\x22; Type: {{{ExceptionType}}}" +
			(base.Attributes.HasAttribute( "comment" ) ? $" Comment: \x22{base.Attributes[ "comment" ]}\x22" : "") +
			"\r\n";

		#region Static Methods
		/// <summary>Reports on whether a provided Type object refers to a known Exception-derived Type.</summary>
		/// <returns>TRUE if the provided Type is derived from Exception, otherwise FALSE.</returns>
		public static bool IsException( Type type ) =>
			(type is null) || (type == typeof( object )) ? false : (type == typeof( Exception )) || IsException( type.BaseType );

		public static bool IsException( string name ) => !string.IsNullOrWhiteSpace(name) && !(DeriveType( name ) is null);

		/// <summary>
		/// Given a String containing the name of a valid Exception-derived class, this will return a Type
		/// object relating to the requested Type if one exists, otheriwse NULL will be returned.
		/// </summary>
		public static Type DeriveType( string name )
		{

			// If the string is null, empty, whitespace, or malformed, it's not valid and we ignore it...
			if ( !string.IsNullOrWhiteSpace( name ) && Regex.IsMatch( name, @"^([a-z][\w]*[a-z0-9])$", RegexOptions.IgnoreCase ) )
			{
				foreach ( Assembly assy in AppDomain.CurrentDomain.GetAssemblies() )
					foreach ( Type type in assy.GetTypes() )
					{
						bool check = Regex.IsMatch( type.Name, @"^A[\w]+Exception$", RegexOptions.IgnoreCase );
						if ( type.Name.Equals( name, StringComparison.OrdinalIgnoreCase ) )
							return type;
					}
			}

			// The type could not be determined, return null:
			return null;
		}

		/// <summary>Facilitates creating a ExceptionPrompt{T} object from a string identifying the exception type to use for "T"</summary>
		/// <param name="exceptionName">A String value specifying the name of the Exception Type to create a collection for.</param>
		/// <returns>A new ExceptionPrompt{T} object with "T" defined by the supplied string.</returns>
		//public static ExceptionPrompt CreatePrompt( string exceptionName )
		//{
		//	Type excType = DeriveType( exceptionName );
		//	if ( !(excType is null) )
		//	{
		//		return (ExceptionPrompt)Activator.CreateInstance( excType );
		//	}
		//	return null;
		//}

		/// <summary>Facilitates creating a ExceptionPrompt{T} object from an XmlNode</summary>
		/// <param name="node">A validly formed XmlNode from which to create the appropriate ExceptionPrompt{T}.</param>
		/// <returns>A new ExceptionPrompt{T} object defined by the supplied XmlNode.</returns>
		//public static ExceptionPrompt CreatePrompt( XmlNode node )
		//{
		//	if ( PreParse( node, "exception", new string[] { "type", "id" } ) )
		//	{
		//		if ( !IsException( node.GetAttributeValue( "type" ) ) )
		//			throw new XmlException( $"The specified 'type' is not a valid Exception (\x22{node.GetAttributeValue( "type" )}\x22)" );

		//		ExceptionPrompt result = CreatePrompt( node.GetAttributeValue( "type" ) );
		//		result.Parse( node );

		//		return result;
		//	}

		//	throw new XmlException( $"The supplied node cannot be parsed as an Exception node. (\x22{node.Name}\x22)." );
		//}
		#endregion
		#endregion
	}

	/// <summary>Collection class to hold/manage ExceptionPrompts.</summary>
	/// <remarks>Structurally Similar to LanguageMethod.</remarks>
	/// <seealso cref="LanguageMethod"/>
	public sealed class Exceptions : LanguageBaseCollection<ExceptionPrompt>
	{
		#region Constructors
		public Exceptions() : base( "Exceptions" ) { }

		public Exceptions( XmlNode node ) : base( node ) => Parse( node );
		#endregion

		#region Accessors
		public string[] ExceptionNames
		{
			get
			{
				List<string> names = new List<string>();
				foreach ( ExceptionPrompt lp in this._data )
					names.Add( lp.Id );
				return names.ToArray();
			}
		}
		#endregion

		#region Methods
		public bool HasPrompt( string promptName ) => base.HasItem( promptName );

		public override string ToString() =>
			$"Name: \x22{Name}\x22; Prompts: {{{string.Join( ", ", ExceptionNames )}}}\r\n";

		public override XmlNode ToXmlNode() =>
			throw new NotImplementedException();

		public XmlNode ToXmlNode( XmlNode parent )
		{
			//XmlNode result = CreateXmlNode( "", "method" ); // $"<method name=\"{Name}\"></method>".ToXmlNode();
			foreach ( ExceptionPrompt lp in this._data )
				parent.AppendChild( lp.ToXmlNode() );

			return parent;
		}

		public string GetChild( string name, CultureInfo culture ) =>
			this[ name ][ culture ];

		public override void Parse( XmlNode node )
		{
			if ( !(node is null) )
			{
				XmlNode[] nodes = node.GetNamedElements( "exception", "type" );
				foreach ( XmlNode child in nodes )
				{
					dynamic group = new ExceptionPrompt( child );
					this.Add( group );
				}
			}
		}
		#endregion
	}
	#endregion

	#region Language Prompt Management
	/// <summary>Manages an individual prompt.</summary>
	public sealed class LanguageEntity : LanguageBase
	{
		#region Properties
		private CultureInfo _culture = CultureInfo.CurrentCulture;
		private string _payload = "";
		#endregion

		#region Constructor
		public LanguageEntity( string payload, CultureInfo culture = null ) : base()
		{
			if ( !(culture is null) && !culture.Equals( CultureInfo.InvariantCulture ) ) _culture = culture;
			Payload = string.IsNullOrEmpty( payload ) ? string.Empty : payload;
		}

		public LanguageEntity( XmlNode node ) : base() => Parse( node );
		#endregion

		#region Operators
		public static implicit operator XmlNode( LanguageEntity source ) => source.ToXmlNode();
		public static implicit operator LanguageEntity( XmlNode node ) => new LanguageEntity( node );
		public static implicit operator LanguageEntity( string source ) => new LanguageEntity( source );
		#endregion

		#region Accessors
		public string Payload
		{
			get => _payload.XmlDecode();
			set => _payload = value is null ? String.Empty : value.XmlEncode();
		}

		public CultureInfo Culture
		{
			get => _culture;
			set
			{
				if ( !value.Equals( CultureInfo.InvariantCulture ) )
					_culture = value is null ? CultureInfo.CurrentCulture : value;
			}
		}

		new public string Name => this._culture.DisplayName;
		#endregion

		#region Methods
		public string Params( object[] values = null ) =>
			(values is null) ? Payload : Payload.Replace( values );

		public void Parse( XmlNode node )
		{
			if ( PreParse( node, "data", "culture" ) && ValidateCulture( node.GetAttributeValue( "culture" ) ) )
			{
				base.ParseXmlNode( node );
				_culture = new CultureInfo( base.Attributes[ "name" ] = node.GetAttributeValue( "culture" ) );
				_payload = Regex.Replace( node.InnerXml.Trim(), @"([\r\n]+[ \t]*)", " " );
				return;
			}

			if ( !LanguageEntity.ValidateCulture( node.GetAttributeValue( "culture" ) ) )
				//throw Language.Prompt.GetException( 0, new object[] { node.GetAttributeValue( "culture" ) } );
				throw new ArgumentException( $"The supplied culture isn't valid. (\x22{node.GetAttributeValue( "culture" )}\x22)." );
		}

		public override string ToString() => $"{{{_culture}}}: \x22{_payload}\x22";

		public XmlNode ToXmlNode() => CreateXmlNode( Payload, "data" );
		//{
		//	string comment = string.IsNullOrWhiteSpace( _comment ) ? "" : $" comment='{Comment}'";
		//	return $"<data culture='{Culture}'{comment}>{Payload}</data>".ToXmlNode();
		//}
		#endregion
	}

	/// <summary>Manages all prompts (languages) related to a single specific Prompt.</summary>
	public sealed class LanguagePrompt : LanguageBaseCollection<LanguageEntity>
	{
		#region Constructors
		public LanguagePrompt( string id ) : base( id ) { }

		public LanguagePrompt( XmlNode node ) : base( "placeholder" ) => Parse( node );
		#endregion

		#region Accessors
		/// <summary>Facilitates retrieving a prompt by a specified Culture.</summary>
		public string this[ CultureInfo culture ]
		{
			get
			{
				int i = IndexOf( culture );
				return (i < 0) ? $"{{Prompt: {Id}; Culture: {culture};}}" : this._data[ i ].Payload;
			}
		}

		/// <summary>Reports the name of the Prompt managed by this class.</summary>
		public string Id
		{
			get => base.Name;
			private set
			{
				if ( !string.IsNullOrWhiteSpace( value ) && (ValidateName( value ) || Regex.IsMatch( value, @"^[0-9a-fA-F]{1,2}$", RegexOptions.None )) )
					base.Attributes[ "name" ] = value;
			}
		}

		//new protected string Name => base.Name; // obfuscates the base class 'Name' accessor

		/// <summary>Returns a list of strings identifying all languages supported for this Prompt.</summary>
		public string[] Languages
		{
			get
			{
				List<string> result = new List<string>();
				foreach ( LanguageEntity le in this._data )
					result.Add( le.Culture.ToString() );

				return result.ToArray();
			}
		}
		#endregion

		#region Methods
		private int IndexOf( CultureInfo culture )
		{
			int i = -1;
			if ( !(culture is null) && !culture.Equals( CultureInfo.InvariantCulture ) )
				while ( (++i < Count) && !this._data[ i ].Culture.Equals( culture ) ) ;

			return i < Count ? i : -1;
		}

		new private int IndexOf( string culture )
		{
			if ( !string.IsNullOrWhiteSpace( culture ) && LanguageEntity.ValidateCulture( culture ) )
				return IndexOf( CultureInfo.GetCultureInfo( culture ) );

			//throw Language.Prompt.GetException( 0, new object[] { culture, Id } );
			throw new CultureNotFoundException( $"The culture \x22{culture}\x22 isn't supported in this Method (\x22{Id}\x22" );
		}

		/// <summary>Reports on whether or not a Prompt exists for the specified Culture.</summary>
		public bool HasCulture( CultureInfo culture ) => IndexOf( culture ) >= 0;

		/// <summary>Adds a new prompt to this Method</summary>
		/// <remarks>If a prompt already exists with the same Culture, the one being added will replace it.</remarks>
		public void Add( string prompt, CultureInfo culture = null ) =>
			this.Add( new LanguageEntity( prompt, culture ) );

		/// <summary>Removes a prompt from this Collection by it's Culture</summary>
		public void Remove( CultureInfo culture )
		{
			int i = IndexOf( culture );
			if ( i >= 0 ) this._data.RemoveAt( i );
		}

		public override XmlNode ToXmlNode()
		{
			XmlNode result = CreateXmlNode( "", "prompt" ); // $"<prompt id=\"{Id}\" />".ToXmlNode();
			foreach ( LanguageEntity le in this._data )
				result.AppendChild( le.ToXmlNode() );

			return result;
		}

		public override void Parse( XmlNode node )
		{
			if ( PreParse( node, "prompt", "id" ) )
			{
				base.ParseXmlNode( node );
				this.Id = node.GetAttributeValue( "id" );
				XmlNode[] entities = node.GetNamedElements( "data", "culture" );
				foreach ( XmlNode nodes in entities )
					this.Add( new LanguageEntity( nodes ) );

				return;
			}

			//if ( !LanguageEntity.ValidateName( node.GetAttributeValue( "id" ) ) )
			//	throw new ArgumentException( $"The supplied prompt id isn't valid. (\x22{node.GetAttributeValue( "id" )}\x22)." );
		}

		public override string ToString() =>
			$"Name: \x22{Id}\x22; Languages: {{{string.Join( ", ", Languages )}}}\r\n";
		#endregion
	}

	/// <summary>Manages all Prompts for a particular Method (function).</summary>
	/// <remarks>LanguageMethod objects may contain Exception declarations (for individual Methods).</remarks>
	public class LanguageMethod : LanguageBaseCollection<LanguagePrompt>
	{
		#region Properties
		#endregion

		#region Constructors
		public LanguageMethod( string name ) : base( name ) { }

		public LanguageMethod( XmlNode node ) : base( "placeholder" ) => Parse( node );
		#endregion

		#region Accessors
		public string[] PromptNames
		{
			get
			{
				List<string> names = new List<string>();
				foreach ( LanguagePrompt lp in this._data )
					names.Add( lp.Id );
				return names.ToArray();
			}
		}

		public Exceptions Exceptions { get; set; } = new Exceptions();
		#endregion

		#region Methods
		public bool HasPrompt( string promptName ) => base.HasItem( promptName );

		public override string ToString() =>
			$"Name: \x22{Name}\x22; Prompts: {{{string.Join( ", ", PromptNames )}}}\r\n";

		public override XmlNode ToXmlNode()
		{
			XmlNode result = CreateXmlNode( "", "method" ); // $"<method name=\"{Name}\"></method>".ToXmlNode();
			foreach ( LanguagePrompt lp in this._data )
				result.AppendChild( lp.ToXmlNode() );

			return Exceptions.ToXmlNode( result );
		}

		public string GetChild( string name, CultureInfo culture ) =>
			this[ name ][ culture ];

		public Exception GetException() => throw new NotImplementedException();

		public override void Parse( XmlNode node )
		{
			if ( PreParse( node, "method", "name" ) && LanguageEntity.ValidateName( node.GetAttributeValue( "name" ) ) )
			{
				base.ParseXmlNode( node );
				//this.Name = node.GetAttributeValue( "name" );
				XmlNode[] entities = node.GetNamedElements( "prompt", "id" );
				foreach ( XmlNode prompt in entities )
					this.Add( new LanguagePrompt( prompt ) );

				this.Exceptions.Parse( node );
				return;
			}

			if ( !LanguageEntity.ValidateName( node.GetAttributeValue( "name" ) ) )
				//throw Language.Prompt.GetException( 0, new object[] { node.GetAttributeValue( "name" ) } );
				throw new ArgumentException( $"The supplied method name isn't valid. (\x22{node.GetAttributeValue( "name" )}\x22)." );
		}
		#endregion
	}

	/// <summary>Manages all Methods specific to a particular class.</summary>
	/// <remarks>LanguageClass objects may contain Exception declarations (for Accessors/Constructors).</remarks>
	public class LanguageClass : LanguageBaseCollection<LanguageMethod>
	{
		#region Properties
		#endregion

		#region Constructors
		public LanguageClass( string name ) : base( name ) { }

		public LanguageClass( XmlNode node ) : base( node ) { }
		#endregion

		#region Accessors
		public Exceptions Exceptions { get; set; } = new Exceptions();
		#endregion

		#region Methods
		public string GetPrompt( string name, CultureInfo culture )
		{
			if ( Regex.IsMatch( name, @"^([a-z][\w]*[a-z0-9])[.]([a-z0-9][\w]*[a-z0-9]|[0-9]{1,2})$", RegexOptions.IgnoreCase ) )
			{
				KeyValuePair<string, string> split = LanguageBase.SplitDotString( name );
				return this[ split.Key ].GetChild( split.Value, culture );
			}

			//throw Language.Prompt.GetException( 0, new object[] { name } );
			throw new KeyNotFoundException( $"The supplied key, \"{name}\" is invalid." );
		}

		public override void Parse( XmlNode node )
		{
			if ( PreParse( node, "class", "name" ) )
			{
				base.ParseXmlNode( node );
				//this.Name = node.GetAttributeValue( "name" );

				XmlNode[] nodes = node.GetNamedElements( "method", "name" );
				if ( nodes.Length > 0 )
					foreach ( XmlNode n in nodes )
						this.Add( new LanguageMethod( n ) );

				this.Exceptions.Parse( node );
			}
		}

		public override XmlNode ToXmlNode()
		{
			XmlNode node = $"<class name='{Name}'></class>".ToXmlNode();
			foreach ( LanguageMethod lm in this._data )
				node.AppendChild( lm.ToXmlNode() );

			return Exceptions.ToXmlNode( node );
		}

		public LanguageMethod GetTargetObject( string dereference )
		{
			if ( Regex.IsMatch( dereference, @"^([a-z][\w]*[a-z0-9])[.]([a-z0-9][\w]*[a-z0-9]|[0-9]{1,2})$", RegexOptions.IgnoreCase ) )
			{
				KeyValuePair<string, string> split = LanguageBase.SplitDotString( dereference );
				return this[ split.Key ]; // .GetChild( split.Value, culture );
			}

			//throw Language.Prompt.GetException( 0, new object[] { dereference } );
			throw new KeyNotFoundException( $"The supplied key, \"{dereference}\" is invalid." );
		}
		#endregion
	}

	/// <summary>Manages all classes (and child namespaces!) related to a specific NameSpace</summary>
	public class LanguageNameSpace : LanguageBase
	{
		#region Properties
		private List<LanguageNameSpace> _nameSpaces = new List<LanguageNameSpace>();
		private List<LanguageClass> _classes = new List<LanguageClass>();
		#endregion

		#region Constructors
		public LanguageNameSpace( string name ) : base( name ) { }

		public LanguageNameSpace( XmlNode node ) : base() => Parse( node );
		#endregion

		#region Accessors
		/// <summary>
		/// Looks for a namespace name that matches the provided string and returns it, if found, otherwise it looks for a classname
		/// that matches the specified string and returns THAT instead, if one is found.
		/// </summary>
		/// <param name="name">A string specifying the namespace or class desired.</param>
		/// <remarks>This search prioritizes namespace names over classes, so any class whose name matches a defined namespace will
		/// be unreachable via this accessor (must use the GetClass function instead).</remarks>
		/// <exception cref="KeyNotFoundException">The class "{name}" couldn't be found ("{Name}")</exception>
		public LanguageBase this[ string name ]
		{
			get
			{
				//Regex pattern = new Regex( @"^(([a-z][\w]*[a-z0-9])[.])+([a-z][\w]*[a-z0-9]|[0-9]{1,2})$", RegexOptions.IgnoreCase );
				//if ( pattern.IsMatch( name ) )
				if ( LanguageBase.PathMatch.IsMatch( name ) )
				{
					KeyValuePair<string, string> split = LanguageBase.SplitDotString( name );
					name = split.Key;
				}

				if ( Regex.IsMatch( name, @"^[a-z][\w]*[a-z0-9]$", RegexOptions.IgnoreCase ) )
				{
					int i = NameSpaceIndexOf( name );
					if ( i >= 0 ) return this._nameSpaces[ i ];

					i = ClassIndexOf( name );
					if ( i >= 0 ) return this._classes[ i ];
				}

				//throw Language.Prompt.GetException( 0, new object[] { name, Name } );
				throw new KeyNotFoundException( $"The class \x22{name}\x22 couldn't be found (\x22{Name}\x22)" );
			}
		}

		/// <summary>Reports the TOTAL number of namespaces AND classes that are managed by this object.</summary>
		public int Count => _nameSpaces.Count + _classes.Count;

		public string[] Names => GetNames( 3 );
		#endregion

		#region Methods
		/// <summary>Searches the local class collection for the specified name.</summary>
		/// <returns>-1 if the specified name couldn't be found, otherwise the index of the requested name in the collection.</returns>
		private int ClassIndexOf( string className )
		{
			int i = -1;
			if ( !string.IsNullOrWhiteSpace( className ) )
				while ( (++i < _classes.Count) && !this._classes[ i ].Name.Equals( className, StringComparison.OrdinalIgnoreCase ) ) ;

			return i < _classes.Count ? i : -1;
		}

		/// <summary>Searches the local namespace collection for the specified name.</summary>
		/// <returns>-1 if the specified name couldn't be found, otherwise the index of the requested name in the collection.</returns>
		private int NameSpaceIndexOf( string nameSpaceName )
		{
			int i = -1;
			if ( !string.IsNullOrWhiteSpace( nameSpaceName ) )
				while ( (++i < _nameSpaces.Count) && !this._nameSpaces[ i ].Name.Equals( nameSpaceName, StringComparison.OrdinalIgnoreCase ) ) ;

			return i < _nameSpaces.Count ? i : -1;
		}

		public bool HasNameSpace( string name ) => NameSpaceIndexOf( name ) >= 0;

		public LanguageBase GetTargetObject( string dereference )
		{
			if ( LanguageBase.PathMatch.IsMatch( dereference ) )
			{
				KeyValuePair<string, string> split = LanguageBase.SplitDotString( dereference );
				int i = NameSpaceIndexOf( split.Key );
				if ( i >= 0 )
					return this._nameSpaces[ i ].GetTargetObject( split.Value );

				i = ClassIndexOf( split.Key );
				if ( i >= 0 )
					return this._classes[ i ].GetTargetObject( split.Value );

				//throw Language.Prompt.GetException( 0, new object[] { split.Key, Name } );
				throw new KeyNotFoundException( $"The supplied key, \"{split.Key}\" could not be found. {{{Name}}}" );
			}

			//throw Language.Prompt.GetException( 1, new object[] { dereference, Name } );
			throw new KeyNotFoundException( $"The supplied key, \"{dereference}\" is invalid. {{{Name}}}" );
		}

		/// <summary>Parses a chain of namespaces to find a buried class.</summary>
		/// <remarks>This function is recursive: The specified "dereference" can contain a chain of dot-separated namespaces and this
		/// method will endeavour to follow that path down to a class.</remarks>
		/// <exception cref="KeyNotFoundException">The class "{dereference}" couldn't be found ("{Name}")</exception>
		protected LanguageClass ChildNameSpace( string dereference )
		{
			int i = -1;
			if ( dereference.IndexOf( '.' ) > 0 )
			{
				KeyValuePair<string, string> parse = SplitDotString( dereference );

				if ( !string.IsNullOrEmpty( parse.Value ) )
				{
					i = this.NameSpaceIndexOf( parse.Key );
					if ( i >= 0 )
						return this._nameSpaces[ i ].ChildNameSpace( parse.Value );

					//throw Language.Prompt.GetException( 0, new object[] { dereference, Name } );
					throw new KeyNotFoundException( $"The class \x22{dereference}\x22 couldn't be found (\x22{Name}\x22)" );
				}

				dereference = parse.Key;
			}

			i = ClassIndexOf( dereference );
			if ( i >= 0 ) return this._classes[ i ];

			//throw Language.Prompt.GetException( 0, new object[] { dereference, Name } );
			throw new KeyNotFoundException( $"The class \x22{dereference}\x22 couldn't be found (\x22{Name}\x22)" );
		}

		/// <summary>Retrieves a namespace from this object's namespace collection by its name,</summary>
		/// <remarks>This function is NOT recursive, the "name" specified must exist in this class to be returned.</remarks>
		public LanguageNameSpace GetNameSpace( string name )
		{
			int i = NameSpaceIndexOf( name );
			return (i >= 0) ? this._nameSpaces[ i ] : null;
		}

		/// <summary>Retrieves a Class from this object's class collection, by name,</summary>
		public LanguageClass GetClass( string name )
		{
			int i = ClassIndexOf( name );
			return (i >= 0) ? this._classes[ i ] : null;
		}

		/// <summary>Integrates a supplied namespace into this object.</summary>
		public void Add( LanguageNameSpace nameSpace )
		{
			int i = NameSpaceIndexOf( nameSpace.Name );
			if ( i < 0 )
				this._nameSpaces.Add( nameSpace );
			else
			{
				foreach ( LanguageNameSpace ns in nameSpace._nameSpaces )
					this._nameSpaces[ i ].Add( ns );

				foreach ( LanguageClass clss in nameSpace._classes )
					this._nameSpaces[ i ].Add( clss );
			}
		}

		/// <summary>Integrates a supplied class into this object.</summary>
		public void Add( LanguageClass clss )
		{
			int i = ClassIndexOf( clss.Name );
			if ( i < 0 )
				this._classes.Add( clss );
			else
				foreach ( LanguageMethod method in clss )
					this._classes[ i ].Add( method );
		}

		/// <summary>Creates an XmlNode representation of this object.</summary>
		public XmlNode ToXmlNode()
		{
			XmlNode node = $"<namespace name='{Name}'></namespace>".ToXmlNode();
			// Put in all the child namespaces...
			if ( _nameSpaces.Count > 0 )
				foreach ( LanguageNameSpace lns in _nameSpaces )
					node.AppendChild( lns.ToXmlNode() );

			// Put in all the child classes
			if ( _classes.Count > 0 )
				foreach ( LanguageClass lc in _classes )
					node.AppendChild( lc.ToXmlNode() );

			return node;
		}

		public void Parse( XmlNode node )
		{
			if ( PreParse( node, "namespace", "name" ) )
			{
				base.ParseXmlNode( node );
				//this.Name = node.GetAttributeValue( "name" );

				XmlNode[] childNameSpaces = node.GetNamedElements( "namespace" );
				foreach ( XmlNode ns in childNameSpaces )
					this._nameSpaces.Add( new LanguageNameSpace( ns ) );

				XmlNode[] childClasses = node.GetNamedElements( "class", "name" );
				foreach ( XmlNode clss in childClasses )
					this._classes.Add( new LanguageClass( clss ) );
			}
		}

		public string[] GetNames( byte value )
		{
			List<string> names = new List<string>();
			if ( value.BitMaskMatch( 1 ) )
				foreach ( LanguageNameSpace ns in this._nameSpaces )
					names.Add( $"ns: {ns.Name}" );

			if ( value.BitMaskMatch( 2 ) )
				foreach ( LanguageClass clss in this._classes )
					names.Add( $"cs: {clss.Name}" );

			return names.ToArray();
		}

		public override string ToString() => $"{this.Name}: {_nameSpaces.Count} namespaces; {_classes.Count} classes;";
		#endregion
	}

	/// <summary>Manages all of the assemblies in the project.</summary>
	public sealed class LanguageManager : LanguageBaseCollection<LanguageNameSpace>
	{
		#region Properties
		private LanguageSettings _settings = new LanguageSettings();
		#endregion

		#region Constructors
		public LanguageManager() : base( "Temp" ) =>
			this.Name = Assembly.GetEntryAssembly().GetName().Name;

		public LanguageManager( string name ) : base( name ) { }

		public LanguageManager( XmlNode node, string name = null ) : base( "Temp" )
		{
			this.Name = string.IsNullOrWhiteSpace( name ) || !ValidateName( name ) ? this.GetType().Assembly.GetName().Name : name;
			this.Parse( node );
		}

		public LanguageManager( XmlDocument doc, string name = null ) : base( "Temp" )
		{
			this.Name = string.IsNullOrWhiteSpace( name ) || !ValidateName( name ) ? this.GetType().Assembly.GetName().Name : name;
			this.Parse( doc );
		}
		#endregion

		#region Accessors
		//new public LanguageAssembly this[ string assyName ]
		//{ 
		//	get
		//	{
		//		int i = IndexOf( assyName );
		//		if ( i >= 0 ) return this._data[ i ];
		//		throw new KeyNotFoundException( $"The requested assembly, \"{assyName}\" could not be found in this collection." );
		//	}
		//}

		public CultureInfo DefaultCulture
		{
			get => this._settings.DefaultCulture;
			set => this._settings.Translator.DefaultCulture = value;
		}

		public TranslationEngine Translator => this._settings.Translator;

		public CultureInfo[] Languages
		{
			get
			{
				List<CultureInfo> languages = new List<CultureInfo>();
				foreach ( LanguageTranslation lang in this._settings.Translator )
					languages.Add( lang.From );

				return languages.ToArray();
			}
		}
		#endregion

		#region Methods
		public bool HasAssembly( string name ) => base.HasItem( name );

		public bool HasLanguage( CultureInfo culture ) =>
			this._settings.Translator.HasCulture( culture );

		public bool HasLanguage( string name ) =>
			ValidateCulture( name ) && HasLanguage( CultureInfo.GetCultureInfo( name ) );

		public CultureInfo GetLanguage( string name ) =>
			HasLanguage( name ) ? CultureInfo.GetCultureInfo( name ) : this.DefaultCulture;

		/// <summary>Finds the dot-notation-declared child from the data tree.</summary>
		/// <param name="dereference">A string containing the destination child to find/retrieve.</param>
		/// <returns>If found, the requested object, otherwise null.</returns>
		/// <remarks>The return types here can only be LanguageMethod or LanguageClass objects (or NULL).</remarks>
		private LanguageBase GetTargetObject( string dereference )
		{
			if ( PathMatch.IsMatch( dereference ) )
			{
				KeyValuePair<string, string> split = LanguageBase.SplitDotString( dereference );
				return this[ split.Key ].GetTargetObject( split.Value );
			}

			return null;
		}

		/// <summary>Finds a prompt based on a dot-separated tree.</summary>
		/// <param name="dereference">A string containing the dot-separated list of names to dereference.</param>
		/// <param name="culture">The Culture/Language to return.</param>
		/// <returns>A string of text pulled from the prompt library from the dereferenced location.</returns>
		/// <remarks>
		/// "dereference" Schema: [assemblyName]:[namespaces].[method].[fieldName]
		/// "namespaces" contains the dot-separated list of name space names AND the classname to be dereferenced!
		/// </remarks>
		public string GetByName( string dereference, CultureInfo culture = null )
		{
			// NOTE: Using backreferences to the Language library causes infinite recursion,
			//		 so Prompts in here MUST be left as statically defined strings.

			//Regex pattern = new Regex( @"^(?<path>(?<nameSpace>[a-z][\w]*)[.](?<method>(?:[a-z][\w]*[.])+))(?<field>[a-z][\w]*)$", RegexOptions.IgnoreCase );
			//Regex pattern = new Regex( @"^(([a-z][\w]*[a-z0-9])[.])+([a-z][\w]*[a-z0-9]|[0-9]{1,2})$", RegexOptions.IgnoreCase );
			string errMsg = "Prompt Not Found"; // Language.Prompt.Get( 0 );
			LanguageBase target = GetTargetObject( dereference );
			if ( !(target is null) && target.GetType().Equals(typeof(LanguageMethod)) )
			{
				culture = (culture is null) || culture.Equals( CultureInfo.InvariantCulture ) ? DefaultCulture : culture;
				try { return (target as LanguageMethod).GetChild( DotStringLast(dereference), culture ).Replace( "$r", "\r" ).Replace( "$n", "\n" ); }
				catch ( Exception e ) { errMsg = e.Message; }
			}
			return $"Resource Failure [{culture}]: {errMsg}: \"{dereference}\"";
			// Language.Prompt.Get( 1, new object[] { culture, errMsg, dereference } );  
		}

		public string GetByName( string dereference, object[] data, CultureInfo culture = null ) =>
			GetByName( dereference, culture ).Replace( data );

		public string Get( object[] indexes, CultureInfo culture = null )
		{
			string result = "";

			System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace();
			string name = trace.GetFrame( 1 ).GetMethod().DeclaringType.FullName;
			foreach ( var index in indexes )
				result += this.GetByName( $"{name}.{index}", culture );
			return result;
		}

		public string Get( object index, object[] data, CultureInfo culture = null )
		{
			MethodBase mb = (new System.Diagnostics.StackTrace()).GetFrame( 1 ).GetMethod();
			return this.GetByName( $"{mb.DeclaringType.FullName}.{mb.Name}.{index}", culture ).Replace( data );
		}

		public string Get( object index, CultureInfo culture = null )
		{
			MethodBase mb = (new System.Diagnostics.StackTrace()).GetFrame( 1 ).GetMethod();
			return this.GetByName( $"{mb.DeclaringType.FullName}.{mb.Name}.{index}", culture );
		}

		public Exception GetExceptionByName( string dereference, CultureInfo culture = null, object[] data = null, Exception innerException = null )
		{
			// NOTE: Using backreferences to the Language library here causes infinite recursion,
			//		 so Prompts in here have to be left as statically defined strings.

			string errMsg = "Exception not found"; // Language.Prompt.Get( 0 );
			LanguageBase target = GetTargetObject( dereference );
			if ( !(target is null) )
			{
				culture = (culture is null) || culture.Equals( CultureInfo.InvariantCulture ) ? DefaultCulture : culture;
				try 
				{ 
					switch (target.GetType().Name)
					{
						case "LanguageMethod":
							return (target as LanguageMethod).Exceptions[ DotStringLast( dereference ) ].CreateException( culture, data, innerException );
						case "LanguageClass":
							return (target as LanguageClass).Exceptions[ DotStringLast( dereference ) ].CreateException( culture, data, innerException );
					}
					errMsg = $"Unrecognized Class Returned: \x22{target.GetType()}\x22";
					// Language.Prompt.Get( 1, new object[] { target.GetType() } );
				}
				catch ( Exception e ) { errMsg = e.Message; }
			}
			return new KeyNotFoundException( $"Resource Failure [{culture}]: {errMsg}: \"{dereference}\"" );
			// Language.Prompt.GetException( 0, new object[] { culture, errMsg, dereference } );
		}

		public Exception GetException( object index, CultureInfo culture, object[] data = null, Exception innerException = null )
		{
			MethodBase mb = (new System.Diagnostics.StackTrace()).GetFrame( 1 ).GetMethod();
		return this.GetExceptionByName( $"{mb.DeclaringType.FullName}.{mb.Name}.{index}", culture, data, innerException );
		}

		public Exception GetException( object index, object[] data = null, Exception innerException = null ) =>
			GetException( index, null, data, innerException );

		public override string ToString() =>
			$"{this.Name}: {Count} NameSpaces";

		#region Xml Functions
		/// <summary>Compiles the contents of this object into a new XmlDocument object.</summary>
		public XmlDocument ToXmlDoc()
		{
			string root = NetXpertExtensions.Xml.XML.HEADER;
			foreach ( LanguageNameSpace ns in _data )
				root += ns.ToXmlNode().OuterXml + "\r\n";

			XmlDocument doc = new XmlDocument();
			doc.LoadXml( root );

			return doc;
		}

		public override void Parse( XmlNode node )
		{
			if ( LanguageBase.PreParse( node, "project" ) )
			{
				base.ParseXmlNode( node );

				if ( string.IsNullOrEmpty( this.Name ) ) this.Name = Assembly.GetEntryAssembly().GetName().Name;

				if ( LanguageSettings.HasSettingsNode( node ) )
					this._settings = new LanguageSettings( node );

				XmlNode[] nodes = node.GetNamedElements( "namespace", "name" );
				foreach ( XmlNode ns in nodes )
					this.Add( new LanguageNameSpace( ns ) );
			}
		}

		public void Parse( XmlDocument doc )
		{
			XmlNode[] nodes = doc.GetNamedElements( "project" );
			if ( nodes.Length > 0 )
				Parse( nodes[ 0 ] );
		}

		public override XmlNode ToXmlNode()
		{
			XmlNode node = ("<project" + (string.IsNullOrEmpty( Name ) ? "" : $" name='{Name}'") + "/>").ToXmlNode();

			if ( !(_settings is null) )
				node.AppendChild( _settings.ToXmlNode() );

			foreach ( LanguageNameSpace ns in this._data )
				node.AppendChild( ns.ToXmlNode() );

			return node;
		}
		#endregion

		#region IO Functions
		public void ImportResources( LanguageManager newResources )
		{
			if ( !(newResources is null) )
				foreach ( LanguageNameSpace ns in newResources )
					this.Add( ns );

			this._settings.Import( newResources._settings );
		}

		public void SaveFile( string fileName = "" )
		{
			if ( string.IsNullOrWhiteSpace( fileName ) )
				fileName = ConfigManagement.ConfigManagementBase.ExecutablePath( MiscellaneousExtensions.ExecutableName );

			string path = Path.GetDirectoryName( fileName ), file = Path.GetFileNameWithoutExtension( fileName );
			fileName = (string.IsNullOrWhiteSpace( path ) ? "." : path) + "\\" + file + (Regex.IsMatch( file, @".res$", RegexOptions.IgnoreCase ) ? "" : ".res") + ".xml";
			File.WriteAllText( fileName, XML.HEADER + this.ToString() );
		}

		/// <summary>Saves a compiled resource configuration to disk.</summary>
		/// <param name="fileName">The path + filename of the file to write.</param>
		public byte[] ExportResourceFile( string fileName )
		{
			if ( File.Exists( fileName ) ) File.Delete( fileName );
			string data = this.ToXmlDoc().OuterXml;
			byte[] output = TextCompress.Compress( data );
			File.WriteAllBytes( fileName, output );
			//byte[] encr = SimpleAES.Encrypt( output, CipherKey );
			//File.WriteAllBytes( fileName, encr );
			return output;
		}

		///<summary>Loads a specified XML file and adds it to the collection.</summary>
		public static LanguageManager LoadFile( string fileName = "" )
		{
			LanguageManager result = new LanguageManager();
			if ( string.IsNullOrWhiteSpace( fileName ) )
				fileName = ConfigManagement.ConfigManagementBase.ExecutablePath( MiscellaneousExtensions.ExecutableName );

			string path = Path.GetDirectoryName( fileName ), file = Path.GetFileNameWithoutExtension( fileName );
			fileName = (string.IsNullOrWhiteSpace( path ) ? "." : path) + "\\" + file + (Regex.IsMatch( file, @".res$", RegexOptions.IgnoreCase ) ? "" : ".res") + ".xml";
			if ( File.Exists( fileName ) )
			{
				XmlDocument doc = new XmlDocument();
				doc.Load( fileName );
				result.Parse( doc );

				return result;
			}
			else //throw Language.Prompt.GetException( 0, new object[] { fileName } );
				throw new FileNotFoundException( $"The requested file (\"{fileName}\") was not found!" );
		}

		/// <summary>Loads a compiled string resource file and adds it to the collection.</summary>
		public static LanguageManager LoadResourceFiles( Assembly ass, string fileName = null )
		{
			string FileNotFound()
			{
				// Attempt to load a raw XML version instead, if one exists...
				string name = ass.ExtractName(), fName = (string.IsNullOrWhiteSpace( fileName ) ? $"{name}.ExternalResources.{name}" : fileName) + ".res.xml";
				using ( Stream rawXmlStream = ass.GetManifestResourceStream( fName ) )
				{
					if ( !(rawXmlStream is null) && rawXmlStream.CanRead )
					{
						char[] charData;
						using ( BinaryReader sr = new BinaryReader( rawXmlStream ) )
							charData = sr.ReadChars( (int)rawXmlStream.Length );

						return string.Concat( charData );
					}
					else
					{
						fName = $"{MiscellaneousExtensions.LocalExecutablePath}{name}.res.xml";
						if ( File.Exists( fName ) )
							return File.ReadAllText( fName );

						fName = $"{MiscellaneousExtensions.LocalExecutablePath}ExternalResources\\{name}.res.xml";
						if ( File.Exists( fName ) )
							return File.ReadAllText( fName );
					}
				}
				return "";
			}

			LanguageManager result = new LanguageManager( "LoadResourceFiles" );
			result.DefaultCulture = CultureInfo.CurrentCulture;

			string rawData = "", fName = "";

			try
			{
				byte[] data = null;
				fName = (string.IsNullOrWhiteSpace( fileName ) ? $"{ass.ExtractName()}.ExternalResources.{ass.ExtractName()}" : fileName) + ".netx";
				using ( Stream stream = ass.GetManifestResourceStream( fName ) )
				{
					if ( !(stream is null) )
					{
						using ( BinaryReader sr = new BinaryReader( stream ) )
							data = sr.ReadBytes( (int)stream.Length );

						//data = SimpleAES.Decrypt( data, CipherKey );
						string raw = TextCompress.TextUncompress( data );
						rawData += Regex.Replace( raw, @"^\\<\\?xml [\s\S]*\\?\\>", "", RegexOptions.IgnoreCase );
					}
					else
						rawData = FileNotFound();
				}
			}
			catch ( FileNotFoundException fnfe ) { rawData = FileNotFound(); }
			catch ( FileLoadException fle ) { rawData = FileNotFound(); }

			if ( !string.IsNullOrWhiteSpace( rawData ) )
				result.Parse( rawData.ToXmlNode() );

			return result;
		}

		public static LanguageManager AutoLoadResources()
		{
			LanguageManager result = new LanguageManager();
			result.DefaultCulture = CultureInfo.CurrentCulture;

			foreach ( Assembly ass in AppDomain.CurrentDomain.GetAssemblies() )
				result.ImportResources( LoadResourceFiles( ass ) );

			return result;
		}
		#endregion
		#endregion
	}
	#endregion

	public static class Language
	{
		public static LanguageManager Prompt { get; set; } = 
			LanguageManager.AutoLoadResources();
	}
}
