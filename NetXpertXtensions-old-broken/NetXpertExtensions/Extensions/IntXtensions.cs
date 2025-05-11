using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using NetXpertExtensions.Classes;

namespace NetXpertExtensions
{
#nullable disable
	public static partial class NetXpertExtensions
	{
		#region Int extensions
		/// <summary>Returns a pseudo-random number between 0 and the value of the integer that's calling the function.</summary>
		/// <returns>A pseudo-random number between 0 and the value of the integer that's calling the function.</returns>
		public static int Random( this int source ) => RandomNumberGenerator.GetInt32( source );

		/// <summary>Populates an <seealso cref="int"/> object with a pseudo-random value from a specified range.</summary>
		/// <param name="rangeSize">Specifies the <i>size</i> of the random range from which to derive the value (default 100).</param>
		/// <param name="floor">The lower-bounds of the random range (default 0).</param>
		/// <remarks>
		/// A random number between 0 and <paramref name="rangeSize"/> is generated, and <paramref name="floor"/> 
		/// is added to it.<br/>The original value of the reference object is overwritten.
		/// </remarks>
		public static int Random( this int source, int rangeSize, int floor = 0 )
		{
			int value = RandomNumberGenerator.GetInt32( Math.Abs( rangeSize ) );
			value = (rangeSize == 100) && (floor == 0) ?
				(int)Math.Round( (float)(value / rangeSize) * (float)source )
				: value + floor;
			return value;
		}

		/// <summary>Populates the calling object with a pseudo-random number between 0 (zero) and <paramref name="maxValue"/>".</summary>
		/// <param name="maxValue">The maximum integer value of the random range.</param>
		public static void Randomize( this int source, int maxValue ) => source = maxValue.Random();

		/// <summary>Provides a mechanism for quickly determining if a given number falls within a specified range.</summary>
		/// <typeparam name="T">The C# numeric type that is being tested.</typeparam>
		/// <param name="top">A number specifying the top of the range to check.</param>
		/// <param name="bottom">An optional value specifying the bottom of the range. Defaults to 0 if unspecified.</param>
		/// <param name="includeBoundaries">If <b>TRUE</b>, the test will accept values equal to the upper or lower bounds, otherwise it won't.</param>
		/// <returns><b>TRUE</b> if the tested value is within the specified range according to the passed parameters.</returns>
		/// <remarks>
		/// The <paramref name="includeBoundaries"/> option is a nullable boolean. If <b>NULL</b> is passed here, the InRange check includes the lower bounds
		/// but excludes the upper. This is useful for checks on loop ranges where zero is valid, but the Length/Count value is actually out of range.
		/// </remarks>
		/*
		public static bool InRange<T>( this T source, T top, T bottom = default(T), bool? includeBoundaries = true ) where T : INumber<T>
		{
			// If the call accidently inverts the top/bottom, correct it for them...
			if (top < bottom) bottom.SwapWith(ref top);

			// Use to check ranges on loop values where ".Count" or ".Length" values are 1 step beyond the allowable range...
			if ( includeBoundaries is null ) return (source >= bottom) && (source < top);
			
			return (bool)includeBoundaries ?
				(source >= bottom) && (source <= top)
			:
				(source > bottom) && (source < top);
		}
		*/

		/// <summary>Facilitates swapping values between two objects.</summary>
		/// <remarks>
		/// The value passed through <paramref name="swapIn"/> is assigned to the reference object, and the reference 
		/// object's original value is stored in <paramref name="swapIn"/>.
		/// </remarks>
		public static void SwapWith<T>( this T source, ref T swapIn ) => (source, swapIn) = (swapIn, source);

		/// <summary>Provides a mechanism for pluralizing a string from an <seealso cref="INumber{TSelf}"/> value.</summary>
		/// <typeparam name="T">Any C# numeric data type.</typeparam>
		/// <param name="value">A string to pluralize.</param>
		/// <param name="with">What to append to the string if it's plural.</param>
		/// <param name="numberFormat">
		/// A format string to feed into <seealso cref="String.Format"/> to re-format the numeric value when the %v marker is used.
		/// </param>
		/// <returns>The <paramref name="sample"/> string pluralized with the supplied suffix from <paramref name="with"/>.</returns>
		/// <remarks>
		/// Placing a '%v' marker in the <paramref name="sample"/> string will be replaced by the source number after being formatted
		/// via the <seealso cref="String.Format"/> function with the supplied <paramref name="numberFormat"/> string.
		/// </remarks>
		public static string Pluralize<T>( this T source, string value, string with = "s", string numberFormat = "{0:#,##0}" ) where T : INumber<T>
		{
			if ( string.IsNullOrWhiteSpace( value ) ) return value;

			value = value.Replace( "%v", String.Format( CultureInfo.CurrentCulture.NumberFormat, numberFormat, source ) );
			if ( !value.Contains( "%s" ) ) value += "%s";
			return value.Replace( "%s", source == T.One ? "" : with );
		}

