using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using NetXpertExtensions;
using NetXpertExtensions.Classes;
using System;

namespace SpellCheckTool
{
	/*	Parsing Schema:

	EXAMPLES:

		"ROOT"; // Optional comment
		"ROO": {*} => [TING,TED,TS,M,MS,MIE,MING]; // Produces: ROOT, ROOTING, ROOTED, ROOTS, ROOM, ROOMS, ROOMIE, ROOMING
		"ROO": {+T} => [ING,ED,S]; // Produces: ROOT, ROOTING, ROOTED, ROOTS
		"ROOT": {-T} => [M,MS,MIE,MING,K,KS,KERY]; // Produces: ROOM, ROOMS, ROOMIE, ROOMING, ROOK, ROOKS, ROOKERY

	BASICS:
		* Word declarations must occur within a single line of the source string.
		* At a minimum, the line must define a Base(root) word, but can also optionally declare a list of variants, and a comment.

	BASE(ROOT) WORD:

		* All base(root) word declarations must be encapsulated in double-quotes and must begin at character 0 of the line.
		* Each individual word definition must be terminated with a semi-colon. If there are variants defined, you do not need
		  redundant semi-colon(s), but they won't cause any errors if present.
		* Optional comments may be added to the end of lines by placing either '#' or '//' after the semi-colon and before the 
		  comment itself.
		* If variants are defined, the base(root) word's declaration must be immediately followed by an assignment operator and 
		  then the complete list of variants.
		* Base(root) words may only contain letters, a single apostrophe, and a single hyphen, but neither hyphens nor apostrophe's 
		  can be the first character of a valid base(root) word.

	VARIANTS:

		* When variants are provided, they must be associated with the base(root) word with an assignment operator. 
		* There are 3 valid assignment indicators ('=', '>' and ':') and two valid moderators ('+', and '='). Only the 
		  moderators have an actual effect, but they're optional, while indicators are always required. Moderators, when given,
		  must precede the assignment indicator.If no moderator is provided, '=' will be inferred.
			> the '+' modifier indicates that the base(root) word is NOT a valid word on it's own and its variants are mandatory.
			> the '=' modifier indicates that the base(root) word IS also a valid word on it's own.
		* If a root word requires valid Variants, but none are found, the word itself will be ignored / rejected.
		* Individual variants are enclosed within square brackets and define a single function. They must define both an Operation
		  and one or more Operands and be terminated with a semi-colon (';').
		* Operations and Operands can be optionally associated within the declaration by an arrow ('->', '=>' or '>'), an 
		  equal-sign ('='), or a colon (':'). When one is provided, any whitespace found around this indicator is ignored.

		OPERATIONS:

		* Operations are encased in brace brackets and cannot contain any invalid/unrecognized characters, including whitespace.
		* Operations consist of either a function indicator (+ or -) followed by 1 to 5 letters, OR a single asterisk (*).
		* If a function indicator is not supplied, '*' (Concatenative) is assumed.
		* In cases where the defined Operation contains letters, and the length of that declaration exceeds the length of the base
		  word, or does not match the ending of the base(root) the entire variant will be ignored/discarded.
		* The following three operations are recognized: 
			Additive (+): 
				"+[chars]" -- The value of [chars] is appended to the end of the base(root) word with each of the supplied 
				Operands then being added to the modified base(root).
			Additive+Keep (%):
				"%[chars]" -- Performs identically to an 'Add' operation, but also creates another word by adding only the Op Chars
				to the base(root) word.
			Subtractive (-):
				"-[chars]" -- The value of [chars] must be at least one character in length;  and must match with the ending of
				the base(root) word. In each defined variant, those matching characters are then replaced with supplied Operands.
			Subtractive+Keep ($):
				"$[chars]" -- A number of characters in the root equal in length to the 'chars' specification, are replaced by
				them, and all variant operands are appended.
			Concatenative (*):
				The root word is unmodified and all provided Operands are simply appended to it as-is.
				This operation does not support a [chars] declaration and the variant will be rejected if any are provided.

		OPERANDS:

		* Operands are comma-separated sets of valid characters. Valid characters are letters, hyphens, and/or apostrophes.
		* Unlike in root words, Apostrophes and Hyphens may occur anywhere within the Operand, but are still limited to only 
		  a single instance of each (per Operand)!
		* Whitespace may NOT occur within an Operand, but are fine if used between them (i.e. before/after/around the commas).
		* Operands are necessary for all operations except Concatenation, in which case providing any invalidates the Variant.
	*/

	public sealed class DictionaryWordCollection : ICollection<DictionaryWord>
	{
		#region Properties
		private readonly List<DictionaryWord> _words = new();
		#endregion

		#region Constructors
		public DictionaryWordCollection() { }

		public DictionaryWordCollection( string[] words, int perLetterLimit = -1 ) => this.AddRange( words );
		#endregion

		#region Accessors
		/// <summary>Reports the number of words managed by this collection <i><u>including</u> variants!</i></summary>
		/// <remarks>See also: <seealso cref="Length"/> for the number of <seealso cref="DictionaryWord"/> objects stored.</remarks>
		public int Count
		{
			get
			{
				int count = 0;
				foreach ( var dw in _words ) count += dw.Count;
				return count;
			}
		}

		/// <summary>Reports the number of <seealso cref="DictionaryWord"/> objects managed by the collection, <i><u>excluding</u> variants!</i></summary>
		/// <remarks>See Also: <seealso cref="Count"/> for the total number of words that are defined in this collection (including variants).</remarks>
		public int Length => this._words.Count;

