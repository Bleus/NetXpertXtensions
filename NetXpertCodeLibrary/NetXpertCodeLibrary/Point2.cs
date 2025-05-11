using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using NetXpertExtensions;
using static NetXpertExtensions.Classes.Range;
using CompareResult = NetXpertCodeLibrary.Point2.Point2CompareResult.CompareResult;

namespace NetXpertCodeLibrary
{
	[Serializable]
	public class Point2 : ISerializable
	{
		#region Properties
		public struct Point2CompareResult
		{
			#region Properties
			[Flags] public enum CompareResult : byte { 
				None					= 0x00, // 00000000
				LessThan				= 0x01, // 00000001
				GreaterThan				= 0x02, // 00000010
				EqualTo					= 0x04, // 00000100
				LessThanOrEqualTo		= 0x05, // 00000101 = CompareResult.EqualTo | CompareResult.LessThan
				GreaterThanOrEqualTo	= 0x06, // 00000110 = CompareResult.EqualTo | CompareResult.GreaterThan
				LeftIsNull				= 0x10, // 00010000
				RightIsNull				= 0x20, // 00100000
				Unknown					= 0x40, // 01000000
				Error					= 0x80  // 10000000
			}

			/// <summary>Holds the result of compared X values.</summary>
			public CompareResult X;

			/// <summary>Holds the results of compared Y values.</summary>
			public CompareResult Y;
			#endregion

			#region Constructors
			public Point2CompareResult(
					CompareResult x = CompareResult.None,
					CompareResult y = CompareResult.None 
				)
			{ X = x; Y = y; }

			public Point2CompareResult( Point2 left, Point2 right )
			{
				X = Y = CompareResult.None;
				Compare( left, right );
			}

			public Point2CompareResult( Point left, Point right)
			{
				X = Y = CompareResult.None;
				Compare( left, right );
			}

			public Point2CompareResult( Point2 left, Point right )
			{
				X = Y = CompareResult.None;
				Compare( left, (Point2)right );
			}

			public Point2CompareResult( Point left, Point2 right )
			{
				X = Y = CompareResult.None;
				Compare( (Point2)left, right );
			}
			#endregion

			#region Accessors
			/// <summary><b>TRUE</b> if either comparison reported an error.</summary>
			public readonly bool HasError => X.HasFlag( CompareResult.Error ) || Y.HasFlag( CompareResult.Error );
			#endregion

			#region Methods
			public void Clear() => X = Y = CompareResult.None;

			public static CompareResult Compare( int left, int right )
			{
				CompareResult result = left < right ? CompareResult.LessThan : CompareResult.None;
				result |= left > right ? CompareResult.GreaterThan : CompareResult.None;
				result |= left == right ? CompareResult.EqualTo : CompareResult.None;
				
				return result;
			}

			public static Point2CompareResult Compare(Point2 left, Point2 right)
			{
				Point2CompareResult result = new();
				if ((left is null) || (right is null))
					result.X = result.Y = CompareResult.Error;

				if (left is null ) // If it's null, treat as (0,0) (error flag has been set, set LeftIsNull flag)
				{
					result.X |= CompareResult.LeftIsNull;
					left = new( 0, 0 );
				}

				if ( right is null ) // If it's null, treat as (0,0) (error flag has been set, set RightIsNull flag)
				{
					result.Y |= CompareResult.RightIsNull;
					right = new( 0, 0 );
				}

				result.X = Compare( left.X, right.X );
				result.Y = Compare( left.Y, right.Y );

				return result;
			}

			public static Point2CompareResult Compare( Point left, Point right ) =>
				Compare( (Point2)left, (Point2)right );

			public static Point2CompareResult Compare( Size left, Size right ) =>
				Compare( (Point2)left, (Point2)right );
			#endregion
		}

		[Flags] public enum PointCompareOptions : byte {
			Both				= 0x01,	// Test both values; equivalent to logical AND
			Equal				= 0x02,	// Test for equality
			LessThan			= 0x04,	// Test for less-than
			GreaterThan			= 0x08,	// Test for greater-than

			LessOrEqual			= 0x06,	// Test for less-than or equal-to
			GreaterOrEqual		= 0x0A, // Test for greater-than or equal-to

			BothEqual			= 0x01 | 0x02,
			BothLessThan		= 0x01 | 0x04,
			BothGreaterThan		= 0x01 | 0x08,
			BothLessOrEqual		= 0x01 | 0x02 | 0x04,
			BothGreaterOrEqual	= 0x01 | 0x02 | 0x08,

			/*
			///<summary>My X OR my Y is less than the corresponding attribute of the comparator.</summary>
			EitherLessThan,
			///<summary>My X OR my Y is greater than the corresponding attribute of the comparator.</summary>
			EitherGreaterThan,
			///<summary>My X OR my Y is equal to the corresponding attribute of the comparator.</summary>
			EitherEqualTo,
			///<summary>My X OR my Y is less than, or equal to, the corresponding attribute of the comparator.</summary>
			EitherLessThanOrEqualTo,
			///<summary>My X OR my Y is greater than, or equal to, the corresponding attribute of the comparator.</summary>
			EitherGreaterThanOrEqualTo,
			///<summary>Both of my X AND Y values are less than those of the comparator.</summary>
			BothLessThan,
			///<summary>Both of my X AND Y values are greater than those of the comparator.</summary>
			BothGreaterThan,
			///<summary>Both of my X AND Y values are equal to those of the comparator (the two points are equal).</summary>
			BothEqualTo,
			///<summary>Both of my X and Y values are less than, or equal to, the corresponding attributes of the comparator.</summary>
			BothLessThanOrEqualTo,
			///<summary>Both of my X and Y values are greater than, or equal to, the corresponding attributes of the comparator.</summary>
			BothGreaterThanOrEqualTo
			*/
		}
		#endregion

		#region Constructors
		/// <summary>Creates a new <see cref="Point2"/> object with values specified in "x" and "y".</summary>
		public Point2( int x = 0, int y = 0 ) => Set( x, y );

		/// <summary>Creates a new <see cref="Point2"/> object from an existing <seealso cref="Point"/> object.</summary>
		public Point2( Point p ) => Set( p.X, p.Y );

		/// <summary>Creates a new <see cref="Point2"/> object using values from an existing one.</summary>
		public Point2( Point2 p ) => Set( p.X, p.Y );

		/// <summary>Creates a new <see cref="Point2"/> object using values from an existing <seealso cref="Size"/> object.</summary>
		/// <param name="s"></param>
		public Point2( Size s ) => Set( s.Width, s.Height );

		/// <summary>Attempts to initialize this class with a string source.</summary>
		/// <exception cref="InvalidOperationException">Thrown if the passed string cannot be parsed into a valid Point2 object.</exception>
		public Point2( string value )
		{
			if ( TryParse( value, out Point2 v ) )
				this.Set( v.X, v.Y );
			else
				throw InvalidPoint( value );
		}

