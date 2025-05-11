using NetXpertCodeLibrary.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace NetXpertCodeLibrary.ConsoleFunctions
{
	/// <summary>Used to manage various bits of output from a rule parsing operation.</summary>
	public class FormatRuleData
	{
		#region Properties
		/// <summary>Stores the Color of the data.</summary>
		protected CliColor _color = Con.DefaultColor;

		/// <summary>Stores the data itself.</summary>
		protected string _data = "";

		/// <summary>Defines the number of spaces to use for each tabstop when \t characters are translated.</summary>
		protected int _tabSize = 5;
		#endregion

		#region Constructors
		public FormatRuleData() { }

		public FormatRuleData(string data, CliColor color = null, int tabSize = 5)
		{
			this._data = data;
			this._color = (color is null) ? Con.DefaultColor : color;
			this.TabSize = tabSize;
		}

		public FormatRuleData(string data, ConsoleColor fore, ConsoleColor? back = null, int tabSize = 5)
		{
			this._data = data;
			this.Fore = fore;
			this.Back = (back is null) ? Con.DefaultColor.Back : (ConsoleColor)back;
			this.TabSize = tabSize;
		}
		#endregion

		#region Operators
		// Equivalence is determined stricly according to the value of the DATA element,
		// neither the Color nor the TabSize are considered.
		public static bool operator !=(FormatRuleData left, FormatRuleData right) => !(left == right);
		public static bool operator ==(FormatRuleData left, FormatRuleData right)
		{
			if (left is null) return (right is null);
			if (right is null) return false;
			return left.Data.Equals(right.Data);
		}

		public static bool operator !=(FormatRuleData left, string right) => !(left == right);
		public static bool operator ==(FormatRuleData left, string right)
		{
			if (left is null) return String.IsNullOrEmpty(right);
			if (right is null) return false;
			return left.Data.Equals(right);
		}

		// Facilitates appending a string to a FormatRuleData object via the + operator.
		public static FormatRuleData operator +(FormatRuleData left, string right) =>
			new FormatRuleData(left.Data + right, left.Color, left.TabSize);

		// Facilitates prepending a sting to a FormatRuleData object via the + operator.
		public static FormatRuleData operator +(string left, FormatRuleData right) =>
			new FormatRuleData(left + right.Data, right.Color, right.TabSize);

		public static implicit operator FormatRuleData(string source) => new FormatRuleData( source );
		public static implicit operator FormatRuleData(CliColor source) => new FormatRuleData( "", source );
		public static implicit operator string(FormatRuleData source) => source.Data;
		public static implicit operator CliColor(FormatRuleData source) => source.Color;
		#endregion

		#region Accessors
		/// <summary>Provides Get/Set access to the object's Data.</summary>
		public string Data
		{
			get => this._data;
			set => this._data = value;
		}

		/// <summary>Provides Get/Set access to the object's Fore(groundColor) property.</summary>
		public ConsoleColor Fore
		{
			get => this._color.Fore;
			set => this._color = new CliColor(value, this.Back);
		}

		/// <summary>Provides Get/Set access to the object's Back(groundColor) property.</summary>
		public ConsoleColor Back
		{
			get => this._color.Back;
			set => this._color = new CliColor(this.Fore, value);
		}

		/// <summary>Provides Get/Set access to the object's Color property.</summary>
		public CliColor Color
		{
			get => this._color;
			set => this._color = value;
		}

		/// <summary>Provides a quick means to determine if there's any data here.</summary>
		public bool IsEmpty => (this._data.Length == 0);

		/// <summary>Provides a means to Get or Set the TabSize property after instantiation + initialization.</summary>
		public int TabSize
		{
			get => this._tabSize;
			set => this._tabSize = Math.Max(1, Math.Min(value, 10));
		}
		#endregion

		#region Methods
		public void Write(Encoding encoding = null) => Write(TabSize, encoding);

		public void Write(int tabSize, Encoding encoding = null) => ToCon(this._data, this._color, tabSize, encoding);

		public override string ToString() =>
			"{§§{" + this.Fore.ToString() + "," + this.Back.ToString() + "}" + this.Data + "§§}";

		public override bool Equals(object obj) => base.Equals(obj);

		public override int GetHashCode() => base.GetHashCode();
		#endregion

		#region Static Methods
		/// <summary>Parses a string to derive colour information and data from it.</summary>
		/// <param name="source">The string to parse.</param>
		/// <param name="defaultBack">The default background colour to use when/if one isn't specified.</param>
		/// <returns>A populated FormatRuleData object containing the derived information.</returns>
		public static FormatRuleData Parse(string source, ConsoleColor? defaultBack = null)
		{
			CliColor result = Con.DefaultColor;
			string contents = source;
			MatchCollection colors = new Regex(FormatRule.PATTERN).Matches(source);
			if (colors.Count > 0)
			{
				result.Fore = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), colors[0].Groups["fore"].Value);
				result.Back = (colors[0].Groups["back"].Success) ?
					(ConsoleColor)Enum.Parse(typeof(ConsoleColor), colors[0].Groups["back"].Value) :
					((defaultBack is null) ? Con.DefaultColor.Back : (ConsoleColor)defaultBack);
				contents = colors[0].Groups["text"].Success ? colors[0].Groups["text"].Value : "";
			}
			return new FormatRuleData(contents, result);
		}

		/// <summary>Writes a specified string to the default Console instance with the specified information.</summary>
		/// <param name="what">The data to be written.</param>
		/// <param name="color">The color to style it in.</param>
		/// <param name="tabSize">The tabSize to use when parsing '\t' characters.</param>
		/// <param name="encoding">The Encoding mechanism to apply to the console.</param>
		public static void ToCon(string what, CliColor color, int tabSize, Encoding encoding = null)
		{
			if ((what is null) || (what.Length == 0)) return;
			color.ToConsole();

			// Convert internal tab characters ("\t" | (char)0x09) to appropriate spacings...
			if (what.IndexOf("\t") >= 0)
			{
				int tabIndex = what.IndexOf("\t"),
					size = tabSize - ((Console.CursorLeft + tabIndex) % tabSize);

				if (size != tabSize)
					what = ((tabIndex > 0) ? what.Substring(0, tabIndex) : "") + "".PadLeft(size, ' ') + what.Substring(tabIndex + 1);

				while (what.IndexOf("\t") >= 0)
					what = what.Replace("\t", "".PadRight(tabSize));
			}

			if (what.EndsWith("\r\n") || what.EndsWith("\n\r"))
			{
				if (what.Length == 2) what = " \r\n";
				what = what.Substring(0, what.Length - 2).PadRight(Console.BufferWidth - (what.Length - 2 + Console.CursorLeft), ' ') + "\r\n";
			}

			Encoding baseEncoding = Console.OutputEncoding;
			Console.OutputEncoding = (encoding is null) ? Encoding.UTF8 : encoding;
			Console.Write(what);
			Console.OutputEncoding = baseEncoding;
		}
		#endregion
	}

	/// <summary>Used to define / declare a single text-formatting rule.</summary>
	/// <remarks>
	/// Encoded Format:		{§§{ foreColor, backColor }string to apply formatting to.§§}
	/// </remarks>
	public class FormatRule
	{
		#region Properties
		/// <summary>The RegEx pattern that defines what our markup looks like.</summary>
		public const string PATTERN = @"\{§§\{[ \t]*(?<fore>[a-zA-Z]{3,16})(?:(?:[ \t]*[,;][ \t]*)(?<back>[a-zA-Z]{3,16}))?[ \t]*}(?<text>[^§]*)§§\}";

		/// <summary>Stores the Regex object that defines our rule.</summary>
		protected Regex _pattern = new Regex(@"[\s\S]*", RegexOptions.ExplicitCapture);

		/// <summary>Specifies the desired colour matched text will be formatted-to.</summary>
		protected CliColor _color = Con.DefaultColor;

		/// <summary>Stores a name for the rule.</summary>
		protected string _name = new Guid().ToString();
		#endregion

		#region Constructors
		public FormatRule(string pattern, CliColor color, string name = "", RegexOptions options = RegexOptions.IgnoreCase)
		{
			this._name = string.IsNullOrWhiteSpace(name) ? new Guid().ToString() : name;
			this.Pattern = pattern;
			this.RegexOptions = options;
			this.Color = color;
		}

		public FormatRule(string pattern, ConsoleColor fore, ConsoleColor? back = null, string name = "", RegexOptions options = RegexOptions.IgnoreCase)
		{
			this._name = ((name is null) || (name.Trim().Length == 0)) ? new Guid().ToString() : name;
			this.Pattern = pattern;
			this.RegexOptions = options;
			this.Fore = fore;
			this.Back = (back is null) ? Con.DefaultColor.Back : (ConsoleColor)back;
		}
		#endregion

		#region Accessors
		/// <summary>Facilitates retrieving the rule's name.</summary>
		public string Name
		{
			get => this._name;
			protected set =>
				this._name = string.IsNullOrWhiteSpace(value) ? new Guid().ToString() : Regex.Replace( value, @"[^a-zA-Z0-9]", "" );
		}

		/// <summary>Provides Get/Set access to the Fore(ground) color for matched text.</summary>
		public ConsoleColor Fore
		{
			get => this._color.Fore;
			set => this._color = new CliColor(value, this.Back);
		}

		/// <summary>Provides Get/Set access to the Back(ground) color for matched text.</summary>
		public ConsoleColor Back
		{
			get => this._color.Back;
			set => this._color = new CliColor(this.Fore, value);
		}

		/// <summary>Provides Get/Set access to the Color for matched text.</summary>
		public CliColor Color
		{
			get => this._color;
			set => this._color = value;
		}

		/// <summary>Facilitates interactions with the Regex pattern that defines this rule.</summary>
		public string Pattern
		{
			get
			{
				string value = this._pattern.ToString();
				int start = (value.StartsWith("^") ? 1 : 0) + this.Name.Length + 4,
					length = value.Length - start - (value.EndsWith("$") ? 2 : 1);

				return value.Substring(start, length);
			}
			set
			{
				// converts any capturing groups in the provided pattern to non-capturing groups, they'll only fuck with us otherwise...
				int start = value.StartsWith("^") ? 1 : 0,
					  end = value.Length - start - (value.EndsWith("$") ? 1 : 0);
				string pattern = Regex.Replace(value.Substring(start, end), @"(?:\(\?[!=<Pp]?<[a-zA-Z]*\>)|(?<![\\])\((?![:?=|><])", @"(?:", RegexOptions.IgnoreCase);
				pattern = (start > 0 ? "^" : "") + @"(?<" + this.Name + @">" + pattern + @")" + ((value.Length - end) > start ? "$" : "");
				this._pattern = new Regex(pattern, Regx.Options | RegexOptions.ExplicitCapture);
			}
		}

		/// <summary>Facilitates updating / retrieving the RegexOptions used when parsing with this rule.</summary>
		public RegexOptions RegexOptions
		{
			get => this.Regx.Options;
			set => this._pattern = new Regex(this._pattern.ToString(), value | RegexOptions.ExplicitCapture);
		}

		/// <summary>Facilitates accessing the actual RegEx object used by this rule.</summary>
		public Regex Regx
		{
			get => this._pattern;
			set => this.Pattern = value.ToString();
		}
		#endregion

		#region Methods
		/// <summary>Facilitates replacing a specified string within the source with it's encoded equivalent.</summary>
		/// <param name="source">The string in which to apply the specified encoding.</param>
		/// <returns>The original string with the specified element(s) replaced with encoded equivalents.</returns>
		public string Replace(string source) =>
			Regx.Replace(source, EncodeColor(source, this.Color), 1);

		/// <summary>
		///
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		public string ApplyTo(string source)
		{
			MatchCollection matches = Regx.Matches(source);

			// Loop moves from the end of the string to the beginning to preserve index values.
			for (int i = matches.Count; i > 0;)
			{
				Match match = matches[--i];
				if (match.Groups[Name].Success)
				{
					int start = match.Groups[Name].Index, end = start + match.Groups[Name].Length;
					source = ((start > 0) ? source.Substring(0, start) : "") +
						EncodeColor(match.Groups[Name].Value, this.Color) +
						((end < source.Length) ? source.Substring(end) : "");
				}
			}
			return source;
		}

		public MatchCollection Matches(string test) => Regx.Matches(test);

		public bool IsMatch(string test) => Regx.IsMatch(test);

		#endregion

		#region Static Methods
		/// <summary>Crafts a colour-encoded string for the proviced CliColor and text.</summary>
		/// <param name="source">A string to encode.</param>
		/// <param name="color">A CliColor object to use.</param>
		/// <returns>A colour-encoded string for the proviced CliColor and text.</returns>
		public static string EncodeColor(string source, CliColor color) => "{§§{" + color.Fore.ToString() + "," + color.Back.ToString() + "}" + source + "§§}";

		/// <summary>Crafts a colour-encoded string for the proviced CliColor and text.</summary>
		/// <param name="source">A string to encode.</param>
		/// <param name="fore">A ConsoleColor to use for the foreground.</param>
		/// <param name="back">An optional ConsoleColor to use for the background.</param>
		/// <returns>A colour-encoded string for the proviced fore and background colours and text.</returns>
		public static string EncodeColor(string s, ConsoleColor fore, ConsoleColor? back = null) =>
			EncodeColor(s, new CliColor(fore, (back is null) ? Con.DefaultColor.Back : (ConsoleColor)back));

		/// <summary>Extracts a CliColor object from a provided encoded string.</summary>
		/// <param name="source">A string containing an encoded colour declaration to decipher.</param>
		/// <param name="defaultBack">An optional value to use for the background colour if one isn't provided.</param>
		/// <returns>A new CliColor object populated based on the supplied information.</returns>
		public static CliColor DecodeColor(string source, ConsoleColor? defaultBack = null)
		{
			CliColor result = Con.DefaultColor;
			MatchCollection colors = new Regex(PATTERN).Matches(source);
			if (colors.Count > 0)
			{
				result.Fore = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), colors[0].Groups["fore"].Value);
				result.Back = (colors[0].Groups["back"].Success) ?
					(ConsoleColor)Enum.Parse(typeof(ConsoleColor), colors[0].Groups["back"].Value) :
					((defaultBack is null) ? Con.DefaultColor.Back : (ConsoleColor)defaultBack);
			}
			return result;
		}

		public static FormatRuleData Deconstruct(string source, ConsoleColor? defaultBack = null)
		{
			CliColor result = Con.DefaultColor;
			string contents = source;
			MatchCollection colors = new Regex(PATTERN).Matches(source);
			if (colors.Count > 0)
			{
				result.Fore = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), colors[0].Groups["fore"].Value);
				result.Back = (colors[0].Groups["back"].Success) ?
					(ConsoleColor)Enum.Parse(typeof(ConsoleColor), colors[0].Groups["back"].Value) :
					((defaultBack is null) ? Con.DefaultColor.Back : (ConsoleColor)defaultBack);
				contents = colors[0].Groups["text"].Success ? colors[0].Groups["text"].Value : "";
			}
			return new FormatRuleData(contents, result);
		}
		#endregion
	}

	public class FormatReplaceRule : FormatRule
	{
		#region Properties
		/// <summary>Stores the Regex object that defines the replacement resultour rule.</summary>
		protected string _replacePattern = "";
		#endregion

		#region Constructors
		public FormatReplaceRule(string searchPattern, string replacePattern, CliColor color, string name = "", RegexOptions options = RegexOptions.IgnoreCase)
			: base( searchPattern, color, name, options ) =>
			this._replacePattern = replacePattern;

		public FormatReplaceRule(string searchPattern, string replacePattern, ConsoleColor fore, ConsoleColor? back = null, string name = "", RegexOptions options = RegexOptions.IgnoreCase)
			: base( searchPattern, fore, back, name, options ) =>
			this._replacePattern = replacePattern;
		#endregion

		#region Accessors

		// Rename the Pattern accessor.
		public string SearchPattern => base.Pattern;

		// Hide the Pattern accessor.
		new private string Pattern => base.Pattern;

		public string ReplacePattern
		{
			get => this._replacePattern;
			set => this._replacePattern = value;
		}

		/// <summary>Facilitates interactions with the Regex pattern that defines this rule.</summary>
		//public string Pattern
		//{
		//	get
		//	{
		//		string value = this._pattern.ToString();
		//		int start = (value.StartsWith( "^" ) ? 1 : 0) + this.Name.Length + 4,
		//			length = value.Length - start - (value.EndsWith( "$" ) ? 2 : 1);

		//		return value.Substring( start, length );
		//	}
		//	set
		//	{
		//		// converts any capturing groups in the provided pattern to non-capturing groups, they'll only fuck with us otherwise...
		//		int start = value.StartsWith( "^" ) ? 1 : 0,
		//			  end = value.Length - start - (value.EndsWith( "$" ) ? 1 : 0);
		//		string pattern = Regex.Replace( value.Substring( start, end ), @"(?:\(\?[!=<Pp]?<[a-zA-Z]*\>)|(?<![\\])\((?![:?=|><])", @"(?:", RegexOptions.IgnoreCase );
		//		pattern = (start > 0 ? "^" : "") + @"(?<" + this.Name + @">" + pattern + @")" + ((value.Length - end) > start ? "$" : "");
		//		this._pattern = new Regex( pattern, Regx.Options | RegexOptions.ExplicitCapture );
		//	}
		//}

		/// <summary>Facilitates updating / retrieving the RegexOptions used when parsing with this rule.</summary>
		#endregion

		#region Methods
		/// <summary>Facilitates replacing a specified string within the source with it's encoded equivalent.</summary>
		/// <param name="source">The string in which to apply the specified encoding.</param>
		/// <returns>The original string with the specified element(s) replaced with encoded equivalents.</returns>
		//public string Replace(string source) =>
		//	Regx.Replace( source, EncodeColor( source, this.Color ), 1 );

		/// <summary>
		///
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		//public string ApplyTo(string source)
		//{
		//	MatchCollection matches = Regx.Matches( source );

		//	// Loop moves from the end of the string to the beginning to preserve index values.
		//	for (int i = matches.Count; i > 0;)
		//	{
		//		Match match = matches[ --i ];
		//		if (match.Groups[ Name ].Success)
		//		{
		//			int start = match.Groups[ Name ].Index, end = start + match.Groups[ Name ].Length;
		//			source = ((start > 0) ? source.Substring( 0, start ) : "") +
		//				EncodeColor( match.Groups[ Name ].Value, this.Color ) +
		//				((end < source.Length) ? source.Substring( end ) : "");
		//		}
		//	}
		//	return source;
		//}
		#endregion

		#region Static Methods
		//public static FormatRuleData Deconstruct(string source, ConsoleColor? defaultBack = null)
		//{
		//	CliColor result = CliColor.Default;
		//	string contents = source;
		//	MatchCollection colors = new Regex( PATTERN ).Matches( source );
		//	if (colors.Count > 0)
		//	{
		//		result.Fore = (ConsoleColor)Enum.Parse( typeof( ConsoleColor ), colors[ 0 ].Groups[ "fore" ].Value );
		//		result.Back = (colors[ 0 ].Groups[ "back" ].Success) ?
		//			(ConsoleColor)Enum.Parse( typeof( ConsoleColor ), colors[ 0 ].Groups[ "back" ].Value ) :
		//			((defaultBack is null) ? CliColor.Default.Back : (ConsoleColor)defaultBack);
		//		contents = colors[ 0 ].Groups[ "text" ].Success ? colors[ 0 ].Groups[ "text" ].Value : "";
		//	}
		//	return new FormatRuleData( contents, result );
		//}
		#endregion
	}

	public class FormatRuleCollection : IEnumerator<FormatRule>
	{
		#region Properties
		public const string PATTERN = @"(?:(?<preamble>[^\r\n]*?(?=({§§)))(?<break>" + FormatRule.PATTERN + @"))(?<tail>[^\r\n]*)?$";

		protected List<FormatRule> _classes = new List<FormatRule>();
		private int _position = 0, _tabSize = 5;
		protected CliColor _defaultColor;
		#endregion

		#region Constructors
		public FormatRuleCollection(CliColor defaultColor = null ) =>
			_defaultColor = defaultColor is null ? Con.DefaultColor : defaultColor;

		public FormatRuleCollection(FormatRule newClass, CliColor defaultColor, int newTabSize = 5)
		{
			_defaultColor = defaultColor is null ? Con.DefaultColor : defaultColor;
			Add(newClass); TabSize = newTabSize;
		}

		public FormatRuleCollection(FormatRule[] classList, CliColor defaultColor, int newTabSize = 5)
		{
			_defaultColor = defaultColor is null ? Con.DefaultColor : defaultColor;
			AddRange(classList); TabSize = newTabSize;
		}
		#endregion

		#region Accessors
		public FormatRule this[int index]
		{
			get => this._classes[index];
			set => this._classes[index] = value;
		}

		public FormatRule this[string name]
		{
			get
			{
				int i = IndexOf(name);
				return (i < 0) ? null : this[i];
			}
			set
			{
				int i = IndexOf(name);
				if (i < 0)
					this._classes.Add(value);
				else
					this[i] = value;
			}
		}

		public int TabSize
		{
			get => this._tabSize;
			set => this._tabSize = Math.Max(1, Math.Min(value, 10));
		}

		public int Count => this._classes.Count;

		public CliColor DefaultColor
		{
			get => this._defaultColor;
			set => this._defaultColor = (value is null) ? CliColor.CaptureConsole() : value;
		}

		// IEnumerator Support Accessors...
		FormatRule IEnumerator<FormatRule>.Current => this[this._position];

		object IEnumerator.Current => this[this._position];
		#endregion

		#region Methods
		protected int IndexOf(string name)
		{
			int i = -1; while ((++i < Count) && !this[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ;
			return (i < Count) ? i : -1;
		}

		public void Add(FormatRule newClass) => this[newClass.Name] = newClass;

		public void Add(string pattern, CliColor color = null, RegexOptions options = RegexOptions.None, string name = "") =>
			this.Add(new FormatRule(pattern, color, name, options));

		public void AddRange(FormatRule[] newClasses)
		{
			if (!(newClasses is null) && (newClasses.Length > 0))
				foreach (FormatRule c in newClasses)
					this.Add(c);
		}

		public void Remove(string name)
		{
			int i = IndexOf(name);
			if (i >= 0) this._classes.RemoveAt(i);
		}

		public FormatRule[] ToArray() => _classes.ToArray();

		public string Markup(string source, CliColor wrapperColor = null)
		{
			if (wrapperColor is null) wrapperColor = DefaultColor;
			string[] lines = source.Replace("\r\n", "\r").Split(new char[] { '\r' }, StringSplitOptions.None);
			string result = "";

			foreach (string line in lines)
			{
				string work = line;
				foreach (FormatRule fc in this)
				{
					MatchCollection segments = Regex.Matches(work, FormatRuleCollection.PATTERN, RegexOptions.Compiled|RegexOptions.ExplicitCapture);
					if (segments.Count > 0)
					{
						foreach (Match seg in segments)
						{
							work = (seg.Groups["preamble"].Value.Length > 0) && fc.IsMatch(seg.Groups["preamble"].Value) ? fc.ApplyTo(seg.Groups["preamble"].Value) : seg.Groups["preamble"].Value;
							if (seg.Groups["break"].Value.Length > 0)
								work += seg.Groups["break"].Value;
							work += (seg.Groups["tail"].Value.Length > 0) && fc.IsMatch(seg.Groups["tail"].Value) ? fc.ApplyTo(seg.Groups["tail"].Value) : seg.Groups["tail"].Value;
						}
					}
					else
						if (fc.IsMatch(work))
							work = fc.ApplyTo(line);
				}
				result += work + "\r\n";
			}

			return result;
		}
		#endregion

		#region Static Methods
		#endregion

		//IEnumerator Support
		public IEnumerator<FormatRule> GetEnumerator() => this._classes.GetEnumerator();

		bool IEnumerator.MoveNext() => (++this._position) < this._classes.Count;

		void IEnumerator.Reset() => this._position = 0;

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
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
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
	}

	public class CustomFormat
	{
		#region Properties
		protected string _data = "";
		protected FormatRuleData _header = new FormatRuleData(), _footer = new FormatRuleData();
		protected FormatRuleCollection _rules = new FormatRuleCollection();
		protected int _tabSize = 4;
		protected Encoding _consoleEncoding = Encoding.ASCII;
		#endregion

		#region Constructors
		public CustomFormat(FormatRuleCollection collection = null, string data = "", int tabSize = 5, Encoding encoding = null)
		{
			this._data = data;
			this._tabSize = tabSize;
			this.OutputEncoding = encoding;
			this._rules = (collection is null) ? new FormatRuleCollection() : collection;
		}

		public CustomFormat(FormatRule[] collection, string data = "", int tabSize = 5, Encoding encoding = null)
		{
			this._data = data;
			this._tabSize = tabSize;
			this.OutputEncoding = encoding;
			this.AddRange(collection is null ? new FormatRule[] { } : collection);
		}
		#endregion

		#region Accessors
		public int Count => this._rules.Count;

		public string Data
		{
			get => this._data;
			set => this._data = value;
		}

		public FormatRule[] Rules
		{
			get => this._rules.ToArray();
			set => this._rules = new FormatRuleCollection(value,null);
		}

		public CliColor DefaultColor
		{
			get => this._rules.DefaultColor;
			set => this._rules.DefaultColor = value;
		}

		public FormatRuleData Header
		{
			get => this._header;
			set => this._header = (value is null) ? new FormatRuleData() : value;
		}

		public FormatRuleData Footer
		{
			get => this._footer;
			set => this._footer = (value is null) ? new FormatRuleData() : value;
		}

		public string[] Lines => this._data.Split(new char[] { '\r', '\n' });

		public int TabSize
		{
			get => this._tabSize;
			set => this._tabSize = Math.Max(1, Math.Min(value, 10));
		}

		public Encoding OutputEncoding
		{
			get => this._consoleEncoding;
			set => this._consoleEncoding = (value is null) ? Encoding.ASCII : value;
		}

		public bool SuppressHeader { get; set; }

		public bool SuppressFooter { get; set; }
		#endregion

		#region Methods
		public void Add(FormatRule newRule) => this._rules.Add(newRule);

		public void AddRange(FormatRule[] newRules) => this._rules.AddRange(newRules);

		// ═╡╞

		protected void WriteHeader(char bar = '═', char left = '╡', char right = '╞')
		{
			if (Header.IsEmpty)
				FormatRuleData.ToCon("".PadRight(Console.BufferWidth - 1, bar) + "\r\n", Header.Color, TabSize, OutputEncoding);
			else
			{
				FormatRuleData.ToCon(left.ToString().PadLeft(5, bar), Header.Color, TabSize, OutputEncoding);
				FormatRuleData.ToCon((Footer.IsEmpty ? " " : "[Start] ") + Header.Data + " ", new CliColor { Fore = Header.Color.Back, Back = Header.Color.Fore }, TabSize, OutputEncoding);
				FormatRuleData.ToCon(right.ToString().PadRight(Console.BufferWidth - Console.CursorLeft - 1, bar) + "\r\n", Header.Color, TabSize, OutputEncoding);
			}
		}

		protected void Writefooter(char bar = '═', char left = '╡', char right = '╞')
		{
			if (Footer.IsEmpty)
				FormatRuleData.ToCon("".PadRight(Console.BufferWidth - 1, bar) + "\r\n", Footer.Color, TabSize, OutputEncoding);
			else
			{
				FormatRuleData.ToCon(left.ToString().PadLeft(5, bar), Header.Color, TabSize, OutputEncoding);
				FormatRuleData.ToCon((Header.IsEmpty ? " " : "[End] ") + Footer.Data + " ", new CliColor { Fore = Footer.Color.Back, Back = Footer.Color.Fore }, TabSize, OutputEncoding);
				FormatRuleData.ToCon(right.ToString().PadRight(Console.BufferWidth - Console.CursorLeft - 1, bar) + "\r\n", Footer.Color, TabSize, OutputEncoding);
			}
		}

		protected void WriteDetailLine(string line, CliColor baseColor = null)
		{
			MatchCollection segments = Regex.Matches(line, FormatRuleCollection.PATTERN);
			baseColor = (baseColor is null) ? Con.DefaultColor : baseColor;
			if (segments.Count > 0)
			{
				foreach (Match seg in segments)
				{
					if (seg.Groups["preamble"].Value.Length > 0)
						FormatRuleData.ToCon(seg.Groups["preamble"].Value, baseColor, TabSize, OutputEncoding);

					if (seg.Groups["break"].Value.Length > 0)
						FormatRuleData.Parse(seg.Groups["break"].Value, baseColor.Back).Write(TabSize, OutputEncoding);

					if (seg.Groups["tail"].Value.Length > 0)
						WriteDetailLine(seg.Groups["tail"].Value, baseColor);
				}
			}
			else
				FormatRuleData.ToCon(line, baseColor, TabSize, OutputEncoding);
		}

		public void Write(CliColor baseColor = null)
		{
			baseColor = (baseColor is null) ? Con.DefaultColor : baseColor;
			string output = this._rules.Markup(this._data);
			string[] lines = output.Replace("\r\n", "\r").Split(new char[] { '\r' }, StringSplitOptions.None);

			if (!SuppressHeader) WriteHeader('═', '═', '═');
			for (int i = 0; i < lines.Length; i++)
			{
				if (i > 0) FormatRuleData.ToCon("\r\n", baseColor, TabSize, OutputEncoding);
				WriteDetailLine(lines[i], baseColor);
			}
			if (!SuppressFooter) Writefooter('═', '═', '═');
			Con.DefaultColor.ToConsole();
		}

		public void WriteLn(CliColor baseColor = null)
		{
			Write(baseColor);
			Con.Tec("{$1rn}", (baseColor is null) ? Con.DefaultColor : baseColor );
//			Con.Write("\r\n", (baseColor is null) ? CliColor.Default : baseColor);
		}

		public void WriteXml(CliColor baseColor = null)
		{
			string output;
			try
			{
				System.Xml.Linq.XDocument doc = System.Xml.Linq.XDocument.Parse(this._data);
				output = doc.ToString();
			}
			catch(Exception) { output = this._data; }

			baseColor = (baseColor is null) ? Con.DefaultColor : baseColor;
			output = this._rules.Markup(output);
			string[] lines = output.Replace("\r\n", "\r").Split(new char[] { '\r' }, StringSplitOptions.None);

			if (!SuppressHeader) WriteHeader('═', '═', '═');
			for (int i = 0; i < lines.Length; i++)
			{
				if (i > 0) FormatRuleData.ToCon("\r\n", baseColor, TabSize, OutputEncoding);
				WriteDetailLine(lines[i], baseColor);
			}
			if (!SuppressFooter) Writefooter('═', '═', '═');
			Con.DefaultColor.ToConsole();
		}
		#endregion

		#region Static Methods
		#endregion
	}
}
