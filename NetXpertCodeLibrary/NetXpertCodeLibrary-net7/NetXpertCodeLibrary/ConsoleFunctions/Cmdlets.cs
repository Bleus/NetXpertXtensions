using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using NetXpertCodeLibrary.ConfigManagement;
using NetXpertCodeLibrary.ContactData;
using NetXpertCodeLibrary.Extensions;
using NetXpertExtensions;
using NetXpertExtensions.Classes;
using static NetXpertCodeLibrary.Extensions.NetXpertExtensions;
using static NetXpertExtensions.NetXpertExtensions;

namespace NetXpertCodeLibrary.ConsoleFunctions
{
	public abstract class CmdletFoundation
	{
		#region Properties
		private string _cmd = "";
		protected readonly bool _suppressVer; // If set to true, "About()" returns nothing.
		#endregion

		#region Constructors
		public CmdletFoundation( string cmd, bool suspendVer = false )
		{
			this._suppressVer = suspendVer;
			Cmd = cmd;
		}
		#endregion

		#region Accessors
		/// <summary>Contains/manages the specific CLI command for the cmdlet.</summary>
		public string Cmd
		{
			get => this._cmd;
			protected set
			{
				if ( string.IsNullOrEmpty( this._cmd ) )
				{
					if ( !string.IsNullOrWhiteSpace( value ) && Regex.IsMatch( value, @"(?:[?]|[a-zA-Z][a-zA-Z0-9]{1,2})" ) )
						this._cmd = value.ToUpperInvariant();
					else
						throw new InvalidOperationException(
							Language.Prompt.GetByName( "**Exceptions.CmdletFoundatation_Cmd", new object[] { value } )
						);
						// "The specified CMD value is invalid. (\"" + value + "\")"
				}
			}
		}

		public bool SuspendVersion => this._suppressVer;

		/// <summary>Manages the command line information issued when this cmdlet was instantiated.</summary>
		public Command Command { get; private set; } = null;

		public CommandLineSwitches Switches => Command is null ? null : Command.Switches;

		public CommandLineArgs Args => Command is null ? null : Command.Args;

		protected UserInfo User => Command is null ? null : Command.User;

		/// <summary>Provides a reference to the particular CLI environment that instantiated this cmdlet.</summary>
		protected CommandLineInterface Cli => CommandLineInterface.CLI;

		protected CliRegObject AppRegObj => Cli.Registry[ CliRegistry.Hive.App ][ this.GetType().Name ] as CliRegObject;

		protected CliRegObject UserRegObj => 
			Cli.Registry.HasActiveUser ? Cli.Registry[ CliRegistry.Hive.User ][ $"Applications.{this.GetType().Name}" ] as CliRegObject : null;

		public string Syntax
		{
			get
			{
				string value = MySyntax(), syntax = "";
				if ( (value.Length > 3) && (value.Substring( 0, 2 ) == "==") ) return value.Substring( 2 );

				if ( !string.IsNullOrWhiteSpace( value ) )
				{
					value = value.Replace( "{", "&lbrace;" ).Replace( "}", "&rbrace;" ); // No passed DECL encoding allowed!
					Regex data = new Regex( @"(?:\[(?<prefix>[-\/]?)(?<switch>[a-zA-Z][a-zA-Z0-9]*|[a-zA-Z]+[|][a-zA-Z0-9]+|\x22?[a-zA-Z][\w ]*[a-zA-Z0-9]\x22?)(?<data>(?<sep>[=:])(?<value>(?:\x22[^\x22]*\x22|[\w |&;]*)))?\])", RegexOptions.ExplicitCapture );

					if ( data.IsMatch( value ) )
					{
						syntax = "";
						foreach ( Match m in data.Matches( value ) )
						{
							syntax += "{9}[{B}";
							if ( m.Groups[ "prefix" ].Success && !string.IsNullOrEmpty( m.Groups[ "prefix" ].Value ) && (m.Groups[ "switch" ].Value.IndexOf( ' ' ) < 0) )
							{
								syntax += m.Groups[ "prefix" ].Value;
								if ( m.Groups[ "switch" ].Success )
									syntax += m.Groups[ "switch" ].Value;
							}
							else
								if ( m.Groups[ "switch" ].Success )
								syntax += "{A}" + Regex.Replace( m.Groups[ "switch" ].Value, @"([^|])(\|)([^|])", "$1{4}|{A}$3" );

							if ( m.Groups[ "data" ].Success )
							{
								syntax += "{7}" + (m.Groups[ "sep" ].Success ? m.Groups[ "sep" ].Value : ":");

								if ( m.Groups[ "value" ].Success )
								{
									string v = (m.Groups[ "value" ].Value[ 0 ] == '"')
										?
										"{3}\"{E}" + m.Groups[ "value" ].Value.Trim( new char[] { '"' } ) + "{3}\""
										:
										"{E}" + m.Groups[ "value" ].Value;
									syntax += Regex.Replace( v, @"([^|]*)([|]+)([^|]*)", "{E}$1{3}|{E}$3" );
								}
							}
							syntax += "{9}] ";
						}
					}
				}
				return syntax.Trim();
			}
		}

		public bool SuppressPrompt { get; set; } = false;

		public string Purpose => MyPurpose();

		public bool HasPurpose => !string.IsNullOrWhiteSpace( MyPurpose() );

		public bool HasHelp => !string.IsNullOrWhiteSpace( MyHelp() );

		protected string AppName => Process.GetCurrentProcess().ProcessName;

		#endregion

		#region Operators
		public static bool operator ==( CmdletFoundation left, string right )
		{
			if ( left is null ) return string.IsNullOrEmpty( right );
			if ( string.IsNullOrWhiteSpace( right ) ) return false;
			return left.Cmd.Equals( right, StringComparison.OrdinalIgnoreCase );
		}

		public static bool operator !=( CmdletFoundation left, string right ) => !(left == right);
		#endregion

		#region Methods
		protected virtual string MySyntax() => "";

		protected virtual string MyHelp() => "";

		protected abstract string MyPurpose();

		//public string About() => _suppressVer ? "" :
		//	"{}&raquo;{3,3}Cmdlet {7}[{B}$1{7}({E}$2{7})] version {A,rn}$3".Replace(
		//		new object[] { this.GetType().Name, Cmd, Assembly.GetEntryAssembly().GetName().Version }
		//	);

		//public string Details() =>
		//	"$3{,3}&raquo; {F}Syntax{7,rn}:{,7}> {F}$2$4{,rn}$5{,rn}".Replace(
		//		new object[] { Cli.Prompt.Value, Cmd,
		//			string.IsNullOrWhiteSpace( Purpose ) ? "" : "{,3}&raquo; {F}Purpose{7}: {9}" + Purpose + "{,rn}",
		//			string.IsNullOrWhiteSpace( Syntax ) ? "" : " " + Syntax,
		//			string.IsNullOrWhiteSpace( MyHelp() ) ? "" : "\n{,3}&raquo; {F}Notes{7,rn}: " + MyHelp()
		//		}
		//	);

		public virtual string About() => 
			Language.Prompt.Get( "About", new object[] { this.GetType().Name, Cmd, Assembly.GetEntryAssembly().GetName().Version } );

		public string Details()
		{
			if ( _suppressVer ) Tec( About() ); // If the Ver prompt is disabled, we still want to show it here!
			return Language.Prompt.Get( "Details",
				new object[] { Cli.Prompt.Value, Cmd,
					string.IsNullOrWhiteSpace( Purpose ) ? "" : Language.Prompt.Get( "Purpose" ) + Purpose + "{,rn}",
					string.IsNullOrWhiteSpace( Syntax ) ? "" : " " + Syntax.Trim(),
					string.IsNullOrWhiteSpace( MyHelp() ) ? "" : Language.Prompt.Get( "Help" ) + MyHelp()
				}
			);
		}

		/// <summary>Used when the Cmdlets are installed into the CLI to initialize the environment.</summary>
		public abstract void Installer();

		/// <summary>Provides a means of validating that a specified rank is sufficient to perform an action.</summary>
		/// <remarks>If the "possessed" value is unspecified, the rank of the currently logged-on user is substituted instead.</remarks>
		protected bool ValidateAccess( Ranks reqd, Ranks possessed = Ranks.Unverified )
		{
			if ( User.Rank.IsAllowed( reqd ) ) return true;

			if ( possessed == Ranks.Unverified ) possessed = Cli.LocalUser.Rank;
			Tec( Language.Prompt.Get( 0 ), new object[] { possessed, reqd } );
			return false;
		}

		#region Console output API
		protected void Tec( string what ) => Con.Tec( what );

		protected void Tec( string what, object item ) => Con.Tec( what, item );

		protected void Tec( string what, object[] items ) => Con.Tec( what, items );
		#endregion

		/// <summary>
		/// This is the function that is called when the cmdlet is invoked. It provides basic functions that prepare every
		/// applet for operation, then hands off control via the "PeformWork()" function.
		/// </summary>
		/// <param name="cli">A reference to the controlling CommandLineInterface object that instantiated the cmdlet.</param>
		/// <param name="cmd">A reference to the Command object that contains all the switches and parameters from the invoking command line.</param>
		/// <returns>An Applets.OperationalStates value telling the CLI the outcome of the operation.</returns>
		public Applets.OperationalStates Execute( Command cmd )
		{
			Applets.OperationalStates result = Applets.OperationalStates.IncompleteWithErrors;
			if ( !(cmd is null) )
			{
				this.Command = cmd;
				this.Installer(); // Ensure that the environment is configured for the command.

				result = Applets.OperationalStates.Running;
				Console.CursorVisible = false;
				if (!SuspendVersion) Tec( About() );
				if ( !Cmd.Equals( "?" ) && (Switches.HasSwitch( "?" ) || Switches.HasSwitch( "help" ) || cmd.Payload.Equals( "?" ) || cmd.Payload.Equals( "help", StringComparison.OrdinalIgnoreCase )) )
				{
					Tec( Details() );
					result = Applets.OperationalStates.Complete;
				}
				else
				{
					// Parse all passed switches, and invoke any methods that are configured to process them.
					Applets.OperationalStates swResult = Applets.OperationalStates.Running;
					if ( Switches.Count > 0 )
						foreach ( CommandLineSwitch s in Switches )
						{
							BindingFlags bf = BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic;
							MethodInfo mi = this.GetType().GetMethod( s.Id, bf, null, new Type[] { typeof( Applets.OperationalStates ) }, null );
							if ( !(mi is null) )
								//swResult = (Applets.OperationalStates)mi.Invoke( this, new object[] { swResult } );
								try { swResult = (Applets.OperationalStates)mi.Invoke( this, new object[] { swResult } ); }
								catch ( TargetInvocationException tie )
								{
									Applets.ExceptionDump( tie.InnerException );
									swResult = Applets.OperationalStates.IncompleteWithErrors;
								}

							if (
								(swResult != Applets.OperationalStates.None) &&
								(swResult != Applets.OperationalStates.Idle) &&
								(swResult != Applets.OperationalStates.Running)
								)
								return swResult;
						}

					result = Main();
				}

				Console.CursorVisible = true;
			}
			return result;
		}

		/// <summary>Facilitates retrieving saved settings from Windows' Registry.</summary>
		/// <param name="valueName">The name of the valueKey from which to obtain the data.</param>
		/// <param name="key">The registry (sub)key in which the value resides.</param>
		/// <param name="hive">The registry hive where the (sub)key is stored.</param>
		/// <param name="view">Which registry (32- or 64- bit) is being used.</param>
		/// <returns>If the specified key/value exists, whatever data it holds.</returns>
		/// <remarks>Any error that occurs when this is attempted will be returned in "RegMgmt.LastError"</remarks>
		protected object GetRegistrySetting( string valueName, string key, RegistryHive? hive = null, RegistryView? view = null )
		{
			if ( hive is null ) hive = RegistryHive.CurrentUser;
			if ( view is null ) view = RegistryView.Default;
			return RegMgmt.GetValue( valueName, key, hive, view );
		}

		/// <summary>Facilitates sending settings to the Windows Registry to be saved.</summary>
		/// <param name="valueName">The name of the valueKey from which to obtain the data.</param>
		/// <param name="key">The registry (sub)key in which the value resides.</param>
		/// <param name="value">The value to save to the specified key -- must be a type compatible with the key/value!</param>
		/// <param name="type">What kind of value we're sending (must match the type defined for the specified value in the Registry!)</param>
		/// <param name="view">Which registry (32- or 64- bit) is being used.</param>
		/// <returns>TRUE if the operation was a success.</returns>
		/// <remarks>
		/// 1) Any error that occurs when this operation is attempted will be returned in "RegMgmt.LastError"
		/// 2) This function only permits setting values in the HKCU hive of the Default view.
		/// </remarks>
		protected bool SetRegistrySetting( string valueName, string key, object value, RegistryValueKind? type )
		{
			RegistryValueKind kind = ( type is null ) ? RegistryValueKind.String : (RegistryValueKind)type;
			return RegMgmt.SetValue(valueName, value, key, kind, RegistryHive.CurrentUser, RegistryView.Default );
		}

		/// <summary>This is the method that gets called when the cmdlet is executed.</summary>
		/// <returns>An Applets.OperationalStates value giving some indication of what happened.</returns>
		protected abstract Applets.OperationalStates Main();

		public override bool Equals( object obj ) =>
			base.Equals( obj );

		public override int GetHashCode() =>
			base.GetHashCode();

		protected static string MassReplace( string source, string[] replacements)
		{
			if (!string.IsNullOrWhiteSpace(source) && (source.IndexOf("$") >= 0) && !(replacements is null) && (replacements.Length > 0))
			{
				for ( int i = 0; i < replacements.Length; i++ )
					source = source.Replace( "$" + (i + 1), replacements[ i ] );
			}
			return source;
		}

		// Replaced by the Type extension method, "HasAncestor<T>" from NetXpertExtensions (Extensions.cs)
		//public static bool IsCmdlet( Type type ) =>
		//	(type.BaseType == typeof( Object )) ? false : (type.BaseType == typeof( CmdletFoundation ) ? true : IsCmdlet( type.BaseType ));

		/// <summary>Performs pattern testing on a string to see if it's in a form recognizable as an absolute path.</summary>
		/// <param name="pathToTest">The string to test.</param>
		/// <param name="testExists">If TRUE, this also verifies that the specified path exists.</param>
		/// <returns>TRUE if the contents of the passed string are valid, and, if requested, the path exists.</returns>
		public static bool ValidateWindowsPath( string pathToTest, bool testExists = false )
		{
			if ( !string.IsNullOrWhiteSpace( pathToTest ) )
			{
				string drivePattern = /* language=regex */
						   @"^(([A-Z]:(?:\.{1,2}[\/\\]|[\/\\])?)|([\/\\]{1,2}|\.{1,2}[\/\\]))?",
						pattern = drivePattern + /* language=regex */
						   @"([^\x00-\x1A|*?\t\v\f\r\n+\/,;""'`\\:<>=[\]]+[\/\\]?)+$";

				if ( Regex.IsMatch( pathToTest, pattern, RegexOptions.ExplicitCapture ) )
				{
					pattern = drivePattern + /* language=regex */
						@"(([^\/\\. ]|[^\/. \\][\/. \\][^\/. \\]|[\/\\]$)*[^\x00-\x1A|*?\s+,;""'`:<.>=[\]])$";
					if ( Regex.IsMatch( pathToTest, pattern, RegexOptions.ExplicitCapture ) )
						return !testExists || Directory.Exists( pathToTest );
				}
			}
			return false;
		}
		#endregion
	}

	public class CmdletCollection : IEnumerator<CmdletFoundation>
	{
		#region Properties
		protected List<CmdletFoundation> _cmds = new List<CmdletFoundation>();
		protected int _position = 0;
		#endregion

		#region Constructors
		public CmdletCollection() { }

		public CmdletCollection( CmdletFoundation cmd ) => this.Add( cmd );

		public CmdletCollection( CmdletFoundation[] cmds ) => this.AddRange( cmds );

		public CmdletCollection( CmdletCollection cmds ) => this.AddRange( cmds.ToArray() );
		#endregion

		#region Accessors
		public int Count => _cmds.Count;

		public dynamic this[ string cmd ]
		{
			get
			{
				int i = IndexOf( cmd );
				return (i < 0) ? null : _cmds[ i ];
			}
		}

		public CmdletFoundation this[ int index ] =>
			this._cmds[ index ];

		public string[] CmdList
		{
			get
			{
				List<string> cmds = new List<string>();
				foreach ( CmdletFoundation cmd in _cmds )
					cmds.Add( cmd.Cmd );

				return cmds.ToArray();
			}
		}

