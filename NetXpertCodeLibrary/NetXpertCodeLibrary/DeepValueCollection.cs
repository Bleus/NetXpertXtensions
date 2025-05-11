using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using NetXpertCodeLibrary.ConfigManagement;

namespace NetXpertCodeLibrary
{
	public abstract class DeepValueFoundation
	{
		protected class GroupChain
		{
			#region Properties
			private string _root = "", _path = "", _key = "";
			#endregion

			#region Constructor
			public GroupChain( string root = "", string path = "", string key = "" )
			{
				Root = root;
				Path = path;
				Key = key;
			}
			#endregion

			#region Accessors
			public string Root
			{
				get => this._root;
				set
				{
					if ( !string.IsNullOrWhiteSpace( value ) && Regex.IsMatch( value, @"^[a-zA-Z][\w]*[a-zA-Z0-9]\.?$" ) )
						this._root = value.TrimEnd( new char[] { '.' } );
				}
			}

			public string Path
			{
				get => this._path;
				set
				{
					if ( !string.IsNullOrWhiteSpace( value ) && Regex.IsMatch( value, @"^([a-zA-Z][\w]*[a-zA-Z0-9]\.)+$" ) )
						this._path = value.TrimEnd( new char[] { '.' } );
				}
			}

			public string Key
			{
				get => this._key;
				set
				{
					if ( !string.IsNullOrWhiteSpace( value ) && Regex.IsMatch( value, @"^[a-zA-Z][\w]*[a-zA-Z0-9]$" ) )
						this._path = value;
				}
			}

			public bool HasRoot => this._root.Length > 0;

			public bool HasPath => this._path.Length > 0;

			public bool HasKey => this._key.Length > 0;
			#endregion

			#region Operators
			public static implicit operator GroupChain( string source ) =>
				string.IsNullOrWhiteSpace( source ) ? new GroupChain() : CliFoundation.ParseGroupChain( source );

			public static implicit operator string( GroupChain source ) => source is null ? "" : source.ToString();
			#endregion

			#region Methods
			public override string ToString() => $"{Root}.{Path}.{Key}";
			#endregion
		}

		#region Properties
		private readonly string _name = "";
		private string _comment = null;
		public const string GROUP_PATH = /* language=regex */ @"^((?:[a-zA-Z][\w]*[a-zA-Z0-9])+(\.|$))+";
		#endregion

		#region Constructor
		protected DeepValueFoundation( string name )
		{
			if ( string.IsNullOrWhiteSpace( name ) || !Regex.IsMatch( name, @"^[a-zA-Z][\w]*[a-zA-Z0-9]+$" ) )
				throw new ArgumentException( $"The specified object name, `{name}' is not acceptable." );

			this._name = name;
		}
		#endregion

		#region Accessors
		public int Count => 0;

		protected string Name => this._name;

		/// <summary>Gets / Sets the comment string for this object.</summary>
		public string Comment
		{
			get => IniLineItem.IsValidComment( this._comment ) ? this._comment : "";
			set { if ( IniLineItem.IsValidComment( value ) ) { this._comment = value; } }
		}
		#endregion

		#region Methods
		/// <summary>Parses a dot-separated string of names into a Root, a path, and a key.</summary>
		/// <returns>A GroupChain struct with the Root, Path and Key values parsed from the supplied string.</returns>
		protected static GroupChain ParseGroupChain( string groupChain )
		{
			GroupChain result = new GroupChain();

			if ( ValidateGroupChain( groupChain ) )
			{
				MatchCollection matches = Regex.Matches( groupChain, GROUP_PATH );
				switch ( matches.Count )
				{
					case 1:
						if ( matches[ 0 ].Value.EndsWith( "." ) )
							result.Root = matches[ 0 ].Value;
						else
							result.Key = matches[ 0 ].Value;
						break;
					case 2:
						// Will fail if matches[0].Value doesn't end with a period, but that shouldn't be possible due to the pattern!
						result.Root = matches[ 0 ].Value;
						if ( matches[ 1 ].Value.EndsWith( "." ) )
							result.Path = matches[ 1 ].Value;
						else
							result.Key = matches[ 1 ].Value;
						break;
					default:
						result.Root = matches[ 0 ].Value;
						for ( int i = 1; i < matches.Count; i++ )
							if ( matches[ i ].Value.EndsWith( "." ) )
								result.Path = $"{result.Path}.{matches[ i ].Value}";
							else
								result.Key = matches[ i ].Value;
						break;
				}

			}
			return result;
		}

		public static bool ValidateGroupChain( string test ) =>
			!string.IsNullOrWhiteSpace( test ) && Regex.IsMatch( test, GROUP_PATH );
		#endregion
	}

	public class DeepValueItem<T,Y> : DeepValueFoundation
	{
		public DeepValueItem( string name, T keyName, Y value ) : base( name ) { }
	}

	public class DeepValueCollection<T,Y>
	{
	}
}
