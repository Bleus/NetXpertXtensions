using System.Collections;
using System.Text.RegularExpressions;

namespace NetXpertExtensions.Classes
{
	#nullable disable

	/// <summary>Functions as an improved <seealso cref="List{string}"/> variant.</summary>
	/// <remarks>Adds functionality to emulate a <seealso cref="Queue"/> or <seealso cref="Stack"/>, and 
	/// to search/extract elements via <seealso cref="Regex"/></remarks>
	public class StringCollection : CollectionBase<string>
	{
		#region Properties
		protected DuplicateMode _disallowDuplicates = DEFAULT_DUPLICATE_MODE;
		protected const DuplicateMode DEFAULT_DUPLICATE_MODE = DuplicateMode.DontDetect;
		protected CleanFn _cleaner = null;

		/// <summary>
		/// A delegate used to define a function that can be assigned to a <seealso cref="StringCollection"/> to sanitize incoming
		/// items prior to their being added to the collection.
		/// </summary>
		/// <param name="source">An incoming object to be processed.</param>
		/// <returns>A post-processing string containing the actual text to be added to the collection.</returns>
		public delegate string CleanFn( object source );

		/// <summary>Specifies what kinds of duplicate entries to disallow into the collection.</summary>
		public enum DuplicateMode : byte { ReadOnly = 0x00, CurrentCulture, CurrentCultureIgnore, InvariantCulture, InvariantCultureIgnore, OrdinalIgnore, Ordinal, DontDetect = 0xff };
		#endregion

		#region Constructors
		public StringCollection( DuplicateMode disallowDuplicates = DEFAULT_DUPLICATE_MODE, bool allowEmptyEntries = false )
			: base( false, allowEmptyEntries )
		{
			this.DetectDuplicatesBy = disallowDuplicates;
			this.DefaultComparer = CreateComparer( CompareMode );
		}

		public StringCollection( CleanFn cleaner, DuplicateMode disallowDuplicates = DEFAULT_DUPLICATE_MODE, bool allowEmptyEntries = false )
			: base( false, allowEmptyEntries )
		{
			if ( cleaner is not null ) this.Cleaner = cleaner;
			this.DetectDuplicatesBy = disallowDuplicates;
			this.DefaultComparer = CreateComparer( CompareMode );
		}

		public StringCollection( string line, DuplicateMode disallowDuplicates = DEFAULT_DUPLICATE_MODE, bool allowEmptyEntries = false )
			: base( false, allowEmptyEntries )
		{
			this.DetectDuplicatesBy = disallowDuplicates;
			this.DefaultComparer = CreateComparer( CompareMode );
		}

		public StringCollection( string line, CleanFn cleaner, DuplicateMode disallowDuplicates = DEFAULT_DUPLICATE_MODE, bool allowEmptyEntries = false )
			: base( false, allowEmptyEntries )
		{
			if ( cleaner is not null ) this.Cleaner = cleaner;
			this.DetectDuplicatesBy = disallowDuplicates;
			this.DefaultComparer = CreateComparer( CompareMode );
		}

		public StringCollection( object item, DuplicateMode disallowDuplicates = DEFAULT_DUPLICATE_MODE, bool allowEmptyEntries = false )
			: base( item.ToString(), false, allowEmptyEntries )
		{
			this.DetectDuplicatesBy = disallowDuplicates;
			this.DefaultComparer = CreateComparer( CompareMode );
		}

		public StringCollection( object item, CleanFn cleaner, DuplicateMode disallowDuplicates = DEFAULT_DUPLICATE_MODE, bool allowEmptyEntries = false )
			: base( item.ToString(), false, allowEmptyEntries )
		{
			if ( cleaner is not null ) this.Cleaner = cleaner;
			this.DetectDuplicatesBy = disallowDuplicates;
			this.DefaultComparer = CreateComparer( CompareMode );
		}

		public StringCollection( IEnumerable<string> lines, DuplicateMode disallowDuplicates = DEFAULT_DUPLICATE_MODE, bool allowEmptyEntries = false )
			: base( lines, false, allowEmptyEntries )
		{
			this.DetectDuplicatesBy = disallowDuplicates;
			this.DefaultComparer = CreateComparer( CompareMode );
		}

