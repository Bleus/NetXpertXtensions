using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using NetXpertCodeLibrary.ConfigManagement;
using NetXpertCodeLibrary.Extensions;
using NetXpertExtensions;
using NetXpertExtensions.Xml;

namespace NetXpertCodeLibrary
{
	public sealed class LanguageTranslator : CultureInfo
	{
		private CultureInfo _translationLanguage;
		private string _name = CultureInfo.InvariantCulture.ToString();

		public LanguageTranslator( CultureInfo translate, CultureInfo alias ) : base( (alias is null ? InvariantCulture : alias).ToString() )
		{
			_translationLanguage = translate is null ? CurrentCulture : translate;
			Name = translate.Name;
		}

		public LanguageTranslator( string translate, string alias ) : base( string.IsNullOrWhiteSpace( alias ) ? InvariantCulture.ToString() : alias )
		{
			_translationLanguage = string.IsNullOrWhiteSpace( translate ) ? CurrentCulture : new CultureInfo( translate );
			Name = translate;
		}

		new public string Name
		{
			get => _name;
			private set { if ( !string.IsNullOrWhiteSpace( value ) && IsValidName( value ) ) { _name = value; } }
		}

		public string Alias => base.Name;

		public string ToXmlString() =>
			$"<translation name='{_translationLanguage}' alias='{base.ToString()}' />";

		public XmlNode ToXmlNode() => ToString().ToXmlNode();

		public static LanguageTranslator Parse( string source ) => Parse( (XmlNode)source.ToXmlNode() );

		public static LanguageTranslator Parse( XmlNode node )
		{
			if (
				!(node is null) && 
				node.Name.Equals( "translation", StringComparison.OrdinalIgnoreCase ) && 
				node.HasAttribute("name") && IsValidName( node.Attributes[ "name" ].Value ) && 
				node.HasAttribute( "alias" ) && IsValidName( node.Attributes["alias"].Value )
				)
					return new LanguageTranslator( node.Attributes["name"].Value, node.Attributes[ "alias" ].Value );

			throw new SyntaxErrorException( "The supplied XmlNode could not be parsed." );
		}

		/// <summary>Reports on whether a supplied string conforms to the "xx-XX" naming pattern.</summary>
		private static bool IsValidName( string s ) => 
			!string.IsNullOrWhiteSpace( s ) && Regex.IsMatch( s, @"^[a-z]{2}[-][A-Z]{2}$" );
	}

	/// <summary>Manages a single static string entry.</summary>
	public sealed class StringResourceEntity
	{
		#region Properties
		private CultureInfo _culture = CultureInfo.InvariantCulture;
		private string _payload = "";
		private string _comment = "";
		#endregion

		#region Constructor
		public StringResourceEntity( string payload, CultureInfo culture = null )
		{
			_culture = culture is null ? DefaultCulture : culture;
			Payload = string.IsNullOrEmpty( payload ) ? string.Empty : payload;
		}

		public StringResourceEntity( XmlNode node ) => Parse( node );
		#endregion

		#region Operators
		public static implicit operator XmlNode( StringResourceEntity source ) => source.ToXmlNode();
		public static implicit operator StringResourceEntity( XmlNode node ) => new StringResourceEntity( node );
		#endregion

		#region Accessors
		public CultureInfo DefaultCulture { get; set; } = CultureInfo.InvariantCulture;

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

		public string Payload
		{
			get => _payload.XmlDecode();
			set => _payload = value is null ? String.Empty : value.XmlEncode();
		}

		public string Comment
		{
			get => UnencodeXmlAttributeValue( _comment );
			set => _comment = EncodeXmlAttributeValue( value );
		}

		public CultureInfo Culture
		{
			get => _culture;
			set => _culture = value is null ? DefaultCulture : value;
		}
		#endregion

		#region Methods
		public string Params( object[] values ) =>
			(values is null) ? Payload : Payload.Replace( values );

		public void Parse( XmlNode node )
		{
			if ( node.Name.Equals( "data", StringComparison.OrdinalIgnoreCase ) && node.HasAttribute( "culture" ) )
			{
				_culture = new CultureInfo( node.Attributes[ "culture" ].Value );
				_payload = Regex.Replace( node.InnerXml.Trim(), @"[\r\n]+", " " );
				this._comment = node.HasAttribute( "comment" ) ? node.Attributes[ "comment" ].Value : String.Empty;
			}
		}

		public void Parse( string source ) => Parse( (XmlNode)source.ToXmlNode() );

