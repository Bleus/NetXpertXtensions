using System.Collections;
using System.ComponentModel;
using System.Text.RegularExpressions;
using NetXLMP;

namespace NetXpertExtensions.Classes
{
    #nullable disable

    public static class Utility
    {
        #region Data
        /// <summary>Regex Pattern to identify the VALUE portion of a switch.</summary>
        /// <remarks>(?<data>\x22[^\x22\n\r]*\x22|'[^'\r\n]*'|[^\s\x22']*|\[[^\]\r\n]*\])</remarks>
        public static readonly string SW_VALUE = /* language=regex */
            @"(?<data>\x22[^\x22\n\r]*\x22|'[^'\r\n]*'|[^\s\x22']*|\[[^\]\r\n]*\])";

        /// <summary>Regex Pattern to identify a switch from within a string.</summary>
        /// <remarks>(?:(?<swsgn>[\/-])(?<id>[a-zA-Z?][\w]*[a-zA-Z0-9]?)(?:(?<opsgn>[:=]){SW_VALUE})?)</remarks>
        public static readonly string SWITCH = /* language=regex */
            @"(?:(?<swsgn>((?<=[\s])[\/-]|^[\/-]))(?<id>[a-zA-Z?][\w]*[a-zA-Z0-9]?)(?:(?<opsgn>[:=])$1)?)".Replace("$1", SW_VALUE);

        /// <summary>Regex Pattern to identify non-switch-related string data.</summary>
        /// <remarks>(?<body>'[^'\r\n]*'|\x22[^\x22\r\n]*\x22|[^\x22'\s\/-]+)</remarks>
        public static readonly string ARGUMENT = /* language=regex */
            @"(?<body>'[^'\r\n]*'|\x22[^\x22\r\n]*\x22|[^\x22'\s\/\\-][^\x22\'\s\/\\]*)";

        /// <summary>(?:{ARGUMENT}|{SWITCH})</summary>
        /// <remarks>
        /// ARGUMENT: (?:(?<swsgn>[\/-])(?<id>[a-zA-Z?][\w]*[a-zA-Z0-9]?)(?:(?<opsgn>[:=])
        ///           (?<data>\x22[^\x22\n\r]*\x22|'[^'\r\n]*'|[^\s\x22']*|\[[^\]\r\n]*\]))?)
        /// SWITCH:   (?<body>'[^'\r\n]*'|\x22[^\x22\r\n]*\x22|[^\x22'\s\/-]+)
        /// </remarks>
        public static readonly string LINE = $"(?:{ARGUMENT}|{SWITCH})";

        /// <summary>Regex Pattern to validate a full command line.</summary>
        /// <remarks>^[\s]*(?<cmd>[a-zA-Z][\w]*[a-zA-Z0-9])[\s]+(?<data>[\S \t]+)?$</remarks>
        public static readonly string COMMANDLINE = /* language=regex */
            @"^[\s]*(?<cmd>[a-zA-Z][\w]*[a-zA-Z0-9])(?:[\s]+(?<data>[\S \t]+))?$";

        //@"^[\s]*(?<cmd>[a-zA-Z][\w]*[a-zA-Z0-9])(?:(?: (?<args>$1|$2))*)*[\s]*$".Replace(new object[] { ARGUMENT, SWITCH } );

        /// <summary>Defines the default set of RegexOptions.</summary>
        /// <remarks>Default: RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture</remarks>
        public static RegexOptions DefOptions = RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture;

        /// <summary>Used to specify what character will be interpretted as the splitter for identifying multiple values.</summary>
        /// <remarks>Possible Values: None, Pipe, Comma, Colon, Semicolon, Space and Unknown.</remarks>
        public enum SplitChar : byte { None = 0, Pipe = (byte)'|', Comma = (byte)',', Colon = (byte)':', SemiColon = (byte)';', Space = (byte)' ', Unknown = 255 }
        #endregion

        #region Methods
        /// <summary>Factory method to generate a Regex class using the specified pattern and RegexOptions.</summary>
        /// <param name="pattern">A string containing the Regex pattern to incorporate into the class. If NULL, defaults to Utility.LINE.</param>
        /// <param name="options">The RegexOptions to apply. If NULL, Utility.DefOptions is used.</param>
        /// <returns>A new Regex class built using the specified pattern and options.</returns>
        public static Regex Parser(string pattern = "", RegexOptions? options = null) =>
            new(string.IsNullOrWhiteSpace(pattern) ? LINE : pattern, options is null ? DefOptions : (RegexOptions)options);

        /// <summary>Takes a passed string and reports on its validity as a recognized switch.</summary>
        /// <param name="data">The string to parse/test.</param>
        /// <returns>TRUE if the passed string is recognized as a valid switch.</returns>
        public static bool ValidateSwitch(string data) =>
            !string.IsNullOrWhiteSpace(data) && Regex.IsMatch(data.Trim(), $"^{SWITCH}$", DefOptions);

        /// <summary>Takes a passed string and reports on its validity as recognized command line string data.</summary>
        /// <param name="data">The string to parse/test.</param>
        /// <returns>TRUE if the passed string is recognized as valid command line text data.</returns>
        public static bool ValidateArgument(string data) =>
            !string.IsNullOrWhiteSpace(data) && Regex.IsMatch(data.Trim(), $"^{ARGUMENT}$", DefOptions);

