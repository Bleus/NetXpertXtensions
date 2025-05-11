using System.Security.Principal;

namespace IniFileManagement
{
	public static class ConfigFunctions
	{
		#region Static Methods
		/// <summary>Returns the path to the currently running Executable (.EXE)</summary>
		/// <returns>A string value containing the path to the currently running EXE.</returns>
		public static string ExecutablePath() => ExecutablePath( "" );

		/// <summary>Returns the path to the specified file in the currently running Executable's (.EXE) home folder.</summary>
		/// <param name="fileName">A string specifying the name of the file to be encoded to the path.</param>
		/// <returns>A string value containing the path to the currently running EXE and the supplied filename.</returns>
		public static string ExecutablePath( string fileName ) =>
			$"{Path.GetDirectoryName( System.Reflection.Assembly.GetExecutingAssembly().Location )}\\{fileName}";

		/// <summary>Returns the SID for the currently logged-on Windows User.</summary>
		public static string UserSID => $"{{{WindowsIdentity.GetCurrent().User}}}";

		/// <summary>Returns the username for the currently-logged-on Windows User.</summary>
		public static string WinUserName => WindowsIdentity.GetCurrent().Name;
		#endregion
	}
}
