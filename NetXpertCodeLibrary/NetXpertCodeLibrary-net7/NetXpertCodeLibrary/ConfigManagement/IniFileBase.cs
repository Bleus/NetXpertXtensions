using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace NetXpertCodeLibrary.ConfigManagement
{
	public abstract class IniFileBase : IEnumerator<IniGroupItem>
	{
		#region Properties
		protected List<IniGroupItem> _groups = new List<IniGroupItem>();
		private int _position = 0;
		protected List<string> _parameters = new List<string>();
		#endregion

		#region Constructors
		public IniFileBase() { }
		#endregion

		#region Accessors
		/// <summary>Facilitates direct access to dereference the _group list by index, from the parent object itself.</summary>
		/// <param name="index">An integer value that indicates which index of the _group list to dereference.</param>
		protected IniGroupItem this[int index]
		{
			get => ReadOnly ? new IniGroupItem(this._groups[index]) : this._groups[index];
			set
			{
				if (!ReadOnly)
				{
					if ((index >= Count) || (index < 0))
						this._groups.Add(value);
					else
						this._groups[index] = value;
				}
			}
		}

		/// <summary>Facilitates direct access to a member group by using it's name as a reference.</summary>
		/// <param name="groupName">A string specifying the Group Name to dereference.</param>
		public IniGroupItem this[string groupName]
		{
			get
			{
				if (groupName.IndexOf('.') > 0)
				{
					string[] parts = groupName.Split(new char[] { '.' }, 2);
					groupName = parts[0];
				}

				int i = IndexOf(groupName);
				if (i < 0) { i = Count; this._groups.Add(new IniGroupItem(groupName)); }
				return ReadOnly ? new IniGroupItem(this[i]) : this[i];
			}
			set
			{
				if (!ReadOnly)
				{
					int i = IndexOf(groupName);
					if (i < 0)
					{
						if (value is null)
							this._groups.Add(new IniGroupItem(groupName));
						else
							this._groups.Add(value);
					}
					else
					{
						if (value is null)
							this._groups.RemoveAt(i);
						else
							this[i] = value;
					}
				}
			}
		}

		/// <summary>Reports the number of IniGroups that are currently being managed by this collection.</summary>
		public int Count => this._groups.Count;

		/// <summary>Reports the total number of items contained in all Groups of this container.</summary>
		public int DeepCount
		{
			get
			{
				int c = 0;
				foreach (IniGroupItem g in this) { c += g.Count; }
				return c;
			}
		}

		/// <summary>Permits access to the local collection of Groups.</summary>
		public IniGroupItem[] Groups
		{
			get => this._groups.ToArray();
			set { if (!ReadOnly) { this.AddRange(value); } }
		}

		/// <summary>
		/// Indicates the value of the ReadOnly setting of the file. If the file is NOT marked Read-Only, this can be turned on,
		/// but once it is set, it cannot be turned back off again.
		/// </summary>
		public bool ReadOnly
		{
			get => Regex.IsMatch(string.Join(" ",this._parameters), @"[ \t]*(read-only|readonly)[\t ]*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
			set { if (!ReadOnly && value) this._parameters.Add("Read-Only"); }
		}

		/// <summary>Facilitates read-access to the file-parameter stack.</summary>
		public string[] Parameters
		{
			get => this._parameters.ToArray();
			protected set
			{
				// Things to do...
				throw new NotImplementedException();
			}
		}

		// IEnumerator Support Accessors...
		IniGroupItem IEnumerator<IniGroupItem>.Current => this[this._position];

		object IEnumerator.Current => this._groups[this._position];
		#endregion

		#region Methods
		/// <summary>Searches for a GroupName (case-insensitive) in the collection and returns it's index if found.</summary>
		/// <param name="groupName">A string containing the name of the group to search for.</param>
		/// <returns>If a group with a matching name is found, it's index, otherwise -1.</returns>
		protected int IndexOf(string groupName)
		{
			int i = -1; while ((++i < Count) && (!this._groups[i].FullName.Equals(groupName, StringComparison.OrdinalIgnoreCase))) ;
			return (i < Count) ? i : -1;
		}

		/// <summary>Adds (or updates) an IniGroupItem in the collection.</summary>
		/// <param name="group">An IniGroupObject to add or update in the collection.</param>
		public void Add(IniGroupItem group) { if (!ReadOnly) { this[group.FullName] = group; } }

		/// <summary>Adds (or updates) a collection of IniGroupItems from an IniFile object.</summary>
		/// <param name="file">An IniFile instance whose Group objects are to be imported to this object.</param>
		public void Add(IniFileBase file)
		{
			if (!ReadOnly)
				foreach (IniGroupItem group in file)
					this[group.FullName] = group;
		}

		/// <summary>Adds (or updates) the collection from an array of IniGroupItems.</summary>
		/// <param name="groups">An array of IniGroupItems to add (or update) into this collection.</param>
		public void AddRange(IniGroupItem[] groups)
		{
			if (!ReadOnly)
				foreach (IniGroupItem group in groups)
					this[group.FullName] = group;
		}

		/// <summary>Creates a collection of IniGroupItems from an array of strings containing Group Names.</summary>
		/// <param name="groupNames">An array of strings containing group names to create in this collection.</param>
		public void AddRange(string[] groupNames)
		{
			if (!ReadOnly)
				foreach (string name in groupNames)
					if (!HasGroup(name)) { this.Add(new IniGroupItem(name)); }
		}

		/// <summary>Creates a text representation of the data contained in this object's instance.</summary>
		/// <returns>The contents of this object as a string.</returns>
		public override string ToString()
		{
			string result =
				"## File created on: " + DateTime.Now.ToString("MMM dd, yyyy") +
				" at " + DateTime.Now.ToString("H:mm") +
				" by " + System.Security.Principal.WindowsIdentity.GetCurrent().Name +
				"\r\n~BEGIN: " + String.Join(" ", this._parameters) + "\r\n\r\n";

			foreach (IniGroupItem group in this._groups)
				if (!group.IsVirtual)
					result += group.ToString() + "\r\n";

			return result + "~:END\r\n";
		}

		/// <summary>Returns an array of strings containing the names of all IniGroupItems managed by this object.</summary>
		public string[] GroupNames()
		{
			List<string> names = new List<string>();
			foreach (IniGroupItem ili in this)
				names.Add(ili.Name);

			return names.ToArray();
		}

		/// <summary>Reports on the presence of a specified IniGroupObject (by its name) in this collection.</summary>
		/// <param name="groupName">A string specifying the name of the group to validate.</param>
		/// <returns>TRUE if an IniGroupItem object exists in the collection with the specified name, FALSE otherwise.</returns>
		public bool HasGroup(string groupName) => (IndexOf(groupName) >= 0);

		/// <summary>Reports on the presence of a specified IniLineObject (by its Group Name and Key) in this collection.</summary>
		/// <param name="groupName">A string specifying the group name that the item should be included in.</param>
		/// <param name="itemName">A string specifying the Key identity of the IniLineItem to be validated.</param>
		/// <returns>TRUE if the IniLineItem was found, otherwise FALSE.</returns>
		public bool HasItem(string groupName, string itemName) =>
			this.HasGroup(groupName) && this[groupName].HasItem(itemName);

		/// <summary>Facilitates Item retrieval via an X22 Group.Item identifier.</summary>
		/// <param name="identifier">A string containing an X22 formatted Group.Item identifier.</param>
		/// <returns>A copy of the requested IniLineItem (if it exists), otherwise NULL.</returns>
		/// <exception cref="InvalidDataException"></exception>
		public IniLineItem X22Get(string identifier)
		{
			if (identifier.IndexOf('.') > 0)
			{
				string[] parts = identifier.Split(new char[] { '.' });
				return (
							(parts.Length > 1) &&
							this.HasGroup(parts[0]) &&
							this[parts[0]].HasItem(parts[1])
						) ? new IniLineItem(this[parts[0]][parts[1]]) : null;
			}
			throw new InvalidDataException("The provided string doesn't conform to an X.22 standard compatible with this function.");
		}

		/// <summary>Facilitates setting an IniLineItem value using an X22 Group.Item identifier.</summary>
		/// <param name="identifier">A string containing an X22 formatted Group.Item identifier.</param>
		/// <param name="value">A string containing the value to assign.</param>
		/// <returns>TRUE if the assignment succeeded, otherwise FALSE.</returns>
		public bool X22Set(string identifier, string value)
		{
			if (!ReadOnly && (identifier.IndexOf('.') > 0))
			{
				string[] parts = identifier.Split(new char[] { '.' });
				if ((parts.Length > 1) && this.HasGroup(parts[0]) && this[parts[0]].HasItem(parts[1]))
				{
					this[parts[0]][parts[1]].Value = value;
					return true;
				}
			}
			return false;
		}

		/// <summary>Facilitates searching the file's parameter list for entries matching a specified pattern.</summary>
		/// <param name="regexPattern">The RegEx pattern to apply to the search.</param>
		/// <param name="options">The RegExOptions to implement (Default = Compiled + IgnoreCase + ExplicitCapture).</param>
		/// <returns>An array containing all of the Parameters found that match the provided RegEx parameters.</returns>
		public string[] FindMatchingParameters(string regexPattern, RegexOptions options = RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture)
		{
			List<string> results = new List<string>();
			foreach (string s in Parameters)
				if (Regex.IsMatch(s, regexPattern, options))
					results.Add(s);

			return results.ToArray();
		}

		/// <summary>If the file is defined as Read-Only, facilitates clearing all Groups from it.</summary>
		public void Clear() { if (!ReadOnly) { this._groups = new List<IniGroupItem>(); } }

		/// <summary>Descendent classes must implement a public Load() routine to facilitate retrieving the settings from an appropriate source.</summary>
		/// <returns>TRUE if the Load() process succeeded, otheriwse FALSE.</returns>
		public abstract bool Load(string fileName = "");

		/// <summary>Descendent classes must implement a public Save() routine to facilitate saving the settings to an appropriate destination.</summary>
		/// <returns>TRUE if the Save() process succeeded, otheriwse FALSE.</returns>
		public abstract bool Save(string fileName = "");
		#endregion

		//IEnumerator Support
		public IEnumerator<IniGroupItem> GetEnumerator() => this._groups.GetEnumerator();

		bool IEnumerator.MoveNext() => (++this._position) < this._groups.Count;

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
}