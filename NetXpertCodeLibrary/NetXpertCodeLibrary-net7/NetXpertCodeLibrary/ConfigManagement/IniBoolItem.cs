using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace NetXpertCodeLibrary.ConfigManagement
{
	/// <summary>Provides a specialised implementation of IniLineItem designed to manage line items containing boolean values.</summary>
	/// <remarks>Format: true|yes|1|y|on|enabled</remarks>
	public class IniBoolItem : IniLineItem
	{
		#region Constructors
		public IniBoolItem(string key, bool value, bool encrypt = false, string comment = "", bool enabled = true)
			: base(key, value.ToString(), encrypt, comment, enabled) { }

		public IniBoolItem(IniLineItem source) : base(source.Key)
		{
			if (!IniBoolItem.Validate(source.Value))
				throw new ArgumentException("The format of the provided data is incompatible with a Boolean object.");

			this._enabled = source.Enabled;
			this._encrypt = source.Encrypted;
			this._value = source.Value;
			this._comment = source.Comment;
		}

		protected IniBoolItem() : base() { this._value = "(0, 0)"; }
		#endregion

		#region Operators
		public static bool operator !=(IniBoolItem left, IniBoolItem right) => !(left == right);
		public static bool operator ==(IniBoolItem left, IniBoolItem right)
		{
			if (left is null) return (right is null);
			if (right is null) return false;
			return (left.Value == right.Value);
		}

		public static bool operator !=(IniBoolItem left, bool right) => !(left == right);
		public static bool operator ==(IniBoolItem left, bool right)
		{
			if (left is null) return false;
			return left.Value == right;
		}

		public static bool operator !=(bool right, IniBoolItem left) => !(left == right);
		public static bool operator ==(bool right, IniBoolItem left)
		{
			if (left is null) return false;
			return left.Value == right;
		}
		#endregion

		#region Accessors
		/// <summary>Intercepts the base Value accessor to ensure assigned values conform to the proper format.</summary>
		/// <remarks>Using the base.Value accessor leverages its encryption/decryption features.</remarks>
		new public bool Value
		{
			get => IniBoolItem.Validate(base.Value);
			set => base.Value = value.ToString();
		}

		public string ToString(string value, int indent = 0)
		{
			base.Value = (IniBoolItem.Validate(value) ? value : "false");
			return base.ToString(indent);
		}
		#endregion

		#region Methods
		public static implicit operator bool(IniBoolItem data) => data.Value;

		public override int GetHashCode() => base.GetHashCode();
		public override bool Equals(object obj) => base.Equals(obj);
		#endregion

		#region Static Methods
		public static bool Validate(string value) =>
			!(value is null) && (value.Length > 0) &&
			Regex.IsMatch(
				value.Trim(), 
				@"(true|on|y|yes|enable[d]?|1|t)",
				RegexOptions.IgnoreCase | RegexOptions.Compiled
			);

		new public static bool Parse(string source) => 
			IniBoolItem.Validate(source);
		#endregion
	}
}