		public override string ToString() =>
			$"<data culture='{_culture}'" + (string.IsNullOrWhiteSpace( _comment ) ? "" : $" comment='{_comment}'") + $">{_payload}</data>\r\n";

		public XmlNode ToXmlNode() => this.ToString().ToXmlNode();

		public static bool ValidateName( string name ) =>
			!string.IsNullOrWhiteSpace( name ) && Regex.IsMatch( name, @"^[a-z][\w]*[a-z0-9]$", RegexOptions.IgnoreCase );

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
		#endregion
	}

	/// <summary>Manages a collection of StringResourceEntity objects for a single prompt in multiple languages.</summary>
	public sealed class StringResourceEntityCollection
	{
		#region Properties
		private List<StringResourceEntity> _prompts = new List<StringResourceEntity>();
		private string _index;
		private CultureInfo _defaultCulture = CultureInfo.InvariantCulture;
		private string _comment = "";
		#endregion

		#region Constructors
		public StringResourceEntityCollection( string index )
		{
			if ( string.IsNullOrWhiteSpace(index) )
				throw new ArgumentOutOfRangeException( $"You must supply a valid index to instantiate this class. ({index})" );

			Index = index;
		}

		public StringResourceEntityCollection( XmlNode node, CultureInfo defaultCulture = null ) => 
			Parse( node, defaultCulture );
		#endregion

		#region Operators
		public static implicit operator XmlNode( StringResourceEntityCollection source ) => source.ToXmlNode();
		public static implicit operator StringResourceEntityCollection( XmlNode node ) => new StringResourceEntityCollection( node );
		#endregion

		#region Accessors
		public string Index
		{
			get => this._index;
			set
			{
				if ( !string.IsNullOrWhiteSpace(value) && string.IsNullOrEmpty(_index) )
					this._index = value;
			}
		}

		public int Count => _prompts.Count;

		public string Comment
		{
			get => StringResourceEntity.UnencodeXmlAttributeValue( _comment );
			set => _comment = StringResourceEntity.EncodeXmlAttributeValue( value );
		}

		public string this[ CultureInfo culture ]
		{
			get
			{
				int i = IndexOf( culture is null ? DefaultCulture : culture );
				return i >= 0 ? this._prompts[ i ].Payload : string.Empty;
			}
		}

		public CultureInfo DefaultCulture 
		{
			get => _defaultCulture; 
			set
			{
				if (!(value is null))
				{
					this._defaultCulture = value;
					for ( int i = 0; i < Count; i++ )
						this._prompts[ i ].DefaultCulture = value;
				}
			}
		}
		#endregion

		#region Methods
		private int IndexOf( CultureInfo culture )
		{
			int i = -1;
			if ( !(culture is null) )
				while ( (++i < Count) && !culture.Equals( _prompts[ i ].Culture ) ) ;

			return (i < Count) ? i : -1;
		}

		public void Add( CultureInfo culture, string value )
		{
			if ( !(culture is null) )
				this.Add( new StringResourceEntity( value, culture ) { DefaultCulture = this.DefaultCulture } );
		}

		public void Add( StringResourceEntity item )
		{
			int i = IndexOf( item.Culture );
			if ( i < 0 )
				this._prompts.Add( item );
			else
				this._prompts[ i ] = item;
		}

		public void AddRange( StringResourceEntity[] items )
		{
			foreach ( StringResourceEntity entity in items )
				this.Add( entity );
		}

		public void Remove( CultureInfo culture )
		{
			int i = IndexOf( culture );
			if ( i >= 0 ) _prompts.RemoveAt( i );
		}

		public void Clear() => this._prompts.Clear();

		public override string ToString()
		{
			string result = $"<prompt id='{Index}'" + (string.IsNullOrWhiteSpace(_comment) ? "" : $" comment='{_comment}'") +">\r\n";
			foreach ( StringResourceEntity line in _prompts )
				result += line.ToString();
			return result + "</prompt>\r\n";
		}

		public StringResourceEntity[] ToArray() => _prompts.ToArray();

		public XmlNode ToXmlNode() => this.ToString().ToXmlNode();

