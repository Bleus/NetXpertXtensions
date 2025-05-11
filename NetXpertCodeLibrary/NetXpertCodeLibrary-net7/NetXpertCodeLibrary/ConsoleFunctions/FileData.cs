using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NetXpertCodeLibrary.Extensions;
using static NetXpertExtensions.NetXpertExtensions;

namespace NetXpertCodeLibrary.ConsoleFunctions
{
	/// <summary>Used to hold and manage File information.</summary>
	public class FileData
	{
		#region Properties
		private FileInfo _fileInfo = null;
		private ulong _fileSize = 0;
		#endregion

		#region Constructors
		public FileData(string fullFileName, CliColor color = null)
		{
			FullFileName = fullFileName;
			Color = (color is null) ? CliColor.CaptureConsole() : color;
		}

		public FileData(string fileName, string path, CliColor color = null)
		{
			FileName = fileName;
			Path = path;
			Color = (color is null) ? CliColor.CaptureConsole() : color;
		}
		#endregion

		#region Operators
		public static implicit operator FileData(FileInfo source) => new FileData( source.FullName );
		public static implicit operator FileInfo(FileData source) => source.FileInfo;
		#endregion

		#region Accessors
		public string FileName { get; set; }

		public string Path { get; set; }

		public CliColor Color { get; set; }

		public ulong Size
		{
			get => (FileInfo is null) ? _fileSize : (ulong)FileInfo.Length;
			set => _fileSize = Math.Max( 0, value );
		}

		public string SizeToString =>
			(Size > 2000000) ? (Size / 1048576).ToString( "N1" ) + " MB" : Size.ToString( "N0" );

		public string FullFileName
		{
			get => this.Path + "\\" + this.FileName;
			set
			{
				this.Path = System.IO.Path.GetDirectoryName( value );
				this.FileName = System.IO.Path.GetFileName( value );
			}
		}

		public FileInfo FileInfo
		{
			get
			{
				if ( (_fileInfo is null) && Exists() ) this._fileInfo = new FileInfo( FullFileName );
				return this._fileInfo;
			}
			set
			{
				if ( (value is null) && Exists() ) this._fileInfo = new FileInfo( FullFileName );
				if ( value != null ) this._fileInfo = value;
			}
		}

		public DateTime LastAccessed => (FileInfo is null) ? new DateTime( 2000, 01, 01, 0, 0, 1 ) : FileInfo.LastAccessTime;

		public FileAttributes Attributes
		{
			get => FileInfo.Attributes;
			set => FileInfo.Attributes = value;
		}
		#endregion

		#region Methods
		public bool Exists() => File.Exists(FullFileName);

		/// <summary>Tests a filename string against a supplied file pattern and reports if the filename comports with the pattern.</summary>
		public bool MatchesPattern(string fileNamePattern) =>
			Regex.IsMatch(this.FileName, CompileFilePatternToRegex(fileNamePattern), RegexOptions.IgnoreCase);

		public override string ToString() =>
			FullFileName + " (" + SizeToString + " bytes) " + LastAccessed.ToString( "YYYY-mm-dd HH:mm:ss" );

		public static string CompileFilePatternToRegex(string fileNamePattern)
		{
			if (string.IsNullOrEmpty(fileNamePattern.Trim())) return @"^([a-z \d_]*)$";
			if ( fileNamePattern.Trim() == "*.*" ) return @"(.*)";
			if (ValidateFilePattern(fileNamePattern))
			{
				fileNamePattern = fileNamePattern.Replace(".", @"\."); // Periods in the pattern need to be interpretted literally.
				fileNamePattern = fileNamePattern.Replace("?", @"([a-z_\d ])");  // Replace "?" in the pattern with the regex equivalent.
				fileNamePattern = fileNamePattern.Replace("*", @"([a-z_\d ]*)"); // Replace "*" in the pattern with the regex equivalent.
				if (fileNamePattern.EndsWith(".")) fileNamePattern += @"[\S]*";
				return "^" + fileNamePattern + "$";
			}
			return "";
		}

