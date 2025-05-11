using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using NetXpertExtensions;

namespace NetXpertCodeLibrary
{
	[Serializable]
	public class Point2 : ISerializable
	{
		#region Properties
		private static readonly Regex pattern = new Regex( @"[([{]?[\s]*(?<x>-?[0-9]+)[\s]*[,Xx;][\s]*(?<y>-?[0-9]+)[\s]*[)\]}]?", RegexOptions.None );

		public struct Point2CompareResult
		{
			#region Properties
			[Flags] public enum CompareResult { 
				None					= 0x00, // 00000000
				LessThan				= 0x01, // 00000001
				GreaterThan				= 0x02, // 00000010
				EqualTo					= 0x04, // 00000100
				LessThanOrEqualTo		= 0x05, // 00000101
				GreaterThanOrEqualTo	= 0x06, // 00000110
				Error					= 0xff  // 11111111
			}
			/// <summary>Holds the result of compared X values.</summary>
			public CompareResult X;
			/// <summary>Holds the results of compared Y values.</summary>
			public CompareResult Y;
			#endregion

			#region Constructors
			public Point2CompareResult(
				CompareResult x = CompareResult.None,
				CompareResult y = CompareResult.None )
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
				Compare( left, right );
			}

			public Point2CompareResult( Point left, Point2 right )
			{
				X = Y = CompareResult.None;
				Compare( left, right );
			}
			#endregion

			#region Accessors
			/// <summary>TRUE if either comparison is reporting an error.</summary>
			public bool Error => (X == CompareResult.Error) || (Y == CompareResult.Error);
			#endregion

			#region Methods
			public void Clear() =>
				X = Y = CompareResult.None;

			public Point2CompareResult Compare(Point2 left, Point2 right)
			{
				Clear();
				if ((left is null) || (right is null))
					X = Y = CompareResult.Error;
				else
				{
					if (left.X < right.X) this.X |= CompareResult.LessThan;
					if (left.X > right.X) this.X |= CompareResult.GreaterThan;
					if (left.X == right.X) this.X |= CompareResult.EqualTo;

					if (left.Y < right.Y) this.Y |= CompareResult.LessThan;
					if (left.Y > right.Y) this.Y |= CompareResult.GreaterThan;
					if (left.Y == right.Y) this.Y |= CompareResult.EqualTo;
				}
				return this;
			}

			public static Point2CompareResult ComparePoints(Point2 left, Point2 right) =>
				new Point2CompareResult( left, right );
			#endregion
		}

		public enum PointCompareOptions {
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
		}
		#endregion

		#region Constructors
		/// <summary>Creates a new Point2 object with values specified in "x" and "y".</summary>
		public Point2( int x = 0, int y = 0 ) => Set( x, y );

		/// <summary>Creates a new Point2 object from an existing Point object.</summary>
		public Point2( Point p ) => Set( p.X, p.Y );

		/// <summary>Creata a new Point2 object using values from an existing Point2 object.</summary>
		public Point2( Point2 p ) => Set( p.X, p.Y );

		/// <summary>Attempts to initialize this class with a string source.</summary>
		/// <exception cref="InvalidOperationException">Thrown if the passed string cannot be parsed into a valid Point2 object.</exception>
		public Point2( string value )
		{
			if (!IsValid( value ) || !TryParse(value))
				throw InvalidPoint( value );
		}

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
		public static implicit operator Point2(Point data) => new Point2( data );
		public static implicit operator Point2(string data) => new Point2( data );
		public static implicit operator Point2(int data) => new Point2( data, 0 );
		public static implicit operator Point2(Size s) => new Point2( s.Width, s.Height );

		public static implicit operator Point(Point2 data) => new Point(data.X, data.Y);
		public static implicit operator string(Point2 data) => data.ToString();
		public static implicit operator int(Point2 data) => data.X;
		public static implicit operator Size(Point2 data) => new Size( data.X, data.Y );

		/// <summary>Creates a new Point2 object whose X and Y values represent the sum of the values from "left" and "right".</summary>
		public static Point2 operator +(Point2 left, Point2 right) =>
			new Point2( left.X + right.X, left.Y + right.Y );

		/// <summary>Creates a new Point2 object whose X and Y values represent the result of subtracting the values of "right" from "left".</summary>
		public static Point2 operator -(Point2 left, Point2 right) =>
			new Point2( left.X - right.X, left.Y - right.Y );

		/// <summary>Creates a new Point2 object whose X and Y values represent the sum of the values from "left" and "right".</summary>
		public static Point2 operator +(Point left, Point2 right) =>
			new Point2( left.X + right.X, left.Y + right.Y );

		/// <summary>Creates a new Point2 object whose X and Y values represent the result of subtracting the values of "right" from "left".</summary>
		public static Point2 operator -(Point left, Point2 right) =>
			new Point2( left.X - right.X, left.Y - right.Y );

		/// <summary>Creates a new Point2 object whose X and Y values represent the sum of the values from "left" and "right".</summary>
		public static Point2 operator +(Point2 left, Point right) =>
			new Point2( left.X + right.X, left.Y + right.Y );

		/// <summary>Creates a new Point2 object whose X and Y values represent the result of subtracting the values of "right" from "left".</summary>
		public static Point2 operator -(Point2 left, Point right) =>
			new Point2( left.X - right.X, left.Y - right.Y );

		// "Size" is not zero based, so we have to subtract 1 from it's height and width when adding it to a point.
		public static Point2 operator +(Point2 left, Size right) =>
			new Point2( left.X + (right.Width - 1), left.Y + (right.Height - 1) );

