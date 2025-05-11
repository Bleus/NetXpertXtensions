using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using NetXpertCodeLibrary.Extensions;

namespace NetXpertCodeLibrary.ConsoleFunctions
{
	/// <remarks> Sample internal HelpData.xml:
	///    <?xml version="1.0" encoding="utf-8" standalone="yes"?>
	///    <!DOCTYPE cobblestone[
	///      <!ENTITY bksp "\b">
	///      <!ENTITY tab "\t">
	///      <!ENTITY space " ">
	///      <!ENTITY quote '"'>
	///      <!ENTITY apos "'">
	///      <!ENTITY lt "<">
	///      <!ENTITY gt ">">
	///      <!ENTITY laquo "«">
	///      <!ENTITY raquo "»">
	///      <!ENTITY copy "©">
	///      <!ENTITY bull "•">
	///    ]>
	///    <cobblestone>
	///       <category id="header">
	///          <p pre="0" post="1" style="foreColor:Default;backColor:Default;">
	///             <text addcr = "true" style="foreColor:black;backColor:gold;">[Cobblestone Properties Desktop Application Console]</text>
	///             <text addcr = "true" style="backColor:Default;">Written By Brett Leuszler</text>
	///             <text addcr = "true" style="backColor:Default;" > (c)Copyright 2018</text>
	///	            All Rights Reserved.
	///          </p>
	///       </category>
	///       <category id="ver">
	///	         <p indent="3" pre="0" post="1">
	///	            &raquo; Applet[<text style="foreColor:white;backColor:black;">{$cmdName}</text>] (
	///		        <text style="foreColor:LightCyan;"></text>
	///	         </p>
	///       </category>
	///       <category id="help">
	///          <intro copyright="2018, Brett Leuszler">
	///	            <p pre="0" post="1">
	///                <text style="foreColor:Yellow;" >{$cmdName}</text>
	///                &space;- a Cobblestone Properties Command Line Applet(
	///                <text style="foreColor:White;" > &space; v{$cmdVersion}</text>)
	///             </p>
	///             <p pre="0" post="2">Copyright © {$copyright}. All Rights Reserved.</p>
	///          </intro>
	///          <switchset keys="/ver /help" >
	///		        <switch id="VER">
	///		           <shortHelp>Switch help for this.">More detailed help here.</shortHelp>
	///		           <longHelp>
	///					   Detailed help for this switch.
	///		           </longHelp>
	///		        </switch>
	///             <switch id="HELP">
	///                <shortHelp>Switch help for this.">More detailed help here.</shortHelp>
	///                <longHelp>
	///					   Detailed help for this switch.
	///                </longHelp>
	///             </switch>
	///          </switchset>
	///       </category>
	///    </cobblestone>
	/// </remarks>

	/// <summary>Manages a parameter-value pair for performing parameterized value substitutions within a block of text.</summary>
	public class ParseParameter
	{
		#region Properties
		/// <summary>Defines the pattern for acceptable parameter Id values.</summary>
		/// <remarks>Format: {{$label}}</remarks>
		public const string PATTERN = @"{{\$2}[\w]+}{2}";

		/// <summary>Contains the parameter that is to be replaced.</summary>
		protected string _paramId = "";

		/// <summary>Contains the value to replace the parameter with.</summary>
		protected string _value = "";
		#endregion

		#region Constructors
		/// <summary>Creates a new XmlParseParameter object from a supplied pair of strings.</summary>
		/// <param name="id">Used to define the Id value.</param>
		/// <param name="value">Used to define the Value.</param>
		public ParseParameter(string id, string value = "")
		{
			this.Id = id;
			this.Value = value;
		}

		/// <summary>Creates a new XmlParseParameter object from a supplied KeyValuePair object.</summary>
		/// <param name="data">A KeyValuePair object whose Key becomes our Id and whose Value becomes ours.</param>
		public ParseParameter(KeyValuePair<string, string> data)
		{
			this.Id = data.Key;
			this.Value = data.Value;
		}

		/// <summary>Creates a parameter value from a supplied array of strings (of at least length 1).</summary>
		/// <param name="parameter">
		/// An array of strings with at least a single value. The first value in the array is assigned to the parameter Id
		/// with any remaining values being JOIN'd (with a space) as the Value.
		/// </param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public ParseParameter(string[] parameter)
		{
			if ((parameter is null) || (parameter.Length == 0) || (parameter[0].Trim().Length == 0))
				throw new ArgumentOutOfRangeException("The Id value cannot be empty, null or whitespace!");

			this.Id = parameter[0];
			this.Value = (parameter.Length > 1) ? String.Join(" ", parameter, 1) : "";
		}
		#endregion