		CmdletFoundation IEnumerator<CmdletFoundation>.Current => this._cmds[ this._position ];
		object IEnumerator.Current => this._cmds[ this._position ];
		#endregion

		#region Methods
		protected int IndexOf( string cmd )
		{
			if ( string.IsNullOrWhiteSpace( cmd ) ) return -1;

			int i = -1; while ( (++i < Count) && !_cmds[ i ].Cmd.Equals( cmd, StringComparison.OrdinalIgnoreCase ) ) ;
			return (i < Count) ? i : -1;
		}

		public void Add( CmdletFoundation cmd )
		{
			if ( !(cmd is null) )
			{
				int i = IndexOf( cmd.Cmd );
				if ( i < 0 )
					_cmds.Add( cmd );
				else
					_cmds[ i ] = cmd;
			}
		}

		public void AddRange( CmdletFoundation[] cmds )
		{
			if ( !(cmds is null) && (cmds.Length > 0) )
			{
				foreach ( CmdletFoundation cmd in cmds )
					this.Add( cmd );

				Sort();
			}
		}

		/// <summary>Removes a CmdLet from the active list.</summary>
		/// <param name="cmd">The command name to remove.</param>
		public void Remove( string cmd )
		{
			if ( Regex.IsMatch( cmd, @"^([a-z][\w]{1,2}[,; ]+)+([a-z][\w]{1,2}[,; ]*)$", RegexOptions.IgnoreCase ) )
				RemoveRange( cmd.Split( new char[] { ',', ';', ' ' } ) );
			else
			{
				int i = IndexOf( cmd );
				if ( i >= 0 )
					_cmds.RemoveAt( i );
			}
		}

		public void RemoveRange( string[] cmds )
		{
			foreach ( string cmd in cmds )
				Remove( cmd );
		}

		public void Sort() =>
			_cmds.Sort( ( x, y ) => x.Cmd.CompareTo( y.Cmd ) );

		public CmdletFoundation[] ToArray() => _cmds.ToArray();

		public bool HasCmd( string cmd ) => IndexOf( cmd ) >= 0;

		public IEnumerator<CmdletFoundation> GetEnumerator() => this._cmds.GetEnumerator();

		bool IEnumerator.MoveNext() => (++this._position) < this.Count;