		/// <summary>Tests a provided string to see if it's a valid filename pattern.</summary>
		public static bool ValidateFilePattern(string test) =>
			Regex.IsMatch(test, @"([ a-z_\d*?]+[.]?)+([a-z_\d ]+)?", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

		public static string PromptParser( string source, PromptPrimitive parent, Ranks defaultUserRank = Ranks.None )
		{
			if ( string.IsNullOrWhiteSpace( source ) && (parent is null) ) return /* language=regex */ @"\$DISK\[(?:[a-z]{4,})\]";

			if ( source.Trim().Equals( "--help", StringComparison.OrdinalIgnoreCase ) && (parent is null) )
				return @"{7,7}&bull; {6}$disk{3}[{E}drive{3}|{E}path{3}|{E}full{3,rn}]";

			string path = Environment.CurrentDirectory;
			// UserInfo user = (parent.Data is null) ? UserInfo.DefaultUser() : parent.GetDataAs<UserInfo>();
			MatchCollection matches = Regex.Matches( source, @"(?<cmd>\$disk\[(?<opt>drive|path|full)\])", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase );
			foreach ( Match m in matches )
			{
				if ( m.Groups[ "cmd" ].Success && m.Groups[ "opt" ].Success )
				{
					string replace = "$disk[err]";
					switch ( m.Groups[ "opt" ].Value.ToLower() )
					{
						case "drive": replace = Directory.GetDirectoryRoot(path).Trim(new char[] { '\\' }); break;
						case "path": replace = System.IO.Path.GetFullPath(path); break;
						case "full": replace = path + System.IO.Path.GetFileName( System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName ); break;
					}
					source = source.Replace( m.Value, replace );
				}
			}
			return source;
		}
		#endregion
	}

	/// <summary>Used to store and manage a collection of files.</summary>
	public class FileDataCollection : IEnumerator<FileData>
	{
		#region Properties
		List<FileData> _files = new List<FileData>();
		private int _position = 0;
		#endregion

		#region Constructors
		public FileDataCollection() { }

		public FileDataCollection( string homePath )
		{
			if ( Directory.Exists( homePath ) )
			{
				string[] files = Directory.GetFiles( homePath );
				foreach (string f in files)
					Add( new FileData( f ) );
			}
		}
		#endregion

		#region Accessors
		public FileData this[int index] => _files[index];

		public FileData this[string fileName]
		{
			get
			{
				int i = -1; while ((++i < Count) && !this[i].FullFileName.Equals(fileName, StringComparison.OrdinalIgnoreCase)) ;
				if (i < 0)
					i = IndexOf(fileName);

				return (i < 0) ? null : this[i];
			}
		}

		public int Count => _files.Count;

		public ulong TotalSize
		{
			get
			{
				ulong result = 0;
				foreach ( FileData file in this._files )
					result += file.Size;

				return result;
			}
		}

		FileData IEnumerator<FileData>.Current => this[this._position];

		object IEnumerator.Current => this[this._position];
		#endregion

		#region Methods
		protected int IndexOf(string fileName)
		{
			int i = -1; while ((++i < Count) && !this[i].FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase)) ;
			return (i < Count) ? i : -1;
		}

		protected int IndexOf(FileData fD)
		{
			int i = -1; while ((++i < Count) && !this[i].FullFileName.Equals(fD.FullFileName, StringComparison.OrdinalIgnoreCase)) ;
			return (i < Count) ? i : -1;
		}

		public void Add(FileData nfd, bool sortAfter = false)
		{
			int i = IndexOf( nfd );
			if ( i < 0 )
			{
				_files.Add( nfd );
				if ( sortAfter ) this.Sort();
			}
		}

		public void Add(string fileName, CliColor color = null, bool sortAfter = false) =>
			this.Add(new FileData(fileName, color), sortAfter);

		public void Add( DirectoryInfo dir ) =>
			this.AddRange( dir.GetFiles() );

		public void AddRange( FileData[] files )
		{
			foreach ( FileData fd in files )
				this.Add( fd );
		}

		public void AddRange( FileInfo[] files )
		{
			foreach ( FileInfo fi in files )
				this.Add( (FileData)fi );
		}

		public void Remove(FileData nfd)
		{
			int i = IndexOf(nfd);
			if (i >= 0) this._files.RemoveAt(i);
		}

