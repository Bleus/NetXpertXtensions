using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NetXpertCodeLibrary.ConfigManagement;
using NetXpertCodeLibrary.Extensions;

namespace NetXpertCodeLibrary.ConsoleFunctions
{
	public sealed class AliasDefinition
	{
		private string _alias = "";
		private string _value = "";

		public AliasDefinition( string alias, string value = "" )
		{
			Alias = alias;
			Value = value;
		}

		public string Alias
		{
			get => _alias;
			set
			{
				if ( !string.IsNullOrWhiteSpace( value ) && Regex.IsMatch( value, @"^[a-zA-Z][a-zA-Z0-9]{3,}$" ) && (_alias == "") )
					_alias = value.ToUpperInvariant();
			}
		}

		public string Value // ¶
		{ 
			get => _value.Replace( "\x0182", "\"" );
			set
			{
				if ( Regex.IsMatch( value, @"\\x[\d]{1,2}" ) )
				{
					MatchCollection matches = Regex.Matches( value, @"(\\x(?<ascii>[\d]{1,2}))" );
					foreach ( Match mt in matches )
					{
						string ascii = ((char)int.Parse( $"{mt.Groups[ "ascii" ].Value}", System.Globalization.NumberStyles.AllowHexSpecifier )).ToString();
						value = value.Replace( mt.Groups[ 0 ].Value, ascii );
					}
				}
				_value = value.Replace( "\"", "\x0182" );
			}
		}

		public static AliasDefinition Parse( string source )
		{
			Regex pattern = new Regex( @"^[\s]*(?:(?<alias>[a-z][a-z0-9]{3,7})[=:])(?<payload>(?:""(?<value>[^""\t\f\v\x00-\x1a]+)?"")|"""")?$", RegexOptions.IgnoreCase );
			if ( pattern.IsMatch( source ) )
			{
				Match m = pattern.Match( source );

				string alias = m.Groups[ "alias" ].Value,
					value = m.Groups[ "value" ].Value;

				if ( !string.IsNullOrWhiteSpace( alias ) )
					return new AliasDefinition( alias, value );
			}
			return null;
		}

		public override string ToString() =>
			this.Alias + "=\"" + _value + "\"";
	}

	public sealed class AliasCollection : IEnumerator<AliasDefinition>
	{
		#region Properties
		private List<AliasDefinition> _aliases = new List<AliasDefinition>();
		private int _limit = 10; // Maximum allowed aliases.
		private int _position = 0;
		#endregion

		#region Constructors
		public AliasCollection(int limit) =>
			_limit = limit.InRange( 255 ) ? limit : 10;
		#endregion

		#region Accessors
		public int Count => _aliases.Count;

		public int Limit => _limit;

		public AliasDefinition this[string alias]
		{
			get
			{
				int i = IndexOf( alias );
				return (i < 0) ? null : _aliases[ i ];
			}
		}

		public  AliasDefinition this[ int index ] => _aliases[ index ];

		AliasDefinition IEnumerator<AliasDefinition>.Current => this._aliases[ this._position ];

		object IEnumerator.Current => this._aliases[ this._position ];
		#endregion

		#region Operators
		public static implicit operator IniMultiString(AliasCollection source)
		{
			IniMultiString result = new IniMultiString();
			foreach ( AliasDefinition ad in source )
				result.Add( ad.ToString() );
			return result;
		}
		#endregion

		#region Methods
		private int IndexOf( string alias )
		{
			int i = -1;
			if (!string.IsNullOrWhiteSpace(alias))
				while ( (++i < Count) && !alias.Equals( _aliases[ i ].Alias, StringComparison.OrdinalIgnoreCase ) ) ;

			return (i < Count) ? i : -1;
		}

		public void Add( AliasDefinition alias )
		{
			if ( Count < Limit )
			{
				if ( !(alias is null) )
				{
					int i = IndexOf( alias.Alias );
					if ( i < 0 )
						this._aliases.Add( alias );
					else
						this._aliases[ i ] = alias;
				}
			}
		}

		public void AddRange( AliasDefinition[] aliases)
		{
			if ( !(aliases is null) && (aliases.Length > 0))
			{
				foreach ( AliasDefinition ad in aliases )
					this.Add( ad );

				this.Sort();
			}
		}

		public void AddRange( AliasCollection aliases ) =>
			AddRange( aliases.ToArray() );

		public void Remove( string alias )
		{
			if ( !string.IsNullOrWhiteSpace( alias ) )
			{
				int i = IndexOf( alias );
				if ( i >= 0 )
					this._aliases.RemoveAt( i );
			}
		}

		public void Clear() => this._aliases.Clear();

		public void Sort() =>
			this._aliases.Sort( ( x, y ) => x.Alias.CompareTo( y.Alias ) );

		public bool HasAlias( string alias ) =>
			IndexOf( alias ) >= 0;

		public AliasDefinition[] ToArray()
		{
			this.Sort();
			return this._aliases.ToArray();
		}

		public static AliasCollection Parse( string source )
		{
			AliasCollection result = new AliasCollection(-1);
			if ( !string.IsNullOrWhiteSpace( source ) )
			{
				Regex pattern = new Regex( @"(?<=[\s]|^)(?:(?<alias>[a-z][a-z0-9]{3,7})[=:])(?<payload>(?:""(?<value>[^""\t\f\v\x00-\x1a]+)?""))?", RegexOptions.IgnoreCase );
				if ( pattern.IsMatch( source ) )
				{
					foreach ( Match m in pattern.Matches( source ) )
					{
						string alias = m.Groups[ "alias" ].Value,
							value = m.Groups[ "value" ].Value.Replace( "\x2302", "`" );

						if ( !string.IsNullOrWhiteSpace( alias ) )
							result.Add( new AliasDefinition( alias, value ) );
					}
				}
			}
			return result;
		}

		public override string ToString()
		{
			string result = "[ ";
			foreach ( AliasDefinition ad in this )
				result += ((result.Length > 2) ? ", " : "") + "`" + ad.ToString().Replace( "`", "\x2302" ) + "`"; // ⌂

			return result + " ]";
		}

		#region IEnumerator support
		public IEnumerator<AliasDefinition> GetEnumerator() => this._aliases.GetEnumerator();

		bool IEnumerator.MoveNext() => (++this._position) < this.Count;

		void IEnumerator.Reset() => this._position = 0;

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
		void IDisposable.Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose( true );
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
		#endregion
		#endregion
	}
}