		public void Parse( XmlNode node, CultureInfo defaultCulture = null )
		{
			this.DefaultCulture = (defaultCulture is null) ? CultureInfo.InvariantCulture : defaultCulture;
			if ( node.Name.Equals( "prompt", StringComparison.OrdinalIgnoreCase ) && node.HasAttribute( "id" ) )
			{
				if ( !node.HasAttribute("id") || !ValidateId( node.Attributes[ "id" ].Value ) )
					throw new InvalidDataException( "You cannot parse a node with a missing id." );

				_comment = node.HasAttribute( "comment" ) ? node.Attributes[ "comment" ].Value : String.Empty;
				this._index = node.Attributes[ "id" ].Value;
				XmlNode[] prompts = node.GetNamedElements( "data", "culture" );
				foreach ( XmlNode prompt in prompts )
					this.Add( new StringResourceEntity( prompt ) );
			}
		}

		public void Parse( string rawData ) => Parse( rawData.ToXmlNode() );

		public static bool ValidateId( string id ) =>
			!string.IsNullOrWhiteSpace( id ) && Regex.IsMatch( id, @"^(?:[a-z][\w]*[a-z\d])$", RegexOptions.IgnoreCase );
		#endregion
	}

	/// <summary>Manages all of the StringResourceEntityCollection prompts for an individual Method.</summary>
	public sealed class StringResourceMethodCollection
	{
		#region Properties
		private string _name = "";
		private SortedDictionary<string, StringResourceEntityCollection> _prompts = new SortedDictionary<string, StringResourceEntityCollection>();
		private CultureInfo _defaultCulture = CultureInfo.InvariantCulture;
		private string _comment = "";
		#endregion

		#region Constructors
		public StringResourceMethodCollection( string name, CultureInfo defaultCulture = null )
		{
			Name = name;
			DefaultCulture = defaultCulture is null ? CultureInfo.InvariantCulture : defaultCulture;
		}

		public StringResourceMethodCollection( XmlNode node, CultureInfo defaultCulture = null ) => Parse( node, defaultCulture );
		#endregion

		#region Accessors
		public int Count => _prompts.Count;

		public StringResourceEntityCollection this[ string index ] =>
			this._prompts.ContainsKey( index ) ? this._prompts[ index ] : null;

		public string Name
		{
			get => _name;
			set
			{
				if ( ValidateName( value ) )
					_name = value;
			}
		}

		public string Comment
		{
			get => StringResourceEntity.UnencodeXmlAttributeValue( _comment );
			set => _comment = StringResourceEntity.EncodeXmlAttributeValue( value );
		}

		public CultureInfo DefaultCulture
		{
			get => _defaultCulture;
			set
			{
				if (!(value is null))
				{
					this._defaultCulture = value;
					foreach ( string key in _prompts.Keys )
						_prompts[ key ].DefaultCulture = value;
				}
			}
		}
		#endregion

		#region Methods
		public void Add( StringResourceEntityCollection newPrompt )
		{

			if ( this._prompts.ContainsKey( newPrompt.Index ) )
				this._prompts.Remove( newPrompt.Index );

			this._prompts.Add( newPrompt.Index, newPrompt );
		}

		public void Remove( string index )
		{
			if ( this._prompts.ContainsKey( index ) )
				this._prompts.Remove( index );
		}

		public void Clear() => this._prompts.Clear();

		public override string ToString()
		{
			string result = $"<method name='{Name}' default='{DefaultCulture}'" + (string.IsNullOrWhiteSpace(_comment) ? "" : $" comment='{_comment}'") + ">\r\n";
			foreach ( KeyValuePair<string, StringResourceEntityCollection> prompt in _prompts )
				result += prompt.Value.ToString();
			return result + "</method>\r\n";
		}

		public XmlNode ToXmlNode() => this.ToString().ToXmlNode();

		public void Parse( XmlNode node, CultureInfo defaultCulture = null )
		{
			if ( defaultCulture is null ) defaultCulture = DefaultCulture; else DefaultCulture = defaultCulture;
			if ( !(node is null) && node.Name.Equals( "method", StringComparison.OrdinalIgnoreCase ) && node.HasAttribute( "name" ) )
			{
				_comment = node.HasAttribute( "comment" ) ? node.Attributes[ "comment" ].Value : String.Empty;
				this.Name = node.Attributes[ "name" ].Value;
				if ( node.HasAttribute( "default" ) )
					this.DefaultCulture = new CultureInfo( node.Attributes[ "default" ].Value );
				XmlNode[] prompts = node.GetNamedElements( "prompt", "id" );
				foreach ( XmlNode prompt in prompts )
					this.Add( new StringResourceEntityCollection( prompt, defaultCulture ) );
			}
		}