		/// <summary>
		/// Creates a new Point2 object whose X and Y values represent the result of subtracting the right's Width from the left's X
		/// and the right's Height from the left's Y.
		/// </summary>
		public static Point2 operator -(Point2 left, Size right) =>
			new Point2( left.X - right.Width, left.Y - right.Height );

		/// <summary>TRUE if both co-ordinates of "left" are less than their counterparts in "right".</summary>
		public static bool operator <(Point2 left, Point2 right)
		{
			if (left is null) return right is null;
			if (right is null) return false;
			return left.CompareTo( right, PointCompareOptions.BothLessThan );
		}

		/// <summary>TRUE if both co-ordinates of "left" are greater than their counterparts in "right".</summary>
		public static bool operator >(Point2 left, Point2 right)
		{
			if (left is null) return right is null;
			if (right is null) return false;
			return left.CompareTo( right, PointCompareOptions.BothGreaterThan );
		}

		/// <summary>TRUE if both co-ordinates of "left" are less than or equal-to their counterparts in "right".</summary>
		public static bool operator <=(Point2 left, Point2 right)
		{
			if (left is null) return right is null;
			if (right is null) return false;
			return left.CompareTo( right, PointCompareOptions.BothLessThanOrEqualTo );
		}

		/// <summary>TRUE if both co-ordinates of "left" are greater than or equal-to their counterparts in "right".</summary>
		public static bool operator >=(Point2 left, Point2 right)
		{
			if (left is null) return right is null;
			if (right is null) return false;
			return left.CompareTo( right, PointCompareOptions.BothGreaterThanOrEqualTo );
		}

		/// <summary>TRUE if the X and Y coordinates of the left object match those of the right.</summary>
		public static bool operator ==(Point2 left, Point2 right)
		{
			if (left is null) return right is null;
			if (right is null) return false;
			return left.CompareTo( right, PointCompareOptions.BothEqualTo );
		}

		/// <summary>TRUE if the X and Y coordinates of the left object match those of the right.</summary>
		public static bool operator ==(Point2 left, Point right) =>
			left == (Point2)right;

		/// <summary>TRUE if the X and Y coordinates of the left object match those of the right.</summary>
		public static bool operator ==(Point left, Point2 right) =>
			right == (Point2)left;

		/// <summary>TRUE if either of the X and Y coordinates of the left object don't match those of the right.</summary>
		public static bool operator !=(Point2 left, Point right) =>
			!(left == (Point2)right);

		/// <summary>TRUE if either of the X and Y coordinates of the left object don't match those of the right.</summary>
		public static bool operator !=(Point2 left, Point2 right) =>
			!(left == (Point2)right);

		/// <summary>TRUE if either of the X and Y coordinates of the left object don't match those of the right.</summary>
		public static bool operator !=(Point left, Point2 right) =>
			!(right == (Point2)left);

		/// <summary>Returns a new Point2 object whose X value is the sum of "left.X" and "X" and whose Y value is "left.Y".</summary>
		public static Point2 operator +(Point2 left, int X) =>
			new Point2( left.X + X, left.Y );

		/// <summary>Returns a new Point2 object whose X value is the sum of "left.X" and "X" and whose Y value is "left.Y".</summary>
		public static Point2 operator +(int X, Point2 left) =>
			left + X;

		/// <summary>Returns a new Point2 object whose X value is the result of subtracting "X" from "left.X" and whose Y value  is "left.Y".</summary>
		public static Point2 operator -(Point2 left, int X) =>
			new Point2( left.X - X, left.Y );

		/// <summary>Returns a new Point2 object whose X value is the result of subtracting "left.X" from "X" and whose Y value  is "left.Y".</summary>
		public static Point2 operator -(int X, Point2 left) =>
			new Point2( X - left.X, left.Y );

		public static Point2 operator --(Point2 value) => value - 1;

		public static Point2 operator ++(Point2 value) => value + 1;
		#endregion

		#region Methods
		/// <summary>Adds the X and Y values of the supplied Point2 object to this object's values.</summary>
		/// <param name="p">A Point2 object whose X and Y values are to be added.</param>
		/// <returns>This object, after it's values have been modified.</returns>
		/// <remarks>
		/// This method alters the values of the calling object! Do NOT use it if you don't want them changed,
		/// use the addition operator instead.
		/// </remarks>
		public Point2 Add(Point2 p)
		{
			this.Set( this.X + p.X, this.Y + p.Y );
			return this;
		}

		/// <summary>Subtracts the X and Y values of the supplied Point object from this object's values.</summary>
		/// <param name="p">A Point object whose X and Y values are to be subtracted.</param>
		/// <returns>This object, after it's values have been modified.</returns>
		/// <remarks>
		/// This method alters the values of the calling object! Do NOT use it if you don't want them changed,
		/// use the addition operator instead.
		/// </remarks>
		public Point2 Add(Point p) => this.Add( (Point2)p );

		/// <summary>Adds the supplied X and Y values to this object's values.</summary>
		/// <param name="x">An int value to add to this class's X value.</param>
		/// <param name="y">An int value to add to this class's Y value.</param>
		/// <returns>This object, after it's values have been modified.</returns>
		/// <remarks>
		/// This method alters the values of the calling object! Do NOT use it if you don't want them changed,
		/// use the addition operator instead.
		/// </remarks>
		public Point2 Add(int x, int y = 0) => this.Add( new Point2( x, y ) );

		/// <summary>Subtracts the X and Y values of the supplied Point2 object from this object's values.</summary>
		/// <param name="p">A Point2 object whose X and Y values are to be subtracted.</param>
		/// <returns>This object, after it's values have been modified.</returns>
		/// <remarks>
		/// This method alters the values of the calling object! Do NOT use it if you don't want them changed,
		/// use the subtraction operator instead.
		/// </remarks>
		public Point2 Subtract(Point2 p)
		{
			this.Set( this.X - p.X, this.Y - p.Y );
			return this;
		}

