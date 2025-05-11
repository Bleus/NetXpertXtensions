using NetXpertCodeLibrary.Extensions;
using NetXpertExtensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;

namespace NetXpertCodeLibrary.ConsoleFunctions
{
	public class TextElementCmd
	{
		/* TextElementCmd Supported Syntax:    ┌─┤ OPTIONAL! ├──────────────────────────────────────────────────────────────────────────────────────────────────┐
  		 *                                     │  A HexPair Color Designation. This does not need to be specified: if it is omitted the default color is used.  │
  		 *   ┌─────────────────────────────────┤  Also, the second character may be omitted, in which case the default background color is substituted.         │
		 *   │                                 └────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
		 *   │     ┌───────────────┤ Horizontal ABSOLUTE Cursor Position to place text at prior to printing. If omitted, the current cursor position is used. Valid Range = 0 to Console.BufferWidth │
		 *   │     │                                                                           ┌───────────────────────────────────────────────────────────────┐
		 *   │     │     ┌─────────────────────────────────────────────────────────────────────┤ The Pad Style to apply:     If the style is specified, a size │
		 *   │     │     │               ┌───────────────────────────────────────────┐         │   "<" means "Pad Left"      value must be present too or the  │ ───────┐
		 *   │     │     │               │  Char value to pad with (not a string!)   │         │   "|" means "Center"        cmd pattern won't be recognized!  │        │
 		 *  ┌┤    ┌┼┐    │   ┌───────────┤  defaults to space if not specified!      │◄────┐   │   ">" means "Pad Right"   ┌───────────────────────────────────┘        │
 		 *  ││    │││    │  ┌┼┐          │  must be enclosed in single-quotes!       │     │   └───────────────────────────┘     ┌─┤ OPTIONAL! ├────────────────────────┴─────────────────┐
		 *  ▼▼    ▼▼▼   ▼  ▼▼▼           └───────────────────────────────────────────┘     └─────────────────────────────────────┤   As with the other sections, the third one is also    │
		 * {A9rzn,>13rzn,<20'-'} ───┐                                                                                            │   entirely optional and may be excluded. The pad Char  │
		 *    ▲▲▲    ▲▲▲  ▲▲        └─────┤ Must be enclosed in brace brackets, and cannot have any spaces! │                    │   defaults to a space if one isn't specified!          │
		 *    │││    │││  ││                                                                                                     └──────────────────────────────────────┬─────────────────┘
		 *    └┼┘    └┼┘  └┴───────────────┤ Width to pad the controlled string out to. 3-digit max! Asterisk = screen-width │◄─────────────────────────────────────────┘                   
		 *     │      │                 ┌─┤ OPTIONAL! ├───────────────────────────────────────────────────────────────────────────────────────┐
		 *     │      │                 │   Flags:                      These are binary switches whose case and order don't matter, but are  │
		 *     │      │                 │   r = add a Carriage Return   only relevant once each per section. Flags that are set in the first  │
		 *     └──────┴─────────────────┤   n = add a New Line          section are processed BEFORE anything is printed.  Flags that are set │
		 *                              │   z = clear the line          in the second section are processed AFTER printing is completed.      │
		 *                              └─────────────────────────────────────────────────────────────────────────────────────────────────────┘
		 *                           
		 * TextElementCmds can be as little as two brace brackets: {} (which will simply use the default colour, at the current position, with no padding and no CR or LF suffixes).
		 * Furthermore, if the first and/or second parameters are going to be omitted, commas must still be present for the string to be recognized. ( i.e. "{,,>20}" is a valid
		 * command to pad the controlled text, on the right, to 20 characters, but {>20} and {,>20) are not valid and won't be processed. Controlled strings are parsed for HTML entities 
		 * prior to display. Also, if brace brackets are required in the controlled strings, the pseudo-entities "&lbrace;" and "&rbrace;" can be used.
		 * 
		 * The two static parameterized Write functions of TextElementCollection permit using markers in the strings that are replaced by values provided in the attached object arrays.
		 * Such markers are delimited by a dollar-sign followed by the matching index of the supplied value. Markers may appear multiple times; Markers whose index exceeds the supplied
		 * data will be unprocessed, as will data in indexes that aren't referenced. If you need to use a $ sign in the control text, escape it with a preceding backslash.
		 * 
		 *  ▲ ▼ ◄ │ ├ ┌ ┬ ┐ ┴ ─ └ ┼ ┤ ┘
		 */

		#region Properties
		[Flags] public enum Options : ushort { 
			None			= 0x0000, 
			AddCrBefore		= 0x0001, 
			AddCrAfter		= 0x0002,  
			AddNlBefore		= 0x0004, 
			AddNlAfter		= 0x0008, 
			ClearLnBefore	= 0x0010, 
			ClearLnAfter	= 0x0020,
			RightJustify    = 0x0040,
			AddCrLfBefore	= 0x0005,
			AddCrLfAfter    = 0x000A,
			All				= 0xFFFF 
		};
		public enum PadStyles { None, Left, Center, Right };
		protected Options _myOptions = Options.None;
		protected string _colorCode = Con.DefaultColor.ToHexPair();
		protected int _curPos = -1; // -1 means leave it where it is.
		public static readonly string CMD_REGEX_PATTERN = 
				Regex.Replace( /* language=regex */ @"{
									(?<color>[a-f*0-9]{1,2})?
									(?<adds1>zrn|rzn|rnz|rz|zr|nz|zn|rn|nr|r|n|z)?
									(?<tail>
										(?:[, ;]
											(?<justify>[<>])?
											(?<posn>[0-9]{1,3})?
											(?<adds2>zrn|rzn|rnz|rz|zr|nz|zn|rn|nr|r|n|z)?
											(?:[, ;]
												(?:
													(?<padspec>[<|>](?:[0-9]{1,3}|[*]))
													(?<padchar>'[\S \t]')?
												)?
											)?
										)
									)?
								 }", @"(?:^[\s]*|[\s]*$)","", RegexOptions.Multiline | RegexOptions.ExplicitCapture ).Replace("\r","").Replace("\n","");
		#endregion