		public void Parse( string rawData ) => Parse( rawData.ToXmlNode() );

		public StringResourceEntityCollection[] ToArray()
		{
			List<StringResourceEntityCollection> items = new List<StringResourceEntityCollection>();
			foreach ( KeyValuePair<string, StringResourceEntityCollection> prompt in _prompts )
				items.Add( prompt.Value );

			return items.ToArray();
		}

		public static bool ValidateName( string name ) =>
			!string.IsNullOrWhiteSpace( name ) && Regex.IsMatch( name, @"^((([*]{2})[a-z][\w]*)|([a-z][\w]*[a-z0-9][.]?)+)$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture );
		#endregion
	}

	public sealed class StringResourcesNamespace
	{
		#region Properties
		private List<CultureInfo> _supportedCultures = new List<CultureInfo>();
		private string _name = "";
		private CultureInfo _defaultCulture = CultureInfo.InvariantCulture;
		private Dictionary<string, StringResourceMethodCollection> _methods = new Dictionary<string, StringResourceMethodCollection>();
		#endregion

		#region Constructors
		public StringResourcesNamespace( string name, CultureInfo defaultCulture = null )
		{
			Name = name;
			DefaultCulture = defaultCulture is null ? CultureInfo.InvariantCulture : defaultCulture;
		}

		public StringResourcesNamespace( XmlNode node, CultureInfo defaultCulture = null )
		{
			DefaultCulture = defaultCulture;
			Parse( node );
		}
		#endregion

		#region Accessors
		public int Count => _methods.Count;

		public int CultureCount => _supportedCultures.Count;

		public StringResourceMethodCollection this[ string fullMethodName ] =>
			_methods.ContainsKey( fullMethodName ) ? _methods[ fullMethodName ] : null;

		public string Name
		{
			get => _name;
			set
			{
				if ( StringResourceEntity.ValidateName( value ) )
					_name = value;
			}
		}

		public CultureInfo DefaultCulture
		{
			get => _defaultCulture;
			set
			{
				if ( !(value is null) && _supportedCultures.Contains( value ) )
				{
					_defaultCulture = value;
					foreach( string method in this._methods.Keys )
						this._methods[ method ].DefaultCulture = value;
				}
			}
		}
		#endregion

		#region Methods
		public void Add( StringResourceMethodCollection method )
		{
			if ( !(method is null) )
			{
				if ( this._methods.ContainsKey( method.Name ) )
				{
					foreach ( StringResourceEntityCollection m in method.ToArray() )
						this._methods[ method.Name ].Add( m );
				}
				else
					this._methods.Add( method.Name, method );
			}
		}

		public void AddLanguage( CultureInfo culture )
		{
			if ( !(culture is null) && !_supportedCultures.Contains( culture ) )
				_supportedCultures.Add( culture );
		}

		public void RemoveLanguage( CultureInfo culture )
		{
			if ( !(culture is null) && _supportedCultures.Contains( culture ) )
				_supportedCultures.Remove( culture );
		}

		public void ClearLanguages()
		{
			this._supportedCultures.Clear();
			_supportedCultures.AddRange( new CultureInfo[] { CultureInfo.CurrentCulture, CultureInfo.InvariantCulture } );
		}

		public override string ToString()
		{
			string result = $"<resource name='{Name}'>\r\n";
			result +=
				$"<settings>\r\n<defaultCulture name='{DefaultCulture}' />\r\n" +
				$"<methodCount>{Count}</methodCount>\r\n" +
				$"<languages count='{CultureCount}'>\r\n";

			foreach ( object culture in _supportedCultures )
				result += culture.GetType().Equals( typeof( LanguageTranslator ) )
					? $"<translation name='{(culture as LanguageTranslator).Name}' alias='{(culture as LanguageTranslator).Alias}' />"
					: $"<language name='{culture as CultureInfo}'>{(culture as CultureInfo).NativeName}</language>\r\n";

			result += "</languages>\r\n</settings>\r\n";

			foreach ( KeyValuePair<string, StringResourceMethodCollection> method in _methods )
				result += method.Value.ToString();

			return result + "</resource>";
		}

		public XmlNode ToXmlNode() => this.ToString().ToXmlNode();

