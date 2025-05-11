using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using NetXpertExtensions;
using NetXpertExtensions.Xml;
using BoundaryRule = NetXpertExtensions.Classes.Range.BoundaryRule;

namespace NetXpertCodeLibrary
{
    public static class TreeExtensions
	{
        /// <summary>Creates an Extension method for all classes to support XML Serialization.</summary>
        public static string XmlSerialize<T>( this T sourceObject )
        {
            if ( sourceObject is null ) return "";

            XmlSerializer xmlSerialized = new( typeof( T ) );
            StringWriter sw = new();
            using ( XmlWriter w = XmlWriter.Create( sw ) )
                xmlSerialized.Serialize( w, sourceObject );

            return $"{sw}";
        }
    }

    /// <summary>Provides a mechanism for managing dot-separated value strings.</summary>
    /// <remarks>Expects a string in the form of "field1.field2.field3" and manages its contents in easily accessible ways.</remarks>
    public sealed class TreeGroupChain
    {
        #region Properties
        private string _data = "";
        #endregion

        #region Constructors
        public TreeGroupChain( string source = "" )
        {
            if ( !ValidatePath( source ) )
                throw new FormatException( $"The supplied group chain (\x22{source}\x22) isn't recognizable." );

            this._data = source;
        }

        public TreeGroupChain( string[] source )
		{
            if ( !(source is null) && (source.Length > 0) )
			{
                string work = string.Join( ".", source );

                if ( ValidatePath( work ) )
                {
                    this._data = work;
                    return;
                }
            }

            throw new FormatException( $"The supplied group chain (\x22{source}\x22) isn't recognizable." );
        }
        #endregion

        #region Operators
        public static implicit operator string( TreeGroupChain source ) => source is null ? "" : source.Path;
        public static implicit operator TreeGroupChain( string source ) => new( source );
        public static implicit operator string[]( TreeGroupChain source ) => source is null ? new string[] { } : source.Parts;
        public static implicit operator TreeGroupChain( string[] source ) => new( source );
        #endregion

        #region Accessors
        public string this[int index]
		{
            get
			{
                if ( index.InRange( Count, 0, BoundaryRule.Loop ) ) return Parts[ index ];
                throw new IndexOutOfRangeException( $"The specified index ({index}) lies outside the bounds of this TreeGroupChain (0-{Count})." );
			}
		}

        /// <summary>Reports the total number of characters in the source/base string.</summary>
        public int Length => this._data.Length;

        /// <summary>Reports the total number of segments in the source path.</summary>
        public int Count => this.Parts.Length;

        /// <summary>Presents the loaded chain as an array of node names.</summary>
        private string[] Parts => this._data.Split( new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries );

        /// <summary>Returns the remainder of the source path when the first element has been removed.</summary>
        public string Tail =>
            Count < 2 ? "" : this._data.Split( new char[] { '.' }, 2, StringSplitOptions.RemoveEmptyEntries )[ 1 ];

        /// <summary>Gets/Sets the base path to be managed by this object.</summary>
        public string Path
        {
            get => this._data;
            set => this._data = string.IsNullOrWhiteSpace( value ) ? "" : (ValidatePath( value ) ? value : "");
        }

        /// <summary>If the base path contains a root element, it's name.</summary>
        /// <remarks>Root elements are distinguished by a colon separating them from the remainder of the path.</remarks>
        public string Root
        {
            get
            {
                if ( Count > 0 )
                {
                    Regex pattern = new( @"^((?<root>[a-zA-Z][\w]*[a-zA-Z0-9])[:])?(?<path>((?:[a-zA-Z][\w]*[a-zA-Z0-9])+(\.|$))+)" );
                    if ( pattern.IsMatch( Parts[ 0 ] ) )
                    {
                        Match match = pattern.Match( Parts[ 0 ] );
                        if ( match.Groups[ "root" ].Success ) return match.Groups[ "root" ].Value;
                    }
                }
                return "";
            }
        }

        /// <summary>Returns the first path element from the base path string.</summary>
        public string First
        {
            get
            {
                if ( Count > 0 )
                    return HasRoot ? Regex.Replace( Parts[ 0 ], $"^{Root}[:](.+)", "$1" ) : Parts[ 0 ];

                return "";
            }
        }

        /// <summary>Returns the last path element in the base path string.</summary>
        public string Last => Count switch { 1 => First, 2 => Parts[ Count - 1 ], _ => "" };