		#region Constructors
		public TextElementCmd(CliColor color = null, int curPos = -1, Options options = Options.None, PadStyles pad = PadStyles.None, byte padSize = 0, char padWith = ' ' )
		{
			this._colorCode = (color is null) ? "**" : color.ToHexPair();
			this.CurPos    = curPos;
			this.PadStyle  = pad;
			this.PadSize   = padSize;
			this.PadChar   = padWith;
			this.MyOptions = options;
		}

		public TextElementCmd(string source) =>
			this.Parse( source );
		#endregion

		#region Accessors
		public int CurPos
		{
			get => this._curPos;
			set => this._curPos = (value < 0) ? -1 : Math.Min( value, Console.BufferWidth );
		}

		public CliColor Color
		{
			get => new CliColor( this._colorCode );
			set => this._colorCode = value.ToHexPair();
		}

		public Options MyOptions
		{
			get => this._myOptions;
			set => this._myOptions = value;
		}

		public CliColor DefaultColor { get; set; } = Con.DefaultColor;

		public PadStyles PadStyle { get; set; } = PadStyles.None;

		public byte PadSize { get; set; } = 0;

		public char PadChar { get; set; } = ' ';

		public static Regex Pattern =>
			new Regex( @"^" +  CMD_REGEX_PATTERN + "$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace );
		#endregion

		#region Methods
		public void SetFlag( Options option, bool value)
		{
			if (value) 
				MyOptions |= option; 
			else 
				MyOptions &= (0xffff - option);
		}

		public void Parse(string source)
		{
			if (source.Equals( "{}" )) source = "{**}";			
			if (Validate(source))
			{
				MatchCollection matches = Pattern.Matches( source );
				if (matches.Count > 0)
				{
					GroupCollection g = matches[ 0 ].Groups;
					this._colorCode = g[ "color" ].Success ? g[ "color" ].Value : "**";
					this.CurPos = g[ "posn" ].Success ? byte.Parse(g[ "posn" ].Value) : -1;

					if (g[ "adds1" ].Success)
					{						
						SetFlag( Options.AddCrBefore, g[ "adds1" ].Value.ToLowerInvariant().IndexOf( "r" ) >= 0 );
						SetFlag( Options.AddNlBefore, g[ "adds1" ].Value.ToLowerInvariant().IndexOf( "n" ) >= 0 );
						SetFlag( Options.ClearLnBefore, g[ "adds1" ].Value.ToLowerInvariant().IndexOf( 'z' ) >= 0 );
					}

					if (g["adds2"].Success)
					{
						SetFlag( Options.AddCrAfter, g[ "adds2" ].Value.ToLowerInvariant().IndexOf( "r" ) >= 0 );
						SetFlag( Options.AddNlAfter, g[ "adds2" ].Value.ToLowerInvariant().IndexOf( "n" ) >= 0 );
						SetFlag( Options.ClearLnAfter, g[ "adds2" ].Value.ToLowerInvariant().IndexOf( 'z' ) >= 0 );
					}

					if ( g[ "justify" ].Success )
						SetFlag( Options.RightJustify, g[ "justify" ].Value == ">" );

					if (g["padspec"].Success)
					{
						switch(g["padspec"].Value[0])
						{
							case '<': this.PadStyle = PadStyles.Left; break;
							case '>': this.PadStyle = PadStyles.Right; break;
							case '|': this.PadStyle = PadStyles.Center; break;
							default: this.PadStyle = PadStyles.None; break;
						}

						this.PadSize = (g["padspec"].Value[1]=='*') ? (byte)(Console.BufferWidth - 1 - Console.CursorLeft) : byte.Parse( g[ "padspec" ].Value.Substring( 1 ) );
						if (g[ "padchar" ].Success)
							this.PadChar = g[ "padchar" ].Value[ 1 ];
					}
					else
					{
						this.PadStyle = PadStyles.None;
						this.PadSize = 0;
						this.PadChar = ' ';
					}
				}
			}
		}

		public override string ToString() =>
			CreatePattern( "", this._colorCode.ToUpperInvariant(), _myOptions, _curPos, PadStyle, PadSize, PadChar );

		//{
		//	string result = 
		//		this._colorCode.ToUpperInvariant() + 
		//		(MyOptions.HasFlag(Options.ClearLnBefore) ? "Z" : "") + 
		//		(MyOptions.HasFlag(Options.AddCrBefore) ? "R" : "") +
		//		(MyOptions.HasFlag(Options.AddNlBefore) ? "N" : "");

		//	if ((this._curPos >= 0) || MyOptions.HasFlag( Options.AddCrAfter | Options.AddNlAfter | Options.ClearLnAfter))
		//		result += "," + ((this._curPos < 0) ? "" : this._curPos.ToString()) + 
		//			(MyOptions.HasFlag(Options.ClearLnAfter) ? "Z" : "") + 
		//			(MyOptions.HasFlag(Options.AddCrAfter) ? "R" : "") + 
		//			(MyOptions.HasFlag(Options.AddNlAfter) ? "N" : "");
		//	else if ((PadStyle != PadStyles.None) && (PadSize > 0))
		//		result += ","; // Need to add a placeholder comma if padding information is present.

		//	if ((PadStyle != PadStyles.None) && (PadSize > 0))
		//	{
		//		switch (PadStyle)
		//		{
		//			case PadStyles.Left: result += ",<" + PadSize.ToString(); break;
		//			case PadStyles.Center: result += ",|" + PadSize.ToString(); break;
		//			case PadStyles.Right: result += ",>" + PadSize.ToString(); break;
		//		}
		//		if (PadChar != ' ')
		//			result += "'" + PadChar + "'";
		//	}
		//	return "{" + result + "}";
		//}

		public int Write(string data)
		{
			if (this._curPos >= 0) Console.CursorLeft = Math.Max(0,Math.Min( Console.BufferWidth, this._curPos - 1 )); // Math.Max( 0, Math.Min( Console.BufferWidth, this._curPos ) - data.Length - 1);
			string clrCode = this._colorCode.PadRight( 2, '*' );

			if (MyOptions.HasFlag(Options.AddCrBefore)) Console.Write( "\r" );
			if (MyOptions.HasFlag(Options.AddNlBefore)) Console.Write( "\n" );

			Console.ForegroundColor = (clrCode[ 0 ] == '*') ? DefaultColor.Fore : this.Color.Fore;
			Console.BackgroundColor = (clrCode[ 1 ] == '*') ? DefaultColor.Back : this.Color.Back;

			if (MyOptions.HasFlag(Options.ClearLnBefore))
			{
				int cpos = Console.CursorLeft, vPos = Console.CursorTop; 
				Console.Write( " ".PadRight( Console.BufferWidth - Console.CursorLeft, ' ' ) ); 
				Console.CursorTop = vPos;
				Console.CursorLeft = cpos; 
			}

			if (!string.IsNullOrEmpty( data ))
			{
				data = data.DecodeHtmlEntities();
				switch (this.PadStyle)
				{
					case PadStyles.Left: data = data.PadLeft( this.PadSize, this.PadChar ); break;
					case PadStyles.Center: data = data.PadCenter( this.PadSize, this.PadChar ); break;
					case PadStyles.Right: data = data.PadRight( this.PadSize, this.PadChar ); break;
				}

//				Console.Out.Write( data.Replace( "\x2022", "\x25cf" ) );
				Console.Write( data );

				if (MyOptions.HasFlag( Options.ClearLnAfter ))
				{
					int cpos = Console.CursorLeft, vPos = Console.CursorTop;
					Console.Write( " ".PadRight( Console.BufferWidth - Console.CursorLeft, ' ' ) );
					Console.CursorTop = vPos;
					Console.CursorLeft = cpos;
				}
			}

			// Allows sending a second CRLF, even if there's nothing to write...
			if (MyOptions.HasFlag( Options.AddCrAfter )) Console.Write( "\r" );
			if (MyOptions.HasFlag( Options.AddNlAfter )) Console.Write( "\n" );

			return Console.CursorLeft;
		}

		public int Write(RichTextBox rtb, string data)
		{
			/*
			if ( this._curPos >= 0 ) Console.CursorLeft = Math.Max( 0, Math.Min( Console.BufferWidth, this._curPos - 1 ) ); // Math.Max( 0, Math.Min( Console.BufferWidth, this._curPos ) - data.Length - 1);
			string clrCode = this._colorCode.PadRight( 2, '*' );

			if ( MyOptions.HasFlag( Options.AddCrBefore ) ) Console.Write( "\r" );
			if ( MyOptions.HasFlag( Options.AddNlBefore ) ) Console.Write( "\n" );

			Console.ForegroundColor = (clrCode[ 0 ] == '*') ? DefaultColor.Fore : this.Color.Fore;
			Console.BackgroundColor = (clrCode[ 1 ] == '*') ? DefaultColor.Back : this.Color.Back;

			if ( MyOptions.HasFlag( Options.ClearLnBefore ) )
			{
				int cpos = Console.CursorLeft, vPos = Console.CursorTop;
				Console.Write( " ".PadRight( Console.BufferWidth - Console.CursorLeft, ' ' ) );
				Console.CursorTop = vPos;
				Console.CursorLeft = cpos;
			}

			if ( !string.IsNullOrEmpty( data ) )
			{
				data = data.DecodeHtmlEntities();
				switch ( this.PadStyle )
				{
					case PadStyles.Left: data = data.PadLeft( this.PadSize, this.PadChar ); break;
					case PadStyles.Center: data = data.PadCenter( this.PadSize, this.PadChar ); break;
					case PadStyles.Right: data = data.PadRight( this.PadSize, this.PadChar ); break;
				}

				//				Console.Out.Write( data.Replace( "\x2022", "\x25cf" ) );
				Console.Write( data );

				if ( MyOptions.HasFlag( Options.ClearLnAfter ) )
				{
					int cpos = Console.CursorLeft, vPos = Console.CursorTop;
					Console.Write( " ".PadRight( Console.BufferWidth - Console.CursorLeft, ' ' ) );
					Console.CursorTop = vPos;
					Console.CursorLeft = cpos;
				}
			}

			// Allows sending a second CRLF, even if there's nothing to write...
			if ( MyOptions.HasFlag( Options.AddCrAfter ) ) Console.Write( "\r" );
			if ( MyOptions.HasFlag( Options.AddNlAfter ) ) Console.Write( "\n" );

			return Console.CursorLeft;
			*/
			return -1;
		}

		public static string CreatePattern(string data, CliColor color, Options options = Options.None, int indent = -1, PadStyles padStyle = PadStyles.None, int padSize = -1, char padChar = ' ') =>
			CreatePattern( data, color.ToHexPair(), options, indent, padStyle, padSize, padChar );

		public static string CreatePattern( string data, string colors = "**", Options options = Options.None, int indent = -1, PadStyles padStyle = PadStyles.None, int padSize = -1, char padChar = ' ' )
		{
			string start = colors.Replace( "*", "" ); // colors.PadRight(2,'*');

			if (options.HasFlag( Options.ClearLnBefore )) start += "Z";
			if (options.HasFlag( Options.AddCrBefore )) start += "R";
			if (options.HasFlag( Options.AddNlBefore )) start += "N";

			if ((indent >=0) || (((int)options & 0x2a) > 0) || (padStyle != PadStyles.None)) // 0x2a = CrAfter (0x02) | NlAfter (0x08) | ClearLnAfter (0x20)
			{
				start += "," + ((indent >= 0) ? indent.ToString() : "");
				if (options.HasFlag( Options.AddCrAfter )) start += "R";
				if (options.HasFlag( Options.AddNlAfter )) start += "N";
				if (options.HasFlag( Options.ClearLnAfter )) start += "Z";

				if (padStyle != PadStyles.None)
				{
					switch(padStyle)
					{
						case PadStyles.Left: start += "<"; break;
						case PadStyles.Right: start += ">"; break;
						case PadStyles.Center: start += "|"; break;
					}
					if (padSize >= 0)
						start += padSize.ToString();

					if (padChar != ' ')
						start += "'" + padChar.ToString() + "'";

				}
			}
			return "{" + start + "}" + data;
		}

		public static string CreatePattern(string data, ConsoleColor foreColor, int indent = -1, Options options = Options.None, PadStyles padStyle = PadStyles.None, int padSize = -1, char padChar = ' ') =>
			CreatePattern( data, new CliColor( foreColor ), options, indent, padStyle, padSize, padChar );

		public static bool Validate(string test) =>
			!string.IsNullOrWhiteSpace(test) || Pattern.IsMatch( test.Trim() );
		#endregion
	}

