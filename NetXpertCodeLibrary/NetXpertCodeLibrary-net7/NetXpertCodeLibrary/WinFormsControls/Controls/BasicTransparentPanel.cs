using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetXpertCodeLibrary.WinFormsControls.Controls
{
	public partial class BasicTransparentPanel : Panel
	{
        private byte _alpha = 0;

        /// <summary>Amount of alpha blending to apply to the background color of the panel.</summary>
        public byte Alpha 
        {
            get => _alpha;
            set
			{
                _alpha = value;
                this.Refresh();
			}
        }

        // These settings hide background image settings from the Panel from being used by this control.
        new private Image BackgroundImage { get => null; set => base.BackgroundImage = null; }

        new private ImageLayout BackgroundImageLayout { get => ImageLayout.None; set => base.BackgroundImageLayout = ImageLayout.None; }

        public BasicTransparentPanel() =>
            InitializeComponent();

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000020; // WS_EX_TRANSPARENT
                return cp;
            }
        }

        protected override void OnPaintBackground( PaintEventArgs e )
        {
            //base.OnPaintBackground(e);
            e.Graphics.FillRectangle( new SolidBrush( Color.FromArgb( Alpha, BackColor ) ), this.ClientRectangle );
        }
    }
}
