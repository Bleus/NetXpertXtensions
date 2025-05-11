using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using NetXpertCodeLibrary.Extensions;
using NetXpertCodeLibrary.LanguageManager;

namespace NetXpertCodeLibrary.ConsoleFunctions
{
	public static class Applets
	{
		public enum OperationalStates { None, Idle, Running, Complete, CompleteWithErrors, Cancelled, Incomplete, IncompleteWithErrors };

		#region Static Methods
		/// <summary>Returns the first, non-abstract, ancestor Type of the provided class that's above AppletFoundation.</summary>
		/// <returns>The highest non-abstrace ancestor class of the provided Type or NULL if none exist.</returns>
		public static Type GetDerivedType(Type test)
		{
			Type result = null;
			if (IsApplet(test)) // Ensure that the supplied type IS derived from AppletFoundation.
			{
				result = test; // Start at the top...
				while ((result != typeof(AppletFoundation)) && result.IsAbstract) result = result.BaseType;
			}
			return ((result is null) || result.IsAbstract || (result == typeof(AppletFoundation))) ? null : result;
			//(test == typeof(Object)) ? null : (test.BaseType == typeof(AppletFoundation)) ? test : GetDerivedType(test.BaseType);
		}

		/// <summary>Reports whether a specified object is derived from the AppletFoundation class.</summary>
		public static bool IsApplet(dynamic obj) => IsApplet(obj.GetType());

		/// <summary>Reports whether a specified Type is derived from the AppletFoundation class.</summary>
		public static bool IsApplet(Type objType) =>
			(objType is null) || (objType == typeof(Object)) ? false : (objType == typeof(AppletFoundation) ? true : IsApplet(objType.BaseType));

		/// <summary>Tests a specified Type to see if it's derived from AppletFoundation and throws an Exception if it isn't.</summary>
		public static void ValidateType(Type type) { if (!IsApplet(type)) { throw InvalidType(type); } }

		/// <summary>Provides a pre-fabricated global Exception for when an attempt to check for an Applet type fails</summary>
		public static ArgumentException InvalidType(Type type) => InvalidType(type.Name);

		/// <summary>Provides a pre-fabricated global Exception for when an attempt to check for an Applet type fails</summary>
		public static ArgumentException InvalidType(string name) => new ArgumentException("The specified source object is not a valid Applet type (\"" + name + "\")");

		/// <summary>Deconstructs the information contained within an Exception.</summary>
		public static OperationalStates ExceptionDump( Exception e, bool debugMode = false )
		{
			debugMode |= CommandLine.LaunchCommand().HasSwitch( "debug" );
			void PrintException( Exception ie, int indent = 3, string title = "Exception Caught" )
			{
				bool innerExc = !(ie.InnerException is null);

				string prefix = Regex.IsMatch( title, @"^Inner ", RegexOptions.IgnoreCase ) ? "└─►" : "  ";
				Con.Tec( "{9,$1}$2{F4}$3 {04}&lbrace;{e4}$4{04}&rbrace;{74}:{,rn}", new object[] { indent - 3, prefix, title, ie.GetType().Name } );

				prefix = (innerExc ? "│" : " ") + "└" + (debugMode ? "┬" : "─") + "─►";
				Con.Tec( "{9,$1}$2{F3}\"{E3}$3{F3}\"{,rn} ", new object[] { indent, prefix, ie.Message.Replace("\r\n", "{03}[{13}└►{03}]{E3}" ) } );

				if ( debugMode) 
				{
					int count = ie.StackTraceDetails().Count;
					TextElementRule rule1 = new TextElementRule( /* parameters */ /* language=regex */
						@"([(,]?)([\w&]+)([ ]+)", "$1{D}$2{7}$3" );
					TextElementRule rule2 = new TextElementRule( /* fnName */ /* language=regex */
						@"([\w]+[.]+)+([\w]+)(?:(\[)([^\]]*)(\]))?\(([^\)]*)\)$", "{A}$1{6}$2{9}$3{F}$4{9}$5({7}$6{9})" );

					for ( int j = 0; j < count; )
					{

						ExceptionStackTraceLine estl = ie.StackTraceDetails()[ j ];
						string line = rule2.ApplyRule( rule1.ApplyRule( estl.Procedure ).Replace( ",", "{9}," ) ).Replace( ".", "{7}." );

						Con.Tec( "{9,$1}$2", new object[] { indent, (innerExc ? "│" : " ") } );
						Con.Tec( "{9} $2$3─► {A}$1{,rn} ", new object[] { line, ++j < count ? "├" : "└", string.IsNullOrWhiteSpace(estl.Module) ? "─" : "┬" } );

						if ( !string.IsNullOrWhiteSpace(estl.Module) )
						{
							Con.Tec( "{9,$1}$2", new object[] { indent, (innerExc ? "│" : " ") } );
							Con.Tec( "{9} $1", (j<count ? "│" : " ") );
							Con.Tec( "{9}└──► {E}in {F}\"{B}$1{F}\" {E}on line{7}: {0F} $2 {,rn} ",
								new object[] { estl.ModuleFile, estl.AtLine } );
						}
					}
				}

				if ( innerExc )
				{
					Con.Tec( "{9,$1rn}│", indent );
					PrintException( ie.InnerException, indent + 3, "Inner Exception" );
				}
			}

			Console.WriteLine();
			PrintException( e, 3 );

			return OperationalStates.IncompleteWithErrors;
		}

		/// <summary>Deconstructs the information contained within a collection of Exceptions within the CLI.</summary>
		public static OperationalStates ExceptionDump( IEnumerable<Exception> exceptions, bool debugMode = false )
		{
			foreach ( Exception e in exceptions )
				ExceptionDump( e, debugMode );

			return OperationalStates.IncompleteWithErrors;
		}