		#region Operators
		/// <summary>Compares two XmlParseParameter objects and returns FALSE if their Id's match (case-insensitive!).</summary>
		public static bool operator !=(ParseParameter left, ParseParameter right) => !(left == right);
		/// <summary>Compares two XmlParseParameter objects and returns TRUE (equal) if their Id's match (case-insensitive!).</summary>
		public static bool operator ==(ParseParameter left, ParseParameter right)
		{
			if (left is null) return (right is null);
			if (right is null) return false;
			return left.Id.Equals(right.Id, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>Compares an XmlParseParameter object against a string and returns FALSE if the Id matches the string (case-insensitive!).</summary>
		public static bool operator !=(ParseParameter left, string right) => !(left == right);
		/// <summary>Compares an XmlParseParameter object against a string and returns TRUE (equal) if the Id matches the string (case-insensitive!).</summary>
		public static bool operator ==(ParseParameter left, string right)
		{
			if (left is null) return string.IsNullOrWhiteSpace(right) || (right.Length == 0);
			if (right is null) return false;
			return left.Id.Equals(right, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>Compares an XmlParseParameter object against a string and returns FALSE if the Id matches the string (case-insensitive!).</summary>
		public static bool operator !=(string left, ParseParameter right) => !(right == left);
		/// <summary>Compares an XmlParseParameter object against a string and returns TRUE (equal) if the Id matches the string (case-insensitive!).</summary>
		public static bool operator ==(string left, ParseParameter right) => (right == left);
		#endregion

		#region Accessors
		/// <summary>Gets/Sets the parameter Id value in appropriate formats.</summary>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="FormatException"></exception>
		/// <remarks>Format: {{$label}}</remarks>
		public string Id
		{
			get => "{{$" + this._paramId + "}}";
			protected set
			{
				if (string.IsNullOrWhiteSpace(value) || (value.Trim().Length == 0))
					throw new ArgumentOutOfRangeException("The Id value cannot be empty, null or whitespace!");

				if (ValidateParameter(value))
				{
					string v = value.Trim();
					if (v.Match(@"^" + PATTERN + @"$"))
						v = v.Substring(3, v.Length - 5); // Remove leading and trailing brace brackets.

					this._value = v;
				}
				else
					throw new FormatException("The supplied Id value (\"" + value + "\") is not appropriate for use as a parameter id.");
			}
		}

		public string Value { get => this._value; set => this._value = value; }
		#endregion

		#region Methods
		/// <summary>
		/// This function is intended to be called using a block of text containing ParseParameter blocks that are to be
		/// replaced with the data contained in this class.
		/// </summary>
		/// <param name="source">A string containing the block of text who's parameters are to potentially be modified by this object.</param>
		/// <returns>The passed text with any references to this class</returns>
		public string Parse(string source)
		{
			while (source.IndexOf(this.Id) >= 0)
				source = source.Replace(this.Id, this.Value);

			return source;
		}

		public override bool Equals(object obj) => base.Equals(obj);
		public override int GetHashCode() => base.GetHashCode();
		public override string ToString() => this.Id + " => \"" + this.Value + "\";";

		/// Facilitate some simple format conversions to/from KeyValuePair and string[] types...
		public static implicit operator KeyValuePair<string, string>(ParseParameter data) => new KeyValuePair<string, string>(data.Id, data.Value);
		public static implicit operator ParseParameter(KeyValuePair<string, string> data) => new ParseParameter(data.Key, data.Value);
		public static implicit operator string[] (ParseParameter data) => new string[] { data.Id, data.Value };
		public static implicit operator ParseParameter(string[] data) => new ParseParameter(data);

		/// <summary>Checks a supplied string for suitability as a parameter.</summary>
		/// <param name="text">A string containing the text to validate as a potential parameter value.</param>
		/// <returns>TRUE if the supplied text comports with the format for being a parameter.</returns>
		/// <remarks>
		/// Although the true pattern for a parameter id consists of {{parameterId}}, this routine will validate with or
		/// without the braces as it's perfectly fine for either format to be passed into this routine (it will manage them
		/// for itself anyway).
		/// </remarks>
		public static bool ValidateParameter(string text) =>
			Regex.IsMatch(text.Trim(), @"^(" + PATTERN + @")|([\w]+)$", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Compiled);
		#endregion
	}

	/// <summary>[IEnumerator] Manages a collection of ParseParameter objects.</summary>
	public class ParameterCollection : IEnumerator<ParseParameter>
	{
		#region Properties
		/// <summary>Used for internal IEnumerable support.</summary>
		private int _position = 0;

		/// <summary>Holds the collection of XmlParseParameters that this object manages.</summary>
		protected List<ParseParameter> _collection = new List<ParseParameter>();
		#endregion

		#region Constructors
		public ParameterCollection() { }

		public ParameterCollection(string id, string value = "") =>
			this.Add(id, value);

		public ParameterCollection(ParseParameter parameter) =>
			this.Add(parameter);

		public ParameterCollection(ParseParameter[] parameters) =>
			this.AddRange(parameters);
		#endregion

		#region Accessors
		/// <summary>Reports the number of items currently being managed by the collection.</summary>
		public int Count => this._collection.Count;

		/// <summary>Facilitates direct access to the collection via index.</summary>
		/// <param name="index">An int value specifying the item to dereference.</param>
		public ParseParameter this[int index]
		{
			get => this._collection[index];
			set => this._collection[index] = value;
		}

		/// <summary>Facilitates dereferencing an item value from the collection by an its Id.</summary>
		/// <param name="id">The parameter id of the item to access.</param>
		/// <returns>If the specified id exists, it's Value, otherwise an empty string.</returns>
		/// <remarks>On a SET call, if the specified id doesn't already exist, it will be added.</remarks>
		public string this[string id]
		{
			get { int i = IndexOf(id); return (i < 0) ? "" : this[i].Value; }
			set
			{
				int i = IndexOf(id);
				if (i >= 0)
					this[i].Value = value;
				else
					this.Add(id, value);
			}
		}

		// IEnumerable support accessors...
		ParseParameter IEnumerator<ParseParameter>.Current => this[this._position];

		object IEnumerator.Current => this._collection[this._position];
		#endregion

		#region Methods
		/// <summary>Finds the index of a specified XmlParseParameter object in the collection based on its Id.</summary>
		/// <param name="id">A string specifying the Parameter Id find.</param>
		/// <returns>The index of the requested object, if found, otherwise -1.</returns>
		protected int IndexOf(string id)
		{
			int i = -1; while ((++i < this.Count) && (this[i].Id != id)) ;
			return (i < this.Count) ? i : -1;
		}

		/// <summary>Finds the index of a specified XmlParseParameter object in the collection.</summary>
		/// <param name="id">An XmlParseParameter object to find.</param>
		/// <returns>The index of the requested object, if found, otherwise -1.</returns>
		protected int IndexOf(ParseParameter id) =>
			IndexOf(id.Id);

		/// <summary>Adds or modifies an XmlParseParameter to the collection.</summary>
		/// <param name="p">An XmlParseParameter to modify (if it already exists) or add to the collection.</param>
		public void Add(ParseParameter p)
		{
			int i = this.IndexOf(p);
			if (i < 0)
				this._collection.Add(p);
			else
				this._collection[i] = p;
		}

		/// <summary>Adds or modifies an element in the collection using the provided Id and Value.</summary>
		/// <param name="id">A string specifying an XmlParseParameter identity to add/modify.</param>
		/// <param name="value">A string specifying the value to assign to the new XmlParseParameter.</param>
		public void Add(string id, string value) => Add(new ParseParameter(id, value));

		/// <summary>Adds an array of XmlParseParameters to the collection.</summary>
		public void AddRange(ParseParameter[] parameters)
		{
			foreach (ParseParameter p in parameters)
				this.Add(p);
		}

		/// <summary>Removes an XmlParseParameter from the collection by its Id value.</summary>
		/// <param name="id">A string specifying the Id of the element to remove.</param>
		public void Remove(string id)
		{
			int i = this.IndexOf(id);
			if (i >= 0)
				this._collection.RemoveAt(i);
		}

		/// <summary>Removes an XmlParseParameter from the collection.</summary>
		public void Remove(ParseParameter p) => this.Remove(p.Id);

		/// <summary>Removes an element from the collection by it's Index.</summary>
		public void RemoveAt(int index) => this._collection.RemoveAt(index);

		/// <summary>Empties the collection.</summary>
		public void Clear() => this._collection = new List<ParseParameter>();

		/// <summary>
		/// This function is intended to be called using a block of text containing ParseParameter blocks that is to be
		/// modified with the data contained in this library.
		/// </summary>
		/// <param name="source">A string containing the block of text who's parameters are to potentially be modified by this collection.</param>
		/// <returns>The passed text updated according to the information contained in this collection.</returns>
		public string Parse(string source)
		{
			foreach (ParseParameter p in this._collection)
				source = p.Parse(source);

			return source;
		}

		/// <summary>Outputs the contents of this collection as an array of XmlParseParameter objects.</summary>
		/// <returns>An array of XmlParseParameter objects culled from this collection.</returns>
		public ParseParameter[] ToArray() => this._collection.ToArray();

		// Facilitate simple data movement between XmlParameterCollection and an XmlParseParameter array...
		public static implicit operator ParameterCollection(ParseParameter[] data) => new ParameterCollection(data);
		public static implicit operator ParseParameter[] (ParameterCollection data) => data.ToArray();

		/// <summary>This routine creates a new XmlParameterCollection object with some basic intial generic parameters.</summary>
		/// <param name="cmdVersion">A value specifying the application version.</param>
		/// <param name="copyright">A value specifying the application Copyright declaration.</param>
		/// <param name="cmdName">An optional parameter specifying the application's name.</param>
		/// <returns>An XmlParameterCollection populated with the supplied values.</returns>
		public static ParameterCollection Create(string cmdVersion, string copyright, string cmdName = "")
		{
			ParameterCollection collection = new ParameterCollection(
					new ParseParameter[]
					{
					new ParseParameter("cmdVersion", cmdVersion),
					new ParseParameter("cmdCopy", copyright)
					}
				);

			if (!string.IsNullOrWhiteSpace(cmdName) && (cmdName.Length > 0)) collection.Add("cmdName", cmdName);
			return collection;
		}

		/// <summary>Returns a clean, empty XmlParameterCollection object.</summary>
		public static ParameterCollection Empty => new ParameterCollection();
		#endregion

		//IEnumerator Support
		public IEnumerator<ParseParameter> GetEnumerator() { return this._collection.GetEnumerator(); }

		bool IEnumerator.MoveNext() { return (++this._position) < this._collection.Count; }

		void IEnumerator.Reset() { this._position = 0; }

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

		#region Static Methods
		/// <summary>Converts an array of arrays of strings, into an array of XmlParseParameter objects.</summary>
		/// <param name="source">An array of string arrays to convert.</param>
		/// <returns>An array of XmlParseParameter objects populated from the supplied data.</returns>
		public static ParseParameter[] Convert(string[][] source)
		{
			ParameterCollection data = new ParameterCollection();
			foreach (string[] row in source)
				if (row.Length > 1)
					data.Add(row[0], String.Join(" ", row, 1));

			return data.ToArray();
		}
		#endregion
	}

	/// <summary>Provides a class for managing an Xml Entity.</summary>
	public class HelpXmlEntity
	{
		#region Properties
		/// <summary>A Regex pattern to identify / validate an Entity string.</summary>
		public const string PATTERN = @"&?[a-zA-Z0-9]{2,6};?";

		/// <summary>A Regex pattern for identifying valid translation text.</summary>
		public const string TRANSLATION_REGEX = @"(?:[\S| ]+)";

		/// <summary>A Regex pattern to identify an Xml Entity declaration from raw XML text.</summary>
		public const string ENTITY_REGEX = @"<[\s]?!ENTITY[ ]+(?<entity>" + PATTERN + @")[ ]+(?<translate>([""]" + TRANSLATION_REGEX + @"[""])|([']" + TRANSLATION_REGEX + @"[']))[ ]?>";

		/// <summary>Holds the raw Xml Entity identifier (stripped of leading ampersand and trailing semi-colons).</summary>
		/// <remarks>This is marked private to require outside elements to interact with it solely via the accessor.</remarks>
		private string _entity = "";

		/// <summary>Holds the replacement (translation) text that the entity corresponds with.</summary>
		/// <remarks>This is marked private to require outside elements to interact with it solely via the accessor.</remarks>
		private string _translate = "";
		#endregion

		#region Constructors
		/// <summary>Creates a new XmlEntity.</summary>
		/// <param name="entity">A string specifying the Entity identification string (i.e. "trade" => "&trade;")</param>
		/// <param name="translate">A string specifying the Translation value to substitute for the Entity.</param>
		/// <param name="library">An XmlEntityCollection from which to try and copy values if a translation string isn't provided.</param>
		public HelpXmlEntity(string entity, string translate = "", XmlEntityCollection library = null) =>
			this.Initialize(entity, translate, library);

		/// <summary>Creates a new XmlEntity.</summary>
		/// <param name="entity">An XmlNode object containing the relevant Entity declaration.</param>
		/// <param name="translate">A string specifying the Translation value to substitute for the Entity.</param>
		/// <param name="library">An XmlEntityCollection from which to try and copy values if a translation string isn't provided.</param>
		public HelpXmlEntity(XmlNode entity, string translate = "", XmlEntityCollection library = null)
		{
			if (entity.NodeType == XmlNodeType.EntityReference)
			{
				if ((translate is null) || (translate.Trim().Length == 0)) translate = Parse(entity.OuterXml).Translation;
				this.Initialize(entity.LocalName, translate, XmlEntityCollection.DefaultEntities());
			}
			else
				throw new ArgumentException("The supplied value is not a recognized XmlEntity.");
		}

		/// <summary>Creates a new XmlEntity.</summary>
		/// <param name="entity">An XmlEntity object containing the relevant Entity declaration.</param>
		/// <param name="translate">A string specifying the Translation value to substitute for the Entity.</param>
		/// <param name="library">An XmlEntityCollection from which to try and copy values if a translation string isn't provided.</param>
		public HelpXmlEntity(XmlEntity entity, string translate = "", XmlEntityCollection library = null)
		{
			if (entity.NodeType == XmlNodeType.EntityReference)
			{
				if ((translate is null) || (translate.Trim().Length == 0)) translate = Parse(entity.OuterXml).Translation;
				this.Initialize(entity.LocalName, translate, XmlEntityCollection.DefaultEntities());
			}
			else
				throw new ArgumentException("The supplied value is not a recognized XmlEntity.");
		}

		/// <summary>Initializes the object with the specified values.</summary>
		/// <param name="entity">A string specifying the entity identifier.</param>
		/// <param name="translate">A string specifying the corresponding text.</param>
		/// <param name="library">An XmlEntityCollection from which to try and copy values if a translation string isn't provided.</param>
		private void Initialize(string entity, string translate, XmlEntityCollection library)
		{
			if (library is null) library = XmlEntityCollection.DefaultEntities();
			translate = ((translate is null) || (translate.Trim().Length == 0)) && library.HasEntity(entity) ? library[entity].Translation : translate;

			this.Entity = entity;
			this.Translation = translate;

			if (this.Entity.Equals(this.Translation, StringComparison.OrdinalIgnoreCase))
				throw new ArgumentException("The supplied entity translation value causes an internal infinite recursion loop.");
		}
		#endregion

		#region Accessors
		/// <summary>Facilitates interactions with the EntityShim's entity specification while managing style and formatting.</summary>
		public string Entity
		{
			get => "&" + this._entity + ";";
			set
			{
				string v = value.Trim();
				if (ValidateEntity(v))
				{
					// remove potential leading/trailing characters that we don't want polluting our data...
					if (v[0] == '&') v = v.Substring(1);
					if (v.EndsWith(";")) v = v.Substring(0, v.Length - 1);

					//string data = value.Replace(" ", "").Replace("\r", "").Replace("\n", "").Trim(new char[] { '&', ';' });
					if (v.Length > 0) this._entity = v.ToLowerInvariant();
				}
				else
					throw new FormatException("The supplied Entity value (\"" + value + "\") is not appropriate for use as an Xml Entity.");
			}
		}

		/// <summary>Facilitates interactions with the EntityShim's Translation value.</summary>
		public string Translation
		{
			get => this._translate;
			set
			{
				if (Regex.IsMatch(value, TRANSLATION_REGEX))
				{
					string result = "";
					for (int i = 0; i < value.Length; i++)
					{
						string v = value.Substring(i, 1);
						if (Regex.IsMatch(v, @"[^\\]"))
							result += v;
						else // process escaped characters...
						{
							if ((v == @"\") && (++i < value.Length))
								switch (value.Substring(i, 1))
								{
									case @"t": // tab
										result += "\t"; break;
									case @"b": // backspace
										result += "\b"; break;
									case @"r": // carriage-return
										result += "\r"; break;
									case @"n": // new-line
										result += "\n"; break;
									case @"\": // backslash
										result += "\\"; break;
									case "\"": // double-quote
										result += "\""; break;
								}
						}
					}
					this._translate = result;
				}
				else
					throw new FormatException("The value provided for the translation (\"" + value + "\") is invalid.");
			}
		}

		public int Length => this.Entity.Length;
		#endregion

		#region Operators
		public static bool operator !=(HelpXmlEntity left, HelpXmlEntity right) => !(left == right);
		public static bool operator ==(HelpXmlEntity left, HelpXmlEntity right)
		{
			if (left is null) return (right is null);
			if (right is null) return false;
			return left.Entity.Equals(right.Entity, StringComparison.OrdinalIgnoreCase);
		}

		public static bool operator !=(HelpXmlEntity left, string right) => !(left == right);
		public static bool operator ==(HelpXmlEntity left, string right)
		{
			if (left is null) return (right is null) || string.IsNullOrEmpty(right) || string.IsNullOrWhiteSpace(right);
			if ((right is null) || string.IsNullOrEmpty(right) || string.IsNullOrWhiteSpace(right)) return false;
			return left.Entity.Equals(right, StringComparison.OrdinalIgnoreCase);
		}

		public static bool operator !=(string left, HelpXmlEntity right) => (right != left);
		public static bool operator ==(string left, HelpXmlEntity right) => (right == left);
		#endregion

		#region Methods
		//  <!ENTITY bksp "\b">
		public override string ToString() => "<!ENTITY " + this._entity.ToLowerInvariant() + " \"" + this._translate + "\">";

		/// <summary>Reports the number of instances of this Entity within a provided string.</summary>
		/// <param name="text">A string to search for this Entity.</param>
		/// <returns>A number indicating the number of independent instances of this Entity in the provided string.</returns>
		public int InstanceCount(string text)
		{
			if ((text is null) || (text.Length == 0)) return 0;

			return (text.Length - text.Replace(this.Entity, "").Length) / Length;
		}

		/// <summary>Finds all instances of translated texts in the supplied source and replaces them with appropriate Entities instead.</summary>
		/// <param name="source">The text to revert text to entities in.</param>
		/// <returns>The supplied text with all appropriate texts replaced by associated entities.</returns>
		public string Encode(string source) =>
			(source.IndexOf(this.Translation) >= 0) ? source.Replace(this.Translation, this.Entity) : source;

		/// <summary>Finds all existing entities in the provided string and replaces them with appropriate translations.</summary>
		/// <param name="source">The text whose entites require translation to text equivalents.</param>
		/// <returns>The supplied text with all appropriate entities replaced with associated translations.</returns>
		public string Decode(string source) =>
			(source.IndexOf(this.Entity) >= 0) ? source.Replace(this.Entity, this.Translation) : source;

		// Implemented due to the use of Operator overrides...
		public override bool Equals(object obj) => base.Equals(obj);

		public override int GetHashCode() => base.GetHashCode();

		// Facilitates creating an HelpXmpEntity from a System.Xml.XmlEntity object...
		public static implicit operator HelpXmlEntity(XmlEntity data) => new HelpXmlEntity(data);
		#endregion

		#region Static Methods
		/// <summary>Validates a supplied string as being suitable for use as an entity.</summary>
		/// <param name="text">A string containing the text to validate.</param>
		/// <returns>TRUE if the string is suitable to be used as an Entity, otherwise FALSE.</returns>
		public static bool ValidateEntity(string text) =>
			Regex.IsMatch(text.Trim(), @"^(?:(?:" + PATTERN + @")|(?:[a-z]{2,6}))$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

		/// <summary>Parses an expected XML Entity string into an XmlEntity object.</summary>
		/// <param name="source">A string containing the Xml text to parse.</param>
		/// <returns>If successful, a populared XmlEntity object, otherwise a FormatException is thrown.</returns>
		/// <exception cref="FormatException"></exception>
		public static HelpXmlEntity Parse(string source)
		{
			string EntityPattern = ENTITY_REGEX; // @"<!ENTITY (?<entity>[a-zA-Z]{2,6}) (?<translate>([""][\w\s] +[""])|(['][\w\s][']))>";
			Regex regex = new Regex(EntityPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);
			MatchCollection matches = regex.Matches(source);
			if (matches.Count > 0)
				return new HelpXmlEntity(matches[0].Groups["entity"].Value, matches[0].Groups["translate"].Value);

			throw new FormatException("The supplied string does not conform to a recognized XML Entity structure.");
		}
		#endregion
	}

	public class XmlEntityCollection : IEnumerator<HelpXmlEntity>
	{
		#region Properties
		/// <summary>A Regex pattern for identifying a full DOCTYPE declaration.</summary>
		public const string DOCTYPE_REGEX = @"<!DOCTYPE[ ]+(?<rootTag>[\w]+)\[(?:" + HelpXmlEntity.ENTITY_REGEX + @")*[\s]*\]>";

		/// <summary>Stores our collection of XmlHelpEntity objects.</summary>
		protected List<HelpXmlEntity> _entities = new List<HelpXmlEntity>();

		/// <summary>Used internally to support IEnumerator functionality.</summary>
		private int _position = 0;
		#endregion

		#region Constructors
		/// <summary>Creates a new, clean (empty) XmlEntityCollection.</summary>
		public XmlEntityCollection() { }

		/// <summary>Creates a new XmlEntityCollection with the supplied HelpXmlEntity value included.</summary>
		public XmlEntityCollection(HelpXmlEntity entity) => this.Add(entity);

		/// <summary>Creates a new XmlEntityCollection with the supplied HelpXmlEntity array included.</summary>
		public XmlEntityCollection(HelpXmlEntity[] entities) => this.AddRange(entities);

		/// <summary>Creates a new XmlEntityCollection with the supplied XmlEntity value included.</summary>
		public XmlEntityCollection(XmlEntity entity) => this.Add(entity);

		/// <summary>Creates a new XmlEntityCollection with the supplied XmlEntity array included.</summary>
		public XmlEntityCollection(XmlEntity[] entities) => this.AddRange(entities);

		/// <summary>Creates a new XmlEntityCollection with its contents copied from the supplied XmlEntityCollecton.</summary>
		public XmlEntityCollection(XmlEntityCollection source) => this.AddRange(source);

		/// <summary>Creates a new XmlEntityCollection with the supplied XmlDocument's entities imported into it.</summary>
		public XmlEntityCollection(XmlDocument source) => this.AddRange(ExtractEntities(source));
		#endregion

		#region Accessors
		/// <summary>Facilitates indexed interaction with this collection.</summary>
		/// <param name="index">An int value spcifying the index to dereference.</param>
		public HelpXmlEntity this[int index]
		{
			get => this._entities[index];
			set => this._entities[index] = value;
		}

		/// <summary>Facilitates accessing the collection via the Entity value.</summary>
		/// <param name="entity">A string specifying the Entity value to match.</param>
		public HelpXmlEntity this[string entity] =>
			this._entities[IndexOf(entity)];

		/// <summary>Reports the number of elements in the collection.</summary>
		public int Count => this._entities.Count;

		// IEnumerable support accessors...
		HelpXmlEntity IEnumerator<HelpXmlEntity>.Current => this[this._position];

		object IEnumerator.Current => this._entities[this._position];
		#endregion

		#region Operators
		/// <summary>Takes two XmlEntityCollection objects and rolls their contents together into a single collection.</summary>
		public static XmlEntityCollection operator +(XmlEntityCollection left, XmlEntityCollection right)
		{
			XmlEntityCollection result = new XmlEntityCollection(left.ToArray());
			result.AddRange(right.ToArray());
			return result;
		}

		/// <summary>Adds the contents of an HelpXmlEntity array to an XmlEntityCollection and returns a new XmlEntityCollection.</summary>
		public static XmlEntityCollection operator +(XmlEntityCollection left, HelpXmlEntity[] right)
		{
			XmlEntityCollection result = new XmlEntityCollection(left.ToArray());
			result.AddRange(right);
			return result;
		}

		// transitive version of above
		/// <summary>Adds the contents of an HelpXmlEntity array to an XmlEntityCollection and returns a new XmlEntityCollection.</summary>
		public static XmlEntityCollection operator +(HelpXmlEntity[] left, XmlEntityCollection right) =>
			right + left;

		/// <summary>Adds an HelpXmlEntity to an XmlEntityCollection and returns the result in a new XmlEntityCollection.</summary>
		public static XmlEntityCollection operator +(XmlEntityCollection left, HelpXmlEntity right) =>
			left + new HelpXmlEntity[] { right };

		// transitive version of above
		/// <summary>Adds an HelpXmlEntity to an XmlEntityCollection and returns the result in a new XmlEntityCollection.</summary>
		public static XmlEntityCollection operator +(HelpXmlEntity left, XmlEntityCollection right) =>
			right + new HelpXmlEntity[] { left };

		/// <summary>Adds the entities from an XmlDocument to an XmlEntityCollection and returns the result in a new XmlEntityCollection.</summary>
		public static XmlEntityCollection operator +(XmlEntityCollection left, XmlDocument right) =>
			left + ExtractEntities(right);
		#endregion

		#region Methods
		/// <summary>Searches the collection for an Entity whose entity value matches the supplied string.</summary>
		/// <param name="entityName">A string specifying the Entity-value of the Entity to be located.</param>
		/// <returns>If a match is found, it's index value in the collection, otherwise -1.</returns>
		protected int IndexOf(string entityName)
		{
			int i = -1; while ((++i < this.Count) && (this[i] != entityName)) ;
			return (i < Count) ? i : -1;
		}

		/// <summary>Searches the collection for an Entity whose entity value matches the supplied string.</summary>
		/// <param name="entity">An HelpXmlEntity whose Entity value matches the one we want to find the index of.</param>
		/// <returns>If a match is found, it's index value in the collection, otherwise -1.</returns>
		protected int IndexOf(HelpXmlEntity entity) => IndexOf(entity.Entity);

		/// <summary>Reports whether or not a specified Entity-value (name) exists in the collection.</summary>
		/// <param name="entityName">A string specifying the Entity-value of the Entity to be located.</param>
		/// <returns>TRUE if there's a matching Entity in the collection, otherwise FALSE.</returns>
		public bool HasEntity(string entityName) => (IndexOf(entityName) >= 0);

		/// <summary>Applies this Entity collection to a supplied string and returns the result.</summary>
		/// <param name="target">A string containng XML entities that need to be translated.</param>
		/// <returns>The provided string, with all entities defined in this collection replaced by their translation values.</returns>
		public int Apply(ref string target)
		{
			int count = 0;
			foreach (HelpXmlEntity e in this._entities)
			{
				count += e.InstanceCount(target);
				target = e.Decode(target);
			}
			return count;
		}

		/// <summary>Adds (or modifies if it exists) an HelpXmlEntity in the collection.</summary>
		/// <param name="entity">An HelpXmlEntity to add (or modify).</param>
		public void Add(HelpXmlEntity entity)
		{
			int i = IndexOf(entity);
			if (i < 0)
				this._entities.Add(entity);
			else
				this._entities[i] = entity;
		}

		/// <summary>Adds (or modifies if it exists) an XmlEntity in the collection.</summary>
		/// <param name="entity">An XmlEntity to add (or modify).</param>
		public void Add(XmlEntity entity) =>
			Add(new HelpXmlEntity(entity));

		/// <summary>Adds (or modifies if they exist) an XmlEntityCollection to this collection.</summary>
		/// <param name="collection">An XmlEntityCollection whose contents are to be added (or modified).</param>
		public void AddRange(XmlEntityCollection collection) =>
			this.AddRange(collection.ToArray());

		/// <summary>Adds (or modifies if they exist) an array of HelpXmlEntity objects to this collection.</summary>
		/// <param name="entities">An array of HelpXmlEntity object to be add (or modify).</param>
		public void AddRange(HelpXmlEntity[] entities)
		{
			foreach (HelpXmlEntity e in entities)
				this.Add(e);
		}

		/// <summary>Adds (or modifies if they exist) an array of XmlEntity objects to this collection.</summary>
		/// <param name="entities">An XmlEntity array whose contents are to be added (or modified).</param>
		public void AddRange(XmlEntity[] entities)
		{
			foreach (XmlEntity e in entities)
				this.Add(new HelpXmlEntity(e));
		}

		/// <summary>Removes an entity from this collection by its entity name.</summary>
		/// <param name="entityName">A string specifying the Entity-value of the object to remove from the collection.</param>
		/// <returns>TRUE if the item was found + removed, otherwise FALSE.</returns>
		public bool Remove(string entityName)
		{
			int i = IndexOf(entityName);
			if (i >= 0)
			{
				this._entities.RemoveAt(i);
				return true;
			}
			return false;
		}

		/// <summary>Removes an entity from this collection.</summary>
		/// <param name="entity">An HelpXmlEntity to remove from the collection.</param>
		/// <returns>TRUE if the item was found + removed, otherwise FALSE.</returns>
		public bool Remove(HelpXmlEntity entity) => this.Remove(entity.Entity);

		/// <summary>Creates the text header (DOCTYPE + ENTITY declaration) for an XML document defining the entities managed by this object.</summary>
		/// <param name="rootTagName">The root XML tag name for the document.</param>
		/// <returns>A string containing the appropriate DOCTYPE declaration to define these Entities.</returns>
		public string ToString(string rootTagName)
		{
			if ((rootTagName is null) || (rootTagName.Trim().Length == 0))
				throw new ArgumentException("The supplied root tag name is invalid.");

			string result = "<!DOCTYPE " + rootTagName + "[\r\n";
			foreach (HelpXmlEntity e in this)
				result += "   " + e.ToString() + "\r\n";

			result += "]>\r\n";
			return result;
		}

		/// <summary>Facilitates extracting the collection to an array of HelpXmlEntities.</summary>
		/// <returns>An array of HelpXmlEntities culled from this collection.</returns>
		public HelpXmlEntity[] ToArray() => this._entities.ToArray();
		#endregion

		//IEnumerator Support
		public IEnumerator<HelpXmlEntity> GetEnumerator() { return this._entities.GetEnumerator(); }

		bool IEnumerator.MoveNext() { return (++this._position) < this._entities.Count; }

		void IEnumerator.Reset() { this._position = 0; }

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

		#region Static Methods
		public static XmlEntityCollection Empty => new XmlEntityCollection();

		/// <summary>Constructs a pre-defined Entity Library containing the most commonly used Help Entities.</summary>
		public static XmlEntityCollection DefaultEntities()
		{
			XmlEntityCollection result = Empty;
			result.AddRange(
				new HelpXmlEntity[] {
					new HelpXmlEntity("amp","&",Empty),
					new HelpXmlEntity("crlf", "\\r",Empty),
					new HelpXmlEntity("bksp", "\\b",Empty),
					new HelpXmlEntity("space", " ",Empty),
					new HelpXmlEntity("lt", "<",Empty),
					new HelpXmlEntity("gt", ">",Empty),
					new HelpXmlEntity("quote", "\"",Empty),
					new HelpXmlEntity("apos", "'",Empty),
					new HelpXmlEntity("raquo", "»",Empty),
					new HelpXmlEntity("laquo", "«",Empty)
				}
			);

			return result;
		}

		/// <summary>Applies a collection of EntityShim objects to a supplied string.</summary>
		/// <param name="source">The text to apply the EntityShim collection to.</param>
		/// <param name="entityList">An array of EntityShim objects to apply to the string.</param>
		/// <returns>The original string with all specified entites translated to text.</returns>
		public static string MassEncode(string source, XmlEntityCollection entityList = null)
		{
			if (entityList is null) entityList = DefaultEntities();
			foreach (HelpXmlEntity e in entityList) source = e.Encode(source);
			return source;
		}

		/// <summary>Removes the translated texts from a suplied string and replaces them with corresponding Entities.</summary>
		/// <param name="source">The text to reverse-translate the EntityShim collection from.</param>
		/// <param name="entityList">An array of EntityShim objects to apply to the string.</param>
		/// <returns>The original string with all relevant strings reverse translated to their EntityShim counterparts.</returns>
		public static string MassDecode(string source, XmlEntityCollection entityList = null)
		{
			if (entityList is null) entityList = DefaultEntities();
			foreach (HelpXmlEntity e in entityList) source = e.Decode(source);
			return source;
		}

		/// <summary>Parses the string version of the XmlDocument to extract the DocType + Entity declaration into XmlEntities.</summary>
		/// <returns>An array of XmlEntities extracted from any DocType declarations contained in this XmlDocument.</returns>
		public static XmlEntityCollection ExtractEntities(string xmlSource)
		{
			List<HelpXmlEntity> results = new List<HelpXmlEntity>();
			if (!(xmlSource is null) && (xmlSource != string.Empty) & (xmlSource.Length > 0))
			{
				Regex pattern = new Regex(XmlEntityCollection.DOCTYPE_REGEX, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.ExplicitCapture);
				MatchCollection matches = pattern.Matches(xmlSource);
				foreach (Match m in matches)
					results.Add(new HelpXmlEntity(m.Groups["entity"].Value, m.Groups["translate"].Value));
			}
			return new XmlEntityCollection(results.ToArray());
		}

		/// <summary>Parses an XmlDocument to extract the DocType + Entity declaration into an XmlEntityCollection.</summary>
		/// <param name="doc">An XmlDocument whose entities are going to be extracted.</param>
		/// <returns>An XmlEntityCollection extracted from any DocType declarations contained in the provided XmlDocument.</returns>
		public static XmlEntityCollection ExtractEntities(XmlDocument doc) => ExtractEntities(doc.OuterXml);
		#endregion
	}

	/// <summary>Manages processing blocks of text within nodes.</summary>
	public class CliHelpBlock : XmlElement
	{
		#region Properties
		public static XmlDocument XmlDocWorker = new XmlDocument();

		protected CliColor _defaultColor = Con.DefaultColor;
		#endregion

		#region Constructor
		protected CliHelpBlock() : base("", "", "", XmlDocWorker) { }

		public CliHelpBlock(string tagName, string innerXml = "") : base("", tagName, "", XmlDocWorker) =>
			this.InnerXml = innerXml;

		public CliHelpBlock(XmlElement source) : base(source.Prefix, source.LocalName, source.NamespaceURI, XmlDocWorker)
		{
			switch(source.NodeType)
			{
				case XmlNodeType.Text:
					this.SetAttribute("style", source.ParentNode.GetAttributeValue("style"));
					this.SetAttribute("indent", source.ParentNode.GetAttributeValue("indent"));
					switch (source.ParentNode.LocalName.ToLowerInvariant())
					{
						case "p":
							break;
						case "text":
							this.SetAttribute("pre", source.ParentNode.GetAttributeValue("pre"));
							this.SetAttribute("post", source.ParentNode.GetAttributeValue("post"));
							break;
					}
					break;
				case XmlNodeType.Element:
					if (source.HasAttribute("AddCr") && (PostSpacing < 1))
						PostSpacing = 1;

					// Copy all attributes (except AddCr) from the source...
					foreach (XmlAttribute a in source.Attributes)
						if (!a.Name.Equals("AddCr", StringComparison.OrdinalIgnoreCase))
							this.SetAttribute(a.Name, a.Value);

					// Copy all child nodes from the source...
					//foreach (XmlNode child in source.ChildNodes) this.AppendChild(child);
					this.InnerXml = source.InnerXml;

					// Validate that the "pre" value is a number...
					if (!Regex.IsMatch(this.GetAttributeValue("pre"), @"[\d]+", RegexOptions.Compiled))
						this.SetAttribute("pre", "0");

					// Validate that the "post" value is a number...
					if (!Regex.IsMatch(this.GetAttributeValue("post"), @"[\d]+", RegexOptions.Compiled))
						this.SetAttribute("post", "0");
					break;
			}
		}
		#endregion

		#region Accessors
		public string Id
		{
			get => this.HasAttribute("id") ? this.GetAttributeValue("id") : "";
			set => this.SetAttribute("id", value);
		}

		public byte PreSpacing
		{
			get => this.HasAttribute("pre") ? byte.Parse(this.GetAttributeValue("pre")) : (byte)0;
			set => this.SetAttribute("pre", Math.Min((byte)5, value).ToString());
		}

		public byte PostSpacing
		{
			get => this.HasAttribute("post") ? byte.Parse(this.GetAttributeValue("post")) : (byte)0;
			set => this.SetAttribute("post", Math.Min((byte)5, value).ToString());
		}

		public CliColor Color
		{
			get => this.HasAttribute("style") ? CliColor.ParseXml(this.GetAttributeValue("style"), _defaultColor) : _defaultColor;
			set => this.SetAttribute("style", value.ToStyleString());
		}

		public string Style =>
			this.HasAttribute("style") ? this.GetAttributeValue("style") : "";

		public ConsoleColor Fore
		{
			get => this.Color.Fore;
			set => this.Color = new CliColor(value, Back);
		}

		public ConsoleColor Back
		{
			get => this.Color.Back;
			set => this.Color = new CliColor(Fore, value);
		}

		public byte Indent
		{
			get => this.HasAttribute("indent") ? Byte.Parse(this.GetAttributeValue("indent")) : (byte)0;
			set => this.SetAttribute("indent", value.ToString());
		}
		#endregion

		#region Methods
		public void Write(int indent = 0)
		{
			if (this.LocalName.Equals("p", StringComparison.OrdinalIgnoreCase) && (PreSpacing > 0))
				Console.Write("\r" + "".PadRight(PreSpacing, '\n'));

			XmlEntityCollection entities = XmlEntityCollection.DefaultEntities();
			foreach (dynamic child in this.ChildNodes)
				switch (child.NodeType)
				{
					case XmlNodeType.Text:
						if (PreSpacing > 0)
							Con.Tec( "{$1,0}$2", new object[] { new CliColor( Fore, Back ), "".PadRight( PreSpacing, '\n' ) } );
//							Con.Write("\r" + , Fore, Back); // Prefix line spacing

						string[] lines = child.InnerText.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
						foreach (string line in lines)
						{
							if ((Indent + indent) > 0)
								Con.Tec( "{$1,,>$2} ", new object[] { new CliColor(Fore,Back), Indent + indent } );
//								Con.Write("".PadRight(Indent + indent, ' '), Fore, Back); // Indentation

							string text = line.Trim();
							entities.Apply(ref text);
							Con.Tec( "{$1}$2 ", new object[] { new CliColor( Fore, Back ), text } );
//							Con.Write(text, Fore, Back); Console.Write(" ");
						}

						if (PostSpacing > 0)
							Con.Tec( "{$1,0}$2", new object[] { new CliColor( Fore, Back ), "".PadRight( PreSpacing, '\n' ) } );
//							Con.Write("\r" + "".PadRight(PostSpacing, '\n'), Fore, Back); // Suffix line spacing
						break;
					case XmlNodeType.Entity:
						entities.Add(HelpXmlEntity.Parse(child.OuterXml));
						break;
					case XmlNodeType.EntityReference:
						break;
					default:
						CliHelpBlock.Convert(child).Write(Indent + indent);
						break;
				}

			if (this.LocalName.Equals("p", StringComparison.OrdinalIgnoreCase) && (PostSpacing > 0))
				Console.Write("\r" + "".PadRight(PostSpacing, '\n'));
		}
		#endregion

		#region Static Methods
		public static bool IsTextNode(XmlElement test) => (test.NodeType == XmlNodeType.Text);

		public static CliHelpBlock Convert(XmlElement element) =>
			new CliHelpBlock(element);

		public static CliHelpBlock Build(string tagName = "text", int indentSize = 0, CliColor defaultColor = null)
		{
			XmlDocWorker.LoadXml(NetXpertExtensions.XML_HEADER + "<" + tagName + "></" + tagName + ">");
			return (CliHelpBlock)XmlDocWorker.GetFirstNamedElement(tagName);
		}
		#endregion
	}

	/// <summary>Facilitates Interactions with a Switch node.</summary>
	public class CliSwitchBlock : XmlElement
	{
		#region Properties
		protected CliHelpBlock _shortHelp;
		protected CliHelpBlock _longHelp;
		#endregion

		#region Constructors
		public CliSwitchBlock(XmlElement parent) : base(parent.Prefix, parent.LocalName, parent.NamespaceURI, CliHelpBlock.XmlDocWorker)
		{
			foreach (XmlAttribute a in parent.Attributes)
				this.SetAttribute(a.Name, a.Value);

			XmlNode ch = parent.GetFirstNamedElement("shortHelp");
			this._shortHelp = (ch is null) ? new CliHelpBlock("shortHelp") : CliHelpBlock.Convert(ch as XmlElement);

			ch = parent.GetFirstNamedElement("longHelp");
			this._longHelp = (ch is null) ? new CliHelpBlock("longHelp") : CliHelpBlock.Convert(ch as XmlElement);
		}
		#endregion

		#region Accessors
		public string Id => this.HasAttribute("id") ? this.GetAttributeValue("id") : "";
		public CliHelpBlock ShortHelp => this._shortHelp;
		public CliHelpBlock LongHelp => this._longHelp;
		#endregion
	}

	/// <summary>Facilitates Interactions with a Switchset node.</summary>
	public class SwitchSet : XmlElement
	{
		#region Properties
		protected List<CliSwitchBlock> _switches = new List<CliSwitchBlock>();
		#endregion

		#region Constructors
		public SwitchSet(XmlElement parent) : base(parent.Prefix, parent.LocalName, parent.NamespaceURI, CliHelpBlock.XmlDocWorker)
		{
			foreach (XmlAttribute a in parent.Attributes)
				this.SetAttribute(a.Name, a.Value);

			if (parent.HasChildNodes)
				foreach (XmlNode node in parent.ChildNodes)
					if ((node.NodeType == XmlNodeType.Element) && node.LocalName.Equals("switch", StringComparison.OrdinalIgnoreCase))
						this._switches.Add(new CliSwitchBlock(node as XmlElement));
		}

		public SwitchSet() : base("","switchset", "", CliHelpBlock.XmlDocWorker) { }
		#endregion

		#region Accessors
		public CliSwitchBlock this[int index] => this._switches[index];

		new public CliSwitchBlock this[string switchName] => this[IndexOf(switchName)];

		public int Count => this._switches.Count;

		public string[] Keys =>
			this.HasAttribute("keys") ? this.GetAttributeValue("keys").Split(new char[] { ' ' }) : new string[] { };
		#endregion

		#region Methods
		protected int IndexOf(string switchName)
		{
			int i = -1; while ((++i < Count) && !this[i].Id.Equals(switchName, StringComparison.OrdinalIgnoreCase)) ;
			return (i < Count) ? i : -1;
		}

		public bool HasSwitch(string test) => (IndexOf(test) >= 0);
		#endregion
	}

	/// <summary>Facilitates Interactions with a CmdBlock node.</summary>
	public class CliCmdBlock : XmlElement
	{
		#region Properties
		protected CliHelpBlock _intro;
		protected SwitchSet _switchSet;
		#endregion

		#region Constructors
		public CliCmdBlock(XmlElement parent) : base(parent.Prefix, parent.LocalName, parent.NamespaceURI, CliHelpBlock.XmlDocWorker)
		{
			foreach (XmlAttribute a in parent.Attributes)
				this.SetAttribute(a.Name, a.Value);

			XmlNode ch = parent.GetFirstNamedElement("intro");
			this._intro = (ch is null) ? new CliHelpBlock("intro") : CliHelpBlock.Convert(ch as XmlElement);

			ch = parent.GetFirstNamedElement("switchset");
			this._switchSet = (ch is null) ? new SwitchSet() : new SwitchSet(ch as XmlElement);
		}
		#endregion

		#region Accessors
		public CliHelpBlock Intro => this._intro;
		public SwitchSet SwitchSet => this._switchSet;
		#endregion
	}

	public class CliHelp
	{
		#region Properties
		protected XmlDocument _doc;
		#endregion

		#region Constructors
		public CliHelp(XmlDocument source) => this._doc = source;

		public CliHelp(string source)
		{
			this._doc = new XmlDocument();
			this._doc.LoadXml(source);
		}
		#endregion

		#region Accessors
		public CliHelpBlock Header => FetchCategory("Header");

		public CliHelpBlock Version => FetchCategory("Ver");

		public CliCmdBlock Help => new CliCmdBlock(FetchElement("category", "id", "help"));
		#endregion

		#region Methods
		protected CliHelpBlock FetchCategory(string id = "")
		{
			XmlElement category = this.FetchElement("category", "id", id);
			return (category is null) ? new CliHelpBlock("category") :  new CliHelpBlock(category);
		}

		protected XmlElement FetchElement(string tag, string attribute = "", string value = "")
		{
			XmlNodeList results = this._doc.GetElementsByTagName(tag);
			foreach (XmlNode node in results)
				if ((attribute != "") && (node as XmlElement).HasAttribute(attribute) && (node as XmlElement).AttributeValueEquals(attribute, value))
					return (node as XmlElement);

			return null;
		}
		#endregion

		#region Static Methods
		/// <summary>Produces a populated CliHelp object from a specified internal (resource) XML file.</summary>
		/// <param name="name">The resource filename to import.</param>
		/// <returns>A new CliHelp object populated with the information from the specified file.</returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="FileLoadException"></exception>
		public static CliHelp FetchInternalResourceFile(string name)
		{
			if ((name is null) || (name.Length == 0) || (name == String.Empty))
				throw new ArgumentNullException("A valid filename must be provided for this function to operate.");

			if (!Path.GetExtension(name).Equals(".xml", StringComparison.OrdinalIgnoreCase))
				throw new FileLoadException("The file to be imported must be an XML file.");

			// Using .GetEntryAssembly() as this routine is in a DLL and .GetExecutingAssembly() refers to the DLL!
			var assembly = System.Reflection.Assembly.GetEntryAssembly();
			using (Stream stream = assembly.GetManifestResourceStream(name))
			using (StreamReader reader = new StreamReader(stream))
				return new CliHelp(reader.ReadToEnd());
		}

		/// <summary>Produces a populated CliHelp object from a specified external XML file.</summary>
		/// <param name="name">The path and filename of the item to import.</param>
		/// <returns>A new CliHelp object populated with the information from the specified file.</returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="FileLoadException"></exception>
		public static CliHelp FetchExternalResourceFile(string name)
		{
			if ((name is null) || (name.Length == 0) || (name == String.Empty))
				throw new ArgumentNullException("A valid filename must be provided for this function to operate.");

			if (!File.Exists(name))
				throw new FileLoadException("The file \"" + name + "\" could not be located.");

			System.Xml.Linq.XDocument doc = System.Xml.Linq.XDocument.Load(name);
			return new CliHelp(doc.Declaration.ToString() + doc.ToString(System.Xml.Linq.SaveOptions.DisableFormatting));
		}

		/// <summary>Produces a basic default Help file -- not recommended for use other than during development!</summary>
		public static CliHelp Default =>
			new CliHelp(
				"<?xml version='1.0' encoding='utf-8' standalone='yes'?>" +
				"<!DOCTYPE cobblestone[" +
					"<!ENTITY crlf '\\r\\n'>" +
					"<!ENTITY tab '\\t'>" +
					"<!ENTITY space ' '>" +
					"<!ENTITY quote '\"'>" +
					"<!ENTITY apos \"'\">" +
					"<!ENTITY lt '<'>" +
					"<!ENTITY gt '>'>" +
					"<!ENTITY laquo '«'>" +
					"<!ENTITY raquo '»'>" +
					// "<!ENTITY copy '©'>" + // '\\u00a9' '©'
					"<!ENTITY bull '•'>" +
				"]>\r\n" +
				"<cobblestone>\r\n" +
					"<category id='header'>" +
						"<p pre='0' post='1' style='foreColor:lightgray;backColor:black;'>" +
							"<text post='1' style='foreColor:white;backColor:darkblue'> ** Sample Application Header ** </text>" +
							"<text post='1' indent='1' style='foreColor:white;'>" +
								"Written By Author's Name&crlf;" +
								"© Copyright [year]&crlf;" +
								"All Rights Reserved." +
							"</text>" +
						"</p>" +
					"</category>\r\n" +
					"<category id='ver'>" +
						"<p indent='3' pre='0' post='1'>" +
							"&raquo; Applet[<text style='foreColor:white;backColor:black;'>{{$cmdName}}</text>] (" +
							"<text style='foreColor:LightCyan;'>v{{$cmdVersion}}</text>)" +
						"</p>" +
					"</category>\r\n" +
					"<category id='help'>" +
						"<intro copyright='2018, Brett Leuszler'>" +
							"<p pre='0' post='1'>" +
								"<text style='foreColor:Yellow;' >{{$cmdName}}</text>" +
								"&space;- a Cobblestone Properties Command Line Applet(" +
								"<text style='foreColor:White;' > &space; v{{$cmdVersion}}</text>)" +
							"</p>" +
							"<p pre='0' post='2'>Copyright © {{$copyright}}. All Rights Reserved.</p>" +
						"</intro>\r\n" +
						"<switchset keys='/ver /help' >" +
							"<switch id='VER'>" +
								"<shortHelp>Switch help for this.'>More detailed help here.</shortHelp>" +
								"<longHelp>" +
									"Detailed help for this switch." +
								"</longHelp>" +
							"</switch>\r\n" +
							"<switch id='HELP'>" +
								"<shortHelp>Switch help for this.'>More detailed help here.</shortHelp>" +
								"<longHelp>" +
									"Detailed help for this switch." +
								"</longHelp>" +
							"</switch>\r\n" +
						"</switchset>" +
					"</category>" +
				"</cobblestone>"
				);
		#endregion
	}
}
