using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using NetXpertExtensions;
using NetXpertExtensions.Xml;

namespace NetXpertCodeLibrary
{
	public sealed class xXmlNode
	{
		#region Properties
		private readonly string _tag = "";
		private string _value = "";
		private List<KeyValuePair<string, string>> _attributes = new List<KeyValuePair<string, string>>();
		private List<xXmlNode> _children = new List<xXmlNode>();
		private xXmlNode _parent = null;
		#endregion

		#region Constructors
		public xXmlNode( XmlNode source )
		{
			if ( source is null )
				throw new ArgumentNullException( "You must supply a non-null XmlNode to instantiate this object." );

			if ( source.NodeType != XmlNodeType.Element )
				throw new ArgumentException( $"Only XmlElements can be parsed into xXmlNode objects. (\x22{source.NodeType}: {source.Name}" );

			this._tag = source.Name;

			if ( !(source.Attributes is null) && (source.Attributes.Count > 0) )
				foreach ( XmlAttribute attr in source.Attributes )
					this._attributes.Add( new KeyValuePair<string,string>( attr.Name, attr.Value.XmlDecode() ) );

			string innerText = source.InnerXml;
			if ( source.HasChildNodes )
				foreach ( XmlNode child in source.ChildNodes )
					switch ( child.NodeType )
					{
						case XmlNodeType.Comment:
							break;
						case XmlNodeType.Text:
							this._value = child.InnerText;
							break;
						case XmlNodeType.Element:
						default:
							this._children.Add( (xXmlNode)child );
							innerText = innerText.Replace( child.OuterXml, "" );
							break;
					}

			this._value = innerText;
		}

		public xXmlNode( string tag, Dictionary<string,string> attributes = null, string value = "", XmlNode[] children = null )
		{
			if ( !IsValidTag( tag ) )
				throw new XmlException( $"The 'tag' value provided is unacceptable (\x22{tag}\x22)." );

			this._tag = tag;

			if ( !(attributes is null) )
				foreach ( KeyValuePair<string, string> attr in attributes )
					if ( IsValidTag( attr.Key ) )
						this._attributes.Add( new KeyValuePair<string,string>( attr.Key, attr.Value ) );

			if ( !(children is null) )
				foreach ( XmlNode node in children )
					this._children.Add( (xXmlNode)node );

			if ( !string.IsNullOrWhiteSpace( value ) ) this.Value = value;
		}
		#endregion

		#region Operators
		public static implicit operator xXmlNode(XmlNode source) => 
			source is null ? null : new xXmlNode(source);

		public static implicit operator XmlNode(xXmlNode source) =>
			source is null ? null : source.ToXmlNode();

		public static implicit operator String(xXmlNode source ) =>
			source is null ? "" : source.OuterXml;

		public static implicit operator xXmlNode(string source ) =>
			string.IsNullOrWhiteSpace( source ) ? null : (source.IsXml() ? xXmlNode.Parse( source ) : null);
		#endregion

		#region Accessors
		public string this[ string attrName ]
		{
			get => GetAttributeValue( attrName );
			set
			{
				if ( IsValidTag( attrName ) )
				{
					int i = IndexOfAttr( attrName );
					KeyValuePair<string, string> attr = new KeyValuePair<string, string>( attrName, value.XmlEncode() );
					if ( i < 0 ) // Attribute doesn't exist!
						this._attributes.Add( attr );
					else
					{
						if ( string.IsNullOrEmpty( value ) )
							this._attributes.RemoveAt( i );
						else
							this._attributes[ i ] = attr;
					}
				}
			}
		}

		public xXmlNode this[ int index ]
		{
			get => index.InRange( Count, 0, NetXpertExtensions.Classes.Range.BoundaryRule.Loop ) ? this._children[ index ] : null;
			private set
			{
				if ( index.InRange( Count, 0, NetXpertExtensions.Classes.Range.BoundaryRule.Loop) )
				{
					if ( value is null )
						this._children.RemoveAt( index );
					else
						this._children[ index ] = value;
				}
			}
		}

		public int Count => this._children.Count;

		public string Name => this._tag;

		public string Value
		{
			get => this._value.XmlDecode();
			private set => this._value = string.IsNullOrWhiteSpace( value ) ? "" : value.XmlEncode();
		}

		public string OuterXml => ToXmlNode().OuterXml;