		public StringCollection( IEnumerable<string> lines, CleanFn cleaner, DuplicateMode disallowDuplicates = DEFAULT_DUPLICATE_MODE, bool allowEmptyEntries = false )
			: base( lines, false, allowEmptyEntries )
		{
			if ( cleaner is not null ) this.Cleaner = cleaner;
			this.DetectDuplicatesBy = disallowDuplicates;
			this.DefaultComparer = CreateComparer( CompareMode );
		}

		public StringCollection( string source, char[] splitChars, StringSplitOptions options, DuplicateMode disallowDuplicates = DEFAULT_DUPLICATE_MODE, bool allowEmptyEntries = false )
			: base( false, allowEmptyEntries )
		{
			this.DetectDuplicatesBy = disallowDuplicates;
			this.DefaultComparer = CreateComparer( CompareMode );
			if (!string.IsNullOrWhiteSpace(source))
				this.AddRange( source.Split( splitChars, options ) );
		}
		#endregion

		#region Operators
		/// <summary>Adds a string to the collection.</summary>
		/// <remarks>Alternative to <seealso cref="Add(object)"/></remarks>
		public static StringCollection operator +(StringCollection a, string b)
		{
			if ( a is null ) return new StringCollection( b );
			if (string.IsNullOrEmpty(b)) return a;
			a.Add( b );
			return a;
		}

		/// <summary>Adds another <seealso cref="StringCollection"/> to this one.</summary>
		/// <remarks>Alternative to <seealso cref="Add(object)"/></remarks>
		public static StringCollection operator +(StringCollection a, StringCollection b)
		{
			if ( a is null ) return b; // a 'Null' return value is valid/acceptable!
			if ( b is null ) return a;
			a.AddRange( b.ToArray() );
			return a;
		}

		public static implicit operator StringCollection( string s ) => new( s );
		public static implicit operator StringCollection( string[] s ) => new( s );
		public static implicit operator StringCollection( List<string> s ) => new( s.ToArray() );
		public static implicit operator string[]( StringCollection s ) => s is null ? Array.Empty<string>() : s.ToArray();
		public static implicit operator List<string>( StringCollection s ) =>
			s is null ? new() : new( s.ToArray() );
		#endregion

		#region Accessors
		new public string this[int index] => base[ index ].ToString();

		/// <summary>Provides a local conversion mechanism between the various <see cref="DuplicateMode"/> flags and the
		/// relevant <seealso cref="StringComparison"/> value.</summary>
		protected StringComparison CompareMode => ParseMode( DetectDuplicatesBy );

		/// <summary>Facilitates defining a function that will be applied to all incoming data before it is added to the collection.</summary>
		public CleanFn Cleaner 
		{
			get => this._cleaner is null ? DefaultCleaner() : this._cleaner;
			set => this._cleaner = value; 
		}

		/// <summary>Specifies whether or not the collection will accept duplicate entries.</summary>
		/// <remarks>
		/// If not set to <i><seealso cref="DuplicateMode.DontDetect"/></i>, this specifies which 
		/// matching method will be used to detect duplicates.
		/// </remarks>
		public DuplicateMode DetectDuplicatesBy 
		{
			get => this._disallowDuplicates;
			set
			{
				this._disallowDuplicates = value;
				base.AllowDuplicates = value == DuplicateMode.DontDetect;
			}
		}

		/// <summary>Neuters the <seealso cref="CollectionBase{string}.AllowDuplicates"/> accessor of the base class.</summary>
		/// <remarks><b>NOTE:</b> 
		/// This is retained solely to provide superficial accessor continuity with the ancestor <seealso cref="CollectionBase{T}"/> class,
		/// use the <i><u><seealso cref="DetectDuplicatesBy"/></u></i> accessor instead!
		/// </remarks>
		new public bool AllowDuplicates
		{
			get => base.AllowDuplicates;
			private set { }
		}
		#endregion

		#region Methods
		/// <summary>Facilitates comparing two strings under the auspices of the <see cref="DetectDuplicatesBy"/> setting.</summary>
		protected override bool ObjectEquals( string s1, string s2 ) =>
			AllowDuplicates ? s1 == s2 : s1.Equals( s2, (StringComparison)DetectDuplicatesBy );