		public void Parse( XmlNode node )
		{
			if ( !(node is null) && node.Name.Equals( "resource", StringComparison.OrdinalIgnoreCase ) && node.HasAttribute( "name" ) && node.HasChildNodes )
			{
				int methodCount = 0;
				this.Name = node.Attributes[ "name" ].Value;
				XmlNode settings = node.GetFirstNamedElement( "settings" );
				if ( !(settings is null) )
				{
					XmlNode work = settings.GetFirstNamedElement( "methodCount" );
					if ( !(work is null) && Regex.IsMatch( work.InnerText, @"^[\d]+$" ) )
						methodCount = int.Parse( work.InnerText );

					work = settings.GetFirstNamedElement( "languages", "count" );
					if ( !(work is null) )
					{
						this.ClearLanguages();
						XmlNode[] languages = work.GetNamedElements( "language", "name" );
						if ( !(languages is null) && (languages.Length > 0) )
						foreach ( XmlNode language in languages )
							this.AddLanguage( new CultureInfo( language.Attributes[ "name" ].Value ) );

						languages = work.GetNamedElements( "translation", "name" );
						foreach ( XmlNode translation in languages )
						{
							LanguageTranslator translator = LanguageTranslator.Parse( translation );
							if ( 
								!_supportedCultures.Contains( new CultureInfo( translator.Name ) ) && 
								_supportedCultures.Contains( new CultureInfo( translator.Alias ) ) 
								)
								_supportedCultures.Add( translator );
						}
					}

					work = settings.GetFirstNamedElement( "defaultCulture", "name" );
					if ( !(work is null) )
						this.DefaultCulture = new CultureInfo( work.Attributes[ "name" ].Value );
				}

				XmlNode[] methods = node.GetNamedElements( "method", "name" );
				if ( methods.Length > 0 )
					foreach ( XmlNode method in methods )
						this.Add( new StringResourceMethodCollection( method, this.DefaultCulture ) );
			}
		}

		public void Parse( string rawData ) => Parse( (XmlNode)rawData.ToXmlNode() );

		public string GetByName( string dereference, CultureInfo culture = null )
		{
			Regex pattern = new ( @"^(?<longname>[*]{2}[a-z][\w]*[a-z\d]|(?:[.]?(?<method>[a-z][\w]*[a-z\d]))+)[.](?<id>[a-z][\w]*[a-z\d])$", RegexOptions.IgnoreCase );
			if ( pattern.IsMatch( dereference ) )
			{
				culture = culture is null ? DefaultCulture : culture;
				Match m = pattern.Match( dereference );
				if ( m.Groups[ "name" ].Success && m.Groups[ "method" ].Success && m.Groups[ "id" ].Success )
				{
					try
					{
						StringResourceMethodCollection s1 = this[ m.Groups[ "longname" ].Value ];
						StringResourceEntityCollection s2 = s1[ m.Groups[ "id" ].Value ];
						return s2[ culture ];
					}
					catch (Exception e) 
					{
						return $"Resource Failure [{culture}]: {e.Message}";
					}
				}
			}
			return $"Resource Failure [{culture}]: Reference not found: \"{dereference}\"";
		}

		public string GetByName( string dereference, object[] data, CultureInfo culture = null ) =>
			GetByName( dereference, culture ).Replace( data );

		public string Get( string[] indexes, CultureInfo culture = null )
		{
			string result = "";

			System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace();
			string name = trace.GetFrame( 1 ).GetMethod().DeclaringType.FullName;
			foreach ( string index in indexes )
				result += this.GetByName( $"{name}.{index}", culture );
			return result;
		}

		public string Get( string index, object[] data, CultureInfo culture = null )
		{
			System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace();
			string name = trace.GetFrame( 1 ).GetMethod().DeclaringType.FullName;
			return this.GetByName( $"{name}.{index}", culture ).Replace( data );
		}

		public string Get( string index, CultureInfo culture = null )
		{
			System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace();
			string name = trace.GetFrame( 1 ).GetMethod().DeclaringType.FullName;
			return this.GetByName( $"{name}.{index}", culture );
		}

		public StringResourceMethodCollection[] ToArray()
		{
			List<StringResourceMethodCollection> items = new ();
			foreach ( KeyValuePair<string, StringResourceMethodCollection> method in this._methods )
				items.Add( method.Value );
			return items.ToArray();
		}

		public CultureInfo[] Languages => this._supportedCultures.ToArray();

		public bool HasLanguage( ref string test )
		{
			bool result = !(test is null) && Regex.IsMatch( test, @"^[a-z]{2}-[a-z]{2}$", RegexOptions.IgnoreCase );
			if (result)
			{
				test = test.Substring( 0, 2 ).ToLowerInvariant() + "-" + test.Substring( 3 ).ToUpperInvariant();
				result = HasLanguage( new CultureInfo( test ) );
			}
			return result;
		}