		void IEnumerator.Reset() => this._position = 0;

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose( bool disposing )
		{
			if ( !disposedValue )
			{
				if ( disposing )
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
			Dispose( true );
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion

		public static CmdletCollection GetAllCmdlets()
		{
			CmdletCollection cmdlets = new CmdletCollection();

			// Get a collection of all Assemblies in the current collection.
			Assembly myAssembly = Assembly.GetExecutingAssembly();
			foreach ( TypeInfo ti in myAssembly.DefinedTypes )
				if ( ti.HasAncestor<CmdletFoundation>() ) // CmdletFoundation.IsCmdlet( ti ))
					if ( !ti.Name.Equals("CmdletExit") || !string.IsNullOrWhiteSpace( CommandLineInterface.EXIT_COMMAND ) )
					{
						dynamic result = Activator.CreateInstance( ti );
						cmdlets.Add( result );
					}

			return cmdlets;
		}
		#endregion
	}

	// All Cmdlet derivatives defined below here are automatically incorporated into the base CLI at runtime unless
	// the execution name is included in the ".RevokedCmdlets" string for the relevant CommandLineInterface object.
	// NOTE: "Cli.RevokedCmdlets" holds a comma-separated list of commmands, in a string, that are ignored by the CLI).

	public class CmdletAlias : CmdletFoundation
	{
		public CmdletAlias() : base( "NAM" ) { }

		protected override string MyPurpose() => Language.Prompt.Get( "MyPurpose" );
		// "Facilitates managing CLI aliases ({F}Macros{9}).";

		protected override string MySyntax() =>
			"[alias=\"command\"] [/remove:alias] [/clear] [/list] [/save:reg|ini] [/load:reg|ini] [/limit:value]";

		protected override string MyHelp() => Language.Prompt.Get( "MyHelp", new object[] { Cli.Aliases.Limit } );
		//"{7,5}&bull;{,7}Aliases must be a minimum of {B}4 {,rn}characters in length." +
		//"{7,5}&bull;{,7rn}The data portion of an alias cannot contain quotes. If you need quotes in the data," +
		//"{,7}use the hex char identifier {E}\\x22 {,rn}instead.{7,5}&bull;{,7rn}Multiple aliases can be defined " +
		//"at the same time by separating them with spaces.{,7}For example{7,rn}:{,10}CS> {F}NAM {6}alias1{F}={B}" +
		//"\"{E}a macro{B}\" {6}alias2{F}={B}\"{E}another macro{B}\" {6}alias3{F}={B}\"{E}more macros{B}\"{,rn}" +
		//"{,rn}{7,5}&bull;{,7}Aliases can be removed individually with the {9}[{B}/remove{7}:{e}alias{9}]{,rn} " +
		//"switch,{,7rn}or in a group by setting them to an empty value.{,7}For example all of the following are " +
		//"valid{7,rn}:{,10}CS> {F}NAM {B}/remove{7}:{6,rn}alias1{,10}CS> {F}NAM {6}alias2{F}={B}\"\" {6}alias3" +
		//"{F}={,rn}{,rn}{7,5}&bull;{,7}The {B}/clear{}, {B}/load{}, and {B}/save{,rn} switches will prompt for " +
		//"confirmation if there is a{,7}potential for data loss. If you wish to override this inquiry, add " +
		//"{e}silent {,rn}to the{,7}instruction like so{7,rn}:{,10}CS> {F}NAM {B}/clear{7}:{e,rn}silent{,10}CS> " +
		//"{F}NAM {B}/save{7}:{6}reg{F}|{e,rn}silent{,rn}{7,5}&bull;{,7}The {B}/limit {}switch is only available " +
		//"to users with {3}System{7}.{e}Admin {,rn}or higher access and{,7}is range limited between {b}0 {}and " +
		//"{b}255{,rn}.{,rn}{B,5}NOTE{F}: {}Currently the system only permits a maximum of {B}$1 {,rn}Aliases to " +
		//"be defined per user.".Replace( new object[] { Cli.Aliases.Limit });

		public override void Installer() { }

		private Applets.OperationalStates Remove( Applets.OperationalStates runState )
		{
			if ( Cli.Aliases.HasAlias( Command.Switches[ "remove" ].Value ) )
			{
				Cli.Aliases.Remove( Command.Switches[ "remove" ].Value );
				Tec( Language.Prompt.Get( 0 ), Command.Switches[ "remove" ].Value );
				// "{,3}&bull; {}Removed alias{7}: {9}\"{E}$1{9,rn}\""
			}
			else
				Tec( Language.Prompt.Get( 6 ), Command.Switches[ "remove" ].Value );
				// "{,3}&raquo; There is no alias called {9}\"{e}$1{9,}\" {,rn}to remove."
			return Applets.OperationalStates.Complete;
		}

		private Applets.OperationalStates Clear( Applets.OperationalStates runState )
		{
			if ( Cli.Aliases.Count > 0 )
			{
				bool proceed = Command.Switches[ "clear" ].HasValue( "silent" );
				if ( !proceed )
				{
					Tec( Language.Prompt.Get( 12 ) );
					proceed = Con.AreYouSure( Language.Prompt.Get( 13 ) );
				}

				if ( proceed ) Cli.Aliases.Clear();

				Tec( $"{Language.Prompt.Get( proceed ? 14 : 15 )}{{7,rn}}!" );
				//Con.Tec( $"{Language.Prompt.Get( proceed ? 14 : 15 )}{{7,rn}}!" );
				// "{A}Cleared{7,rn}!" / "{C,5}Aborted{7,rn}!"
				runState = Applets.OperationalStates.Complete;
			}
			else
			{
				Tec( Language.Prompt.Get( 16 ) );
				runState = Applets.OperationalStates.CompleteWithErrors;
				// "{,3}&raquo;{7,5}There are no aliases defined: {F,rn}There's nothing to clear!"
			}

			return runState;
		}

		private Applets.OperationalStates List( Applets.OperationalStates runState )
		{
			if ( Cli.Aliases.Count > 0 )
			{
				Cli.Aliases.Sort();
				// "{,3}&raquo;{7,5}The following {E}$1 {7,rn}alias$2 are currently defined:"
				Tec( Language.Prompt.Get( 17 ), new object[] { Cli.Aliases.Count, (Cli.Aliases.Count == 1 ? "" : "es") } );
				Tec( Language.Prompt.Get( 18 ) + "{,rn,>*'\x2500'}    " ); // "{B,5}Alias{B,20rn}Content"
																		   //Con.Tec( Language.Prompt.Get( 17, new object[] { Cli.Aliases.Count, (Cli.Aliases.Count == 1 ? "" : "es") } ) );
																		   //Con.Tec( Language.Prompt.Get( 18 ) + "{,rn,>*'\x2500'}    " ); // "{B,5}Alias{B,20rn}Content"
				foreach ( AliasDefinition ad in Cli.Aliases )
					Tec( "{6,5}$1{F,20}\"{9}$2{F,rn}\"", new object[] { ad.Alias, ad.Value } );
				//Con.Tec( "{6,5}$1{F,20}\"{9}$2{F,rn}\"", new object[] { ad.Alias, ad.Value } );

				Tec( "{,rn,>*'\x2500'}    " );
				//Con.Tec( "{,rn,>*'\x2500'}    " );
			}
			else
			{
				Con.Tec( Language.Prompt.Get( 19 ) );
				// "{,3}&raquo;{7,5rn}There are no aliases defined for you currently."
			}

			return Applets.OperationalStates.Complete;
		}

		private Applets.OperationalStates Save( Applets.OperationalStates runState )
		{
			if ( Command.Switches[ "save" ].HasValue( "ini" ) )
			{

			}

			if ( Command.Switches[ "save" ].HasValue( "reg" ) )
			{
				if ( Command.Switches[ "save" ].HasValue( "clear" ) )
				{
					bool proceed = Command.Switches[ "save" ].HasValue( "silent" );
					if ( !proceed )
					{
						// "{C,5}Warning{7}: {a,rn}This operation will permanently remove your saved aliases from the Registry,"
						Tec( Language.Prompt.Get( 20 ) );
						proceed = Con.AreYouSure( Language.Prompt.Get( 13 ) );
						// "{a,14}are you Sure? {7}({A}Y{7})es or ({C}N{7})o? "
					}
					if ( proceed )
					{
						SetRegistrySetting( "Aliases", $"SOFTWARE\\NetXpert\\{MiscellaneousExtensions.ExecutableName}\\Settings", new string[] { }, RegistryValueKind.MultiString );
						Tec( Language.Prompt.Get( 21 ) ); // "{,3}&raquo;{7,5rn}The settings have been cleared."
					}
					else
						Con.Tec( "{,5}$1{7,rn}!", Language.Prompt.Get( 15 ) );
				}
				else
				{
					if ( Cli.Aliases.Count == 0 )
						Tec( Language.Prompt.Get( 22 ) );
						// "{,3}&raquo;{7,5}There are no aliases to save. {,rn}If you want to clear the saved settings use:{,7}&raquo;{7,9}NAM {3}/save:{e,rn}reg|clear"
					else
					{
						Tec( Language.Prompt.Get( 23 ) );
						//Con.Tec( "{8,5}-> {9}\"{6}$1{9,rn}\"", Cli.Aliases.ToString() ); // {6}$1{9,rn}\"", temp.ToString() );
						Tec( "{8,5}-> {9}\"{6}$1{9,rn}\"", Cli.Aliases.ToString() ); // {6}$1{9,rn}\"", temp.ToString() );

						IniMultiString temp = new IniMultiString( Cli.Aliases.ToString() );
						SetRegistrySetting( "Aliases", $"SOFTWARE\\NetXpert\\{MiscellaneousExtensions.ExecutableName}\\Settings", temp.ToArray(), RegistryValueKind.MultiString );

						Tec( "{,3}&raquo;" );
						if ( RegMgmt.LastError == "" )
							Tec( Language.Prompt.Get( 24 ) );
						else
							Tec( Language.Prompt.Get( 25 ) + " {F}\"{7}$1{F,rn}\"", RegMgmt.LastError ); // {C}Error:
					}
				}
			}

			return runState;
		}

		private Applets.OperationalStates Load( Applets.OperationalStates runState )
		{
			if ( Command.Switches[ "load" ].HasValue( "ini" ) )
			{

			}

			if ( Command.Switches[ "load" ].HasValue( "reg" ) )
			{
				try
				{
					var temp = (string[])GetRegistrySetting( "Aliases", $"SOFTWARE\\NetXpert\\{MiscellaneousExtensions.ExecutableName}\\Settings" );
					if ( RegMgmt.LastError.Length > 0 )
						Tec( "{,3}&raquo;{,5}$2{F}\"{7}$1{F,rn}\"", new object[] { RegMgmt.LastError, Language.Prompt.Get( "Main25" ) } );
					else
					if ( (RegMgmt.LastError == "") && !(temp is null) && (temp.Length > 0) )
					{
						// "{,3}&raquo;{E,5}$1 {}alias$2 were found in the Registry, there $4 currently {e}$3 {,rn}defined in the CLI."
						Tec( Language.Prompt.Get( 26 ), new object[] { temp.Length, temp.Length == 1 ? "" : "es", Cli.Aliases.Count, Cli.Aliases.Count == 1 ? "is" : "are" } );
						bool proceed = (Cli.Aliases.Count == 0) || Command.Switches[ "load" ].HasValue( "silent" );
						if ( !proceed )
						{
							// "{C,5}Warning{7}: {a,rn}This operation will remove your existing aliases and{a,14rn}replace them with the ones stored in the Registry,"
							Tec( Language.Prompt.Get( 27 ) );
							proceed = Con.AreYouSure( Language.Prompt.Get( 13 ) ); // "{a,15}are you Sure? {7}({A}Y{7})es or ({C}N{7})o? "
							if ( !proceed ) Tec( "{,5}$1{7,rn}!", Language.Prompt.Get( 15 ) );
						}

						if ( proceed )
						{
							if ( Cli.Aliases.Count > 0 )
							{
								foreach ( AliasDefinition ad in Cli.Aliases )
									Tec( "{,7}&raquo; $3: {7}\"{6}$1{7}\" {3}-> {7}\"{e}$2{7,rn}\"", new object[] { ad.Alias, ad.Value, Language.Prompt.Get( 28 ) } );

								// {,7}&raquo;{e,9}$1 {,rn}existing alias$2 were cleared.
								Tec( Language.Prompt.Get( 29, new object[] { Cli.Aliases.Count, Cli.Aliases.Count == 1 ? "" : "es" } ) );
								Cli.Aliases.Clear();
							}

							foreach ( string s in temp )
							{
								AliasDefinition a = AliasDefinition.Parse( s );
								Tec( "{,7}&raquo; $3:  {7}\"{6}$1{7}\" {3}-> {7}\"{e}$2{7,rn}\"", new object[] { a.Alias, a.Value, Language.Prompt.Get( 30 ) } );
								Cli.Aliases.Add( a );
							}
							Tec( Language.Prompt.Get( 31 ) ); // "{,5}&raquo;{7,7rn}The saved settings have been restored."
						}
					}
				}
				catch
				{ }
			}

			return runState;
		}

		private Applets.OperationalStates Limit( Applets.OperationalStates runState )
		{
			if ( Cli.LocalUser.Rank.IsAllowed( Ranks.SystemAdmin ) )
			{
				string value = Regex.Replace( Command.Switches[ "limit" ].Value, @"[^\d]", "" );
				if ( Regex.IsMatch( value, @"^([01]?[\d]{0,2}|2[0-4][\d]|25[0-5])$" ) )
				{
					int limit = int.Parse( value );
					if ( limit != Cli.Aliases.Limit )
					{
						// "{,3}&raquo;{a,5}Resizing the {f}Alias {a}table{7}: {7}From {e}$1 {7}to {e}$2 {7}items{}... "
						Tec( Language.Prompt.Get( 7, new object[] { Cli.Aliases.Limit, value } ) );
						AliasCollection backup = new AliasCollection( limit );
						for ( int i = 0; i < Math.Min( limit, Cli.Aliases.Count ); i++ )
							backup.Add( Cli.Aliases[ i ] );
						Cli.Aliases = backup;
						Tec( Language.Prompt.Get( 8, new object[] { limit, limit.Pluralize() } ) );
						// "{B,rn}Done.{,3}&raquo;{a,5}The table has been successfully reset to a maximum of {e}$1 {,rn}item$2."
						runState = Applets.OperationalStates.Complete;
					}
					else
					   Tec( Language.Prompt.Get( 9 ) ); // "{,3}&raquo;{7,5rn}Nothing to do."
				}
				else
				{
					// "{,3}&raquo; {C}Error{7}: {}The allowable alias range is {e}0 {}to {e}255{,rn}."
					Tec( Language.Prompt.Get( 18 ) );
					runState = Applets.OperationalStates.CompleteWithErrors;
				}
			}
			else
			{
				runState = Applets.OperationalStates.CompleteWithErrors;
				Tec( Language.Prompt.Get( 11 ) );
				// "{,3}&raquo; {C}Error{7}: {}You must be a {3}System{7}.{e}Admin {}to alter the system's {F}Alias{,rn} limit."
			}

			return runState;
		}

		protected override Applets.OperationalStates Main()
		{
			AliasCollection aliases = new AliasCollection( Cli.Aliases.Limit );
			if ( string.IsNullOrWhiteSpace( Command.Payload ) )
				return List( Applets.OperationalStates.Running );

			Regex pattern = new Regex( @"(?<=[\s]|^)(?:(?<alias>[a-z][a-z0-9]{3,7})[=:])(?<payload>(?:""(?<value>[^""\t\f\v\x00-\x1a]+)?""))?", RegexOptions.IgnoreCase );
			if ( pattern.IsMatch( Command.Payload ) )
			{
				foreach ( Match m in pattern.Matches( Command.Payload ) )
				{
					string alias = m.Groups[ "alias" ].Value,
						value = m.Groups[ "value" ].Value;

					if ( !string.IsNullOrWhiteSpace( alias ) )
					{
						if ( string.IsNullOrWhiteSpace( value ) )
						{
							if ( Cli.Aliases.HasAlias( alias ) )
							{
								Cli.Aliases.Remove( alias );
								Tec( Language.Prompt.Get( 0 ), alias );
								// {,3}&bull; {}Removed alias{7}: {9}\"{E}$1{9,rn}\"

							}
							else
								Tec( Language.Prompt.Get( 1 ), alias );
								// {,3}&raquo; {}There is no alias called {9}\"{E}$1{9}\"{,rn} to remove.
						}
						else
							if ( !(Cli.Applets is null) )
							{
								if ( !Cli.Applets.HasCommand( alias ) )
								{
									if ( (Cli.Aliases.Count < 10) || Cli.Aliases.HasAlias( alias ) )
									{
										aliases.Add( new AliasDefinition( alias, value ) );
										Tec( Language.Prompt.Get( 2, new object[] { alias, value } ) );
										// {,3}&bull;{,5}Added alias{7}: {9}\"{E}$1{9}\"{7,32}-\x25ba {9,36}\"{6}$2{9,rn}\"
									}
									else
										Tec( Language.Prompt.Get( 3 ), Cli.Aliases.Count );
										// {,3}» {7,5}You cannot declare more than {9}$1 {7,rn}aliases.{7,5rn}You'll need to remove an existing alias to add another one.
								}
								else
									Tec( "", alias.ToUpperInvariant() );
									// {,3}&raquo;{C,5}Error{7}: {7}You cannot use {9}\"{E}$1{9}\" {7,rn}as an alias because it's already the name of an installed applet.
							}
					}
					else
						Tec( Language.Prompt.Get( 4 ), m.Value );
						// "{,5}&raquo;{E,7}Error: {9}\"{6}$1{9}\" {7}is not in a recognized form."
				}
				Cli.Aliases.AddRange( aliases );
			}
			else
				if ( (Command.Payload.Length > 0) && (Command.CmdLine.SwitchCount == 0) )
					Tec( Language.Prompt.Get( 5 ) );

			//"{,3}&raquo;{7,5}Alias creation {C}Failed{7}! -- {A,rn}The supplied alias declaration does not conform to a valid structure." +
			//"{E,3}Remember{7,rn}:{,5}&bull; {7,rn}The alias itself must be a minimum of 4 characters." +
			//"{,5}&bull; {7}The definition must be enclosed in double-quotes and {C}CAN NOT {7,rn}contain double-quotes.{,rn}"

			return Applets.OperationalStates.Complete;
		}
	}

	public class CmdletAttach : CmdletFoundation
	{
		public CmdletAttach() : base( "ATT" ) { }

		protected override string MySyntax() => "[dllName] [/onpath=\"{path spec}\"]";

		protected override string MyPurpose() => Language.Prompt.Get( "MyPurpose" );
		// "Dynamically incorporates an external library (DLL) into the application.";

		protected override string MyHelp() => Language.Prompt.Get( "MyHelp" );

		public override void Installer() { }

		protected string SearchFor( string fileName, string searchPath = "" )
		{
			List<string> path = new List<string>( new string[] { MiscellaneousExtensions.LocalExecutablePath, } );

			if ( Cli.Env.HasVariable("CWD") && !path[ 0 ].Equals( Cli.Env[ "CWD" ].Value, StringComparison.OrdinalIgnoreCase ) )
				path.Add( Cli.Env[ "CWD" ].Value );

			if ( Cli.Env.HasVariable("WinDir") && !path.Contains( Cli.Env["WinDir"].Value) )
				path.Add( Cli.Env[ "WinDir" ].Value );

			if ( Command.CmdLine.HasSwitch( "onpath" ) ) 
				searchPath += Command.CmdLine[ "onpath" ].Value.Unwrap( StripOuterOptions.DoubleQuotes | StripOuterOptions.SingleQuotes );

			if ( !string.IsNullOrWhiteSpace( searchPath ) )
			{
				string[] p = searchPath.Split( new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries );
				if (p.Length > 0)
					foreach( string pt in p )
						if (ValidateWindowsPath( pt, true ))
							path.Add( pt );
			}

			int i = -1; while ( ++i < path.Count )
			{
				if ( !Regex.IsMatch( path[ i ], @"[\\]$" ) ) path[ i ] += "\\";
				if ( File.Exists( path[ i ] + fileName ) ) return path[i] + fileName; 
			}

			return "[FileNotFound]";
		}

		protected override Applets.OperationalStates Main()
		{
			if ( (Command.Payload.Length > 0) && (Command.CmdLine.ArgCount > 0) )
			{
				string fileName = Command.Args[ 0 ] + ".dll";
				string dllName = SearchFor( fileName ); // NetXpertExtensions.LocalExecutablePath + fileName;
				Con.Tec( Language.Prompt.Get( 0, new object[] { Cli.Color, fileName } ) );
				// "{,3}&raquo;{7,5}Attempting to attach {7}\"{E}$2{7}\"{$1}..."
				if ( !fileName.Equals("[FileNotFound]" ) ) //File.Exists( dllName ) )
				{
					try
					{
						Assembly assembly = Assembly.LoadFrom( dllName );
						AppDomain.CurrentDomain.Load( assembly.GetName() );
						Con.Tec( "{7}[{A}\x221a{7}] {9,rn}$1!", Language.Prompt.Get( 1 ) );
					}
					catch ( Exception e )
					{
						return Applets.ExceptionDump( e, Cli.HasEnvVar( "DEBUG" ) );
						//Con.Tec( "{7}[{C}X{7}]{C,rn}Failed!{C}Error: {,rn}$1", e.Message );
					}
				}
				else
				{
					Con.Tec( Language.Prompt.Get( 2, new object[] { TextElement.CheckBoxLn( false ), dllName } ) );
					// "{}$1{,rn}The requested DLL file was not found!{,3}&raquo; {7}[{E}$2{7,rn}]"
				}
			}
			else
				Con.Tec( Language.Prompt.Get( 3 ) );
			// "{}You need to specify the name of the DLL file ({B}without extension!{,rn}) that you'd like to add."

			return Applets.OperationalStates.Complete;
		}
	}

	public class CmdletCls : CmdletFoundation
	{
		public CmdletCls() : base( "CLS" ) { }

		protected override string MyPurpose() => Language.Prompt.Get( "MyPurpose" );
		// "Clears the screen and CLI scoll-back buffer.";

		public override void Installer() { }

		protected override Applets.OperationalStates Main()
		{
			Con.DefaultColor.ToConsole(); // Ensure default colour is applied before clearing the screen.
			Console.Clear();
			Cli.Prompt.Write();
			SuppressPrompt = true;
			return Applets.OperationalStates.Complete;
		}
	}

	public class CmdletCommand : CmdletFoundation
	{
		#region Properties
		private string root = Assembly.GetEntryAssembly().GetName().Name + ".ExternalResources.";
		#endregion

		#region Constructor
		public CmdletCommand() : base( "CMD" ) { }
		#endregion

		#region Help
		protected override string MySyntax() => Language.Prompt.Get( "MySyntax" );
		// "[/INSTALL] [/UNINSTALL] [/RUN:scriptName] [/LIST:script|cmd|buffer] [/DISABLE:{cmdList}]";

		protected override string MyPurpose() => Language.Prompt.Get( "MyPurpose" );
		// "Manages CLI command functions, and scripts.";
		#endregion

		#region Utility Methods
		protected FileDataCollection ListScripts( string root, int startRec = -1, int recLength = -1 )
		{
			FileDataCollection scriptFiles = new FileDataCollection();
			string[] scripts = Assembly.GetEntryAssembly().GetManifestResourceNames();

			if ( scripts.Length > 0 )
				Con.Tec( Language.Prompt.Get( "0" ) );

			foreach ( string s in scripts )
				if (
					(s.Substring( 0, root.Length ) == root) &&
					Regex.IsMatch( s, @"\.script$", RegexOptions.IgnoreCase )
					)
				{
					string[] parts = Regex.Replace( s, @"^(.*)[\\.]([\w]+[.]script)$", "[$1]:$2", RegexOptions.IgnoreCase ).Split( new char[] { ':' } );
					scriptFiles.Add( new FileData( parts[ 1 ], parts[ 0 ] ) );
					//					scriptFiles.Add( new FileData( Regex.Replace( s.Replace( ".", "\\" ), @"^([\w]+)[\\]([\w]+)(script)$", "[$1]\\$2.$3", RegexOptions.IgnoreCase ) ) );
				}

			FileDataCollection dir;
			foreach ( string place in new string[] { Cli[ "CWD" ], Cli[ "HOMEPATH" ] + "\\SCRIPTS", Cli[ "HOMEPATH" ] } )
			{
				dir = new FileDataCollection( place );
				scriptFiles.AddRange( dir.ToArray( "*.script" ) );
			}

			if ( scriptFiles.Count > 0 )
			{
				int count = 0,
					start = startRec.InRange( scriptFiles.Count, 0, NetXpertExtensions.Classes.Range.BoundaryRule.Loop ) ? startRec : 0,
					length = (recLength < 0) ? scriptFiles.Count - start : Math.Min( Math.Max( 0, scriptFiles.Count - start ), recLength );

				scriptFiles.Sort();
				//foreach ( FileData file in scriptFiles )
				for ( int i = 0; i < length; i++ )
				{
					FileData file = scriptFiles[ i + start ];
					string path = file.Path.Condense( Console.BufferWidth - 30, "{F}...{9}" );
					if ( Regex.IsMatch( path, @"[\[]([\w{}.]+)[\]]" ) )
						path = Regex.Replace( path.Replace( ".", "{7}.{e}" ), @"^[\[]([{}\w.]+)[\]]$", "{9}[{B}$1{9}]" );
					else
						path = Regex.Replace( path, @"(^[a-z]:[\\]?|[\\])", "{1}$1{9}", RegexOptions.IgnoreCase );

					Con.Tec( "{6,,<3}$1:{B,5}$2{9,32}$3{,rn} ", new object[] { ++count, file.FileName, path } );
				}

				// scripts were found | script was found
				Con.Tec( "{,rn,>*'─'}─{F,2}$1 {,rn}$2.{,rn,>*'═'}═", new object[] { count, Language.Prompt.Get( ((count == 1) ? "2" : "3") ) } );
			}
			else
				Con.Tec( Language.Prompt.Get( "1" ) ); // "{,rn}No scripts were found.";

			return scriptFiles;
		}

		public override void Installer() { }
		#endregion

		#region Switch Methods
		private Applets.OperationalStates Run( Applets.OperationalStates runState )
		{
			string scriptName = Command.Switches[ "RUN" ].Value;

			if ( Switches[ "RUN" ].IsEmpty )
			{
				// "{f,3}&raquo;{C,5rn}No script name was specified!{f,3}&raquo;{A,5}Listing known scripts{7,rn}:"
				Tec( Language.Prompt.Get( 11 ) );
				FileDataCollection files = ListScripts( this.root );
				if ( files.Count > 0 )
				{
					Console.CursorTop -= 2; Console.CursorLeft = 0;
					// "{B,32}&raquo; {e}Please enter the number of the Script that you'd like to run {9}[{6}1{9}..{6}$1{9}]{7}: "
					Tec( Language.Prompt.Get( 12 ), files.Count );

					List<ConsoleKey> keys = new List<ConsoleKey>();
					keys.AddRange( new ConsoleKey[] { ConsoleKey.Escape, ConsoleKey.D0, ConsoleKey.NumPad1 } );
					for ( int i = 0; i < Math.Min( files.Count, 9 ); i++ )
						keys.AddRange( new ConsoleKey[] { (ConsoleKey)(49 + i), (ConsoleKey)(97 + i) } );
					ConsoleKey input = Con.ReadKey( keys.ToArray() );

					if ( (input == ConsoleKey.Escape) || (input == ConsoleKey.NumPad0) || (input == ConsoleKey.D0) )
					{
						Tec( Language.Prompt.Get( 13 ) ); // "{C,rn}Cancelled!{,rn} "
						return Applets.OperationalStates.Cancelled;
					}
					scriptName = files[ ((int)input - 1) & 0x0f ].FullFileName;
					Console.Write( Regex.Replace( input.ToString(), @"[^\d]", "" ) + "\r\n\r\n\r\n" );
					if ( Regex.IsMatch( scriptName, @"^[\[]([\w]+[.])+[\w]+[\]]\\" ) )
						scriptName = files[ ((int)input - 1) & 0x0f ].FileName;
					else
					{
						Cli.RunScript( File.ReadAllLines( scriptName ) );
						Tec( Language.Prompt.Get( 14 ), scriptName ); // "{}Running Script {7}[{B}$1{7}]:"
						return Applets.OperationalStates.Complete;
					}
				}
			}
			else
			{
				if ( !Regex.IsMatch( scriptName, @"\.script$", RegexOptions.IgnoreCase ) )
					scriptName = scriptName.TrimEnd( new char[] { '\\', '.' } ) + ".script";
			}

			if ( scriptName.Length > 0 )
			{
				if ( Cli.HasInternalScript( scriptName ) )
				{
					string file = MiscellaneousExtensions.FetchInternalResourceFile( this.root + scriptName );
					if ( file.Length > 0 )
					{
						Cli.RunScript( file, Command.User );
						// "{f}&raquo;{} Running Internal Script {7}[{B}$1{7,rn}]:"
						Tec( Language.Prompt.Get( 15 ), scriptName );
						return Applets.OperationalStates.Complete;
					}
				}
				else
				{
					string[] places = new string[] { Cli[ "CWD" ] + "\\Scripts", Cli[ "HOMEPATH" ] + "\\Scripts" };
					foreach ( string s in places )
						if ( Cli.HasEnvVar( s ) )
						{
							string path = Cli[ s ];
							if ( Directory.Exists( path ) )
							{
								FileData file = new FileDataCollection( path ).GetFile( scriptName + ".script", false );
								if ( !(file is null) )
								{
									Cli.RunScript( File.ReadAllLines( file.FullFileName ) );
									Con.Tec( Language.Prompt.Get( 16 ), scriptName );
									return Applets.OperationalStates.Complete;
								}
							}
						}
				}

				Con.Tec( Language.Prompt.Get( 17 ), Command.Switches[ "run" ].Value );
				//"{,3}&raquo;{C,5}Error: {}The requested file ({7}\"$1.script{7}\"{,rn}) could not " +
				//"be found.{,5rn}Below is a list of scripts that can be executed with " +
				//"this command:", Args.Args[ "run" ].Value

				if ( !Switches.HasSwitch( "list" ) || (!Switches[ "list" ].HasValue( "script" ) && !Switches[ "list" ].HasValue( "scripts" )) )
					Switches.Add( new CommandLineSwitch( "list", "scripts" ) );
			}
			else
			{
				Con.Tec( Language.Prompt.Get( 25 ) ); // "{C}Error: {,rn}No file specified to run!"
				runState = Applets.OperationalStates.CompleteWithErrors;
			}

			return runState;
		}

		private Applets.OperationalStates List( Applets.OperationalStates runState )
		{
			// If no object is specified, dump them all..
			if ( Command.Switches[ "list" ].IsEmpty ) Command.Switches[ "list" ].Value = "script|cmd|buffer";

			if ( Command.Switches[ "list" ].HasValue( new string[] { "script", "scripts", "scr", "s" } ) )
				ListScripts( this.root );

			if ( Command.Switches[ "list" ].HasValue( new string[] { "cmd", "cmds", "c" } ) )
			{
				if ( !(Cli.Applets is null) && (Cli.Applets.Count > 0) )
				{
					// "{,3}&raquo; The following {F}$1{} applets were found and have been installed{7,rn}:"
					Con.Tec( Language.Prompt.Get( 18 ), Cli.Applets.Count );
					foreach ( AppletDescriptor aF in Cli.Applets )
						// "{,5}&raquo; {7}Applet: {6}[{B}$1{6}] {F,27}&lbrace;{E}Rank: {C,,<3}$2{F}&rbrace;{D,54}-► {A,rn}Installed!"
						Con.Tec( Language.Prompt.Get( 19 ), new object[] { aF.Command, aF.RankReqd } );

					Con.Tec( Language.Prompt.Get( 20 ) );
					//"{Frn}  &raquo;{6,5}General information about these commands can be found by typing {9}\"{B}? {F}/" +
					//"{E}details{9}\"{6,rn}.{6,5}More specific information is available by typing the name of the command" +
					//"{7} ({6,rn}or{6,5}applet{7}){6} that you're interested in and adding the switch {9}\"{e}/?{9}\"{6,rn}."
				}
				else
					Con.Tec( Language.Prompt.Get( 21 ) );
			}

			if ( Command.Switches[ "list" ].HasValue( new string[] { "buff", "buffer", "b" } ) )
			{
				TextElementRuleCollection buffList = new TextElementRuleCollection(
					new TextElementRule[]
					{
							new TextElementRule( /* cmd */ /* language=regex */
								@"(?<=\]:)([?\w]+)(?=[\s])?", " {B}$1{D}", RegexOptions.IgnoreCase ),
							new TextElementRule( /* time grammar */ /* language=regex */
								@"([\d])([:.])([\d])", "$1{1}$2{3}$3", RegexOptions.None),
							new TextElementRule( /* time */ /* language=regex */
								@"(?:\[)([^\]]+)(?:\]:)", "{7}[{3}$1{7}]{8}:", RegexOptions.IgnoreCase ),
							new TextElementRule( /* rank */ /* language=regex */
								@"(?<=[\s])(&lbrace;)([\w ]+)(&rbrace;)", "{9,80}$1{E}$2{9}$3", RegexOptions.IgnoreCase )
					}
				);

				string buffer = "";
				foreach ( Command buff in Cli.CommandCache )
					buffer += buff.ToString() + " &lbrace;" + buff.AsRank.ToString() + "&rbrace; \r\n";

				// Con.Tec( "{6,,<2'0'}$1{8}\x2502{,5rn}$2", new object[] { count++, buff.ToString(), buff.Processed ? "5" : "A" } );
				// "{,rn,>*'═'}═{7}Idx{7,8}Age{7,27}Issued Command{7,80rn}Applied Rank{,rn,>*'\x2500'}\x2500"
				Con.Tec( Language.Prompt.Get( 22 ) );
				ShowContent.Dump( buffer, Language.Prompt.Get( 23 ), buffList, ShowContent.DumpOptions.LineNos ); // "CLI Command Buffer"
				Con.Tec( "{,rn,>*'═'}═" );
			}

			return Applets.OperationalStates.Complete;
		}

		/// <summary>Manages the disabled CLI Command List.</summary>
		private Applets.OperationalStates Disable( Applets.OperationalStates runState)
		{
			if ( !ValidateAccess( Ranks.SystemAdmin ) ) return Applets.OperationalStates.IncompleteWithErrors;

			if ( runState == Applets.OperationalStates.Complete )
			{
				string disableList = Command.Switches[ "disable" ].Value;
				if ( disableList.Equals( "show", StringComparison.OrdinalIgnoreCase ) )
					Con.Tec( "{,3}&raquo;{7,5rn}$1", new object[] { Language.Prompt.Get( string.IsNullOrEmpty( Cli.RevokedCmdlets ) ? "2a" : "2b" ), Cli.RevokedCmdlets } );
				else
					if ( !string.IsNullOrWhiteSpace( disableList ) && Regex.IsMatch( disableList, @"^([a-z][\w]{1,2}|([a-z][\w]{1,2}[,; ]+)+([a-z][\w]{1,2}[,; ]*))$", RegexOptions.IgnoreCase ) )
				{
					List<string> cmds = new List<string>();
					if ( Cli.RevokedCmdlets.Length > 0 )
						cmds.AddRange( Cli.RevokedCmdlets.Split( new char[] { ' ', ',', ';', ' ' } ) );

					cmds.AddRange( disableList.Split( new char[] { ' ', ',', ';', ' ' } ) );

					Cli.RevokedCmdlets = string.Join( ",", cmds );
					Con.Tec( "{,3}&raquo;{7,5rn} $1", new object[] { Language.Prompt.Get( "2b" ), Cli.RevokedCmdlets } );
				}
			}

			return runState;
		}

		/// <summary>Searches all attached application nodes and DLL's for Applets, then installs them into the CLI.</summary>
		private Applets.OperationalStates Install( Applets.OperationalStates runState )
		{
			if (!ValidateAccess( Ranks.SystemAdmin )) return Applets.OperationalStates.IncompleteWithErrors;

			bool verbose = Command.Switches[ "INSTALL" ].HasValue( "verbose" );
			if ( !(Cli.Applets is null) && (Cli.Applets.Count > 0) )
			{
				// "{}&raquo;{A,3}Clearing the existing applet library{7}: [{A}\x221a{7}] {9,rn}Done."
				Con.Tec( Language.Prompt.Get( 0 ) );
				Cli.Applets = null;
			}

			// "{}&raquo;{A,3}Searching all loaded modules for viable applets{7,rn}... "
			Con.Tec( Language.Prompt.Get( 1 ) );
			try
			{
				Cli.Applets = new AppletList( false );
				Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

				foreach ( Assembly assembly in assemblies )
				{
					string name = assembly.FullName.Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries )[ 0 ];
					if ( Regex.IsMatch( assembly.ManifestModule.ScopeName, "(^Microsoft.|^System.|^CommonLanguageRuntimeLibrary$|^RefEmit_InMemoryManifestModule$|^Presentation.)" ) )
					{
						if ( verbose )
							Con.Tec( Language.Prompt.Get( 2 ), name );
						// "{z,3}&raquo;{C,5}Skipped  {9}\"{B}$1{9}\"{7}: {E,rn}Not a valid Applet assembly."
					}
					else
					{
						// "{z,3}&raquo;{7,5}Looking for applets in assembly {9}\"{B}$1{9}\"{7,rn}... "
						Con.Tec( Language.Prompt.Get( 3 ), name );
						int count = 0;
						foreach ( TypeInfo ti in assembly.DefinedTypes )
						{
							// "{z,5}&raquo;{7,7}Checking type {}[{B}$1{}]{7} "
							Con.Tec( Language.Prompt.Get( 4 ), ti.Name.Condense( Console.BufferWidth - 25 ) );
							if ( Applets.IsApplet( ti ) && Regex.IsMatch( ti.Name, @"^CMD_" ) )
							{
								// "{z,5}&raquo;{F,7}Found! {6}[{B}$1{6}]{D,54}-► {F}Loading..."
								Con.Tec( Language.Prompt.Get( 5 ), ti.FullName.Replace( name + ".", "" ) );
								count++; Cli.Applets.Add( ti );
								Con.Tec( "{A,57rn}$1!   ", Language.Prompt.Get( 6 ) );
							}
						}

						if ( count == 0 )
						{
							Console.CursorTop -= 1;
							if ( verbose )
								Con.Tec( Language.Prompt.Get( 7 ), name );
							// "{z,3}&raquo; {4,5}Searched {9}\"{B}$1{9}\"{7}: {A,rn}No Applets Found.{z,r} "
						}
					}
				}
				runState = Applets.OperationalStates.Complete;
			}
			catch ( ReflectionTypeLoadException e ) { return Applets.ExceptionDump( e.LoaderExceptions, Cli.HasEnvVar( "DEBUG" ) ); }
			catch ( Exception e ) { return Applets.ExceptionDump( e, Cli.HasEnvVar( "DEBUG" ) ); }

			if ( Cli.Applets.Count == 0 )
			{
				// "{7}[{C}X{7}] {9,rn}Done.{,3}&raquo;{F4,5}Error:{7,rn} No applets were discovered.{,5}Try {F}ATT{,rn}aching an Applet Resource file (DLL)."
				Con.Tec( Language.Prompt.Get( 8 ) );
				runState = Applets.OperationalStates.Complete;
			}
			else
			{
				// Con.Tec( "{7}[{A}\x221a{7}] {9,rn}Done." );
				// "{z,1}&raquo;{A,3rn}Module search has been completed."
				Con.Tec( Language.Prompt.Get( 9 ) );
				if ( Command.Switches.HasSwitch( "list" ) && !Command.Switches[ "list" ].HasValue( "cmds" ) )
					Command.Switches.Add( "/list:cmds" );
			}

			return runState;
		}

		private Applets.OperationalStates Uninstall( Applets.OperationalStates runState )
		{
			if ( !ValidateAccess( Ranks.SystemAdmin ) ) return Applets.OperationalStates.IncompleteWithErrors;

			Cli.Applets = new AppletList( false );
			Con.Tec( Language.Prompt.Get( 10 ) ); // "{,3}&raquo;{7,5rn}All loaded applets have been uninstalled."
			return Applets.OperationalStates.Complete;
		}
		#endregion

		protected override Applets.OperationalStates Main()
		{
			Con.Tec( Language.Prompt.Get( 24 ) ); // {,3}» {F,rn}Nothing to do!
			return Applets.OperationalStates.Complete;
		}
	}