		/// <summary>Tests a number against a series of other numbers and returns the lowest value of the collection.</summary>
		/// <typeparam name="T">Any C# numeric data type.</typeparam>
		/// <param name="values">All subsequent values to test.</param>
		/// <returns>The lowest value between the calling number, and all of those provided in the call.</returns>
		public static T Min<T>( this T source, params T[] values ) where T : INumber<T>
		{
			if ( (values is null) || (values.Length == 0) ) return source;

			T lowValue = source;

			if ( values.Length > 1 )
				for ( int i = 0; i < values.Length; i++ )
					lowValue = T.Min( lowValue, values[ i ] );

			return lowValue;
		}

		/// <summary>Tests an integer against a collection of int's and returns the highest value.</summary>
		/// <typeparam name="T">Any C# numeric data type.</typeparam>
		/// <param name="values">All subsequent int values to test.</param>
		/// <returns>The highest value between the calling number, and all of those provided in the call.</returns>
		public static T Max<T>( this T source, params T[] values ) where T : INumber<T>
		{
			if ( (values is null) || (values.Length == 0) ) return source;

			T highValue = source;

			if ( values.Length > 1 )
				for ( int i = 0; i < values.Length; i++ )
					highValue = T.Max( highValue, values[ i ] );

			return highValue;
		}

		/// <summary>Facilitates enforcing a range on a numeric value simply by providing an upper and lower bound.</summary>
		/// <remarks>
		/// If the calling <paramref name="value"/> is less than the <paramref name="lowerBound"/> value, then the <paramref name="lowerBound"/> 
		/// is returned instead.<br/>
		/// If the calling <paramref name="value"/> is greater than the <paramref name="upperBound"/> then the <paramref name="upperBound"/> value is returned.<br/>
		/// If the bounding values are made equal to each other, then that value is returned regardless of the calling <paramref name="value"/>.<br/>
		/// If the <paramref name="lowerBound"/> is greater than the <paramref name="upperBound"/>, the values are flipped prior to ranging.
		/// </remarks>
		/// <typeparam name="T">A valid C# numeric class/struct.</typeparam>
		/// <param name="lowerBound">Specifies the lower limit of the range.</param>
		/// <param name="upperBound">Specifies the upper limit of the range.</param>
		/// <returns>A value that falls within the specified range derived from the calling one.</returns>
		public static T LimitToRange<T>( this T value, T lowerBound, T upperBound ) where T : INumber<T>
		{
			if ( upperBound < lowerBound ) lowerBound.SwapWith( ref upperBound );
			if ( upperBound == lowerBound ) return lowerBound;

			if ( value < lowerBound ) return lowerBound;
			if ( value > upperBound ) return upperBound;
			return value;
		}

		/// <summary>Constrains a value to within the MinValue and MaxValue bounds of the specified <typeparamref name="T"/> class.</summary>
		/// <typeparam name="T">The <seealso cref="INumber{TSelf}"/> type whose <i>MaxValue</i> amd <i>MinValue</i> settings will be used to define the range.</typeparam>
		/// <typeparam name="U">The <seealso cref="INumber{TSelf}"/> type of the value being ranged.</typeparam>
		/// <exception cref="InvalidOperationException"></exception>
		public static T LimitTo<T,U>( U value ) where T : INumber<T> where U : INumber<U>
		{
			T work = (T)Convert.ChangeType( value, typeof( T ) );
			FieldInfo fiMax = typeof( T ).GetField( "MaxValue", BindingFlags.Public | BindingFlags.Static ),
					  fiMin = typeof( T ).GetField( "MinValue", BindingFlags.Public | BindingFlags.Static );

			if ( (fiMax is null) || (fiMin is null) )
				throw new InvalidOperationException( $"An Invalid Type was passed to LimitToRange<T>(): \x22{typeof( T ).Name}\x22" );

			return work.LimitToRange<T>( (T)fiMin.GetValue( null ), (T)fiMax.GetValue( null ) );
		}

