using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Cobblestone.Forms
{
	[TypeDescriptionProvider(typeof(VS_AbstractionOverride.AbstractControlDescriptionProvider<CobblestoneBaseForm, Form>))]
	internal abstract class CobblestoneBaseForm : FormManagementObject
	{
		protected Size _scale;
		protected Size _originalSize;

		private CobblestoneBaseForm() : base("Constructor:Designer") { }

		public CobblestoneBaseForm(string name) : base(name)
		{
			this.InitializeComponent();
			this.ResizeRedraw = true;
		}

		protected  CobblestoneBaseForm GetOtherInstance()
		{
			return Application.OpenForms.OfType<CobblestoneBaseForm>().FirstOrDefault();
		}

		public static Color DarkBrown { get { return Color.FromArgb(65, 42, 29); } }
		public static Color MedBrown { get { return Color.FromArgb(97, 74, 61); } }
		public static Color LightBrown { get { return Color.FromArgb(128, 106, 93); } }
		public static Color DarkGold { get { return Color.FromArgb(255, 215, 0); } }
		public static Color LightBlue { get { return Color.FromArgb(0, 200, 255); } }

		protected override void OnBackgroundImageChanged(EventArgs e)
		{
			this._originalSize = new Size(this.BackgroundImage.Width, this.BackgroundImage.Height);
			base.OnBackgroundImageChanged(e);
		}

		protected override void OnPaintBackground(PaintEventArgs e)
		{
			if ((BackgroundImage != null) && (this.BackgroundImageLayout == ImageLayout.None))
			{
				Point p = new Point(
								(int)((this.Width - this._scale.Width) / 2),
								(int)((this.Height - this._scale.Height) / 2)
								);

				e.Graphics.DrawImage(this.BackgroundImage, new Rectangle(p, this._scale));
			}
			else
				base.OnPaintBackground(e);
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			if ((BackgroundImage != null) && (this.BackgroundImageLayout == ImageLayout.None))
			{
				float ratioWidth = (float) this.Width / this._originalSize.Width;
				float ratioHeight = (float) this.Height / this._originalSize.Height;
				float ratio = (ratioWidth > ratioHeight) ? ratioWidth : ratioHeight;

				this._scale.Height = (int)(this._originalSize.Height * ratio);
				this._scale.Width = (int)(this._originalSize.Width * ratio);
			}

			base.OnSizeChanged(e);
		}

		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CobblestoneBaseForm));
			this.SuspendLayout();
			// 
			// CobblestoneBaseForm
			// 
			this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(106)))), ((int)(((byte)(93)))));
			this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
			this.ClientSize = new System.Drawing.Size(632, 463);
			this.DoubleBuffered = true;
			this.Name = "CobblestoneBaseForm";
			this.ResumeLayout(false);

		}
	}
}
