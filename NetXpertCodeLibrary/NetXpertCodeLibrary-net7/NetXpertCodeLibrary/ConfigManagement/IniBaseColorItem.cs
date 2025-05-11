using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;

namespace NetXpertCodeLibrary.ConfigManagement
{
	/// <summary>Provides a specialised implementation of IniLineItem designed to manage line items containing color values.</summary>
	/// <remarks>Format: (foreColor, backColor)</remarks>
	public class IniColorItem : IniLineItem
	{
		/// <summary>Facilitates working with fore and back -ground colours as a single piece of data.</summary>
		public class ConsoleColors
		{
			#region Properties
			protected Color _fore = Color.Gray;
			protected Color _back = Color.Black;
			#endregion

			#region Constructors
			public ConsoleColors() { }

			public ConsoleColors(Color fore) =>
				this._fore = fore;

			public ConsoleColors(Color fore, Color back)
			{
				this._fore = fore;
				this._back = back;
			}
			#endregion

			#region Operators
			public static bool operator !=(ConsoleColors left, ConsoleColors right) => !(left == right);
			public static bool operator ==(ConsoleColors left, ConsoleColors right)
			{
				if (left is null) return right is null;
				if (right is null) return false;
				return (left.Fore == right.Fore) && (left.Back == right.Back);
			}
			#endregion

			#region Accessors
			public Color Fore
			{
				get => this._fore;
				set => this._fore = value;
			}

			public Color Back
			{
				get => this._back;
				set => this._back = value;
			}
			#endregion

			#region Methods
			public override string ToString() =>
				"(" + this._fore.ToString().Replace("Color ", "").Trim(new char[] { '[', ']' }) + ", " +
				this._back.ToString().Replace("Color ", "").Trim(new char[] { '[', ']' }) + ")";

			public override bool Equals(object obj) => base.Equals(obj);

			public override int GetHashCode() => base.GetHashCode();
			#endregion

			#region Static Methods
			public static ConsoleColors Default => new ConsoleColors();
			#endregion
		}

		#region Constructors
		public IniColorItem(string value, string key = "Color", bool encrypt = false, string comment = "", bool enabled = true)
			: base(key, "", encrypt, comment, enabled)
		{
			value = value.Trim(); if (string.IsNullOrEmpty(value)) { value = ConsoleColors.Default.ToString(); }

			if (!Validate(value))
				throw new ArgumentException("The supplied value (\"" + value + "\") is not an appropriate for use as an IniColorItem.");
			this._value = value;
		}

		public IniColorItem(Color fore, string key = "Color", bool encrypt = false, string comment = "", bool enabled = true)
			: base(key, "", encrypt, comment, enabled)
		{
			this._value = new ConsoleColors().ToString();
			this.Fore = fore;
		}

		public IniColorItem(Color fore, Color back, string key = "Color", bool encrypt = false, string comment = "", bool enabled = true)
			: base(key, "", encrypt, comment, enabled) =>
			this._value = new ConsoleColors(fore, back).ToString();

		public IniColorItem(ConsoleColors colors, string key = "Color", bool encrypt = false, string comment = "", bool enabled = true)
			: base(key, "", encrypt, comment, enabled) =>
			this._value = colors.ToString();

		public IniColorItem(IniLineItem source) : base(source) { }

		protected IniColorItem() : base() { }
		#endregion

		#region Operators
		public static bool operator !=(IniColorItem left, IniColorItem right) => !(left == right);
		public static bool operator ==(IniColorItem left, IniColorItem right)
		{
			if (left is null) return (right is null);
			if (right is null) return false;
			return (left.Fore == right.Fore) && (left.Back == right.Back);
		}

		public static bool operator !=(IniColorItem left, ConsoleColors right) => !(left == right);
		public static bool operator ==(IniColorItem left, ConsoleColors right)
		{
			if (left is null) return Object.ReferenceEquals(right, null);
			if (Object.ReferenceEquals(right, null)) return false;
			return (left.Fore == right.Fore) && (left.Back == right.Back);
		}
		#endregion

		#region Accessors
		new public string Value
		{
			get => this._value;
			protected set
			{
				if (Validate(value))
					this.AsColor = Parse(value);
			}
		}

		public ConsoleColors AsColor
		{
			get
			{
				if (!Validate(this._value)) throw new InvalidDataException();
				return Parse(this._value);
			}
			set => this._value = value.ToString();
		}

		public Color Fore
		{
			get => this.AsColor.Fore;
			set => this.AsColor = new ConsoleColors(value, this.AsColor.Back);
		}

		public Color Back
		{
			get => this.AsColor.Back;
			set => this.AsColor = new ConsoleColors(this.AsColor.Fore, value);
		}

		public bool IsColor => Validate(this._value);
		#endregion

		#region Methods
		new public string ToString(int indent) =>
			this._key.PadLeft(indent, ' ') + " = " + this.AsColor.ToString();

		public override string ToString() => this.ToString(0);

		public static implicit operator ConsoleColors(IniColorItem data) => data.AsColor;

		public override int GetHashCode() => base.GetHashCode();
		public override bool Equals(object obj) => base.Equals(obj);
		#endregion

		#region Static Methods
		public static bool Validate(string value) =>
			Regex.IsMatch(value.Trim(), @"[({][\s]*(([#]?[0-9a-f]{6})|([a-z]{3,16}))[\s]*[,][\s]*(([#]?[0-9a-f]{6})|([a-z]{3,16}))[\s]*[})]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		new public static ConsoleColors Parse(string source) =>
			Parse(source, ConsoleColors.Default);

		public static ConsoleColors Parse(string source, Color defaultFore) =>
			Parse(source, new ConsoleColors(defaultFore, Color.Black));

		public static ConsoleColors Parse(string source, Color defaultFore, Color defaultBack) =>
			Parse(source, new ConsoleColors(defaultFore, defaultBack));

		public static ConsoleColors Parse(string source, ConsoleColors def)
		{
			if (Validate(source))
			{
				string[] parts = source.Trim(new char[] { ' ', '{', '}', '(', ')' }).Split(new char[] { ',' }, 2, StringSplitOptions.None);
				Color fore = (parts[0].Length > 2) ? Color.FromName(parts[0]) : def.Fore;
				Color back = (parts[1].Length > 2) ? Color.FromName(parts[1]) : def.Back;
				return new ConsoleColors(fore, back);
			}
			throw new ArgumentException("\"" + source + "\" is not a recognized ConsoleColors value / format!");
		}
		#endregion
	}
}