        /// <summary>TRUE if the base path string contains a Root designator.</summary>
        public bool HasRoot => Root.Length > 0;
        #endregion

        #region Methods
        public override string ToString() => this.Path;

        /// <summary>Tests a supplied string to see if it's a valid path chain.</summary>
        /// <returns>TRUE if the supplied string is in a valid FORMAT for use as a path chain.</returns>
        /// <remarks>NOTE: This does NOT verify that the supplied path EXISTS, only whether or not it is formatted correcty!</remarks>
        public static bool ValidatePath( string test ) =>
            !string.IsNullOrWhiteSpace( test ) && Regex.IsMatch( test, @"^([a-zA-Z][\w]*[a-zA-Z0-9][:])?((?:[a-zA-Z][\w]*[a-zA-Z0-9])+(\.|$))+" );

        public bool Equals( TreeGroupChain compare ) => !(compare is null) && this._data.Equals( compare._data, StringComparison.OrdinalIgnoreCase );
        #endregion
    }

    /// <summary>Provides basic class functionality and Static methods.</summary>
    /// <remarks>Provides a non-instantiable common foundation for Type-agnostic functions across descendant DataTree&lt;T&gt; objects.</remarks>
    public abstract class TreeFoundation
    {
        #region Properties
        private readonly string _name;
        private TreeFoundation _parent = null;
        private string _xmlGroupTag = "group";
        #endregion

        #region Constructors
        protected TreeFoundation( string name, TreeFoundation parent = null )
        {
            if ( !ValidateName( name ) ) name = RandomName();
            this._name = name;
            this.Parent = parent;
        }

        protected TreeFoundation( XmlNode source, TreeFoundation parent = null )
        {
            if ( !(source is null) )
            {
                this._xmlGroupTag = source.Name;
                this._name = ValidateName( source.GetAttributeValue( "name" ) ) ? source.GetAttributeValue( "name" ) : RandomName();
                this.Parent = parent;
            }
            else
                throw new ArgumentNullException( $"You must provide a non-null XmlNode object." );
        }
        #endregion

        #region Accessors
        public string Name => this._name;

        /// <summary>Recursive mechanism to build a dot-separated path string to point to this object within the hierarchy.</summary>
        public string Path => this._parent is null ? Name : $"{Parent.Path}.{Name}";

        /// <summary>Manages the _parent referent value: outside callers aren't allowed to change it once it's been set</summary>
        protected TreeFoundation Parent
        {
            get => this._parent;
            set
            {
                if ( !(value is null) && (this._parent is null) && (value != this) )
                    this._parent = value;
            }
        }

        public TreeFoundation Root => this._parent is null ? this : this._parent.Root;

        protected string XmlGroupTag
        {
            get => this._xmlGroupTag;
            set
            {
                if ( !string.IsNullOrWhiteSpace( value ) && Regex.IsMatch( value, @"^[a-zA-Z]{2,}$" ) )
                    this._xmlGroupTag = value;
            }
        }
        #endregion

        #region Methods
        /// <summary>Tests a supplied string to see if it's a valid object 'name'.</summary>
        /// <returns>TRUE if the supplied string is valid for use as an object name.</returns>
        public static bool ValidateName( string name ) =>
            !string.IsNullOrWhiteSpace( name ) && Regex.IsMatch( name, @"^[a-zA-Z][\w]*[a-zA-Z0-9]$" );

        /// <summary>Creates a random unique name for an object.</summary>
        /// <param name="prefix">A prefix to place into the result/</param>
        /// <returns>A randomly generated UID name value with the specified prefix attached.</returns>
        public static string RandomName( string prefix = "" )
        {
            prefix = string.IsNullOrWhiteSpace( prefix ) ? "A" : Regex.Replace( prefix, @"[^\w]", "" );
            return (Regex.IsMatch( prefix, @"^[a-zA-Z]" ) ? $"A{prefix}" : prefix) + new Guid().ToString();
        }

        public static XmlNode ExtractNode( string tagName, XmlDocument doc )
		{
            if ( string.IsNullOrWhiteSpace( tagName ) || !Regex.IsMatch( tagName, @"^[a-zA-Z]{2,16}$" ) )
                throw new ArgumentException( $"The specified tag name cannot be invalid, null, empty or whitespace! (\x22{tagName}\x22)." );

            if ( doc is null )
                throw new ArgumentNullException( "You must provide a populated XmlDocument for this function." );

            return doc.GetFirstNamedElement( tagName );
		}
        #endregion
    }