		public static TextElementRuleCollection Decl =>
			new TextElementRuleCollection(
				new TextElementRule[]
				{
					new TextElementRule( /* Outer braces (red) */ /* language=regex */
						"^([{]{2})(.*)([}]{2})$", "{4}&lbrace;&lbrace;$2{4}&rbrace;&rbrace;",
						RegexOptions.Multiline | RegexOptions.IgnoreCase
						),
					new TextElementRule( /* Table and record field (dark gray and light green) */ /* language=regex */
						"((?:[{][a-f0-9][}])(?:&[lr]brace;|[{}]){1,2})((\\[)([a-z][a-z_0-9]*)([|]([\\- *\\w]+))?(?=\\]))",
						"$1{C}$3{A}$4{C}|{A}$6{C}", RegexOptions.IgnoreCase | RegexOptions.Multiline
						),
					new TextElementRule( /* fields and values (light cyan and yellow) */ /* language=regex */
						"(([a-z][a-z_0-9]*)=`([^`]*)`;)", "{B}$2{C}=`{E}$3{C}`;",
						RegexOptions.IgnoreCase | RegexOptions.Multiline
						)
				}
			);
		#endregion
	}

	#region AppletList Class
	public class AppletList : ReflectedApplet, IEnumerator<AppletDescriptor>
	{
		#region Properties
		protected List<AppletDescriptor> _applets = new List<AppletDescriptor>();
		private int _position = 0;
		#endregion

		#region Constructors
		public AppletList(bool autoLoad = true) : base() { if (autoLoad) FetchApplets(); }

		~AppletList() { while (this._applets.Count > 0) { this._applets.RemoveAt(0); } }
		#endregion

		#region Accessors
		AppletDescriptor IEnumerator<AppletDescriptor>.Current => this._applets[this._position];

		public AppletDescriptor this[int index] => this._applets[index];

		public int Count => this._applets.Count;

		public dynamic this[string cmd]
		{
			get
			{
				int i = IndexOf(cmd);
				return (i < 0) ? null : Activator.CreateInstance(this[i].Type);
			}
		}

		object IEnumerator.Current => this._applets[this._position];
		#endregion

		#region Methods
		protected int IndexOf(string cmdName)
		{
			int i = -1; while ((++i < Count) && !this[i].Command.Equals(cmdName, StringComparison.OrdinalIgnoreCase)) ;
			return (i < Count) ? i : -1;
		}

		public bool HasCommand(string cmdName) => (IndexOf(cmdName) >= 0);

		private void FetchApplets()
		{
			this._applets = new List<AppletDescriptor>();
			this.AddRange( GetAllApplets() );
			foreach ( TypeInfo ti in GetAllApplets() )
				this.Add( ti );

			this.Sort();
		}

		public void Add( AppletDescriptor applet)
		{
			if ( !(applet is null) )
			{
				int i = IndexOf( applet.Command );
				if ( i < 0 )
					this._applets.Add( applet );
			}
		}

		public void Add( TypeInfo appletType )
		{
			if ( Applets.IsApplet( appletType ) )
			{
				AppletFoundation aF = (AppletFoundation)Activator.CreateInstance( appletType );
				if ( aF.Command.Length > 3 )
					this.Add( aF.Descriptor() );
			}
		}

		public void Remove( string cmd )
		{
			if ( !string.IsNullOrWhiteSpace( cmd ) )
			{
				int i = IndexOf( cmd );
				if ( i >= 0 )
					this._applets.RemoveAt( i );
			}
		}

		public void AddRange( AppletDescriptor[] applets )
		{
			if (!(applets is null))
				foreach ( AppletDescriptor ad in applets )
					this.Add( ad );
		}

		public void AddRange( TypeInfo[] typeList )
		{
			if (!(typeList is null))
				foreach ( TypeInfo ti in typeList )
					this.Add( ti );
		}

		public void Sort() =>
			this._applets.Sort( ( x, y ) => x.Command.CompareTo( y.Command ) );

		public string[] AppletNames()
		{
			List<string> names = new List<string>();
			foreach (AppletDescriptor aD in this)
				names.Add(aD.Command);

			return names.ToArray();
		}

		public string Find(string partialCmd, Ranks userRank = Ranks.None)
		{
			int i = -1;
			while (++i < Count)
			{
				if (((RankManagement)userRank).IsAllowed(this._applets[i].RankReqd))
				{
					string cmd = this._applets[i].Command;
					if ((cmd.Length >= partialCmd.Length) && cmd.Substring(0, partialCmd.Length).Equals(partialCmd, StringComparison.OrdinalIgnoreCase))
						return cmd;
				}
			}
			return "";
		}

		public static AppletDescriptor[] FetchDescriptors( string search, int rank )
		{
			List<AppletDescriptor> collection = new List<AppletDescriptor>();
			foreach ( TypeInfo ti in GetAllApplets() )
			{
				AppletDescriptor aD = AppletDescriptor.GetDescriptor( ti );
				if (
					(rank >= aD.RankReqd) &&
						(
						(search.Length == 0) ||
						aD.Command.Substring( 0, Math.Min( search.Length, aD.Command.Length ) ).Equals( search, StringComparison.InvariantCultureIgnoreCase )
						)
					)
					collection.Add( aD );
			}
			collection.Sort( ( x, y ) => x.Command.CompareTo( y.Command ) );
			return collection.ToArray();
		}

		public IEnumerator<AppletDescriptor> GetEnumerator() => this._applets.GetEnumerator();

		bool IEnumerator.MoveNext() => (++this._position) < this.Count;

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
		#endregion
	}
	#endregion

	#region AppletDescriptionList Class
	public class AppletDescriptionList : ReflectedApplet, IEnumerator<AppletDescriptor>
	{
		protected List<AppletDescriptor> _applets;
		private int _position;

		#region Constructors
		public AppletDescriptionList() : base() =>
			this.FetchApplets("", Ranks.SuperUser);

		public AppletDescriptionList(string search, Ranks rank = Ranks.BasicUser) : base() =>
			this.FetchApplets(search, rank);

		public AppletDescriptionList(string search, RankManagement rank) : base() =>
			this.FetchApplets(search, rank);

		~AppletDescriptionList() { while (this._applets.Count > 0) { this._applets.RemoveAt(0); } }
		#endregion

		#region Accessors
		public int Count => this._applets.Count;

		AppletDescriptor IEnumerator<AppletDescriptor>.Current => this._applets[this._position];

