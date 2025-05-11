using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using NetXpertCodeLibrary.ConfigManagement;

namespace Cobblestone.Classes
{
	public class FormSettings
	{
		#region Properties
		protected Point _location;
		protected Size _size;
		protected FormWindowState _state;
		protected bool _visible;
		protected Type _formType;
		protected string _name;
		#endregion

		#region Constructors
		public FormSettings() =>
			SetDefaultValues();

		public FormSettings(Form form) =>
			this.ImportForm(form);

		public FormSettings(IniFormItem item) =>
			this.ImportConfigItem(item);

		#endregion

		#region Accessors
		public Point Location =>
			this._location;

		public Size Size =>
			this._size;

		public bool Visible =>
			this._visible;

		public FormWindowState WindowState =>
			this._state;

		public Type FormType =>
			this._formType;

		public string TypeName =>
			this._formType.Name;

		public string Name =>
			this._name;

		public bool IsDefault =>
			(this.Size == Default.Size) &&
			(this.Location == Default.Location) &&
			(this.WindowState == Default.WindowState) &&
			(this.Visible == Default.Visible);
		#endregion

		#region Operators
		public static bool operator ==(FormSettings left, FormSettings right)
		{
			if (left is null) return (right is null);
			if (right is null) return false;

			return (left.FormType == right.FormType);
		}

		public static bool operator !=(FormSettings left, FormSettings right) => !(left == right);

		public static bool operator ==(FormSettings left, Form right)
		{
			FormSettings settings = new FormSettings(right);
			return (left == settings);
		}

		public static bool operator !=(FormSettings left, Form right) => !(left == right);

		public static bool operator ==(FormSettings left, Type right)
		{
			if (left is null) return (right is null);
			if (right is null) return false;
			return (left.FormType == right);
		}

		public static bool operator !=(FormSettings left, Type right) => !(left == right);
		#endregion

		#region Methods
		public IniFormItem ToConfigItem() =>
			IniFormItem.Parse(this._formType.Name, this._formType.BaseType.Name, this._location, this._size, this._visible);

		public dynamic Instantiate() =>
			Activator.CreateInstance(this._formType, new string[] { });

		public void ImportForm(Form form)
		{
			if (form is null)
				SetDefaultValues();
			else
			{
				this._location = form.Location;
				this._size = form.Size;
				this._state = form.WindowState;
				this._visible = form.Visible;
				this._formType = form.GetType();
				this._name = form.Name;
			}
		}

		public void ImportConfigItem(IniFormItem item)
		{
			FormSettings s = Parse(item);
			this._location = s._location;
			this._size = s._size;
			this._state = s._state;
			this._visible = s._visible;
			this._formType = s._formType;
			this._name = s._name;
		}

		protected void SetDefaultValues()
		{
			this._location = new Point(0, 0);
			this._size = new Size(0, 0);
			this._state = FormWindowState.Normal;
			this._visible = true;
			this._name = "";
			this._formType = null;
		}

		public static implicit operator FormSettings(Form data) => new FormSettings(data);

		public override bool Equals(object obj) => base.Equals(obj);

		public override int GetHashCode() => base.GetHashCode();
		#endregion

		#region Static Methods
		public static bool IsFormType(dynamic form) =>
			IsFormType(form.GetType());

		public static bool IsFormType(Type test) =>
			(test is null) || (test.GetType() == typeof(Object)) ? false : (test.GetType() == typeof(Form) || IsFormType(test.BaseType));

		public static FormSettings Parse(IniFormItem item)
		{
			FormSettings result = new FormSettings();
			result._formType = Type.GetType(item.Key);
			result._location = item.Location;
			result._size = item.Size;
			result._state = item.WindowState;
			result._visible = item.Visible;
			return result;
		}

		public static IniFormItem Parse(FormSettings settings) =>
			IniFormItem.Parse(settings._formType.Name, settings._formType.BaseType.Name, settings.Location, settings.Size, settings.Visible);

		public static FormSettings Parse(IniLineItem item) => Parse(new IniFormItem(item));

		public static FormSettings Default =>
			new FormSettings();
		#endregion
	}

	internal class FormSettingsCollection : IEnumerator<FormSettings>
	{
		protected List<FormSettings> _forms = new List<FormSettings>();
		private int _position = 0;

		#region Constructors
		public FormSettingsCollection() { }

		public FormSettingsCollection(FormSettings settings) =>
			this._forms.Add(settings);

