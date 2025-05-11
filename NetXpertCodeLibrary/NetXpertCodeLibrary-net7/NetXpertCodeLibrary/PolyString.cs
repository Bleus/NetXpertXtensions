using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using NetXpertExtensions;

namespace NetXpertCodeLibrary
{
	/// <summary>Extends my Polymorphic Variable class (PolyVar) to add support for XmlDocument, XmlElement and DateTime classes.</summary>
	public class PolyString : PolyVar, IEnumerator<char>
	{
		#region Constructors
		public PolyString() : base() { }

		public PolyString(string value) : base(value) { }

		public PolyString(PolyVar value) : base(value.Value) { }

		public PolyString(DateTime value) : base() => this.AsDateTime = value;

		public PolyString(XmlDocument data) : base(data) { }

		public PolyString(XmlElement data) : base(data) { }

		public PolyString(PolyString data) : base(data.Value) { }

		public PolyString(object data) : base(data) { }
		#endregion

		#region Operators
		//public static PolyString operator +(PolyString left, string right) => new PolyString(left._value + right);
		//public static PolyString operator +(string left, PolyString right) => new PolyString(left + right._value);

		//public static PolyString operator +(PolyString left, PolyString right) => new PolyString(left._value + right._value);

		//public static bool operator ==(string left, PolyString right) { return (right == left); }
		//public static bool operator ==(PolyString left, string right)
		//{
		//	if (left is null) return (right is null) || (right.Length == 0);
		//	if (right is null) return false;
		//	return right.Equals(left._value, StringComparison.InvariantCultureIgnoreCase);
		//}

		//public static bool operator !=(PolyString left, string right) => !(left == right);
		//public static bool operator !=(string left, PolyString right) => !(right == left);

		//public static bool operator ==(PolyString left, PolyString right)
		//{
		//	if (left is null) return (right is null) || (right._value.Length == 0);
		//	if (right is null) return false;
		//	return left == right._value;
		//}

		//public static bool operator !=(PolyString left, PolyString right) => !(left == right);

		public static bool operator ==(PolyString left, PolyString right) => (PolyVar)left == (PolyVar)right;
		public static bool operator !=(PolyString left, PolyString right) => !(left == right);


		public static bool operator ==(XmlDocument left, PolyString right) => (right == left);
		public static bool operator ==(PolyString left, XmlDocument right)
		{
			if (left is null) return (right is null);
			if (right is null) return false;
			return String.Equals(left._value, right.OuterXml, StringComparison.InvariantCultureIgnoreCase);
		}

		public static bool operator !=(PolyString left, XmlDocument right) => !(left == right);
		public static bool operator !=(XmlDocument left, PolyString right) => !(right == left);


		public static bool operator ==(XmlElement left, PolyString right) => (right == left);
		public static bool operator ==(PolyString left, XmlElement right)
		{
			if (left is null) return (right is null);
			if (right is null) return false;
			return String.Equals(left._value, right.OuterXml, StringComparison.InvariantCultureIgnoreCase);
		}

		public static bool operator !=(PolyString left, XmlElement right) => !(left == right);
		public static bool operator !=(XmlElement left, PolyString right) => !(right == left);


		public static bool operator ==(DateTime left, PolyString right) => (right == left);
		public static bool operator ==(PolyString left, DateTime right)
		{
			if (left is null) return false;
			return (left.AsDateTime == right);
		}

		public static bool operator !=(PolyString left, DateTime right) => !(left == right);
		public static bool operator !=(DateTime left, PolyString right) => !(right == left);
		#endregion

