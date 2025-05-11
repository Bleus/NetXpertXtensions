using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using NetXpertExtensions;

namespace NetXpertCodeLibrary.ConsoleFunctions
{
	/// <summary>Functions for reading/writing to the Console window screen.</summary>
	public static class CRW
	{
		#region Accessors
		/// <summary>Gets/Sets the cursor position in the viewer via a Point2 object.</summary>
		public static Point2 CursorPosition
		{
			get => new Point2( Console.CursorLeft, Console.CursorTop );
			set
			{
				if (!(value is null))
				{
					int x = (value.X < 0) ? Console.WindowWidth : value.X;
					int y = (value.Y < 0) ? Console.WindowHeight : value.Y;

					x = Math.Max( 0, Math.Min( Console.WindowWidth, x ) );
					y = Math.Max( 0, Math.Min( Console.WindowHeight, y ) );
					Console.SetCursorPosition( x, y );
				}
			}
		}

		/// <summary>Provides an internallly consistent mechanism for accessing the Console.CursorSize feature.</summary>
		/// <remarks>If a CursorSize less than 1 is assigned, the Cursor size is not changed, but the cursor is turned off.</remarks>
		public static int CursorSize
		{
			get => Console.CursorVisible ? Console.CursorSize : -1;
			set
			{
				Console.CursorVisible = value > 0;
				if (Console.CursorVisible)
					Console.CursorSize = value;
			}
		}
		#endregion

		#region Static Methods for accessing the console screen buffer.
		/// <summary>Outputs provided text to the console at the specified location.</summary>
		/// <remarks>This function leverages the DECL formatting engine for its output, and so the provided text and object array are
		/// consisent with their corresponding DECL application.</remarks>
		/// <param name="what">The text to output.</param>
		/// <param name="location">A Point2 object specifying where to place the text.</param>
		/// <param name="data">An optional list of objects to inject into the output string as per the DECL standard.</param>
		/// <param name="restoreCursor">If TRUE, the cursor position is saved prior to the operation, then restored afterwards.</param>
		public static void WriteAt(string what, Point2 location, object[] data = null, bool restoreCursor = false)
		{
			Point2 curPos = CursorPosition;
			CursorPosition = location;
			Con.Tec( what, data );
			if (restoreCursor) CursorPosition = curPos;
		}

		/// <summary>Outputs provided text to the console at the specified location.</summary>
		/// <remarks>This function leverages the DECL formatting engine for its output, and so the provided text and object array are
		/// consisent with their corresponding DECL application.</remarks>
		/// <param name="what">The text to output.</param>
		/// <param name="x">The horizontal cursor position to place it at.</param>
		/// <param name="y">The vertical cursor position.</param>
		/// <param name="data">An optional list of objects to inject into the output string as per the DECL standard.</param>
		/// <param name="restoreCursor">If TRUE, the cursor position is saved prior to the operation, then restored afterwards.</param>
		public static void WriteAt(string what, int x = -1, int y = -1, object[] data = null, bool restoreCursor = false) =>
			WriteAt( what, new Point2( x < 0 ? Console.WindowWidth : x, y < 0 ? Console.WindowHeight : y ), data, restoreCursor );

		/// <summary>Reads a single character from the specified location.</summary>
		/// <param name="location">A Point2 object specifying where to read the character from.</param>
		/// <param name="handle">The Console Handle for the window to read from.</param>
		/// <returns>The character found at the specified location.</returns>
		/// <exception cref="Win32Exception">Occurs if the attempt to read the specified location fails for any reason.</exception>
		public static char ReadScreenChar(Point2 location, int handle = -11)
		{
			IntPtr stdOut = GetStdHandle( handle );
			uint coord = (uint)location.X | (uint)location.Y << 16;
			if (!ReadConsoleOutputCharacterA(
				stdOut, out byte chAnsi, 1, coord, out _ ))
				throw new Win32Exception();
			return (char)chAnsi;
		}

		/// <summary>Reads a single-line of text from the specified point, for the specified length.</summary>
		/// <param name="location">A Point2 object specifying where to start reading.</param>
		/// <param name="length">How many characters to read.</param>
		/// <param name="handle">The Console Handle for the window to read from.</param>
		/// <returns>A string containing the characters copied from the specified console screen coordinates.</returns>
		public static string ReadScreenLine(Point2 location, int length, int handle = -11)
		{
			string result = "";
			if (length > 1)
				for (int x = location.X; x < location.X + length; x++)
					result += ReadScreenChar( location + x, handle );
			else
				result = ReadScreenChar( location, handle ).ToString();

			return result;
		}

		/// <summary>Reads a multi-line (box/rectangle) section of the screen into an array of strings (1 per line).</summary>
		/// <param name="area">A Rectangle object specifying the area to read.</param>
		/// <param name="handle">The Console Handle for the window to read from.</param>
		/// <returns>An array of strings containing the data copied from the specified region of the console.</returns>
		/// <remarks>If the Rectangle defines only a single line, this simply returns the single-line ReadScreen value in an array of one.</remarks>
		public static string[] ReadScreenArea(Rectangle area, int handle = -11)
		{
			if (area.Top == area.Bottom) // Single line reads go here:
				return new string[] { ReadScreenLine( new Point( area.Left, area.Top ), area.Right - area.Left, handle ) };

			List<string> result = new();
			for (int y = area.Top; y < area.Bottom; y++)
				result.Add( ReadScreenLine( area.Location.Add( 0, y ), area.Width, handle ) );
			// Note that the "area.Location.Add" call above is performed on a POINT object! (thus doesn't alter the base value)

			return result.ToArray();
		}

		/// <summary>Fills an area of the screen buffer with a specified character, in a specified colour.</summary>
		/// <param name="location">A Point2 object defining the top left corner of the area to be filled.</param>
		/// <param name="size">A Size object specifying the width and height of the area to be filled.</param>
		/// <param name="withChar">A Char value to fill with.</param>
		/// <param name="color">What color to fill with.</param>
		/// <param name="restoreCursor">If TRUE, puts the cursor back where it was found when the function was called.</param> 
		public static void FillArea(Point2 location, Size size, char withChar = ' ', CliColor color = null, bool restoreCursor = true) =>
			FillArea( new Rectangle( location, size ), withChar, color, restoreCursor );