        /// <summary>Takes a passed string and reports on its validity as a recognized command line parameter sequence.</summary>
        /// <param name="data">The string to parse/test.</param>
        /// <returns>TRUE if the passed string is recognized as valid for command line switch/application data.</returns>
        public static bool ValidateLine(string data) =>
            !string.IsNullOrWhiteSpace(data) && Regex.IsMatch(data.Trim(), LINE, DefOptions);

        /// <summary>Attempts to decipher an Argument value split character from a provided string.</summary>
        /// <param name="source">A string containing the data to try and parse.</param>
        /// <param name="defaultChar">If the value is indeterminate, you can specify what character to return anyway.</param>
        /// <returns>A Utility.SplitChar value representing the best-guess character from the provided string, otherwise the 'defaultChar' value.</returns>
        public static SplitChar ParseSplitChar(string source, SplitChar defaultChar = SplitChar.None)
        {
            if (ValidateArgument(source))
            {
                string basePattern = /* language=regex */
                    @"(?:'[^'\r\n]*'|\x22[^\x22\r\n]*\x22|[^\x22'\s\/\\-][^\x22\'\s\/\\]*)";

                foreach (SplitChar sc in (SplitChar[])Enum.GetValues(typeof(SplitChar)))
                    if (Regex.IsMatch(source, $"{basePattern}[{(char)sc}]{basePattern}"))
                        return sc;
            }

            return defaultChar;
        }
        #endregion
    }

    public sealed class CommandLineDataValue
    {
        #region Properties
        private string _data = "";
        #endregion

        #region Constructors
        public CommandLineDataValue(string source = "") =>
            Source = source;
        #endregion

        #region Accessors
        public int Length => _data.Length;

