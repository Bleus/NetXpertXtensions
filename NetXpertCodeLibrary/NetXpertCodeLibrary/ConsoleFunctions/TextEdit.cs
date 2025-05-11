using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using NetXpertCodeLibrary.Extensions;

namespace NetXpertCodeLibrary.ConsoleFunctions
{
	// Used for storing / managing the data used in a TextEditField
	public sealed class TextEditFieldData
	{
		#region Properties
		string _value = "";
		int _selectionStart = -1;
		int _selectionLength = -1;
		int _lineLength = int.MaxValue;
		#endregion

		#region Constructors
		public TextEditFieldData(string value = "", int lineLength = -1)
		{
			_value = value;
			LineLength = lineLength;
		}

		public TextEditFieldData(string[] lines, int lineLength) =>
			ImportLines( lines, lineLength );
		#endregion

		#region Operators
		public static implicit operator string(TextEditFieldData data) => data._value;
		public static implicit operator TextEditFieldData(string data) => new TextEditFieldData( data );

		public static bool operator ==(TextEditFieldData left, TextEditFieldData right)
		{
			if (left is null) return (right is null) || string.IsNullOrEmpty( right._value ); ;
			if (right is null) return string.IsNullOrEmpty( left );
			return (left._value == right._value);
		}

		public static bool operator !=(TextEditFieldData left, TextEditFieldData right) => !(left == right);
		#endregion

		#region Accessors
		/// <summary>The length of the string managed by this object.</summary>
		public int Length => this._value.Length;

		/// <summary>Reports the number of lines represented by this object using the currently defined LineLength.</summary>
		public int Count => this.Lines.Length;

		public int LineLength
			{
			get => Math.Max( _lineLength, 1 );
			set => _lineLength = value < 1 ? int.MaxValue : value;
			}

		public int MaxLength { get; set; } = int.MaxValue;

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

		public string Value
		{
			get => this._value;
			set => this._value = Regex.Replace( value, @"[\t]", "".PadRight(TabSize, ' '));
		}

		public int TabSize { get; set; } = 4;

		public string this[ int index ]
		{
			get => ((index >= 0) && (index < Length)) ? _value[ index ].ToString() : "";
			set
			{
				if ((index >= 0) && (index < Length))
					_value = _value.MidString( 0, index - 1 ) + value + _value.MidString( index );
				else
					throw new IndexOutOfRangeException( "The specified index lies outside of the range of this data (" + index.ToString() + ")" );
			}
		}

		public string[] Lines
		{
			get => LineBreak();
			set => ImportLines( value );
		}
		#endregion

		#region Methods
		///<summary>Returns this data as a set of lines (uses CRLF, and the specified line length to break lines.</summary>
		///<param name="lineLength">Defines the maximum length of each line if CRLF doesn't occur first.</param>
		public string[] LineBreak( int lineLength = -1)
		{
			if (lineLength < 1) lineLength = _lineLength;
			string source = _value;
			List<string> lines = new List<string>();

			// If there are CRLF's in the string...
			if ( Regex.IsMatch( source, @"[\r\n]" ) )
			{
				lines.AddRange( source.Replace( "\r", "" ).Split( '\n' ) );
				for ( int i = 0; i < lines.Count; i++ )
					if ( lines[ i ].Length > lineLength )
					{
						string remainder = lines[ i ].Substring( lineLength );
						lines[ i ] = lines[ i ].Substring( 0, lineLength );
						if ( i == lines.Count - 1 )
							lines.Add( remainder );
						else
							lines[ i + 1 ] = remainder + lines[ i + 1 ];
					}

				return lines.ToArray();
			}

			return (source.Length < lineLength) ? new string[] { source } : source.Split( lineLength );
		}

		///<summary>Populates this object from an array of lines.</summary>
		///<param name="lines">An array of strings to derive the data from.</param>
		///<param name="lineLength">An optional int to specify the new default LineLength.</param>
		public void ImportLines(string[] lines, int lineLength = -1 )
		{
			_value = string.Join( "\r\n", lines );
			if (lineLength > 0) LineLength = lineLength;
		}