	public class CmdletCwd : CmdletFoundation
	{
		public CmdletCwd() : base( "CD" ) { }

		protected override string MySyntax() =>
			"[/set:folder] [/show]";

		protected override string MyPurpose() => Language.Prompt.Get( "MyPurpose" );
		// "Permits viewing, changing and resetting the current system working directory.";

		protected override string MyHelp() =>
			Language.Prompt.Get( "MyHelp",
				new object[] {
					Path.GetPathRoot( Environment.CurrentDirectory ),
					AppName, MiscellaneousExtensions.LocalExecutablePath
					}
				);
		//"{f,5}&bull;{,7}The {9}\"{B}/show{9}\" {,rn}switch will display the current working directory. It will also be assumed if" +
		//"{,7rn}this command is issued without any switches or other parameters.{,rn} " +
		//"{F,5}&bull;{,7}You can set a specific folder by specifying it, long-form, with the {B}/SET{7}:{E}folder {,rn}switch." +
		//"{F,5}NOTE: {}If the {9}\"{B}/SET{9}\" {,rn}switch is used with an invalid path, or an empty value, no changes will be applied." +
		//"{,rn} {,5}Additionally, {F}..{}, {F}~{}, and {F}\\ {}can be used directly (without the switch) to navigate to{7,rn}:" +
		//"{9,7}\"{F}..{9}\" {,rn}Up one folder." +
		//"{9,7}\"{F}\\{9}\"  {}To the root of the current path. {9}({6}$1{9,rn})".Replace( "$1", Path.GetPathRoot( Environment.CurrentDirectory ) ) +
		//"{9,7}\"{F}~{9}\"  {}To the {B}$1 {,rn}home folder.{9,12}({6}$2{9,rn})".Replace( new object[] { AppName, NetXpertExtensions.LocalExecutablePath } ) +
		//"{,rn} {F,5}NOTE: {,rn}These symbols are individually iconic for their particular purpose!" +
		//"{,11rn}They are not parsed and cannot be combined to aggregate effects.";

		public override void Installer() { }

		protected override Applets.OperationalStates Main()
		{
			string path = Environment.CurrentDirectory;

			if ( Command.Payload.Trim().Equals( ".." ) || Command.Payload.Trim().Equals( "~" ) || Command.Payload.Trim().Equals( "\\" ) )
			{
				switch ( Command.Payload.Trim() )
				{
					case "..":
						var result = Directory.GetParent( path );
						path = (result is null) ? path : result.FullName;
						break;
					case "~":
						path = MiscellaneousExtensions.LocalExecutablePath;
						break;
					case "\\":
						path = (Regex.IsMatch( path, @"^[A-Z]:[\\]?", RegexOptions.IgnoreCase ) ? path[ 0 ] : Environment.CurrentDirectory[ 0 ]) + ":\\";
						break;
				}

				path = Regex.Replace( path, @"^([a-z])([:].*)", $"{char.ToUpper( path[ 0 ] )}$2" );
				Environment.CurrentDirectory = path;
				Con.Tec( Language.Prompt.Get( 0 ), Environment.CurrentDirectory );
				// "{,3}»{A,5}The current working directory has been changed to{7,rn}:{9,5}\"{6}$1{9,rn}\""
				return Applets.OperationalStates.Complete;
			}

			if ( string.IsNullOrWhiteSpace( Command.Payload ) || Command.Switches.HasSwitch( "show" ) )
			{
				Con.Tec( Language.Prompt.Get( 1 ), Environment.CurrentDirectory );
				// "{,3}&raquo;{7,5}The current working directory is{7,rn}:{9,5}\"{6}$1{9,rn}\""
				return Applets.OperationalStates.Complete;
			}

			if ( !Command.Switches.HasSwitch( "set" ) && FolderCollection.ValidatePath( Command.Payload ) )
				Command.Switches.Add( new CommandLineSwitch( "set", Command.Payload, "/", ":" ) );


			if ( Command.Switches.HasSwitch( "set" ) )
			{
				string dest = Command.Switches[ "set" ].Value;
				if ( Regex.IsMatch( dest, @"^[\x22'][^'\x22]+[\x22']$" ) )
					dest = dest.Unwrap( StripOuterOptions.DoubleQuotes | StripOuterOptions.SingleQuotes );

				if ( Regex.IsMatch( dest, @"^([.]\\|[\w][^:]).+" ) )
					dest = Environment.CurrentDirectory + (Environment.CurrentDirectory.EndsWith( "\\" ) ? "" : "\\") + Regex.Replace( dest, @"^[.]\\", "" );

				if ( FolderCollection.ValidatePath( dest ) )
				{
					if ( Directory.Exists( dest ) )
					{
						dest = Regex.Replace( dest, @"^([a-z])([:].*)", $"{char.ToUpper( dest[ 0 ] )}$2" );
						Environment.CurrentDirectory = dest;
						Con.Tec( Language.Prompt.Get( 0 ), Environment.CurrentDirectory );
						// "{,3}&raquo;{A,5}The current working directory has been changed to{7,rn}:{9,5}\"{6}$1{9,rn}\""
						return Applets.OperationalStates.Complete;
					}
					else
						Con.Tec( Language.Prompt.Get( 2 ), dest );
					// "{,3}&raquo;{C,5rn}The supplied path doesn't seem to exist.{9,5}\"{e}$1{9,rn}\""
				}
				else
					Con.Tec( Language.Prompt.Get( 3 ), Command.Switches[ "set" ].Value );
				// "{,3}&raquo;{C,5}The supplied path isn't in a recognized form{7,rn}:{9,5}\"{e}$1{9,rn}\""

				return Applets.OperationalStates.Complete;
			}

			Con.Tec( Language.Prompt.Get( 4 ) ); // "{,3}&raquo;{E,5rn}Nothing to do!"
			return Applets.OperationalStates.Complete;
		}
	}

	public class CmdletDir : CmdletFoundation
	{
		public CmdletDir () : base( "DIR" ) { }

		protected override string MyPurpose() => Language.Prompt.Get( "MyPurpose" );
		// "Displays the contents of the current working directory.";

		public override void Installer() { }

