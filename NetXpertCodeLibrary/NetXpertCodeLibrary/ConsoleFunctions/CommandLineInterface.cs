using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;
using NetXpertCodeLibrary.ConfigManagement;
using NetXpertCodeLibrary.Extensions;
using NetXpertCodeLibrary.LanguageManager;
using NetXpertCodeLibrary.ContactData;

namespace NetXpertCodeLibrary.ConsoleFunctions
{
	/// <summary>Used to facilitate overriding the specified Windows command keys to steal focus from the CLI.</summary>
	[Flags] public enum SuspendKeys { None = 0, CtrlEsc = 1, AltTab = 2, WinKey = 4, All = 255 }

	/// <summary>Defines the data that a Heartbeat event handler will receive from the CLI when it's called.</summary>
	public class CommandLineEventArgs : EventArgs
	{
		#region Properties
		protected UserInfo _currentUser = null;
		int x = 0, y = 0;
		AppletList _applets = null;
		#endregion

		#region Constructors
		public CommandLineEventArgs(int cursorLeft, int currentLine, UserInfo user, AppletList applets)
		{
			this.x = cursorLeft;
			this.y = currentLine;
			this.CurrentUser = user;
			this._applets = applets;
		}

		public CommandLineEventArgs(UserInfo user, AppletList applets)
		{
			this.x = Console.CursorLeft;
			this.y = Console.CursorTop;
			this.CurrentUser = user;
			this._applets = applets;
		}
		#endregion

		#region Accessors
		/// <summary>Contains the UserInfo credentials of the currently authenticated user at the time that the event was raised.</summary>
		public UserInfo CurrentUser
		{
			get => this._currentUser;
			protected set => this._currentUser = (value is null) ? UserInfo.DefaultUser() : value;
		}

		/// <summary>Specifies the location of the cursor in the Console window when the event was raised.</summary>
		public Point CursorLocation
		{
			get => new Point(this.x, this.y);
			protected set { this.x = value.X; this.y = value.Y; }
		}

		/// <summary>A list of the currently loaded applet prototypes at the time the event was raised.</summary>
		public AppletList Applets => this._applets;
		#endregion
	}

	/// <summary>Commandeers the active Console window and creates a new generic CLI interface on top of it.</summary>
	public sealed class CommandLineInterface
	{
		#region Properties
		private static readonly CommandLineInterface _instance = new CommandLineInterface();

		///<summary>The value here defines the command to exit / close the shell. MAX THREE CHARACTERS!</summary>
		public const string EXIT_COMMAND = "XIT";

		/// <summary>The number of milliseconds to delay on each pass of the keyboard polling function.</summary>
		public static int KEYBOARD_INTERVAL = 10;

		/// <summary>The number of milliseconds to delay on each pass of the command polling function.</summary>
		public static int COMMAND_INTERVAL = 250;

		/// <summary>If there's to be a hearbeat function, this specifies on what intervals it is triggered (in millisecods)</summary>
		public static int HEARTBEAT_INTERVAL = 0;

		/// <summary>Supports attaching functions to perform on heartbeat intervals.</summary>
		public delegate void HeartbeatPulse( object myObject, CommandLineEventArgs args );
		public event HeartbeatPulse OnHeartbeat;

		///<summary>Stores the currently known command-line text.</summary>
		private CommandInput _cmd = new CommandInput( "NewCLI>" );

		///<summary>Keep processing until this is false.</summary>
		private bool _keepAlive = true;

		/// <summary>Stores the default console color settings</summary>
		private CliColor _defaultColor = CliColor.CaptureConsole();

		/// <summary>Specifies the Rank to use when no user has authenticated.</summary>
		private Ranks? _defaultUserRank = null;

		/// <summary>This class is used by applets that use Windows Forms to provide an independent Messaging Thread for those forms to operate on.</summary>
		//private BackgroundForm _backgroundForm = new BackgroundForm();

		/// <summary>Specifies an interval at which specified Heartbeat events will be raised.</summary>
		private Stopwatch _heartbeat = new Stopwatch();

		/// <summary>Specifies the intervals (in seconds) at which the Heartbeat pulses. 0 disables the heartbeat.</summary>
		private int _hbInterval = 0;

		/// <summary>If set to TRUE, user log-on/authentication will be required in order to gain access to the CLI.</summary>
		/// <remarks>This is a nullable boolean so that it's uninitialized state can be detected.</remarks>
		private bool? _requireAuthorization = null;

		///<summary>Manages the three threads that do the heavy lifting here.</summary>
		private Thread ConsoleKeyManagement;
		private Thread _commandManagement = null;
		//private Thread WinFormsManagement;
		//private Thread FormsThread;

		/// <summary>Used internally to identify system Cmdlets that are to be disabled/disallowed.</summary>
		private string _revokedCmdlets = "";

		/// <summary>Used to hold/maintain the local registry settings for the CLI.</summary>
		private readonly CliRegistry _registry = CliRegistry.DefaultRegistry();
		#endregion

		#region Constructors
		/// <summary>Initializes the CLI</summary>
		//public CommandLineInterface( CliColor color = null, Prompt prompt = null, bool requireAuthorization = false, Ranks defaultUserRank = Ranks.None )
		private CommandLineInterface( params CliRegObject[] customSysObjects )
		{
			//FormsThread = new Thread( FormsWorkerStart );
			//FormsThread.IsBackground = true;
			//FormsThread.Start();

			var args = Environment.GetCommandLineArgs();

			// Initializes the Ctrl-C event handler system and hooks it to Console.CancelEvent ...
			this.CtrlCEvents = new ConsoleCancelEventCollection();
			this.CtrlCEvents.Add( "Default", ConsoleCancelEventCollection.DefaultHandler );
			this.Color = CliColor.CaptureConsole();

			// Loads the Registry: 'null' here indicates that the default registry filename should be used.
			this._registry = CliRegistry.Load( null );
		}

		/// <summary>Explicit static constructor to tell C# compiler not to mark type as beforefieldinit.</summary>
		static CommandLineInterface() { }
		#endregion

		#region Accessors
		/// <summary>Instantiates/implements a Singleton implementation of this class.</summary>
		public static CommandLineInterface CLI => _instance;

		public Thread CommandManagement
		{
			get => this._commandManagement;
			set
			{
				if ( !(this._commandManagement is null) )
				{
					DateTime timeout = DateTime.Now;
					_keepAlive = false;
					while ( (((TimeSpan)(DateTime.Now - timeout)).Seconds < 10) && (this._commandManagement.ThreadState == System.Threading.ThreadState.Running) ) ;

					if ( this._commandManagement.ThreadState == System.Threading.ThreadState.Running )
						_commandManagement.Abort();

					_commandManagement = null;
				}
				_commandManagement = (value is null) ? new Thread( CommandProcessor ) : value;
				_commandManagement.Start();
			}
		}