		public string InnerXml => ToXmlNode().InnerXml;

		public XmlNodeType NodeType => XmlNodeType.Element;

		public string InnerText
		{
			get => ToXmlNode().InnerText;
			set
			{
				xXmlNode temp = $"<temp>{value}</temp>";
				this._children = temp._children;
				this._value = temp._value;
			}
		}

		public XmlAttributeCollection Attributes
		{
			get
			{
				string rawXml = "<temp";
				foreach ( KeyValuePair<string, string> attr in this._attributes )
					rawXml += $" {attr.Key}='{attr.Value.XmlEncode()}'";
				XmlNode result = $"{rawXml}></temp>".ToXmlNode();

				return result.Attributes;
			}
			set
			{
				if ( !(value is null))
					foreach ( XmlAttribute xa in value )
						this[ xa.Name ] = xa.Value;
			}
		}

		public bool HasChildNodes => this._children.Count > 0;

		public xXmlNode[] ChildNodes => this._children.ToArray();
		#endregion

		#region Methods
		private int IndexOfAttr( string attrName, bool caseSensitive = false )
		{
			int i = -1;
			StringComparison sc = caseSensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			if ( !string.IsNullOrWhiteSpace( attrName ) )
				while ( (++i < this._attributes.Count) && !this._attributes[ i ].Key.Equals( attrName, sc ) );

			return (i < this._attributes.Count) ? i : -1;
		}

		private int IndexOfElementName( string tagName, bool caseSensitive = false )
		{
			int i = -1;
			StringComparison sc = caseSensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			if ( !string.IsNullOrWhiteSpace( tagName ) )
				while ( (++i < this._children.Count) && !this._children[ i ].Name.Equals( tagName, sc ) ) ;

			return (i < this._children.Count) ? i : -1;
		}

		private int IndexOfElementByAttr( string attrName, string attrValue, bool caseSensitive = false )
		{
			int i = -1;
			StringComparison sc = caseSensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			if ( !string.IsNullOrWhiteSpace( attrName ) && !string.IsNullOrWhiteSpace( attrValue ) )
				while ( (++i < this._children.Count) && !this._children[ i ][ attrName ].Equals( attrValue, sc ) ) ;

			return (i < this._children.Count) ? i : -1;
		}

		private int IndexOf( xXmlNode item )
		{
			int i = -1;
			if ( (Count > 0) && !(item is null) )
				while ( (++i < Count) && !this._children[ i ].Equals( item ) ) ;

			return i < Count ? i : -1;
		}

		public bool HasAttribute( string attrName, bool caseSensitive = false ) => IndexOfAttr( attrName, caseSensitive ) >= 0;

		public string GetAttributeValue( string attrName )
		{
			int i = IndexOfAttr( attrName );
			return (i < 0) ? "" : this._attributes[ i ].Value.XmlDecode();
		}

		public void InsertAfter( xXmlNode newNode, xXmlNode refAfter = null )
		{
			if ( !(newNode is null) )
			{
				if ( refAfter is null )
					this._children.Add( newNode );
				else
				{
					int i = IndexOf( refAfter );
					if ( i < Count )
						this._children.Insert( i, newNode );
					else
						this._children.Add( newNode );
				}
			}
		}

		public void InsertBefore( xXmlNode newNode, xXmlNode refBefore = null )
		{
			if ( !(newNode is null) )
			{
				if ( (refBefore is null) )
				{
					if ( Count == 0 )
						this._children.Add( newNode );
					else
						this._children.Insert( 0, newNode );
				}
				else
				{
					int i = IndexOf( refBefore );
					if ( i < Count )
						this._children.Insert( i, newNode );
					else
						this._children.Add( newNode );
				}
			}
		}

		public void InsertAt( xXmlNode newNode, int position )
		{
			if ( (Count == 0) || (position >= Count) )
				this._children.Add( newNode );
			else
			{
				if ( position < 1 )
					this._children.Insert( 0, newNode );
				else
					if ( position.InRange( Count, 0 ) )
						this._children.Insert( position, newNode );
			}
		}

