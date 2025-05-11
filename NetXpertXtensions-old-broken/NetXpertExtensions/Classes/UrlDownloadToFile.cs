using System;
using System.Runtime.InteropServices;

namespace NetXpertExtensions.Classes
{
	class UnssafeNativeMethods
	{
		// The following section adds two new options for scrolling the RichTextBox control more accurately. This was made
		// necessary because the default control method, ".scrollToCaret" performs very inconsistently and sometimes inaccurately,
		// and this class needs tighter control of these operations...

		// The "dynamic" return type allows the compiler to determine the appropriate return type which accomodates the
		// different integer types returned depending on the base platform the application is running on (32 vs 64 bit)

		/// <summary>Used for sending Scroll specific commands to a control via windows message handling.</summary>
		/// <param name="hWnd">The handle of the control to receive the message.</param>
		/// <param name="wMsg">The message to send.</param>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

		/// <summary>
		/// References the external urlmon.dll library to gain access to the URLDownloadToFile function.
		/// This facilitates downloading a specified file from a URL and saving it locally.
		/// </summary>
		/// <param name="pCaller"></param>
		/// <param name="szURL">A string specifying the URL of the file to download.</param>
		/// <param name="szFileName">A string specifying the name of the file to save locally.</param>
		/// <param name="dwReserved"></param>
		/// <param name="lpfnCB"></param>
		[DllImport("urlmon.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern Int32 URLDownloadToFile(
			[MarshalAs(UnmanagedType.IUnknown)] object pCaller,
			[MarshalAs(UnmanagedType.LPWStr)] string szURL,
			[MarshalAs(UnmanagedType.LPWStr)] string szFileName,
			Int32 dwReserved,
			IntPtr lpfnCB);
	}
}