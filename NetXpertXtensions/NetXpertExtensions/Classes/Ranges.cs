using System.Collections;
using System.Numerics;
using System.Reflection;
using NetXpertExtensions.Classes;

namespace NetXpertExtensions.Classes
{
	public abstract class Range
	{
		/// <summary>
		/// <b>Unknown</b> - For use when the boundary checking condition is unknown or can't be determined.<br/>
		/// <b>Inclusive</b> - All values within the range, including the first and last.<br/>
		/// <b>Exclusive</b> - All values within the range <i>except</i> the first and last values,<br/>
		/// <b>Loop</b> - All values within the range, including the first, but <i>excluding the <u>last</u></i>.<br/>
		/// <b>NotFirst</b> - All values within the range,<i>excluding</i> the first, but including the last.
		/// </summary>
		/// <remarks><see cref="BoundaryRule.Loop"/> is useful for range checking iterative loops whose Count or Length 
		/// values are higher than the number of elements within the collection.<br/>If used in an evaluation condition, 
		/// <see cref="BoundaryRule.Unknown"/> is treated as <see cref="BoundaryRule.Inclusive"/>.</remarks>
		public enum BoundaryRule { Unknown, Inclusive, Exclusive, Loop, NotFirst };
	}

	public sealed class RangeLimits<T> : Range where T : INumber<T>
	{
		#region Properties
		private T _vOne = T.Zero;
		private T _vTwo;
		private T _inc = T.One;
		#endregion

		#region Constructors
		public RangeLimits() { this._vTwo = T.Zero; }

		/// <summary>Creates a new <seealso cref="RangeLimits{T}"/> object.<br/>Up to three parameters can be supplied.</summary>
		/// <param name="values">Provide one to three values.</param>
		/// <remarks>
		/// Possible combinations:<br/>
		/// 1 parameter:  Defines the <i>upperLimit</i> value; the lower limit is set to zero and the increment to 1.<br/>
		/// 2 parameters: Defines the <i>lowerLimit</i> and <i>upperLimit</i> values; the increment is set to 1.<br/>
		/// 3 parameters: Defines the <i>lowerLimit</i>, <i>upperLimit</i> and <i>increment</i> values.<br/><br/>
		/// Additional parameters are ignored.
		/// </remarks>
		public RangeLimits( params T[] values )
		{
			switch( values.Length ) 
			{
				case 0: this._vTwo = T.Zero; break;
				case 1: this._vTwo = values[0]; break;
				case 2:
					this._vOne = T.Max( MinValue, values[0] );
					this._vTwo = T.Min( values[1], MaxValue );
					break;
				default:
					values[ 0 ] = T.Max( MinValue, values[ 0 ] );
					this._vOne = T.Min( values[ 0 ], values[ 1 ] );
					this._vTwo = T.Max( _vOne, values[ 1 ] );
					this.Increment = values[2];
					break;
			}
		}
		#endregion

		#region Accessors
		public int Count
		{
			get
			{
				if ( T.IsRealNumber( Increment ) )
				{
					decimal tm = (decimal)Convert.ChangeType( Top, typeof( decimal ) ),
						bm = (decimal)Convert.ChangeType( Bottom, typeof( decimal ) ),
						im = (decimal)Convert.ChangeType( Increment, typeof( decimal ) );

					return (int)Math.Ceiling( (tm - bm) / im );
				}

				int t = (int)Convert.ChangeType( Top, typeof( int ) ),
					b = (int)Convert.ChangeType( Bottom, typeof( int ) ),
					i = (int)Convert.ChangeType( Increment, typeof( int ) );

				return Increment == T.One ? t - b : (int)Math.Ceiling( (decimal)(t - b) / i );
			}
		}

		public T Bottom => T.Min( _vOne, _vTwo );

		public T Top => T.Max( _vOne, _vTwo );

		public T Increment
		{
			get => _inc;
			set => _inc = T.Abs( value );
		}

