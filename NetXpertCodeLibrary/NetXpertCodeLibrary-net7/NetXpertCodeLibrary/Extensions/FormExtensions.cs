using System;
using System.Windows.Forms;

namespace NetXpertCodeLibrary.Extensions
{
	public static class FormExtensions
	{
		#region Implement form Un-Minimize functionality.
		[System.Runtime.InteropServices.DllImport( "user32.dll" )]
		private static extern int ShowWindow( IntPtr hWnd, uint Msg );

		/// <summary>Provides an "un-minimize" ability to restore a form to it's prior state (Normal/Maximized) if it is currently minimized.</summary>
		public static void RestoreMinimized(this Form form)
		{
			if ( form.WindowState == FormWindowState.Minimized )
				ShowWindow( form.Handle, 0x09 );
		}
		#endregion
	}
}
