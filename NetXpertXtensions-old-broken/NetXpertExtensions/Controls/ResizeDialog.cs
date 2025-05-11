using System.ComponentModel;
using System.Drawing.Imaging;
using System.Media;

namespace NetXpertExtensions.Controls
{
	public partial class ResizeDialog : Form
	{
		private readonly Image _original;
		private Size _newSize;
		private bool _sizeInPixels = true;

		protected ResizeDialog()
		{
			this._original = new Bitmap( 1, 1 );
			this._newSize = this._original.Size;
			InitializeComponent();
		}

		public ResizeDialog( Image source, FileInfo file )
		{
			this._original = source;
			this._newSize = source.Size;
			this.ImageType = Path.GetExtension( file.FullName ).ToLowerInvariant() switch
			{
				".png" => ImageFormat.Png,
				".gif" => ImageFormat.Gif,
				".jpg" or ".jpeg" or ".jpe" => ImageFormat.Jpeg,
				".tif" or ".tiff" => ImageFormat.Tiff,
				".ico" => ImageFormat.Icon,
				".bmp" => ImageFormat.Bmp,
				".emf" => ImageFormat.Emf,
				".wmf" => ImageFormat.Wmf,
				".webp" or ".web" => ImageFormat.Webp,
				_ => throw new NotSupportedException( $"The image type of \"{Path.GetFileName( file.FullName )}\" could not be determined." )
			};
			InitializeComponent();
			this.Image = source;
			this.toolStripFileName.Text = file.Name;
			this.aspectLockCheckBox.Checked = true;
			this.toolStripProgressBar.Visible = true;
			this.widthTextBox.Focus();
		}

		public Image Image
		{
			get => this.thumbnail.Image;
			set
			{
				if ( value is null ) throw new ArgumentNullException( nameof( value ) );

				this.thumbnail.Image = value;
				this.dimensionsDisplay.Text = $"{value.Width:N0} x {value.Height:N0}";
				this.sizeDisplay.Text = Image.GetSize( ImageType ).FileSizeToString();
				this.thumbnail.Image = value;
				this.widthTextBox.Text = $"{value.Width}";
				this.heightTextBox.Text = $"{value.Height}";
			}
		}

		public bool SizeInPixels
		{
			get => this._sizeInPixels;
			set
			{
				if ( value != this._sizeInPixels )
				{
					this._sizeInPixels = value;

					label1.Text = label8.Text = value ? $"px" : "%";
					widthTextBox.TextBoxMaxLength = heightTextBox.TextBoxMaxLength = value ? 6 : 3;
					widthTextBox.Value = value ? $"{(WidthAsInt / 100f) * _original.Width}" : $"{(int)((WidthAsInt / _original.Width) * 100)}";
					heightTextBox.Value = value ? $"{(HeightAsInt / 100f) * _original.Height}" : $"{(int)((HeightAsInt / _original.Height) * 100)}";
				}
			}
		}

		private int WidthAsInt => int.Parse( widthTextBox.Value );

		private int HeightAsInt => int.Parse( heightTextBox.Value );

		public float AspectRatio => (float)this.Image.Width / (float)this.Image.Height;

		public ImageFormat ImageType { get; protected set; } = ImageFormat.MemoryBmp;

		// Validates input keystrokes for image dimensions.
		private void textBoxWithLabel1_KeyPress( object sender, KeyPressEventArgs e )
		{
			switch ( e.KeyChar )
			{
				case '0':
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
				case '\x08': // backspace
				case '\x0d': // Enter
					break;
				default:
					e.Handled = true;
					break;
			}
			return;
		}

		private SizeF CalcSizeF() =>
			new(
				(float)(SizeInPixels ? WidthAsInt : Math.Round( (WidthAsInt / 100f) * Image.Width )),
				(float)(SizeInPixels ? HeightAsInt : Math.Round( (HeightAsInt / 100f) * Image.Height ))
			);

		private Size CalcSize() =>
			new(
				SizeInPixels ? WidthAsInt : (int)Math.Round( (WidthAsInt / 100f) * Image.Width ),
				SizeInPixels ? HeightAsInt : (int)Math.Round( (HeightAsInt / 100f) * Image.Height )
			);

		// Calculate new Width value from the changed Height when the aspect ratio must be maintained.
		private void textBoxWithLabel2_KeyUp( object sender, KeyEventArgs e )
		{
			if ( aspectLockCheckBox.Checked ) // Maintain aspect ratio
			{
			}
		}