		/// <summary>Facilitates creating a range of integers from a starting value to a specified ending one.<br/><b>NOTE:</b>
		/// Using this to iterate arrays is <i>highly</i> inefficient as the computer will have to loop all values twice!
		/// </summary>
		/// <param name="end">The value to iterate to. This value <b>will</b> be included in the output!</param>
		/// <remarks>If the 'end' value is <i>lower</i> than the starting value, the resulting array will count <i>down</i>
		/// to the ending value!</remarks>
		/// <returns>An array of integers from (and including) the <i>start</i> value through to (and including) the <i>ending</i> one.</returns>
		public static int[] RangeTo( this int start, int end )
		{
			if ( end == start ) return new int[] { start };

			List<int> values = new();

			if ( end > start )
				for ( int i = start; i <= end; i++ ) values.Add( i );
			else
				for ( int i = start; i >= end; i-- ) values.Add( i );

			return values.ToArray();
		}

		/// <summary>Raises a <seealso cref="byte"/> value to an arbitrary, specified (<seealso cref="int"/>) power.</summary>
		/// <param name="exponent">The value to which the base will be raised.</param>
		/// <returns>A <seealso cref="UInt128""/> value containing the result of the operation.</returns>
		public static UInt128 RaiseTo( this byte @base, int exponent ) => (UInt128)BigInteger.Pow( @base, exponent );

		/// <summary>Raises an <seealso cref="sbyte"/> value to an arbitrary, specified (<seealso cref="int"/>) power.</summary>
		/// <param name="exponent">The value to which the base will be raised.</param>
		/// <returns>A <seealso cref="Int128""/> value containing the result of the operation.</returns>
		public static Int128 RaiseTo( this sbyte @base, int exponent ) => (Int128)BigInteger.Pow( @base, exponent );

		/// <summary>Raises a <seealso cref="ushort"/> value to an arbitrary, specified (<seealso cref="int"/>) power.</summary>
		/// <param name="exponent">The value to which the base will be raised.</param>
		/// <returns>A <seealso cref="UInt128""/> value containing the result of the operation.</returns>
		public static UInt128 RaiseTo( this ushort @base, int exponent ) => (UInt128)BigInteger.Pow( @base, exponent );

		/// <summary>Raises a <seealso cref="short"/> value to an arbitrary, specified (<seealso cref="int"/>) power.</summary>
		/// <param name="exponent">The value to which the base will be raised.</param>
		/// <returns>A <seealso cref="Int128""/> value containing the result of the operation.</returns>
		public static Int128 RaiseTo( this short @base, int exponent ) => (Int128)BigInteger.Pow( @base, exponent );

		/// <summary>Raises a <seealso cref="uint"/> value to an arbitrary, specified (<seealso cref="int"/>) power.</summary>
		/// <param name="exponent">The value to which the base will be raised.</param>
		/// <returns>A <seealso cref="UInt128""/> value containing the result of the operation.</returns>
		public static UInt128 RaiseTo( this uint @base, int exponent ) => (UInt128)BigInteger.Pow( @base, exponent );

		/// <summary>Raises a <seealso cref="int"/> value to an arbitrary, specified (<seealso cref="int"/>) power.</summary>
		/// <param name="exponent">The value to which the base will be raised.</param>
		/// <returns>A <seealso cref="Int128""/> value containing the result of the operation.</returns>
		public static Int128 RaiseTo( this int @base, int exponent ) => (Int128)BigInteger.Pow( @base, exponent );

		/// <summary>Raises a <seealso cref="ulong"/> value to an arbitrary, specified (<seealso cref="int"/>) power.</summary>
		/// <param name="exponent">The value to which the base will be raised.</param>
		/// <returns>A <seealso cref="UInt128""/> value containing the result of the operation.</returns>
		public static UInt128 RaiseTo( this ulong @base, int exponent ) => (UInt128)BigInteger.Pow( @base, exponent );

		/// <summary>Raises a <seealso cref="long"/> value to an arbitrary, specified (<seealso cref="int"/>) power.</summary>
		/// <param name="exponent">The value to which the base will be raised.</param>
		/// <returns>A <seealso cref="Int128""/> value containing the result of the operation.</returns>
		public static Int128 RaiseTo( this long @base, int exponent ) => (Int128)BigInteger.Pow( @base, exponent );

		/// <summary>Returns a <seealso cref="byte"/> value as an array of bytes.</summary>
		public static byte[] AsBytes( this byte source ) => new byte[] { source };

		/// <summary>Populates a <seealso cref="byte"/> value from an array of bytes.</summary>
		public static void FromBytes( this byte source, byte[] value ) => 
			source = (byte)IntFromBytes(value);

		/// <summary>Returns a <seealso cref="sbyte"/> value as an array of bytes.</summary>
		public static byte[] AsBytes( this sbyte source ) => ((byte)source).AsBytes();