		/// <summary>Display the field.</summary>
		/// <param name="dataColor">Color for text.</param>
		/// <param name="selectionColor">Color for selected text.</param>
		/// <param name="templateColor">Color of the template (non-data area)</param>
		/// <param name="templateChar">The character to use to map out the template area.</param>
		/// <param name="restoreCursor">If TRUE (default), put the cursor back where it was before the render began.</param>
		/*
		public void Render(CliColor dataColor, CliColor selectionColor = null, CliColor templateColor = null, char templateChar = '_', bool restoreCursor = true)
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
				if (Console.CursorLeft == new Box( Location, Size ).Right) CRW.CursorPosition = new Point2( Location.X, Console.CursorTop + 1 ); // new line
			}

			if (restoreCursor) CRW.CursorPosition = curPos; // If restoration is specified, put the cursor back where we found it.
			CRW.CursorSize = curSize;                       // Turn the cursor back on.
		}
		*/

		public override bool Equals(object obj) =>
			base.Equals( obj );

		public bool Equals(string value, StringComparison stringComparison) =>
			value.Equals( this._value, stringComparison );

		public override int GetHashCode() =>
			base.GetHashCode();

		public override string ToString() => this._value;

		/// <summary>Delete one character to the right of the current cursor position.</summary>
		/// <remarks>Functionally equivalent to expected behaviour when DEL key is pressed.</remarks>
		/// <param name="position">Specifies the position to delete from.</param>
		/// <returns>The number of characters deleted. This may be greater than one if CRLF was the deleted character.</returns>
		public int DelCharRight(int position)
		{
			int amount = SelectionLength;
			if (SelectionLength > 0)
			{
				_value = _value.Replace( SelectionStart, SelectionLength, "" );
				SelectionLength = 0;
				return amount;
			}

			if ((position >= 0) && (position < Length))
			{
				if (">\r\n".IndexOf( _value[ position ] ) > 0)
				{
					amount = 0;
					while (">\r\n".IndexOf( _value[ position ] ) > 0)
					{
						_value = _value.MidString( 0, position ) + _value.MidString( position + 1 );
						amount++;
					}
				}
				else
				{
					_value = _value.MidString( 0, position ) + _value.MidString( position + 1 );
					amount = 1;
				}
			}
			return amount;
		}

		/// <summary>Delete one character to the left of the current cursor position.</summary>
		/// <remarks>Functionally equivalent to expected behaviour when BKSP key is pressed.</remarks>
		/// <param name="position">Specifies the position to delete from.</param>
		/// <returns>The number of characters deleted. This may be greater than one if CRLF was the deleted character.</returns>
		public int DelCharLeft(int position)
		{
			int amount = 0;
			if (_value.Length > 0)
			{
				if (SelectionLength > 0)
				{
					_value = _value.Replace( SelectionStart, SelectionLength, "" );
					SelectionLength = 0;
					return SelectionStart;
				}

				if ( position > _value.Length )
					position = _value.Length;

				amount = 0;
				if ( position == _value.Length )
				{
					_value = _value.Substring( 0, position - 1 );
					amount = 1;
				}
				else
					if ( position.InRange(_value.Length, 0, false) )
					{
						if (">\r\n".IndexOf( _value[ position ] ) > 0)
						{
							while (">\r\n".IndexOf( _value[ position ] ) > 0)
							{
								_value = _value.MidString( 0, position - 1 ) + _value.MidString( position );
								amount++;
							}
						}
						else
						{
							amount = 1;
							if ( position < _value.Length)
								_value = _value.MidString( 0, position - 1 ) + _value.MidString( position );
							else
								_value = _value.Substring( 0, _value.Length - 1 );
						}
					}
			}
			return amount;
		}

		/// <summary>Inserts a string at the current location. If text is selected, the data replaces the selected text, otherwise it's inserted.</summary>
		/// <param name="data">The data to insert.</param>
		public void Insert(int position, string data)
		{
			if (_value.Length == 0)
				_value = data;
			else
			{
				if (SelectionLength > 0)
				{
					_value = _value.Replace( SelectionStart, SelectionLength, data );
					SelectionLength = 0;
				}
				else
				{
					if (position >= _value.Length)
						_value += data;
					else
						if (CRW.CursorSize > 90) // Overwrite
						_value = (position + data.Length > Length) ? _value.Substring( 0, position ) + data : _value.Replace( position, data.Length, data );
					else // Insert
						_value = _value.MidString( 0, position ) + data + _value.MidString( position );
				}
			}
		}

