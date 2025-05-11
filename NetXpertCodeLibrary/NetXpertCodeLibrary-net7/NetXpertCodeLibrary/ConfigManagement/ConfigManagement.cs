using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace NetXpertCodeLibrary.ConfigManagement
{
	/// <summary>
	/// This class is the basic foundation block that referencing applications should build their internal configuration mechanics
	/// around. It is Abstract because each independent application will need to specify implementation-specific parameters, and
	/// interfaces.
	/// </summary>
	public abstract class ConfigManagementBase : IEnumerator<IniFile>
	{
		#region Properties
		/// <summary>Stores all of the active IniFile objects that comprise this configuration.</summary>
		protected List<IniFile> _files = new List<IniFile>();

		/// <summary>Defines whether or not configuration files are allowed to reference subsequent files (cascading configuration)</summary>
		//private bool _allowCascades = true;

		/// <summary>
		/// Any IniLineItem objects whose keys are contained in this list will be treated as cascading configuration pointers. That
		/// is, the system will automatically try to load additonal configuration files using the value of such keys as the filename.
		/// </summary>
		private string[] _cascadeKeys = new string[] { };

		/// <summary>This value specifies whether files loaded as cascades will be forced to be read-only or not.</summary>
		//private bool _cascadesForceReadOnly = true;

		/// <summary>Specifies the default file extension to use for configuration files managed for the parent application.</summary>
		private string _defaultFileExtension = ".ini";

		/// <summary>Specifies the default folders which will be searched for configuration files when no specific path is indicated</summary>
		/// <remarks>By default, the folder containing the base .EXE file is selected.</remarks>
		private string[] _searchPaths = new string[] { ConfigManagementBase.ExecutablePath() };

		/// <summary>Used to specify the name of the default configuration file, when one isn't specified.</summary>
		private string _defaultFileName = "Settings";

		/// <summary>Used as a buffer to store the most recently compiled version of this configuration.</summary>
		protected IniFile _compiled = null;

		/// <summary>Used internally for Enumeration</summary>
		private int _position = 0;
		#endregion

		#region Constructors
		public ConfigManagementBase()
		{
			DefaultPath = null;
			this._files.Add(DefaultIniFile());
			this.Compile();
		}

		public ConfigManagementBase(string defaultPath, string fileName = "Settings")
		{
			DefaultPath = defaultPath;
			this._files.Add(DefaultIniFile());
			this.Compile();
		}

		public ConfigManagementBase(IniFile file)
		{
			DefaultPath = file.FilePath;
			this._files.Add(DefaultIniFile());
			this.Add(file);
			this.Compile();
		}
		#endregion

		#region Accesors
		/// <summary>Gets the value specifying whether or not cascading configuration is allowed.</summary>
		protected bool AllowCascades { get; set; }
		//{
		//	get => this._allowCascades;
		//	set => this._allowCascades = value;
		//}

		/// <summary>Facilitates access to a copy of the CascadeKeys array.</summary>
		protected string[] CascadeKeys
		{
			get => new List<string>(this._cascadeKeys).ToArray(); // forces a COPY of the array to be passed, not the array itself.
			set => this._cascadeKeys = value;
		}

		/// <summary>Reports the number of files currently being managed by this object.</summary>
		public int Count => this._files.Count;

		/// <summary>Reports the number of defined Search Paths.</summary>
		protected int SearchLength => this._searchPaths.Length;

		/// <summary>Returns the number of defined Cascade Keys.</summary>
		protected int CascadeLength => this._cascadeKeys.Length;

		/// <summary>Gets/Sets the default filename that the object uses when one isn't provided.</summary>
		protected string DefaultFileName
		{
			get => _defaultFileName + ((Path.GetExtension(_defaultFileName) == "") ? DefaultExtension : "");
			set
			{
				string fn = value.Trim();
				if ((fn.Length > 0) && Regex.IsMatch(fn, @" ^[\w\-.] + $", RegexOptions.IgnoreCase | RegexOptions.Compiled))
					this._defaultFileName = fn;
			}
		}

		/// <summary>Specifies whether or not Cascaded configuration files are forced to be Read-Only.</summary>
		protected bool CascadeForceReadOnly { get; set; }
		//{
		//	get => this._cascadesForceReadOnly;
		//	set => this._cascadesForceReadOnly = value;
		//}

		/// <summary>Facilitates interaction with each file in the collection by it's index number.</summary>
		protected IniFile this[int index]
		{
			get => this._files[index];
			set
			{
				if ((index >= Count) || (index < 0))
					this.Add(value);
				else
					this._files[index] = value;
			}
		}

		/// <summary>Facilitates interaction with specified Groups within the collated collection.</summary>
		protected IniGroupItem this[string groupName] => ((this._compiled is null) ? Compile() : this._compiled)[groupName];

		/// <summary>Gets/Sets the default path upon which this object will look for specified INI files if no path is given.</summary>
		protected string DefaultPath
		{
			get => this._searchPaths[0];
			set
			{
				if (value is null) this._searchPaths[0] = ConfigManagementBase.ExecutablePath();
				else
					try
					{
						string path = Path.GetFullPath(value);
						if (Path.IsPathRooted(path) && Directory.Exists(path))
							this._searchPaths[0] = path;
					}
					catch { }
			}
		}

		/// <summary>Gets/Sets the defined default extension for this collection. (Form: ".abc")</summary>
		protected string DefaultExtension
		{
			get => ((this._defaultFileExtension[0]=='.') ? "" : ".") + this._defaultFileExtension;
			set
			{
				string pattern = @"^\*?\.?([a - zA - Z0 - 9_@#$%^&(){}\[\]-~]+)$";
				string data = value.Trim();
				if (Regex.IsMatch(data, pattern, RegexOptions.IgnoreCase))
					this._defaultFileExtension = "." + data.Trim(new char[] { '.', '*' });
			}
		}

		/// <summary>Determines the most relevant default target filename and returns it as a string.</summary>
		protected int TopWriteableIndex
		{
			get
			{
				if (Count == 0) return -1;
				int result = Count;
				while ((--result>=0) && this[result].ReadOnly);
				return (result);
				//string fileName = "Default.ini";
				//if (this.HasFile("Cobblestone.ini")) fileName = "Cobblestone.ini";
				//if (this.HasFile(Common.UserName + ".ini")) fileName = Common.UserName + ".ini";
				//return fileName;
			}
		}

		/// <summary>Facilitates simple access to the last writeable IniFile in the collection.</summary>
		/// <exception cref="IndexOutOfRangeException"></exception>
		protected IniFile Top
		{
			get
			{
				int index = TopWriteableIndex;
				if (index >= 0) return this._files[index];
				throw new IndexOutOfRangeException("There are no writeable IniFiles in the collection.");
			}
		}

		/// <summary>Returns the index value of the topmost loaded configuration file.</summary>
		protected IniFile Last =>
			(Count > 0) ? this._files[Count - 1] : null;

		/// <summary>Provides access to the collated settings managed by this object.</summary>
		protected IniFile Settings => this._compiled;

		IniFile IEnumerator<IniFile>.Current => this[this._position];

		object IEnumerator.Current => this._files[this._position];
		#endregion

		#region Methods
		/// <summary>Searches the collection for an IniFile object with the specified file name.</summary>
		/// <param name="fileName">A string specifying the file name to search for.</param>
		/// <returns>If a match is found, the index of that object, otherwise -1.</returns>
		/// <remarks>
		/// The index performs a full-file-path-and-name comparison looking for a match. If found, it returns the index,
		/// but if not found, it starts again and looks for just a filename match (no path) and returns the result of that
		/// search.
		/// </remarks>
		protected int IndexOf(string fileName)
		{
			if (Path.GetExtension(fileName) == "") fileName += DefaultExtension;
			int i = -1; while ((++i < Count) && (!this._files[i].File.Equals(fileName, StringComparison.OrdinalIgnoreCase))) ;

			if (i==Count)
			{
				fileName = Path.GetFileName(fileName);
				i = -1; while ((++i < Count) && (!this._files[i].FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase))) ;
			}
			return (i < Count) ? i : -1;
		}

		/// <summary>Returns an array containing the names of all INI files currently being managed.</summary>
		/// <returns>An array containing the names of all INI files currently being managed.</returns>
		public string[] FileNames()
		{
			List<string> names = new List<string>();
			foreach (IniFile ili in this)
				names.Add(ili.FileName);

			return names.ToArray();
		}

		/// <summary>Empties the collection.</summary>
		public void Clear() => this._files = new List<IniFile>();

		/// <summary>Clears the collection, then attempts to add the specified file.</summary>
		/// <param name="fileName">A string specifying the file to try and add to the empty collection.</param>
		public void Clear(string fileName)
		{
			this._files = new List<IniFile>();
			this.Add(fileName);
		}

		/// <summary>Clears the collection, then adds the provided IniFile object.</summary>
		/// <param name="file">An IniFile object to add to the emptied collection.</param>
		public void Clear(IniFile file)
		{
			this._files = new List<IniFile>();
			this.Add(file);
		}

		/// <summary>Used for descendent classes to perform a basic load of the default configuration file from the default folder.</summary>
		/// <returns>TRUE if the operation worked, otherwise FALSE.</returns>
		protected bool Load() =>
			Add(this.DefaultPath + this.DefaultFileName);

		/// <summary>Attempts to add a new INI file by it's specified name.</summary>
		/// <param name="fileName">The name of the proposed INI file to add to the collection.</param>
		/// <returns>If the file was found and added, TRUE, otherwise FALSE.</returns>
		public bool Add(string fileName)
		{
			fileName = fileName.Trim();
			IniFile newFile = new IniFile();

			if (!File.Exists(fileName))
			{
				fileName = Path.GetFileName(fileName);
				if (Path.GetExtension(fileName)=="") fileName += DefaultExtension;
				int i = -1;
				while ((++i < this._searchPaths.Length) && !File.Exists(this._searchPaths[i] + fileName)) ;
				if (i < this._searchPaths.Length) fileName = this._searchPaths[i] + fileName;
			}

			if (File.Exists(fileName))
			{
				bool result = newFile.Load(fileName);
				if (result)
				{
					this.Add(newFile);
					this.LoadCascades(newFile);
					this.Compile();
					return true;
				}
			}

			return false;
		}

		/// <summary>Adds (or updates) a provided IniFile object to the collection.</summary>
		/// <param name="file">An IniFile object to add / merge with the collection.</param>
		public IniFile Add(IniFile file)
		{
			int i = IndexOf(file.FileName);
			if (i < 0)
				this._files.Add(file);
			else
				this._files[i] = file;

			this.Compile();
			return file;
		}

		/// <summary>Removes a configuration file from the collection using it's file name as an identifier.</summary>
		/// <param name="fileName">The name of the file to remove from the collection.</param>
		/// <returns>TRUE if the requested file was located and and sucessfully removed.</returns>
		public bool Remove(string fileName)
		{
			int i = IndexOf(fileName);
			if (i >= 0)
			{
				this._files.RemoveAt(i);
				this.Compile();
				return true;
			}
			return false;
		}

		/// <summary>Retrievs an IniFile from the collection by it's name.</summary>
		/// <param name="fileName">A string specifying the file name to search for or "*" for the last writeable file in the collection.</param>
		/// <returns>If a matching file is found, that file's IniFile object, otherwise NULL</returns>
		/// <exception cref="FileNotFoundException"></exception>
		public IniFile GetFile(string fileName = "*")
		{
			fileName = fileName.Trim();
			if (fileName.Length==0)
				return (this.Count > 0) ? Settings : new IniFile();
			else
			{
				if (fileName == "*") { fileName = this._files[TopWriteableIndex].FileName; }

				int i = IndexOf(fileName);
				if (i < 0)
				{
					i = Count;
					if (!this.Add(fileName))
						throw new FileNotFoundException("The INI file \"" + fileName + "\" could not be located.");
				}
				return this._files[i];
			}
		}

		/// <summary>Facilitates Setting a value.</summary>
		/// <param name="group">The Group to set the value in.</param>
		/// <param name="key">The Key to set.</param>
		/// <param name="value">The Value to assign to the Key.</param>
		/// <param name="fileName">The destination filename in which to save this setting. If omitted, the last writeable file laoded will be used.</param>
		public void SetValue(string group, string key, string value, string fileName = "*")
		{
			int index = (fileName=="*") ? TopWriteableIndex : IndexOf(fileName);
			if (index>=0)
				this._files[index][group].Add(new IniLineItem(key, value));
		}

		/// <summary>Facilitates Setting a value.</summary>
		/// <param name="group">The Group to set the value in.</param>
		/// <param name="item">An IniLineItem value to set.</param>
		/// <param name="fileName">The destination filename in which to save this setting. If omitted, the last writeable file laoded will be used.</param>
		public void SetValue(string group, IniLineItem item, string fileName = "*")
		{
			int index = (fileName == "*") ? TopWriteableIndex : IndexOf(fileName);
			if (index >= 0)
				this._files[index][group].Add(item);
		}

		/// <summary>Retrieves an item from the configuration as designated by an X.22 (dotted-notation) indicator.</summary>
		/// <param name="identifier">A string specifying the group and item to set/define in dotted-notation.</param>
		/// <returns>If the specified item exists, it's returned in an IniLineItem object, otherwise Exceptions are thrown.</returns>
		/// <exception cref="InvalidDataException"></exception>
		public IniLineItem X22Get(string identifier)
		{
			if (identifier.IndexOf('.')>0)
			{
				string[] parts = identifier.Split(new char[] { '.' });
				if (this.HasGroup(parts[0]) && this[parts[0]].HasItem(parts[1])) return this[parts[0]][parts[1]];
				throw new InvalidDataException("The requested item (\"" + identifier + "\") does not refer to a valid Group/Item.");
			}
			throw new InvalidDataException("The provided string doesn't conform to an X.22 standard compatible with this function.");
		}

		/// <summary>Facilitates setting an item within a group using an X.22 (dotted-notation) designation.</summary>
		/// <param name="identifier">A string specifying the group and item to set/define in dotted-notation.</param>
		/// <param name="value">A string containing the value to set.</param>
		/// <returns>TRUE if the operation succeeded, otherwise FALSE.</returns>
		/// <exception cref="InvalidDataException"></exception>
		public bool X22Set(string identifier, string value)
		{
			if (identifier.IndexOf('.') > 0)
			{
				string[] parts = identifier.Split(new char[] { '.' });
				this[parts[0]][parts[1]].Value = value;

				// This test can report FALSE if the destination file/group is marked ReadOnly and the set operation was rejected.
				return (this[parts[0]][parts[1]].Value == value);
			}
			throw new InvalidDataException("The provided string doesn't conform to an X.22 standard compatible with this function.");
		}

		/// <summary>Reports on whether a file with the specified name exists in the collection.</summary>
		/// <param name="fileName">The name of the file to search for in the collection.</param>
		/// <returns>TRUE if a file with the specified name exists in the collection, otherwise FALSE.</returns>
		public bool HasFile(string fileName) => (IndexOf(fileName) >= 0);

		/// <summary>Reports on whether an specified file, group and/or item exists in the collection.</summary>
		/// <param name="fileName">The Filename to search in.</param>
		/// <param name="groupName">The Group name to search on.</param>
		/// <param name="itemName">The Item to validate the existence of.</param>
		/// <returns>TRUE if the specified item could be found, otherwise FALSE.</returns>
		public bool HasItem(string groupName, string itemName, string fileName) =>
			(!string.IsNullOrEmpty(fileName) && this.HasFile(fileName)) &&
			(!string.IsNullOrEmpty(groupName) && this.GetFile(fileName).HasGroup(groupName)) &&
			(!string.IsNullOrEmpty(itemName) && this.GetFile(fileName)[groupName].HasItem(itemName));

		/// <summary>
		/// Collates all of the settings from all of the loaded file(s) (bottom to top) to produce a hierarchical
		/// IniFile object that represents the complete collection of settings drawn from all loaded files.
		/// </summary>
		/// <returns>A dynamically-built, populated IniFile object containing all settings.</returns>
		protected IniFile Compile()
		{
			IniFile result = new IniFile();
			result.Header = "This is a hybrid (compiled) configuration file.\r\n" + String.Join(",", this.FileNames());
			foreach (IniFile iF in this._files)
				result.Add(iF);
			result.ReadOnly = true;

			this._compiled = result;
			return result;
		}

		/// <summary>Attempts to load a cascaded file based on it's name.</summary>
		/// <param name="fileName">A string specifying the fileName to attempt to add.</param>
		protected void LoadCascades(string fileName) =>
			LoadCascades(IndexOf(fileName));

		/// <summary>
		/// Parses a the loaded IniFile object at the specified index, looking for tags that correlate to values in the _cascadeKeys
		/// variable and then attempting to load additional (cascaded) configuration files if these are found.
		/// </summary>
		/// <param name="index">The index, within the loaded collection, to parse.</param>
		protected void LoadCascades(int index)
		{
			if ((this._cascadeKeys.Length > 0) && (index >= 0) && (index < Count))
				LoadCascades(this[index]);
		}

		/// <summary>
		/// Parses a provided IniFile object looking for tags that correlate to values in the _cascadeKeys variable and then
		/// attempting to load additional (cascaded) configuration files if these are found.
		/// </summary>
		/// <param name="file">An IniFile object to parse.</param>
		protected void LoadCascades(IniFile file)
		{
			if (AllowCascades)
				foreach (IniGroupItem group in file)
					foreach (string f in this._cascadeKeys)
						if (group.HasItem(f))
						{
							string cascadeFileName = group[f].Value;
							if (Path.GetExtension(cascadeFileName) == "") cascadeFileName += DefaultExtension;
							// If the file already exists in the collection (and is Read-Only!) move it to the top of the processing
							// order, otherwise go load it, if we can.
							if (HasFile(cascadeFileName))
							{
								int idx = IndexOf(cascadeFileName);
								if (this[idx].ReadOnly)
								{
									IniFile ifi = this._files[idx];
									this._files.RemoveAt(idx);
									this._files.Add(ifi);
								}
							}
							else
							{
								this.Add(cascadeFileName);
								this.Last.ReadOnly = CascadeForceReadOnly;
							}
						}
		}

		/// <summary>Allows a quick (case-insensitive) check to determine if a specified key is a defined Cascade Key.</summary>
		/// <param name="key">A string containing the key name to validated.</param>
		/// <returns>TRUE if the specified value is a recognized Cascade Key, otherwise FALSE.</returns>
		protected bool IsCascadeKey(string key)
		{
			int i = -1;
			while ((++i < this._cascadeKeys.Length) && !key.Equals(this._cascadeKeys[i], StringComparison.OrdinalIgnoreCase)) ;
			return (i < this._cascadeKeys.Length);
		}

		/// <summary>Facilitates adding a Cascade Key to the configuration.</summary>
		/// <param name="key">A string containing the key to be added.</param>
		/// <returns>TRUE if the operation succeeded, otherwise false.</returns>
		protected bool AddCascadeKey(string key)
		{
			key = IniLineItem.CertifyKey(key);
			if (key.Length > 0)
			{
				List<string> keys = new List<string>(this._cascadeKeys);
				int i = -1; while ((++i < keys.Count) && !keys[i].Equals(key, StringComparison.OrdinalIgnoreCase)) ;
				if (i == keys.Count)
				{
					keys.Add(key);
					this._cascadeKeys = keys.ToArray();
				}
				return true;
			}
			return false;
		}

		/// <summary>Facilitates adding a collection of Cascade Keys to the configuration.</summary>
		/// <param name="keyd">An array of strings containing the Keys to be added.</param>
		protected void AddCascadeKeys(string[] keys) { foreach (string key in keys) { this.AddCascadeKey(key); } }

		/// <summary>Empties the Cascade Key collection.</summary>
		protected void ClearCascadeKeys() => this._cascadeKeys = new string[] { };

		/// <summary>Restores the Search Path collection to its base state.</summary>
		protected void ClearSearchPaths() => this._searchPaths = new string[] { ConfigManagementBase.ExecutablePath() };

		/// <summary>Facilitates adding a path to the list of search paths defined for this object.</summary>
		/// <param name="path">A string containing the path value to add.</param>
		/// <returns>TRUE is the specified path was valid, rooted, and exists, otherwise FALSE.</returns>
		protected bool AddSearchPath(string path)
		{
			try
			{
				path = Path.GetFullPath(path);
				if (!path.EndsWith(@"\")) path += @"\";
				if (Path.IsPathRooted(path) && Directory.Exists(path))
				{
					List<string> paths = new List<string>(this._searchPaths);
					if (!paths.Contains(path)) { paths.Add(path); this._searchPaths = paths.ToArray(); return true; }
				}
			}
			catch { }
			return false;
		}

		/// <summary>Reports the final collated configuration definition of this object as a string.</summary>
		/// <returns>A string containing the final collated configuration as plaintext.</returns>
		public override string ToString() => this.Compile().ToString();

		/// <summary>Reports whether a specified Group exists in the final collated collection of settings.</summary>
		/// <param name="name">A string specifying the Group name to look for.</param>
		/// <returns>TRUE if the requested Group was found, otherwise FALSE.</returns>
		public bool HasGroup(string name) => this.Settings.HasGroup(name);

		/// <summary>Iterates all loaded files and saves any that are not marked as Read-Only.</summary>
		public bool Save()
		{
			bool result = false;
			if (Count == 0) return false;
			// The first file in the order is always the internal static configuration, if it's the only file loaded, then build
			// a new configuration file from the defaults and save that.
			if (Count == 1)
			{
				IniFile newFile = new IniFile();
				newFile.FilePath = DefaultPath + DefaultFileName;
				newFile.AddRange(this._files[0].Groups);
				result = newFile.Save();
			}
			else
			{
				result = true;
				// don't bother with the first file because it's the internal static configuration...
				for (int i = 1; i < Count; i++)
					if (!this[i].ReadOnly) result &= this[i].Save();
			}

			return result;
		}

		/// <summary>
		/// This routine needs to be overridden with an instance-specific implementation for the default configuration file that the
		/// system will use when/if all attempts to locate an external (filesystem) or resource file have failed.
		/// </summary>
		/// <remarks>
		/// Example:
		/// IniFile def = new IniFile();
		/// def.AddRange(new string[] { "Connection", "Settings", "UserInterface", "Style-Default", "Style-AltDefault", "Style-Highlight1", "Style-Highlight2", "Style-Highlight3", "Style-Alert", "Style-Commands", "Style-TextDump" });
		/// def["Connection"].AddRange(new IniLineItem[] {
		///		new IniLineItem("AuthKey", @"", false, "This value must be issued to you by the Cobblestone Server"), // 0x8a79b42c67fce29b12e09b3fd78ec540
		///		new IniLineItem("Url", @"https://dev.cobblestonelondon.ca", false, "Don't Modify!"),
		///		new IniLineItem("ApiPath", @"/api/", false, "Don't Modify!"),
		///	});
		///
		///	def["Settings"].AddRange(new IniLineItem[] {
		///		new IniLineItem("AutoMinimizeConsole", @"no", false, "If turned on, the console window will be left open / visible when the program is opened"),
		///		new IniLineItem("StyleFile", @"Default", false, "Don't Modify!"),
		///		new IniLineItem("Prompt", @"DAC&gt;&nbsp;", false, "Default System Commandline Prompt"),
		///		new IniLineItem("PhoneNbrs", @"[+]?[01]{0,3}[-. ]?[(]?[0-9][0-9][0-9][)]?[-. ]?[0-9][0-9][0-9][-. ]?[0-9][0-9][0-9][0-9]", false, "RegEx Pattern for Phone Numbers"),
		///		new IniLineItem("PostalCodes", @"[ABCEGHJKLMNPRSTVXYabceghjklmnprstvxy][0-9][ABCEGHJKLMNPRSTVWXYZabceghjklmnprstvwxyz][\s.-]?[0-9][ABCEGHJKLMNPRSTVWXYZabceghjklmnprstvwxyz][0-9]", false, "RegEx Pattern for Postal Codes")
		///	});
		///
		///	def["Style-Default"].AddRange(new IniLineItem[] {
		///		new IniColorItem("Color", Color.White, Color.Navy, false, ""),
		///		new IniLineItem("FontFamily", @"Consolas", false, ""),
		///		new IniLineItem("FontSize", @"10.75", false, ""),
		///		new IniLineItem("FontStyle", @"Regular", false, "")
		///	});
		///
		///	def["Style-AltDefault"].AddRange(new IniLineItem[] {
		///		new IniColorItem("Color", Color.LightGray, Color.Navy, false, ""),
		///	});
		///
		///	def["Style-Highlight1"].AddRange(new IniLineItem[] {
		///		new IniColorItem("Color", Color.White, Color.Navy, false, ""),
		///		new IniLineItem("FontFamily", @"Consolas", false, ""),
		///		new IniLineItem("FontSize", @"10.75", false, ""),
		///		new IniLineItem("FontStyle", @"Regular", false, "")
		///	});
		///
		///	def["Style-Highlight2"].AddRange(new IniLineItem[] {
		///		new IniColorItem("Color", Color.Lime, Color.Navy, false, "")
		///	});
		///
		///	def["Style-Highlight3"].AddRange(new IniLineItem[] {
		///		new IniColorItem("Color", Color.White, Color.Navy, false, "")
		///	});
		///
		///	def["Style-Alert"].AddRange(new IniLineItem[] {
		///		new IniColorItem("Color", Color.Coral, Color.Navy, false, "")
		///	});
		///
		///	def["Style-Commands"].AddRange(new IniLineItem[] {
		///		new IniColorItem("Color", Color.White, Color.Green, false, "")
		///	});
		///
		///	def["Style-TextDump"].AddRange(new IniLineItem[] {
		///		new IniColorItem("Color", Color.SkyBlue, Color.Navy, false, ""),
		///		new IniLineItem("FontFamily", @"Consolas", false, ""),
		///		new IniLineItem("FontStyle", @"Italic", false, "")
		///	});

		/// def.ReadOnly = true;
		///	return def;
		/// </remarks>
		protected abstract IniFile DefaultIniFile();

		// Can't implement this because ConfigManagementBase is an Abstract class, left here as a template for children...
		//public static implicit operator ConfigManagementBase(IniFile data) => new ConfigManagementBase(data);
		public static implicit operator IniFile(ConfigManagementBase data) => data.Compile();
		#endregion

		#region Static Methods
		/// <summary>Returns the path to the currently running Executable (.EXE)</summary>
		/// <returns>A string value containing the path to the currently running EXE.</returns>
		public static string ExecutablePath() => ConfigManagementBase.ExecutablePath("");

		/// <summary>Returns the path to the specified file in the currently running Executable's (.EXE) home folder.</summary>
		/// <param name="fileName">A string specifying the name of the file to be encoded to the path.</param>
		/// <returns>A string value containing the path to the currently running EXE and the supplied filename.</returns>
		public static string ExecutablePath(string fileName) =>
			Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + fileName;

		public static string UserSID() =>
			"{" + System.Security.Principal.WindowsIdentity.GetCurrent().User.ToString() + "}";
		#endregion

		//IEnumerator Support
		public IEnumerator<IniFile> GetEnumerator() => this._files.GetEnumerator();

		bool IEnumerator.MoveNext() => (++this._position) < this._files.Count;

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