		/// <summary>Initializes a new <see cref="Point2"/> object using <seealso cref="SerializationInfo"/> data.</summary>
		protected Point2( SerializationInfo info, StreamingContext context )
		{
			X = info.GetInt32( this.GetType().FullName + ".Point2.X" );
			Y = info.GetInt32( this.GetType().FullName + ".Point2.Y" );
		}
		#endregion

		#region Accessors
		public int X { get; set; } = 0;

		public int Y { get; set; } = 0;
		#endregion

		#region Operators
		// Create a bi-directional translations between Point2 and Point, String and Int:
		public static implicit operator Point2( Point data ) => new ( data );
		public static implicit operator Point2( string data ) => new ( data );
		public static implicit operator Point2( int data ) => new ( data, 0 );
		public static implicit operator Point2( Size data ) => new ( data );

		public static implicit operator Point( Point2 data ) => new (data.X, data.Y);
		public static implicit operator string( Point2 data ) => data.ToString();
		public static implicit operator int( Point2 data ) => data.X;
		public static implicit operator Size( Point2 data ) => new ( data.X, data.Y );

		/// <summary>Creates a new Point2 object whose X and Y values represent the sum of the values from "left" and "right".</summary>
		public static Point2 operator +( Point2 left, Point2 right ) =>
			new ( left.X + right.X, left.Y + right.Y );

		/// <summary>Creates a new Point2 object whose X and Y values represent the result of subtracting the values of "right" from "left".</summary>
		public static Point2 operator -( Point2 left, Point2 right ) =>
			new ( left.X - right.X, left.Y - right.Y );

		/// <summary>Creates a new Point2 object whose X and Y values represent the sum of the values from "left" and "right".</summary>
		public static Point2 operator +( Point left, Point2 right ) =>
			new ( left.X + right.X, left.Y + right.Y );

		/// <summary>Creates a new Point2 object whose X and Y values represent the result of subtracting the values of "right" from "left".</summary>
		public static Point2 operator -( Point left, Point2 right ) =>
			new ( left.X - right.X, left.Y - right.Y );

		/// <summary>Creates a new Point2 object whose X and Y values represent the sum of the values from "left" and "right".</summary>
		public static Point2 operator +( Point2 left, Point right ) =>
			new ( left.X + right.X, left.Y + right.Y );

		/// <summary>Creates a new Point2 object whose X and Y values represent the result of subtracting the values of "right" from "left".</summary>
		public static Point2 operator -( Point2 left, Point right ) =>
			new ( left.X - right.X, left.Y - right.Y );

		// "Size" is not zero based, so we have to subtract 1 from it's height and width when adding it to a point.
		public static Point2 operator +( Point2 left, Size right ) =>
			new ( left.X + (right.Width - 1), left.Y + (right.Height - 1) );

		/// <summary>
		/// Creates a new Point2 object whose X and Y values represent the result of subtracting the right's Width from the left's X
		/// and the right's Height from the left's Y.
		/// </summary>
		public static Point2 operator -( Point2 left, Size right ) =>
			new ( left.X - right.Width, left.Y - right.Height );

		/// <summary>TRUE if both co-ordinates of "left" are less than their counterparts in "right".</summary>
		public static bool operator <( Point2 left, Point2 right )
		{
			if (left is null) return right is null;
			if (right is null) return false;
			return left.CompareTo( right, PointCompareOptions.BothLessThan );
		}

		/// <summary>TRUE if both co-ordinates of "left" are greater than their counterparts in "right".</summary>
		public static bool operator >( Point2 left, Point2 right )
		{
			if (left is null) return right is null;
			if (right is null) return false;
			return left.CompareTo( right, PointCompareOptions.BothGreaterThan );
		}

		/// <summary>TRUE if both co-ordinates of "left" are less than or equal-to their counterparts in "right".</summary>
		public static bool operator <=( Point2 left, Point2 right )
		{
			if (left is null) return right is null;
			if (right is null) return false;
			return left.CompareTo( right, PointCompareOptions.BothLessOrEqual );
		}

		/// <summary>TRUE if both co-ordinates of "left" are greater than or equal-to their counterparts in "right".</summary>
		public static bool operator >=( Point2 left, Point2 right )
		{
			if (left is null) return right is null;
			if (right is null) return false;
			return left.CompareTo( right, PointCompareOptions.BothGreaterOrEqual );
		}

		/// <summary>TRUE if the X and Y coordinates of the left object match those of the right.</summary>
		public static bool operator ==( Point2 left, Point2 right )
		{
			if (left is null) return right is null;
			if (right is null) return false;
			return left.CompareTo( right, PointCompareOptions.BothEqual );
		}

		/// <summary>TRUE if the X and Y coordinates of the left object match those of the right.</summary>
		public static bool operator ==( Point2 left, Point right ) =>
			left == (Point2)right;

		/// <summary>TRUE if the X and Y coordinates of the left object match those of the right.</summary>
		public static bool operator ==( Point left, Point2 right ) =>
			right == (Point2)left;

		/// <summary>TRUE if either of the X and Y coordinates of the left object don't match those of the right.</summary>
		public static bool operator !=( Point2 left, Point right ) =>
			!(left == (Point2)right);

		/// <summary>TRUE if either of the X and Y coordinates of the left object don't match those of the right.</summary>
		public static bool operator !=(Point2 left, Point2 right) =>
			!(left == (Point2)right);

		/// <summary>TRUE if either of the X and Y coordinates of the left object don't match those of the right.</summary>
		public static bool operator !=( Point left, Point2 right ) =>
			!(right == (Point2)left);

		/// <summary>Returns a new Point2 object whose X value is the sum of "left.X" and "X" and whose Y value is "left.Y".</summary>
		public static Point2 operator +( Point2 left, int X ) =>
			new ( left.X + X, left.Y );

		/// <summary>Returns a new Point2 object whose X value is the sum of "left.X" and "X" and whose Y value is "left.Y".</summary>
		public static Point2 operator +( int X, Point2 left ) =>
			left + X;

		/// <summary>Returns a new Point2 object whose X value is the result of subtracting "X" from "left.X" and whose Y value  is "left.Y".</summary>
		public static Point2 operator -( Point2 left, int X ) =>
			new ( left.X - X, left.Y );

		/// <summary>Returns a new Point2 object whose X value is the result of subtracting "left.X" from "X" and whose Y value  is "left.Y".</summary>
		public static Point2 operator -( int X, Point2 left ) =>
			new ( X - left.X, left.Y );

		public static Point2 operator --( Point2 value ) => value - 1;

		public static Point2 operator ++( Point2 value ) => value + 1;
		#endregion

		#region Methods
		/// <summary>Adds the X and Y values of the supplied <seealso cref="Point2"/> object to this object's values.</summary>
		/// <param name="p">A <seealso cref="Point2"/> object whose X and Y values are to be added.</param>
		/// <returns>This object, after it's values have been modified.</returns>
		/// <remarks>
		/// This method alters the values of the calling object! Do NOT use it if you don't want them changed, use the addition operator instead.
		/// </remarks>
		public Point2 Add(Point2 p) => this.Set( this.X + p.X, this.Y + p.Y );