		/// <summary>Initialises and Loads the CliRegistry settings.</summary>
		public CliRegistry Registry => this._registry;

		///<summary>Manages Environment Variables</summary>
		public EnvironmentVars Env { get; set; } = new EnvironmentVars();

		/// <summary>Reports the number of managed environment variables.</summary>
		public int Count => Env.Count;

		/// <summary>Facilitates interacting with the Environment variables directly via "this["envName"]"</summary>
		/// <param name="name">A string specifying the name of the environment variable to interrogate.</param>
		/// <returns>If the requested environment variable exists, it's value, otherwise an empty string.</returns>
		public string this[ string name ]
		{
			get => this.Env.HasVariable( name ) ? this.Env[ name ].Value : "";
			set => this.Env.Set( name, value );
		}

		/// <summary>Sets/Gets the current color settings of the CLI as a CliColor object.</summary>
		public CliColor Color
		{
			get => this._defaultColor;
			set => (this._defaultColor = (value is null ? CliColor.CaptureConsole() : value)).ToConsole();
		}

		/// <summary>Sets/Gets the CLI's current foreground color.</summary>
		public ConsoleColor ForegroundColor
		{
			get => Console.ForegroundColor;
			set => Console.ForegroundColor = value;
		}

		/// <summary>Stores a collection of applet prototypes that can be executed from the command line.</summary>
		public AppletList Applets { get; set; } = null;

		/// <summary>Stores a collection of cmdlets that can be executed from the command line.</summary>
		public CmdletCollection Cmdlets { get; private set; } = null;

		/// <summary>A comma-separated list of Cmdlets to disable by their 3-digit name.</summary>
		/// <remarks>Built-in Cmdlets are all in the "NetXpertCodeLibrary.ConsoleFunctions.Cmdlets.cs" file.</remarks>
		public string RevokedCmdlets
		{
			get => this._revokedCmdlets;
			set
			{
				if ( string.IsNullOrWhiteSpace( value ) )
					this._revokedCmdlets = "";
				else
				{
					value = Regex.Replace( value.ToUpperInvariant(), @"[^\w,]", "" ); // Remove invalid characters...
					if ( value.Length > 1 )
					{
						List<string> cmds = new List<string>();

						// Add any existing revoked cmdlets to the list...
						if ( this._revokedCmdlets.Length > 0 )
							cmds.AddRange( this._revokedCmdlets.Split( new char[] { ',' } ) );

						string[] cmdList = value.Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries );

						// Add all valid entries in the supplied string, but no duplicates!
						foreach ( string c in cmdList )
							if ( Regex.IsMatch( c, @"^[a-zA-Z][\w]{1,2}$" ) && 
								((this.Cmdlets.Count < 1) || this.Cmdlets.HasCmd(c)) &&
								!cmds.Contains( c ) 
								)
									cmds.Add( c );
						cmds.Sort(); // makes the list tidier..

						// Reconstruct the revocation string with the new data.
						this._revokedCmdlets = string.Join( ",", cmds );

						if ( !(Cmdlets is null) )
							Cmdlets.Remove( this._revokedCmdlets );
					}
				}
			}
		}

		/// <summary>Stores User-created Aliases</summary>
		public AliasCollection Aliases { get; set; } = new AliasCollection( 10 );

		/// <summary>Sets/Gets the CLI's current background color.</summary>
		public ConsoleColor BackgroundColor
		{
			get => Console.BackgroundColor;
			set => Console.BackgroundColor = value;
		}

		/// <summary>Allows the CLI to disable the system functions of the specified keys.</summary>
		/// <remarks>Uses the definitions in SuspendKeys: None, CtrlEsc, AltTab, WinKey, All</remarks>
		public SuspendKeys SuspendSystemKeys { get; set; } = SuspendKeys.None;

		///<summary>Used to tell the keyboard thread when to listen and when not to...</summary>
		public bool Listener { get; set; } = false;

		/// <summary>Sets/Gets the CLI's prompt.</summary>
		public Prompt Prompt
		{
			get => this._cmd.Prompt;
			set
			{
				if ( !(value is null) )
				{
					if ( _cmd is null )
						this._cmd = new CommandInput( value );
					else
						this._cmd.Prompt = value;
				}

				this._cmd.Write();
			}
		}

		/// <summary>Provides solution-wide access to the SharedData pool.</summary>
		public SharedData SharedDataPool => this.Pool;

		/// <summary>If "LocalUser" is unpopulated, returns the system-defined "_defaultUserRank", otherwise, "LocalUser.Rank" is returned.</summary>
		public Ranks EffectiveRank => LocalUser is null ? DefaultUserRank : LocalUser.Rank.ToRank;

		/// <summary>Specifies (or indicates) how often (in seconds) the Heartbeat Event should be raised.</summary>
		public int HeartBeatInterval
		{
			get => (this._hbInterval / 1000);
			set
			{
				if ( value > 0 )
				{
					this._hbInterval = (value * 1000);
					if ( !this._heartbeat.IsRunning )
						this._heartbeat.Start();
				}
				else
				{
					this._hbInterval = 0;
					if ( this._heartbeat.IsRunning ) this._heartbeat.Reset();
				}
			}
		}

		/// <summary>Gets/Sets the Console Window location (screen relative).</summary>
		public Point Location
		{
			get => GetConsoleRectangle().Location;
			set => MoveConsoleWindow( value, Size );
		}

		/// <summary>Gets/Sets the Console Window size. (pixels, not characters!)</summary>
		public Size Size
		{
			get => GetConsoleRectangle().Size;
			set => MoveConsoleWindow( Location, value );
		}

		/// <summary>Creates/Applies a WindowInfo object from/to the Console Window.</summary>
		public WindowInfo WindowInfo
		{
			get => new WindowInfo( Location, Size );
			set => MoveConsoleWindow( value.Location, value.Size );
		}

		/// <summary>Attempts to apply the FormWindowState value to a ConsoleWindow (seems to just always return "Normal" though).</summary>
		public FormWindowState WindowState
		{
			get => GetConsoleWindowState();
			set => SetConsoleWindowState( value );
		}