	public class TextElement
	{
		#region Properties
		protected string _data = "";
		#endregion

		#region Constructors
		public TextElement() { }

		public TextElement(string data) => 
			this.Parse(data);

		public TextElement(TextElementCmd command, string data)
		{
			if (!(command is null))
			{
				this.Command = command;
				this.Data = data;
			}
		}
		#endregion

		#region Accessors
		public string Data
		{
			get
			{
				string work = Regex.Replace( this._data, @"(&lbrace;)", "{", RegexOptions.IgnoreCase );
				work = Regex.Replace( work, @"(&rbrace;)", "}", RegexOptions.IgnoreCase );
				return work.DecodeHtmlEntities();
			}
			set
			{
				if (!string.IsNullOrEmpty( value ))
				{
					value = Regex.Replace( value, TextElementCmd.Pattern.ToString().Trim( new char[] { '^', '$' } ), "" ); // Remove command strings

					if (value.IndexOf( "{" ) >= 0)
						value = value.Replace( "{", "&lbrace;" );

					if (value.IndexOf( "}" ) >= 0)
						value = value.Replace( "}", "&rbrace;" );

					this._data = value;
				}
			}
		}

		public TextElementCmd Command { get; set; } = null;

		public int Length => this.Data.Length;

		public static Regex Pattern =>
			new Regex( @"(?<command>" + TextElementCmd.CMD_REGEX_PATTERN + @")(?<data>[\s\S]*)?", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace );
		#endregion