        public string Source
        {
            get => _data.Trim();
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    value = value.Trim();
                    if (value.Length == 0 || Regex.IsMatch(value, @"^(\x22{2}|'{2}|\[\])$"))
                        _data = "";
                    else
                    {
                        if ( Regex.IsMatch( value, @"^([\x22]([^\x22]+)[\x22]|[']([^']+)[']|[\[]([^\]]+)[\]])$") )
                        {
                            _data = Regex.Replace( value, @"^[\[\x22'](?<data>.+)[\]\x22']$", "${data}" );
                            Wrapper = value[ 0 ] switch
                            {
                                '"' => NetXpertExtensions.StripOuterOptions.DoubleQuotes,
                                '\'' => NetXpertExtensions.StripOuterOptions.SingleQuotes,
                                '[' => NetXpertExtensions.StripOuterOptions.SquareBrackets,
                                _ => NetXpertExtensions.StripOuterOptions.None
                            };
                        }
                        else
                        {
                            _data = value;
                            Wrapper = Regex.IsMatch(value, @"[\s]*") ? NetXpertExtensions.StripOuterOptions.DoubleQuotes : NetXpertExtensions.StripOuterOptions.None;
                        }
                    }

                    MatchCollection matches = Regex.Matches(_data, @"([\\]{1,2}[xX]([\da-fA-F]{2}))");
                    Dictionary<byte, char> chars = new();
                    foreach (Match m in matches)
                    {
                        byte b = byte.Parse(m.Groups[2].Value);
                        if (!chars.ContainsKey(b)) chars.Add(b, (char)b);
                    }

                    foreach (KeyValuePair<byte, char> c in chars)
                        _data = Regex.Replace(_data, @"([\\]{1,2}[xX][\da-fA-F])", c.Value.ToString());

                    return;
                }
                throw Language.Prompt.GetException(0, new object[] { value });
                //throw new ArgumentException( $"The supplied string \x22{value}\x22 isn't valid." );
            }
        }

        public NetXpertExtensions.StripOuterOptions Wrapper { get; set; } =
            NetXpertExtensions.StripOuterOptions.DoubleQuotes;

        /// <summary>Returns the raw data stored in this object, without any wrappers.</summary>
        public string Value => _data;
        #endregion

        #region Operators
        public static implicit operator CommandLineDataValue(string source) => new(source);
        public static implicit operator string(CommandLineDataValue source) => source.ToString();
        #endregion

        #region Methods
        public T As<T>() => (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromString(_data);

        public override string ToString()
        {
            char[] wrap = new char[] { '\x00', '\x00' };
            if (Wrapper.HasFlag(NetXpertExtensions.StripOuterOptions.DblAnglBrackets))
                wrap = new char[] { '«', '»' };
            if (Wrapper.HasFlag(NetXpertExtensions.StripOuterOptions.BraceBrackets))
                wrap = new char[] { '{', '}' };
            if (Wrapper.HasFlag(NetXpertExtensions.StripOuterOptions.AngleBrackets))
                wrap = new char[] { '<', '>' };
            if (Wrapper.HasFlag(NetXpertExtensions.StripOuterOptions.SquareBrackets))
                wrap = new char[] { '[', ']' };
            if (Wrapper.HasFlag(NetXpertExtensions.StripOuterOptions.RoundBrackets))
                wrap = new char[] { '(', ')' };
            if (Wrapper.HasFlag(NetXpertExtensions.StripOuterOptions.BackQuotes))
                wrap = new char[] { '`', '`' };
            if (Wrapper.HasFlag(NetXpertExtensions.StripOuterOptions.SingleQuotes))
                wrap = new char[] { '\'', '\'' };
            if (Wrapper.HasFlag(NetXpertExtensions.StripOuterOptions.DoubleQuotes))
                wrap = new char[] { '"', '"' };

            string data = _data;
            if (data.IndexOf(wrap[0]) >= 0)
                data = data.Replace(wrap[0].ToString(), "\\" + ((byte)wrap[0]).ToString("X2"));
            if (wrap[1] != wrap[0] && data.IndexOf(wrap[0]) >= 0)
                data = data.Replace(wrap[1].ToString(), "\\" + ((byte)wrap[1]).ToString("X2"));

            if (_data.Length == 0)
                return Wrapper == NetXpertExtensions.StripOuterOptions.None ? string.Empty : string.Join("", wrap);

            return Wrapper == NetXpertExtensions.StripOuterOptions.None ? _data : wrap[0] + _data + wrap[1];
        }
        #endregion
    }

    /// <summary>A class for managing indvidual data segments for each switch.</summary>
    public sealed class CommandLineData : IEnumerator<CommandLineDataValue>
    {
        #region Properties
        private List<CommandLineDataValue> _data = new();
        private Utility.SplitChar _splitChar = Utility.SplitChar.None;
        private int _position = 0;
        #endregion

        #region Constructors
        public CommandLineData(string source = "", Utility.SplitChar splitChar = Utility.SplitChar.Pipe)
        {
            SplitChar = splitChar;
            Parse(source);
        }
        #endregion

        #region Operators
        public static implicit operator string(CommandLineData source) => source is null ? "" : source.ToString();

        public static implicit operator CommandLineData(string source) => new(source);
        #endregion

        #region Accessors
        public CommandLineDataValue this[int index]
        {
            get
            {
                if (index < 0) return _data is null ? "" : _data.ToString();
                if (index.InRange(_data)) return _data[index];
                throw new IndexOutOfRangeException($"{index} lies outside the bounds of this collection (0-{Count})");
            }

            set
            {
                if (index.InRange(_data))
                {
                    if (!string.IsNullOrWhiteSpace(value))
                        _data[index] = new CommandLineDataValue(value);
                    else
                        _data.RemoveAt(index);
                }
                else
                    throw new IndexOutOfRangeException($"{index} lies outside the bounds of this collection (0-{Count})");
            }
        }

        /// <summary>Facilitates dereferencing / changing the defined split character for the data.</summary>
        public Utility.SplitChar SplitChar
        {
            get => _splitChar;
            set
            {
                // Can't set the split character to "Unknown"...
                if (value != Utility.SplitChar.Unknown)
                    _splitChar = value;
            }
        }

        /// <summary>Returns TRUE if the SplitChar is a value other than "None", and the payload contains more than one value.</summary>
        public bool IsMultiValue => _data.Count > 0;

        /// <summary>Reports on the overall length of the data managed by this object (in characters).</summary>
        public int Length => ToString().Length;

        /// <summary>Reports on the number of pieces of data (delimited by the split character) managed by this object.</summary>
        public int Count => _data.Count;

        // IEnumerator support
        CommandLineDataValue IEnumerator<CommandLineDataValue>.Current => _data[_position];

        object IEnumerator.Current => _data[_position];
        #endregion

        #region Methods
        /// <summary>Parses this argument data into parts if it's a multiple-value string.</summary>
        /// <param name="splitChar">The character by which the switch data should be broken up.</param>
        /// <returns>A string array containing the data split into pieces according to the specified SplitChar.</returns>
        /// <remarks>If the split character occurs within a string demarcated by single or double quotes, it will be
        /// treated as a normal character. If the SplitChar is set to "None", the entire data portion of the switch 
        /// is returned as the sole element of the resultant array.</remarks>
        private string[] ExtractValues(string source, Utility.SplitChar splitChar = Utility.SplitChar.Unknown)
        {
            // Establish the splitChar to use.
            if (splitChar == Utility.SplitChar.Unknown) splitChar = SplitChar; else SplitChar = splitChar;
            if (!string.IsNullOrWhiteSpace(source))
            {
                // If SplitChar.None is specified, simply return the data as-is (in an array).
                if (splitChar == Utility.SplitChar.None)
                    return new string[] { source };

                // Establish needed values...
                List<string> pieces = new();
                string data = source;
                string pattern = /* language=regex */ // Defines the Regex pattern used to find quote-delineated values.
                    @"(?<data>['[\x22][^\]'x22]*['\x22]|[^$1\s]+)[\s]*([$1]|$)".Replace(new object[] { (char)splitChar });

                // Find and extract all values from the data that match the defined pattern:
                MatchCollection matches = Utility.Parser(pattern).Matches(source);
                if (matches.Count > 0)
                    foreach (Match m in matches)
                        if (data.Length > 0 && m.Groups["data"].Success)
                        {
                            pieces.Add(m.Groups["data"].Value.Trim());// Add the value to the results.
                            data = data.Replace(m.Value, "");           // Remove the result from the string.
                        }

                // If there are still elements within the string, split and extract them.
                if (data.Contains( (char)splitChar ) )
                {
                    string[] p = source.Split(new char[] { (char)splitChar }, StringSplitOptions.RemoveEmptyEntries);
                    if (p.Length > 0) pieces.AddRange(p);
                }

                return pieces.ToArray(); // Return the result.
            }

            return Array.Empty<string>(); // Return an empty result -> happens whenever the data segment is unpopulated.
        }

        /// <summary>Tests the data to see if the requested piece exists within it.</summary>
        /// <param name="value">The string to search for.</param>
        /// <param name="compMethod">The type of comparison to perform (default: OrginalIgnoreCase).</param>
        /// <param name="splitChar">The character that delineates multiple values within an individual string.</param>
        /// <returns>TRUE if the string equals the source, or has a value that matches it (for multi-value strings).</returns>
        public bool HasValue(string value, StringComparison compMethod = StringComparison.OrdinalIgnoreCase, Utility.SplitChar splitChar = Utility.SplitChar.Unknown)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                if (Regex.IsMatch(value, @"[\x22'][^\x22']*[\x22']", Utility.DefOptions))
                    value = value.Length < 3 ? "" : value.Substring(1, value.Length - 2);

                if (value.Length > 0 && !(_data is null) && _data.Count > 0 && !string.IsNullOrWhiteSpace(value))
                {
                    int i = -1; while (++i < _data.Count && !value.Equals(_data[i].Value, compMethod)) ;
                    return i < _data.Count;
                }
            }

            /* Original search - always returns true!
			if ( !( this._data is null ) && !string.IsNullOrWhiteSpace( value ) )
			{
				if ( splitChar == Utility.SplitChar.Unknown )
					splitChar = this.SplitChar;

				int i = -1;
				if ( Regex.IsMatch( value, @"[\x22'][^\x22']*][\x22']", Utility.DefOptions ) )
					value = value.Substring( 1, value.Length - 2 );

				string[] pieces = ExtractValues( value, splitChar );
				while ( ++i < pieces.Length ) // && !pieces[ i ].Equals( value, compMethod ) ) ;
				{
					string piece = pieces[ i ].Trim();
					if ( Regex.IsMatch( piece, @"[\x22'][^\x22']*][\x22']", Utility.DefOptions ) )
						piece = piece.Substring( 1, piece.Length - 2 );

					if ( value.Equals( piece, compMethod ) )
						return true;
				}
			}
			*/

            return false;
        }

        /// <summary>Deconstructs a supplied string.</summary>
        /// <param name="value">The string to parse and deconstruct.</param>
        /// <param name="splitChar">Optional character to use to segregate separate values.</param>
        public void Parse(string value, Utility.SplitChar splitChar = Utility.SplitChar.Unknown)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                value = value.Trim();
                if (Utility.ValidateArgument(value))
                {
                    if (splitChar == Utility.SplitChar.Unknown) splitChar = SplitChar;
                    _data = new List<CommandLineDataValue>();
                    foreach (string s in ExtractValues(value))
                        if (s.Length >= 0 && !Regex.IsMatch(s, @"^(\x22{2}|'{2}|\[\])$"))
                            _data.Add(new CommandLineDataValue(s.Trim()));
                }
                return;
            }

            throw new ArgumentException($"The supplied string \x22{value}\x22 isn't valid.");
        }

        public string[] ToArray()
        {
            List<string> result = new();
            foreach (CommandLineDataValue v in _data)
                result.Add(v.ToString());

            return result.ToArray();
        }

        /// <summary>Collates all known values into a single string.</summary>
        public override string ToString()
        {
            string result = "";
            foreach (CommandLineDataValue val in _data)
                result += result.Length > 0 ? (char)SplitChar + val.ToString() : val.ToString();

            return result;
        }

        /// <summary>Adds a provided string to the collection of values.</summary>
        public void Add(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                _data.Add(value);
        }

        /// <summary>Adds a provided array of strings to the collection of values.</summary>
        public void AddRange(string[] values)
        {
            foreach (string s in values)
                Add(s);
        }

        public bool Equals(string value, StringComparison comparer = StringComparison.OrdinalIgnoreCase) =>
            value.Equals(ToString(), comparer);

        #region IEnumerator support
        public IEnumerator<CommandLineDataValue> GetEnumerator() => _data.GetEnumerator();

        bool IEnumerator.MoveNext() => ++_position < Count;

        void IEnumerator.Reset() => _position = 0;

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        private void Dispose(bool disposing)
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
        #endregion
    }

    /// <summary>A class for managing an individual Switch.</summary>
    public sealed class CommandLineSwitch
    {
        #region Properties
        /// <summary>Stores the string that identifies the switch.</summary>
        private string _id = "";

        /// <summary>Stores a character that separates the switch from it's data/payload.</summary>
        private string _delimiter = "=";

        /// <summary>Stores any data that accompanies the switch.</summary>
        private CommandLineData _data;

        /// <summary>Stores the default switch identification character(s).</summary>
        private string _marker = "";
        #endregion

        #region Constructors
        /// <summary>Instantiates a new CommandLineSwitch object.</summary>
        /// <param name="id">A string specifying the switch's base identifier.</param>
        /// <param name="data">An optional string that specifyies the switch's payload/data value.</param>
        /// <param name="marker">An optional parameter that specifies the character(s) that identify switches (allowed: -, -- or /)</param>
        /// <param name="delimiter">An optional parameter that specifies the character that separates the switch's ID from its Value</param>
        /// <param name="splitChar">
        /// An optional Utility.SplitChar value that specifies the method for parsing multiple-value data strings in the payload.
        /// </param>
        public CommandLineSwitch(string id, string data = "", string marker = "-", string delimiter = "=", Utility.SplitChar splitChar = Utility.SplitChar.None)
        {
            Id = id;
            Delimiter = delimiter;
            Marker = marker;
            _data = string.IsNullOrWhiteSpace(data) ? null : new CommandLineData(data, splitChar);
        }

        /// <summary>Instantiates a new CommandLineSwitch object from a switch-format-compliant string.</summary>
        /// <param name="source">The string to parse into a switch.</param>
        /// <param name="splitChar">The Utility.SplitChar value to assign to the switch.</param>
        public CommandLineSwitch(string source, Utility.SplitChar splitChar = Utility.SplitChar.Pipe)
        {
            //this._data.SplitChar = splitChar;
            CommandLineSwitch p = Parse(source, splitChar);
            Id = p.Id;
            Delimiter = p.Delimiter;
            Marker = p.Marker;
            _data = p._data;
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
            get => _id.ToUpperInvariant();
            set
            {
                if (_id == "" && value.Trim().Length > 0 && Regex.IsMatch(value, @"^[a-z?][\w]*[a-z0-9]?$", RegexOptions.IgnoreCase))
                    _id = value;
            }
        }

        /// <summary>Gets or Sets the character (typically ':' or '=') that separates the switch ID from the payload.</summary>
        public string Delimiter
        {
            get => _delimiter;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    value = value.Trim();
                    if (">=:".IndexOf(value[0]) > 0) _delimiter = value.Substring(0, 1);
                }
            }
        }

        /// <summary>Gets or Sets the marker to use ('/' or '-') when parsing, or exporting this switch.</summary>
        public string Marker
        {
            get => _marker;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    value = value.Trim();
                    if (">/-".IndexOf(value[0]) > 0) _marker = value.Substring(0, 1);
                }
            }
        }

        /// <summary>Gets or Sets the payload for the switch.</summary>
        public string Value
        {
            get => _data is null || _data.Length == 0 ? "" : _data.ToString();
            set
            {
                if (value is null || string.IsNullOrWhiteSpace(value))
                    _data = null;
                else
                {
                    Utility.SplitChar sC = _data is null ? Utility.ParseSplitChar(value) : _data.SplitChar;
                    _data = new CommandLineData(value, sC);
                }
            }
        }

        /// <summary>Returns all provided values in an array of strings.</summary>
        public string[] Values => _data is null ? new string[] { } : _data.ToArray();

        /// <summary>Returns TRUE if the object represents a valid switch (i.e. if the ID has been assigned to it).</summary>
        public bool IsValid => _id.Length > 0;

        /// <summary>Returns TRUE if the data value for the switch is null or unpopulated.</summary>
        //public bool IsEmpty => this._data is null;

        /// <summary>Reports the number of sub-elements (values) contained within this switch.</summary>
        public int Count => Values.Length;

        /// <summary>Facilitates checking / modifying the Value split character.</summary>
        public Utility.SplitChar SplitChar => _data is null ? Utility.SplitChar.None : _data.SplitChar;

        /// <summary>If the contained data has sub-elements, this returns TRUE, otherwise FALSE.</summary>
        public bool HasMultipleValues => _data is not null && _data.IsMultiValue;

        /// <summary>Reports on whether the Value of this switch is null or empty.</summary>
        public bool IsEmpty => _data is null || string.IsNullOrEmpty(_data);
        #endregion

        #region Operators
        /// <summary>Reports two ArgSwitch objects as being "equal" if their ID's match (case-insensitive).</summary>
        public static bool operator ==(CommandLineSwitch left, CommandLineSwitch right)
        {
            if (left is null) return right is null;
            if (left.Id == "" || right is null || right.Id == "") return false;
            return left.Id == right.Id;
        }

        /// <summary>
        /// This override pulls double duty: If the provided string can be parsed as an ArgSwitch, it will report TRUE
        /// if it matches the ID of the companion ArgSwitch object. If the string ISN'T parseable as an ArgSwitch, this
        /// comparison will return TRUE if the supplied string matches the companion ArgSwitch's VALUE.
        /// </summary>
        public static bool operator ==(string left, CommandLineSwitch right)
        {
            left = left.Trim(); // Remove leading or trailing whitespace...
            if (left is null || left == "") return right is null || right.Id == ""; // String is empty or null

            // If the left string is a valid ArgSwitch string, parse it and compare...
            if (Validate(left)) return Parse(left, right.SplitChar) == right;

            // ...otherwise just compare the string against the ArgSwitch object's Value (case-insensitive!)
            return left.Equals(right.Value);
        }

        public static bool operator !=(CommandLineSwitch left, CommandLineSwitch right) { return !(left == right); }
        public static bool operator !=(string left, CommandLineSwitch right) { return !(left == right); }
        #endregion

        #region Methods
        /// <summary>Attempts to return the data package of this switch in the specified type.</summary>
        /// <typeparam name="T">The desired Type for the returned value.</typeparam>
        /// <param name="index">Which value to dereference.</param>
        /// <returns>An object of the specified Type, derived from the value of this switch.</returns>
        public T As<T>(int index = -1) => _data[index].As<T>();
        //{
        //	return this._data[]
        //	string value = "";
        //	if ( index < 0 )
        //	{
        //		if ( this._data.Count > 0 )
        //			value = this._data[ 0 ].ToString();
        //		else
        //		{
        //			if ( this._data.Length > 0 ) value = this._data.ToString();
        //		}
        //	}
        //	else
        //	{
        //		if ( index.InRange( _data.Count, 0, null ) )
        //			value = this._data[ index ].ToString();
        //	}	

        //	return (T)TypeDescriptor.GetConverter( typeof( T ) ).ConvertFromString( value );
        //}

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
            !(_data is null) && (string.IsNullOrEmpty(check) ? _data.Length > 0 : _data.HasValue(check, comparer));

        /// <summary>Checks to see if any ONE string from a collection, is present in the switch's Value.</summary>
        /// <param name="values">An array of strings to search for in this switch Value.</param>
        /// <param name="comparer">Defines how to compare the values.</param>
        /// <returns>TRUE if any of the supplied values is found in the switch's Value.</returns>
        public bool HasValue(string[] values, StringComparison comparer = StringComparison.OrdinalIgnoreCase)
        {
            int i = -1;
            if (!(values is null))
                while (++i < values.Length && !string.IsNullOrWhiteSpace(values[i]) && !HasValue(values[i], comparer)) ;

            return i.InRange(values);
        }

        /// <summary>Compares the currently stored value with the string version of a supplied value.</summary>
        /// <param name="value">A value to compare with the one stored in the switch.</param>
        /// <param name="stringComparison">An Enum specifying the kind of match to test against.</param>
        /// <returns>TRUE if the stored value of the switch matches the string version of the supplied comparator.</returns>
        public bool IsEqualTo(dynamic compare, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase) =>
            _data.Equals(compare.ToString(), stringComparison);

        public override string ToString()
        {
            if (IsValid)
            {
                string result = _marker + Id;
                if (Value.Length > 0) result += Delimiter + Value;
                return result;
            }

            return Value;
        }

        /// <summary>Compares a provided ArgSwitch's Id against this one and returns a value indicating the outcome.</summary>
        /// <param name="value">An ArgSwitch value to compare against.</param>
        /// <returns>-1 if the supplied ArgSwitch's Id is greater than ours, +1 if it's less than, and 0 if they're equal.</returns>
        public int CompareTo(CommandLineSwitch value) =>
            Id.CompareTo(value.Id);

        public override int GetHashCode() => base.GetHashCode();
        public override bool Equals(object obj) => base.Equals(obj);
        #endregion

        #region Static Methods
        /// <summary>Parses a passed string into a switch object without data.</summary>
        /// <param name="source">The string to parse. Must have either a dash or slash as the first character! ('/' or '-').</param>
        /// <returns>A new, populated, ArgSwitch object configured from the provided string value or NULL if parsing was unsuccessful.</returns>
        public static CommandLineSwitch Parse(string source, Utility.SplitChar splitChar = Utility.SplitChar.None)
        {
            if (Validate(source))
            {
                if (splitChar == Utility.SplitChar.Unknown) splitChar = Utility.SplitChar.None;
                //Regex parser = new Regex(PATTERN, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
                Match match = Regex.Match(source, Utility.SWITCH, Utility.DefOptions);
                if (match.Groups.Count > 3)
                {
                    return new CommandLineSwitch(
                        match.Groups["id"].Value,      // base Id
                        match.Groups["data"].Value,    // payload
                        match.Groups["swsgn"].Value,   // marker (switch indicator)
                        match.Groups["opsgn"].Value,   // delimiter (separates switch base id from value)
                        Utility.ParseSplitChar(match.Groups["data"].Value, splitChar) // How to parse the payload on multi-value strings.
                    );
                }
            }
            return null;
        }

        /// <summary>Checks a string and determines if it conforms to the default regex pattern for a switch.</summary>
        /// <param name="test">The string to check against the pattern.</param>
        /// <returns>TRUE if the passed string conforms to the pattern, otherwise FALSE.</returns>
        public static bool Validate(string test) =>
            !string.IsNullOrEmpty(test) && Utility.Parser().IsMatch(test);
        //(test.Length < 2) ? false : Regex.IsMatch(test, "^[" + markers + "][a-z0-9?]+([:=].+)?$",RegexOptions.IgnoreCase);
        #endregion
    }

    /// <remarks>/status /close /init /apikey:"request" /ping:[count] /posttest /test:data hello "world at war"</remarks>
    public sealed class CommandLineSwitches : IEnumerator<CommandLineSwitch>
    {
        #region Properties
        private int _position = 0;
        private List<CommandLineSwitch> _items = new();
        #endregion

        #region Constructors
        public CommandLineSwitches() { }

        public CommandLineSwitches(string source) => Parse(source);
        #endregion

        #region Accessors
        public int Count => _items.Count;

        public CommandLineSwitch this[int index]
        {
            get
            {
                if (index.InRange(_items))
                    return _items[index];

                throw new IndexOutOfRangeException($"{index} is out of range for this collections. (0-{Count - 1}");
            }
        }

        public CommandLineSwitch this[string switchName]
        {
            get
            {
                int i = IndexOf(switchName);
                return i < 0 ? null : _items[i];
            }

            private set
            {
                int i = IndexOf(switchName);
                if (value is null)
                    _items.RemoveAt(i);
                else
                    if (i < 0)
                    _items.Add(value);
                else
                    _items[i] = value;
            }
        }

        // IEnumerator support
        CommandLineSwitch IEnumerator<CommandLineSwitch>.Current => _items[_position];

        object IEnumerator.Current => _items[_position];
        #endregion

        #region Methods
        private int IndexOf(string name)
        {
            int i = -1;
            if (!string.IsNullOrWhiteSpace(name))
                while (++i < Count && !name.Equals(_items[i].Id, StringComparison.OrdinalIgnoreCase)) ;

            return i < Count ? i : -1;
        }

        public bool HasSwitch(string name) => IndexOf(name) >= 0;

        public bool HasSwitch(CommandLineSwitch sw) => IndexOf(sw.Id) >= 0;

        public void Parse(string source, Utility.SplitChar splitChar = Utility.SplitChar.None)
        {
            if (!string.IsNullOrEmpty(source))
            {
                _items = new List<CommandLineSwitch>();
                MatchCollection matches = Regex.Matches(source, Utility.SWITCH, Utility.DefOptions);
                foreach (Match m in matches)
                    _items.Add(CommandLineSwitch.Parse(m.Value, splitChar));
            }
        }

        public void Add(CommandLineSwitch sw)
        {
            int i = IndexOf(sw.Id);
            if (i < 0)
                _items.Add(sw);
            else
                _items[i] = sw;
        }

        public void Add(string source, Utility.SplitChar splitChar = Utility.SplitChar.None) =>
            Add(CommandLineSwitch.Parse(source, splitChar));

        public void AddRange(CommandLineSwitch[] switches)
        {
            if (!(switches is null))
                foreach (CommandLineSwitch sw in switches)
                    Add(sw);
        }

        public int Remove(string id)
        {
            int i = IndexOf(id);
            if (i >= 0)
                _items.RemoveAt(i);

            return i;
        }

        public void RemoveAt(int index)
        {
            if (index.InRange(_items))
                _items.RemoveAt(index);
        }

        internal class SwitchComparer : IComparer<CommandLineSwitch>
        {
            public int Compare(CommandLineSwitch left, CommandLineSwitch right) =>
                left.Id.CompareTo(right.Id);
        }

        public void Sort() =>
            _items.Sort(new SwitchComparer());

        public CommandLineSwitch[] ToArray() => _items.ToArray();

        public override string ToString()
        {
            string result = "";
            foreach (CommandLineSwitch sw in _items)
                result += $" {sw}";

            return result.TrimStart();
        }

        #region IEnumerator support
        public IEnumerator<CommandLineSwitch> GetEnumerator() => _items.GetEnumerator();

        bool IEnumerator.MoveNext() => ++_position < Count;

        void IEnumerator.Reset() => _position = 0;

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        private void Dispose(bool disposing)
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
        #endregion
    }

    /// <summary>Stores and manages command line values that are not switches.</summary>
    public sealed class CommandLineArgs : IEnumerator<CommandLineDataValue>
    {
        #region Properties
        private List<CommandLineDataValue> _data = new();
        public int _position = 0;
        #endregion

        #region Constructors
        public CommandLineArgs() { }

        public CommandLineArgs(string source) => Parse(source);
        #endregion

        #region Operators
        public static implicit operator string(CommandLineArgs source) => source.ToString();
        public static implicit operator CommandLineArgs(string source) => new(source);
        #endregion

        #region Accessors
        public CommandLineDataValue this[int index]
        {
            get
            {
                if (index.InRange(_data)) return _data[index];
                throw new IndexOutOfRangeException($"{index} is out of range for this collections. (0-{Count - 1}");
            }
        }

        public int Count => _data.Count;

        // IEnumerator support
        CommandLineDataValue IEnumerator<CommandLineDataValue>.Current => _data[_position];

        object IEnumerator.Current => _data[_position];
        #endregion

        #region Methods
        public void Add(string argument)
        {
            if (!string.IsNullOrWhiteSpace(argument))
                _data.Add(new CommandLineDataValue(argument));
        }

        public void Add(CommandLineDataValue argument)
        {
            if (!(argument is null))
                _data.Add(argument);
        }

        public void AddRange(string[] args)
        {
            if (!(args is null))
                foreach (string arg in args)
                    Add(arg);
        }

        public void AddRange(CommandLineDataValue[] args)
        {
            if (!(args is null))
                foreach (string arg in args)
                    Add(arg);
        }

        public CommandLineDataValue RemoveAt(int index)
        {
            int i = index.InRange(_data) ? index : -1;
            if (i < 0) return null;

            CommandLineDataValue v = _data[i];
            _data.RemoveAt(i);
            return v;
        }

        public bool Parse(string source, bool throwExc = false)
        {
            if (string.IsNullOrWhiteSpace(source) && throwExc)
                if (throwExc) throw new FormatException("The supplied string does not contain valid arguments.");

            MatchCollection matches = Utility.Parser(Utility.ARGUMENT).Matches(source);
            if (matches.Count > 0)
            {
                _data.Clear();
                foreach (Match m in matches)
                    _data.Add(new CommandLineDataValue(m.Value));

                return true;
            }

            return false;
        }

        public CommandLineDataValue[] ToArray() => _data.ToArray();

        public override string ToString()
        {
            string result = "";
            foreach (CommandLineDataValue arg in _data)
                result += $" {arg}";

            return result.TrimStart();
        }

        #region IEnumerator support
        public IEnumerator<CommandLineDataValue> GetEnumerator() => _data.GetEnumerator();

        bool IEnumerator.MoveNext() => ++_position < Count;

        void IEnumerator.Reset() => _position = 0;

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        private void Dispose(bool disposing)
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
        #endregion
    }

    public sealed class CommandLine
    {
        #region Properties
        private string _source = "", _cmd = "";
        private CommandLineSwitches _switches = new();
        private CommandLineArgs _args = new();
        #endregion

        #region Constructors
        public CommandLine(string source) => Parse(source);
        #endregion

        #region Accessors
        public int ArgCount => _args.Count;

        public int SwitchCount => _switches.Count;

        /// <summary>Reports on whether or not this Commandline has arguments and/or switches.</summary>
        /// <returns>TRUE if there are any Arguments OR Switches defined for this command.</returns>
        public bool HasArguments => ArgCount + SwitchCount > 0;

        /// <summary>Accesses any stored string arguments.</summary>
        /// <param name="index">An Int value specifying the index of the argument to dereference.</param>
        public CommandLineDataValue this[int index] => _args[index];

        /// <summary>Facilitates Accessing a switch by it's name (Case-Insensitive).</summary>
        /// <param name="id">The Id (name) of the switch to find.</param>
        public CommandLineSwitch this[string id] => _switches[id];

        /// <summary>Contains the full source string that represents the data as it was originally passed.</summary>
        public string Source
        {
            get => _source;
            private set => Parse(value);
        }

        public string Cmd
        {
            get => _cmd.ToUpperInvariant();
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    if (Utility.Parser( /* language=regex */ @"^[a-zA-Z][\w]*[a-zA-Z0-9]$").IsMatch(value.Trim()))
                        _cmd = value.Trim();
                }
            }
        }

        public CommandLineSwitches Switches => _switches;

        public CommandLineArgs Args => _args;
        #endregion

        #region Operators
        public static implicit operator CommandLine(string source) => new(source);
        public static implicit operator string(CommandLine source) => source.Source;
        #endregion

        #region Methods
        public bool HasSwitch(string id) => _switches.HasSwitch(id);

        public bool Parse(string source, bool throwExc = false, Utility.SplitChar defaultSplitChar = Utility.SplitChar.None)
        {
            _source = "";
            Regex parser = Utility.Parser(Utility.COMMANDLINE);
            if (!string.IsNullOrWhiteSpace(source) && parser.IsMatch(source))
            {
                MatchCollection matches = parser.Matches(source.Trim());
                if (matches[0].Groups["cmd"].Success)
                {
                    _source = source;
                    Cmd = matches[0].Groups["cmd"].Value; // Extract the command.
                    if (matches[0].Groups["data"].Success)  // source.Length > _cmd.Length )
                    {
                        source = matches[0].Groups["data"].Value; // Remove leading command and spaces.

                        // Find all segments that correspond with the Switch pattern...
                        matches = Utility.Parser(Utility.SWITCH).Matches(source);
                        if (matches.Count > 0)
                            foreach (Match m in matches)
                            {
                                _switches.Add(m.Value);
                                source = source.Replace(m.Value, ""); // Remove this switch from the parsing string
                            }

                        // Find all remaining segments that correspond with the Argument pattern...
                        matches = Utility.Parser(Utility.ARGUMENT).Matches(source);
                        if (matches.Count > 0)
                            foreach (Match m in matches)
                                _args.Add(m.Value);
                    }
                    return true;
                }
            }

            _source = "[INVALID]: \x22" + source + "\x22;";

            if (throwExc) throw new FormatException("The supplied string is not a valid command string.");
            return false;
        }

        public override string ToString() =>
            $"{Cmd} {Args} {Switches}".Trim();
        #endregion

        /// <summary>Provides a CommandLine object populated with the command line issued when the source program was executed.</summary>
        public static CommandLine LaunchCommand() =>
            System.Diagnostics.Process.GetCurrentProcess().ProcessName + " " + string.Join(" ", Environment.GetCommandLineArgs());
    }
}