    /// <summary>A self-referential class for managing a hierarchical tree of generic data elements.</summary>
    /// <typeparam name="T">The type of the data that is to be managed by the hierarchy.</typeparam>
    /// <remarks>This abstract class is the basis for constructing a hierarchical data collection, it is intended
    /// to be inherited by a specific data-management class that's implemented by the app being used.</remarks>
	public abstract class DataTree<T> : TreeFoundation, IEnumerator<DataTree<T>>
    {
        #region Properties
        private List<DataTree<T>> _items = new();
        private int _pointer = 0;
        #endregion

        #region Constructor
        protected DataTree( string name, T value = default, DataTree<T> parent = null ) : base( name, parent ) =>
            this.Data = value;

        protected DataTree( XmlNode source, XmlNodeToData xmlParser, DataTree<T> parent = null ) : base( source, parent )
        {
            XmlNode value = source.GetFirstNamedElement( "value", "created" );
            this.Data =  value is null ? default : xmlParser( value );

            XmlNode[] children = source.GetNamedElements( this.XmlGroupTag, "name" );
            foreach ( XmlNode child in children )
            {
                //var instance = CreateInstance( child, xmlParser, parent );
                var instance = CreateInstance( child, xmlParser, this );
                this.AddItem( instance );
            }
        }
        #endregion

        #region Accessors
        /// <summary>How many items are managed by this class.</summary>
        public int Count => this._items.Count;

        /// <summary>Holds the data payload for this object.</summary>
        public T Data { get; set; } = default;

        /// <summary>Facilitates access to a managed child by its index.</summary>
        protected DataTree<T> this[ int index ] => this._items[ index ];

        /// <summary>Facilitates direct-referencing the hierarchy via dot-separated names.</summary>
        public DataTree<T> this[ TreeGroupChain path ]
        {
            get
            {
                int i = IndexOf( path.Last );
                if ( i < 0 ) throw new ArgumentOutOfRangeException( $"The requested node does not exist! (\"{path.Last}\")" );
                return this[ i ];
            }
        }

        /// <summary>Provides a reference to the object which stores / manages this object.</summary>
        /// <remarks>If this reference is set to NULL, then this object is the root of it's tree.</remarks>
        new public DataTree<T> Parent
        {
            get => base.Parent as DataTree<T>;
            protected set => base.Parent = value;
        }

        /// <summary>Recursively provides direct access to the root node.</summary>
        new public DataTree<T> Root => Parent is null ? this : Parent.Root;

        /// <summary>Collates a collection of the value of this object, plus the values of it's constituent groups.</summary>
        /// <remarks>NOTE: This collection is non-recursive! Sub-groups' values are not included!</remarks>
        public T[] Values
        {
            get
            {
                List<T> items = new();
                foreach ( var item in this._items )
                    items.Add( item.Data );

                return items.ToArray();
            }
        }

        /// <summary>TRUE if this object has child objects.</summary>
        public bool HasChildren => Count > 0;

        /// <summary>TRUE if this object's 'Data' accessor is not equal to 'default( T )'.</summary>
        public bool HasValue => !this.Data.Equals( default( T ) );

        /// <summary>Recursively counts the number of nodes managed under this node.</summary>
        public int NodeCount
		{
            get
			{
                int nodeCount = 1;
                foreach ( var item in this ) nodeCount = item.NodeCount;
                return nodeCount;
			}
		}

        DataTree<T> IEnumerator<DataTree<T>>.Current => this._items[ _pointer ];

        object IEnumerator.Current => this._items[ this._pointer ];
        #endregion

        #region Delgates
        /// <summary>Delegate declaration for use in the ToXmlNode method.</summary>
        /// <returns>A valid XmlNode object containing the data needed to reconstruct the supplied 'T' object.</returns>
        public delegate XmlNode DataToXmlNode( T source );

        /// <summary>Delegate declaration for use in parsing an XmlNode object into a valid 'T' object.</summary>
        /// <param name="node">An XmlNode object that is to be parsed into a populated 'T' object.</param>
        /// <returns>A new 'T' object populated correctly by parsing the supplied 'node' object.</returns>
        public delegate T XmlNodeToData( XmlNode node );
        #endregion

