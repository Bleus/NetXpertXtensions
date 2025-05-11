using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using NetXpertCodeLibrary.Extensions;

namespace NetXpertCodeLibrary.ContactData
{
	/// <summary>This class provides the common foundation for any class that serves as the Data type for BasicTypeCollection-derived collections.</summary>
	/// <typeparam name="Y">The Flagged Enum type that governs the underlying data.</typeparam>
	public abstract class BasicTypedDataFoundation<Y> where Y : Enum
	{
		#region Properties
		private string _xmlTag = "";
		#endregion

		#region Constructors
		public BasicTypedDataFoundation( Y myType ) =>
			this.Type = myType;

		public BasicTypedDataFoundation( XmlNode node )
		{
			if ( node is null )
				throw new ArgumentNullException( "You must supply an XmlNode value to use this constructor!" );

			this.XmlTag = node.Name;
			ParseXml( node );
		}
		#endregion

		#region Accessors
		public Y Type { get; set; }

		public bool Unknown => IsUnknown( Type );

		public string XmlTag
		{
			get => this._xmlTag;
			set
			{
				if ((this._xmlTag.Length == 0) && !string.IsNullOrWhiteSpace( value ) && Regex.IsMatch(value.Trim(), @"^[a-zA-Z][\w]*[a-zA-Z\d]$" ) )
					this._xmlTag = value.Trim();
			}
		}
		#endregion

		#region Methods
		/// <summary>Parses out the foundational XML data from a supplied XmlNode.</summary>
		/// <remarks>This does NOT do anything with the node's payload!<br/>Descendant classes must parse their own data!</remarks>
		/// <exception cref="ArgumentNullException">If the supplied XmlNode is null.</exception>
		/// <exception cref="MissingFieldException">If the supplied XmlNode doesn't have a 'type=' attribute.</exception>
		protected virtual void ParseXml( XmlNode source )
		{
			if ( source is null )
				throw new ArgumentNullException( "You must supply an XmlNode value to use this constructor!" );

			if ( !source.HasAttribute( "type" ) )
				throw new MissingFieldException( "The supplied XmlNode object does not specify a 'type'." );

			string work = source.GetAttributeValue( "type" );

			if ( Regex.IsMatch( work, @"^[\d]+$" ) )
				this.Type = ParseInt( int.Parse( work ) );
			else
			{
				if ( Regex.IsMatch( work, $"^({string.Join( "|", Enum.GetNames( typeof( Y ) ) )})$", RegexOptions.IgnoreCase ) )
					this.Type = (Y)Enum.Parse( typeof( Y ), work );
			}
		}

		/// <summary>Takes a flagged bitmap enum value and compares it with the defined Type for this object and returns an enum that contains all bits that match.</summary>
		/// <remarks>The returned enum value will represent the intersection of the flagged enum that was passed with the one defined for this object.</remarks>
		public Y IntersectionOf( Y map )
		{
			int myTypeInt = (int)Convert.ChangeType( Type, typeof( int ) ), mapTypeInt = (int)Convert.ChangeType( map, typeof( int ) );
			return (Y)Convert.ChangeType( myTypeInt & mapTypeInt, typeof( Y ) );
		}

		/// <summary>Leverages the Intersection function to report on whether a supplied bitmapped enum contains flags that are set on this object's own Type.</summary>
		/// <returns>TRUE if at least one bit in the supplied enum matches a bit in the 'Type' of this object.</returns>
		public bool Intersects( Y map ) => Parse( IntersectionOf( map ) ) > 0;
		#endregion

		#region Static Methods
		/// <summary>Converts an integer value to the defined enum type.</summary>
		protected static Y ParseInt( int value ) => (Y)Convert.ChangeType( value, typeof( Y ) );

		/// <summary>Convers an enum value to an integer.</summary>
		public static int Parse( Y value ) => (int)Convert.ChangeType( value, typeof( int ) );

		/// <summary>Returns TRUE if the supplied enum evaluates to 0.</summary>
		protected static bool IsUnknown( Y value ) => Parse( value ) == 0;
		#endregion

		#region Abstract/Overrideable Methods
		public abstract XmlNode ToXmlNode();

		public abstract bool IsEmpty();

		protected virtual XmlNode ToXmlNode( string payloadData )
		{
			payloadData = string.IsNullOrWhiteSpace( payloadData ) ? "" : payloadData.XmlEncode();
			return $"<{this.XmlTag} type='{Convert.ChangeType( Type, typeof( int ) )}'>{payloadData}</{this.XmlTag}>".ToXmlNode();
		}

		protected virtual XmlNode ToXmlNode( XmlNode xml ) => ToXmlNode( (xml is null) ? "" : xml.OuterXml );

		public override bool Equals( object obj ) => base.Equals( obj );

		public override int GetHashCode() => base.GetHashCode();
		#endregion
	}

	/// <summary>Provides the basis for creating Collections with classes that have an enumarable 'Type'.</summary>
	/// <typeparam name="T">The class type of the data that is being managed by this collection.</typeparam>
	/// <typeparam name="Y">A flagged enum that is used to identify separate records.</typeparam>
	/// <remarks>The managed data type &lt;T&gt; must inherit from "BasicTypedDataFoundation&lt;Y&gt;()"</remarks>
	public abstract class BasicTypedCollection<T, Y> : IEnumerator<T> where Y : Enum where T : BasicTypedDataFoundation<Y>
	{
		#region Properties
		protected List<T> _data = new List<T>();
		private int _position = 0;
		private readonly int _limit = int.MaxValue;
		private string _xmlTag = "";
		#endregion