		public AppletDescriptor this[int index] => this._applets[index];

		public AppletDescriptor this[string cmd]
		{
			get
			{
				int i = -1; while ((++i < this._applets.Count) && !this[i].Command.Equals(cmd, StringComparison.InvariantCultureIgnoreCase)) ;
				return (i < this._applets.Count) ? this[i] : null;
			}
		}

		object IEnumerator.Current => this._applets[this._position];
		#endregion

		#region Methods
		private void FetchApplets(string search, RankManagement rank)
		{
			this._applets = new List<AppletDescriptor>();
			foreach (System.Reflection.TypeInfo ti in GetAllApplets())
			{
				AppletDescriptor aD = AppletDescriptor.GetDescriptor(ti);
				if (
					(rank >= aD.RankReqd) &&
						(
						(search.Length == 0) ||
						aD.Command.Substring(0, Math.Min(search.Length, aD.Command.Length)).Equals(search, StringComparison.InvariantCultureIgnoreCase)
						)
					)
					this._applets.Add(aD);
			}
			this._applets.Sort((x, y) => x.Command.CompareTo(y.Command));
		}

		public IEnumerator<AppletDescriptor> GetEnumerator() { return this._applets.GetEnumerator(); }

		bool IEnumerator.MoveNext() { return (++this._position) < this.Count; }

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
		#endregion
	}
	#endregion

	/// <summary>Provides an informational descriptor that contains relevant information about an applet.</summary>
	public class AppletDescriptor
	{
		#region Properties
		/// <summary>What Rank is required in order to execute this applet.</summary>
		private RankManagement _rank = 0;

		/// <summary>What's the .NET type designation of the applet (it's Class)</summary>
		private Type _type = typeof(AppletFoundation);

		/// <summary>Specifies the datatype of the data managed / created by the applet.</summary>
		private Type _dataType = typeof(Object);

		/// <summary>What's the string that represents the applet's name (what you type to activate it).</summary>
		private string _cmd = "UNDEFINED";

		/// <summary>Specifies the version of the applet.</summary>
		private Version _version = null;

		/// <summary>Contains the arguments associated with the applet described by this object.</summary>
		//private ArgumentCollection _args = null;
		private CommandLine _cmdLine = new CommandLine( "UNDEFINED" );
		#endregion

		#region Constructors
		/// <summary>Manually populates the descriptor with the specified values.</summary>
		/// <param name="command">The CLI command used to call this applet. If set to NULL, the parent object Type name (less "CMD_") is used instead.</param>
		/// <param name="appletType">A C# Type object for the class to be described.</param>
		/// <param name="dataType">A C# Type that defines the kind of data the Applet manages.</param>
		/// <param name="version">A Version object specifying the Applet's version.</param>
		/// <param name="rank">A Rank enumeration value specifying the minimum rank required to use the described Applet.</param>
		/// <param name="parameters">An ArgumentsMgt object specifying the argument schema that the applet supports.</param>
		/// <param name="description">A string containing a description that succinctly identifies the applet's purpose.</param>
		protected AppletDescriptor(Type appletType, Type dataType, Version version, CommandLine parameters, Ranks rank = Ranks.BasicUser, string description = "", string help = "")
		{
			if ( parameters is null ) throw new ArgumentNullException( "The 'parameters' value cannot be null!" );

			this._cmdLine = parameters;
			//this.Arguments = parameters.Args;
			this.RankReqd = rank;
			this.Type = appletType;
			this.DataType = dataType;
			this.Version = version;
			this.Command = appletType.Name.Replace("CMD_", "");
			this.Description = description;
			this.Help = help;
		}

		/// <summary>
		/// Populates the descriptor based on only the C# Type of the designated Applet.
		/// NOTE: Unlike all of the other constructors, this one uses an instantiation of the specified type to populate
		/// the descriptor! Be aware of possible ramifications of this.
		/// </summary>
		/// <param name="appletType">The Applet Type to be described.</param>
		//public AppletDescriptor(Type appletType)
		//{
		//	if (!Applets.IsApplet(appletType)) throw Applets.InvalidType(appletType);

		//	AppletFoundation aF = (AppletFoundation)Activator.CreateInstance(appletType);
		//	this.Initialise(appletType, aF.Command, aF.Version, aF.MIN_RANK, (this._arguments.Count > 0));
		//}

		/// <summary>Populates the descriptor using an existing instance of the specified Applet</summary>
		/// <param name="applet">An existing instance of the Applet to be described.</param>
		//public AppletDescriptor(AppletFoundation applet) =>
		//	this.Initialise(applet.GetType(), applet.Command, applet.Version, applet.MIN_RANK, applet.Quiet, applet.Flavour, applet.CliSafe);
		#endregion

		#region Accessors
		public string Command
		{
			get => this._cmd;
			protected set
			{
				string cmd = ValidateCommand((value is null) ? this._type.Name.Replace("CMD_", "") : value);
				if (cmd.Length < 4) throw new ArgumentException("The command \"" + value + "\" cannot be assiged because it contains an insufficient number of valid characters.");
				this._cmd = cmd;
				this._cmdLine.Cmd = cmd;
			}
		}

		/// <summary>Returns the Minimum access rank required to access the described applet.</summary>
		public RankManagement RankReqd { get => this._rank; protected set => this._rank = value.Rank; }

		public Type Type
		{
			get => this._type;
			protected set { if (Applets.IsApplet(value)) { this._type = value; } }
		}

		public bool HasParameters => this._cmdLine.HasArguments;

		public Version Version
		{
			get => this._version;
			protected set { if ((this._version is null) && !(value is null)) { this._version = value; } }
		}

		public CommandLineArgs Arguments 
		{ 
			get => this._cmdLine.Args;
			protected set
			{
				if (!(value is null))
					this._cmdLine.Args.AddRange( value.ToArray() );
			}
		}

		public CommandLineSwitches Switches
		{
			get => this._cmdLine.Switches;
			protected set
			{
				if ( !(value is null) )
					this._cmdLine.Switches.AddRange( value.ToArray() );
			}
		}

