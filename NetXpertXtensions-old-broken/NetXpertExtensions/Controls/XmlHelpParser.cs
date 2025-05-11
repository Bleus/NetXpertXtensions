using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Xml;
using NetXpertExtensions;

namespace NetXpertXtensions.HelpSystem
{
	/// <summary>Provides more traditional / descriptive alternatives to the Console.Color defaults:</summary>
	public class HelpColor
	{
		#region Properties
		protected ConsoleColor _foreGround = ConsoleColor.Gray;
		protected ConsoleColor _backGround = ConsoleColor.Black;
		#endregion

		#region Constructors
		public HelpColor(ConsoleColor fore = ConsoleColor.Gray, ConsoleColor back = ConsoleColor.Black)
		{
			this._foreGround = fore;
			this._backGround = back;
		}

		public HelpColor(string foreName, string backName = "Black")
		{
			this._foreGround = Parse(foreName);
			this._backGround = Parse(backName);
		}
		#endregion

		#region Accessors
		public ConsoleColor Fore
		{
			get => this._foreGround;
			set => this._foreGround = value;
		}

		public ConsoleColor Back
		{
			get => this._backGround;
			set => this._backGround = value;
		}

		public static HelpColor Default => new HelpColor();
		#endregion

		#region Methods
		/// <summary>Returns the contents of this object in a style compatible with the XML help system.</summary>
		public string ToStyleString() => "foreGround:" + this._foreGround.ToString() + "; backGround:" + this._backGround.ToString() + ";";

		public override string ToString() => "( " + this._foreGround.ToString() + ", " + this._backGround + " )";
		#endregion

		#region Static Methods
		/// <summary>Takes a color name and tries to convert it to an equivalent ConsoleColor.</summary>
		/// <exception cref="FormatException"></exception>
		protected static ConsoleColor Translate(string colorName)
		{
			colorName = colorName.Trim();
			if ((colorName is null) || (colorName.Length == 0))
				throw new FormatException("You need to provide a System.Drawing.Color name to translate.");

			switch (colorName.ToLowerInvariant())
			{
				case "blue":
				case "darkblue":
					return ConsoleColor.DarkBlue;

				case "green":
				case "forestgreen":
				case "darkgreen":
					return ConsoleColor.DarkGreen;

				case "aqua":
				case "darkcyan":
				case "cyan":
					return ConsoleColor.DarkCyan;

				case "darkred":
				case "red":
					return ConsoleColor.DarkRed;

				case "darkmagenta":
				case "purple":
					return ConsoleColor.DarkMagenta;

				case "darkyellow":
				case "brown":
				case "orange":
					return ConsoleColor.DarkYellow;

				case "lightgray":
				case "lightgrey":
				case "grey":
				case "gray":
					return ConsoleColor.Gray;

				case "darkgray":
				case "darkgrey":
					return ConsoleColor.DarkGray;

				case "lightblue":
				case "royalblue":
					return ConsoleColor.Blue;

				case "lightgreen":
				case "neongreen":
					return ConsoleColor.Green;

				case "lightcyan":
					return ConsoleColor.Cyan;

				case "lightred":
				case "coral":
					return ConsoleColor.Red;

				case "pink":
					return ConsoleColor.Magenta;

				case "yellow":
				case "magenta":
				case "white":
					return (ConsoleColor)Enum.Parse(typeof(ConsoleColor), colorName);
			}
			throw new FormatException("The provided value (\"" + colorName + "\")could not be converted to a ConsoleColor equivalent.");
		}

		public static ConsoleColor Convert(byte redByte, byte greenByte, byte blueByte)
		{
			ConsoleColor result = 0;
			double red = redByte,
				   green = greenByte,
				   blue = blueByte,
				   spread = double.MaxValue;

			foreach (ConsoleColor color in (ConsoleColor[])Enum.GetValues(typeof(ConsoleColor)))
			{
				string colorName = Enum.GetName(typeof(ConsoleColor), color);

				Color color2 = Color.FromName(colorName == "DarkYellow" ? "Orange" : colorName);

				double compare = Math.Pow(color2.R - red, 2.0) + Math.Pow(color2.G - green, 2.0) + Math.Pow(color2.B - blue, 2.0);
				if (compare == 0.0) return color;
				if (compare < spread) {  spread = compare; result = color; }
			}
			return result;
		}

		public static ConsoleColor Convert(Color source) =>
			Convert(source.R, source.G, source.B);

		/// <summary>Attempts to parse a supplied value to a ConsoleColor value.</summary>
		/// <param name="name">A string containing the language to convert.</param>
		/// <returns>A best attempt to reasonably convert the value to a valid ConsoleColor. If all attempts fail, the LightGray value is returned.</returns>
		public static ConsoleColor Parse(string name, ConsoleColor defaultColor = ConsoleColor.Gray)
		{
			if ((name is null) || (name.Trim().Length==0)) return defaultColor;

			name = name.Trim();
			ConsoleColor result = defaultColor;

			// If it's an HTML-esque #rrggbb value, try to parse it...
			if (Regex.IsMatch(name, @"#(?<red>[0-9a-f]{2})(?<green>[0-9a-f]{2})(?<blue>[0-9a-f]{2})", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture))
			{
				MatchCollection matches = new Regex(@"#(?<red>[0-9a-f]{2})(?<green>[0-9a-f]{2})(?<blue>[0-9a-f]{2})").Matches(name);
				if (matches.Count > 0)
				{
					byte red = System.Convert.ToByte(matches[0].Groups["red"].Value, 16),
						 green = System.Convert.ToByte(matches[0].Groups["green"].Value, 16),
						 blue = System.Convert.ToByte(matches[0].Groups["blue"].Value, 16);
					result = Convert(red, green, blue);
				}
			}
			else // Assume it's some form of english...
			{
				// Try it as a System.Drawing.Color name...
				Color c1 = Color.FromName(name);
				if ((c1.R == 0) && (c1.G == 0) && (c1.B == 0) && !name.Equals("black", StringComparison.OrdinalIgnoreCase))
				{
					// The attempt to translate the value as a System.Color name failed.
					// Try to translate it as an english word, or a ConsoleColor value...
					try { result = HelpColor.Translate(name); }
					catch (Exception e)
					{
						if (e.GetType().Name == "FormatException")
							result = defaultColor;
						else
							throw e;
					}
				}
				else
					result = ConsoleColor.Black;
			}
			return result;
		}

		/// <summary>Parses a supplied "style" string to extract the defined colours.</summary>
		/// <param name="source">A string containing the "style" text to parse.</param>
		/// <param name="defaultColor">Define an optional default HelpColour to use when values fail to be parsed.</param>
		/// <returns>A new HelpColor object populated from the provided "style" string.</returns>
		public static HelpColor ParseXml(string source, HelpColor defaultColor = null)
		{
			if (defaultColor is null) defaultColor = Default;

			source += ";"; // ensures that there's always a final semi-colon on the string...
			if (source.IndexOf(";") >= 0)
			{
				string[] parts = source.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				HelpColor result = defaultColor;

				foreach (string part in parts)
					if (part.IndexOf(':') > 0)
					{
						string[] split = part.Split(new char[] { ':' }, 2);
						switch (split[0].Trim().ToUpperInvariant())
						{
							case "FORECOLOR":
								result.Fore = Parse(split[1], defaultColor.Fore);
								break;
							case "BACKCOLOR":
								result.Back = Parse(split[1], defaultColor.Back);
								break;
						}
					}
				return result;
			}
			return defaultColor;
		}
		#endregion
	}

	/// <summary>Manages a parameter-value pair for performing parameterized value substitutions within a block of text.</summary>
	public class XmlParseParameter
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
		public XmlParseParameter(string id, string value = "")
		{
			this.Id = id;
			this.Value = value;
		}

		/// <summary>Creates a new XmlParseParameter object from a supplied KeyValuePair object.</summary>
		/// <param name="data">A KeyValuePair object whose Key becomes our Id and whose Value becomes ours.</param>
		public XmlParseParameter(KeyValuePair<string, string> data)
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
		public XmlParseParameter(string[] parameter)
		{
			if ((parameter is null) || (parameter.Length == 0) || (parameter[0].Trim().Length == 0))
				throw new ArgumentOutOfRangeException("The Id value cannot be empty, null or whitespace!");

			this.Id = parameter[0];
			this.Value = (parameter.Length > 1) ? String.Join(" ", parameter, 1) : "";
		}
		#endregion

