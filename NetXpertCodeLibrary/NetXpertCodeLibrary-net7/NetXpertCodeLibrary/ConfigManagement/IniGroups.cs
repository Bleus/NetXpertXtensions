using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace NetXpertCodeLibrary.ConfigManagement
{

	/// <summary>Manages a collection of IniLineItem derived objects associated to a specific group name.</summary>
	public class IniGroupItem : IEnumerator<IniLineItem>
	{
		#region Properties
		private int _position = 0;
		protected string _name = "";
		protected string _comment = "";
		private string _hive = "";
		protected List<IniLineItem> _itemList = new List<IniLineItem>();
		#endregion

		#region Constructors
		public IniGroupItem(string groupName, RegistryHive? hive = null)
		{
			this.Name = groupName;
			if (!(hive is null))
				this.Hive = (RegistryHive)hive;
		}

		public IniGroupItem(IniGroupItem copy)
		{
			this._name = copy._name;
			this._comment = copy._comment;
			this._itemList = new List<IniLineItem>(copy._itemList.ToArray());
			this._hive = copy.HiveText;
		}

		protected IniGroupItem() { }
		#endregion

		#region Accessors
		/// <summary>Facilitates Get/Set access to elements of this collection by the requisite Index value.</summary>
		protected IniLineItem this[int index]
		{
			get => this._itemList[index];
			set
			{
				if ((index >= Count) || (index < 0))
					this._itemList.Add(value);
				else
					this._itemList[index] = value;
			}
		}

		/// <summary>Facilitates direct Get/Set access to any key in the collection by it's Key identity.</summary>
		public IniLineItem this[string key]
		{
			get
			{
				if (key.IndexOf('.') > 0)
				{
					string[] parts = key.Split(new char[] { '.' });
					if (parts.Length > 1) key = parts[1];
				}

				int i = IndexOf(key);
				if (i < 0) { i = Count; this._itemList.Add(new IniLineItem(key)); }
				return this[i];
			}
			set
			{
				int i = IndexOf(key);
				if (i < 0)
				{
					if (value is null) { }
					else
						this._itemList.Add(new IniLineItem(key, value.Value, value.Encrypted, value.Comment, value.Enabled));
				}
				else
				{
					if (value is null)
						this._itemList.RemoveAt(i);
					else
						this[i] = value;
				}
			}
		}

		/// <summary>Reports the number if IniLineItem objects are managed by this group.</summary>
		public int Count => this._itemList.Count;

		/// <summary>Gets/Sets the name of the group.</summary>
		public string Name
		{
			get => this._name;
			protected set
			{
				string name = CertifyGroupName(value);
				if (!string.IsNullOrEmpty(name))
					this._name = name;
			}
		}

		/// <summary>Reports the full group name including the Hive deisgnation if one is defined.</summary>
		public string FullName => ((this.HiveText.Length > 0) ? this.HiveText + ":" : "") + this.Name;

		/// <summary>Facilitates controlled interaction with the Group's Hive designation via strings.</summary>
		public string HiveText
		{
			get => this._hive;
			set
			{
				if (string.IsNullOrEmpty(value) || (value.Trim().Length == 0))
					this._hive = "";
				else
				{
					string v = value;
					if (v.Length < 3) { v = "HK" + v; }
					if (Regex.IsMatch(v, "^" + IniUtility.GROUP_HIVE_PATTERN + "$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled))
						this._hive = v.Replace(":", "").ToUpper(); // Regex allows there to be a suffix colon, which we don't want...
				}
			}
		}

		/// <summary>Facilitates interacting with this group's Hive designation via RegistryHive enumeration, if possible.</summary>
		public RegistryHive Hive
		{
			get => RegMgmt.ParseHiveAbbr(this.HiveText);
			set => this._hive = RegMgmt.RegistryHiveToAbbr(value);
		}

		/// <summary>Facilitates getting and setting the Group Comments.</summary>
		public string Comment
		{
			get => this._comment;
			set
			{
				if (string.IsNullOrEmpty(value.Trim()))
					this._comment = "";
				else
				{
					string[] lines = (value.IndexOf("\r\n") < 0) ? new string[] { value } : value.Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
					for (int i=0; i<lines.Length; i++)
					{
						lines[i] = lines[i].Trim();
						if (!Regex.IsMatch(lines[i], @"^[ \t]*##[\t ]*")) lines[i] = "## " + lines[i];
					}
					this._comment = String.Join("\r\n", lines);
				}
			}
		}

		/// <summary>Gets/Sets the encryption state of all keys managed by this group. When used to set a value, all keys in the group are set to the specified value.</summary>
		/// <returns>TRUE if all keys managed by this group are encrypted, otherwise FALSE.</returns>
		public bool Encrypted
		{
			get
			{
				bool result = true; int i = -1;
				while (result && (++i < Count)) result &= this[i].Encrypted;
				return result;
			}
			set
			{
				for (int i = 0; i < Count; i++)
					this[i].Encrypted = value;
			}
		}

		public bool IsVirtual => this.Name.Equals("Virtual", StringComparison.OrdinalIgnoreCase) || this.Name.Equals("Dynamic", StringComparison.OrdinalIgnoreCase);

		IniLineItem IEnumerator<IniLineItem>.Current => this[this._position];

		object IEnumerator.Current => this._itemList[this._position];
		#endregion

		#region Methods
		/// <summary>Searches the collection for an IniLineItem whose Key matches the provided text. (Case-Insensitive!)</summary>
		/// <param name="key">A string specifying the Key value of the desired item.</param>
		/// <returns>If a matching object is found, it's index in the collection, otherwise -1.</returns>
		protected int IndexOf(string key)
		{
			int i = -1; while ((++i < this.Count) && !key.Equals(this[i].Key, StringComparison.OrdinalIgnoreCase)) ;
			return (i < Count) ? i : -1;
		}

		/// <summary>Searches the collection for an IniLineItem whose Key matches the provided text. (Case-Insensitive!)</summary>
		/// <param name="item">An IniLineItem whose Key value matches that of the desired item.</param>
		/// <returns>If a matching object is found, it's index in the collection, otherwise -1.</returns>
		protected int IndexOf(IniLineItem item) => IndexOf(item.Key);

		/// <summary>Parses a supplied string and attempts to merge it with the contents of this object.</summary>
		/// <param name="source">A string to parse.</param>
		/// <remarks>
		/// Regex Pattern Breakdown (GROUP_PATTERN): Each "match" is an IniGroup, in which each RegEx Group are:
		///		Group 0: Comments
		///		Group 2: Group Name
		///		Group 3: Group Contents
		/// </remarks>
		public bool Import(string source)
		{
			if (Validate(source))
			{
				MatchCollection matches = new Regex( IniUtility.GROUP_GROUP_PATTERN, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture).Matches($"\r\n{source}\r\n\r\n");
				if ((matches.Count > 0) && (matches[0].Groups.Count > 2))
				{
					this.Comment = matches[0].Groups["comment"].Value;
					this.Name = matches[0].Groups["name"].Value;
					this.HiveText = matches[0].Groups["hive"].Value;

					if (matches[0].Groups["items"].Value.Trim().Length > 0)
					{
						string[] lines = matches[0].Groups["items"].Value.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
						this._itemList = new List<IniLineItem>();
						foreach ( string line in lines )
							this.Add( IniLineItem.Parse( line ) );
					}
					return true;
				}
			}
			return false;
		}

		/// <summary>Adds or updates an IniLineItem in this collection.</summary>
		/// <param name="newItem">An IniLineItem object to either add, or update in the collection.</param>
		public void Add(IniLineItem newItem) => this[newItem.Key] = newItem;

		/// <summary>Adds (or updates) a collection of IniLineItems in this group.</summary>
		/// <param name="newItems">An array of IniLineItem objects to add/update.</param>
		public void AddRange(IniLineItem[] newItems)
		{
			foreach (IniLineItem ili in newItems)
				this[ili.Key] = ili;
		}

		/// <summary>Adds (or updates) the collection with the contents of a supplied IniGroupItem object.</summary>
		/// <param name="group">An IniGroupItem object whose items will be merged with those of this collection.</param>
		public void AddRange(IniGroupItem group)
		{
			foreach (IniLineItem ili in group)
				this[ili.Key] = ili;
		}

		/// <summary>Removes a key from this collection if it exists.</summary>
		/// <param name="key">A string specifying the Key of the item to remove.</param>
		public bool Remove( string key )
		{
			int i = IndexOf( key );
			if ( i >= 0 )
				this._itemList.RemoveAt( i );

			return i >= 0;
		}

		/// <summary>Clears all items from the group.</summary>
		public void Clear() => this._itemList = new List<IniLineItem>();

		public string ToString(int indentSize)
		{
			string result = this.Comment + "\r\n" + "[" + this.FullName + "]\r\n";

			foreach (IniLineItem line in this._itemList)
				result += line.ToString(indentSize + 3) + "\r\n";
			return result.PadLeft(indentSize, ' ');
		}

		public string[] ItemNames()
		{
			List<string> names = new List<string>();
			foreach (IniLineItem ili in this)
				names.Add(ili.Key);

			return names.ToArray();
		}

		public override string ToString() => ToString(0);

		public bool HasItem(string itemName) => (IndexOf(itemName) >= 0);

		public IniLineItem[] ToArray() => this._itemList.ToArray();
		#endregion

		#region Static Methods
		/// <summary>Tests a provided string to see if it conforms to a pattern recognizable as a Group.</summary>
		/// <param name="source">A string containing the text to validate.</param>
		/// <returns>TRUE if the string corresponds to a recognizable syntax / format, otherwise FALSE.</returns>
		public static bool Validate(string source) =>
			!string.IsNullOrEmpty(source) &&
			Regex.IsMatch(
				"\r\n" + source + "\r\n\r\n",
				IniUtility.GROUP_GROUP_PATTERN, 
				RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.Compiled
			);

		/// <summary>Parses a supplied string and attempts to return a valud Group Name value from it.</summary>
		/// <param name="source">A string containing a proposed Group Name.</param>
		/// <returns>If successful, a string containing a valid Group Name, otherwise an empty string.</returns>
		public static string CertifyGroupName(string source)
		{
			if (!string.IsNullOrWhiteSpace( source ))
			{

				source = (source.StartsWith( "$" ) ? "$" : "") + Regex.Replace( source, @"[^\w-]", "" );
				return Regex.IsMatch( source, IniUtility.GROUP_BASENAME_PATTERN, RegexOptions.IgnoreCase ) ? source : "";
			}
			return "";
		}

		/// <summary>Tests a supplied string for validity as a Group Name.</summary>
		/// <param name="source">A string containing a proposed Group Name.</param>
		/// <returns>TRUE if the supplied value conforms with the dictates for a valid Group Name, otherwise FALSE.</returns>
		public static bool IsValidGroupName(string source) =>
			!string.IsNullOrWhiteSpace(source) &&
			Regex.IsMatch(
				source,
				IniUtility.GROUP_BASENAME_PATTERN,
				RegexOptions.IgnoreCase
			);

		/// <summary>Attempts to parse a string to load configuration settings into a new IniGroupItem object.</summary>
		/// <param name="source">A string whose contents are to be parsed.</param>
		/// <returns>A new IniGroupItem whose contents are populated with the relevant information if Parsing was successful.</returns>
		/// <exception cref="System.InvalidOperationException">Thrown if the parse attempt failed.</exception>
		public static IniGroupItem Parse(string source)
		{
			IniGroupItem result = null;
			if (!string.IsNullOrWhiteSpace( source ))
			{
				result = new IniGroupItem();
				if (!result.Import( source ))
					throw new InvalidOperationException( "The provided data cannot be parsed into an IniGroupItem object." );
			}
			return result;
		}
		#endregion

		//IEnumerator Support
		public IEnumerator<IniLineItem> GetEnumerator() => 
			this._itemList.GetEnumerator();

		public bool MoveNext() => 
			(++this._position) < this._itemList.Count;

		public void Reset() => 
			this._position = 0;

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
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
	}
}