		/// <summary>Subtracts the X and Y values of the supplied Point object from this object's values.</summary>
		/// <param name="p">A Point2 object whose X and Y values are to be subtracted.</param>
		/// <returns>This object, after it's values have been modified.</returns>
		/// <remarks>
		/// This method alters the values of the calling object! Do NOT use it if you don't want them changed,
		/// use the subtraction operator instead.
		/// </remarks>
		public Point2 Subtract(Point p) => this.Subtract( (Point2)p );

		/// <summary>Subtracts the supplied X and Y values from this object's values.</summary>
		/// <param name="x">An int value to subtract from this class's X value.</param>
		/// <param name="y">An int value to subtract from this class's Y value.</param>
		/// <returns>This object, after it's values have been modified.</returns>
		/// <remarks>
		/// This method alters the values of the calling object! Do NOT use it if you don't want them changed, 
		/// use the subtraction operator instead.
		/// </remarks>
		public Point2 Subtract(int x, int y = 0) => this.Subtract( new Point2( x, y ) );

		/// <summary>Returns a new Point2 object that is the result of rotating this point around (0,0), the X/Y origin.</summary>
		/// <param name="radians">The number of radians to rotate. Negative values rotate to the left, positive rotate to the right.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the Radians value evaluates to NaN, NegativeInfinity or PositiveInfinity.</exception>
		public Point2 Rotate(double radians)
		{
			if (double.IsNaN( radians ) || double.IsInfinity( radians ))
				throw new ArgumentOutOfRangeException( "The specified radian value isn't valid." );
			
			if (radians == 0.0) return new Point2( this ); // if the Angle is 0, there's nothing to do.

			// Forces the radians value either into a positive or negative range of 2PI (no need to perform multiple meaningless rotations)
			radians = radians % (((radians < 0) ? -1 : 1) * (Math.PI * 2));

			Point2 result = new Point2();
			double cos = Math.Cos( radians ), sin = Math.Sin( radians );
			result.X = (int)((double)(this.X * cos) - (double)(this.Y * sin));
			result.Y = (int)((double)(this.Y * cos) + (double)(this.X * sin));
			return result;
		}

		/// <summary>Returns a new Point2 object that is the result of rotating this point around (0,0), the X/Y origin.</summary>
		/// <param name="degrees">The number of degrees to rotate. Negative values rotate to the left, positive rotate to the right.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the Radians value evaluates to NaN, NegativeInfinity or PositiveInfinity.</exception>
		public Point2 Rotate(decimal degrees) =>
			Rotate( DegreesToRadians( degrees ) );

		/// <summary>Returns a new Point2 object that is the result of rotating this point around the provided center of rotation.</summary>
		/// <param name="degrees">The number of degrees to rotate. Negative values rotate to the left, positive rotate to the right.</param>
		/// <param name="around">A Point2 object specifying the center of rotation.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the Radians value evaluates to NaN, NegativeInfinity or PositiveInfinity.</exception>
		public Point2 Rotate(decimal degrees, Point2 around) =>
			Rotate( DegreesToRadians( degrees ), around );

		/// <summary>Returns a new Point2 object that is the result of rotating this point around the provided center of rotation.</summary>
		/// <param name="radians">The number of radians to rotate. Negative values rotate to the left, positive rotate to the right.</param>
		/// <param name="around">A Point2 object specifying the center of rotation.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the Radians value evaluates to NaN, NegativeInfinity or PositiveInfinity.</exception>
		public Point2 Rotate(double radians, Point2 around) =>
			// Translate this point by the "around" point (make rotation point 0,0), perform the rotation about the origin, then translate it back.
			(this - around).Rotate( radians ) + around; 

		/// <summary>Creates a new Point2 object derived from the minimums of the provided Point2 X and Y values and those of this object.</summary>
		public Point2 Min( Point2 p ) =>
			new Point2( Math.Min( this.X, p.X ), Math.Min( this.Y, p.Y ) );

		/// <summary>Creates a new Point2 object derived from the minimum of the provided X value and this object's X value.</summary>
		public Point2 Min(int x) =>
			new Point2( Math.Min( x, this.X ), this.Y );

		/// <summary>Creates a new Point2 object derived from the minimums of the provided X and Y values and this object's values.</summary>
		public Point2 Min(int x, int y) =>
			new Point2( x, y ).Min( this );

		/// <summary>Creates a new Point2 object derived from the maximum of the provided X value and this object's X value.</summary>
		public Point2 Max(int x) =>
			new Point2( Math.Max( x, this.X ), this.Y );

		/// <summary>Creates a new Point2 object derived from the maximums of the provided X and Y values and this object's values.</summary>
		public Point2 Max(int x, int y) =>
			new Point2( x, y ).Max( this );

		/// <summary>Creates a new Point2 object derived from the maximums of the provided Point2 X and Y values and those of this object.</summary>
		public Point2 Max(Point2 p) =>
			new Point2( Math.Max( this.X, p.X ), Math.Max( this.Y, p.Y ) );

		/// <summary>Compares this object to a provided Point object and returns TRUE if their X and Y values match.</summary>
		public bool Equals(Point p) =>
			(this.X == p.X) && (this.Y == p.Y);

		/// <summary>Compares this object to another provided Point2 object and returns TRUE if their X and Y values match.</summary>
		public bool Equals(Point2 p) =>
			(this.X == p.X) && (this.Y == p.Y);

		/// <summary>Compares this object to provided X and Y values and returns TRUE if they match.</summary>
		public bool Equals(int x, int y) =>
			(x == X) && (y == Y);