		/// <summary>Subtracts the X and Y values of the supplied <seealso cref="Point"/> object from this object's values.</summary>
		/// <param name="p">A <seealso cref="Point"/> object whose X and Y values are to be subtracted.</param>
		/// <returns>This object, after it's values have been modified.</returns>
		/// <remarks>
		/// This method alters the values of the calling object! Do NOT use it if you don't want them changed,
		/// use the addition operator instead.
		/// </remarks>
		public Point2 Add( Point p ) => this.Set( this.X + p.X, this.Y + p.Y );

		/// <summary>Adds the supplied X and Y values to this object's values.</summary>
		/// <param name="x">An int value to add to this class's X value.</param>
		/// <param name="y">An int value to add to this class's Y value.</param>
		/// <returns>This object, after it's values have been modified.</returns>
		/// <remarks>
		/// This method alters the values of the calling object! Do NOT use it if you don't want them changed, use the addition operator instead.
		/// </remarks>
		public Point2 Add(int x, int y = 0) => this.Set( this.X + x, this.Y + y );

		/// <summary>Subtracts the X and Y values of the supplied <seealso cref="Point2"/> object from this object's values.</summary>
		/// <param name="p">A <seealso cref="Point2"/> object whose X and Y values are to be subtracted.</param>
		/// <returns>This object, after it's values have been modified.</returns>
		/// <remarks>
		/// This method alters the values of the calling object! Do NOT use it if you don't want them changed, use the subtraction operator instead.
		/// </remarks>
		public Point2 Subtract(Point2 p) => this.Set( this.X - p.X, this.Y - p.Y );

		/// <summary>Subtracts the X and Y values of the supplied <seealso cref="Point"/> object from this object's values.</summary>
		/// <param name="p">A <seealso cref="Point"/> object whose X and Y values are to be subtracted.</param>
		/// <returns>This object, after it's values have been modified.</returns>
		/// <remarks>
		/// This method alters the values of the calling object! Do NOT use it if you don't want them changed,
		/// use the subtraction operator instead.
		/// </remarks>
		public Point2 Subtract(Point p) => this.Set( this.X - p.X, this.Y - p.Y );

		/// <summary>Subtracts the supplied X and Y values from this object's values.</summary>
		/// <param name="x">An int value to subtract from this class's X value.</param>
		/// <param name="y">An int value to subtract from this class's Y value.</param>
		/// <returns>This object, after it's values have been modified.</returns>
		/// <remarks>
		/// This method alters the values of the calling object! Do NOT use it if you don't want them changed, use the subtraction operator instead.
		/// </remarks>
		public Point2 Subtract(int x, int y = 0) => this.Set( this.X - x, this.Y - y );

		/// <summary>Returns a new Point2 object that is the result of rotating this point around (0,0), the X/Y origin.</summary>
		/// <param name="radians">The number of radians to rotate. Negative values rotate to the left, positive rotate to the right.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the Radians value evaluates to NaN, NegativeInfinity or PositiveInfinity.</exception>
		public Point2 Rotate(double radians)
		{
			if (double.IsNaN( radians ) || double.IsInfinity( radians ))
				throw new ArgumentOutOfRangeException( nameof(radians) );
			
			if (radians == 0.0) return new Point2( this ); // if the Angle is 0, there's nothing to do.

			// Forces the radians value either into a positive or negative range of 2PI (no need to perform multiple meaningless rotations)
			radians %= ((radians < 0) ? -1 : 1) * (Math.PI * 2);

			Point2 result = new();
			double cos = Math.Cos( radians ), sin = Math.Sin( radians );
			result.X = (int)((double)(this.X * cos) - (double)(this.Y * sin));
			result.Y = (int)((double)(this.Y * cos) + (double)(this.X * sin));
			return result;
		}

		/// <summary>Returns a new <see cref="Point2"/> object that is the result of rotating this point around (0,0), the X/Y origin.</summary>
		/// <param name="degrees">The number of degrees to rotate. Negative values rotate to the left, positive rotate to the right.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the Radians value evaluates to <i>NaN, NegativeInfinity or PositiveInfinity</i>.</exception>
		public Point2 Rotate(decimal degrees) => Rotate( DegreesToRadians( degrees ) );

		/// <summary>Returns a new <see cref="Point2"/> object that is the result of rotating this point around the provided center of rotation.</summary>
		/// <param name="degrees">The number of degrees to rotate. Negative values rotate to the left, positive rotate to the right.</param>
		/// <param name="around">A <see cref="Point2"/> object specifying the center of rotation.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the Radians value evaluates to <i>NaN, NegativeInfinity or PositiveInfinity</i>.</exception>
		public Point2 Rotate(decimal degrees, Point2 around) => Rotate( DegreesToRadians( degrees ), around );

		/// <summary>Returns a new <see cref="Point2"/> object that is the result of rotating this point around the provided center of rotation.</summary>
		/// <param name="radians">The number of radians to rotate. Negative values rotate to the left, positive rotate to the right.</param>
		/// <param name="around">A <see cref="Point2"/> object specifying the center of rotation.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the Radians value evaluates to <i>NaN, NegativeInfinity or PositiveInfinity</i>.</exception>
		public Point2 Rotate(double radians, Point2 around) =>
			// Translate this point by the "around" point (make rotation point 0,0), perform the rotation about the origin, then translate it back.
			(this - around).Rotate( radians ) + around; 

		/// <summary>Creates a new Point2 object derived from the minimum of the provided X value and this object's X value.</summary>
		public Point2 Min(int x) => new( Math.Min( x, this.X ), this.Y );

		/// <summary>Creates a new Point2 object derived from the minimums of the provided X and Y values and this object's values.</summary>
		public Point2 Min(int x, int y) => new Point2( x, y ).Min( this );

		/// <summary>Creates a new Point2 object derived from the maximum of the provided X value and this object's X value.</summary>
		public Point2 Max(int x) => new( Math.Max( x, this.X ), this.Y );

		/// <summary>Creates a new Point2 object derived from the maximums of the provided X and Y values and this object's values.</summary>
		public Point2 Max(int x, int y) => new Point2( x, y ).Max( this );

		/// <summary>Creates a new <see cref="Point2"/> object derived from the minimums of this object, and those of the provided set's X and Y values.</summary>
		public Point2 Min( params Point2[] points )
		{
			Point2 result = this.Clone();
			if ( (points is not null) && (points.Length > 0) )
				foreach ( var p in points )
					result = new( Math.Min( result.X, p.X ), Math.Min( result.Y, p.Y ) );

			return result;
		}

		/// <summary>Creates a new <see cref="Point2"/> object derived from the maximums of this object, and those of the provided set's X and Y values.</summary>
		public Point2 Max( params Point2[] points )
		{
			Point2 result = this.Clone();
			if ( (points is not null) && (points.Length > 0) )
				foreach ( var p in points )
					result = new( Math.Max( result.X, p.X ), Math.Max( result.Y, p.Y ) );

			return result;
		}

		/// <summary>Compares this object to a provided Point object and returns TRUE if their X and Y values match.</summary>
		public bool Equals( Point p ) => (this.X == p.X) && (this.Y == p.Y);

