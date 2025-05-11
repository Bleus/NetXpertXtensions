using System;
using System.Security.Cryptography;
using NetXpertExtensions;

namespace NetXpertCodeLibrary
{
	public static class CryptoRNG
	{
		#region Properties
		public static readonly Type[] IntegerTypes =
			new Type[] {
					typeof( int ),
					typeof( uint ),
					typeof( short ),
					typeof( ushort ),
					typeof( sbyte ),
					typeof( byte ),
					typeof( long ),
					typeof( ulong )
			};

		public static readonly Type[] SignedIntegerTypes =
			new Type[] {
					typeof( int ),
					typeof( short ),
					typeof( sbyte ),
					typeof( long ),
			};

		public static readonly Type[] UnsignedIntegerTypes =
			new Type[] {
					typeof( uint ),
					typeof( ushort ),
					typeof( byte ),
					typeof( ulong )
			};
		#endregion

		#region Methods
		private static uint GetRandomUInt() =>
			BitConverter.ToUInt32( GenerateCryptoRNG( sizeof( uint ) ), 0 );

		/// <summary>Creates a byte array of the specified length containing a Cryptographically-secure set of values
		/// within the specified range.</summary>
		/// <param name="length">The number of bytes to generate.</param>
		/// <param name="lowerBound">The lowest allowable value in the array (def = 0).</param>
		/// <param name="upperBound">The highest allowable value in the array (def = 255)</param>
		private static byte[] GenerateCryptoRNG( int length, byte lowerBound = 0x00, byte upperBound = 0xff )
		{
			length = Math.Max( length, 4 );
			byte lB = Math.Min( lowerBound, upperBound ), uB = Math.Max( lowerBound, upperBound );

			byte[] result = new byte[ length ], buffer = new byte[ 1 ];
			RandomNumberGenerator rng = RandomNumberGenerator.Create();
			for ( int i = 0; i < length; i++ )
				do
				{
					rng.GetBytes( buffer );
					result[ i ] = buffer[ 0 ];
				} 
				while ( result[i] < lB || result[i] > uB );

			return result;
		}

		/// <summary>Tests a Generic parameter to see if it's a C# integer type.</summary>
		/// <returns>TRUE if &lt;T&gt; is one of the C# Integer classes.</returns>
		public static bool IsIntegerType<T>() where T : struct => IsIntegerType( typeof( T ) );

		/// <summary>Tests a supplied `Type` object and reports if it's one of the built-in Integer classes.</summary>
		/// <param name="t">The `Type` object to test.</param>
		/// <returns>TRUE if &lt;T&gt; is one of the C# base Integer classes.</returns>
		public static bool IsIntegerType( Type t ) =>
			(t is not null) && t.IsValueType && IntegerTypes.Contains( t );

		private static T MinValue<T>() where T : struct
		{
			if ( IsIntegerType<T>() )
				return (T)(SignedIntegerTypes.Contains( typeof( T ) ) ? Convert.ChangeType( long.MinValue, typeof( T ) ) : 0);

			throw Throw<T>();
		}

		private static T MaxValue<T>() where T : struct
		{
			if ( IsIntegerType<T>() )
				return (T)(SignedIntegerTypes.Contains( typeof( T ) ) ? Convert.ChangeType( long.MaxValue, typeof( T ) ) : ulong.MaxValue );

			throw Throw<T>();
		}

		/// <summary>Creates a percentage (type <b>double</b>) from any provided integer type.</summary>
		/// <typeparam name="T">Any integer type (signed or unsigned)</typeparam>
		/// <param name="value"></param>
		/// <returns></returns>
		/// <exception cref="InvalidCastException">Thrown if the Generic type isn't one of C#'s Integer types.</exception>
		private static double Multiplier<T>( T value ) where T : struct
		{
			if ( IsIntegerType<T>() )
			{
				if ( UnsignedIntegerTypes.Contains( typeof(T) ) )
					return (double)Convert.ChangeType( value, typeof( double ) ) / (double)Convert.ChangeType( MaxValue<T>(), typeof( double ) );

				ulong work = (ulong)Convert.ChangeType( value, typeof( ulong ) );

			}

			throw Throw<T>();
		}

		/// <summary>Dynamically generates a cryptographically-safe random number of the specified type.</summary>
		/// <typeparam name="T">Specifies the Integer type of the number to generate.</typeparam>
		/// <returns>A cryptographically safe random number, of the specified type.</returns>
		/// <exception cref="InvalidCastException">Thrown if the Generic type isn't one of C#'s Integer types.</exception>
		private static T Generate<T>() where T : struct
		{
			if (IsIntegerType<T>())
				return (T)Convert.ChangeType( BitConverter.ToUInt64( GenerateCryptoRNG( 8 ), 0 ), typeof( T ) );

			throw Throw<T>();
		}

