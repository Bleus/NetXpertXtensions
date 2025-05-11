using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace NetXpertCodeLibrary.WinFormsControls.Controls
{
	public partial class UpgradedBinaryViewer : Control
	{
		private int _lastSelectedIndex = -1;
		private int _lastTextSize = -1;

		public UpgradedBinaryViewer()
		{
			InitializeComponent();
			this.ReadOnly = false;
			this.SelectedIndex = 0;
			this.Clear();
		}

		#region Accessors
		public Font RtbFont
		{
			get => this.richTextBox1.Font;
			set => this.richTextBox1.Font = this.richTextBox2.Font = richTextBox3.Font = value;
		}

		/// <summary>Reports the number of bytes currently being managed by this control.</summary>
		public int Count => this.Data.Length;

		//public TabPageCollection TabPages => this.tabControl1.TabPages;

		public int SelectedIndex
		{
			get => this.tabControl1.SelectedIndex;
			set => this.tabControl1.SelectedIndex = value;
		}

		public int BinaryColumns
		{
			get => this.byteViewer1.ColumnCount;
			set => this.byteViewer1.ColumnCount = value;
		}

		public TableLayoutColumnStyleCollection BinaryColumnStyle =>this.byteViewer1.ColumnStyles;

		public bool ReadOnly
		{
			get => this.richTextBox3.ReadOnly;
			set => this.richTextBox3.ReadOnly = value;
		}

		public System.ComponentModel.Design.DisplayMode DisplayMode
		{
			get => this.byteViewer1.GetDisplayMode();
			set => this.byteViewer1.SetDisplayMode( value );
		}

		new public string Text
		{
			get => this.richTextBox3.Text;
			set
			{
				if ( string.IsNullOrEmpty( value ) )
					this.Clear();
				else
				{
					if ( IsBase64String( value ) )
						Base64 = value;
					else
					{
						if ( IsBinaryData( value ) )
						{
							Data = ParseBinaryContent( value );
							//BinaryDataToTextBox( data );
							//this.byteViewer1.SetBytes( data );
							//this.richTextBox2.Text = Convert.ToBase64String( data );
						}
						else
						{
							richTextBox3.Text = value;
							byte[] data = Encoding.UTF8.GetBytes( value );
							BinaryDataToTextBox( data ); // richTextBox1
							richTextBox2.Text = Convert.ToBase64String( data );
							this.byteViewer1.SetBytes( data );
						}
					}
				}
			}
		}

		new public ContextMenuStrip ContextMenuStrip
		{
			get => base.ContextMenuStrip;
			set => this.richTextBox3.ContextMenuStrip = value;
		}

		public byte[] Data
		{
			get => this.byteViewer1.GetBytes();
			set
			{
				if ( (value is null) || (value.Length == 0) )
					this.Clear();
				else
				{
					this.byteViewer1.SetBytes( value );
					BinaryDataToTextBox( value ); // richTextBox1
					richTextBox2.Text = Convert.ToBase64String( value );
					richTextBox3.Text = Encoding.UTF8.GetString( value );
				}	
			}
		}

		public string Base64
		{
			get => this.richTextBox2.Text;
			set
			{
				if ( string.IsNullOrEmpty( value ) )
					this.Clear();
				else
				{
					if ( IsBase64String( value ) )
					{
						this.richTextBox2.Text = value;
						Data = Convert.FromBase64String( value ); // richTextBox1
						richTextBox3.Text = Encoding.UTF8.GetString( Data );
					}
					else throw new FormatException( "The supplied data is not a valid Base64 string." );
				}
			}
		}
		#endregion

		#region Operators
		public static bool operator ==( UpgradedBinaryViewer left, UpgradedBinaryViewer right )
		{
			if ( left is null ) return right is null;
			if ( right is null ) return false;
			return (left.Count == right.Count) && (left.Base64 == right.Base64);
		}

		public static bool operator !=( UpgradedBinaryViewer left, UpgradedBinaryViewer right ) => !(left == right);

		public static bool operator ==( UpgradedBinaryViewer left, string right )
		{
			if ( (left is null) || (left.Data.Length == 0) ) return string.IsNullOrEmpty( right );
			if ( string.IsNullOrEmpty( right ) ) return false;
			return IsBase64String( right ) ? right.Equals(left.Base64,StringComparison.OrdinalIgnoreCase) : right.Equals( left.Text );
		}

		public static bool operator !=( UpgradedBinaryViewer left, string right ) => !(left == right);

		public static bool operator ==( UpgradedBinaryViewer left, byte[] right )
		{
			if ( left is null ) return right is null;
			if ( left.Count == 0 ) return right.Length == 0;
			if ( (right is null) || (right.Length == 0) ) return false;
			if ( left.Count != right.Length ) return false;
			int i = -1; while ( (++i < left.Count) && (left.Data[ i ] == right[ i ]) ) ;
			return i == left.Count;
		}

		public static bool operator !=( UpgradedBinaryViewer left, byte[] right ) => !(left == right);

		public override bool Equals( object obj )
		{
			if ( obj.GetType() == typeof( UpgradedBinaryViewer ) ) return (obj as UpgradedBinaryViewer) == this;
			if ( obj.GetType() == typeof( string ) ) return this == (obj as string);
			if ( obj.GetType() == typeof( byte[] ) ) return this == (obj as byte[]);

			return base.Equals( obj );
		}

		public override int GetHashCode() => this.Data.GetHashCode();
		#endregion

		#region Methods
		// Text is considered 'binary' if it uses the custom markup "[\x##]" or contains control, or high-ascii, characters.
		private static bool IsBinaryData( string text ) =>
			!string.IsNullOrWhiteSpace( text ) && Regex.IsMatch( text.TrimEnd(new char[] { '\0' }), @"(\[\\x[\da-fA-F]{2}\]|[\x00-\x1f]|[\x80-\xff])" );

		private static bool IsBase64String( string source ) =>
			!string.IsNullOrWhiteSpace( source ) && (source.Length % 4 == 0) && Regex.IsMatch( source, @"^[a-zA-Z\d\+\/]*={0,3}$" );

		public void Clear()
		{
			this.richTextBox1.Text = "";
			this.richTextBox2.Text = "";
			this.richTextBox3.Text = "";
			this.byteViewer1.SetBytes( new byte[] { } );
		}

		protected void BinaryDataToTextBox( byte[] data )
		{
			richTextBox1.Text = "";

			foreach ( byte b in data )
				switch ( b )
				{
					case byte n when (n < 33) || (n > 127):
						richTextBox1.SelectionFont = new Font( richTextBox1.Font, FontStyle.Bold );
						richTextBox1.AppendText( $"[\\x{b:X2}]" );
						richTextBox1.SelectionFont = new Font( richTextBox1.Font, FontStyle.Regular );
						break;
					default:
						richTextBox1.AppendText( $"{(char)b}" );
						break;
				}
		}

		protected string BinaryDataToString( byte[] data )
		{
			string rtbText = "";
			foreach ( byte b in data )
				switch ( b )
				{
					case byte n when (n < 33) || (n > 127):
						rtbText += $"[\\x{b:X2}]";
						break;
					default:
						rtbText += $"{(char)b}";
						break;
				}

			return rtbText;
		}

		private byte[] ParseBinaryContent( string source )
		{
			List<byte> _data = new List<byte>();
			Regex parser = new Regex( @"(\[\\x[\da-fA-F]{2}\]|[\x00-\x20\x80-\xff])", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline );
			MatchCollection matches = parser.Matches( source );
			foreach ( Match m in matches )
			{
				if ( Regex.IsMatch( m.Groups[ 0 ].Value, @"(\[\\x[\da-fA-F]{2}\])" ) )
					_data.Add( byte.Parse( Regex.Replace( m.Groups[ 0 ].Value, @"[^a-fA-F\d]", "" ), System.Globalization.NumberStyles.HexNumber ) );
				else
				{
					foreach( Group group in m.Groups )
						if (group.Value.Length == 1)
							_data.Add( (byte)m.Groups[ 0 ].Value[ 0 ] );
						else
							throw new InvalidDataException( $"The source string could not be parsed [{m.Groups[ 0 ].Value}/{_data.Count}]." );
						//if ( m.Groups[ 0 ].Value.Length == 1 )
						//	_data.Add( (byte)m.Groups[ 0 ].Value[ 0 ] );
						//else
						//	throw new InvalidDataException( $"The source string could not be parsed [{m.Groups[ 0 ].Value}/{_data.Count}]." );
				}
			}
			return _data.ToArray();
		}

		private void RichTextBox3_TextChanged( object sender, System.EventArgs e )
		{
			if ( (richTextBox3.Text.Length > _lastTextSize + 2) || (richTextBox3.SelectionStart > _lastSelectedIndex + 2) )
			{
				TabControl1_TabIndexChanged( sender, e );
				_lastSelectedIndex = richTextBox3.SelectionStart;
				_lastTextSize = richTextBox3.Text.Length;
			}
		}

		private void TabControl1_TabIndexChanged( object sender, System.EventArgs e )
		{
			byte[] data = Encoding.UTF8.GetBytes( richTextBox3.Text );
			BinaryDataToTextBox( data ); // richTextBox1
			richTextBox2.Text = Convert.ToBase64String( data );
			this.byteViewer1.SetBytes( data );
		}
		#endregion
	}
}