		/// <summary>Compares this object to another provided Point2 object and returns TRUE if their X and Y values match.</summary>
		public bool Equals( Point2 p ) => (this.X == p.X) && (this.Y == p.Y);

		/// <summary>Compares this object to provided integer values and returns <b>TRUE</b> if they match.</summary>
		/// <remarks><paramref name="x"/> and <paramref name="y"/> can be any valid integer type.</remarks>
		public bool Equals<T>( T x, T y ) where T : IBinaryInteger<T> => x.Equals(X) && y.Equals(Y);

		/// <summary>Compares this object to a provided string value</summary>
		/// <returns><b>TRUE</b> if the string can be parsed into a <see cref="Point2"/> object whose coordinates match this one's.</returns>
		public bool Equals( string value ) => TryParse( value, out Point2 convert ) && (convert == this);

		/// <summary>Compares a provided value against this object's X value and returns <b>TRUE</b> if they match.</summary>
		/// <remarks><paramref name="x"/> can be any valid Integer type.</remarks>
		public bool Equals<T>( T x ) where T : IBinaryInteger<T> => x.Equals(this.X);

		/// <summary>Override the default .Equals comparator to intercept comparisons to Point or Point2 objects.</summary>
		public override bool Equals( object o )
		{
			if ( (o is Point2) || (o is Point) ) return this.Equals( (Point2)o );
			if ( o is Size size ) return (size.Width == this.X) && (this.Y == size.Height);
			if ( o is int i) return this.Equals( i );
			return base.Equals( o );
		}

		/// <summary>Assigns the provided values to the current object.</summary>
		public Point2 Set( int x, int y = int.MinValue )
		{
			this.X = x;
			this.Y = (y == int.MinValue) ? this.Y : y; // allow "y" to be omitted, setting only X
			return this;
		}

		/// <summary>Creates a new Rectangle class from this object and a supplied Size object.</summary>
		public Rectangle ToRectangle(Size s) => new ( this, new Size( s.Width, s.Height ) );

		/// <summary>Endeavours to create a new <see cref="Point2"/> class from a supplied string.</summary>
		/// <param name="source">The string to try and parse into this object.</param>
		/// <exception cref="InvalidOperationException">The passed string could not be successfully parsed.</exception>
		/// <remarks>
		/// This method works by removing all non-numeric characters (except valid separation: ',', ';', 'x' or 'X' and negation '-' indicators),<br/>
		/// then it identifies an integer on each side of the separation character and imports them, if possible, as X and Y coordinates respectively.
		/// </remarks>
		public static Point2 Parse( string source )
		{
			if ( !string.IsNullOrWhiteSpace( source ) )
			{
				source = Regex.Replace( source, @"[^-\d,;]", "", RegexOptions.None );
				Match m = Regex.Match( source, @"^(?<x>-?[\d]+)[;,](?<y>-?[\d]+)$", RegexOptions.None );
				if ( m.Success )
						return new Point2(
							int.Parse( m.Groups[ "x" ].Value ),
							int.Parse( m.Groups[ "y" ].Value )
							);
			}
			throw InvalidPoint( source );
		}

		/// <summary>Endeavours to parse a string into a valid Point2 object.</summary>
		/// <param name="source">The string to parse.</param>
		/// <param name="value">An object to capture the result of the parsing operation. Will be <i>null</i> if the parse attempt fails.</param>
		/// <returns><b>TRUE</b> if the operation succeeded, otherwise <b>FALSE</b>.</returns>
		public static bool TryParse(string source, out Point2 value )
		{
			try { value = Parse( source ); }
			catch { value = null; }

			return value is not null;
		}

		/// <summary>Creates a string of the form "( {x}, {y} )" from the valus of this object.</summary>
		public override string ToString() => $"( {X}, {Y} )";
			//"( X = " + this.X.ToString() + ", Y = " + this.Y.ToString() + " )";

		// Needed due to use of the "==" operator override
		public override int GetHashCode() => base.GetHashCode();

		/// <summary>Instantiates a <see cref="Point2CompareResult"/> struct using this object and another <see cref="Point2"/> object.</summary>
		/// <param name="p2">A <see cref="Point2"/> object to compare against.</param>
		/// <returns>A <see cref="Point2CompareResult"/> value derived from this object and the provided one.</returns>
		public Point2CompareResult Compare(Point2 p2) => Point2CompareResult.Compare( this, p2 );

		/// <summary>Compares the values of this object with those of another <seealso cref="Point2"/> object according to the specified <seealso cref="PointCompareOptions"/> criteria.</summary>
		/// <param name="p2">The <seealso cref="Point2"/> object to compare against this one.</param>
		/// <param name="how">A <seealso cref="PointCompareOptions"/> value indicating how the objects are to be compared.</param>
		/// <param name="ignoreNullCheck">If set <b>TRUE</b>, the comparison will not fail if <paramref name="p2"/> is null.</param>
		/// <returns><b>TRUE</b> if the relationship between this object and the provided one match according to the specified method of comparison.</returns>
		/// <remarks><b>NOTE</b>:
		/// If <paramref name="ignoreNullCheck"/> is set <b>TRUE</b>, and <paramref name="p2"/> is <i>null</i>, it will be compared as though 
		/// <paramref name="p2"/>'s value was ( 0, 0 ), otherwise an error will be reported.</remarks>
		public bool CompareTo(Point2 p2, PointCompareOptions how, bool ignoreNullCheck = false )
		{
			if (p2 is null) return false;

			Point2CompareResult cr = Compare( p2 );
			return (!cr.HasError || ignoreNullCheck) && how switch
			{
				PointCompareOptions.BothEqual => 
					cr.X.HasFlag( CompareResult.EqualTo ) && cr.Y.HasFlag( CompareResult.EqualTo ),
				PointCompareOptions.BothGreaterThan => 
					cr.X.HasFlag( CompareResult.GreaterThan ) && cr.Y.HasFlag( CompareResult.GreaterThan ),
				PointCompareOptions.BothLessThan => 
					cr.X.HasFlag( CompareResult.LessThan ) && cr.Y.HasFlag( CompareResult.LessThan ),
				PointCompareOptions.BothGreaterOrEqual =>
					cr.X.HasFlag( CompareResult.GreaterThanOrEqualTo ) && cr.Y.HasFlag( CompareResult.GreaterThanOrEqualTo ),
				PointCompareOptions.BothLessOrEqual =>
					cr.X.HasFlag( CompareResult.LessThanOrEqualTo ) && cr.Y.HasFlag( CompareResult.LessThanOrEqualTo ),
				PointCompareOptions.Equal =>
					cr.X.HasFlag( CompareResult.EqualTo ) || cr.Y.HasFlag( CompareResult.EqualTo ),
				PointCompareOptions.GreaterThan =>
					cr.X.HasFlag( CompareResult.GreaterThan ) || cr.Y.HasFlag( CompareResult.GreaterThan ),
				PointCompareOptions.LessThan =>
					cr.X.HasFlag( CompareResult.LessThan ) || cr.Y.HasFlag( CompareResult.LessThan ),
				PointCompareOptions.GreaterOrEqual =>
					cr.X.HasFlag( CompareResult.GreaterThanOrEqualTo ) || cr.Y.HasFlag( CompareResult.GreaterThanOrEqualTo ),
				PointCompareOptions.LessOrEqual =>
					cr.X.HasFlag( CompareResult.LessThanOrEqualTo ) || cr.Y.HasFlag( CompareResult.LessThanOrEqualTo ),
				_ => false,
			};
		}