		/// <summary>Provides a handy mechanism for reading the DEFAULT system settings.</summary>
		// A file must be stored in a Folder called "ExternalResources" under the main project that has the same
		// filename as the executable, with an ".ini" extension. The file must be compiled into the source (see
		// it's Properties > Build Action setting and set it to "Embedded Resource").
		public IniFile DefaultSettings
		{
			get
			{
				string exeName = NetXpertExtensions.ExecutableName;
				return IniFile.FetchResourceFile( $"{exeName}.ExternalResources.{exeName}.ini" );
			}
		}

		/// <summary>Stores the user information (credentials / names) for the currently authenticated user.</summary>
		public UserInfo LocalUser => this.Registry.CurrentUser.Data; 

		///<summary>Stores and manages entered commands.</summary>
		public CommandCollection CommandCache { get; private set; } = new CommandCollection();

		/// <summary>Holds any switches / data passed on the command line when the app was launched.</summary>
		public CommandLine LaunchArgs =>
			new CommandLine( Process.GetCurrentProcess().ProcessName + String.Join( " ", Environment.GetCommandLineArgs() ) );

		/// <summary>Provides a mechanism for sharing data between apps.</summary>
		private SharedData Pool { get; set; } = new SharedData();

		/// <summary>Provides a mechanism for managing CTRL-C keypress events if the console's "TreatCtrlCAsInput" value is FALSE.</summary>
		public ConsoleCancelEventCollection CtrlCEvents { get; set; }

		/// <summary>If set to TRUE the system will require user-authentication prior to allowing access to the CLI.</summary>
		/// <remarks>This can *only* be set once!</remarks>
		public bool RequireAuthentication
		{
			get => this._requireAuthorization is null ? false : (bool)this._requireAuthorization;
			set
			{
				if ( this._requireAuthorization is null )
					this._requireAuthorization = value;
			}
		}

		/// <summary>Defines the Default Rank used when an applet/cmdlet is instantiated and no LocalUser is defined.</summary>
		public Ranks DefaultUserRank
		{
			get => this._defaultUserRank is null ? Ranks.Unverified : (Ranks)this._defaultUserRank;
			private set
			{
				if ( this._defaultUserRank is null )
					this._defaultUserRank = value;
			}
		}
		#endregion

		#region Methods
		#region Methods to facilitate working with forms
		/*
		private void FormsWorkerStop() =>
			this._backgroundForm.BeginInvoke( (Action)delegate { this._backgroundForm.Stop(); } );

		public void FormsWorkerOpenForm( ThreadedHandle formHandle ) =>
			this._backgroundForm.BeginInvoke( (Action)delegate { this._backgroundForm.Show( formHandle ); } );

		public void FormsWorkerOpenForm( Type formType ) =>
			this._backgroundForm.BeginInvoke( (Action)delegate { this._backgroundForm.Show( formType ); } );

		public void FormsWorkerOpenForm( ThreadedFormBase form ) =>
			this._backgroundForm.BeginInvoke( (Action)delegate { this._backgroundForm.Show( form ); } );

		public void FormsWorkerCloseform( ThreadedHandle formHandle ) =>
			this._backgroundForm.BeginInvoke( (Action)delegate { this._backgroundForm.Close( formHandle ); } );

		public void ModifyFormAttribute( ThreadedHandle formHandle, string attributeName, object value = null ) =>
			this._backgroundForm.BeginInvoke( (Action)delegate { this._backgroundForm.ModifyAttribute( formHandle, attributeName, value ); } );

		public object GetFormAttribute( ThreadedHandle formHandle, string attributeName )
		{
			object result = null;
			this._backgroundForm.BeginInvoke( (Action)delegate { result = this._backgroundForm.GetAttribute( formHandle, attributeName ); } );
			return result;
		}

		public FormWindowState GetFormWindowState( ThreadedHandle formHandle )
		{
			var result = GetFormAttribute( formHandle, "WindowState" );
			return (result is null) ? FormWindowState.Normal : (FormWindowState)result;
		}

		public void SetFormWindowState( ThreadedHandle formHandle, FormWindowState state ) =>
			ModifyFormAttribute( formHandle, "WindowState", state );

		public ThreadedHandle FindFormHandle<T>() where T : ThreadedFormBase =>
			this._backgroundForm.FindFormHandle<T>();

		/// <summary>Returns the requested ThreadedFormBase derivative form based on it's Uid value.</summary>
		/// <typeparam name="T">Defines the Type of ThreadedFormBase derivative class to return.</typeparam>
		/// <param name="uid">The UID to search for and return.</param>
		/// <returns>A form of the specified type, with the specified Uid if one exists, otherwise Null.</returns>
		/// <exception cref="System.InvalidOperationException">Cross-thread operation not valid. Control <FormType> accessed from a thread other than the thread it was created on.</FormType></exception>
		/// <remarks>Note that, due to the threading model used to effect Windows Forms in a console application, attempting to do virtually ANYTHING 
		/// with the form returned by this function will result in the Exception noted above being thrown.</remarks>
		public T GetForm<T>( string uid ) where T : ThreadedFormBase =>
			(T)this._backgroundForm.GetFormByUid( uid );

		/// <summary>Returns the requested ThreadedFormBase derivative form based on it's ThreadedHandle.</summary>
		/// <typeparam name="T">Defines the Type of ThreadedFormBase derivative class to return.</typeparam>
		/// <param name="handle">The ThreadedHandle value to search for and return.</param>
		/// <returns>A form of the specified type, with the specified Uid if one exists, otherwise Null.</returns>
		/// <exception cref="System.InvalidOperationException">Cross-thread operation not valid. Control <FormType> accessed from a thread other than the thread it was created on.</FormType></exception>
		/// <remarks>Note that, due to the threading model used to effect Windows Forms in a console application, attempting to do virtually ANYTHING 
		/// with the form returned by this function will result in the Exception noted above being thrown.</remarks>
		public T GetForm<T>( ThreadedHandle handle ) where T : ThreadedFormBase =>
			(T)this._backgroundForm.GetFormByHandle( handle );

		public void FormsWorkerStart()
		{
			Application.EnableVisualStyles();
			this._backgroundForm = new BackgroundForm();
			this._backgroundForm.Show();
			this._backgroundForm.Hide();
			while ( FormsWorker() && _keepAlive ) ;
		}

		public bool FormsWorker()
		{
			try { Application.Run(); }
			catch ( Exception e )
			{
				if ( this.HasEnvVar( "EXCEPTIONS" ) && this[ "EXCEPTIONS" ].Equals( "Allow", StringComparison.OrdinalIgnoreCase ) )
					throw e;

				ConsoleFunctions.Applets.ExceptionDump( e, this.HasEnvVar( "DEBUG" ) );
				//Con.Tec( "{,rn}{7,3}&raquo;{F4,5} WARNING! -- An Untrapped Exception has crashed the Forms Thread! {,rn} " +
				//	"{,5rn}This shell is now in an UNSTABLE condition and all open Forms have been terminated! " +
				//	"{,5}Please save / close any open files / applets, and {b}XIT{,rn} the CLI as soon as possible!\r\n" );
				Con.Tec( "{,rn}{7,3}&raquo;{F4,5} $1\r\n", Language.Prompt.Get( 0 ) );

				Prompt.Write();
				return true;
			}
			return false;
		}
		*/
		#endregion