		/// <summary>Compares this object to a provided string value and returns TRUE if the string is a valid Point2 value whose 
		/// values match those of this object.</summary>
		public bool Equals( string value )
		{
			// Uses the "TryParse" method to obviate having to use another try..catch block.
			Point2 convert = new();
			return convert.TryParse( value ) ? convert == this : false;	
		}

		/// <summary>Compares a provided int value against this object's X value and returns TRUE if they match.</summary>
		public bool Equals(int x) => (x == X);

		/// <summary>Override the default .Equals comparator to intercept comparisons to Point or Point2 objects.</summary>
		public override bool Equals( object o ) =>
			(o.GetType() == typeof( Point2 )) || (o.GetType() == typeof( Point )) ? this.Equals( (Point2)o ) : base.Equals( o );

		/// <summary>Assigns the provided values to the current object.</summary>
		public void Set( int x, int y = int.MinValue )
		{
			this.X = x;
			this.Y = (y == int.MinValue) ? this.Y : y; // allow "y" to be omitted, setting only X
		}

		/// <summary>Creates a new Rectangle class from this object and a supplied Size object.</summary>
		public Rectangle ToRectangle(Size s) => new( new Point( this.X, this.Y ), new Size( s.Width, s.Height ) );

		/// <summary>Endeavours to create a new Point2 class from a supplied string.</summary>
		/// <param name="source">The string to try and parse into this object.</param>
		/// <exception cref="InvalidOperationException">The passed string could not be successfully parse.</exception>
		public static Point2 Parse( string source )
		{
			if (pattern.IsMatch( source ))
			{
				Match m = pattern.Matches( source )[ 0 ];
				if (m.Success)
					return new Point2(
						int.Parse( m.Groups[ "x" ].Value ),
						int.Parse( m.Groups[ "y" ].Value )
						);
			}

			throw InvalidPoint( source );
		}

		/// <summary>Endeavours to parse a string into a valid Point2 object.</summary>
		/// <param name="source">The string to parse.</param>
		/// <returns>TRUE if the operation succeeded, otherwise FALSE.</returns>
		public bool TryParse(string source)
		{
			bool result = false; // fail by default
			try { Parse( source ); result = true; }
			catch { /* do nothing */ }

			return result;
		}

		/// <summary>Creates a string of the form "( {x}, {y} )" from the valus of this object.</summary>
		public override string ToString() => $"( X = {X}, Y = {Y} )";

		// Needed due to use of the "==" operator override
		public override int GetHashCode() => base.GetHashCode();

		/// <summary>Instantiates a Point2CompareResult struct using this object, and a provided Point2 object.</summary>
		/// <param name="p2">A Point2 object to compare against.</param>
		/// <returns>A Point2CompareResult value derived from this object and the provided one.</returns>
		public Point2CompareResult Compare(Point2 p2) => Point2CompareResult.ComparePoints( this, p2 );

		/// <summary>Compares the values of this Point2 object with those of a provided Point2 object according to the specified method.</summary>
		/// <param name="p2">The Point2 object to Compare.</param>
		/// <param name="how">A PointCompareOptions value indicating how the objects are to be compared.</param>
		/// <returns>TRUE if the relationship between this Point2 object and the provided one match according to the specified method of comparison.</returns>
		public bool CompareTo(Point2 p2, PointCompareOptions how)
		{
			if (p2 is null) return false;

			Point2CompareResult cr = Compare( p2 );
			return how switch
			{
				PointCompareOptions.BothEqualTo =>
					(cr.X == Point2CompareResult.CompareResult.EqualTo) && (cr.Y == Point2CompareResult.CompareResult.EqualTo),
				PointCompareOptions.BothGreaterThan =>
					(cr.X == Point2CompareResult.CompareResult.GreaterThan) && (cr.Y == Point2CompareResult.CompareResult.GreaterThan),
				PointCompareOptions.BothLessThan =>
					(cr.X == Point2CompareResult.CompareResult.LessThan) && (cr.Y < Point2CompareResult.CompareResult.LessThan),
				PointCompareOptions.BothGreaterThanOrEqualTo =>
					(cr.X == Point2CompareResult.CompareResult.GreaterThanOrEqualTo) && (cr.Y == Point2CompareResult.CompareResult.GreaterThanOrEqualTo),
				PointCompareOptions.BothLessThanOrEqualTo =>
					(cr.X == Point2CompareResult.CompareResult.LessThanOrEqualTo) && (cr.Y == Point2CompareResult.CompareResult.LessThanOrEqualTo),
				PointCompareOptions.EitherEqualTo =>
					(cr.X == Point2CompareResult.CompareResult.EqualTo) || (cr.Y == Point2CompareResult.CompareResult.EqualTo),
				PointCompareOptions.EitherGreaterThan =>
					(cr.X == Point2CompareResult.CompareResult.GreaterThan) || (cr.Y == Point2CompareResult.CompareResult.GreaterThan),
				PointCompareOptions.EitherLessThan =>
					(cr.X == Point2CompareResult.CompareResult.LessThan) || (cr.Y == Point2CompareResult.CompareResult.LessThan),
				PointCompareOptions.EitherGreaterThanOrEqualTo =>
					((cr.X & Point2CompareResult.CompareResult.GreaterThanOrEqualTo) != 0) || ((cr.Y & Point2CompareResult.CompareResult.GreaterThanOrEqualTo) != 0),
				PointCompareOptions.EitherLessThanOrEqualTo =>
					((cr.X & Point2CompareResult.CompareResult.LessThanOrEqualTo) != 0) || ((cr.Y & Point2CompareResult.CompareResult.LessThanOrEqualTo) != 0),
				_ => false
			};
			//switch( how )
			//{
			//	case PointCompareOptions.BothEqualTo:
			//		return (cr.X == Point2CompareResult.CompareResult.EqualTo) && (cr.Y == Point2CompareResult.CompareResult.EqualTo);
			//	case PointCompareOptions.BothGreaterThan:
			//		return (cr.X == Point2CompareResult.CompareResult.GreaterThan) && (cr.Y == Point2CompareResult.CompareResult.GreaterThan);
			//	case PointCompareOptions.BothLessThan:
			//		return (cr.X == Point2CompareResult.CompareResult.LessThan) && (cr.Y < Point2CompareResult.CompareResult.LessThan);
			//	case PointCompareOptions.BothGreaterThanOrEqualTo:
			//		return (cr.X == Point2CompareResult.CompareResult.GreaterThanOrEqualTo) &&
			//			   (cr.Y == Point2CompareResult.CompareResult.GreaterThanOrEqualTo);
			//	case PointCompareOptions.BothLessThanOrEqualTo:
			//		return (cr.X == Point2CompareResult.CompareResult.LessThanOrEqualTo) &&
			//			   (cr.Y == Point2CompareResult.CompareResult.LessThanOrEqualTo);

			//	case PointCompareOptions.EitherEqualTo:
			//		return (cr.X == Point2CompareResult.CompareResult.EqualTo) || (cr.Y == Point2CompareResult.CompareResult.EqualTo);
			//	case PointCompareOptions.EitherGreaterThan:
			//		return (cr.X == Point2CompareResult.CompareResult.GreaterThan) || (cr.Y == Point2CompareResult.CompareResult.GreaterThan);
			//	case PointCompareOptions.EitherLessThan:
			//		return (cr.X == Point2CompareResult.CompareResult.LessThan) || (cr.Y == Point2CompareResult.CompareResult.LessThan);
			//	case PointCompareOptions.EitherGreaterThanOrEqualTo:
			//		return ((cr.X & Point2CompareResult.CompareResult.GreaterThanOrEqualTo) != 0) ||
			//			   ((cr.Y & Point2CompareResult.CompareResult.GreaterThanOrEqualTo) != 0) ;
			//	case PointCompareOptions.EitherLessThanOrEqualTo:
			//		return ((cr.X & Point2CompareResult.CompareResult.LessThanOrEqualTo) != 0) ||
			//			   ((cr.Y & Point2CompareResult.CompareResult.LessThanOrEqualTo) != 0) ;
			//}

			//return false;
		}