		public void GetObjectData( SerializationInfo info, StreamingContext context )
		{
			info.AddValue( this.GetType().FullName + ".Point2.X", X );
			info.AddValue( this.GetType().FullName + ".Point2.Y", Y );
		}

		public Point2 Clone() => new( this.X, this.Y );

		/// <summary>Reports on whether this object's coordinates fall within the ranges specified.</summary>
		/// <param name="xHigh">The upper bounds permitted to the X value.</param>
		/// <param name="yHigh">The upper bounds permitted to the Y value.</param>
		/// <param name="xLow">The lower bounds for the X value (default = 0).</param>
		/// <param name="yLow">The lower bounds for the Y value (default = 0).</param>
		/// <param name="includeBoundaries">References the <seealso cref="BoundaryRule"/> enumerator to specify how the range check should handle the Upper and Lower bounds of the range.</param>
		/// <returns><b>TRUE</b> if this object's coordinates lie within the bounds specified.</returns>
		public bool InRange( int xHigh, int yHigh, int xLow = 0, int yLow = 0, BoundaryRule includeBoundaries = BoundaryRule.Inclusive ) =>
			this.X.InRange( xHigh, xLow, includeBoundaries ) && this.Y.InRange( yHigh, yLow, includeBoundaries );

		#region Static Methods
		private static InvalidOperationException InvalidPoint(string value) =>
			new ( $"The supplied value (\"{value}\") could not be parsed into a valid Point2 object." );

		/// <summary>Tests to see if a provided string can be parsed into a valid <see cref="Point2"/> syntax.</summary>
		/// <param name="value">A string to test.</param>
		/// <returns><b>TRUE</b> if the passed string is can be parsed to a valid <see cref="Point2"/> object.</returns>
		public static bool IsValid( string value ) => TryParse( value, out _ );

		/// <summary>Converts a <see cref="decimal"/> degree value into a <see cref="double"/> radians equivalent.</summary>
		/// <param name="degrees">The degrees to convert to radians.</param>
		/// <returns>The number of radians equivalent to the supplied degree value.</returns>
		public static double DegreesToRadians( decimal degrees ) => (double)degrees * (Math.PI / 180);

		/// <summary>Convert a <see cref="double"/> radian value to a <see cref="decimal"/> degree equivalent.</summary>
		/// <param name="radians">The radians to convert to degrees.</param>
		/// <returns>The number of degrees that is equivalent to the supplied radian value.</returns>
		public static decimal RadiansToDegrees( double radians ) => (decimal)(radians * (180 / Math.PI));

		/// <summary>Endeavours to construct an array of points on a line between provided starting and ending points.</summary>
		/// <param name="start">A <see cref="Point2"/> object specifying where to start.</param>
		/// <param name="endPoint">A <see cref="Point2"/> object specifying where to end.</param>
		public static Point2[] Line( Point2 start, Point2 endPoint )
		{
			// Length is the longest distance, either on the X or Y axis...
			int length = Math.Max( Math.Abs( endPoint.Y - start.Y ), Math.Abs( endPoint.X - start.X ) );

			List<Point2> line = new() { start };
			decimal xInc = 0.0M, yInc;

			// establish the value of the incrementors:
			switch ( endPoint.X.CompareTo( start.X ) )
			{
				case 0: // Straight vertical line:
					yInc = endPoint.Y.CompareTo( start.Y ) switch 
					{
						<0 => 0 - ((start.Y - endPoint.Y) / (decimal)length),
						>0 => (endPoint.Y - start.Y) / (decimal)length,
					_ => 0M,
					};
					break;
				case -1: // Line to the left:
					xInc = 0M - ((start.X - endPoint.X) / (decimal)length);
					goto default;
				case 1: // Line to the right:
					xInc = (endPoint.X - start.X) / (decimal)length;
					goto default;
				default: // Should only be reachable when the line deviates to the left or right...
					yInc = endPoint.Y.CompareTo( start.Y ) switch
					{
						< 0 => // Line goes upwards:
							0 - ((start.Y - endPoint.Y) / (decimal)length),
						> 0 => // Line goes downwards:
							(endPoint.Y - start.Y) / (decimal)length,
						_ => 0M,
					};
					break;
			}

			decimal x = start.X, y = start.Y;
			for ( int i = 0; i < length; i++ )
			{
				x += xInc; y += yInc;
				Point2 p = new ( (int)Math.Round( x ), (int)Math.Round( y ) );
				if ( !line.Contains( p ) ) line.Add( p );
			}

			return line.ToArray();
		}

		/// <summary>Endeavours to create an array of points lying along the circumference of a circle defined by a supplied 
		/// center point (<paramref name="origin"/>), a <paramref name="radius"/> and, optionally, an arc starting and ending 
		/// point (in degrees).</summary>
		/// <param name="origin">A <see cref="Point2"/> object representing the center of the circle.</param>
		/// <param name="radius">An <see cref="int"/> value indicating the radius of the circle.</param>
		/// <param name="degreesStart">Where to start calculating (in degrees).</param>
		/// <param name="degreesEnd">Where to end calculating (in degrees).</param>
		/// <remarks><b>Note</b>: 0 degrees is at <i>6-o'clock</i>, 90 is at 3, 180 at 12 and 270 at 9.</remarks>
		public static Point2[] Circle( Point2 origin, int radius, int degreesStart = 0, int degreesEnd = 360 ) =>
			Circle( origin, radius, DegreesToRadians(degreesStart), DegreesToRadians(degreesEnd) );

		/// <summary>Endeavours to create an array of points around the origin (0, 0) that lie along the circumference of a circle 
		/// defined by a radius and, optionally, an arc starting and ending point (in degrees).</summary>
		/// <param name="radius">An Int32 value indicating the radius of the circle.</param>
		/// <param name="degreesStart">Where to start calculating (in degrees).</param>
		/// <param name="degreesEnd">Where to end calculating (in degrees).</param>
		public static Point2[] Circle( int radius, int degreesStart = 0, int degreesEnd = 360 ) =>
			Circle( radius, DegreesToRadians( degreesStart ), DegreesToRadians( degreesEnd ) );