		/// <summary>Fills an area of the screen buffer with a specified character, in a specified colour.</summary>
		/// <param name="area">A Rectangle defining the area to fill.</param>
		/// <param name="withChar">A Char value to fill it with.</param>
		/// <param name="color">What color to fill with.</param>
		/// <param name="restoreCursor">If TRUE, puts the cursor back where it was found when the function was called.</param>
		public static void FillArea(Rectangle area, char withChar = ' ', CliColor color = null, bool restoreCursor = true)
		{
			Point2 curPos = CursorPosition;
			if (color is null) color = Con.DefaultColor;

			Console.ForegroundColor = color.Fore;
			Console.BackgroundColor = color.Back;
			for (int y = 0; y < area.Height; y++)
				if (y + area.Location.Y < Console.WindowHeight)
					for (int x = 0; x < area.Width; x++)
						if (x + area.Location.X < Console.WindowWidth)
						{
							CRW.CursorPosition = area.Location + new Point2( x, y );
							Console.Write( withChar );
						}

			if (restoreCursor) CursorPosition = curPos;
		}

		/// <summary>Resets the X value to 0, and adds 1 to the Y value.</summary>
		/// <remarks>Extends the Point2 class to add a simple function to perform CRLF on the point.</remarks>
		public static void ApplyCrLf(this Point2 point) =>
			point.Subtract( point.X, -1 );

		// Extend the Rectangle class to add a simple call to calculate the area of the rectangle.
		/// <summary>Calculate the area of the Rectangle (width * height).</summary>
		/// <returns>The area covered by the Rectangle.</returns>
		public static int Area(this Rectangle rect) =>
			rect.Width * rect.Height;

		[DllImport( "kernel32", SetLastError = true )]
		static extern IntPtr GetStdHandle(int num);

		[DllImport( "kernel32", SetLastError = true, CharSet = CharSet.Ansi )]
		[return: MarshalAs( UnmanagedType.Bool )] // ̲┌──────────────────^
		static extern bool ReadConsoleOutputCharacterA(
			IntPtr hStdout,   // result of 'GetStdHandle(-11)'
			out byte ch,      // A̲N̲S̲I̲ character result
			uint c_in,        // (set to '1')
			uint coord_XY,    // screen location to read, X:loword, Y:hiword
			out uint c_out);  // (unwanted, discard)

		[DllImport( "kernel32", SetLastError = true, CharSet = CharSet.Unicode )]
		[return: MarshalAs( UnmanagedType.Bool )] // ̲┌───────────────────^
		static extern bool ReadConsoleOutputCharacterW(
			IntPtr hStdout,   // result of 'GetStdHandle(-11)'
			out Char ch,      // U̲n̲i̲c̲o̲d̲e̲ character result
			uint c_in,        // (set to '1')
			uint coord_XY,    // screen location to read, X:loword, Y:hiword
			out uint c_out);  // (unwanted, discard)
		#endregion
	}

	public class ScreenEditStringManager
	{
		#region Properties
		protected string _value = "";
		protected int _selectionStart = -1;
		protected int _selectionLength = -1;
		#endregion

		#region Constructors
		public ScreenEditStringManager(Point2 location, Size s)
		{
			Location = location;
			Size = s;
		}

		public ScreenEditStringManager(Point2 location, Size s, string value)
		{
			Location = location;
			this._value = value;
			this.Size = s;
		}

		public ScreenEditStringManager(Point2 location, Size s, string[] lines)
		{
			Location = location;
			this.Lines = lines;
			this.Size = s;
		}
		#endregion

		#region Operators
		public static implicit operator String(ScreenEditStringManager data) => data._value;

		public static bool operator ==(ScreenEditStringManager left, ScreenEditStringManager right)
		{
			if (left is null) return (right is null) || string.IsNullOrEmpty( right._value ); ;
			if (right is null) return string.IsNullOrEmpty( left._value );
			return left._value.Equals(right._value);
		}

		public static bool operator !=(ScreenEditStringManager left, ScreenEditStringManager right) => !(left == right);
		#endregion

		#region Accessors
		/// <summary>The length of the string managed by this object.</summary>
		public int Length => this._value.Length;

		/// <summary>The number of lines represented by this objedt.</summary>
		public int Count => this.Lines.Length;

		/// <summary>Returns this object as an array of lines rather than as a single string.</summary>
		public string[] Lines
		{
			set => _value = string.Join( "\r\n", value );
			get
			{
				List<string> lines = new List<string>( Regex.IsMatch( _value, @"[\r\n]" ) ? _value.Replace( "\r", "" ).Split( '\n' ) : new string[] { _value } );
				for (int i = 0; i < lines.Count; i++)
					if (lines[ i ].Length > Size.Width)
					{
						string remainder = lines[ i ].Substring( Size.Width );
						lines[ i ] = lines[ i ].Substring( 0, Size.Width );
						lines.Insert( i + 1, remainder );
					}

				while (lines.Count > Size.Height)
					lines.RemoveAt( Size.Height );

				return lines.ToArray();
			}
		}

		public Size Size { get; set; }

		public int SelectionStart
		{
			get => Math.Max( -1, _selectionStart );
			set => _selectionStart = Math.Max( -1, value );
		}

		public int SelectionLength
		{
			get => _selectionStart > 0 ? _selectionStart : 0;
			set => Math.Min( Math.Max( 0, value ), Math.Max( 0, _value.Length - SelectionStart ) );
		}

		public string SelectedText => (SelectionLength > 0) ? _value.MidString( SelectionStart, SelectionLength ) : "";

		public bool IsSingleLine => Size.Height == 1;

		public string Value
		{
			get => this._value;
			set => this._value = IsSingleLine ? Regex.Replace( value, @"[\r\n]+", " " ) : value;
		}

		public Point2 Location { get; set; } = new Point2();

		/// <summary>References the position of the cursor within the central string (as opposed to screen position).</summary>
		public int InsertPosition
		{
			get
			{
				Point2 position = CRW.CursorPosition - Location;
				if (IsSingleLine) return position.X > _value.Length ? -1 : position.X; // DGAF about "Y" on single line fields.

				string[] lines = Lines;
				if ((position.Y > lines.Length) || (position.X > lines[ position.Y ].Length)) return -1;

				if (position.Y == 0) return position.X;

				int insertPosition = 0;
				for (int i = 0; i < position.Y - 1; i++) insertPosition += lines[ i ].Length + 2; // +2 for "\r\n"
				insertPosition += position.X;
				return insertPosition;
			}

			set
			{
				Point2 position = new Point2();
				if (value >= 0)
				{
					int target = Math.Min( _value.Length, value );
					for (int i = 0; i < target; i++)
					{
						if (_value[ i ] == '\n') position.ApplyCrLf(); else position.Add( (_value[ i ] == '\r') ? 0 : 1 );
						if (">\r\n".IndexOf( _value[ i ] ) > 0) target++; // CR and LF don't count!
					}
				}

				CRW.CursorPosition = Location + position;
			}
		}
		#endregion

