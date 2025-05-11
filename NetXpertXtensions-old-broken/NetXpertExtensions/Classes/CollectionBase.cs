using System.Collections;
using System.Text.RegularExpressions;

namespace NetXpertExtensions.Classes
{
#nullable disable

	/// <summary>Functions as an improved <seealso cref="List{string}"/> variant.</summary>
	/// <remarks>Adds functionality to emulate a <seealso cref="Queue"/> or <seealso cref="Stack"/>, and 
	/// to search/extract elements via <seealso cref="Regex"/></remarks>
	public abstract class CollectionBase<T> : IEnumerator<T>, IEnumerable<T>, IEnumerable, ICollection
	{
		#region Properties
		public readonly List<T> _items = new();
		private int _position = 0;
		private bool disposedValue;
		protected bool _isSorted = false;
		private IComparer<T> _defaultComparer = null;
		#endregion

		#region Constructors
		protected CollectionBase( bool allowDuplicates = true, bool allowEmptyEntries = false )
		{
			this.AllowDuplicates = allowDuplicates;
			this.AllowEmptyEntries = allowEmptyEntries;
		}

		protected CollectionBase( T item, bool allowDuplicates = true, bool allowEmptyEntries = false )
		{
			this.AllowDuplicates = allowDuplicates;
			this.AllowEmptyEntries = allowEmptyEntries;
			this.Add( item );
		}

		protected CollectionBase( IEnumerable<T> items, bool allowDuplicates = false, bool allowEmptyEntries = false )
		{
			this.AllowDuplicates = allowDuplicates;
			this.AllowEmptyEntries = allowEmptyEntries;
			this.AddRange( items );
		}
		#endregion

		#region Accessors
		/// <summary>Facilitates directly setting or getting collection entries by index.</summary>
		/// <remarks>If <i>null</i> (or an empty string: &quot;&quot;) are assigned to an index by this method, it will be removed from the collection!</remarks>
		public T this[ int index ]
		{
			get => this._items[ index ];
			set
			{
				if ( value is null )
					this.RemoveAt( index );
				else
					this._items[ index ] = value;
			}
		}

		/// <summary>Reports the number of items currently managed within the collection.</summary>
		public int Count => this._items.Count;

		/// <summary>Specifies whether or not the collection will accept empty/null entries.</summary>
		public bool AllowEmptyEntries { get; set; } = false;

		/// <summary>Specifies whether or not the collection will accept duplicate entries.</summary>
		/// <remarks>If not set to <i>off</i>, this specifies which matching method will be used to detect if a duplication is <b>true</b>.</remarks>
		public bool AllowDuplicates { get; set; } = true;

		public T Current => this._items[ this._position ];

		object IEnumerator.Current => Current;

		/// <summary>Determines whether or not the collection is sorted by default.</summary>
		/// <remarks>If this is activated, the object will use the value of <see cref="DefaultComparer"/> for performing internal sort operations.</remarks>
		public bool IsSortedCollection
		{
			get => this._isSorted;
			set
			{
				if ( value != this._isSorted )
				{
					this._isSorted = value;
					if ( value && (this._defaultComparer is not null) )
						this.Sort();
				}
			}
		}

		/// <summary>This is the comparer that's used for all internal sorting/searching functions if one isn't provided.</summary>
		public IComparer<T> DefaultComparer
		{
			get => this._defaultComparer;
			set 
			{ 
				if ( value is not null ) 
				{ 
					this._defaultComparer = value;
					if (this._isSorted) this._items.Sort( value );
				} 
			}
		}

		public bool IsSynchronized => ((ICollection)this._items).IsSynchronized;

		public object SyncRoot => ((ICollection)this._items).SyncRoot;
		#endregion

		#region Methods
		/// <summary>Facilitates comparing two strings under the auspices of the <see cref="AllowDuplicates"/> setting.</summary>
		/// <remarks><b>Example:</b><br/>=> DisallowDuplicates == DuplicateMode.Off ? s1 == s2 : s1.Equals( s2, (StringComparison)DisallowDuplicates );</remarks>
		protected abstract bool ObjectEquals( T s1, T s2 );

		/// <summary>Searches the collection for the first item to match the supplied <paramref name="item"/>.</summary>
		/// <returns>-1 if no match is found, otherwise the index of the <i>first</i> matching item.</returns>
		public int IndexOf( T item )
		{
			int i = -1; while ( (++i < Count) && !item.Equals( this._items[ i ] ) ) ;
			return (i < Count) ? i : -1;
		}

		/// <summary>Adds a new string to the collection.</summary>
		/// <param name="item">The string to be added to the collection.</param>
		public void Add( T item )
		{
			if ( AllowEmptyEntries || ( item is not null ) )
			{
				if ( AllowDuplicates || (IndexOf( item ) < 0) )
				{
					if ( this._isSorted )
					{
						int i = this._items.BinarySearch( item );
						if ( i < 0 ) this._items.Insert( ~i, item );
					}
					else
						this._items.Add( item );
				}
			}
		}