		/// <summary>Endeavours to create an array of points lying along the circumference of a circle defined by a supplied 
		/// center point, a radius and, optionally, an arc starting and ending point (in degrees).</summary>
		/// <param name="origin">A <see cref="Point2"/> object representing the center of the circle.</param>
		/// <param name="radius">An <seealso cref="int"/> value indicating the radius of the circle.</param>
		/// <param name="radiansStart">Where to start calculating (in radians).</param>
		/// <param name="radiansEnd">Where to end calculating (in radians).</param>
		public static Point2[] Circle( Point2 origin, int radius, double? radiansStart = null, double? radiansEnd = null )
		{
			double start = (double)(radiansStart is null ? 0.0 : radiansStart), 
					end = (double)(radiansEnd is null ? Pi( 2 ) : radiansEnd);

			origin ??= new (0,0);
			List<Point2> circle = new();
			for (double radians = start; radians <= end; radians += Math.PI/180 )
			{
				Point2 point = new Point2( (int)Math.Round( radius * Math.Sin( radians ) ), (int)Math.Round( radius * Math.Cos( radians ) ) ) + origin;
				if ( !circle.Contains(point ) ) circle.Add(point );
			}
			return circle.ToArray();
		}

		/// <summary>Endeavours to create an array of points around the origin (0, 0) that lie along the circumference of a circle 
		/// defined by a radius and, optionally, an arc starting and ending point (in radians).</summary>
		/// <param name="radius">An <seealso cref="int"/> value indicating the radius of the circle.</param>
		/// <param name="radiansStart">Where to start calculating (in radians).</param>
		/// <param name="radiansEnd">Where to end calculating (in radians).</param>
		public static Point2[] Circle( int radius, double? radiansStart = null, double? radiansEnd = null ) =>
			Circle( new(0,0), radius, radiansStart, radiansEnd );

		/// <summary>Creates a new <see cref="Point2"/> object that contains the smallest X and Y values from the provided sets of points.</summary>
		/// <returns>A <see cref="Point2"/> object whose X and Y coordinate are the lowest values contained in the set of provided objects.</returns>
		public static Point2 Min( Point2 one, params Point2[] two ) => one?.Min( two );

		public static Point2 Minimum( Point2 one, params Point2[] two ) => Min( one, two );

		/// <summary>Creates a new <see cref="Point2"/> object that contains the largest X and Y values from the provided set of points.</summary>
		/// <returns>A <see cref="Point2"/> object whose X and Y coordinate are the highest values contained in the provided set of objects.</returns>
		public static Point2 Max( Point2 one, params Point2[] two ) => one?.Max( two );

		public static Point2 Maximum( Point2 one, params Point2[] two ) => Max( one, two );

		public static Point[] Converter( IEnumerable<Point2> source )
		{
			List<Point> _items = new();
			foreach ( var p in source ) _items.Add( p );
			return _items.ToArray();
		}

		/// <summary>Produces multiples of PI.</summary>
		public static double Pi( double multiplier ) => Math.PI * multiplier;

		/// <summary>Produces multiples of PI from any numeric multiplier.</summary>
		public static double Pi<T>( T multiplier ) where T : INumber<T> => Pi( Convert.ToDouble( multiplier ) );
		#endregion
		#endregion
	}

	public class Point3D
	{
		#region Properties
		/// <summary>Used to specify which axis a rotation operation is to be performed around.</summary>
		/// <remarks>Defined as a "Flags" enum so multiple axes can be defined in a single value.</remarks>
		[Flags] public enum Axis : byte { X = 0x01, Y = 0x02, Z = 0x04 }
		#endregion

		#region Constructors
		/// <summary>Creates a new Point3D object with values of ( 0,0 ).</summary>
		public Point3D() => Set( 0, 0, 0 );

		/// <summary>Creates a new Point3D object with values specified in "x" and "y".</summary>
		public Point3D(int x, int y, int z) => Set( x, y, z );

		/// <summary>Creates a new Point3D object from an existing Point object plus a Z co-ordinate.</summary>
		public Point3D(Point p, int z) => Set( p.X, p.Y, z );

		/// <summary>Creates a new Point3D object using values from an existing Point2 object plus a Z co-ordinate.</summary>
		public Point3D(Point2 p, int z) => Set( p.X, p.Y, z );

		/// <summary>Creates a new Point3D object using values from an existing Point3D object.</summary>
		public Point3D(Point3D p) => Set( p.X, p.Y, p.Z );

		/// <summary>Endeavours to create a Point3D object from a supplied string.</summary>
		/// <exception cref="InvalidOperationException">Thrown if the passed string can't be parsed.</exception>
		public Point3D( string value ) => Parse( value );
		#endregion

		#region Accessors
		public int X { get; set; } = 0;

		public int Y { get; set; } = 0;

		public int Z { get; set; } = 0;
		#endregion

		#region Operators
		public static implicit operator Point(Point3D data) => new Point( data.X, data.Y );
		public static implicit operator Point2(Point3D data) => new Point2( data.X, data.Y );
		public static implicit operator Point3D(Point data) => new Point3D( data.X, data.Y, 0 );
		public static implicit operator Point3D(Point2 data) => new Point3D( data.X, data.Y, 0 );

		/// <summary>Creates a new Point2 object whose X and Y values represent the sum of the values from "left" and "right".</summary>
		public static Point3D operator +(Point3D left, Point3D right) =>
			new( left.X + right.X, left.Y + right.Y, left.Z + right.Z );

		/// <summary>Creates a new Point3D object whose X and Y values represent the result of subtracting the values of "right" from "left".</summary>
		public static Point3D operator -(Point3D left, Point3D right) =>
			new( left.X - right.X, left.Y - right.Y, left.Z + right.Z );

		/// <summary>Creates a new Point3D object whose X and Y values represent the sum of the values from "left" and "right".</summary>
		public static Point3D operator +(Point left, Point3D right) =>
			new( left.X + right.X, left.Y + right.Y, right.Z );

		/// <summary>Creates a new Point3D object whose X and Y values represent the result of subtracting the values of "right" from "left".</summary>
		public static Point3D operator -(Point left, Point3D right) =>
			new( left.X - right.X, left.Y - right.Y, 0 - right.Z );

		/// <summary>Creates a new Point3D object whose X and Y values represent the sum of the values from "left" and "right".</summary>
		public static Point3D operator +(Point3D left, Point right) =>
			new( left.X + right.X, left.Y + right.Y, left.Z );

		/// <summary>Creates a new Point3D object whose X and Y values represent the result of subtracting the values of "right" from "left".</summary>
		public static Point3D operator -(Point3D left, Point right) =>
			new Point3D( left.X - right.X, left.Y - right.Y, left.Z );

		/// <summary>TRUE if both co-ordinates of "left" are less than their counterparts in "right".</summary>
		public static bool operator <(Point3D left, Point3D right) =>
			(left.X < right.X) && (left.Y < right.Y) && (left.Z < right.Z);

		/// <summary>TRUE if both co-ordinates of "left" are greater than their counterparts in "right".</summary>
		public static bool operator >(Point3D left, Point3D right) =>
			(left.X > right.X) && (left.Y > right.Y) && (left.Z > right.Z);

		/// <summary>TRUE if both co-ordinates of "left" are less than or equal-to their counterparts in "right".</summary>
		public static bool operator <=(Point3D left, Point3D right) =>
			(left.X <= right.X) && (left.Y <= right.Y) && (left.Z <= right.Z);