		#region Methods
		/// <summary>Display the field.</summary>
		/// <param name="dataColor">Color for text.</param>
		/// <param name="selectionColor">Color for selected text.</param>
		/// <param name="templateColor">Color of the template (non-data area)</param>
		/// <param name="templateChar">The character to use to map out the template area.</param>
		/// <param name="restoreCursor">If TRUE (default), put the cursor back where it was before the render began.</param>
		public void Render( CliColor dataColor, CliColor selectionColor = null, CliColor templateColor = null, char templateChar = '_', bool restoreCursor = true )
		{
			Point2 curPos = CRW.CursorPosition; // Save the current cursor location...
			int curSize = CRW.CursorSize;       // Save the current cursor size...
			CRW.CursorSize = 0;                 // Turn off the cursor.
			selectionColor = selectionColor is null ? dataColor.Inverse : selectionColor;
			templateColor = templateColor is null ? Con.DefaultColor : templateColor;

			Point2 position = new Point2( Location );
			Point2 end = Location + Size;

			if (Length > 0)
			{
				string data = _value.Replace( "\r", "" );
				for (int i = 0; i < data.Length; i++)
				{
					CliColor color = (SelectionLength > 0) ? ((i >= SelectionStart) && (i < SelectionStart + SelectionLength) ? selectionColor : dataColor) : dataColor;
					object[] what = (data[ i ] == '\n') ? new object[] { templateColor, templateChar.ToString().PadRight( Size.Width - (CRW.CursorPosition.X - Location.X) ), "rn" } : new object[] { color, data[ i ], "" };
					Con.Tec( "{$1$3}$2", what );
					if (CRW.CursorPosition >= end) break;
				}
			}

			// Draw the rest of the field if necessary.
			while (CRW.CursorPosition < end)
			{
				Con.Tec( "{$1}$2", new object[] { templateColor, templateChar } );
				if (Console.CursorLeft == new Corners( Location, Size ).Right) CRW.CursorPosition = new Point2( Location.X, Console.CursorTop + 1 ); // new line
			}

			if (restoreCursor) CRW.CursorPosition = curPos; // If restoration is specified, put the cursor back where we found it.
			CRW.CursorSize = curSize;						// Turn the cursor back on.
		}

		public Corners Box(Point2 location = null) =>
			new Corners( (location is null) ? Location : location, Size );

		public override bool Equals(object obj) =>
			base.Equals( obj );

		public bool Equals(string value, StringComparison stringComparison) =>
			value.Equals( this._value, stringComparison );

		public override int GetHashCode() =>
			base.GetHashCode();

		public override string ToString() => this._value;

		/// <summary>TRUE if the cursor is currently pointing to a region of the template where data resides.</summary>
		public bool InData => InsertPosition >= 0;

		#region Navigation
		/// <summary>Advance the cursor one word to the right (forward)</summary>
		/// <returns>A Point2 object for the new cursor position in the field.</returns>
		/// <remarks>Equivalent Key: CTRL-RIGHT-ARROW</remarks>
		public void GoWordRight()
		{
			int insertPosition = InsertPosition;
			if (insertPosition >= 0)
			{
				while ((++insertPosition < _value.Length) && Regex.IsMatch( _value.Substring( insertPosition, 1 ), @"[a-zA-Z0-9]", RegexOptions.Multiline )) ;
				while ((++insertPosition < _value.Length) && Regex.IsMatch( _value.Substring( insertPosition, 1 ), @"[^a-zA-Z0-9]", RegexOptions.Multiline )) ;
				InsertPosition = insertPosition;
			}
		}

		/// <summary>Move the cursor one word to the left (backwards).</summary>
		/// <remarks>Equivalent Key: CTRL-LEFT-ARROW</remarks>
		public void GoWordLeft()
		{
			int insertPosition = InsertPosition;
			if (insertPosition > 0)
			{
				// While the current position isn't a recognized word character...
				while ((--insertPosition > 0) && Regex.IsMatch( _value.Substring( insertPosition, 1 ), @"[^a-zA-Z0-9]", RegexOptions.Multiline )) ;
				// While the current position IS a recognized word character...
				while ((--insertPosition > 0) && Regex.IsMatch( _value.Substring( insertPosition, 1 ), @"[a-zA-Z0-9]", RegexOptions.Multiline )) ;

				InsertPosition = insertPosition;
			}
		}

		/// <summary>Delete one character to the right of the current cursor position.</summary>
		/// <remarks>Functionally equivalent to expected behaviour when DEL key is pressed.</remarks>
		/// <param name="insertPosition">Optional - can specify a different position from the current cursor position to delete from.</param>
		public void DelCharRight(int insertPosition = -1)
		{
			if (SelectionLength > 0)
			{
				_value = _value.Replace( SelectionStart, SelectionLength, "" );
				return;
			}

			if (insertPosition < 0) insertPosition = InsertPosition;
			if ((insertPosition >= 0) && (insertPosition < Length-1))
			{
				if (">\r\n".IndexOf( _value[ insertPosition ] ) > 0)
				{
					while (">\r\n".IndexOf( _value[ insertPosition ] ) > 0)
						_value = _value.MidString( 0, insertPosition ) + _value.MidString( insertPosition + 1 );
				}
				else
					_value = _value.MidString( 0, insertPosition ) + _value.MidString( insertPosition + 1 );
			}
		}

		/// <summary>Delete one character to the left of the current cursor position.</summary>
		/// <remarks>Functionally equivalent to expected behaviour when BKSP key is pressed.</remarks>
		/// <param name="insertPosition">Optional - can specify a different position from the current cursor position to delete from.</param>
		public void DelCharLeft(int insertPosition = -1)
		{
			if (SelectionLength > 0)
			{
				_value = _value.Replace( SelectionStart, SelectionLength, "" );
				return;
			}

			if (insertPosition < 0) insertPosition = InsertPosition;
			if (insertPosition > 0)
			{
				if (">\r\n".IndexOf( _value[ insertPosition ] ) > 0)
				{
					while (">\r\n".IndexOf( _value[ insertPosition ] ) > 0)
						_value = _value.MidString( 0, insertPosition - 1 ) + _value.MidString( insertPosition );
				}
				else
					_value = _value.MidString( 0, insertPosition - 1) + _value.MidString( insertPosition );
			}
		}