		public Type DataType
		{
			get => this._dataType;
			set { if ((this._dataType == typeof(Object)) && !(value is null)) { this._dataType = value; } }
		}

		/// <summary>Holds a description string to succinctly identify the applet's purpose.</summary>
		public string Description { get; set; } = "";

		/// <summary>Holds a string that provides deeper help on the applet.</summary>
		public string Help { get; set; } = "";
		#endregion

		#region Methods
		public override string ToString() => "[" + this._type.Name + "] " + this._cmd + " (" + this._rank.ToString() + ")";

		public static string ValidateCommand(string source)
		{
			if (source == "") throw new InvalidOperationException("A command value cannot be empty.");
			source = source.ToUpperInvariant().Filter("ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890-_", true);
			if (source.Length < 4)
				throw new ArgumentOutOfRangeException("There are insufficient valid characters in \"" + source + "\".");

			if (!Regex.IsMatch(source, @"^([A-Z])(([A-Z|0-9]*)[-_]?)*([A-Z|0-9]+)$"))
				throw new ArgumentOutOfRangeException("There are invalid characters in the defined MY_CMD value.");

			return source;
		}

		/// <summary>Creates a new AppletDescriptor with the provided specifications.</summary>
		/// <param name="appletType">A C# Type object for the class to be described.</param>
		/// <param name="dataType">A C# Type that defines the kind of data the Applet manages.</param>
		/// <param name="version">A Version object specifying the Applet's version.</param>
		/// <param name="rank">A Rank enumeration value specifying the minimum rank required to use the described Applet.</param>
		/// <param name="parameters">A CommandLine object specifying the argument schema that the applet supports.</param>
		/// <param name="desc">A string containing a description that succinctly identifies the applet's purpose.</param>
		public static AppletDescriptor Create(Type appletType, Type dataType, Version version, CommandLine parameters, Ranks rank = Ranks.BasicUser, string desc = "", string help = "") =>
			new AppletDescriptor(appletType, dataType, version, parameters, rank, desc, help);

		/// <summary>Given an object class Type descended from AppletFoundation, this routine will obtain it's Descriptor.</summary>
		/// <param name="forType">A valid AppletFoundation-derived class to poll.</param>
		/// <returns>If the class is valid, and instantiation succeeds, the AppletDescriptor for that class, otherwise NULL.</returns>
		public static AppletDescriptor GetDescriptor(Type forType)
		{
			try
			{
				Applets.ValidateType(forType);
				dynamic obj = Activator.CreateInstance(forType);
				return (obj as AppletFoundation).Descriptor();
			}
			catch (Exception e) { }
			return null;
		}
		#endregion
	}

	/// <summary>
	/// This class is used to support dynamically accessing applets at runtime without having to write any special
	/// additional code. Applets can be placed anywhere in the project (but stored in the Cobblestone.Classes.Commands
	/// namespace) and can be immediately accessed by the CLI or any other forms.
	/// </summary>
	public class ReflectedApplet
	{
		#region Properties
		protected dynamic _object = null;
		#endregion

		#region Constructors
		public ReflectedApplet(dynamic source)
		{
			if (!this.IsConformingType(source)) throw new ArgumentException();
			this._object = source;
		}

		public ReflectedApplet(Type flavour, dynamic source)
		{
			if (!this.IsConformingType(source) || !this.IsConformingType(flavour)) throw Applets.InvalidType(flavour); // new ArgumentException();
			this._object = source;
		}

		public ReflectedApplet(string cmd) => this.Populate(cmd);

		protected ReflectedApplet() { }
		#endregion

		#region Accessors
		public AppletFoundation AsBase => (this._object as AppletFoundation);

		public dynamic AsType => (this._object is null) ? null : this.Convert(this._object);

		public bool Success => this.AsBase.Success;

		public Type Type => (this._object is null) ? null : this._object.GetType();

		public CommandToken Token => (this.AsType is null) ? CommandToken.Empty : this.AsBase;

		public dynamic Archetype => (this._object is null) ? null : Applets.GetDerivedType(this._object.GetType());

		public string Uid => (this._object is null) ? CommandToken.Empty.Uid : this.AsBase.Uid;
		#endregion

		#region Methods
		protected bool IsConformingType(object test) => Applets.IsApplet(test);

		protected bool IsConformingType(Type test) => Applets.IsApplet(test);

		protected dynamic Convert(object o, Type t)
		{
			if (!IsConformingType(o)) throw new ArgumentException("The specified source object is not a valid type (\"" + o.GetType().Name + "\")");
			if (!IsConformingType(t)) throw new ArgumentException("The specified target Type is not a valid type (\"" + t.Name + "\")");
			return (dynamic)System.Convert.ChangeType(o, t);
		}

		protected dynamic Convert(object o)
		{
			if (!IsConformingType(o)) throw new ArgumentException("The specified source object is not a valid type (\"" + o.GetType().Name + "\")");
			return (dynamic)System.Convert.ChangeType(o, o.GetType());
		}

		public override string ToString() =>
			((this._object == null) ? "NULL" : this._object.GetType().ToString()) + ";";

		/// <summary>
		/// Uses reflection to search for all AppletFoundation classes (and derivatives) whose class name begins with "CMD_"
		/// and that manage the requested command. If it finds one, it populates this object with a new instance of that class.
		/// </summary>
		/// <param name="cmd">A (case-insensitive) string specifying the command to look for.</param>
		protected void Populate(string cmd)
		{
			TypeInfo[] applets = ReflectedApplet.GetAllApplets();
			foreach (TypeInfo ti in applets)
				if (this.IsConformingType(ti))
				{
					dynamic result = Activator.CreateInstance(ti);
					if (result.Command.Equals(cmd, StringComparison.InvariantCultureIgnoreCase))
					{
						this._object = result;
						return;
					}
				}

			this._object = null;
		}
		#endregion