		#region Operators
		public static implicit operator string(TextElement data) => data.ToString();
		public static implicit operator TextElement(string data) => new TextElement(data);

		public static bool operator ==(TextElement left, TextElement right)
		{
			if (left is null) return right is null;
			if (right is null) return false;
			return left.Data.Equals( right.Data );
		}

		public static bool operator !=(TextElement left, TextElement right) => !(left == right);

		public static bool operator ==(TextElement left, string right) => left == (TextElement)right;

		public static bool operator !=(TextElement left, string right) => !(left == right);
		#endregion

		#region Methods
		public void Parse(string source)
		{
			// "{colors,xpos}data"
			if (Pattern.IsMatch( source ))
			{
				MatchCollection matches = Pattern.Matches( source );
				if (matches.Count > 0)
				{
					Match m = matches[ 0 ];
					this.Data = m.Groups[ "data" ].Success ? m.Groups[ "data" ].Value : "";
					this.Command = m.Groups[ "command" ].Success ? new TextElementCmd( m.Groups[ "command" ].Value ) : new TextElementCmd();
					return;
				}
			}
			this.Data = source;
		}

		public int Write() =>
			Command.Write( this.Data );

		public override string ToString() =>
			((Command is null) ? "" : Command.ToString()) + this._data;

		public override bool Equals(object obj) =>
			base.Equals( obj );

		public override int GetHashCode() =>
			base.GetHashCode();