		public void GetObjectData( SerializationInfo info, StreamingContext context )
		{
			info.AddValue( this.GetType().FullName + ".Point2.X", X );
			info.AddValue( this.GetType().FullName + ".Point2.Y", Y );
		}

		/// <summary>Reports on whether this Point2 object's coordinates fall within the ranges specified.</summary>
		/// <param name="xHigh">The upper bounds permitted to the X value.</param>
		/// <param name="yHigh">The upper bounds permitted to the Y value.</param>
		/// <param name="xLow">The lower bounds for the X value (default = 0).</param>
		/// <param name="yLow">The lower bounds for the Y value (default = 0).</param>
		/// <param name="includeBoundaries">Specifies the <seealso cref="NetXpertExtensions.Classes.Range.BoundaryRule"/> mechanism to employ.</param>
		/// <returns>TRUE if this object's coordinates lie within the bounds specified.</returns>
		public bool InRange( int xHigh, int yHigh, int xLow = 0, int yLow = 0, NetXpertExtensions.Classes.Range.BoundaryRule includeBoundaries = NetXpertExtensions.Classes.Range.BoundaryRule.Inclusive ) =>
			this.X.InRange( xHigh, xLow, includeBoundaries ) && this.Y.InRange( yHigh, yLow, includeBoundaries );

		#region Static Methods
		private static InvalidOperationException InvalidPoint(string value) =>
			new InvalidOperationException( "The supplied value (\"" + value + "\") could not be parsed into a valid Point2 object." );

		/// <summary>Tests a provided string to determine if it corresponds to a valid Point2 syntax.</summary>
		/// <param name="value">A string to test.</param>
		/// <returns>TRUE if the passed string is a valid Point2 syntax.</returns>
		public static bool IsValid(string value) =>
			pattern.IsMatch( value );

		/// <summary>Convert a decimal degree value into a double radians equivalent.</summary>
		/// <param name="degrees">The degrees to convert to radians.</param>
		/// <returns>The number of radians equivalent to the supplied degree value.</returns>
		public static double DegreesToRadians( decimal degrees ) =>
			(double)degrees * (Math.PI / 180);

		/// <summary>Convert a double radian value to a decimal degree equivalent.</summary>
		/// <param name="radians">The radians to convert to degrees.</param>
		/// <returns>The number of degrees that is equivalent to the supplied radian value.</returns>
		public static decimal RadiansToDegrees(double radians) =>
			(decimal)(radians * (180 / Math.PI));