		private T MinValue
		{
			get
			{
				FieldInfo mvf = typeof( T ).GetField( "MinValue", BindingFlags.Public | BindingFlags.Static );

				if ( mvf is null )
					throw new NotSupportedException( typeof( T ).Name );

				return (T)mvf.GetValue( null );
			}
		}

		private T MaxValue
		{
			get
			{
				FieldInfo mvf = typeof( T ).GetField( "MaxValue", BindingFlags.Public | BindingFlags.Static );

				if ( mvf is null )
					throw new NotSupportedException( typeof( T ).Name );

				return (T)mvf.GetValue( null );
			}
		}
		#endregion

		#region Operators
		public static implicit operator RangeLimits<T>( T[] value ) =>
			(value is null) || (value.Length < 2) ? null : new( value );

		public static bool operator ==( RangeLimits<T> left, RangeLimits<T> right)
		{
			if ( left is null ) return right is null;
			if ( right is null ) return false;
			return (left.Top == right.Top) && (left.Bottom == right.Bottom) && (left.Increment == right.Increment);
		}

		public static bool operator !=( RangeLimits<T> left, RangeLimits<T> right ) => !(left == right);
		#endregion

		#region Methods
		/// <summary>
		/// Checks to see if the supplied <paramref name="value"/> lies in between the <see cref="Top"/> and <see cref="Bottom"/>
		/// values, but does <i>NOT</i> check to see if it is an actual element of the range.
		/// </summary>
		/// <param name="value">The value to compare against the range.</param>
		/// <returns>
		/// <b>TRUE</b> if the specified <paramref name="value"/> is mathematically greater-than-or-equal-to 
		/// <see cref="Bottom"/> and less-than-or-equal-to <see cref="Top"/>.
		/// </returns>
		public bool WithIn( T value, BoundaryRule type ) =>
			type switch
			{
				BoundaryRule.Exclusive => (Bottom < value) && (value < Top),
				BoundaryRule.Loop => (Bottom <= value) && (value < Top),
				BoundaryRule.NotFirst => (Bottom < value) && (value <= Top),
				_ => (Bottom <= value) && (value <= Top)
			};

		public override bool Equals( object? obj ) =>
			obj.IsDerivedFrom<Range>() ? this == obj : base.Equals( obj );

		public override int GetHashCode() => base.GetHashCode();

		public override string ToString() => $"[ {Bottom} .. {Top} ] {{{Increment}}}";
		#endregion
	}

	public sealed class Range<T> : Range, IEnumerable<T>, IEnumerator<T> where T : INumber<T>
	{
		#region Properties
		private readonly RangeLimits<T> _range;
		private bool disposedValue;
		int _position = 0;
		#endregion

		#region Constructors
		public Range() => this._range = new();
		/// <summary>Creates a new <seealso cref="Range{T}"/> object.<br/>Up to three parameters can be supplied.</summary>
		/// <param name="values">
		/// Possible combinations:<br/>
		/// 1 parameter:  Defines the <i>upperLimit</i> value; the lower limit is set to zero and the increment to 1.<br/>
		/// 2 parameters: Defines the <i>lowerLimit</i> and <i>upperLimit</i> values; the increment is set to 1.<br/>
		/// 3 parameters: Defines the <i>lowerLimit</i>, <i>upperLimit</i> and <i>increment</i> values.<br/><br/>
		/// Additional parameters are ignored.
		/// </param>
		public Range( params T[] values ) =>
			this._range = new( values );

		public Range( RangeLimits<T> range ) => this._range = range;
		#endregion

		#region Accessors
		public int Length => this._range.Count;

		public T this[ int index ] => ToArray()[ index ];

		public T Current => this[ this._position ];

		object IEnumerator.Current => Current;
		#endregion