		/// <summary>Adds all elements from a supplied collection of strings to this collection.</summary>
		/// <param name="newLines">An <seealso cref="IEnumerable{string}"/> collection whose contents will be added (typically a string array).</param>
		public void AddRange( IEnumerable<T> newLines )
		{
			if ( newLines is not null && (newLines.Count() > 0) )
				foreach ( var s in newLines )
					this.Add( s );
		}

		/// <summary>Returns the contents of the collection as an array.</summary>
		public T[] ToArray() => this._items.ToArray();

		/// <summary>Reports on whether the specified string exists in the collection.</summary>
		/// <remarks>The compared string looks for a FULL match. For partial matches use <seealso cref="Regex"/> via <seealso cref="Contains(Regex)"/> instead.</remarks>
		/// <param name="item">A string to search for.</param>
		/// <param name="caseSensitive">Whether or not the search differentiates by case.</param>
		/// <returns><b>TRUE</b> if a string was found in the collection that matches the one supplied, otherwise <b>FALSE</b>.</returns>
		public bool Contains( T item ) => IndexOf( item ) >= 0;

		/// <summary>Returns the contents of the collection as a comma-separated-values string (with values enclosed in quotes).</summary>
		/// <remarks><b>NOTE</b>: Every double-quote (") character occurring within the String representation of an object of the 
		/// collection will be replaced with "\x22" in the output of this function.</remarks>
		/// <param name="useTab">If <b>TRUE</b>, TABs (ASCII:0x09) are used instead of commas.</param>
		public string ToCSV( bool useTab = false )
		{
			string result = "\x22";
			foreach ( var i in this._items )
				result += ((result.Length == 1) ? "" : $"\x22{(useTab ? "\t" : ",")}\x22") + i.ToString().Replace( "\"", "\\x22" );
			return result + "\x22";
		}
//			$"\x22{this.ToString( useTab ? "\x22\x09\x22" : "\x22,\x22" )}\x22";

		/// <summary>Returns the contents of the collection as a single string.</summary>
		/// <remarks>Individual items are combined using the string provided in <i>glue</i>.</remarks>
		/// <param name="glue">A string specifying the character(s) that will bind the strings together.</param>
		public string ToString( string glue )
		{
			switch (this.Count)
			{
				case 0: return "";
				case 1: return this._items[0].ToString();
				default:
					string result = "";
					foreach ( var i in this._items )
						result += (result.Length == 0 ? glue : "") + i.ToString();
					return result;
			}
		}

		/// <summary>Removes the item from the collection at the specified index and returns it to the caller.</summary>
		/// <param name="index">The location within the collection that is to be removed.</param>
		/// <returns>The value that was removed from the collection.</returns>
		public T RemoveAt( int index )
		{
			T r = this[ index ];
			this._items.RemoveAt( index );
			return r;
		}

		// --> I genuinely have no idea what this would be used for, but I put it here anyway
		//     for the sake of algorithmic completeness..
		/// <summary>Searches for a matching string within the collection and removes it, if found.</summary>
		/// <param name="item">A string to seek and remove from the collection.</param>
		/// <param name="compare">A <seealso cref="StringComparison"/> value to govern the mechanism of matching.</param>
		/// <returns>The removed string if found, otherwise an empty string.</returns>
		public T Remove( T item )
		{
			int i = IndexOf( item );
			if ( i >= 0 )
			{
				T result = this[ i ];
				this._items.RemoveAt( i );
				return result;
			}
			return default(T);
		}

		/// <summary>Reports <b>TRUE</b> if there are any elements in the collection, otherwise <b>FALSE</b>.</summary>
		public bool Any() => this._items.Count > 0;

		#region Sorting methods.
		/// <summary>Sorts the collection using the defined <seealso cref="DefaultComparer"/>.</summary>
		/// <remarks>Auto-sorting collections use this function, and <seealso cref="DefaultComparer"/> must be
		/// defined in order for the auto-sorting feature to be enabled.</remarks>
		/// <exception cref="InvalidOperationException"></exception>
		public void Sort()
		{
			if ( this._defaultComparer is null ) throw new InvalidOperationException( $"No DefaultComparer has been defined." );
			this._items.Sort( DefaultComparer );
		}

		/// <summary>Sorts the collection using the supplied <seealso cref="IComparer{T}"/> object.</summary>
		/// <param name="iComparer">An appropriate <seealso cref="IComparer{T}"/> object to use for sorting the collection.></param>
		/// <remarks>Attempting to use this function on an auto-sorting collection will cause an <i>InvalidOperationException</i> to be thrown.</remarks>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		public void Sort( IComparer<T> iComparer )
		{
			if ( this._isSorted ) throw new InvalidOperationException( $"You cannot apply a manual sort to a self-sorting list." );
			if ( iComparer is null ) throw new ArgumentNullException( nameof( iComparer ) );
			this._items.Sort( iComparer );
		}