		/// <summary>Populates a <seealso cref="sbyte"/> value from an array of bytes.</summary>
		public static void FromBytes( this sbyte source, byte[] value ) =>
			source = (sbyte)IntFromBytes( value );

		/// <summary>Returns a <seealso cref="short"/> value as an array of bytes.</summary>
		public static byte[] AsBytes( this short source ) => 
			new byte[] { (byte)(source & 0xff00 >> 8), (byte)(source & 0x00ff) };

		/// <summary>Populates a <seealso cref="short"/> value from an array of bytes.</summary>
		public static void FromBytes( this short source, byte[] value ) =>
			source = (short)IntFromBytes( value );

		/// <summary>Returns a <seealso cref="ushort"/> value as an array of bytes.</summary>
		public static byte[] AsBytes( this ushort source ) => ((ushort)source).AsBytes();

		/// <summary>Populates a <seealso cref="ushort"/> value from an array of bytes.</summary>
		public static void FromBytes( this ushort source, byte[] value ) =>
			source = (ushort)IntFromBytes( value );

		/// <summary>Returns a <seealso cref="uint"/> value as an array of bytes.</summary>
		public static byte[] AsBytes( this uint source ) => new byte[]
			{
				(byte) (source & 0xff000000 >> 0x18), (byte) (source & 0x00ff0000 >> 0x10),
				(byte) (source & 0x0000ff00 >> 0x08), (byte) (source & 0x000000ff)
			};

		/// <summary>Populates a <seealso cref="byte"/> value from an array of bytes.</summary>
		public static void FromBytes( this uint source, byte[] value ) =>
			source = (uint)IntFromBytes( value );

		/// <summary>Returns a <seealso cref="int"/> value as an array of bytes.</summary>
		public static byte[] AsBytes( this int source ) => ((uint)source).AsBytes();

		/// <summary>Populates a <seealso cref="int"/> value from an array of bytes.</summary>
		public static void FromBytes( this int source, byte[] value ) =>
			source = (int)IntFromBytes( value );

		/// <summary>Returns a <seealso cref="ulong"/> value as an array of bytes.</summary>
		public static byte[] AsBytes( this ulong source ) => new byte[]
			{
				(byte) (source & 0xff00000000000000 >> 0x38), (byte) (source & 0x00ff000000000000 >> 0x30),
				(byte) (source & 0x0000ff0000000000 >> 0x28), (byte) (source & 0x000000ff00000000 >> 0x20),

				(byte) (source & 0x00000000ff000000 >> 0x18), (byte) (source & 0x0000000000ff0000 >> 0x10),
				(byte) (source & 0x000000000000ff00 >> 0x08), (byte) (source & 0x00000000000000ff)
			};

		/// <summary>Populates a <seealso cref="ulong"/> value from an array of bytes.</summary>
		public static void FromBytes( this ulong source, byte[] value ) =>
			source = IntFromBytes( value );

		/// <summary>Populates a <seealso cref="ulong"/> value from an array of bytes,</summary>
		/// <param name="value">An array of bytes to populate the value with.</param>
		/// <returns>A <seealso cref="ulong"/> value populated from the supplied array.</returns>
		/// <remarks>In the supplied array, the first byte is taken to be the most significant (largest) value, while the last is the least (lowest).</remarks>
		private static ulong IntFromBytes( byte[] value )
		{
			ulong source = 0;
			if ( value.Length > 0 )
				for ( int i = value.Length; i > 0; i++ )
					source += (ulong)value[ i ] << (64 - (i * 8));
			return source;
		}

		/// <summary>Returns a <seealso cref="long"/> value as an array of bytes.</summary>
		public static byte[] AsBytes( this long source ) => ((ulong)source).AsBytes();

		/* For when C# fully implements U/Int128 types...
		public static byte[] AsBytes( this UInt128 source ) => new byte[]
			{
				(byte) (source & 0xff000000000000000000000000000000 >> 0x78),
				(byte) (source & 0x00ff0000000000000000000000000000 >> 0x70),
				(byte) (source & 0x0000ff00000000000000000000000000 >> 0x68),
				(byte) (source & 0x000000ff000000000000000000000000 >> 0x60),
				(byte) (source & 0x00000000ff0000000000000000000000 >> 0x58),
				(byte) (source & 0x0000000000ff00000000000000000000 >> 0x50),
				(byte) (source & 0x000000000000ff000000000000000000 >> 0x48),
				(byte) (source & 0x00000000000000ff0000000000000000 >> 0x40),

				(byte) (source & 0x0000000000000000ff00000000000000 >> 0x38),
				(byte) (source & 0x000000000000000000ff000000000000 >> 0x30),
				(byte) (source & 0x00000000000000000000ff0000000000 >> 0x28),
				(byte) (source & 0x0000000000000000000000ff00000000 >> 0x20),
				(byte) (source & 0x000000000000000000000000ff000000 >> 0x18),
				(byte) (source & 0x00000000000000000000000000ff0000 >> 0x10),
				(byte) (source & 0x0000000000000000000000000000ff00 >> 0x08),
				(byte) (source & 0x000000000000000000000000000000ff)
			};

		public static byte[] AsBytes( this Int128 source ) => ((Uint128)source).AsBytes();
		*/

