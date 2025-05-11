using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using NetXpertCodeLibrary;
using NetXpertExtensions;
using NetXpertExtensions.Classes;

namespace SpellCheckTool
{
	/// <summary>Basic character node management object.</summary>
	/// <remarks>Stores, manages and facilitates navigation of an ascii-character-based word (tree).</remarks>
	public sealed class WordBloom
	{
		#region Properties
		/// <summary>Defines the number of characters that can be managed (in bits; 5 bits = 32 possible values).</summary>
		/// <remarks>
		/// <b>NOTE:</b> As the underlying data is stored as a <seealso cref="byte"/>, this value <i>CANNOT</i> exceed 8 bits!<br/>
		/// <b>WARNING:</b> Changing this value will have catastrophic effects unless substantial changes are made to the underlying algorithms!
		/// </remarks>
		private const byte DEPTH = 5;

		/// <summary>Stores the index value of the relevant-character.</summary>
		/// <remarks>Indexes are zero-based references to the values defined in the translation table -> <seealso cref="IndexFromChar(char)"/>.</remarks>
		private byte _value = 0xff;

		/// <summary>Stores and manages child nodes for every possible child.</summary>
		private WordBloom?[] _children;

		/// <summary>Used internally to track how long processing some function calls takes.</summary>
		private Stopwatch _buildTime = new();

		/// <summary>Specifies if this node ends a word, even if it has descendants.</summary>
		/// <remarks>DO <b>NOT</b> reference this property directly! Use the <seealso cref="IsWordEnd"/> accessor instead!!</remarks>
		private bool _isWordEnd = false;
		#endregion

		#region Constructors
		private WordBloom() => this.Clear(); // Populate all child values with null initially.

		public WordBloom( char value )
		{
			this.Clear(); // Populate all child values with null initially.
			this.Value = value;
		}

		public WordBloom( string word )
		{
			this.Clear(); // Populate all child values with null initially.
			if ( !string.IsNullOrEmpty( word ) && (word.Length > 0) )
			{
				this.Value = word[ 0 ];
				if (word.Length > 1)
					this.Add( word );
			}
		}

		public WordBloom( byte value )
		{
			this.Clear(); // Populate all child values with null initially.
			this.Value = (char)value;
		}
		#endregion

		#region Accessors
		/// <summary>Returns the number of child nodes contained within this node (and all it's descendants).</summary>
		public int Count
		{
			get
			{
				int i = 0;
				foreach ( var b in this._children )
					if (b is not null) i += b.Count + 1;

				return i;
			}
		}

		/// <summary>Facilitates getting/setting the value of this node as a <seealso cref="char"/>.</summary>
		public char Value
		{
			get => CharFromIndex( this._value );
			private set
			{
				byte i = IndexFromChar( value );
				if ( i == byte.MaxValue ) 
					throw new ArgumentOutOfRangeException( nameof( value ), $"The supplied Value is not a valid WordBloom character ('{value}' / 0x{((byte)value).ToString( "X2" )})." );
				this._value = i;
			}
		}

		/// <summary>Used to gain access to the internal timer's current value.</summary>
		public TimeSpan Timer => this._buildTime.Elapsed;

		private static byte Scope => (byte)Math.Pow( 2, DEPTH );

		/// <summary>Indicates if this node represents the end of a valid word, even if there are child nodes (for longer words which have this as s root) present.</summary>
		public bool IsWordEnd 
		{ 
			get => (this._value < Scope) && this._isWordEnd;
			private set => this._isWordEnd = value;
		}
		#endregion

		#region Operators
		public static bool operator ==(WordBloom? left, WordBloom? right)
		{
			if (left is null) return right is null;
			if (right is null) return false;
			return left._value == right._value;
		}

		public static bool operator ==(WordBloom? left, char right)
		{
			if ( left is null ) return false;
			return left._value == IndexFromChar( right );
		}

		public static bool operator ==(WordBloom? left, byte right)
		{
			if ( left is null ) return false;
			return left._value == (byte)(right & 0x1f);
		}

