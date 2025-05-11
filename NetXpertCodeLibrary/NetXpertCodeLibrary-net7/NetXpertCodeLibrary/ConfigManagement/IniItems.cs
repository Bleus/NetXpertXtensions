using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace NetXpertCodeLibrary.ConfigManagement
{
	public static class IniUtility
	{
		public const string LINE_KEY_TEMPLATE = /* language=regex */ @"(?<key>[a-z][a-z_\d]{0,63}(\<[a-zA-Z0-9]\>)?\??)";
		public const string LINE_DATA_TEMPLATE = /* language=regex */ @"^(?<value>[ \t]*[\S ]*?(?=##|[\r\n]))";
		public const string LINE_COMMENT_TEMPLATE = /* language=regex */ @"(?<comment>[ \t]*##[^\r\n]*)";
		public const string LINE_DETAIL_LINE =
			/* language=regex */ @"[\t ]*" + LINE_KEY_TEMPLATE +
			/* language=regex */ @"(?:[ \t]*=)(?<data>[\t\S ]*)?";

		//public const string LINE_DETAIL_LINE = @"[\t ]*" + KEY_TEMPLATE + @"(?:[\s]*[=][\s]*)((?<value>[\S ]+?)[\s]*(?<comment>##([\S ]*)))?";

		public const string GROUP_COMMENT_PATTERN = /* language=regex */ @"(^[ \t]*##[ \t]*(?<comment>[\S \t]*))*[\s]+";
		public const string GROUP_BASENAME_PATTERN = /* language=regex */ @"(?<name>[a-z0-9$][a-z0-9-_]{2,63}(?:[\\][a-z0-9$][a-z0-9-_]{2,63})*)";
		public const string GROUP_HIVE_PATTERN = /* language=regex */ @"(?<hive>HK(?:LM|[DP]D|C[CUR]|US(ERS|ER|R)?))[:]?";
		public const string GROUP_NAME_PATTERN = @"(?:^\[(?:" + GROUP_HIVE_PATTERN + @")?" + GROUP_BASENAME_PATTERN + @"\])";
		public const string GROUP_GROUP_PATTERN = GROUP_COMMENT_PATTERN + GROUP_NAME_PATTERN + /* language=regex */ @"(?<items>[\S\s]*?)(?=[\r\n]{3,})";
		// (^[ \t]*##[ \t]*(?<comment>[\S \t]*))*[\s]+(?:^\[(?:(?<hive>HK(?:LM|[DP]D|C[CUR]|U(SERS|SER|SR|S)))[:]?)?(?<name>[a-z0-9$][a-z0-9-_]{2,63})\])(?<items>[\S\s]*?)(?=[\r\n]{3,})
	}

	/// <summary>Manages individual line items from plaintext INI configuration files.</summary>
	public class IniLineItem
	{
		#region Properties
		protected string _key;
		protected string _value;
		protected string _comment;
		protected bool _encrypt;
		protected bool _enabled;
		#endregion

		#region Constructors
		public IniLineItem( string key, object value, bool encrypt = false, string comment = "", bool enabled = true ) =>
			Initialise( key, value.ToString(), encrypt, comment, enabled );

		public IniLineItem( string key, string value = "", bool encrypt = false, string comment = "", bool enabled = true ) =>
			Initialise( key, value, encrypt, comment, enabled );

		public IniLineItem( IniLineItem source ) =>
			Initialise( source._key, source._value, source._encrypt, source._comment, true );

		public IniLineItem( RegistryKey source, string valueName = "" )
		{
			this._enabled = true;
			this._value = (string.IsNullOrEmpty(valueName) ? source.GetValue("") : source.GetValue(valueName)).ToString();
			this.Key = string.IsNullOrEmpty(valueName) ? "(default)" : valueName;
			this._comment = source.Name;
		}

		/// <summary>Allows local routines (and descendants) to instantiate the object with no data.</summary>
		protected IniLineItem() { }

		protected void Initialise( string key, string value = "", bool encrypt = false, string comment = "", bool enabled = true )
		{
			if ( !IsValidKey( key ) )
				throw new ArgumentException( $"\"{key}\" is not a valid value for a configuration key." );

			this._value = value.ToString();
			this._enabled = enabled;
			this.Key = key;
			if ( !Encrypted && encrypt )
				this.Encrypted = encrypt;

			this.Comment = comment;
		}
		#endregion

		#region Operators
		public static bool operator !=(IniLineItem left, IniLineItem right) => !(left == right);
		public static bool operator ==(IniLineItem left, IniLineItem right)
		{
			if (left is null) return (right is null);
			if (right is null) return false;
			return (left.Key.Equals(right.Key, StringComparison.InvariantCultureIgnoreCase)) && (left.Value == right.Value);
		}

		public static bool operator !=(IniLineItem left, string right) => !(left == right);
		public static bool operator ==(IniLineItem left, string right)
		{
			if (left is null) return string.IsNullOrEmpty(right);
			if (string.IsNullOrEmpty(right)) return false;
			return left.Value == right;
		}

		public static IniLineItem operator +(IniLineItem left, IniLineItem right) =>
			new IniLineItem(left.Key, left.Value + ' ' + right.Value, left.Encrypted, "Combined keys (" + left.Key + ", " + right.Key + ").", left.Enabled);

		public static IniLineItem operator +(IniLineItem left, string right) =>
			new IniLineItem(left.Key, left.Value + ' ' + right, left.Encrypted, left.Comment, left.Enabled);

		public static string operator +(string left, IniLineItem right) => left + ' ' + right.Value;
		#endregion

		#region Accessors
		/// <summary>Gets / Sets the specified Key for this object. Automatically parses the value for encryption settings.</summary>
		public string Key
		{
			get => this._key;
			protected set
			{
				if (Enabled)
				{
					string key = CertifyKey(value);
					if (Regex.IsMatch(key, IniUtility.LINE_KEY_TEMPLATE,RegexOptions.IgnoreCase)) // if (key.Match(KEY_TEMPLATE))
					{
						if (key.EndsWith("?")) { this._encrypt = true; key = key.Substring(0, key.Length - 1); }
						this._key = key;
					}
				}
			}
		}

		/// <summary>Gets/Sets the Encryption status of this object and modifies the stored Value appropriately.</summary>
		public bool Encrypted
		{
			get => this._encrypt;
			set
			{
				if (Enabled && (value != this._encrypt))
				{
					this._value = value ? AES.EncryptStringToString(this._value) : AES.DecryptStringToString(this._value);
					this._encrypt = value;
				}
			}
		}

		/// <summary>Allows toggling of the setting to be active or not.</summary>
		public bool Enabled
		{
			get => this._enabled;
			set => this._enabled = value;
		}

		/// <summary>Gets / Sets the comment string for this object.</summary>
		public string Comment
		{
			get => IsValidComment(this._comment) ? this._comment : "";
			set { if (Enabled && IsValidComment(value)) { this._comment = value; } }
		}

		/// <summary>Gets/Sets the plaintext Value of this object, managing encryption automatically as required.</summary>
		public string Value
		{
			get => this._encrypt ? AES.DecryptStringToString(this._value) : this._value;
			set { if (Enabled) { this._value = (this._encrypt ? AES.EncryptStringToString(value) : value); } }
		}

		public bool AsBool => Regex.IsMatch(this._value, @"^[\s]*(yes|y|on|1|true|t)[\s]*$", RegexOptions.IgnoreCase);

		/// <summary>Passes the value of this object through Base64 encoding.
		/// GET: decode a Base64-encoded Value to its plaintext equivalent;
		/// SET: encode the supplied plaintext string into the Value using Base64 encoding.
		/// </summary>
		public string Base64Value
		{
			get => Base64Decode(this.Value);
			set { if (Enabled) { this.Value = Base64Encode(value); } }
		}
		#endregion

		#region Methods
		/// <summary>Parses a supplied string and attempts to populate this object from its data.</summary>
		/// <param name="data">A plaintext string to try parsing.</param>
		public void Import(string data)
		{
			data = data.Trim();
			if (IsValidDetail(data))
			{
				MatchCollection matches = new Regex(IniUtility.LINE_DETAIL_LINE, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture).Matches(data);
				string key = matches[0].Groups["key"].Value.Trim(),
						value = matches[0].Groups["data"].Value.Trim();

				// Break comments out from the data if they're there...
				MatchCollection parse = new Regex( IniUtility.LINE_COMMENT_TEMPLATE, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture).Matches(value);
				if (parse.Count > 0)
				{
					this._comment = parse[0].Groups["comment"].Value.TrimEnd();
					value = value.Replace(this._comment, "").Trim();
				}

				if (!IsValidKey(key))
					throw new ArgumentException("\"" + key + "\" is not a valid value for a configuration key.");

				this.Enabled = true; // Can't import values to disabled keys!
				this.Key = key;
				this.Value = value;
			}
		}

		public RegistrySubKey ToRegistryKey(RegistryHive hive, RegistryView view) =>
			new RegistrySubKey(this.Key, this._value, RegistryValueKind.String, hive, view);

		public override string ToString() => this.ToString(0);

		public string ToString(int indentSize) =>
			// NOTE: We use "this._value" here (instead of the "Value" accessor) to avoid unintended en/de-cryption!
			(Enabled ? "" : ";").PadLeft(indentSize, ' ') + Key + (Encrypted ? "?" : "") + " = " + this._value + (string.IsNullOrWhiteSpace(Comment) ? "" : Comment);

		public override bool Equals(object obj) => base.Equals(obj);

		public override int GetHashCode() => base.GetHashCode();

		public static implicit operator string(IniLineItem data) => data.Value;
		public static implicit operator KeyValuePair<string, string>(IniLineItem data) => new KeyValuePair<string, string>(data.Key, data.Value);
		public static implicit operator IniLineItem(KeyValuePair<string, string> data) => new IniLineItem(data.Key, data.Value);
		#endregion

		#region Static Methods
		/// <summary>
		/// This function takes a supplied string and attempts to extract valid Detail information and returns it as a nice
		/// clean string. NOTE: If a string passes a validation check, performing this operation is probably unneccessary.
		/// </summary>
		/// <param name="data">A string to try and clean up and reformat neatly.</param>
		/// <returns>If data extraction worked, a clean Detial Line string containing the found data, otherwise an empty string.</returns>
		public static string CertifyDetail(string data)
		{
			data = data.Trim();
			if (IsValidDetail(data))
			{
				MatchCollection matches = new Regex( IniUtility.LINE_DETAIL_LINE, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture).Matches(data);
				string  key = matches[0].Groups[0].Value,
						value = matches[0].Groups[1].Value,
						comment = (matches[0].Groups.Count > 4) ? matches[0].Groups[4].Value : "";

				return key + " = " + value + " // " + comment;
			}
			return "";
		}

		/// <summary>Compares a provided string against the Detail line Regex pattern and reports if it appears valid.</summary>
		/// <param name="data">A string to test.</param>
		/// <returns>TRUE if the supplied string conforms to the Regex Pattern, otherwise FALSE.</returns>
		public static bool IsValidDetail(string data) =>
			string.IsNullOrEmpty(data.Trim()) ? false : Regex.IsMatch(data, IniUtility.LINE_DETAIL_LINE, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture );

		/// <summary>Takes a provided string and does its best to turn it into a valid Detail line key value.</summary>
		/// <param name="key">A string to parse / clean into a valid Key.</param>
		/// <returns>Either a string containing a value suitable for use as a Key, or an empty string.</returns>
		public static string CertifyKey(string key)
		{
			key = Regex.Replace( key, @"(<[a-z0-9]>$|[^-\w])", "", RegexOptions.IgnoreCase ) + (key.EndsWith( "?" ) ? "?" : "");
			// Filter(key.Trim(),"abcdefghijklmnopqrstuvwxyz0123456789-_", true) + (key.EndsWith("?") ? "?" : "");
			if (!string.IsNullOrEmpty(key))
				while ((key.IndexOf("__") >= 0) || (key.IndexOf("--") >= 0))
					key = key.Replace("--", "-").Replace("__", "_");

			return key;
		}

		/// <summary>Tests a supplied string against the Regex Pattern for validity as a key.</summary>
		/// <param name="key">A string containing the value to test.</param>
		/// <returns>TRUE if the supplied Key value conforms to the rules for being a valid key, otherwise FALSE.</returns>
		public static bool IsValidKey(string key) => Regex.IsMatch(CertifyKey(key), IniUtility.LINE_KEY_TEMPLATE );

		/// <summary>Takes a supplied string and renders it suitable for use as a comment (primarily removes unwanted characters)</summary>
		/// <param name="comment">A string to parse and clean up for use as a comment.</param>
		/// <returns>The supplied string with invalid characters/character-combinations cleaned out ofit.</returns>
		//public static string CertifyComment(string comment)
		//{
		//	comment = Filter(comment.Trim(),"abcdefghijklmnopqrstuvwxyz1234567890-_ =+!@#$%^&*()[]{}\"|'\\;:<>,./?`~");
		//	if (!string.IsNullOrEmpty(comment))
		//		while ((comment.IndexOf("__") >= 0) || (comment.IndexOf("--") >= 0) || (comment.IndexOf("  ") >= 0))
		//			comment = comment.Replace("--", "-").Replace("__", "_").Replace("  ", " ");

		//	return comment;
		//}

		/// <summary>Tests a supplied string against the Regex Pattern for validity as an in-line comment.</summary>
		/// <param name="comment">A string containing the text to validate.</param>
		/// <returns>TRUE if the provided string conforms to the prescribed pattern for an in-line comment, otherwise FALSE.</returns>
		public static bool IsValidComment(string comment) =>
			(comment is null) ? false : Regex.IsMatch(comment, IniUtility.LINE_COMMENT_TEMPLATE, RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

		/// <summary>Provides an easy means to convert strings to Base64Encoding.</summary>
		/// <param name="data">A string to encode in Base64.</param>
		/// <returns>A string containing the Base64 representation of the supplied string.</returns>
		public static string Base64Encode(string data) =>
			string.IsNullOrEmpty(data.Trim()) ? "" : Convert.ToBase64String(Encoding.UTF8.GetBytes(data.Trim()));

		/// <summary>Provides an easy means to convert Base64 encoded strings to plaintext.</summary>
		/// <param name="data">A Base64 encoded string to convert back to plaintext.</param>
		/// <returns>A string containing the plaintext data decoded from the supplied data.</returns>
		public static string Base64Decode(string data) =>
			string.IsNullOrEmpty(data.Trim()) ? "" : Encoding.UTF8.GetString(Convert.FromBase64String(data.Trim()));

		/// <summary>Provides a means to instantiate an IniLineItem object from a raw string.</summary>
		/// <param name="source">A string from which to populate the new IniLineItem object.</param>
		/// <returns>A new IniLineItem instance populated from the supplied string.</returns>
		public static IniLineItem Parse(string source)
		{
			IniLineItem result = new IniLineItem();
			result.Import(source);
			return result;
		}

		/// <summary>Converts a string containing  "true", "yes", "on", "y", "t" or "1" to Boolean TRUE, otherwise returns FALSE.</summary>
		/// <param name="source">A string to parse into a boolean value.</param>
		/// <returns>TRUE, if the provided string contains any of the following: "true", "yes", "on", "y", "t" or "1"; otherwise FALSE.</returns>
		public static bool ParseBool(string source) =>
			Regex.IsMatch(source.Trim(),@"^(true|yes|y|t|on|1)$", RegexOptions.IgnoreCase);

		/// <summary>Given a string of valid characters, filters all non-matching characters out of a string.</summary>
		/// <param name="validChars">A string of valid (permitted) characters to retain.</param>
		/// <param name="ignoreCase">Specifies whether case should be ignored.</param>
		/// <returns>A string containing only the permitted characters.</returns>
		internal static string Filter(string source, string validChars, bool ignoreCase = true)
		{
			if ((source.Length == 0) || (validChars.Length == 0)) return "";
			if (ignoreCase)
			{
				validChars = validChars.ToLowerInvariant();
				foreach (char c in validChars)
					if (" abcdefghijklmnopqrstuvwxyz".IndexOf(c) > 0) validChars += (char)(c & 223);
			}

			int i = 0;
			while (i < source.Length)
				if (validChars.IndexOf(source.Substring(i, 1)) < 0)
					source = source.Replace(source.Substring(i, 1),"");
				else i++;

			return source;
		}
		#endregion
	}
}