using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using NetXpertExtensions;

namespace NetXpertCodeLibrary.ConsoleFunctions
{
	/// <summary>Simple static routines to facilitate writing text to the Console in color more efficiently.</summary>
	public static class Con
	{
		#region Accessors
		private static CliColor _defaultColor = new CliColor();

		public static int Cursor { get => Console.CursorLeft; set => Console.CursorLeft = value % Console.BufferWidth; }

		public static Encoding DefaultEncoding
		{
			get => Console.OutputEncoding;
			set => Console.OutputEncoding = value;
		}

		public static CliColor DefaultColor
		{
			get => Con._defaultColor;
			set { Con._defaultColor = value; _defaultColor.ToConsole(); }
		}

		public static bool ScrollLock => (((ushort)GetKeyState( 0x91 ) & 0xffff)) != 0;

		public static bool NumLock => (((ushort)GetKeyState( 0x90 ) & 0xffff)) != 0;

		public static bool CapsLock => (((ushort)GetKeyState( 0x14 ) & 0xffff)) != 0;
		#endregion

		#region Color Normalization Functions
		private static CliColor Color(CliColor value = null)
			=> new CliColor(value is null ? _defaultColor.Fore : value.Fore, value is null ? _defaultColor.Back : value.Back);

		private static CliColor Color(ConsoleColor? fore = null, ConsoleColor? back = null) =>
			CliColor.Normalize(fore, back);

		private static CliColor Color(ConsoleColor? fore, CliColor back) =>
			CliColor.Normalize(fore, Color(back).Back);
		#endregion

		#region Extended Functions - leverage the basic functions for added functionality.
		public static void ClearToEndOfLine( CliColor color = null, char with = ' ')
		{
			if ( !(color is null) ) color.ToConsole();
			int posn = Console.CursorLeft;
			Console.Write( "".PadRight( Console.BufferWidth - Console.CursorLeft, with ) + "\b" );
			Console.CursorLeft = posn;
		}

		/// <summary>Clears the current line and leaves the cursor at position 1.</summary>
		/// <param name="color">An optional CliColor object to specify what colour to clear the line with.</param>
		/// <param name="with">An optional character to fill the line with (default = ' ').</param>
		public static void ClearLine(CliColor color = null, char with = ' ')
		{
			if (!(color is null)) color.ToConsole();
			Console.Write( "\r".PadRight( Console.BufferWidth - 1, with ) + "\r");
		}

		/// <summary>Clears the current line and leaves the cursor at position 1.</summary>
		/// <param name="p">A prompt object to display.</param>
		/// <param name="data">Optional data to post after the Prompt.</param>
		/// <param name="color">An optional CliColor object to specify what colour to clear the line with (and write the Data in).</param>
		/// <param name="replaceCursor">If TRUE, places the cursor back where it found it, otherwise at EOL.</param>
		public static void ClearLine(Prompt prompt, string data = "", CliColor color = null, bool replaceCursor = true)
		{
			int curPos = Console.CursorLeft;
			ClearLine(color);
			prompt.Write(data, color);
			if (replaceCursor) Console.CursorLeft = curPos;
		}

		/// <summary>Pads a string to a fixed length with a succession of characters.</summary>
		/// <param name="source">The string to pad.</param>
		/// <param name="toLength">The number of characters to pad the string to.</param>
		/// <param name="with">Optional char value specifying what character to pad with (default is '.')</param>
		/// <param name="withBuffer">Optional value, if TRUE, the padding is enclosed in a leading and trailing space.</param>
		/// <returns>A string of the specified length, either padded according to the parameters specified, or truncated if too long.</returns>
		public static string PadString(string source, int toLength, char with = '.', bool withBuffer = true)
		{
			int padSize = toLength - source.Length - (withBuffer ? 2 : 0);
			if (padSize > 0)
			{
				string padding = with.ToString().PadLeft(padSize,with);
				if (withBuffer) padding = " " + padding + " ";
				return source + padding;
			}
			if (padSize < 0) return source.Substring(0, padSize);
			return source;
		}