		public static TextElementCollection CheckBox(bool state, ConsoleColor color, CliColor defaultColor = null, char[] opts = null, bool addCrLf = false)
		{
			if (defaultColor is null) defaultColor = TextElementCollection.GlobalDefaultColor;

			if ((opts is null) || (opts.Length < 2))
				opts = new char[] { 'X', '√' };

			return new TextElementCollection(
				new string[]
				{
					TextElementCmd.CreatePattern( "[", defaultColor ),
					TextElementCmd.CreatePattern( opts[ state ? 1 : 0 ].ToString(), color ),
					TextElementCmd.CreatePattern( "]", defaultColor, addCrLf ? TextElementCmd.Options.AddCrLfAfter : TextElementCmd.Options.None )
				}
				) { DefaultColor = defaultColor };
		}

		public static TextElementCollection CheckBoxLn(bool state, ConsoleColor color, CliColor defaultColor = null, char[] opts = null) =>
			CheckBox( state, color, defaultColor, opts, true );

		public static TextElementCollection CheckBox(bool state, CliColor defaultColor = null, char[] opts = null) =>
			CheckBox( state, (state ? ConsoleColor.Green : ConsoleColor.Red), defaultColor, opts );

		public static TextElementCollection CheckBoxLn(bool state, CliColor defaultColor = null, char[] opts = null) =>
			CheckBox( state, (state ? ConsoleColor.Green : ConsoleColor.Red), defaultColor, opts, true );

		public static TextElementCollection InductionLine( string action, string source, string[] targets, bool useCrLf = false, CliColor defaultColor = null )
		{
			if (defaultColor is null) defaultColor = TextElementCollection.GlobalDefaultColor;
			TextElementCollection result = new TextElementCollection(
				action + source + " {7}(" + string.Join( "{7}|", targets ) + "{7}){" + (useCrLf ? ",rn" : "") + "}..."
			) { DefaultColor = defaultColor };

			return result;
		}

		public static TextElementCollection InductionLine(string action, string source, string target, bool useCrLf = false, CliColor defaultColor = null) =>
			InductionLine( action, source, new string[] { target }, useCrLf, defaultColor );
		#endregion
	}

	public class TextElementRule
	{
		#region Proeprties
		protected string _search = "";
		protected string _replace = "";
		protected RegexOptions _options = RegexOptions.None;
		#endregion

		#region Constructors
		public TextElementRule(string search, string replace, RegexOptions options = RegexOptions.None)
		{
			this._search = search;
			this._replace = replace;
			this._options = options;
		}
		#endregion

		#region Accessors
		public string SearchPattern
		{
			get => this._search;
			set => this._search = value;
		}

		public string ReplacePattern
		{
			get => this._replace;
			set => this._replace = value;
		}

		public RegexOptions Options
		{
			get => this._options;
			set => this._options = value;
		}
		#endregion

		#region Methods
		public string ApplyRule(string target) =>
			Regex.Replace( target, _search, _replace, _options );
		#endregion
	}

	public class TextElementInnerRule : TextElementRule
	{
		#region Properties
		protected string _outerSearch = "";
		#endregion

		#region Constructors
		/// <summary>Allows building a more complex rule that facilitates search and replace within the scope of a larger external pattern.</summary>
		/// <param name="outerSearch">
		/// A Regular Expression that identifies the whole pattern to isolate. Must use a pattern group labled "?<outer>" to wrap the whole 
		/// pattern, and a contained group labled "?<inner>" that identifies the internal pattern to find.
		/// </param>
		/// <param name="innerSearch">A Regular Expression that specifies the pattern WITHIN THE "inner" PATTERN GROUP to replace.</param>
		/// <param name="replace">A string to specify the replacement content for the inner pattern.</param>
		/// <param name="options">A RegexOptions matrix specifying the options to apply to the pattern matching.</param>
		public TextElementInnerRule(string outerSearch, string innerSearch, string replace, RegexOptions options = RegexOptions.None) :
			base(innerSearch, replace, options) =>
			this._outerSearch = outerSearch;
		#endregion

		#region Accessors
		public string OuterSearch
		{
			get => this._outerSearch;
			set => this._outerSearch = value;
		}
		#endregion

		#region Methods
		new public string ApplyRule(string target)
		{
			Regex search = new Regex( _outerSearch, _options );
			string work = target;
			if (search.IsMatch(target))
			{
				MatchCollection matches = search.Matches( target );
				foreach (Match m in matches)
				{
					if (m.Groups["inner"].Success && m.Groups["outer"].Success)
					{
						work = m.Groups[ "inner" ].Value;
						work = Regex.Replace( work, _search, _replace, _options );
						work = m.Groups[ "outer" ].Value.Replace( m.Groups[ "inner" ].Value, work );
						target = target.Replace( m.Groups[ "outer" ].Value, work );
					}
				}
			}
			return work;
		}
		#endregion
	}

	public class TextElementRuleCollection : IEnumerator<TextElementRule>
	{
		#region Properties
		protected List<TextElementRule> _rules = new List<TextElementRule>();
		protected int _position = 0;
		#endregion

		#region Constructors
		public TextElementRuleCollection() { }

		public TextElementRuleCollection( TextElementRule[] rules ) =>
			this.AddRange( rules );

		public TextElementRuleCollection( TextElementRule rule ) =>
			this.Add( rule );
		#endregion

		#region Accessors
		public TextElementRule this[int index] =>
			this._rules[index];

		public int Count =>
			this._rules.Count;

		public ShowContent.DumpOptions DisplayOptions { get; set; } = ShowContent.DumpOptions.None;

		/// <summary>If set to TRUE, the content will be parsed and displayed as XML data,</summary>
		public bool ForceXml { get; set; } = false;