		/// <summary>Provides a bitwise representation of an <seealso cref="sbyte"/> value in a string.</summary>
		/// <param name="byteSplit">What character to use for separating bits on the byte-boundary (Default = <i>null</i>).</param>
		/// <param name="nibbleSplit">What character to use for separating bits on nibble boundaries. (Default = <i>null</i>).</param>
		/// <returns>A string representing the original value as a binary string (largest order bits first!)</returns>
		/// <remarks>The returned string will <i>include</i> the standard "<b>0b</b>" binary prefix!</remarks>
		public static string ToBinaryString( this sbyte source, string byteSplit = null, string nibbleSplit = null ) =>
			CreateBinaryString( (byte)source, byteSplit, nibbleSplit );

		/// <summary>Provides a bitwise representation of a <seealso cref="byte"/> value in a string.</summary>
		/// <param name="byteSplit">What character to use for separating bits on the byte-boundary (Default = <i>null</i>).</param>
		/// <param name="nibbleSplit">What character to use for separating bits on nibble boundaries. (Default = <i>null</i>).</param>
		/// <returns>A string representing the original value as a binary string (largest order bits first!)</returns>
		/// <remarks>The returned string will <i>include</i> the standard "<b>0b</b>" binary prefix!</remarks>
		public static string ToBinaryString( this byte source, string byteSplit = null, string nibbleSplit = null ) =>
			CreateBinaryString( source, byteSplit, nibbleSplit );

		/// <summary>Provides a bitwise representation of a <seealso cref="ushort"/> value in a string.</summary>
		/// <param name="byteSplit">What character to use for separating bits on the byte-boundary (Default = <i>null</i>).</param>
		/// <param name="nibbleSplit">What character to use for separating bits on nibble boundaries. (Default = <i>null</i>).</param>
		/// <returns>A string representing the original value as a binary string (largest order bits first!)</returns>
		/// <remarks>The returned string will <i>include</i> the standard "<b>0b</b>" binary prefix!</remarks>
		public static string ToBinaryString( this ushort source, string byteSplit = null, string nibbleSplit = null ) =>
			CreateBinaryString( source, byteSplit, nibbleSplit );

		/// <summary>Provides a bitwise representation of a <seealso cref="short"/> value in a string.</summary>
		/// <param name="byteSplit">What character to use for separating bits on the byte-boundary (Default = <i>null</i>).</param>
		/// <param name="nibbleSplit">What character to use for separating bits on nibble boundaries. (Default = <i>null</i>).</param>
		/// <returns>A string representing the original value as a binary string (largest order bits first!)</returns>
		/// <remarks>The returned string will <i>include</i> the standard "<b>0b</b>" binary prefix!</remarks>
		public static string ToBinaryString( this short source, string byteSplit = null, string nibbleSplit = null ) =>
			CreateBinaryString( (ushort)source, byteSplit, nibbleSplit );

		/// <summary>Provides a bitwise representation of an <seealso cref="int"/> value in a string.</summary>
		/// <param name="byteSplit">What character to use for separating bits on the byte-boundary (Default = <i>null</i>).</param>
		/// <param name="nibbleSplit">What character to use for separating bits on nibble boundaries. (Default = <i>null</i>).</param>
		/// <returns>A string representing the original value as a binary string (largest order bits first!)</returns>
		/// <remarks>The returned string will <i>include</i> the standard "<b>0b</b>" binary prefix!</remarks>
		public static string ToBinaryString( this int source, string byteSplit = null, string nibbleSplit = null ) =>
			CreateBinaryString( (uint)source, byteSplit, nibbleSplit );

		/// <summary>Provides a bitwise representation of a <seealso cref="uint"/> value in a string.</summary>
		/// <param name="byteSplit">What character to use for separating bits on the byte-boundary (Default = <i>null</i>).</param>
		/// <param name="nibbleSplit">What character to use for separating bits on nibble boundaries. (Default = <i>null</i>).</param>
		/// <returns>A string representing the original value as a binary string (largest order bits first!)</returns>
		/// <remarks>The returned string will <i>include</i> the standard "<b>0b</b>" binary prefix!</remarks>
		public static string ToBinaryString( this uint source, string byteSplit = null, string nibbleSplit = null ) =>
			CreateBinaryString( source, byteSplit, nibbleSplit );

