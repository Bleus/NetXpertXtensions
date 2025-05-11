using System.Reflection;
using System.Text.RegularExpressions;
using NetXpertExtensions;
using NetXpertExtensions.Classes;
using IniFileManagement.Values;
using System.Data;

namespace IniFileManagement
{
	/*
	 
	Sample File:

	## {AppName} Default Settings File
	## WARNING: Modifying any of the values below *may* result in loss of functionality.
	~PARAMS:
		Version=1.0.0.0
		EncryptByDefault=on
		Created=2025-01-01 12:00:00
		Creator=brett
		Modified=2025-01-01 12:00:00
		Modifier=brett
		SerialNo=UdnK4WkTSz69Cx/XLNPPyNYy3I3PIL7goQcfZFipuZtH6wmnYXrSa7Db8202kueP/B7QjWotYl5KfXiD/0UaAtEf2mFjlozu1jF45o//I5NevKC4QtV1o6FoXp+rfpwILwn/DKE3KXM7Wsz416Jcmv2uWbNNQuluR7CSCqP9Stw=
		ReadOnly=false
	~:END

	## System-wide Settings
	~CONFIG:
	[GroupName1]: "Description"
	{
		## URL to use for submitting web requests.
		Server=https://www.botanybay.net
		Path=/pa/					## Path on target server to the request-fulfilling script.
		## Name of the script to use, with parameter list specifying how to make the request:
		Target=MySQL-Portal.php?user=%user%&pass=%pass%&db=%database%&query=%query%
		Database=Brett-Taxes		## The default table to reference when a connection is established.
		UserId="username"				## Username to gain access to the server.
		Password="password"			## Password for the given user.
		Timeout=15000				## Defines the amount of time that will be alotted for a query to be returned.
	}

	[GroupName2]: "Description"
	{
		## More Settings Here...
	}
	~:END

	## User-specific Settings:
	~SETTINGS:
	[User:{uid}]: "{userName}"
	{
		## User's Settings
	}

	[User:{uid}]: "{userName}"
	{
		## User's Settings
	}
	~:END
	 */

	/// <summary>The main/base class for managing INI files.</summary>
	public sealed partial class IniFileMgmt
	{
		#region Properties
		private const int PARAM_GROUP_INDEX = 0;

		private string _source = "";
		private string _filename = DEFAULT_FILE;
		private readonly List<IniBlockMgmt> _blocks = [];

		public static readonly string DEFAULT_FILE =
			$"{MiscellaneousExtensions.LocalExecutablePath}{MiscellaneousExtensions.ExecutableName}.ini";

		public static DateTime DATE_NOT_SET => new( 2000, 01, 01, 00, 00, 01 );

		/* language=regex */
		//private const string FILE_PATTERN = @"^(?<header>[\s\S]*)(?<start>(^|[\r\n])[\s]*~(?:Settings|Config(uration)?):[ \t]*(?<params>[^\r\n]*)[\s]+)(?<body>[\s\S]*)(?<end>[\r\n][\s]*~:End)(?<footer>[\s\S]*)$";
		//private const string FILE_PATTERN = @"^(?<header>.*)(?<section>(?<preamble>.*?)((~(?<type>Params|Settings|Config(uration)?):)(?<data>.*?)(~:End)))(?<trailer>.*)$";
		public const string FILE_PATTERN = @"(?<block>(?<preamble>.*?)(?<type>~(params|config(uration)?|settings):)(?<body>.*?)~:end)";
		#endregion

		#region Constructors
		private IniFileMgmt( bool IsEmpty = false )
		{
			if (!IsEmpty) this._blocks.Add( IniParamMgmt.Create( this ) );
		}

