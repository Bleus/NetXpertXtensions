using System;

namespace NetXpertExtensions
{
	public static partial class NetXpertExtensions
	{
		#region Decimal extensions
		private static decimal MyTruncate(decimal source, int decimalPlaces = 2)
		{
			decimal factor = (decimal)Math.Pow(10, decimalPlaces);
			return (decimalPlaces < 1) ? Math.Truncate(source) : (Math.Truncate(source * factor) / factor);
		}

		/// <summary>Facilitates rounding off a Decimal value to a specified number of decimal places.</summary>
		/// <param name="decimalPlaces">The number of decimal places to round off to. (Default: 2)</param>
		public static decimal Round(this decimal source, int decimalPlaces = 2) =>
			Math.Round(source, decimalPlaces);

		/// <summary>Facilitates truncating a Decimal value at a specified number of decimal places.</summary>
		/// <param name="decimalPlaces">The number of decimal places to truncate at. (Default: 2)</param>
		public static decimal Truncate(this decimal source, int decimalPlaces = 2) =>
			MyTruncate(source, decimalPlaces);

		public static decimal Floor(this decimal source, int decimalPlaces = 2) =>
			Math.Floor(MyTruncate(source,decimalPlaces));

		public static decimal Ceiling(this decimal source, int decimalPlaces = 2) =>
			Math.Ceiling(MyTruncate(source,decimalPlaces));
		#endregion
	}
}