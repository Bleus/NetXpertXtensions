using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace NetXpertExtensions.Controls
{
	#nullable disable

	public abstract partial class LabelledInputControl<T> : UserControl where T : Control, new()
	{
		#region Properties
		public delegate void GenericEventDelegate( object sender, EventArgs eventArgs );

		[Browsable( true )]
		[Category( "Action" )]
		[Description( "Facilitates capturing the input control's 'Enter' event." )]
		new public event GenericEventDelegate Enter;

		[Browsable( true )]
		[Category( "Action" )]
		[Description( "Facilitates capturing the input control's 'Leave' event." )]
		new public event GenericEventDelegate Leave;

		[Browsable( true )]
		[Category( "Action" )]
		[Description( "Facilitates capturing the input control's 'TextChanged' event." )]
		new public event GenericEventDelegate TextChanged;

		[Flags]
		protected enum InputType { Unknown, TextBox, RichTextBox, MaskedTextBox, ComboBox };
		#endregion

		#region Constructors
		public LabelledInputControl()
		{
			InitializeComponent();

			this.SizeChanged += TextBoxWithLabel_SizeChanged;
			this.label1.AutoSize = true;
			this.textBox1.Anchor = AnchorStyles.None;
			this.textBox1.Enter += this.TextBox1_Enter;
			this.textBox1.Leave += this.TextBox1_Leave;
			this.textBox1.TextChanged += this.TextBox1_TextChanged;
			this.label1.Anchor = AnchorStyles.None;
			this.Size = new( 300, 23 );
		}
		#endregion

		#region Accessors
		new public Font Font
		{
			get => base.Font;
			set
			{
				base.Font = value;
				this.label1.Font = value;
				this.ResetControls();
			}
		}

		new public bool Enabled
		{
			get => this.textBox1.Enabled;
			set => this.textBox1.Enabled = value;
		}

		public bool ReadOnly
		{
			get => this.ReadOnlyParser();
			set => this.ReadOnlyParser( value );
		}

		public string LabelText
		{
			get => this.label1.Text;
			set
			{
				this.label1.Text = value;
				this.ResetControls();
			}

		}

		new public Color ForeColor
		{
			get => base.ForeColor;
			set
			{
				base.ForeColor = value;
				this.label1.ForeColor = value;
			}
		}

		new public Color BackColor
		{
			get => base.BackColor;
			set
			{
				base.BackColor = value;
				this.label1.BackColor = value;
			}
		}

		public Color InputForeColor
		{
			get => this.textBox1.ForeColor;
			set => this.textBox1.ForeColor = value;
		}

		public Color InputBackColor
		{
			get => this.textBox1.BackColor;
			set => this.textBox1.BackColor = value;	
		}

		public Font InputFont
		{
			get => this.textBox1.Font;
			set
			{
				this.textBox1.Font = value;
				ResetControls();
			}
		}

		new public string Text
		{
			get => this.textBox1.Text;
			set => this.textBox1.Text = value;
		}

		public Size InputMaximumSize
		{
			get => this.textBox1.MaximumSize;
			set
			{
				this.textBox1.MaximumSize = value;
				ResetControls();
			}
		}

		protected InputType MyInputType
		{
			get
			{
				// Classes derived from TextBoxBase()...
				if ( typeof( T ) == typeof( TextBox ) || typeof( T ).HasAncestor<TextBox>() )
					return InputType.TextBox;

				if ( typeof( T ) == typeof( RichTextBox ) || typeof( T ).HasAncestor<RichTextBox>() )
					return InputType.RichTextBox;

				if ( typeof( T ) == typeof( MaskedTextBox ) || typeof( T ).HasAncestor<MaskedTextBox>() )
					return InputType.MaskedTextBox;

				// Other classes that support SelectAll()...
				if ( typeof( T ) == typeof( ComboBox ) || typeof( T ).HasAncestor<ComboBox>() )
					return InputType.ComboBox;

				return InputType.Unknown;
			}
		}

		//new public bool AllowDrop
		//{
		//	get => this.textBox1.AllowDrop;
		//	set
		//	{
		//		this.textBox1.AllowDrop =
		//		this.label1.AllowDrop =
		//		value;
		//	}
		//}
		#endregion

		#region Methods
		protected void ResetControls()
		{
			this.label1.Location = new( 0, (this.Height - this.label1.Height) / 2 );
			this.textBox1.Size = new( this.Width - this.label1.Width - 5, this.textBox1.Height );
			this.textBox1.Location = new( this.Width - textBox1.Width, (this.Height - this.textBox1.Height) / 2 );
		}

		private void TextBoxWithLabel_SizeChanged( object sender, EventArgs e ) =>
			ResetControls();

		new public virtual void Select() => this.textBox1.Select();

		public virtual void SelectAll()
		{
			#pragma warning disable CS8602 // Dereference of a possibly null reference.
			switch (MyInputType)
			{
				// TextBoxBase derived classes...
				case InputType.TextBox:
				case InputType.RichTextBox:
				case InputType.MaskedTextBox:
					(this.textBox1 as TextBoxBase).SelectAll();
					break;

				// Other classes that support SelectAll()...
				case InputType.ComboBox:
					(this.textBox1 as ComboBox).SelectAll();
					break;

				default: 
					throw new NotImplementedException( $"No 'SelectAll()' functionality has been defined for objects of type \x22{typeof( T ).FullName}\x22." );
			}
			#pragma warning restore CS8602 // Dereference of a possibly null reference.
		}

		new public virtual void Focus() => this.textBox1.Focus();

		protected abstract bool ReadOnlyParser( bool? value = null );

		public override string ToString() =>
			$"{this.label1.Text}: \x22{this.textBox1.Text}\x22";

		private void TextBox1_Leave( object sender, EventArgs e )
		{
			if ( Leave is not null )
				Leave( sender, e );
		}

		private void TextBox1_Enter( object sender, EventArgs e )
		{
			if ( Enter is not null )
				Enter( sender, e );
		}

		private void TextBox1_TextChanged( object sender, EventArgs e )
		{
			if ( TextChanged is not null )
				TextChanged( sender, e );
		}
		#endregion
	}

	public abstract class LabelledTextBoxControl<T> : LabelledInputControl<T> where T : TextBoxBase, new()
	{
		#region Properties
		public enum DropDataType { Unknown, Text, Unicode, WindowsString, OemText, Html, Rtf, Csv, File };
		#endregion

		#region Constructor
		public LabelledTextBoxControl() : base() 
		{
			this.DragDrop += this.LabelledInputControl_DragDrop;
			this.DragOver += this.LabelledInputControl_DragOver;
		}
		#endregion

		#region Accessors
		public int SelectionStart
		{
			get => this.textBox1.SelectionStart;
			set => this.textBox1.SelectionStart = value;
		}

		public int SelectionLength
		{
			get => this.textBox1.SelectionLength;
			set => this.textBox1.SelectionLength = value;
		}

		public string SelectedText
		{
			get => this.textBox1.SelectedText;
			set => this.textBox1.SelectedText = value;
		}

		public bool HideSelection
		{
			get => this.textBox1.HideSelection;
			set => this.textBox1.HideSelection = value;
		}
		#endregion

		#region Methods
		public DropDataType WhatTypeIsThis( IDataObject data )
		{
			DropDataType result = DropDataType.Unknown;
			if ( data is not null )
			{
				if ( data.GetDataPresent( DataFormats.StringFormat ) ) result &= DropDataType.WindowsString;
				if ( data.GetDataPresent( DataFormats.Text ) ) result &= DropDataType.Text;
				if ( data.GetDataPresent( DataFormats.UnicodeText ) ) result &= DropDataType.Unicode;
				if ( data.GetDataPresent( DataFormats.CommaSeparatedValue ) ) result &= DropDataType.Csv;
				if ( data.GetDataPresent( DataFormats.Rtf ) ) result &= DropDataType.Rtf;
				if ( data.GetDataPresent( DataFormats.Html ) ) result &= DropDataType.Html;
				if ( data.GetDataPresent( DataFormats.OemText ) ) result &= DropDataType.OemText;
				if ( data.GetDataPresent( DataFormats.Rtf ) ) result &= DropDataType.Rtf;
				if ( data.GetDataPresent( DataFormats.FileDrop ) ) result &= DropDataType.File;
			}

			return result;
		}

		public override void SelectAll() => this.textBox1.SelectAll();

		public void Cut() => this.textBox1.Cut();

		public void Copy() => this.textBox1.Copy();

		new public bool Focus() => this.textBox1.Focus();

		private void LabelledInputControl_DragOver( object sender, DragEventArgs e ) =>
			e.Effect = this.AllowDrop && WhatTypeIsThis(e.Data) > 0 ? DragDropEffects.Copy : DragDropEffects.None;

		private void LabelledInputControl_DragDrop( object sender, DragEventArgs e )
		{
			void InsertText( object data )
			{
				string d = data is null ? string.Empty : data as string;

				if ( this.SelectionLength > 0 )
					this.textBox1.SelectedText = d;
				else
					this.textBox1.Text = d;
			}

			if ( this.AllowDrop )
			{
				DropDataType ddt = WhatTypeIsThis( e.Data );
				if ( ddt.HasFlag( DropDataType.Rtf ) ) 
				{
					if ( typeof( T ) == typeof( RichTextBox ) )
						(this.textBox1 as RichTextBox).Rtf = e.Data.GetData( DataFormats.Rtf ) as string;
					else
					{
						RichTextBox worker = new();
						worker.Rtf = e.Data.GetData( DataFormats.Rtf ) as string;
						InsertText( worker.Text );
					}
				}

				if ( ddt.HasFlag( DropDataType.WindowsString ) )
					InsertText( e.Data.GetData( DataFormats.StringFormat ) );

				if ( ddt.HasFlag( DropDataType.Text ) )
					InsertText( e.Data.GetData( DataFormats.Text ) );

				if ( ddt.HasFlag( DropDataType.Html ) )
					InsertText( e.Data.GetData( DataFormats.Html ) );

				if ( ddt.HasFlag( DropDataType.Csv ) )
					InsertText( e.Data.GetData( DataFormats.CommaSeparatedValue ) );

				if (ddt.HasFlag( DropDataType.File ) )
				{
					string[] f = (string[])e.Data.GetData( DataFormats.FileDrop );
					if ( f.Length > 0 )
					{
						var fi = new FileInfo( f[ 0 ] );
						if ( IsBinaryFile( f[ 0 ] ) )
							InsertText( $"[{fi.FullName} ({fi.Length.FileSizeToString()})]" );
						else
						{
							string data = File.ReadAllText( f[ 0 ] );
							InsertText( data );
						}
					}
				}
			}
		}
		#endregion

		private static bool IsBinaryFile( string file, byte threshold = 5 )
		{
			FileInfo fi = new( file );
			byte ctrlCount = 0;
			long length = fi.Length;
			if ( length == 0 ) return false;

			using ( StreamReader stream = new( file ) )
			{
				int ch; while ( (ch = stream.Read()) != -1 )
					ctrlCount += (byte)(ch.InRange( 0x08, 0x00 ) || ch.InRange( 0x1a, 0x0e ) ? 1 : 0);
						
				if (ctrlCount > threshold) return true;
			}
			return false;
		}
	}
}