		#region Static Methods
		/// <summary>Retrieves an array of qualifying TypeInfo objects reflecting the specified criteria from within the supplied Assembly.</summary>
		/// <param name="source">The source assembly in which to work.</param>
		/// <param name="prefix">A signature beginning string that qualifying classses must possess.</param>
		/// <param name="nameSpace">The namespace within the assembly to search.</param>
		/// <returns>An array of qualifying TypeInfo objects reflecting the specified criteria from within the supplied Assembly.</returns>
		protected static TypeInfo[] GetAppletsFromAssembly(Assembly source, string prefix, string nameSpace = "*")
		{
			if ((prefix is null) || (prefix.Trim().Length == 0)) prefix = "*";
			List<TypeInfo> classes = new List<TypeInfo>();
			foreach (TypeInfo ti in source.DefinedTypes)
				if (
						((nameSpace == "*") || ti.Namespace.Equals(nameSpace)) &&
						((prefix == "*") || ((ti.Name.Length > prefix.Length) && ti.Name.Substring(0, prefix.Length).Equals(prefix))) &&
						Applets.IsApplet(ti)
					)
					classes.Add(ti);
			return classes.ToArray();
		}

		/// <summary>Uses reflection to obtain a list of all of the classes in the specified namespace that begin with the specified prefix.</summary>
		/// <param name="assemblyName">The assembly name to search. If specified as an asterisk, all assemblies will be searched.</param>
		/// <param name="nameSpace">The namespace within the assembly to search.</param>
		/// <param name="prefix">A signature beginning string that qualifying classses must possess.</param>
		/// <returns>An array of TypeInfo objects relating to all of the classes discovered.</returns>
		protected static TypeInfo[] GetAllApplets(string prefix = "CMD_", string assemblyName = "*", string nameSpace = "*")
		{
			// Get a collection of all Assemblies in the current collection.
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

			if (assemblyName.Equals("*")) // Look in them all...
			{
				List<TypeInfo> classes = new List<TypeInfo>();
				foreach (Assembly assembly in assemblies)
					if ( !Regex.IsMatch( assembly.ManifestModule.ScopeName, "(^Microsoft.|^System.|^CommonLanguageRuntimeLibrary$)" ) )
						classes.AddRange(GetAppletsFromAssembly(assembly, prefix, nameSpace));

				return classes.ToArray();
			}
			else
			{
				// Search through the list of known assemblies for the one specified...
				int i = -1; while ((++i < assemblies.Length) && !assemblies[i].FullName.Substring(0, assemblyName.Length + 1).Equals(assemblyName + ",")) ;
				if (i < assemblies.Length)
					return GetAppletsFromAssembly(assemblies[i], prefix, nameSpace);
			}
			return new TypeInfo[] { };
		}
		#endregion
	}

	/// <summary>Forms the basis for an Applet. Stores and manages any data that is created by the applet.</summary>
	public class CommandToken
	{
		#region Properties
		/// <summary>Every instance of an applet gets a unique id that is used for identifying it's data in the shared memory pool.</summary>
		protected Guid _guid;

		/// <summary>Holds whatever data the applet wishes to make available for other applets to use.</summary>
		protected dynamic _data = null;

		/// <summary>Stores/reports the current state of the applet.</summary>
		protected Applets.OperationalStates _state = Applets.OperationalStates.None;

		/// <summary>Stores any command line arguments that are passed to the applet at execution time.</summary>
		protected CommandLine _parameters = new CommandLine( "UNKNOWN" );

		/// <summary>Stores any/all errors that are generated by the applet during its execution.</summary>
		protected List<string> _errors;

		/// <summary>Stores the Type of the stored data.</summary>
		protected Type _dataType = null;

		/// <summary>Stores the AppletDescriptor declaration for the object.</summary>
		protected AppletDescriptor _descriptor = null;

		/// <summary>A string to store any raw XML that's associated with this object.</summary>
		protected string _rawXml = "";

		/// <summary>Stores the User information for who instantiated the applet.</summary>
		protected UserInfo _owner;

		/// <summary>Specifies how long this object's shared Data remains "alive" in the shared memory pool.</summary>
		protected TimeSpan _lifetime = new TimeSpan(23, 59, 59);

		/// <summary>Specifies how many times this object's shared Data can be retrieved from the Shared memory pool.</summary>
		protected int _readLimit = int.MaxValue;
		#endregion

		#region Constructors
		//public CommandToken(AppletDescriptor descriptor, string[] parameters = null, string[] errors = null)
		//{
		//	this._guid = Guid.NewGuid();
		//	this._parameters = (parameters is null) || (parameters.Length == 0) ? new ArgumentsMgt() : new ArgumentsMgt(parameters);
		//	this._errors = (errors is null) || (errors.Length == 0) ? new List<string>() : new List<string>(errors);
		//}

		//public CommandToken(AppletDescriptor descriptor, ArgumentsMgt args, string[] errors = null)
		//{
		//	this._guid = Guid.NewGuid();
		//	this._parameters = args;
		//	this._errors = (errors is null) || (errors.Length == 0) ? new List<string>() : new List<string>(errors);
		//}

		protected CommandToken()
		{
			this._guid = Guid.NewGuid();
			//this._parameters = new CommandLine( "UNKNOWN" );
			this._errors = new List<string>();
			this._owner = UserInfo.DefaultUser();
		}

		protected CommandToken(UserInfo user, string[] args, string[] errors = null)
		{
			this._guid = Guid.NewGuid();
			//this._parameters = new CommandLine( "UNKNOWN" );
			if (!(args is null) && (args.Length > 0))
				this._parameters.Args.AddRange( args );

			this._errors = (errors is null) || (errors.Length == 0) ? new List<string>() : new List<string>(errors);
			this._owner = user;
		}

		protected CommandToken(UserInfo user, CommandLineArgs args = null, string[] errors = null)
		{
			this._guid = Guid.NewGuid();
			//this._parameters = new CommandLine( "UNKNOWN" );
			if ( !(args is null) && (args.Count > 0) )
				this._parameters.Args.AddRange( args.ToArray() );

			this._errors = (errors is null) || (errors.Length == 0) ? new List<string>() : new List<string>(errors);
			this._owner = user;
		}

		protected CommandToken(UserInfo user, Guid source, string[] errors = null)
		{
			this._guid = source;
			//this._parameters = new CommandLine( "UNKNOWN" );
			this._errors = (errors is null) || (errors.Length == 0) ? new List<string>() : new List<string>(errors);
			this._owner = user;
		}
		#endregion

