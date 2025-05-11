using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NetXpertCodeLibrary.ConsoleFunctions
{
	/// <summary>Manages the data portion of the switch facilitating multiple value identification / parsing.</summary>
	public class ArgData : IEnumerator<string>
	{
		#region Properties
		/// <summary>Used to specify what character will be interpretted as the splitter for identifying multiple values.</summary>
		public enum SplitChar { None, Pipe, Comma, Colon, SemiColon, Space }

		/// <summary>A string that holds the raw data value for the switch.</summary>
		protected string _source = "";

		/// <summary>Specifies the currently defined SplitChar value for parsing multiple values in the data.</summary>
		protected SplitChar _splitter = SplitChar.None;

		/// <summary>Supports the IEnumerator functionality.</summary>
		private int _position = 0;
		#endregion

		#region Constructors
		public ArgData(SplitChar splitChar = SplitChar.None) =>
			this._splitter = splitChar;

		public ArgData(string source = "", SplitChar splitChar = SplitChar.None)
		{
			this._splitter = splitChar;
			this.Value = source;
		}

		public ArgData(string[] sources, SplitChar splitChar = SplitChar.None)
		{
			this._splitter = splitChar;
			this.Pieces = sources;
		}
		#endregion

		#region Accessors
		/// <summary>Returns the length of the entire source string.</summary>
		public int Length =>
			this._source.Length;

		/// <summary>Returns TRUE if the managed string data is empty, otherwise FALSE.</summary>
		public bool IsEmpty =>
			string.IsNullOrEmpty(this._source);

		/// <summary>Returns a count of the sub-elements contained in the managed string.</summary>
		public int Count =>
			this.IsEmpty ? 0 : this.Pieces.Length;

		/// <summary>Permits direct {get; set;} interaction with the base data string.</summary>
		/// <remarks>
		/// String values passed in to the Set accessor are analysed and, if they come wrapped in quotes (single or double)
		/// the quotes are *removed* prior to the string being stored. The Get accessor, on the other hand, always returns
		/// the raw data value of the object. Use "ToString()" to retrieve the value wrapped in quotes when they're necessary.
		/// </remarks>
		public string Value
		{
			get => this._source;
			set => this._source = QuoteUnwrap(value);
		}

		/// <summary>Indicates if this object supports MultiValue parsing.</summary>
		public bool MultiValue => this._splitter != SplitChar.None;

		/// <summary>Serves as a translator between the SpliChar specification and the actual CHAR value it represents.</summary>
		protected char SplitWith
		{
			get
			{
				switch (this._splitter)
				{
					case SplitChar.Comma: return ',';
					case SplitChar.Colon: return ':';
					case SplitChar.SemiColon: return ';';
					case SplitChar.Pipe: return '|';
				}
				// SplitChar.None | SplitChar.Space:
				return ' ';
			}
		}

		/// <summary>Facilitates (public)Get/(protected)Set access to the specified split character setting for the object.</summary>
		public SplitChar SplitType
		{
			get => this._splitter;
			protected set
			{
				if (value != this._splitter)
				{
					string[] p = this.Pieces;
					this._splitter = value;
					this.Pieces = p;
				}
			}
		}

		/// <summary>
		/// Returns all of the sub-elements as separate values in an array of strings. If the string doesn't contain any
		/// sub-elements, the returned array contains just the source string itself as the only element.
		/// </summary>
		public string[] Pieces
		{
			get =>
				(this.MultiValue && (this._source.IndexOf(this.SplitWith) >= 0)) ?
					this._source.Split(new char[] { this.SplitWith }, StringSplitOptions.RemoveEmptyEntries) :
					new string[] { this._source };
			set =>
				this._source =
					(value is null) || (value.Length == 0) ? "" :
					(value.Length == 1 ? value[0] : String.Join(this.SplitWith.ToString(), value));
		}

		/// <summary>
		/// Permits direct {get; set;} interaction with the sub-elements of the managed data. When using the Set feature, providing an
		/// index less than zero, or greater than (or equal to) the length of the string, appends the value. If there is only a single
		/// sub-element is in the data, then it will be replaced by the provided value, regardless of the specified index
		/// </summary>
		/// <param name="index">An integer reference to the desired sub-element string.</param>
		/// <returns>If there are sub-elements, and the index is in range, the requested sub-element, otherwise the entire source string itself.</returns>
		public string this[int index]
		{
			get
			{
				string[] pieces = Pieces;
				return ((index >= 0) && (index < pieces.Length)) ? pieces[index] : this._source;
			}

			set
			{
				int count = Count;
				if (count > 0)
				{
					if ((value is null) || (value.Length < 1))
					{ // Removes a specified sub-element...
						if (count == 1)
							this._source = "";
						else
							if ((index >= 0) && (index < count))
						{
							List<string> pieces = new List<string>(Pieces);
							pieces.RemoveAt(index);
							this._source = (pieces.Count == 1) ? pieces[0] : String.Join(this.SplitWith.ToString(), pieces.ToArray());
						}
					}
					else
					{ // Adds or replaces a value...
						if ((index < 0) || (index >= count))
							this.AppendElement(value);
						else
						{
							if ((count == 1) && (index == 0))
								this._source = value;
							else
							{
								string[] pieces = Pieces;
								pieces[index] = value;
								this._source = String.Join(this.SplitWith.ToString(), pieces);
							}
						}
					}
				}
			}
		}

		// IEnumerator support
		string IEnumerator<string>.Current =>
			this[this._position];

		object IEnumerator.Current =>
			this[this._position];
		#endregion

		#region Operators
		public static bool operator ==(ArgData left, ArgData right)
		{
			if (left is null) return (right is null);
			if (right is null) return false;

			return left.Value == right.Value;
		}

		public static bool operator ==(ArgData left, string right)
		{
			if (left is null) return ((right is null) || (right.Length == 0) || (right == ""));
			return (left.Value == right);
		}

		public static bool operator ==(string left, ArgData right) =>
			(right == left);

		public static bool operator !=(ArgData left, ArgData right) => !(left == right);

		public static bool operator !=(ArgData left, string right) => !(left == right);

		public static bool operator !=(string left, ArgData right) => !(right == left);
		#endregion

		#region Methods
		/// <summary>Searches for the specified value and, if it's found, returns its index.</summary>
		/// <param name="value">A string specifying the value to search for.</param>
		/// <param name="compareType">Describes the type of comparison to apply.</param>
		/// <returns>If a match is found, the index of it, otherwise -1.</returns>
		public int IndexOf(string value, StringComparison compareType = StringComparison.InvariantCultureIgnoreCase)
		{
			if ((value == "") || (value is null) || (this.Length == 0))
				return -1;

			string[] pieces = Pieces;
			if (pieces.Length > 1)
			{
				int i = -1; while ((++i < pieces.Length) && !pieces[i].Equals(value, compareType)) ;
				return (i < pieces.Length) ? i : -1;
			}
			else
				return this._source.Equals(value, compareType) ? 0 : -1;
		}

		/// <summary>Evaluates the managed value against a supplied string and reports if they're equal according to the prescribed criteria.</summary>
		/// <param name="comparer">The string to compare against the managed data.</param>
		/// <param name="compareType">Describes the type of comparison to apply.</param>
		/// <returns>TRUE if the provided valud matches the managed value according to the defined criteria, otherwise FALSE.</returns>
		public bool IsEqualTo(string comparer, StringComparison compareType = StringComparison.InvariantCultureIgnoreCase) =>
			this._source.Equals(comparer, compareType);

		/// <summary>Performs the equivalent of "IsEqualTo" against all sub-element strings of the managed value.</summary>
		/// <param name="comparer">The string to compare against the managed data.</param>
		/// <param name="compareType">Describes the type of comparison to apply.</param>
		/// <returns>TRUE if the provided valud matches the value of a sub-element according to the defined criteria, otherwise FALSE.</returns>
		public bool HasValue(string comparer, StringComparison compareType = StringComparison.InvariantCultureIgnoreCase) =>
			(this.IndexOf(comparer, compareType) >= 0);

		/// <summary>Applies a new splitter character to the object and returns the results of reparsing the value with the new splitter.</summary>
		/// <param name="newSplitter">A SplitChar (enum) value to use as the new split marker for re-parsing.</param>
		/// <returns>An array of strings containing the new sub-elements as determined by applying the new splitter.</returns>
		public string[] Reparse(SplitChar newSplitter)
		{
			this._splitter = newSplitter;
			return this.Pieces;
		}

		/// <summary>Adds a value to the managed data as a sub-element.</summary>
		/// <param name="newValue">The value to add as a sub-element.</param>
		/// <param name="allowDupes">If set to TRUE, will append the element, even if there's already one like it in the package.</param>
		/// <returns>TRUE if the value was added, otherwise FALSE.</returns>
		public bool AppendElement(string newValue, bool allowDupes = false)
		{
			if (!this.HasValue(newValue) || allowDupes)
			{
				this._source += this._splitter + newValue;
				return true;
			}
			return false;
		}

		public override string ToString() => this.ToString('"');

		public string ToString(char wrapChar) => QuoteWrap(this._source, wrapChar);

		// Functions required to support overridden equivalence operators...
		public override bool Equals(object obj) =>
			this.Equals(obj, StringComparison.InvariantCultureIgnoreCase);

		public bool Equals(object obj, StringComparison compareType) =>
			this.Value.Equals(obj.ToString(), compareType);

		public override int GetHashCode() => base.GetHashCode();

		// Supports direct assignment between ArgData and String classes.
		public static implicit operator string(ArgData data) => data.ToString();
		public static implicit operator ArgData(string data) => new ArgData(data);

		//IEnumerator Support
		public IEnumerator<string> GetEnumerator() =>
			new List<string>().GetEnumerator();

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
		#endregion

		#region Static Methods
		/// <summary>Attempts to process a supplied string into an ArgData object.</summary>
		/// <param name="source">The string to parse.</param>
		/// <returns>A new ArgData object populated with the supplied string.</returns>
		public static ArgData Parse(string source) =>
			new ArgData(source);

		/// <summary>
		/// Takes an incoming string and determines if it needs to have quotes around it (because it contains whitespace, for
		/// example) and either does, if necessary, or doesn't apply them.
		/// </summary>
		/// <param name="source">The string to process.</param>
		/// <param name="quotes">Optional character to use as the quotes (default = '"')</param>
		/// <returns>A string that either has opening and closing quotes if they're required, or doesn't, if they're not.</returns>
		public static string QuoteWrap(string source, char quotes = '"')
		{
			if ((source.IndexOf(" ") < 0) || string.IsNullOrEmpty(source.Trim())) return source.Trim();

			// The string's already wrapped!
			if (Regex.IsMatch(source, @"^(('[^'\r\n]+')|(" + quotes + @"[^" + quotes + @"\r\n]+" + quotes + @"))$", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Compiled))
				return source;

			// If the string has 'quote' characters in it, we replace them with this symbol: '§'
			return quotes + source.Replace(quotes, '§') + quotes;
		}

		/// <summary>Used to unwrap a provided string of either single or double quotes.</summary>
		/// <param name="source">A string which may have its contents wrapped in quotes.</param>
		/// <param name="quotes">Optional character to use as the quotes (default = '"')</param>
		/// <returns>The supplied string without quotes.</returns>
		public static string QuoteUnwrap(string source, char quotes = '"')
		{
			source = source.Trim();
			if (Regex.IsMatch(source, @"^(('[^'\r\n]*')|(" + quotes + @"[^" + quotes + @"\r\n]*" + quotes + @"))$", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Compiled))
				// If it passed the match, it shouldn't be possible for it to be less than 2...
				return (source.Length < 3) ? "" : source.Substring(1, source.Length - 2).Replace('§', quotes);

			return source;
		}
		#endregion
	}

	/// <summary>A class for managing an individual ArgumentSwitch.</summary>
	public class ArgSwitch
	{
		#region Properties
		/// <summary>A Regex Pattern for validating / parsing a string to see if it's a valid switch.</summary>
		//public const string MARKER_PATTERN = /* language=regex * / @"\/|-+";
		//public const string ID_PATTERN =     /* language=regex * / @"[a-zA-Z?][\w]{0,31}";
		//public const string EQU_PATTERN =    /* language=regex * / @"[ \t]*[=:][ \t]*";
		//public const string DATA_PATTERN =   /* language=regex * / @"\x22[^\x22\r\n]*\x22|'[^'\r\n]*'|[^\x22]+";
		//public const string PATTERN = @"(?<marker>" + MARKER_PATTERN + @")(?<id>" + ID_PATTERN + @")((?<delimiter>[:=])(?<data>(([""][^""\r\n]*[""])|(['][^'\r\n]*['])|([\S]*))))?";
		//public const string PATTERN = @"(?<marker>" + MARKER_PATTERN + @")(?<id>" + ID_PATTERN + @")((?<delimiter>[:=])(?<data>(([\S ]*)|([\S]*))))?";
		//public static readonly string PATTERN = $"(?<marker>{MARKER_PATTERN})(?<id>{ID_PATTERN})((?<delimiter>{EQU_PATTERN})(?<data>{DATA_PATTERN}))?";
		protected static readonly Regex PARSER = new Regex( @"^((?<marker>[|\/-])(?<id>[a-z?][\w]*[a-z0-9]?)((?<equ>[:=])(?<data>\x22[^\x22\n\r]*\x22|'[^'\r\n]*'|[^\s\x22']*|\[[^\]\r\n]*\]))?)$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

		/// <summary>Stores the string that identifies the switch.</summary>
		protected string _id = "";

		/// <summary>Stores a character that separates the switch from it's data/payload.</summary>
		protected string _delimiter = "=";

		/// <summary>Stores any data that accompanies the switch.</summary>
		protected ArgData _data = "";

		/// <summary>Stores the default switch identification character(s).</summary>
		protected string _marker = "";
		#endregion

		#region Constructors
		/// <summary>Instantiates a new ArgSwitch object.</summary>
		/// <param name="id">A string specifying the switch's base identifier.</param>
		/// <param name="data">An optional string that specifyies the switch's payload/data value.</param>
		/// <param name="marker">An optional parameter that specifies the character(s) that identify switches (allowed: -, -- or /)</param>
		/// <param name="delimiter">An optional parameter that specifies the character that separates the switch's ID from its Value</param>
		/// <param name="splitChar">
		/// An optional ArgData.SplitChar value that specifies the method for parsing multiple-value data strings in the payload.
		/// </param>
		public ArgSwitch(string id, string data = "", string marker = "-", string delimiter = "=", ArgData.SplitChar splitChar = ArgData.SplitChar.None)
		{
			this.Id = id;
			this.Delimiter = delimiter;
			this.Marker = marker;
			this._data = new ArgData(data, splitChar);

		}
		#endregion

		#region Accessors
		/// <summary>
		/// Gets the ID of the configured switch. The ID is the portion of the switch that identifies the switch.
		/// If the object was configured without an ID, this accessor will allow one to be assigned, otherwise the
		/// field is Read-Only.
		/// </summary>
		public string Id
		{
			get => this._id.ToUpperInvariant();
			set
			{
				if ((this._id == "") && (value.Trim().Length > 0) && Regex.IsMatch(value, @"^[a-z?][\w]*[a-z0-9]?$", RegexOptions.IgnoreCase) )
					this._id = value;
			}
		}

		/// <summary>Gets or Sets the delimiter to use ('/' or '-') when parsing, or exporting this switch.</summary>
		public string Delimiter
		{
			get => this._delimiter;
			set => this._delimiter = (value.Trim().Length == 1) && (">/-".IndexOf(value.Trim()[0]) > 0) ? value.Trim().Substring(0, 1) : "=";
		}

		/// <summary>Gets or Sets the character (typically ':' or '=') that separates the switch ID from the payload.</summary>
		public string Marker
		{
			get => this._marker;
			set 
			{
				if ( !string.IsNullOrWhiteSpace( value ) )
				{
					value = value.Trim();
					if ( ">=:".IndexOf( value[ 0 ] ) > 0 ) this._marker = value.Substring( 0, 1 );
				}
			}
		}

		/// <summary>Gets or Sets the payload for the switch.</summary>
		public String Value
		{
			get => this._data.Value;
			set
			{
				if ( !string.IsNullOrWhiteSpace( value ) )
					this._data.Value = "";
				else
				{
					if ( Regex.IsMatch( value, @"^(\x22[^\x22\r\n]*\x22|'[^'\r\n]*'|\[[^\]\r\n]*\])$" ) )
						this._data.Value = value.Substring( 1, value.Length - 2 );
					else
						this._data.Value = value;
				}
			}
		}

		/// <summary>Returns all provided values in an array of strings.</summary>
		public string[] Values => this.HasMultipleValues() ? this.ArgData.Pieces : new string[] { Value };

		/// <summary>Exposes the underlying ArgData object.</summary>
		public ArgData ArgData => this._data;

		/// <summary>Returns TRUE if the object represents a valid switch (i.e. if the ID has been assigned to it).</summary>
		public bool IsValid => (this._id.Length > 0);

		/// <summary>Returns TRUE if the data value for the switch is null or unpopulated.</summary>
		public bool IsEmpty => (this._data is null) || this._data.IsEmpty;

		/// <summary>Reports the number of sub-elements (values) contained within this switch.</summary>
		public int Count => this._data.Count;

		/// <summary>Facilitates checking / modifying the Value split character.</summary>
		public ArgData.SplitChar SplitChar
		{
			get => this._data.SplitType;
			set => this._data = new ArgData(this._data.Value, value);
		}
		#endregion

		#region Operators
		/// <summary>Reports two ArgSwitch objects as being "equal" if their ID's match (case-insensitive).</summary>
		public static bool operator ==(ArgSwitch left, ArgSwitch right)
		{
			if (left is null) return right is null;
			if ((left.Id == "") || right is null || (right.Id == "")) return false;
			return left.Id == right.Id;
		}

		/// <summary>
		/// This override pulls double duty: If the provided string can be parsed as an ArgSwitch, it will report TRUE
		/// if it matches the ID of the companion ArgSwitch object. If the string ISN'T parseable as an ArgSwitch, this
		/// comparison will return TRUE if the supplied string matches the companion ArgSwitch's VALUE.
		/// </summary>
		public static bool operator ==(string left, ArgSwitch right)
		{
			left = left.Trim(); // Remove leading or trailing whitespace...
			if ((left is null) || (left == "")) return (right is null) || (right.Id == ""); // String is empty or null

			// If the left string is a valid ArgSwitch string, parse it and compare...
			if (Validate(left)) return (ArgSwitch.Parse(left, right.ArgData.SplitType) == right);

			// ...otherwise just compare the string against the ArgSwitch object's Value (case-insensitive!)
			return left == right.Value;
		}

		public static bool operator !=(ArgSwitch left, ArgSwitch right) { return !(left == right); }
		public static bool operator !=(string left, ArgSwitch right) { return !(left == right); }
		#endregion

		#region Methods
		public string[] ParseValue() =>
			this._data.Pieces;

		/// <summary>Applies a new splitter-character to the object's data and returns the results of reparsing the value with the new splitter.</summary>
		/// <param name="newSplitter">An ArgData.SplitChar value to use as the new split marker for re-parsing.</param>
		/// <returns>An array of strings containing the new sub-elements as determined by applying the new splitter.</returns>
		public string[] ParseValue(ArgData.SplitChar separator) =>
			this._data.Reparse(separator);

		/// <summary>Attempts to return the data package of this switch in the specified type.</summary>
		/// <typeparam name="T">The desired Type for the returned value.</typeparam>
		/// <returns>An object of the specified Type, derived from the value of this switch.</returns>
		public T As<T>() =>
			(T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromString(this._data.Value);


		/// <summary>Attempts to return the data package of this switch in the specified type, and, upon failure, returns the specified default value instead.</summary>
		/// <typeparam name="T">The desired Type for the returned value.</typeparam>
		/// <param name="defaultValue">An object of the appropriate type to return if the attempt to parse this switches value fails.</param>
		/// <returns>An object of the specified Type, derived from the value of this switch.</returns>
		public T TryAs<T>(T defaultValue)
		{
			try { return As<T>(); } catch { };
			return defaultValue;
		}

		/// <summary>Provides a means to check for the existence of a specific value in MultiValue switches.</summary>
		/// <param name="check">The value to check for.</param>
		/// <param name="comparer">Specifies the kind of comparison to perform.</param>
		/// <returns>TRUE if a value matching the provided string by the defined method is found, otherwise FALSE.</returns>
		public bool HasValue(string check = "", StringComparison comparer = StringComparison.OrdinalIgnoreCase) =>
			string.IsNullOrEmpty(check) ? this._data.Value.Length > 0 : this._data.HasValue(check, comparer);

		/// <summary>Compares the currently stored value with the string version of a supplied value.</summary>
		/// <param name="value">A value to compare with the one stored in the switch.</param>
		/// <param name="stringComparison">An Enum specifying the kind of match to test against.</param>
		/// <returns>TRUE if the stored value of the switch matches the string version of the supplied comparator.</returns>
		public bool IsEqualTo(dynamic compare, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase) =>
			this._data.IsEqualTo(compare.ToString(), stringComparison);

		/// <summary>If the contained data has sub-elements, this returns TRUE, otherwise FALSE.</summary>
		public bool HasMultipleValues() => ArgData.MultiValue && (ArgData.Count > 0);

		public override string ToString()
		{
			if (this.IsValid)
			{
				string result = this._marker + this.Id;
				if (this._data.Length > 0) result += this.Delimiter + this._data.ToString();
				return result;
			}

			return this._data.Value;
		}

		/// <summary>Compares a provided ArgSwitch's Id against this one and returns a value indicating the outcome.</summary>
		/// <param name="value">An ArgSwitch value to compare against.</param>
		/// <returns>-1 if the supplied ArgSwitch's Id is greater than ours, +1 if it's less than, and 0 if they're equal.</returns>
		public int CompareTo( ArgSwitch value ) =>
			this.Id.CompareTo( value.Id );

		public override int GetHashCode() => base.GetHashCode();
		public override bool Equals(object obj) => base.Equals(obj);
		#endregion

		#region Static Methods
		/// <summary>Parses a passed string into a switch object without data.</summary>
		/// <param name="source">The string to parse. Must have either a dash or slash as the first character! ('/' or '-').</param>
		/// <returns>A new, populated, ArgSwitch object configured from the provided string value or NULL if parsing was unsuccessful.</returns>
		public static ArgSwitch Parse(string source, ArgData.SplitChar splitChar = ArgData.SplitChar.None)
		{
			if (ArgSwitch.Validate(source))
			{
				//Regex parser = new Regex(PATTERN, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
				MatchCollection matches = PARSER.Matches(source);
				if ((matches.Count > 0) && (matches[0].Groups.Count > 3))
					return new ArgSwitch(
						matches[0].Groups["id"].Value,          // base Id
						matches[0].Groups["data"].Value,        // payload
						matches[0].Groups["marker"].Value,      // marker (switch indicator)
						matches[0].Groups["equ"].Value,			// delimiter (separates switch base id from value)
						splitChar                               // How to parse the payload on multi-value strings.
						);
			}
			return null;
		}

		/// <summary>Checks a string and determines if it conforms to the default regex pattern for a switch.</summary>
		/// <param name="test">The string to check against the pattern.</param>
		/// <returns>TRUE if the passed string conforms to the pattern, otherwise FALSE.</returns>
		public static bool Validate(string test) => 
			!string.IsNullOrEmpty(test) && PARSER.IsMatch( test );
		//(test.Length < 2) ? false : Regex.IsMatch(test, "^[" + markers + "][a-z0-9?]+([:=].+)?$",RegexOptions.IgnoreCase);
		#endregion
	}

	/// <summary>Manages a set of command-line arguments in an easily accessible way.</summary>
	public class ArgumentCollection : IEnumerator<ArgSwitch>
	{
		#region Properties
		// ((?<char>[\/-])(?<id>[a-zA-Z?][\w]*[a-zA-Z0-9]?)(?:(?<equ>[:=])(?<data>\x22[^\x22\n\r]*\x22|'[^'\r\n]*'|[^\s\x22']*|\[[^\]\r\n]*\]))?)
		// ((?<body>[\w@!#$%^&*()_=+\\|\][{;:}[]+|'[^'\r\n]*'|\x22[^\x22\r\n]*\x22)|((?<char>[\/-])(?<id>[a-zA-Z?][\w]*[a-zA-Z0-9]?)(?:(?<equ>[:=])(?<data>\x22[^\x22\n\r]*\x22|'[^'\r\n]*'|[^\s\x22']*|\[[^\]\r\n]*\]))?))

		public static readonly Regex PARSER = new Regex( @"((?<body>[\w@!#$%^&*()_=+\\|\][{;.,<>?:}[]+|'[^'\r\n]*'|\x22[^\x22\r\n]*\x22)|((?<char>[\/-])(?<id>[a-zA-Z?][\w]*[a-zA-Z0-9]?)(?:(?<equ>[:=])(?<data>\x22[^\x22\n\r]*\x22|'[^'\r\n]*'|[^\s\x22']*|\[[^\]\r\n]*\]))?))", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture );
		// $"(?<seg>{ArgSwitch.PATTERN}|[\\S]*{ArgSwitch.DATA_PATTERN}[\\S]*)";

		protected List<string> _args = new List<string>();
		protected List<ArgSwitch> _switches = new List<ArgSwitch>();
		protected ArgData.SplitChar _defaultSplitChar = ArgData.SplitChar.None;
		protected int _position = 0;
		#endregion

		#region Constructors
		public ArgumentCollection(ArgData.SplitChar defaultSplitChar = ArgData.SplitChar.None) =>
			this._defaultSplitChar = defaultSplitChar;

		public ArgumentCollection(string source, ArgData.SplitChar defaultSplitChar = ArgData.SplitChar.None)
		{
			this._defaultSplitChar = defaultSplitChar;
			this.Import(source);
		}

		public ArgumentCollection(string[] source, ArgData.SplitChar defaultSplitChar = ArgData.SplitChar.None)
		{
			this._defaultSplitChar = defaultSplitChar;
			this.Import(source);
		}

		public ArgumentCollection(ArgSwitch[] source, ArgData.SplitChar defaultSplitChar = ArgData.SplitChar.None)
		{
			this._defaultSplitChar = defaultSplitChar;
			this.Import(source);
		}
		#endregion

		#region Accessors
		public ArgSwitch this[string id]
		{
			get
			{
				int i = this.FindSwitch(id);
				return (i < 0) ? null : this._switches[i];
			}
			set
			{
				int i = this.FindSwitch(id);
				if (i < 0)
					this.Add(value);
				else
					this._switches[i] = value;
			}
		}

		public ArgSwitch this[int index]
		{
			get => ((index < 0) || (index >= this._switches.Count)) ? null : this._switches[index];
			set
			{
				if ((index < 0) || (index >= this._switches.Count))
				{
					int i = this.FindSwitch(value.Id);
					if (i < 0)
					{
						if (index < 0) this._switches.Insert(0, value);
						else
							this._switches.Add(value);
					}
					else
						this._switches[i] = value;
				}
				else
					this._switches[index] = value;
			}
		}

		public ArgSwitch this[ArgSwitch arg]
		{
			get => this[arg.Id];
			set => this[arg.Id] = value;
		}

		/// <summary>Gets the number of objects (switches and arguments) being managed by the object.</summary>
		public int Count { get { return this._args.Count + this._switches.Count; } }
		/// <summary>Gets just the number of switches being managed by the object (arguments aren't counted).</summary>
		public int SwitchCount { get { return this._switches.Count; } }
		/// <summary>Gets an array of ArgSwitch objects corresponding to the collection of switches being managed.</summary>
		public ArgSwitch[] Switches { get { return this._switches.ToArray(); } }
		/// <summary>Gets an array of Strings corresponding to the collection of non-switch arguments being managed.</summary>
		public string[] Args { get { return this._args.ToArray(); } }

		public IEnumerator<ArgSwitch> GetEnumerator() => this._switches.GetEnumerator();

		ArgSwitch IEnumerator<ArgSwitch>.Current => this._switches[this._position];

		object IEnumerator.Current => this._switches[this._position];
		#endregion

		#region Operators
		public static ArgumentCollection operator +(ArgumentCollection left, string right) { left.Add(right); return left; }

		public static ArgumentCollection operator +(ArgumentCollection left, ArgSwitch right) { left.Add(right); return left; }
		#endregion

		#region Methods
		/// <summary>Adds the contents of an array of ArgSwitch objects to this repository.</summary>
		/// <param name="data">An array of ArgSwitch objects to add to this object.</param>
		public void Import(ArgSwitch[] data)
		{
			foreach (ArgSwitch s in data) this.Add(s);
		}

		/// <summary>Processes an array of strings into arguments and switches to be managed by this object.</summary>
		/// <param name="data">An array of strings to be parsed.</param>
		public void Import(string[] data)
		{
			if (data.Length > 0)
				foreach (string arg in data)
					this.Add(arg, this._defaultSplitChar);
		}

		/// <summary>Processes a string into arguments and switches (broken by whitespace) that are to be managed by this object.</summary>
		/// <param name="data">A string to be parsed (broken into chunks by whitespace).</param>
		/// <param name="splitChar">An optional value to specify if the specified switches are Multivalued.</param>
		public void Import(string data)
		{
			MatchCollection ms = PARSER.Matches( data );
			if (ms.Count > 0)
				foreach (Match segments in ms)
					if (segments.Groups[0].Success) //(Validate(data))
					{
						if (ArgSwitch.Validate(segments.Groups[0].Value))
						{
							string work = segments.Groups[0].Value;
							if ((work.IndexOf("\r") >= 0) || (work.IndexOf("\n") >= 0))
							{
								string[] lines = work.Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
								foreach (string line in lines)
									this.Add(line,_defaultSplitChar);
							}
							else
							{
								MatchCollection matches = PARSER.Matches(work);
								foreach (Match m in matches)
									if (m.Groups[0].Success && ArgSwitch.Validate(m.Groups[0].Value))
										this.Add(m.Groups[0].Value,_defaultSplitChar);
							}
						}
						else
							this._args.Add(segments.Groups[0].Value);
					}
		}

		/// <summary>Searches the local collection of Switches for one matching the Id passed.</summary>
		/// <param name="id">A string specifying the Id of the switch to look for.</param>
		/// <returns>The index of the switch in the collection, if found, otherwise, -1.</returns>
		protected int FindSwitch(string id)
		{
			int i = -1; while ((++i < this._switches.Count) && !id.Equals(this._switches[i].Id, StringComparison.OrdinalIgnoreCase)) ;
			return (i < this._switches.Count) ? i : -1;
		}

		/// <summary>Searches the local collection of Switches for one that matches the Id of the passed switch.</summary>
		/// <param name="id">An ArgSwitch object corresponding to the Id of the switch we're to look for.</param>
		/// <returns>The index of the switch in the collection, if found, otherwise, -1.</returns>
		protected int FindSwitch(ArgSwitch id) => this.FindSwitch(id.Id);

		/// <summary>
		/// Parses a passed string and incorporates it into the local collection, either by adding it as a new object
		/// (either an argument or a switch), or by updating a matching object (switch) if it already exists.
		/// </summary>
		/// <param name="value">A string containing the information to be parsed and collected.</param>
		/// <param name="splitChar">An optional value to specify if the new switch is Multivalued.</param>
		public void Add(string value, ArgData.SplitChar splitChar = ArgData.SplitChar.None)
		{
			if (ArgSwitch.Validate(value))
				this.Add(ArgSwitch.Parse(value, splitChar));
			else
				this._args.Add(value);
		}

		/// <summary>Properly incorporates the ArgSwitch object into the local collections.</summary>
		/// <param name="value">An ArgSwitch object to process and incorporate.</param>
		public void Add(ArgSwitch value)
		{
			if (value.IsValid)
			{
				int i = this.FindSwitch(value);
				if (i < 0) this._switches.Add(value); else this._switches[i] = value;
			}
			else
				this._args.Add(value.Value);
		}

		/// <summary>Searches the switch collection for one matching the provided Id, then returns it.</summary>
		/// <param name="id">A string specifying the ID of the desired switch.</param>
		/// <returns>If a matching switch is found, it is returned, otherwise NULL.</returns>
		public ArgSwitch GetSwitch(string id)
		{
			int i = this.FindSwitch(id);
			return (i < 0) ? null : this._switches[i];
		}

		/// <summary>Searches the switch collection for one matching the provided switch's Id, then returns it.</summary>
		/// <param name="id">An ArgSwitch object to match from the local collection.</param>
		/// <returns>If a matching switch is found, it is returned, otherwise NULL.</returns>
		public ArgSwitch GetSwitch(ArgSwitch id)
		{
			int i = this.FindSwitch(id);
			return (i < 0) ? null : this._switches[i];
		}

		/// <summary>
		/// Accepts an array of strings identifying a batch of switches to retrieve from the collection as an array.
		/// </summary>
		/// <param name="switches">An array of strings specifying all of the switches that are to be retrieved.</param>
		/// <returns>An array of ArgSwitch objects corresponding to all matched switches from the collection.</returns>
		public ArgSwitch[] GetSwitches(string[] switches)
		{
			List<ArgSwitch> coll = new List<ArgSwitch>();
			foreach (string s in switches)
			{
				ArgSwitch hunt = this.GetSwitch(s);
				if (hunt != null) coll.Add(hunt);
			}
			return coll.ToArray();
		}

		/// <summary>
		/// Accepts a string of switches identifying a group of switches to retrieve from the collection as an array.
		/// </summary>
		/// <param name="switches">A string containing all of the switches to be retrieved, separated by whitespace.</param>
		/// <returns>An array of ArgSwitch objects corresponding to all matched switches from the collection.</returns>
		public ArgSwitch[] GetSwitches(string switches) => this.GetSwitches(switches.Split(new char[] { ' ', '\r', '\n', '\t' }));

		/// <summary>Looks for the existence of a specified switch in the collection and reports the result.</summary>
		/// <param name="test">A string specifying the Id of the desired switch.</param>
		/// <returns>TRUE if the switch was located, otherwise FALSE.</returns>
		public bool HasSwitch(string test) => (this.FindSwitch(test) >= 0);

		/// <summary>Looks for the existence of a specified switch in the collection and reports the result.</summary>
		/// <param name="test">An ArgSwitch object whose Id matches that of the desired switch.</param>
		/// <returns>TRUE if the switch was located, otherwise FALSE.</returns>
		public bool HasSwitch(ArgSwitch test) => (this.FindSwitch(test) >= 0);

		/// <summary>Tests a collection of switches against the current local collection.</summary>
		/// <param name="test">An array of strings specifying the Id's of the switches to look for.</param>
		/// <returns>TRUE if ALL of the specified switches were found in the collection, otherwise FALSE.</returns>
		public bool HasSwitch(string[] test)
		{
			bool result = false;
			if (test.Length > 0)
			{
				int i = 0;
				while ((i < test.Length) && !result) result = this.HasSwitch(test[i++]);
			}
			return result;
		}

		/// <summary>Sorts the arguments alphabetically by their Id.</summary>
		/// <returns>This collection, after sorting.</returns>
		public ArgumentCollection Sort() { this._switches.Sort( ( x, y ) => x.CompareTo( y ) ); return this; }

		public ArgSwitch[] ToArray() => this._switches.ToArray();

		/// <summary>Outputs the local collections in the form of a single "command line" string.</summary>
		/// <returns>A string containing all of the arguments and switches currently being managed in a "command line" like format.</returns>
		public override string ToString()
		{
			string result = "";
			foreach (string s in this._args) if (s.Length > 0) result += " " + ArgData.QuoteWrap(s);
			foreach (ArgSwitch s in this._switches) if (s.Id.Length > 0) result += " " + s.ToString();
			return result.Trim();
		}

		// Facilitates interchangeability between string arrays and ArgumentsMgt objects.
		public static implicit operator string[] (ArgumentCollection data)
		{
			List<string> result = new List<string>();
			foreach (string value in data._args)
				result.Add(ArgData.QuoteWrap(value));

			foreach (ArgSwitch sw in data._switches)
				result.Add(sw.ToString());

			return result.ToArray();
		}

		public static implicit operator ArgumentCollection(string[] data) => new ArgumentCollection(data);

		bool IEnumerator.MoveNext() => (++this._position) < this._switches.Count;

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
		#endregion

		#region Static Methods
		/// <summary>Test a supplied string and determines if it can be parsed as a set of command line arguments.</summary>
		/// <param name="source">A string containing the value to validate.</param>
		/// <returns>TRUE if the supplied value can be parsed as a set of command line instructions.</returns>
		public static bool Validate( string source ) =>
			!string.IsNullOrEmpty( source ) && PARSER.IsMatch( source );
		
		/// <summary>
		/// Tests a supplied array of strings to see if they can, when glued together with a space, be parsed as a set of valid command
		/// line arguments.
		/// </summary>
		/// <param name="source">An array of strings that will be Join'd with a space and validated against the ArgumentsMgt Regex Pattern.</param>
		/// <returns>TRUE if the operation was successful, otherwise FALSE.</returns>
		public static bool Validate(string[] source) =>
			Validate(string.Join(" ", source));

		/// <summary>Makes the application launch switches available to any calling function.</summary>
		/// <returns>An ArgumentCollection object populated with the arguments passed to the application on the Console command line.</returns>
		public static ArgumentCollection LaunchArgs() =>
			new ArgumentCollection( Environment.GetCommandLineArgs() );
		#endregion
	}
}
