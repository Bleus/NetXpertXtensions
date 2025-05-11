using System.Text.RegularExpressions;

namespace NetXpertExtensions.Classes
{
	#nullable disable

	public abstract partial class PolyVarFoundation
	{
		#region Properties
		protected string _value = string.Empty;
		protected Type _dataType = typeof(string);
		#endregion

		#region Constructors
		public PolyVarFoundation() { }

		protected PolyVarFoundation( object value, Type t )
		{
			this._value = (value is null) ? string.Empty : value.ToString();
			this._dataType = t;
		}

		protected PolyVarFoundation( string value )
		{
			this._value = string.IsNullOrEmpty( value ) ? string.Empty : value;
			this._dataType= typeof(string);
		}
		#endregion

		#region Accessors
		public int Length => this._value.Length;

		public string Value
		{
			get => this._value;
			set => this._value = string.IsNullOrEmpty( value ) ? string.Empty : value;
		}
		#endregion

		#region Operators
		//public static implicit operator PolyVarFoundation( string source )
		//{
		//	Type t = DetectType( source, true );
		//	if ( t == typeof( string ) ) return new PolyVar( source );

		//	PolyVarFoundation result = Activator.CreateInstance( )
		//}
		#endregion

		#region Methods

		/// <summary>Facilitates accessing the value of the object as a variety of types.</summary>
		/// <returns>The value of this object as the type indicated by the Type parameter.</returns>
		/// <exception cref="InvalidCastException">If the supplied type isn't recognized.</exception>
		/// <remarks>
		/// Supported types: <i>sbyte</i>, <i>short</i>, <i>int</i>, <i>long</i>, <i>byte</i>, <i>ushort</i>, <i>uint</i>, <i>ulong</i>, <i>double</i>, 
		/// <i>float</i>, <i>decimal</i>, <i>string</i>, <i>char</i>, <i>bool</i>, <i>char[]</i>, <i>byte[]</i>, <i>Size/Point</i>, <i>SizeF/PointF</i></remarks>
		protected dynamic As<T>()
		{
			if ( typeof( T ) == typeof( string ) ) return _value;

			string value = _value;
			T var = (T)Activator.CreateInstance( typeof( T ) );
			if ( var is not null )
				switch ( var )
				{
					case sbyte:
					case short:
					case int:
					case long:
						value = Regex.Replace( value, @"[^-\d]", "" );
						return Regex.IsMatch( value, @"-?[\d]+$", RegexOptions.None ) ?
							var switch
							{
								int => int.Parse( value ),
								short => short.Parse( value ),
								sbyte => sbyte.Parse( value ),
								long => long.Parse( value ),
								_ => default( T )
							} : default( T );
					case byte:
					case ushort:
					case uint:
					case ulong:
						value = Regex.Replace( value, @"[^\d]", "" );
						return Regex.IsMatch( value, @"[\d]+$", RegexOptions.None ) ?
							var switch
							{
								uint => uint.Parse( value ),
								ushort => ushort.Parse( value ),
								byte => byte.Parse( value ),
								ulong => ulong.Parse( value ),
								_ => default( T )
							} : default( T );
					case decimal:
					case float:
					case double:
						value = Regex.Replace( value, @"[^-.\d]", "" );
						return Regex.IsMatch( value, @"-?[\d]+[.][\d]*$", RegexOptions.None ) ?
							var switch
							{
								decimal => decimal.Parse( value ),
								float => float.Parse( value ),
								double => double.Parse( value ),
								_ => default( T )
							} : default( T );
					case bool:
						return BooleanValueValidation().IsMatch( value ) && BooleanValueParser().IsMatch( value );
					case DateTime:
						return DateTime.TryParse( value, out DateTime d ) ? d : DateTime.Now;
					case char:
						return char.Parse( value );
					case char[]:
						return value.ToCharArray();
					case byte[]:
						// If the underlying string is a valid Base-64 string, convert that to a byte-array
						// and return it, otherwise just convert the unencrypted version of the value to a byte-array.
						return
							string.IsNullOrEmpty( value )
							? Array.Empty<byte>()
							: (Base64Validation().IsMatch( value ) ? Convert.FromBase64String( value ) : value.ToByteArray());
					case Point:
					case Size:
						Match m1 = PointValidation().Match( value );
						if ( m1.Success )
						{
							int x = m1.Groups[ "x" ].Success ? int.Parse( m1.Groups[ "x" ].Value ) : int.MinValue,
								y = m1.Groups[ "y" ].Success ? int.Parse( m1.Groups[ "y" ].Value ) : int.MinValue;

							return typeof( T ) == typeof( Point ) ? new Point( x, y ) : new Size( x, y );
						}
						break;
					case PointF:
					case SizeF:
						Match m2 = PointFValidation().Match( value );
						if ( m2.Success )
						{
							float x = m2.Groups[ "x" ].Success ? float.Parse( m2.Groups[ "x" ].Value ) : float.MinValue,
								  y = m2.Groups[ "y" ].Success ? float.Parse( m2.Groups[ "y" ].Value ) : float.MinValue;

							return typeof( T ) == typeof( PointF ) ? new PointF( x, y ) : new SizeF( x, y );
						}
						break;
				}

			throw new InvalidCastException( $"The specified 'type' is not recognized. (\x22{(typeof( T )).Name}\x22)" );
		}