		public static bool operator !=( WordBloom? left, WordBloom? right) => !(left == right);

		public static bool operator !=( WordBloom? left, char right ) => !(left == right);

		public static bool operator !=(WordBloom? left, byte right) => !(left == right);

		public static implicit operator char(WordBloom? source) => (char)(source is null ? 0xff : source.Value);
		public static implicit operator string( WordBloom? source ) => source is null ? string.Empty : source.ToString();
		public static implicit operator WordBloom( char source ) => new( source );
		public static implicit operator WordBloom?( string source ) => string.IsNullOrWhiteSpace( source ) ? new() : new( source );
		#endregion

		#region Methods
		/// <summary>Populates the child nodes related to the given word.</summary>
		/// <remarks>Assumes that the first character of the given word corresponds to THIS object's value, and populates the child nodes with the <i>remainder</i> of the word.</remarks>
		/// <param name="word">The string to parse.</param>
		public void Add( string word )
		{
			this.tStart();
			word = Clean( word ); // clean up the input!
			if ( !string.IsNullOrWhiteSpace( word ) )
			{
				int i = IndexFromChar( word[ 0 ] );
				if ( this._children[ i ] is null ) this._children[ i ] = new WordBloom( word[ 0 ] ) { IsWordEnd = word.Length == 1 };
				if ( word.Length > 1 ) this._children[ i ].Add( word[ 1.. ] );
			}
			this.tStop();
		}

		public override string ToString()
		{
			string result = $"'{Value}' => [";
			for ( int i = 0; i < Scope; i++ )
				if ( this._children[ i ] is not null )
					result += (result.Length > 10 ? ", " : "") + $"'{this._children[ i ].Value}' ({this._children[ i ].Count})";

			return result + " ];";
		}

		/// <remarks>Installed primarily for debugging purposes, left primarily for the same reason.</remarks>
		/// <param name="indent">Defines the number of 3-space indents to put before the content.</param>
		public string ToString( int indent )
		{
			string pad = "".PadRight( 3*indent, ' ' ), result = $"{pad}'{Value}':\r\n";
			for ( int i = 0; i < Scope; i++ )
				if ( this._children[ i ] is not null )
					result += $"{pad}{pad}'{this._children[ i ].Value}' => [\r\n{this._children[ i ].ToString( ++indent )}\r\n{pad}{pad}{pad}\r\n";

			return $"{result}\r\n";
		}

		/// <summary>Creates and populates an entire bloom from an array containing the words to be stored within it.</summary>
		/// <param name="words">A collection of <seealso cref="string"/> values, each holding an individual word that will be incorporated into the new bloom.</param>
		/// <returns>A new <seealso cref="WordBloom"/> object populated with the words provided.</returns>
		/// <remarks>
		/// <b>NOTE:</b> By its nature, a bloom cannot store duplicate words, but if the supplied word list contains duplicates, 
		/// the processing time will be lengthened.<br/>Also, pre-sorting the list won't affect (i.e. improve) performance: the bloom ends 
		/// up being sorted naturally by its construction, regardless of the order in which the data is entered into it.
		/// </remarks>
		public static WordBloom BuildDictionary( IEnumerable<string> words )
		{
			WordBloom result = new();
			Stopwatch timer = new(); // the 'Add' function uses the internal timer, so keep a separate one for this function...
			timer.Reset();
			timer.Start();
			if ( words is not null )
				foreach ( string s in words )
					result.Add( s );

			timer.Stop();
			result._buildTime = timer; // replace the value of the internal timer with this one...
			return result;
		}

		public static WordBloom ImportResourceDictionary( string name = "Dictionary.dat" )
		{
			string dictData = TextCompress.TextUncompress(
				MiscellaneousExtensions.FetchInternalBinaryResourceFile( name )
			);
			StringCollection words = new( dictData.Split( new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries ) );
			return BuildDictionary( words );
		}