		/// <summary>When set to TRUE, this causes the input data to be parsed and all CRLF ( "\r", "\n" ) replaced with entities ( "&lbrace;", "&rbrace;" ).</summary>
		public bool ReplaceCRLF { get; set; } = false;

		/// <summary>When set to TRUE, this causes the input data to be parsed and all brace brackets ( "{", "}" ) replaced with DECL entities ( "{,rn}" ).</summary>
		public bool ReplaceBraces { get; set; } = false;

		// IEnumerator Support Accessors...
		TextElementRule IEnumerator<TextElementRule>.Current => this[ this._position ];

		object IEnumerator.Current => this[ this._position ];
		#endregion

		#region Methods
		public void Add( TextElementRule rule) =>
			this._rules.Add(rule);

		public void AddRange( TextElementRule[] rules ) =>
			this._rules.AddRange( rules );

		public string ApplyRules(string target)
		{
			foreach (TextElementRule rule in this._rules)
				target = rule.ApplyRule( target );

			return target;
		}

		public string ApplyRules(string target, string[] data)
		{
			int i = -1; while (++i < data.Length)
				target = Regex.Replace( 
					target, 
					/* language=regex */ @"(?<prechar>[^\\])[$]$1".Replace( new object[] { i + 1 } ),
					/* language=regex */ @"${prechar}$2".Replace( new object[] { data[ i ] } ), 
					RegexOptions.None 
				);

			return ApplyRules( target );
		}

		public TextElementCollection CreateCollection( string source ) =>
			new TextElementCollection( this.ApplyRules( source ) );

		public TextElementCollection CreateCollection( string source, string[] data ) =>
			new TextElementCollection( this.ApplyRules( source, data ) );

		private bool IsXml(string source)
		{
			XmlDocument doc = new XmlDocument() { XmlResolver = null };
			try { doc.LoadXml( source ); }
			catch
			{
				try { doc.LoadXml( NetXpertExtensions.Xml.XML.HEADER + source ); }
				catch { return false; }
			}
			return true;
		}

		public string Compile(string data, CliColor defaultColor = null) =>
			Compile( data, new string[] { }, defaultColor );

		public string Compile(string source, object[] data, CliColor defaultColor = null) =>
			Compile( source, TextElementCollection.ObjectPrep( data ), defaultColor );

		public string Compile(string source, string[] data, CliColor defaultColor = null)
		{
			if (ReplaceBraces)
				source = source.Replace( "{", "&lbrace;" ).Replace( "}", "&rbrace;" );

			if (ReplaceCRLF)
				source = source.Replace( "\r", "" ).Replace( "\n", "{,rn} " );

			if (!string.IsNullOrEmpty( source ))
			{
				string output = source;
				if (ForceXml || IsXml( source ))
				{
					try
					{
						System.Xml.Linq.XDocument doc = System.Xml.Linq.XDocument.Parse( source );
						output = doc.ToString();
					}
					catch (Exception) { output = source; }
				}
				if (output[ 0 ] != '{') output = "{}" + output;
				TextElementCollection collection = CreateCollection( output, data );
				collection.DefaultColor = (defaultColor is null) ? TextElementCollection.GlobalDefaultColor : defaultColor;
				return collection.ToString();
			}
			return source;
		}

		public void Write(string data, CliColor defaultColor = null) =>
			Write( data, new string[] { }, defaultColor );

		public void WriteLn(string data, CliColor defaultColor = null)
		{
			Write( data, new string[] { }, defaultColor );
			Console.WriteLine();
		}

		public void Write(string source, object[] data, CliColor defaultColor = null) =>
			Write( source, TextElementCollection.ObjectPrep( data ), defaultColor );

		public void WriteLn(string source, object[] data, CliColor defaultColor = null)
		{
			Write( source, TextElementCollection.ObjectPrep( data ), defaultColor );
			Console.WriteLine();
		}

		public void Write(string source, string[] data, CliColor defaultColor = null) =>
			CreateCollection( Compile(source, data, defaultColor ) ).Write();

		public void WriteLn(string source, string[] data, CliColor defaultColor = null)
		{
			CreateCollection( Compile( source, data, defaultColor ) ).Write();
			Console.WriteLine();
		}

		//IEnumerator Support
		public IEnumerator<TextElementRule> GetEnumerator() => this._rules.GetEnumerator();

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
			Dispose( true );
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
		#endregion
	}

	public class TextElementCollection : IEnumerator<TextElement>
	{
		#region Properties
		protected CliColor _defaultColor = Con.DefaultColor;
		protected List<TextElement> _nodes = new List<TextElement>();
		protected int _position = 0;
		#endregion

		#region Constructors
		public TextElementCollection() { }

		public TextElementCollection(TextElement item) =>
			this.Add( item );

		public TextElementCollection(TextElement[] items) =>
			this.AddRange( items );

		public TextElementCollection(string[] items) =>
			this.AddRange( items );

		public TextElementCollection(string content) =>
			this.Parse( content );

		public TextElementCollection(string content, object[] data) =>
			this.Parse( content, ObjectPrep( data ) );

		public TextElementCollection(string content, string[] data) =>
			this.Parse( content, data );
		#endregion

		#region Accessors
		public int Count => this._nodes.Count;

		public CliColor DefaultColor
		{
			get => this._defaultColor;
			set
			{
				if (value is null) value = GlobalDefaultColor;
				this._defaultColor = value;
				for (int i = 0; i < this._nodes.Count; i++)
					this._nodes[ i ].Command.DefaultColor = value;
			}
		}