		/// <summary>Inserts a string at the current location. If text is selected, the data replaces the selected text, otherwise it's inserted.</summary>
		/// <param name="data">The data to insert.</param>
		public void Insert( string data )
		{
			int insertPosition = InsertPosition;
			if (SelectionLength > 0)
				_value = _value.Replace( SelectionStart, SelectionLength, data );
			else
			{
				if (CRW.CursorSize > 90) // Overwrite
					_value = (insertPosition + data.Length > Length) ? _value.Substring( 0, insertPosition ) + data : _value.Replace( insertPosition, data.Length, data);
				else // Insert
					_value = _value.MidString( 0, insertPosition ) + data + _value.MidString( insertPosition );
			}

			InsertPosition = insertPosition + data.Length;
		}
		#endregion
		#endregion
	}

	public class ScreenEditFieldManager
	{
		#region Properties
		/// <summary>Contains the data being manipulated by this field.</summary>
		protected ScreenEditStringManager _myData = null;
		#endregion

		#region Constructors
		/// <summary>Used to instantiate a multi-line (rectangle) input field.</summary>
		/// <param name="location">A Point2 object specifying where to place the field.</param>
		/// <param name="size">A Size object specifying the size of the field.</param>
		/// <param name="data">The content of the field (if any).</param>
		/// <param name="activeColor">The color to display the data in.</param>
		/// <param name="baseColor">The color of the non-data field elements.</param>
		/// <param name="selectionColor">The color of text when it's selected.</param>
		public ScreenEditFieldManager(Point2 location, Size size, string data = "", CliColor activeColor = null, CliColor baseColor = null, CliColor selectionColor = null) =>
			Initialize( location, size, data, activeColor, baseColor, selectionColor );

		/// <summary>Used to instantiate a multi-line (rectangle) input field.</summary>
		/// <param name="workArea">A Rectangle specifying the location and size of the field.</param>
		/// <param name="data">The content of the field (if any).</param>
		/// <param name="activeColor">The color to display the data in.</param>
		/// <param name="baseColor">The color of the non-data field elements.</param>
		/// <param name="selectionColor">The color of text when it's selected.</param>
		public ScreenEditFieldManager(Rectangle workArea, string data = "", CliColor activeColor = null, CliColor baseColor = null, CliColor selectionColor = null) =>
			Initialize( workArea.Location, workArea.Size, data, activeColor, baseColor, selectionColor );

		/// <summary>Used to instantiate a single line instance of this class.</summary>
		/// <param name="location">A Point2 object specifying where to place the field.</param>
		/// <param name="width">How wide is the field.</param>
		/// <param name="data">The content of the field (if any).</param>
		/// <param name="activeColor">The color to display the data in.</param>
		/// <param name="baseColor">The color of the non-data field elements.</param>
		/// <param name="selectionColor">The color of text when it's selected.</param>
		public ScreenEditFieldManager(Point2 location, int width, string data = "", CliColor activeColor = null, CliColor baseColor = null, CliColor selectionColor = null) =>
			Initialize( location, new Size( width, 1 ), data, activeColor, baseColor, selectionColor );
		#endregion

		#region Accessors
		/// <summary>Implements a quick/easy mechanism to determine if this field is a single line or a block.</summary>
		public bool IsSingleLine => _myData.IsSingleLine;

		/// <summary>Gets/Sets whether or not the field is a read-only field.</summary>
		public bool ReadOnly { get; set; } = false;

		/// <summary>Defines the maximum allowed length of the data for this control.</summary>
		/// <remarks>A negative value specifies no maximum length.</remarks>
		public int MaxLength { get; set; } = -1;

		/// <summary>Provides direct access (via string array of lines) to the data managed by the field.</summary>
		public string[] Lines
		{
			set => _myData.Lines = value;
			get => _myData.Lines;
		}

		/// <summary>The position of the cursor within the work area rectangle.</summary>
		/// <remarks>The values returned by this accessor are relative to the field itself, and NOT the screen/console buffer!</remarks>
		public Point2 CursorPosition
		{
			get => CRW.CursorPosition - Location;
			set => CRW.CursorPosition = value + Location;
		}

		/// <summary>Where is the field located.</summary>
		public Point2 Location => this._myData.Location;

		/// <summary>What is the size of the field.</summary>
		public Size Size => this._myData.Size;

		/// <summary>A single string value representing the data stored-in/managed-by the field.</summary>
		/// <remarks>Provides direct access (via a standalone string) to the data managed by this field.</remarks>
		public string MyData
		{
			get => _myData.Value;
			set => _myData.Value = value;
		}

		/// <summary>Gets/Sets that character that is used to display the template (non-data) area of the field.</summary>
		public char TemplateChar { get; set; } = '_';

		/// <summary>Returns the Length of the complete data string.</summary>
		public int Length => MyData.Length;

		/// <summary>Gets/Sets the value of the SelectionStart attribute.</summary>
		public int SelectionStart
		{
			get => _myData.SelectionStart;
			set => _myData.SelectionStart = Math.Min( Math.Max( -1, value ), Length );
		}

		/// <summary>Gets/Sets the value of the SelectionLength attribute.</summary>
		public int SelectionLength
		{
			get => SelectionStart < 0 ? 0 : Math.Min( Math.Max( -1, _myData.SelectionLength ), Length - SelectionStart );
			set
			{
				if (value < 1)
				{ _myData.SelectionLength = 0; _myData.SelectionStart = 0; }
				else
					_myData.SelectionLength = value > (Length - SelectionStart) ? Length - SelectionStart : _myData.SelectionLength = value;
			}
		}

		/// <summary>Returns the contents of the currently selected section of text. (or an empty string if nothing is selected).</summary>
		public string SelectedText => _myData.SelectedText;

		/// <summary>Gets/Sets the color used to draw the template/background elements of the field.</summary>
		public CliColor BaseColor { get; set; } = Con.DefaultColor;

		/// <summary>Gets/Sets the color used to draw the data elements of the field.</summary>
		public CliColor TextColor { get; set; } = Con.DefaultColor.Alt( ConsoleColor.White );

