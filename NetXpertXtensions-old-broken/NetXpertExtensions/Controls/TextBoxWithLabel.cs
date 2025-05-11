using System.ComponentModel;
using System.Configuration;

namespace NetXpertExtensions.Controls
{
	#nullable disable

	public partial class TextBoxWithLabel : LabelledTextBoxControl<TextBox>
	{
		#region Properties
		#region Bubble Events
		[Browsable( true )]
		[Category( "Action" )]
		[Description( "Bubbles up the event from the TextBox" )]
		new public event GenericEventDelegate TextChanged;

		[Browsable( true )]
		[Category( "Action" )]
		[Description(  "Bubbles up the event from the TextBox")]
		new public event KeyPressEventHandler KeyPress;

		[Browsable( true )]
		[Category( "Action" )]
		[Description( "Bubbles up the event from the TextBox" )]
		new public event KeyEventHandler KeyUp;

		[Browsable( true )]
		[Category( "Action" )]
		[Description( "Bubbles up the event from the TextBox" )]
		new public event KeyEventHandler KeyDown;

		[Browsable( true )]
		[Category( "Action" )]
		[Description( "Bubbles up the event from the TextBox" )]
		new public event PreviewKeyDownEventHandler PreviewKeyDown;

		[Browsable ( true )]
		[Category( "Action" )]
		[Description( "Bubbles up the event from the TextBox" )]
		new public event GenericEventDelegate Enter;

		[Browsable( true )]
		[Category( "Action" )]
		[Description( "Bubbles up the event from the TextBox" )]
		new public event GenericEventDelegate Leave;

		#endregion

		protected bool _autoSize = true;
		protected Size _textBoxSize;
		protected bool _labelLeft = true;
		#endregion

		#region Constructor
		public TextBoxWithLabel() : base()
		{
			this.Size = new( 150, 12 );
			this._textBoxSize = new( this.Size.Width - this.label1.Width - 5, this.textBox1.Height );
			InitializeComponent();

			this.textBox1.KeyPress += this.TextBox1_KeyPress;
			this.textBox1.KeyDown += this.TextBox1_KeyDown;
			this.textBox1.KeyUp += this.TextBox1_KeyUp;
			this.textBox1.PreviewKeyDown += this.TextBox1_PreviewKeyDown;
			this.textBox1.TextChanged += this.TextBox1_TextChanged;
			this.textBox1.Enter += this.TextBox1_Enter;
			this.textBox1.Leave += this.TextBox1_Leave;
			this.label1.AutoSize = true;
			this.textBox1.Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
			this.label1.Anchor = AnchorStyles.Left | AnchorStyles.Top;
			this.Resize += this.TextBoxWithLabel_Resize;
			this.label1.TextAlign = ContentAlignment.TopLeft;
		}
		#endregion

		#region Accessors
		public AutoCompleteMode AutoCompleteMode
		{
			get => this.textBox1.AutoCompleteMode;
			set => this.textBox1.AutoCompleteMode = value;
		}

		public AutoCompleteSource AutoCompleteSource
		{
			get => this.textBox1.AutoCompleteSource;
			set => this.textBox1.AutoCompleteSource = value;
		}

		public AutoCompleteStringCollection AutoCompleteCustomSource
		{
			get => this.textBox1.AutoCompleteCustomSource;
			set => this.textBox1.AutoCompleteCustomSource = value;
		}

		public HorizontalAlignment TextBoxAlign
		{
			get => this.textBox1.TextAlign;
			set => this.textBox1.TextAlign = value;
		}

		public BorderStyle TextBoxBorderaStyle
		{
			get => this.textBox1.BorderStyle;
			set => this.textBox1.BorderStyle = value;
		}

		new public string SelectedText
		{
			get => this.textBox1.SelectedText;
			set => this.textBox1.SelectedText = value;
		}

		new public int SelectionStart
		{
			get => this.textBox1.SelectionStart;
			set => this.textBox1.SelectionStart = value;
		}

		/// <summary>Reports the length of the text contained in the <seealso cref="TextBox"/>.</summary>
		public int Length => this.textBox1.Text.Length;

		public bool LabelOnLeft
		{
			get => this._labelLeft;
			set
			{
				if (value != this._labelLeft)
				{
					this.label1.Anchor = AnchorStyles.Top | (value ? AnchorStyles.Left : AnchorStyles.Right);
					this.label1.Location = new( value ? 0 : this.Width - this.label1.Width );
					this.textBox1.Anchor = AnchorStyles.Top | ( value ? AnchorStyles.Right : AnchorStyles.Left );
					this.textBox1.Location = new( value ? this.Width - this.textBox1.Width : 0, 0 );
					this._labelLeft = value;
					this.Invalidate();
				}
			}
		}

		new public bool AutoSize
		{
			get => this._autoSize;
			set
			{
				this._autoSize = value;
				this.textBox1.Size = value ? new( this.Size.Width - this.label1.Width - 5, this.textBox1.Height ) : this._textBoxSize;
			}
		}

		public Size TextBoxSize
		{
			get => this.textBox1.Size;
			set
			{
				this._textBoxSize = value;
				if ( !AutoSize ) this.textBox1.Size = value;
			}
		}

		public bool MultiLine
		{
			get => this.textBox1.Multiline;
			set => this.textBox1.Multiline = value;
		}

		public Size TextBoxMaxSize
		{
			get => this.textBox1.MaximumSize;
			set => this.textBox1.MaximumSize = value;
		}

		public Size TextBoxMinSize
		{
			get => this.textBox1.MinimumSize;
			set => this.textBox1.MinimumSize = value;
		}

		public string Value
		{
			get => this.textBox1.Text;
			set => this.textBox1.Text = value;
		}

		public int TextBoxMaxLength
		{
			get => this.textBox1.MaxLength;
			set => this.textBox1.MaxLength = value;
		}
		#endregion

		#region Methods
		private void TextBoxWithLabel_Resize( object sender, EventArgs e )
		{
			if ( AutoSize )
				this.textBox1.Size = new( this.Size.Width - this.label1.Width - 5, this.textBox1.Height );
		}

		protected override bool ReadOnlyParser( bool? value = null )
		{
			if ( value is not null ) 
				this.textBox1.ReadOnly = (bool)value;

			return this.textBox1.ReadOnly;
		}

		private void TextBox1_Leave( object sender, EventArgs e )
		{
			if ( Leave is not null ) Leave( this, e );
		}

		private void TextBox1_Enter( object sender, EventArgs e )
		{
			if ( Enter is not null ) Enter( this, e );
		}

		private void TextBox1_TextChanged( object sender, EventArgs e )
		{
			if ( TextChanged is not null ) TextChanged( this, e );
		}

		private void TextBox1_PreviewKeyDown( object sender, PreviewKeyDownEventArgs e )
		{
			if ( PreviewKeyDown is not null ) PreviewKeyDown( this, e );
		}

		private void TextBox1_KeyUp( object sender, KeyEventArgs e )
		{
			if ( KeyUp is not null ) KeyUp( this, e );
		}

		private void TextBox1_KeyDown( object sender, KeyEventArgs e )
		{
			if ( KeyDown is not null ) KeyDown( this, e );
		}

		private void TextBox1_KeyPress( object sender, KeyPressEventArgs e )
		{
			if ( KeyPress is not null ) KeyPress( this, e );
		}

		new public bool Focus() => this.textBox1.Focus();

		new public void SelectAll() => this.textBox1.SelectAll();
		#endregion
	}
}