		public int Find( string value ) => Find( value, 0, Count, CompareMode );

		public int Find( string value, StringComparison mode ) => Find( value, 0, Count, mode );

		protected int Find( string word, int bottom, int top, StringComparison mode )
		{
			bottom = Math.Max( bottom, 0 ); // Can't start below zero!
			if ( top > Count ) top = Count;
			if ( (top > bottom) && (bottom >= 0) && (Count > 0) && (top <= Count) )
			{
				if ( _items[ bottom ].Equals( word, mode ) ) return bottom;
				if ( _items[ --top ].Equals( word, mode ) ) return top;

				if ( top - bottom > 1 )
				{
					int midPoint = (top + bottom) / 2;
					switch( string.Compare( _items[ midPoint ], word ) )
					{
						case -1: return Find( word, bottom, midPoint, mode );
						case 1: return Find( word, midPoint, top, mode );
						default: return midPoint;
					}
					//if ( _items[ midPoint ].Equals( word, mode ) ) return midPoint;
					//if ( word < _items[ midPoint ] ) return Find( word, bottom, midPoint, mode );
					//if ( word > _items[ midPoint ] ) return Find( word, midPoint, top, mode );
				}
			}
			return -1;
		}


		/// Searches the collection for the first string to match the supplied <paramref name="item"/> according to
		/// the mechanism specified in <seealso cref="DetectDuplicatesBy"/>.
		/// <param name="item">A string to search for.</param>
		/// <returns>-1 if no match is found, otherwise the index of the <i>first</i> matching string.</returns>
		/// <remarks>
		/// If this is a sorted collection, a <seealso cref="List{T}.BinarySearch(T, IComparer{T}?)"/> search is performed, 
		/// otherwise the collection is searched linearly.
		/// </remarks>
		new public int IndexOf( string item ) => IndexOf( item, CompareMode );

		/// <summary>
		/// Searches the collection for the first string to match the supplied <paramref name="item"/> according to
		/// the mechanism specified in <paramref name="compare"/>.
		/// </summary>
		/// <returns>-1 if no match is found, otherwise the index of the <i>first</i> matching string.</returns>
		public int IndexOf( string item, StringComparison compare )
		{
			if ( this._isSorted )
				return Find( item, compare );
			else
			{
				int i = -1; while ( (++i < Count) && !this._items[ i ].Equals( item, compare ) ) ;
				return (i < Count) ? i : -1;
			}
		}

		/// <summary>Searches the collection for the first string to match the supplied <seealso cref="Regex"/> pattern.</summary>
		/// <returns>-1 if no match is found, otherwise the index of the <i>first</i> matching string.</returns>
		/// <remarks>
		/// <b>Note:</b> Employing a <seealso cref="Regex"/> pattern for matching mandates the use of a linear search, and overrides the 
		/// <seealso cref="CompareMode"/> setting.
		/// </remarks>
		public int IndexOf( Regex match )
		{
			int i = -1; while ( (++i < Count) && !match.IsMatch( this._items[ i ] ) ) ;
			return (i < Count) ? i : -1;
		}

		/// <summary>Attempts to add a string to the collection.</summary>
		/// <param name="item">The string to be added.</param>
		/// <remarks>
		/// <b>NOTE:</b> This function <i>assumes</i> that the defined <seealso cref="Cleaner"/> has <i>already</i> processed the 
		/// <paramref name="item"/>!
		/// </remarks>
		protected void AddStringItem( string item )
		{
			if ( DetectDuplicatesBy == DuplicateMode.ReadOnly )
				throw new InvalidOperationException( $"You cannot add items to a collection that is marked \x22{DetectDuplicatesBy}\x22." );

			if (	( (DetectDuplicatesBy == DuplicateMode.DontDetect) || (IndexOf( item, CompareMode ) < 0) )
				 && 
					( AllowEmptyEntries || !string.IsNullOrEmpty( item ) ) 
			   ) 
				this._items.Add( item );
		}