		#region Constructors
		public BasicTypedCollection( bool sorted = false, int limit = -1 )
		{
			this.Sorted = sorted;
			this._limit = (limit < 1) ? int.MaxValue : limit;
		}

			public BasicTypedCollection( T data, bool sorted = false, int limit = -1 )
		{
			this.Sorted = sorted;
			this._limit = (limit < 1) ? int.MaxValue : limit;
			this.Add( data, sorted );
		}

		public BasicTypedCollection( IEnumerable<T> data, bool sorted = false )
		{
			this.Sorted = sorted;
			this.AddRange( data, sorted );
		}

		public BasicTypedCollection( XmlNode node )
		{
			if ( !(node is null) && node.HasChildNodes )
			{
				this.Sorted = node.GetAttributeValue( "sorted" ).Equals( "true", StringComparison.OrdinalIgnoreCase );
				this.XmlTag = node.Name;

				if ( node.HasAttribute( "limit" ) && Regex.IsMatch( node.GetAttributeValue( "limit" ), @"^[\d]+$" ) )
					this._limit = int.Parse(node.GetAttributeValue( "limit" ));

				foreach ( XmlNode child in node.ChildNodes )
					this.Add( Instantiate( child ) );
			}
		}
		#endregion

		#region Accessors
		public T this[ int index ] => (index >= 0) && (index < Count) ? this._data[ index ] : Instantiate();

		public T[] this[ Y type ] => Filter( type );

		public int Count => _data.Count;

		public bool Sorted { get; set; } = false;

		public int Limit => this._limit;

		public string XmlTag
		{
			get => this._xmlTag;
			set
			{
				if ( (this._xmlTag.Length == 0) && !string.IsNullOrWhiteSpace( value ) && Regex.IsMatch( value.Trim(), @"^[a-zA-Z][\w]*[a-zA-Z\d]$" ) )
					this._xmlTag = value.Trim();
			}
		}

		T IEnumerator<T>.Current => this._data[ this._position ];

		object IEnumerator.Current => this._data[ this._position ];
		#endregion

		#region Abstract Methods
		protected abstract int Comparer( T a, T b );

		public abstract XmlNode ToXmlNode();
		#endregion

		#region Methods
		protected int IndexOf( T value )
		{

			int i = -1;
			if ( !(value is null) )
				while ( (++i < Count) && !this.Equals( this._data[ i ], value) ) ;

			return (i < Count) ? i : -1;
		}

		public T[] ToArray() => this._data.ToArray();

		public void Add( T pnbr, bool postSort = false )
		{
			if ( (Count < Limit) && !(pnbr is null) && !pnbr.IsEmpty() )
			{
				int i = IndexOf( pnbr ); // See if it already exists
				if ( i < 0 ) // it doesn't..
					this._data.Add( pnbr );
				else // There is an empty record, replace it with this number.
					this._data[ i ] = pnbr;

				if ( postSort && Sorted ) 
					this._data.Sort( ( T a, T b ) => Comparer( a, b ) );
			}
		}

		public void AddRange( IEnumerable<T> data, bool postSort = false )
		{
			if ( !(data is null) )
			{
				foreach ( T nbr in data )
					this.Add( nbr, false );

				if ( postSort && Sorted )
					this._data.Sort( ( a, b ) => Comparer( a, b ) );
			}
		}

		public T Remove( T data )
		{
			T result = default( T );
			int i = IndexOf( data );
			if ( i >= 0 )
			{
				result = this._data[ i ];
				this._data.RemoveAt( i );
			}
			return result;
		}

		public void Sort() => this._data.Sort( (a,b) => Comparer(a,b) );

		protected bool Equals( T obj1, T obj2 ) => Comparer( obj1, obj2 ) == 0;

		protected XmlNode CreateXmlNode( string tag = null )
		{
			this.XmlTag = tag; // This won't take if a value has already been assigned!
			return CreateXmlNode();
		}

		protected XmlNode CreateXmlNode()
		{
			XmlNode node = $"<{XmlTag}{(Sorted ? " sorted='TRUE'" : "")}{(Limit < int.MaxValue ? $" limit='{Limit}'" : "")}</{XmlTag}>".ToXmlNode();

			foreach ( T pn in this ) node.AppendChild( pn.ToXmlNode() );

			return node;
		}

		public T[] Filter( Y type )
		{
			List<T> results = new List<T>();
			foreach ( T ph in this )
				if ( ph.Intersects( type ) || ph.Unknown ) results.Add( ph );

			return results.ToArray();
		}

		protected virtual T Instantiate( XmlNode node ) => (T)Activator.CreateInstance( typeof( T ), node );

		protected virtual T Instantiate() => (T)Activator.CreateInstance( typeof( T ) );

		#region IEnumerator support
		public IEnumerator<T> GetEnumerator() => this._data.GetEnumerator();

		bool IEnumerator.MoveNext() => (++this._position) < this.Count;

		void IEnumerator.Reset() => this._position = 0;
		#endregion

		#region IDisposable Support
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
			GC.SuppressFinalize( this );
		}
		#endregion
		#endregion
	}
}