		/// <summary>Reports on whether a speficied <seealso cref="Type"/> is supported.</summary>
		/// <param name="t">The C# <seealso cref="Type"/>To validate.</param>
		/// <returns><b>TRUE</b> if the provided type is supported by <seealso cref="IniLineValueFoundation"/></returns>
		protected static bool IsValidDataType( Type t ) =>
			//t == typeof( string ) ||
			t == typeof( int ) ||
			t == typeof( sbyte ) ||
			t == typeof( short ) ||
			t == typeof( long ) ||
			t == typeof( uint ) ||
			t == typeof( byte ) ||
			t == typeof( ushort ) ||
			t == typeof( ulong ) ||
			t == typeof( decimal ) ||
			t == typeof( float ) ||
			t == typeof( double ) ||
			t == typeof( bool ) ||
			t == typeof( DateTime ) ||
			t == typeof( Point ) || t == typeof( Size ) ||
			t == typeof( PointF ) || t == typeof( SizeF );

		/// <summary>Reports on whether a specified generic <seealso cref="Type"/> is supported.</summary>
		/// <typeparam name="T">The generic type to validate.</typeparam>
		/// <returns><b>TRUE</b> if the type is supported by <seealso cref="IniLineValueFoundation"/></returns>
		protected static bool IsValidDataType<T>() => IsValidDataType( typeof( T ) );

		/// <summary>Endeavours to identify an appropriate type for the kind of data that was passed.</summary>
		/// <param name="data">A string containing the raw data to analyse.</param>
		/// <param name="goDeep">If set <b>true</b>, the routine does more thorough testing and attempts to return a more accurate/specific type. 
		/// Otherwise only superficial testing is performed and perfunctory data types are returned (<seealso cref="Decimal"/>,
		/// <seealso cref="int"/>, <seealso cref="uint"/> and <seealso cref="string"/>).</param>
		/// <returns>The best determination of the type of data that was passed, according to the depth of inspection requested.</returns>
		/// <remarks>
		/// Performing a 'Deep' inspection will determine if the value is a number or a string.<br/><br/>For numbers:
		/// First it determines if it has a decimal or not, if not, Integers are range checked and the smallest system type that's 
		/// capable of holding the passed value will be returned.<br/><br/>
		/// Negative integers will always return as a <i>signed</i> type while non-negatives will always come back as <i><u>un</u>signed</i>.<br/><br/>
		/// For decimalized values, it will recognize the following alphabetic suffixes: 'M' = <seealso cref="Decimal"/>, 'F' = <seealso cref="float"/>,
		/// 'D' = <seealso cref="double"/> (if none is specified, <seealso cref="decimal"/> will be the default).<br/><br/>
		/// If no number type can be determined, the value will be checked to see if it can be parsed into either <seealso cref="bool"/>, or <seealso cref="DateTime"/>.
		/// Finally, it will check to see if the string is <seealso cref="System.Buffers.Text.Base64"/> encoded and report the return type as 
		/// a <i>byte[]</i> if so.<br/><br/>If no specific type is able to be identified, the default result will be a <seealso cref="string"/>.
		/// </remarks>
		public static Type DetectType( string data, bool goDeep = false )
		{
			if ( !string.IsNullOrWhiteSpace( data ) )
			{
				if ( Regex.IsMatch( data, @"^-?[\d,]+$", RegexOptions.None ) )
				{
					data = data.Replace( ",", "" );
					if ( (data.Length > 0) && (data[ 0 ] == '-') )
					{
						long v = long.Parse( data );
						return goDeep ? v switch
						{
							> int.MaxValue => typeof( long ),
							< int.MinValue => typeof( long ),
							> short.MaxValue => typeof( int ),
							< short.MinValue => typeof( int ),
							> sbyte.MaxValue => typeof( short ),
							< sbyte.MinValue => typeof( short ),
							_ => typeof( sbyte )
						} : typeof( int );
					}
					else
					{
						ulong v = ulong.Parse( data );
						return goDeep ? v switch
						{
							> uint.MaxValue => typeof( ulong ),
							> ushort.MaxValue => typeof( uint ),
							> byte.MaxValue => typeof( ushort ),
							_ => typeof( byte )
						} : typeof( uint );
					}
				}

				if ( Regex.IsMatch( data, @"^-?[\d,]+(?:[.][\d]*)?[mMsSdDfF]?$", RegexOptions.None ) )
				{
					return goDeep ? data.ToLowerInvariant()[ ^1 ] switch
					{

						's' => typeof( float ),
						'f' => typeof( float ),
						'd' => typeof( double ),
						_ => typeof( decimal ),
					} : typeof( decimal );
				}

				if ( goDeep )
				{
					if ( PointValidation().IsMatch( data ) )
						return typeof( Point );

					if ( PointFValidation().IsMatch( data ) )
						return typeof( PointF );

					if ( Base64Validation().IsMatch( data ) ) return typeof( byte[] );

					if ( DateTime.TryParse( data, out _ ) ) return typeof( DateTime );

					if ( BooleanValueValidation().IsMatch( data ) ) return typeof( bool );
				}
			}
			return typeof( string );
		}

