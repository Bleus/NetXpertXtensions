using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NetXpertCodeLibrary.ConsoleFunctions
{
	public delegate string PropertyPrimitiveValue(string what, PromptPrimitive parent, Ranks defaultRank = Ranks.None);

	/// <summary>Used to define an encoding parameter for a prompt.</summary>
	/// <remarks></remarks>
	public class PromptPrimitive
	{
		#region Properties
		protected string _name = "";
		protected Regex _pattern;	// A regex pattern that describes this primitive.
		protected PropertyPrimitiveValue _delegate;
		protected dynamic _data = null;
		protected Ranks _defaultRank;
		#endregion

		#region Constructors
		public PromptPrimitive(PropertyPrimitiveValue sourceFn, string name = null, dynamic data = null, Ranks defaultRank = Ranks.None)
		{
			this._name = (string.IsNullOrEmpty(name) || (name.Trim().Length == 0)) ? new Guid().ToString() : name;
			this.Pattern = sourceFn(null, null);
			this._delegate = sourceFn;
			this._data = data;
			this._defaultRank = defaultRank;
		}

		public PromptPrimitive(string regexPattern, PropertyPrimitiveValue sourceFn, string name = null, dynamic data = null, Ranks defaultRank = Ranks.None)
		{
			this._name = (string.IsNullOrEmpty(name) || (name.Trim().Length == 0)) ? new Guid().ToString() : name;
			this.Pattern = (regexPattern is null) ? sourceFn(null,null) : regexPattern;
			this._delegate = sourceFn;
			this._data = data;
			this._defaultRank = defaultRank;
		}
		#endregion

		#region Operators
		public static bool operator !=(PromptPrimitive left, string right) => !(left == right);
		public static bool operator ==(PromptPrimitive left, string right)
		{
			if (left is null) return (right is null);
			if (right is null) return false;
			return left.Name.Equals(right, StringComparison.OrdinalIgnoreCase);
		}

		public static bool operator !=(PromptPrimitive left, PromptPrimitive right) => (left != right.Name);
		public static bool operator ==(PromptPrimitive left, PromptPrimitive right) => (left == right.Name);
		#endregion

		#region Accessors
		public string Pattern
		{
			get => _pattern.ToString();
			set => _pattern = new Regex(value, RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
		}

		public PropertyPrimitiveValue Delegate { set => this._delegate = value; }

		public string Name => this._name;

		public dynamic Data
		{
			get => this._data;
			set => this._data = value;
		}

		public string Help => _delegate( "--help", null );
		#endregion

		#region Methods
		public string Apply(string source) => Apply(source, _defaultRank);

		public string Apply(string source, Ranks useRank) =>
			source = _delegate(source, this, useRank);

		public T GetDataAs<T>() => (this._data.GetType()==typeof(T)) ? this._data : (T)Convert.ChangeType(this._data, typeof(T));

		public override string ToString() => this.Name + " => " + this.Pattern;

		public override bool Equals(object obj) => base.Equals(obj);

		public override int GetHashCode() => base.GetHashCode();
		#endregion
	}

	/// <summary>Stores and manages a collection of encoding parameters for crafting a prompt.</summary>
	public class PromptPrimitiveCollection : IEnumerator<PromptPrimitive>
	{
		#region Properties
		List<PromptPrimitive> _primitives = new List<PromptPrimitive>();
		private int _position = 0;
		#endregion

		#region Constructors
		public PromptPrimitiveCollection() { }

		public PromptPrimitiveCollection(PromptPrimitive first) => 
			_primitives.Add(first);

		public PromptPrimitiveCollection(PromptPrimitive[] primitives) => 
			_primitives.AddRange(primitives);

		public PromptPrimitiveCollection(PromptPrimitiveCollection collection) => 
			_primitives.AddRange(collection.ToArray());
		#endregion

		#region Accessors
		/// <summary>Returns the number of PromptPrimitive objects managed by this collection.</summary>
		public int Count => this._primitives.Count;

		public PromptPrimitive this[int index]
		{
			get => this._primitives[index];
			set => this._primitives[index] = value;
		}

		public PromptPrimitive this[string name]
		{
			get { int i = IndexOf(name); return (i < 0) ? null : this[i]; }
			set => this.Add(value);
		}

		// IEnumerator Support Accessors...
		PromptPrimitive IEnumerator<PromptPrimitive>.Current => this[this._position];

		object IEnumerator.Current => this[this._position];
		#endregion

		#region Methods
		protected int IndexOf(string name)
		{
			int i = -1; while ((++i < Count) && !this[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ;
			return (i < Count) ? i : -1;
		}

		public void Add(PromptPrimitive item)
		{
			int i = IndexOf(item.Name);
			if (i < 0)
				this._primitives.Add(item);
			else
				this._primitives[i] = item;
		}

		public void AddRange(PromptPrimitive[] items)
		{
			foreach (PromptPrimitive i in items)
				this.Add(i);
		}

		public void AddRange(PromptPrimitiveCollection items) =>
			this.AddRange(items.ToArray());

		public string Apply(string source)
		{
			foreach (PromptPrimitive p in this)
				source = p.Apply(source);

			return source;
		}

		public bool Remove(string name)
		{
			int i = IndexOf(name);
			if (i >= 0) this._primitives.RemoveAt(i);
			return (i >= 0);
		}

		public string Help()
		{
			string result = "";
			foreach (PromptPrimitive pp in this)
				result += pp.Help;

			return result;
		}

		public bool HasName(string name) => (IndexOf(name) >= 0);

		public PromptPrimitive[] ToArray() => this._primitives.ToArray();
		#endregion

		//IEnumerator Support
		public IEnumerator<PromptPrimitive> GetEnumerator() => this._primitives.GetEnumerator();
		bool IEnumerator.MoveNext() => (++this._position) < this.Count;
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

	/// <summary>A Class for storing, managing and interpretting a formulated prompt.</summary>
	public class Prompt
	{
		#region Properties
		protected const string PATTERN = /* language=regex */ @"[$]([a-z]{1,8})[\[][\x22']?([a-z|0-9]*)[\x22']?[\]];?";

		protected PromptPrimitiveCollection _primitives;
		protected string _rawPrompt;
		protected string _compiled = "";
		protected CliColor _color = new CliColor();
		#endregion

		#region Constructor
		public Prompt(string newPrompt = "", CliColor color = null)
		{
			this.Color = color;
			this.RawPrompt = newPrompt;
			this._primitives = new PromptPrimitiveCollection();
		}

		public Prompt(string newPrompt, CliColor color, PromptPrimitiveCollection primitives)
		{
			this.Color = color;
			this.RawPrompt = newPrompt;
			this._primitives = (primitives is null) ? new PromptPrimitiveCollection() : primitives;
		}

		public Prompt(string newPrompt, CliColor color, PromptPrimitive[] primitives)
		{
			this.Color = color;
			this.RawPrompt = newPrompt;
			this._primitives = (primitives is null) ? new PromptPrimitiveCollection() : new PromptPrimitiveCollection(primitives);
		}
		#endregion

		#region Accessors
		/// <summary>Facilitates interacting with a specific PromptPrimitive (primarily for setting/updating its stored value!)</summary>
		/// <param name="primitiveName">A string specifying the name for the PromptPrimitive that is desired.</param>
		public PromptPrimitive this[string primitiveName] =>
			this._primitives.HasName(primitiveName) ? this._primitives[primitiveName] : null;

		/// <summary>Returns the plaintext compiled prompt.</summary>
		public string Value => Compile(); // Compiled ? this._compiled : Compile();

		/// <summary>Returns the Length of the plaintext, compiled prompt.</summary>
		public int Length => this.Value.Length;

		/// <summary>Gets/Sets the color to display the prompt in. Setting to NULL uses the Console's defined colours.</summary>
		public CliColor Color
		{
			get => this._color;
			protected set => this._color = (value is null) ? CliColor.CaptureConsole() : value;
		}

		/// <summary>Facilitates interaction with the raw (underlying, uncompiled) prompt string.</summary>
		public string RawPrompt
		{
			get => this._rawPrompt;
			set
			{
				//if (!Validate(value)) throw new FormatException("The supplied Prompt value isn't a valid or recognized format.");
				this._rawPrompt = string.IsNullOrEmpty(value) || (value.Trim().Length == 0) ? AppDomain.CurrentDomain.FriendlyName + "> " : value;
				this._compiled = "";
			}
		}

		/// <summary>Reports TRUE if there's a compiled version of the Prompt, otherwise FALSE.</summary>
		protected bool Compiled => (this._compiled.Length > 0);

		/// <summary>Returns the set of rules used to administer this prompt.</summary>
		protected PromptPrimitiveCollection Rules => this._primitives;
		#endregion

		#region Methods
		/// <summary>Runs the _rawPrompt through the PromptPrimitiveCollection and stores the result in the _compiled string.</summary>
		/// <returns>The compiled version of the string.</returns>
		protected string Compile()
		{
			this._compiled = (_primitives.Count > 0) ? _primitives.Apply(_rawPrompt) : _rawPrompt;
			return this._compiled;
		}

		/// <summary>Add a PromptPrimitive to the internal collection.</summary>
		/// <param name="newPrimitive">The new PromptPrimitive to add.</param>
		public void Add(PromptPrimitive newPrimitive) => this._primitives.Add(newPrimitive);

		public void AddRange(PromptPrimitive[] primitives) => this._primitives.AddRange(primitives);

		public void AddRange(PromptPrimitiveCollection primitives) => this._primitives.AddRange(primitives);

		public bool Remove(string name)
		{
			if (this._primitives.Remove(name))
			{
				this.Compile();
				return true;
			}
			return false;
		}

		public string[] GetPrimitiveNames()
		{
			List<string> names = new List<string>();
			foreach (PromptPrimitive pp in this._primitives)
				names.Add(pp.Name);
			return names.ToArray();
		}

		///<summary>Outputs the compiled prompt to the Console in the prescribed colour.</summary>
		public void Write(string what="", CliColor textColor = null, bool useCRLF = false)
		{
			//Con.Tec(
			//	new TextElementCollection( "{$1r}$2", new object[] { Color, Value } ).ToString() +
			//	(!string.IsNullOrWhiteSpace( what ) ? new TextElementCollection( "{$2}$1", new object[] { what, textColor } ).ToString() : "") +
			//	new TextElement( "{$1,rn} " ).ToString()
			//);

			CliColor store = CliColor.CaptureConsole();
			this.Color.ToConsole();
			Console.Write( "\r" + this.Value );
			if (!string.IsNullOrEmpty( what ) && (what.Trim().Length > 0))
			{
				textColor.ToConsole();
				Console.Write( what );
			}

			Con.ClearToEndOfLine( textColor ); //Con.Write( what, textColor );
			if ( useCRLF) Console.WriteLine();

			store.ToConsole();
			//Con.Write( "\r" + this.Value, this.Color );
		}

		public override string ToString() => RawPrompt;

		public string Help() =>
			this._primitives.Help();
		#endregion

		public static implicit operator Prompt(string source) => new Prompt(source);
		public static implicit operator string(Prompt source) => source.Value;

		#region Static Methods
		public static bool Validate(string test) =>
			!(test is null) && (test.Length > 0) &&
			Regex.IsMatch(
				test, 
				PATTERN, 
				RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture
			);

		// This prompt parser can take in a reference DataTime value, or if one isn't specified, uses DateTime.Now.
		/// <summary>A base Prompt pre-processor to support date and time values.</summary>
		/// <seealso cref="https://www.c-sharpcorner.com/blogs/date-and-time-format-in-c-sharp-programming1"/>
		/// <remarks>
		/// Supported functions:
		///		time: h:mm:ss tt (12 hr clock with AM/PM)
		///		24hr: HH:mm:ss (24 hr clock with seconds)
		///		date: yyyy-MM-dd (metric date)
		///		long: ddd MMM d, yyyy ("DayOfWeek Month, day, year")
		///
		/// The function can also be any valid DateTime.ToString() string and it will be parsed appropriately. 
		/// </remarks>
		protected static string DateParser(string source, PromptPrimitive parent, Ranks defaultUserRank = Ranks.None)
		{
			if (string.IsNullOrWhiteSpace(source) && (parent is null)) return /* language=regex */ @"\$DATE\[(?:[a-z0-9. \:\/\-,]+)\]";

			if (source.Trim().Equals( "--help", StringComparison.OrdinalIgnoreCase ) && (parent is null))
				return @"{7,7}&bull; {6}$date{3}[{E}time{3}|{E}24hr{3}|{E}date{3}|{E}long{3}|{7}&lbrace;{3}format string{7}&rbrace;{3,rn}]";

			DateTime date = (parent.Data is null) ? DateTime.Now : parent.GetDataAs<DateTime>();
			MatchCollection matches = Regex.Matches(source, @"(?<cmd>\$date\[(?<opt>date|long|time|24hr|[dyYHhmMstK. \:\/\-,]*)\])", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
			foreach(Match m in matches)
			{
				if (m.Groups["cmd"].Success && m.Groups["opt"].Success)
				{
					string replace = "$date[err]";
					switch (m.Groups["opt"].Value.ToLower())
					{
						case "time": replace = date.ToString("h:mm:ss tt"); break;
						case "24hr": replace = date.ToString("HH:mm:ss"); break;
						case "date": replace = date.ToString("yyyy-MM-dd"); break;
						case "long": replace = date.ToString("ddd MMM d, yyyy"); break;
						default:
							if (Regex.IsMatch(m.Groups["opt"].Value,@"[dyYHhmMstK. \:\/\-,]*", RegexOptions.Compiled))
								replace = date.ToString(m.Groups["opt"].Value);
							break;
					}
					source = source.Replace(m.Value, replace);
				}
			}
			return source;
		}

		// This prompt parser gathers all of its data from independent sources and doesn't reference the _parent object for it.
		/// <summary>A base Prompt pre-processor to support some application-specific values.</summary>
		protected static string AppParser(string source, PromptPrimitive parent, Ranks defaultUserRank = Ranks.None)
		{
			if (string.IsNullOrWhiteSpace(source) && (parent is null)) return /* language=regex */ @"\$APP\[(?:[a-z]{3,4})\]";

			if (source.Trim().Equals( "--help", StringComparison.OrdinalIgnoreCase ) && (parent is null))
				return @"{7,7}&bull; {6}$app{3}[{E}name{3}|{E}ver{3}|{E}exe{3,rn}]";

			MatchCollection matches = Regex.Matches(source, @"(?<cmd>\$app\[(?<opt>name|ver|exe)\])", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
			foreach (Match m in matches)
			{
				if (m.Groups["cmd"].Success && m.Groups["opt"].Success)
				{
					string replace = "$app[err]";
					switch (m.Groups["opt"].Value.ToLower())
					{
						case "name":
							replace = System.Reflection.Assembly.GetEntryAssembly().GetName().Name;
							break;
						case "ver":
							replace = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();
							break;
						case "exe":
							replace = System.IO.Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
							break;
					}
					source = source.Replace(m.Value, replace);
				}
			}
			return source;
		}

		protected static string NewLineParser( string source, PromptPrimitive parent, Ranks defaultUserRank = Ranks.None )
		{
			if ( string.IsNullOrWhiteSpace( source ) && (parent is null) ) return /* language=regex */ @"\$NL;";

			if ( source.Trim().Equals( "--help", StringComparison.OrdinalIgnoreCase ) && (parent is null) )
				return @"{7,7}&bull; {6}$nl;{,rn}";

			Match m = Regex.Match( source, @"(?<cmd>[$]nl;)", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase );
			if ( m.Success )
				source = source.Replace( m.Value, "\r\n" );

			return source;
		}

		///<summary>Creates a basic Prompt library of features / functions.</summary>
		public static PromptPrimitiveCollection DefaultPrimitives(PromptPrimitiveCollection extras = null, Ranks defaultUserRank = Ranks.None)
		{
			if (extras is null) extras = new PromptPrimitiveCollection();

			extras.AddRange(
				new PromptPrimitive[]
				{
					new PromptPrimitive(Prompt.DateParser, "Date", null, defaultUserRank),
					new PromptPrimitive(Prompt.AppParser, "Application", null, defaultUserRank),
					new PromptPrimitive(UserInfo.PromptParser, "User", null, defaultUserRank),
					new PromptPrimitive(FileData.PromptParser, "Disk", null, defaultUserRank),
				}
			);

			return extras;
		}

		public string FormatRawPrompt( CliColor color = null)
		{
			if ( color is null ) color = Con.DefaultColor;
			return "{" + color.ToHexPair() + "}" + PromptRules.ApplyRules( this.RawPrompt ).Replace( "⌂", "{" + color.ToHexPair() + "}");
		}

		public static TextElementRuleCollection PromptRules =>
			new TextElementRuleCollection(
				new TextElementRule[]
				{
					new TextElementRule( /* meta tag inner data */ /* language=regex */
						@"([$][a-zA-Z]+)\[([^]]*)\]", "$1[{D}$2]", RegexOptions.IgnoreCase
					),
					new TextElementRule( /* meta tag prefix */ /* language=regex */
						@"\$([a-zA-Z]+)\[([^]]*)\]",
						"{9}${6}$1{9}[$2{9}]⌂", RegexOptions.IgnoreCase
					),
				}
			);
		#endregion
	}
}