		/// <summary>Takes an object, passes it through the <seealso cref="Cleaner"/> and sends the result to <seealso cref="AddStringItem(string)"/></summary>
		/// <param name="item">An object to try and add into the collection.</param>
		/// <remarks>
		/// <b>NOTE:</b> This function <i>assumes</i> that the defined <seealso cref="Cleaner"/> has <i><b>NOT</b></i> pre-processed the 
		/// <paramref name="item"/> and will apply it before calling <seealso cref="AddStringItem(string)"/>!
		/// </remarks>
		protected void AddNonStringItem( object item )
		{
			string value = Cleaner( item );
			this.AddStringItem( value is null ? string.Empty : value );
		}

		/// <summary>Add the <seealso cref="object.ToString"/> representation of a non-string object to the collection.</summary>
		/// <remarks><b>NOTE:</b> <paramref name="item"/> is run through the defined <seealso cref="Cleaner"/> function prior to being added to the collection.</remarks>
		public void Add( object item )
		{
			string cleaned = Cleaner( item );
			cleaned ??= string.Empty;

			if ( item.IsDerivedFrom<string>() && (AllowEmptyEntries || !string.IsNullOrEmpty(cleaned)) )
				this.AddStringItem( cleaned );
			else
			{
				if ( item is IEnumerable en ) this.AddRange( en );
				else
					this.AddNonStringItem( item );
			}
		}

		new public void Add( string item ) => this.AddStringItem( Cleaner(item) );

		/// <summary>Adds all entries from the supplied <seealso cref="StringCollection"/> to this one.</summary>
		/// <param name="data">A <seealso cref="StringCollection"/> object whose contents are to be added to this one.</param>
		public void AddRange( StringCollection data )
		{
			if ( data is not null )
				foreach ( var s in data._items )
					this.AddStringItem( Cleaner(s) );
		}

		/// <summary>Adds all elements from the supplied collection to this one.</summary>
		/// <typeparam name="T">Any type derived from <seealso cref="IEnumerable"/>.</typeparam>
		/// <param name="objects">An <seealso cref="IEnumerable"/> object whose elements are to be added to the collection.</param>
		/// <param name="recursive">
		/// If set to <b>TRUE</b>, and the children of <paramref name="objects"/> are derived from 
		/// <seealso cref="IEnumerable"/>, each one will be recursively parsed and imported accordingly.<br/>
		/// Otherwise the output of <see cref="object.ToString()"/> will be added instead.
		/// </param>
		public void AddRange<T>( T objects, bool recursive = false ) where T : IEnumerable
		{
			if ( objects is not null )
				foreach ( T item in objects )
					if ( recursive ) 
						this.AddRange( item ); 
					else 
						this.AddStringItem( Cleaner(item) );
		}

		/// <summary>Adds all elements from the supplied collection to this one.</summary>
		/// <typeparam name="T">Any class for which an <seealso cref="Array"/> is going to be passed.</typeparam>
		/// <param name="objects">An array of <typeparamref name="T"/> whose contents are to be added to the collection.</param>
		/// <param name="recursive">
		/// If set to <b>TRUE</b>, and the elements in <paramref name="objects"/> are derived from 
		/// <seealso cref="IEnumerable"/>, each one will be recursively parsed and imported accordingly.<br/>
		/// Otherwise the output of <see cref="object.ToString()"/> will be added instead.
		/// </param>
		public void AddRange<T>( T[] objects, bool recursive = false )
		{
			if ( typeof( T ).IsDerivedFrom<IEnumerable>() )
				this.AddRange( new List<T>( objects ), recursive );
			else
				foreach ( T item in objects )
					this.Add( item );
		}

		/// <summary>Reports on whether the specified string exists in the collection.</summary>
		/// <remarks>
		/// Looks for a <i>FULL</i> string match according to the specified <paramref name="compare"/> setting.<br/>
		/// For partial matches use a <seealso cref="Regex"/> pattern via <seealso cref="Contains(Regex)"/> or 
		/// <seealso cref="Contains(string, RegexOptions)"/> instead.
		/// </remarks>
		/// <param name="item">A string to search for.</param>
		/// <param name="compare">Specifies the <seealso cref="StringComparison"/> mechanism to use when looking for a match..</param>
		/// <returns><b>TRUE</b> if a string was found in the collection that matches the one supplied, otherwise <b>FALSE</b>.</returns>
		public bool Contains( string item, StringComparison compare = StringComparison.OrdinalIgnoreCase ) =>
			!string.IsNullOrWhiteSpace(item) && IndexOf( item, compare ) >= 0;

