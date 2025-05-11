using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using NetXpertExtensions;

namespace NetXpertExtensions.Controls
{
	#nullable disable
	//[TypeDescriptionProvider( typeof( AbstractControlDescriptionProvider<BorderedGroupBox, GroupBox> ) )]
	public class BorderedGroupBox : GroupBox
	{
		private Color _borderColor = Color.Black;
		private int _borderWidth = 2;
		private int _borderRadius = 5;
		private int _textIndent = 10;

		#region Constructors
		public BorderedGroupBox() : base()
		{
			InitializeComponent();
			this._borderColor = base.ForeColor;
			this.Paint += this.BorderedGroupBox_Paint;
		}
		#endregion

		#region Accessors
		/// <summary>In what colour should the border be drawn?</summary>
		public Color BorderColor
		{
			get => this._borderColor;
			set
			{
				this._borderColor = value;
				DrawGroupBox();
			}
		}

		/// <summary>How thick is the border?</summary>
		public int BorderWidth
		{
			get => this._borderWidth;
			set
			{
				if (value > 0)
				{
					this._borderWidth = Math.Min(value, 10);
					DrawGroupBox();
				}
			}
		}

		/// <summary>How much radius should the curved corners use.</summary>
		/// <remarks>Setting this to zero results in square corners.</remarks>
		public int BorderRadius
		{
			get => this._borderRadius;
			set
			{
				if (value >= 0)
				{
					this._borderRadius = value;
					this.DrawGroupBox();
				}
			}
		}

		/// <summary>How far in from the edge should the label be drawn.</summary>
		/// <remarks>Using a negative value indents from the right.</remarks>
		public int LabelIndent
		{
			get => this._textIndent;
			set { this._textIndent = value; this.DrawGroupBox(); }
		}

		/*/ <summary>If set to TRUE, the border is turned off.</summary>
		public bool InvisibleBorder
		{
			get => this._invisibleBorder;
			set 
			{ 
				if (this._invisibleBorder != value) 
				{ 
					this._invisibleBorder = value; 
					this.DrawGroupBox(); 
				} 
			}
		}
		*/

		/// <summary>What text is shown in the BorderedGroupBox caption.</summary>
		/// <remarks>If set to an empty string, no caption is shown.</remarks>
		new public string Text
		{
			get => base.Text;
			set => base.Text = value;
		}
		#endregion

		#region Methods
		private void BorderedGroupBox_Paint( object sender, PaintEventArgs e )
		{
			DrawGroupBox( e.Graphics );

			/*
			if ( NetXpertExtensions.ExecutableName == "devenv" )
			{
				// Code that should only be executed by the designer:
				//base.OnPaint( e );
			}
			*/
		}

		private void DrawGroupBox() =>
			this.DrawGroupBox(this.CreateGraphics());

		private void DrawGroupBox(Graphics g)
		{
			Brush textBrush = new SolidBrush(this.ForeColor);
			SizeF strSize = g.MeasureString(this.Text, this.Font);

			Brush borderBrush = new SolidBrush(this.BorderColor);
			Pen borderPen = new Pen(borderBrush,(float)this._borderWidth);
			Rectangle rect = new Rectangle(this.ClientRectangle.X,
											this.ClientRectangle.Y + (int)(strSize.Height / 2),
											this.ClientRectangle.Width - 1,
											this.ClientRectangle.Height - (int)(strSize.Height / 2) - 1);

			Brush labelBrush = new SolidBrush(this.BackColor);

			// Clear text and border
			g.Clear( this.BackColor );

			// Drawing Border
			int rectX = (0 == this._borderWidth % 2) ? rect.X + this._borderWidth / 2 : rect.X + 1 + this._borderWidth / 2;
			int rectHeight = (0 == this._borderWidth % 2) ? rect.Height - this._borderWidth / 2 : rect.Height - 1 - this._borderWidth / 2;
			g.DrawRoundedRectangle(borderPen, rectX, rect.Y, rect.Width-_borderWidth, rectHeight, (float)this._borderRadius);

			// Draw text
			if (this.Text.Length > 0)
			{
				int width = (int)rect.Width, posX;
				posX = (this._textIndent < 0) ? Math.Max(0-width,this._textIndent) : Math.Min(width, this._textIndent);
				posX = (posX < 0) ? rect.Width + posX - (int)strSize.Width : posX;
				g.FillRectangle(labelBrush, posX, 0, strSize.Width, strSize.Height);
				g.DrawString(this.Text, this.Font, textBrush, posX, 0);
			}
		}
		#endregion

		#region Component Designer generated code
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() =>
			components = new System.ComponentModel.Container();
		#endregion
	}
}