		#region Wait / AreYouSure Prompts
		/// <summary>Implements a basic "Press Any Key" pause.</summary>
		/// <param name="withEsc">If set to TRUE, adds the text "(or [ESC] to quit)." to the prompt.</param>
		/// <param name="fore">The foreground colour to apply to the base text.</param>
		/// <param name="back">The background colour to use.</param>
		/// <param name="highlight">Optional highlight colour to apply to the "[ESC]" portion of the output.</param>
		/// <param name="fore">The foreground colour to use for the first and third strings (default = LightGray).</param>
		/// <param name="back">The background colour to use throughout (default = black).</param>
		/// <returns>A ConsoleKeyInfo value representing the key that was pressed to clear the wait.</returns>
		public static ConsoleKeyInfo WaitPrompt(int indent, bool withEsc = false, ConsoleColor highlight = ConsoleColor.White, ConsoleColor? fore = null, ConsoleColor? back = null)
			=> WaitPrompt(indent, withEsc, highlight, fore, back);

		/// <summary>Implements a basic "Press Any Key" pause.</summary>
		/// <param name="withEsc">If set to TRUE, adds the text "(or [ESC] to quit)." to the prompt.</param>
		/// <param name="fore">The foreground colour to apply to the base text.</param>
		/// <param name="back">The background colour to use.</param>
		/// <param name="highlight">Optional highlight colour to apply to the "[ESC]" portion of the output.</param>
		/// <param name="color">CliColor defining the base fore and background colors to use.</param>
		/// <returns>A ConsoleKeyInfo value representing the key that was pressed to clear the wait.</returns>
		public static ConsoleKeyInfo WaitPrompt(int indent = 0, bool withEsc = false, ConsoleColor highlight = ConsoleColor.White, CliColor color = null)
		{
			if (color is null) color = Con.DefaultColor;
			Tec( "{$1,,<$2}Press any key...", new object[] { color.ToHexPair(), indent } );
			//Write("Press any key...".PadLeft(indent, ' '), color);
			//if (withEsc) Highlight("(or ", "[ESC]", " to quit).", highlight, color);
			if (withEsc) Tec( "{$2}(or {$1}[ESC]{2}to quit).", new object[] { new CliColor(highlight).ToHexPair(), color.ToHexPair() } );
			ConsoleKeyInfo result = Console.ReadKey();

			// Using \r's, without a \n causes the Cursor to return to column 1, of the same line. This effectively clears
			// the prompt and leaves the cursor where it would have been if the prompt hadn't come up.
			Tec( "{$1,0r,>40} ", color.ToHexPair() );
			//Write("\r                                      \r", color);
			return result;
		}

		/// <summary>Implements a basic "Are you sure?" prompt.</summary>
		/// <param name="prompt">The text to display</param>
		/// <param name="opts">An array of two KeyValuePair<ConsoleKey,string> values that map onto True and False respectively.</ConsoleKey></param>
		/// <param name="enterResult">The value to infer if ENTER is pressed. (Default = true / yes)</param>
		/// <param name="escResult">The value to infer if ESCape is pressed. (Default = false / no)</param>
		/// <returns>TRUE if the user is sure, otherwise FALSE.</returns>
		/// <remarks>
		/// The `opts` value is used to map a keystroke (ConsoleKey) and a Word (string) onto the affirmative (index 0)
		/// and negative (index 1) responses.
		/// </remarks>
		public static bool AreYouSure( 
			string prompt = "Are you sure? $1 / $2 {F}: ", 
			KeyValuePair<ConsoleKey,string>[] opts = null,
			bool enterResult = true, 
			bool escResult = false )
		{
			if ( (opts is null) || (opts.Length < 2) ) 
				opts = new KeyValuePair<ConsoleKey, string>[]
				   {
					   new KeyValuePair<ConsoleKey, string>( ConsoleKey.Y, "Yes" ),
					   new KeyValuePair<ConsoleKey, string>( ConsoleKey.N, "No" ),
				   };
			opts[ 0 ] = new KeyValuePair<ConsoleKey, string>( opts[ 0 ].Key, opts[ 0 ].Value.Replace( $"{(char)opts[ 0 ].Key}", $"({(char)opts[ 0 ].Key})" ) );
			opts[ 1 ] = new KeyValuePair<ConsoleKey, string>( opts[ 1 ].Key, opts[ 1 ].Value.Replace( $"{(char)opts[ 1 ].Key}", $"({(char)opts[ 1 ].Key})" ) );
			prompt = prompt.Replace( new object[] { opts[ 0 ].Value, opts[ 1 ].Value } );
			Tec( prompt );
			bool? result = null;
			while ( result is null )
			{
				ConsoleKeyInfo response = Console.ReadKey(true);
				switch (response.Key)
				{
					case ConsoleKey.Escape:
						result = escResult;
						break;
					case ConsoleKey.Enter:
						result = enterResult;
						break;
					default:
						if ( response.Key == opts[ 0 ].Key ) result = true;
						else
						{
							if ( response.Key == opts[ 1 ].Key ) 
								result = false;
							else
								System.Media.SystemSounds.Beep.Play();
						}
						break;
				}
			}
			Console.CursorVisible = false;
			Tec( "{$1,rn}$2!", (bool)result ? new object[] { "B", opts[0].Value } : new object[] { "A", opts[1].Value } );
			Console.CursorVisible = true;
			return (bool)result;
		}
		#endregion

