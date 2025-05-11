using System.Text.RegularExpressions;

namespace NetXpertCodeLibrary.ConsoleFunctions
{
	/// <summary>Provides a means to manage a foreground and background colour for consoles together as a single entity.</summary>
	public class CliColor
	{
		#region Properties
		protected ConsoleColor _foreGround = ConsoleColor.Gray;
		protected ConsoleColor _backGround = ConsoleColor.Black;
		#endregion

		#region Constructors
		public CliColor(ConsoleColor fore = ConsoleColor.Gray, ConsoleColor back = ConsoleColor.Black)
		{
			this._foreGround = fore;
			this._backGround = back;
		}

		public CliColor(string foreName, string backName = "")
		{
			if (Regex.IsMatch( foreName, @"^[0-9*a-f](?:[; ,]?[0-9*a-f])?$", RegexOptions.IgnoreCase ))
			{
				foreName = foreName.PadRight( 2, '*' );
				Regex pattern = new Regex( @"^(?<fore>[a-f*0-9])(?<back>[a-f*0-9])?$", RegexOptions.IgnoreCase );

				this._foreGround = (foreName[ 0 ] == '*') ? Console.ForegroundColor : (ConsoleColor)int.Parse( foreName.Substring( 0, 1 ), System.Globalization.NumberStyles.HexNumber );
				this._backGround = (foreName[ 1 ] == '*') ? Console.BackgroundColor : (ConsoleColor)int.Parse( foreName.Substring( 1, 1 ), System.Globalization.NumberStyles.HexNumber );
			}
			else
			{
				this._foreGround = Parse( foreName );
				this._backGround = string.IsNullOrWhiteSpace( backName ) ? Console.BackgroundColor : Parse( backName );
			}
		}

		public CliColor() { this.Fore = Console.ForegroundColor; this.Back = Console.BackgroundColor; }
		#endregion

		#region Accessors
		public ConsoleColor Fore
		{
			get => this._foreGround;
			set => this._foreGround = value;
		}

		public ConsoleColor Back
		{
			get => this._backGround;
			set => this._backGround = value;
		}

		public CliColor Inverse => new CliColor( this.Back, this.Fore );

		public static CliColor Default { protected get; set; } = new CliColor();
		#endregion

		#region Methods
		/// <summary>Facilitates making a derived CliColour using this one as the template.</summary>
		/// <param name="fore">The new foreColor to use. If NULL, the foreColor of the base object is substituted.</param>
		/// <param name="back">The new backColor to use. If NULL, the backColor of the base object is substituted.</param>
		/// <returns>A new CliColor object with the specified colours set.</returns>
		public CliColor Alt(ConsoleColor? fore = null, ConsoleColor? back = null)
		{
			if (fore is null) fore = this.Fore;
			if (back is null) back = this.Back;
			return new CliColor( (fore is null) ? this.Fore : (ConsoleColor)fore, (back is null) ? this.Back : (ConsoleColor)back );
		}

		/// <summary>Returns the contents of this object in a style compatible with the XML help system.</summary>
		public string ToStyleString() => "foreGround:" + this._foreGround.ToString() + "; backGround:" + this._backGround.ToString() + ";";

		public override string ToString() => "( " + this._foreGround.ToString() + ", " + this._backGround + " )";

		public string ToHexPair() => (((int)this._foreGround * 16) + (int)this._backGround).ToString("X2");

		/// <summary>Applies the values of this object to the Console.</summary>
		public void ToConsole()
		{
			Console.ForegroundColor = this.Fore;
			Console.BackgroundColor = this.Back;
		}
		#endregion

		#region Static Methods
		protected static ConsoleColor Translate(char color)
		{
			string colorName = Regex.Replace( color.ToString(), @"[^0-9a-fA-F]", "" );
			return colorName.Length > 0 ? Translate( System.Convert.ToByte( colorName, 16 ) ) : CliColor.Default.Fore;
		}