		public string Replace( char find, char replace ) =>
			_value = _value.Replace( find, replace );

		public string Replace( string find, string replace ) =>
			_value = _value.Replace( find, replace );

		public string Replace( int start, int length, string with ) =>
			_value = _value.Replace( start, length, with );

		/// <summary>Constructs a Size object from a provided string.</summary>
		/// <param name="data">The string to build the Size from.</param>
		/// <seealso cref="TextEditField.StringArea(string, Point2)"/>
		public static Size StringArea(string data) => new Size( data.Length, 1 );
		#endregion
	}

	public class TextEditField
	{
		#region Parameters
		/// <summary>Defines where the prompt is to be displayed.</summary>
		protected Point2 _promptLocation;

		/// <summary>Manages the location and size of the input field.</summary>
		protected Rectangle _field;

		/// <summary>Manages the field data.</summary>
		protected TextEditFieldData _value;

		/// <summary>Stores the prompt text.</summary>
		protected string _prompt;

		/// <summary>Stores the InsertPosition</summary>
		protected int _position = 0;

		/// <summary>Manages the various colours used by this control.</summary>
		protected CliColor _baseColor = Con.DefaultColor;
		protected CliColor _dataColor = Con.DefaultColor;
		protected CliColor _activeColor = Con.DefaultColor;
		protected CliColor _selectedColor = Con.DefaultColor.Inverse;
		#endregion

		#region Constructors
		public TextEditField(Point2 promptLocation, string prompt, Point2 fieldLocation, Size fieldSize, string data, CliColor baseColor = null, CliColor dataColor = null, CliColor selectedColor = null, CliColor activeColor = null)
		{
			_promptLocation = promptLocation;
			_field = new Rectangle( fieldLocation, fieldSize );
			_value = new TextEditFieldData( data, _field.Width );
			Prompt = prompt;
			BaseColor = baseColor;
			ActiveColor = activeColor;
			DataColor = dataColor;
			SelectedColor = selectedColor;
		}

		public TextEditField(Point2 promptLocation, string prompt, Rectangle field, string data, CliColor baseColor = null, CliColor dataColor = null, CliColor selectedColor = null, CliColor activeColor = null)
		{
			_promptLocation = promptLocation;
			_field = field;
			_value = new TextEditFieldData( data, _field.Width );
			Prompt = prompt;
			BaseColor = baseColor;
			ActiveColor = activeColor;
			DataColor = dataColor;
			SelectedColor = selectedColor;
		}
		#endregion

		#region Operators
		public static implicit operator string(TextEditField data) => data._value;
		#endregion

		#region Accessors
		public string Value
		{
			get => _value.Value;
			set => _value.Value = IsSingleLine ? Regex.Replace( value, @"[\r\n]+", " " ) : value;
		}

		public string Prompt
		{
			get => this._prompt;
			set => this._prompt = value.EndsWith( ":" ) ? value : value + ":";
		}

		/// <summary>Defines the colour of the prompt when inactive, and the field template.</summary>
		public CliColor BaseColor
		{
			get => _baseColor;
			set => _baseColor = (value is null) ? Con.DefaultColor : value;
		}

		/// <summary>Defines the colour of data when displayed in the field.</summary>
		public CliColor DataColor 
		{
			get => _dataColor; 
			set => _dataColor = (value is null) ? Con.DefaultColor : value; 
		}

		/// <summary>Defines the colour of the prompt when the field is active.</summary>
		public CliColor ActiveColor 
		{
			get => _activeColor;
			set => _activeColor = (value is null) ? Con.DefaultColor : value;
		}

		/// <summary>Defines the colour of selected text in the field.</summary>
		public CliColor SelectedColor
		{
			get => _selectedColor;
			set => _selectedColor = (value is null) ? Con.DefaultColor.Inverse : value;
		}

		/// <summary>Defines what character will be used to display the field template.</summary>
		public char TemplateChar { get; set; } = '_';

		/// <summary>Refers to the screen-relative position of the data field.</summary>
		public Point2 Location
		{
			get => _field.Location;
			set => _field.Location = value;
		}