		protected override Applets.OperationalStates Main()
		{
			string path = Cli[ "CWD" ];
			string pattern = Command.CmdLine.ArgCount > 0 ? (string)Command.Args[0] : "*.*";

			Con.Tec( Language.Prompt.Get( 0 ), path.Condense(Console.BufferWidth-20) );
			// "{}Directory of {9}\"{6}$1{9}\"{7,rn}:{F,3}Name{F,62,|20}Size{F,80,|19}Last Accessed{F,104rn}Attr{,rn,>*'─'}─"
			List<string> folders = new List<string>(Directory.GetDirectories( path ));
			ulong count = (ulong)folders.Count, size = 0, lines = 5;
			folders.Sort( ( x, y ) => x.CompareTo( y ) );
			foreach ( string s in folders )
			{
				string folder = Regex.Replace( s, @"^((?:(?:[a-z][:]\\?)?(?:[^\\]+\\))*)(?=[^\\]+$)", "", RegexOptions.IgnoreCase ),
					Rx = FileData.CompileFilePatternToRegex( pattern );
				
				if ( Regex.IsMatch( folder + ".", Rx, RegexOptions.IgnoreCase ) )
					Con.Tec( Language.Prompt.Get( 1 ), folder );
					// "{6,3}$1{F,71}[{D}DIR{F}]{e1,80,|19}Directory{7,103}...{9}D{7,rn}..."

				if ( (++lines % (ulong)Console.WindowHeight == 0) && Command.Switches.HasSwitch( "p" ) )
					Con.WaitPrompt();
			}

			FileAttributes filter = (FileAttributes)( Command.Switches.HasSwitch( "A" ) ? 0 : 0xfff );
			if (Command.Switches.HasSwitch("A"))
			{
				if (Regex.IsMatch( Command.Switches[ "A" ].Value, @"A", RegexOptions.IgnoreCase )) filter |= FileAttributes.Archive;
				if (Regex.IsMatch( Command.Switches[ "A" ].Value, @"R", RegexOptions.IgnoreCase )) filter |= FileAttributes.ReadOnly;
				if (Regex.IsMatch( Command.Switches[ "A" ].Value, @"H", RegexOptions.IgnoreCase )) filter |= FileAttributes.Hidden;
				if (Regex.IsMatch( Command.Switches[ "A" ].Value, @"D", RegexOptions.IgnoreCase )) filter |= FileAttributes.Directory;
				if (Regex.IsMatch( Command.Switches[ "A" ].Value, @"S", RegexOptions.IgnoreCase )) filter |= FileAttributes.System;
				if (Regex.IsMatch( Command.Switches[ "A" ].Value, @"C", RegexOptions.IgnoreCase )) filter |= FileAttributes.Compressed;
				if (Regex.IsMatch( Command.Switches[ "A" ].Value, @"E", RegexOptions.IgnoreCase )) filter |= FileAttributes.Encrypted;
			}

			FileDataCollection files = new( path );
			count += (ulong)files.Count;
			size = files.TotalSize;
			foreach ( FileData file in files )
				if ( file.MatchesPattern( pattern ) && (((int)filter & (int)file.Attributes) > 0) )
				{
					string attr = "";
					attr += "{7,103}" + (file.Attributes.HasFlag( FileAttributes.Archive ) ? "A" : ".");
					attr += file.Attributes.HasFlag( FileAttributes.ReadOnly) ? "{B}R" : "{7}.";
					attr += file.Attributes.HasFlag( FileAttributes.Hidden ) ? "{E}H" : "{7}.";
					attr += file.Attributes.HasFlag( FileAttributes.Directory ) ? "{B}D" : "{7}.";
					attr += file.Attributes.HasFlag( FileAttributes.System ) ? "{C}S" : "{7}.";
					attr += file.Attributes.HasFlag( FileAttributes.Compressed ) ? "{D}C" : "{7}.";
					attr += file.Attributes.HasFlag( FileAttributes.Encrypted ) ? "{A}E" : "{7}.";

					Con.Tec( "{3,3}$1{e,60,<16}$2{7,80}$3$4{,rn} ",
						new object[] { file.FileName, file.SizeToString, file.LastAccessed.ToString( "yyyy-MM-dd HH:mm:ss" ), attr }
					);

					if ( (++lines % (ulong)Console.WindowHeight == 0) && Command.Switches.HasSwitch( "p" ) )
						Con.WaitPrompt();
				}

			// "{,rn,>*'─'}─{7,3}$1 {}file(s) {7,53,<24}$2 {,rn}bytes."
			Con.Tec( Language.Prompt.Get( 2 ), new object[] { count, size.ToString("N0") } );

			return Applets.OperationalStates.Complete;
		}
	}

	public class CmdletEcho : CmdletFoundation
	{
		public CmdletEcho() : base( "EKO", true ) { }

		protected override string MySyntax() => Language.Prompt.Get( "MySyntax" );
		//"[ON|OFF] [\"some text to output\"]";

		protected override string MyHelp() => Language.Prompt.Get( "MyHelp", new object[] { Cli[ "ECHO" ] } );
			//"{,5}Environment variables can be dereferenced in the output by enclosing them in '{7}%{,rn}' " +
			//"signs.{,5}For example: {}OUT {7}\"{}Echo is {E}%{3}ECHO{E}%{7}\"{,rn} would output:\n{3}\"{9}Echo is $1{3,rn}\"{,rn}".Replace( "$1", Cli["ECHO"] );

		protected override string MyPurpose() => Language.Prompt.Get( "MyPurpose" );
		// "Echos information out to the console.";

		public override void Installer() { }

		private Applets.OperationalStates Set( Applets.OperationalStates runState )
		{
			if (Regex.IsMatch( Switches[ "set" ].Value.Trim(), @"(ON|OFF)", RegexOptions.IgnoreCase))
			{
				Tec( About() );
				Cli.SetEnvVar( "ECHO", Switches[ "set" ].Value.Trim(), false, "System" );
				Cli.Prompt.Write();
			}
			return runState;
		}

		private Applets.OperationalStates Ver ( Applets.OperationalStates runState )
		{
			Tec( base.About() );
			return runState;
		}

		/// <param name="args">[0] = _localUser [1] = _environment</param>
		protected override Applets.OperationalStates Main()
		{
			string output;
			if ( Args.Count == 0 )
			{
				output = Language.Prompt.Get( 0 );
				output = Cli.ApplyEnvironmentTo( output );
				Tec( output );
			}
			else
				foreach ( string arg in Args )
				{
					output = arg.Replace( "&lbrace;", "{" ).Replace( "&rbrace;", "}" );
					if ( Regex.IsMatch( output, @"^[\s]*([\x22][^\x22]*[\x22]|['][^']*['])[\s]*$" ) )
						output = output.Unwrap( StripOuterOptions.DoubleQuotes | StripOuterOptions.SingleQuotes, true );

					output = Cli.ApplyEnvironmentTo( output );
					Tec( "{,0}\"{3}$1{,rn}\"", output );
				}

			//Regex pattern = new Regex( @"^\x22(?<data>[^\x22]+)\x22$", RegexOptions.IgnoreCase );
			//string output = Command.Payload.Replace( "&lbrace;", "{" ).Replace( "&rbrace;", "}" );
			//if ( pattern.IsMatch( output ) )
			//	Tec( "{,0}\"{3}$1{,rn}\"", Cli.ApplyEnvironmentTo( pattern.Matches( output )[ 0 ].Groups[ "data" ].Value ) );
			//else
			//{
			//	Regex test = new Regex( @"^(?:ON|OFF)$", RegexOptions.Compiled | RegexOptions.IgnoreCase );
			//	if ( output.Length == 0 ) Command.Payload = Language.Prompt.Get( 0 ); // "\"Echo is %ECHO%.\"";
			//	if ( test.IsMatch( output ) )
			//	{
			//		Cli.SetEnvVar( "ECHO", output.ToLower(), false, "System" );
			//		Cli.Prompt.Write();
			//	}
			//	else
			//	{
			//		if ( Command.Switches.HasSwitch( "set" ) && test.IsMatch( Command.Switches[ "set" ].Value ) )
			//			Cli.SetEnvVar( "ECHO", Command.Switches[ "set" ].Value, false, "System" );

			//		output = (Command.CmdLine.ArgCount > 0)
			//			?
			//			((Command.CmdLine.ArgCount > 1)
			//				?
			//				Command.Args.ToString()
			//				:
			//				(string)Command.Args[ 0 ]
			//			).Replace( "&lbrace;", "{" ).Replace( "&rbrace;", "}" ) 
			//			: "";
			//		if ( output.Length > 0 ) // "Echo is {7}\"{B}%ECHO%{7}\""
			//			Tec( "{,0}\"{3}$1{,rn}\"", (output.Length > 0) ? Cli.ApplyEnvironmentTo( output ) : Language.Prompt.Get( 1 ) );
			//	}
			//}
			//return result;
			return Applets.OperationalStates.Running;
		}
	}

	public class CmdletExit : CmdletFoundation
	{
		public CmdletExit() : base( CommandLineInterface.EXIT_COMMAND ) { }

		protected override string MyPurpose() => Language.Prompt.Get( "MyPurpose" );
		//"Closes all open files, connections, forms and operations then ends CLI operation.";

		public override void Installer() { }

		protected override Applets.OperationalStates Main()
		{
			Cli.LogOn(); // <-- forces log off
			Cli.Registry.Save(); // <-- save the Registry!

			if ( Cli.HasInternalScript( "AutoClose" ) )
				Cli.Enqueue( new Command( "CMD /RUN:AutoClose", UserInfo.DefaultUser( Ranks.SuperUser ) ) );
			
			//endState = Cmdlets[ "CMD" ].Execute( new Command( "CMD /RUN:AutoClose", UserInfo.DefaultUser( Ranks.SuperUser ) ) );

			return Applets.OperationalStates.Complete;
		}
	}

	public class CmdletForms : CmdletFoundation
	{
		public CmdletForms() : base( "FRM" ) { }

		protected override string MySyntax() => "[/list] [/close:id] [/start] [/stop]";

		protected override string MyPurpose() => Language.Prompt.Get( "MyPurpose" );
		// "Facilitates oversight and management of dependent forms.";

		public override void Installer() { }

		protected override Applets.OperationalStates Main()
		{
			return Applets.OperationalStates.Complete;
		}
	}

	public class CmdletHelp : CmdletFoundation
	{
		public CmdletHelp() : base( "?" ) { }

		protected override string MySyntax() => "[?]";

		protected override string MyPurpose() => Language.Prompt.Get( "MyPurpose" );
		// "Lists commands and applets that are available and recognized by the shell.";

		public override void Installer() { }

		protected override Applets.OperationalStates Main()
		{
			Applets.OperationalStates result = Applets.OperationalStates.Running;
			int spacing = 12;

			if ( !Command.Switches.HasSwitch( "details" ) && !Command.Switches.HasSwitch( "?" ) && !Command.Payload.Trim().Equals( "?" ) )
				Con.Tec( $"{{,rn}} {Language.Prompt.Get( 8 )}{{F,rn}}.{{,rn}} " );

			Con.Tec( Language.Prompt.Get( 0 ) ); // "{,3}&raquo;{F,5rn}Internal System Commands:"
			if ( Command.Switches.HasSwitch( "details" ) || Command.Switches.HasSwitch( "?" ) || Command.Payload.Trim().Equals( "?" ) )
			{
				Con.Tec( "{E,5}Cmd{E,12rn}$1{,rn,>*'─'}    ", Language.Prompt.Get( 1 ) ); // Description
				foreach ( string s in Cli.Cmdlets.CmdList )
				{
					string p = Cli.Cmdlets.HasCmd( s ) && Cli.Cmdlets[ s ].HasPurpose ? Cli.Cmdlets[ s ].Purpose : Language.Prompt.Get( 2 );
					Con.Tec( "{B,5}$1{F,10}-{9,12}$2{,rn}", new object[] { s.ToUpperInvariant(), p } );
				}
				Con.Tec( "{,rn,>*'─'}    " );
			}
			else
			{
				Con.Tec( "{}    " );
				foreach ( string s in Cli.Cmdlets.CmdList )
				if ( !s.Equals( "?" ) )
					{
						//if ( Console.BufferWidth - Console.CursorLeft < spacing ) Con.Tec( "{,rn}{}    " );
						if ( Console.CursorLeft > 95 ) Con.Tec( "{,rn}{}    " );
						Con.Tec( "{$2,,>$3}$1", new object[] { s.ToUpper(), Cli.Color.Alt( ConsoleColor.Cyan ), spacing } );
					}
				Con.Tec( "{,rn}" );
			}

			Con.Tec( Language.Prompt.Get( 3 ) );
			// "{6,5}For help specific to each command, type the command name and add the switch {9}\"{B}/?{9,rn}\""

			if ( (Cli.Applets is not null) && (Cli.Applets.Count > 0) )
			{
				Console.WriteLine();
				Con.Tec( Language.Prompt.Get( 4 ) );
				// "{,3}&raquo;{F,5rn}Applets that are currently installed and available to you:{,,>4} "
				if ( Command.Switches.HasSwitch( "details" ) || Command.Switches.HasSwitch( "?" ) || Command.Payload.Trim().Equals( "?" ) )
				{
					Con.Tec( Language.Prompt.Get( 5 ) + "{,5rn,>*'─'}─" ); // "{E,5}Command{E,20}Rank Required{E,40rn}Description{,5rn,>*'─'}─"
					for ( int i = 0; i < Cli.Applets.Count; i++ )
						if ( Cli.Applets[ i ].RankReqd <= Cli.LocalUser.Rank )
						{
							string d = Cli.Applets[ i ].Description;
							if ( string.IsNullOrWhiteSpace( d ) ) d = Language.Prompt.Get( 2 ); // "{1}&laquo;{8} No Description Available {1}&raquo;";
							Con.Tec(
								$"{{B,5}}$3 {{9,20}}&lbrace;{{6}}$4{{9}}&rbrace;{{$1,40}}{d}{{,rn}} ",
								new object[] {
										new CliColor( "9" ),
										Process.GetCurrentProcess().ProcessName,
										Cli.Applets[ i ].Command.ToUpper(),
										Cli.Applets[ i ].RankReqd.Name
								}
							);
						}
					Con.Tec( "{,rn,>*'─'}    " );
				}
				else
				{
					spacing = 20; Console.Write( "    " );
					foreach ( string s in Cli.Applets.AppletNames() )
						if ( Cli.Applets[ s ].AccessRank <= Cli.LocalUser.Rank )
						{
							if ( Console.BufferWidth - Console.CursorLeft < spacing ) Con.Tec( "{rn,,>4} " );
							//if ( Console.CursorLeft > 95 ) Con.Tec( "{rn,,>4} " );
							Con.Tec( "{$2,,>$3}$1", new object[] { s.ToUpper(), Cli.Color.Alt( ConsoleColor.Cyan ), spacing } );
						}

					if ( Console.CursorLeft > 4 ) Console.WriteLine();
				}
				Con.Tec( Language.Prompt.Get( 6 ) );
				// "{6,5}For help specific to each applet, type the applet name and add the switch {9}\"{B}/?{9,rn}\""
			}

			if ( (Cli.Aliases is not null) && (Cli.Aliases.Count > 0) )
			{
				Cli.Aliases.Sort();
				// "{,rn}{,3}&raquo;{F,5}Your Defined Aliases ({9}$1 {7}of {9}$2{F,rn}):{E,5,>15}Alias{E,rn}Translation"
				Con.Tec( Language.Prompt.Get( 7, new object[] { Cli.Aliases.Count, Cli.Aliases.Limit } ) + "{,rn,>*'─'}    " );
				foreach ( AliasDefinition ad in Cli.Aliases )
					Con.Tec( "{6,5,>15}$1{F}\"{9}$2{F,rn}\"", new object[] { ad.Alias, ad.Value } );
				Con.Tec( "{,rn,>*'─'}    " );
			}

			return result;
		}
	}

	public class CmdletIf : CmdletFoundation
	{
		public CmdletIf() : base( "IF", true ) { }

		protected override string MyPurpose() => Language.Prompt.Get( "MyPurpose" );
		//"Performs evaluations simple expressions and executes specified instructions accordingly.";

		protected override string MySyntax() => Language.Prompt.Get( "MySyntax" );

		//"{9}({F}${e}&lbrace;{B}EnvVarName{e}&rbrace;{9}|{6}\"{e}&lbrace;{b}value{e}&rbrace;{9}{6}\"{9})" +
		//"{9}({F}=={9}|{F}!={9}|{F}<={9}|{F}<{9}|{F}>{9}|{F}>={9})" +
		//"{9}({F}${e}&lbrace;{B}EnvVarName{e}&rbrace;{9}|{6}\"{e}&lbrace;{b}value{e}&rbrace;{9}{6}\"{9})" +
		//"{7} THEN {9}({e}&lbrace;{b}Command{e}&rbrace;{9}|{7}SET {e}&lbrace;{b}variable{9}={6}\"{e}&lbrace;{b}value{e}&rbrace;{6}\"{9})";