		/// <summary>Creates an array of cryptographically-safe random numbers with the specified length.</summary>
		/// <typeparam name="T">Specifies the Integer type of the numbers to be generated.</typeparam>
		/// <param name="count">The number of elements to populate the array with.</param>
		/// <returns>An array of specified values within the provided range, of the specified type, and length.</returns>
		/// <exception cref="InvalidCastException">Thrown if the Generic type isn't one of C#'s Integer types.</exception>
		public static T[] Generate<T>( int count ) where T : struct
		{
			if ( count > 0 )
			{
				if ( IsIntegerType<T>() )
				{
					T[] result = new T[ count ];
					for ( int i = 0; i < count; i++ )
						result[ i ] = Generate<T>();

					return result;
				}
			}

			throw Throw<T>();
		}

		/// <summary>Dynamically generates a cryptographically-safe random number of the specified type within the specified range.</summary>
		/// <typeparam name="T">Specifies the Integer type of the number to generate.</typeparam>
		/// <param name="lowerValue">The (inclusive) lower bound value of the desired range.</param>
		/// <param name="upperValue">The (inclusive) upper bound value of the desired range.</param>
		/// <returns>A cryptographically safe random number, of the specified type wihtin the specified range.</returns>
		/// <exception cref="InvalidCastException">Thrown if the Generic type isn't one of the Integer types.</exception>
		public static T Generate<T>( T lowerValue, T upperValue ) where T : struct =>
			Rangify<T>( Generate<T>(), lowerValue, upperValue );

		/// <summary>Squeezes a provided integer into a given range.</summary>
		/// <typeparam name="T">Specifies the Integer type of the number to generate.</typeparam>
		/// <param name="value">The value to create a percentage value from for rangification.</param>
		/// <param name="lowerValue">The (inclusive) lower bound value of the desired range.</param>
		/// <param name="upperValue">The (inclusive) upper bound value of the desired range.</param>
		/// <returns>A value of the specified type, within the specified range.</returns>
		/// <exception cref="InvalidCastException">Thrown if the Generic type isn't one of the Integer types.</exception>
		/// <remarks>The <b>value</b> is divided by the MaxValue of the &lt;T&gt; type to create a percentage as a <b>double</b> value.</remarks>
		private static T Rangify<T>(T value, T lowerValue, T upperValue ) where T : struct
		{
			if (IsIntegerType<T>())
			{
				// Do all the maths via 64-bit unsigned integers, then convert the result back to the source type:
				ulong
					lB = Math.Min( Convert.ToUInt64( lowerValue ), Convert.ToUInt64( upperValue ) ),
					uB = Math.Max( lB, Convert.ToUInt64( upperValue ) );

				return (T)Convert.ChangeType( ((uB - lB) * Multiplier<T>(value) ) + lB, typeof(T) );
			}
			throw Throw<T>();
		}
		#endregion

		#region Public Random Number extension Generators by Integer types
		public static byte Generate( byte minValue, byte maxValue ) => Generate<byte>( minValue, maxValue );
		public static sbyte Generate( sbyte minValue, sbyte maxValue ) => Generate<sbyte>( minValue, maxValue );
		public static ushort Generate( ushort minValue, ushort maxValue ) => Generate<ushort>( minValue, maxValue );
		public static short Generate( short minValue, short maxValue ) => Generate<short>( minValue, maxValue );
		public static uint Generate( uint minValue, uint maxValue ) => Generate<uint>( minValue, maxValue );
		public static int Generate( int minValue, int maxValue ) => Generate<int>( minValue, maxValue );
		public static ulong Generate( ulong minValue, ulong maxValue ) => Generate( minValue, maxValue );
		public static long Generate( long minValue, long maxValue ) => Generate<long>( minValue, maxValue );
		public static byte[] Generate( int count ) => Generate<byte>( count );
		#endregion

		#region Exceptions
		/// <summary>Creates a new <b>InvalidCastException</b> instance with an appropriate message for the supplied type.</summary>
		/// <param name="value">The name of the type causing the error.</param>
		/// <returns>A new <b>InvalidCastException</b> instance with an appropriate message for the supplied type.</returns>
		private static InvalidCastException Throw( string value ) =>
			new( $"The supplied `type` for this generic method must be either some kind of integer, or a class that is derived from one! (\x22{value}\x22)" );

		/// <summary>Creates a new <b>InvalidCastException</b> instance with an appropriate message for the supplied type.</summary>
		/// <param name="value">The name of the type causing the error.</param>
		/// <returns>A new <b>InvalidCastException</b> instance with an appropriate message for the supplied type.</returns>
		private static InvalidCastException Throw( Type type ) => Throw( type.Name );

		/// <summary>Creates a new <b>InvalidCastException</b> instance with an appropriate message for the supplied type.</summary>
		/// <param name="value">The name of the type causing the error.</param>
		/// <returns>A new <b>InvalidCastException</b> instance with an appropriate message for the supplied type.</returns>
		private static InvalidCastException Throw<T>() => Throw( typeof( T ) );
		#endregion
	}
}
