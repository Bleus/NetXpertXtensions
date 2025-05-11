using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace NetXpertCodeLibrary.ConfigManagement
{
	/// <summary>Used to aid in managing Form information stored in the INI files.</summary>
	// _name,_state,_location,_size,_visible
	// Name = BaseType:Minimized|(x,y)|(w,h)|true
	public class IniFormItem : IniLineItem
	{
		public const string TEMPLATE = @"^([a-z0-9]{3,}):(minimized|maximized|normal)\|(\([-]?[\d]{1,5},[-]?[\d]{1,5}\))\|(\([\d]{1,5},[\d]{1,5}\))\|(true|false)?$";

		#region Properties
		protected FormWindowState _windowState = FormWindowState.Normal;
		protected Size _windowSize = new Size(1024, 768);
		protected Point _windowLoc = new Point(0, 0);
		protected bool _visible = true;
		#endregion

		#region Constructors
		protected IniFormItem(string key, string value, bool encrypt = false, string comment = "", bool enabled = true)
			: base("t", "", encrypt, comment, enabled)
		{
			if (!Validate(value))
				throw new FormatException("The provided value (\"" + value + "\") is not in a proper format for this object.");

			if (!IniLineItem.IsValidKey(key))
				throw new FormatException("The provided key (\"" + key + "\") is not in a proper form.");

			base.Key = key;
			base.Value = value;
		}

		public IniFormItem(IniLineItem source) : base(source)
		{
			if (!Validate(base.Value))
				throw new FormatException("The provided value (\"" + base.Value + "\") is not in a proper format for this object.");
		}

		public IniFormItem(Form form, bool encrypt = false, string comment = "")
			: base(form.GetType().Name, "Form:Normal|(0,0)|(10,10)|true", encrypt, comment)
		{
			this.Key = form.Name;
			this.WindowState = form.WindowState;
			this.Location = form.Location;
			this.Size = form.Size;
			this.Visible = form.Visible;
			base.Comment = form.Text.Trim();
		}
		#endregion

		#region Operators
		public static bool operator !=(IniFormItem left, IniFormItem right) => !(left == right);
		public static bool operator ==(IniFormItem left, IniFormItem right)
		{
			if (left is null) return (right is null);
			if (right is null) return false;
			return left.Value.Equals(right.Value, StringComparison.OrdinalIgnoreCase);
		}

		public static bool operator !=(IniFormItem left, string right) => !(left == right);
		public static bool operator ==(IniFormItem left, string right)
		{
			if (left is null) return string.IsNullOrEmpty(right);
			if (string.IsNullOrEmpty(right.Trim())) return false;
			return Regex.IsMatch(right, TEMPLATE, RegexOptions.IgnoreCase) &&
					left.Value.Equals(right, StringComparison.OrdinalIgnoreCase);
		}
		#endregion

		#region Accessors
		public string BaseType
		{
			get => Parts[1];
			set => Assemble(1, value);
		}

		public FormWindowState WindowState
		{
			get => (FormWindowState)Enum.Parse(typeof(FormWindowState), Parts[2]);
			set => Assemble(2, value);
		}

		public bool Visible
		{
			get => IniBoolItem.Validate(Parts[5]);
			set => Assemble(5, value);
		}

		public Size Size
		{
			get => IniPointItem.ParseS(Parts[4]);
			set => Assemble(4, "(" + value.Width.ToString() + "," + value.Height.ToString() + ")");
		}

		public Point Location
		{
			get => IniPointItem.Parse(Parts[3]);
			set => Assemble(3, "(" + value.X.ToString() + "," + value.Y.ToString() + ")");
		}

		/// <summary>Tears apart a properly formatted value into constituent parts.</summary>
		/// <remarks>
		/// Index   Contains
		///   0     Complete base string
		///   1     Form Base Type Name
		///   2     Form Window State
		///   3     Form Location
		///   4     Form Size
		///   5     Visibility
		/// </remarks>
		protected string[] Parts
		{
			get
			{
				List<string> parts = new List<string>();
				if (Regex.IsMatch(base.Value, TEMPLATE, RegexOptions.Compiled | RegexOptions.IgnoreCase))
				{
					Regex regex = new Regex(TEMPLATE, RegexOptions.Compiled | RegexOptions.IgnoreCase);
					foreach (Group g in regex.Match(base.Value).Groups)
						parts.Add(g.Value);
				}
				return parts.ToArray();
			}
		}
		#endregion

		#region Methods
		protected void Assemble(int index, dynamic value)
		{
			string[] p = Parts; p[index] = value.ToString();
			Assemble(p);
		}

		protected void Assemble(string[] parts)
		{
			string work = parts[1] + ":" + parts[2] + "|" + parts[3] + "|" + parts[4] + "|" + parts[5];
			if (Validate(work))
				base.Value = work;
		}

		public void ApplyToForm(ref Form target, bool strict = false)
		{
			if (!strict || this.BaseType.Equals(target.GetType().BaseType.Name, StringComparison.OrdinalIgnoreCase))
			{
				if (this.Comment.Length > 0)
					target.Text = this.Comment;

				string[] parts = Parts;
				target.Location = IniPointItem.Parse(parts[3]);
				target.Size = IniPointItem.ParseS(parts[4]);
				target.Visible = IniLineItem.ParseBool(parts[5]);
				target.WindowState = (FormWindowState)Enum.Parse(typeof(FormWindowState), Parts[2]);
			}
		}

		public override bool Equals(object obj) => base.Equals(obj);
		public override int GetHashCode() => base.GetHashCode();
		#endregion

		#region Static Methods
		public static implicit operator IniFormItem(Form data) => new IniFormItem(data);

		public static bool Validate(string sample) =>
			!string.IsNullOrEmpty(sample) &&
			Regex.IsMatch(
				sample, 
				TEMPLATE, 
				RegexOptions.Compiled | RegexOptions.IgnoreCase
			);

		public static string Construct(Form form) =>
			(form is null) ? "" : new IniFormItem(form).ToString();

		new public static IniFormItem Parse(string rawData)
		{
			IniLineItem start = null;
			if (!string.IsNullOrEmpty( rawData ))
			{
				start = IniLineItem.Parse( rawData );
				if (!Regex.IsMatch( start.Value.Trim(), TEMPLATE ))
					throw new FormatException( "The provided value (\"" + rawData + "\") is not in a recognized format." );
			}
			return (IniFormItem)start;
		}

		public static IniFormItem Parse(string typeName, string baseTypeName, Point location, Size size, bool visible = true)
		{
			string result = typeName + " = " + baseTypeName + "|(" + location.X.ToString() + "," + location.Y.ToString() + ")|(" +
							size.Width.ToString() + "," + size.Height.ToString() + ")|" + visible.ToString();
			return Parse(result);
		}
		#endregion
	}

	public static class FormExtensions
	{
		#region Form Extensions
		/// <summary>Extends the Form class to facilitate assigning IniFormItem data to the form.</summary>
		/// <param name="setting">An IniFormItem object whose settings are to be applied to this form.</param>
		public static void ApplyFormSetting(this Form source, IniFormItem setting) =>
			setting.ApplyToForm(ref source);

		/// <summary>Extends the Form class to facilitate exporting its settings to an IniFormItem object directly.</summary>
		/// <returns>An IniFormItem object populated with the settings of the form.</returns>
		public static IniFormItem ToFormSetting(this Form source) =>
			new IniFormItem(source);
		#endregion
	}
}