		// Calculate new Height value from the changed Width when the aspect ratio must be maintained.
		private void textBoxWithLabel1_KeyUp( object sender, KeyEventArgs e )
		{
			if ( aspectLockCheckBox.Checked ) // Maintain aspect ratio
			{
			}
		}

		// Mirror
		private void checkBox2_CheckedChanged( object sender, EventArgs e )
		{
			Bitmap obj = (Bitmap)this.flipPicOriginal.Image.Clone();
			if ( invertTextBox.Checked )
				obj.RotateFlip( RotateFlipType.RotateNoneFlipY );

			if ( mirrorCheckBox.Checked )
				obj.RotateFlip( RotateFlipType.RotateNoneFlipX );

			applyButton.Enabled |= mirrorCheckBox.Checked;
			flipPicResult.Image = obj;
			flipPicResult.Invalidate();
			this.Refresh();
		}

		// Invert
		private void checkBox3_CheckedChanged( object sender, EventArgs e )
		{
			Bitmap obj = (Bitmap)this.flipPicOriginal.Image.Clone();
			if ( mirrorCheckBox.Checked )
				obj.RotateFlip( RotateFlipType.RotateNoneFlipX );

			if ( invertTextBox.Checked )
				obj.RotateFlip( RotateFlipType.RotateNoneFlipY );

			applyButton.Enabled |= invertTextBox.Checked;
			flipPicResult.Image = obj;
			flipPicResult.Invalidate();
			this.Refresh();
		}

		private void backgroundWorker1_DoWork( object sender, System.ComponentModel.DoWorkEventArgs e )
		{
			((BackgroundWorker)sender).ReportProgress( 0 );
			void Enable( Control obj, bool state = true ) => obj.Invoke( x => x.Enabled = state );
			var group = new Control[ 8 ] { widthTextBox, heightTextBox, aspectLockCheckBox, mirrorCheckBox, invertTextBox, applyButton, cancelButton, acceptButton };
			foreach ( var c in group ) Enable( c, false );

			Bitmap work = (Bitmap)Image.Clone();
			((BackgroundWorker)sender).ReportProgress( 15 );
			if ( work != null )
			{
				work = work.ResizeTo( CalcSizeF(), null, aspectLockCheckBox.Checked );
				((BackgroundWorker)sender).ReportProgress( 25 );
				if ( mirrorCheckBox.Checked )
					work.RotateFlip( RotateFlipType.RotateNoneFlipX );

				((BackgroundWorker)sender).ReportProgress( 50 );
				if ( invertTextBox.Checked )
					work.RotateFlip( RotateFlipType.RotateNoneFlipY );

				Image = work;
				flipPicResult.Image = (Image)flipPicOriginal.Image.Clone();
			}
			mirrorCheckBox.Invoke( x => x.Checked = false );
			invertTextBox.Invoke( x => x.Checked = false );
			foreach ( var c in group ) Enable( c, true );
			((BackgroundWorker)sender).ReportProgress( 100 );
		}

		private void backgroundWorker1_ProgressChanged( object sender, System.ComponentModel.ProgressChangedEventArgs e ) =>
			this.toolStripProgressBar.Value = e.ProgressPercentage;

		private void backgroundWorker1_RunWorkerCompleted( object sender, System.ComponentModel.RunWorkerCompletedEventArgs e )
		{
			this.toolStripProgressBar.Value = 0;
			this.toolStripProgressBar.Visible = false;
		}

		private void button1_Click( object sender, EventArgs e )
		{
			this.toolStripProgressBar.Value = 0;
			this.toolStripProgressBar.Visible = false;
			this.backgroundWorker1.RunWorkerAsync();
		}

		private void textBoxWithLabel_Enter( object sender, EventArgs eventArgs ) =>
			((TextBoxWithLabel)sender).SelectAll();

		private void textBoxWithLabel_Leave( object sender, EventArgs eventArgs )
		{
			var obj = (TextBoxWithLabel)sender;
			if ( obj.Length == 0 )
			{
				SystemSounds.Exclamation.Play();
				obj.Text = obj.LabelText == "Width:" ? $"{Image.Width}" :  $"{Image.Height}";
				obj.Focus();
			}
			else
				applyButton.Enabled |= !(CalcSize() == Image.Size);
		}

		private void label_DoubleClick( object sender, EventArgs e ) =>
			SizeInPixels = ((Label)sender).Text == "%";
	}
}