        #region Abstractions
        /// <summary>Forces descendant classes to implement a function to self-instantiate.</summary>
        /// <remarks>This is required in order for the `Clone` function to work.</remarks>
        protected abstract DataTree<T> CreateInstance( string name, T value = default, DataTree<T> parent = null );

        /// <summary>Forces descendant classes to implement a function to self-instantiate.</summary>
        /// <param name="source">An XmlNode object to parse.</param>
        /// <param name="xmlParser">A delegate function to enable parsing the 'T' datatype from an XmlNode.</param>
        protected abstract DataTree<T> CreateInstance( XmlNode source, XmlNodeToData xmlParser, DataTree<T> parent = null );
        #endregion

        #region Methods
        /// <summary>Get the index associated with the supplied name.</summary>
        protected int IndexOf( string name )
        {
            int i = -1;
            if ( ValidateName( name ) )
                while ( (++i < this.Count) && !this._items[ i ].Name.Equals( name, StringComparison.OrdinalIgnoreCase ) ) ;

            return (i < this.Count) ? i : -1;
        }

        /// <summary>Provide a means for checking if a name exists in this object.</summary>
        public bool HasItem( string name ) => IndexOf( name ) >= 0;

        /// <summary>Manage item addition to prevent name duplication.</summary>
        protected void AddItem( DataTree<T> item )
        {
            if ( !(item is null) )
            {
                item.Parent = this;
                int i = IndexOf( item.Name );
                if ( i < 0 )
                    this._items.Add( item );
                else
                    this._items[ i ] = item;
            }
        }

        /// <summary>Facilitates adding a collection of objects in a single call.</summary>
        protected void AddRangeOfItems( IEnumerable<DataTree<T>> items )
        {
            if ( !(items is null) )
                foreach ( DataTree<T> item in items )
                    this.AddItem( item );
        }

        /// <summary>Facilitate item removal by name.</summary>
        /// <returns>If the requested object was found and removed, that object is returned from this call.</returns>
        public DataTree<T> Remove( string name )
        {
            DataTree<T> result = null;

            int i = IndexOf( name );
            if ( i >= 0 )
            {
                result = this._items[ i ];
                this._items.RemoveAt( i );
            }
            return result;
        }

        /// <summary>Provide a mechanism to prune and graft this object from one parent to another.</summary>
        /// <returns>A reference to this object after being re-homed.</returns>
        public DataTree<T> Rehome( DataTree<T> newParent )
        {
            if ( !(this.Parent is null) )
                this.Parent.Remove( this.Name );

            this.Parent = newParent;
            if ( !(newParent is null) )
                newParent._items.Add( this );

            return this; // Provides a means for the calling method to keep a reference to this one.
        }

        /// <summary>Provide a mechanism to prune and graft this object from one parent to another.</summary>
        /// <param name="groupChain">A dot-separated path that identifies the new parent object.</param>
        /// <returns>A reference to this object after being re-homed.</returns>
        public DataTree<T> Rehome( TreeGroupChain groupChain )
        {
            if ( !(groupChain is null) )
            {
                DataTree<T> target = this[ groupChain ];
                if ( !(target is null) )
                    return Rehome( target );
            }
            return this;
        }

        /// <summary>Given a dot-separated chain of object names, attempts to navigate to and return a requested object.</summary>
        public DataTree<T> Get( TreeGroupChain path )
        {
            if ( !(path is null) && (path.Length > 0) )
            {
                if ( path.Count > 0 )
                {
                    if ( path.HasRoot && !(this.Parent is null) )
                        return this.Root.Get( path.Path.Substring( path.Path.IndexOf( ':' ) + 1 ) );

                    int i = IndexOf( path.First );
                    if ( i >= 0 )
                        return (path.Count == 1) ? this._items[ i ] : ( (path.Count == 2) ? this._items[ i ][ path.Last] : this._items[i].Get( path.Tail ) );
                }
                throw new KeyNotFoundException( $"The supplied key (\x22{path}\x22) was not found." );

            }
            throw new ArgumentException( $"The supplied key (\x22{path}\x22) isn't valid." );
        }