		#region WriteCheckBox
		/// <summary>
		/// Writes a checkbox prompt to the console window reflecting the state of the passed boolean value, as well as
		/// a subsequent string.
		/// </summary>
		/// <param name="type">If true, a checkmark is posted in the Highlight1 style, otherwise and X is written in the Alert style.</param>
		/// <param name="post">The text to write after the checkbox.</param>
		/// <param name="bright">A ConsoleColor specifying the highlight colour.</param>
		/// <param name="def">An optional CliColor object specifying the default fore and background colours.</param>
		public static void WriteCheckbox(bool type, string post, ConsoleColor bright, CliColor def = null, char[] opts = null) =>
			Tec( TextElement.CheckBox( type, bright, def, opts ) + " " + post );

		/// <summary>Writes a checkbox prompt to the console window reflecting the state of the passed boolean value.</summary>
		/// <param name="type">If true, a checkmark is posted in the Highlight1 style, otherwise and X is written in the Alert style.</param>
		/// <param name="bright">A ConsoleColor specifying the highlight colour.</param>
		/// <param name="def">An optional CliColor object specifying the default fore and background colours.</param>
		public static void WriteCheckbox(bool type, ConsoleColor bright, CliColor def = null, char[] opts = null) =>
			WriteCheckbox(type, "", bright, def, opts);

		/// <summary>Writes a checkbox prompt to the console window and adds a newline break.</summary>
		/// <param name="type">If true, a checkmark is posted in the Highlight1 style, otherwise and X is written in the Alert style.</param>
		public static void WriteCheckboxLn(bool type, ConsoleColor bright, CliColor def = null, char[] opts = null) =>
			TextElement.CheckBoxLn( type, bright, def, opts ).Write();

		/// <summary>
		/// Writes a checkbox prompt to the console window reflecting the state of the passed boolean value, as well as
		/// a subsequent string.
		/// </summary>
		/// <param name="type">If true, a checkmark is posted in the Highlight1 style, otherwise and X is written in the Alert style.</param>
		/// <param name="def">An optional CliColor object specifying the default fore and background colours.</param>
		public static void WriteCheckBox(bool type, CliColor def = null, char[] opts = null) =>
			WriteCheckbox( type, "", type ? ConsoleColor.Green : ConsoleColor.Red, def, opts );

		/// <summary>Writes a checkbox prompt to the console window and adds a newline break.</summary>
		/// <param name="type">If true, a checkmark is posted in the Highlight1 style, otherwise and X is written in the Alert style.</param>
		/// <param name="def">An optional CliColor object specifying the default fore and background colours.</param>
		public static void WriteCheckboxLn(bool type, CliColor def = null, char[] opts = null) =>
			WriteCheckbox( type, "{rn}", type ? ConsoleColor.Green : ConsoleColor.Red, def, opts );
		#endregion