		/// <summary>Gets/Sets the color used to highlight selected text.</summary>
		public CliColor SelectionColor { get; set; } = Con.DefaultColor.Alt( ConsoleColor.White ).Inverse;

		public bool AtTop => IsSingleLine || (_myData.Box().Top == CRW.CursorPosition.Y);

		public bool AtBottom => IsSingleLine || (_myData.Box().Bottom == CRW.CursorPosition.Y);

		public bool AtLeft => _myData.Box().Left == CRW.CursorPosition.X;

		public bool AtRight => _myData.Box().Right == CRW.CursorPosition.X;

		public int InsertionIndex
		{
			get => this._myData.InsertPosition;
			set => this._myData.InsertPosition = value;
		}
		#endregion

		#region Methods
		/// <summary>Used internally to initialize the values for this object.</summary>
		private void Initialize(Point2 location, Size size, string data = "", CliColor activeColor = null, CliColor baseColor = null, CliColor selectionColor = null)
		{
			if (size.Width < 1) throw new InvalidDataException( "You can't create a zero-width field." );
			if (size.Height < 1) throw new InvalidDataException( "You can't create a field with no height." );
			if (
				(location.X < 0) ||
				(location.X + size.Width > Console.WindowWidth) ||
				(location.Y < 0) ||
				(location.Y + size.Height > Console.WindowHeight)
			   )
				throw new InvalidDataException( "The location of the field is invalid." );

			this._myData = new ScreenEditStringManager( location, size, data );
			this.BaseColor = baseColor is null ? Con.DefaultColor : baseColor;
			this.TextColor = activeColor is null ? Con.DefaultColor.Alt( ConsoleColor.White ) : activeColor;
			this.SelectionColor = selectionColor is null ? TextColor.Inverse : selectionColor;
		}

		#region Cursor Navigation Functions
		/// <summary>Moves the cursor to position 0 of the current line. On single line fields invokes GoHome() instead.</summary>
		/// <remarks>Equivalent Key: HOME KEY</remarks>
		/// <seealso cref="GoHome()"/>
		public void GoStartOfLine() =>
			CRW.CursorPosition.Subtract( CursorPosition.X );

		/// <summary>Moves the cursor to the top-left position of the input field.</summary>
		/// <remarks>Equivalent Key: CTRL-HOME</remarks>
		/// <seealso cref="GoStartOfLine()"/>
		public void GoHome() => 
			CRW.CursorPosition.Subtract( CursorPosition );

		/// <summary>Advances the cursor to the end of the data on the current line. On single line fields, invokes GoEnd() instead.</summary>
		/// <remarks>Equivalent Key: END KEY</remarks>
		/// <seealso cref="GoEnd()"/>
		public void GoEndOfLine()
		{
			string[] lines = _myData.Lines;
			if (CursorPosition.Y < lines.Length)
				CursorPosition = new Point2( lines[ CursorPosition.Y ].Length, CursorPosition.Y );
		}

		/// <summary>Advance the cursor to the end of the input data.</summary>
		/// <remarks>Equivalent Key: CTRL-END</remarks>
		/// <seealso cref="GoEndOfLine()"/>
		public void GoEnd() 
		{
			string[] lines = _myData.Lines;
			CursorPosition = new Point2( lines[ lines.Length - 1 ].Length, lines.Length - 1);
		}

		/// <summary>Advance the cursor one word to the right (forward)</summary>
		/// <returns>A Point2 object for the new cursor position in the field.</returns>
		/// <remarks>Equivalent Key: CTRL-RIGHT-ARROW</remarks>
		public void GoWordRight()
		{
			if (_myData.InData)
				_myData.GoWordRight();
		}

		/// <summary>Move the cursor one word to the left (backwards).</summary>
		/// <remarks>Equivalent Key: CTRL-LEFT-ARROW</remarks>
		public void GoWordLeft()
		{
			if (_myData.InData)
				_myData.GoWordLeft();
		}

		/// <summary>Move the cursor back one position.</summary>
		/// <param name="count">An int value specifying the number of positions to move left.</param>
		public void GoCharLeft(uint count = 1)
		{
			int insertPosition = _myData.InsertPosition;
			count = Math.Min( (uint)(Length - insertPosition), count );
			for (uint i = 0; i < count; i++)
				while ((--insertPosition > 0) && Regex.IsMatch( MyData.Substring( insertPosition, 1 ), @"[\r\n]", RegexOptions.Multiline )) ;

			_myData.InsertPosition = insertPosition;
		}

		/// <summary>Move the cursor forward one position.</summary>
		/// <param name="count">An int value specifying the number of positions to move right.</param>
		public void GoCharRight(uint count = 1)
		{
			int insertPosition = _myData.InsertPosition;
			count = Math.Min( (uint)_myData.InsertPosition, count );
			for (uint i = 0; i < count; i++)
				while ((++insertPosition < MyData.Length) && Regex.IsMatch( MyData.Substring( insertPosition, 1 ), @"[\r\n]", RegexOptions.Multiline )) ;

			_myData.InsertPosition = insertPosition;
		}

		public void GoLineUp(uint count = 1)
		{
			if (!IsSingleLine)
				while ((count-- > 0) && !AtTop)
					CRW.CursorPosition -= new Point2( 0, 1 );
		}

		public void GoLineDown(uint count = 1)
		{
			if (!IsSingleLine)
				while ((count-- > 0) && !AtBottom)
					CRW.CursorPosition += new Point2( 0, 1 );
		}
		#endregion

		/// <summary>Displays the full field + template, but without "flashing" the text. This is the preferred means of displaying the field.</summary>
		/// <param name="restoreCursor">If TRUE (default) puts the cursor back wherever it was found after the display operation completes.</param>
		/// <param name="selectedColor">The color to show selected text in. If null (default), the inverse of the defined TextColor is used.</param>
		/// <returns>The cursor position after the draw operation completes. Really only meaningful if the "restoreCursor" value is FALSE.</returns>
		public void Refresh(bool restoreCursor = true, CliColor selectedColor = null) =>
			_myData.Render( TextColor, selectedColor, BaseColor, TemplateChar, restoreCursor );

		/// <summary>Gives the field focus, refreshes the content and leaves the cursor at the end of the data.</summary>
		/// <returns>The cursor position after the operation completes.</returns>
		public void Focus() =>
			Refresh( false ); // Leaves the cursor at the end of the data