		/// <summary>Endeavours to construct an array of points on a line between a starting and ending point.</summary>
		/// <param name="start">A Point2 object specifying where to start.</param>
		/// <param name="end">A Point2 object specifying where to end.</param>
		public static Point2[] Line( Point2 start, Point2 endPoint )
		{
			// Length is the longest distance, either on the X or Y axis...
			int length = Math.Max( Math.Abs( endPoint.Y - start.Y ), Math.Abs( endPoint.X - start.X ) );

			List<Point2> line = new List<Point2>();
			line.Add( start );
			decimal xInc = 0.0M, yInc = 0.0M;

			// establish the value of the incrementors:
			switch ( endPoint.X.CompareTo( start.X ) )
			{
				case 0: // Straight vertical line:
					switch ( endPoint.Y.CompareTo( start.Y ) )
					{
						case -1:
							yInc = 0 - ((start.Y - endPoint.Y) / (decimal)length);
							break;
						case 0: break;
						case 1:
							yInc = (endPoint.Y - start.Y) / (decimal)length;
							break;
					}
					break;

				case -1: // Line to the left:
					xInc = 0 - ((start.X - endPoint.X) / (decimal)length);
					goto default;

				case 1: // Line to the right:
					xInc = (endPoint.X - start.X) / (decimal)length;
					goto default;

				default: // Should only be reachable when the line deviates to the left or right...
					switch ( endPoint.Y.CompareTo( start.Y ) )
					{
						case -1: // Line goes upwards:
							yInc = 0 - ((start.Y - endPoint.Y) / (decimal)length);
							break;
						case 0:  // Straight horizontal line.
							yInc = 0;
							break;
						case 1:  // Line goes downwards:
							yInc = (endPoint.Y - start.Y) / (decimal)length;
							break;
					}
					break;
			}

			decimal x = start.X, y = start.Y;
			for ( int i = 0; i < length; i++ )
			{
				x += xInc; y += yInc;
				Point2 p = new Point2( (int)Math.Round( x ), (int)Math.Round( y ) );
				if ( !line.Contains( p ) ) line.Add( p );
			}

			return line.ToArray();
		}

		/// <summary>Endeavours to create an array of points lying along the circumference of a circle defined by a supplied 
		/// center point, a radius and, optionally, an arc starting and ending point (in degrees).</summary>
		/// <param name="origin">A Point2 object representing the center of the circle.</param>
		/// <param name="radius">An Int32 value indicating the radius of the circle.</param>
		/// <param name="degreesStart">Where to start calculating (in degrees).</param>
		/// <param name="degreesEnd">Where to end calculating (in degrees).</param>
		/// <remarks>0 degrees is at 6-o'clock, 90 at 3, 180 at 12 and 270 at 9.</remarks>
		public static Point2[] Circle( Point2 origin, int radius, int degreesStart = 0, int degreesEnd = 360 )
		{
			List<Point2> circle = new List<Point2>();
			for (int degrees = degreesStart; degrees <= degreesEnd; degrees++ )
			{
				double radians = DegreesToRadians( degrees );
				Point2 point = new Point2( (int)Math.Round(radius * Math.Sin( radians )), (int)Math.Round(radius * Math.Cos( radians )) ) + origin;
				if ( !circle.Contains( point ) ) 
					circle.Add( point );
			}
			return circle.ToArray();
		}

		/// <summary>Endeavours to create an array of points lying along the circumference of a circle defined by a radius and, 
		/// optionally, an arc starting and ending point (in degrees).</summary>
		/// <param name="origin">A Point2 object representing the center of the circle.</param>
		/// <param name="radius">An Int32 value indicating the radius of the circle.</param>
		/// <param name="degreesStart">Where to start calculating (in degrees).</param>
		/// <param name="degreesEnd">Where to end calculating (in degrees).</param>
		public static Point2[] Circle( int radius, int degreesStart = 0, int degreesEnd = 360 ) =>
			Circle( new Point2( 0, 0 ), radius, degreesStart, degreesEnd );

		/// <summary>Endeavours to create an array of points lying along the circumference of a circle defined by a supplied 
		/// center point, a radius and, optionally, an arc starting and ending point (in degrees).</summary>
		/// <param name="origin">A Point2 object representing the center of the circle.</param>
		/// <param name="radius">An Int32 value indicating the radius of the circle.</param>
		/// <param name="radiansStart">Where to start calculating (in radians).</param>
		/// <param name="radiansEnd">Where to end calculating (in radians).</param>
		public static Point2[] Circle( Point2 origin, int radius, double radiansStart, double radiansEnd ) =>
			Circle( origin, radius, (int)RadiansToDegrees( radiansStart ), (int)RadiansToDegrees( radiansEnd ) );

		/// <summary>Endeavours to create an array of points lying along the circumference of a circle defined by a radius and, 
		/// optionally, an arc starting and ending point (in degrees).</summary>
		/// <param name="origin">A Point2 object representing the center of the circle.</param>
		/// <param name="radius">An Int32 value indicating the radius of the circle.</param>
		/// <param name="radiansStart">Where to start calculating (in radians).</param>
		/// <param name="radiansEnd">Where to end calculating (in radians).</param>
		public static Point2[] Circle( int radius, double radiansStart, double radiansEnd ) =>
			Circle( new Point2(0,0), radius, (int)RadiansToDegrees( radiansStart ), (int)RadiansToDegrees( radiansEnd ) );

		/// <summary>Creates a new Point2 object that contains the smallest X and Y values from the provided pair of points.</summary>
		/// <returns>A Point2 object whose X and Y coordinate are the lowest values contained in the two provided objects.</returns>
		public static Point2 Min(Point2 one, Point2 two) =>
			new Point2( Math.Min( one.X, two.X ), Math.Min( one.Y, two.Y ) );

		public static Point2 Minimum( Point2 one, Point2 two ) => Min( one, two );

		/// <summary>Creates a new Point2 object that contains the largest X and Y values from the provided pair of points.</summary>
		/// <returns>A Point2 object whose X and Y coordinate are the highest values contained in the two provided objects.</returns>
		public static Point2 Max(Point2 one, Point2 two) =>
			new Point2( Math.Max( one.X, two.X ), Math.Max( one.Y, two.Y ) );

		public static Point2 Maximum( Point2 one, Point2 two ) => Max( one, two );