		/// <summary>TRUE if both co-ordinates of "left" are greater than or equal-to their counterparts in "right".</summary>
		public static bool operator >=(Point3D left, Point3D right) =>
			(left.X >= right.X) && (left.Y >= right.Y) && (left.Z >= right.Z);

		public static bool operator ==(Point3D left, Point3D right) =>
			left.Equals( right );

		public static bool operator ==(Point3D left, Point right) =>
			left.Equals( right );

		public static bool operator ==(Point left, Point3D right) =>
			right.Equals( left );

		public static bool operator !=(Point3D left, Point right) =>
			!left.Equals( right );

		public static bool operator !=(Point3D left, Point3D right) =>
			!left.Equals( right );

		public static bool operator !=(Point left, Point3D right) =>
			!right.Equals( left );

		/// <summary>Returns a new Point3D object whose X value is the sum of "left.X" and "X" and whose Y value is "left.Y".</summary>
		public static Point3D operator +(Point3D left, int X) =>
			new( left.X + X, left.Y, left.Z );

		/// <summary>Returns a new Point3D object whose X value is the sum of "left.X" and "X" and whose Y value is "left.Y".</summary>
		public static Point3D operator +(int X, Point3D left) =>
			left + X;

		/// <summary>Returns a new Point3D object whose X value is the result of subtracting "X" from "left.X" and whose Y value  is "left.Y".</summary>
		public static Point3D operator -(Point3D left, int X) =>
			new( left.X - X, left.Y, left.Z );

		/// <summary>Returns a new Point3D object whose X value is the result of subtracting "left.X" from "X" and whose Y value  is "left.Y".</summary>
		public static Point3D operator -(int X, Point3D left) =>
			new( X - left.X, left.Y, left.Z );
		#endregion

		#region Methods
		public Point3D Add( int x, int y, int z )
		{
			this.X += x;
			this.Y += y;
			this.Z += z;
			return new( this );
		}

		public Point3D Subtract( int x, int y, int z)
		{
			this.X -= x;
			this.Y -= y;
			this.Z -= z;
			return new( this );
		}

		public Point3D Add( Point3D translate ) => Add( translate.X, translate.Y, translate.Z );

		public Point3D Add( Point2 translate, int z = 0 ) => Add( translate.X, translate.Y, z );

		public Point3D Add( Point translate, int z = 0 ) => Add( translate.X, translate.Y, z );

		public Point3D Subtract( Point3D translate ) => Subtract( translate.X, translate.Y, translate.Z );

		public Point3D Subtract( Point2 translate, int z = 0 ) => Subtract( translate.X, translate.Y, z );

		public Point3D Subtract( Point translate, int z = 0 ) => Subtract( translate.X, translate.Y, z );

		public Point3D Min( Point3D p ) =>
			new( Math.Min( p.X, this.X ), Math.Min( p.Y, this.Y ), Math.Min( p.Z, this.Z ) );

		public Point3D Max( Point3D p ) =>
			new( Math.Max( p.X, this.X ), Math.Max( p.Y, this.Y ), Math.Max( p.Z, this.Z ) );

		/// <summary>Returns a new <see cref="Point3D"/> object that is the result of rotating this point around (0,0), the X/Y origin.</summary>
		/// <param name="radians">The number of radians to rotate. Negative values rotate to the left, positive rotate to the right.</param>
		/// <param name="axis">Specifies which axis to rotate the point around. Multiple axes can be specified.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the Radians value evaluates to NaN, NegativeInfinity or PositiveInfinity.</exception>
		public Point3D Rotate( double radians, Axis axis )
		{
			if (double.IsNaN( radians ) || double.IsInfinity( radians ))
				throw new ArgumentOutOfRangeException( nameof( radians ) );

			radians %= (radians < 0) ? -360 : 360;
			if (radians == 0.0) return new( this ); // if the Angle is 0, there's nothing to do.

			Point3D result = new();
			double cos = Math.Cos( radians ), sin = Math.Sin( radians );
			if (axis.HasFlag( Axis.X )) // X and Y coordinates rotate
			{
				result.X = (int)((double)(this.X * cos) - (double)(this.Y * sin));
				result.Y = (int)((double)(this.Y * cos) + (double)(this.X * sin));
			}

			if (axis.HasFlag( Axis.Y )) // Y and Z coordinates rotate
			{
				result.Z = (int)((double)(this.Z * cos) - (double)(this.Y * sin));
				result.Y = (int)((double)(this.Y * cos) + (double)(this.Z * sin));
			}

			if (axis.HasFlag( Axis.Z )) // X and Z coordinates rotate
			{
				result.X = (int)((double)(this.X * cos) - (double)(this.Z * sin));
				result.Z = (int)((double)(this.Z * cos) + (double)(this.X * sin));
			}
			return result;
		}

		/// <summary>Returns a new <see cref="Point3D"/> object that is the result of rotating this point around (0,0), the X/Y origin.</summary>
		/// <param name="degrees">The number of degrees to rotate. Negative values rotate to the left, positive rotate to the right.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the Radians value evaluates to NaN, NegativeInfinity or PositiveInfinity.</exception>
		public Point3D Rotate(decimal degrees, Axis axis) =>
			Rotate( Point2.DegreesToRadians( degrees ), axis );

		/// <summary>Returns a new <see cref="Point3D"/> object that is the result of rotating this point around the provided center of rotation.</summary>
		/// <param name="degrees">The number of degrees to rotate. Negative values rotate to the left, positive rotate to the right.</param>
		/// <param name="around">A Point2 object specifying the center of rotation.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the Radians value evaluates to NaN, NegativeInfinity or PositiveInfinity.</exception>
		public Point3D Rotate(decimal degrees, Point3D around, Axis axis = Axis.X) =>
			Rotate( Point2.DegreesToRadians( degrees ), around, axis );

		/// <summary>Returns a new <seealso cref="Point3D"/> object that is the result of rotating this point around the provided center of rotation.</summary>
		/// <param name="radians">The number of radians to rotate. Negative values rotate to the left, positive rotate to the right.</param>
		/// <param name="around">A <see cref="Point3D"/> object specifying the center of rotation.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the Radians value evaluates to NaN, NegativeInfinity or PositiveInfinity.</exception>
		public Point3D Rotate(double radians, Point3D around, Axis axis) =>
			// Translate this point by the "around" point (make rotation point 0,0), perform the rotation about the origin, then translate it back.
			(this - around).Rotate( radians, axis ) + around;

		/// <summary>Compares this object to a provided <see cref="Point3D"/> object and returns <b>TRUE</b> if their X. Y and Z values all match.</summary>
		public bool Equals(Point3D p) => (p.X == this.X) && (p.Y == this.Y) && (p.Z == this.Z);

		/// <summary>Compares this object to a provided <seealso cref="Point"/> object and returns <b>TRUE</b> if their X and Y values match.</summary>
		public bool Equals(Point p) => (this.X == p.X) && (this.Y == p.Y);

