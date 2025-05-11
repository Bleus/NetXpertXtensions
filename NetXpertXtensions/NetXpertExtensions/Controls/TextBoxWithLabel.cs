using System.ComponentModel;

namespace NetXpertExtensions.Controls
{
	#nullable disable

	public partial class TextBoxWithLabel : LabelledTextBoxControl<TextBox>
	{
		#region Properties
		[Browsable( true )]
		[Category( "Action" )]
		[Description( "Bubbles up the event from the ComboBox" )]
		public event GenericEventDelegate SelectionChangeCommitted;

		[Browsable( true )]
		[Category( "Action" )]
		[Description( "Bubbles up the event from the ComboBox" )]
		public event GenericEventDelegate SelectedValueChanged;

		[Browsable( true )]
		[Category( "Action" )]
		[Description( "Bubbles up the event from the ComboBox" )]
		public event GenericEventDelegate SelectedIndexChanged;

		[Browsable( true )]
		[Category( "Action" )]
		[Description( "Bubbles up the event from the ComboBox" )]
		public event GenericEventDelegate TextUpdate;

		[Browsable( true )]
		[Category( "Action" )]
		[Description( "Bubbles up the event from the ComboBox" )]
		new public event GenericEventDelegate TextChanged;
		#endregion

		#region Constructor
		public TextBoxWithLabel() : base() => InitializeComponent();
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

		public HorizontalAlignment TextAlign
		{
			get => this.textBox1.TextAlign;
			set => this.textBox1.TextAlign = value;
		}
		#endregion

		#region Methods
		protected override bool ReadOnlyParser( bool? value = null )
		{
			if ( value is not null ) 
				this.textBox1.ReadOnly = (bool)value;

			return this.textBox1.ReadOnly;
		}

		private void TextBox1_SelectionChangeCommitted( object sender, EventArgs e )
		{
			if ( SelectionChangeCommitted is not null )
				SelectionChangeCommitted( sender, e );
		}

		private void TextBox1_SelectedValueChanged( object sender, EventArgs e )
		{
			if ( SelectedValueChanged is not null )
				SelectedValueChanged( sender, e );
		}

		private void TextBox1_SelectedIndexChanged( object sender, EventArgs e )
		{
			if ( SelectedIndexChanged is not null )
				SelectedIndexChanged( sender, e );
		}

		private void TextBox1_TextUpdate( object sender, EventArgs e )
		{
			if ( TextUpdate is not null )
				TextUpdate( sender, e );
		}

		private void TextBox1_TextChanged( object sender, EventArgs e )
		{
			if ( TextChanged is not null )
				TextChanged( sender, e );
		}
		#endregion
	}
}