		/// <summary>Provides a null-safe mechanism for Applets to see if the local user has the specified access.</summary>
		/// <param name="checkRank">A Ranks enumeration value to compare with the current user.</param>
		/// <returns>TRUE if the local user's rank is greater than or equal to the specified value.</returns>
		public bool AllowAccess( Ranks checkRank ) => AllowAccess( (short)checkRank );

		/// <summary>Provides a null-safe mechanism for Applets to see if the local user has the specified access.</summary>
		/// <param name="checkRank">A RankManagement class to compare with the current user.</param>
		/// <returns>TRUE if the local user's rank is greater than or equal to the specified value.</returns>
		public bool AllowAccess( RankManagement rank ) => AllowAccess( rank.Rank );

		/// <summary>Provides a null-safe mechanism for Applets to see if the local user has the specified access.</summary>
		/// <param name="checkRank">An Int value to compare with the current user's Rank.</param>
		/// <returns>TRUE if the local user's rank is greater than or equal to the specified value.</returns>
		public bool AllowAccess( short checkRank ) =>
			(LocalUser is null) ? checkRank == 0 : checkRank <= LocalUser.Rank;

		/// <summary>Multi-threaded function for managing keyboard input (replacing Console.ReadLine())</summary>
		[STAThread]
		public void ManageInput()
		{
			while ( _keepAlive )
			{
				while ( Console.KeyAvailable && Listener )
				{
					Listener = false; // Don't listen for more keys until we finish processing this one...
					ConsoleKeyInfo key = Console.ReadKey( true );
					switch ( key.Key )
					{
						case ConsoleKey.Tab:
							if ( (int)key.Modifiers == 0 )
							{
								if ( (this._cmd.Cursor > 0) && !(Applets is null) )
								{
									string s = Applets.Find( this._cmd.Data.Trim(), LocalUser.Rank );
									if ( s.Length > 0 )
									{
										this._cmd.Data = s + " ";
										this._cmd.Cursor = s.Length + 1;
										this._cmd.Write();
									}
									else
										System.Media.SystemSounds.Beep.Play();
								}
							}
							//System.Media.SystemSounds.Beep.Play();
							break;
						case ConsoleKey.X: // Ctrl-X = Cut command-line and copy to clipboard...
							if ( (key.Modifiers == ConsoleModifiers.Control) && (this._cmd.Data.Length > 0) )
							{
								Clipboard.SetText( this._cmd.Data );
								this._cmd.Data = "";
								this._cmd.Write();
								break;
							}
							goto default; // Regular 'X' pressed, go process it...
						case ConsoleKey.V: // Ctrl-V = Paste from clipboard into command-line at cursor...
							if ( key.Modifiers == ConsoleModifiers.Control )
							{
								string[] lines = (Clipboard.GetText() + "\r\n").Split( new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries );
								if ( lines.Length > 0 )
								{
									if ( lines.Length == 1 )
									{
										this._cmd.InsertAtCursor( lines[ 0 ] );
										Con.ClearLine( this.Prompt, this._cmd.Data, Color );
									}
									else
										foreach ( string line in lines )
										{
											Con.ClearLine( this.Prompt, line + "\r\n", Color );
											CommandCache.Add( line, LocalUser );
										}
								}
								break;
							}
							goto default; // Regular 'V' pressed, go deal with it...
						case ConsoleKey.T: // Ctrl-T = Insert Timestamp at cursor
							if ( key.Modifiers == ConsoleModifiers.Control )
							{
								string time = DateTime.Now.ToMySqlString();
								if ( !key.Modifiers.HasFlag( ConsoleModifiers.Alt ) )
									time = '"' + time + '"';

								int curPos = Console.CursorLeft;
								this._cmd.InsertAtCursor( time );
								Con.Cursor += time.Length;
								Con.ClearLine( this.Prompt, this._cmd.Data, Color );
								break;
							}
							goto default; // Regular 'T' key pressed, go process it...
						case ConsoleKey.C: // Ctrl-C = Copy command-line to Clipboard...
							if ( (key.Modifiers == ConsoleModifiers.Control) && (this._cmd.Data.Length > 0) )
							{
								Clipboard.SetText( this._cmd.Data );
								break;
							}
							goto default; // Regular 'C' key pressed, go process it...
						case ConsoleKey.F4: // Accomodates the Windows' ALT-F4 "Close this Window" keyboard shortcut.
							if ( key.Modifiers == ConsoleModifiers.Alt )
							{
								this._cmd.Data = EXIT_COMMAND;
								CommandCache.Add( this._cmd.WriteLn( LocalUser ) );
							}
							break;
						case ConsoleKey.Escape: // Clear line back to prompt.
							if ( key.Modifiers == ConsoleModifiers.Shift )
							{
								this._cmd.Data = EXIT_COMMAND;
								CommandCache.Add( this._cmd.WriteLn( LocalUser ) );
							}

							Console.Write( "".PadRight( this._cmd.Data.Length, '\b' ) );
							this._cmd.Data = "";
							this._cmd.Write();
							break;
						case ConsoleKey.Enter: // Enter accept command as entered.
							if ( this._cmd.Length > 0 )
							{
								CommandCache.Add( new Command( this._cmd.Data, LocalUser ) );
								this._cmd.Data = "";
								Con.ClearLine();
							}
							else { Console.WriteLine(); Prompt.Write(); }
							break;
						case ConsoleKey.Backspace: // Delete character left...
							if ( this._cmd.Cursor > 0 )
							{
								this._cmd.Cursor -= 1;
								this._cmd.DeleteAtCursor();
								this._cmd.Write();
							}
							break;
						case ConsoleKey.Delete: // Delete character under cursor...
							if ( this._cmd.Cursor < this._cmd.Length )
							{
								this._cmd.DeleteAtCursor();
								this._cmd.Write();
							}
							break;
						case ConsoleKey.LeftArrow: // Move Word/Character left
							if ( this._cmd.Cursor > 0 )
							{
								if ( key.Modifiers == ConsoleModifiers.Control )
								{
									// CTRL-Left-Cursor -> word-left
									this._cmd.WordLeft();
								}
								else if ( !key.Modifiers.HasFlag( ConsoleModifiers.Alt | ConsoleModifiers.Control | ConsoleModifiers.Shift ) )
								{
									// Left-Cursor -> character-left
									this._cmd.Cursor -= 1;
								}
							}
							break;
						case ConsoleKey.RightArrow: // Word/Character right
							if ( this._cmd.Cursor < this._cmd.Length )
							{
								if ( key.Modifiers.HasFlag( ConsoleModifiers.Control ) )
								{
									// CTRL-Right-Cursor -> word-right
									this._cmd.WordRight();
								}
								else if ( !key.Modifiers.HasFlag( ConsoleModifiers.Alt | ConsoleModifiers.Control | ConsoleModifiers.Shift ) )
								{
									// Right-Cursor -> character-right
									this._cmd.Cursor += 1;
								}
							}
							break;
						case ConsoleKey.UpArrow: // Previous cached command
							if ( !key.Modifiers.HasFlag( ConsoleModifiers.Alt | ConsoleModifiers.Control ) )
							{
								if ( this._cmd.Length + this.Prompt.Length > Console.BufferWidth )
								{
									Console.Write( "\r".PadRight( Console.BufferWidth - 1, ' ' ) );
									int lines = (this._cmd.Length + Prompt.Length) / Console.BufferWidth;
									Console.CursorTop = Math.Max( 0, Console.CursorTop - lines );
								}

								this._cmd.Data = CommandCache.Previous;
								this._cmd.Cursor = int.MaxValue;
								this._cmd.Write();
							}
							break;
						case ConsoleKey.DownArrow: // Next cached command
							if ( !key.Modifiers.HasFlag( ConsoleModifiers.Alt | ConsoleModifiers.Control ) )
							{
								if ( this._cmd.Length + this.Prompt.Length > Console.BufferWidth )
								{
									Console.Write( "\r".PadRight( Console.BufferWidth - 1, ' ' ) );
									int lines = (this._cmd.Length + this.Prompt.Length) / Console.BufferWidth;
									Console.CursorTop = Math.Max( 0, Console.CursorTop - lines );
								}

								this._cmd.Data = CommandCache.Next;
								this._cmd.Cursor = int.MaxValue;
								this._cmd.Write();
							}
							break;
						case ConsoleKey.End: // Cursor to end of line.
							if ( !key.Modifiers.HasFlag( ConsoleModifiers.Alt | ConsoleModifiers.Control ) )
								this._cmd.Cursor = _cmd.Length;
							break;
						case ConsoleKey.Home: // Cursor to beginning of line.
							if ( !key.Modifiers.HasFlag( ConsoleModifiers.Alt | ConsoleModifiers.Control ) )
								this._cmd.Cursor = 0;
							break;
						default:
							if (
								!key.Modifiers.HasFlag( ConsoleModifiers.Alt | ConsoleModifiers.Control ) &&
								@"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ!@#$%^&*()_+=-0987654321`~[]{}\|,./<>?;:'"" ".ToCharArray().Contains( key.KeyChar )
								)
							{
								this._cmd += key.KeyChar; //≤|≥
								this._cmd.Write();
							}
							break;
					}
					Listener = true;
				}
				if ( _keepAlive && (KEYBOARD_INTERVAL > 0) )
					Thread.Sleep( KEYBOARD_INTERVAL );
			}
		}

