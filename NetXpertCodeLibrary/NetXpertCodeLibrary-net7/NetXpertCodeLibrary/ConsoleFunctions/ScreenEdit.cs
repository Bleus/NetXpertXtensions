using NetXpertExtensions;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace NetXpertCodeLibrary.ConsoleFunctions
{
	public abstract class ScreenEditDataPrototype
	{
		#region Properties
		string _fieldName = "";
		Type _type = typeof( object );
		#endregion

		#region Constructors
		public ScreenEditDataPrototype(string name, string rawValue = "", Type type = null )
		{
			this.Name = name;
			this._type = type is null ? typeof( string ) : type;
			if ( string.IsNullOrWhiteSpace( this._fieldName ) )
				throw Language.Prompt.GetException( 0 );
				//new Exception( "You must provide a valid name to instantiate this object!" );

			this.RawValue = this.RevertValue = rawValue;
		}
		#endregion

		#region Accessors
		public string Name
		{
			get => this._fieldName;
			set
			{
				if (!string.IsNullOrWhiteSpace( value ) && Regex.IsMatch( value, @"^[a-z][a-zA-Z_0-9]*[a-zA-Z0-9]$" ))
					this._fieldName = value;
			}
		}

		public Type Type => _type;

		protected string RawValue { get; set; } = "";

		public string RevertValue { get; private set; } = "";
		#endregion

		#region Operators
		public static implicit operator KeyValuePair<string, string>(ScreenEditDataPrototype source) => 
			new KeyValuePair<string, string>( source.Name, source.RawValue );
		#endregion

		#region Methods
		public dynamic ToDerivedClass() =>
			InstantiateDerivedType( this );

		public static dynamic InstantiateDerivedType(string name, Type type, string value = "")
		{
			Type maker = typeof( ScreenEditData<> ).MakeGenericType( new Type[] { type } );
			dynamic result = Activator.CreateInstance( maker, new object[] { new KeyValuePair<string, string>( name, value ) } );
			return result;
		}

		public static dynamic InstantiateDerivedType( ScreenEditDataPrototype prototype ) =>
			InstantiateDerivedType( prototype.Name, prototype.Type, prototype.RawValue );

		public T As<T>() => (T)Convert.ChangeType( this.RawValue, typeof( T ) );

		public abstract void Revert();
		#endregion
	}

	public class ScreenEditData<T> : ScreenEditDataPrototype
	{
		#region Constructors
		public ScreenEditData( string name, T value ) : base( name, value.ToString(), typeof(T) ) =>
			this.RawValue = value.ToString();

		// Removed because it's ambiguous with the prior Constructor when the type of T is a String
		//public ScreenEditData(string name, string rawValue) : base( name, rawValue ) { }

		public ScreenEditData(KeyValuePair<string, string> data) : base( data.Key, data.Value, typeof(T) ) { }
		#endregion

		#region Accessors
		public T Value
		{
			get => As<T>();
			set => this.RawValue = value.ToString();
		}

		new public T RevertValue => (T)Convert.ChangeType( base.RevertValue, typeof( T ) );
		#endregion

		#region Operators
		public static implicit operator KeyValuePair<string, T>(ScreenEditData<T> source) => new KeyValuePair<string, T>( source.Name, source.Value );
		public static implicit operator ScreenEditData<T>(KeyValuePair<string, T> source) => new ScreenEditData<T>( source.Key, source.Value );
		public static implicit operator ScreenEditData<T>(KeyValuePair<string, string> source) => new ScreenEditData<T>( source );
		#endregion

		#region Methods
		public override void Revert() => this.Value = this.RevertValue;
		#endregion
	}

	public class ScreenEditDataCollection : IEnumerator<ScreenEditDataPrototype>
	{
		#region Properties
		protected List<ScreenEditDataPrototype> _fields = new List<ScreenEditDataPrototype>();
		protected int _position = 0;
		#endregion

		#region Constructors
		public ScreenEditDataCollection() { }

		public ScreenEditDataCollection(ScreenEditDataPrototype data) =>
			this.Add( data );

		public ScreenEditDataCollection(ScreenEditDataPrototype[] data) =>
			this.AddRange( data );
		#endregion

		#region Accessors
		public int Count => this._fields.Count;

		public ScreenEditDataPrototype this[ int index ] => this._fields[ index ];

		public ScreenEditDataPrototype this[ string name ] => HasItem( name ) ? this._fields[ IndexOf( name ) ] : null;

		// IEnumerator support
		ScreenEditDataPrototype IEnumerator<ScreenEditDataPrototype>.Current => this._fields[ this._position ];

		object IEnumerator.Current => this._fields[ this._position ];
		#endregion

		#region Methods
		protected int IndexOf(string name)
		{
			int i = -1; while ((++i < this.Count) && !name.Equals( this._fields[ i ].Name, StringComparison.OrdinalIgnoreCase )) ;
			return (i < Count) ? i : -1;
		}

		protected int IndexOf(ScreenEditDataPrototype item) =>
			IndexOf( item.Name );

		public bool HasItem(string name) => IndexOf( name ) >= 0;

		public void Add(ScreenEditDataPrototype item)
		{
			int i = IndexOf( item );
			if (i < 0)
				this._fields.Add( item );
			else
				this._fields[ i ] = item;
		}

		public void AddRange(ScreenEditDataPrototype[] items)
		{
			foreach (ScreenEditDataPrototype item in items)
				this.Add( item );
		}

		public void Remove(string name)
		{
			int i = IndexOf( name );
			if (i >= 0)
				this._fields.RemoveAt( i );
		}

		public void RemoveAt(int index) =>
			this._fields.RemoveAt( index );

		//IEnumerator Support
		public IEnumerator<ScreenEditDataPrototype> GetEnumerator() =>
			this._fields.GetEnumerator();

		bool IEnumerator.MoveNext() =>
			++this._position < this.Count;

		void IEnumerator.Reset() =>
			this._position = 0;

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

	/*
	public abstract class ScreenEditFieldPrototype
	{
		#region Properties
		protected Type _dataType = typeof( Object );
		protected string _name = "";
		protected string _dataName = "";
		protected Point _location;
		protected Rectangle _workArea;
		protected CliColor _labelColor = Con.DefaultColor;
		protected CliColor _dataColor = Con.DefaultColor;
		protected dynamic _data;
		protected int _selectionStart = -1;
		protected int _selectionLength = -1;
		#endregion

		#region Constructor
		public ScreenEditFieldPrototype(string name, dynamic data, Point location, Rectangle workArea, string dataName = "", CliColor dataColor = null, CliColor labelColor = null)
		{
			if (!string.IsNullOrWhiteSpace(dataName))
				this.DataName = dataName;

			this.Name = name;
			this.DataType = data.GetType();
			this._data = data;
			this.WorkArea = workArea;
			this.PromptLocation = location;
			this.LabelColor = labelColor;
			this.DataColor = dataColor;
		}
		#endregion

		#region Accessors
		public dynamic Data
		{
			get => this._data;
			set { if (value.GetType() == this._dataType) this._data = value; }
		}

		/// <summary>Endeavours to build a coherent string from the available onscreen information.</summary>
		protected string MyScreenValue
		{
			get
			{
				string result = "";
				foreach (string s in ScreenEditController.ReadScreen( WorkArea ))
				{
					// Remove all trailing underscores from the line if present.
					string work = Regex.IsMatch( s, @"[_]+$" ) ? Regex.Replace( s, @"([_]*)$", "" ) : s;
					// If the remaining string is shorter than the line limit, insert a CRLF...
					result += work + ((work.Length == WorkArea.Width) ? "" : "\r\n");
				}
				// Trim excess CRLF's off the end of the line...
				return result.TrimEnd( new char[] { '\r', '\n' } );
			}
			set => DisplayValue( value );
		}

		/// <summary>Reports the current Insert condition.</summary>
		/// <remarks>Instead of defining and managing a boolean value across several classes, I simply let the current CursorSize tell me!</remarks>
		protected bool InsertMode => (Console.CursorSize < 100);

		/// <summary>The current position of the cursor within the scope of the data (string), as opposed to on the screen.</summary>
		/// <remarks>This collates the string from the screen, and parses it to determine where, within the content itself, the current cursor position currently points.</remarks>
		protected int InsertionIndex
		{
			get
			{
				if (IsActive)
				{
					string myValue = MyScreenValue;
					Point saveCurPos = WorkArea.Location.Add( 0 );
					int i = -1;
					while ((++i < myValue.Length) && (CursorPos != saveCurPos))
						switch (myValue[ i ])
						{
							case '\r':
								saveCurPos = new Point( WorkArea.X, saveCurPos.Y );
								break;
							case '\n':
								saveCurPos = saveCurPos.Add( 0, 1 );
								break;
							default:
								saveCurPos = (saveCurPos.X < WorkArea.Right)
										   ? saveCurPos.Add( 1 )
										   : new Point( WorkArea.X, saveCurPos.Y + 1 );
								break;
						}
					return i;
				}
				return -1;
			}

			set
			{
				string myValue = MyScreenValue;
				if (value < 0) value = myValue.Length;
				value = Math.Min( value, myValue.Length );
				int i = -1; CursorPos = WorkArea.Location;
				while (++i < value)
					switch (myValue[ i ])
					{
						case '\r':
							Console.CursorLeft = WorkArea.X;
							break;
						case '\n':
							if (CursorPos.Y < WorkArea.Bottom)
								Console.CursorTop += 1;
							break;
						default:
							if (Console.CursorLeft < WorkArea.Right)
								Console.CursorLeft += 1;
							else
								Console.SetCursorPosition( WorkArea.X, Math.Min( WorkArea.Bottom, Console.CursorTop + 1 ) );
							break;
					}
			}
		}

		public Rectangle WorkArea
		{
			get => this._workArea;
			set
			{
				// Prevent out-of-bounds value assignment
				this._workArea = new Rectangle(
					Math.Max( 0, value.Left ),
					Math.Max( 0, value.Top ),
					Math.Min( Console.WindowWidth, value.Width ),
					Math.Min( Console.WindowHeight, value.Height )
				);
			}
		}

		public string Name
		{
			get => this._name;
			set
			{
				if (!string.IsNullOrWhiteSpace( value ) && string.IsNullOrEmpty( _name ))
				{
					this._name = value;
					if (string.IsNullOrEmpty( this._dataName ))
						this.DataName = "";
				}
				else
					throw new Exception( "The supplied 'name' value cannot be null, empty or whitespace." );
			}
		}

		public virtual Type DataType
		{
			get => this._dataType;
			set
			{
				switch (value.FullName)
				{
					case "System.Boolean":
					case "System.String":
					case "System.UInt64":
					case "System.UInt32":
					case "System.UInt16":
					case "System.Int64":
					case "System.Int32":
					case "System.Int16":
					case "System.DateTime":
					case "System.Decimal":
						this._dataType = value;
						break;
					default:
						Type[] types = GetAllDerivedTypes();
						int i = -1; while ((++i < types.Length) && !types[ i ].Name.Equals( value.Name + "ScreenEditField" )) ;
						if (i < types.Length)
							this._dataType = value;
						else
							throw new Exception( "Invalid Data Type Specified: \"" + value.FullName + "\"" );
						break;
				}
			}
		}

		public string DataName
		{
			get => this._dataName;
			set
			{
				if (string.IsNullOrEmpty( value ) && !string.IsNullOrEmpty( Name ))
				{
					value = String.Join( "", Regex.Split( Name.UCWords( true ), @"[^\w]+" ) );
					value = Char.ToLowerInvariant( value[ 0 ] ) + value.Substring( 1 );
				}

				if (!string.IsNullOrWhiteSpace( value ))
				{
					if (string.IsNullOrEmpty( _dataName ) && Regex.IsMatch( value, @"[a-z][a-z_0-9]*[a-z0-9]", RegexOptions.IgnoreCase ))
						this._dataName = value;
				}
				else
					throw new Exception( "The supplied 'dataName' value cannot be null, empty or whitespace." );
			}
		}

		/// <summary>Reports how many characters long the data stream can be.</summary>
		public int DataLength => FieldLines * FieldLength;

		/// <summary>Reports how many characters there are per line.</summary>
		public int FieldLength => this._workArea.Width;

		/// <summary>Reports how many lines are managed by the control.</summary>
		public int FieldLines => this._workArea.Height;

		/// <summary>Holds the location of where the prompt will be printed.</summary>
		public Point PromptLocation
		{
			get => this._location;
			set
			{
				if ((value.X >= 0) && (value.X < Console.WindowWidth - Name.Length) && (value.Y >= 0) && (value.Y < Console.WindowHeight))
					this._location = value;
				else
					throw new Exception( "The supplied co-ordinates are invalid." );
			}
		}

		/// <summary>Provides an easy means to capture/access/set the current (Window-relative) cursor position.</summary>
		public Point CursorPos
		{
			get => new Point( Console.CursorLeft, Console.CursorTop );
			protected set
			{
				if ((value.X >= this._workArea.Left) && (value.X < WorkArea.Right) && (value.Y >= this._workArea.Top) && (value.Y < WorkArea.Bottom))
					Console.SetCursorPosition( value.X, value.Y );
			}
		}

		public Point AbsPos
		{
			get => new Point( Console.CursorLeft - WorkArea.X, Console.CursorTop - WorkArea.Y );
			protected set
			{
				value = value.Add( WorkArea.X, WorkArea.Y );
				Console.SetCursorPosition( Math.Min( WorkArea.Right, value.X ), Math.Min( WorkArea.Bottom, value.Y ) );
			}
		}

		public CliColor LabelColor
		{
			get => this._labelColor;
			set => this._labelColor = value is null ? Con.DefaultColor : value;
		}


		public CliColor DataColor
		{
			get => this._dataColor;
			set => this._dataColor = value is null ? Con.DefaultColor : value;
		}

		public bool IsActive => Contains( CursorPos );

		/// <summary>Any characters matching the defined pattern here will be rejected.</summary>
		public Regex FilterPattern { get; set; } = null;

		public Regex ValidationPattern { get; set; } = null;

		public bool RealTimeValidation { get; set; } = true;
		#endregion

		#region Operators
		public static bool operator ==(ScreenEditFieldPrototype left, ScreenEditFieldPrototype right)
		{
			if (left is null) return (right is null);
			if (right is null) return false;
			return (left.DataName.Equals( right.DataName, StringComparison.OrdinalIgnoreCase ) && (left.DataType == right.DataType));
		}

		public static bool operator !=(ScreenEditFieldPrototype left, ScreenEditFieldPrototype right) => !(left == right);

		public static implicit operator string(ScreenEditFieldPrototype data) => data._data.ToString();
		#endregion

		#region Methods
		public bool Contains(Point point) =>
			this.WorkArea.ContainsAny( new Point[] { point, point.Add( -1 ) } );

		public virtual void Write()
		{
			ScreenEditController.WriteAt( _location, "{$1}$2:", new object[] { _labelColor, _name } );
			string work = Regex.Replace( _data.ToString(), @"[^\x20-\x7e\r\n\t]", "" ); // Remove all non-printable characters except \r, \n and \t

			DisplayValue( work, false );
		}

		protected void DisplayValue(string value, bool restoreCursor) =>
			DisplayValue( value, -1, restoreCursor );

		/// <summary>Takes a string, and outputs it to the screen according to the format of the defined data template.</summary>
		/// <param name="value">The data to write.</param>
		/// <param name="toPosition">If provided, tells the routine to only print to this position of the string and then stop.</param>
		/// <param name="restoreCursor">If TRUE (default), the cursor position is stored before writing, and then restored afterwards.</param>
		protected void DisplayValue(string value, int toPosition = -1, bool restoreCursor = true)
		{
			Point saveCurPos = CursorPos; // Save the current cursor position.
			if (value is null) value = _data.ToString();
			if (toPosition < 0) toPosition = value.Length;

			// Write out the template lines...
			//for (int y = 0; y < FieldLines; y++)
			//	ScreenEditController.WriteAt( WorkArea.Location.Add( 0, y ), "{$1}$2", new object[] { LabelColor, "".PadRight( FieldLength, '_' ) } );

			// If realtime validation is active, checks the data and adjusts the colour accordingly...
			CliColor color = RealTimeValidation ? (PatternMatch( value ) ? DataColor : DataColor.Alt( ConsoleColor.Black, ConsoleColor.Red )) : DataColor;

			// If "toPosition" is 0, there's nothing to do!
			if (toPosition > 0)
			{
				// If the depth is only one line, this is easy(ish)!
				if (FieldLines == 1)
				{
					int limit = Math.Min( toPosition, FieldLength );
					if (value.Length > limit) value = value.Substring( 0, limit );
					if (value.Length > 0)
					{
						if (_selectionLength < 1)
							ScreenEditController.WriteAt( WorkArea.Location, "{$1}$2{$3}$4 ", new object[] { color, value, LabelColor, "".PadRight( WorkArea.Width - value.Length, '_') } );
						else
						{
							if (_selectionStart > 0) // Text before the selection
								ScreenEditController.WriteAt( WorkArea.Location, "{$1}$2", new object[] { color, value.Substring( 0, _selectionStart ) } );

							// Selected text
							ScreenEditController.WriteAt( WorkArea.Location.Add( _selectionStart ), "{$1}$2", new object[] { color.Inverse, value.Substring( _selectionStart, SelectionLength ) } );

							// Text after the selection
							if (_selectionStart + SelectionLength < MyScreenValue.Length)
								ScreenEditController.WriteAt( WorkArea.Location.Add( _selectionStart + SelectionLength ), "{$1}$2", new object[] { color, value.Substring( _selectionStart + SelectionLength ) } );

							// Remaining template if needed...
							if (CursorPos.X < WorkArea.Right)
								Con.Tec( "{$1}$2", new object[] { LabelColor, "".PadRight( WorkArea.Right - CursorPos.X, '_' ) } );
						}
					}
					if (restoreCursor) CursorPos = saveCurPos; // Restore the Cursor Position
				}
				else // Not so easy for multiple lines..
				{
					CursorPos = WorkArea.Location; // start at the beginning....
					for (int i = 0; i < Math.Min( value.Length, toPosition ); i++)
					{
						if ((CursorPos.X < WorkArea.Right) || (CursorPos.Y < WorkArea.Bottom))
							switch (value[ i ])
							{
								case '\r': // Carriage return - goto beginning of the current line...
									if (CursorPos.X < WorkArea.Right)
										Con.Tec( "{$1}$2", new object[] { LabelColor, "_".PadRight( WorkArea.Right - CursorPos.X, '_' ) } );
									Console.SetCursorPosition( WorkArea.X, Console.CursorTop );
									break;
								case '\n': // Newline - drop down a line, keep column...
									if (Console.CursorTop < WorkArea.Bottom)
										Console.CursorTop += 1;
									break;
								default:
									Con.Tec( "{$1}$2", new object[] { (i < SelectionStart) || (i > SelectionStart + SelectionLength) ? color : color.Inverse, value[ i ] } );
									break;
							}

						if (CursorPos.X >= WorkArea.Right)
						{
							if (CursorPos.Y < WorkArea.Bottom)
								Console.SetCursorPosition( WorkArea.X, CursorPos.Y + 1 );
							else break;
						}
					}
					if (!restoreCursor) saveCurPos = CursorPos.Add(1);
					if (CursorPos.X < WorkArea.Right)
						Con.Tec( "{$1}$2", new object[] { LabelColor, "".PadRight( WorkArea.Right - CursorPos.X, '_' ) } );

					if (CursorPos.Y < WorkArea.Bottom)
					{
						ScreenEditController.Goto( WorkArea.Left, CursorPos.Y + 1 );
						for (int y = CursorPos.Y; y < WorkArea.Bottom; y++)
							ScreenEditController.WriteAt( "{$1,rn}$2", WorkArea.Left, y, new object[] { LabelColor, "".PadRight( WorkArea.Width, '_' ) } );
					}
					CursorPos = saveCurPos;
				}
			}
		}

		public void Focus() => CursorPos = WorkArea.Location;

		public virtual dynamic ReadValue() =>
			ScreenEditDataPrototype.InstantiateDerivedType( this._dataName, this._dataType, MyScreenValue );

		public abstract void ProcessKeyStroke(ConsoleKeyInfo keyPressed);

		public override bool Equals(object obj) =>
			base.Equals( obj );

		public override int GetHashCode() =>
			base.GetHashCode();

		/// <summary>Returns true if there's no filter pattern defined, or the passed string isn't matched by the one defined.</summary>
		/// <param name="test">A string (typically a single character) to test.</param>
		/// <returns>TRUE if there's no filter pattern defined, or the passed string isn't matched by it. A TRUE result means that the subject string has been rejected!</returns>
		/// <seealso cref="FilterPattern"/>
		public bool InFilter(string test) =>
			!(FilterPattern is null) && FilterPattern.IsMatch( test );

		/// <summary>Returns TRUE if there's no validation pattern defined, OR the passed string matches a defined pattern.</summary>
		/// <param name="test">The string to test.</param>
		/// <returns>TRUE if there's no validation pattern defined, OR the passed string matches a defined pattern.</returns>
		/// <seealso cref="ValidationPattern"/><seealso cref="RealTimeValidation"/>
		public bool PatternMatch(string test) =>
			(ValidationPattern is null) || ValidationPattern.IsMatch( test );

		public abstract bool IsValidData();

		public static dynamic CreateTypedInstance(ScreenEditFieldPrototype source)
		{
			Type maker = typeof( ScreenEditField<> ).MakeGenericType( new Type[] { source.DataType } );
			return Activator.CreateInstance( maker, new object[] { source.Name, source.Data, source._location, source.WorkArea, source.DataName, source.DataColor, source.LabelColor } );
		}

		public static TypeInfo[] GetAllDerivedTypes(string assemblyName = "*", string nameSpace = "*")
		{
			// Get a collection of all Assemblies in the current collection.
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			bool ValidExtensionName(string value) =>
				!Regex.IsMatch( value, @"(U?Int(8|16|32|64)|Bool(ean)?|DateTime|Decimal|String)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture );

			if (assemblyName.Equals( "*" )) // Look in them all...
			{
				List<TypeInfo> classes = new List<TypeInfo>();
				foreach (Assembly assembly in assemblies)
					foreach (TypeInfo ti in assembly.DefinedTypes)
						if (((nameSpace == "*") || ti.Namespace.Equals( nameSpace )) && Regex.IsMatch(ti.Name, @"[a-zA-Z0-9]+ScreenEditField$") && ValidExtensionName( ti.Name ))
							classes.Add( ti );

				return classes.ToArray();
			}
			else
			{
				// Search through the list of known assemblies for the one specified...
				int i = -1; while ((++i < assemblies.Length) && !assemblies[ i ].FullName.Substring( 0, assemblyName.Length + 1 ).Equals( assemblyName + "," )) ;
				if (i < assemblies.Length)
				{
					List<TypeInfo> classes = new List<TypeInfo>();
					foreach (TypeInfo ti in assemblies[ i ].DefinedTypes)
						if (((nameSpace == "*") || ti.Namespace.Equals( nameSpace )) && ValidExtensionName( ti.Name ))
							classes.Add( ti );

					return classes.ToArray();
				}
			}
			return new TypeInfo[] { };
		}
		#endregion
	}
	*/

	public class ScreenEditFieldLookupData
	{
		#region Properties
		protected string _value = "";
		protected ulong _recId = ulong.MaxValue;
		#endregion

		#region Constructors
		protected ScreenEditFieldLookupData() { }

		public ScreenEditFieldLookupData(string value, ulong id)
		{
			this.Value = value;
			this.Id = id;
		}
		#endregion

		#region Accessors
		public string Value
		{
			get => _value;
			set
			{
				if ( !string.IsNullOrWhiteSpace( value ) )
					_value = value;
				else
					throw Language.Prompt.GetException( 0 );
					//new ArgumentNullException( "You can't use an empty string, or one containing only whitespace." );
			}
		}

		public ulong Id
		{
			get => (this._recId == ulong.MaxValue) ? 0 : this._recId;
			set => this._recId = ((this._recId == ulong.MaxValue) && (value > 0) ? value : ulong.MaxValue);
		}
		#endregion

		#region Operators
		public static implicit operator ScreenEditFieldLookupData(string value) => new ScreenEditFieldLookupData() { Value  = value };
		public static implicit operator string(ScreenEditFieldLookupData data) => data.Value;
		#endregion
	}

	/// <summary>Provides a common root class that all descendants can be referenced by, regardless of their <T> designation.</summary>
	public abstract class ScreenEditFieldPrototype
	{
		#region Properties
		protected Type _dataType = typeof( Object );
		protected string _dataName = "";
		protected dynamic _data;
		private dynamic _revertData;
		protected TextEditField _field;
		protected string _dbName = "";
		#endregion

		#region Constructor
		public ScreenEditFieldPrototype(string title, dynamic data, Point2 location, Rectangle workArea, string dataName = "", CliColor dataColor = null, CliColor activeColor = null, CliColor selectedColor = null, CliColor baseColor = null)
		{
			this.DataType = data.GetType();
			this._revertData = this._data = data;
			this._field = new TextEditField( location, title, workArea, DisplayMechanism(), baseColor, dataColor, selectedColor, activeColor );
			this.DataName = dataName;
			this.Title = title;
		}
		#endregion

		#region Accessors
		public dynamic Data
		{
			get => this._data;
			set
			{
				if (value.GetType() == this._dataType)
					Value = (this._data = value).ToString();
			}
		}

		protected string Value
		{
			get => Controller.Value;
			set
			{
				Controller.Value = Regex.Replace( value, @"[^\x20-\x7e\r\n\t]", "" ); // Remove all non-printable characters except \r, \n and \t;
				Controller.Render(); // Redraw the field.
			}
		}

		public int SelectionLength
		{
			get => Controller.SelectionLength;
			protected set
			{
				Controller.SelectionLength = value;
				if (value > 0)
				{
					if (Controller.SelectionStart < 0) Controller.SelectionStart = Controller.FindNearestInsertPoint();
					Controller.Render();
				}
			}
		}

		public int SelectionStart
		{
			get => Controller.SelectionStart;
			protected set
			{
				Controller.SelectionStart = value;
				if ((value > 0) && (Controller.SelectionLength > 0)) Controller.Render();
			}
		}

		public bool ReadOnly
		{
			get => Controller.ReadOnly;
			set => Controller.ReadOnly = value;
		}

		/// <summary>Gets/Sets the work area rectangle values used by this field.</summary>
		public Rectangle WorkArea
		{
			get => (Rectangle)Controller.Box;
			protected set
			{
				// Prevent out-of-bounds value assignment
				Rectangle r = new Rectangle(
					Math.Max( 0, value.Left ),
					Math.Max( 0, value.Top ),
					Math.Min( Console.WindowWidth, value.Width ),
					Math.Min( Console.WindowHeight, value.Height )
				);

				Controller.Location = r.Location;
				Controller.Size = r.Size;
			}
		}

		/// <summary>Gets/Sets the title value for this field.</summary>
		public string Title
		{
			get => Controller.Prompt;
			set
			{
				if ( !string.IsNullOrWhiteSpace( value ) )
				{
					Controller.Prompt = Regex.Replace( value, @"[\r\n]+", " " ); // Replace all CRLF's with a space...
					if ( string.IsNullOrEmpty( this._dataName ) )
						this.DataName = "";
				}
				else
					throw Language.Prompt.GetException( 0, new object[] { "title" } );
					//new ArgumentException( "The supplied 'title' value cannot be null, empty or whitespace." );
			}
		}

		/// <summary>Holds the location of where the prompt will be printed.</summary>
		public Point2 PromptLocation => this._field.PromptLocation;

		/// <summary>Facilitaes access to the field controller.</summary>
		public TextEditField Controller
		{
			get => this._field;
			protected set => this._field = value;
		}

		/// <summary>
		/// Gets/Sets the datatype for this class, according to allowed / defined types. This will also self-search the namespace for 
		/// derived classes in order to dynamically support using non-base/system types/classes.
		/// </summary>
		/// <remarks>
		/// Derived types must conform to this naming standard: "{Type}ScreenEditField" where "{Type}" is the common name of the data
		/// type that is intended to be supported / managed by that class and they must inherit "ScreenEditField<T>" where T is also
		/// the supported type referenced in the name.
		/// </remarks>
		/// <seealso cref="ScreenEditField{T}"/>
		public virtual Type DataType
		{
			get => this._dataType;
			set
			{
				switch (value.FullName)
				{
					case "System.Boolean":
					case "System.String":
					case "System.UInt64":
					case "System.UInt32":
					case "System.UInt16":
					case "System.Byte":
					case "System.Int64":
					case "System.Int32":
					case "System.Int16":
					case "System.SByte":
					case "System.DateTime":
					case "System.Decimal":
						this._dataType = value;
						break;
					default:
						Type[] types = GetAllDerivedTypes();
						int i = -1; while ((++i < types.Length) && !types[ i ].Name.Equals( value.Name + "ScreenEditField" )) ;
						if ( i < types.Length )
							this._dataType = value;
						else
							throw Language.Prompt.GetException( 1, new object[] { value.FullName } );
							//new ArgumentException( Language.Prompt.GetByName( "**Exceptions.DataType", new object[] { value.FullName } ) );
							// "Invalid Data Type Specified: \"" + value.FullName + "\""
						break;
				}
			}
		}

		/// <summary>A string value to identify/reference the data managed by this object.</summary>
		/// <remarks>This must be formatted as a variable-like, single-word, camel-cased.</remarks>
		public string DataName
		{
			get => this._dataName;
			set
			{
				if (string.IsNullOrEmpty( value ) && !string.IsNullOrEmpty( Title ))
					value = Title;

				if ( !string.IsNullOrWhiteSpace( value ) )
				{
					if ( Regex.IsMatch( value.Trim(), @"([\w]+[\s]*)", RegexOptions.IgnoreCase | RegexOptions.Multiline ) )
					{
						// Create a camel-cased value from a basic collection of words/sentence.
						value = String.Join( "", Regex.Split( value.Trim().UCWords( true ), @"[^\w]+" ) );
						value = Char.ToLowerInvariant( value[ 0 ] ) + value.Substring( 1 );
					}

					if ( string.IsNullOrEmpty( _dataName ) && Regex.IsMatch( value, @"[a-z][a-z_0-9]*[a-z0-9]", RegexOptions.IgnoreCase ) )
						this._dataName = value;
				}
				else
					throw Language.Prompt.GetException( 0, new object[] { "dataName" } );
					//new Exception( Language.Prompt.GetByName( "**Exceptions.DataName" ) );
					// "The supplied 'dataName' value cannot be null, empty or whitespace."
			}
		}

		/// <summary>Reports how many characters long the data stream can be.</summary>
		public int DataLength => FieldLines * FieldLength;

		/// <summary>Reports how many characters there are per line.</summary>
		public int FieldLength => this._field.Size.Width;

		/// <summary>Reports how many lines are managed by the control.</summary>
		public int FieldLines => this._field.Size.Height;

		/// <summary>Holds the location of where the prompt will be printed.</summary>
		public Point2 TitleLocation => this.PromptLocation;

		/// <summary>Provides an easy means to capture/access/set the current (Window-relative) cursor position.</summary>
		public Point2 CursorPos
		{
			get => CRW.CursorPosition;
			protected set
			{
				if (Controller.Contains( value ))
					CRW.CursorPosition = value;
			}
		}

		public Point2 AbsPos
		{
			get => Controller.CursorPosition; //new Point2( Console.CursorLeft - WorkArea.X, Console.CursorTop - WorkArea.Y );
			protected set => Controller.CursorPosition = value;
		}

		public string DbName
		{
			get => this._dbName;
			set
			{
				value = Regex.Replace( value, @"[^\w]", "" );
				if (!string.IsNullOrWhiteSpace( value ))
					this._dbName = value;
			}
		}

		public CliColor LabelColor
		{
			get => Controller.ActiveColor;
			set => Controller.ActiveColor = value;
		}

		public CliColor DataColor
		{
			get => Controller.DataColor;
			set => Controller.DataColor = value;
		}

		public CliColor ActiveColor
		{
			get => Controller.ActiveColor;
			set => Controller.ActiveColor = value;
		}

		public CliColor SelectedColor
		{
			get => Controller.SelectedColor;
			set => Controller.SelectedColor = value;
		}
		public bool IsActive => Controller.InField;

		/// <summary>Any characters matching the defined pattern here will be rejected.</summary>
		public Regex FilterPattern { get; set; } = null;

		public Regex ValidationPattern { get; set; } = null;

		public bool RealTimeValidation { get; set; } = true;
		#endregion

		#region Operators
		public static bool operator ==(ScreenEditFieldPrototype left, ScreenEditFieldPrototype right)
		{
			if (left is null) return (right is null);
			if (right is null) return false;
			return (left.DataName.Equals( right.DataName, StringComparison.OrdinalIgnoreCase ) && (left.DataType == right.DataType));
		}

		public static bool operator !=(ScreenEditFieldPrototype left, ScreenEditFieldPrototype right) => !(left == right);

		public static implicit operator string(ScreenEditFieldPrototype data) => data._data.ToString();
		#endregion

		#region Methods
		public bool Contains(Point2 point) =>
			this.WorkArea.ContainsAny( new Point[] { point, point - 1 } );

		public virtual void Write() => Controller.Render( false );

		protected void DisplayValue(bool restoreCursor) =>
			Controller.Render( restoreCursor );

		public void Focus() => CRW.CursorPosition = Controller.Location;

		public virtual dynamic ReadValue() =>
			ScreenEditDataPrototype.InstantiateDerivedType( this._dataName, this._dataType, Value );

		public abstract void ProcessKeyStroke(ConsoleKeyInfo keyPressed);

		public override bool Equals(object obj) =>
			base.Equals( obj );

		public void Revert()
		{
			this.Controller.Value = this._revertData.ToString();
			this._data = this._revertData;
			this.Write();
		}

		public override int GetHashCode() =>
			base.GetHashCode();

		/// <summary>Returns true if there's no filter pattern defined, or the passed string isn't matched by the one defined.</summary>
		/// <param name="test">A string (typically a single character) to test.</param>
		/// <returns>TRUE if there's no filter pattern defined, or the passed string isn't matched by it. A TRUE result means that the subject string has been rejected!</returns>
		/// <seealso cref="FilterPattern"/>
		public bool InFilter(string test) =>
			!(FilterPattern is null) && FilterPattern.IsMatch( test );

		/// <summary>Returns TRUE if there's no validation pattern defined, OR the passed string matches a defined pattern.</summary>
		/// <param name="test">The string to test.</param>
		/// <returns>TRUE if there's no validation pattern defined, OR the passed string matches a defined pattern.</returns>
		/// <seealso cref="ValidationPattern"/><seealso cref="RealTimeValidation"/>
		public bool PatternMatch(string test) =>
			(ValidationPattern is null) || ValidationPattern.IsMatch( test );

		/// <summary>Tests a supplied ScreenEditFieldPrototype object's prompt/title and work area to see if either overlaps with this object's prompt/title and/or workarea.</summary>
		/// <param name="test">A TextEditField object to test.</param>
		/// <returns>True if either element of the "test" object overlaps either element of this object.</returns>
		public bool Overlaps(ScreenEditFieldPrototype test) =>
			Controller.Overlaps( test._field );

		/// <summary>Tests a supplied Rectangle to see if it overlaps this object's work area.</summary>
		/// <param name="area">A Rectangle defining an area to test.</param>
		/// <returns>TRUE if this object's work area overlaps the supplied Rectangle.</returns>
		/// <remarks>DOES NOT CHECK/TEST the prompt!</remarks>
		public bool Overlaps(Rectangle area) =>
			_field.Overlaps( area );

		/// <summary>Placeholder for routines to report on the validity of the data.</summary>
		/// <returns>TRUE if the data is valid.</returns>
		public abstract bool IsValidData();

		/// <summary>Effects display of the underlying datatype.</summary>
		/// <remarks>In most cases, this is just an alias for the data's own ".ToString()" method, but if the value returned by that means
		/// is incompatible with this tool, a custom version can override this to produce properly formatted data.</remarks>
		protected virtual string DisplayMechanism() => "Wrong!";

		public override string ToString() =>
			"«" + (string.IsNullOrEmpty( this.DataName ) ? "No Name" : this.DataName) + "» {" + this._dataType.FullName + "}: \"" + DisplayMechanism() + "\"";

		/// <summary>Creates an instance of the datatype specified within the provided parser.</summary>
		/// <param name="source">The ScreenEditFieldPrototype class whose data type requires instancing.</param>
		public static dynamic CreateTypedInstance(ScreenEditFieldPrototype source)
		{
			Type maker = typeof( ScreenEditField<> ).MakeGenericType( new Type[] { source.DataType } );
			return Activator.CreateInstance( maker, new object[] { source.Title, source.Data, source.PromptLocation, source.WorkArea, source.DataName, source.DataColor, source.LabelColor } );
		}

		/// <summary>Produces a an array of TypeInfo objects.</summary>
		/// <remarks>The array contains all of the known DataParser variants within the supplied assembly / namespace.</remarks>
		public static TypeInfo[] GetAllDerivedTypes(string assemblyName = "*", string nameSpace = "*")
		{
			// Get a collection of all Assemblies in the current collection.
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			bool ValidExtensionName(string value) =>
				!Regex.IsMatch( value, @"(U?Int(8|16|32|64)|Bool(ean)?|DateTime|Decimal|String)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture );

			if (assemblyName.Equals( "*" )) // Look in them all...
			{
				List<TypeInfo> classes = new List<TypeInfo>();
				foreach (Assembly assembly in assemblies)
					foreach (TypeInfo ti in assembly.DefinedTypes)
						if (((nameSpace == "*") || ti.Namespace.Equals( nameSpace )) && Regex.IsMatch( ti.Name, @"[a-zA-Z0-9]+ScreenEditField$" ) && ValidExtensionName( ti.Name ))
							classes.Add( ti );

				return classes.ToArray();
			}
			else
			{
				// Search through the list of known assemblies for the one specified...
				int i = -1; while ((++i < assemblies.Length) && !assemblies[ i ].FullName.Substring( 0, assemblyName.Length + 1 ).Equals( assemblyName + "," )) ;
				if (i < assemblies.Length)
				{
					List<TypeInfo> classes = new List<TypeInfo>();
					foreach (TypeInfo ti in assemblies[ i ].DefinedTypes)
						if (((nameSpace == "*") || ti.Namespace.Equals( nameSpace )) && ValidExtensionName( ti.Name ))
							classes.Add( ti );

					return classes.ToArray();
				}
			}
			return new TypeInfo[] { };
		}

		public abstract ScreenEditDataPrototype ToData();
		#endregion
	}

	/// <summary>This is the real workhorse class that all derived type classes must inherit from.</summary>
	/// <typeparam name="T">The data type that the class is intended to manage.</typeparam>
	public abstract class ScreenEditField<T> : ScreenEditFieldPrototype
	{
		#region Properties
		protected List<ConsoleKey> _allowedKeys = new List<ConsoleKey>();
		#endregion

		#region Constructor
		public ScreenEditField(string name, T data, Point2 location, Rectangle area, string dataName = "", CliColor dataColor = null, CliColor labelColor = null)
			: base( name, data, location, area, dataName, dataColor, labelColor ) { }

		public ScreenEditField(string name, T data, int locationX, int locationY, int areaX, int areaY, int width, string dataName = "", CliColor dataColor = null, CliColor labelColor = null)
			: base( name, data, new Point2( locationX, locationY ), new Rectangle( 0, 0, 1, 1 ), dataName, dataColor, labelColor ) =>
			WorkArea = new Rectangle( new Point2( areaX, areaY ), new Size( width, 1 ) );
		#endregion

		#region Accessors
		new public T Data
		{
			get => (T)this._data;
			set => this._data = value;
		}
		#endregion

		#region Operators
		//		public static bool operator ==(ScreenEditField<T> left, ScreenEditField<T> right) =>
		//			(left as ScreenEditFieldPrototype) == (right as ScreenEditFieldPrototype);

		//		public static bool operator !=(ScreenEditField<T> left, ScreenEditField<T> right) => !(left == right);
		#endregion

		#region Methods
		public override void ProcessKeyStroke(ConsoleKeyInfo keyPressed)
		{
			if (IsActive)
			{
				//string myValue = MyScreenValue; int insIndex = InsertionIndex;
				switch (keyPressed.Key)
				{
					case ConsoleKey.Backspace:
						if (this.Value.Length == 0)
						{
							if ( CRW.CursorPosition.X > _field.Location.X ) CRW.CursorPosition -= 1;
							break;
						}

						// If the cursor is in the field's home position, BKSP becomes DEL...
						if (CRW.CursorPosition == this._field.Location)
						{
							if ( !this._field.DelCharRight() )
								CRW.CursorPosition += 1;
							break;
						}

						if (this._field.DelCharLeft())
						{
							if ( (CRW.CursorPosition.X < this._field.Location.X) && (CRW.CursorPosition.Y > this._field.Location.Y) )
								CRW.CursorPosition = new Point2( this._field.Location + this._field.Size.Width - 1, CRW.CursorPosition.Y - 1 );
							break;
						}
						
						CRW.CursorPosition -= 2;
						break;
					case ConsoleKey.Delete:
						if (!this._field.DelCharRight())
							CRW.CursorPosition += 1;
						break;
					case ConsoleKey.Home:
						if (keyPressed.Modifiers.HasFlag( ConsoleModifiers.Control ))
							CRW.CursorPosition = this._field.Location;
						else
							this._field.GoStartOfLine();
						break;
					case ConsoleKey.End:
						if (keyPressed.Modifiers.HasFlag( ConsoleModifiers.Control ))
							this._field.InsertPosition = -1; // End of the string.
						else
							this._field.GoEndOfLine();
						break;
					case ConsoleKey.Enter:
						if (!this._field.IsSingleLine && (CursorPos.Y < WorkArea.Bottom))
							this._field.Insert( "\r\n" );
						break;
					case ConsoleKey.Escape:
						if ( keyPressed.Modifiers.HasFlag( ConsoleModifiers.Control ) && !ReadOnly )
							this.Value = "";
						break;
					default:
						if ((_allowedKeys.Count == 0) || _allowedKeys.Contains( keyPressed.Key ))
						{
							string c = keyPressed.KeyChar.ToString();
							if (!keyPressed.Modifiers.HasFlag( ConsoleModifiers.Alt ) && !keyPressed.Modifiers.HasFlag( ConsoleModifiers.Control ) && !InFilter( c ))
							{
								this._field.Insert( c );
								CRW.CursorPosition += c.Length;
								if ((CRW.CursorPosition.X > _field.Box.Right) && (CRW.CursorPosition.Y < _field.Box.Bottom))
									CRW.CursorPosition = new Point2( _field.Box.Left, Console.CursorTop + 1 );
							}
						}
						break;
				}
			}
		}

		public abstract T Parse(string value);

		public override bool IsValidData() =>
			PatternMatch( Value );

		protected override string DisplayMechanism() => ((T)_data).ToString();

		public override ScreenEditDataPrototype ToData() => 
			new ScreenEditData<T>( new KeyValuePair<string, string>( _dataName, base.Value ) );
		#endregion
	}

	#region Derived ScreenEditField<T> types
	public class IntScreenEditField<T> : ScreenEditField<T>
	{
		#region Constructors
		public IntScreenEditField(string name, T data, Point location, Rectangle workArea, string dataName = "", CliColor dataColor = null, CliColor labelColor = null) :
			base( name, data, location, workArea, dataName, dataColor, labelColor ) => Initialize( data.GetType() );

		public IntScreenEditField(string name, T data, int locationX, int locationY, int areaX, int areaY, int width, string dataName = "", CliColor dataColor = null, CliColor labelColor = null) :
			base( name, data, locationX, locationY, areaX, areaY, width, dataName, dataColor, labelColor ) => Initialize( data.GetType() );
		#endregion

		#region Methods
		private void Initialize(Type test)
		{
			List<Type> _allowedTypes = new List<Type>( new Type[] { typeof( sbyte ), typeof( short ), typeof( int ), typeof( long ), typeof( decimal ) } );
			if ( !_allowedTypes.Contains( test ) )
				throw Language.Prompt.GetException( 0, new object[] { test.FullName } );
				//new InvalidCastException( "You cannot use this generic with the type \"" + test.FullName + "\"" );

			_allowedKeys.AddRange(
				new ConsoleKey[] {
					ConsoleKey.D1, ConsoleKey.D2, ConsoleKey.D3, ConsoleKey.D4, ConsoleKey.D5, ConsoleKey.D6, ConsoleKey.D7, ConsoleKey.D8, ConsoleKey.D9, ConsoleKey.D0,
					ConsoleKey.NumPad1, ConsoleKey.NumPad2, ConsoleKey.NumPad3, ConsoleKey.NumPad4, ConsoleKey.NumPad5, ConsoleKey.NumPad6, ConsoleKey.NumPad7,
					ConsoleKey.NumPad8, ConsoleKey.NumPad9, ConsoleKey.NumPad0, ConsoleKey.Subtract, ConsoleKey.OemMinus
				}
			);
		}

		public override void ProcessKeyStroke(ConsoleKeyInfo keyPressed)
		{
			// Check for '-' key, and process when appropriate.
			if ((keyPressed.KeyChar != '-') || (_field.InsertPosition == 0))
				base.ProcessKeyStroke( keyPressed );
		}

		public override T Parse(string value) =>
			(T)Convert.ChangeType( value, typeof( T ) );
		#endregion
	}

	public class UIntScreenEditField<T> : ScreenEditField<T>
	{
		#region Constructors
		public UIntScreenEditField(string name, T data, Point location, Rectangle workArea, string dataName = "", CliColor dataColor = null, CliColor labelColor = null) :
			base( name, data, location, workArea, dataName, dataColor, labelColor ) => Initialize( data.GetType() );

		public UIntScreenEditField(string name, T data, int locationX, int locationY, int areaX, int areaY, int width, string dataName = "", CliColor dataColor = null, CliColor labelColor = null) :
			base( name, data, locationX, locationY, areaX, areaY, width, dataName, dataColor, labelColor ) => Initialize( data.GetType() );
		#endregion

		#region Methods
		private void Initialize(Type test)
		{
			//List<Type> _allowedTypes = new List<Type>( new Type[] { typeof( sbyte ), typeof( short ), typeof( int ), typeof( long ) } );
			List<Type> _allowedTypes = new List<Type>( new Type[] { typeof( byte ), typeof( ushort ), typeof( uint ), typeof( ulong ) } );
			if ( !_allowedTypes.Contains( test ) )
				throw Language.Prompt.GetException( 0, new object[] { test.FullName } );
				//new InvalidCastException( "You cannot use this generic with the type \"" + test.FullName + "\"" );

			_allowedKeys.AddRange(
				new ConsoleKey[] {
					ConsoleKey.D1, ConsoleKey.D2, ConsoleKey.D3, ConsoleKey.D4, ConsoleKey.D5, ConsoleKey.D6, ConsoleKey.D7, ConsoleKey.D8, ConsoleKey.D9, ConsoleKey.D0,
					ConsoleKey.NumPad1, ConsoleKey.NumPad2, ConsoleKey.NumPad3, ConsoleKey.NumPad4, ConsoleKey.NumPad5, ConsoleKey.NumPad6, ConsoleKey.NumPad7,
					ConsoleKey.NumPad8, ConsoleKey.NumPad9, ConsoleKey.NumPad0
				}
			);

		}

		public override void ProcessKeyStroke(ConsoleKeyInfo keyPressed) =>
			base.ProcessKeyStroke( keyPressed );

		public override T Parse(string value) =>
			(T)Convert.ChangeType( value, typeof( T ) );

		public override dynamic ReadValue()
		{
			string v = Value; if (string.IsNullOrWhiteSpace( v )) v = "0";
			return ScreenEditDataPrototype.InstantiateDerivedType( this._dataName, typeof( T ), v );
		}
		#endregion
	}

	public class Int8ScreenEditField : IntScreenEditField<sbyte>
	{
		#region Constructors
		public Int8ScreenEditField(string name, sbyte data, Point location, Rectangle workArea, string dataName = "", CliColor labelColor = null, CliColor dataColor = null) :
			base( name, data, location, workArea, dataName, dataColor, labelColor ) { }

		public Int8ScreenEditField(string name, sbyte data, int locX, int locY, int areaX, int areaY, int width, string dataName = "", CliColor labelColor = null, CliColor dataColor = null) :
			base( name, data, locX, locY, areaX, areaY, width, dataName, dataColor, labelColor ) { }
		#endregion

		public override sbyte Parse(string value) => sbyte.Parse( value );
	}

	public class Int16ScreenEditField : IntScreenEditField<short>
	{
		#region Constructors
		public Int16ScreenEditField(string name, short data, Point location, Rectangle workArea, string dataName = "", CliColor labelColor = null, CliColor dataColor = null) :
			base( name, data, location, workArea, dataName, dataColor, labelColor ) { }

		public Int16ScreenEditField(string name, short data, int locX, int locY, int areaX, int areaY, int width, string dataName = "", CliColor labelColor = null, CliColor dataColor = null) :
			base( name, data, locX, locY, areaX, areaY, width, dataName, dataColor, labelColor) { }
		#endregion

		public override short Parse(string value) => short.Parse( value );
	}

	public class Int32ScreenEditField : IntScreenEditField<int>
	{
		#region Constructors
		public Int32ScreenEditField(string name, int data, Point location, Rectangle workArea, string dataName = "", CliColor labelColor = null, CliColor dataColor = null) :
			base( name, data, location, workArea, dataName, dataColor, labelColor ) { }

		public Int32ScreenEditField(string name, int data, int locX, int locY, int areaX, int areaY, int width, string dataName = "", CliColor labelColor = null, CliColor dataColor = null) :
			base( name, data, locX, locY, areaX, areaY, width, dataName, dataColor, labelColor ) { }
		#endregion

		public override int Parse(string value) => int.Parse( value );
	}

	public class Int64ScreenEditField : IntScreenEditField<long>
	{
		#region Constructors
		public Int64ScreenEditField(string name, long data, Point location, Rectangle workArea, string dataName = "", CliColor labelColor = null, CliColor dataColor = null) :
			base( name, data, location, workArea, dataName, dataColor, labelColor ) { }

		public Int64ScreenEditField(string name, long data, int locX, int locY, int areaX, int areaY, int width, string dataName = "", CliColor labelColor = null, CliColor dataColor = null) :
			base( name, data, locX, locY, areaX, areaY, width, dataName, dataColor, labelColor ) { }
		#endregion

		public override long Parse(string value) => long.Parse( value );
	}

	public class UInt8ScreenEditField : UIntScreenEditField<byte>
	{
		#region Constructors
		public UInt8ScreenEditField(string name, byte data, Point location, Rectangle workArea, string dataName = "", CliColor labelColor = null, CliColor dataColor = null) :
			base( name, data, location, workArea, dataName, dataColor, labelColor ) { }

		public UInt8ScreenEditField(string name, byte data, int locX, int locY, int areaX, int areaY, int width, string dataName = "", CliColor labelColor = null, CliColor dataColor = null) :
			base( name, data, locX, locY, areaX, areaY, width, dataName, dataColor, labelColor ) { }
		#endregion

		public override byte Parse(string value) => byte.Parse( value );
	}

	public class UInt16ScreenEditField : UIntScreenEditField<ushort>
	{
		#region Constructors
		public UInt16ScreenEditField(string name, ushort data, Point location, Rectangle workArea, string dataName = "", CliColor labelColor = null, CliColor dataColor = null) :
			base( name, data, location, workArea, dataName, dataColor, labelColor ) { }

		public UInt16ScreenEditField(string name, ushort data, int locX, int locY, int areaX, int areaY, int width, string dataName = "", CliColor labelColor = null, CliColor dataColor = null) :
			base( name, data, locX, locY, areaX, areaY, width, dataName, dataColor, labelColor ) { }
		#endregion

		public override ushort Parse(string value) => ushort.Parse( value );
	}

	public class UInt32ScreenEditField : UIntScreenEditField<uint>
	{
		#region Constructors
		public UInt32ScreenEditField(string name, uint data, Point location, Rectangle workArea, string dataName = "", CliColor labelColor = null, CliColor dataColor = null) :
			base( name, data, location, workArea, dataName, dataColor, labelColor ) { }

		public UInt32ScreenEditField(string name, uint data, int locX, int locY, int areaX, int areaY, int width, string dataName = "", CliColor labelColor = null, CliColor dataColor = null) :
			base( name, data, locX, locY, areaX, areaY, width, dataName, dataColor, labelColor ) { }
		#endregion

		public override uint Parse(string value) => uint.Parse( value );
	}

	public class UInt64ScreenEditField : UIntScreenEditField<ulong>
	{
		#region Constructors
		public UInt64ScreenEditField(string name, ulong data, Point location, Rectangle workArea, string dataName = "", CliColor labelColor = null, CliColor dataColor = null) :
			base( name, data, location, workArea, dataName, dataColor, labelColor ) { }

		public UInt64ScreenEditField(string name, ulong data, int locX, int locY, int areaX, int areaY, int width, string dataName = "", CliColor labelColor = null, CliColor dataColor = null) :
			base( name, data, locX, locY, areaX, areaY, width, dataName, dataColor, labelColor ) { }
		#endregion

		public override ulong Parse(string value) => ulong.Parse( value );
	}

	public class DecimalScreenEditField : IntScreenEditField<decimal>
	{
		#region Properties
		int _decPlaces = 2;
		#endregion

		#region Constructors
		public DecimalScreenEditField(string name, decimal data, int decPlaces, Point location, Rectangle workArea, string dataName = "", CliColor labelColor = null, CliColor dataColor = null) :
			base( name, data, location, workArea, dataName, dataColor, labelColor )
		{
			_allowedKeys.AddRange( new ConsoleKey[] { ConsoleKey.Decimal, ConsoleKey.OemPeriod } );
			_decPlaces = Math.Max( decPlaces, 0 );
		}

		public DecimalScreenEditField(string name, decimal data, int decPlaces, int locX, int locY, int areaX, int areaY, int width, string dataName = "", CliColor labelColor = null, CliColor dataColor = null) :
			base( name, data, locX, locY, areaX, areaY, width, dataName, dataColor, labelColor )
		{
			_allowedKeys.AddRange( new ConsoleKey[] { ConsoleKey.Decimal, ConsoleKey.OemPeriod } );
			_decPlaces = Math.Max( decPlaces, 0 );
		}
		#endregion

		#region Methods
		public override void ProcessKeyStroke(ConsoleKeyInfo keyPressed)
		{
			string m = Value;
			if ((keyPressed.KeyChar != '.') || ((AbsPos.X > 0) && (m.IndexOf('.')<0)))
				base.ProcessKeyStroke( keyPressed );
		}

		public override void Write()
		{
			string template = "#" + ((_decPlaces > 0) ? ".".PadRight( _decPlaces + 1, '0' ) : "");
			ScreenEditController.WriteAt( PromptLocation, "{$1}$2", new object[] { _field.DataColor, _field.Prompt } ); ; ;
			ScreenEditController.WriteAt( _field.Location, "{$1}$2", new object[] { this.DataColor, this.Data.ToString( template ) } );
		}

		public override decimal Parse(string value) => decimal.Parse( value );
		#endregion
	}

	public class DateTimeScreenEditField : ScreenEditField<DateTime>
	{
		#region Constructors
		public DateTimeScreenEditField(string name, DateTime data, Point location, Point workAreaHome, bool dateOnly = false, string dataName = "", CliColor labelColor = null, CliColor dataColor = null) : 
			base( name, data, location, new Rectangle(workAreaHome.X, workAreaHome.Y, dateOnly ? 10 : 19, 1), dataName, dataColor, labelColor ) =>
			Initialize();

		public DateTimeScreenEditField(string name, DateTime data, Point location, int areaX, int areaY = -1, bool dateOnly = false, string dataName = "", CliColor labelColor = null, CliColor dataColor = null) :
			base( name, data, location.X, location.Y, areaX, areaY, dateOnly ? 10 : 19, dataName, dataColor, labelColor ) =>
			Initialize();

		public DateTimeScreenEditField(string name, DateTime data, int locX, int locY, int areaX, int areaY, bool dateonly = false, string dataName = "", CliColor labelColor = null, CliColor dataColor = null) :
			base( name, data, locX, locY, areaX, areaY, dateonly ? 10 : 19, dataName, dataColor, labelColor ) =>
			Initialize();
		#endregion

		#region Accessors
		new public Regex FilterPattern
		{
			get => base.FilterPattern;
			protected set => base.FilterPattern = value;
		}

		new public Regex ValidationPattern
		{
			get => base.ValidationPattern;
			protected set => base.ValidationPattern = value;
		}

		new public bool RealTimeValidation
		{
			get => base.RealTimeValidation;
			protected set => base.RealTimeValidation = value;
		}

		new public Rectangle WorkArea
		{
			get => base.WorkArea;
			protected set => base.WorkArea = value;
		}
		#endregion

		#region Methods
		private void Initialize() // Oem1 = colon/semi-colon; Oem2 = slash/question mark
		{
			string pattern = /* language=Regex */ @"(?<date>(?<year>20[0-9]{2})[-/](?<month>0?[1-9]|1[0-2])[-/](?<day>[0-2]?[0-9]|3[0-1]))";

			if (WorkArea.Width > 10) 
				pattern += /* language=Regex */ @"( (?<time>(?<hour>[0-1][0-9]|2[0-3])[:](?<minute>[0-5][0-9])[:](?<second>[0-5][0-9])))?";

			ValidationPattern = new Regex( pattern );
			FilterPattern = new Regex( @"[^0-9]" );
			RealTimeValidation = true;
		}

		public override void ProcessKeyStroke(ConsoleKeyInfo keyPressed)
		{
			// Check for grammar keys, and process when/how appropriate.
			if (IsActive) // 2020-07-03 08:31:28
			{
				string m = Value;
				Point saveCurPos = CursorPos;
				switch (keyPressed.Key)
				{
					case ConsoleKey.Spacebar:
						if ((FieldLength == 19) && (AbsPos.X < 11))
							Console.CursorLeft = WorkArea.X + 11;
						break;
					case ConsoleKey.End:
						//Console.CursorLeft = WorkArea.Right;
						CRW.CursorPosition = new Point2( _field.Box.Right, Console.CursorTop );
						break;
					case ConsoleKey.Home:
						//Console.CursorLeft = WorkArea.X;
						_field.GoStartOfLine();
						break;
					case ConsoleKey.Backspace:
						switch (AbsPos.X)
						{
							case 0: break;
							case 5: case 8: case 11: case 14: case 17:
								//Console.CursorLeft -= 2;
								_field.GoCharLeft( 2 );
								break;
							default: // 1, 2, 3, 6, 9, 12, 15, 18, 19
								//Console.CursorLeft -= 1;
								_field.GoCharRight( 1 );
								break;
						}
						break;
					case ConsoleKey.Delete:
						switch (AbsPos.X)
						{
							case 2: case 3: case 5: case 6: case 8: case 9:
							case 11: case 12: case 14: case 15: case 17:  case 18:
								Con.Tec( "{$1}$2", new object[] { DataColor, "0" } );
								_field.GoCharLeft( 1 );
								//Console.CursorLeft -= 1;
								break;
						}
						break;
					case ConsoleKey.N: // CTRL-N = "Now" -- sets the field to the current date/time...
						if ( keyPressed.Modifiers.HasFlag(ConsoleModifiers.Control) )
						{
							//CursorPos = WorkArea.Location;
							CRW.CursorPosition = _field.Location;
							Con.Tec( "{$1}$2", new object[] { DataColor, DateTime.Now.ToMySqlString().Substring(0,FieldLength) } );
						}
						break;
					case ConsoleKey.PageUp:
					case ConsoleKey.PageDown:
						if (PatternMatch( Value ))
						{
							DateTime value = Parse( Value );
							int inc = keyPressed.Key == ConsoleKey.PageUp ? 1 : -1;
							switch (AbsPos.X)
							{
								case 0:
								case 1:
								case 2:
								case 3:
									value = value.AddYears( inc );
									break;
								case 5:
								case 6:
									value = value.AddMonths( inc );
									break;
								case 8:
								case 9:
									value = value.AddDays( inc );
									break;
								case 11:
								case 12:
									value = value.AddHours( inc );
									break;
								case 14:
								case 15:
									value = value.AddMinutes( inc );
									break;
								case 17:
								case 18:
								case 19:
									value = value.AddSeconds( inc );
									break;
							}
							_field.Render();
							//CursorPos = WorkArea.Location;
							//Con.Tec( "{$1}$2", new object[] { DataColor, value.ToMySqlString().Substring( 0, FieldLength ) } );
							//CursorPos = saveCurPos;
						}
						else
							System.Media.SystemSounds.Beep.Play();
						break;
					default:
						switch (AbsPos.X)
						{	// Date:
							case 0:  // Year 2 only
							case 1:  // Year 0 only
								int x = AbsPos.X + 1;
								Console.SetCursorPosition( WorkArea.X, WorkArea.Y );
								Con.Tec( "{$1}$2", new object[] { DataColor, m.Substring( 0, x ) } );
								break;
							case 2:  // Year 0-9
							case 3:  // Year 0-9
							case 6:  // Month 0-9
							case 9:  // Day 0-9 only
							case 12: // Hour 0-9
							case 15: // Minute 0-9
							case 18: // Seconds 0-9
								if (">0123456789".IndexOf( keyPressed.KeyChar ) > 0) goto default;
								break;
							case 5:  // Month 0, 1 only
								if (">01".IndexOf( keyPressed.KeyChar ) > 0) goto default;
								break;
							case 8:  // Day 0-3 only
								if (">0123".IndexOf( keyPressed.KeyChar ) > 0) goto default;
								break;
							// Time:
							case 11: // Hour 0-2 only
								if (">012".IndexOf( keyPressed.KeyChar ) > 0) goto default;
								break;
							case 14: // Minute 0-5 only
							case 17: // Seconds 0-5 only
								if (">012345".IndexOf( keyPressed.KeyChar ) > 0) goto default;
								break;
							// Dead characters
							case 4: case 7: case 10: case 13: case 16: break;
							default:
								if (AbsPos.X < FieldLength)
								{
									//base.ProcessKeyStroke( keyPressed );
									Con.Tec( "{$1}$2", new object[] { DataColor, keyPressed.KeyChar } );
									switch (AbsPos.X)
									{
										case 4: case 7: case 10: case 13: case 16:
											//Console.CursorLeft += 1;
											_field.GoCharRight( 1 );
											break;
									}
								}
								break;
						}
						break;
				}
				// saveCurPos = CursorPos;
				ScreenEditController.WriteAt( WorkArea.Location, "{$1}$2", new object[] { PatternMatch( Value ) ? DataColor : new CliColor( "0C" ), Value } );
				CursorPos = saveCurPos;
			}
		}

		public override void Write()
		{
			ScreenEditController.WriteAt( _field.PromptLocation, "{$1}$2", new object[] { LabelColor, Title } );
			ScreenEditController.WriteAt( WorkArea.Location, "{$1}$2", new object[] { DataColor, Data.ToMySqlString() } );
		}

		public override DateTime Parse(string value) =>
			DateTime.ParseExact( value, "yyyy-MM-dd HH:mm:ss".Substring(0,FieldLength), System.Globalization.CultureInfo.InvariantCulture );

		new public bool PatternMatch( string test )
		{
			DateTime waste;
			return base.PatternMatch( test ) &&
				DateTime.TryParseExact( test, "yyyy-MM-dd HH:mm:ss".Substring( 0, FieldLength ), System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.AssumeLocal, out waste );
		}

		public override bool IsValidData() =>
			this.PatternMatch( Value );

		public override dynamic ReadValue() =>
			new ScreenEditData<DateTime>( this._dataName, Parse( Value ) );
		#endregion
	}

	public class BooleanScreenEditField : ScreenEditField<bool>
	{
		#region Properties
		/// <summary>Used to specify how to display the boolean value: True/False, Yes/No, On/Off, 1/0, √/X</summary>
		public enum Display { TrueFalse, YesNo, OnOff, OneZero, CheckX };
		#endregion

		#region Constructors
		public BooleanScreenEditField(string name, bool data, Point location, Rectangle workArea, string dataName = "", CliColor labelColor = null, CliColor dataColor = null) :
			base( name, data, location, workArea, dataName, dataColor, labelColor ) => Initialize();

		public BooleanScreenEditField(string name, bool data, Display kind, Point location, Rectangle workArea, string dataName = "", CliColor labelColor = null, CliColor dataColor = null) :
			base( name, data, location, workArea, dataName, dataColor, labelColor) =>
			Initialize(kind);

		public BooleanScreenEditField(string name, bool data, Display kind, int locX, int locY, int areaX, int areaY, int width, string dataName = "", CliColor labelColor = null, CliColor dataColor = null) :
			base(name, data, locX, locY, areaX, areaY, width, dataName, dataColor, labelColor ) =>
			Initialize(kind);

		private void Initialize(Display kind = Display.YesNo)
		{
			OutputKind = kind;

			_allowedKeys = new List<ConsoleKey>(
				new ConsoleKey[] {
					ConsoleKey.Y, ConsoleKey.T, ConsoleKey.D1, ConsoleKey.NumPad1, 
					ConsoleKey.N, ConsoleKey.F, ConsoleKey.D0, ConsoleKey.NumPad0, 
					ConsoleKey.O, ConsoleKey.Spacebar, ConsoleKey.Home, ConsoleKey.End
				}
			);
			RealTimeValidation = false;
			FilterPattern = null;
			ValidationPattern = new Regex( @"^([YT0F]|yes|true|-?1|o(n|ff)|no?|false)$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture );
		}
		#endregion

		#region Accessors
		public Display OutputKind { get; set; } = Display.YesNo;

		new public Regex FilterPattern
		{
			get => base.FilterPattern;
			protected set => base.FilterPattern = value;
		}

		new public Regex ValidationPattern
		{
			get => base.ValidationPattern;
			protected set => base.ValidationPattern = value;
		}

		new public bool RealTimeValidation
		{
			get => base.RealTimeValidation;
			protected set => base.RealTimeValidation = value;
		}
		#endregion

		#region Methods
		private string Format(bool value)
		{
			string result = "&lbrace;Error&rbrace;";
			switch (OutputKind)
			{
				case Display.YesNo:
					result = (value ? "Yes" : "No");
					break;
				case Display.TrueFalse:
					result = (value ? "True" : "False");
					break;
				case Display.OneZero:
					result = (value ? "1" : "0");
					break;
				case Display.OnOff:
					result = (value ? "On" : "Off");
					break;
				case Display.CheckX:
					result = (value ? "\x221A" : "X"); // √ and X; alternative: ╳ = \x2573
					break;
			}
			return (FieldLength < result.Length) ? result.Substring(0,FieldLength) : result;
		}

		public override void ProcessKeyStroke(ConsoleKeyInfo keyPressed)
		{
			if (_allowedKeys.Contains( keyPressed.Key ))
				switch (keyPressed.Key)
				{
					case ConsoleKey.Y:
					case ConsoleKey.T:
					case ConsoleKey.D1:
					case ConsoleKey.NumPad1:
						this._data = true;
						goto default;
					case ConsoleKey.N:
					case ConsoleKey.F:
					case ConsoleKey.D0:
					case ConsoleKey.NumPad0:
						this._data = false;
						goto default;
					case ConsoleKey.O:
					case ConsoleKey.Spacebar:
						this._data = !this.Data;
						goto default;
					case ConsoleKey.Home:
						Console.CursorLeft = WorkArea.X;
						break;
					case ConsoleKey.End:
						Console.CursorLeft = WorkArea.X + Format( this._data ).Length;
						break;
					default:
						DisplayValue( this._data, false );
						break;
				}
		}

		public override void Write()
		{
			ScreenEditController.WriteAt( _field.PromptLocation, "{$1}$2", new object[] { _field.ActiveColor, _field.Prompt } );
			DisplayValue( this.Data, false );
		}

		protected void DisplayValue(bool value, bool restoreCursor = true)
		{
			Point saveCurPos = CursorPos;
			Console.CursorVisible = false;
			string output = Format( value );
			CursorPos = WorkArea.Location;
			Con.Tec( "{$1}$2", new object[] { DataColor, output } );
			if (output.Length < FieldLength)
				Con.Tec( "{,,>$1'_'}_", FieldLength - output.Length );

			if (restoreCursor) CursorPos = saveCurPos;
			Console.CursorVisible = true;
		}

		public override bool Parse(string value) => 
			!string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value.Trim(), @"^(?:[YT]|yes|true|-?1|on)$", RegexOptions.IgnoreCase);

		public override dynamic ReadValue() =>
			new ScreenEditData<bool>( this._dataName, Parse(Value) );

		public override bool IsValidData() =>
			ValidationPattern.IsMatch( Value );
		#endregion
	}

	public class StringScreenEditField : ScreenEditField<string>
	{
		#region Constructors
		public StringScreenEditField(string name, string data, Point2 location, Rectangle workArea, string dataName = "", CliColor labelColor = null, CliColor dataColor = null) :
			base( name, data, location, workArea, dataName, dataColor, labelColor ) { }

		public StringScreenEditField(string name, string data, int titleX, int titleY, int workX, int workY, int workWidth, int workHeight, string dataName = "", CliColor labelColor = null, CliColor dataColor = null) :
			base( name, data, new Point2( titleX, titleY ), new Rectangle( workX, workY, workWidth, workHeight), dataName, dataColor, labelColor ) { }
		#endregion

		public override string Parse(string value) => value;
	}
	#endregion

	public class ScreenEditFieldCollection : IEnumerator<ScreenEditFieldPrototype>
	{
		#region Properties
		protected List<ScreenEditFieldPrototype> _fields = new List<ScreenEditFieldPrototype>();
		protected int _position = 0;
		protected Rectangle _workArea = new Rectangle( 0, 0, Console.WindowWidth, Console.WindowHeight );
		protected List<Type> _derivedTypes = new List<Type>( ScreenEditFieldPrototype.GetAllDerivedTypes() );

		#endregion

		#region Constructors
		public ScreenEditFieldCollection() { }

		public ScreenEditFieldCollection( ScreenEditFieldCollection toClone )
		{
			this._workArea = toClone._workArea;
			foreach ( Type t in toClone._derivedTypes )
				this._derivedTypes.Add( t );

			this._fields = new List<ScreenEditFieldPrototype>( toClone._fields );
			for ( int i = 0; i < _fields.Count; i++ )
				this._fields[ i ] = toClone[ i ];
		}
		#endregion

		#region Accessors
		public ScreenEditFieldPrototype this[ int index ] => this._fields[ index ];

		public ScreenEditFieldPrototype this[ string fieldName ]
		{
			get
			{
				int i = IndexOf( fieldName );
				return (i < 0) ? null : this._fields[ i ];
			}
		}

		public int Count => _fields.Count;

		public Rectangle WorkArea
		{
			get
			{
				Rectangle result = new Rectangle( Console.WindowWidth, Console.WindowHeight, 0, 0 );
				foreach (ScreenEditFieldPrototype field in this._fields)
				{
					if (field.WorkArea.X < result.X)
						result.X = field.WorkArea.X;
					if (field.WorkArea.Y < result.Y)
						result.Y = field.WorkArea.Y;

					if (field.WorkArea.Right > result.End().X)
						result.Width = field.WorkArea.Right - result.X;
					if (field.WorkArea.Bottom > result.End().Y)
						result.Height = field.WorkArea.Bottom - result.Y;
				}
				return _workArea = result;
			}
		}

		// IEnumerator support
		ScreenEditFieldPrototype IEnumerator<ScreenEditFieldPrototype>.Current =>
			this._fields[ this._position ];

		object IEnumerator.Current =>
			this._fields[ this._position ];
		#endregion

		#region Methods
		private int IndexOf(string name)
		{
			int i = -1; while ((++i < Count) && !name.Equals( _fields[ i ].DataName, StringComparison.OrdinalIgnoreCase )) ;
			return (i < Count) ? i : -1;
		}

		private int IndexOf(ScreenEditFieldPrototype item) =>
			IndexOf( item.DataName );

		private int CollisionIndex( ScreenEditFieldPrototype item)
		{
			int i = -1; while ((++i < Count) && !this[ i ].Overlaps( item.WorkArea )) ;
			return i < Count ? i : -1;
		}

		/// <summary>Parses the collection to see if the supplied "field" overlaps any of the existing collection elements.</summary>
		/// <param name="field">A field to test for collisions.</param>
		/// <returns>TRUE if the supplied field (or its title) overlaps an existing field's area or title.</returns>
		private bool DetectCollision(ScreenEditFieldPrototype field) =>
			CollisionIndex(field) >= 0;

		public void Add(ScreenEditFieldPrototype field)
		{
			int i = IndexOf( field );
			if (i < 0)
			{
				if (!DetectCollision( field ))
					this._fields.Add( field );
			}
			else
			{
				if (!DetectCollision( field ) || field.Overlaps( this._fields[ i ] ))
					this._fields[ i ] = field;
			}
		}

		public void AddRange(ScreenEditFieldPrototype[] fields)
		{
			foreach (ScreenEditFieldPrototype field in fields)
				this.Add( field );
		}

		public void Clear() => this._fields.Clear();

		public void Remove(string dataName)
		{
			int i = IndexOf( dataName );
			if (i >= 0) this._fields.RemoveAt( i );
		}

		protected int GetFieldIndex( Point2 where )
		{
			int i = -1; while ((++i < Count) && !this._fields[ i ].Contains( where )) ;
			return (i < Count) ? i : -1;
		}

		protected int GetFieldIndex() =>
			GetFieldIndex( new Point2( Console.CursorLeft, Console.CursorTop ) );

		public ScreenEditFieldPrototype GetActiveField()
		{
			int i = GetFieldIndex();
			return (i < 0) ? null : this._fields[ i ];
		}

		public ScreenEditFieldPrototype GetField( Point point )
		{
			int i = GetFieldIndex( point );
			return (i < 0) ? null : this._fields[ i ];
		}

		public ScreenEditFieldPrototype Next()
		{
			if (Count > 0)
			{
				int i = GetFieldIndex();
				if (i >= 0)
					return (++i == Count) ? this[0] : this[i];

				// Check the screen, one position at a time, for the next field.
				Rectangle bounds = WorkArea; // The Workarea compiles itself when it's called, so get the answer and store it
				int x = Console.CursorLeft, y = Console.CursorTop; i = -1;
				while (((++x != Console.CursorLeft) || (y != Console.CursorTop)) && (i < 0))
				{
					i = GetFieldIndex( new Point2( x, y ) );
					if (x == bounds.Right) 
					{ 
						x = 1; 
						y = (y==bounds.Bottom) ? bounds.Top : y + 1; 
					}
				}
				if (i >= 0) return this[ i ];
			}
			return null;
		}

		public ScreenEditFieldPrototype Prev()
		{
			if (Count > 0)
			{
				int i = GetFieldIndex();
				if (i >= 0)
					return (--i >= 0) ? this[ i ] : this[ Count - 1 ];

				// Check the screen, one position at a time, for the previous field.
				Rectangle bounds = WorkArea; // The Workarea compiles itself when it's called, so get the answer and store it
				int x = Console.CursorLeft, y = Console.CursorTop; i = -1;
				while (((--x != Console.CursorLeft) || (y != Console.CursorTop)) && (i < 0))
				{
					i = GetFieldIndex( new Point2( x, y ) );
					if (x == bounds.Left) 
					{ 
						x = bounds.Right; 
						y = (y==bounds.Top) ? bounds.Bottom : y - 1;
					}
				}
				if (i >= 0) return this[ i ];
			}
			return null;
		}

		public bool IsCollectionValid()
		{
			bool result = true;
			int i = -1;
			while ((++i < Count) && result)
			{
				switch (_fields[ i ].DataType.Name)
				{
					case "SByte":
						result = (_fields[ i ] as Int8ScreenEditField).IsValidData();
						break;
					case "Int16":
						result = (_fields[ i ] as Int16ScreenEditField).IsValidData();
						break;
					case "Int32":
						result = (_fields[ i ] as Int32ScreenEditField).IsValidData();
						break;
					case "Int64":
						result = (_fields[ i ] as Int64ScreenEditField).IsValidData();
						break;
					case "Byte":
						result = (_fields[ i ] as UInt8ScreenEditField).IsValidData();
						break;
					case "UInt16":
						result = (_fields[ i ] as UInt16ScreenEditField).IsValidData();
						break;
					case "UInt32":
						result = (_fields[ i ] as UInt32ScreenEditField).IsValidData();
						break;
					case "UInt64":
						result = (_fields[ i ] as UInt64ScreenEditField).IsValidData();
						break;
					case "Boolean":
						result = (_fields[ i ] as BooleanScreenEditField).IsValidData();
						break;
					case "DateTime":
						result = (_fields[ i ] as DateTimeScreenEditField).IsValidData();
						break;
					case "Decimal":
						result = (_fields[ i ] as DecimalScreenEditField).IsValidData();
						break;
					case "String":
						result = (_fields[ i ] as StringScreenEditField).IsValidData();
						break;
					default:
						int j = -1; while ((++j <  _derivedTypes.Count) && !_derivedTypes[ j ].Name.Equals( _fields[ i ].DataType.Name + "ScreenEditField" )) ;
						dynamic obj = (j < _derivedTypes.Count) ? Convert.ChangeType( _fields[ i ], _derivedTypes[ j ] ) : (_fields[ i ] as StringScreenEditField);
						result = obj.IsValidData();
						break;
				}
			}
			return (i == Count);
		}

		//IEnumerator Support
		public IEnumerator<ScreenEditFieldPrototype> GetEnumerator() =>
			this._fields.GetEnumerator();

		bool IEnumerator.MoveNext() =>
			++this._position < this.Count;

		void IEnumerator.Reset() =>
			this._position = 0;

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

	public delegate ScreenEditDataCollection FunctionKeyOperation( ScreenEditFieldCollection fields, ScreenEditFieldPrototype activeField, ref ScreenEditController.MyThreadState threadState );

	public class FnKeyHandler
	{
		#region Properties
		protected FunctionKeyOperation _whatToDo;
		ConsoleKey _key;
		string _fnName = "";
		#endregion

		#region Constructor
		public FnKeyHandler( string name, ConsoleKey key, FunctionKeyOperation operation)
		{
			this.Name = name;
			this.Key = key;
			this.Op = operation;
		}
		#endregion

		#region Accessors
		public string Name
		{
			get => _fnName;
			set
			{
				value = Regex.Replace( value, @"[^\w ]", "" );
				if (!string.IsNullOrWhiteSpace( value )) 
					_fnName = value.Length > 12 ? value.Substring(0,12) : value;
			}
		}

		public ConsoleKey Key
		{
			get => _key;
			set
			{
				List<ConsoleKey> keys = new List<ConsoleKey>( new ConsoleKey[] { ConsoleKey.F1, ConsoleKey.F2, ConsoleKey.F3, ConsoleKey.F4, ConsoleKey.F5, ConsoleKey.F6, ConsoleKey.F7, ConsoleKey.F8, ConsoleKey.F9, ConsoleKey.F10, ConsoleKey.F11, ConsoleKey.F12 } );
				if ( keys.Contains( value ) )
					_key = value;
			}
		}

		public FunctionKeyOperation Op
		{
			get => this._whatToDo;
			set
			{
				if ( !(value is null) )
					this._whatToDo = value;
			}
		}
		#endregion
	}

	public class FnKeyCollection : IEnumerator<FnKeyHandler>
	{
		#region Properties
		protected List<FnKeyHandler> _handlers = new List<FnKeyHandler>();
		private int _position = 0;

		[Flags] public enum ActiveKeys { None = 0x000, F1 = 0x001, F2 = 0x002, F3 = 0x004, F4 = 0x008, F5 = 0x010, F6 = 0x020, F8 = 0x040, F9 = 0x080, F10 = 0x100, F11 = 0x200, F12 = 0x400, All = 0xFFF };
		#endregion

		#region Constructor
		public FnKeyCollection() { }
		#endregion

		#region Accessors
		public int Count => _handlers.Count;

		public FnKeyHandler this[ConsoleKey key]
		{
			get
			{
				int i = IndexOf( key );
				return (i >= 0) ? this._handlers[ i ] : null;
			}
		}

		public FnKeyHandler this[int index] => 
			index.InRange( 123, 112 ) ? this[(ConsoleKey)index] : index.InRange( Count, 0, NetXpertExtensions.Classes.Range.BoundaryRule.Loop ) ? this._handlers[index] : null;

		public ActiveKeys ActiveFnKeys { get; set; } = ActiveKeys.F1 | ActiveKeys.F2 | ActiveKeys.F3;
		#endregion

		#region Methods
		protected int IndexOf( ConsoleKey key )
		{
			int i = -1; 
			if ( ((int)key).InRange( 123, 112 ) )
				while ( (++i < Count) && (_handlers[ i ].Key != key) ) ;

			return (i < Count) ? i : -1;
		}

		/// <summary>Checks to see if a specified key exists in the collection.</summary>
		/// <param name="key">The ConsoleKey value to check for.</param>
		/// <returns>TRUE if the specified key exists in the collection.</returns>
		public bool HasKey( ConsoleKey key ) => IndexOf( key ) >= 0;

		/// <summary>Adds a new FnKeyHandler to the collection.</summary>
		/// <param name="handler">The FnKeyHandler object to add.</param>
		/// <returns>TRUE if the handler was added, otherwise FALSE.</returns>
		/// <remarks>There can only be one handler in the collection for each of the 12 function keys. If an attempt is made to
		/// add a handler with a duplicate key assignment, it will be rejected.</remarks>
		public bool Add( FnKeyHandler handler )
		{
			if ( ((int)handler.Key).InRange( 123, 112 ) && !HasKey( handler.Key ) )
			{
				this._handlers.Add( handler );
				this._handlers.Sort( ( a, b ) => ((int)a.Key).CompareTo( (int)b.Key) );
				return true;
			}

			return false;
		}

		public FnKeyHandler Remove( ConsoleKey key )
		{
			int i = IndexOf( key );
			if ( i < 0 )
				return null;

			FnKeyHandler result = this._handlers[ i ];
			this._handlers.RemoveAt( i );
			return result;
		}

		public void Sort() => this._handlers.Sort( ( a, b ) => ((int)a.Key).CompareTo( (int)b.Key ) );

		public ConsoleKey NextAvailable()
		{
			int i = 112; // ConsoleKey.F12 123;
			while ( this.HasKey( (ConsoleKey)i ) && (i < 124) ) i++;
			return (i < 124) ? (ConsoleKey)i : ConsoleKey.NoName;
		}

		public FnKeyHandler[] ToArray() => _handlers.ToArray();
		#endregion

		#region IEnumerator Support
		FnKeyHandler IEnumerator<FnKeyHandler>.Current => this._handlers[ this._position ];

		object IEnumerator.Current => this._handlers[ this._position ];

		public IEnumerator GetEnumerator() =>
			this._handlers.GetEnumerator();

		public bool MoveNext() =>
			(++this._position) < this.Count;

		public void Reset() =>
			this._position = 0;

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		private void Dispose( bool disposing )
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
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose( true );
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
		#endregion
	}

	public class ScreenEditController
	{
		#region Properties
		protected Point2 _curpos;
		protected ScreenEditFieldCollection _data = new ScreenEditFieldCollection();
		private MyThreadState _running = MyThreadState.Stopped;
		protected FnKeyCollection _fnKeys = null;

		// Enumerators used by this class...
		[Flags] private enum States { None = 0, NumLock = 1, CapsLock = 2, ScrollLock = 4, Insert = 8, All = 15, Unset = 1024 };
		public enum MyThreadState { Stopped, Paused, Running };
		#endregion

		#region Constructor
		public ScreenEditController( string title, FnKeyCollection fnKeys = null )
		{
			Title = title;
			BuildFnKeys( fnKeys );
		}
		#endregion

		#region Accessors
		public ScreenEditFieldPrototype this[ string fieldName ] =>
			this._data[ fieldName ];

		public ScreenEditFieldCollection Fields => this._data;

		protected bool InsertMode => (Console.CursorSize < 33);

		public string Title { get; set; } = "";
		#endregion

		#region Methods
		#region Default Methods for F1-F3
		// Accept / Save data
		protected ScreenEditDataCollection DefaultF1Op( ScreenEditFieldCollection fields, ScreenEditFieldPrototype activeField, ref MyThreadState threadState )
		{
			threadState = MyThreadState.Stopped;
			ScreenEditDataCollection result = new ScreenEditDataCollection();
			foreach ( ScreenEditFieldPrototype sefp in fields )
				result.Add( sefp.ToData() );

			return result;
		}

		// Revert Settings / Values
		protected ScreenEditDataCollection DefaultF2Op( ScreenEditFieldCollection fields, ScreenEditFieldPrototype activeField, ref MyThreadState threadState )
		{
			Point2 saveCurPos = new Point2( Console.CursorLeft, Console.CursorTop );
			ClearFnKeyLine();
			WriteAt( new Point2( 0, Console.WindowHeight ), Language.Prompt.Get( 0 ) );
			// "{,0}Revert changes? {7}[{B}Enter{7}/{B}Y{}es{7} or {e}ESC{7}/{e}N{}o{7}{7}]{}:"
			ConsoleKey vKey = Con.ReadKey( new ConsoleKey[] { ConsoleKey.O, ConsoleKey.N, ConsoleKey.Enter, ConsoleKey.Y }, ConsoleKey.N );
			if ( (fields.Count > 0) && ((vKey == ConsoleKey.Y) || (vKey == ConsoleKey.O) || (vKey == ConsoleKey.Enter)) )
			{
				int i;
				for ( i = fields.Count; i > 0; )
					fields[ --i ].Revert();

				fields[ i ].Focus();
			}
			DrawFnKeyLine();
			Goto( saveCurPos );
			return null;
		}

		// Cancel edit / lose changes...
		protected ScreenEditDataCollection DefaultF3Op( ScreenEditFieldCollection fields, ScreenEditFieldPrototype activeField, ref MyThreadState threadState )
		{
			Point2 saveCurPos = new Point2( Console.CursorLeft, Console.CursorTop );
			ClearFnKeyLine();
			WriteAt( new Point2( 0, Console.WindowHeight ), Language.Prompt.Get( 0 ) );
			// "{,0}Cancel edit? {7}[{B}Enter{7}/{B}Y{}es{7} or {e}ESC{7}/{e}N{}o{7}{7}]{}:"
			ConsoleKey vKey = Con.ReadKey( new ConsoleKey[] { ConsoleKey.O, ConsoleKey.N, ConsoleKey.Enter, ConsoleKey.Y }, ConsoleKey.N );
			if ( (vKey == ConsoleKey.Y) || (vKey == ConsoleKey.O) || (vKey == ConsoleKey.Enter) )
				threadState = MyThreadState.Stopped;

			DrawFnKeyLine();
			Goto( saveCurPos );
			return null;
		}
		#endregion

		private void BuildFnKeys( FnKeyCollection coll)
		{
			_fnKeys = new FnKeyCollection();

			if ( !(coll is null) && (coll.Count > 0) )
				_fnKeys = coll;
			else
				coll = new FnKeyCollection();

			// Keys F1, F2 and F3 are reserved, so if the user has defined functions for
			// those keys, try to re-assign them to available keys further in the collection.
			for (int i = 112; i < 115; i++ )
			{
				if (coll.HasKey( (ConsoleKey)i ))
				{
					FnKeyHandler temp = coll[ (ConsoleKey)i ];
					ConsoleKey newKey = coll.NextAvailable();
					if ( newKey != ConsoleKey.NoName )
					{
						temp.Key = newKey;
						coll.Add( temp );
					}
					coll.Remove( (ConsoleKey)i );
				}
				switch ( i )
				{
					case 112:
						_fnKeys.Add( new FnKeyHandler( Language.Prompt.Get( "Accept" ), (ConsoleKey)i, DefaultF1Op ) );
						break;
					case 113:
						_fnKeys.Add( new FnKeyHandler( Language.Prompt.Get( "Revert" ), (ConsoleKey)i, DefaultF2Op ) );
						break;
					case 114:
						_fnKeys.Add( new FnKeyHandler( Language.Prompt.Get( "Cancel" ), (ConsoleKey)i, DefaultF3Op ) );
						break;
				}
			}
		}

		private void DrawFnKeyLine()
		{
			Point2 curPos = new Point2( Console.CursorLeft, Console.CursorTop );
			int i = 0;
			ClearFnKeyLine();
			Goto( 0, -1 );
			foreach ( FnKeyHandler fkh in _fnKeys )
				if ( i * 15 < Console.WindowWidth - 65 )
				{
					bool enabled = _fnKeys.ActiveFnKeys.HasFlag( (FnKeyCollection.ActiveKeys)Math.Pow(2,i) );
					CliColor keyClr = enabled ? Con.DefaultColor.Inverse : new CliColor( "80" ),
							nameClr = enabled ? Con.DefaultColor : new CliColor( "80" );
					Con.Tec( "{$1,$2}F$3={$4,,>12} $5", new object[] { keyClr, i++ * 15, i.ToString("X1"), nameClr, fkh.Name } );
				}
			Goto( curPos );
		}

		private void ClearFnKeyLine() =>
			WriteAt( "{,0r,>$1} ".Replace( "$1", (Console.BufferWidth - 51).ToString() ), 0, -1 );

		private void DrawScreen()
		{
			Con.DefaultColor.ToConsole(); // Ensure default colour is applied before clearing the screen.
			Console.Clear();
			WriteAt( "{,0}&raquo; " + Title, 1, 1 );
			WriteAt( "{,0,>*'\u2550'}\u2550", 1, 2 ); // ═

			WriteAt( "{,0,>*'\u2550'}\u2550", 0, Console.WindowHeight - 1 ); // ═
			DrawFnKeyLine();

			foreach (ScreenEditFieldPrototype field in _data) 
				field.Write();
			
			if (_data.Count > 0)
				_data[ 0 ].Focus();

			DrawStatus();
		}

		/// <summary>Manages display of UI elements.</summary>
		/// <remarks>This runs independently as a separate thread!</remarks>
		private void MonitorKeys()
		{
			States set, lastSet = States.Unset;
			FnKeyCollection.ActiveKeys lastActiveFnKeys = FnKeyCollection.ActiveKeys.None;
			while ( _running != MyThreadState.Stopped )
			{
				set =
					( Con.NumLock ? States.NumLock : States.None ) |
					( Con.CapsLock ? States.CapsLock : States.None ) |
					( Con.ScrollLock ? States.ScrollLock : States.None ) |
					( this.InsertMode ? States.Insert : States.None );

				if ( _running == MyThreadState.Running )
				{

					if ( _fnKeys.ActiveFnKeys != lastActiveFnKeys )
					{
						lastActiveFnKeys = _fnKeys.ActiveFnKeys;
						DrawFnKeyLine();
					}

					if ( set != lastSet )
					{
						lastSet = set;
						Console.CursorVisible = false;
						Point2 saveCurPos = new Point2( Console.CursorLeft, Console.CursorTop );
						Goto( Console.WindowWidth - 50, -1 );
						Con.Tec( set.HasFlag( States.ScrollLock ) ? "{0A}SCROLL{} " : "{80}      " );
						Con.Tec( set.HasFlag( States.Insert ) ? "{0A}INS{} " : "{}OVR " );
						Con.Tec( set.HasFlag( States.CapsLock ) ? "{0A}CAPS{} " : "{80}     " );
						Con.Tec( set.HasFlag( States.NumLock ) ? "{0A}NUM{} " : "{80}    " );
						Goto( saveCurPos );
						Console.CursorVisible = true;
					}
				}
				Thread.Sleep( 250 );
			}
		}

		private void DrawStatus()
		{
			Console.CursorVisible = false;
			Point2 saveCurPos = new Point2( Console.CursorLeft, Console.CursorTop );

			Goto( Console.WindowWidth - 30, -1 );
			Con.Tec( "{}$4: {$1}[ X: {$1,,<3}$2{$1}  Y: {$1,,<3}$3{$1} ]", new object[] { Con.DefaultColor.Inverse, saveCurPos.X, saveCurPos.Y, Language.Prompt.Get(0) } );

			// if the collection is in an invalid state (has a field with bad data), shade the "Accept" key, otherwise illuminate it...
			//Goto( 4, Console.WindowHeight ); Con.Tec( "{$1}Accept", _data.IsCollectionValid() ? Con.DefaultColor : Con.DefaultColor.Alt( ConsoleColor.DarkGray ) );
			_fnKeys.ActiveFnKeys = _data.IsCollectionValid() ? _fnKeys.ActiveFnKeys | FnKeyCollection.ActiveKeys.F1 : _fnKeys.ActiveFnKeys & ~FnKeyCollection.ActiveKeys.F1;
			Goto( 1, 1 ); Con.Tec( "{,$1z} ", Console.WindowWidth - 30 );
			foreach ( ScreenEditFieldPrototype af in this._data )
			{
				if ( af.Contains( saveCurPos ) )
				{
					Goto( 1, 1 );
					Con.Tec( "{,$1}$4: {$2}[{$2,,|20}$3{$2}]", new object[] { Console.WindowWidth - 29, Con.DefaultColor.Inverse, af.DataType.FullName, Language.Prompt.Get( 1 ) } );
				}

				Goto( af.PromptLocation );
				Con.Tec( "{$1}$2{}", new object[] { af.Contains( saveCurPos ) ? af.LabelColor.Alt( ConsoleColor.Magenta ) : af.LabelColor, af.Title } );
			}

			Goto( saveCurPos );
			Console.CursorVisible = true;
		}

		protected ulong SelectObject( ScreenEditFieldPrototype field, int width = -1, int height = -1 )
		{
			Point2 curPos = CRW.CursorPosition;
			List<KeyValuePair<string, ulong>> items = new List<KeyValuePair<string, ulong>>();
			int top = 1, selectedId = 0;
			Console.CursorVisible = false;

			Size s = new Size( Math.Max( width, 30 ), Math.Max( 8, height ) );
			Point2 location = (field.WorkArea.Location - new Point2( 0, height / 2 )).Max( 0, 2 );
			Rectangle workArea = new Rectangle( location + new Point2( 1, 1 ), new Size( s.Width-3, s.Height-2 ) );
			CRW.CursorPosition = location; Con.Tec( "{$1}┌─┤{$2} $3 {$1}├{$1,,<$4'─'}╖{$5}▲", 
				new object[] { field.LabelColor, field.DataColor, field.DbName.UCWords(), s.Width - 7 - field.DbName.Length, field.LabelColor } 
			);

			int y;
			for (y = 0; y < s.Height - 2; y++)
			{
				CRW.CursorPosition = location.Add( 0, 1 ); 
				Con.Tec( "{$1,,>$2' '}│{$1}║{$3}▒", new object[] { field.LabelColor, s.Width - 2, field.LabelColor.Inverse } );
			}
			CRW.CursorPosition = location.Add( 0, 1 ); 
			Con.Tec( "{$1,,>$2'═'}╘{$1}╝{$3}▼", new object[] { field.LabelColor, s.Width - 2, field.LabelColor } );

			CRW.CursorPosition = new Point2( ((Corners)workArea).Right + 2, workArea.Top );
			Con.Tec( "{$1}●", field.LabelColor.Inverse );

			CRW.FillArea( workArea, '☺', new CliColor( "D0" ), false );

			Console.ReadKey( true );
			DrawScreen();
			CRW.CursorPosition = curPos;
			Console.CursorVisible = true;
			return ulong.MaxValue;
		}

		public void ConsoleCancelEventHandler( object sender, ref ConsoleCancelEventArgs e )
		{
			_running = MyThreadState.Stopped;
			e.Cancel = true; // Stops CTRL-C from crashing entire application...
		}

		public ScreenEditDataCollection Execute()
		{
			bool rememberControlCAsInputSetting = Console.TreatControlCAsInput;
			Console.TreatControlCAsInput = false;
			_running = MyThreadState.Running;
			ScreenEditDataCollection results = new ScreenEditDataCollection();
			Thread keyStatus = new Thread( new ThreadStart( this.MonitorKeys ) );

			DrawScreen();
			ScreenEditFieldPrototype lastActiveField = null;
			keyStatus.Start();
			while (_running != MyThreadState.Stopped)
				while (Console.KeyAvailable)
				{
					_running = MyThreadState.Paused;
					ConsoleKeyInfo key = Console.ReadKey( true );
					ScreenEditFieldPrototype activeField = Fields.GetActiveField();
					if (activeField != lastActiveField)
					{
						//if (!(lastActiveField is null)) lastActiveField.Leave();
						//if (!(activeField is null)) activeField.Enter();
						lastActiveField = activeField;
					}

					switch (key.Key)
					{
						case ConsoleKey.UpArrow:
							if (Console.CursorTop > _data.WorkArea.Top)
								Console.CursorTop--;
							break;
						case ConsoleKey.DownArrow:
							if (Console.CursorTop < Corners.Instantiate(_data.WorkArea).Bottom)
								Console.CursorTop++;
							break;
						case ConsoleKey.LeftArrow:
							if (key.Modifiers.HasFlag( ConsoleModifiers.Control ))
							{
								if (Console.CursorLeft > _data.WorkArea.Left)
								{
									// Word Left
									// Rectangle bounds = new Rectangle( 0, Console.CursorTop, Console.CursorLeft, 1 );
									Rectangle bounds = new Rectangle( _data.WorkArea.Left, Console.CursorTop, Console.CursorLeft - _data.WorkArea.Left, 1 );
									string line = ReadScreen( bounds.Location, bounds.Width );
									if (Regex.IsMatch( line, @"[a-z_A-Z0-9]" )) // Make sure there's actually *something* to go "back" to!
									{
										int x = line.Length; // start at the end

										// If we're on a valid word character, move backwards until we're not...
										if ((Console.CursorLeft < bounds.Right) && Regex.IsMatch( line.Substring( x - 1, 1 ), @"[a-z_A-Z0-9]" ))
											while ((--x > 0) && Regex.IsMatch( line.Substring( x, 1 ), @"[a-z_A-Z0-9]" )) ;

										// If there's more line left to scan...
										if (x > 0)
										{
											// While we're on an invalid character, keep moving back...
											while ((--x > 0) && Regex.IsMatch( line.Substring( x - 1, 1 ), @"[^a-zA-Z0-9]" )) ;

											// If there's still more line left to scan...
											if (x > 0) // Keep moving back while we're on valid characters.
												while ((--x > 0) && Regex.IsMatch( line.Substring( x, 1 ), @"[a-z_A-Z0-9]" )) ;

											if (x > 0) x += 1;
										}
										else
											System.Media.SystemSounds.Beep.Play();

										Console.CursorLeft = bounds.Left + x;
									}
									else
										System.Media.SystemSounds.Beep.Play();
								}
								else
									System.Media.SystemSounds.Beep.Play();
							}
							else
								if (Console.CursorLeft > Fields.WorkArea.Left)
									Console.CursorLeft -= 1;
							break;
						case ConsoleKey.RightArrow:
							if (key.Modifiers.HasFlag( ConsoleModifiers.Control ))
							{
								if (Console.CursorLeft < _data.WorkArea.Right)
								{
									// Word Right
									// Rectangle bounds = new Rectangle( Console.CursorLeft, Console.CursorTop, Console.WindowWidth - Console.CursorLeft, 1 );
									Rectangle bounds = new Rectangle( Console.CursorLeft, Console.CursorTop, _data.WorkArea.Right - Console.CursorLeft, 1 );
									string line = ReadScreen( bounds.Location, bounds.Width );
									if (Regex.IsMatch( line, @"[a-zA-Z0-9]" ))
									{
										int x = 0;
										while ((++x < line.Length) && Regex.IsMatch( line.Substring( x, 1 ), @"[a-zA-Z0-9]" )) ;
										if ((x < line.Length) && Regex.IsMatch( line.Substring( x ), @"[a-zA-Z0-9]" ))
											while ((++x < line.Length) && Regex.IsMatch( line.Substring( x, 1 ), @"[^a-z_A-Z0-9]" )) ;
										else
											System.Media.SystemSounds.Beep.Play();
										Console.CursorLeft = Math.Min( Console.WindowWidth - 1, Console.CursorLeft + x );
									}
									else
										System.Media.SystemSounds.Beep.Play();
								}
								else
									System.Media.SystemSounds.Beep.Play();
							}
							else
								if (Console.CursorLeft < Fields.WorkArea.Right)
									Console.CursorLeft += 1;
							break;
						case ConsoleKey.Tab:
							var nextField = key.Modifiers.HasFlag( ConsoleModifiers.Shift ) ? _data.Prev() : _data.Next();
							if (!(nextField is null))
								nextField.Focus();
							break;
						case ConsoleKey.Insert:
							Console.CursorSize = InsertMode ? 100 : 10;
							break;

						case ConsoleKey.F1:  // Accept
						case ConsoleKey.F2:  // Revert
						case ConsoleKey.F3:  // Abort
						case ConsoleKey.F4:  // User defined...
						case ConsoleKey.F5:  //      "
						case ConsoleKey.F6:  //      "
						case ConsoleKey.F7:  //      "
						case ConsoleKey.F8:  //      "
						case ConsoleKey.F9:  //      "
						case ConsoleKey.F10: //      "
						case ConsoleKey.F11: //      "
						case ConsoleKey.F12: //      "
							if (this._fnKeys.HasKey(key.Key))
								results = this._fnKeys[ key.Key ].Op( _data, activeField, ref _running );
							break;

						case ConsoleKey.Home:
							if (activeField is null)
							{
								CRW.CursorPosition = key.Modifiers.HasFlag( ConsoleModifiers.Control ) 
									? _data.WorkArea.Location 
									: new Point( _data.WorkArea.Left, Console.CursorTop );
								break;
							}
							goto default;
						case ConsoleKey.End:
							if (activeField is null)
							{
								CRW.CursorPosition = key.Modifiers.HasFlag( ConsoleModifiers.Control ) 
									? Corners.Instantiate( _data.WorkArea ).BottomRight 
									: new Point2( _data.WorkArea.Right, Console.CursorTop );
								break;
							}
							goto default;
						default:
							if ( !(activeField is null) )
								activeField.ProcessKeyStroke( key );
							break;
					}
					DrawStatus();
					if ( _running != MyThreadState.Stopped )
						_running = MyThreadState.Running;
				}

			Goto( 0, Console.WindowHeight );
			Con.Tec( "{z,rn} " );
			Console.CursorSize = 10; // Make sure we put the cursor back to normal!
			Console.Clear();
			Console.TreatControlCAsInput = rememberControlCAsInputSetting;
			return results;
		}
		#endregion

		#region Static Methods for accessing the console screen buffer.
		public static void Goto(Point2 location) =>
			Goto( location.X, location.Y );

		public static void Goto(int x = -1, int y = -1)
		{
			if (x < 0) x = Console.WindowWidth;
			if (y < 0) y = Console.WindowHeight;

			x = Math.Max( 0, Math.Min( Console.WindowWidth, x ) );
			y = Math.Max( 0, Math.Min( Console.WindowHeight, y ) );
			Console.SetCursorPosition( x, y );
		}

		public static void WriteAt(Point2 location, string what, object[] data = null)
		{
			Goto( location );
			Con.Tec( what, data );
		}

		public static void WriteAt(string what, int x = -1, int y = -1, object[] data = null) =>
			WriteAt( new Point2( x < 0 ? Console.WindowWidth : x, y < 0 ? Console.WindowHeight : y ), what, data );

		/// <summary>Reads a single-line of text from the specified point, for the specified length.</summary>
		/// <param name="location">A Point2 object specifying where to start reading.</param>
		/// <param name="length">How many characters to read.</param>
		/// <param name="handle">The Console Handle for the window to read from.</param>
		/// <returns>A string containing the characters copied from the specified console screen coordinates.</returns>
		public static string ReadScreen(Point2 location, int length, int handle = -11)
		{
			string result = "";
			IntPtr stdOut = GetStdHandle( handle );

			for (int x = location.X; x < location.X + length; x++)
			{
				uint coord = (uint)x | (uint)location.Y << 16;
				if (!ReadConsoleOutputCharacterA(
					stdOut, out byte chAnsi, 1, coord, out _ ))
					throw new Win32Exception();
				result += (char)chAnsi;
			}

			return result;
		}

		/// <summary>Reads a multi-line (box/rectangle) section of the screen into an array of strings (1 per line).</summary>
		/// <param name="area">A Rectangle object specifying the area to read.</param>
		/// <param name="handle">The Console Handle for the window to read from.</param>
		/// <returns>An array of strings containing the data copied from the specified region of the console.</returns>
		/// <remarks>If the Rectangle defines only a single line, this simply returns the single-line ReadScreen value in an array of one.</remarks>
		public static string[] ReadScreen(Rectangle area, int handle = -11)
		{
			if (area.Top == area.Bottom) // Single line reads go here:
				return new string[] { ReadScreen( new Point2( area.Left, area.Top ), area.Right - area.Left, handle ) };

			List<string> result = new List<string>();
			IntPtr stdOut = GetStdHandle( handle );
			for (int y = area.Top; y < area.Bottom; y++)
			{
				string line = "";
				for (int x = area.Left; x < area.Right; x++)
				{
					uint coord = (uint)x | (uint)y << 16;
					if (!ReadConsoleOutputCharacterA(
						stdOut, out byte chAnsi, 1, coord, out _ ))
						throw new Win32Exception();
					line += (char)chAnsi;
				}
				result.Add( line );
			}
			return result.ToArray();
		}

		[DllImport( "kernel32", SetLastError = true )]
		static extern IntPtr GetStdHandle(int num);

		[DllImport( "kernel32", SetLastError = true, CharSet = CharSet.Ansi )]
		[return: MarshalAs( UnmanagedType.Bool )] // ┌──────────────────^
		static extern bool ReadConsoleOutputCharacterA(
			IntPtr hStdout,   // result of 'GetStdHandle(-11)'
			out byte ch,      // ANSI character result
			uint c_in,        // (set to '1')
			uint coord_XY,    // screen location to read, X:loword, Y:hiword
			out uint c_out);  // (unwanted, discard)

		[DllImport( "kernel32", SetLastError = true, CharSet = CharSet.Unicode )]
		[return: MarshalAs( UnmanagedType.Bool )] // ┌───────────────────^
		static extern bool ReadConsoleOutputCharacterW(
			IntPtr hStdout,   // result of 'GetStdHandle(-11)'
			out Char ch,      // Unicode character result
			uint c_in,        // (set to '1')
			uint coord_XY,    // screen location to read, X:loword, Y:hiword
			out uint c_out);  // (unwanted, discard)
		#endregion
	}
}
