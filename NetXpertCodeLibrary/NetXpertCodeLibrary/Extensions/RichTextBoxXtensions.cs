using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows.Forms;
//using NetXpertCodeLibrary.ConsoleFunctions;

namespace NetXpertCodeLibrary.Extensions
{
	internal class RtbColor
	{
		#region Static Methods
		public static Color Translate( byte color )
		{
			switch ( color % 16 )
			{
				case 0: return Color.Black;
				case 1: return Color.DarkBlue;
				case 2: return Color.DarkGreen;
				case 3: return Color.DarkCyan;
				case 4: return Color.Crimson;
				case 5: return Color.Purple;
				case 6: return Color.Brown;
				case 7: return Color.Gray;
				case 8: return Color.DarkGray;
				case 9: return Color.RoyalBlue;
				case 10: return Color.Green;
				case 11: return Color.Cyan;
				case 12: return Color.Red;
				case 13: return Color.Pink;
				case 14: return Color.Yellow;
				case 15: return Color.White;
			}
			throw new System.NotSupportedException();
		}
		#endregion
	}

	public static class RichTextBoxXtensions
	{
		/// <summary>Facilitates sending coloured text to a RichTextBox.</summary>
		/// <param name="rtb">The System.Windows.Forms.RichTextBox to target.</param>
		/// <param name="what">The text to send.</param>
		/// <remarks>Sent text can include HTML entities, and coded colour markups: {#x|rgb|rrggbb#x|rgb|rrggbb,n}</remarks>
		public static void Write( this RichTextBox rtb, string what )
		{
			Color defaultFore = rtb.ForeColor, defaultBack = rtb.BackColor;

			void rtbWrite( Color fore, Color back, string text )
			{
				rtb.SelectionStart = rtb.Text.Length;
				rtb.ScrollToCaret();
				rtb.SelectionColor = fore;
				rtb.SelectionBackColor = back;
				rtb.AppendText( text );
			}

			Color ParseColor( string raw, Color aDefaultColor )
			{
				if ( string.IsNullOrWhiteSpace(raw) || (raw == "#") ) return aDefaultColor;

				if (Regex.IsMatch( raw.Trim(), @"^#?([a-z0-9]|[a-f0-9]{3}|[a-f0-9]{6})$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture))
				{
					int r = aDefaultColor.R, g = aDefaultColor.G, b = aDefaultColor.B;
					raw = Regex.Replace( raw, @"[^a-f0-9A-F]", "" );

					if (raw.Length == 1)
						return RtbColor.Translate( byte.Parse( raw , NumberStyles.HexNumber ) );

					if (raw.Length == 3)
						raw = new string( new char[] { raw[ 0 ], raw[ 0 ], raw[ 1 ], raw[ 1 ], raw[ 2 ], raw[ 2 ] } );

					if (raw.Length == 6)
					{
						r = int.Parse( raw.Substring( 0, 2 ), NumberStyles.HexNumber );
						g = int.Parse( raw.Substring( 2, 2 ), NumberStyles.HexNumber );
						b = int.Parse( raw.Substring( 4, 2 ), NumberStyles.HexNumber );
					}

					return Color.FromArgb( r, g, b );
				}

				return aDefaultColor;
			}

			string pattern = /* language=regex */
				@"[{]((?<basic>[0-9a-f]{1,2})|((?<fore>#([0-9a-f]|[0-9a-f]{3}|[0-9a-f]{6})?)(?<back>#[0-9a-f]|#[0-9a-f]{3}|#[0-9a-f]{6})?))(?<cr>,n?)?[}](?<data>[^{]*)";
			Regex rx = new Regex( pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture );

			if ( rx.IsMatch( what ) )
			{

				MatchCollection matches = rx.Matches( what );
				foreach ( Match m in matches )
				{
					string basic = m.Groups[ "basic" ].Success ? m.Groups[ "basic" ].Value : "",
						   rawFore = m.Groups[ "fore" ].Success ? m.Groups[ "fore" ].Value : "",
						   rawBack = m.Groups[ "back" ].Success ? m.Groups[ "back" ].Value : "",
						   text = m.Groups[ "data" ].Success ? m.Groups[ "data" ].Value : "",
						   cr = m.Groups[ "cr" ].Success ? "\r\n" : "";

					text = HttpUtility.HtmlDecode( text.Replace( "&rbrace;", "}" ).Replace( "&lbrace;", "{" ) ) + cr;

					
					Color fore = ParseColor( (basic.Length > 0) ? basic.Substring(0,1) : rawFore, defaultFore ),
						  back = ParseColor( (basic.Length > 1) ? basic.Substring(1,1) : rawBack, defaultBack );

					rtbWrite( fore, back, text );
				}
			}
		}

		/// <summary>Facilitates sending coloured text to a <i>RichTextBox</i>.</summary>
		/// <param name="rtb">The <b>System.Windows.Forms.RichTextBox</b> to target.</param>
		/// <param name="what">The text to send.</param>
		/// <param name="o">An item that can be inserted into the passed text via the $1 parameter.</param>
		/// <remarks>Sent text can include HTML entities, and coded colour markups: {#x|rgb|rrggbb#x|rgb|rrggbb,n}</remarks>
		public static void Write( this RichTextBox rtb, string what, object o ) =>
			rtb.Write( what.Replace( new object[] { o } ) );

		/// <summary>Facilitates sending coloured text to a <i>RichTextBox</i>.</summary>
		/// <param name="rtb">The <b>System.Windows.Forms.RichTextBox</b> to target.</param>
		/// <param name="what">The text to send.</param>
		/// <param name="o">Items that will be inserted into the passed text via serialized $x parameters.</param>
		/// <remarks>Sent text can include HTML entities, and coded colour markups: {#x|rgb|rrggbb#x|rgb|rrggbb,n}</remarks>
		public static void Write( this RichTextBox rtb, string what, object[] o ) =>
			rtb.Write( what.Replace( o ) );
	}
}