		public TextElement this[ int i ]
		{
			get => this._nodes[ i ];
			set => this._nodes[ i ] = value;
		}

		// IEnumerator Support Accessors...
		TextElement IEnumerator<TextElement>.Current => this[ this._position ];

		object IEnumerator.Current => this[ this._position ];

		public static CliColor GlobalDefaultColor => Con.DefaultColor;
		#endregion

		#region Operators
		public static implicit operator TextElementCollection(string data) => new TextElementCollection( data );
		public static implicit operator TextElementCollection(string[] data) => new TextElementCollection( data );
		public static implicit operator string[](TextElementCollection data)
		{
			List<string> temp = new List<string>();
			foreach (TextElement te in data._nodes)
				temp.Add( te.ToString() );
			return temp.ToArray();
		}
		#endregion

		#region Methods
		public void Add(TextElement item)
		{
			item.Command.DefaultColor = this._defaultColor;
			this._nodes.Add( item );
		}

		public void Add(string line) =>
			this.Add( new TextElement( line ) );

		public void AddRange(TextElement[] items)
		{
			foreach (TextElement item in items)
				this.Add( item );
		}

		public void AddRange(string[] items)
		{
			foreach (string s in items)
			{
				TextElement te = new TextElement( s );
				te.Command.DefaultColor = this.DefaultColor;
				this.Add( te );
			}
		}

		public void Clear() =>
			this._nodes = new List<TextElement>();

		public void RemoveAt(int index) =>
			this._nodes.RemoveAt( index );

		public bool Swap(int nodeIndex1, int nodeIndex2)
		{
			if ((nodeIndex1 < Count) && (nodeIndex1 >= 0) && (nodeIndex2 < Count) && (nodeIndex2 >= 0))
			{
				TextElement hold = this[ nodeIndex1 ];
				this[ nodeIndex1 ] = this[ nodeIndex2 ];
				this[ nodeIndex2 ] = hold;
				return true;
			}
			return false;
		}

		public TextElement[] ToArray() =>
			this._nodes.ToArray();

		public void Parse(string source, string[] data)
		{
			int i = -1; while (++i < data.Length)
				source = Regex.Replace(
					source, 
					/* language=regex */ @"(?<prechar>[^\\])[$]$1".Replace( new object[] { (i + 1) } ),
					/* language=regex */ @"${prechar}$1".Replace( new object[] { data[ i ] } ), 
					RegexOptions.None 
				);

			Parse( source );
		}

		public void Parse(string source)
		{
			Regex ctrl = new Regex( /* language=regex */
				@"(?<command>$1)(?<data>[^{]*)".Replace( new object[] { TextElementCmd.CMD_REGEX_PATTERN } ),
				RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace
			);

			MatchCollection matches = ctrl.Matches( source );
			if (matches.Count > 0)
			{
				this.Clear();
				foreach (Match m in matches)
				{
					string cmd = m.Groups[ "command" ].Success ? m.Groups[ "command" ].Value : "",
						content = m.Groups[ "data" ].Success ? m.Groups[ "data" ].Value : "";
					content = Regex.Replace( content, @"(&lbr;)", "{", RegexOptions.IgnoreCase );
					content = Regex.Replace( content, @"(&rbr;)", "}", RegexOptions.IgnoreCase );

					TextElement item = new TextElement( new TextElementCmd( cmd ), content );
					item.Command.DefaultColor = this._defaultColor;
					this.Add( item );
				}
			}
		}

		public override string ToString()
		{
			string work = "";
			foreach (TextElement te in this._nodes)
				work += te.ToString();
			return work;
		}

		public void Write()
		{
			foreach (TextElement te in this._nodes)
				te.Write();
		}

		//IEnumerator Support
		public IEnumerator<TextElement> GetEnumerator() => this._nodes.GetEnumerator();

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
			Dispose( true );
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion

		public static string[] ObjectPrep(object[] data)
		{
			if (data is null) return new string[] { };
			List<string> results = new List<string>();
			foreach (object o in data)
				if (!(o is null))
				{
					switch (o.GetType().Name)
					{
						case "CliColor":
							results.Add( (o as CliColor).ToHexPair() );
							break;
						default:
							results.Add( o.ToString() );
							break;
					}
				}
			return results.ToArray();
		}

		public static void Write(string data, CliColor defaultColor = null) =>
			new TextElementCollection( data ) { DefaultColor = defaultColor }.Write();
		#endregion

		#region Parameterized Write Options
		public static void Write(string template, object data, CliColor defaultColor = null) =>
			new TextElementCollection( template, ObjectPrep( new object[] { data } ) ) { DefaultColor = defaultColor }.Write();

		public static void Write(string template, object[] data, CliColor defaultColor = null) =>
			new TextElementCollection( template, data ) { DefaultColor = defaultColor }.Write();

		public static void Write(string template, string[] data, CliColor defaultColor = null) =>
			new TextElementCollection( template, data ) { DefaultColor = defaultColor }.Write();
		#endregion
	}

	public static class ShowContent
	{
		/// <summary>Provides a mechanism for implementing various Dump features / options.</summary>
		[Flags] public enum DumpOptions : byte { None = 0, More = 1, LineNos = 2, Header = 4, Footer = 8, Default = 12, All = 255 }

