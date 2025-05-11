using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NetXpertExtensions;

namespace NetXpertCodeLibrary.ConfigManagement
{
	/// <summary>Facilitates working with an array of strings, as either an array, or as a single-line string that's compatible with the IniFile system.</summary>
	public sealed class IniMultiString : IEnumerator<string>
	{
		#region Properties
		private List<string> _values = new();
		private readonly static string DATA_PATTERN = /* language=regex */ @"(?:`(?<item>[^`\v\x00-\x1a]*)`)";
		private readonly static string VALIDATION_PATTERN = /* language=regex */
			@"^(\[[\s]*)(?<values>(?:[\s]*(?:`[^`\v\x00-\x1a]*`)[\r\n ,;]+)*)([\s]*\];?)$";
		private int _position = 0;
		#endregion

		#region Constructors
		public IniMultiString( IniMultiString source )
		{
			if (!(source is null) && (source.Count > 0))
				AddRange( source.ToArray() );
		}

		public IniMultiString( string source ) =>
			Add( source );

		public IniMultiString( string[] source ) =>
			AddRange( source );

		public IniMultiString() { }
		#endregion

		#region Operators
		public static implicit operator IniMultiString( string source ) => new ( (source is null) ? "" : source.Trim() );
		public static implicit operator string( IniMultiString source ) => (source is null) ? "[ ];" : source.ToString();
		public static implicit operator IniMultiString( string[] source ) => new ( source );
		public static implicit operator string[]( IniMultiString source ) => source.ToArray();

		public static IniMultiString operator +( IniMultiString left, IniMultiString right )
		{
			if ( left is null ) return right;
			if ( right is null ) return left;
			IniMultiString add = new( left );
			add.AddRange( right );
			return add;
		}

		public static IniMultiString operator +( IniMultiString left, string[] right )
		{
			if ( left is null ) return new( right );
			if ( (right is null) || (right.Length == 0) ) return left;
			IniMultiString add = new( left );
			add.AddRange( right );
			return add;
		}

		public static IniMultiString operator +(IniMultiString left, string right)
		{
			if ( left is null ) return new( right );
			if ( string.IsNullOrEmpty( right ) ) return left;
			IniMultiString add = new( left );
			add.Add( right );
			return add;
		}
		#endregion

		#region Accessors
		public string this[int index]
		{
			get => RangeCheck( index, false ) ? this._values[ index ] : "";
			set
			{
				if ( RangeCheck(index, false ) )
					this._values[ index ] = value;
			}
		}

		public int Count => this._values.Count;
		#endregion

		#region Methods
		private bool RangeCheck(int x, bool suppressException = true)
		{
			bool result = (x < 0) || (x >= Count);

			if ( result && suppressException )
				return result;

			throw new ArgumentOutOfRangeException( 
				"$1 is out of range for this object (0$2)."
				.Replace( new object[] { x, (Count > 0) ? "-" + Count.ToString() : "" } )
			);
		}

		private string[] Parse( string source )
		{
			List<string> results = new();
			if (Validate(source))
			{
				MatchCollection matches = Regex.Matches( source, DATA_PATTERN, RegexOptions.IgnoreCase );
				foreach ( Match m in matches )
					if ( m.Groups[ "item" ].Success )
						results.Add( m.Groups[ "item" ].Value.Replace( '\x0167', '`' ) ); // ŧ
			}
			return results.ToArray();
		}

		public void Add( string item )
		{
			if ( Validate( item ) ) // If the contents of 'item' conform to the correct schema, decode it and add it's elements to our own.
				this._values.AddRange( Parse( item ) );
			else // If the contents aren't in a recognizable form, just add them as another item as they are.
				this._values.Add( item );
		}

		public void AddRange( string[] items )
		{
			if (!(items is null) && (items.Length > 0))
				foreach (string s in items)
					Add( s );
		}

		public void AddRange( IniMultiString source ) =>
			AddRange( source.ToArray() );

		public override string ToString()
		{
			if ( Count == 0 ) return "[];";
			string result = "[ ";
			foreach ( string s in this._values )
				result += ((result.Length > 2) ? ", `" : "`") + s.Replace( '`', '\x0167' ) + "`"; // ŧ
			return result + " ];";
		}

		public string[] ToArray() =>
			this._values.ToArray();

		public static bool Validate( string source ) =>
			!string.IsNullOrWhiteSpace( source) && Regex.IsMatch( source, VALIDATION_PATTERN, RegexOptions.Singleline );
		#endregion

		#region IEnumerator Support
		string IEnumerator<string>.Current => this[ this._position ];

		object IEnumerator.Current => this._values[ this._position ];
		public IEnumerator GetEnumerator() =>
			this._values.GetEnumerator();

		public bool MoveNext() =>
			(++this._position) < this.Count;

		public void Reset() =>
			this._position = 0;

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		private void Dispose( bool disposing )
		{
			if ( !disposedValue )
			{
				if ( disposing )
				{
					// TODO: dispose managed state (managed objects).
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~AppletParameters() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose( true );
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
		#endregion
	}

	/// <summary>Manages an IniLineItem for an array of strings.</summary>
	public class IniMultiStringItem : IniLineItem
	{
		protected static readonly string PATTERN = /* language=regex */
			@"^[\s]*(?<name>[a-z][a-z_0-9]+<[Mm]>[?]?)[\s]*[=:][\s]*(?<value>[\[][\s]*([^\]]*)[\s]*[\];?]).*$";

		#region Constructors
		public IniMultiStringItem( string key, string value, bool encrypt = false, string comment = "", bool enabled = true )
			: base( "test", "", encrypt, comment, enabled ) 
		{
			this.Key = key;
			this.Value = value;
		}

		public IniMultiStringItem( IniLineItem source ) : base( source )
		{
			if ( !IniMultiString.Validate( source.Value ) )
				throw new ArgumentException( "The format of the provided data is incompatible with a Multi-string object." );

			this.Key = source.Key;
			this._enabled = source.Enabled;
			this._encrypt = source.Encrypted;
			this.Value = source.Value;
			this._comment = source.Comment;
		}

		protected IniMultiStringItem() : base() { this._value = "[]"; }
		#endregion

		#region Operators
		#endregion

		#region Accessors
		new public string Key
		{
			get => Regex.Replace( base.Key, @"[?]$", "") + "<M>" + (Encrypted ? "?" : "");
			set => base.Key = Regex.Replace( value, @"(<[Mm]>)([?])?$", "$2" );
		}

		new public IniMultiString Value
		{
			get => new( base.Value );
			set => base.Value = value.ToString();
		}
		#endregion

		#region Methods
		#endregion

		#region Static Methods
		public static bool Validate( string value ) =>
			!string.IsNullOrWhiteSpace( value ) &&
			Regex.IsMatch( value.Trim(), PATTERN, RegexOptions.IgnoreCase );

		new public static IniMultiStringItem Parse( string source )
		{
			if (Validate( source ))
			{
				Match m = Regex.Matches( source, PATTERN, RegexOptions.IgnoreCase )[0];
				if (m.Groups["name"].Success && m.Groups["value"].Success)
				{
					string key = m.Groups[ "name" ].Value.Trim(), value = m.Groups[ "value" ].Value.Trim();
					return new( key, value, key.EndsWith( "?" ) );
				}
			}
			return null;
		}
		#endregion
	}
}