		public bool IsReadOnly { get; set; } = false;

		public DictionaryWord this[ int index ] => this._words[ index ];
		#endregion

		#region Methods
		public int IndexOf( string word )
		{
			int i = -1;
			while ( (++i < Length) && !this._words[ i ].Contains( word ) ) ;
			return (i < Length) ? i : -1;
		}

		public int Find( string word ) => Find( word, 0, Length );

		private int Find( string word, int bottom, int top )
		{
			bottom = Math.Max( bottom, 0 ); // Can't start below zero!
			if ( top > Length ) top = Length;
			if ( (top > bottom) && (bottom >= 0) && (this.Length > 0) && (top <= Length) )
			{
				if ( _words[ bottom ].Contains( word ) ) return bottom;
				if ( _words[ --top ].Contains( word ) ) return top;

				if ( top - bottom > 1 )
				{
					int midPoint = (top + bottom) / 2;
					if ( _words[ midPoint ].Contains( word ) ) return midPoint;
					if ( word < _words[ midPoint ] ) return Find( word, bottom, midPoint );
					if ( word > _words[ midPoint ] ) return Find( word, midPoint, top );
				}
			}
			return -1;
		}

		public void Add( DictionaryWord item )
		{
			if ( IsReadOnly ) throw new InvalidOperationException( "New entries cannot be added while the list is ReadOnly!" );

			int i = Find( item.Root );
			if ( i < 0 )
				this._words.Add( item );
			else
				this._words[ i ].AddVariants( item.Variants );
		}

		public void Add( string word, params string[] variants )
		{
			if ( IsReadOnly ) throw new InvalidOperationException( "New entries cannot be added while the list is set to ReadOnly!" );

			this._words.Add( new( word, variants ) );
		}

		public void Add( string word, string replaceTail, params string[] variants )
		{
			if ( IsReadOnly ) throw new InvalidOperationException( "New entries cannot be added while the list is set to ReadOnly!" );

			this._words.Add( new( word, replaceTail, variants ) );
		}

		public void Add( string word )
		{
			if ( IsReadOnly ) throw new InvalidOperationException( "New entries cannot be added while the list is set to ReadOnly!" );

			var attempt = DictionaryWord.Import( word );
			if ( attempt is not null ) this.Add( attempt );
		}

		public void AddRange( params string[] words ) => this.AddRange( words, -1 );