		/// <summary>Facilitates interaction with the location of the field Prompt.</summary>
		public Point2 PromptLocation
		{
			get => _promptLocation;
			protected set => _promptLocation = value;
		}

		/// <summary>Refers to the size of the data field.</summary>
		public Size Size
		{
			get => _field.Size;
			set
			{
				_field.Size = value;
				_value.LineLength = value.Width;
			}
		}

		public int SelectionStart
		{
			get => ReadOnly ? -1 : _value.SelectionStart;
			set => _value.SelectionStart = value;
		}

		public int SelectionLength
		{
			get => ReadOnly ? 0 : _value.SelectionLength;
			set => _value.SelectionLength = value;
		}

		public string SelectedText => ReadOnly ? "" : _value.SelectedText;

		/// <summary>Facilitates quick access to the (screen-relative) corners of the field area.</summary>
		public Corners Box => (Corners)this._field;

		/// <summary>Refers to the field-relative cursor position.</summary>
		/// <remarks>Use CRW.CursorPosition for screen-relative cursor location.</remarks>
		/// <seealso cref="CRW.CursorPosition"/>
		public Point2 CursorPosition
		{
			get => CRW.CursorPosition - Location;
			set => CRW.CursorPosition = (Location + value).Min( (Point2)Size );
		}

		/// <summary>References the position of the cursor within the central string (as opposed to screen position).</summary>
		/// <returns>An int that points to the location with the data string where newly added characters are to be inserted.</returns>
		/// <remarks>If the cursor indicates a position that is out of range, a value of -1 will be returned.</remarks>
		public int InsertPosition
		{
			get
			{
				int insertPosition = TranslateFieldPosition();
				return (_value.Length == 0) || (insertPosition < 0) ? _value.Length : insertPosition;

				//if ( !insertPosition.InRange( _value.Length, 0, null ) )
				//{
				//	if ( insertPosition < 0 ) return _value.Length;
				//	Point2 position = CursorPosition;
				//	if ( IsSingleLine ) return Math.Min( _value.Length, position.X ); // DGAF about "Y" on single line fields.

				//	string[] lines = _value.LineBreak( Size.Width );
				//	if ( (position.Y == lines.Length) && (position.X == 0) ) return _value.Length;
				//	if ( (position.Y > lines.Length) || (position.X > lines[ position.Y ].Length) ) return -1;

				//	if ( position.Y == 0 ) return position.X;

				//	int insertPosition = 0;
				//	for ( int i = 0; i < position.Y; i++ )
				//		insertPosition += lines[ i ].Length + ((lines[ i ].Length == Size.Width) ? 0 : 2); // +2 for "\r\n"

				//	insertPosition += position.X;
				//}
				//return insertPosition;
			}

			set => CRW.CursorPosition = TranslateInsertPosition( value );
		}

		/// <summary>If set to TRUE, changes to the field are not accepted/processed.</summary>
		public bool ReadOnly { get; set; } = false;

		/// <summary>TRUE if the field area is only one line in height.</summary>
		public bool IsSingleLine => _field.Height == 1;

		/// <summary>TRUE if the cursor is a block, otherwise FALSE.</summary>
		public bool InsertMode => CRW.CursorSize > 90;

		/// <summary>TRUE if the cursor currently resides anywhere within the field.</summary>
		public bool InField => _field.Contains( CRW.CursorPosition );

		/// <summary>TRUE if the cursor currently resides where data is displayed.</summary>
		public bool InData => InField && (InsertPosition >= 0);

		public bool AtTop => InField && CRW.CursorPosition.Y == Box.Top;

		public bool AtBottom => InField && CRW.CursorPosition.Y == Box.Bottom;

		public bool AtLeft => InField && CRW.CursorPosition.X == Box.Left;

		public bool AtRight => InField && CRW.CursorPosition.X == Box.Right;
		#endregion

		#region Methods
		/// <summary>Returns TRUE if this field's work area or prompt overlaps the provided field's work area or prompt.</summary>
		/// <param name="field">The TextEditField to check.</param>
		public bool Overlaps(TextEditField field) =>
			Overlaps( field._field ) || Overlaps( StringArea( field._promptLocation, field._prompt ) );