		public IniFileMgmt( string fileName, bool neaten = false )
		{
			this.FileName = fileName;
			if (this._filename != fileName)
				throw new ArgumentException( $"You did not provide a valid filename! (\x22{fileName}\x22)" );

			IniFileMgmt? import;

			if (MiscellaneousExtensions.ValidateWindowsFilePath( fileName, true ))
				import = LoadIniFile( fileName );
			else
			{
				string assembly = Assembly.GetEntryAssembly()?.GetName().Name;
				import = Parse( MiscellaneousExtensions.FetchAssemblyResourceFile( $"{assembly}.{Path.GetFileName( fileName )}" ) );
				import.FileName = MiscellaneousExtensions.LocalExecutablePath + fileName;
				import.Save( false, 0, 0 );
			}

			if (import is not null)
			{
				this._source = import._source;
				this._filename = import.FileName;
				this._blocks = import._blocks;
				this.NeatenOutput = neaten;
			}
			else
				throw new InvalidOperationException( $"Unable to instantiate this object from the supplied source (\x22{fileName}\x22)" );
		}
		#endregion

		#region Accessors
		private IniParamMgmt Params => (IniParamMgmt)this._blocks[ PARAM_GROUP_INDEX ];

		public IniBlockMgmt AppSettings => this[ IniBlockMgmt.BlockTypes.Config ];

		public IniBlockMgmt UserSettings => this[ IniBlockMgmt.BlockTypes.Settings ];

		public IniBlockMgmt? this[ IniBlockMgmt.BlockTypes block ]
		{
			get
			{
				if (block == IniBlockMgmt.BlockTypes.Params) return this.Params;

				int i = IndexOf( block );
				return (i < 0) ? null : this._blocks[ i ];
			}
		}

		public DateTime Created => this.Params.Created;

		public DateTime Modified => this.Params.Modified;

		public IniSerialNumber? SerialNbr => this.Params.SerialNumber;

		public IniEncryptionKey? EncryptionKey => this.Params.EncryptionKey;

		public bool EncryptByDefault
		{
			get => this.Params.EncryptByDefault;
			set => this.Params.EncryptByDefault = value;
		}

		public VersionMgmt Version
		{
			get => this.Params.Version;
			set => this.Params.Version = value;
		}

		public bool ReadOnly
		{
			// Need to report false here if the params block hasn't been created/loaded/parsed etc..
			get => this._blocks.Count == 0 ? false : this.Params.ReadOnly;
			set => this.Params.ReadOnly = value;
		}

		public bool NeatenOutput { get; set; } = true;

		public string FileName
		{
			get => this._filename;
			set
			{
				if ( !ReadOnly && ValidateFileName( value ))
					this._filename = value;
			}
		}

		public static IniFileMgmt Empty => new( true );
		#endregion

		#region Methods
		private int IndexOf( IniBlockMgmt.BlockTypes block )
		{
			int i = -1; while ((++i < this._blocks.Count) && (this._blocks[ i ].BlockType != block)) ;
			return i < this._blocks.Count ? i : -1;
		}

		public string Compile( uint increment = 0, int depth = -1 )
		{
			if (increment > 0)
				this.Version.Increment( increment, depth );

			string result = this.Params is null ? string.Empty : this.Params.ToString();

			foreach (IniBlockMgmt block in this._blocks)
				if (block is not null)
				{
					string b = block.ToString();
					result += result.Length > 2 ? (result[ ^3 ] == '\n' ? b : $"\r\n{b}") : b;
				}

			return result;
		}

		public static IniFileMgmt? LoadIniFile( string fileName )
		{
			IniFileMgmt? result = null;
			if (ValidateFileName( fileName ) && MiscellaneousExtensions.ValidateWindowsFilePath( fileName, true ))
			{
				result = Parse( File.ReadAllText( fileName ) );
				result._filename = fileName;
			}

			return result;
		}

		public void Add( IniBlockMgmt.BlockTypes type, IniGroupMgmt group )
		{
			if (ReadOnly) throw ReadOnlyException();

			switch (type)
			{
				case IniBlockMgmt.BlockTypes.Params:
					this.Params.AddRange( group.ToArray() );
					break;
				case IniBlockMgmt.BlockTypes.Unknown:
					throw new ArgumentException( "You cannot assign a group to an Unknown block." );
				default:
					int i = IndexOf( type );
					if (i < 0)
						this._blocks.Add( new IniBlockMgmt( this, type, group ) );
					else
						this._blocks[ i ].Add( group );
					break;
			}
		}

		public void MergeBlock( IniBlockMgmt block )
		{
			if (ReadOnly) throw ReadOnlyException();

			int i = IndexOf( block.BlockType );
			if (i < 0)
				this._blocks.Add( block );
			else
				this._blocks[ i ].MergeBlock( block );
		}