		public bool HasLanguage( CultureInfo culture ) => 
			!(culture is null) && this._supportedCultures.Contains( culture );

		/// <summary>Saves the contents of this object as a plain-text XML file.</summary>
		public void SaveFile( string fileName = "" )
		{
			if ( string.IsNullOrWhiteSpace( fileName ) )
				fileName = ConfigManagement.ConfigManagementBase.ExecutablePath( MiscellaneousExtensions.ExecutableName );

			string path = Path.GetDirectoryName( fileName ), file = Path.GetFileNameWithoutExtension( fileName );
			fileName = (string.IsNullOrWhiteSpace( path ) ? "." : path) + "\\" + file + (Regex.IsMatch( file, @".res$", RegexOptions.IgnoreCase ) ? "" : ".res") + ".xml";
			string data = XML.HEADER + $"<project name='{Name}'>{this}</project>";
			File.WriteAllText( fileName, data );
		}

		/// <summary>Saves a compiled resource configuration to disk.</summary>
		/// <param name="fileName">The path + filename of the file to write.</param>
		public void ExportResourceFile( string fileName )
		{
			XmlDocument doc = new();
			doc.LoadXml( XML.HEADER + this.ToString() );

			if ( File.Exists( fileName ) ) File.Delete( fileName );
			File.WriteAllBytes( fileName, TextCompress.Compress( doc.OuterXml ) );
		}
		#endregion
	}

	public sealed class StringResources
	{
		#region Properties
		//private List<CultureInfo> _supportedCultures = new List<CultureInfo>();
		private string _name = "";
		private CultureInfo _defaultCulture = CultureInfo.InvariantCulture;
		private Dictionary<string, StringResourcesNamespace> _namespaces = new Dictionary<string, StringResourcesNamespace>();
		private static readonly AESKey CipherKey = // Used to de/en-crypt compressed string files
			AESKey.Parse(
				new byte[]
				{
					0x7B,0x67,0x5A,0x63,0x48,0x59,0x71,0x43,0x6E,0x54,0x45,0x4F,0x48,0x71,0x66,0x37,0x4E,
					0x79,0x47,0x4D,0x48,0x78,0x48,0x4D,0x62,0x79,0x48,0x63,0x6F,0x4C,0x5A,0x74,0x6E,0x57,
					0x54,0x57,0x6D,0x54,0x47,0x77,0x67,0x45,0x33,0x67,0xA7,0x70,0x32,0x63,0x48,0x52,0x35,
					0x7A,0x68,0x70,0x43,0x7A,0x68,0x43,0x35,0x63,0x48,0x54,0x45,0x4F,0x6E,0x6A,0x50,0x7D
				}
			);
		#endregion

		//public StringResources() { }

		#region Accessors
		/// <summary>Reports the number of namespaces currently being served.</summary>
		public int Count => _namespaces.Count;

		/// <summary>Reports the total number of methods contained across all namespaces.</summary>
		public int MethodCount
		{
			get
			{
				int result = 0;
				foreach ( KeyValuePair<string, StringResourcesNamespace> nameSpace in _namespaces )
					result += nameSpace.Value.Count;
				return result;
			}
		}

		/// <summary>Reports the total number of unique cultures supported by this library.</summary>
		public int CultureCount => SupportedCultures.Count;

		public StringResourcesNamespace this[ string namespaceName ] =>
			_namespaces.ContainsKey( namespaceName ) ? _namespaces[ namespaceName ] : null;

		public string Name
		{
			get => _name;
			set
			{
				if ( StringResourceEntity.ValidateName( value ) )
					_name = value;
			}
		}

		public List<CultureInfo> SupportedCultures
		{
			get
			{
				List<CultureInfo> _cultures = new List<CultureInfo>();
				foreach ( KeyValuePair<string, StringResourcesNamespace> nameSpace in _namespaces )
					foreach ( CultureInfo culture in nameSpace.Value.Languages )
						if ( !_cultures.Contains( culture ) )
							_cultures.Add( culture );

				return _cultures;
			}
		}

		public CultureInfo DefaultCulture
		{
			get => _defaultCulture;
			set
			{
				if ( !(value is null) && SupportedCultures.Contains( value ) )
				{
					_defaultCulture = value;
					foreach ( string method in this._namespaces.Keys )
						this._namespaces[ method ].DefaultCulture = value;
				}
			}
		}
		#endregion