		/// <summary>Reports on the existence of a specified environment variable.</summary>
		/// <param name="name">The name of the variable to query (case-insensitive).</param>
		/// <returns>TRUE if the variable exists and is defined in the environment, otherwise false.</returns>
		public bool HasEnvVar( string name ) => (this.Env.HasVariable( name ) && (this[ name ].Length > 0));

		public bool SetEnvVar( string name, string value = "", bool readOnly = false, string owner = "" ) =>
			SetEnvVar( new EnvironmentVar( name, value, readOnly, string.IsNullOrWhiteSpace( owner ) ? LocalUser.UserName : owner ) );

		public bool SetEnvVar( EnvironmentVar newEnvVar ) =>
			this.Env.Set( newEnvVar );

		public bool SetEnvVars( EnvironmentVar[] vars ) =>
			this.Env.AddRange( vars ) == vars.Length;

		public bool SetEnvVars( KeyValuePair<string, string>[] values, bool readOnly = false, string owner = "" ) =>
			this.Env.AddRange( values, readOnly, owner ) == values.Length;

		public string ApplyEnvironmentTo( string source ) =>
			Env.ApplyTo( source );

		/// <summary>Reports on whether a specified script name exists within the internal resources of the parent project's executable.</summary>
		/// <param name="name">The name of the script to look for (no extension!).</param>
		/// <returns>TRUE if the requested script file was located in the "ExternalResources" folder of the parent project.</returns>
		public bool HasInternalScript( string name )
		{
			if ( !string.IsNullOrWhiteSpace( name ) )
			{
				if ( !Regex.IsMatch( name, @"\.script$", RegexOptions.IgnoreCase ) )
					name = name.TrimEnd( new char[] { '\\', '.' } ) + ".script";

				string root = Assembly.GetEntryAssembly().GetName().Name + ".ExternalResources.";
				string[] scripts = Assembly.GetEntryAssembly().GetManifestResourceNames();
				int i = -1; while ( ++i < scripts.Length )
					if ( scripts[ i ].Substring( 0, root.Length ).Equals( root ) && Regex.IsMatch( scripts[ i ], $"{name}$", RegexOptions.Compiled ) )
						return true;
			}
			return false;
		}

		/// <summary>Given an array of strings (essentially a batch), executes all instructions that are contained therein.</summary>
		public void RunScript( string[] source, UserInfo user = null )
		{
			if ( user is null ) user = LocalUser is null ? UserInfo.DefaultUser( Ranks.BasicUser ) : LocalUser;
			string echo = this.Env.HasVariable( "ECHO" ) ? this.Env[ "ECHO" ].Value : "";
			foreach ( string s in source )
				if ( !s.Match( @"^[\t ]*(\/\/|\#\#)[\t ]*[\s\S]*" ) && (s.Trim().Length > 0) )
					Enqueue( s, user, false );

			if ( echo.Length > 0 )
				Enqueue( "EKO /SET:" + echo, user, false );
		}

		public void RunScript( string source, UserInfo user = null )
		{
			if ( user is null ) user = UserInfo.DefaultUser( Ranks.BasicUser );
			RunScript( source.Split( new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries ), user );
		}