        /// <summary>Attempts to Get a node, but captures KeyNotFound and Argument Exceptions instead of throwing them.</summary>
        /// <returns>If the requested node exists, a reference to it, otherwise NULL.</returns>
        public DataTree<T> TryGet( TreeGroupChain path )
		{
			try { return Get( path ); }
            catch( KeyNotFoundException ) { }
            catch( ArgumentException ) { }
            catch( Exception e ) { throw e; }
            return null;
        }

        /// <summary>Tests a supplied path to see if the specified node exists.</summary>
        /// <returns>TRUE if the specified path points to an existing node.</returns>
        public bool HasNode( TreeGroupChain path )
        {
            try { return !( this.Get( path ) is null ); } 
            catch( KeyNotFoundException ) { return false; }
            catch ( ArgumentException ) { return false; }
        }

        /// <summary>Supplied with a node, destructively imports its contents into this node.</summary>
        /// <param name="node">The node who's values are to be imported.</param>
        /// <param name="destructive">Indicates whether the import mode is destructive or not.</param>
        /// <remarks>The supplied node is first Clone'd, then the clone's contents are imported.<br/>
        /// This function is recursive and will import all descendent values of the supplied node as well.<br/>
        /// A 'Destructive' import will replace the values of keys with names that match those of the imported node with the new node's value(s).
        /// </remarks>
        protected void ImportNode( DataTree<T> node, bool destructive = true )
		{
            foreach ( DataTree<T> child in node.Clone( "", this ) )
			{
                if ( this.HasItem( child.Name ) )
                {
                    if (destructive)
                        this[ child.Name ].Set( child.Name, child.Data );

                    if ( child.HasChildren )
                        this[ child.Name ].ImportNode( child, destructive );
                }
                else
                    this.AddItem( child );
			}
		}

        /// <summary>Sets a registry value.</summary>
        /// <param name="name">A String specifying the name of the key to create.</param>
        /// <param name="value">The value to assign to the key (Type &lt;T&gt;).</param>
        /// <returns>TRUE if the operation was successful.</returns>
        public bool Set( string name, T value ) =>
            Set( null, CreateInstance( name, value ) );

        /// <summary>Sets a registry value.</summary>
        /// <param name="path">The dot-notation path to the group/item to set the value on.</param>
        /// <param name="value">The value to set (Type is &lt;T&gt;).</param>
        /// <returns>TRUE if the operation was successful.</returns>
        public bool Set( TreeGroupChain path, T value ) =>
            Set( path, CreateInstance( path.Last, value ) );

        /// <summary>Sets a registry value.</summary>
        /// <param name="path">The dot-notation path to the group/item to set the value on.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>TRUE if the operation was successful.</returns>
        /// <remarks>If the supplied value is null, and the path points to an existing entry, the entry will be deleted. If the entpoint is valid, but there's no matching value, it will be created.</remarks>
        public bool Set( TreeGroupChain path, DataTree<T> value )
		{
            int i;
            if ( (path is null) || string.IsNullOrWhiteSpace(path) )
			{
                // the path is null, or empty:
                i = (value is null) ? -1 : IndexOf( value.Name );
                if ( i >= 0 )
                {
                    if ( value is null )
                        this._items.RemoveAt( i );
                    else
                        this._items[ i ] = value;

                    return true;
                }

                // item doesn't exist and value is not null:
                if ( !(value is null) ) { this.AddItem( value ); return true; }
            }
            else if ( path.Count > 0 )
			{
                if ( path.HasRoot && !(this.Parent is null) )
                    return this.Root.Set( path.Path.Substring( path.Path.IndexOf( ':' ) + 1 ), value );

                i = IndexOf( path.First );
                if ( i >= 0 )
                    return (path.Count == 1) ? this._items[ i ].Set( null, value ) : this._items[ i ].Set( path.Tail, value );
			}

            // Operation was unsuccessful:
            return false;
        }

        /// <summary>Creates a value-equivalent copy of this object with a specified parent object.</summary>
        /// <param name="name">A string specifying a new name to assigne to the clone.</param>
        /// <param name="newParent">A new parent object to make the clone's parent. The clone will be automatically added to this object.</param>
        /// <remarks> If the 'name' value is null or empty, the original source object's name will be used.<br />
        /// If the `newParent` object matches
        /// </remarks>
        protected DataTree<T> Clone( string name = "", DataTree<T> newParent = null )
        {
            if ( newParent == this ) newParent = null;  // if we're cloning ourself, disown the clone...

            if ( string.IsNullOrWhiteSpace( name ) ) name = this.Name;
            DataTree<T> clone = CreateInstance( name, this.Data, newParent );

            // Clone the groups
            foreach ( DataTree<T> group in this._items )
                clone.AddItem( group.Clone( "", clone ) );

            if ( !(newParent is null))
                newParent.AddItem( clone );

            return clone;
        }