		/// <summary>Reports on whether any strings in the collection match the supplied <seealso cref="Regex"/> pattern.</summary>
		/// <param name="match">A <seealso cref="Regex"/> object that defines the match being sought.</param>
		/// <returns><b>TRUE</b> if a string was found in the collection that matches the supplied Regex, otherwise <b>FALSE</b>.</returns>
		public bool Contains( Regex match ) => match is null ? false : IndexOf( match ) >= 0;

		/// <summary>Reports on whether any strings in the collection match the supplied <seealso cref="Regex"/> pattern + options.</summary>
		/// <returns><b>TRUE</b> if a string was found in the collection that matches the supplied <seealso cref="Regex"/> pattern, otherwise <b>FALSE</b>.</returns>
		public bool Contains( string pattern, RegexOptions options ) =>
			!string.IsNullOrWhiteSpace(pattern) && IndexOf( new Regex( pattern, options ) ) >= 0;

		/// <summary>Searches the collection for all strings that match the supplied <seealso cref="Regex"/> and returns them in a new <seealso cref='StringCollection'/> object.</summary>
		/// <param name="regex">A <seealso cref="Regex"/> object that defines the matches being sought.</param>
		/// <param name="allowDuplicates">If set to <b>TRUE</b> the new collection will accept duplicate entries.</param>
		/// <param name="allowEmptyEntries">If set to <b>TRUE</b> the new collection will accept empty entries.</param>
		/// <returns>A new <seealso cref='StringCollection'/> object containing all discovered matches.</returns>
		public StringCollection Matches( Regex regex, DuplicateMode allowDuplicates, bool allowEmptyEntries )
		{
			StringCollection result = new(allowDuplicates, allowEmptyEntries);
			foreach ( string s in this._items )
				if ( regex.IsMatch( s ) ) result += s;

			return result;
		}

		/// <summary>Searches the collection for all strings that match the supplied <seealso cref="Regex"/> and returns them in a new <seealso cref='StringCollection'/> object.</summary>
		/// <param name="regex">A <seealso cref="Regex"/> object that defines the matches being sought.</param>
		/// <returns>A new <seealso cref='StringCollection'/> object containing all discovered matches.</returns>
		/// <remarks>The generated collection's <see cref="AllowEmptyEntries"/> and <see cref="AllowDuplicates"/> values will match those of the source.</remarks>
		public StringCollection Matches( Regex regex ) => Matches( regex, this.DetectDuplicatesBy, this.AllowEmptyEntries );

		/// <summary>
		/// Searches the collection for all strings that match the supplied <seealso cref="Regex"/>, returns 
		/// them (in a new <seealso cref='StringCollection'/> object), <i>and <b><u>removes</u></b> them</i> from the source.
		/// </summary>
		/// <param name="regex">A <seealso cref="Regex"/> object that defines the matches being sought.</param>
		/// <returns>A new <seealso cref='StringCollection'/> object containing the extracted matches.</returns>
		public StringCollection ExtractMatches( Regex regex, DuplicateMode allowDuplicates, bool allowEmptyEntries ) 
		{
			if ( DetectDuplicatesBy == DuplicateMode.ReadOnly )
				throw new InvalidOperationException( $"You cannot extract items from a collection that is marked \x22{DetectDuplicatesBy}\x22." );

			StringCollection result = new( allowDuplicates, allowEmptyEntries );
			for ( int i = 0; i < Count; i++ )
				if ( regex.IsMatch( this[ i ] ) )
					result += this.RemoveAt( i-- ); // "--" is to decrement the counter!

			return result;
		}

		/// <summary>
		/// Searches the collection for all strings that match the supplied <seealso cref="Regex"/>, returns 
		/// them (in a new <seealso cref='StringCollection'/> object), <i>and <b><u>removes</u></b> them</i> from the source.
		/// </summary>
		/// <param name="regex">A <seealso cref="Regex"/> object that defines the matches being sought.</param>
		/// <returns>A new <seealso cref='StringCollection'/> object containing the extracted matches.</returns>
		public StringCollection ExtractMatches( Regex regex ) =>
			ExtractMatches( regex, this.DetectDuplicatesBy, this.AllowEmptyEntries );