		#region WriteMsg / WriteMsgLn
		/// <summary>
		/// Writes a variably defined output line to the console in the form of:
		///   • Some text [some other text] and some more text.
		/// </summary>
		/// <param name="data">An array of between 1 and 3 strings to display.</param>
		/// <param name="accent">A console colour designation for the highlighted text.</param>
		/// <param name="color">A CliColor object specifying the highlight colour to use.</param>
		/// <param name="useCRLF">If true, appends a newline break to the output.</param>
		private static string WriteMsg(string[] data, ConsoleColor Accent, CliColor color = null, bool useCRLF = false)
		{
			if ((data.Length == 0) || (String.Join("", data) == "")) return "";

			CliColor accent = new CliColor( Accent );
			string result = "{z,2}&bull; ";
			//ClearLine();
			//string pre = "  • ";
			switch (data.Length)
			{
				case 1: result += new TextElementCollection( "{$1}[{$2}$3{1}]", new object[] { color, accent, data[ 0 ] } ).ToString(); 
					//Highlight(pre + "[", data[0], "]", accent, color); 
					break;
				case 2:
					if ((data[ 1 ].Length == 0) || data[ 1 ] == string.Empty)
						result += new TextElementCollection( "{$1}$2", new object[] { color, data[ 0 ] } ).ToString();
					//Write(pre, data[0], color);
					else
						result += new TextElementCollection( "{$4}$1 [{$2}{3}{$4}]", new object[] { data[ 0 ], accent, data[ 1 ], color } );
						//Highlight(pre + data[0] + "[", data[1], "]", accent, color);
					break;
				default:
					result += new TextElementCollection( "{$5}$1 [{$4}{$2}{$5}] $3", new object[] { data[0], data[1], data[2], accent, color } );
					//Highlight(pre + data[0] + "[", data[1], "] " + data[2], accent, color); 
					break;
			}
			if (useCRLF) result += "{rn} "; // Console.WriteLine();
			return result;
		}

		/// <summary>
		/// Writes an output line to the console in the form: "• Some text here [and more text here]" with the outer text
		/// in the Default style and the inner text in the Highlight1 style.</summary>
		/// <param name="pre">The text to place between the bullet and the brackets.</param>
		/// <param name="msg">The text to place inside the brackets.</param>
		/// <param name="accent">A console colour designation for the highlighted text.</param>
		/// <param name="color">A CliColor object specifying the highlight colour to use.</param>
		public static string WriteMsg(string pre, string msg, ConsoleColor accent, CliColor color = null) =>
			WriteMsg(new string[] { pre, msg }, accent, color, false);

		/// <summary>
		/// Writes an output line to the console in the form: "• Some text here [and more text here] and more text here."
		/// with the outer strings in the Default style and the inner text in the Highlight1 style.</summary>
		/// <param name="pre">The text to place between the bullet and the brackets.</param>
		/// <param name="msg">The text to place inside the brackets.</param>
		/// <param name="post">The text to place after the brackets.</param>
		/// <param name="accent">A console colour designation for the highlighted text.</param>
		/// <param name="color">A CliColor object specifying the highlight colour to use.</param>
		public static string WriteMsg(string pre, string msg, string post, ConsoleColor accent, CliColor color = null) =>
			WriteMsg(new string[] { pre, msg, post }, accent, color, false);

		/// <summary>
		/// Writes an output line to the console with a newline break, in the form:
		/// "• [some text here]" with the text in the specified style.
		/// </summary>
		/// <param name="msg">The text to place inside the brackets.</param>
		/// <param name="accent">A console colour designation for the highlighted text.</param>
		/// <param name="color">A CliColor object specifying the highlight colour to use.</param>
		public static string WriteMsgLn(string msg, ConsoleColor accent, CliColor color = null) =>
			WriteMsg(new string[] { msg }, accent, color, true);

		/// <summary>
		/// Writes an output line to the console with a newline break in the form:
		/// "• Some text here [and more text here]" with the outer text in the Default style and the inner
		/// text in the Highlight1 style.
		/// </summary>
		/// <param name="pre">The text to place between the bullet and the brackets.</param>
		/// <param name="msg">The text to place inside the brackets.</param>
		/// <param name="accent">A console colour designation for the highlighted text.</param>
		/// <param name="color">A CliColor object specifying the highlight colour to use.</param>
		public static string WriteMsgLn(string pre, string msg, ConsoleColor accent, CliColor color = null) =>
			WriteMsg(new string[] { pre, msg }, accent, color, true);

		/// <summary>
		/// Writes an output line and newline break to the console in the form:
		/// "• Some text here [and more text here] and more text here." with the outer strings in the Default
		/// style and the inner text in the Highlight1 style.
		/// </summary>
		/// <param name="pre">The text to place between the bullet and the brackets.</param>
		/// <param name="msg">The text to place inside the brackets.</param>
		/// <param name="post">The text to place after the brackets.</param>
		/// <param name="accent">A console colour designation for the highlighted text.</param>
		/// <param name="color">A CliColor object specifying the highlight colour to use.</param>
		public static string WriteMsgLn(string pre, string msg, string post, ConsoleColor accent, CliColor color = null) =>
			WriteMsg(new string[] { pre, msg, post }, accent, color, true);
		#endregion

		#endregion

