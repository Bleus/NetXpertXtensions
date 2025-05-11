using System.Drawing;
using System.Windows.Forms;

namespace NetXpertCodeLibrary.WinFormsControls.Controls
{
	public partial class TransparentTextBox : TextBox
	{
		public TransparentTextBox()
		{
			InitializeComponent();
			SetStyle( ControlStyles.SupportsTransparentBackColor |
					  ControlStyles.OptimizedDoubleBuffer |
					  ControlStyles.AllPaintingInWmPaint |
					  ControlStyles.ResizeRedraw |
					  ControlStyles.UserPaint, true
					);
			BackColor = Color.Transparent;
		}
	}
}