		/// <summary>Called when the control gains focus.</summary>
		public void Enter() =>
			this.Refresh();

		/// <summary>Called whenever the control loses focus.</summary>
		public void Leave()
		{
			this.SelectionLength = 0; // deselect any selected text
			this.Refresh();
		}

		/// <summary>Inserts a string into the data at the current InsertionPosition.</summary>
		/// <remarks>Automatically accomodates for selected text, or insert/overwrite.</remarks>
		/// <param name="what">The string to insert.</param>
		/// <param name="updateField">If TRUE (default), automatically updates the onscreen field with the new data.</param>
		public void Insert(string what, bool updateField = true)
		{
			_myData.Insert( what );

			if (updateField) this.Refresh();
		}

		/// <summary>Removes a specified section of data from the source string.</summary>
		/// <param name="start">Where to start cutting. Using a negative value here is equivalent to 0, or the start of the string.</param>
		/// <param name="size">How much to cut. NOTE: Using zero (or a negative value) here means "...to the end of the string".</param>
		/// <param name="updateField">If TRUE (default), automatically updates the onscreen field with the new data.</param>
		/// <returns>The deleted text, if any.</returns>
		public string DeleteAt(int start = -1, int size = -1, bool updateField = true)
		{
			string work = MyData, result = "";
			if ((start < 1) && (size < 0)) { MyData = ""; result = work; }

			if (result == "")
			{
				if (start < 0) start = 0;
				if (start < Length) // If the start value is beyond the end of the string, there's nothing to do...
				{
					size = (size < 1) ? Length - start : Math.Min( size, Length - start );

					if (start == 0)
					{
						result = MyData.Substring( 0, size );
						MyData = work.Substring( size );
					}
					else
					{
						result = work.Substring( start, size );
						MyData = work.Substring( 0, start ) + work.Substring( start + size );
					}
				}
			}
			if (updateField) { _myData.InsertPosition = start; Refresh(); }
			return result;
		}

		/// <summary>Selects the entire field.</summary>
		/// <param name="color">What color to show the selected text in. The default is the inverse of the defined TextColor.</param>
		/// <returns>The contents of the selected text.</returns>
		public string SelectAll(CliColor color = null) =>
			Select( -1, -1, color );

		/// <summary>Selects a portion of the text as defined by the Start and Size parameters.</summary>
		/// <param name="start">Where in the string to start. Any value less than one equates to 0.</param>
		/// <param name="size">How much text to select. Any value less than one equates to "the rest of the string".</param>
		/// <param name="color">What color to show the selected text in. The default is the inverse of the defined TextColor.</param>
		/// <returns>The contents of the selected text.</returns>
		public string Select(int start = -1, int size = -1, CliColor color = null)
		{
			if (color is null) color = TextColor.Inverse;
			string work = MyData, result = "";
			if ((start < 1) && (size < 1)) result = work;

			if (result == "")
			{
				if (start < 0) start = 0;
				if (start < Length) // If the start value is beyond the end of the string, there's nothing to do...
				{
					size = (size < 1) ? Length - start : Math.Min( size, Length - start );
					result = (start == 0) ? MyData.Substring( 0, size ) : work.Substring( start, size );
				}
			}

			_myData.SelectionStart = start;
			_myData.SelectionLength = size;
			Refresh(true, color);
			return result;
		}

		/// <summary>Replaces a specified region of the data string with another string.</summary>
		/// <param name="start">Where in the string to start. Any value less than one equates to 0.</param>
		/// <param name="replaceText">Text to replace the specified section with.</param>
		/// <returns>The section of text that was replaced.</returns>
		public string Replace(int start = -1, string replaceText = "") =>
			Replace( start, replaceText.Length, replaceText );

		/// <summary>Replaces a specified region of the data string with another string.</summary>
		/// <param name="start">Where in the string to start. Any value less than one equates to 0.</param>
		/// <param name="size">How much text to select. Any value less than one equates to "the rest of the string".</param>
		/// <param name="replaceText">Text to replace the specified section with.</param>
		/// <returns>The section of text that was replaced.</returns>
		public string Replace(int start = -1, int size = -1, string replaceText = "")
		{
			start = Math.Min( Length, Math.Max( start, 0 ) );
			string work = MyData;
			string result = work.MidString( start, size ); // Capture the text we're about to replace.
			if (start + size > work.Length) // if the new text is longer than the remainder of the string...
				work = work.Substring( 0, start ) + replaceText;
			else
				work = work.Replace( start, size, replaceText ); // Replace the text.

			MyData = work;
			Refresh(); // Update the field.
			return result;
		}

		/// <summary>Reports on whether a specified point lies within the bounds of this field's size.</summary>
		/// <param name="point">A Point2 value to test.</param>
		/// <returns>TRUE if the value of the supplied Point object lies within the SIZE of this field's work area.</returns>
		/// <remarks>This tests a point based on the home position of the field (0,0) and it's width and height. Valid points lie between
		/// zero and the width of the field across, and 0 and the height of the field up and down. </remarks>
		public bool Within(Point2 point) =>
			Size.ToRectangle().Contains( point );

		/// <summary>Reports on whether a specified point lies within the bounds of this field's size.</summary>
		/// <param name="x">An int value for the X co-ordinate to test.</param>
		/// <param name="y">An int value for the Y co-ordinate to test.</param>
		/// <returns>TRUE if the supplied co-ordinates lie within the SIZE of this field's work area.</returns>
		/// <remarks>This tests a point based on the home position of the field (0,0) and it's width and height. Valid points lie between
		/// zero and the width of the field across, and 0 and the height of the field up and down. </remarks>
		public bool Within(int x, int y) =>
			Size.ToRectangle().Contains( x, y );

		/// <summary>Reports on whether a specified cursor location point lies within the bounds of this field's work area.</summary>
		/// <param name="point">A Point value to test.</param>
		/// <returns>TRUE if the supplied Point lies within the defined _area of this field.</returns>
		public bool Contains(Point2 point) =>
			((Rectangle)_myData.Box()).Contains( point );

		/// <summary>Reports on whether the current cursor position lies within the bounds of this field's work area.</summary>
		/// <returns>TRUE if the supplied Point lies within the defined _area of this field.</returns>
		public bool Contains() =>
			Contains( CRW.CursorPosition );

		/// <summary>Reports on whether a supplied rectangle overlaps, or is overlapped-by, this rectangle.</summary>
		/// <param name="area">A rectangle to test.</param>
		/// <returns>TRUE if either area overlaps the other.</returns>
		public bool Overlaps(Rectangle area) =>
			((Rectangle)_myData.Box()).Overlaps( area );