		#region ReadPasswordLine
		/// <summary>Manages text input for password fields. (like a password-secure "Console.ReadLine").</summary>
		/// <param name="source">If we have a string to put into the field, specify it here.</param>
		/// <param name="minLength">The minimum width of the field to construct (can't be less than 10 chars!)</param>
		/// <param name="maxLength">The maxiume length of the password to accept.</param>
		/// <param name="size">The size of the input area of the field.</param>
		/// <param name="outer">A CliColor object to specify the color of the non-data-entry area portions of the prompt.</param>
		/// <param name="inner">A CliColor object to specify the colour of the data-entry portion of the prompt.</param>
		/// <param name="empty">A char value to display when a character hasn't been entered.</param>
		/// <param name="full">A char value to display when a character has been entered. Using "(char)0x00" for this will turn off character obfuscation.</param>
		/// <param name="allowCancel">If set to TRUE, this allows CTRL-C to cancel the entry.</param>
		/// <returns>The contents of the input field, as a string.</returns>
		/// <exception cref="OperationCanceledException">Thrown if CTRL-C is pressed and "allowCancel" was set to TRUE.</exception>
		/// <remarks>
		/// If "allowCancel" is set to TRUE, it enables using CTRL-C to cancel entry. This is effected by throwing an
		/// OperationCancelledException, so you'll have to use a TRY..CATCH if you want to use this feature. If this
		/// happens, the thrown exception will come with whatever the contents of the field were when it was cancelled,
		/// saved as the message portion of a generic Inner Exception.
		/// </remarks>
		public static string InputTemplate(CliColor outer = null, CliColor inner = null, string source = "", byte minLength = 10, byte maxLength = 64, byte size = 32, char empty = '.', char full = '.', bool allowCancel = true)
		{
			if ( full == (char)0x00 ) full = empty;
			minLength = Math.Max(Math.Min(minLength, (byte)(Console.BufferWidth - 5)), (byte)10);
			size = Math.Min(maxLength,Math.Max(Math.Min((byte)(Console.BufferWidth - 5), size), minLength));
			int x = Console.CursorLeft, y = Console.CursorTop, posn = source.Length, horizon = 0;
			if (x + size > Console.BufferWidth) { x = 0; y += 1; }
			outer = (outer is null) ? CliColor.CaptureConsole() : outer;
			inner = (inner is null) ? new CliColor(outer.Back, outer.Fore) : inner;
			bool cursorState = Console.CursorVisible;
			Console.CursorVisible = true;

			void Write( string what, CliColor color = null)
			{
				if (color is null) color = Con.DefaultColor;
				Console.ForegroundColor = color.Fore;
				Console.BackgroundColor = color.Back;
				Console.Write( what );
			}

			Write("[", outer);
			Write( (full==empty) ? source : "".PadRight(source.Length, full),inner );
			Write("".PadRight(size - source.Length, empty), outer);
			Write("]", outer);
			Console.CursorLeft = x + posn + 1;

			bool KeepGoing = true;
			while (KeepGoing)
			{
				while (Console.KeyAvailable)
				{
					ConsoleKeyInfo key = Console.ReadKey(true); // intercept the keypress.
					switch(key.Key)
					{
						// Intercept CTRL-X, CTRL-C, CTRL-V (Cut, Copy, Paste to/from clipboard).
						case ConsoleKey.C:
							if (key.Modifiers.HasFlag( ConsoleModifiers.Control ))
							{
								if ( allowCancel )
									throw new OperationCanceledException( "Input Operation Cancelled", new Exception( source ) );

								System.Media.SystemSounds.Beep.Play();
								break;
							}
							goto default ;
						case ConsoleKey.X:
						case ConsoleKey.V:
							if (key.Modifiers.HasFlag( ConsoleModifiers.Control ))
							{
								System.Media.SystemSounds.Beep.Play();
								break;
							}
							goto default;
						case ConsoleKey.Escape: // Clear line or cancel.
							source = ""; KeepGoing = !allowCancel;
							break;
						case ConsoleKey.Enter: // Enter
							KeepGoing = false;
							break;
						case ConsoleKey.Backspace: // Delete character left...
							if (posn > 0)
							{
								posn = source.Length - 1;
								source = source.Substring(0,posn);
							}
							break;
						default:
							if (
								!key.Modifiers.HasFlag(ConsoleModifiers.Alt) &&
								!key.Modifiers.HasFlag(ConsoleModifiers.Control) &&
								Regex.IsMatch( key.KeyChar.ToString(), @"[ \w{}!@#$%^&*()+=\-`~\[\]{}\|,.\/<>?;:'""]", RegexOptions.Compiled) 
								)
							{
								source += key.KeyChar;
								posn = source.Length;
							}
							break;
					}

					Console.CursorLeft = x;
					Write("[", outer);
					Write(((empty==full) ? source : "".PadRight(Math.Min(size,source.Length), full)), inner);
					Write("".PadRight(Math.Max(0,size - source.Length), empty), outer);
					Write("] ", outer);
					Console.CursorLeft = x + posn + 1;
				}
				//System.Threading.Thread.Sleep(25); // gives the system time to do something else!
			}
			Console.CursorLeft = x;
			if ( full == empty )
				Tec("{$2,rnz}$1", new object[] { source, outer } );
			else
				Tec("{z}({F}$1 {7}characters received{,rn})", source.Length );
			Console.CursorVisible = cursorState;
			return source;
		}
		#endregion

