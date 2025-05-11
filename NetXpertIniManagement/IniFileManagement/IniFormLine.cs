using System.Text.RegularExpressions;
using IniFileManagement.Values;
using NetXpertExtensions;

namespace IniFileManagement
{
	public abstract partial class IniFormLineBase : IniLine<Form>
	{
		#region Properties
		#endregion

		#region Constructors
		protected IniFormLineBase( IniFileMgmt root, Type childType, Form form, string comment = "" )
			: base( root, $"Form:{childType.FullName}", ExtractData( form ), comment ) { }

		protected IniFormLineBase( IniFileMgmt root, Type childType, string data = "", string comment = "" )
			: base( root, $"Form:{childType.FullName}", "", comment )
		{
			data ??= string.Empty; data = data.Trim();
			if (data.Length == 0 || !ValidateForm_Rx().IsMatch( data ))
			{
				Form form = (Form)Activator.CreateInstance( childType );
				if (form is not null) this.Value = (root,ExtractData( form ));
			}
			else
				this.Value = (root,data);
		}
		#endregion

		#region Accessors
		public string Title
		{
			get => FetchItem( TitleParser_Rx() );
			set => EmbedItem( "Title", value, TitleParser_Rx() );
		}

		public Size Size
		{
			get
			{
				(int W, int H) = ParsePoint( FetchItem( PointParser_Rx() ) );
				return new Size( W, H );
			}
			set => EmbedItem( "Size", $"(W:{value.Width},H:{value.Height})", PointParser_Rx() );
		}

		public Point Location
		{
			get
			{
				(int X, int Y) = ParsePoint( FetchItem( PointParser_Rx() ) );
				return new Point( X, Y );
			}
			set => EmbedItem( "Location", $"(X:{value.X},Y:{value.Y})", PointParser_Rx() );
		}

		public FormWindowState WindowState
		{
			get
			{
				string value = FetchItem( WindowStateParser_Rx() );
				return value.ToEnum(FormWindowState.Normal);
			}
			set => EmbedItem( "WindowState", $"{value}", WindowStateParser_Rx() );
		}
		#endregion

		#region Methods
		protected string FetchItem( Regex pattern )
		{
			ArgumentNullException.ThrowIfNull( pattern, nameof( pattern ) );
			var m = pattern.Match( this.Value );
			return m.Success ? m.Groups[ "value" ].Value : string.Empty;
		}

		protected void EmbedItem( string identifier, string value, Regex pattern )
		{
			ArgumentException.ThrowIfNullOrWhiteSpace( identifier );
			ArgumentNullException.ThrowIfNull( pattern, nameof( pattern ) );

			if (string.IsNullOrWhiteSpace( value )) value = string.Empty;

			string tag = FetchItem( pattern );
			if (tag.Length == 0) // The item does not exist, or has no value.
			{
				if (value.Length > 0) // data has length, so we add the declaration to the record
					this.Value = (Root, $"{identifier}={value};{Value}");
			}
			else // The item exists
			{
				this.Value = (Root, pattern.Replace( Value, "" )); // Remove the item.
				if (value.Length == 0)
					Value = (Root, $"{identifier}={value};{Value}"); // Add it back with the new value.
			}
		}

		public void ApplyToForm( ref Form form )
		{
			form.Location = Location;
			form.Size = Size;
			form.Text = Title;
			form.WindowState = WindowState;
		}

		protected static (int, int) ParsePoint( string source )
		{
			string[] values = [];
			if (!string.IsNullOrWhiteSpace( source ))
			{
				source = CleanPointStrings_Rx().Replace( source, "" );
				if ( source.Length > 0 )
					values = source.Split( ',' );
			}
			return values.Length switch
			{
				1 => (int.Parse( values[ 0 ] ), 0),
				2 => (int.Parse( values[ 1 ] ), int.Parse( values[ 2 ] )),
				_ => (-1, -1),
			};
		}

		public static string ExtractData( Form form ) =>
				$"Title:\x22{form.Text}\x22;"
				+ $"Location:(X:{form.Location.X},Y:{form.Location.Y});"
				+ $"Size:(W:{form.Width},H:{form.Height});"
				+ $"WindowState:{form.WindowState};";
		#endregion

		#region Generated Regex
		[GeneratedRegex( @"[^\d,]" )]
		private static partial Regex CleanPointStrings_Rx();

		[GeneratedRegex( @"Title:[\x22](?<value>:[^\x22]*)[\x22](;|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant )]
		private static partial Regex TitleParser_Rx();

		[GeneratedRegex( @"(Size|Location):(?<value>\([XW]:[-+]?[\d]*,[YH]:[-+]?[\d]*\))(;|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant )]
		private static partial Regex PointParser_Rx();

		[GeneratedRegex( @"WindowState:(?<value>[a-z.]*)(;|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant )]
		private static partial Regex WindowStateParser_Rx();

		[GeneratedRegex( @"^((Title|Size|Location|WindowState):[\x22(]?.*[\x22)]?(;|$))?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant )]
		private static partial Regex ValidateForm_Rx();
		#endregion
	}

	public class IniFormLine<T> : IniFormLineBase where T : Form, new()
	{
		#region Constructors
		public IniFormLine( IniFileMgmt root, T form, string comment = "" )
			: base( root, typeof(T), form, comment ) { }

		public IniFormLine( IniFileMgmt root, string data = "", string comment = "" )
			: base(root, typeof( T ), "", comment ) { }
		#endregion

		#region Methods
		public void ApplyToForm( ref T form )
		{
			Form f = form as Form;
			base.ApplyToForm( ref f );
			form = (T)f;
		}
		#endregion
	}
}