		protected override string MyHelp() => Language.Prompt.Get( "MyHelp" );
		//"{7,5}&bull;{,7}This command does {e}NOT{,rn} use switches!" +
		//"{7,5}&bull; {f}${e}&lbrace;{b}EnvVarName{e}&rbrace; {}is the name of an environment variable whose {e}value{} will be compared (typically {7}ERRORLEVEL{,rn})." +
		//"{7,7}&raquo; {}A {B}${}, as the first character, marks it as an environment variable reference and {e}not{,rn} a value." +
		//"{7,7}&raquo; {}If the referenced environment variable doesn't exist, a {e}&lbrace;{b}null{e}&rbrace;{} value {9}({6}\"\"{9}){,rn} will be substituted." +
		//"{7,5}&bull; {6}\"{e}&lbrace;{b}value{e}&rbrace;{6}\" {,rn}represents a literal value to compare;" +
		//"{7,7}&raquo; {}As shown, quotes {9}({6}&quot;{9}){,rn} are usually used to contain/identify literal values, but they aren't strictly" +
		//"{,9}required {e}unless {}the literal contains non-alphanumeric-characters, spaces or the word {9}\"{7}THEN{9}\"{,rn}." +
		//"{7,7}&raquo; {}In any event, literals {e}can not contain {}the quote character {9}({6}&quot;{9}){,rn}." +
		//"{7,7}&raquo; {}Any {b}$ {}appearing in a value that's enclosed by quotes will be treated {e}literally{,rn}!" +
		//"{7,7}&raquo;{} Brackets aren't allowed unless they're a part of the value itself." +
		//"{7,5}&bull;{,rn} Numeric values will be compared numerically, everything else will be compared as strings." +
		//"{7,7}&raquo;{} If a numeric value and a non-numeric value are compared, {e}both {}will be treated as {e}strings{,rn}." +
		//"{7,7}&raquo;{} The cmdlet determines numeracy {e}on it's own{,rn}. There's no mechanism to force one comparison type or the other." +
		//"{7,5}&bull; {}The shown operators are the {e}only {}ones supported. {F,rn}DON'T USE BRACKETS!" +
		//"{7,5}&bull; {e}&lbrace;{b}Command{e}&rbrace; {,rn}represents the full text instruction that will be sent to the CLI if the comparison passes." +
		//"{7,7}&raquo;{} Literally {e}everything{} after the {7}THEN{} directive is passed to the CLI {e}verbatim{,rn}. As such,{,9rn}you only " +
		//"have to use quotes or other markers when they're part of the instruction itself.{,rn}" +

		//"{7,5}&bull; {F}Be aware {}that a {e}&lbrace;{b}Command{e}&rbrace;{} generated by {7}IF {}is {b}queued{}, and will {e}NOT {,rn}be executed immediately!" +
		//"{,7}This means that, if the {7}IF {}command is contained in a script, any command it issues will {e,rn}not" +
		//"{,7rn}be run until after the entire script has finished running." +
		//"{7,7}&raquo; {,rn}Queued commands will then be executed in the order that they were sent." +
		//"{7,7}&raquo; SET{} instructions, on the other hand, {e}are{} processed immediately by {7}IF {,rn}itself.";

		public override void Installer() { }

		protected override Applets.OperationalStates Main()
		{   // "\x22" is the hex code for double-quotes (") --  using literal quotes appears to break VisualStudio Regex formatting...
			string vP = /* language=regex */ @"(?<$1>[\x22]([^\x22]*)[\x22]|[$][a-z][\w]*[a-z0-9]|[\d]+|[a-z0-9]?[a-z0-9]*)",
				pattern = /* language=regex */ @"^" +
					vP.Replace( new object[] { "leftSide" } ) + /* language=regex */ @"[\s]*(?<operation>==|>=|<=|~|!=|=|≠|<|>|≤|≥)[\s]*" +
					vP.Replace( new object[] { "rightSide" } ) + /* language=regex */ @"[\s]+THEN[\s]+(?<cmd>[\S][\s\S]*)$";

			if ( Command.Payload.Length == 0 )
			{
				//Con.Tec( "{,3}&raquo; {7,rn}You must specify a condition this command." );
				Con.Tec( Language.Prompt.Get( 0 ) );
				Cli.Enqueue( "IF /?", Cli.LocalUser, false );
				return Applets.OperationalStates.Complete;
			}

			string work = Command.Payload.Trim();
			if ( Regex.IsMatch( work, pattern, RegexOptions.IgnoreCase ) )
			{
				Match m = Regex.Match( work, pattern, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture );
				string varName = "", op = "", value = "", then = "", antonym = "";
				if ( m.Groups[ "leftSide" ].Success ) varName = m.Groups[ "leftSide" ].Value.Trim( new char[] { ' ', '"' } );
				if ( m.Groups[ "rightSide" ].Success ) value = m.Groups[ "rightSide" ].Value.Trim( new char[] { ' ', '"' } );
				if ( m.Groups[ "operation" ].Success ) op = m.Groups[ "operation" ].Value.Trim( new char[] { ' ', '"' } );
				if ( m.Groups[ "cmd" ].Success ) then = m.Groups[ "cmd" ].Value.Trim();

				if ( (varName.Length > 0) && (op.Length > 0) && (then.Length > 0) )
				{
					if ( (varName.Length > 1) && (varName[ 0 ] == '$') )
						varName = Cli.HasEnvVar( varName.Substring( 1 ) ) ? Cli[ varName.Substring( 1 ) ] : "";

					if ( (value.Length > 1) && (value[ 0 ] == '$') )
						value = Cli.HasEnvVar( value.Substring( 1 ) ) ? Cli[ value.Substring( 1 ) ] : "";

					dynamic left, right;

					if ( Regex.IsMatch( varName, @"^([+-]?[\d]+(.[\d]+)?|)$" ) && Regex.IsMatch( value, @"^([+-]?[\d]+(.[\d]+)?|)$" ) )
					{
						if ( (varName.IndexOf( '.' ) > 0) || (value.IndexOf( '.' ) > 0) )
						{
							left = decimal.Parse( varName );
							right = decimal.Parse( value );
						}
						else
						{
							left = string.IsNullOrEmpty( varName ) ? 0 : int.Parse( varName );
							right = string.IsNullOrEmpty( value ) ? 0 : int.Parse( value );
						}
					}
					else
					{
						left = varName.ToUpperInvariant();
						right = value.ToUpperInvariant();
					}

					bool result = false;
					switch ( op )
					{
						case "==":
						case "=":
							result = left.CompareTo( right ) == 0;
							antonym = "≠";
							break;
						case "<":
							result = left.CompareTo( right ) < 0;
							antonym = "≥";
							break;
						case "≤":
						case "<=":
							result = left.CompareTo( right ) < 1;
							antonym = ">";
							break;
						case ">":
							result = left.CompareTo( right ) > 0;
							antonym = "≤";
							break;
						case "≥":
						case ">=":
							result = left.CompareTo( right ) > -1;
							antonym = "<";
							break;
						case "!=":
						case "~":
							result = left != right;
							antonym = "=";
							break;
					}

					if ( result )
					{
						if ( Regex.IsMatch( then, @"^SET ([a-z][a-z0-9]*)=([\x22][^\x22]*[\x22]|[\w\s]+)$", RegexOptions.IgnoreCase ) )
						{
							Con.Tec( "{,3}&raquo; {7}SET " );
							Match mSet = Regex.Match( then, @"^SET (?<varName>[a-z][a-z0-9]*)=(?<varValue>[\x22][^\x22]*[\x22]|[\w\s]+)$", RegexOptions.IgnoreCase );
							if ( mSet.Groups[ "varName" ].Success && mSet.Groups[ "varValue" ].Success )
							{
								string envValue = mSet.Groups[ "varValue" ].Value.Trim( new char[] { '"' } );
								Cli.SetEnvVar( mSet.Groups[ "varName" ].Value, envValue, false, Cli.LocalUser.SystemName );
								Con.Tec( $"{{B,9}}$1 {{8}}─► {{6}}\"{{e}}$2{{6}}\" {{9}}&lbrace;{{b}}$3{{9}}&rbrace; {{a,rn}}" + Language.Prompt.Get( 1 ), // Done!
									new object[] { mSet.Groups[ "varName" ].Value, envValue, Cli.LocalUser.SystemName }
								);
							}
							else
								Con.Tec( Language.Prompt.Get( 2 ) ); // "{c,9}Error: {7,rn}Invalid Syntax, nothing was done!"
						}
						else
						{
							if ( Cli.HasEnvVar( "debug" ) && Cli[ "debug" ].Equals( "on", StringComparison.OrdinalIgnoreCase ) )
								Con.Tec( $"{{,3}}&raquo; {{7}}{Language.Prompt.Get( 3 )}{{8}} ─► {{9}}\"{{B}}$1{{9}}\"{{,rn}}...", then ); // Tested condition is {A}TRUE{7}; Queueing
							Cli.Enqueue( then, UserInfo.DefaultUser(Cli.EffectiveRank), false );
						}
					}
					else
					{
						if ( Cli.HasEnvVar( "debug" ) && Cli[ "debug" ].Equals( "on", StringComparison.OrdinalIgnoreCase ) )
							Con.Tec( $"{{,3}}&raquo; {{7}}{Language.Prompt.Get( 4 )}{{7}}; $4{{e}}$1$4{{F}} $2 $4{{e}}$3$4{{,rn}}", // Tested condition is {4}FALSE
								new object[] { left, antonym, right, left.GetType() == typeof( string ) ? "{9}\"" : "" } );
					}
				}
			}
			else
			{
				//Con.Tec( "{,3}&raquo; {7,rn}You must provide a validly formatted payload for this command to function." );
				Con.Tec( Language.Prompt.Get( 5 ) );
				if ( Cli.HasEnvVar( "debug" ) && Cli[ "debug" ].Equals( "on", StringComparison.OrdinalIgnoreCase ) )
				{
					//Con.Tec( "{,5}&raquo;{7,5}Left: {9}\"{e}$1{9,rn}\"{,5}&raquo;{7,5}Right: {9}\"{e}$2{9,rn}\"" )
					//cli.Enqueue( "IF /?", cli.LocalUser.Rank );
				}
				return Applets.OperationalStates.Complete;
			}
			return Applets.OperationalStates.Complete;
		}
	}

	public class CmdletLanguage : CmdletFoundation
	{
		public CmdletLanguage() : base( "LNG" ) { }

		protected override string MyPurpose() => Language.Prompt.Get( "MyPurpose" );
		// Manages the CLI's culture and language settings.

		protected override string MySyntax() =>
			"[/show] [/set:{cultureSpec}] [/list] [/load:{fileSpec}] [/save:{fileSpec}]"; // undocumented: /compile:filename

		protected override string MyHelp() => "";

		public override void Installer() { }

		private Applets.OperationalStates Show( Applets.OperationalStates runState )
		{
			Con.Tec( Language.Prompt.Get( 0 ) + ": {e}\"{B}$1{e,rn}\"",  // Current Culture
				Language.Prompt.DefaultCulture.DisplayName );

			Con.Tec( Language.Prompt.Get( 8 ) + "{7,rn}:" );
			foreach ( System.Globalization.CultureInfo culture in Language.Prompt.Languages )
				Con.Tec( "{,5}&bull;{9,7}[{f}$2{9}]{}: \"{e}$1{,rn}\"", new object[] { culture.DisplayName, culture.Name } );

			Con.Tec( Language.Prompt.Get( 9, new object[] { Language.Prompt.Languages.Length, Language.Prompt.Languages.Length == 1 ? "" : "s" } ) + "{7,rn} " );

			return runState;
		}

		private Applets.OperationalStates List( Applets.OperationalStates runState )
		{
			Con.Tec( "{,3}&raquo;{7,5rn}$1:", Language.Prompt.Get( 1 ) );
			foreach ( System.Globalization.CultureInfo lang in Language.Prompt.Languages )
				if ( !lang.Equals( System.Globalization.CultureInfo.InvariantCulture ) )
					Con.Tec( "{7,5}&bull;{9,7}[{F}$2{9}] \"{e}$1{9,rn}\"", new object[] { lang.DisplayName, lang } );

			return Applets.OperationalStates.Complete;
		}

		private Applets.OperationalStates Load( Applets.OperationalStates runState )
		{
			string dest = Command.Switches[ "load" ].Value.Length > 0 ? Command.Switches[ "load" ].Value : MiscellaneousExtensions.LocalExecutablePath + MiscellaneousExtensions.ExecutableName,
				path = Path.GetDirectoryName( dest );
			if ( string.IsNullOrWhiteSpace( path ) || Directory.Exists( path ) )
			{
				//if ( !Regex.IsMatch( dest, @"\.res.xml$", RegexOptions.IgnoreCase ) )
				//	dest = dest.TrimEnd( new char[] { '.', '\\' } ) + ".res.xml";

				System.Globalization.CultureInfo currentCulture = Language.Prompt.DefaultCulture;
				Language.Prompt = LanguageManager.LoadFile( dest );
				Language.Prompt.DefaultCulture = currentCulture;
				Con.Tec( Language.Prompt.Get( 3 ), Path.GetFileName( dest ) );
				// "{,3}&raquo;{7,5}The requested XML resources file, {9}\"{6}$1{9}\"{7,rn}, has been loaded."
			}
			else
			{
				Con.Tec( Language.Prompt.Get( 2 ), dest );
				// "{7,3}&raquo;{C,5}Error{7}: {e}The specified file {9}\"{0e}$1{9}\" {e,rn}could not be found."
				return Applets.OperationalStates.CompleteWithErrors;
			}

			return runState;
		}

		private Applets.OperationalStates Set( Applets.OperationalStates runState )
		{
			string value = Command.Switches[ "set" ].Value.Unwrap( StripOuterOptions.DoubleQuotes | StripOuterOptions.SingleQuotes ); ;
			if ( Regex.IsMatch( value, @"^[a-z]{2}-[a-z]{2}?$", RegexOptions.IgnoreCase ) )
			{
				if ( Language.Prompt.HasLanguage( value ) )
				{
					// The system language has been changed to...
					Language.Prompt.DefaultCulture = new System.Globalization.CultureInfo( value );
					Con.Tec( Language.Prompt.Get( 4 ) + " {9}[{f}$1{9}] \"{e}$2{9,rn}\"",
						new object[] { value, Language.Prompt.DefaultCulture.DisplayName } );
					return Applets.OperationalStates.Complete;
				}
				else
					Con.Tec( Language.Prompt.Get( 5 ), value );
				// {f,3}&raquo;{C,5}Error{7}: {e}The requested language ($1) is not supported!{,rn}
			}
			else
				Con.Tec( Language.Prompt.Get( 6 ), value );
			// {f,3}&raquo;{C,5}Error{7}: {e}The language specification provided is unrecognized ($1)!{,rn}
			return Applets.OperationalStates.CompleteWithErrors;
		}

		private Applets.OperationalStates Compile( Applets.OperationalStates runState )
		{
			string dest = Command.Switches[ "compile" ].Value.Length > 0 ? Command.Switches[ "compile" ].Value : MiscellaneousExtensions.LocalExecutablePath + MiscellaneousExtensions.ExecutableName,
				path = Path.GetDirectoryName( dest );
			if ( string.IsNullOrWhiteSpace( path ) || Directory.Exists( path ) )
			{
				if ( !Regex.IsMatch( dest, @"\.resx$", RegexOptions.IgnoreCase ) )
					dest = dest.TrimEnd( new char[] { '.', '\\' } ) + ".resx";

				if ( File.Exists( dest ) ) File.Delete( dest );
				Language.Prompt.ExportResourceFile( dest );
				Con.Tec( Language.Prompt.Get( 7 ), Path.GetFileName( dest ) );
				// {,3}&raquo;{7,5}The compiled resource file, {9}\"{B}$1{9}\"{7,rn} has been created.
			}

			return runState;
		}

		protected override Applets.OperationalStates Main() =>
			!Command.CmdLine.HasArguments ? Show( Applets.OperationalStates.Running ) : Applets.OperationalStates.Complete;
	}

	public class CmdletPrompt : CmdletFoundation
	{
		public CmdletPrompt() : base( "PMT" ) { }

		protected override string MySyntax() => Language.Prompt.Get( "MySyntax" ); // [prompt schema]";

		protected override string MyHelp() => Language.Prompt.Get( "MyHelp", new object[] { Cli.Prompt.Help(), Cli.Prompt.RawPrompt } );
		//("{f,5}PMT {,rn}supports using several dynamic entities to customize it's output." +
		//"{,5rn}The Currently Recognized/Supported Entities are:$1\n{B,5}Be Aware: {}DECL patterning and HTML entities are {F}NOT{,rn} " +
		//"supported!{,rn}{}The current Prompt is: {9}\"$2{9,rn}\"").Replace( new object[] { Cli.Prompt.Help(), Cli.Prompt.RawPrompt } );

		protected override string MyPurpose() => Language.Prompt.Get( "MyPurpose" ); // "Manages the CLI prompt.";

		public override void Installer() 
		{
			if (AppRegObj is null)
			{
				Cli.Registry.Copy( CliRegistry.Hive.App, $"{this.GetType().Name},UserDefaults", $"Applications.{this.GetType().Name}", CliRegistry.Hive.User );
				//CliGroup myGroup = new CliGroup( this.GetType().Name );
				//myGroup.Add( new IniLineItem( "Prompt", "[$disk[drive]] $data[HH:mm] CS> ", false, "\t## Default User-defined Prompt" ) );

				//Cli.Registry[ CliRegistry.Hive.User ].Add( myGroup );
			}
		}