		#region Operators
		public static bool operator ==(CommandToken left, string right)
		{
			if (left is null) return (right is null) || (right == string.Empty) || (right == "");
			if ((right is null) || (right == string.Empty) || (right == "")) return false;
			return left.Uid == right;
		}

		public static bool operator !=(CommandToken left, string right) { return !(left == right); }

		public static bool operator ==(CommandToken left, CommandToken right)
		{
			if (left is null) return right is null;
			if (right is null) return false;
			return left.Uid == right.Uid;
		}

		public static bool operator !=(CommandToken left, CommandToken right) { return !(left == right); }

		public static bool operator ==(CommandToken left, ReflectedApplet right)
		{
			if (left is null) return right is null;
			if (right is null) return false;
			return left.Uid == right.Token.Uid;
		}

		public static bool operator !=(CommandToken left, ReflectedApplet right) { return !(left == right); }
		#endregion

		#region Accessors
		public string Uid => this._guid.ToString();

		public Guid Token => this._guid;

		public bool Success => (this._state == Applets.OperationalStates.Complete);

		public virtual string Payload => this._data.ToString();

		public UserInfo Owner => this._owner;

		public XmlDocument Xml
		{
			get
			{
				XmlDocument result = new XmlDocument();
				if (this._rawXml.Length > 0)
				{
					try { result.LoadXml(this._rawXml); }
					catch(Exception e) {
						result.LoadXml(NetXpertExtensions.XML_HEADER + "<xmlConversionError>" + e.Message + "</xmlConversionError");
					}
				}
				return result;
			}
			set => this._rawXml = value.OuterXml;
		}

		public virtual Type DataType => this._dataType;

		public virtual Applets.OperationalStates State
		{
			get => this._state;
			protected set => this._state = value;
		}

		public int ParamCount => (this._parameters is null) ? 0 : this._parameters.ArgCount + this._parameters.SwitchCount;

		public CommandLineSwitches Switches => this._parameters.Switches;

		public CommandLineSwitch this[string switchName]
		{
			get => this.HasSwitch(switchName) ? this._parameters[switchName] : null;
			set
			{
				if (this._parameters.HasSwitch(switchName))
					this._parameters.Switches[switchName].Value = value.Value;
				else
					this.Parameters.Switches.Add(value);
			}
		}

		public CommandLine Parameters => this._parameters;

		public CommandLineArgs Args => this._parameters.Args;

		public string[] Values
		{
			get
			{
				List<string> values = new List<string>();
				foreach ( CommandLineDataValue value in this._parameters.Args )
					values.Add( value.ToString() );

				return values.ToArray();
			}
		}

		public virtual dynamic Data
		{
			get => this._data;
			set
			{
				this._data = value;
				if (this._data != null)
					this._dataType = value.GetType();
			}
		}

		public ulong RecId
		{
			get => (this.HasSwitch("recId")) ? new PolyString(this.GetSwitch("recId")) : ulong.MaxValue;
			set
			{
				if (this.HasSwitch("recId"))
					this._parameters["recId"].Value = value.ToString();
				else
					this._parameters.Switches.Add(new CommandLineSwitch( $"/recId:{value}", Utility.SplitChar.None ));
			}
		}

		public static CommandToken Empty => new CommandToken();

		public bool HasErrors => (this._errors.Count > 0);

		/// <summary>Gets the Number of errors reported by the command.</summary>
		public int ErrorCount => this._errors.Count;

		public string[] Errors => this._errors.ToArray();

		/// <summary>Facilitates accessing the maximum shared data pool reads permitted by this object.</summary>
		/// <remarks>The setter functions such that external mechanisms may reduce the maximum reads allowed, but can never increase them.</remarks>
		public int DataReadLimit
		{
			get => this._readLimit;
			set => this._readLimit = (value >= 0) ? Math.Min(value, this._readLimit) : this._readLimit;
		}

		public TimeSpan LifeLimit => this._lifetime;
		#endregion

		#region Nethods
		/// <summary>Regenerates the UID for this object. Not usually a good idea!</summary>
		protected void ReRoll() => this._guid = Guid.NewGuid();

		/// <summary>Reports on whether a specfied switch exists in the arguments passed to this object.</summary>
		/// <param name="test">A string specifying the name (id) of the switch to look for.</param>
		/// <returns>TRUE if the specified switch exists, otherwise FALSE.</returns>
		protected bool HasSwitch(string test) => this._parameters.HasSwitch(test);

		protected CommandLineSwitch GetSwitch(string test) => this.HasSwitch(test) ? this._parameters[test] : null;

		protected void AddSwitch( CommandLineSwitch @switch ) =>
			this._parameters.Switches.Add( @switch );

		protected void AddSwitch( string value ) =>
			this._parameters.Switches.Add( value );

		public override string ToString() => this.Uid;

		public override bool Equals(object obj) => base.Equals(obj);

		public override int GetHashCode() => base.GetHashCode();

		public void AddError(string error) => this._errors.Add(error);

		public void AddError(string[] errors) => this._errors.AddRange(errors);
		#endregion
	}

	/// <summary>
	/// Literally the foundation block for all applets that can be used with this application. If your applet doesn't
	/// need to communicate with the server, use this as your predicate object.
	/// </summary>
	/// <seealso cref="CobblestoneCommon.AppletBase"></seealso>
	public abstract partial class AppletFoundation : CommandToken
	{
		#region Properties
		#endregion

		#region Constructor
		public AppletFoundation() : base() => this.Initialise();

		public AppletFoundation( UserInfo user, string[] parameters ) : base( user, parameters ) => this.Initialise();

		public AppletFoundation( UserInfo user, CommandLineArgs args ) : base( user, args ) => this.Initialise();

		public AppletFoundation( ulong recId ) : base()
		{
			this.Initialise();
			this.RecId = recId;
		}