		public bool Overlaps(Rectangle area) =>
			_field.Overlaps( area ) || StringArea( _value, _promptLocation ).Overlaps( area );

		public bool Contains(Point2 point) => _field.Contains( point );

		public bool ContainsAny(Point2[] points) => _field.ContainsAny( points );

		public bool ContainsAll(Point2[] points) => _field.ContainsAll( points );

		/// <summary>Displays the field onscreen.</summary>
		/// <param name="restoreCursor">If TRUE (default), after drawing the field, put the cursor back where it was before drawing began.</param>
		public void Render( bool restoreCursor = true )
		{
			Point2 curPos = CRW.CursorPosition; // Save the current cursor location...
			Console.CursorVisible = false;      // Hide the cursor while we work...

			CRW.CursorPosition = _promptLocation;
			Con.Tec( "{$1}$2", new object[] { InField ? ActiveColor : BaseColor, _prompt } );

			if ( IsSingleLine )
			{
				CRW.WriteAt( "{$1}$2", Location, new object[] { DataColor, _value }, false );
				if ( _value.Length < Size.Width ) Con.Tec( "{$2,,>$1'_'}_", new object[] { Size.Width - _value.Length, BaseColor } );
			}
			else
			{
				for ( int i = 0; i < Size.Height; i++ )
					if ( i < _value.Count )
					{
						CRW.WriteAt( "{$1}$2", Location + new Point2( 0, i ), new object[] { DataColor, _value.Lines[ i ] } );
						if ( _value.Lines[ i ].Length < Size.Width )
							Con.Tec( "{$2,,>$1'_'}_", new object[] { Size.Width - _value.Lines[ i ].Length, BaseColor } );
					}
					else
						CRW.WriteAt( "{$1,,>$2'_'}_", Location + new Point2( 0, i ), new object[] { BaseColor, Size.Width } );
			}

			if ( restoreCursor) CRW.CursorPosition = curPos; // If restoration is specified, put the cursor back where we found it.
			Console.CursorVisible = true;                   // Restore the cursor...
		}

		/// <summary>Seeks out the closest valid insertion point and puts the cursor there.</summary>
		/// <returns>An int pointing to the location, within the field's linear string, equivalent to the selected location.</returns>
		public int FindNearestInsertPoint()
		{
			Console.CursorVisible = false;
			int i = InsertPosition;
			if ( i != TranslateFieldPosition() ) CRW.CursorPosition = TranslateInsertPosition( i );
			if (i < 0)
			{
				while ((InsertPosition < 0) && (CRW.CursorPosition.X >= Location.X) || (CRW.CursorPosition.Y >= Location.Y))
				{
					CRW.CursorPosition.Subtract( 1 );
					if (CRW.CursorPosition.X < Box.Left) CRW.CursorPosition = new Point2( Box.Right, Console.CursorTop - 1 );
				}
				i = InsertPosition;	
			}
			Console.CursorVisible = true;
			return i;
		}

		/// <summary>Gets a location on the screen (relative to the location of the field) that corresponds to the current Insert Position.</summary>
		/// <param name="position">An int specifying what position in the source string is to be translated.</param>
		/// <returns>A Point2 object specifying the correlated X,Y location of the cursor in the input field.</returns>
		/// <remarks>If position is omitted, or less than 0, the current InsertPosition is used instead.</remarks>
		protected Point2 TranslateInsertPosition( int position )
		{
			position = position.InRange( _value.Length, 0, true ) ? position : (position < 0) ? InsertPosition : _value.Length;

			if ( position < 0 ) position = InsertPosition;
			if ( IsSingleLine ) return new Point2( InsertPosition, 0 );

			// Ensure any lonely \n's or \r's get paired up...
			string mydata = Regex.Replace( _value, @"(?:([^\r\n])[\r\n]([^\r\n]))", "$1\r\n$2" );

			// If the requested position is somewhere on the first line, we can send back a result now...
			if ( position < ((mydata.IndexOf( "\r\n" ) >= 0) ? Math.Min( this.Size.Width, mydata.IndexOf( "\r\n" ) ) : this.Size.Width) )
				return this.Location + new Point2( position, 0 );

			if ( Regex.IsMatch( mydata, @"[\r\n]", RegexOptions.Singleline ) )
			{
				int i = 0; Point2 search = new Point2( 0, 0 );
				while ( (search.X < position) )
				{
					if ( search.X + _value.Lines[ search.Y ].Length > position )
						return this.Location + new Point2( position - search.X, search.Y );

					search += new Point( _value.Lines[ search.Y ].Length, 1 );
				}
				return (i == position) ? search + Location : null; // Translate the search result by the upper left point of the control.
			}

			// There are no CRLF's in the string, so the translation is just a basic calculation...
			return this.Location + new Point2( mydata.Length % this.Size.Width, (int)Math.Floor( mydata.Length / (decimal)this.Size.Width ) );
		}