		/// <summary>Exports supplied content to the screen in as intelligible a form as possible.</summary>
		/// <param name="source">The content  to try and disply.</param>
		/// <param name="name">What the content is named (title).</param>
		/// <param name="rules">A TextElementRuleCollection to apply to the content.</param>
		/// <param name="options">A DumpOptions value specifying what options to apply to the output.</param>
		/// <remarks>If the TextElementRuleCollection has defined DisplayOptions, and this value is left to Default, the rule collection options will be used instead.</remarks>
		public static void Dump( object source, string name = "", TextElementRuleCollection rules = null, DumpOptions options = DumpOptions.Default )
		{
			Console.CursorVisible = false;
			if ( (rules is null) && IsXml( source.ToString(), true ) )
				rules = XmlFile;

			if ( rules is null )
				rules = PlainText; // throw new Exception( "You cannot dump without specifying the rules to use!" );

			// If the rule collection has defined Display options, and none were specifically defined when this procedure was called, use the rule's DisplayOptions.
			if ( (rules.DisplayOptions != DumpOptions.None) && (options == DumpOptions.Default) )
				options = rules.DisplayOptions;

			int lineCount = 0;
			if ( options.HasFlag(DumpOptions.Header) )
			{
				if ( !string.IsNullOrWhiteSpace( name ) )
					Con.Tec( 
						"{$1z,,<10'\x2550'} {$2}$3{$1,rn,>$4'\x2550'} ", // ═
						new object[] { 
							Con.DefaultColor.Inverse, 
							Con.DefaultColor.Inverse.Alt( ConsoleColor.White ), 
							name, 
							Console.BufferWidth - 11 - name.Length 
						} 
					); 
				else
					Con.Tec( "{$1z,rn,>*'\x2550'}\x2550" );
				lineCount++;
			}

			string content = rules.Compile( source.ToString() );
			foreach ( string line in content.Split( new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries ) )
			{
				if ( options.HasFlag(DumpOptions.LineNos) ) Con.Tec( "{6,,<6'0'}$1: ", (options.HasFlag(DumpOptions.Header) ? lineCount++ : ++lineCount) );
				Con.Tec( "{}$1", line.Replace( "\r", "◄" ).Replace( "\n", "┘" ) );
				Console.WriteLine();
				if ( options.HasFlag(DumpOptions.More) && (++lineCount % Console.WindowHeight == 0) ) Con.WaitPrompt();
			}

			if ( options.HasFlag( DumpOptions.Footer ) )
				Con.Tec( "{$1z,rn,>*'\x2550'}\x2550", Con.DefaultColor.Inverse ); // ═
			Console.CursorVisible = true;
		}

		/// <summary>Tries to parse a string as an XmlDocument and reports success or failure.</summary>
		/// <param name="allowHeaderlessXml">If set to TRUE (default) the test will pass valid XML, even if the header is missing.</param>
		/// <param name="resolver">Allows using a defined XmlResolver. Defaults to null.</param>
		/// <returns>TRUE if the supplied string can be parsed as XML.</returns>
		private static bool IsXml( string source, bool allowHeaderlessXml = true, XmlResolver resolver = null )
		{
			XmlDocument doc = new XmlDocument() { XmlResolver = resolver };
			try { doc.LoadXml( source ); return true; } catch { }

			if ( allowHeaderlessXml )
				try { doc.LoadXml( "<?xml version='1.0' encoding='ISO - 8859 - 1'?>" + source ); return true; } catch { }

			return false;
		}

		/// <summary>A very simple rule to display plaintext data.</summary>
		public static TextElementRuleCollection PlainText =>
			new TextElementRuleCollection( new TextElementRule[] { new TextElementRule( /* language=regex */ @"^(.*)$", "{}$1", RegexOptions.Multiline ) } ) { ForceXml = false };

		/// <summary>A simple rule collection for managing / displaying XML content.</summary>
		public static TextElementRuleCollection XmlFile =>
			new TextElementRuleCollection(
				new TextElementRule[]
				{
					new TextElementRule( /* Inner Text */ /* language=regex */
						@"([<][a-z_][\w]*[^>]*[>])(.*?)(?=[<][\/][a-z_][\w]*[>])", "$1{F}$2", RegexOptions.IgnoreCase | RegexOptions.Multiline ),
					new TextElementRule(  /* Tag < > signs. */ /* language=regex */
						//"[<](([a-z_][\\w]*)([ ][a-z_][\\w]*[\\s]?=[\\s]?[\"']?[^'\">]*[\"']?)?|[\\/][a-z_][\\w]*)[>]","{9}<$1{9}>", 
						@"([<][\/]?)(.+?)(?=[>])","{1}$1$2{1}", RegexOptions.IgnoreCase | RegexOptions.Multiline ),
					new TextElementRule(  /* Xml Tags and End-Tags */ /* language=regex */
						@"(?<=[<])([\/]?)([a-z_][\w]*)(?=[^>]*[>])", "$1{3}$2", RegexOptions.IgnoreCase | RegexOptions.Multiline ),
					new TextElementInnerRule( /* Xml Attributes & Values */ /* language=regex */
						@"(?<outer>(?:[{][a-f0-9][}])?[<](?:[{][a-f0-9][}])?[a-z_][\w]_ (?<inner>([ ]?[a-z][\w]*[\s]*=[\s]*[\x22']?[^'\x22>][\x22']?)(?:[{][a-f0-9][}])?[>])", /* language=regex */
						@"([a-z][\w]*)([\s]*=[\s]*[\x22']?)([^'\x22>]*)([\x22']?)", "{9}$1{8}$2{B}$3{8}$4", RegexOptions.IgnoreCase | RegexOptions.Multiline )
				}
			)
			{ ForceXml = true };
	}
}
