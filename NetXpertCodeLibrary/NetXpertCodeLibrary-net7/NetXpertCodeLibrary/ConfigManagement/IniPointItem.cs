using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace NetXpertCodeLibrary.ConfigManagement
{
	/// <summary>Provides a specialised implementation of IniLineItem designed to manage line items containing co-ordinate values.</summary>
	/// <remarks>Format: ( a, b )</remarks>
	public class IniPointItem : IniLineItem
	{
		#region Constructors
		public IniPointItem(string key, string value, bool encrypt = false, string comment = "", bool enabled = true)
			: base(key, "", encrypt, comment, enabled)
		{
			if (!IniPointItem.Validate(value))
				throw new ArgumentException("The format of the provided data is incompatible with a Size/Point object.");

			this.AsPoint = Parse(value);
		}

		public IniPointItem(string key, Point value, bool encrypt = false, string comment = "", bool enabled = true)
			: base(key, "", encrypt, comment, enabled) => this.AsPoint = value;

		public IniPointItem(string key, Size value, bool encrypt = false, string comment = "", bool enabled = true)
			: base(key, "", encrypt, comment, enabled) => this.AsSize = value;

		public IniPointItem(IniLineItem source) : base(source.Key)
		{
			if (!IniPointItem.Validate(source.Value))
				throw new ArgumentException("The format of the provided data is incompatible with a Size/Point object.");

			this._enabled = source.Enabled;
			this._encrypt = source.Encrypted;
			this._value = source.Value;
			this._comment = source.Comment;
		}

		protected IniPointItem() : base() { this._value = "(0, 0)"; }
		#endregion

		#region Operators
		public static bool operator !=(IniPointItem left, IniPointItem right) => !(left == right);
		public static bool operator ==(IniPointItem left, IniPointItem right)
		{
			if (left is null) return (right is null);
			if (right is null) return false;
			return (left.X == right.X) && (left.Y == right.Y);
		}

		public static bool operator !=(IniPointItem left, Point right) => !(left == right);
		public static bool operator ==(IniPointItem left, Point right)
		{
			if (left is null) return Object.ReferenceEquals(right, null);
			if (Object.ReferenceEquals(right, null)) return false;
			return (left.X == right.X) && (left.Y == right.Y);
		}
		#endregion

		#region Accessors
		/// <summary>Intercepts the base Value accessor to ensure assigned values conform to the proper format.</summary>
		/// <remarks>Using the base.Value accessor leverages its encryption/decryption features.</remarks>
		new public string Value
		{
			get => this.Value;
			protected set
			{
				if (IniPointItem.Validate(value))
					base.Value = value;
			}
		}

		/// <summary>Facilitates interaction to/from this instance as a System.Drawing.Point object.</summary>
		/// <remarks>Using the base.Value accessor leverages its encryption/decryption features.</remarks>
		public Point AsPoint
		{
			get => Parse(base.Value);
			set => base.Value = "(" + value.X + ", " + value.Y + ")";
		}

		/// <summary>Facilitates interaction to/from this instance as a System.Drawing.Size object.</summary>
		/// <remarks>Using the base.Value accessor leverages its encryption/decryption features.</remarks>
		public Size AsSize
		{
			get { Point p = Parse(base.Value); return new Size(p.X, p.Y); }
			set => base.Value = "(" + value.Width + ", " + value.Height + ")";
		}

		/// <summary>Facilitates interaction to/from this instance as a System.Drawing.Point object's X accessor.</summary>
		/// <remarks>Using the base.Value accessor leverages its encryption/decryption features.</remarks>
		public int X
		{
			get => this.AsPoint.X;
			set => this.AsPoint = new Point(value, this.AsPoint.Y);
		}

		/// <summary>Facilitates interaction to/from this instance as a System.Drawing.Point object's Y accessor.</summary>
		/// <remarks>Using the base.Value accessor leverages its encryption/decryption features.</remarks>
		public int Y
		{
			get => this.AsPoint.Y;
			set => this.AsPoint = new Point(this.AsPoint.X, value);
		}

		/// <summary>Facilitates interaction to/from this instance as a System.Drawing.Size object's Width accessor.</summary>
		/// <remarks>Using the base.Value accessor leverages its encryption/decryption features.</remarks>
		public int Width
		{
			get => this.X;
			set => this.X = value;
		}

		/// <summary>Facilitates interaction to/from this instance as a System.Drawing.Size object's Height accessor.</summary>
		/// <remarks>Using the base.Value accessor leverages its encryption/decryption features.</remarks>
		public int Height
		{
			get => this.Y;
			set => this.Y = value;
		}

		/// <summary>Reports on whether the currently stored value in this instance corresponds to the correct format for a Point object.</summary>
		public bool IsPoint => IniPointItem.Validate(base.Value);
		#endregion

		#region Methods
		public static implicit operator Point(IniPointItem data) => data.AsPoint;
		public static implicit operator Size(IniPointItem data) => data.AsSize;

		public override int GetHashCode() => base.GetHashCode();
		public override bool Equals(object obj) => base.Equals(obj);
		#endregion

		#region Static Methods
		public static bool Validate(string value) =>
			!string.IsNullOrEmpty(value) &&
			Regex.IsMatch(
				value.Trim(), 
				@"[({][\\s]*([-]?[0-9]{1,5})[\\s]*[,;/][\\s]*([-]?[0-9]{1,5})[\\s]*[})]"
			);

		new public static Point Parse(string source)
		{
			Point p = new Point(0, 0);
			if (IniPointItem.Validate(source))
			{
				string[] points = source.Trim(new char[] { '(', ')', '{', '}' }).Split(new char[] { ',', ';', '/' }, 2);
				int x = int.Parse(points[0]);
				int y = int.Parse(points[1]);
				p = new Point(x, y);
			}
			return p;
		}

		public static Size ParseS(string source)
		{
			Point p = Parse(source);
			return new Size(Math.Max(0, p.X), Math.Max(p.Y, 0));
		}
		#endregion
	}
}