		/// <summary>Manages command parsing and applet execution.</summary>
		[STAThread]
		public void CommandProcessor()
		{
			Cmdlets = new CmdletCollection( CmdletCollection.GetAllCmdlets() );
			if (RevokedCmdlets.Length > 0)
				Cmdlets.Remove( RevokedCmdlets );

			Prompt.Write();
			ConsoleKeyManagement = new Thread( ManageInput );
			ConsoleKeyManagement.SetApartmentState( ApartmentState.STA );
			ConsoleKeyManagement.Start();

			while ( _keepAlive || CommandCache.Active )
			{
				if ( !(OnHeartbeat is null) && (this._hbInterval > 0) && (this._heartbeat.ElapsedMilliseconds > this._hbInterval) && !(Applets is null))
					OnHeartbeat( this, new CommandLineEventArgs( LocalUser, Applets ) );

				if (CommandCache.Active)
				{
					// Used to suppress automatic prompt display by the system.
					bool suppressPrompt;
					while (CommandCache.Active)
					{
						suppressPrompt = this.Env[ "echo" ].Value.Equals( "off", StringComparison.OrdinalIgnoreCase );
						Listener = false; // Tell the keyboard thread to stop listening
						Command cmd = CommandCache.NextWaiting;

						if ( this.Env[ "ECHO" ].Value.Equals( "on", StringComparison.OrdinalIgnoreCase ) )
						{
							if ( Console.CursorLeft > 0 ) Con.Tec( "{$1r}", Color );// Write("\r", Color);
							Prompt.Write( cmd.Cmd, CliColor.Normalize( ConsoleColor.White, Color.Back ) );
							Con.Tec( "{7,rn}$1", (cmd.Payload.Length > 0) ? " " + cmd.Payload : "" );
						}

						Applets.OperationalStates endState = ConsoleFunctions.Applets.OperationalStates.None;
						if ( cmd.Cmd.Length > 3 )
						{
							// Applet!
							if ( !(Applets is null) && Applets.HasCommand( cmd.Cmd ) )
							{
								// Causes a new instance of the specified command to be created.
								dynamic applet = Applets[ cmd.Cmd ]; 

								this.Listener = false; // <-- Stop the keyboard thread from listening for input.
								suppressPrompt = true;
								if ( this.Env.HasVariable( "EXCEPTIONS" ) && this.Env[ "EXCEPTIONS" ].Value.Equals( "ALLOW", StringComparison.OrdinalIgnoreCase ) )
								{
									endState = applet.Execute( cmd, cmd.AsRank );
									SetEnvVar( "ERRORLEVEL", "", false, "System" );
								}
								else
								{
									try { 
										endState = applet.Execute( cmd, cmd.AsRank );
										SetEnvVar( "ERRORLEVEL", "", false, "System" );
									}
									catch ( Exception e )
									{
										if ( e.GetType() == typeof( ThreadAbortException ) ) { _keepAlive = false; CommandCache.Clear(); }
										ConsoleFunctions.Applets.ExceptionDump( e, this.HasEnvVar("DEBUG") );
										SetEnvVar( "ERRORLEVEL", "1", false, "System" );
									}
								}

								if ( (endState == ConsoleFunctions.Applets.OperationalStates.CompleteWithErrors) || (endState == ConsoleFunctions.Applets.OperationalStates.Complete) )
									this.Pool.AddItem( applet );

								if ( (endState == ConsoleFunctions.Applets.OperationalStates.CompleteWithErrors) || (endState == ConsoleFunctions.Applets.OperationalStates.IncompleteWithErrors) )
								{
									Con.Tec( "{,3}&raquo;{F4,5rn}$1", Language.Prompt.Get( 0 ) ); // Errors occurred during execution.
									SetEnvVar( "ERRORLEVEL", "1", false, "System" );
								}

								this.Listener = true; // (Re)Start the keyboard thread listening for input.
								suppressPrompt = false;
							}
							else
							{
							if ( !(Aliases is null) && Aliases.HasAlias( (cmd.Cmd).Trim() ) )
								{
									// Alias Invoked!
									Con.Tec( "{,3}&raquo; {f}$4 {9}\"{6}$1 $3{9}\" {f}-► \"{E}$2 $3{F,rn}\"", 
										new object[] { cmd.Cmd, Aliases[ cmd.Cmd ].Value.Trim(), cmd.Payload.Trim(), Language.Prompt.Get( 1 ) }  
									);
									this.Enqueue( Aliases[ (cmd.Cmd).Trim() ].Value + " " + cmd.Payload.Trim(), LocalUser, false );
								}
								else
									if ( ((string)cmd.Cmd).Trim().Length > 0 ) Con.Tec( Language.Prompt.Get( 2, new object[] { cmd.Cmd } ) );
									// "{F4}Error:{9,8}\"{F}$1{9}\" {7,rn}is not a recognized instruction."
							}
						}
						else
						{
							// Cmdlet!
							if ( (cmd.Cmd.Length < 4) && Cmdlets.HasCmd( cmd.Cmd ) )
							{
								Listener = false; // <-- Don't process CLI keystrokes while executing commands!
								_keepAlive = !cmd.Cmd.Equals( EXIT_COMMAND, StringComparison.OrdinalIgnoreCase );
								if ( 
									this.Env.HasVariable( "EXCEPTIONS" ) && 
									this.Env[ "EXCEPTIONS" ].Value.Equals( "ALLOW", StringComparison.OrdinalIgnoreCase ) 
									)
									endState = Cmdlets[ cmd.Cmd ].Execute( cmd );
								else
									try { endState = Cmdlets[ cmd.Cmd ].Execute( cmd ); }
									catch ( Exception e ) 
									{
										this.SetEnvVar( "ERRORLEVEL", "1", true, "System" );
										if (e.GetType()==typeof(ThreadAbortException))
										{
											CommandCache.Clear();
											_keepAlive = false;
											Listener = false;
										}
										else
											ConsoleFunctions.Applets.ExceptionDump( e, this.HasEnvVar("DEBUG") ); 
									}

								// suppressPrompt = _commands[ cmd.Cmd ].SuppressPrompt;
								Listener = true;
							}
							else
								Con.Tec( Language.Prompt.Get(2, new object[] { cmd.Cmd } ) );
								// "{F4}Error:{9,8}\"{F}$1{9}\" {7,rn}is not a recognized instruction."
						}
					}

					Prompt.Write();
					Listener = true; // <-- Tell the keyboard thread to resume listening.
				}
				Pool.Prune(); // Clean up the shared memory pool...
				if (COMMAND_INTERVAL > 0)
					Thread.Sleep( COMMAND_INTERVAL );
			}
			//FormsWorkerStop();
			Con.Tec( "{,rn} {,3}&raquo;{7,5rn}$1", Language.Prompt.Get(3) );
			Console.TreatControlCAsInput = false;
			Console.ResetColor(); // <-- Reset the console to it's default colour scheme.
		}

