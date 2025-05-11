using System;
using System.Collections.Generic;

namespace NetXpertExtensions
{
	public static partial class NetXpertExtensions
	{
		#region TimeSpan extensions
		/// <summary>Extends the .NetV3.5 TimeSpan class to add the ability to use a formatted-output string with the ToString() function.</summary>
		/// <param name="source">The source object to which this function is attached.</param>
		/// <param name="format">A string containing the format mask to parse.</param>
		/// <returns>A string containing a version of the Timespan formatted according to the provided mask.</returns>
		public static string ToString(this TimeSpan source, string format)
		{
			if (format.Length == 0) format = "h:m:s";
			char[] intChars = new char[] { 'h', 'H', 'm', 'M', 's', 'S', 'd', 'D', 'f', 'F' };

			//Turn all multiple character instances of intChars into single character instances.
			foreach (char c in intChars)
			{
				string s = c.ToString() + c.ToString();
				while (format.IndexOf(s) >= 0)
					format = format.Replace(s, c.ToString());
			}

			List<string> rawParts = new List<string>();
			for (int i = 0; i < format.Length; i++)
				switch (format.Substring(i, 1).ToUpperInvariant())
				{
					case "D":
						rawParts.Add(source.Days.ToString());
						break;
					case "H":
						// If there's no Day designation in the format string, return all the hours..
						if (format.ToUpperInvariant().IndexOf('D') < 0)
							rawParts.Add(source.Hours.ToString());
						else // otherwise just return the sub-day hour count...
							rawParts.Add((source.Hours % 24).ToString("00"));
						break;
					case "M":
						rawParts.Add((source.Minutes % 60).ToString("00"));
						break;
					case "S":
						rawParts.Add((source.Seconds % 60).ToString("00"));
						break;
					case "F":
						rawParts.Add((source.Milliseconds % 1000).ToString("000"));
						break;
					default:
						rawParts.Add(format.Substring(i, 1));
						break;
				}

			return String.Join("", rawParts.ToArray()); // ""; foreach (string s in rawParts) result += s;
		}
		#endregion
	}
}