		private void Initialise()
		{
			this._descriptor = this.Descriptor();
			this.State = Applets.OperationalStates.Idle;
			this._dataType = this.Descriptor().DataType;
			this._parameters = new CommandLine( this._descriptor.Command );
			//this._help = Common.Help.GetCmdNode(this.Command);
			this.ClearErrors();
		}
		#endregion

		#region Accessors
		protected string MY_CMD => this.Descriptor().Command;

		protected string MY_TYPE_NAME => this.GetType().Name;

		protected CommandLineInterface Cli => CommandLineInterface.CLI;

		protected Command Cmd { get; private set; } = null;

		public Version Version => this.Descriptor().Version;

		public RankManagement AccessRank => this.Descriptor().RankReqd;

		public bool UsesParameters => this.Descriptor().HasParameters;

		public string LastError => (this._errors.Count > 0) ? this._errors[this._errors.Count - 1] : "";

		public string Command => this.Descriptor().Command;

		public XmlElement ErrorsAsXml
		{
			get
			{
				XmlDocument d = new XmlDocument();
				XmlElement e = d.CreateElement("InvocationErrors");
				XmlAttribute a = d.CreateAttribute("count");
				a.Value = this._errors.Count.ToString();
				e.Attributes.Append(a);
				string result = "";
				foreach (string err in this._errors)
					result += "<err>" + WebUtility.HtmlEncode(err) + "</err>\r\n";

				e.InnerXml = result;
				return e;
			}
		}

		public string sRecId
		{
			get => this.RecId.ToString();
			set
			{
				ulong r;
				try
				{
					r = ulong.Parse(value);
					this.RecId = r;
				}
				catch { }
			}
		}

		public Ranks RunRank { get; protected set; }

		/// <summary>Parses the ArgumentCollection populated in the resident AppletDescription into a form suitable for display as help.</summary>
		protected string Syntax()
		{
			AppletDescriptor descriptor = this.Descriptor();
			string value = $"{descriptor.Arguments} ";
			if ( descriptor.Switches.Count > 0 ) value += $"{descriptor.Switches}";
			string syntax = "";
			if ( (value.Length > 3) && (value.Substring( 0, 2 ) == "==") ) return value.Substring( 2 );

			if ( !string.IsNullOrWhiteSpace( value ) )
			{
				value = value.Replace( "{", "&lbrace;" ).Replace( "}", "&rbrace;" ); // No passed DECL encoding allowed!
				Regex data = new Regex( @"
					(?:(?<prefix>[-\/]?)
						(?<switch>[a-z][a-z0-9]*|[a-z]+[|][a-z0-9]+|\x22?[a-z][a-z0-9_ ]*[a-z0-9]\x22?)
						(?<data>
							(?<sep>[=:])
							(?<value>\[?
								(?:\x22[^\x22]*\x22|[a-z0-9_ ,|]*)
							\]?)
						)?
					)", RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase 
				);

				if ( data.IsMatch( value ) )
				{
					syntax = "";
					MatchCollection matches = data.Matches( value );
					for ( int i = 0; i < matches.Count; )
					{
						Match m = matches[ i ];
						syntax += "{B}";
						if ( m.Groups[ "prefix" ].Success && !string.IsNullOrEmpty( m.Groups[ "prefix" ].Value ) && (m.Groups[ "switch" ].Value.IndexOf( ' ' ) < 0) )
						{
							syntax += m.Groups[ "prefix" ].Value;
							if ( m.Groups[ "switch" ].Success )
								syntax += m.Groups[ "switch" ].Value;
						}
						else
							if ( m.Groups[ "switch" ].Success )
							syntax += "{A}" + Regex.Replace( m.Groups[ "switch" ].Value, @"([^|,])([|,])([^|,])", "$1{4}$2{A}$3" );

						if ( m.Groups[ "data" ].Success )
						{
							syntax += "{7}" + (m.Groups[ "sep" ].Success ? m.Groups[ "sep" ].Value : ":");

							if ( m.Groups[ "value" ].Success )
							{
								string v;
								if ( Regex.IsMatch( m.Groups[ "value" ].Value, @"^[\[][\w]+[\]]$" ) )
									v = Regex.Replace( m.Groups[ "value" ].Value, @"([\[])([\w]+)([\]])$", "{9}&lbrace;{6}$2{9}&rbrace;" );
								else
									v = 
										m.Groups[ "value" ].Value[ 0 ] == '"' 
										? "{3}\"{E}" + m.Groups[ "value" ].Value.Trim( new char[] { '"' } ) + "{3}\""
										: "{9}" + m.Groups[ "value" ].Value;

								v = Regex.Replace( v, @"([\]\x22])$", "{9}$1" );
								syntax += Regex.Replace( v, @"([^,\[|]*)([,|]+)([^,|]*)", "{E}$1{9}$2{E}$3" );
							}
						}
						syntax += (++i < matches.Count) ? "{,rn}{,$1}".Replace( new object[] { MY_CMD.Length + 10 } ) : "";
					}
				}
			}
			return syntax;
		}
		#endregion

		#region Methods
		public void Details() =>
			Con.Tec(
				"$3{,3}&raquo; {F}Syntax{7,rn}:{,7}> {F}$2$4{,rn}$5{,rn}".Replace(
					new object[] { Cli.Prompt.Value, Descriptor().Command,
						string.IsNullOrWhiteSpace( Descriptor().Description ) ? "" : "{,3}&raquo; {F}Description{7}: {9}" + Descriptor().Description + "{,rn}",
						Descriptor().HasParameters ? " " + this.Syntax() : " ",
						string.IsNullOrWhiteSpace( Descriptor().Help ) ? "" : "{,3}&raquo; {F}Notes{7,rn}: " + Cli.ApplyEnvironmentTo( Descriptor().Help )
					}
				)
			);


		#region Console output API
		protected void Tec( string what ) => 
			Con.Tec( what );

		protected void Tec( string what, object item ) => 
			Con.Tec( what, item );

		protected void Tec( string what, object[] items ) => 
			Con.Tec( what, items );

		protected void Lang( object id, object[] items = null ) => 
			Tec( Language.Prompt.Get( id ), items );

		protected void Lang( object[] id, object[] items = null ) => 
			Tec( Language.Prompt.Get( id ), items );