		/// <summary>Provides a bitwise representation of a <seealso cref="ulong"/> value in a string.</summary>
		/// <param name="byteSplit">What character to use for separating bits on the byte-boundary (Default = <i>null</i>).</param>
		/// <param name="nibbleSplit">What character to use for separating bits on nibble boundaries. (Default = <i>null</i>).</param>
		/// <returns>A string representing the original value as a binary string (largest order bits first!)</returns>
		/// <remarks>The returned string will <i>include</i> the standard "<b>0b</b>" binary prefix!</remarks>
		public static string ToBinaryString( this ulong source, string byteSplit = null, string nibbleSplit = null ) =>
			CreateBinaryString( source, byteSplit, nibbleSplit );

		/// <summary>Provides a bitwise representation of a <seealso cref="long"/> value in a string.</summary>
		/// <param name="byteSplit">What character to use for separating bits on the byte-boundary (Default = <i>null</i>).</param>
		/// <param name="nibbleSplit">What character to use for separating bits on nibble boundaries. (Default = <i>null</i>).</param>
		/// <returns>A string representing the original value as a binary string (largest order bits first!)</returns>
		/// <remarks>The returned string will <i>include</i> the standard "<b>0b</b>" binary prefix!</remarks>
		public static string ToBinaryString( this long source, string byteSplit = null, string nibbleSplit = null ) =>
			CreateBinaryString( (ulong)source, byteSplit, nibbleSplit );

		/// <summary>Creates a bitwise representation of a <seealso cref="ulong"/> value as a string.</summary>
		/// <param name="byteSplit">What character to use for separating bits on the byte-boundary (Default = <i>null</i>).</param>
		/// <param name="nibbleSplit">What character to use for separating bits on nibble boundaries. (Default = <i>null</i>).</param>
		/// <returns>A string representing the original value as a binary string (largest order bits first!)</returns>
		/// <remarks>The returned string <i>includes</i> the "<b>0b</b>" prefix indicating that it's a binary value!</remarks>
		private static string CreateBinaryString( ulong value, string byteSplit = null, string nibbleSplit = null )
		{
			// Convert.ToString() does NOT have a method to convert from ULONG to Base2, however LONG is bit-compatible with ULONG
			// so casting it accordingly still works.

			if ( byteSplit is null ) byteSplit = " ";
			if ( nibbleSplit is null ) nibbleSplit = "";

			byte[] bytes = value.AsBytes();
			StringCollection nibbles = new();

			foreach( byte b in bytes)
				nibbles.Add( Convert.ToString( b, 2 ).PadLeft( 8, '0' ).Split( 4 ) );

			string result = "";
			for ( int i = 0; i < nibbles.Count; i++ )
				result += ((i > 0) && (i % 2 == 0) ? byteSplit : nibbleSplit) + nibbles[ i ];

			return $"0b{result[ nibbleSplit.Length.. ]}";
		}

		///<summary>Validates whether or not a given <seealso cref='ulong'/> value matches a supplied bitmap.</summary>
		///<returns><b>TRUE</b> if the source value matches the supplied bitmap mask.</returns>
		public static bool BitMaskMatch( this ulong source, ulong mask ) => (source & mask) == mask;

		///<summary>Validates whether or not a given <seealso cref="long"/> value matches a supplied bitmap.</summary>
		///<returns><b>TRUE</b> if the source value matches the supplied bitmap mask.</returns>
		public static bool BitMaskMatch( this long source, long mask ) => (source & mask) == mask;

		///<summary>Validates whether or not a given <seealso cref="uint"/> value matches a supplied bitmap.</summary>
		///<returns><b>TRUE</b> if the source value matches the supplied bitmap mask.</returns>
		public static bool BitMaskMatch( this uint source, uint mask ) => (source & mask) == mask;

		///<summary>Validates whether or not a given <seealso cref="int"/> value matches a supplied bitmap.</summary>
		///<returns><b>TRUE</b> if the source value matches the supplied bitmap mask.</returns>
		public static bool BitMaskMatch( this int source, int mask ) => (source & mask) == mask;

		///<summary>Validates whether or not a given <seealso cref="ushort"/> value matches a supplied bitmap.</summary>
		///<returns><b>TRUE</b> if the source value matches the supplied bitmap mask.</returns>
		public static bool BitMaskMatch( this ushort source, ushort mask ) => (source & mask) == mask;

