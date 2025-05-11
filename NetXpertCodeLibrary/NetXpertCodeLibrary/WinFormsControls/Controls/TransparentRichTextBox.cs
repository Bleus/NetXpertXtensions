using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace NetXpertCodeLibrary.WinForms.Controls
{
	public partial class TransparentRichTextBox : RichTextBox
	{
		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		static extern IntPtr LoadLibrary(string lpFileName);

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams prams = base.CreateParams;
				if (LoadLibrary("msftedit.dll") != IntPtr.Zero)
				{
					prams.ExStyle |= 0x020; // transparent
					prams.ClassName = "RICHEDIT50W";
				}
				return prams;
			}
		}
	}
}