		/// <summary>Reports on whether a supplied ScreenEditFieldManager area overlaps, or is overlapped-by, this one.</summary>
		/// <param name="field">A ScreenEditFieldManager object to test.</param>
		/// <returns>TRUE if either field manager object area overlaps the other.</returns>
		public bool Overlaps(ScreenEditFieldManager field) =>
			this.Overlaps( field._myData.Box() );

		/// <summary>Performs a backspace (del char left) operation on the string.</summary>
		/// <param name="count">The number of backspaces to implement.</param>
		public void Backspace( int count = 1)
		{
			if (_myData.InsertPosition > 0)
			{
				count = Math.Min( Math.Max( 1, count ), _myData.Length );
				for (int i = 0; i < count; i++)
					_myData.DelCharLeft();

				Refresh();
			}
		}

		/// <summary>Performs a delete (del char right) operation on the string.</summary>
		/// <param name="count">The number of deletions to perform.</param>
		public void DelCharRight( int count = 1)
		{
			if (_myData.InsertPosition < _myData.Length)
			{ 
				count = Math.Min( Math.Max( 1, count ), _myData.InsertPosition );
				for (int i = 0; i < count; i++)
					_myData.DelCharRight();

				Refresh();
			}
		}
		#endregion
	}

	public class ScreenEditFieldController
	{
		#region Properties
		protected string _myData = "";
		protected Rectangle _area;
		protected int _selectionStart = -1;
		protected int _selectionLength = -1;
		protected int _insertPosition = 0;
		#endregion

		#region Constructors
		/// <summary>Used to instantiate a multi-line (rectangle) input field.</summary>
		/// <param name="location">A Point2 object specifying where to place the field.</param>
		/// <param name="size">A Size object specifying the size of the field.</param>
		/// <param name="data">The content of the field (if any).</param>
		/// <param name="activeColor">The color to display the data in.</param>
		/// <param name="baseColor">The color of the non-data field elements.</param>
		/// <param name="selectionColor">The color of text when it's selected.</param>
		public ScreenEditFieldController(Point2 location, Size size, string data = "", CliColor activeColor = null, CliColor baseColor = null, CliColor selectionColor = null) =>
			Initialize( location, size, data, activeColor, baseColor, selectionColor );

		/// <summary>Used to instantiate a multi-line (rectangle) input field.</summary>
		/// <param name="workArea">A Rectangle specifying the location and size of the field.</param>
		/// <param name="data">The content of the field (if any).</param>
		/// <param name="activeColor">The color to display the data in.</param>
		/// <param name="baseColor">The color of the non-data field elements.</param>
		/// <param name="selectionColor">The color of text when it's selected.</param>
		public ScreenEditFieldController(Rectangle workArea, string data = "", CliColor activeColor = null, CliColor baseColor = null, CliColor selectionColor = null) =>
			Initialize( workArea.Location, workArea.Size, data, activeColor, baseColor, selectionColor );

		/// <summary>Used to instantiate a single line instance of this class.</summary>
		/// <param name="location">A Point2 object specifying where to place the field.</param>
		/// <param name="width">How wide is the field.</param>
		/// <param name="data">The content of the field (if any).</param>
		/// <param name="activeColor">The color to display the data in.</param>
		/// <param name="baseColor">The color of the non-data field elements.</param>
		/// <param name="selectionColor">The color of text when it's selected.</param>
		public ScreenEditFieldController(Point2 location, int width, string data = "", CliColor activeColor = null, CliColor baseColor = null, CliColor selectionColor = null) =>
			Initialize( location, new Size( width, 1 ), data, activeColor, baseColor, selectionColor );
		#endregion

		#region Accessors
		/// <summary>Implements a quick/easy mechanism to determine if this field is a single line or a block.</summary>
		public bool IsSingleLine => _area.Height == 1;

		/// <summary>Gets/Sets whether or not the field is a read-only field.</summary>
		public bool ReadOnly { get; set; } = false;

		/// <summary>Defines the maximum allowed length of the data for this control.</summary>
		/// <remarks>A negative value specifies no maximum length.</remarks>
		public int MaxLength { get; set; } = -1;

		/// <summary>Provides direct access (via string array of lines) to the data managed by the field.</summary>
		public string[] Lines
		{
			set => _myData = string.Join( "\r\n", value );
			get
			{
				if (IsSingleLine) return new string[] { _myData };
				return Breakup();
			}
		}

		/// <summary>The position of the cursor within the work area rectangle.</summary>
		/// <remarks>The values returned by this accessor are relative to the field itself, and NOT the screen/console buffer!</remarks>
		public Point2 CursorPosition
		{
			get => CRW.CursorPosition - Location;
			set => CRW.CursorPosition = value + Location;
		}

		/// <summary>References the position of the cursor within the central string (as opposed to screen position).</summary>
		public int InsertPosition
		{
			get => this._insertPosition;
			set
			{
				if ((value < 0) || (value > MyData.Length)) value = MyData.Length;
				this._insertPosition = value;
				CRW.CursorPosition = TranslateInsertPosition() + Location;
			}
		}

		/// <summary>Where is the field located.</summary>
		public Point2 Location => this._area.Location;

		/// <summary>What is the size of the field.</summary>
		public Size Size => this._area.Size;

		/// <summary>A single string value representing the data stored-in/managed-by the field.</summary>
		/// <remarks>Provides direct access (via a standalone string) to the data managed by this field.</remarks>
		public string MyData
		{
			get => _myData;
			set
			{
				_myData = (IsSingleLine ? Regex.Replace( value, @"[\r\n]+", " " ) : value);
				if ((MaxLength > 0) && (_myData.Length > MaxLength))
					_myData = _myData.Substring( 0, MaxLength );
			}
		}

		/// <summary>Gets/Sets that character that is used to display the template (non-data) area of the field.</summary>
		public char TemplateChar { get; set; } = '_';

		/// <summary>Returns the Length of the complete data string.</summary>
		public int Length => MyData.Length;

		/// <summary>Gets/Sets the value of the SelectionStart attribute.</summary>
		public int SelectionStart
		{
			get => _selectionStart;
			set => _selectionStart = Math.Min( Math.Max( -1, value ), Length );
		}