		///<summary>Validates whether or not a given <seealso cref="short"/> value matches a supplied bitmap.</summary>
		///<returns><b>TRUE</b> if the source value matches the supplied bitmap mask.</returns>
		public static bool BitMaskMatch( this short source, short mask ) => (source & mask) == mask;

		///<summary>Validates whether or not a given <seealso cref="byte"/> value matches a supplied bitmap.</summary>
		///<returns><b>TRUE</b> if the source value matches the supplied bitmap mask.</returns>
		public static bool BitMaskMatch( this byte source, byte mask ) => (source & mask) == mask;

		///<summary>Validates whether or not a given <seealso cref="sbyte"/> value matches a supplied bitmap.</summary>
		///<returns><b>TRUE</b> if the source value matches the supplied bitmap mask.</returns>
		public static bool BitMaskMatch( this sbyte source, sbyte mask ) => (source & mask) == mask;


		/// <summary>Returns the byte-equivalent of the low-order nibble (4-bits :: <seealso cref="byte"/>) from the source <seealso cref="byte"/> value.</summary>
		public static byte Lo( this byte source ) => (byte)( source & 0x0f );

		/// <summary>Returns the byte-equivalent of the high-order nibble (4-bits :: <seealso cref="byte"/>) from the source <seealso cref="byte"/> value.</summary>
		public static byte Hi( this byte source ) => (byte)((source & 0xf0) >> 4);

		/// <summary>Returns the value of the low-order byte (8-bits :: <seealso cref="byte"/>) of the source <seealso cref="ushort"/> value.</summary>
		public static byte Lo( this ushort source ) => (byte)(source & 0xff);

		/// <summary>Returns the byte-equivalent of the high-order byte (8-bits :: <seealso cref="byte"/>) from the source <seealso cref="ushort"/> value.</summary>
		public static byte Hi( this ushort source ) => (byte)((source & 0xff00) >> 8);

		/// <summary>Returns the value of the low-order word (16-bits :: <seealso cref="ushort"/>) from the source <seealso cref="uint"/>.</summary>
		public static ushort Lo( this uint source ) => (ushort)(source & 0x0000ffff);

		/// <summary>Returns the value of the high-order word (16-bits :: <seealso cref="ushort"/>) from the source <seealso cref="uint"/>.</summary>
		public static ushort Hi( this uint source ) => (ushort)((source & 0xffff0000) >> 16);

		/// <summary>Returns the value of the low-order double-word (32-bits :: <seealso cref="uint"/>) of the source <seealso cref="ulong"/> value.</summary>
		public static uint Lo( this ulong source ) => (uint)(source & 0x00000000ffffffff);

		/// <summary>Returns the value of the high-order double-word (32-bit :: <seealso cref="uint"/>) of the source <seealso cref="ulong"/> value.</summary>
		public static uint Hi( this ulong source ) => (uint)((source & 0xffffffff00000000) >> 32);

		/// <summary>Returns the value of the low-order double-word (32-bit :: <seealso cref="uint"/>) of the source <seealso cref="ulong"/> value.</summary>
		public static byte Lo( this sbyte source ) => (byte)(source & 0x0f);

		/// <summary>Returns the value of the high-order nibble (4-bit :: <seealso cref="byte"/>) of the source <seealso cref="sbyte"/> value.</summary>
		public static byte Hi( this sbyte source ) => (byte)((source & 0xf0) >> 4);

		/// <summary>Returns the value of the low-order nibble (4-bit :: <seealso cref="byte"/>) of the source <seealso cref="byte"/> value.</summary>
		public static byte Lo( this short source ) => (byte)(source & 0xff);

		/// <summary>Returns the value of the high-order byte (8-bit :: <seealso cref="byte"/>) of the source <seealso cref="short"/> value.</summary>
		public static byte Hi( this short source ) => (byte)((source & 0xff00) >> 8);

		/// <summary>Returns the value of the low-order byte (8-bit :: <seealso cref="byte"/>) of the source <seealso cref="int"/> value.</summary>
		public static ushort Lo( this int source ) => (ushort)(source & 0x0000ffff);

		/// <summary>Returns the value of the high-order byte (16-bit :: <seealso cref="byte"/>) of the source <seealso cref="int"/> value.</summary>
		public static ushort Hi( this int source ) => (ushort)((source & 0xffff0000) >> 16);

		/// <summary>Returns the value of the low-order double-word (32-bit :: <seealso cref="byte"/>) of the source <seealso cref="long"/> value.</summary>
		public static uint Lo( this long source ) => (uint)(source & 0x00000000ffffffff);