		public void Sort() =>
			this._files.Sort((x, y) => x.FileName.CompareTo(y.FileName));

		public void Sort(Comparison<FileData> sort) => this._files.Sort(sort);

		public void Sort(IComparer<FileData> sort) => this._files.Sort(sort);

		public void Sort(int index, int count, IComparer<FileData> sort) => this._files.Sort(index, count, sort);

		public FileData[] ToArray() => this._files.ToArray();

		public FileData[] ToArray(string pattern)
		{
			if (!FileData.ValidateFilePattern(pattern)) return this._files.ToArray();

			Regex regex = new Regex(FileData.CompileFilePatternToRegex(pattern), RegexOptions.IgnoreCase | RegexOptions.Compiled);
			List<FileData> results = new List<FileData>();
			foreach (FileData fd in this._files)
				if (regex.IsMatch(fd.FileName)) results.Add(fd);

			return results.ToArray();
		}

		public bool Contains( string name, bool caseSensitive = true ) =>
			!(GetFile( name, caseSensitive ) is null);

		public FileData GetFile( string name, bool caseSensitive = true )
		{
			int i = -1;
			while ( (++i < Count) && !this._files[ i ].FileName.Equals( name, caseSensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal ) ) ;
			return (i < Count) ? this._files[i] : null;
		}

		// IEnumerator support functions
		public IEnumerator<FileData> GetEnumerator() => this._files.GetEnumerator();

		bool IEnumerator.MoveNext() => (++this._position) < this.Count;

		void IEnumerator.Reset() => this._position = 0;
		#endregion

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

	/// <summary>An internally recursive class that facilitates dynamically navigating/accessing/managing a complete folder structure.</summary>
	public class FolderCollection
	{
		#region Properties
		protected string _myPath = "";
		#endregion