        /// <summary>Given a valid node-path, creates any nodes along it that cannot be found.</summary>
        /// <param name="path">A dot-notation path of nodes to create.</param>
        /// <returns>TRUE if the operation completes successfully, otherwise FALSE.</returns>
        /// <remarks>This idea is non-viable as this abstract routine cannot instantiate new DataTree&lt;T&gt; objects on its own (descendent classes must do it themselves).</remarks>
        public bool CreateNodePath( TreeGroupChain path )
        {
            if ( !(path is null) )
            {
                if ( !HasItem( path.First ) )
                    this.AddItem( CreateInstance( path.First, default, this ) );

                return path.Count <= 1 || this[ path.First ].CreateNodePath( path.Tail );
            }

            return false;
        }

        /// <summary>Recursively merges a supplied group into this one.</summary>
        public void Merge( DataTree<T> newGroup )
        {
            if ( !(newGroup is null) )
            {
                this.Data = newGroup.Data;
                foreach ( DataTree<T> group in newGroup._items )
                {
                    int i = IndexOf( group.Name );
                    if ( i < 0 )
                        this._items.Add( group.Clone( "", this ) );
                    else
                        this._items[ i ].Merge( group );
                }
            }
        }

        /// <summary>Recursively searches the tree for the first instance of the specified name.</summary>
        /// <param name="name">The name of the node to look for. CANNOT BE A groupChain!</param>
        /// <returns>If a node with a matching name is found, that node, otherwise NULL.</returns>
        public DataTree<T> Find( string name )
		{
            if ( ValidateName( name ) )
			{
                int i = IndexOf( name );
                if ( i >= 0 ) return this._items[ i ];
                if ( Count > 0 )
                {
                    i = -1; 
                    while (++i < Count)
					{
                        DataTree<T> result = this._items[ i ].Find( name );
                        if ( !(result is null) ) return result;
                    }
                }
			}

            return null;
		}

        /// <summary>Empties the _items collections.</summary>
        public void Clear() => this._items.Clear();

        /// <summary>Creates and XmlNode object containing this object's data.</summary>
        /// <param name="xmlConverter">A method declaration that creates a custom XmlNode for the datatype being managed by this object.</param>
        /// <returns>An XmlNode object containing the necessary prerequisite data needed to re-construct this object.</returns>
        public XmlNode ToXmlNode( DataToXmlNode xmlConverter )
        {
            xXmlNode result = $"<{XmlGroupTag} name=\x22{Name}\x22>{(this.Data is null ? "" : xmlConverter( this.Data ).OuterXml)}</{XmlGroupTag}>";
            foreach ( DataTree<T> item in this )
            {
                xXmlNode node = item.ToXmlNode( xmlConverter );
                result.AppendChild( node );
            }

            return result;
        }

        public override string ToString() => $"{this.Path}: {Count} items. {NodeCount} nodes.";

        /// <summary>Creates a Dictionary&lt;string,T&gt; object from the contents of this object.</summary>
        /// <remarks>
        /// NOTE: Only Child nodes of THIS argument, and their values, are contained in the created Dictionary; no other descendents are recognized.<br/><br/>
        /// The data value of this object are stored in the dictionary under the "[root"] entry.<br/>
        /// The full path value for this object is stored in the dictionary under the "[path]" entry.
        /// </remarks>
        public Dictionary<string,string> ToDictionary()
		{
            Dictionary<string, string> result = new();
            result.Add( "[root]", this.Data.ToString() );
            result.Add( "[path]", this.Path.ToString() );
            foreach ( DataTree<T> item in this )
                result.Add( item.Name, item.Data.ToString() );

            return result;
		}

        public DataTree<T>[] ToArray() => this._items.ToArray();