		/// <summary>Returns the value of the high-order double-word (32-bit :: <seealso cref="byte"/>) of the source <seealso cref="long"/> value.</summary>
		public static uint Hi( this long source ) => (uint)(((ulong)source & 0xffffffff00000000) >> 32);

		/// <summary>Returns the value of the source <seealso cref="ulong"/> as an array of bytes.</summary>
		/// <param name="limit">If you don't need ALL of the bytes, you can limit the number returned by setting this value.</param>
		/// <returns>An array of bytes representing the binary value of the referenced object.</returns>
		/// <remarks>The bytes returned in the resulting array start from the <i>HIGHEST</i> order bits and progress to the <i>LOWEST</i>.</remarks>
		public static byte[] GetBytes( this ulong value, int limit = 16 )
		{
			byte[] result = new byte[ 16 ] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			if ( !limit.InRange( 16, 1 ) ) limit = 16;
			for (int i = 0; i < limit; i++ ) 
				result[ i ] = (byte)((value >> (i * 8)) & 0xff);

			return result;
		}

		/// <summary>Facilitates populating a <seealso cref="ulong"/> value from an array of bytes.</summary>
		/// <param name="bytes">The byte array to draw the values from.</param>
		/// <param name="limit">If only a limited number of bytes are to be dereferenced from the array, that value can be specified here.</param>
		public static void SetBytes( this ulong source, byte[] bytes, int limit = 8 ) 
		{ 
			source &= 0x00; // Sets the value of the calling object to 0;
			limit = limit.InRange( 8, 2 ) ? limit-1 : 7;
			if ( bytes.Length > limit )
				for ( int i = 0; i < bytes.Length; i++ )
					source |= (ulong)bytes[ i ] << (i * 8);
		}

		/// <summary>An implementation of the Euclidean Algorithm for determining the Greatest Common Divisor (GCD) of two Integers.</summary>
		/// <param name="value">The second integer for which to find the GCD.</param>
		/// <returns>The largest common divisor for the two given values.</returns>
		/// <remarks>
		/// This uses the 128-bit Unsigned Integer (<seealso cref="UInt128"/>) type to facilitate passing arbitrarily large numbers.<br/>
		/// The values must be unsigned as the algorithm must be reversed (inverted) to process negatives.
		/// </remarks>
		public static UInt128 GCD( this UInt128 source, UInt128 value )
		{
			UInt128 work = source;
			while ( work > 0 && value > 0 ) 
			{
				if ( work > value )
					work %= value;
				else
					value %= work;
			}
			return work | value;
		}

		/// <summary>An implementation of the Euclidean Algorithm for determining the Greatest Common Divisor (GCD) of two Integers.</summary>
		/// <param name="value">The second integer for which to find the GCD.</param>
		/// <returns>The largest common divisor for the two given values.</returns>
		/// <remarks>
		/// The values must be unsigned as the algorithm must be reversed (inverted) to process negatives.
		/// </remarks>
		public static ulong GCD( this ulong source, ulong value ) => (ulong)((UInt128)source).GCD( (UInt128)value );

		/// <summary>An implementation of the Euclidean Algorithm for determining the Greatest Common Divisor (GCD) of two Integers.</summary>
		/// <param name="value">The second integer for which to find the GCD.</param>
		/// <returns>The largest common divisor for the two given values.</returns>
		/// <remarks>
		/// The values must be unsigned as the algorithm must be reversed (inverted) to process negatives.
		/// </remarks>
		public static ulong GCD( this uint source, uint value ) => (uint)((UInt128)source).GCD( (UInt128)value );

		/// <summary>An implementation of the Euclidean Algorithm for determining the Greatest Common Divisor (GCD) of two Integers.</summary>
		/// <param name="value">The second integer for which to find the GCD.</param>
		/// <returns>The largest common divisor for the two given values.</returns>
		/// <remarks>
		/// The values must be unsigned as the algorithm must be reversed (inverted) to process negatives.
		/// </remarks>
		public static ulong GCD( this ushort source, ushort value ) => (ushort)((UInt128)source).GCD( (UInt128)value );

		/// <summary>An implementation of the Euclidean Algorithm for determining the Greatest Common Divisor (GCD) of two Integers.</summary>
		/// <param name="value">The second integer for which to find the GCD.</param>
		/// <returns>The largest common divisor for the two given values.</returns>
		/// <remarks>
		/// The values must be unsigned as the algorithm must be reversed (inverted) to process negatives.
		/// </remarks>
		public static byte GCD( this byte source, byte value ) => (byte)((UInt128)source).GCD( (UInt128)value );
		#endregion
	}
}