		/// <summary>Gets/Sets the value of the SelectionLength attribute.</summary>
		public int SelectionLength
		{
			get => _selectionStart < 0 ? 0 : Math.Min( Math.Max( -1, _selectionLength ), Length - SelectionStart );
			set
			{
				if (value < 1)
				{ _selectionLength = 0; _selectionStart = 0; }
				else
					_selectionLength = value > (Length - _selectionStart) ? Length - _selectionStart : _selectionLength = value;
			}
		}

		/// <summary>Returns the contents of the currently selected section of text. (or an empty string if nothing is selected).</summary>
		public string SelectedText
		{
			get
			{
				if (_selectionLength == 0) return "";

				if (_selectionStart < 1)
					_selectionStart = 0;

				if (_selectionStart + _selectionLength > Length)
					_selectionLength = Length - _selectionStart;

				return MyData.Substring( _selectionStart, _selectionLength );
			}
		}

		/// <summary>Gets/Sets the color used to draw the template/background elements of the field.</summary>
		public CliColor BaseColor { get; set; } = Con.DefaultColor;

		/// <summary>Gets/Sets the color used to draw the data elements of the field.</summary>
		public CliColor TextColor { get; set; } = Con.DefaultColor.Alt( ConsoleColor.White );

		/// <summary>Gets/Sets the color used to highlight selected text.</summary>
		public CliColor SelectionColor { get; set; } = Con.DefaultColor.Alt( ConsoleColor.White ).Inverse;

		public bool AtTop => CRW.CursorPosition.Y == _area.Top;

		public bool AtBottom => CRW.CursorPosition.Y == _area.Bottom;

		public bool AtLeft => CRW.CursorPosition.X == _area.Left;

		public bool AtRight => CRW.CursorPosition.X == _area.Right;
		#endregion

		#region Methods
		/// <summary>Used internally to initialize the values for this object.</summary>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if any of the coordinate values aren't valid.</exception>
		private void Initialize(Point2 location, Size size, string data = "", CliColor activeColor = null, CliColor baseColor = null, CliColor selectionColor = null)
		{
			if (size.Width < 1) throw new ArgumentOutOfRangeException( "You can't create a zero-width field." );
			if (size.Height < 1) throw new ArgumentOutOfRangeException( "You can't create a field with no height." );
			if (
				(location.X < 0) ||
				(location.X + size.Width > Console.WindowWidth) ||
				(location.Y < 0) ||
				(location.Y + size.Height > Console.WindowHeight)
			   )
				throw new ArgumentOutOfRangeException( "The location of the field is invalid." );

			this._area = location.ToRectangle( size );
			this._myData = string.IsNullOrEmpty( data ) ? "" : data;
			this.BaseColor = baseColor is null ? Con.DefaultColor : baseColor;
			this.TextColor = activeColor is null ? Con.DefaultColor.Alt( ConsoleColor.White ) : activeColor;
			this.SelectionColor = selectionColor is null ? TextColor.Inverse : selectionColor;
		}

		/// <summary>Parses a single string into a multi-line format according to it's contents.</summary>
		/// <param name="source">A string to parse.</param>
		/// <returns>An array of strings representing the lines of text as they'd be applied onscreen from the source string.</returns>
		private string[] Breakup(string source = null)
		{
			if (string.IsNullOrEmpty( source )) source = MyData;
			List<string> lines = new List<string>(); // source.Replace( "\r", "" ).Split( '\n' ) );

			// If there are CRLF's in the string...
			if ( Regex.IsMatch( source, @"[\r\n]" ) )
			{
				lines.AddRange( source.Replace( "\r", "" ).Split( '\n' ) );
				for ( int i = 0; i < lines.Count; i++ )
					if ( lines[ i ].Length > Size.Width )
					{
						string remainder = lines[ i ].Substring( Size.Width );
						lines[ i ] = lines[ i ].Substring( 0, Size.Width );
						if ( i == lines.Count - 1 )
							lines.Add( remainder );
						else
							lines[ i + 1 ] = remainder + lines[ i + 1 ];
					}

				// If there are too many lines, get rid of the extras...
				while ( lines.Count > Size.Height )
					lines.RemoveAt( Size.Height );

				return lines.ToArray();
			}

			return (source.Length < Size.Width) ? new string[] { source } : source.Split( Size.Width );
		}

		/// <summary>Gets a location on the screen (relative to the location of the field) that corresponds to the current Insert Position.</summary>
		/// <param name="position">An int specifying what position in the source string is to be translated.</param>
		/// <returns>A Point2 object specifying the correlated X,Y location of the cursor in the input field.</returns>
		/// <remarks>If position is omitted, or less than 0, the current InsertPosition is used instead.</remarks>
		protected Point2 TranslateInsertPosition(int position = -1)
		{
			position = position.InRange( MyData.Length, 0, NetXpertExtensions.Classes.Range.BoundaryRule.Loop ) ? position : (position < 0) ? InsertPosition : MyData.Length;
			if (position < 0) position = InsertPosition;
			if (IsSingleLine) return new Point2( InsertPosition, 0 );

			// Ensure any lonely \n's or \r's get paired up...
			string mydata = Regex.Replace( MyData, @"(?:([^\r\n])[\r\n]([^\r\n])|)", "$1\r\n$2" );

			// If the requested position is somewhere on the first line, we can send back a result now...
			if ( position < ((mydata.IndexOf( "\r\n" ) >= 0) ? Math.Min(this.Size.Width, MyData.IndexOf("\r\n")) : this.Size.Width) )
				return this.Location + new Point2( position, 0 );

			if ( Regex.IsMatch( mydata, @"[\r\n]", RegexOptions.Singleline ) )
			{
				string[] lines = Breakup( mydata.Substring( 0, position ) );
				int i = 0; Point2 search = new(0,0);
				while ( (search.X < position) )
				{
					if ( search.X + lines[search.Y].Length > position )
						return this.Location + new Point2(position - search.X, search.Y);

					search += new Point( lines[ search.Y ].Length, 1 );
				}
				return (i == position) ? search + Location : null; // Translate the search result by the upper left point of the control.
			}

			// There are no CRLF's in the string, so the translation is just a basic calculation...
			return this.Location + new Point2(mydata.Length % this.Size.Width, (int)Math.Floor( mydata.Length / (decimal)this.Size.Width));
			//return (lines.Length > 0) ? new Point2( lines[lines.Length-1].Length, lines.Length ) : new Point2(0,0);
		}
		#endregion
	}
}