		/// <summary>Compares this object to another provided <seealso cref="Point2"/> object and returns <b>TRUE</b> if their X and Y values match.</summary>
		public bool Equals(Point2 p) => (this.X == p.X) && (this.Y == p.Y);

		/// <summary>Compares this object to provided X, Y and Z values and returns <b>TRUE</b> if they match.</summary>
		public bool Equals(int x, int y, int z) => (x == this.X) && (y == this.Y) && (z == this.Z);

		/// <summary>Compares a provided int value against this object's X value and returns <b>TRUE</b> if they match.</summary>
		public bool Equals(int x) => (x == X);

		public void Set( int x, int y, int z) { this.X = x; this.Y = y; this.Z = z; }

		// required to support equivalency operator overrides in the "Operators" declarations.
		public override bool Equals(object obj) => base.Equals( obj );

		// required to support equivalency operator overrides in the "Operators" declarations.
		public override int GetHashCode() => base.GetHashCode();

		/// <summary>Endeavours to create a new <see cref="Point3D"/> class from a supplied string.</summary>
		/// <param name="source">The string to try and parse into this object.</param>
		/// <exception cref="InvalidOperationException">The passed string could not be successfully parsed.</exception>
		/// <remarks>
		/// This method works by removing all non-numeric characters (except valid separation: ',', ';', 'x' or 'X' and negation '-' indicators),<br/>
		/// then it identifies integers segregated by a separation characters and imports them, if possible, as X and Y coordinates respectively.
		/// </remarks>
		public static Point3D Parse( string source )
		{
			if ( !string.IsNullOrWhiteSpace( source ) )
			{
				source = Regex.Replace( source, @"[^-\d,;]", "", RegexOptions.None );
				Match m = Regex.Match( source, @"^(?<x>-?[\d]+)[;,](?<y>-?[\d]+)[;,](?<z>-?[\d]+)$", RegexOptions.None );
				if ( m.Success )
					return new Point3D(
						int.Parse( m.Groups[ "x" ].Value ),
						int.Parse( m.Groups[ "y" ].Value ),
						int.Parse( m.Groups[ "z" ].Value )
					);
			}
			throw InvalidPoint( source );
		}

		/// <summary>Endeavours to parse a string into a valid <see cref="Point3D"/> object.</summary>
		/// <param name="source">The string to parse.</param>
		/// <param name="value">An object to capture the result of the parsing operation. Will be <i>null</i> if the parse attempt fails.</param>
		/// <returns><b>TRUE</b> if the operation succeeded, otherwise <b>FALSE</b>.</returns>
		public static bool TryParse( string source, out Point3D value )
		{
			try { value = Parse( source ); }
			catch { value = null; }

			return value is not null;
		}

		public override string ToString() => $"( {this.X}, {this.Y}, {this.Z} )";

		#region Static Methods
		private static InvalidOperationException InvalidPoint(string value) =>
			new( $"The supplied value (\"{value}\") could not be parsed into a valid Point2 object." );

		/// <summary>Tests a provided string to determine if it corresponds to a valid <see cref="Point3D"/> syntax.</summary>
		/// <param name="value">A string to test.</param>
		/// <returns><b>TRUE</b> if the passed string can be parsed int a <see cref="Point3D"/> instance.</returns>
		public static bool IsValid(string value) => TryParse( value, out _ );
		#endregion
		#endregion
	}

	/// <summary>Like a rectangle, but only defines the four corners.</summary>
	public class Corners
	{
		#region Properties
		protected Point2 _topLeft = new Point2();
		protected Point2 _bottomRight = new Point2();
		#endregion

		#region Constructors
		public Corners(Point2 topLeft, Point2 bottomRight) =>
			Initialize( topLeft, bottomRight );

		public Corners( Rectangle rectangle ) =>
			Initialize( rectangle.Location, (Point2)rectangle.Location + rectangle.Size );

		public Corners( Point2 topLeft, Size size ) =>
			Initialize( topLeft, topLeft + size );

		public Corners(Size size) =>
			Initialize( new Point2( 0, 0 ), size );
		#endregion

		#region Accessors
		public int Left
		{
			get => _topLeft.X;
			set => _topLeft.X = (value < _bottomRight.X) ? value : _topLeft.X;
		}

		public int Top
		{
			get => _topLeft.Y;
			set => _topLeft.Y = (value <= _bottomRight.Y) ? value : _topLeft.Y;
		}

		public int Right
		{
			get => _bottomRight.X;
			set => _bottomRight.X = (value > _topLeft.X) ? value : _bottomRight.X;
		}

		public int Bottom
		{
			get => _bottomRight.Y;
			set => _bottomRight.Y = (value >= _topLeft.Y) ? value : _bottomRight.Y;
		}

		public Point2 Location
		{
			get => _topLeft;
			set => Initialize( value, _bottomRight);
		}

		public Point2 End
		{
			get => _bottomRight;
			set => Initialize( _topLeft, value );
		}

		public Point2 TopLeft => _topLeft;

		public Point2 TopRight => new Point2( _bottomRight.X, _topLeft.Y );

		public Point2 BottomLeft => new Point2( _topLeft.X, _bottomRight.Y );

		public Point2 BottomRight => _bottomRight;

		public Size Size => new Size( _bottomRight.X - _topLeft.X + 1, _bottomRight.Y - _topLeft.Y + 1 );
		#endregion

		#region Operators
		public static implicit operator Rectangle(Corners data) => data.AsRectangle();
		public static implicit operator Corners(Rectangle data) => new Corners( data );
		public static implicit operator Size(Corners data) => data.Size;
		public static implicit operator Corners(Size data) => new Corners( new Point2(0,0), data);

		public static bool operator ==(Corners left, Corners right)
		{
			if (left is null) return right is null;
			if (right is null) return false;
			return (left._topLeft == right._topLeft) && (left._bottomRight == right._bottomRight);
		}

		public static bool operator !=(Corners left, Corners right) => !(left == right);

		public static bool operator ==(Corners left, Size right)
		{
			if (left is null) return false;
			return (left.Size == right);
		}

		public static bool operator !=(Corners left, Size right) => !(left == right);

		public static bool operator ==(Corners left, Rectangle right)
		{
			if (left is null) return false;
			return (left.Location == right.Location) && (left.Size == right.Size);
		}

		public static bool operator !=(Corners left, Rectangle right) => !(left == right);
		#endregion

		#region Methods
		protected void Initialize( Point2 one, Point2 two )
		{
			_topLeft = Point2.Min(one, two);
			_bottomRight = Point2.Max( one, two );
		}

		public override string ToString() =>
			"[ " + _topLeft.ToString() + " / " + _bottomRight.ToString() + " ]";

		public override bool Equals(object obj) =>
			base.Equals( obj );

		public override int GetHashCode() =>
			base.GetHashCode();

		public Rectangle AsRectangle() =>
			new Rectangle(this.Location,this.Size);

		public static Corners Instantiate(Rectangle source) => new Corners( source );

		public static Corners Instantiate(Point2 location, Size size) => new Corners( location, size );
		#endregion
	}
}