		#region Methods
		public void Add( StringResourcesNamespace nameSpace )
		{
			if ( !(nameSpace is null) )
			{
				if ( this._namespaces.ContainsKey( nameSpace.Name ) )
				{
					foreach ( StringResourceMethodCollection m in nameSpace.ToArray() )
						this._namespaces[ nameSpace.Name ].Add( m );
				}
				else
					this._namespaces.Add( nameSpace.Name, nameSpace );
			}
		}

		/// <summary>Facilitates importing another StringResources object into this one.</summary>
		public void Add( StringResources resource )
		{
			foreach ( KeyValuePair<string, StringResourcesNamespace> item in resource._namespaces )
				this.Add( item.Value );
		}

		public bool HasNameSpace( string name ) => !string.IsNullOrWhiteSpace(name) && this._namespaces.ContainsKey( name );

		public void RemoveLanguage( CultureInfo culture )
		{
			if ( !(culture is null) && SupportedCultures.Contains( culture ) )
				foreach ( KeyValuePair<string,StringResourcesNamespace> nameSpace in _namespaces.ToArray() )
					this[ nameSpace.Value.Name ].RemoveLanguage( culture );
		}

		public void ClearLanguages()
		{
			foreach ( KeyValuePair<string, StringResourcesNamespace> nameSpace in _namespaces )
				nameSpace.Value.ClearLanguages();
		}

		public override string ToString()
		{
			string result = $"<project name='{Name}'>\r\n";

			foreach ( KeyValuePair<string, StringResourcesNamespace> method in _namespaces )
				result += method.Value.ToString();

			return result + "</project>";
		}

		public XmlNode ToXmlNode() => this.ToString().ToXmlNode();

		public XmlDocument ToXmlDoc()
		{
			XmlDocument doc = new ();
			doc.LoadXml( XML.HEADER + this.ToString() );
			return doc;
		}

		public void Parse( XmlNode node )
		{
			if ( 
				(node is not null) && 
				node.Name.Equals( "project", StringComparison.OrdinalIgnoreCase ) && 
				node.HasAttribute( "name" ) && node.HasChildNodes 
				)
			{
				XmlNode[] nameSpaces = node.GetNamedElements( "resource", "name" );
				if ( nameSpaces.Length > 0 )
					foreach ( XmlNode nameSpace in nameSpaces )
						this.Add( new StringResourcesNamespace( nameSpace ) );
			}
		}

		public void Parse( string rawData ) => Parse( (XmlNode)rawData.ToXmlNode() );