		public void AddRange( string[] words, int perLetterLimit )
		{
			if ( IsReadOnly ) throw new InvalidOperationException( "New entries cannot be added while the list is set to ReadOnly!" );

			int[] counts = new int[ 26 ]; counts.Fill( 0 );
			if ( perLetterLimit < 1 ) perLetterLimit = int.MaxValue;

			foreach ( string word in words )
				if ( DictionaryWord.IsParseable( word ) )
				//if ( Regex.IsMatch( word, @"([a-z](([a-z]|['-])*[a-z])?)[\s,;]?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture ) )
				{
					var w = DictionaryWord.Import( word, false );
					if ( w is not null )
					{
						if ( Regex.IsMatch( w.Comment, @"[A-Z-a-z']+;", RegexOptions.CultureInvariant ) )
						{
							StringCollection check = new()
							{
								Cleaner = ( o ) => Regex.Replace( StringCollection.DefaultCleaner()( o ), @"[^A-Z-a-z';]", "" )
							};
							check.AddRange( w.Comment.Split( ';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries ) );
							if ( check.Count != w.Words.Count )
							{
								string msg = $"Checksum mismatch! -- {{{w}}}: {check.Count} -> {w.Words.Count}";
								var differences = w.Words.Differences( check );
								System.Diagnostics.Debug.WriteLine( msg );
								w = DictionaryWord.Import( word );
								if ( w is null ) throw new InvalidOperationException( msg );
							}
						}
						if ( w.Root.Length > 0 )
						{
							byte index = (byte)(0xc0 & char.ToUpper( w.Root[ 0 ], CultureInfo.InvariantCulture ));
							if ( (--index < 26) && (counts[ index ] < perLetterLimit) ) 
							{ 
								counts[ index ]++; 
								this.Add( w ); 
							}
						}
					}
				}
		}

		public void Clear()
		{
			if ( IsReadOnly ) throw new InvalidOperationException( "The collection cannot be cleared while it is ReadOnly!" );

			this._words.Clear();
		}

		public bool Contains( DictionaryWord item ) =>
			this._words.Contains( item );

		public void CopyTo( DictionaryWord[] array, int arrayIndex )
		{
			if ( IsReadOnly ) throw new InvalidOperationException( "New entries cannot be added while the list is set to ReadOnly!" );

			this._words.CopyTo( array, arrayIndex );
		}

		public bool Remove( string word )
		{
			int i = Find( word );
			if ( i >= 0 )
			{
				if ( IsReadOnly ) throw new InvalidOperationException( "Words cannot be removed from the list while is set to ReadOnly!" );

				this._words.RemoveAt( i );
				return true;
			}
			return false;
		}

		public void Sort() => this._words.Sort( DictionaryWord.DefaultComparer );

		public bool Remove( DictionaryWord item ) => Remove( item.Root );

		public IEnumerator<DictionaryWord> GetEnumerator() => this._words.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => this._words.GetEnumerator();
		#endregion
	}

	public sealed partial class DictionaryWord : StringCollection
	{
		#region Properties
		private string _rootWord = "";
		private string _comment = "";
		private readonly DictionaryVariantCollection _derivedVariants = new();
		private bool _rootIsRealWord = true;
		#endregion

		#region Constructors
		public DictionaryWord( string word ) : base( DuplicateMode.InvariantCultureIgnore )
		{
			this.Root = word;
			this._derivedVariants = new();
		}

		public DictionaryWord( string word, params string[] variants ) : base( DuplicateMode.InvariantCultureIgnore )
		{
			this.Root = word;
			foreach ( string s in variants )
				this._derivedVariants.Add( DictionaryVariant.Parse( s ) );
		}

		public DictionaryWord( string word, string replaceTail ) : base( DuplicateMode.InvariantCultureIgnore )
		{
			this.Root = word;
			this._derivedVariants.Add( DictionaryVariant.Parse( replaceTail ) );
		}

		public DictionaryWord( string word, string op, params string[] operands ) : base( DuplicateMode.InvariantCultureIgnore )
		{
			this.Root = word;
			this._derivedVariants.Add( new DictionaryVariant( op, operands ) );
		}

		private DictionaryWord() { }
		#endregion

		#region Operators
		public static implicit operator string( DictionaryWord source ) => source is null ? string.Empty : source.Root;
		public static implicit operator DictionaryWord( string source ) => new( source );

		public static bool operator ==( string left, DictionaryWord right )
		{
			if ( string.IsNullOrEmpty( left ) ) return right is null;
			if ( right is null ) return false;
			return right.Contains( left );
		}

		public static bool operator !=( string left, DictionaryWord right ) => !(left == right);

		public static bool operator <( string left, DictionaryWord right )
		{
			if ( left is null ) throw new ArgumentNullException( nameof( left ) );
			if ( right is null ) throw new ArgumentNullException( nameof( right ) );
			return string.Compare( left, right.Root, StringComparison.OrdinalIgnoreCase ) < 0;
		}

		public static bool operator >( string left, DictionaryWord right )
		{
			if ( left is null ) throw new ArgumentNullException( nameof( left ) );
			if ( right is null ) throw new ArgumentNullException( nameof( right ) );
			return string.Compare( left, right.Root, StringComparison.OrdinalIgnoreCase ) > 0;
		}

		public static bool operator <=( string left, DictionaryWord right ) => (left < right) || (left == right);

		public static bool operator >=( string left, DictionaryWord right ) => (left > right) || (left == right);
		#endregion

		#region Accessors
		public string Root
		{
			get => string.IsNullOrEmpty( this._rootWord ) ? string.Empty : this._rootWord.ToUpperInvariant();
			private set
			{
				if ( string.IsNullOrWhiteSpace( value ) )
					throw new InvalidOperationException( "You cannot assign a null, empty or white-space value as a root." );

				if ( !IsValidRoot( value ) )
					throw new InvalidOperationException( $"The supplied word is improperly formatted and/or contains invalid characters: Only letters and a single apostrophe are permitted. (\x22{value}\x22)" );

				this._rootWord = value.ToLowerInvariant();
			}
		}

		/// <summary>Reports the length of the base word managed by this object.</summary>
		public int Length => string.IsNullOrEmpty( this._rootWord ) ? 0 : this._rootWord.Length;

		/// <summary>Reports the number of words represented by this object.</summary>
		/// <remarks>This value counts the root word (if it's a stand-alone word!), plus those of all assigned variants.</remarks>
		new public int Count => this.Words.Count;

		/// <summary><b>TRUE</b> if the base(Root) word is an actual word on its own.</summary>
		public bool RootIsWord => this._rootIsRealWord;

		/// <summary>Returns <b>TRUE</b> if there are defined variants for this word.</summary>
		public bool HasVariants => this._derivedVariants.Count > 0;

		/// <summary>Returns a <seealso cref="StringCollection"/> containing all of the words that are defined by this object.</summary>
		public StringCollection Words
		{
			get
			{
				StringCollection result = new( DuplicateMode.InvariantCultureIgnore ) { IsSortedCollection = true };
				if ( this._rootIsRealWord ) result.Add( this.Root );

				foreach ( var dv in this._derivedVariants )
					result.AddRange( dv.Apply( this.Root ).ToArray() );

				return result;
			}
		}

		public Dictionary<string, int[]> Duplicates() => this._derivedVariants.DetectDuplicates( this._rootWord );

		public DictionaryVariantCollection Variants => this._derivedVariants;

		public string Comment
		{
			get => this._comment;
			set
			{
				if ( string.IsNullOrWhiteSpace( value ) )
				{
					this._comment = string.Empty;
					return;
				}
				if ( !Regex.IsMatch( value.TrimEnd(), @"^[\s]*(?:#+|/{2,})[\s]*[^\x00-\x1f\xff]+$", RegexOptions.CultureInvariant ) )
					value = " // " + Regex.Replace( value.Trim(), @"[^\x00-\x1f\xff]", "", RegexOptions.CultureInvariant );

				if ( Regex.IsMatch( value.TrimEnd(), @"^[\s]*(?:#+|/{2,})[\s]*.+$" ) ) this._comment = value.TrimEnd();
			}
		}
		#endregion

		#region Methods
		public void AddVariant( string newTail ) => this._derivedVariants.Add( DictionaryVariant.Parse( newTail ) );

		public void AddVariant( DictionaryVariant variant ) =>
			this._derivedVariants.Add( variant );

		public void AddVariants( params string[] tails ) => this._derivedVariants.AddRange( tails );

		public void AddVariants( DictionaryVariantCollection variants )
		{
			foreach ( var dv in variants )
				this.AddVariant( dv );
		}

		public void ClearVariants() => this._derivedVariants.Clear();

		public void RemoveVariant( string tail ) => this._derivedVariants.Remove( tail );

		new public bool Contains( string word ) =>
			this.Words.Contains( word, StringComparison.InvariantCultureIgnoreCase );

		public static DictionaryVariantCollection SuggestedVariants( string word )
		{
			throw new NotImplementedException();
			/*
			DictionaryWord work = new( word );
			if ( IsValidRoot( word ) )
			{
				// Plurals > -s, -es
				if ( work.EndChar != 's' ) results.Add( "s" ); else results.Add( "es" );

				// Present-tense > -ing
				switch( work.EndChar )
				{
					default:
						results.Add( "ing" );
						break;
				}

				// Past-tense > -ed

				// -er
			}

			return results;
			*/
		}

		public override string ToString()
		{
			string result = $"{(this._rootIsRealWord ? "+" : "")}{Root} {this._derivedVariants}";
			//foreach ( var v in this._derivedVariants ) result += $"{v}";
			if ( this._comment.Length > 0 ) result += this._comment;

			return result;
		}

		public override bool Equals( object? obj ) =>
			(obj is null) ? false : (obj.GetType() == typeof( string ) ? (obj as string) == this : base.Equals( obj ));

		public override int GetHashCode() => $"{this}".GetHashCode();

		public static DictionaryWord? Parse( string s, bool suppressExceptions = true )
		{
			try
			{
				if ( IsValidRoot( s ) ) return new DictionaryWord( s );

				Match m = DictionaryWordParsingPattern().Match( s );
				if ( m.Success )
				{
					DictionaryWord result = new();
					if ( m.Groups[ "word" ].Success )
					{

						result.Root = m.Groups[ "word" ].Value.ToUpperInvariant();
						result._rootIsRealWord = !m.Groups[ "mod" ].Success;

						string variants = m.Groups[ "variants" ].Success ? m.Groups[ "variants" ].Value : "";
						result.Variants.Clear();
						if ( !string.IsNullOrWhiteSpace( variants ) )
							result.Variants.AddRange( DictionaryVariantCollection.Parse( variants ).ToArray<DictionaryVariant>() );

					}
					result._comment = m.Groups[ "comment" ].Success ? m.Groups[ "comment" ].Value : "";

					return result;
				}
			}
			catch { if ( !suppressExceptions ) throw; }

			if ( suppressExceptions )
				return null;

			throw new FormatException( $"The supplied string (\"{s}\") could not be parsed into a valid DictionaryWord." );
		}

		/// <summary>Endeavours to parse a supplied string into a valid <seealso cref="DictionaryWord"/> object.</summary>
		/// <param name="s">The source string to try parsing.</param>
		/// <param name="suppressExceptions">If set <b>TRUE</b>, any exceptions will be suppressed, and <b>NULL</b> will be returned to the calling function instead.</param>
		/// <returns>If successful, a <seealso cref="DictionaryWord"/> object populated from <paramref name="s"/>, otherwise <b>NULL</b></returns>
		/// <exception cref="FormatException">Thrown if <paramref name="suppressExceptions"/> is <b>FALSE</b> and <paramref name="s"/> cannot be parsed.</exception>
		public static DictionaryWord? Import( string s, bool suppressExceptions = true )
		{
			if ( !string.IsNullOrEmpty( s ) )
			{
				try
				{
					if ( IsValidRoot( s ) ) return new DictionaryWord( s );
					if ( IsParseable( s ) ) return Parse( s, suppressExceptions );
				}
				catch { if ( !suppressExceptions ) throw; }
			}

			if ( suppressExceptions ) return null;

			throw new FormatException( $"The supplied string cannot be null, empty or whitespace." );
		}

		/// <summary>Reports <b>TRUE</b> if the supplied <paramref name="text"/> value can be successfully parsed.</summary>
		public static bool IsParseable( string text ) =>
			!string.IsNullOrWhiteSpace( text ) && DictionaryWordParsingPattern().IsMatch( text );

		/// <summary>Reports <b>TRUE</b> if the supplied <paramref name="text"/> value is a valid root word.</summary>
		/// <remarks>This tests the word itself for validity; any unrecognized punctuation (i.e. quotes, semi-colons etc) will cause this to report <b>FALSE</b>!</remarks>
		public static bool IsValidRoot( string text ) =>
			!string.IsNullOrWhiteSpace( text ) && ValidatePlainWordPattern().IsMatch( text ) && (text.CountChar( '-' ) < 2) && (text.CountChar( '\'' ) < 2);

		/// <summary>Reports <b>TRUE</b> if the supplied <paramref name="text"/> value is a valid <i>unparsed</i>root word.</summary>
		public static bool IsParseableRoot( string text )
		{
			Regex pattern = DictionaryWordParsingPattern();
			if ( !string.IsNullOrWhiteSpace( text ) && !pattern.IsMatch( text ) )
			{
				Match m = pattern.Match( text ); //Regex.IsMatch( text.Trim(), @"^\x22[a-z](([a-z]|['-])*[a-z])?\x22;$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture );
				return (m.Success && m.Groups[ "word" ].Success);
			}
			return false;
		}

		/// <summary>Provides a static <seealso cref="DefaultComparer(DictionaryWord, DictionaryWord)"/> function to facilitate sorting.</summary>
		new public static int DefaultComparer( DictionaryWord word1, DictionaryWord word2 ) =>
			string.Compare( word1.Root, word2.Root, StringComparison.OrdinalIgnoreCase );

		/// <summary>Provides a static <seealso cref="DefaultComparer(string, DictionaryWord)"/> function to facilitate sorting.</summary>
		new public static int DefaultComparer( string word1, DictionaryWord word2 ) =>
			string.Compare( word1, word2.Root, StringComparison.OrdinalIgnoreCase );

		//[GeneratedRegex( @"^""(?<word>[a-z](([a-z]|['-])*[a-z])?)""([\s]*(?<mod>[+=]?[=>:])[\s]*(?<variants>(?<variant>\[[\s]*\{(?<op>[*]|[+$-]?[A-Z'-]+)\}[\s]*([=:]|[-=]?>)?[\s]*(?<values>([A-Z'-]+([\s]*,[\s]*)?)+)[\s]*\][\s]*;)*)?)?([\s]*;)*(?<comment>[\s]*(/{2,}|\#+).*)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline )]
		[GeneratedRegex( @"^((?<mod>[+])?[\s]*(?<word>[a-z]([a-z'-]*[a-z])?)[\s]*(?<variants>(?<variant>[\s]*(?<op>[*]|[+?%$-][A-Z'-]{1,5})[\s]*[:]?[\s]*(?<values>([A-Z'-]+([\s]*,[\s]*)?)+)[\s]*;)*)?)?(?<comment>[\s]*([/]{2,}|[#]+).+)?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline )]
		public static partial Regex DictionaryWordParsingPattern();

		[GeneratedRegex( "^[a-z](([a-z]|['-])*[a-z]?)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture )]
		private static partial Regex ValidatePlainWordPattern();
		#endregion
	}

	public sealed partial class DictionaryVariantCollection : ICollection<DictionaryVariant>
	{
		#region Properties
		private readonly List<DictionaryVariant> _variants = new();
		#endregion

		#region Constructor
		public DictionaryVariantCollection( params DictionaryVariant[] tails ) =>
			this.AddRange( tails );

		public DictionaryVariantCollection() { }

		public DictionaryVariantCollection( DictionaryVariantCollection variants ) =>
			this.AddRange( variants.ToArray() );
		#endregion

		#region Accessors
		/// <summary>Reports the number of Variants managed by this collection.</summary>
		public int Count => this._variants.Count;

		public int TotalPermutations
		{
			get
			{
				int i = 0;
				foreach ( var v in this._variants ) i += v.Count;
				return i;
			}
		}

		public bool IsReadOnly { get; set; } = false;

		public DictionaryVariant this[ int index ] => this._variants[ index ];
		#endregion

		#region Methods
		public int IndexOf( DictionaryVariant? tail )
		{
			int i = -1;
			if ( tail is not null )
				while ( ++i < Count && !this._variants[ i ].Equals( tail ) ) ;

			return i < Count ? i : -1;
		}

		public void Add( DictionaryVariant? item )
		{
			int i = IndexOf( item );
			if ( i < 0 )
				this._variants.Add( item );
			else
				this._variants[ i ] = item;
		}

		public void AddRange( DictionaryVariant[] variants )
		{
			foreach ( var v in variants )
				this.Add( v );
		}

		public void AddRange( DictionaryVariantCollection variants )
		{
			foreach ( var v in variants )
				this.Add( v );
		}

		public void AddRange( StringCollection tails )
		{
			if ( tails is not null )
				foreach ( string s in tails )
					this.Add( DictionaryVariant.Parse( s ) );
		}

		public void Clear() =>
			this._variants.Clear();

		public bool Contains( DictionaryVariant tail ) => IndexOf( tail ) >= 0;

		public void CopyTo( DictionaryVariant[] array, int arrayIndex ) =>
			this._variants.CopyTo( array, arrayIndex );

		public IEnumerator<DictionaryVariant> GetEnumerator() =>
			this._variants.GetEnumerator();

		public bool Remove( string tail )
		{
			DictionaryVariant? dv = DictionaryVariant.Parse( tail );
			if ( dv is not null )
			{
				int i = IndexOf( dv );
				if ( i >= 0 )
				{
					this._variants.RemoveAt( i );
					return true;
				}
			}
			return false;
		}

		public bool Remove( DictionaryVariant item ) =>
			this.Remove( item.Tail );

		IEnumerator IEnumerable.GetEnumerator() =>
			this._variants.GetEnumerator();

		public DictionaryVariant[] ToArray() => this._variants.ToArray();

		public StringCollection Words( string rootWord )
		{
			StringCollection result = new( StringCollection.DuplicateMode.InvariantCultureIgnore );
			foreach ( var v in this._variants )
			{
				var words = v.Apply( rootWord );
				if ( (words is not null) && (words.Count > 0) )
					result.AddRange( words );
			}
			return result;
		}

		public Dictionary<string, int[]> DetectDuplicates( string root )
		{
			Dictionary<string, int[]> result = new();

			if (DictionaryWord.IsValidRoot(root))
				for (int i=0; i<this._variants.Count; i++) 
				{
					StringCollection words = this._variants[ i ].Apply( root );
					foreach( string s in words )
					{
						List<int> variantIds = result.ContainsKey( s ) ? new( result[ s ] ) : new();
						variantIds.Add( i );
						result[ s ] = variantIds.ToArray();
					}
				}

			foreach ( var dupe in result )
				if ( dupe.Value.Length < 2 ) result.Remove( dupe.Key );

			return result;
		}

		public override string ToString()
		{
			string result = "";
			if ( Count > 0 )
				for ( int i = 0; i < this.Count; i++ )
					result += $"{this[ i ].Tail}";
			return result;
		}

		public static Dictionary<int,StringCollection> ConvertDuplicateList( Dictionary<string, int[]> raw )
		{
			Dictionary<int, StringCollection> result = new();

			if ( (raw is not null) && (raw.Count > 0) )
			{
				foreach( var dupe in raw )
					if (dupe.Value.Length > 1)
					{
						foreach ( var index in dupe.Value )
							if ( result.ContainsKey( index ) )
								result[ index ].Add( dupe.Key );
							else
							{
								result.Add( index, new StringCollection( StringCollection.DuplicateMode.OrdinalIgnore, false ) );
								result[ index ].Add( dupe.Key );
							}
					}
			}
			return result;
		}

		public static DictionaryVariantCollection Parse( string s )
		{
			DictionaryVariantCollection result = new();
			if ( IsParseable( s ) )
			{
				MatchCollection mx = VariantParsingPattern().Matches( s );
				if ( mx.Count > 0 )
					foreach ( Match m in mx )
						result.Add( DictionaryVariant.Parse( m.Value ) );

				return result;
				/*
				string repl = m.Groups[ "repl" ].Success ? m.Groups[ "repl" ].Value : "";
				if ( repl.Length > 0 )
				{
					if ( m.Groups[ "tails" ].Success )
						for ( int i = 0; i < m.Groups[ "tails" ].Captures.Count; i++ )
							if ( m.Groups[ "tails" ].Captures[ i ].Value != "%" )
								result.Add( m.Groups[ "tails" ].Captures[ i ].Value );

					return result;
				}
				*/
			}
			throw new FormatException( $"The supplied string (\"{s}\") cannot be parsed into a valid DictionaryVariantCollection." );
		}

		public static bool IsParseable( string source ) =>
			!string.IsNullOrWhiteSpace( source ) && VariantParsingPattern().IsMatch( source );

		//[GeneratedRegex( @"(?<variant>\[[\s]*\{(?<op>[*]|[+$-]?[A-Z'-]+)\}[\s]*([=:]|[-=]?>)?[\s]*(?<values>([A-Z'-]+([\s]*,[\s]*)?)+)[\s]*\][\s]*;)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture )]
		[GeneratedRegex( @"(?<variant>[\s]*(?<op>[*]|[+$%?-][A-Z'-]{1,5})[\s]*[:]?[\s]*(?<values>([A-Z'-]+([\s]*,[\s]*)?)+)[\s]*;)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture )]

		private static partial Regex VariantParsingPattern();
		#endregion
	}

	/// <summary>
	/// 0) Unknown		(?): This usually means something's gone wrong, if Op Chars are defined they'll be retained but the variant can't be processed.<br/>
	/// 1) Add			(+): Adds the Op Chars to the root, then appends each of the operands to the new, extended, root.<br/>
	/// 2) AddNKeep		(%): Same as Add, but also creates an extra word by appending only the Op Chars to the root.<br/>
	/// 3) Subtract		(-): Replace Op Chars at the end of the root, then append each of the operands to the shortened root.<br/>
	/// 4) SubtractNKeep($): Replace Op Chars at the end of the root, then append each of the operands to the shortened root.<br/>
	/// 5) Concatenate	(*): No Op Chars are supported, simply adds each operand to the base(root) word.<br/>
	/// </summary>
	public enum DictionaryVariantOp { Unknown = 0, Add = 1, AddNKeep = 2, Subtract = 3, SubtractNKeep = 4, Concatenate = 5 };

	public sealed partial class DictionaryVariant
	{
		#region Properties
		public DictionaryVariantOperation? _op = null;
		public StringCollection _operands = new( StringCollection.DuplicateMode.InvariantCultureIgnore );
		#endregion

		#region Constructors
		public DictionaryVariant( string opValue, params string[] operands )
		{
			this._op = DictionaryVariantOperation.Parse( opValue ) ?? throw new FormatException( $"\x22{opValue}\x22 is not a valid operation string." );
			foreach ( string s in operands )
				if ( !string.IsNullOrWhiteSpace( s ) && Regex.IsMatch( s, @"", RegexOptions.CultureInvariant ) )
					this._operands.Add( s );

			if ( (((int)this._op.Function & 0x02) > 0) && (this._operands.Count == 0) )
				throw new InvalidOperationException( $"Operands must be defined for this {this._op.Function} variant." );
		}

		public DictionaryVariant( DictionaryVariantOp op, string value, params string[] operands )
		{
			this._op = new( op, value );
			this._operands = operands;
		}

		private DictionaryVariant() { }
		#endregion

		#region Accessors
		/// <summary>Facilitates interacting with the Variant's values as a single, comma-separated string.</summary>
		public string Tail
		{
			get => $"{string.Join(",", this._operands)}";
			set
			{
				if ( string.IsNullOrWhiteSpace( value ) )
					throw new InvalidOperationException( "You cannot assign a null, empty or white-space value as a tail." );

				if ( Regex.IsMatch( value, @"(?:[\s]*[a-z'A-Z]+)(?:[\s]*,)?", RegexOptions.IgnoreCase ) && value.CountChar( '\'' ) < 2 )
				{
					string[] work = Regex.Split( value.Trim(), @"[\s]*,[\s]*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture );
					this._operands.Clear();
					if ( work.Length > 0 )
						foreach ( string s in work )
							if ( !string.IsNullOrWhiteSpace( s ) ) this._operands.Add( s.Trim() );
				}
				else
					throw new InvalidOperationException( "The supplied tail contains invalid characters: Only letters and a single apostrophe are permitted." );
			}
		}

		/// <summary>Reports which <seealso cref="DictionaryVariantOp"/> function this variant uses.</summary>
		/// <remarks><b>NOTE:</b> This will report <i><seealso cref="DictionaryVariantOp.Unknown"/></i> if the Variant's function is unknown/undefined.</remarks>
		public DictionaryVariantOp Function => this._op is null ? DictionaryVariantOp.Unknown : this._op.Function;

		/// <summary>Reports on the number of Values specified in this variant.</summary>
		public int Count => this._operands.Count + (Function == DictionaryVariantOp.AddNKeep ? 1 : 0);

		/// <summary>Facilitates interacting with the Operands of this variant as a collection (array) of strings.</summary>
		public string[] Operands
		{
			get => this._operands;
			set
			{
				this._operands.Clear();
				if ( (value is not null) && (value.Length > 0) )
					foreach ( var s in value )
						if ( Regex.IsMatch( s, @"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture ) )
							this._operands.Add( s );
			}
		}

		/// <summary>Facilitates interrogating this variant's <seealso cref="DictionaryVariantOperation"/> information.</summary>
		public DictionaryVariantOperation Op => this._op;
		#endregion

		#region Methods
		public StringCollection Apply( string rootWord, bool suppressExceptions = true )
		{
			StringCollection result = new( StringCollection.DuplicateMode.InvariantCultureIgnore );
			try
			{
				if ( this.Function == DictionaryVariantOp.AddNKeep )
					result.Add( rootWord + this._op.Chars );

				if ( (this.Function == DictionaryVariantOp.SubtractNKeep) && (rootWord.Length > this._op.Length) )
					result.Add( rootWord[ 0..(rootWord.Length - this._op.Length) ] + this._op.Chars );

				foreach ( string operand in this._operands )
					if ( this.Function != DictionaryVariantOp.Subtract || (this._op.Chars.Length < operand.Length) )
						result.Add( this._op.Apply( rootWord, operand ) );
			}
			catch { if ( !suppressExceptions ) throw; }
			if ( result.Count == 0 )
				if ( suppressExceptions )
					result.Clear();
				else
					throw new InvalidOperationException( $"You cannot apply this Variant to \"{rootWord}\"." );

			return result;
		}

		public bool HasOperand( string value ) => this._operands is not null && this._operands.Contains( value,StringComparison.OrdinalIgnoreCase );

		public override string ToString()
		{
			string result = $"{this._op}";
			if ( _operands.Count > 0 )
				result += $":{(_operands.Count > 1 ? string.Join( ',', _operands ) : _operands[ 0 ])}";

			return $"{result};";
		}

		public bool Equals( DictionaryVariant obj )
		{
			if ( (obj is not null) && (obj._op == this._op) && (obj._operands.Count == this._operands.Count) )
			{
				StringCollection myOperands = new( StringCollection.DuplicateMode.CurrentCultureIgnore ) { IsSortedCollection = true },
					theirOperands = new( StringCollection.DuplicateMode.CurrentCultureIgnore ) { IsSortedCollection = true };

				myOperands.AddRange( this._operands );
				theirOperands.AddRange( obj._operands );

				int i = -1;
				while ( (++i < myOperands.Count) && myOperands[ i ].Equals( theirOperands[ i ], StringComparison.OrdinalIgnoreCase ) ) ;
				return i == myOperands.Count;
			}
			return false;
		}

		public void Add( string operand )
		{
			if (IsValidOperand(operand) && !this._operands.Contains( operand, StringComparison.OrdinalIgnoreCase ) )
				this._operands.Add( operand );
		}

		public static bool IsParseable( string s ) =>
			!string.IsNullOrWhiteSpace( s ) && ValidateVariantString().IsMatch( s );

		public static bool IsValidOperand( string s ) =>
			!string.IsNullOrWhiteSpace( s ) && 
			Regex.IsMatch( s, @"^[\s]*[a-zA-Z'-]+?[\s]*$", RegexOptions.CultureInvariant ) && 
			(s.CountChar( '\'' ) < 2) && 
			(s.CountChar( '-' ) < 2);

		public static DictionaryVariant? Parse( string s )
		{
			DictionaryVariant? result = null;
			if ( IsParseable( s ) )
			{
				Regex pattern = ValidateVariantString();
				if ( pattern.IsMatch( s.Trim() ) )
				{
					GroupCollection g = pattern.Matches( s.Trim() )[ 0 ].Groups;
					result = new()
					{
						_op = DictionaryVariantOperation.Parse( g[ "op" ].Value ),
						Tail = g[ "values" ].Value
					};
				}

				return result;
			}

			throw new FormatException( $"The supplied string could not be parsed into a valid DictionaryVariant object (\x22{s}\x22)." );
		}

		//[GeneratedRegex( @"^(?<variant>\[[\s]*\{(?<op>[*]|[+$?-]?[A-Z'-]+)\}[\s]*([=:]|[-=]?>)?[\s]*(?<values>([A-Z'-]+([\s]*,[\s]*)?)+)[\s]*\][\s]*;)$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant )]
		[GeneratedRegex( @"(?<variant>[\s]*(?<op>[*]|[+$%?-][A-Z'-]{1,5})[\s]*[:]?[\s]*(?<values>([A-Z'-]+([\s]*,[\s]*)?)+)[\s]*;)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant )]
		private static partial Regex ValidateVariantString();
		#endregion
	}

	public sealed class DictionaryVariantOperation
	{
		#region Properties
		private DictionaryVariantOp _op = DictionaryVariantOp.Unknown;
		private string _chars = "";
		public const byte MAX_CHAR_SIZE = 5;
		#endregion

		#region Constructors
		private DictionaryVariantOperation() { }

		public DictionaryVariantOperation( DictionaryVariantOp op, string chars )
		{
			this._op = op == DictionaryVariantOp.Unknown ? DictionaryVariantOp.Add : op;
			this.Chars = chars;
		}
		#endregion

		#region Accessors
		/// <summary>Facilitates interacting directly with this object as a string.</summary>
		public string Op
		{
			get => this.ToString();
			set
			{
				DictionaryVariantOperation temp = Parse( value );
				if ( temp is not null )
				{
					this._op = temp._op;
					this._chars = temp._chars;
				}
			}
		}

		/// <summary>Facilitates access to the Operation modifiers</summary>
		public string Chars
		{
			get => this._chars;
			set
			{
				Regex pattern = new( @"^(?:[-+=]|[*])?(?<chars>[A-Z-a-z']+)?$", RegexOptions.CultureInvariant );
				if ( !string.IsNullOrWhiteSpace( value ) && pattern.IsMatch( value ) )
				{
					Match m = pattern.Match( value );
					if ( m.Success && m.Groups[ "chars" ].Success )
					{
						value = m.Groups[ "chars" ].Value;
						this._chars = this._op switch
						{
							DictionaryVariantOp.Unknown => this._chars,
							DictionaryVariantOp.Concatenate => "",
							_ => string.IsNullOrWhiteSpace( value ) ?
								throw new FormatException( $"You cannot assign a null, empty, or whitespace value ({this._op})." )
								:
								value.Substring( 0, Math.Min( value.Length, MAX_CHAR_SIZE ) ).ToUpperInvariant()
						};
					}
				}
			}
		}

		/// <summary>Returns this operation's specific function.</summary>
		public DictionaryVariantOp Function => this._op;

		/// <summary>Reports the length of the operation's char string.</summary>
		public int Length => this._chars.Length;
		#endregion

		#region Methods
		public string Apply( string root, string operand )
		{
			switch ( this._op )
			{
				case DictionaryVariantOp.Concatenate: 
					return $"{root}{operand}".ToUpperInvariant();

				case DictionaryVariantOp.AddNKeep:
				case DictionaryVariantOp.Add:
					return $"{root}{_chars}{operand}".ToUpperInvariant();

				case DictionaryVariantOp.SubtractNKeep:
				case DictionaryVariantOp.Subtract:
					if ( root.Length > this._chars.Length )
					{
						root = root[ 0..(root.Length - _chars.Length) ];
						return  root[ ^this._chars.Length.. ].Equals( this._chars, StringComparison.InvariantCultureIgnoreCase ) 
							?
							$"{root}{operand}".ToUpperInvariant() 
							: 
							$"{root}{this._chars}{operand}".ToUpperInvariant();
					}
					break;
			};

			return $"{root}?{_chars}?{operand}".ToUpperInvariant();
		}

		public bool Equals( DictionaryVariantOperation obj )
		{
			if ( obj is null ) return false;
			return (obj.Op == this.Op) && this.Chars.Equals( obj.Chars, StringComparison.OrdinalIgnoreCase );
		}

		public override string ToString() =>
			_op switch
			{
				DictionaryVariantOp.Concatenate => "*",
				DictionaryVariantOp.Add => $"+{_chars}",
				DictionaryVariantOp.Subtract => $"-{_chars}",
				DictionaryVariantOp.AddNKeep => $"%{_chars}",
				DictionaryVariantOp.SubtractNKeep => $"${_chars}",
				_ => $"?{_chars}"
			};

		public static bool IsValidOp( string s ) =>
			!string.IsNullOrWhiteSpace( s ) && Regex.IsMatch( s, @"^(?:[*?]|[-?%$+]?[A-Z'-]+)$", RegexOptions.CultureInvariant );

		public static DictionaryVariantOperation? Parse( string s )
		{
			DictionaryVariantOperation? result = null;
			if ( !string.IsNullOrEmpty( s ) && IsValidOp( s.Trim() ) )
			{
				s = s.Trim();
				result = new()
				{
					_op = s[ 0 ] switch
					{
						'-' => DictionaryVariantOp.Subtract,
						'*' => DictionaryVariantOp.Concatenate,
						'+' => DictionaryVariantOp.Add,
						'%' => DictionaryVariantOp.AddNKeep,
						'$' => DictionaryVariantOp.SubtractNKeep,
						_ => DictionaryVariantOp.Unknown
					},
					Chars = Regex.Replace( s, @"^[-$*%?+]", "" ) // Remove the _op character and send for additional parsing!
				};
			}
			return result;
		}

		public static char Operator( DictionaryVariantOp value ) =>
			value switch
			{
				DictionaryVariantOp.Add => '+',
				DictionaryVariantOp.Subtract => '-',
				DictionaryVariantOp.AddNKeep => '%',
				DictionaryVariantOp.Concatenate => '*',
				DictionaryVariantOp.SubtractNKeep => '$',
				_ => '?'
			};
		#endregion
	}
}