		new public string Details() =>
			base.Details().Replace( "$1", Cli.Prompt.Help() );

		protected override Applets.OperationalStates Main()
		{
			if ( Command.Payload.Length > 0 )
			{
				string newPrompt = Regex.Replace( Command.Payload.TrimStart(), @"[%]prompt[%]", Cli.Prompt.RawPrompt, RegexOptions.IgnoreCase );
				if ( Regex.IsMatch( newPrompt, @"^(([""][\S ]+[""])|(['][\S ]+[']))$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture ) )
					newPrompt = newPrompt.Substring( 1, newPrompt.Length - 2 );

				Cli.Prompt.RawPrompt = newPrompt; // "{,0}The Prompt has been changed to: {9}\"$1{9,rn}\""
				Con.Tec( Language.Prompt.Get( 0 ), Cli.Prompt.FormatRawPrompt( new CliColor( "F" ) ) );
				Cli.SetEnvVar( "PROMPT", newPrompt, false, "System" );
				Cli.Registry[ CliRegistry.Hive.User ][ this.GetType().Name ][ "Prompt" ].Data = newPrompt;
			}
			else
				Con.Tec( Language.Prompt.Get( 1 ), Cli.Prompt.FormatRawPrompt( new CliColor( "F" ) ) );
				// "{,0}The Current Prompt is: {9}\"$1{9,rn}\""

			return Applets.OperationalStates.Complete;
		}
	}

	public class CmdletRunAs : CmdletFoundation
	{
		public CmdletRunAs() : base( "SU" ) { }

		protected override string MyPurpose() => Language.Prompt.Get( "MyPurpose" );
		// "Manages viewing, setting and unsetting environment variables."

		protected override string MySyntax() => Language.Prompt.Get( "MySyntax" );

		protected override string MyHelp() => Language.Prompt.Get( "MyHelp" );
		//"{7,5}&bull;{} The {b}/IMPORT {}switch imports the {6}System {,rn}environment variable settings.";

		public override void Installer() { }

		protected override Applets.OperationalStates Main()
		{
			if ( (Args.Count > 0) && Switches.HasSwitch( "user" ) && !Switches[ "user" ].IsEmpty && Cli.Registry.HasUser( Switches[ "user" ].Value ) )
			{
				string userName = Switches[ "edit" ].Value, password;
				// {}Password for
				Tec( Language.Prompt.Get( 0 ) + " {9}\x22{6}$1{9}\x22{7}: ", userName ); ;
				try
				{
					do
						password = Con.InputTemplate( new CliColor( "9" ), Cli.Color, "", 10, 64, 16, '.', '*', true );
					while ( password.Length < 3 );
					if ( Cli.Registry.ValidateUser( userName, password ) )
						Cli.Enqueue( Args[ 0 ], Cli.Registry.GetUser( userName ), false );
					else
						// {,3}»{F,5rn}The supplied credentials could not be authenticated.
						Tec( Language.Prompt.Get( 1 ) );
				}
				catch (OperationCanceledException oce)
				{
					// {,3}»{C,5rn}Cancelled!
					Tec( Language.Prompt.Get( 2 ) );
				}
				return Applets.OperationalStates.Complete;
			}

			return Applets.OperationalStates.IncompleteWithErrors;
		}
	}

	public class CmdletSet : CmdletFoundation
	{
		public CmdletSet() : base( "SET" ) { }

		protected override string MyPurpose() => Language.Prompt.Get( "MyPurpose" );
		// "Manages viewing, setting and unsetting environment variables."

		protected override string MySyntax() => "[varName=\"value\"] [/import]";

		protected override string MyHelp() => Language.Prompt.Get( "MyHelp" );
		//"{7,5}&bull;{} The {b}/IMPORT {}switch imports the {6}System {,rn}environment variable settings.";

		public override void Installer() { }

		private Applets.OperationalStates Import( Applets.OperationalStates runState )
		{
			IDictionary envVars = Environment.GetEnvironmentVariables( EnvironmentVariableTarget.Machine | EnvironmentVariableTarget.Machine );
			Con.Tec( Language.Prompt.Get( 0 ), envVars.Count );
			if ( envVars.Count > 0 )
				foreach ( DictionaryEntry value in envVars )
				{
					string name = value.Key.ToString();
					if ( !string.IsNullOrWhiteSpace( name ) && !Cli.HasEnvVar( name ) )
					{
						Con.Tec( "{7,5}&bull;{,5}$2{7}: {B}$1", new object[] { value.Key, Language.Prompt.Get( 1 ) } );
						EnvironmentVar ev = new EnvironmentVar( name, value.Value.ToString(), true, "System" );
						Cli.SetEnvVar( ev );
						Con.Tec( ev.Value.Length > Console.BufferWidth - Console.CursorLeft - 10 ? "{7,rn}:{9,6}└─► \"" : "{9} ─► \"" );
						Con.Tec( "\"{e}$1{9,rn}\"", ev.Value.Condense( Console.BufferWidth - 15 ) );
					}
					else
						Con.Tec( Language.Prompt.Get( 2 ), value.Key );
					// "{7,5}&bull;{6,5}Skipping {9}\"{B}$1{9}\"{7}: {D,rn}Variable already exists!"
				}

			return runState;
		}

		protected override Applets.OperationalStates Main()
		{
			Applets.OperationalStates result = Applets.OperationalStates.Running;
			Console.Write( "\r" ); // Return cursor to BOL
			if ( Command.Payload.Length == 0 )
			{
				if ( Cli.Env.Count > 0 )
				{
					Con.Tec( Language.Prompt.Get( 3 ) ); // "{,rn}Defined Environment Variables:"
					foreach ( EnvironmentVar var in Cli.Env.ToArray() )
					{
						string value = var.Value.Replace( "{", "&lbrace;" ).Replace( "}", "&rbrace;" );
						Con.Tec( "{F,5}$1{8} = {E}\"$2{E}\"", new object[] { var.Name, value } );
						if ( var.Owner.Length > 0 )
							Con.Tec( "{9} &lbrace;{B}$1{9}&rbrace;", var.Owner );
						Console.WriteLine();
					}
				}
				else
					Con.Tec( Language.Prompt.Get( 4 ) );
				// "{,rn}There are no defined environment variables."
				result = Applets.OperationalStates.Complete;
			}
			else
				foreach ( string sw in Command.Args )
				{
					EnvironmentVar newVar = new EnvironmentVar( sw, Command.Switches.HasSwitch( "readonly" ), Cli.LocalUser.SystemName );
					if ( Cli.SetEnvVar( newVar ) )
					{
						if ( Cli.HasEnvVar( newVar.Name ) )
						{
							Con.Tec( "{,rn}$3:{F,5}$1{8} = {7}\"{E}$2{7}\"",
								new object[] { newVar.Name, newVar.Value, Language.Prompt.Get( 5 ) }
								);
							if ( newVar.Owner.Length > 0 )
								Con.Tec( "{9} &lbrace;{B}$1{9}&rbrace;", newVar.Owner );
							Console.WriteLine();
						}
						else
							Con.Tec( Language.Prompt.Get( 6 ), newVar.Name );
						// "{}The variable {7}\"{E}$1{7}\" {,rn}has been unset."

						result = Applets.OperationalStates.Complete;
					}
					else
					{
						// "{}The attempt to create the environment variable {7}\"{E}$1{7}\"{,rn} failed."
						Con.Tec( Language.Prompt.Get( 7 ), newVar.Name );
						result = Applets.OperationalStates.CompleteWithErrors;
					}
				}
			return result;
		}
	}

	public class CmdletSu : CmdletFoundation
	{
		public CmdletSu() : base( "SU" ) { }

		protected override string MySyntax() => Language.Prompt.Get( "MySyntax" );

		protected override string MyHelp() => Language.Prompt.Get( "MyHelp" );

		protected override string MyPurpose() => Language.Prompt.Get( "MyPurpose" );

		// This cmdlet doesn't have any settings...
		public override void Installer() { }

		protected override Applets.OperationalStates Main()
		{
			string cmd = Command.Payload;

			if ( Cli.LaunchArgs.Switches.HasSwitch( "debug" ) && Cli.LaunchArgs[ "debug" ].HasValue( "enablesu" ) || ((CliRegObject)Cli.Registry[ CliRegistry.Hive.System]["Global.AllowSu"]).As<bool>() )
			{
				Con.Tec( "{6}User: " );
				string userName = Con.InputTemplate(
						Cli.Color.Alt( ConsoleColor.DarkYellow ),
						new CliColor( ConsoleColor.Black, ConsoleColor.Yellow ),
						"", 6, 32, 32, '.', '\x00', true
					);

				// If the supplied username is correct in FORM...
				if ( UserInfo.ValidateUserName( userName ) )
				{
					Con.Tec( "{6}Password: " );
					string pass = Con.InputTemplate(
						Cli.Color.Alt( ConsoleColor.DarkYellow ),
						new CliColor( ConsoleColor.Black, ConsoleColor.Yellow ),
						"", 10, 10, 10, '.', 'X', true );

					if ( Cli.Registry.ValidateUser( userName, pass ) )
					{
						UserInfo user = Cli.Registry[ userName ];
						Cli.Enqueue( cmd, user, true );
						// "{,3}&raquo;{F}The requested command has been queued to run under the credentials of {C}$1 {7}(Rank: {E}$2{7,rn})"
						Con.Tec( Language.Prompt.Get( 1 ), new object[] { user.UserName, user.Rank } );
						return Applets.OperationalStates.Complete;
					}
					else
					{
						// "{,3}&raquo;{A,5}Aborted{F}: {,rn}The credentials supplied could not be validated."
						Con.Tec( Language.Prompt.Get( 2 ) );
						return Applets.OperationalStates.IncompleteWithErrors;
					}
				}
				else
				{
					// "{,3}&raquo;{A,5rn}Aborted."
					Con.Tec( Language.Prompt.Get( 3 ) );
					return Applets.OperationalStates.Cancelled;
				}
			}

			// "{,3}&raquo;{A,5rn}The requested operation is unsupported in this mode."
			Con.Tec( Language.Prompt.Get( 4 ) );
			return Applets.OperationalStates.Complete;
		}
	}

	public class CmdletSysSettings : CmdletFoundation
	{
		public CmdletSysSettings() : base( "SYS" ) { }

		/// <remarks>[/LIST:{details}] [/ADDGRP:{groupName}] [/RMGRP:{groupName}] [/SET:{itemSpec}|{value}] [/LOAD:{recover}] [/SAVE]</remarks>
		protected override string MySyntax() => Language.Prompt.Get( "MySyntax" );

		protected override string MyHelp() => Language.Prompt.Get( "MyHelp" );

		protected override string MyPurpose() => Language.Prompt.Get( "MyPurpose" );

		public override void Installer() 
		{
			if ( (AppRegObj is null) || !AppRegObj.HasItem("DefUserSettings" ) ) // !Cli.Registry[ CliRegistry.Hive.System ].HasGroup( this.GetType().Name ) )
			{
				//Cli.Registry.Set( CliRegistry.Hive.App, $"{this.GetType().Name}.DefUserSettings.activeHive", CliRegistry.User );
				//Cli.Registry.Set( CliRegistry.Hive.App, $"{this.GetType().Name}.DefUserSettings.activePath", "" );
			}
		}

		private Applets.OperationalStates DumpReg( Applets.OperationalStates runState )
		{
			ShowContent.DumpOptions opts = ShowContent.DumpOptions.Header | ShowContent.DumpOptions.LineNos | ShowContent.DumpOptions.Footer;
			if ( Command.Switches.HasSwitch( "more" ) ) opts |= ShowContent.DumpOptions.More;
			ShowContent.Dump( Cli.Registry.LastSaved, "Cli.Registry Raw Text", IniFile, opts );
			Console.WriteLine();

			return runState;
		}

		private Applets.OperationalStates List( Applets.OperationalStates runState )
		{
			foreach ( CliRegistry.Hive hive in (CliRegistry.Hive[])Enum.GetValues( typeof( CliRegistry.Hive ) ) )
				if (Command.Switches["list"].IsEmpty || Command.Switches["list"].Values.ContainsAny( new string[] { "All", $"{hive}", $"{hive}Hive" } ))
				{
					Con.Tec( "{7,3}&bull;{,5}Hive {9}[{e}$1Hive{9}]{}: ".Replace( new object[] { hive } ) );

					/*
					if ( Cli.Registry[ hive ].Groups.Length > 0 )
					{
						Console.WriteLine();
						foreach ( string group in Cli.Registry[ hive ].GroupNames() )
							Con.Tec( "{f,5}&raquo;{9,7}\x22{6}$1{9,rn}\x22".Replace( new object[] { group } ) );

						Console.WriteLine();
					}
					else
						Con.Tec( "{e4,24}[{f4}No Groups Found{e4}]{,rn}" );
					*/
					Console.WriteLine( Cli.Registry[ hive ].Diagram() );
				}

			return runState;
		}

		private Applets.OperationalStates Show( Applets.OperationalStates runState )
		{
			Con.Tec( "{}Filename: {9}\x22{e}$1{9 }\x22 ", Cli.Registry.FileName.Condense( Console.WindowWidth - 17, " {9}~{e} " ) );
			Con.Tec( "{7}[{$2}$1{7,rn}]",
				(Cli.Registry.IsSaved ?
					new object[] { "√", new CliColor( ConsoleColor.Green ), "Yes" } :
					new object[] { "X", new CliColor( ConsoleColor.Red ), "No" }
					)
				);

			foreach ( CliRegistry.Hive hive in (CliRegistry.Hive[])Enum.GetValues( typeof( CliRegistry.Hive ) ) )
			{
				ShowContent.DumpOptions opts = ShowContent.DumpOptions.Header | ShowContent.DumpOptions.LineNos | ShowContent.DumpOptions.Header;
				if ( Command.Switches.HasSwitch( "more" ) ) opts |= ShowContent.DumpOptions.More;
				if ( Command.Switches[ "show" ].IsEmpty || Command.Switches[ "show" ].Values.ContainsAny( new string[] { "All", $"{hive}", $"{hive}Hive" } ) )
				{
					
					//ShowContent.Dump( Cli.Registry[ hive ].RawIni, Cli.Registry[ hive ].Name, IniFile, opts );
					Console.WriteLine();
				}
			}

			return runState;
		}

		private Applets.OperationalStates AddGrp( Applets.OperationalStates runState )
		{
			if ( Command.Switches[ "AddGrp" ].HasMultipleValues )
			{
				/*

				string grpName = Command.CmdLine[ "AddGrp" ].Value;
				if ( IniGroupItem.IsValidGroupName( grpName ) )
				{
					if ( Cli.Registry[ CliRegistry.Hive.System ].HasGroup( grpName ) )
					{
						// "{7,3}&raquo;{,5}The group {9}\x22{E}$1{9}\x22 alredy exists."
						Con.Tec( Language.Prompt.Get( 0 ), grpName );
						return Applets.OperationalStates.CompleteWithErrors;
					}

					Cli.Registry[ CliRegistry.Hive.System ].Add( new IniGroupItem( grpName ) );
					// {7,3}&raquo;{,5}The group {9}\x22{e}$1{9}\x22 has been created.
					Con.Tec( Language.Prompt.Get( 1 ), grpName );
					return Applets.OperationalStates.Complete;
				}
				// {7,3}&raquo;{,5}The specified value {9}\x22{e}$1{9}\x22 {,rn}isn't a valid group name.
				Con.Tec( Language.Prompt.Get( 2 ), grpName );
				*/
			}

			return Applets.OperationalStates.IncompleteWithErrors;
		}
		
		private Applets.OperationalStates RmGrp( Applets.OperationalStates runState )
		{
			string grpName = Command.CmdLine[ "RmGrp" ].Value;
			if ( IniGroupItem.IsValidGroupName( grpName ) )
			{
				/*
				if ( Cli.Registry[ CliRegistry.Hive.System ].HasGroup( grpName ) )
				{
					Cli.Registry[ CliRegistry.Hive.System ].RemoveGroup( grpName );
					// {7,3}»{,5}The group {9}\x22{E}$1{9}\x22 {,rn}has been deleted.
					Con.Tec( Language.Prompt.Get( 1 ), grpName );
					return Applets.OperationalStates.Complete;
				}
				// {7,3}»{,5}The group {9}\x22{E}$1{9}\x22 {,rn}doesn&apos;t exist.
				Con.Tec( Language.Prompt.Get( 0 ), grpName );
				*/
			}
			else
				// {7,3}»{,5}The specified value, {9}\x22{e}$1{9}\x22 {,rn}isn&apos;t a valid group name.
				Con.Tec( Language.Prompt.Get( 2 ), grpName );

			return Applets.OperationalStates.CompleteWithErrors;
		}

		private Applets.OperationalStates Load( Applets.OperationalStates runState )
		{
			bool result = false;

			if ( Command.Switches[ "Load" ].HasValue( "recover" ) )
				result = Cli.Registry.Recover( 0 );
			else
			{
				Con.Tec( "{7,3}&raquo;{,5}Loading {9}\x22{6}$1{9}\x22 {}... ", Path.GetFileName( Cli.Registry.FileName ) );
				//result = Cli.Registry.Load();
			}
			Con.Tec( "{$2,rn}$1", 
				result ? 
					new object[] { "Done!", new CliColor( ConsoleColor.Green ) } :
					new object[] { "Failed!", new CliColor( ConsoleColor.Red ) }
				);

			if ( result ) Con.Tec( "{b,5}$1 {,rn}loaded.", Cli.Registry.ToString().Length.FileSizeToString(true) );

			return result ? runState : Applets.OperationalStates.IncompleteWithErrors;
		}

		private Applets.OperationalStates Save( Applets.OperationalStates runState )
		{
			Con.Tec( "{7,3}&raquo;{,5}Backing up the old Registry... " );
			Cli.Registry.Backup(); // Save the existing configuration to a backup image.
			Con.Tec( "{a,rn}Done.{7,3}&raquo;{,5}Saving the current Registry... " );
			Cli.Registry.Save();   // Save the current configuration.
			Con.Tec( "{a,rn}Done.{b,5}$1 {,rn}saved.", Cli.Registry.ToString().Length.FileSizeToString(true) );
			return runState;
		}

		// NOTE: This obfuscates the `User` Accessor from the base class!
		new private Applets.OperationalStates User( Applets.OperationalStates runState )
		{
			ShowContent.DumpOptions opts = ShowContent.DumpOptions.Header | ShowContent.DumpOptions.LineNos | ShowContent.DumpOptions.Header;
			if ( Command.Switches.HasSwitch( "more" ) ) opts |= ShowContent.DumpOptions.More;
			//ShowContent.Dump( Cli.LocalUser.ToCliGroup(), $"Logged-on User: \x22{Cli.LocalUser.FullName}\x22", IniFile, opts );

			return runState;
		}

		protected override Applets.OperationalStates Main()
		{

			return Applets.OperationalStates.Complete;
		}

		public static TextElementRuleCollection IniFile =>
			new TextElementRuleCollection(
				new TextElementRule[]
				{
					new TextElementRule( /* ~Start: tag */ /* language=regex */
						@"^(~(?:START|BEGIN):)(.*)$", "{F}$1 {E}$2", RegexOptions.Multiline ),
					new TextElementRule( /* ~:End tag */ /* language=regex */
						@"^(~:END)(.*)$", "{F}$1{}$2", RegexOptions.IgnoreCase | RegexOptions.Multiline ),
					new TextElementRule( /* Category */ /* language=regex */
						@"^([\s]*)(\[)([a-zA-Z][\w]+[a-zA-Z0-9])(\])", "{a}$2{e}$3{a}$4", RegexOptions.Multiline ),
					new TextElementRule( /* Item name */ /* language=regex */
						@"^([\s]*)([a-z][a-z_0-9]*[a-z0-9])(\??)([\s]*?=[\s]*?)", "{}$1{9}$2{D}$3$4", RegexOptions.IgnoreCase | RegexOptions.Multiline ),
					new TextElementRule( /* Item value */ /* language=regex */
						@"^([^=]+)(=)(.*)$", "$1{7}$2{6}$3", RegexOptions.Multiline ),
					new TextElementRule( /* End of Line Comment */ /* language=regex */
						@"^(.*)(## )(.*)$", "$1{8}$2{B}$3", RegexOptions.Multiline )
				}
			)
			{ ForceXml = false, ReplaceBraces = true, DisplayOptions = ShowContent.DumpOptions.Default };
	}

	public class CmdletUser : CmdletFoundation
	{
		public CmdletUser() : base( "USR" ) { }

		protected override string MyPurpose() => Language.Prompt.Get( "MyPurpose" );
		// "Manages viewing, setting and unsetting environment variables."

		protected override string MySyntax() => Language.Prompt.Get( "MySyntax" );

		protected override string MyHelp() => Language.Prompt.Get( "MyHelp" );
		//"{7,5}&bull;{} The {b}/IMPORT {}switch imports the {6}System {,rn}environment variable settings.";

		private UserInfo EditUserController( UserInfo user )
		{
			bool create = user is null;
			if ( create )
				user = UserInfo.DefaultUser( Ranks.None );

			string	userName =	create ? "" : user.UserName,
					fullName =	create ? "" : user.FullName,
					title =		create ? "Create" : "Edit";

			CliColor white = Con.DefaultColor.Alt( ConsoleColor.White );
			FnKeyCollection fnKeys = new FnKeyCollection();
			//fnKeys.Add( new FnKeyHandler( "Look Up", ConsoleKey.F4, DefaultF4Op ) );
			fnKeys.ActiveFnKeys |= FnKeyCollection.ActiveKeys.F4;

			ScreenEditController sec = new ScreenEditController( $"{title} User: {{6}}{user.FullName}", fnKeys );
			sec.Fields.AddRange(
				new ScreenEditFieldPrototype[]
				{
					new StringScreenEditField("User Name", userName, 0, 4, 20, 4, 16, 1, "", null, white ),
					new StringScreenEditField("Full Name", fullName, 0, 5, 20, 5, 48, 1, "", null, white ),
					new PhoneNumberScreenEditField( "Phone Nbr", user.Phone, new Point(0,6), new Point(20,6), 20, "", null, white),
					new StringScreenEditField( "Email", user.Email, new Point( 0,7 ), new Rectangle( 20,7,60,1 ),"", null, white),
					new Int16ScreenEditField("Rank", user.Rank, 0, 8, 20, 8, 4, "", null, white ),
				}
			);

			sec.Fields[ "userName" ].ValidationPattern = new Regex( @"[a-z][a-z_0-9]+[a-z0-9]", RegexOptions.IgnoreCase );
			sec.Fields[ "userName" ].FilterPattern = new Regex( @"[^a-z0-9_]", RegexOptions.IgnoreCase );
			sec.Fields[ "rank" ].ValidationPattern = new Regex( @"^(?:[0-3]?[0-9]{3}|40[0-8][0-9]|409[0-6]|[0-9]{1,2})$" );
			sec.Fields[ "rank" ].FilterPattern = new Regex( "@[^0-9]" ); // Rank has to be defined as an Int16, but we don't want negatives

			sec[ "userName" ].ReadOnly = !create;
			if (Cli.LocalUser.Rank <= user.Rank) sec.Fields[ "rank" ].ReadOnly = !create;

			ScreenEditDataCollection results = sec.Execute();
			user = new UserInfo(
				results[ "userName" ].As<string>(),
				results[ "fullName" ].As<string>(),
				RankManagement.Convert( results[ "rank" ].As<short>() ),
				results[ "email" ].As<string>(),
				PhoneNumber.Parse( results[ "phoneNbr" ].As<string>() )
			);
			user.Password = new Password( "password" );
			if (!Cli.Registry.HasUser( user.UserName ))
				Cli.Registry.AddUser( user );

			return user;
		}

		public override void Installer() { }

		/// <summary>List Users</summary>
		private Applets.OperationalStates List( Applets.OperationalStates runState )
		{
			if ( !ValidateAccess( Ranks.SystemAdmin ) ) return Applets.OperationalStates.IncompleteWithErrors;

			return Applets.OperationalStates.Complete;
		}

		/// <summary>Add User</summary>
		private Applets.OperationalStates Add( Applets.OperationalStates runState )
		{
			if ( !ValidateAccess( Ranks.SystemAdmin ) ) return Applets.OperationalStates.IncompleteWithErrors;

			UserInfo newUser = EditUserController( null );

			return Applets.OperationalStates.Complete;
		}

		/// <summary>Delete User</summary>
		private Applets.OperationalStates Del( Applets.OperationalStates runState )
		{
			if ( !ValidateAccess( Ranks.SystemAdmin ) ) return Applets.OperationalStates.IncompleteWithErrors;

			return Applets.OperationalStates.Complete;
		}

		private Applets.OperationalStates Edit( Applets.OperationalStates runState )
		{
			string userName = Switches[ "edit" ].IsEmpty || !ValidateAccess( Ranks.SystemAdmin ) ? User.UserName : Switches[ "edit" ].Value;

			if ( Cli.Registry.HasUser( userName ) )
			{
				UserInfo user = EditUserController( Cli.Registry.GetUser( userName ) );

			}
			else
				Tec( "{,3}&raquo;{C}ERROR{7}: {F}The specified user {9}'{6}$1{9}' doesn't exist!", userName );

			return Applets.OperationalStates.Complete;
		}

		/// <summary>Log (on/off).</summary>
		private Applets.OperationalStates Log( Applets.OperationalStates runState )
		{
			bool onOrOff = Switches[ "log" ].IsEmpty ? Cli.LocalUser.IsAnonymous : Regex.IsMatch( Switches[ "Log" ].Value, @"ON", RegexOptions.IgnoreCase );
			if (onOrOff)
			{
				string userName, password;
				// Log On
				try
				{
					// "{}Login{7}: "
					Tec( "$1 ", Language.Prompt.Get( 5 ) );
					do
						userName = Con.InputTemplate( new CliColor( "9" ), Cli.Color, "", 32, 32, 32, '.', '\x00', true );
					while ( userName.Length < 3 );

					// "{}Password{7}: "
					Tec( "$1 ", Language.Prompt.Get( 6 ) );
					do
						password = Con.InputTemplate( new CliColor( "9" ), Cli.Color, "", 10, 64, 16, '.', '*', true );
					while ( password.Length < 3 );

					Tec( "{7,3}&raquo;" );
					if ( Cli.LogOn( userName, password ) )
						// {F}Access {a}confirmed{7}! {9}Welcome, {6}$1{9,rn}!
						Tec( Language.Prompt.Get( 1 ), Cli.LocalUser.FirstName );
					else
						// {C,5}ERROR{7}: {e,rn}The credentials supplied could not be validated.
						Tec( Language.Prompt.Get( 2 ) );
				}
				catch (OperationCanceledException oec)
				{
					// Cancelled!
					Tec( "{,3}&raquo;{e,5rn}$1", Language.Prompt.Get( 4 ) );
				}
			}
			else
			{
				// {B}Good-bye {E}$1{B,rn}!{9,3}->{7,6rn}You have been logged off!
				Tec( Language.Prompt.Get( 0 ), Cli.LocalUser.FirstName );
				Cli.LogOn();
			}

			// If the system requires authentication, and the currently defined user is anyonymous, force the log-on prompt:
			/*
			if ( Cli.RequireAuthentication && Cli.LocalUser.IsAnonymous )
			{
				// {F,rn}Authentication is required, please log in:
				Tec( Language.Prompt.Get( 3 ) );
				Cli.Enqueue( "USR /LOG:ON", false );
			}
			*/

			return Applets.OperationalStates.Complete;
		}

		private Applets.OperationalStates Show( Applets.OperationalStates runState )
		{
			ShowContent.DumpOptions opts = ShowContent.DumpOptions.Header | ShowContent.DumpOptions.LineNos | ShowContent.DumpOptions.Header;
			if ( Command.Switches.HasSwitch( "more" ) ) opts |= ShowContent.DumpOptions.More;
			// Language.Prompt.Get(0) = "Local User Database"
			//ShowContent.Dump( Cli.Registry.ExtractUserDb(), Language.Prompt.Get(0), CmdletSysSettings.IniFile, opts );
			Console.WriteLine();

			return Applets.OperationalStates.Complete;
		}

		/// <summary>Reset password.</summary>
		/// <remarks>System Admins can specify a name to reset, otherwise the currently logged on user is used.</remarks>
		private Applets.OperationalStates Reset( Applets.OperationalStates runState )
		{
			string userName = Switches[ "reset" ].IsEmpty || !ValidateAccess( Ranks.SystemAdmin ) ? User.UserName : Switches[ "reset" ].Value;

			if (Cli.Registry.HasUser(userName))
			{
				if (User.Rank < Ranks.SystemAdmin)
				{
					Tec( Language.Prompt.Get( 0 ) );
					//Tec( "{}&raquo; {7}Please enter your current password: " );
					try
					{
						string passWord = Con.InputTemplate( new CliColor( "9" ), Cli.Color, "", 10, 64, 16, '.', '*', true );
						if ( !Cli.Registry.ValidateUser( userName, passWord ) )
						{
							//Tec( "{,3}&raquo;{5,7rn}The supplied password doesn't match!" );
							Tec( Language.Prompt.Get( 1 ) );
							return Applets.OperationalStates.IncompleteWithErrors;
						}
					}
					catch ( OperationCanceledException oce ) 
					{
						Tec( Language.Prompt.Get( 2 ) );
						//Tec( "{,3}&raquo;{E,7rn}Cancelled!" );
						return Applets.OperationalStates.Cancelled;
					}
				}

				string newPass1, newPass2;
				try
				{
					do
					{
						//Tec( "{,3}&raquo; {7}Please enter the new password for {9}\"{D}$2{9}\"{7,rn}:{,5}", userName );
						Tec( Language.Prompt.Get( 3 ), userName );
						newPass1 = Con.InputTemplate( new CliColor( "9" ), Cli.Color, "", 10, 64, 16, '.', '*', true );
						//Tec( "{7,5rn}Again:{,5}" );
						Tec( Language.Prompt.Get( 4 ) );
						newPass2 = Con.InputTemplate( new CliColor( "9" ), Cli.Color, "", 10, 64, 16, '.', '*', true );

						if ( !string.IsNullOrEmpty( newPass1.Trim() ) || (newPass1.Trim().Length < 6) )
						{
							//Tec( "{C,7}ERROR{7}: {F,rn}The Password must be at least 6 characters in length and cannot begin or end with whitespace.{,rn} " );
							Tec( Language.Prompt.Get( 5 ) );
							throw new OperationCanceledException( "The new password must be at least 6 characters in length and cannot begin or end with whitespace." );
						}

						if ( !newPass1.Equals( newPass2, StringComparison.CurrentCulture ) )
						{
							//Tec( "{C,7}ERROR{7}: {F,rn}The supplied passwords do not match.{,rn} " );
							Tec( Language.Prompt.Get( 6 ) );
							throw new OperationCanceledException( "The supplied passwords did not match." );
						}

					} while ( !newPass1.Equals( newPass2, StringComparison.CurrentCulture ) );

					Cli.Registry.CurrentUser.Data.Password = Cli.Registry.CurrentUser.Data.CreateNewPassword( newPass1 );
					Tec( "{,3}&raquo;{5,rn}$1", Language.Prompt.Get(7) );
					return Applets.OperationalStates.Complete;
				}
				catch ( OperationCanceledException oce )
				{
					Tec( "{,3}&raquo;{E,7rn}$1", Language.Prompt.Get( 8 ) );
					return Applets.OperationalStates.Cancelled;
				}
			}

			return runState;
		}

		protected override Applets.OperationalStates Main()
		{

			return Applets.OperationalStates.Complete;
		}
	}

	public class CmdletVer : CmdletFoundation
	{
		public CmdletVer() : base( "VER" ) { }

		protected override string MyPurpose() => Language.Prompt.Get( "MyPurpose" );
		// "Displays details about all currently loaded modules."

		public override void Installer() { }

		/// <param name="args">[0] = _localUser</param>
		protected override Applets.OperationalStates Main()
		{
			// "{,3}&raquo;{E,5}$1 {7}- {D,rn}$2{7,5rn}Included Assemblies:"
			Con.Tec( Language.Prompt.Get( 0 ), new object[] {
				Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName),
				Assembly.GetEntryAssembly().GetName().Version
			} );
			string template = "{,7}&raquo;{B,9}$1 {}&lbrace;$3{}, $4{}&rbrace;";
			string[] parse( string input )
			{
				string[] segments = input.Split( new char[] { ',' } );
				for ( int i = 1; i < segments.Length; i++ )
					segments[ i ] = "{9}" + segments[ i ].Replace( "=", "{}: {E}" ).Trim();
				return segments;
			}

			foreach ( Assembly assy in AppDomain.CurrentDomain.GetAssemblies() )
			{
				Con.Tec( template, parse( Path.GetFileName( assy.GetName().FullName ) ) );
				Con.Tec( "{F} Ver{}: {D,rn}$1", assy.GetName().Version );
			}

			return Applets.OperationalStates.Complete;
		}
	}
}