		/// <summary>Searches the collection for all strings that match the supplied <seealso cref="Regex"/> pattern and returns them in a new <seealso cref='StringCollection'/> object.</summary>
		/// <param name="pattern">A string containing the regex pattern to use for matching.</param>
		/// <param name="options">A <i>RegexOptions</i> value modifying the way the match is compared.</param>
		/// <returns>A new <seealso cref='StringCollection'/> object containing all discovered matches.</returns>
		/// <remarks>The generated collection's <see cref="AllowEmptyEntries"/> value will be the same as the source's.</remarks>
		public StringCollection Matches( string pattern, RegexOptions options ) =>
			this.Matches( new Regex(pattern, options) );

		/// <summary>Searches the collection for all strings that match the supplied <seealso cref="Regex"/> pattern and returns them in a new <seealso cref='StringCollection'/> object.</summary>
		/// <param name="pattern">A string containing the regex pattern to use for matching.</param>
		/// <param name="options">A <i>RegexOptions</i> value modifying the way the match is compared.</param>
		/// <param name="allowEmptyEntries">If set to <b>TRUE</b> the new collection will accept empty entries.</param>
		/// <returns>A new <seealso cref='StringCollection'/> object containing all discovered matches.</returns>
		public StringCollection Matches( string pattern, RegexOptions options, DuplicateMode allowDuplicates, bool allowEmptyEntries ) =>
			this.Matches( new Regex( pattern, options ), allowDuplicates, allowEmptyEntries );

		/// <summary>Returns the contents of the collection as a single line with entries separated by a space.</summary>
		public override string ToString() => this.ToString( " " ); // "\r\n• "

		/// <summary>Returns the contents of the collection as a single string.</summary>
		/// <remarks>Individual items are combined using the string provided in <i>glue</i>.</remarks>
		/// <param name="glue">A string specifying the character(s) that will bind the strings together.</param>
		new public string ToString( string glue ) =>
			this.Count switch
			{
				0 => "",
				1 => this._items[ 0 ],
				_ => string.Join( glue, this._items )
			};

		// I genuinely have no idea what this would be used for, but is put here for completeness..
		/// <summary>Searches for a matching string within the collection and removes it, if found.</summary>
		/// <param name="item">A string to seek and remove from the collection.</param>
		/// <param name="compare">A <seealso cref="StringComparison"/> value to govern the mechanism of matching.</param>
		/// <returns>The removed string if found, otherwise an empty string.</returns>
		public string Remove( string item, StringComparison compare = StringComparison.CurrentCulture )
		{
			if ( DetectDuplicatesBy == DuplicateMode.ReadOnly )
				throw new InvalidOperationException( $"You cannot remove items from a collection that is marked \x22{DetectDuplicatesBy}\x22." );

			string result = "";
			int i = IndexOf( item, compare );
			if ( i >= 0 )
			{
				result = this[ i ];
				this._items.RemoveAt( i );
			}
			return result;
		}

		/// <summary>Facilitates inserting a string at a specific point in an unsorted collection.</summary>
		/// <param name="item">The string to insert.</param>
		/// <param name="where">The location within the collection for where to insert the item.</param>
		new public void Insert( string item, int where = 0 )
		{
			if ( DetectDuplicatesBy == DuplicateMode.ReadOnly )
				throw new InvalidOperationException( $"You cannot insert items into a collection that is marked \x22{DetectDuplicatesBy}\x22." );

			base.Insert( item, where );
		}

		/// <summary>Injects the string representation of the supplied object into the collection.</summary>
		/// <param name="item">The object whose <seealso cref="object.ToString"/> value is to be inserted.</param>
		/// <param name="where">The location within the collection for where to insert the item.</param>
		public void Insert<T>( T item, int where = 0 )
		{
			if ( DetectDuplicatesBy == DuplicateMode.ReadOnly )
				throw new InvalidOperationException( $"You cannot insert items into a collection that is marked \x22{DetectDuplicatesBy}\x22." );

			base.Insert( item is null ? "" : item.ToString(), where );
		}

