using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NetXpertExtensions.Classes;

namespace Cobblestone.Classes
{
	// {{[table|record id]:field1=value1;field2=value2;field3=value3;field4=value4;...}}

	internal class TextEncodedField
	{
		#region Properties
		public const string REGEX = /* language=regex */ @"[a-zA-Z0-9]+=`([\s\S]*?)`;";
		public const RegexOptions REGEX_OPTIONS = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase;
		protected string _data = "";
		protected PolyVar _name = "";
		#endregion

		#region Constructors
		public TextEncodedField(string name)
		{
			if (name.Length == 0)
				throw new ArgumentNullException("The 'name' of a field must be defined!");

			this._name = name;
		}

		public TextEncodedField(string name, string data)
		{
			if (name.Length == 0)
				throw new ArgumentNullException("The 'name' of a field must be defined!");

			this._name = name;
			this._data = data;
		}
		#endregion

		#region Accessors
		public string Name => this._name;
		public string Data
		{
			get => this._data;
			set => this._data = value;
		}
		#endregion

		#region Methods
		public override string ToString() => this._name + "=\"" + this._data + "\";";

		public string ToXmlString(int padLeft) =>
			("<field name='" + this._name + "'>" + this._data + "</field>").PadLeft(padLeft);

		public string ToXmlString() => ToXmlString(0);

		public HttpsGetVar ToCmd() => ToCmd("");

		public HttpsGetVar ToCmd(string prefix) =>
			new HttpsGetVar( prefix + this.Name, this.Data );

		public static bool Validate(string source) =>
			Regex.IsMatch(source.Trim(), REGEX, REGEX_OPTIONS);

		public static TextEncodedField Parse(string source)
		{
			if (Validate(source))
			{
				Regex parse = new Regex("(" + REGEX.Replace("+=`", "+)=`"), REGEX_OPTIONS);
				MatchCollection matches = parse.Matches(source.Trim());
				if ((matches.Count > 0) && (matches[0].Groups.Count>1))
					return new TextEncodedField(matches[0].Groups[1].Value,matches[0].Groups[2].Value);
			}
			return null;
		}
		#endregion
	}

	internal class TextEncodedFieldCollection : IEnumerator<TextEncodedField>
	{
		#region Properties
		public const string REGEX = "(" + TextEncodedField.REGEX + ")";

		protected List<TextEncodedField> _data = new List<TextEncodedField>();
		protected int _position = 0;
		#endregion

		#region Constructors
		public TextEncodedFieldCollection() { }

		public TextEncodedFieldCollection(TextEncodedField data) => this.Add(data);

		public TextEncodedFieldCollection(TextEncodedField[] data) => this.AddRange(data);

		public TextEncodedFieldCollection(TextEncodedFieldCollection data) => this.AddRange(data);
		#endregion

		#region Accessors
		public int Count => this._data.Count;

		public TextEncodedField this[int index]
		{
			get => ((index < this.Count) && (index >= 0)) ? this._data[index] : null;
			set
			{
				if ((index >= 0) && (index < this.Count))
					this._data[index] = value;
				else
					this._data.Add(value);
			}
		}

		public PolyVar this[string field]
		{
			get { int i = this.FindIndexOf(field); return (i < 0) ? null : this[i].Data; }
			set { int i = this.FindIndexOf(field); if (i >= 0) { this[i].Data = value; } }
		}

		TextEncodedField IEnumerator<TextEncodedField>.Current => this[this._position];

		object IEnumerator.Current => this[this._position];
		#endregion

		#region Methods
		/// <summary>Retgurns the index, within the currently managed list for a field with a name matching the one specified.</summary>
		/// <param name="field">A string specifying the field to look for.</param>
		/// <returns>The index (position) within the list of the specified field, if it is found, otherwise -1.</returns>
		protected int FindIndexOf(string field)
		{
			int i = -1; while ((++i < this.Count) && !this._data[i].Name.Equals(field, StringComparison.InvariantCultureIgnoreCase)) ;
			return (i < this.Count) ? i : -1;
		}