		protected static ConsoleColor Translate(byte color)
		{ 
			switch (color)
			{
				case 0: return ConsoleColor.Black;
				case 1: return ConsoleColor.DarkBlue;
				case 2: return ConsoleColor.DarkGreen;
				case 3: return ConsoleColor.DarkCyan;
				case 4: return ConsoleColor.DarkRed;        // Crimson
				case 5: return ConsoleColor.DarkMagenta;    // Purple
				case 6: return ConsoleColor.DarkYellow; // Brown
				case 7: return ConsoleColor.Gray;
				case 8: return ConsoleColor.DarkGray;
				case 9: return ConsoleColor.Blue;           // Royal Blue
				case 10: return ConsoleColor.Green;
				case 11: return ConsoleColor.Cyan;
				case 12: return ConsoleColor.Red;           // Red
				case 13: return ConsoleColor.Magenta;       // Pink
				case 14: return ConsoleColor.Yellow;
				case 15: return ConsoleColor.White;
			}
			return Console.ForegroundColor;
		}

		/// <summary>Takes a System.Drawing.Color object and converts it mathematically to it's approximate equivalent ConsoleColor.</summary>
		protected static ConsoleColor Translate( Color color )
		{
			int index = (color.R > 128 | color.G > 128 | color.B > 128) ? 8 : 0; // Bright bit
			index |= (color.R > 64) ? 4 : 0; // Red bit
			index |= (color.G > 64) ? 2 : 0; // Green bit
			index |= (color.B > 64) ? 1 : 0; // Blue bit
			return (ConsoleColor)index;
		}

		/// <summary>Takes a System.Drawing.Color object and converts it to an equivalent System.Drawing.Color.</summary>
		public static Color Translate( ConsoleColor color )
		{
			uint[] ConsoleColors = { 
					0xff000000, // Black
					0xff000080, // DarkBlue = 1
                    0xff008000, // DarkGreen = 2
                    0xff008080, // DarkCyan = 3
                    0xff800000, // DarkRed = 4
                    0xff800080, // DarkMagenta = 5
                    0xff808000, // DarkYellow = 6
                    0xffC0C0C0, // Gray = 7
                    0xff808080, // DarkGray = 8
                    0xff0000FF, // Blue = 9
                    0xff00FF00, // Green = 10
                    0xff00FFFF, // Cyan = 11
                    0xffFF0000, // Red = 12
                    0xffFF00FF, // Magenta = 13
                    0xffFFFF00, // Yellow = 14
                    0xffFFFFFF  // White = 15
                };

			return Color.FromArgb( (int)ConsoleColors[ (int)color ] );
		}