		#region Constructors
		public FolderCollection( string homePath = @"C:\" )
		{
			MyPath = homePath;
			if ( _myPath.Length == 0 )
				throw new DirectoryNotFoundException( homePath );
		}
		#endregion

		#region Accessors
		/// <summary>Creates a new FolderCollection for the specified directory.</summary>
		/// <param name="name">A String specifying the directory to encapsulate. This MUST be an actual directory, spelling counts, case doesn't!</param>
		/// <returns>A NEW FolderCollection object referencing the specified folder.</returns>
		/// <remarks>You can pass a double-dot ("..") value for the directory name to obtain the parent folder. Note, however, if this is 
		/// already the root folder, doing so will return NULL instead.</remarks>
		public FolderCollection this[ string name ]
		{
			get
			{
				if (name.Equals("..")) // Go up!
				{
					if (!Regex.IsMatch( _myPath, @"^([A-Za-z]:)?\\$", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase))
						return new FolderCollection( Directory.GetParent( MyPath ).FullName );

					return null;
				}

				string[] dirNames = Directory.GetDirectories( MyPath ); // this automatically leaves "." and ".." out!
				if (dirNames.Length > 0)
				{
					int i = -1; while ( (++i < dirNames.Length) && !dirNames[ i ].Equals( MyPath + name, StringComparison.OrdinalIgnoreCase ) ) ;
					if ( i < dirNames.Length ) return new FolderCollection( dirNames[ i ] );
				}
				return null;
				throw new DirectoryNotFoundException( MyPath + name );
			}
		}

		/// <summary>Gets/Sets the Path pointed to by this object.</summary>
		public string MyPath
		{
			get => this._myPath.TrimEnd( new char[] { '\\' } ) + "\\";
			set
			{
				if ( ValidatePath( value ) && Directory.Exists( value ) )
					this._myPath = value.TrimEnd('\\');
			}
		}

		/// <summary>Creates a FileDataCollection object containing the file information of every file in this object's directory.</summary>
		public FileDataCollection Files =>
			new FileDataCollection( MyPath );

		/// <summary>Returns an array of Strings containing the full path name of all subdirectories in this object's directory.</summary>
		/// <remarks>Does NOT include referential "directories" (i.e. "." and "..")!</remarks>
		public string[] Directories =>
			Directory.GetDirectories( MyPath );
		#endregion

		#region Methods
		#region File Operations
		protected int IndexOfFile( string fileName, bool matchCase = true )
		{
			int i = -1; while ( (++i < Files.Count) && !Files[ i ].FileName.Equals( fileName, matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase ) ) ;
			return (i < Files.Count) ? i : -1;
		}

		/// <summary>Renames a specified file.</summary>
		/// <param name="currentName">The current name of the file to be renamed.</param>
		/// <param name="newName">The new name for the file.</param>
		/// <param name="silentExceptions">If TRUE (default) any Exceptions generated by the operation are silenced, otherwise they're thrown.</param>
		/// <returns>TRUE if the operation was successful. FALSE if the operation failed, and Exceptions are silenced.</returns>
		/// <remarks>Path information is not supported in either the currentName, or the newName.</remarks>
		public bool RenameFile( string currentName, string newName, bool silentExceptions = true )
		{
			if ( !string.IsNullOrWhiteSpace( currentName ) && !string.IsNullOrWhiteSpace( newName ) )
			{
				int i = IndexOfFile( currentName );
				if ( (i >= 0) && Regex.IsMatch( newName, @"^([-~\w!# $%^&()\[\]{}]+(\.(?=[^\\]))?)+$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture ) )
					try { Files[ i ].FileInfo.MoveTo( newName ); return true; }
					catch (Exception e)
					{
						if ( !silentExceptions )
							throw e;
					}
			}
			return false;
		}

		/// <summary>Permanently deletes the specified file.</summary>
		/// <param name="name">The name of the file to delete.</param>
		/// <param name="silentExceptions">If TRUE (default) any Exceptions generated by the operation are silenced, otherwise they're thrown.</param>
		/// <returns>TRUE if the operation was successful. FALSE if the operation failed, and Exceptions are silenced.</returns>
		/// <remarks>Path information is not supported in the name.</remarks>
		public bool DeleteFile( string name, bool silentExceptions = true )
		{
			if (!string.IsNullOrWhiteSpace( name ))
			{
				int i = IndexOfFile( name );
				if ( i >= 0 )
					try { Files[ i ].FileInfo.Delete(); return true; }
					catch (Exception e)
					{
						if ( !silentExceptions )
							throw e;
					}
			}
			return false;
		}

		/// <summary>Moves a file to a specified destination.</summary>
		/// <param name="currentName">The current name of the file to be renamed.</param>
		/// <param name="destination">The path and filename of the destination.</param>
		/// <param name="silentExceptions">If TRUE (default) any Exceptions generated by the operation are silenced, otherwise they're thrown.</param>
		/// <returns>TRUE if the operation was successful. FALSE if the operation failed, and Exceptions are silenced.</returns>
		/// <remarks>Path information is not supported in the currentName, but is allowed in the destination.</remarks>
		public bool MoveFile( string currentName, string destination, bool silentExceptions = true )
		{
			if ( !string.IsNullOrWhiteSpace( currentName ) && !string.IsNullOrWhiteSpace( destination ) )
			{
				int i = IndexOfFile( currentName );
				if ( (i >= 0) && ValidatePath( destination ) )
					try { Files[ i ].FileInfo.MoveTo( destination ); return true; }
					catch ( Exception e )
					{
						if ( !silentExceptions )
							throw e;
					}
			}
			return false;
		}

		/// <summary>Copies a file to a specified destination.</summary>
		/// <param name="name">The name of the file to copy.</param>
		/// <param name="destination">The path and filename of the destination.</param>
		/// <param name="silentExceptions">If TRUE (default) any Exceptions generated by the operation are silenced, otherwise they're thrown.</param>
		/// <returns>TRUE if the operation was successful. FALSE if the operation failed, and Exceptions are silenced.</returns>
		/// <remarks>Path information is not supported in the name, but is allowed in the destination.</remarks>
		public bool CopyFile( string name, string destination, bool silentExceptions = true )
		{
			if ( !string.IsNullOrWhiteSpace( name ) && !string.IsNullOrWhiteSpace( destination ) )
			{
				int i = IndexOfFile( name );
				if ( (i >= 0) && ValidatePath( destination ) )
					try { Files[ i ].FileInfo.CopyTo( destination ); return true; }
					catch ( Exception e )
					{
						if ( !silentExceptions )
							throw e;
					}
			}
			return false;
		}
		#endregion

		#region Directory Operations
		protected int IndexOfDir( string dirName, bool matchCase = true )
		{
			int i = -1; while ( (i < Directories.Length) && !Path.GetFileName( Directories[ i ] ).Equals( dirName, matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase ) ) ;
			return (i < Directories.Length) ? i : -1;
		}

		public bool RenameDir( string dirName, string newName, bool silentExceptions = true)
		{
			if ( !string.IsNullOrWhiteSpace( dirName ) && !string.IsNullOrWhiteSpace( newName ) )
			{
				int i = IndexOfDir( dirName );
				if ( (i >= 0) && Regex.IsMatch( newName, @"^([-~\w!# $%^&()\[\]{}]+(\.(?=[^\\]))?)+$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture ) )
					try { Directory.Move( MyPath + dirName, MyPath + newName ); return true; }
					catch ( Exception e )
					{
						if ( !silentExceptions )
							throw e;
					}
			}
			return false;
		}
		#endregion

		/// <summary>Performs pattern testing on a string to see if it's in a form recognizable as an absolute path.</summary>
		/// <param name="test">The string to test.</param>
		/// <param name="testExists">If TRUE, this also verifies that the specified path exists.</param>
		/// <returns>TRUE if the contents of the passed string are valid, and, if requested, the path exists.</returns>
		/// <remarks>If the test string contains multiple lines, they're separated and processed as a set of multiple paths 
		/// returning TRUE only if ALL paths pass validation.</remarks>
		/// <seealso cref="ValidatePaths(string[], bool)"/>
		public static bool ValidatePath( string test, bool testExists = false )
		{
			bool result = !string.IsNullOrWhiteSpace(test);

			if ( Regex.IsMatch( test, @"[\r\n]" ) )
			{
				KeyValuePair<string, bool>[] tests = ValidatePaths( test.Split( new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries ) );
				int i = -1; while ( result && (i < tests.Length) ) result = tests[ i ].Value;
			}
			else
			{
				if ( Regex.IsMatch( test, @"^[\x22'][^\x22']+[\x22']$" ) )
					test = test.Unwrap( StripOuterOptions.DoubleQuotes | StripOuterOptions.SingleQuotes );

				// Validate the general form and content of the string...
				string drivePattern = /* language=regex */ @"^(?<root>(?<drive>[A-Z]:(?:\.{1,2}[\/\\]|[\/\\])?)|(?<noDrive>[\/\\]{1,2}|\.{1,2}[\/\\]))?",
					   pattern = drivePattern + /* language=regex */ @"(?<path>[^\x00-\x1A|*?\t\v\f\r\n+\/,;\x22'`\\:<>=[\]]+[\/\\]?|[\\])+$";

				result &= Regex.IsMatch( test, pattern, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture );

				// Ensure that unwanted patterns that aren't caught above, aren't present...
				pattern = drivePattern + /* language=regex */
					@"(?:[^\/\\. ]|[^\/. \\][\/. \\][^\/. \\]|[\/\\]$)*[^\x00-\x1A|*?\s+,;\x22'`:<.>=[\]]$";

				result &= Regex.IsMatch( test, pattern ) && (!testExists || Directory.Exists( test ));
			}

			return result;
		}

		/// <summary>Performs pattern testing on an array of strings to see if they're valid Windows paths.</summary>
		/// <param name="paths">An array of strings containing paths to test.</param>
		/// <param name="testExists">If TRUE, it will also verifies that each specified path exists.</param>
		/// <returns>An array of KeyValuePair<string,bool> objects containing the supplied paths and the result of their individual validations.</string></returns>
		public static KeyValuePair<string,bool>[] ValidatePaths( string[] paths, bool testExists = false)
		{
			List<KeyValuePair<string, bool>> results = new List<KeyValuePair<string, bool>>();
			foreach ( string s in paths )
				results.Add( new KeyValuePair<string, bool>( s, ValidatePath( s, testExists ) ) );

			return results.ToArray();
		}
		#endregion
	}
}