		/// <summary>Checks the currently managed list of fields and reports on whether one exists with the specified name.</summary>
		/// <param name="field">A string specifying the field to look for.</param>
		/// <returns>TRUE if a field exists in the list with the specified name, otherwise FALSE.</returns>
		public bool HasField(string field) => ((field.Length>0) && (this.FindIndexOf(field) >= 0));

		/// <summary>Takes an array of strings and compares it to the currently stored list of fields.</summary>
		/// <param name="fields">An array of strings containing the names of the fields to check for.</param>
		/// <returns>TRUE if all of the fields specified in the supplied array are found, otherwise FALSE.</returns>
		public bool HasFields(string[] fields)
		{
			int i = -1;
			while ((++i < fields.Length) && this.HasField(fields[i])) ;
			return (i == fields.Length);
		}

		/// <summary>
		/// Takes in a CmdLineData object and either adds it to the existing list, if one doesn't already exist
		/// with the same name. If a record already exists by that name, then that record is updated with the value
		/// of the provided object instead.
		/// </summary>
		/// <param name="data">A CmdLineData object to add / update in the current library.</param>
		public void Add(TextEncodedField data)
		{
			if (!(data is null))
			{
				int i = this.FindIndexOf(data.Name);
				if (i < 0) this._data.Add(data); else this._data[i] = data;
			}
		}

		/// <summary>
		/// Takes in a field name and value either adds them to the existing list (if a record doesn't already exist
		/// with the same name) or updates the existing record if one already exists by that name.
		/// </summary>
		/// <param name="name">The name of the field to be added/updated.</param>
		/// <param name="data">The date to be stored in the field.</param>
		public void Add(string name, string data) => this.Add(new TextEncodedField(name, data));

		/// <summary>Adds/Updates a range of CmdLineData objects for the managed list.</summary>
		/// <param name="data">An array of CmdLineData objects to add/update within the list.</param>
		public void AddRange(TextEncodedField[] data)
		{
			foreach (TextEncodedField item in data)
				this.Add(item);
		}

		/// <summary>Merges the contents of another CmdLineDataCollection with this one.</summary>
		/// <param name="collection">The CmdLineDataCollection object to be merged.</param>
		public void AddRange(TextEncodedFieldCollection collection)
		{
			foreach (TextEncodedField data in collection)
				this.Add(data);
		}

		/// <summary>Removes a field with the specified name from the list and reports the results.</summary>
		/// <param name="field">A string specifying the field to remove.</param>
		/// <returns>TRUE if the specified field existed and was removed, otherwise FALSE.</returns>
		public bool Remove(string field)
		{
			int i = this.FindIndexOf(field);
			if (i >= 0)
				this._data.RemoveAt(i);

			return (i >= 0);
		}

		/// <summary>Removes a field from the list and reports the results.</summary>
		/// <param name="field">A CmdLineData object specifying the field to be removed.</param>
		/// <returns>TRUE if the specified field existed and was removed, otherwise FALSE.</returns>
		public bool Remove(TextEncodedField data) => this.Remove(data.Name);

		/// <summary>Removes a collection of fields from the list and reports the results.</summary>
		/// <param name="fields">An array of strings specifying the fields to be removed.</param>
		/// <returns>An integer specifying the number of fields that were found and removed.</returns>
		public int RemoveRange(string[] fields)
		{
			int result = 0;
			foreach (string field in fields)
				result += this.Remove(field) ? 1 : 0;
			return result;
		}

		/// <summary>Removes a collection of fields from the list and reports the results.</summary>
		/// <param name="data">An array of CmdLineData objects representing the fields to be removed.</param>
		/// <returns>An integer specifying the number of fields that were found and removed.</returns>
		public int RemoveRange(TextEncodedField[] data)
		{
			int result = 0;
			foreach (TextEncodedField item in data)
				result += this.Remove(item.Name) ? 1 : 0;
			return result;
		}