		/// <summary>Takes a color name and tries to convert it to an equivalent ConsoleColor.</summary>
		/// <exception cref="FormatException"></exception>
		protected static ConsoleColor Translate(string colorName)
		{
			if (string.IsNullOrWhiteSpace(colorName))
				throw new FormatException( "You need to provide a System.Drawing.Color name to translate." );

			colorName = colorName.Trim();

			if (Regex.IsMatch(colorName, @"^[0-9a-f]$", RegexOptions.IgnoreCase))
				return (ConsoleColor)int.Parse( colorName.Substring( 0, 1 ), System.Globalization.NumberStyles.HexNumber );

			switch (System.Convert.ToByte(Regex.Replace(colorName,@"[^0-9a-f]","",RegexOptions.IgnoreCase), 16))
				{
					case  0: return ConsoleColor.Black;
					case  1: return ConsoleColor.DarkBlue;
					case  2: return ConsoleColor.DarkGreen;
					case  3: return ConsoleColor.DarkCyan;
					case  4: return ConsoleColor.DarkRed;		// Crimson
					case  5: return ConsoleColor.DarkMagenta;	// Purple
					case  6: return ConsoleColor.DarkYellow;	// Brown
					case  7: return ConsoleColor.Gray;
					case  8: return ConsoleColor.DarkGray;
					case  9: return ConsoleColor.Blue;			// Royal Blue
					case 10: return ConsoleColor.Green;
					case 11: return ConsoleColor.Cyan;
					case 12: return ConsoleColor.Red;           // Red
					case 13: return ConsoleColor.Magenta;       // Pink
					case 14: return ConsoleColor.Yellow;
					case 15: return ConsoleColor.White;
				}

			switch (colorName.ToLowerInvariant())
			{
				case "blue":
				case "darkblue":
					return ConsoleColor.DarkBlue;

				case "green":
				case "forestgreen":
				case "darkgreen":
					return ConsoleColor.DarkGreen;

				case "aqua":
				case "darkcyan":
				case "cyan":
					return ConsoleColor.DarkCyan;

				case "darkred":
				case "red":
					return ConsoleColor.DarkRed;

				case "darkmagenta":
				case "purple":
					return ConsoleColor.DarkMagenta;

				case "darkyellow":
				case "brown":
				case "orange":
					return ConsoleColor.DarkYellow;

				case "lightgray":
				case "lightgrey":
				case "grey":
				case "gray":
					return ConsoleColor.Gray;

				case "darkgray":
				case "darkgrey":
					return ConsoleColor.DarkGray;

				case "lightblue":
				case "royalblue":
					return ConsoleColor.Blue;

				case "lightgreen":
				case "neongreen":
					return ConsoleColor.Green;

				case "lightcyan":
					return ConsoleColor.Cyan;

				case "lightred":
				case "coral":
					return ConsoleColor.Red;

				case "pink":
					return ConsoleColor.Magenta;

				case "black":
				case "yellow":
				case "magenta":
				case "white":
					return (ConsoleColor)Enum.Parse(typeof(ConsoleColor), colorName, true);
			}
			throw new FormatException("The provided value (\"" + colorName + "\")could not be converted to a ConsoleColor equivalent.");
		}

		public static ConsoleColor Convert(byte redByte, byte greenByte, byte blueByte)
		{
			ConsoleColor result = 0;
			double red = redByte,
				   green = greenByte,
				   blue = blueByte,
				   spread = double.MaxValue;

			foreach (ConsoleColor color in (ConsoleColor[])Enum.GetValues(typeof(ConsoleColor)))
			{
				string colorName = Enum.GetName(typeof(ConsoleColor), color);

				Color color2 = Color.FromName(colorName == "DarkYellow" ? "Orange" : colorName);

				double compare = Math.Pow(color2.R - red, 2.0) + Math.Pow(color2.G - green, 2.0) + Math.Pow(color2.B - blue, 2.0);
				if (compare == 0.0) return color;
				if (compare < spread) { spread = compare; result = color; }
			}
			return result;
		}

		public static ConsoleColor Convert(Color source) =>
			Convert(source.R, source.G, source.B);

		/// <summary>Attempts to parse a supplied value to a ConsoleColor value.</summary>
		/// <param name="name">A string containing the language to convert.</param>
		/// <returns>A best attempt to reasonably convert the value to a valid ConsoleColor. If all attempts fail, the LightGray value is returned.</returns>
		public static ConsoleColor Parse(string name, ConsoleColor defaultColor = ConsoleColor.Gray)
		{
			if ((name is null) || (name.Trim().Length == 0)) return defaultColor;

			name = name.Trim();
			ConsoleColor result = defaultColor;

			try { result = Translate(name); }
			catch
			{
				// If it's an HTML-esque #rrggbb value, try to parse it...
				if (Regex.IsMatch(name, @"#(?<red>[0-9a-f]{2})(?<green>[0-9a-f]{2})(?<blue>[0-9a-f]{2})", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture))
				{
					MatchCollection matches = new Regex(@"#(?<red>[0-9a-f]{2})(?<green>[0-9a-f]{2})(?<blue>[0-9a-f]{2})").Matches(name);
					if (matches.Count > 0)
					{
						byte red = System.Convert.ToByte(matches[0].Groups["red"].Value, 16),
							 green = System.Convert.ToByte(matches[0].Groups["green"].Value, 16),
							 blue = System.Convert.ToByte(matches[0].Groups["blue"].Value, 16);
						result = Convert(red, green, blue);
					}
				}
				else // Assume it's some form of english...
				{
					// Try it as a System.Drawing.Color name...
					Color c1 = Color.FromName(name);
					if ((c1.R == 0) && (c1.G == 0) && (c1.B == 0) && !name.Equals("black", StringComparison.OrdinalIgnoreCase))
					{
						// The attempt to translate the value as a System.Color name failed.
						// Try to translate it as an english word, or a ConsoleColor value...
						try { result = CliColor.Translate(name); }
						catch (Exception e)
						{
							if (e.GetType().Name == "FormatException")
								result = defaultColor;
							else
								throw e;
						}
					}
					else
						result = ConsoleColor.Black;
				}
			}
			return result;
		}