		#region Operators
		public static implicit operator RangeLimits<T>( Range<T> value ) => value is null ? null : value._range;
		public static implicit operator Range<T>( RangeLimits<T> range ) => range is null ? null : new( range );

		public static bool operator ==(Range<T> left, RangeLimits<T> right) => 
			left is null ? right is null : left._range == right;

		public static bool operator !=( Range<T> left, RangeLimits<T> right ) => !(left == right);
		#endregion

		#region Methods
		public List<T> AsList()
		{
			List<T> list = new();
			for ( T i = this._range.Bottom; i < this._range.Top; i += this._range.Increment )
				list.Add( i );

			return list;
		}

		public T[] ToArray() => AsList().ToArray();

		/// <summary>Checks to see if the supplied <paramref name="value"/> is a valid component of the Range.</summary>
		/// <param name="value">The value to look for in the Range.</param>
		/// <returns><b>TRUE</b> if the specified <paramref name="value"/> is an element of the range.</returns>
		/// <remarks>
		/// <b>NOTE</b>: This can return false, even if the specified value is within the range, <i><b>IF</b></i> 
		/// the defined increment steps over the value. This method only returns <b>true</b> when the value actually
		/// occurs within the set of numbers that are specifically defined by the values of <see cref="Top"/>, 
		/// <see cref="Bottom"/> and <see cref="Increment"/>.
		/// </remarks>
		/// <seealso cref="In(T)"/>
		public bool Contains( T value ) => AsList().Contains( value );

		/// <summary>
		/// Checks to see if the supplied <paramref name="value"/> lies in between the <see cref="Top"/> and <see cref="Bottom"/>
		/// values, but does <i>NOT</i> check to see if it is an actual element of the range.
		/// </summary>
		/// <param name="value">The value to compare against the range.</param>
		/// <param name="type">Defines the <seealso cref="RangeLimits{T}.Type"/> of check to perform.</param>
		/// <returns>
		/// <b>TRUE</b> if the specified <paramref name="value"/> is mathematically greater-than-or-equal-to 
		/// <see cref="Bottom"/> and less-than-or-equal-to <see cref="Top"/>.
		/// </returns>
		/// <seealso cref="Contains(T)"/>
		public bool WithIn( T value, BoundaryRule type = BoundaryRule.Loop ) => this._range.WithIn( value, type );

		public IEnumerator<T> GetEnumerator() => this.AsList().GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => this.AsList().GetEnumerator();

		public bool MoveNext() => ++this._position > Length;

		public void Reset() => this._position = 0;

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
		// ~Range()
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

		public override string ToString() => this._range.ToString();

		public override bool Equals( object? obj ) =>
			obj.IsDerivedFrom<Range>() ? this == obj : base.Equals( obj );

		public override int GetHashCode() => base.GetHashCode();
		#endregion
	}
}

namespace NetXpertExtensions
{ 
	public static class RangeExtension
	{
		public static bool InRange<N>( this N source, N top, N bottom = default(N), Classes.Range.BoundaryRule comparisonType = Classes.Range.BoundaryRule.Loop ) where N : INumber<N> =>
			new RangeLimits<N>( bottom, top, N.One ).WithIn( source, comparisonType );

		public static bool InRange<N>( this N source, RangeLimits<N> range, Classes.Range.BoundaryRule comparisonType = Classes.Range.BoundaryRule.Loop ) where N : INumber<N> =>
			range is not null && range.WithIn( source, comparisonType );

		public static bool InRange<N>( this N source, Range<N> range, Classes.Range.BoundaryRule comparisonType = Classes.Range.BoundaryRule.Loop ) where N : INumber<N> =>
			range is not null && range.WithIn( source, comparisonType );

		public static bool InRange( this int source, ICollection collection, Classes.Range.BoundaryRule comparisonType = Classes.Range.BoundaryRule.Loop ) =>
			collection is null ? false : new RangeLimits<int>( 0, collection.Count, 1).WithIn(source, comparisonType );
	}
}
