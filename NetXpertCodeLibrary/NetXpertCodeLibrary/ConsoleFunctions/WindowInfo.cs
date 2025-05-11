using NetXpertCodeLibrary.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetXpertCodeLibrary.ConsoleFunctions
{
	public sealed class WindowInfo
	{
		#region Properties
		Size _windowSize;
		Point _location;
		FormWindowState _windowState;
		ThreadedHandle _windowHandle;
		static Regex _pattern =
			new Regex(
				@"(Location: ?)?\((?<loc>-?[0-9]+, ?-?[0-9]+)\);([\s]+Size: ?)?\((?<size>-?[0-9]+, ?-?[0-9]+)\);([\s]+\[(?<state>minimized|maximized|normal)\])?",
				RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture
			);
		#endregion

		#region Constructors
		private WindowInfo() { } // Used internally!

		public WindowInfo(Form parent, ThreadedHandle handle = null) => Import( parent, handle );

		public WindowInfo(Point location, Size size, FormWindowState formWindowState = FormWindowState.Normal, ThreadedHandle handle = null)
		{
			this._location = location;
			this._windowSize = size;
			this._windowState = formWindowState;
			this._windowHandle = handle;
		}

		public WindowInfo(string source)
		{
			WindowInfo temp = Parse( source );
			this._location = temp._location;
			this._windowSize = temp._windowSize;
			this._windowState = temp._windowState;
			this._windowHandle = temp._windowHandle;
		}
		#endregion

		#region Operators
		public static implicit operator WindowInfo(Form parent) => new WindowInfo( parent );
		public static implicit operator String(WindowInfo data) => data.ToString();
		public static implicit operator WindowInfo(string source) => Parse(source);

		public static bool operator ==(WindowInfo left, WindowInfo right)
		{
			if (left is null) return right is null;
			if (right is null) return false;
			return 
				(left.Rectangle.Size.Width == right.Rectangle.Size.Width) &&
				(left.Rectangle.Size.Height == right.Rectangle.Size.Height) &&
				(left.Rectangle.Location.X == right.Rectangle.Location.X) &&
				(left.Rectangle.Location.Y == right.Rectangle.Location.Y) &&
				(left.WindowState == right.WindowState);
		}

		public static bool operator !=(WindowInfo left, WindowInfo right) => !(left == right);

		public static bool operator ==(WindowInfo left, Point location)
		{
			if (left is null) return false; // location is non-nullable
			return left.Location == location;
		}

		public static bool operator !=(WindowInfo left, Point location) => !(left == location);

		public static bool operator ==(WindowInfo left, Size size)
		{
			if (left is null) return false;
			return left.Size == size;
		}

		public static bool operator !=(WindowInfo left, Size size) => !(left == size);
		#endregion

		#region Accessors
		public Size Size => this._windowSize;

		public Point Location => this._location;

		public FormWindowState WindowState => this._windowState;

		public ThreadedHandle Handle
		{
			get => this._windowHandle;
			set => this._windowHandle = value;
		}

		public Rectangle Rectangle
		{
			get => new Rectangle( this.Location, this.Size );
			set
			{
				this._location = value.Location;
				this._windowSize = value.Size;
			}
		}
		#endregion

		#region Methods
		public void ApplyTo(Form form)
		{
			form.Location = Location;
			form.Size = Size;
			form.WindowState = WindowState;
		}

		public void Import( Form form, ThreadedHandle handle = null )
		{
			this._windowSize = form.Size;
			this._location = form.Location;
			this._windowState = form.WindowState;
			this._windowHandle = handle;
		}

		public override bool Equals(object obj) =>
			base.Equals( obj );

		public override int GetHashCode() =>
			base.GetHashCode();

		public override string ToString() =>
			"Location: (" + this._location.X.ToString() + ", " + this._location.Y.ToString() + "); " +
			"Size: (" + this._windowSize.Width.ToString() + ", " + this._windowSize.Height.ToString() + "); " +
			"[" + this._windowState.ToString() + "]";

		public static bool IsValid(string test) =>
			_pattern.IsMatch( test.Trim() );

		private static Point ParseCoords( string data )
		{
			Regex pattern = new Regex( @"[\[{(<]?(?<x>[0-9]+)[,;][\s]*(?<y>[0-9]+)[\]})>]?", RegexOptions.ExplicitCapture );
			if (pattern.IsMatch( data.Trim() ))
			{
				Match m = pattern.Matches( data.Trim() )[ 0 ];
				if (m.Success)
					return new Point( int.Parse( m.Groups[ "x" ].Value ), int.Parse( m.Groups[ "y" ].Value ) );
			}
			return new Point( -1, -1 );
		}

		public static WindowInfo Parse(string source, ThreadedHandle handle = null)
		{
			WindowInfo result = null;
			if (!string.IsNullOrEmpty( source ) && _pattern.IsMatch( source.Trim() ))
			{
				Match m = _pattern.Matches( source.Trim() )[ 0 ];
				if (m.Success)
				{
					result = new WindowInfo();
					Point test = ParseCoords( m.Groups[ "loc" ].Value );
					result._location = test;
					test = ParseCoords( m.Groups[ "size" ].Value );
					result._windowSize = new Size( test.X, test.Y );

					switch (m.Groups[ "state" ].Value.ToLowerInvariant())
					{
						case "minimized":
							result._windowState = FormWindowState.Minimized;
							break;
						case "maximized":
							result._windowState = FormWindowState.Maximized;
							break;
						case "normal":
						default:
							result._windowState = FormWindowState.Normal;
							break;
					}

					result._windowHandle = handle;
				}
			}
			return result;
		}
		#endregion
	}
}