		/// <summary>Removes a collection of fields from the list and reports the results.</summary>
		/// <param name="data">A CmdLineDataCollection object specifying the fields to be removed.</param>
		/// <returns>An integer specifying the number of fields that were found and removed.</returns>
		public int RemoveRange(TextEncodedFieldCollection data)
		{
			int result = 0;
			foreach (TextEncodedField item in data)
				result += this.Remove(item.Name) ? 1 : 0;
			return result;
		}

		/// <summary> Dumps the contents of the collection as an array of CmdLineData objects. </summary>
		/// <returns>An array containing the CmdLindData objects currently being managed in the collection.</returns>
		public TextEncodedField[] ToArray() => this._data.ToArray();

		#region IEnumerator support:
		public IEnumerator<TextEncodedField> GetEnumerator() => this._data.GetEnumerator();

		bool IEnumerator.MoveNext() => (++this._position) < this.Count;

		void IEnumerator.Reset() => this._position = 0;
		#endregion

		public override string ToString()
		{
			string result = "";
			foreach (TextEncodedField item in this._data)
				result += item.ToString();
			return result;
		}

		public string ToXmlString(int padLeft)
		{
			string result = "<specification>\r\n".PadLeft(padLeft);
			foreach (TextEncodedField item in this._data)
				result += item.ToXmlString(padLeft+3) + "\r\n";
			result += "</specification>".PadLeft(padLeft);
			return result;
		}

		public string ToXmlString() => ToXmlString(0);

		public HttpsGetVar[] ToCmdRequest() =>
			ToCmdRequest("");

		/// <summary>Assembles a collection for use when generating HtmlCommand objects based on this collection.</summary>
		/// <returns>An array of string arrays configured for generating HtmlCommand objects from this collection.</returns>
		public HttpsGetVar[] ToCmdRequest(string prefix)
		{
			List<HttpsGetVar> items = new List<HttpsGetVar>();
			if (this.Count > 0)
				foreach (TextEncodedField item in this._data)
					items.Add(item.ToCmd(prefix));

			return items.ToArray();
		}

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

		/// <summary>
		/// Uses the pre-defined RegEx definition to report on whether a provided string conforms to the required format.
		/// </summary>
		/// <param name="source">A string to validate.</param>
		/// <returns>TRUE is the provided string complies with the required format for being parsed by this object.</returns>
		public static bool Validate(string source) =>
			Regex.IsMatch(source.Trim(), REGEX, TextEncodedField.REGEX_OPTIONS);

		/// <summary>Parses a supplied string to populate an instance of this object.</summary>
		/// <param name="source">A string to parse.</param>
		/// <returns>If successful, a CmdLineDataCollection object populated from the supplied string, otherwise null.</returns>
		public static TextEncodedFieldCollection Parse(string source)
		{
			if (Validate(source))
			{
				TextEncodedFieldCollection result = new TextEncodedFieldCollection();
				Regex parse = new Regex(REGEX,TextEncodedField.REGEX_OPTIONS);
				MatchCollection matches = parse.Matches(source.Trim());
				if (matches.Count > 0)
					foreach (Match m in matches)
						result.Add(TextEncodedField.Parse(m.Groups[0].Value));
				return result;
			}
			return null;
		}
		#endregion
	}

	internal class TextEncodedRecord
	{
		#region Properties
		public const string REGEX = @"^{{\[([a-z0-9]+)(\|([+-]?[0-9]{1,8}))?\]:(" + TextEncodedFieldCollection.REGEX + @")}}$";
		public const string REGEX_VALUES = @"^{{" + TextEncodedFieldCollection.REGEX + @"}}$";

		protected TextEncodedFieldCollection _data = new TextEncodedFieldCollection();
		protected string _tableName = "";
		protected ulong _recordId = ulong.MaxValue;
		#endregion

		#region Constructors
		public TextEncodedRecord() { }