		#region ReadKey Alternative
		/// <summary>Gets a keystroke.</summary>
		/// <param name="AllowedKeys">An array of ConsoleKey object specifying what keys are allowed.</param>
		/// <param name="escapeTranslation">What key will be processed as the Escape key. Defaults to ConsoleKey.Escape</param>
		/// <returns>A ConsoleKey object representing a valid keypress.</returns>
		public static ConsoleKey ReadKey( ConsoleKey[] AllowedKeys, ConsoleKey escapeTranslation = ConsoleKey.Escape )
		{
			bool cursorState = Console.CursorVisible; Console.CursorVisible = true;
			int cursorSize = Console.CursorSize; Console.CursorSize = 100;
			List<ConsoleKey> allowed = new List<ConsoleKey>( AllowedKeys );
			if ( escapeTranslation != ConsoleKey.Escape )
			{
				allowed.Add( ConsoleKey.Escape );
				if ( allowed.Contains( escapeTranslation ) )
					allowed.Remove( escapeTranslation );
			}

			ConsoleKeyInfo vKey;
			do { vKey = Console.ReadKey(true); } while ( !allowed.Contains( vKey.Key ) );

			Console.CursorSize = cursorSize;
			Console.CursorVisible = cursorState;

			return (vKey.Key == ConsoleKey.Escape) ? escapeTranslation : vKey.Key;
		}
		#endregion

		#region TextElement
		public static void Tec(string data) =>
			TextElementCollection.Write( data, Con.DefaultColor );

		public static void Tec(string template, object data) =>
			TextElementCollection.Write( template, new object[] { data }, Con.DefaultColor );

		public static void Tec(string template, object[] data ) =>
			TextElementCollection.Write( template, data, Con.DefaultColor );
		#endregion

		#region Drawing
		[Flags]
		public enum BoxType { 
			None = 0,
			SingleLeft = 1,
			SingleTop = 2,
			SingleRight = 4,
			SingleBottom = 8,
			DoubleLeft = 16,
			DoubleTop = 32,
			DoubleRight = 64,
			DoubleBottom = 128,
			SingleAll = 15,
			DoubleAll = 240,
			DropDown = 195
		}
		public static void DrawBox( Rectangle rect, CliColor border, CliColor fill, BoxType boxType )
		{
			// leftside, topside, rightside, bottom, topleft, topright, bottomleft, bottomright
			// ║╖│─┌║╖│─┌┐═╒╓╔╕╖╗╘╙╚╛╜╝
			char[] borderElements = new char[] { '║', '═', '║', '═', '╔', '╗', '╚', '╝' };
			if ( boxType.HasFlag( BoxType.SingleLeft ) )
				borderElements[ 0 ] = '│';
			if (boxType.HasFlag( BoxType.SingleRight ) )
				borderElements[ 2 ] = '│';
			if ( boxType.HasFlag( BoxType.SingleTop ) )
				borderElements[ 1 ] = '─';
			if ( boxType.HasFlag( BoxType.SingleBottom ) )
				borderElements[ 3 ] = '─';



		}
		#endregion