		/// <summary>Provides a means to authenticate a user on the system.</summary>
		/// <param name="userName">The Username to authenticate.</param>
		/// <param name="password">The Password to use for the authentication.</param>
		/// <returns>TRUE if the supplied username and password match with information stored in the Registry,</returns>
		/// <remarks>If the userName is null or empty, this will have the result of logging off any authenticated user.</remarks>
		public bool LogOn( string userName = "", string password = "" )
		{
			bool result = Registry.Logon( userName, password );
			if ( !result && RequireAuthentication && _keepAlive) Enqueue( "USR /LOG:ON" );

			return result;
		}

		/// <summary>Starts up the main command processing function. Recommend running in it's own thread!</summary>
		/// <param name="requireAuthorization">If set to TRUE, user authentication is required before access to the CLI will be granted.</param>
		/// <param name="color">Defines what the default colour of text will be.</param>
		/// <param name="defaultUserRank">What Rank should an anonymous user be given.</param>
		/// <remarks>
		/// Unless specifically needed, leave the Prompt NULL at this point and define it afterward via the Prompt accessor.
		/// If there's not going to be any mechanism for managing access / elevating rank, defaultUserRank should be set to Ranks.SystemAdmin.
		/// </remarks>
		public void Activate( bool requireAuthorization, CliColor color = null, Ranks defaultUserRank = Ranks.None )
		{
			#region Taken from the original constructor!
			if ( color is null ) color = Con.DefaultColor;
			else
				Con.DefaultColor = color;

			this._requireAuthorization = requireAuthorization;
			this.DefaultUserRank = defaultUserRank;
			//this.LocalUser = UserInfo.DefaultUser( DefaultUserRank );

			this.Color = color;
			this._cmd = new CommandInput( new Prompt( "$app[name] > ", color, Prompt.DefaultPrimitives() ), "", color );
			#endregion

			// So we can capture CTRL-C keystrokes and interrupt applets!
			Console.TreatControlCAsInput = false;

			ProcessModule objCurrentModule = Process.GetCurrentProcess().MainModule;
			objKeyboardProcess = new LowLevelKeyboardProc( captureKey );
			ptrHook = SetWindowsHookEx( 13, objKeyboardProcess, GetModuleHandle( objCurrentModule.ModuleName ), 0 );

			this.Env.Set( "PROMPT", Prompt.RawPrompt, false, "System" );
			this.Env.Set( "ECHO", "on", false, "System" );
			this.Env.Set( "HOMEPATH", NetXpertExtensions.LocalExecutablePath, true, "System" );
			//this._environment.Set( "CWD", Environment.CurrentDirectory, false, "System" );
			CommandManagement = new Thread( CommandProcessor );

			Aliases.AddRange(
				new AliasDefinition[]
				{
						new AliasDefinition( "exit", "XIT" ),
						new AliasDefinition( "alias", "NAM")
				}
			);

			if ( LaunchArgs.Switches.HasSwitch( "debug" ) )
			{
				Con.Tec( "{A1}** {B1}$1 {A1}**{,rn} ", Language.Prompt.Get( 0 ) ); // DEBUG SWITCH ACTIVE
				if ( HasInternalScript( "AutoRun" ) )
					Aliases.AddRange(
						new AliasDefinition[] {
							new AliasDefinition( "autorun", "CMD /RUN:AutoRun" ),
							new AliasDefinition( "debug", "cmd /run:debug" ),
							new AliasDefinition( "exec", "cmd /run:"),
					} );
				Env.Set( "DEBUG", "ON", true, "System" );
			}

			Env.Set( "PATH", Environment.CurrentDirectory );

			// If an AutoRun script has been supplied, execute it upon initialization (unless debug mode is active):
			if ( HasInternalScript( "AutoRun" ) && !LaunchArgs.HasSwitch( "debug" ) )
			{
				Con.ClearLine( Color );
				// An auto-initialisation script has been provided, executing it now:
				Con.Tec( "{$1}&raquo; {F,rn}$2", new object[] { Color, Language.Prompt.Get( 1 ) } );
				string script = $"{Assembly.GetEntryAssembly().GetName().Name}.ExternalResources.AutoRun.script";
				script = NetXpertExtensions.FetchInternalResourceFile( script );
				RunScript( script, UserInfo.DefaultUser( Ranks.SuperUser ) );
				//Enqueue( "CMD /RUN:AutoRun", UserInfo.DefaultUser( Ranks.SystemAdmin ), false );
			}

			if ( this.RequireAuthentication )
			{
				if ( !Registry.HasUser( "root" ) )
					Registry.AddUser(
						new UserInfo( "root", "Default System Administrator", Ranks.SuperUser ) { Password = new Password( "password" ) }
					) ;

				//Con.Tec( Language.Prompt.Get( 2 ) );
				Enqueue( "USR /LOG:ON", UserInfo.DefaultUser( Ranks.Unverified ) );
			}

			Listener = true; // Activate the CLI input listener...
		}

		/// <summary>Pushes the supplied Command object into the cache.</summary>
		/// <param name="cmd">A Command object to push onto the cache.</param>
		public void Enqueue(Command cmd) => this.CommandCache.Add( cmd );

		/// <summary>Pushes the supplied Command object into the cache.</summary>
		/// <param name="cmd">A string containing the command to add.</param>
		/// <param name="allowCache">A boolean value indicating whether or not this command should be cached after execution.</param>
		public void Enqueue(string cmd, UserInfo user, bool allowCache = true) =>
			this.CommandCache.Add( cmd, user, allowCache );

		/// <summary>Facilitates sending multiple commands to the CLI in a single call.</summary>
		/// <param name="cmds">An array of Strings containing the commands to enqueue.</param>
		/// <param name="allowCache">A boolean value indicating whether or not these commands should be cached after execution.</param>
		public void Enqueue(string[] cmds, UserInfo user, bool allowCache = true)
		{
			foreach ( string cmd in cmds )
				Enqueue( cmd, user, allowCache );
		}

		/// <summary>Pushes the supplied Command object into the cache with the current user's rank.</summary>
		/// <param name="cmd">A string containing the command to add.</param>
		/// <param name="allowCache">A boolean value indicating whether or not this command should be cached after execution.</param>
		public void Enqueue( string cmd, bool allowCache = true ) =>
			Enqueue(cmd, LocalUser, allowCache);