		public static Point[] Convert( IEnumerable<Point2> source )
		{
			List<Point> _items = new();
			foreach ( var p in source ) _items.Add( p );
			return _items.ToArray();
		}
		#endregion
		#endregion
	}

	public class Point3D
	{
		#region Properties
		private static readonly Regex pattern = new Regex( @"[([{]?[\s]*(?<x>-?[0-9]+)[\s]*[,;][\s]*(?<y>-?[0-9]+)[\s]*[,;][\s]*(?<z>-?[0-9]+)[\s]*[)\]}]?", RegexOptions.None );
		
		/// <summary>Used to specify which axis a rotation operation is to be performed around.</summary>
		/// <remarks>Defined as a "Flags" enum so multiple axes can be defined in a single value.</remarks>
		[Flags]
		public enum Axis { X = 0x01, Y = 0x02, Z = 0x04 }
		#endregion

		#region Constructors
		/// <summary>Creates a new Point3D object with values of ( 0,0 ).</summary>
		public Point3D() => Set( 0, 0, 0 );

		/// <summary>Creates a new Point3D object with values specified in "x" and "y".</summary>
		public Point3D(int x, int y, int z) => Set( x, y, z );

		/// <summary>Creates a new Point3D object from an existing Point object plus a Z co-ordinate.</summary>
		public Point3D(Point p, int z) => Set( p.X, p.Y, z );

		/// <summary>Creata a new Point3D object using values from an existing Point2 object plus a Z co-ordinate.</summary>
		public Point3D(Point2 p, int z) => Set( p.X, p.Y, z );

		/// <summary>Creates a new Point3D object using values from an existing Point3D object.</summary>
		public Point3D(Point3D p) => Set( p.X, p.Y, p.Z );

		/// <summary>Endeavours to create a Point3D object from a supplied string.</summary>
		/// <exception cref="InvalidOperationException">Thrown if the passed string can't be parsed.</exception>
		public Point3D(string value) =>
			Parse( value );
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
			new Point3D( left.X + right.X, left.Y + right.Y, left.Z + right.Z );

		/// <summary>Creates a new Point3D object whose X and Y values represent the result of subtracting the values of "right" from "left".</summary>
		public static Point3D operator -(Point3D left, Point3D right) =>
			new Point3D( left.X - right.X, left.Y - right.Y, left.Z + right.Z );

		/// <summary>Creates a new Point3D object whose X and Y values represent the sum of the values from "left" and "right".</summary>
		public static Point3D operator +(Point left, Point3D right) =>
			new Point3D( left.X + right.X, left.Y + right.Y, right.Z );

		/// <summary>Creates a new Point3D object whose X and Y values represent the result of subtracting the values of "right" from "left".</summary>
		public static Point3D operator -(Point left, Point3D right) =>
			new Point3D( left.X - right.X, left.Y - right.Y, 0 - right.Z );

		/// <summary>Creates a new Point3D object whose X and Y values represent the sum of the values from "left" and "right".</summary>
		public static Point3D operator +(Point3D left, Point right) =>
			new Point3D( left.X + right.X, left.Y + right.Y, left.Z );

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
			new Point3D( left.X + X, left.Y, left.Z );

		/// <summary>Returns a new Point3D object whose X value is the sum of "left.X" and "X" and whose Y value is "left.Y".</summary>
		public static Point3D operator +(int X, Point3D left) =>
			left + X;

		/// <summary>Returns a new Point3D object whose X value is the result of subtracting "X" from "left.X" and whose Y value  is "left.Y".</summary>
		public static Point3D operator -(Point3D left, int X) =>
			new Point3D( left.X - X, left.Y, left.Z );

		/// <summary>Returns a new Point3D object whose X value is the result of subtracting "left.X" from "X" and whose Y value  is "left.Y".</summary>
		public static Point3D operator -(int X, Point3D left) =>
			new Point3D( X - left.X, left.Y, left.Z );
		#endregion

		#region Methods
		public Point3D Add( int x, int y, int z )
		{
			this.X += x;
			this.Y += y;
			this.Z += z;
			return new Point3D( this );
		}

		public Point3D Subtract( int x, int y, int z)
		{
			this.X -= x;
			this.Y -= y;
			this.Z -= z;
			return new Point3D( this );
		}

		public Point3D Add(Point3D translate) =>
			Add( translate.X, translate.Y, translate.Z );

		public Point3D Add(Point2 translate, int z = 0) =>
			Add( translate.X, translate.Y, z );

		public Point3D Add(Point translate, int z = 0) =>
			Add( translate.X, translate.Y, z );

		public Point3D Subtract(Point3D translate) =>
			Subtract( translate.X, translate.Y, translate.Z );

		public Point3D Subtract(Point2 translate, int z = 0) =>
			Subtract( translate.X, translate.Y, z );

		public Point3D Subtract(Point translate, int z = 0) =>
			Subtract( translate.X, translate.Y, z );

		public Point3D Min(Point3D p) =>
			new Point3D( Math.Min( p.X, this.X ), Math.Min( p.Y, this.Y ), Math.Min( p.Z, this.Z ) );

		public Point3D Max(Point3D p) =>
			new Point3D( Math.Max( p.X, this.X ), Math.Max( p.Y, this.Y ), Math.Max( p.Z, this.Z ) );

		/// <summary>Returns a new Point2 object that is the result of rotating this point around (0,0), the X/Y origin.</summary>
		/// <param name="radians">The number of radians to rotate. Negative values rotate to the left, positive rotate to the right.</param>
		/// <param name="axis">Specifies which axis to rotate the point around. Multiple axes can be specified.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the Radians value evaluates to NaN, NegativeInfinity or PositiveInfinity.</exception>
		public Point3D Rotate(double radians, Axis axis)
		{
			if (double.IsNaN( radians ) || double.IsInfinity( radians ))
				throw new ArgumentOutOfRangeException( "The specified radian value isn't valid." );

			radians = radians % ((radians < 0) ? -360 : 360);
			if (radians == 0.0) return new Point3D( this ); // if the Angle is 0, there's nothing to do.

			Point3D result = new Point3D();
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

		/// <summary>Returns a new Point2 object that is the result of rotating this point around (0,0), the X/Y origin.</summary>
		/// <param name="degrees">The number of degrees to rotate. Negative values rotate to the left, positive rotate to the right.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the Radians value evaluates to NaN, NegativeInfinity or PositiveInfinity.</exception>
		public Point3D Rotate(decimal degrees, Axis axis) =>
			Rotate( Point2.DegreesToRadians( degrees ), axis );

		/// <summary>Returns a new Point2 object that is the result of rotating this point around the provided center of rotation.</summary>
		/// <param name="degrees">The number of degrees to rotate. Negative values rotate to the left, positive rotate to the right.</param>
		/// <param name="around">A Point2 object specifying the center of rotation.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the Radians value evaluates to NaN, NegativeInfinity or PositiveInfinity.</exception>
		public Point3D Rotate(decimal degrees, Point3D around, Axis axis = Axis.X) =>
			Rotate( Point2.DegreesToRadians( degrees ), around, axis );

		/// <summary>Returns a new Point2 object that is the result of rotating this point around the provided center of rotation.</summary>
		/// <param name="radians">The number of radians to rotate. Negative values rotate to the left, positive rotate to the right.</param>
		/// <param name="around">A Point2 object specifying the center of rotation.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the Radians value evaluates to NaN, NegativeInfinity or PositiveInfinity.</exception>
		public Point3D Rotate(double radians, Point3D around, Axis axis) =>
			// Translate this point by the "around" point (make rotation point 0,0), perform the rotation about the origin, then translate it back.
			(this - around).Rotate( radians, axis ) + around;

		/// <summary>Compares this object to a provided Point object and returns TRUE if their X. Y and Z values all match.</summary>
		public bool Equals(Point3D p) =>
			(p.X == this.X) && (p.Y == this.Y) && (p.Z == this.Z);

		/// <summary>Compares this object to a provided Point object and returns TRUE if their X and Y values match.</summary>
		public bool Equals(Point p) =>
			(this.X == p.X) && (this.Y == p.Y);

		/// <summary>Compares this object to another provided Point2 object and returns TRUE if their X and Y values match.</summary>
		public bool Equals(Point2 p) =>
			(this.X == p.X) && (this.Y == p.Y);

		/// <summary>Compares this object to provided X, Y and Z values and returns TRUE if they match.</summary>
		public bool Equals(int x, int y, int z) =>
			(x == this.X) && (y == this.Y) && (z == this.Z);

		/// <summary>Compares a provided int value against this object's X value and returns TRUE if they match.</summary>
		public bool Equals(int x) => (x == X);

		public void Set( int x, int y, int z)
		{
			this.X = x;
			this.Y = y;
			this.Z = z;
		}

		// required to support equivalency operator overrides in the "Operators" declarations.
		public override bool Equals(object obj) =>
			base.Equals( obj );

		// required to support equivalency operator overrides in the "Operators" declarations.
		public override int GetHashCode() =>
			base.GetHashCode();

		/// <summary>Endeavours to create a new Point2 class from a supplied string.</summary>
		/// <param name="source">The string to try and parse into this object.</param>
		/// <exception cref="InvalidOperationException">The passed string could not be successfully parse.</exception>
		public void Parse(string source)
		{
			if (pattern.IsMatch( source ))
			{
				Match m = pattern.Matches( source )[ 0 ];
				if (m.Success)
				{
					this.X = int.Parse( m.Groups[ "x" ].Value );
					this.Y = int.Parse( m.Groups[ "y" ].Value );
					this.Z = int.Parse( m.Groups[ "z" ].Value );
					return;
				}
			}

			throw InvalidPoint( source );
		}

		/// <summary>Endeavours to parse a string into a valid Point2 object.</summary>
		/// <param name="source">The string to parse.</param>
		/// <returns>TRUE if the operation succeeded, otherwise FALSE.</returns>
		public bool TryParse(string source)
		{
			if (pattern.IsMatch( source ))
				try { Parse( source ); return true; }
				catch { /* do nothing */ }

			return false;
		}

		public override string ToString() =>
			"( " + this.X.ToString() + ", " + this.Y.ToString() + ", " + this.Z.ToString() + " )";

		#region Static Methods
		private static InvalidOperationException InvalidPoint(string value) =>
			new InvalidOperationException( "The supplied value (\"" + value + "\") could not be parsed into a valid Point2 object." );

		/// <summary>Tests a provided string to determine if it corresponds to a valid Point2 syntax.</summary>
		/// <param name="value">A string to test.</param>
		/// <returns>TRUE if the passed string is a valid Point2 syntax.</returns>
		public static bool IsValid(string value) =>
			pattern.IsMatch( value );
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

	public static class Point2Extensions
	{
		public static bool Contains( this Size source, Point2 point ) =>
			source.Contains( point );

		public static bool ContainsAny( this Size source, params Point2[] points ) =>
			source.ContainsAny( Point2.Convert( points ) );

		public static bool ContainsAll( this Size source, params Point2[] points ) =>
			source.ContainsAll( Point2.Convert( points ) );

		public static bool Contains( this Rectangle source, Point2 point ) =>
			source.Contains( point );

		public static bool ContainsAny( this Rectangle source, params Point2[] points ) =>
			source.ContainsAny(Point2.Convert(points));

		public static bool ContainsAll( this Rectangle source, params Point2[] points ) =>
			source.ContainsAll(Point2.Convert(points));
	}
}
