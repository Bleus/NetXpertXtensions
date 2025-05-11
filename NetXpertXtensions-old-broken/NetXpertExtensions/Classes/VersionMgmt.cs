using System.Text.RegularExpressions;

namespace NetXpertExtensions.Classes
{
	#nullable disable

	// Look Ma! Recursion everywhere!
	public sealed class VersionMgmt
	{
		#region Properties
		private char _separator = '.';
		#endregion

		#region Constructors
		/// <summary>Instantiate the class via the <seealso cref="Parse(string)"/> static function.</summary>
		private VersionMgmt() { }
		#endregion

		#region Accessors
		public VersionMgmt this[ int index ]
		{
			get
			{
				if ( index < 0 ) index = Length - 1;
				if ( index >= Length ) throw new ArgumentOutOfRangeException( $"The supplied index, {index} exceeds the size of this Version value ({Length})." );

				return index == 0 ? this : HasChild ? null : this.Child[ index - 1 ];
			}
		}

		public uint Value { get; private set; } = 1;

		private bool HasChild => this.Child is not null;

		private bool IsRoot => this.Parent is null;

		private VersionMgmt Root => IsRoot ? this : this.Parent.Root;

		private VersionMgmt Parent { get; set; } = null;

		public int Length => HasChild ? Child.Length + 1 : 1;

		private VersionMgmt Child { get; set; }

		public char Separator
		{
			get => this._separator;
			set { if ( ".:/-".Contains( value ) ) { this._separator = value; } }
		}

		/// <summary>Facilitates basic serialization by encoding the version as a 64-bit unsigned integer.</summary>
		/// <remarks>
		/// When using this function, the maximum value that can be stored for each segment is:<br/>
		/// Segment 1-3:  0-1023 ( 0x003ff / 10-bits ea)<br/>
		/// Segment 4-5: 0-131070 ( 0x1ffff / 17-bits ea)<br/>
		/// Max version value: 1023.1023.1023.131070.131070 (64 bits)
		/// </remarks>
		public ulong AsInt
		{
			get => Length switch
			{
				< 3 => (this.Value & 0x1ffff) | (HasChild ? (Child.Value << 17) : 0), // range 0 - 131,070
				_ => (this.Value & 0x003ff) | Child.Value << 10, // range 0 - 1023
			};
			set
			{
				this.Value = (uint)(value & 0x0fff);
				if ( HasChild ) this.Child.AsInt = value >> 12;
			}
		}
		#endregion

		#region Operators
		public static implicit operator Version( VersionMgmt source ) => source is null ? new Version() : new Version( source.ToString( '.', 4 ) );
		public static implicit operator VersionMgmt( Version source ) => source is null ? Parse( "1.0.0.0" ) : Parse( source.ToString() );
		public static implicit operator VersionMgmt( ulong source ) => new VersionMgmt() { AsInt = source };
		public static implicit operator ulong( VersionMgmt source ) => source is null ? 0 : source.AsInt;
		#endregion

		#region Methods
		/// <summary>Faclitates incrementing the version number.</summary>
		/// <param name="value">The amount by which to increment it.</param>
		/// <param name="depth">The 0-based index of the version element to increment.</param>
		/// <returns>The new value of the element incremented.</returns>
		/// <remarks>If the supplied <paramref name="depth"/> is less than zero, or greater than the size of the version,
		/// the last element will be incremented.
		/// </remarks>
		public VersionMgmt Increment( uint value = 1, int depth = -1 )
		{
			if ( depth < 0 ) depth = int.MaxValue;

			if ( (depth > 0) && HasChild )
				this.Child.Increment( value, --depth );
			else
				this.Value += Math.Max( value, 0 );

			return this;
		}

		/// <summary>Adds a supplied child node to the version.</summary>
		/// <param name="child">The child node to add.</param>
		/// <returns>The index of the newly added child.</returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		/// <remarks>
		/// A version may only comprise up to 6 child nodes. Attempting to add beyond this limit generates an 
		/// <seealso cref="ArgumentOutOfRangeException"/>. The new node <i>is always appended to the end</i> of the
		/// version sequence, regardless of which node actually called the <seealso cref="Add(VersionMgmt)"/> method.</remarks>
		public int Add( VersionMgmt child )
		{
			if ( Root.Length > 5 ) throw new ArgumentOutOfRangeException( "This version is full." );
			if ( child is null ) throw new ArgumentNullException( nameof( child ) );

			if ( this.Child is null )
			{
				child.Parent = this;
				this.Child = child;
			}
			else 
				this.Child.Add( child );

			return Length;
		}

