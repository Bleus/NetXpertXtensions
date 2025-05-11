using System.Drawing.Drawing2D;

namespace NetXpertExtensions.Controls
{
	public partial class RoundedPanel : Panel
	{
		protected Color _borderColor;
		protected int _borderWidth = 10;
		protected int _radius = 5;
		protected Color _backgroundColor = Color.LightGray;

		public RoundedPanel() : base()
		{
			InitializeComponent();
			this.ResizeRedraw = true;
			base.BackColor = Color.Transparent;
			base.BorderStyle = BorderStyle.None;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			this.PaintRoundedPanel(e.Graphics);
			base.OnPaint(e);
		}

		public int BorderWidth
		{
			get => this._borderWidth;
			set
			{
				this._borderWidth = (value < 40) ? Math.Max(0, value) : 40;
				this.PaintRoundedPanel();
			}
		}

		public Color BorderColor
		{
			get => this._borderColor;
			set
			{
				this._borderColor = value;
				this.PaintRoundedPanel();
			}
		}

		public int Radius
		{
			get => this._radius;
			set
			{
				this._radius = (value < 40) ? Math.Max(0, value) : 40;
				this.PaintRoundedPanel();
			}
		}

		new public Color BackColor
		{
			get => this._backgroundColor;
			set
			{
				this._backgroundColor = value;
				this.PaintRoundedPanel();
			}
		}

		new protected BorderStyle BorderStyle
		{
			get { return base.BorderStyle; }
		}

		protected void PaintRoundedPanel() => this.PaintRoundedPanel(this.CreateGraphics());

		protected void PaintRoundedPanel(Graphics e)
		{
			e.SmoothingMode = SmoothingMode.AntiAlias;
			if (this._borderWidth > 0)
			{
				e.FillRoundedRectangle(new SolidBrush(this._borderColor), 0, 0, (float)this.Width, (float)this.Height, (float)this._radius);
				//e.Graphics.FillRoundedRectangle(new SolidBrush(this._borderColor), 12, 12, this.Width - 44, this.Height - 64, (float)this._radius);
				e.DrawRoundedRectangle(new Pen(ControlPaint.Light(this._backgroundColor, 0.00f)), this._borderWidth, this._borderWidth, this.Width - (this._borderWidth * 2), this.Height - (this._borderWidth * 2), (float)this._radius);
				e.FillRoundedRectangle(new SolidBrush(this._backgroundColor), this._borderWidth, this._borderWidth, this.Width - (this._borderWidth * 2), this.Height - (this._borderWidth * 2), (float)this._radius * 0.90F);
			}
			else
			{
				e.DrawRoundedRectangle(new Pen(ControlPaint.Light(this._backgroundColor, 0.00f)), 0, 0, (float)this.Width, (float)this.Height, (float)this._radius);
				e.FillRoundedRectangle(new SolidBrush(this._backgroundColor), 0, 0, (float)this.Width, (float)this.Height, (float)this._radius);
			}
		}
	}
}
