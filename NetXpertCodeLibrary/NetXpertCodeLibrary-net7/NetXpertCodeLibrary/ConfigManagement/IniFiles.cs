using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace NetXpertCodeLibrary.ConfigManagement
{
	public class IniFile : IniFileBase
	{
		#region Properties
		private const string COMMENT_PATTERN = /* language=regex */ @"^[ \t]*##[ \t]*(?<comment>[^\r\n]*)";
		private const string FILE_PATTERN = /* language=regex */
			@"(?<header>.*)(?<start>~(?:Start|Begin):[ \t]*(?<sparams>[^\r\n]*)[\s]+)(?<body>[\s\S]*)(?<end>~:End)";

		protected string _fileName;
		protected string _header = "";
		#endregion

		#region Constructors
		/// <summary>Creates a new (empty) IniFile object with the specified parameters and filename.</summary>
		/// <param name="fileName">A string specifying the name of the file that this Object manages.</param>
		/// <param name="parameters">An array of string to define the Parameters of the new file.</param>
		public IniFile(string fileName = "", string[] parameters = null) : base()
		{
			if (!string.IsNullOrEmpty(fileName))
			{
				this._fileName = fileName;
				if ((parameters is null) && System.IO.File.Exists(fileName))
					this.Load(fileName);
				else
					this._parameters = (parameters is null) ? new List<string>() : new List<string>(parameters);
			}
		}
		#endregion

		#region Accessors
		/// <summary>Reports on whether the currently defined INI file can be located.</summary>
		public bool FileExists => System.IO.File.Exists(this._fileName);

		/// <summary>Allows getting or setting the default file header text to use.</summary>
		public string Header
		{
			get => this._header;
			set
			{
				if (!ReadOnly)
				{
					List<string> work = new List<string>();
					MatchCollection matches = Regex.Matches(value, COMMENT_PATTERN, RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.ExplicitCapture);
					foreach (Match m in matches)
						if (!Regex.IsMatch(m.Groups["comment"].Value, @"^File Created on: [a-zA-Z]{3,9} [\d]{1,2}, [\d]{4} at [\d]{1,2}:[\d]{2}", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Compiled))
							work.Add("## " + m.Groups["comment"].Value);

					this._header = String.Join("\r\n", work.ToArray()) + "\r\n";
				}
			}
		}

		/// <summary>Gets / Sets the complete File and Path value for this instance.</summary>
		public string File
		{
			get => this._fileName;
			set { if (!ReadOnly) { this._fileName = value; } }
		}

		/// <summary>Gets/Sets only the file NAME portion of the INI file name value defined for this instance.</summary>
		public string FileName
		{
			get => Path.GetFileName(this._fileName);
			set { if (!ReadOnly) { this._fileName = Path.GetFullPath(this._fileName) + value; } }
		}

		/// <summary>Gets/Sets only the file PATH portion of the INI file name value defined for this instance.</summary>
		public string FilePath
		{
			get => Path.GetFullPath(this._fileName);
			set { if (!ReadOnly) { this._fileName = value + Path.GetFileName(this._fileName); } }
		}
		#endregion

		#region Methods
		/// <summary>Parses a string to try and populate this object.</summary>
		/// <param name="source">A string containing a validly formatted text representation of an IniFile object.</param>
		public void Import(string source)
		{
			if (IsValid(source))
			{
				MatchCollection files = Regex.Matches(source, FILE_PATTERN, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.Compiled);
				if (files.Count > 0)
				{
					this.Header = files[0].Groups["header"].Value;

					string body = "\r\n" + files[0].Groups["body"].Value + "\r\n\r\n"; // ensure there's some extra CRLF padding to aid in parsing.
					MatchCollection matches = Regex.Matches(body, IniUtility.GROUP_GROUP_PATTERN, RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Multiline);
					foreach (Match m in matches)
						this.Add(IniGroupItem.Parse(m.Value));

					this._parameters = new List<string>(files[0].Groups["sparams"].Value.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
				}
			}
		}

		/// <summary>Adds (or updates) a collection of IniGroupItems from an IniFile object.</summary>
		/// <param name="file">An IniFile instance whose Group objects are to be imported to this object.</param>
		public void Add(IniFile file)
		{
			if (!ReadOnly)
				foreach (IniGroupItem group in file)
					this[group.FullName] = group;
		}

		/// <summary>Attempts to load the specified INI file (and re-assigns the defined filename if successful).</summary>
		/// <param name="fileName">A string specifying the full path and file name of the ini to try and load.</param>
		/// <returns>TRUE if the file was located and opened, otherwise FALSE.</returns>
		public override bool Load(string fileName = "")
		{
			if (fileName == "") fileName = this._fileName;
			if (!System.IO.File.Exists(fileName)) return false;

			string data = System.IO.File.ReadAllText(fileName);
			this._fileName = fileName;
			this.Import(data);
			return true;
		}

		/// <summary>Saves the current settings out to the locally-defined path+file.</summary>
		/// <returns>TRUE if the operation succeeded, otherwise FALSE.</returns>
		public override bool Save(string fileName = "") 
		{ 
			if (!ReadOnly)
				return this.Save(this._fileName);
			else 
				return false; 
		}

		/// <summary>Saves the current settings out to the specifief filename.</summary>
		/// <param name="fileName">A string specifying the full path and filename to write the settings out to.</param>
		/// <param name="retainBackup">
		/// If set to TRUE, and a file of the specified name exists, a backup of the file will be created (old backups are overwritten),
		/// otherwise the original file is just deleted. This feature is off by default.
		/// </param>
		/// <returns>TRUE if the operation succeeded, otherwise FALSE.</returns>
		public bool Save(string fileName, bool retainBackup = false)
		{
			if (ReadOnly && fileName.Equals(this._fileName, StringComparison.OrdinalIgnoreCase)) return false;
			try
			{
				if (System.IO.File.Exists(fileName))
				{
					if (retainBackup)
					{
						string fn = Path.GetFileNameWithoutExtension(fileName) + ".bak";
						if (System.IO.File.Exists(fn)) { System.IO.File.Delete(fn); }
						System.IO.File.Move(fileName, fn);
					}
					else
						System.IO.File.Delete(fileName);
				}

				System.IO.File.WriteAllText(fileName, this.ToString());
				return true;
			}
			catch { return false; }
		}

		/// <summary>Creates a text representation of the data contained in this object's instance.</summary>
		/// <returns>The contents of this object as a string.</returns>
		public override string ToString() => Header + base.ToString();
		#endregion

		#region Static Methods
		/// <summary>
		/// This function facilitates retrieving a resource file for the configuration instead of going out to the filesystem. With
		/// this capability, default configuration files can be included as resources with the client application and fetched when
		/// required (i.e. on first-execution, application installation, or when not found).
		/// </summary>
		/// <param name="name">The internal resource name of the item to try and load.</param>
		/// <returns>
		/// Null if the routine could not locate the specified resource, otherwise an IniFile object containing whatever
		/// it's able to parse out of what it finds.
		/// </returns>
		public static IniFile FetchResourceFile(string name)
		{
			string data = string.Empty;
			try
			{
				var assembly = System.Reflection.Assembly.GetEntryAssembly();
				using (Stream stream = assembly.GetManifestResourceStream(name)) // ("Cobblestone." + name))
				using (StreamReader sr = new StreamReader(stream))
					data = sr.ReadToEnd();

				IniFile def = new IniFile();
				def._fileName = name;
				def.Import(data);
				//def.FileName = name;
				return def;
			}
			catch { return null; }
		}

		public static bool IsValid(string source) =>
			Regex.IsMatch(source, FILE_PATTERN, RegexOptions.Multiline | RegexOptions.IgnoreCase);
		#endregion
	}
}