		public XmlNode ToXmlNode()
		{
			string xml = $"<{this.Name}";
			foreach ( var attr in this._attributes )
				xml += $" {attr.Key}='{attr.Value.XmlEncode(true)}'";

			xml += $">{(string.IsNullOrWhiteSpace(this._value)?"":this._value.XmlEncode())}</{this.Name}>";

			// we can't use the string extension for this because it uses xXmlNode!
			XmlDocument doc = new();
			doc.LoadXml( XML.HEADER + xml );
			XmlNode result = doc.GetFirstNamedElement( this.Name );

			if ( this.HasChildNodes )
				foreach ( XmlNode child in this._children )
					if (!child.Name.Equals("#text"))
						result = result.ImportNode( child );

			return result;
		}

		public void AppendChild( xXmlNode node )
		{
			node._parent = this;
			this._children.Add( node );
		}

		public void ImportNodes( params xXmlNode[] nodes )
		{
			if ( (nodes is not null) && (nodes.Length > 0) )
				foreach ( xXmlNode node in nodes )
					this.AppendChild( node );
		}

		public bool Equals( xXmlNode node )
		{
			bool check = false;
			if ( node is not null )
			{
				check = node.Name.Equals( this.Name, StringComparison.OrdinalIgnoreCase );
				check &= node.Attributes.Count == this.Attributes.Count;
				if ( node.Attributes.Count > 0 )
				{
					int i = -1;
					while ( check && (++i < this._attributes.Count)) 
						check = node[ this._attributes[ i ].Key ].Equals( this._attributes[ i ].Value, StringComparison.OrdinalIgnoreCase );
				}

				check &= Count == node.Count;
				check &= InnerXml.Equals( node.InnerXml, StringComparison.OrdinalIgnoreCase );
			}
			return check;
		}

		public override string ToString() => $"Element; \x22{this.OuterXml}\x22;";

		public xXmlNode[] GetNamedElements( string elementName, string attrName = "", string attrValue = "", bool caseSensitive = false, bool recursive = false )
		{
			xXmlNode[] nodes = this.ChildNodes;
			List<xXmlNode> results = new();
			StringComparison sc = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

			if ( nodes.Length > 0 )
				foreach ( xXmlNode node in nodes )
				{
					if ( node.Name.Equals( elementName, sc ) )
					{
						if ( string.IsNullOrEmpty( attrName.Trim() ) || node.HasAttribute( attrName, caseSensitive ) )
						{
							if ( string.IsNullOrEmpty( attrValue.Trim() ) || attrValue.Equals( node.GetAttributeValue( attrName ), sc ) )
								results.Add( node );
						}
					}
					if ( recursive ) results.AddRange( node.GetNamedElements( elementName, attrName, attrValue, caseSensitive, recursive ) );
				}

			return results.ToArray();
		}

		public xXmlNode GetFirstNamedElement( string elementName, string attrName = "", string attrValue = "", bool caseSensitive = false, bool recursive = false )
		{
			xXmlNode[] find = GetNamedElements( elementName, attrName, attrValue, caseSensitive, recursive );
			return (find.Length > 0) ? find[ 0 ] : null;
		}

		public xXmlNode GetElementById( string id, bool caseSensitive = false )
		{
			int i = IndexOfElementByAttr( "id", id, caseSensitive );
			return i < 0 ? null : this._children[ i ];
		}

		public static xXmlNode Parse( string xml )
		{
			XmlDocument doc = new();
			doc.LoadXml( XML.HEADER + xml );
			return (xXmlNode)doc.LastChild;
		}

		public static bool IsValidTag( string tag ) =>
			!string.IsNullOrWhiteSpace( tag ) && Regex.IsMatch( tag, @"^[a-z][\w]*[a-z0-9]$", RegexOptions.IgnoreCase );

		public static XmlNode[] Convert( IEnumerable<xXmlNode> nodes )
		{
			List<XmlNode> result = new();
			foreach ( xXmlNode node in nodes ) result.Add( (XmlNode)node );
			return result.ToArray();
		}

		public static xXmlNode[] Convert( IEnumerable<XmlNode> nodes )
		{
			List<xXmlNode> result = new();
			foreach ( XmlNode node in nodes ) result.Add( (xXmlNode)node );
			return result.ToArray();
		}

		#endregion
	}

	public sealed class xXmlDocument
	{
		#region Properties
		#endregion

		#region Constructors
		public xXmlDocument( XmlDocument doc )
		{

		}
		#endregion

		#region Operators
		#endregion
	}
}