		public IniGroupMgmt? Remove( IniBlockMgmt.BlockTypes blockName, string name )
		{
			int i = IndexOf( blockName );
			return (i < 0) ? null : this._blocks[ i ].Remove( name );
		}

		public bool Save( bool backupExisting = false, uint increment = 1, int depth = -1 ) =>
			Save( this._filename, increment, depth, backupExisting );

		public bool Save( string fileName, uint increment = 1, int depth = -1, bool backupExisting = false )
		{
			if (ReadOnly) throw ReadOnlyException();

			try
			{
				if (ValidateFileName( fileName ))
				{
					if (File.Exists( fileName ))
					{
						if (backupExisting)
						{
							string
								folder = Path.GetFullPath( fileName ),
								filename = Path.GetFileNameWithoutExtension( fileName );
							//backupName = Path.Combine( folder, fileName, DateTime.Now.ToString( "yyyy-MM-dd(HHmmss)" ), ".ini" );

							File.Move( fileName, $"{folder}{filename}.{DateTime.Now:yyyy-MM-dd(HHmmss)}.ini" );
						}
						else
							File.Delete( fileName );
					}

					this._filename = fileName;
					string compiledFile = this.Compile( 1 );
					File.WriteAllText( fileName, compiledFile );
					return true;
				}
			}
			catch (Exception e) { Console.WriteLine( e.Message ); }

			return false;
		}

		public bool HasGroup( IniBlockMgmt.BlockTypes type, string groupName )
		{
			int i = IndexOf( type );
			return (i >= 0) && this._blocks[ i ].HasGroup( groupName );
		}

		public bool HasGroup( string groupName )
		{
			if (groupName.Equals( "params", StringComparison.OrdinalIgnoreCase )) return this.Params is not null;

			var types = new IniBlockMgmt.BlockTypes[] { IniBlockMgmt.BlockTypes.Settings, IniBlockMgmt.BlockTypes.Config };
			foreach (var t in types)
				if (this[ t ].HasGroup( groupName )) return true;

			return false;
		}

		public static bool ValidateFileName( string proposedName ) =>
			MiscellaneousExtensions.ValidateWindowsFilePath( proposedName ) && Regex.IsMatch( proposedName, @"[.]ini$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant );

		/// <summary>Attempts to parse a string containing a configuration file.</summary>
		/// <param name="source">A string containing the plain-text configuration data to parse.</param>
		/// <returns>A new <seealso cref="IniFileMgmt"/> object containing all of the configuration information that could be parsed from the <paramref name="source"/> string. </returns>
		public static IniFileMgmt Parse( string source )
		{
			IniFileMgmt result = new();

			MatchCollection matches = IniFileParser().Matches( source );
			if (matches.Count > 0)
			{
				result._source = source;
				foreach (Match match in matches.Cast<Match>())
					if (match.Success)
					{
						IniBlockMgmt? block = IniBlockMgmt.Parse( result, match.Value );
						if (block is not null)
						{
							if ( block.BlockType == IniBlockMgmt.BlockTypes.Params && result.HasGroup("Params") )
								result.Params.MergeBlock( block );
							else
								result._blocks.Add( block );
						}
					}
			}

			return result;
		}

		public static IniFileMgmt CreateIniFile()
		{
			string template = MiscellaneousExtensions.FetchAssemblyResourceFile( $"{Assembly.GetCallingAssembly().ExtractName()}.DefaultFileTemplate.ini" );
			return IniFileMgmt.Parse( template );
		}

		public static ReadOnlyException ReadOnlyException( string message = "" ) =>
			new( string.IsNullOrWhiteSpace(message) ? $"The configuration file is locked and cannot be modified." : message );

		[GeneratedRegex( FILE_PATTERN, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Singleline )]
		public static partial Regex IniFileParser();

		[GeneratedRegex( @"[\r\n][\s]*\[[a-zA-Z]+[\w-]*\]:[^\r\n]*[\s\S]*?[\r\n][\s]*[{][\s\S]*?[\r\n][\s]*[}]", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Singleline )]
		private static partial Regex IniGroupParser();
		#endregion
	}
}