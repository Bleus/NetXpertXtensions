using System.Text;
using System.Text.RegularExpressions;
using NetXpertExtensions;
using IniFileManagement.Values;
using static IniFileManagement.Values.IniLineValueFoundation;

namespace IniFileManagement
{
	public sealed partial class IniGroupMgmt //<T,U> where U : IniLineValueBase where T : IniLine<U>
	{
		#region Properties
		private string _source = "";
		private string _name = "";
		private string _preamble = "";
		private string _desc = "";
		private readonly IniFileMgmt _root;
		private readonly List<IniLineBase> _lines = [];

		/* language = regex */
		private const string GROUP_PATTERN = @"([\r\n][\s]*\[(?<name>[a-zA-Z]+[\w-]*)\]:(?<desc>[^\r\n]*)(?<preamble>[\s\S]*?))[\r\n][\s]*[{](?<lines>[\s\S]*?)[\r\n][\s]*[}]";
		#endregion

		#region Constructors
		private IniGroupMgmt( IniFileMgmt root )
		{
			ArgumentNullException.ThrowIfNull( root );
			this._root = root;
		}

		public IniGroupMgmt( IniFileMgmt root, string name, string desc = "" )
		{
			ArgumentNullException.ThrowIfNull( root );
			if (string.IsNullOrWhiteSpace( name ) || !ValidateGroupName().IsMatch( name ))
				throw new ArgumentException( $"The supplied group name, \x22{name}\x22 isn't a valid group name!" );

			this._root = root;
			this._name = name;
			this._source = $"[{name}]" + (!string.IsNullOrWhiteSpace( desc ) ? $"\x22{desc}\x22" : "");
		}
		#endregion

		#region Accessors
		public IniLineBase? this[ string fieldName ]
		{
			get
			{
				int i = IndexOf( fieldName );
				return (i < 0) ? null : this._lines[ i ];
			}
		}

		public IniFileMgmt Root => this._root;

		public IniEncryptionKey? EncryptionKey => Root.EncryptionKey;

		public string Name
		{
			get => this._name;
			set
			{
				if (Root.ReadOnly) throw IniFileMgmt.ReadOnlyException();

				if (!string.IsNullOrWhiteSpace( value ) && ValidateGroupName().IsMatch( value ))
					this._name = value;
			}
		}

		public int Count => this._lines.Count;

		public int LongestKeySize
		{
			get
			{
				int l = 0;
				foreach (var line in this._lines) l = Math.Max( l, line.Key.Length );
				return l;
			}
		}

		public int LongestValueSize
		{
			get
			{
				int l = 0;
				foreach (var line in this._lines) l = Math.Max( l, line.Value.Length );
				return l;
			}
		}
		#endregion

		#region Methods
		private int IndexOf( string fieldName )
		{
			int i = -1;
			if (!string.IsNullOrWhiteSpace( fieldName ))
				while ((++i < Count) && !this._lines[ i ].Key.Equals( fieldName, StringComparison.OrdinalIgnoreCase )) ;

			return i < Count ? i : -1;
		}

		public IniLineBase CreateLine( string key, string value, IniComment comment = null, QuoteTypes quoteTypes = QuoteTypes.AutoSense, params char[]? customQuotes )
		{
			var line = new IniLine( Root, key, value, comment, quoteTypes, customQuotes );
			this.Add( line );
			return line;
		}

		public IniLineBase CreateLine<T>( string key, T value, IniComment comment = null, QuoteTypes quoteTypes = QuoteTypes.AutoSense, params char[]? customQuotes ) where T : new()
		{
			var line = new IniLine<T>( Root, key, value, comment, quoteTypes, customQuotes );
			this.Add( line );
			return line;
		}

		public void Add( IniLineBase value )
		{
			if (Root.ReadOnly) throw IniFileMgmt.ReadOnlyException();

			int i = IndexOf( value.Key );
			if (i < 0)
				this._lines.Add( value );
			else
				this._lines[ i ] = value;
		}

		public void AddRange( IEnumerable<IniLineBase> items )
		{
			if (Root.ReadOnly) throw IniFileMgmt.ReadOnlyException();

			foreach (var line in items)
			{
				int i = IndexOf( line.Key );
				if (i < 0)
					this._lines.Add( line );
				else
					this._lines[ i ] = line;
			}
		}