		/// <summary>Facilitates sending multiple commands to the CLI in a single call with the current users' rank.</summary>
		/// <param name="cmds">An array of Strings containing the commands to enqueue.</param>
		/// <param name="allowCache">A boolean value indicating whether or not these commands should be cached after execution.</param>
		public void Enqueue( string[] cmd, bool allowCache = true ) =>
			Enqueue( cmd, LocalUser, allowCache );

		public void ClearScreen(CliColor color = null)
		{
			if (!(color is null))
				this.Color = color;

			Console.Clear();
		}
		#endregion

		#region External Console Keyboard and Window Control Functions
		[DllImport( "kernel32.dll", ExactSpelling = true )]
		private static extern IntPtr GetConsoleWindow();

		[DllImport( "user32.dll", CharSet = CharSet.Auto, SetLastError = true )]
		private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		[DllImport( "user32.dll", CharSet = CharSet.Auto, SetLastError = true )]
		private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, bool bRepaint);

		#region Code to selectively disable form usurpation keys: Windows-Key, Alt-Tab, and/or Ctrl-Esc
		/* Code to facilitate Disabling WinKey, Alt+Tab, and/or Ctrl+Esc Starts Here
		   From the example located at:
		   https://stackoverflow.com/questions/3213606/how-to-suppress-task-switch-keys-winkey-alt-tab-alt-esc-ctrl-esc-using-low/3214882
		*/

		// Structure to contain information about low-level keyboard input event 
		[StructLayout( LayoutKind.Sequential )]
		private struct KBDLLHOOKSTRUCT
		{
			public Keys key;
			public int scanCode;
			public int flags;
			public int time;
			public IntPtr extra;
		}

		// System level functions to be used for hook and unhook keyboard input:
		private delegate IntPtr LowLevelKeyboardProc( int nCode, IntPtr wParam, IntPtr lParam );
		[DllImport( "user32.dll", CharSet = CharSet.Auto, SetLastError = true )]
		private static extern IntPtr SetWindowsHookEx( int id, LowLevelKeyboardProc callback, IntPtr hMod, uint dwThreadId );
		[DllImport( "user32.dll", CharSet = CharSet.Auto, SetLastError = true )]
		private static extern bool UnhookWindowsHookEx( IntPtr hook );
		[DllImport( "user32.dll", CharSet = CharSet.Auto, SetLastError = true )]
		private static extern IntPtr CallNextHookEx( IntPtr hook, int nCode, IntPtr wp, IntPtr lp );
		[DllImport( "kernel32.dll", CharSet = CharSet.Auto, SetLastError = true )]
		private static extern IntPtr GetModuleHandle( string name );
		[DllImport( "user32.dll", CharSet = CharSet.Auto )]
		private static extern short GetAsyncKeyState( Keys key );

		// Declaring Global objects:
		private IntPtr ptrHook;
		private LowLevelKeyboardProc objKeyboardProcess;

		private IntPtr captureKey( int nCode, IntPtr wp, IntPtr lp )
		{
			if ( nCode >= 0 )
			{
				KBDLLHOOKSTRUCT objKeyInfo = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure( lp, typeof( KBDLLHOOKSTRUCT ) );

				// Disable Alt(Shift)-Tab...
				if ( SuspendSystemKeys.HasFlag( SuspendKeys.AltTab ) && (objKeyInfo.key == Keys.Tab && HasAltModifier( objKeyInfo.flags )) )
					return (IntPtr)1;

				// Disable Ctrl-Esc...
				if ( SuspendSystemKeys.HasFlag( SuspendKeys.CtrlEsc ) && (objKeyInfo.key == Keys.Escape && Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) )
					return (IntPtr)1;

				// Disable left and right Win keys..
				if ( SuspendSystemKeys.HasFlag( SuspendKeys.WinKey ) && (objKeyInfo.key == Keys.RWin || objKeyInfo.key == Keys.LWin) )
					return (IntPtr)1; // if 0 is returned then All the above keys will be enabled
			}

			return CallNextHookEx( ptrHook, nCode, wp, lp );
		}

		bool HasAltModifier( int flags ) => (flags & 0x20) == 0x20;

		/* Code to Disable WinKey, Alt+Tab, Ctrl+Esc Ends Here */
		#endregion

		#region Code to facilitate Console Window size/location like a regular Form
		[Serializable]
		[StructLayout( LayoutKind.Sequential )]
		private struct WindowPlacement
		{
			public int length;
			public int flags;
			public int showCmd;
			public Point ptMinPosition;
			public Point ptMaxPosition;
			public Rectangle rcNormalPosition;
		}

		[DllImport( "user32.dll", SetLastError = true )]
		[return: MarshalAs( UnmanagedType.Bool )]
		private static extern bool GetWindowPlacement(IntPtr hWnd, ref WindowPlacement windowData);

		private static Rectangle GetConsoleRectangle ()
		{
			WindowPlacement data = new WindowPlacement();
			GetWindowPlacement( GetConsoleWindow(), ref data );
			return data.rcNormalPosition;
		}

		private static bool MoveConsoleWindow(Point location, Size size) =>
			MoveWindow( GetConsoleWindow(), location.X, location.Y, size.Width, size.Height, true );

		private static bool SetConsoleWindowState(FormWindowState state = FormWindowState.Normal)
		{
			//private const int HIDE = 0;
			//private const int MAXIMIZE = 3;
			//private const int MINIMIZE = 6;
			//private const int RESTORE = 9;
			int newState;
			switch (state)
			{
				case FormWindowState.Maximized: newState = 3; break; // MAXIMIZE = 3
				case FormWindowState.Minimized: newState = 6; break; // MINIMIZE = 6
				case FormWindowState.Normal:
				default:
					newState = 9; break; // RESTORE  = 9
			}
			return ShowWindow( GetConsoleWindow(), newState );
		}

		private static FormWindowState GetConsoleWindowState()
		{
			WindowPlacement data = new WindowPlacement();
			GetWindowPlacement( GetConsoleWindow(), ref data );
			switch( data.showCmd )
			{
				case 1: return FormWindowState.Normal;
				case 2: return FormWindowState.Minimized;
				case 3: return FormWindowState.Maximized;
			}
			throw Language.Prompt.GetException( 0, new object[] { data.showCmd } );
			//new InvalidDataException( "The returned WindowState value was not recognized: " + data.showCmd );
		}

		public static bool HideConsoleWindow(bool hidden) =>
			ShowWindow( GetConsoleWindow(), hidden ? 0 : 5 );
		#endregion
		#endregion
	}
}
