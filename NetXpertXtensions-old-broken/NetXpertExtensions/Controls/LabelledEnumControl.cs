
using System;

namespace NetXpertExtensions.Controls
{
	#nullable disable

	public class LabelledEnumControl<T> : ComboBoxWithLabel where T : Enum
	{
		#region Properties
		protected TranslationTable<T> _translations;
		#endregion

		#region Constructors
		public LabelledEnumControl() : base()
		{
			InitializeComponent();
			this._translations = TranslationTable<T>.Create();
			this.LabelText = typeof( T ).Name;
			this.PopulateFromEnum<T>( this._translations );
		}

		public LabelledEnumControl( T value )
		{
			InitializeComponent();
			this._translations = TranslationTable<T>.Create();
			this.LabelText = typeof( T ).Name;
			this.PopulateFromEnum<T>( this._translations );
			this.Value = value;
		}

		public LabelledEnumControl( T value, TranslationTable<T> translations ) : base()
		{
			InitializeComponent();
			this._translations = translations;
			this.LabelText = typeof( T ).Name;
			this.PopulateFromEnum( translations );
			this.Value = value;
		}
		#endregion

		#region Accessors
		public T Value
		{
			get => EnumeratedValue<T>( this._translations );
			set => EnumeratedValue( value, this._translations );
		}

		public TranslationTable<T> TranslationTable => this._translations;
		#endregion

		#region Designer required code.
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components; // = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing )
		{
			if ( disposing && (components != null) )
			{
				components.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		}

		#endregion
		#endregion
	}
}