        /// <summary>Facilitates Diagraming the Hierarchical structure of this object and its children.</summary>
        /// <param name="indent">Used to track how many layers deep this node is.</param>
        public string Diagram( byte indent = 0 )
		{
            /*
             DataType: "typeName"
             ├─┬─► Name: "name"; Value: "value"
             │ ├─┬─► Name: "name"; Value: "value"
             */
            string result = "";
            if ( indent == 0 ) result = $"Type: \x22{typeof(T).Name}\x22\r\n";

            if ( Count > 0 )
			{
                foreach ( DataTree<T> item in this )
                {
                    if ( indent > 0 )
                        for ( int i = 0; i < indent; i++ ) result += "│ ";

                    result += $"├─{(item.HasChildren ? "┬" : "─")}─► \x22{item.Name}\x22: [{item.Data}]\r\n";

                    if ( item.HasChildren )
                        result += item.Diagram( ++indent );
                }
			}
            else
			{

			}

            return result;
        }

        //IEnumerator Support:
        public IEnumerator<DataTree<T>> GetEnumerator() => this._items.GetEnumerator();

        public bool MoveNext() => (++this._pointer) < this.Count;

        public void Reset() => this._pointer = 0;
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
    }

    /// <summary>A Specialized variant of the DataTree&lt;T&gt; class specifically meant to serve as the Root node.</summary>
    /// <typeparam name="T">The type of the object data that will be managed by the trees in the forest.</typeparam>
    public abstract class DataTreeRoot<T> : DataTree<T>
    {
        #region Properties
        private readonly string _xmlRootTag;

        //private readonly string _xmlGroupTag;
        #endregion

        #region Constructor
        /// <param name="name">A name for this (root) node.</param>
        /// <param name="xmlRootTag">The XML tag that identifies this tree.</param>
        /// <param name="xmlGroupTag">The XML tag that identifies individual item records.</param>
        /// <remarks>The 'xmlRootTag' and 'xmlGroupTag' values must meet standard XML element naming rules, and cannot be identical.</remarks>
        public DataTreeRoot( string name = "", string xmlRootTag = "", string xmlGroupTag = "" ) : base( name, default, null )
        {
            if ( string.IsNullOrWhiteSpace( xmlRootTag ) ) xmlRootTag = "hive";
            if ( string.IsNullOrWhiteSpace( xmlGroupTag ) ) xmlGroupTag = "group";

            if ( !Regex.IsMatch( xmlRootTag, @"^[a-zA-z][\w.-]*[a-zA-Z0-9]{1,60}$" ) )
                throw new InvalidDataException( $"The 'xmlRootTag' value is invalid. (\"{xmlRootTag}\x22)" );

            if ( !Regex.IsMatch( xmlGroupTag, @"^[a-zA-z][\w.-]*[a-zA-Z0-9]{1,60}$" ) )
                throw new InvalidDataException( $"The 'xmlGroupTag' value is invalid. (\"{xmlGroupTag}\x22)" );

            if ( xmlRootTag.Equals( xmlGroupTag, StringComparison.CurrentCulture ) )
                throw new ArgumentException( $"The 'xmlRootTag' and 'xmlGroupTag' parameters cannot match! (\x22{xmlRootTag}\x22/\x22{xmlGroupTag}\x22)" );

            this._xmlRootTag = xmlRootTag;
            this.XmlGroupTag = xmlGroupTag;
        }

        /// <param name="xmlRootTag">The XML tag that identifies this tree.</param>
        /// <param name="xmlGroupTag">The XML tag that identifies individual item records.</param>
        /// <remarks>The 'xmlRootTag' and 'xmlGroupTag' values must meet standard XML element naming rules, and cannot be identical.</remarks>
        public DataTreeRoot( XmlNode source, XmlNodeToData xmlParser, string xmlRootTag = "", string xmlGroupTag = "" ) : base( source, xmlParser, null )
        {
            if ( string.IsNullOrWhiteSpace( xmlRootTag ) || Regex.IsMatch( xmlRootTag, @"[a-zA-Z]{2,16}" ) )
                xmlRootTag = source.Name;

            this._xmlRootTag = xmlRootTag;

            if ( Regex.IsMatch( source.GetAttributeValue( "groupTag" ), @"^[a-zA-z][\w.-]*[a-zA-Z0-9]{1,60}$" ) )
                xmlGroupTag = source.GetAttributeValue( "groupTag" );
            else if ( string.IsNullOrWhiteSpace( xmlGroupTag ) || !Regex.IsMatch( xmlGroupTag, @"^[a-zA-z][\w.-]*[a-zA-Z0-9]{1,60}$" ) )
                throw new ArgumentNullException( $"You must provide a valid name to use for the XML Group tag (\x22{xmlGroupTag}\x22)." );

            this.XmlGroupTag = xmlGroupTag;

            XmlNode[] children = source.GetNamedElements( this.XmlGroupTag, "name" );
            foreach ( XmlNode child in children )
                if ( child.Name.Equals( this.XmlGroupTag, StringComparison.OrdinalIgnoreCase ) )
                    this.AddItem( CreateInstance( child, xmlParser, this ) );
        }
        #endregion