		public int Add( string value ) => this.Add( Parse( value ) );

		public VersionMgmt[] ToArray()
		{
			System.Collections.Generic.List<VersionMgmt> result = new() { this };
			if (HasChild) result.AddRange( Child.ToArray() );
			return result.ToArray();
		}

		/// <summary>The full version value with it's natural separators.</summary>
		public override string ToString() => this.ToString( -1 );

		/// <summary>Facilitates creating a subset string of the full version value, limited to a specified depth.</summary>
		/// <param name="maxDepth">The <i>maximum</i> number of elements to include in the result. If this exceeds the number of elements, only they will be returned.</param>
		/// <returns>A string containing the number of version elements managed up to the depth specified, using the natural separation characters of the stored version.</returns>
		/// <remarks>If the specified depth is negative, or exceeds the length of the managed version value, the entire value will be returned.</remarks>
		public string ToString( int maxDepth )
		{
			if ( maxDepth < 0 ) maxDepth = Length;
			return $"{Value}" + ((maxDepth > 0) && HasChild ? $"{this._separator}" + this.Child.ToString(maxDepth - 1) : "");
		}

		/// <summary>Facilitates returning the version string with a designated separator, to a specified depth.</summary>
		/// <param name="divider">What character to use as a separator. <b>This overrides the stored/natural separator values!</b></param>
		/// <param name="maxDepth">The maximum number of version elements to include.</param>
		public string ToString( char divider, int maxDepth = -1 )
		{
			if (maxDepth  < 0) maxDepth = Length;

			return $"{Value}" + ((maxDepth > 0) && HasChild ? $"{divider}" + this.Child.ToString( divider, maxDepth - 1 ) : "");
		}

		/// <summary>Given a string, searches for a valid version number, and parses it into a <seealso cref="VersionMgmt"/> object.</summary>
		/// <remarks>
		/// To prevent abuse, the parser only reads the first <b>six</b> (6) version elements it finds in the supplied string. If no
		/// valid version values can be found in the supplied string, an <seealso cref="ArgumentException"/> will be thrown. If 
		/// </remarks>
		public static VersionMgmt Parse( string source, uint increment = 0, int depth = -1 )
		{
			VersionMgmt result;
			if ( !string.IsNullOrWhiteSpace( source ) )
			{
				Match m = Regex.Match( source, @"(?<ver>(?:[\d]+[.:/-]){1,5}[\d]+)", RegexOptions.None );
				if ( m.Success && m.Groups[ "ver" ].Success )
				{
					m = Regex.Match( m.Groups[ "ver" ].Value, @"^(?<value>[\d]+)(?<div>[.:/-])?(?<remainder>.+)?$", RegexOptions.None );
					if ( m.Success && m.Groups[ "value" ].Success )
					{
						result = new()
						{
							Value = uint.Parse( m.Groups[ "value" ].Value ),
						};
						if ( m.Groups[ "div" ].Success )
						{
							result.Separator = m.Groups[ "div" ].Value[ 0 ];
							if ( m.Groups[ "remainder" ].Success ) result.Add( m.Groups[ "remainder" ].Value );
						}
						if ( increment > 0 ) result.Increment( increment, depth );
						return result;
					}
				}
				else
				{
					if ( Regex.IsMatch( source, @"^[\d]+$" ) )
					{
						result = new() { Value = uint.Parse( source ) };
						if (increment > 0) result.Increment( increment,depth );
						return result;
					}
				}
			}
			throw new ArgumentException( "No valid versions were found within the supplied string!" );
		}

		/// <summary>Tests a supplied string and reports if a valid version value was detected within it.</summary>
		/// <returns><b>TRUE</b> if the supplied string contains a parseable version value.</returns>
		public static bool ContainsVersion( string source )
		{
			if ( string.IsNullOrWhiteSpace( source ) ) return false;
			return Regex.IsMatch( source, @"(?<ver>(?:[\d]+[.:/-]){1,5}[\d]+)", RegexOptions.None );
		}
		#endregion
	}
}