		public T ToEnum<T>() where T : Enum => this._value.ToEnum<T>();

		[GeneratedRegex( @"^(?<pre>[\^])(?<content>[\s\S]+)(?<post>[$])$", RegexOptions.None )]
		private static partial Regex GenerateQuotesCheck();

		[GeneratedRegex( @"^(?<data>[\s\S]*)?(?<chaff>[\s]+(?<comment>(##|//)[\s\S]*)?)?$" )]
		private static partial Regex QuotelessLineParser();

		[GeneratedRegex( "^([y1tn0f]|On|Off|True|False|Yes|No)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant )]
		private static partial Regex BooleanValueValidation();

		[GeneratedRegex( "^([y1t]|On|True|Yes)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant )]
		private static partial Regex BooleanValueParser();

		[GeneratedRegex( "^(?:[a-z\\d+/]{4})*(?:[a-z\\d+/]{2}==|[a-z\\d+/]{3}=)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant )]
		private static partial Regex Base64Validation();

		[GeneratedRegex( @"^[(\[{][\s]*(?<x>[\d]+[.](?:[\d]*)?)[\s]*[,;][\s]*(?<y>[\d]+[.](?:[\d]*)?)[\s]*[}\])]$", RegexOptions.None )]
		private static partial Regex PointFValidation();

		[GeneratedRegex( @"^[(\[{][\s]*(?<x>[\d]+)[\s]*[,;][\s]*(?<y>[\d]+)[\s]*[}\])]$", RegexOptions.None )]
		private static partial Regex PointValidation();
		#endregion
	}

	public class PolyVar<T> : PolyVarFoundation
	{
		#region Constructors
		public PolyVar() : base( "", typeof( T ) )
		{
			if ( !IsValidDataType<T>() )
				throw new TypeInitializationException( typeof( T ).FullName, new( $"The specified type (\x22{typeof( T ).FullName}\x22) is not supported!" ) );

			this.Value = default( T );
		}

		public PolyVar( string source ) : base( source, typeof( object ) )
		{
			if (string.IsNullOrEmpty(source))
			{
				this.Value = default(T);
				this._dataType = typeof( T );
				return;
			}

			Type t = DetectType( source );
			if ( t == typeof( T ) ) 
			{
				this._value = source;
				return;
			}

			throw new InvalidDataException( $"The data provided (\x22{source}\x22) could not be mapped to the specified type: {typeof( T )}." );
		}

		public PolyVar( T source ) : base( string.Empty, typeof( T ) )
		{
			if ( !IsValidDataType<T>() )
				throw new TypeInitializationException( typeof( T ).FullName, new( $"The specified type (\x22{typeof( T ).FullName}\x22) is not supported!" ) );

			this.Value = source;
		}
		#endregion

		#region Operators
		#endregion

		#region Accessors
		new public T Value
		{
			set => this._value = value is null ? string.Empty : value.ToString();
			get => As<T>();
		}
		#endregion

		#region Methods
		#endregion
	}

	public class PolyVar : PolyVarFoundation
	{
		public PolyVar() : base( string.Empty, typeof(string)) { }

		public PolyVar(string source) : base(source, typeof(string)) { }

		public static implicit operator string( PolyVar instance ) => instance is null ? string.Empty : instance.Value;
		public static implicit operator PolyVar( string data ) => string.IsNullOrEmpty( data ) ? new PolyVar() : new PolyVar( data );
	}
}