		[DllImport( "user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi )]
		private static extern short GetKeyState( int keyCode );

	}

	public class ConsoleListLine
	{
		protected string[] _text;

		#region Constructors
		public ConsoleListLine(string baseData, string connector) =>
			this._text = new string[] { baseData, connector };

		public ConsoleListLine(string baseData, string supplement, string connector) =>
			this._text = new string[] { baseData, supplement, connector };

		public ConsoleListLine(string[] text) => this._text = text;

		public ConsoleListLine(dynamic[] sources)
		{
			List<string> text = new List<string>();
			foreach (dynamic obj in sources)
				text.Add(obj.ToString());
			this._text = text.ToArray();
		}
		#endregion

		public string[] Text
		{
			get => this._text;
			set => this._text = value;
		}
	}

	public class ConsoleList
	{
		#region Properties
		protected string _header = "";
		protected List<ConsoleListLine> _lines = new List<ConsoleListLine>();
		protected CliColor _color;
		protected int _width = 60;
		protected char _padChar = '.';
		protected CliColor _default;
		protected CliColor _styleGreen;
		protected CliColor _styleWhite;
		protected CliColor _styleYellow;
		protected CliColor _styleCyan;
		protected CliColor _styleInverse;
		#endregion

		#region Constructors
		public ConsoleList(int width, char padChar, CliColor baseColor, CliColor defaultColor) =>
			this.Initialise(width, padChar, baseColor, defaultColor);

		public ConsoleList(CliColor baseColor, CliColor defaultColor) =>
			this.Initialise(60, '.', baseColor, defaultColor);

		public ConsoleList(int width, CliColor baseColor, CliColor defaultColor) =>
			this.Initialise(width, '.', baseColor, defaultColor);

		private void Initialise(int width, char padChar, CliColor baseColor, CliColor defaultColor)
		{
			this._width = width;
			this._padChar = padChar;
			this._color = baseColor;
			this._default = defaultColor;
			this._styleGreen = defaultColor.Alt(ConsoleColor.Green);
			this._styleWhite = defaultColor.Alt(ConsoleColor.White);
			this._styleYellow = defaultColor.Alt(ConsoleColor.Yellow);
			this._styleCyan = defaultColor.Alt(ConsoleColor.Cyan);
			this._styleInverse = new CliColor(ConsoleColor.DarkBlue, ConsoleColor.DarkCyan);
		}
		#endregion

		#region Accessors
		public string Header
		{
			get => this._header;
			set => this._header = value;
		}

		public char PadChar
		{
			get => this._padChar;
			set => this._padChar = value;
		}

		public int Width
		{
			get => this._width;
			set => this._width = Math.Max(30, value);
		}

		public CliColor TemplateStyle
		{
			get => this._color;
			set => this._color = value;
		}
		#endregion

		#region Methods
		public void AddLines(ConsoleListLine[] newLines)
		{
			foreach (ConsoleListLine cll in newLines)
				this.AddLine(cll);
		}

		public void AddLine(ConsoleListLine newLine) => this._lines.Add(newLine);

		public void AddLine(string baseData, string connector) => this._lines.Add(new ConsoleListLine(baseData, connector));

		public void AddLine(string baseData, string supplement, string connector) =>
			this._lines.Add(new ConsoleListLine(baseData, supplement, connector));

		public void AddLine(string[] text) => this._lines.Add(new ConsoleListLine(text));

		public void Display(bool showLineNos, bool withMore) =>
			this.Display(this._lines.ToArray(), withMore, showLineNos);

		public void Display(ConsoleListLine[] lines, bool showLineNos, bool withMore)
		{
			int i = 1, leftPad = showLineNos ? 6 : 1;
			if (this._header.Trim().Length > 0)
				Con.Tec( "{$1,rn,<$2}$3", new object[] { _styleWhite, leftPad, _header } );

			Con.Tec( "{$1,rn,>$2'='}=", new object[] { _default, this._width + (leftPad - 1) } );
			foreach (ConsoleListLine line in lines)
			{
				if ((i % 500 == 0) || (withMore && (i % Console.WindowHeight==0)))
				{
					Con.Tec( "{$1}[{B}Paused{$1}] {$2}Press any key to continue, or {$1}[{E}ESC{$1}] {2}to cancel...",
						new object[] { _styleWhite, _default }
					);
					//Con.Highlight("[", "Paused", "]", ConsoleColor.Cyan, this._styleWhite);
					//Con.Write(" Press any key to continue, or ", this._default);
					//Con.Highlight("[", "ESC", "]", ConsoleColor.Yellow, this._styleWhite);
					//Con.Write(" to cancel...", this._default);

					while (!Console.KeyAvailable) { System.Threading.Thread.Sleep(100); }
					ConsoleKeyInfo key = Console.ReadKey();

					if ((key.Key == ConsoleKey.Escape) && ((int)key.Modifiers & 7) == 0)
					{
						Con.Tec( "{$1,0c}  &raquo; Display {$2,rn}Cancelled!",
							new object[] { _default, _styleYellow } );
						//Con.ClearLine(_default);
						//Con.WriteLn("\r  » Display ", "Cancelled!", this._default, this._styleYellow);
						break;
					}
					Con.Tec( "{$1,0c} ", _default );
					//Con.ClearLine(_default);
				}

				if (showLineNos)
					Con.Tec( "{$1,,<4}$2{$3}] ", new object[] { _styleGreen, i++, _styleWhite } );
				else
					Con.Tec( "{$1,,<$2} ", new object[] { _default, leftPad } );
				///Con.Write("".PadLeft(leftPad, ' '), _default);

				Con.Tec( "{$1,rn}$2", new object[] { CliColor.CaptureConsole(), line.Text } );
				//this.WriteLn(line.Text);
			}
		}

		protected void WriteItem(string item) =>
			WriteItem(item, _default);

		protected void WriteItem(string item, CliColor bracketStyle)
		{
			CliColor def = _default;
			if (item.Length > 0)
			{
				//Con.Write(" ", def);
				Con.Tec( "{$1} ", def );
				if (" [{<«(".IndexOf( item[ 0 ] ) > 0)
				{
					char[] brackets = new char[ 2 ] { '|', '|' };
					switch (item.Substring( 0, 1 ))
					{
						case "[": brackets = new char[] { '[', ']' }; break;
						case "{": brackets = new char[] { '{', '}' }; break;
						case "(": brackets = new char[] { '(', ')' }; break;
						case "<": brackets = new char[] { '<', '>' }; break;
						case "«": brackets = new char[] { '«', '»' }; break;
					}
					string txt = item.Trim( brackets );

					//CliColor temp = _styleWhite;
					CliColor temp = _styleWhite;
					if ((item == "[X]") || (item == "[√]"))
						temp = (txt == "X") || (txt == "x") ?
							_default.Alt( ConsoleColor.Red )
							:
							_styleGreen;

					Con.Tec( "{$1}$2{$3}$4{$1}$5", new object[] {
						bracketStyle,
						(brackets[0] == '{' ? "&lbrace;" : brackets[0].ToString()),
						temp,
						txt,
						(brackets[1] == '}' ? "&rbrace;" : brackets[1].ToString()),
						}
					);
					//Con.Highlight(brackets[0].ToString(), txt.DecodeHtmlEntities(), brackets[1].ToString(), bracketStyle, temp);
				}
				else
					Con.Tec( "{$2}$1", new object[] { item, bracketStyle } );
					//Con.Write(item.DecodeHtmlEntities(), bracketStyle);
			}
		}

		protected void WriteLn(string[] text)
		{
			int padCount = this._width - text.Length - 1;
			foreach (string s in text)
				padCount -= s.DecodeHtmlEntities().Length; // - (System.Text.RegularExpressions.Regex.Matches(s, "&amp;").Count * 4); // (" <{[(<«".IndexOf(s[0]) > 0) ? s.Length + 1 :

			int index = 1;
			if (text.Length > 1)
			{
				CliColor def = this._default;
				Con.Tec( "{$1}[{$2}$3{$1}]", new object[] { def, text[ 0 ], _styleYellow } );
				//Con.Highlight("[", text[0].DecodeHtmlEntities(), "]", def, _styleYellow);
				if (text.Length > 2)
				{
					this.WriteItem( text[ 1 ], _styleCyan );
					index = 2;
				}

				Con.Tec( "{$1,,>$2'$3'} ", new object[] { def, Math.Max( 1, padCount - 2 ), _padChar } );
				//Con.Write(" ".PadRight(Math.Max(1, padCount - 2), _padChar), def);

				for (int i = index; i < text.Length; i++)
					WriteItem( text[ i ], _styleCyan );
			}
			else
				Con.Tec( "{$1}$2", new object[] { _styleWhite, text[ 0 ] } );
				//Con.Write(text[0], _styleWhite);

			Con.Tec( "{$1,rn} ", _default );
			//Con.WriteLn("",_default);
		}
		#endregion
	}
}
