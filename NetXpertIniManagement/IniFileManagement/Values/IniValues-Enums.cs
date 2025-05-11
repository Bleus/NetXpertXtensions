using System.Text.RegularExpressions;

namespace IniFileManagement.Values
{
	public sealed partial class IniLineEnumValue<T> : IniLineValueFoundation where T : struct, IConvertible
	{
		#region Constructors
		public IniLineEnumValue( IniFileMgmt root ) : base(root) => Value = DefaultValue;

		public IniLineEnumValue( IniFileMgmt root, QuoteTypes quoteType = QuoteTypes.None, char[]? quoteChars = null )
			: base( root, quoteType, quoteChars ) => Value = DefaultValue;

		public IniLineEnumValue( IniFileMgmt root, string value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, quoteType, customQuotes ) => Value = Parse( value );

		public IniLineEnumValue( IniFileMgmt root, T value, QuoteTypes quoteType = QuoteTypes.None, char[]? customQuotes = null )
			: base( root, QuoteTypes.None, customQuotes ) => Value = value;
		#endregion

		#region Accessors
		public T Value
		{
			get => Parse( RawValue );
			set => RawValue = ValueAsString( value );
		}

		protected override dynamic DefaultValue => default(T);

		protected override Type DataType => typeof( T );
		#endregion

		#region Methods
		private T Parse( string value )
		{
			if (!string.IsNullOrWhiteSpace( value ) && Enum.TryParse<T>( value, out T result ))
				return result;

			throw CantParseException();
		}

		private static string ValueAsString( T value ) => value.ToString();

		protected override bool Validate( string value ) =>
			!string.IsNullOrWhiteSpace( value ) && Enum.TryParse<T>( value, out _ );

		protected override Regex ValidateSource() => throw new NotImplementedException();
		#endregion
	}
}