		#region Operators
		/// <summary>Compares two XmlParseParameter objects and returns FALSE if their Id's match (case-insensitive!).</summary>
		public static bool operator !=(XmlParseParameter left, XmlParseParameter right) => !(left==right);
		/// <summary>Compares two XmlParseParameter objects and returns TRUE (equal) if their Id's match (case-insensitive!).</summary>
		public static bool operator ==(XmlParseParameter left, XmlParseParameter right)
		{
			if (left is null) return (right is null);
			if (right is null) return false;
			return left.Id.Equals(right.Id, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>Compares an XmlParseParameter object against a string and returns FALSE if the Id matches the string (case-insensitive!).</summary>
		public static bool operator !=(XmlParseParameter left, string right) => !(left == right);
		/// <summary>Compares an XmlParseParameter object against a string and returns TRUE (equal) if the Id matches the string (case-insensitive!).</summary>
		public static bool operator ==(XmlParseParameter left, string right)
		{
			if (left is null) return string.IsNullOrWhiteSpace(right) || (right.Length == 0);
			if (right is null) return false;
			return left.Id.Equals(right, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>Compares an XmlParseParameter object against a string and returns FALSE if the Id matches the string (case-insensitive!).</summary>
		public static bool operator !=(string left, XmlParseParameter right) => !(right == left);
		/// <summary>Compares an XmlParseParameter object against a string and returns TRUE (equal) if the Id matches the string (case-insensitive!).</summary>
		public static bool operator ==(string left, XmlParseParameter right) => (right == left);
		#endregion

		#region Accessors
		/// <summary>Gets/Sets the parameter Id value in appropriate formats.</summary>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="FormatException"></exception>
		public string Id
		{
			get => "{{" + this._paramId + "}}";
			protected set
			{
				if (string.IsNullOrWhiteSpace(value) || (value.Trim().Length == 0))
					throw new ArgumentOutOfRangeException("The Id value cannot be empty, null or whitespace!");

				if (ValidateParameter(value))
				{
					string v = value.Trim();
					if (Regex.IsMatch(v,@"^" + PATTERN + @"$"))
						v = v.Substring(2, v.Length - 4); // Remove leading and trailing brace brackets.

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
		public static implicit operator KeyValuePair<string, string>(XmlParseParameter data) => new KeyValuePair<string, string>(data.Id, data.Value);
		public static implicit operator XmlParseParameter(KeyValuePair<string, string> data) => new XmlParseParameter(data.Key, data.Value);
		public static implicit operator string[] (XmlParseParameter data) => new string[]{data.Id, data.Value };
		public static implicit operator XmlParseParameter(string[] data) => new XmlParseParameter(data);

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

	/// <summary>[IEnumerator] Manages a collection of XmlParseParameter objects.</summary>
	public class XmlParameterCollection : IEnumerator<XmlParseParameter>
	{
		#region Properties
		/// <summary>Used for internal IEnumerable support.</summary>
		private int _position = 0;

		/// <summary>Holds the collection of XmlParseParameters that this object manages.</summary>
		protected List<XmlParseParameter> _collection = new List<XmlParseParameter>();
		#endregion

		#region Constructors
		public XmlParameterCollection() { }

		public XmlParameterCollection(string id, string value = "") =>
			this.Add(id, value);

		public XmlParameterCollection(XmlParseParameter parameter) =>
			this.Add(parameter);

		public XmlParameterCollection(XmlParseParameter[] parameters) =>
			this.AddRange(parameters);
		#endregion

		#region Accessors
		/// <summary>Reports the number of items currently being managed by the collection.</summary>
		public int Count => this._collection.Count;

		/// <summary>Facilitates direct access to the collection via index.</summary>
		/// <param name="index">An int value specifying the item to dereference.</param>
		public XmlParseParameter this[int index]
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
		XmlParseParameter IEnumerator<XmlParseParameter>.Current => this[this._position];

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
		protected int IndexOf(XmlParseParameter id) =>
			IndexOf(id.Id);

		/// <summary>Adds or modifies an XmlParseParameter to the collection.</summary>
		/// <param name="p">An XmlParseParameter to modify (if it already exists) or add to the collection.</param>
		public void Add(XmlParseParameter p)
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
		public void Add(string id, string value) => Add(new XmlParseParameter(id, value));

		/// <summary>Adds an array of XmlParseParameters to the collection.</summary>
		public void AddRange(XmlParseParameter[] parameters)
		{
			foreach (XmlParseParameter p in parameters)
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
		public void Remove(XmlParseParameter p) => this.Remove(p.Id);

		/// <summary>Removes an element from the collection by it's Index.</summary>
		public void RemoveAt(int index) => this._collection.RemoveAt(index);

		/// <summary>Empties the collection.</summary>
		public void Clear() => this._collection = new List<XmlParseParameter>();

		/// <summary>
		/// This function is intended to be called using a block of text containing ParseParameter blocks that is to be
		/// modified with the data contained in this library.
		/// </summary>
		/// <param name="source">A string containing the block of text who's parameters are to potentially be modified by this collection.</param>
		/// <returns>The passed text updated according to the information contained in this collection.</returns>
		public string Parse(string source)
		{
			foreach (XmlParseParameter p in this._collection)
				source = p.Parse(source);

			return source;
		}

		/// <summary>Outputs the contents of this collection as an array of XmlParseParameter objects.</summary>
		/// <returns>An array of XmlParseParameter objects culled from this collection.</returns>
		public XmlParseParameter[] ToArray() => this._collection.ToArray();

		// Facilitate simple data movement between XmlParameterCollection and an XmlParseParameter array...
		public static implicit operator XmlParameterCollection(XmlParseParameter[] data) => new XmlParameterCollection(data);
		public static implicit operator XmlParseParameter[](XmlParameterCollection data) => data.ToArray();

		/// <summary>This routine creates a new XmlParameterCollection object with some basic intial generic parameters.</summary>
		/// <param name="cmdVersion">A value specifying the application version.</param>
		/// <param name="copyright">A value specifying the application Copyright declaration.</param>
		/// <param name="cmdName">An optional parameter specifying the application's name.</param>
		/// <returns>An XmlParameterCollection populated with the supplied values.</returns>
		public static XmlParameterCollection Create(string cmdVersion, string copyright, string cmdName = "")
		{
			XmlParameterCollection collection = new XmlParameterCollection(
					new XmlParseParameter[]
					{
					new XmlParseParameter("cmdVersion", cmdVersion),
					new XmlParseParameter("cmdCopy", copyright)
					}
				);

			if (!string.IsNullOrWhiteSpace(cmdName) && (cmdName.Length > 0)) collection.Add("cmdName", cmdName);
			return collection;
		}

		/// <summary>Returns a clean, empty XmlParameterCollection object.</summary>
		public static XmlParameterCollection Empty => new XmlParameterCollection();
		#endregion

		//IEnumerator Support
		public IEnumerator<XmlParseParameter> GetEnumerator() { return this._collection.GetEnumerator(); }

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
		public static XmlParseParameter[] Convert(string[][] source)
		{
			XmlParameterCollection data = new XmlParameterCollection();
			foreach (string[] row in source)
				data.Add(row);

			return data.ToArray();
		}
		#endregion
	}

	/// <summary>Provides an extra layer of functionality to the basic .NET XmlElement class.</summary>
	public abstract class XmlShim : XmlElement
	{
		#region Dependant Classes
		/// <summary>Provides a class for managing Xml Entities.</summary>
		public class XmlEntityShim
		{
			#region Properties
			/// <summary>A Regex pattern to identify / validate an Entity string.</summary>
			public const string PATTERN = @"&[a-zA-Z]{2,6}[;]?";

			/// <summary>A Regex pattern to identify an Xml Entity declaration from raw XML text.</summary>
			public const string ENTITY_REGEX = "[\\s]*(<!ENTITY[ ]+)([a-z][a-z|0-9]{2,6})[ ]+(([\"]([\\S| ]+)[\"])|([']([\\S| ]+)[']))>";

			/// <summary>Holds the raw Xml Entity identifier (stripped of leading ampersand and trailing semi-colons).</summary>
			protected string _entity = "";

			/// <summary>Holds the replacement (translation) text that the entity corresponds with.</summary>
			protected string _translate = "";
			#endregion

			#region Constructors
			public XmlEntityShim(string entity, string translate)
			{
				this.Entity = entity;
				this._translate = translate;
			}

			public XmlEntityShim(string entity) =>
				this.Initialize(entity, "", XmlEntityShim.DefaultEntities());

			public XmlEntityShim(string entity, XmlEntityShim[] library) =>
				this.Initialize(entity, "", library);

			public XmlEntityShim(string entity, string translate, XmlEntityShim[] library) =>
				this.Initialize(entity, translate, library);

			public XmlEntityShim(XmlNode entity)
			{
				if (entity.NodeType == XmlNodeType.EntityReference)
					this.Initialize(entity.LocalName, "", XmlEntityShim.DefaultEntities());
				else
					throw new ArgumentException("The supplied value is not a recognized XmlEntity.");
			}

			public XmlEntityShim(XmlNode entity, string translate)
			{
				if (entity.NodeType == XmlNodeType.EntityReference)
					this.Initialize(entity.LocalName, translate, XmlEntityShim.DefaultEntities());
				else
					throw new ArgumentException("The supplied value is not a recognized XmlEntity.");
			}

			public XmlEntityShim(XmlNode entity, XmlEntityShim[] library)
			{
				if (entity.NodeType == XmlNodeType.EntityReference)
					this.Initialize(entity.LocalName, "", library);
				else
					throw new ArgumentException("The supplied value is not a recognized XmlEntity.");
			}

			public XmlEntityShim(XmlNode entity, string translate, XmlEntityShim[] library)
			{
				if (entity.NodeType == XmlNodeType.EntityReference)
					this.Initialize(entity.LocalName, translate, library);
				else
					throw new ArgumentException("The supplied value is not a recognized XmlEntity.");
			}

			public XmlEntityShim(XmlEntity entity) =>
				this.Initialize(entity.LocalName, "", XmlEntityShim.DefaultEntities());

			public XmlEntityShim(XmlEntity entity, string translate) =>
				this.Initialize(entity.LocalName, translate, XmlEntityShim.DefaultEntities());

			public XmlEntityShim(XmlEntity entity, XmlEntityShim[] library) =>
				this.Initialize(entity.LocalName, "", library);

			public XmlEntityShim(XmlEntity entity, string translate, XmlEntityShim[] library) =>
				this.Initialize(entity.LocalName, translate, library);

			/// <summary>Initializes the object with the specified values.</summary>
			/// <param name="entity">A string specifying the entity identifier.</param>
			/// <param name="translate">A string specifying the corresponding text.</param>
			/// <param name="library"></param>
			private void Initialize(string entity, string translate, XmlEntityShim[] library)
			{
				this.Entity = entity;

				this._translate = (translate.Length == 0) ? XmlEntityShim.DecodeDefaultEntity(this.Entity, library) : translate;
				if (String.Compare(this.Translation, this.Entity, true) == 0)
					throw new ArgumentException("The supplied entity translation value causes an infinite recursion loop.");
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
				set => this._translate = value;
			}
			#endregion

			#region Methods
			//  <!ENTITY bksp "\b">
			public override string ToString() => "<!ENTITY " + this._entity.ToLowerInvariant() + " \"" + this._translate + "\">";

			/// <summary>Finds all instances of translated texts in the supplied source and replaces them with appropriate Entities instead.</summary>
			/// <param name="source">The text to revert text to entities in.</param>
			/// <returns>The supplied text with all appropriate texts replaced by associated entities.</returns>
			protected string Encode(string source) =>
				(source.IndexOf(this.Translation) >= 0) ? source.Replace(this.Translation, this.Entity) : source;

			/// <summary>Finds all existing entities in the provided string and replaces them with appropriate translations.</summary>
			/// <param name="source">The text whose entites require translation to text equivalents.</param>
			/// <returns>The supplied text with all appropriate entities replaced with associated translations.</returns>
			protected string Decode(string source) =>
				(source.IndexOf(this.Entity) >= 0) ? source.Replace(this.Entity, this.Translation) : source;
			#endregion

			#region Static Methods
			/// <summary>Applies a collection of EntityShim objects to a suplied string.</summary>
			/// <param name="source">The text to apply the EntityShim collection to.</param>
			/// <param name="entityList">An array of EntityShim objects to apply to the string.</param>
			/// <returns>The original string with all specified entites translated to text.</returns>
			public static string MassEncode(string source, XmlEntityShim[] entityList = null)
			{
				if (entityList is null) entityList = DefaultEntities();
				foreach (XmlEntityShim e in entityList) source = e.Encode(source);
				return source;
			}

			/// <summary>Removes the translated texts from a suplied string and replaces them with corresponding Entities.</summary>
			/// <param name="source">The text to reverse-translate the EntityShim collection from.</param>
			/// <param name="entityList">An array of EntityShim objects to apply to the string.</param>
			/// <returns>The original string with all relevant strings reverse translated to their EntityShim counterparts.</returns>
			public static string MassDecode(string source, XmlEntityShim[] entityList = null)
			{
				if (entityList is null) entityList = DefaultEntities();
				foreach (XmlEntityShim e in entityList) source = e.Decode(source);
				return source;
			}

			/// <summary>Constructs a pre-defined Entity Library containing the most commonly used Help Entities.</summary>
			public static XmlEntityShim[] DefaultEntities()
			{
				return new XmlEntityShim[] {
					new XmlEntityShim("amp","&"),
					new XmlEntityShim("crlf", "\r"),
					new XmlEntityShim("bksp", "\b"),
					new XmlEntityShim("space", " "),
					new XmlEntityShim("lt", "<"),
					new XmlEntityShim("gt", ">"),
					new XmlEntityShim("quote", "\""),
					new XmlEntityShim("apos", "'"),
					new XmlEntityShim("raquo", "»"),
					new XmlEntityShim("laquo", "«")
				};
			}

			/// <summary>Finds a specified entity from an array of them and returns its translation text.</summary>
			/// <param name="source"></param>
			/// <param name="library"></param>
			/// <returns></returns>
			public static string DecodeDefaultEntity(string source, XmlEntityShim[] library = null)
			{
				if (ValidateEntity(source))
				{
					if (library == null) library = DefaultEntities();

					foreach (XmlEntityShim x in library)
						if (String.Compare(x.Entity, source, true) == 0)
							return x.Translation;

					throw new ArgumentException("The entity '" + source + "' is not in the Default Entity Translation library.");
				}
				else

					throw new FormatException("The supplied Entity value (\"" + source + "\") is not appropriate for use as an Xml Entity.");
			}

			/// <summary>Validates a supplied string as being suitable for use as an entity.</summary>
			/// <param name="text">A string containing the text to validate.</param>
			/// <returns>TRUE if the string is suitable to be used as an Entity, otherwise FALSE.</returns>
			public static bool ValidateEntity(string text) =>
				Regex.IsMatch(text.Trim(), @"^((" + PATTERN + @")|([a-z]{2,6}))$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
			#endregion
		}
		#endregion

		#region Attributes
		public static XmlDocument XmlDoc = new XmlDocument();
		private static XmlEntityShim[] _defaultEntities = XmlEntityShim.DefaultEntities();
		#endregion

		#region Constructor
		public XmlShim(string TagName) : base(TagName, TagName, "", XmlDoc)
		{
			if (TagName == "") throw new ArgumentOutOfRangeException();
		}

		public XmlShim(string TagName, XmlElement source) : base(TagName, TagName, "", XmlDoc) =>
			this.NormalizeAttributes(source, new string[] { "id" });
		#endregion

		#region Accessors
		/// <summary>Facilitates direct access to the Id attribute of these objects.</summary>
		public string Id
		{
			get => this.GetAttributeValue("id");
			set => this.SetAttribute("id", value, "", true);
		}

		/// <summary>Facilitates direct access to the Style attribute of these objects.</summary>
		public string Style
		{
			get => this.HasAttribute("style") ? this.GetAttributeValue("style") : "";
			set => this.SetAttribute("style", value);
		}

		public XmlNode[] UsefulNodes => XmlShim.ExtractUsefulNodes(this);

		public static XmlEntityShim[] DefaultEntities
		{
			get => XmlShim._defaultEntities;
			set => XmlShim._defaultEntities = ((value == null) || (value.Length == 0)) ? XmlEntityShim.DefaultEntities() : value;
		}

		/// <summary>Facilitates checking to see if the Style attribute is defined and has a value.</summary>
		public bool HasStyle => (this.Style.Length > 0);

		/// <summary>Gets/Sets the foreground color attribute for this object.</summary>
		public ConsoleColor Fore
		{
			get => this.Color.Fore;
			set => this.Color = new HelpColor(value, Back);
		}

		/// <summary>Gets/Sets the background color attribute for this object.</summary>
		public ConsoleColor Back
		{
			get => this.Color.Back;
			set => this.Color = new HelpColor(Fore, value);
		}

		/// <summary>Facilitates interacting directly with the HelpColor settings of this object's Style attribute.</summary>
		public HelpColor Color
		{
			get => (this.HasStyle) ? HelpColor.ParseXml(this.Style) : HelpColor.Default;
			set => this.Style = ((value is null) ? HelpColor.Default : value).ToStyleString();
		}
		#endregion

		#region Methods
		protected virtual XmlElement[] FetchAllByTagName(string tagName)
		{
			List<XmlElement> results = new List<XmlElement>();
			if (this.HasChildNodes)
				foreach (XmlElement e in this.ChildNodes)
					if (e.Name.ToUpperInvariant() == tagName.ToUpperInvariant())
						results.Add(e);

			return results.ToArray();
		}

		new protected void SetAttribute(string name, string value)
		{
			// Removes dangerous/unwanted characters from the string.
			value = this.EncodeEntities(value);
			foreach (string s in new string[] { "\\", "\r", "\n" })
				value = value.Replace(s, "");

			base.SetAttribute(name, value);
		}

		protected void SetAttribute(string name, XmlAttribute value) =>
			this.SetAttribute(name, value.Value);

		protected void SetAttribute(string name, string value, string defaultValue, bool clearDefault)
		{
			if (this.HasAttribute(name) && this.Attributes[name].IsEqualTo(value)) return;

			if (string.Compare(value, defaultValue, true) == 0)
			{
				if (this.HasAttribute(name) && clearDefault)
					this.Attributes.RemoveNamedItem(name);
				return; // Nothing more to do...
			}

			if ((string.Compare(value, defaultValue, true) != 0) || !clearDefault)
				this.SetAttribute(name, value);
		}

		protected void SetAttribute(string name, int value, int defaultValue, bool clearDefault) =>
			this.SetAttribute(name, value.ToString(), defaultValue.ToString(), clearDefault);

		protected void SetAttribute(string name, int value) =>
			this.SetAttribute(name, value.ToString());

		protected void AttributeCheck(XmlElement source, string search, string replace)
		{
			if (source.HasAttributes && source.HasAttribute(search) && !this.HasAttribute(replace))
				this.SetAttribute(replace, source.Attributes[search]);

			if (this.HasAttribute(search)) this.Attributes.RemoveNamedItem(search);
		}

		protected void AttributeCheck(XmlElement source, string[] search, string replace)
		{
			foreach (string s in search) this.AttributeCheck(source, s, replace);
			if (!this.HasAttribute(replace) && (source.HasAttribute(replace)))
				this.SetAttribute(replace, source.Attributes[replace].Value);
		}

		protected void NormalizeAttributes(XmlElement source, string[] attributeList)
		{
			foreach (string s in attributeList)
				foreach (XmlAttribute a in source.Attributes)
					if ((a.Name == s) && !this.HasAttribute(s))
						this.SetAttribute(a.Name, a.Value);
					else
						if ((a.Name != s) && (String.Compare(a.Name, s, true) == 0))
						this.AttributeCheck(source, a.Name, s);
		}

		protected bool CompareAttribute(string name, string value, bool ignoreCase)
		{
			if ((name.Length == 0) || !this.HasAttribute(name)) return false;
			return (String.Compare(this.Attributes[name].Value, value, ignoreCase) == 0);
		}

		protected bool CompareAttribute(string name, string value) =>
			this.CompareAttribute(name, value, true);

		protected bool CompareAttributes(XmlElement a, string name, bool ignoreCase)
		{
			if ((a == null) || (name.Length == 0) || !this.HasAttribute(name) || !a.HasAttribute(name)) return false;
			return (String.Compare(this.Attributes[name].Value, a.Attributes[name].Value, ignoreCase) == 0);
		}

		protected bool CompareAttributes(XmlElement a, string name) =>
			this.CompareAttributes(a, name, true);

		protected int GetAttributeValueAsInt(string name, int defaultValue, bool overwrite)
		{
			int result = defaultValue;
			if (this.HasAttribute(name))
				try { result = int.Parse(this.Attributes[name].Value); }
				catch { if (overwrite) this.SetAttribute(name, defaultValue.ToString()); }
			return result;
		}

		protected int GetAttributeValueAsInt(string name, bool overwrite) =>
			this.GetAttributeValueAsInt(name, 0, overwrite);

		protected int GetAttributeValueAsInt(string name) =>
			this.GetAttributeValueAsInt(name, 0, false);

		protected string GetAttributeValue(string name, string defaultValue = "") //, bool overwrite = false)
		{
			if (!this.HasAttribute(name)) this.SetAttribute(name, defaultValue);
			return this.EncodeEntities(this.Attributes[name].Value);
		}

		protected string GetAttributeValue(string name) =>
			this.GetAttributeValue(name, ""); //, true);

		public override string ToString() =>
			"{XmlShim(" + this.Name + "): " + this.InnerXml + "}";

		protected string EncodeEntities(string source) =>
			XmlEntityShim.MassEncode(source);

		protected string DecodeEntities(string source) =>
			XmlEntityShim.MassDecode(source);

		protected XmlElement GetFirstChildByTagName(string tagName)
		{
			XmlNodeList nodes = this.GetElementsByTagName(tagName);
			if (nodes.Count > 0) return (XmlElement)nodes[0];
			return null;
		}
		#endregion

		#region Static Methods
		public static bool CheckTag(XmlNode node, string tag)
		{
			return (node.NodeType == XmlNodeType.Element) && ((String.Compare(node.Name, tag, true) == 0));
		}

		public static bool CheckTag(XmlElement node, string tag) { return XmlShim.CheckTag((XmlNode)node, tag); }

		public static bool XmlCompareAttributes(XmlElement a, XmlElement b, string name, bool ignoreCase)
		{
			if (a == null) return (b == null);
			if (b == null) return false;

			if (a.HasAttribute(name) && b.HasAttribute(name))
				return (String.Compare(a.Attributes[name].Value, b.Attributes[name].Value, ignoreCase) == 0);

			return false;
		}

		public static bool XmlCompareAttributes(XmlElement a, XmlElement b, string name)
		{
			return XmlShim.XmlCompareAttributes(a, b, name, true);
		}

		public static XmlNode[] ExtractUsefulNodes(XmlNodeList source, bool useRecursion)
		{
			List<XmlNode> nodeList = new List<XmlNode>();
			foreach (XmlNode n in source)
				if ((n.NodeType == XmlNodeType.Element) || (n.NodeType == XmlNodeType.Text) || (n.NodeType == XmlNodeType.Entity))
				{
					nodeList.Add(n);
					if (useRecursion && n.HasChildNodes)
						nodeList.AddRange(XmlShim.ExtractUsefulNodes(n.ChildNodes, useRecursion));
				}

			return nodeList.ToArray();
		}

		public static XmlNode[] ExtractUsefulNodes(XmlNodeList source) =>
			XmlShim.ExtractUsefulNodes(source, false);

		public static XmlNode[] ExtractUsefulNodes(XmlDocument source) =>
			XmlShim.ExtractUsefulNodes(source.ChildNodes, false);

		public static XmlNode[] ExtractUsefulNodes(XmlDocument source, bool useRecursion) =>
			XmlShim.ExtractUsefulNodes(source.ChildNodes, useRecursion);

		public static XmlNode[] ExtractUsefulNodes(XmlElement source, bool useRecursion) =>
			XmlShim.ExtractUsefulNodes(source.ChildNodes, useRecursion);

		public static XmlNode[] ExtractUsefulNodes(XmlElement source) =>
			XmlShim.ExtractUsefulNodes(source.ChildNodes, false);

		public static XmlEntityShim[] ParseEntities(XmlDocument source)
		{
			List<XmlEntityShim> entities = new List<XmlEntityShim>();
			if (source.HasChildNodes)
				foreach (XmlNode n in source.ChildNodes)
					if (n.NodeType == XmlNodeType.Entity)
						entities.Add(new XmlEntityShim(n));

			return entities.ToArray();
		}
		#endregion
	}

	/// <summary>Builds on the existing XmlShim structures to provide an Abstract foundation for subsequent various descendants.</summary>
	internal abstract class XmlTextOutput : XmlShim
	{
		#region Attributes
		public enum XmlSubType { None, Group, Paragraph, Text, Entity }

		protected int _indentSize = 0;
		protected List<XmlTextOutput> _nodes = new List<XmlTextOutput>();
		#endregion

		#region Constructor
		public XmlTextOutput(string tagName, HelpColor color = null) : base(tagName)
		{
			this.Color = (color is null) ? HelpColor.Default : color;
		}

		public XmlTextOutput(string tagName, XmlElement source, HelpColor color = null) : base(tagName, source)
		{
			this.Color = (color is null) ? HelpColor.Default : color;
			this.InnerXml = source.InnerXml;

			this.NormalizeAttributes(source, "pre,post,indent,style".Split(','));
			if (this.HasAttribute("indent")) this.IndentSize = this.GetAttributeValueAsInt("indent");
		}
		#endregion

		#region Operators
		//public static bool operator ==(XmlElement a, XmlTextOutput b)
		//{
		//}
		#endregion

		#region Accessors
		/// <summary>Facilitates checking the XmlTextOutput.XmlSubType setting for descendent objects.</summary>
		public XmlSubType SubType => this.MySubType();

		/// <summary>Specifies the number of spaces to prefix this element's content with when being output.</summary>
		public int IndentSize
		{
			get => this._indentSize;
			set
			{
				this._indentSize = Math.Max(value, 0);
				this.SetAttribute("indent", this._indentSize, 0, true);
			}
		}

		/// <summary>Parses this object's children and returns ones that we recognize in an Array.</summary>
		protected new XmlTextOutput[] UsefulNodes
		{
			get
			{
				List<XmlTextOutput> nodes = new List<XmlTextOutput>();
				foreach (XmlNode n in this.ChildNodes)
					switch (n.NodeType)
					{
						case XmlNodeType.Element:
							if (XmlShim.CheckTag(n, XmlParagraph.TAG_NAME))
								nodes.Add(new XmlParagraph((XmlElement)n, this.Color));
							if (XmlShim.CheckTag(n, XmlTextNode.TAG_NAME))
								nodes.Add(new XmlTextNode((XmlElement)n, this.Color));
							break;
						case XmlNodeType.Text:
							XmlTextNode t = new XmlTextNode(n.InnerXml.Length > 0 ? n.InnerXml : n.InnerText, this.Color);
							if (this.SubType == XmlSubType.Text)
							{
								t.Color = this.Color;
								if (((XmlTextNode)this).AddCR) t.SetAttribute("addcr", "true");
							}
							nodes.Add(t);
							break;
					}
				return nodes.ToArray();
			}
		}
		#endregion

		#region Methods
		/// <summary>Validates that a supplied XmlElement has the correct element Name.</summary>
		/// <param name="n">An XmlElement to be checked.</param>
		/// <returns>TRUE if the supplied XmlElement matches the </returns>
		protected bool Check(XmlElement n) =>
			XmlShim.CheckTag(n, this.Prefix);

		// At the base level, perform a render as though done from the group level
		// (since that's the most common derivation of this parent object)
		public virtual int Render(int indentStart, int curPos = 0, XmlParameterCollection parameters = null)
		{
			if (parameters is null) parameters = new XmlParameterCollection();
			if (this.HasChildNodes)
				foreach (XmlTextOutput o in this.UsefulNodes)
					switch (o.SubType)
					{
						case XmlSubType.Paragraph:
							curPos = ((XmlParagraph)o).Render(indentStart, curPos, parameters);
							break;
						case XmlSubType.Text:
							curPos = ((XmlTextNode)o).Render(indentStart, curPos, parameters);
							break;
						case XmlSubType.Group:
							curPos = o.Render(indentStart, curPos, parameters);
							break;
					}
			return curPos;
		}

		public override string ToString() =>
			base.ToString().Replace("XmlShim", "XmlTextOutput");

		/// <summary>Forces descendents to define this attribute.</summary>
		protected abstract XmlSubType MySubType();
		#endregion

		#region Static Methods
		public bool IsXmlHelpType(dynamic obj) => (obj.GetType() == typeof(XmlTextOutput)) || IsXmlHelpType(obj.GetType().BaseType);

		public bool IsXmlHelpType(Type objectType) =>
			(objectType == typeof(object)) ? false : (objectType == typeof(XmlTextOutput) || IsXmlHelpType(objectType.BaseType));
		#endregion
	}

	/// <summary>Provides a foundation for objects that are intended to encapsulate other contents.</summary>
	internal abstract class XmlGenericContentNode : XmlTextOutput
	{
		#region Properties
		protected string _tagName;
		#endregion

		#region Constructors
		public XmlGenericContentNode(string tagName, XmlElement sourceNode, HelpColor color = null) : base(tagName, color)
		{
			if (color is null) color = HelpColor.Default;

			if (tagName.Length == 0) throw new ArgumentNullException("The specified TagName value cannot be blank!");
			this._tagName = tagName;
			this.Color = color;
			if (this.Check(sourceNode))
			{
				this.InnerXml = sourceNode.InnerXml;
				this.AttributeCheck(sourceNode, new string[] { "name", "Name", "NAME", "Id", "ID" }, "id");
			}
		}

		public XmlGenericContentNode(string tagName, HelpColor color) : base(tagName, color)
		{
			if (tagName.Length == 0) throw new ArgumentNullException("The specified TagName value cannot be blank!");
			this._tagName = tagName;
		}
		#endregion

		#region Operators
		#endregion

		#region Accessors
		public XmlNode[] Contents
		{
			get
			{
				List<XmlNode> contents = new List<XmlNode>();
				foreach (XmlNode e in this.UsefulNodes) contents.Add(e);
				return contents.ToArray();
			}
		}

		public int ContentCount { get => this.UsefulNodes.Length; }
		#endregion

		#region Methods
		/// <summary>Facilitates creating and adding a new child element to this object.</summary>
		/// <typeparam name="T">The Type of the child to create and add.</typeparam>
		/// <param name="right"></param>
		/// <returns></returns>
		protected T Add<T>(XmlGenericContentNode right)
		{
			T newNode = (T)Activator.CreateInstance(typeof(T), new object[] { this.Color });

			foreach (XmlNode node in this.Contents)
				(newNode as XmlGenericContentNode).AppendChild(node);

			foreach (XmlNode node in right.Contents)
				(newNode as XmlGenericContentNode).AppendChild(node);

			return newNode;
		}

		public override string ToString() =>
			base.ToString().Replace("XmlTextOutput", "XmlCategoryNode");
		#endregion

		#region Static Methods
		protected static T Parse<T>(string source, HelpColor color = null)
		{
			XmlShim.XmlDoc.Load(source);
			return XmlGenericContentNode.Parse<T>(XmlShim.XmlDoc, color);
		}

		protected static T Parse<T>(XmlDocument source, HelpColor color)
		{
			XmlNodeList nList = source.GetElementsByTagName(XmlCategoryNode.TAG_NAME);
			return (nList.Count > 0) ? XmlGenericContentNode.Instantiate<T>((XmlElement)nList[0], color) : default;
		}

		protected static T Parse<T>(XmlElement source, HelpColor color)
		{
			if (source.HasChildNodes)
				foreach (XmlElement e in source.ChildNodes)
					if (XmlShim.CheckTag(e, XmlCategoryNode.TAG_NAME))
						return XmlGenericContentNode.Instantiate<T>(e, color);

			return default;
		}

		protected static T Parse<T>(XmlDocument source, string byId, HelpColor color)
		{
			if (byId.Length == 0) return XmlGenericContentNode.Parse<T>(source, color);
			XmlNodeList nList = source.GetElementsByTagName(XmlCategoryNode.TAG_NAME);
			if (nList.Count > 0)
				foreach (XmlNode n in nList)
					if (n.HasAttribute("id") && n.AttributeValueEquals("id", byId))
						return XmlGenericContentNode.Instantiate<T>((XmlElement)n, color);

			return default;
		}

		protected static T Instantiate<T>(XmlElement e, HelpColor color)
		{
			if (!IsConformingType(typeof(T)))
				throw new ArgumentException("The specified type (\"" + (typeof(T).Name) + "\") is not valid for this constructor.");

			dynamic result = Activator.CreateInstance(typeof(T));
			return (T)System.Convert.ChangeType(result, typeof(T));
		}

		public static bool IsConformingType(object obj) => IsConformingType(obj.GetType());

		public static bool IsConformingType(Type test) =>
			(test == typeof(Object)) ? false : ((test == typeof(XmlGenericContentNode)) || IsConformingType(test.BaseType));
		#endregion
	}

	// Non-dependent nodes are just simple/basic XML elements with no custom attributes.
	// Declared non-dependent nodes: "categories", "commands"

	/// <summary>Category elements must declare an "id" attribute that specifies the category they cover.</summary>
	internal class XmlCategoryNode : XmlGenericContentNode
	{
		#region Properties
		public const string TAG_NAME = "category";
		#endregion

		#region Constructors
		public XmlCategoryNode(XmlElement sourceNode, HelpColor color) : base(TAG_NAME, sourceNode, color) { }

		public XmlCategoryNode(HelpColor color) : base(TAG_NAME, color) { }
		#endregion

		#region Accessors
		#endregion

		#region Methods
		public XmlCategoryNode Add(XmlGenericContentNode newNode) => base.Add<XmlCategoryNode>(newNode);

		protected override XmlSubType MySubType() => XmlSubType.Group;
		#endregion

		#region Static Methods
		public static XmlCategoryNode Parse(string source, HelpColor color) =>
			XmlGenericContentNode.Parse<XmlCategoryNode>(source, color);

		public static XmlCategoryNode Parse(XmlDocument source, HelpColor color) =>
			XmlGenericContentNode.Parse<XmlCategoryNode>(source, color);

		public static XmlCategoryNode Parse(XmlElement source, HelpColor color) =>
			XmlGenericContentNode.Parse<XmlCategoryNode>(source, color);

		public static XmlCategoryNode Parse(XmlDocument source, string byId, HelpColor color) =>
			XmlGenericContentNode.Parse<XmlCategoryNode>(source, byId, color);
		#endregion
	}

	internal class XmlContentNode : XmlGenericContentNode
	{
		#region Properties
		public const string TAG_NAME = "content";
		#endregion

		#region Constructors
		public XmlContentNode(XmlElement sourceNode, HelpColor color) : base(TAG_NAME, sourceNode, color) { }

		public XmlContentNode(HelpColor color) : base(TAG_NAME, color) { }
		#endregion

		#region Methods
		public XmlContentNode Add(XmlGenericContentNode newNode) => base.Add<XmlContentNode>(newNode);

		protected override XmlSubType MySubType() => XmlSubType.Group;
		#endregion

		#region Static Methods
		public static XmlContentNode Parse(string source, HelpColor color) =>
			XmlGenericContentNode.Parse<XmlContentNode>(source, color);

		public static XmlContentNode Parse(XmlElement source, HelpColor color) =>
			XmlGenericContentNode.Parse<XmlContentNode>(source, color);

		public static XmlContentNode Parse(XmlDocument source, HelpColor color) =>
			XmlGenericContentNode.Parse<XmlContentNode>(source, color);

		public static XmlContentNode Parse(XmlDocument source, string byId, HelpColor color) =>
			XmlGenericContentNode.Parse<XmlContentNode>(source, byId, color);
		#endregion
	}

	internal class XmlCmdSwitchNode : XmlGenericContentNode
	{
		#region Properties
		public const string TAG_NAME = "switch";
		#endregion

		#region Constructors
		public XmlCmdSwitchNode(XmlElement sourceNode, HelpColor color) : base(TAG_NAME, sourceNode, color) { }

		public XmlCmdSwitchNode(HelpColor color) : base(TAG_NAME, color) { }
		#endregion

		#region Methods
		public XmlCmdSwitchNode Add(XmlGenericContentNode newNode) => base.Add<XmlCmdSwitchNode>(newNode);

		protected override XmlSubType MySubType() => XmlSubType.Group;
		#endregion

		#region Static Methods
		public static XmlCmdSwitchNode Parse(string source, HelpColor color = null) =>
			XmlGenericContentNode.Parse<XmlCmdSwitchNode>(source, color);

		public static XmlCmdSwitchNode Parse(XmlDocument source, HelpColor color = null) =>
			XmlGenericContentNode.Parse<XmlCmdSwitchNode>(source, color);

		public static XmlCmdSwitchNode Parse(XmlDocument source, string byId, HelpColor color = null) =>
			XmlGenericContentNode.Parse<XmlCmdSwitchNode>(source, byId, color);

		public static XmlCmdSwitchNode Parse(XmlElement source, HelpColor color) =>
			XmlGenericContentNode.Parse<XmlCmdSwitchNode>(source, color);
		#endregion
	}

	/// <summary>Manages SwitchSet nodes.</summary>
	/// <remarks>
	///		<switchset keys='/ver /help /?'>
	///			<switch id='VER'>Returns the Version of the application.</switch>
	///			<switch id='help'>Returns basic help for this application.</switch>
	///			<switch id='?'>Alternative to /help.</switch>
	///		</switchset>
	/// </remarks>
	internal class XmlCmdSwitchSetNode : XmlShim
	{
		#region Properties
		public const string TAG_NAME = "switchset";
		protected List<XmlCmdSwitchNode> _switches = new List<XmlCmdSwitchNode>();
		protected XmlContentNode _contents;
		#endregion

		#region Constructors
		public XmlCmdSwitchSetNode(XmlElement sourceNode, HelpColor color = null) : base(TAG_NAME, sourceNode)
		{
			this.Color = color;
			if (this.Check(sourceNode))
			{
				this.SetAttribute("keys", sourceNode.GetAttributeValue("keys"));

				XmlNodeList nodes = sourceNode.GetElementsByTagName("switch");
				foreach (XmlNode node in nodes)
				{
					this.AppendChild(this.OwnerDocument.ImportNode(node, true));
					this._switches.Add(new XmlCmdSwitchNode((XmlElement)node, color));
				}

				XmlNode content = sourceNode.GetFirstNamedElement("content");
				this._contents = (content is null) ? null : new XmlContentNode((XmlElement)content, color);
			}
		}

		public XmlCmdSwitchSetNode(HelpColor color = null) : base(TAG_NAME)
		{
			this.Color = color;
			this.Keys = "";
		}
		#endregion

		#region Accessors
		public string Keys
		{
			get => this.GetAttributeValue("keys");
			set => this.SetAttribute("keys", value);
		}

		public XmlCmdSwitchNode[] Switches => this._switches.ToArray();

		new public XmlCmdSwitchNode this[string id]
		{
			get
			{
				foreach (XmlCmdSwitchNode node in this._switches)
				{
					XmlAttribute a = (node as XmlNode).GetAttribute("id");
					if (!(a is null) && (a.Value.Length > 0) && a.IsEqualTo(id))
						return node;
				}
				return null;
			}
		}

		public XmlContentNode Content => this._contents;

		public int SwitchCount => this._switches.Count;
		#endregion

		#region Methods
		protected bool Check(XmlElement n) => XmlShim.CheckTag(n, TAG_NAME);

		protected int IndexOf(string id)
		{
			int i = -1; while ((++i < this._switches.Count) && !this._switches[i].Id.Equals(id, StringComparison.InvariantCultureIgnoreCase)) ;
			return (i < this._switches.Count) ? i : -1;
		}

		public XmlCmdSwitchSetNode Add(XmlCmdSwitchSetNode right)
		{
			XmlCmdSwitchSetNode newNode = new XmlCmdSwitchSetNode(this.Color);
			newNode.Keys = this.Keys;

			foreach (XmlCmdSwitchNode sw in this._switches)
				newNode.Add(sw);

			foreach (XmlCmdSwitchNode sw in right.Switches)
				newNode.Add(sw);

			return newNode;
		}

		public void Add(XmlCmdSwitchNode newSwitch)
		{
			foreach (XmlCmdSwitchNode sw in this._switches)
			{
				int i = this.IndexOf(sw.Id);
				if (i < 0)
				{
					this._switches.Add(sw);
					this.Keys += " /" + sw.Id;
				}
				else
					this._switches[i] = sw;
			}
		}

		public int Render(int indentStart = 0, int curPos = 0, XmlParameterCollection parameters = null)
		{
			if (parameters is null) parameters = new XmlParameterCollection();

			if (!(this._contents is null))
				curPos = this._contents.Render(indentStart, curPos, parameters);

			if (this._switches.Count > 0)
				foreach (XmlCmdSwitchNode xcsn in this.Switches)
					curPos = xcsn.Render(indentStart, curPos, parameters);

			return curPos;
		}

		public override string ToString() =>
			base.ToString().Replace("XmlShim", "XmlTextOutput");

		public static XmlCmdSwitchSetNode Parse(string source, HelpColor color = null)
		{
			XmlShim.XmlDoc.LoadXml(MiscellaneousExtensions.XML_HEADER + source);
			return XmlCmdSwitchSetNode.Parse(XmlShim.XmlDoc.GetFirstNamedElement("switchset"), color);
		}

		public static XmlCmdSwitchSetNode Parse(XmlNode source, HelpColor color = null) =>
			(source.Name.Equals("switchset", StringComparison.InvariantCultureIgnoreCase)) ?
				new XmlCmdSwitchSetNode((XmlElement)source, color)
			:
				new XmlCmdSwitchSetNode(color);

		/// <summary>Emulates the same call as in classes derived from XmlTextNode.</summary>
		protected XmlTextNode.XmlSubType MySubType() => XmlTextNode.XmlSubType.Group;
		#endregion
	}

	/// <summary>Manages Intro nodes (subset of CMD node).</summary>
	/// <remarks>
	///		<intro copyright='2018, Brett Leuszler'>
	///			{introduction content}
	///		</intro>
	/// </remarks>
	internal class XmlCmdIntroNode : XmlGenericContentNode
	{
		#region Properties
		public const string TAG_NAME = "intro";
		#endregion

		#region Accessors
		public string Copyright
		{
			get => this.GetAttributeValue("copyright");
			set => this.SetAttribute("copyright", value, "", true);
		}
		#endregion

		#region Constructors
		public XmlCmdIntroNode(XmlElement sourceNode, HelpColor color) : base(TAG_NAME, sourceNode, color)
		{
			this.AttributeCheck(sourceNode, new string[] { "copyright", "Copyright", "copyRight", "CopyRight", "COPYRIGHT" }, "copyright");
		}

		public XmlCmdIntroNode(HelpColor color) : base(TAG_NAME, color) { }
		#endregion

		#region Methods
		public XmlCmdIntroNode Add(XmlGenericContentNode newNode) => base.Add<XmlCmdIntroNode>(newNode);

		public override int Render(int indentStart, int curPos = 0, XmlParameterCollection parameters = null)
		{
			if (parameters is null) parameters = new XmlParameterCollection("copyright", this.Copyright);
			parameters["copyright"] = this.Copyright;
			return base.Render(indentStart, curPos, parameters);
		}
		protected override XmlSubType MySubType() => XmlSubType.Group;
		#endregion

		#region Static Methods
		public static XmlCmdIntroNode Parse(string source, HelpColor color) =>
			XmlGenericContentNode.Parse<XmlCmdIntroNode>(source, color);

		public static XmlCmdIntroNode Parse(XmlElement source, HelpColor color) =>
			XmlGenericContentNode.Parse<XmlCmdIntroNode>(source, color);

		public static XmlCmdIntroNode Parse(XmlDocument source, HelpColor color) =>
			XmlGenericContentNode.Parse<XmlCmdIntroNode>(source, color);

		public static XmlCmdIntroNode Parse(XmlDocument source, string byId, HelpColor color) =>
			XmlGenericContentNode.Parse<XmlCmdIntroNode>(source, byId, color);
		#endregion
	}

	internal class XmlCmdDetailNode : XmlGenericContentNode
	{
		#region Properties
		public const string TAG_NAME = "detailedHelp";
		#endregion

		#region Constructors
		public XmlCmdDetailNode(XmlElement sourceNode, HelpColor color) : base(TAG_NAME, sourceNode, color) { }

		public XmlCmdDetailNode(HelpColor color) : base(TAG_NAME, color) { }
		#endregion

		#region Methods
		public XmlCmdDetailNode Add(XmlGenericContentNode newNode) => base.Add<XmlCmdDetailNode>(newNode);

		protected override XmlSubType MySubType() => XmlSubType.Group;
		#endregion

		#region Static Methods
		public static XmlCmdDetailNode Parse(string source, HelpColor color) =>
			XmlGenericContentNode.Parse<XmlCmdDetailNode>(source, color);

		public static XmlCmdDetailNode Parse(XmlDocument source, HelpColor color) =>
			XmlGenericContentNode.Parse<XmlCmdDetailNode>(source, color);

		public static XmlCmdDetailNode Parse(XmlElement source, HelpColor color) =>
			XmlGenericContentNode.Parse<XmlCmdDetailNode>(source, color);

		public static XmlCmdDetailNode Parse(XmlDocument source, string byId, HelpColor color) =>
			XmlGenericContentNode.Parse<XmlCmdDetailNode>(source, byId, color);
		#endregion
	}

	internal class XmlTextNode : XmlTextOutput
	{
		#region Properties
		public const string TAG_NAME = "TEXT";
		#endregion

		#region Constructors
		public XmlTextNode(XmlNode sourceNode, HelpColor color = null) : base(XmlTextNode.TAG_NAME, color)
		{
			this._indentSize = 0;
			if (sourceNode.NodeType == XmlNodeType.Text)
				this.Content = sourceNode.InnerText;
			else
				throw new ArgumentException("Passed XmlNode is an invalid type ('" + sourceNode.NodeType.ToString() + "'");
		}

		public XmlTextNode(XmlElement source, HelpColor color) : base(XmlTextNode.TAG_NAME, source, color)
		{
			if (this.Check(source))
			{
				if ((source.ChildNodes.Count == 1) && (source.ChildNodes[0].NodeType == XmlNodeType.Text))
					this.Content = source.InnerText;
				else
					this.InnerXml = source.InnerXml.Trim();

				this.NormalizeAttributes(source, "addcr".Split(','));
				this.CommonInitOps();
			}
			else
				throw new ArgumentException("Passed XmlElement is not suitable for instantiating a valid XmlTextNode object.");
		}

		public XmlTextNode(string text, bool addCr = false, HelpColor color = null) : base(TAG_NAME, color)
		{
			this.AddCR = addCr;
			this.Content = text;
		}

		public XmlTextNode(string tag = TAG_NAME, HelpColor color = null) : base(tag, color) { }

		private void CommonInitOps()
		{
			if (this.HasAttribute("indent")) this.Attributes.RemoveNamedItem("indent");
			if (this.HasAttribute("pre")) this.Attributes.RemoveNamedItem("pre");
			if (this.HasAttribute("post")) this.Attributes.RemoveNamedItem("post");
			this._indentSize = 0;
		}
		#endregion

		#region Accessors
		public new int IndentSize => 0;
		public bool AddCR
		{
			get => String.Compare(this.GetAttributeValue("addcr", "false"), "true", true) == 0;
			protected set
			{
				if (value)
					this.SetAttribute("addcr", "true");
				else
					if (this.HasAttribute("addcr"))
					this.Attributes.RemoveNamedItem("addcr");
			}
		}
		public string Content
		{
			get => Regex.Replace(this.InnerXml, "[\\s]{2,}", " ", RegexOptions.Compiled).Trim();
			set => this.InnerXml = value;
		}

		public new XmlTextNode[] UsefulNodes
		{
			get
			{
				List<XmlTextNode> nodes = new List<XmlTextNode>();
				foreach (XmlNode n in this.ChildNodes)
					switch (n.NodeType)
					{
						case XmlNodeType.Element:
							if (XmlShim.CheckTag(n, XmlTextNode.TAG_NAME))
								nodes.Add(new XmlTextNode((XmlElement)n, this.Color));
							break;
						case XmlNodeType.Text:
							nodes.Add(new XmlTextNode((n.InnerXml.Length > 0 ? n.InnerXml : n.InnerText), this.AddCR, this.Color));
							break;
						case XmlNodeType.EntityReference:
							nodes.Add(new XmlTextEntity(n, this.Color));
							break;
					}

				return nodes.ToArray();
			}
		}
		#endregion

		#region Methods
		private string ReplaceLast(string source, string item, string with)
		{
			int index = source.LastIndexOf(item);
			if (index >= 0)
				return (index < source.Length) ? source.Substring(0, index) + with + source.Substring(index + 1) : source + with;

			return source;
		}

		private string Trim(string source)
		{
			source = source.TrimEnd();
			if (source.Trim() != source) // there's leading whitespace: replace it with just a single space.
				source = " " + source.TrimStart();
			return source;
		}

		public int RenderLine(string indent, ref string data, int curPos, XmlParameterCollection parameters)
		{
			string[] SplitByLastSpaceAt(string source, int splitPoint)
			{
				if (source.Length < splitPoint) return new string[] { source, source, "" };

				int index = Math.Max(source.LastIndexOf(' ', Math.Min(splitPoint, source.Length) - 1), 0);
				string[] result = new string[] { source, "", "" };

				if (index == 0) index = Math.Min(splitPoint, source.Length);
				result[1] = source.Substring(0, index);
				result[2] = (index >= source.Length) ? "" : source.Substring(index + 1);
				return result;
			}

			if (parameters.Count > 0)
				data = parameters.Parse(data);

			if (data.IndexOf("\n") < 0)
			{
				if (curPos + data.Length < Console.BufferWidth)
				{
					Console.Write(this.DecodeEntities(data), this.Color);
					curPos += data.Length;
					data = "";
				}
				else
				{
					while (data.Length > 0)
					{
						string[] parts = SplitByLastSpaceAt(data, Console.BufferWidth - curPos);
						Console.Write(this.DecodeEntities(parts[1]), this.Color);
						curPos += parts[1].Length;
						data = parts[2];
						if (data.Length > 0) { Console.Write("\r\n" + indent); curPos = indent.Length; }
					}
				}
				if (this.AddCR) { Console.Write("\r\n" + indent); curPos = indent.Length; }
			}
			else
			{
				string[] lines = data.Replace("\r", "").Split(new string[] { "\n" }, StringSplitOptions.None);
				foreach (string line in lines)
				{
					string l = Trim(line);
					while (l.Length > 0) curPos = this.RenderLine(indent, ref l, curPos, parameters);
					Console.Write("\r\n" + indent);
					curPos = indent.Length;
				}
			}
			return curPos;
		}

		public override int Render(int indentStart, int curPos = 0, XmlParameterCollection parameters = null)
		{
			if (parameters is null) parameters = new XmlParameterCollection();
			string indent = "".PadRight(indentStart, ' ');

			if ((this.ChildNodes.Count == 1) && (this.ChildNodes[0].NodeType == XmlNodeType.Text))
			{
				string data = this.Content;
				curPos = this.RenderLine(indent, ref data, curPos, parameters);
			}
			else
				foreach (XmlTextOutput o in this.UsefulNodes)
				{
					switch (o.SubType)
					{
						case XmlSubType.Paragraph:
							curPos = ((XmlParagraph)o).Render(indentStart, curPos, parameters);
							break;
						case XmlSubType.Text:
							curPos = ((XmlTextNode)o).Render(indentStart, curPos, parameters);
							break;
						case XmlSubType.Entity:
							curPos = ((XmlTextEntity)o).Render(indentStart, curPos);
							break;
					}
				}
			return curPos;
		}

		protected override XmlSubType MySubType() => XmlSubType.Text;

		public override string ToString() => base.ToString().Replace("XmlTextOutput", "XmlTextNode");
		#endregion

		#region Static Methods
		public static string[] SplitLines(string source, int byLength)
		{
			List<string> lines = new List<string>();
			if (source.IndexOf("\n") >= 0)
			{
				string[] parts = source.Split(new string[] { "\n" }, StringSplitOptions.None);
				for (int i = 0; i < parts.Length; i++)
					lines.AddRange(XmlTextNode.SplitLines(parts[i].Replace("\r", "").Trim(), byLength));
				return lines.ToArray();
			}

			while (source.Length > 0)
			{
				int size = ((source.Length > byLength) ? byLength : source.Length) - 1;
				int breakPoint = source.Length;
				if (size == byLength)
				{
					breakPoint = Math.Min(source.LastIndexOf(' ', size), byLength);
					if (breakPoint < 0) breakPoint = Math.Min(source.Length, byLength);
				}
				lines.Add(source.Substring(0, breakPoint));
				source = (source.Length > breakPoint) ? source.Substring(breakPoint) : "";
			}
			return lines.ToArray();
		}

		public static XmlTextNode Parse(string text, HelpColor color)
		{
			XmlShim.XmlDoc.LoadXml(text);
			if (XmlShim.XmlDoc.HasChildNodes)
				foreach (XmlElement e in XmlShim.XmlDoc.ChildNodes)
					if ((e.NodeType == XmlNodeType.Text) || XmlShim.CheckTag(e, XmlTextNode.TAG_NAME))
						return new XmlTextNode(e, color);

			return null;
		}

		public static XmlTextNode Parse(string text, string byId, HelpColor color)
		{
			XmlShim.XmlDoc.LoadXml(text);
			if (XmlShim.XmlDoc.HasChildNodes)
			{
				XmlElement e = XmlShim.XmlDoc.GetElementById(byId);
				if ((e != null) && XmlShim.CheckTag(e, XmlTextNode.TAG_NAME))
					return new XmlTextNode(e, color);
			}
			return null;
		}
		#endregion
	}

	internal class XmlTextEntity : XmlTextNode
	{
		#region Properties
		protected XmlEntityShim _entity;
		#endregion

		#region Constructors
		public XmlTextEntity(XmlNode source, HelpColor color) : base("", color)
		{
			this._entity = new XmlEntityShim(source);
			this.Color = Color;
		}

		public XmlTextEntity(string source, HelpColor color) : base("", color)
		{
			this._entity = new XmlEntityShim(source);
			this.Color = Color;
		}
		#endregion

		#region Accessors
		new public bool AddCR => false;
		new public string Content => this._entity.Entity;
		public string Entity => this._entity.Entity;
		public string Translation => this._entity.Translation;
		#endregion

		#region Methods
		/// <summary>Renders this object to the console.</summary>
		/// <param name="indentStart"></param>
		/// <param name="curPos"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		new public int Render(int indentStart = 0, int curPos = 0, XmlParameterCollection parameters = null)
		{
			Console.Write(this.Translation, this.Color);
			if (this.Translation.IndexOf("\r") >= 0)
			{   // If the translation includes a carriage return, we have to plot out the advancement of the
				// cursor manually by counting the number of printable characters remaining in the string after
				// the last CR..
				curPos = 0;
				int start = this.Translation.LastIndexOf("\r");
				while (++start < this.Translation.Length)
					switch ("\b\n\0\a\f\t\v".IndexOf(this.Translation[start]))
					{
						case -1: // Any character not represented above...
							curPos++;
							break;
						case 0: // The first character in the string is a backspace, so move the cursor accordingly..
							curPos -= 1;
							break;
					}
			}
			else
				curPos += this._entity.Translation.Length;

			return curPos;
		}

		protected override XmlSubType MySubType() => XmlSubType.Entity;
		#endregion
	}

	internal class XmlParagraph : XmlTextOutput
	{
		#region Attributes
		public const string TAG_NAME = "P";
		#endregion

		#region Constructors
		public XmlParagraph(XmlElement sourceNode, HelpColor color) : base(XmlParagraph.TAG_NAME, sourceNode, color)
		{
			if (this.Check(sourceNode))
			{
				this.InnerXml = sourceNode.InnerXml;
				this.NormalizeAttributes(sourceNode, "pre,post".Split(','));
				this.AttributeCheck(sourceNode, new string[] { "name", "Name", "NAME", "Title", "TITLE" }, "title");
			}
		}

		public XmlParagraph(HelpColor defaultColor) : base(TAG_NAME, defaultColor) { }
		#endregion

		#region Accessors
		public int LinesAbove
		{
			get { return this.GetAttributeValueAsInt("pre", 0, true); }
			set { this.SetAttribute("pre", value, 0, true); }
		}

		public int LinesBelow
		{
			get { return this.GetAttributeValueAsInt("post", 2, true); }
			set { this.SetAttribute("post", value, 2, true); }
		}

		public string Title
		{
			get { return this.GetAttributeValue("title"); }
			set { this.SetAttribute("title", value, "", true); }
		}

		public XmlTextNode[] TextNodes
		{
			get
			{
				List<XmlTextNode> results = new List<XmlTextNode>();
				foreach (XmlNode node in this.ChildNodes)
					switch (node.NodeType)
					{
						case XmlNodeType.EntityReference:
							results.Add(new XmlTextEntity(node, this.Color));
							break;
						case XmlNodeType.Text:
							results.Add(new XmlTextNode(node.InnerXml.Length > 0 ? node.InnerXml : node.InnerText, this.Color));
							break;
						case XmlNodeType.Element:
							if (XmlShim.CheckTag(node, XmlTextNode.TAG_NAME))
								results.Add(new XmlTextNode((XmlElement)node, this.Color));
							break;
					}

				return results.ToArray();
			}
		}
		#endregion

		#region Methods
		private string LineSpacing(int count)
		{
			if (--count < 1) return "";

			string result = "\r";
			for (int i = 0; i < count; i++) result += "\n";
			return result;
		}

		public override int Render(int indentStart = 0, int curPos = 0, XmlParameterCollection parameters = null)
		{
			if (parameters is null) parameters = new XmlParameterCollection();
			indentStart += this.IndentSize;
			string indent = "".PadRight(indentStart, ' '); // Make a string to hold our indent spacing
			if (this.LinesAbove > 0) { Console.Write("\r" + this.LineSpacing(this.LinesAbove)); curPos = 0; }
			if (curPos == 0) { Console.Write(indent); curPos = indent.Length; }

			foreach (XmlTextOutput o in this.UsefulNodes)
				switch (o.SubType)
				{
					case XmlSubType.Paragraph:
						curPos = ((XmlParagraph)o).Render(indentStart, curPos, parameters);
						break;
					case XmlSubType.Text:
						curPos = ((XmlTextNode)o).Render(indentStart, curPos, parameters);
						break;
					case XmlSubType.Entity:
						curPos = ((XmlTextEntity)o).Render(indentStart, curPos);
						break;
				}

			if (this.LinesBelow > 0) { Console.Write("\r" + this.LineSpacing(this.LinesBelow)); curPos = 0; }
			return curPos;
		}

		public override string ToString() => base.ToString().Replace("XmlTextOutput", "XmlParagraph");

		protected override XmlSubType MySubType() => XmlSubType.Paragraph;
		#endregion

		#region Static Methods
		public static XmlParagraph Parse(string text, HelpColor color)
		{
			XmlShim.XmlDoc.LoadXml(text);
			if (XmlShim.XmlDoc.HasChildNodes)
				foreach (XmlElement e in XmlShim.XmlDoc.GetElementsByTagName(TAG_NAME))
					if (XmlShim.CheckTag(e, TAG_NAME))
						return new XmlParagraph(e, color);

			return null;
		}

		public static XmlParagraph Parse(string text, string byId, HelpColor color)
		{
			XmlShim.XmlDoc.LoadXml(text);
			if (XmlShim.XmlDoc.HasChildNodes)
				foreach (XmlElement e in XmlShim.XmlDoc.GetElementsByTagName(XmlParagraph.TAG_NAME))
					if (XmlShim.CheckTag(e, XmlParagraph.TAG_NAME) && ((XmlParagraph)e).CompareAttribute("id", byId, true))
						return new XmlParagraph(e, color);

			return null;
		}
		#endregion
	}

	internal class XmlCmdNode : XmlShim
	{
		#region Properties
		public const string TAG_NAME = "cmd";
		protected XmlCmdSwitchSetNode _switches;
		protected XmlCmdIntroNode _introduction;
		protected XmlCmdDetailNode _detailedHelp;
		#endregion

		#region Constructors
		public XmlCmdNode(XmlElement sourceNode, HelpColor color) : base(TAG_NAME, sourceNode)
		{
			this.Color = color;
			XmlCmdNode generic = null; // ((Common.Help is null) || (Common.Help.GenericCmdNode is null) ? null : Common.Help.GenericCmdNode);
			if (this.Check(sourceNode))
			{
				this.SetAttribute("id", sourceNode.GetAttributeValue("id"));
				this.SetAttribute("keys", sourceNode.GetAttributeValue("keys"));

				if (sourceNode.HasAttribute("name") && !sourceNode.HasAttribute("id") && (sourceNode.GetAttributeValue("name").Length > 0))
					this.SetAttribute("id", sourceNode.GetAttributeValue("name"));

				if (this.GetAttributeValue("id").Length == 0)
					throw new ArgumentException("The source XmlElement does not have a valid Id attribute.");

				this.InnerXml = sourceNode.InnerXml;

				XmlNode node = sourceNode.GetFirstNamedElement("intro");
				if (node is null)
					this._introduction = (!(generic is null) && (generic.Introduction.ContentCount > 0)) ? generic.Introduction : new XmlCmdIntroNode(color);
				else
				{
					if (!(generic is null) && (generic.Introduction.ContentCount > 0))
						this._introduction = generic.Introduction.Add(new XmlCmdIntroNode((XmlElement)node, color));
					else
						this._introduction = new XmlCmdIntroNode((XmlElement)node, color);
				}

				node = sourceNode.GetFirstNamedElement("switchset");
				if (node is null)
					this._switches = (!(generic is null) && !(generic.Switches is null)) ? generic.Switches : new XmlCmdSwitchSetNode(color);
				else
				{
					if (!(generic is null) && (generic.Switches.SwitchCount > 0))
						this._switches = generic.Switches.Add(new XmlCmdSwitchSetNode((XmlElement)node, color));
					else
						this._switches = new XmlCmdSwitchSetNode((XmlElement)node, color);
				}

				node = sourceNode.GetFirstNamedElement("detailedHelp");
				if (node is null)
					this._detailedHelp = (!(generic is null) && !(generic.DetailedHelp is null)) ? generic.DetailedHelp : new XmlCmdDetailNode(color);
				else
				{
					if (!(generic is null) && (generic.DetailedHelp.ContentCount > 0))
						this._detailedHelp = new XmlCmdDetailNode((XmlElement)node, color).Add(generic.DetailedHelp);
					else
						this._detailedHelp = new XmlCmdDetailNode((XmlElement)node, color);
				}
			}
		}

		public XmlCmdNode(HelpColor color) : base(TAG_NAME) =>
			this.Color = color;
		#endregion

		#region Accessors
		public XmlCmdSwitchSetNode Switches => this._switches;

		public XmlCmdIntroNode Introduction => this._introduction;

		public XmlCmdDetailNode DetailedHelp => this._detailedHelp;
		#endregion

		#region Methods
		public int Render(int indentStart = 0, int curPos = 0, XmlParameterCollection parameters = null)
		{
			if (parameters is null) parameters = new XmlParameterCollection();

			if ((this._switches.SwitchCount > 0) || !(this._switches.Content is null))
				curPos = this._switches.Render(indentStart, curPos, parameters);

			if (!(this._detailedHelp is null))
				curPos = this._detailedHelp.Render(indentStart, curPos, parameters);

			return curPos;
		}

		protected bool Check(XmlElement n) => XmlShim.CheckTag(n, TAG_NAME);
		#endregion
	}

	internal class XmlHelpParser
	{
		#region Properties
		protected string _xmlSource;
		protected XmlDocument _xml;
		protected int _cursorPosition = 0;
		protected int _wrapWidth = 80;
		#endregion

		#region Constructors
		/// <summary>Initialises the object.</summary>
		/// <param name="identity">A string specifying the Namespace + Name of the HTML source document to load.</param>
		public XmlHelpParser(string identity, bool useInternal = true)
		{
			this._xml = new XmlDocument();
			if (useInternal)
				this._xmlSource = XmlHelpParser.FetchInternalResourceFile(identity);
			else
				this._xmlSource = XmlHelpParser.FetchExternalResourceFile(identity);

			this._xml.LoadXml(_xmlSource);
		}
		#endregion

		#region Accessors
		public dynamic this[string  what]
		{
			get
			{
				int i = FindCategoryIndex(what);
				if (i<0)
				{
					i = FindCmdIndex(what);
					if (i >= 0) return this.XmlCommands[i].Value;

					throw new ArgumentOutOfRangeException("The requested item (\"" + what + "\") doesn't exist in this library.");
				}

				return this.XmlCategories(i);
			}
		}

		protected XmlCategoryNode[] Categories
		{
			get
			{
				List<XmlCategoryNode> pile = new List<XmlCategoryNode>();
				XmlNodeList categoryGroup = this._xml.GetElementsByTagName("categories");
				if (categoryGroup.Count > 0)
					foreach (XmlNode categorySection in categoryGroup)
						foreach (XmlNode node in categorySection.ChildNodes)
							if (node.Name.Equals("category", StringComparison.InvariantCultureIgnoreCase) && node.HasAttribute("id"))
								pile.Add(new XmlCategoryNode((XmlElement)node, default));

				return pile.ToArray();
			}
		}

		protected XmlCmdNode[] Commands
		{
			get
			{
				List<XmlCmdNode> pile = new List<XmlCmdNode>();
				XmlNodeList commandGroups = this._xml.GetElementsByTagName("commands");
				if (commandGroups.Count > 0)
					foreach (XmlNode commandSection in commandGroups)
						foreach (XmlNode node in commandSection.ChildNodes)
							if (node.Name.Equals("cmd", StringComparison.InvariantCultureIgnoreCase) && node.HasAttribute("id"))
							{
								if (node.GetAttribute("id").IsEqualTo("!DEFAULT!"))
									this._genericCommandHelpNode = new XmlCmdNode((XmlElement)node, default);
								else
									pile.Add(new XmlCmdNode((XmlElement)node, default));
							}

				return pile.ToArray();
			}
		}

		public XmlDocument XmlCommands
		{
			get
			{
				XmlDocument doc = new XmlDocument(this._xml.NameTable);
				string work = NetXpertExtensions.XML_HEADER; // "<?xml version='1.0' encoding='UTF-8' standalone='yes' ?>";
				foreach (XmlShim.XmlEntityShim xE in this.ExportEntities())
					work += xE.ToString();

				work += "<commands>";
				foreach (XmlCmdNode cmd in this.Commands)
					work += cmd.OuterXml;
				work += "</commands>";

				doc.LoadXml(work);
				return doc;
			}
		}

		public XmlDocument XmlCategories
		{
			get
			{
				XmlDocument doc = new XmlDocument(this._xml.NameTable);
				string work = NetXpertExtensions.XML_HEADER; // "<?xml version='1.0' encoding='UTF-8' standalone='yes' ?>";
				foreach (XmlEntities xE in this._xml.ExportEntities())
					work += xE.ToString();

				work += "<categories>";
				foreach (XmlCategoryNode cat in this.Categories)
					work += cat.OuterXml;
				work += "</categories>";

				doc.LoadXml(work);
				return doc;
			}
		}
		#endregion

		#region Methods
		protected int FindCategoryIndex(string id)
		{
			XmlCategoryNode[] cats = this.Categories;
			int i = -1; while ((++i < cats.Length) && (cats[i].HasAttribute("id") && !cats[i].Attributes["id"].Value.Equals(id, StringComparison.InvariantCultureIgnoreCase))) ;
			return (i < cats.Length) ? i : -1;
		}

		protected int FindCmdIndex(string id)
		{
			XmlCmdNode[] cmds = this.Commands;
			int i = -1; while ((++i < cmds.Length) && (cmds[i].Id.Length > 0) && !cmds[i].Id.Equals(id, StringComparison.InvariantCultureIgnoreCase)) ;
			return (i < cmds.Length) ? i : -1;
		}

		public XmlCategoryNode GetCategoryNode(string id)
		{
			int i = FindCategoryIndex(id);
			return (i < 0) ? null : this.Categories[i];
		}

		public XmlCmdNode GetCmdNode(string id)
		{
			int i = FindCmdIndex(id);
			return (i < 0) ? null : this.Commands[i];
		}

		#region Methods
		public bool RenderCategory(string name) =>
			this.RenderCategory(name, 0, XmlParameterCollection.Empty);

		public bool RenderCategory(string name, XmlParameterCollection parameters) =>
			this.RenderCategory(name, 0, parameters);

		public bool RenderCategory(string name, int indent) =>
			this.RenderCategory(name, indent, XmlParameterCollection.Empty);

		public bool RenderCategory(string name, int indent, XmlParameterCollection parameters)
		{
			XmlCategoryNode xcn = GetCategoryNode(name); // XmlCategoryNode2.Parse(this._xml, id, Con.Style);
			if (xcn is null) return false;
			parameters["cmdName"] = name;
			if (parameters["cmdVersion"].Length < 6)
				parameters["cmdVersion"] = "0.0.0.0";

			this._cursorPosition = xcn.Render(indent, this._cursorPosition, parameters);
			Console.WriteLine();
			return true;
		}

		public XmlDocument FetchCategory(string name)
		{
			XmlCategoryNode cat = GetCategoryNode(name);
			if (cat == null) return null;

			XmlDocument doc = new XmlDocument(this._xml.NameTable);
			string work = MiscellaneousExtensions.XML_HEADER; // "<?xml version='1.0' encoding='UTF-8' standalone='yes' ?>";
			foreach (XmlEntities xE in this._xml.ExportEntities())
				work += xE.ToString();

			doc.LoadXml(work + cat.OuterXml);
			return doc;
		}

		public XmlDocument FetchCommand(string name)
		{
			XmlCmdNode cmd = GetCmdNode(name);
			if (cmd == null) return null;

			XmlDocument doc = new XmlDocument(this._xml.NameTable);
			string work = MiscellaneousExtensions.XML_HEADER; // "<?xml version='1.0' encoding='UTF-8' standalone='yes' ?>";
			foreach (XmlEntities xE in this._xml.ExportEntities())
				work += xE.ToString();

			doc.LoadXml(work + cmd.OuterXml);
			return doc;
		}
		#endregion

		#region Static Methods
		protected static string FetchInternalResourceFile(string name) => Common.FetchInternalResourceFile(name);

		protected static string FetchExternalResourceFile(string name)
		{
			if ((name is null) || (name.Length == 0) || (name == String.Empty))
				throw new ArgumentNullException("A valid filename must be provided for this function to operate.");

			if (!File.Exists(name))
				throw new ArgumentException("The file \"" + name + "\" could not be located.");

			System.Xml.Linq.XDocument doc = System.Xml.Linq.XDocument.Load(name);
			return doc.Declaration.ToString() + doc.ToString(System.Xml.Linq.SaveOptions.DisableFormatting);
		}

		public static Version Version() => new System.Version(1, 2, 2, 0);
		#endregion
		#endregion
	}
}