		/// <summary>Compares this object's contents with those of another string collection and reports the differences.</summary>
		/// <param name="data">A <seealso cref="ICollection{T}"/> object to compare contents with.</param>
		/// <param name="mode">A <seealso cref="StringComparison"/> value to govern the mechanism of the comparison.</param>
		/// <param name="extrasName"><i>Optional:</i> Facilitates specifying an alternative name for the 'Extras' collection.</param>
		/// <param name="missingName"><i>Optional:</i> Facilitates specifying an alternative name for the 'Missing' collection.</param>
		/// <returns>
		/// A <seealso cref="Dictionary{TKey, TValue}"/> object containing two <seealso cref="StringCollection"/> objects (named according
		/// to the values of <paramref name="missingName"/> and <paramref name="extrasName"/>) which will contain the items that are 
		/// different between the two collections.<br/><br/>
		/// The result collection, keyed by the <paramref name="extrasName"/> value, contains all strings that exist in this collection, but 
		/// <i>AREN'T</i> found in the <paramref name="data"/> collection.<br/>
		/// The result collection, keyed by the <paramref name="missingName"/> value, contains all strings that exist in <paramref name="data"/> 
		/// but are <i>NOT</i> in this collection.
		/// </returns>
		/// <remarks>
		/// Only letters, numbers and the underscore are permitted in <paramref name="missingName"/> and <paramref name="extrasName"/>;
		/// anything else is removed.<br/><br/>
		/// If the names assigned in <paramref name="extrasName"/> and <paramref name="missingName"/> end up being (case insensitive) 
		/// equal, they will have <i>".Missing"</i> and <i>".Extras"</i> appended to them in order to disambiguate the results.
		/// </remarks>
		public Dictionary<string, StringCollection> Differences<T>( T data, StringComparison mode = StringComparison.InvariantCultureIgnoreCase, string missingName = "Missing", string extrasName = "Extras" ) where T : ICollection<string> =>
			Differences( (data as StringCollection), mode, missingName, extrasName );

		/// <summary>Compares this object's contents with those of another <seealso cref="StringCollection"/> and reports the differences.</summary>
		/// <param name="data">A <seealso cref="StringCollection"/> to compare contents with.</param>
		/// <param name="mode">A <seealso cref="StringComparison"/> value to govern the mechanism of the comparison.</param>
		/// <param name="extrasName"><i>Optional:</i> Facilitates specifying an alternative name for the 'Extras' collection.</param>
		/// <param name="missingName"><i>Optional:</i> Facilitates specifying an alternative name for the 'Missing' collection.</param>
		/// <returns>
		/// A <seealso cref="Dictionary{TKey, TValue}"/> object containing two <seealso cref="StringCollection"/> objects (named according
		/// to the values of <paramref name="missingName"/> and <paramref name="extrasName"/>) which will contain the items that are 
		/// different between the two collections.<br/><br/>
		/// The result collection keyed by the <paramref name="extrasName"/> value contains all strings that exist in this collection, but 
		/// <i>AREN'T</i> found in the <paramref name="data"/> collection.<br/>
		/// The result collection keyed by the <paramref name="missingName"/> value contains all strings that exist in <paramref name="data"/> 
		/// but are <i>NOT</i> in this collection.
		/// </returns>
		/// <remarks>
		/// Only letters, numbers and the underscore are permitted in <paramref name="missingName"/> and <paramref name="extrasName"/>;
		/// anything else is removed.<br/><br/>
		/// If the names assigned in <paramref name="extrasName"/> and <paramref name="missingName"/> end up being (case insensitive) 
		/// equal, they will have <i>".Missing"</i> and <i>".Extras"</i> appended to them in order to disambiguate the results.
		/// </remarks>
		public Dictionary<string, StringCollection> Differences( StringCollection data, StringComparison mode = StringComparison.InvariantCultureIgnoreCase, string missingName = "Missing", string extrasName = "Extras" )
		{
			Regex pattern = new( @"[^\w]", RegexOptions.None | RegexOptions.Compiled );
			missingName = pattern.Replace(string.IsNullOrWhiteSpace( missingName ) ? "Missing" : missingName, "" );
			extrasName = pattern.Replace(string.IsNullOrWhiteSpace( extrasName ) ? "Extras" : extrasName, "" );

			if (missingName.Equals(extrasName, StringComparison.InvariantCultureIgnoreCase))
			{
				missingName += ".Missing";
				extrasName += ".Extras";
			}

			Dictionary<string, StringCollection> result = new() {
				{ extrasName, new StringCollection( this._disallowDuplicates, this.AllowDuplicates ) { _isSorted = this._isSorted } }
			};

			StringCollection compareWith = new( this._disallowDuplicates, this.AllowDuplicates ) { _isSorted = this._isSorted },
							 source = new( this._disallowDuplicates, this.AllowDuplicates ) { _isSorted = this._isSorted };

			compareWith.AddRange( data );
			source.AddRange( this.ToArray() ); // Using 'ToArray' to force the data to be copied!

			int i = -1; while ( (++i < source.Count) && (compareWith.Count > 0) )
			{
				// Look for the source[i] item in the comparison dataset..
				int idx = compareWith.IndexOf( source[ i ], mode );
				if ( idx < 0 ) // It doesn't exist there, so it's an 'Extra'
				{
					result[ extrasName ].Add( source[ i ] );
					source.RemoveAt( i-- );
				}
				else
					// The item exists in both tables, so remove it from 'compareWith'...
					compareWith.Remove( source[ i ], mode );
			}

			if ( source.Count > 0 )
				result [extrasName].AddRange( source );

			result.Add( missingName, compareWith );
			
			return result;
		}

