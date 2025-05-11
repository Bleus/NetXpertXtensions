
using System;
using System.Windows.Forms;

namespace NetXpertExtensions.Controls
{
	public partial class DateBoxWithLabel : LabelledInputControl<DateTimePicker>
	{
		public DateBoxWithLabel() : base()
		{
			InitializeComponent();
		}

		public DateTime Value
		{
			get => this.textBox1.Value;
			set => this.textBox1.Value = value; 
		}

		protected override bool ReadOnlyParser( bool? value = null )
		{
			if ( value is not null )
				this.textBox1.Enabled = (bool)value;

			return this.textBox1.Enabled;
		}
	}
}