		public FormSettingsCollection(Form form) =>
			this._forms.Add(new FormSettings(form));

		public FormSettingsCollection(IniFormItem formItem) =>
			this._forms.Add(FormSettings.Parse(formItem));

		public FormSettingsCollection(IniGroupItem group) =>
			this.AddRange(group);
		#endregion

		#region Accessors
		/// <summary>Reports the number of forms currently being managed in this collection.</summary>
		public int Count => this._forms.Count;

		public FormSettings this[int index]
		{
			get
			{
				if ((index < 0) || (index >= this.Count))
					throw new IndexOutOfRangeException(index.ToString() + " is out of range! (0-" + Math.Max(0, this.Count - 1).ToString() + ")");

				return this._forms[index];
			}
			set
			{
				if ((index < 0) || (index >= this.Count))
					throw new IndexOutOfRangeException(index.ToString() + " is out of range! (0-" + Math.Max(0, this.Count - 1).ToString() + ")");

				this._forms[index] = value;
			}
		}

		public FormSettings this[Form form]
		{
			get => this[IndexOf(form)];
			set => this[IndexOf(form)] = value;
		}

		public FormSettings this[Type formType]
		{
			get => this[IndexOf(formType)];
			set => this[IndexOf(formType)] = value;
		}

		public bool IsDefault
		{
			get
			{
				foreach (FormSettings settings in this)
					if (!settings.IsDefault) return false;

				return true;
			}
		}

		FormSettings IEnumerator<FormSettings>.Current => this[this._position];

		object IEnumerator.Current => this[this._position];
		#endregion

		#region Methods
		protected int IndexOf(FormSettings value) =>
			IndexOf(value.TypeName);

		protected int IndexOf(Type formType) =>
			IndexOf(formType.Name);

		protected int IndexOf(string typeName)
		{
			int i = -1; while ((++i < this.Count) && !this._forms[i].TypeName.Equals(typeName, StringComparison.InvariantCultureIgnoreCase)) ;
			return (i < Count) ? i : -1;
		}

		public void Add(FormSettings settings)
		{
			if (!(settings is null))
			{
				int i = IndexOf(settings);
				if (i < 0)
					this._forms.Add(settings);
				else
					this[i] = settings;
			}
		}

		public void Add(Form form)
		{
			if (FormSettings.IsFormType(form))
				this.Add(new FormSettings(form));
		}

		public void Add(IniFormItem item) =>
			this.Add(FormSettings.Parse(item));

		public void AddRange(IniGroupItem configGroup)
		{
			foreach (IniLineItem item in configGroup)
				this.Add(FormSettings.Parse(item));
		}

		public void AddRange(IniFormItem[] items)
		{
			foreach (IniLineItem item in items)
				this.Add(FormSettings.Parse(item));
		}

		public void AddRange(FormSettings[] settings)
		{
			foreach (FormSettings setting in settings)
				this.Add(setting);
		}

		public bool HasForm(Type formType) =>
			(IndexOf(formType) >= 0);

		public bool HasForm(Form form) =>
			HasForm(form.GetType());

		public bool HasForm(FormSettings settings) =>
			HasForm(settings.FormType);

		public bool HasForm(string formTypeName) =>
			(IndexOf(formTypeName) >= 0);

		public void RemoveAt(int index, bool silent = false)
		{
			if (!silent && ((index < 0) || (index >= this.Count)))
				throw new IndexOutOfRangeException(index.ToString() + " is out of range! (0-" + Math.Max(0, this.Count - 1).ToString() + ")");

			this._forms.RemoveAt(index);
		}

		public void Remove(FormSettings settings) =>
			this.RemoveAt(IndexOf(settings), true);

		public void Remove(Form form) =>
			this.RemoveAt(IndexOf(form), true);

		public void Remove(Type formType) =>
			this.RemoveAt(IndexOf(formType), true);

		public FormSettings[] ToArray() =>
			this._forms.ToArray();

		public IniGroupItem ToConfigGroup(string name)
		{
			IniGroupItem cF = new IniGroupItem(name);

			foreach (FormSettings set in this._forms)
				cF.Add(set.ToConfigItem());

			return cF;
		}
		#endregion

		//IEnumerator Support
		public IEnumerator<FormSettings> GetEnumerator() => this._forms.GetEnumerator();

		bool IEnumerator.MoveNext() => (++this._position) < this._forms.Count;

		void IEnumerator.Reset() => this._position = 0;

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects).
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~AppletParameters() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		void IDisposable.Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
	}
}