		/// <summary>Sorts the collection using the supplied <seealso cref="Comparison{T}"/> object.</summary>
		/// <param name="iComparer">An appropriate <seealso cref="IComparer{T}"/> object to use for sorting the collection.></param>
		/// <remarks>Attempting to use this function on an auto-sorting collection will cause an <i>InvalidOperationException</i> to be thrown.</remarks>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		public void Sort( Comparison<T> comparison )
		{
			if ( this._isSorted ) throw new InvalidOperationException( $"You cannot use a manual sort on a self-sorted list." );
			if ( comparison is null ) throw new ArgumentNullException( nameof( comparison ) );
			this._items.Sort( comparison );
		}

		#endregion
		/// <summary>Clears all entries in the collection.</summary>
		public void Clear() => this._items.Clear();

		/// <summary>(LIFO): Returns the last item from the collection (<i>and <b><u>removes</u></b> it!</i>)</summary>
		/// <returns>If the collection is not empty, the last item from it, otherwise <i>default(<typeparamref name="T"/>)</i>.</returns>
		/// <remarks>The <seealso cref="Add(T)"/> function will <i>Push</i> objects onto the stack.</remarks>
		/// <exception cref="InvalidOperationException"></exception>
		public T Pop()
		{
			if ( Count == 0 ) throw new InvalidOperationException( "There are no elements in the collection."  );
			return this.RemoveAt( Count - 1 );
		}

		/// <summary>(FIFO): Returns the first item in the collection (<i>and <b><u>removes</u></b> it!</i>)</summary>
		/// <returns>If the collection is not empty, the first item from it, otherwise <i>default(<typeparamref name="T"/>)</i>.</returns>
		/// <remarks>The <seealso cref="Add(T)"/> function will <i>Enqueue</i> items into the queue.</remarks>
		/// <exception cref="InvalidOperationException"></exception>
		public T Dequeue()
		{
			if ( Count == 0 ) throw new InvalidOperationException( "There are no elements in the collection." );
			return this.RemoveAt( 0 );
		}

		/// <summary>Injects a new string into the collection at the specified point.</summary>
		/// <param name="item">The new item to add to the collection.</param>
		/// <param name="where">The location, within the collection, where <paramref name="item"/>
		/// is to be inserted.</param>
		/// <remarks><b>NOTE:</b> If <seealso cref="IsSortedCollection"/> is <b>TRUE</b>, this simply mirrors the <seealso cref="Add(T)"/>
		/// function (and the value of <paramref name="where"/> is <i>ignored</i>).
		/// </remarks>
		public void Insert( T item, int where = 0 )
		{
			if ( this._isSorted )
				this.Add( item );
			else
			{
				where = Math.Min( Math.Max( 0, where ), Count);
				this._items.Insert( where, item );
			}
		}

		/// <summary>Facilitates building a default <seealso cref="IComparer{T}"/> object for the type managed by this collection.</summary>
		/// <param name="params">If the comparer used parameters, this facilitates passing them.</param>
		/// <returns>A new <seealso cref="IComparer{T}"/> object suitable for sorting the objects managed by this collection.</returns>
		/// <remarks>
		/// This function is called to set the initial <seealso cref="DefaultComparer"/> value for new objects of this type.<br/><br/>
		/// The abstract sub-class <seealso cref="BaseComparer"/> can be used as a skeleton to build simple custom <seealso cref="IComparer{T}"/>
		/// classes for the specific type <typeparamref name="T"/> objects that are managed in the collection.
		/// </remarks>
		public abstract IComparer<T> CreateComparer( params object[] @params );

		public bool MoveNext() => ++this._position < this._items.Count();

		public void Reset() => this._position = 0;

		#region IDisposable Support
		private void Dispose( bool disposing )
		{
			if ( !disposedValue )
			{
				if ( disposing )
				{
					// TODO: dispose managed state (managed objects)
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				disposedValue = true;
			}
		}

		// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		// ~StringCollection()
		// {
		//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		//     Dispose(disposing: false);
		// }

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose( disposing: true );
			GC.SuppressFinalize( this );
		}

		public IEnumerator GetEnumerator() =>
			((IEnumerable)this._items).GetEnumerator();

		IEnumerator<T> IEnumerable<T>.GetEnumerator() =>
			((IEnumerable<T>)this._items).GetEnumerator();

		public void CopyTo( Array array, int index ) =>
			((ICollection)this._items).CopyTo( array, index );
		#endregion
		#endregion

		/// <summary>Provides a simple framework for constructing <seealso cref="IComparer{T}"/> classes useable by the collection.</summary>
		public abstract class BaseComparer : IComparer<T>
		{
			#region Constructor
			protected BaseComparer() { }
			#endregion

			#region Methods
			/// <summary>Defines a <seealso cref="Compare(T, T)"/> method that compares two objects and reports the result.</summary>
			/// <returns>
			/// <b>&lt; 0</b> -- <i>left <u> precedes </u>right</i> in the sort order.<br/>
			/// <b>= 0</b> -- <i>left is in <u>the same position</u> as right</i> in the sort order.<br/>
			/// <b>&gt; 0</b> -- <i>left <u>follows</u> right</i> in the sort order.
			/// </returns>
			public abstract int Compare( T left, T right );
			#endregion
		}

	}
}