		#region Accessors
		protected DateTime AsDateTime
		{
			get
			{
				string result = this._value;
				if (PolyString.IsDateTime(ref result))
					return DateTime.ParseExact(result, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

				this._error = "The internal value cannot be converted to a DateTime representation.";
				return new DateTime(2000, 01, 01, 0, 0, 0);
			}

			set => this._value = value.ToString("yyyy-MM-dd HH:mm:ss");
		}

		public XmlDocument AsXmlDocument
		{
			set => this._value = value.OuterXml;
			get
			{
				XmlDocument doc = new XmlDocument();
				this._error = "";
				try { doc.LoadXml(this._value); }
				catch (Exception e) { this._error = e.Message; }
				return doc;
			}
		}

		public XmlElement AsXmlElement
		{
			set => this._value = value.OuterXml;
			get
			{
				XmlDocument doc = new XmlDocument();
				this._error = "";
				XmlElement result = null;
				try
				{
					doc.LoadXml(AsXmlDocument.CreateXmlDeclaration("1.1", "UTF-8", "yes").ToString() + this._value);
					result = (XmlElement)doc.FirstChild;
				}
				catch (Exception e)
				{
					this._error = e.Message;
					result = doc.CreateElement("error");
					result.InnerText = e.Message;
				}
				return result;
			}
		}

		char IEnumerator<char>.Current => this._value[this._position];

		object IEnumerator.Current => this._value[this._position];
		#endregion

		#region Methods
		//protected dynamic ConvertNbr(Type t)
		//{
		//	string[] valid = new string[] { "int32","int16","uint32","uint16","int64","uint64","decimal","float","double","sbyte","int","short","long","byte","uint","ushort","ulong" };
		//	int i = -1; while ((++i < valid.Length) && (!String.Equals(t.Name, valid[i], StringComparison.InvariantCultureIgnoreCase))) ;
		//	if (i < valid.Length)
		//	{
		//		dynamic nbr = Activator.CreateInstance(t);
		//		nbr = 0;
		//		var parseMethod = t.GetMethod("Parse",new Type[] { typeof(string) });
		//		if (parseMethod == null) throw new Exception("Could not locate a static Parse method for this type!");
		//		try { nbr = parseMethod.Invoke(null, new object[] { this.FirstWord }); }
		//		catch (Exception e) { this._error = e.Message; nbr = 0; }
		//		return nbr;
		//	}
		//	throw new TypeInitializationException(t.Name, new Exception("The specified type must be a numeric base class."));
		//}

		public static implicit operator string(PolyString data) => data.Value;
		public static implicit operator PolyString(string data) => new PolyString(data);

		public static implicit operator DateTime(PolyString data) => data.AsDateTime;
		public static implicit operator PolyString(DateTime data) => new PolyString(data);

		public static implicit operator PolyString(XmlDocument data) => new PolyString(data);

		public static implicit operator PolyString(XmlElement data) => new PolyString(data);

		private PolyString[] ConvertArray(PolyVar[] source)
		{
			List<PolyString> data = new List<PolyString>();
			foreach (PolyVar p in source) data.Add(new PolyString(p.Value));
			return data.ToArray();
		}

		public PolyString[] Split(string separator) => ConvertArray(base.Split(separator));

		new public PolyString[] Split(string[] separator) => ConvertArray(base.Split(separator));

		public PolyString[] Split(string separator, int count) => ConvertArray(base.Split(new string[] { separator }, count));

		new public PolyString[] Split(string[] separator, int count) => ConvertArray(base.Split(separator, count));

		public PolyString[] Split(string separator, StringSplitOptions options) =>
			ConvertArray(base.Split(new string[] { separator }, options));

		new public PolyString[] Split(string[] separator, StringSplitOptions options) =>
			ConvertArray(base.Split(separator,options));

		public PolyString[] Split(string separator, int count, StringSplitOptions options) =>
			ConvertArray(base.Split(new string[] { separator }, count, options));

		new public PolyString[] Split(string[] separator, int count, StringSplitOptions options) =>
			ConvertArray(base.Split(separator, count, options));

		public int IndexOf(PolyString value) => this._value.IndexOf(value.Value);

		public int LastIndexOf(PolyString value) => this._value.LastIndexOf(value.Value);

		new public PolyString Substring(int start, int length) => new PolyString(base.Substring(start, length));

		new public PolyString Substring(int start) => new PolyString(base.Substring(start));

		new public PolyString PadLeft(int toWidth, char with) => new PolyString(base.PadLeft(toWidth, with));

		new public PolyString PadLeft(int toWidth) => new PolyString(base.PadLeft(toWidth));

		new public PolyString PadRight(int toWidth, char with) => new PolyString(base.PadRight(toWidth, with));

		new public PolyString PadRight(int toWidth) => new PolyString(base.PadRight(toWidth));

		new public PolyString Trim() => new PolyString(base.Trim());

		new public PolyString Trim(char[] trimChars) => new PolyString(base.Trim(trimChars));

		new public PolyString TrimStart() => new PolyString(base.TrimStart());

		new public PolyString TrimStart(char[] trimChars) => new PolyString(base.TrimStart(trimChars));

		new public PolyString TrimEnd() => new PolyString(base.TrimEnd());

		new public PolyString TrimEnd(char[] trimChars) => new PolyString(base.TrimEnd(trimChars));

		new public PolyString ToUpper() => new PolyString(base.ToUpper());

		new public PolyString ToLower() => new PolyString(base.ToLower());

		/// <summary>Removes all instances of a specified character from the string.</summary>
		/// <param name="value">A Char value to remove all instances of from this string.</param>
		/// <returns>The current string with all of the specified characters removed.</returns>
		new public PolyString Remove(char value) => new PolyString(base.Remove(value));

		/// <summary>Removes all instances of a specified string from the string.</summary>
		/// <param name="value">A string value to remove all instances of from this string.</param>
		/// <returns>The current string with all of the specified string removed.</returns>
		new public PolyString Remove(string value) => new PolyString(base.Remove(value));

		/// <summary>Removes all instances of each element in a specified character array from the string.</summary>
		/// <param name="value">An array of Char value to remove all instances of from this string.</param>
		/// <returns>The current string with all of the specified characters removed.</returns>
		new public PolyString Remove(char[] values) => new PolyString(base.Remove(values));

		/// <summary>Extends the string class to add a UCWords function.</summary>
		/// <returns>A string with the initial letter of all words in it capitalised with any existing capitalized letters left as found.</returns>
		new public PolyString UCWords() => new PolyString(base.UCWords());

		/// <summary>Extends the string class to add a UCWords function.</summary>
		/// <param name="strict">If set to true, all letters in the string are converted to lowercase, then the words are capitalised.</param>
		/// <returns>A string with all individual words in it capitalised.</returns>
		new public PolyString UCWords(bool strict) => new PolyString(base.UCWords(strict));

		/// <summary>Given a string of valid characters, filters all non-matching characters out of a string.</summary>
		/// <param name="validChars">A string of valid (permitted) characters to retain.</param>
		/// <param name="ignoreCase">Specifies whether case should be ignored.</param>
		/// <returns>A string containing only the permitted characters.</returns>
		new public PolyString Filter(string validChars, bool ignoreCase) => new PolyString(base.Filter(validChars, ignoreCase));

		/// <summary>Given a string of valid characters, filters all non-matching (case-insensitive) characters out of a string.</summary>
		/// <param name="validChars">A string of valid (permitted) characters to retain.</param>
		/// <returns>A string containing only the permitted characters.</returns>
		new public PolyString Filter(string validChars) => new PolyString(base.Filter(validChars, true));

		/// <summary>Given an array valid characters, filters all non-matching (case-insensitive) characters out of a string.</summary>
		/// <param name="validChars">An array of valid (permitted) characters to retain.</param>
		/// <returns>A string containing only the permitted characters.</returns>
		new public PolyString Filter(char[] validChars) => new PolyString(base.Filter(new string(validChars), true));

		/// <summary>Given an array valid characters, filters all non-matching characters out of a string.</summary>
		/// <param name="validChars">An array of valid (permitted) characters to retain.</param>
		/// <param name="ignoreCase">Specifies whether case should be ignored.</param>
		/// <returns>A string containing only the permitted characters.</returns>
		new public PolyString Filter(char[] validChars, bool ignoreCase) => new PolyString(base.Filter(new string(validChars), ignoreCase));

		public override bool IsEqualTo(string value) => this._value.Equals(value, StringComparison.InvariantCultureIgnoreCase);

		public override bool IsEqualTo(PolyVar value) => this._value.Equals(value.Value, StringComparison.InvariantCultureIgnoreCase);

		public bool IsEqualTo(PolyString value) => this._value.Equals(value.Value, StringComparison.InvariantCultureIgnoreCase);

		public bool Matches(string regexPattern) =>
			this._value.Match(regexPattern, RegexOptions.None);

		public bool Matches(string regexPattern, RegexOptions options) =>
			this._value.Match(regexPattern, options);

		public override string ToString() => this._value;

		public override bool Equals(object obj) => this._value.Equals(obj);

		public override int GetHashCode() => this._value.GetHashCode();

		new public static PolyString Join(string glue, string[] parts) => (PolyVar.Join(glue, parts) as PolyString);

		public static PolyString Join(string glue, PolyString[] parts) => (PolyVar.Join(glue, parts) as PolyString);

		bool IEnumerator.MoveNext() => (++this._position) < this._value.Length;

		void IEnumerator.Reset() => this._position = 0;

		public static bool IsDateTime(string source) => PolyString.IsDateTime(ref source);

		public static bool IsDateTime(ref string source)
		{
			source = source.Trim().Filter("0123456789-: apmh"); // Remove leading/trailing whitespace and all invalid characters.

			DateTime result = new();

			string timeBase = /* language=regex */ @":[0-5]\d(:[0-5]\d([.]\d{1,3})?)?";
			string mySqlBase = /* language=regex */ @"^\d{4}[-/](1[0-2]|0?[1-9])[-/](0?[1-9]|[12]\d|3[01])";
			RegexOptions options = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase;

			Regex genericDate = new( @"^(\d{4}|\d{2})[-/](1[0-2]|0?[1-9])[-/](0?[1-9]|[12]\d|3[01])$");
			//Regex nearYears = new Regex( @"^((19|2\d|30)\d\d|\d{2})[-/](1[0-2]|0?[1-9])[-/](0?[1-9]|[12]\d|3[01])$");
			//Regex minutes = new Regex(timeBase, options);
			Regex genericTime = new( $"^(2[0-3]|[01]?\\d){timeBase}\\s*(h|[ap]m?)?$",options);
			Regex short24hr = new( @"^([01]\d|2[0-3])[0-5]\dh?$",options);
			Regex time12hr = new( $"^(1[012]|0?[1-9]){timeBase}\\s*([ap]m?)?$",options);
			Regex zeroTime = new( @"^0+(:0{1,2}(:0{1,2}([.]\d{1,3})?)?)?\s*([ap]m?|h)?$",options);
			//Regex monthNames = new Regex( @"^\s+(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)",options);
			Regex mySqlDateTime = new( $"{mySqlBase}\\s+(2[0-3]|[01]?\\d){timeBase}$",options);
			Regex mySqlDate = new(mySqlBase, options);

			if (source.Length > 5)
			{
				try
				{
					if (!mySqlDate.IsMatch(source) && !mySqlDateTime.IsMatch(source)) return false;

					string[] parse = Regex.Split(source, @"\s", options);
					string strDate = Regex.Replace(parse[0], @"\s+", "", options);
					string strTime = "";
					if (parse.Length > 1)
					{
						strTime = Regex.Replace(parse[1], @"\s+", "", options);
						if (zeroTime.IsMatch(strTime)) { strTime = ""; }
						else
						{
							if (short24hr.IsMatch(strTime))
								strTime = strTime.Substring(0, 2) + ":" + strTime.Substring(2);
							else if (!genericTime.IsMatch(strTime)) return false;
						}
					}

					if (!genericDate.IsMatch(strDate)) return false;

					parse = strDate.Replace("/", "-").Split(new char[] { '-' });
					if (parse.Length < 3) return false;

					int year = int.Parse(parse[0]), month = int.Parse(parse[1]), day = int.Parse(parse[2]);
					if ((month < 1) || (month > 12) || (day < 1) || (day > 31)) return false;

					switch (month)
					{
						case 2:
							if ((day > 29) || ((day > 28) && ((year % 4) > 0))) return false;
							if ((day == 29) && ((year % 100) == 0) && ((year % 400) > 0)) return false;
							break;
						case 4:
						case 6:
						case 9:
						case 11:
							if (day > 30) return false;
							break;
						default:
							if (day > 31) return false;
							break;
					}

					result = new DateTime(year, month, day, 0, 0, 0);

					if (strTime.Length > 0)
					{
						int hours = 0, mins = 0, secs = 0; //, msecs = 0;
						parse = Regex.Replace(strTime, @"[^\d:.]", "", options).Split(new char[] { ':' });

						if (parse.Length > 1)
						{
							hours = int.Parse(parse[0]);
							mins = int.Parse(parse[1]);
							if ((hours < 0) || (hours > 23) || (mins < 0) || (mins > 59)) return false;
							if (Regex.IsMatch(strTime, "[p]", options) && time12hr.IsMatch(strTime)) hours += 12;
							if (parse.Length > 2)
							{
								if (parse[2].IndexOf('.') > 0)
								{
									string[] parse2 = parse[2].Split(new char[] { '.' }, 2);
									secs = int.Parse(parse2[0]);
									//msecs = int.Parse(parse2[1]);
								}
								else
									secs = int.Parse(parse[2]);
							}

							if ((secs < 0) || (secs > 59)) return false; //  || (msecs < 0) || (msecs > 999)

							source = new DateTime(year, month, day, hours, mins, secs).ToString("yyyy-MM-dd HH:mm:ss");
							return true;
						}
					}

					source = result.ToString("yyyy-MM-dd HH:mm:ss");
					return true;
				}
				catch { }
			}
			return false;
		}
		#endregion
	}
}