		/// <summary>Produces <seealso cref="StringComparison"/> value equivalents to the <seealso cref="DuplicateMode"/> value provided.</summary>
		/// <returns>
		/// <b>0</b>: <seealso cref="DuplicateMode.CurrentCulture"/> -&gt; <seealso cref="StringComparison.CurrentCulture"/> (0)<br/>
		/// <b>1</b>: <seealso cref="DuplicateMode.CurrentCultureIgnore"/> -&gt; <seealso cref="StringComparison.CurrentCultureIgnoreCase"/> (1)<br/>
		/// <b>2</b>: <seealso cref="DuplicateMode.InvariantCulture"/> -&gt; <seealso cref="StringComparison.InvariantCulture"/> (2)<br/>
		/// <b>3</b>: <seealso cref="DuplicateMode.InvariantCultureIgnore"/> -&gt; <seealso cref="StringComparison.InvariantCultureIgnoreCase"/> (3)<br/>
		/// <b>4</b>: <seealso cref="DuplicateMode.OrdinalIgnore"/> -&gt; <seealso cref="StringComparison.OrdinalIgnoreCase"/> (5)<br/>
		/// <b>5</b>: <seealso cref="DuplicateMode.Ordinal"/> -&gt; <seealso cref="StringComparison.Ordinal"/> (4)<br/>
		/// <b>6</b>: <seealso cref="DuplicateMode.DontDetect"/> -&gt; <seealso cref="StringComparison.Ordinal"/> (4)<br/>
		/// </returns>
		public static StringComparison ParseMode( DuplicateMode value ) =>
			(int)value switch
			{
				4 => StringComparison.OrdinalIgnoreCase,
				>4 => StringComparison.Ordinal, // 5-6
				_ => (StringComparison)((int)value), // 0-3
			};

		public static CleanFn DefaultCleaner() => ( o ) => o is null ? String.Empty : o.ToString();

		public override IComparer<string> CreateComparer( params object[] @params ) => 
			new MyStringComparer( (StringComparison)@params[0] );

		private class MyStringComparer : BaseComparer
		{
			#region Properties
			private readonly StringComparison _comparison = StringComparison.CurrentCultureIgnoreCase;
			#endregion

			#region Constructor
			public MyStringComparer( StringComparison stringComparison = StringComparison.CurrentCultureIgnoreCase ) :base() 
				=> this._comparison = stringComparison;
			#endregion

			#region Methods
			public override int Compare(string left, string right )
			{
				if ((left is null) || (right is null))
					throw new ArgumentNullException( left is null ? nameof(left) : nameof(right) );

				return string.Compare( left, right, _comparison );
			}
			#endregion
		}
		#endregion
	}
}