		public TextEncodedRecord(string tableName, ulong recordId, TextEncodedFieldCollection data)
		{
			if (tableName.Length == 0)
				throw new ArgumentNullException("The 'tableName' must be defined!");

			this._tableName = tableName;
			this.iRecordId = recordId;
			this._data = data;
		}
		#endregion

		#region Accessors
		public string Table
		{
			get => this._tableName;
			protected set => this._tableName = value;
		}

		public PolyString RecordId
		{
			get => new PolyString( ((this._recordId == ulong.MaxValue) ? "-1" : this._recordId.ToString()) );
			protected set => this._recordId = (value.Value =="-1") ? ulong.MaxValue : (ulong)value;
		}

		protected ulong iRecordId
		{
			get => this._recordId;
			set => this._recordId = Math.Min(ulong.MaxValue, value); // Only allow -1 from the negative numbers...
		}

		public TextEncodedFieldCollection Data
		{
			get => this._data;
			set => this._data = value;
		}

		public int Count => this._data.Count;
		#endregion

		#region Methods
		public override string ToString() =>
			"{{[" + this._tableName + "|" + this.RecordId + "]:" + this._data.ToString() + "}}";

		public string ToXmlString()
		{
			string result = Settings.Common.XML_HEADER; // "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\r\n";
			result += "<UpdateTable>\r\n";
			result += ("<table>" + this._tableName + "</table>\r\n").PadLeft(3);
			result += ("<recordId>" + this.RecordId + "</recordId>\r\n").PadLeft(3);
			result += this._data.ToXmlString(3);
			result += "</UpdateTable>\r\n";
			return result;
		}

		public HttpsRequest ToCmdRequest(string cmd)
		{
			List<HttpsGetVar> items = new List<HttpsGetVar>();
			if (this.Count > 0)
			{
				items.Add(new HttpsGetVar("cmd", cmd));
				items.Add(new HttpsGetVar("table", this.Table));
				items.Add(new HttpsGetVar("recId", RecordId ));
				items.AddRange(this._data.ToCmdRequest("field_"));
			}
			return new HttpsRequest(items.ToArray());
		}

		public static bool Validate(string source) =>
			Regex.IsMatch(source.Trim(), REGEX, TextEncodedField.REGEX_OPTIONS);

		public static TextEncodedRecord Parse(string command, string source)
		{
			TextEncodedRecord result = new TextEncodedRecord();

			if (Validate(source))
			{
				Regex parse = new Regex(REGEX,TextEncodedField.REGEX_OPTIONS);
				MatchCollection matches = parse.Matches(source.Trim());
				if (matches.Count > 0)
				{
					result.Table = matches[0].Groups[1].Value;
					result.RecordId = matches[0].Groups[3].Value;
					result.Data = TextEncodedFieldCollection.Parse(matches[0].Groups[4].Value);
				}
			}
			return result;
		}

		public static bool ValuesValidate(string source) =>
			Regex.IsMatch(source.Trim(), REGEX_VALUES, TextEncodedField.REGEX_OPTIONS);

		public static TextEncodedRecord ValuesParse(string tableName, ulong recordId, string source)
		{
			if (recordId < 0)
				throw new ArgumentOutOfRangeException("The specified record cannot be a negative value.");

			TextEncodedRecord result = new TextEncodedRecord(tableName, recordId, new TextEncodedFieldCollection());
			if (ValuesValidate(source))
			{
				source = source.Trim();
				result.Data = TextEncodedFieldCollection.Parse(source.Trim(new char[] { '{', '}' }));
			}
			return result;
		}

		public static string CreateTemplateString<T>(uint recordId) where T : RDI_Data
		{
			var obj = Activator.CreateInstance<T>();
			string[] fieldList = obj.RequiredFields();
			string result = "{{" + ((recordId<1) ? "":"[" + obj.GetType().Name + "|" + recordId.ToString() + "]:");
			foreach (string field in fieldList)
				result += field + "=`value`;";
			result += "}}";
			return result;
		}
		#endregion
	}
}