		public string GetByName( string dereference, CultureInfo culture = null )
		{
			Regex pattern = new ( @"^(?<namespace>[a-z][\w]*)[.](?<method>(?:[a-z][\w]*[.])+)(?<field>[a-z][\w]*)$", RegexOptions.IgnoreCase );
			string errMsg = "Reference not found";
			if ( pattern.IsMatch( dereference ) )
			{
				culture = culture is null ? DefaultCulture : culture;
				Match m = pattern.Match( dereference );
				if ( m.Groups[ "namespace" ].Success && m.Groups[ "method" ].Success && m.Groups[ "field" ].Success )
				{
					string ns = m.Groups[ "namespace" ].Value,
						method = m.Groups[ "method" ].Value.TrimEnd( new char[] { '.' } ),
						field = m.Groups[ "field" ].Value, s= "";
					try 
					{
						s = this[ ns ][ method ][ field ][ culture ];
						s = Regex.Replace( s, @"[$][rR]", "\r" );
						s = Regex.Replace( s, @"[$][nN]", "\n" );
						//s = Regex.Replace( s, @"(\\x[0-9a-fA-F]{2}(?:[0-9a-fA-F]{2})?)", "")
						s = string.IsNullOrEmpty( s ) ? $"{{No Prompt Located: {dereference}}}" : s;
						return s; 
					}
					catch ( Exception e ) { errMsg = e.Message; }
				}
			}
			return $"Resource Failure [{culture}]: {errMsg}: \"{dereference}\"";
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

		public StringResourcesNamespace[] ToArray()
		{
			List<StringResourcesNamespace> items = new ();
			foreach ( KeyValuePair<string, StringResourcesNamespace> method in this._namespaces )
				items.Add( method.Value );
			return items.ToArray();
		}

		public CultureInfo[] Languages => this.SupportedCultures.ToArray();

		public bool HasLanguage( ref string test )
		{
			bool result = !(test is null) && Regex.IsMatch( test, @"^[a-z]{2}-[a-z]{2}$", RegexOptions.IgnoreCase );
			if ( result )
			{
				test = test.Substring( 0, 2 ).ToLowerInvariant() + "-" + test.Substring( 3 ).ToUpperInvariant();
				result = HasLanguage( new CultureInfo( test ) );
			}
			return result;
		}

		public void ImportResources( StringResources newResources )
		{
			foreach ( KeyValuePair<string, StringResourcesNamespace> nameSpace in newResources._namespaces )
				this.Add( nameSpace.Value );
		}

		public bool HasLanguage( CultureInfo culture ) =>
			!(culture is null) && this.SupportedCultures.Contains( culture );

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
		public static StringResources LoadFile( string fileName = "" )
		{
			StringResources result = new ();
			if ( string.IsNullOrWhiteSpace( fileName ) )
				fileName = ConfigManagement.ConfigManagementBase.ExecutablePath( MiscellaneousExtensions.ExecutableName );

			string path = Path.GetDirectoryName( fileName ), file = Path.GetFileNameWithoutExtension( fileName );
			fileName = (string.IsNullOrWhiteSpace( path ) ? "." : path) + "\\" + file + (Regex.IsMatch( file, @".res$", RegexOptions.IgnoreCase ) ? "" : ".res") + ".xml";
			if ( File.Exists( fileName ) )
			{
				XmlDocument doc = new ();
				doc.Load( fileName );
				XmlNode project = doc.GetFirstNamedElement( "project" );
				if ( !(project is null) )
				{
					result.Parse( project ); //.ChildNodes[ 0 ] );
					result.Name = project.GetAttributeValue( "name" ); // Path.GetFileNameWithoutExtension( fileName );
				}
				return result;
			}
			else throw new FileNotFoundException( $"The requested file (\"{fileName}\") was not found!" );
		}

		/// <summary>Loads a compiled string resource file and adds it to the collection.</summary>
		public static StringResources LoadResourceFiles( Assembly ass, string fileName = null )
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
						//StringResources importXml = new StringResources();
						//importXml.Parse( rawXml );
						//result.Add( importXml );
					}
					else
					{
						fName = $"{MiscellaneousExtensions.LocalExecutablePath}{name}.res.xml";
						if ( File.Exists( fName ) )
							return File.ReadAllText( fName );

						fName = $"{MiscellaneousExtensions.LocalExecutablePath}\\ExternalResources\\{name}.res.xml";
						if ( File.Exists( fName ) )
							return File.ReadAllText( fName );
					}
				}
				return "";
			}

			StringResources result = new ();
			string rawData = "";

			try
			{
				byte[] data = null;
				string fName = (string.IsNullOrWhiteSpace( fileName ) ? $"{ass.ExtractName()}.ExternalResources.{ass.ExtractName()}" : fileName) + ".netx";
				using ( Stream stream = ass.GetManifestResourceStream( fName ) )
				{
					if ( stream is not null )
					{
						using ( BinaryReader sr = new ( stream ) )
							data = sr.ReadBytes( (int)stream.Length );

						//data = SimpleAES.Decrypt( data, CipherKey );
						string raw = TextCompress.TextUncompress( data );
						rawData += Regex.Replace( raw, @"^\\<\\?xml [\s\S]*\\?\\>", "", RegexOptions.IgnoreCase );
					}
					else
						rawData = FileNotFound();
				}
			}
			catch ( FileNotFoundException ) { rawData = FileNotFound(); }
			catch ( FileLoadException ) { rawData = FileNotFound(); }

			if (!string.IsNullOrWhiteSpace(rawData))
				result.Parse( (XmlNode)rawData.ToXmlNode() );

			if ( result.DefaultCulture.Equals(CultureInfo.InvariantCulture) && result.HasLanguage( CultureInfo.CurrentCulture) ) 
				result.DefaultCulture = CultureInfo.CurrentCulture;
			return result;
		}

		public static StringResources AutoLoadResources()
		{
			StringResources result = new();
			foreach ( Assembly ass in AppDomain.CurrentDomain.GetAssemblies() )
				result.ImportResources( LoadResourceFiles( ass ) );

			return result;
		}
		#endregion
	}

	public static class Strings
	{
		public static StringResources Resources { get; set; } = StringResources.AutoLoadResources();
	}
}