		/// <summary>Parses a supplied "style" string to extract the defined colours.</summary>
		/// <param name="source">A string containing the "style" text to parse.</param>
		/// <param name="defaultColor">Define an optional default HelpColour to use when values fail to be parsed.</param>
		/// <returns>A new HelpColor object populated from the provided "style" string.</returns>
		public static CliColor ParseXml(string source, CliColor defaultColor = null)
		{
			if (defaultColor is null) defaultColor = Default;

			source += ";"; // ensures that there's always a final semi-colon on the string...
			if (source.IndexOf(";") >= 0)
			{
				string[] parts = source.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				CliColor result = defaultColor;

				foreach (string part in parts)
					if (part.IndexOf(':') > 0)
					{
						string[] split = part.Split(new char[] { ':' }, 2);
						switch (split[0].Trim().ToUpperInvariant())
						{
							case "FORECOLOR":
								result.Fore = Parse(split[1], defaultColor.Fore);
								break;
							case "BACKCOLOR":
								result.Back = Parse(split[1], defaultColor.Back);
								break;
						}
					}
				return result;
			}
			return defaultColor;
		}

		/// <summary>Creates a new CliColor object by capturing the current color settings from the Console class.</summary>
		/// <returns>A new CliColor object with settings derived from the Console.</returns>
		public static CliColor CaptureConsole() => new CliColor(Console.ForegroundColor, Console.BackgroundColor);

		/// <summary>Produces a new CliColor object from specified ConsoleColor predicates using NULL to grab the value from the Console itself.</summary>
		/// <param name="fore">A ConsoleColor value to use as the foreground color. If NULL is passed, this value is copied from Console.ForegroundColor.</param>
		/// <param name="back">A ConsoleColor value to use as the background color. If NULL is passed, this value is copied from Console.BackgroundColor.</param>
		/// <returns>A new CliColor object derived from the provided values.</returns>
		public static CliColor Normalize(ConsoleColor? fore = null, ConsoleColor? back = null)
		{
			ConsoleColor f = (fore is null) ? Console.ForegroundColor : (ConsoleColor)fore;
			ConsoleColor b = (back is null) ? Console.BackgroundColor : (ConsoleColor)back;
			return new CliColor(f, b);
		}
		#endregion
	}

	public class RtbColor : CliColor
	{
		#region Constructors
		public RtbColor( ConsoleColor fore = ConsoleColor.Gray, ConsoleColor back = ConsoleColor.Black ) : base( fore, back ) { }

		public RtbColor( Color fore, Color back ) : base( Convert( fore ), Convert( back ) ) { }

		public RtbColor( string foreName, string backName = "" ) : base( foreName, backName ) { }

		public RtbColor() : base() { }
		#endregion

		#region Accessors
		new public Color Fore
		{
			get => Translate( base._foreGround );
			set => base._foreGround = Convert(value);
		}

		new public Color Back
		{
			get => Translate(base._backGround);
			set => base._backGround = Convert(value);
		}

		new public RtbColor Inverse => new RtbColor( _backGround, _foreGround );

		new public static CliColor Default { protected get; set; } = new CliColor();
		#endregion

		#region Static Methods
		new public static Color Translate( byte color )
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
			throw new NotSupportedException();
		}
		#endregion
	}
}