		/// <summary>Translates a coordinate position within the field, to a string index.</summary>
		/// <param name="curPos">The screen location to translate. NULL (default value) means the current cursor position.</param>
		/// <returns>An int value pointing to the location in the parent string that translates to where the cursor is currently located.</returns>
		/// <remarks>If the cursor position is within the field, but beyond the length of the parent string, this returns the complement of the overage instead.</remarks>
		protected int TranslateFieldPosition( Point2 curPos = null )
		{
			if ( curPos is null ) curPos = CRW.CursorPosition;
			if ( _field.Contains( curPos ) )
			{
				curPos -= Location;
				if ( IsSingleLine ) return curPos.X;

				int result = (curPos.Y * Size.Width) + curPos.X;
				return result *= result > _value.Length ? -1 : 1;
			}
			return _value.Length;
		}

		/// <summary>Inserts a string into the current value.</summary>
		/// <param name="what">The string to insert.</param>
		/// <returns>TRUE if the insertion operation was a success.</returns>
		public bool Insert(string what)
		{
			bool result = false;
			if (!ReadOnly)
			{
				_value.Insert( FindNearestInsertPoint(), what );
				this.Render();
				result = true;
			}
			else
				System.Media.SystemSounds.Beep.Play();

			return result;
		}

		/// <summary>Deletes the character immediately to the left of the current cursor position.</summary>
		/// <returns>TRUE if the action was successful.</returns>
		public bool DelCharLeft()
		{
			bool result = false;
			if (!ReadOnly && (InsertPosition >= 0))
			{
				int adjust = _value.DelCharLeft( FindNearestInsertPoint() );
				CRW.CursorPosition -= new Point( adjust, 0 );
				this.Render();
				result = true;
			}
			else
				System.Media.SystemSounds.Beep.Play();
			return result;
		}


		public bool DelCharRight()
		{
			bool result = false;
			if (!ReadOnly && InsertPosition.InRange( _value.Length, 0, null ))
			{
				_value.DelCharRight( InsertPosition );
				this.Render();
				result = true;
			}
			else
				System.Media.SystemSounds.Beep.Play();
			return result;
		}

		public void GoEndOfLine()
		{
			if ( CursorPosition.Y < _value.Lines.Length )
				CRW.CursorPosition = new Point2( this.Box.Left + _value.Lines[ CursorPosition.Y ].Length, Console.CursorTop );
			else
				Console.CursorLeft = this.Box.Right;
		}

		public void GoStartOfLine() =>
			CRW.CursorPosition = new Point2( Box.Left, Console.CursorTop );

		public void GoCharLeft(int count)
		{
			while (count-- > 0)
				if (--CRW.CursorPosition < Box.Left)
				{
					if (CRW.CursorPosition.Y > Box.Top)
						CRW.CursorPosition = new Point2( Box.Right, CRW.CursorPosition.Y - 1 );
					else
						CRW.CursorPosition++;
				}
		}

		public void GoCharRight(int count)
		{
			while (count-- > 0)
				if (++CRW.CursorPosition > Box.Right)
				{
					if (CRW.CursorPosition.Y < Box.Bottom)
						CRW.CursorPosition = new Point2( Box.Left, CRW.CursorPosition.Y + 1 );
					else
						CRW.CursorPosition--;
				}
		}

		protected static Rectangle StringArea( string data, Point2 location = null)
		{
			if (location is null) location = new Point( 0, 0 );
			return new Rectangle( location, new Size( data.Length, 1 ) );
		}
		#endregion
	}
}