		//public string Compile( bool autoAlign = false, int valueAlign = -1, int commentAlign = -1 )
		//{
		//	string result = $"[{this.Name}]:{(this._desc.Length > 0 ? $" \x22{this._desc}\x22" : "")}\r\n";
		//	if (this._preamble.Length > 0)
		//		result += string.Join( "\r\n", this._preamble.Split( [ '\r', '\n' ], (StringSplitOptions)3 ) ) + "\r\n";

		//	result += "{\r\n";
		//	if (autoAlign)
		//	{
		//		if (valueAlign < 1) valueAlign = LongestKeySize + 2;
		//		if (commentAlign < 1) commentAlign = LongestValueSize + 2;
		//	}

		//	foreach (var line in this._lines)
		//		result += line.Compile( "\t", valueAlign, commentAlign );

		//	return result + "}\r\n";
		//}

		public string Compile( bool autoAlign = false, int valueAlign = -1, int commentAlign = -1 )
		{
			StringBuilder result = new();

			string descPart = this._desc.Length > 0 ? $" \x22{this._desc}\x22" : string.Empty;
			result.AppendLine( $"[{this.Name}]:{descPart}" );

			if (this._preamble.Length > 0)
				result.AppendLine( string.Join( "\r\n", this._preamble.Split( [ '\r', '\n' ], (StringSplitOptions)3 ) ) );

			result.AppendLine( "{" );

			if (autoAlign)
			{
				if (valueAlign < 1) valueAlign = this.LongestKeySize + 2;
				if (commentAlign < 1) commentAlign = this.LongestValueSize + 2;
			}

			foreach (var line in this._lines)
				result.Append( line.Compile( "\t", valueAlign, commentAlign ) );

			result.AppendLine( "}" );
			return result.ToString();
		}

		public bool HasItem( string name ) => IndexOf( name ) >= 0;

		public IniLineBase? Remove( string name )
		{
			if (Root.ReadOnly) throw IniFileMgmt.ReadOnlyException();

			int i = IndexOf( name );
			if (i < 0) return null;
			var line = this._lines[ i ];
			this._lines.RemoveAt( i );
			return line;
		}

		public override string ToString() =>
			$"[{this._name}]: \x22{this._desc}\x22 ({this.Count.Pluralize( "%v item%s", "s", "#,###0" )})";

		public static IniGroupMgmt[] ParseBody( IniFileMgmt root, string source )
		{
			List<IniGroupMgmt> result = [];
			if (!string.IsNullOrWhiteSpace( source ) && IniGroupParser().IsMatch( source ))
			{
				MatchCollection matches = IniGroupParser().Matches( source );
				foreach (Match m in matches)
					if (m.Groups[ "name" ].Success && m.Groups[ "lines" ].Success)
					{
						IniGroupMgmt grp = Parse( root, m.Value );
						if (grp is not null) result.Add( grp );
					}
			}
			return [ .. result ];
		}

		public static IniGroupMgmt? Parse( IniFileMgmt root, string source )
		{
			ArgumentNullException.ThrowIfNull( root, nameof( root ) );
			IniGroupMgmt? grp = null;
			if (!string.IsNullOrWhiteSpace( source ))
			{
				Match m = IniGroupParser().Match( source );
				if (m.Groups[ "name" ].Success && m.Groups[ "lines" ].Success)
				{
					grp = new( root )
					{
						_source = m.Value,
						_name = m.Groups[ "name" ].Value,
						_preamble = m.Groups[ "preamble" ].Success ? m.Groups[ "preamble" ].Value : string.Empty,
						_desc = m.Groups[ "desc" ].Success ? m.Groups[ "desc" ].Value : string.Empty
					};

					if (!string.IsNullOrWhiteSpace( m.Groups[ "lines" ].Value ))
					{
						string[] lines = m.Groups[ "lines" ].Value.Split( [ '\n', '\r' ], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries );
						foreach (string s in lines)
						{
							dynamic? line = IniLineBase.ParseLine( root, s );
							if (line is not null)
								grp._lines.Add( line );
						}
					}
				}

			}
			return grp;
		}

		public IniLineBase[] ToArray() => [ .. this._lines ];
		#endregion

		[GeneratedRegex( @"([\r\n][\s]*\[(?<name>[a-zA-Z]+[\w-]*)\]:[\s]*(?<desc>[^\r\n]*)(?<preamble>[\s\S]*?))[\r\n][\s]*[{](?<lines>[\s\S]*?)[\r\n][\s]*[}]", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture )]
		private static partial Regex IniGroupParser();

		[GeneratedRegex( @"^[a-zA-Z][\w-]+$" )] private static partial Regex ValidateGroupName();
	}
}