		protected void Lang( object id, object[] langItems, object item = null ) => 
			Tec( Language.Prompt.Get( id, langItems ), item );

		protected void Lang( object id, object[] langItems, object[] items = null ) => 
			Tec( Language.Prompt.Get( id, langItems ), items );
		#endregion

		public Applets.OperationalStates Execute(Command cmd, Ranks asRank)
		{
			if ( cmd is null )
			{
				this.AddError( "NULL Command passed." );
				return Applets.OperationalStates.IncompleteWithErrors;
			}
			else
				this._parameters = cmd.CmdLine;

			this.Installer();
			this.State = Applets.OperationalStates.Running;
			this.RunRank = asRank;
			this.ShowVersion();

			if (this.HasSwitch("?") || this.HasSwitch("help"))
			{
				this.Details();
				return Applets.OperationalStates.Complete;
			}

			// Use this to avoid having to build the descriptor multiple times...
			AppletDescriptor descriptor = this.Descriptor();

			if (this.HasSwitch("ver"))
			{
				if ( descriptor.Description.Length > 0 )
					Tec( "{7,7}&quot;{$1}$3{7,rn}&quot;", new object[] { new CliColor( "9" ), NetXpertExtensions.ExecutableName, Descriptor().Description } );

				return Applets.OperationalStates.Complete;
			}

			if (descriptor.RankReqd > asRank)
			{
				ShowError("The supplied Privilege Rank is Insufficient for accessing this applet.");
				return Applets.OperationalStates.CompleteWithErrors;
			}

			foreach (CommandLineSwitch sw in this._parameters.Switches)
				if (!descriptor.Switches.HasSwitch(sw))
				{
					Tec( "{F}Warning: {}The switch {7}\"{E}$1{7}\" {,rn}is not recognized for this command.", sw.Id );
					return Applets.OperationalStates.CompleteWithErrors;
				}

			// Parse all passed switches, and call any methods that are configured to process them.
			Applets.OperationalStates swResult = Applets.OperationalStates.Running;
			if ( this.Switches.Count > 0)
				foreach( CommandLineSwitch s in this.Switches )
				{
					BindingFlags bf = BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic;
					MethodInfo mi = this.GetType().GetMethod( s.Id, bf, null, new Type[] { typeof( Applets.OperationalStates ) }, null );
					if ( !(mi is null) )
						swResult = (Applets.OperationalStates)mi.Invoke( this, new object[] { swResult } );

					if (
						(swResult != Applets.OperationalStates.None) &&
						(swResult != Applets.OperationalStates.Complete) &&
						(swResult != Applets.OperationalStates.Idle) &&
						(swResult != Applets.OperationalStates.Running)
						)
						return swResult;
				}

			Console.CursorVisible = false;
			Applets.OperationalStates result = Main( swResult );
			Console.CursorVisible = true;

			return result == Applets.OperationalStates.Running ? Applets.OperationalStates.Complete : result;
		}

		/// <summary>Code that is executed when this Applet is installed into the CLI.</summary>
		/// <remarks>Use this to set-up/configure the applet, especially activating/installing/polling any CLI settings used by the
		/// applet for its functions.</remarks>
		public abstract Applets.OperationalStates Installer();

		protected abstract Applets.OperationalStates Main( Applets.OperationalStates runState );

		public abstract AppletDescriptor Descriptor(); // Must be defined in each child to specify the descriptor for this object.

		protected bool ParameterError(string message)
		{
			this.AddError("Invalid parameters received: " + this._parameters.ToString());
			Tec( "{,3}&raquo;{F4,5}Error:{E} Invalid parameters received.{rn,rn}The format for this command is:{,2}► {F,rn}$1 $2\n",
				new string[] { this.Command, message } );
			//Con.HighlightLn("Error: ", "Invalid parameters received.\r", "The format for this command is:", ConsoleColor.Yellow);
			//Con.Write("  ► "); Con.WriteLn(this.Command + " " + message + "\r\n", ConsoleColor.White);
			this.State = Applets.OperationalStates.CompleteWithErrors;
			return false;
		}

		protected bool ShowError(string message) =>
			ShowError(message, new string[] { });

		protected bool ShowError(string message, string[] data)
		{
			this.AddError(message);
			Tec( "{,3}&raquo;{F4,5}Error:{F,rn} $1", message );
			//Con.Write("Error: "); Con.WriteLn(message, ConsoleColor.Red);
			if (data.Length > 0)
				foreach (string s in data)
					Tec(Con.WriteMsgLn(s, ConsoleColor.Yellow, CliColor.Normalize(ConsoleColor.White)));

			this.State = Applets.OperationalStates.CompleteWithErrors;
			return false;
		}

		protected void InsufficientRank(Ranks reqd, Ranks possessed)
		{
			this.AddError( "Insufficient Rank [" + possessed.ToString() + "] -> [" + reqd.ToString() + "]" );
			Tec( 
				"{,3}&raquo;{F4,5}Not Allowed!{8} - {7,rn}You do not have sufficient rank to use this command:" +
				"{7,20}Your Rank: {8}[{B}$1{8}] {D}-> {7}Rank Required: {8}[{B}$2{8,rn}]", 
				new object[] { possessed, reqd } 
				);
		}

		/// <summary>Outputs a quick and easy applet description.</summary>
		protected void ShowVersion() =>
			Tec( "{,3}&raquo; {A}Applet {7}[{B}$1$2{7}] version {A,rn}$3",
				new object[] { this.GetType().Name, (this.DataType is null) ? "" : "{7}({E}" + this.DataType.Name + "{7})", this.Version } );

		public void AddError(Exception e) =>
			this._errors.Add((e.InnerException is null) ? e.Message : e.Message + " (" + e.InnerException.Message + ")");

		protected void ClearErrors() => this._errors = new List<string>();

		public PoolDataItem CreatePoolItem()
		{
			return new PoolDataItem(
						this.Uid,
						this.Data,
						this.Owner,
						this.LifeLimit,
						this.DataType,
						this.Command,
						this.DataReadLimit,
						this._rawXml
					);
		}
		#endregion
	}
}