		/// <summary>Checks a provided word to see if it is defined within the collection.</summary>
		/// <param name="word">A <seealso cref="string"/> value containing the word to validate.</param>
		/// <returns><b>TRUE</b> if the provided word is defined within the matrix of the existing bloom, otherwise <b>FALSE</b>.</returns>
		/// <remarks>Due to the nature of this algorithm, this will validate ANY/ALL <i>PARTS</i> of any word that is defined within the bloom!</remarks>
		public bool Validate( string word )
		{
			this.tStart();
			if ( !string.IsNullOrEmpty( word ) )
			{
				int i = IndexFromChar( word[ 0 ] );
				if ( this._children[ i ] is not null )
					return ((word.Length == 1) && this.IsWordEnd) || this._children[ i ].Validate( word[ 1.. ] );
			}
			this.tStop();
			return false;
		}

		/// <summary>Extracts the list of stored words from the bloom.</summary>
		/// <returns>An array of strings that represents the contents of the bloom.</returns>
		/// <remarks><b>NOTE:</b> Due to the nature of the bloom, the output array should be sorted alphabetically.</remarks>
		public string[] Deconstruct()
		{
			this.tStart();
			if (this.Count == 0) return new string[] { $"{Value}" };

			List<string> list = new();
			if ( this.IsWordEnd ) list.Add( $"{Value}" );
			for ( int i = 0; i < Scope; i++ )
			{
				var n = this._children[ i ];
				if ( n is not null )
				{
					string[] children = n.Deconstruct();
					if ( children.Length > 0 )
						foreach ( string s in children )
							list.Add( $"{(this._value == 0xff ? "" : Value)}{s}" );
				}
			}

			this.tStop();
			return list.ToArray();
		}

		///<summary>Replaces all child values with <i>null</i>.</summary>
		public void Clear() { this._children = new WordBloom?[ Scope ]; Array.Fill( this._children, null ); }

		public override bool Equals( object? obj ) => base.Equals( obj );

		public override int GetHashCode() => base.GetHashCode();

		/// <summary>Translates recognized ASCII characters to their associated index values.</summary>
		/// <param name="c">A <seealso cref="char"/> value to translate.</param>
		/// <returns>A 5-bit value that represents the character's index in the collection.</returns>
		public static byte IndexFromChar( char c ) =>
			(byte)(c switch
			{
				>= 'A' and <= 'Z' => (byte)c - 0x41,
				>= 'a' and <= 'z' => (byte)c - 0x61,
				'.' => 26,			// Period
				'-' => 28,			// Hyphen
				'\x27' => 29,		// Apostrophe
				' ' => 30,			// Space
				_ => byte.MaxValue	// Unrecognized character <-> Invalid data
			});

		/// <summary>Translates an index value to the associated ASCII <seealso cref="char"/> equivalent.</summary>
		/// <param name="i">The <seealso cref="byte"/> index value to translate.</param>
		/// <returns>The <seealso cref="char"/> equivalent of the given index value.</returns>
		public static char CharFromIndex( byte i ) =>
			(char)((i switch
			{
				<26 => (char) (i + 0x41),
				26 => '.',		// Period
				28 => '-',		// Hyphen
				29 => '\x27',	// Apostrophe
				30 => ' ',		// Space
				_ => '\xea'		// Omega symbol <-> Invalid data
			}
		));

		/// <summary>Removes invalid characters from the provided strings.</summary>
		/// <returns>A string containing only <seealso cref="WordBloom"/> recognized characters.</returns>
		public static string Clean( string source )
		{
			if ( string.IsNullOrWhiteSpace( source ) ) return "";
			return Regex.Replace( source, @"[^a-zA-Z.\- ']", "", RegexOptions.CultureInvariant );
		}

		#region Stopwatch simplification
		private void tStart() 
		{
			this.tStop();
			this._buildTime.Reset(); 
			this._buildTime.Start(); 
		}

		private void tStop()
		{
			if ( this._buildTime.IsRunning ) this._buildTime.Stop();
		}
		#endregion
		#endregion
	}
}