        #region Accessors
        #region Obscure these accessors:
        new protected DataTree<T> Parent => null;

        new protected DataTree<T> Root => this;

        new protected string Path => $"{this.Name}:";

        new protected T Data => base.Data;
		#endregion

		/// <summary>Facilitates direct-referencing the hierarchy via dot-separated names.</summary>
		/// <param name="groupChain"></param>
		/// <returns></returns>
		new public DataTree<T> this[ TreeGroupChain groupChain ]
        {
            get
            {
                int i = IndexOf( groupChain.Last );
                if ( i < 0 ) throw new ArgumentOutOfRangeException( $"The requested node does not exist! (\"{groupChain.Last}\")" );
                return this[ i ];
            }
        }
        #endregion

        #region Methods
        /// <summary>Provides a means to generate an XmlDocument from this Forest.</summary>
        /// <param name="xmlConverter">A 'DataToXmlNode( XmlNode )' delegate function that parses 'T' classes from Xml.</param>
        /// <returns>A complete XmlDocument object populated with the contents of this forest.</returns>
        public XmlDocument ToXml( DataToXmlNode xmlConverter )
        {
            string raw = $"{NetXpertExtensions.Xml.XML.HEADER}<{this._xmlRootTag} name='{Name}' groupTag='{XmlGroupTag}'>";
            foreach ( DataTree<T> group in this )
                raw = group.ToXmlNode( xmlConverter ).OuterXml;

            raw += $"</{this._xmlRootTag}>";

            XmlDocument doc = new();
            doc.LoadXml( raw );
            return doc;
        }

        /// <summary>Given a dot-separated chain of object names, attempts to navigate to and return a requested object.</summary>
        /// <remarks>This is a slightly modified version of the ancestor 'Get' method that ignores 'root' checking / handling.</remarks>
        /// <seealso cref="DataTree{T}.Get(TreeGroupChain)"/>
        /*
        new protected DataTree<T> Get( TreeGroupChain path )
        {
            if ( !(path is null) && (path.Length > 0) )
            {
                if ( path.Count > 0 )
                {
                    int i = IndexOf( path.First );
                    if ( i >= 0 )
                        return path.Count == 1 ? this[ i ] : this[ i ].Get( path.Tail );
                }
                throw new KeyNotFoundException( $"The supplied key (\x22{path}\x22) was not found." );
            }
            throw new ArgumentException( $"The supplied key (\x22{path}\x22) isn't valid." );
        }
        */

        /// <summary>Obfuscates the 'Clone' function which is not supported for DataTreeRoot&lt;T&gt; objects.</summary>
        ///<remarks>There is no function body for this method, calling it will generate a NotImplementedException!</remarks>
        new private DataTree<T> Clone( string name = "", DataTree<T> newParent = null ) =>
            throw new NotImplementedException();

        ///<summary>
        /// Obfuscates and satisfies the ancestor abstract 'CreateInstance( string &lt;T&gt;, DataTree&lt;T&gt; )' 
        /// method that I don't need or want a DataTreeRoot&lt;T&gt; to support or implement.
        ///</summary>
        ///<remarks>There is no function body for this method, calling it will generate a NotImplementedException!</remarks>
        // protected override DataTree<T> CreateInstance( string name, T value = default, DataTree<T> parent = null ) => throw new NotImplementedException();

        ///<summary>
        /// Obfuscates and satisfies the ancestor abstract 'CreateInstance( XmlNode source, XmlNodeToData, DataTree&lt;T&gt; )' 
        /// method that I don't need or want a DataTreeRoot&lt;T&gt; to support or implement.
        ///</summary>
        ///<remarks>There is no function body for this method, calling it will generate a NotImplementedException!</remarks>
        // protected override DataTree<T> CreateInstance( XmlNode source, XmlNodeToData xmlParser, DataTree<T> parent = null ) => throw new NotImplementedException();
        #endregion
    }
}
