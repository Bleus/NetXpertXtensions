#define GenericFormManagement

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Cobblestone.Classes;
using Cobblestone.Classes.Settings;

namespace Cobblestone.Forms
{
	/// <summary>Enhances the default Windows' Form class for use in this application.</summary>
	[TypeDescriptionProvider(typeof(VS_AbstractionOverride.AbstractControlDescriptionProvider<FormManagementObject, Form>))]
	internal abstract class FormManagementObject : Form
	{
		#region Properties
		//protected string _name;
		protected bool _formPreventClose = true;
		protected string _trackerId;
		protected FormWindowState _lastState;
		#endregion

		#region Constructors
		//[Obsolete("Designer only", true)]
		private FormManagementObject() : base()
		{
			this.Name = this.GetType().Name;
			this.Initialise();
		}

		public FormManagementObject(string name) : base()
		{
			this.Name = (name.Length == 0) ? this.GetType().Name : name;
			this.Initialise();
		}

		public FormManagementObject(FormSettings settings) : base()
		{
			this.Name = this.GetType().Name;
			this.Initialise();
			this.FormSettings = settings;
		}

		public FormManagementObject(string name, FormSettings settings) : base()
		{
			this.Name = (name.Length == 0) ? this.GetType().Name : name;
			this.Initialise();
			this.FormSettings = settings;
		}

		private void Initialise()
		{
			this._lastState = base.WindowState;
			this._trackerId = Guid.NewGuid().ToString();
			this.FormSettings = Common.Settings[this];
			base.Icon = global::Cobblestone.Properties.Resources.Cobblestone__32x32_;
			this.FormClosing += OnFormClosing;
		}
		#endregion

		#region Accessors
		new public string Name
		{
			get => base.Name;
			set { if (!String.IsNullOrEmpty(value)) { base.Name = value; } }
		}

		public string TrackerId => this._trackerId;

		public bool AllowMultiples => FormManagementObject.AllowMultipleInstances();

		new public Icon Icon
		{
			get => base.Icon;
			protected set => base.Icon = value;
		}

		public FormSettings FormSettings
		{
			get => new FormSettings(this);
			set
			{
				if (!(value is null))
				{
					this.Size = value.Size;
					this.Location = value.Location;
					this.WindowState = value.WindowState;
					this.Visible = value.Visible;
				}
			}
		}
		#endregion

		#region Operators
		public static bool operator ==(FormManagementObject left, string right)
		{
			if (left is null) return string.IsNullOrEmpty(right);
			if (string.IsNullOrEmpty(right)) return false;

			return (string.Compare(left.Name, right, true) == 0);
		}

		public static bool operator !=(FormManagementObject left, string right) => !(left == right);

		public static bool operator ==(FormManagementObject left, FormManagementObject right)
		{
			if (left is null) return (right is null);
			if (right is null) return false;

			return (left.Name.Equals(right.Name, StringComparison.InvariantCultureIgnoreCase));
		}

		public static bool operator !=(FormManagementObject left, FormManagementObject right) => !(left == right.Name);
		#endregion

		#region Methods
		public abstract string MyTypeName();

		public bool ApplyFormSettings(FormSettings settings) =>
			ApplyFormSettings(Common.Console.Api, settings);

		public bool ApplyFormSettings(CobblestoneConsole console, FormSettings settings) =>
			ApplyFormSettings(console.Api, settings);

		public bool ApplyFormSettings(Controls.CLI_API target, FormSettings settings)
		{
			if (!(target is null))
			{
				target.WriteInduction("Applying console settings", "Windows.Forms." + MyTypeName(), new string[] { "Size", "Location" });
				if (!(settings is null))
				{
					this.FormSettings = settings;
					target.WriteCheckboxLn(true);
					return true;
				}
				target.WriteCheckboxLn(false);
			}
			return false;
		}

		protected void SaveSettings()
		{
			//string ini = (!(Common.Https is null) && Common.Https.SessionOpen && Common.Settings.HasIni(Common.UserName + ".ini")) ?
			//	Common.UserName : "Cobblestone";

			Common.Settings["*"].SetFormSettings(this);
		}

		protected override void OnResizeEnd(EventArgs e)
		{
			this.SaveSettings();
			base.OnResizeEnd(e);
		}

		protected override void OnMove(EventArgs e)
		{
			this.SaveSettings();
			base.OnMove(e);
		}

		protected override void OnClientSizeChanged(EventArgs e)
		{
			if (this.WindowState != FormWindowState.Minimized) this._lastState = this.WindowState;
			this.SaveSettings();
			base.OnClientSizeChanged(e);
		}

		protected virtual void OnFormClosing(object sender, FormClosingEventArgs e)
		{
			if (this._formPreventClose)
			{
				this.SaveSettings();
				Controls.CLI_API api = Common.Console.Api;
				api.ClearLine();
				if (!this.AllowMultiples)
				{
					api.WriteInduction("De-activating", "Windows.Forms", MyTypeName());
					this.Hide();
					e.Cancel = true;
					api.WriteCheckboxLn(true);
				}
				else
				{
					if (!Common.Forms.CloseTracker(this.TrackerId))
					{
						api.Write("ALERT: ", "The attempt to close ", api.Styles.Alert, api.Styles.Default);
						api.Write("Windows.Forms(", api.Styles.Highlight1);
						api.WriteLn(this.GetType().Name, ") failed.", api.Styles.Highlight2, api.Styles.Default);
						api.Write("  * Reason: ", api.Styles.Highlight1);
						api.Write("The Tracker (", this.TrackerId, api.Styles.Default, api.Styles.Highlight3);
						api.WriteLn(") couldn't be removed from the FormManagement cache.", api.Styles.Default);
						e.Cancel = true;
					}
					else
					{
						api.WriteLn();
						api.WriteInduction("Closing", "Windows.Forms", this.GetType().Name);
						api.WriteCheckboxLn(true);
					}
				}
				Common.Console.Prompt.Show();
			}
		}

		/// <summary>
		/// Upgrades the Form.Close() function to allow overriding the FormManagementObject's default action of preventing these
		/// Forms from being closed arbitrarily.
		/// </summary>
		/// <param name="trigger">If set to TRUE, the form is forced to close without regard for the PreventClose settings.</param>
		public void Close(bool trigger)
		{
			if (trigger)
				this._formPreventClose = false;

			this.Close();
		}

		new public void Activate()
		{
			this.TopMost = true;
			if (this.WindowState == FormWindowState.Minimized)
				this.WindowState = this._lastState;

			if (!this.Visible)
				this.Visible = true;

			this.Show();
			base.Activate();
			this.TopMost = false;
		}

		public abstract void LoadContents(dynamic data);

		public override int GetHashCode() => base.GetHashCode();

		public override bool Equals(object obj) => base.Equals(obj);

		public override string ToString() => this.Name + "{" + this.Handle + "}";

		#region Static Methods
		public static bool AllowMultipleInstances() => true;

		public static bool AllowMultipleInstances(Type byType)
		{
			if (!IsManagedForm(byType)) throw InvalidType(byType);

			var methodInfo = byType.GetMethod("AllowMultipleInstances");
			return (methodInfo is null) ? AllowMultipleInstances() : (bool)methodInfo.Invoke(null, null);
		}

		public static bool IsManagedForm(object obj) =>
			IsManagedForm(obj.GetType());

		public static bool IsManagedForm(Type test) =>
			((test == typeof(Object)) || (test is null)) ? false : ((test == typeof(FormManagementObject)) || IsManagedForm(test.BaseType));

		public static ArgumentException InvalidType(Type type) =>
			InvalidType(type.Name);

		public static ArgumentException InvalidType(string name) =>
			new ArgumentException("The specified source object is not a valid type (\"" + name + "\")");

		public static Type ParseTypeName(string name)
		{
			if (!IsManagedForm(name)) throw InvalidType(name);
			return Type.GetType(name);
		}

		public static bool ValidateType(Type check, bool quiet = false)
		{
			bool result = IsManagedForm(check);
			if (!result && !quiet)
				throw InvalidType(check);
			return result;
		}

		public static Color IdealTextColor(Color bg)
		{
			int nThreshold = 105;
			int bgDelta = Convert.ToInt32((bg.R * 0.299) + (bg.G * 0.587) +
										  (bg.B * 0.114));

			Color foreColor = (255 - bgDelta < nThreshold) ? Color.Black : Color.White;
			return foreColor;
		}

		public static Color IdealTextColor(Label source)
		{
			source.ForeColor = IdealTextColor(source.BackColor);
			return source.ForeColor;
		}

		public static Color IdealTextColor(TextBox source)
		{
			source.ForeColor = IdealTextColor(source.BackColor);
			return source.ForeColor;
		}

		public static dynamic CreateInstance(string name) =>
			Activator.CreateInstance(ParseTypeName(name));
		#endregion
		#endregion
	}

#if GenericFormManagement
	#region FormManagement Objects using Generics
	/// <summary>Provides a common "base" class for all derivative generic FormManagementHandle<T> classes.</summary>
	internal class FormManagementHandleBase
	{
		protected Guid _myGuid;
		protected bool _allowMultiples;

		#region Constructors
		public FormManagementHandleBase() =>
			Initialise(Guid.NewGuid());

		public FormManagementHandleBase(Guid uid) =>
			Initialise(uid);

		private void Initialise(Guid uid)
		{
			this._myGuid = uid;
			this._allowMultiples = true;
		}
		#endregion

		#region Operators
		public static bool operator ==(FormManagementHandleBase left, Guid right)
		{
			if (left is null) return (right == Guid.Empty);
			return (left.ToString() == right.ToString());
		}

		public static bool operator ==(FormManagementHandleBase left, string right)
		{
			if (left is null) return ((right is null) || (right == string.Empty) || (right == "") || (right.Length == 0));
			if ((right is null) || (right == string.Empty) || (right == "") || (right.Length == 0)) return false;
			return left.ToString() == right;
		}

		public static bool operator ==(FormManagementHandleBase left, FormManagementHandleBase right)
		{
			if (left is null) return (right is null);
			if (right is null) return false;
			return (left.Id == right.Id);
		}

		public static bool operator !=(FormManagementHandleBase left, Guid right) => !(left == right);

		public static bool operator !=(FormManagementHandleBase left, string right) => !(left == right);

		public static bool operator !=(FormManagementHandleBase left, FormManagementHandleBase right) => !(left == right);
		#endregion

		#region Accessors
		public string Id =>
			this.ToString();

		public dynamic Form
		{
			get
			{
				if (Common.Forms is null)
					throw new ArgumentOutOfRangeException("Error: The Common.Forms collection has not been instantiated.");

				dynamic form = Common.Forms.GetTracker(this).Form;

				if (form is null)
					throw new ArgumentOutOfRangeException("Error: There is no active instance of this form in the library.");

				return form;
			}
		}

		public bool AllowMultiples =>
			this._allowMultiples;
		#endregion

		#region Methods
		public override string ToString() =>
			this._myGuid.ToString();

		protected T Convert<T>(object input) where T : FormManagementObject =>
			(T)System.Convert.ChangeType(input, typeof(T));

		protected T Instantiate<T>() where T : FormManagementObject
		{
			T form = this.Convert<T>(Activator.CreateInstance(typeof(T))); //, typeof(T)));
			string name = FormManagementHandleBase.CreateName(typeof(T), (form as FormManagementObject).AllowMultiples);
			(form as FormManagementObject).Name = name;
			return form;
		}

		public override bool Equals(object obj) =>
			base.Equals(obj);

		public override int GetHashCode() =>
			base.GetHashCode();
		#endregion

		#region Static Methods to serve FormManagementHandle<T>
		public static string CreateName(Type type, bool randomize)
		{
			string name = type.Name;
			if (randomize)
			{
				string unixTimestamp = DateTime.Now.UnixTimeStamp().ToString();
				string ms = DateTime.Now.Millisecond.ToString();
				name += "(" + unixTimestamp.PadLeft(16, '0') + ms.PadLeft(4, '0') + ")";
			}
			return name;
		}

		/// <summary>Creates a new FormManagementTracker&lt;T&gt; class from the <i>name</i> of the desired FormManagementObject derivative class.</summary>
		/// <param name="typeName">A string specifying the name of the desired FormManagermentObject derivative to base the new Tracker on.</param>
		/// <returns>A new FormManagementTracker&lt;T&gt; object based on the specified FormManagementObject class's name.</returns>
		public static dynamic CreateHandleInstance(string typeName)
		{
			if (!FormManagementObject.IsManagedForm(typeName))
				throw FormManagementObject.InvalidType(typeName);

			Type formManagementType = Type.GetType(typeName);
			return CreateHandleInstance(formManagementType);
		}

		/// <summary>Creates a new FormManagementTracker&lt;T&gt; class from the <i>type</i> of the desired FormManagementObject derivative class.</summary>
		/// <param name="formType">A Type object specifying the desired FormManagermentObject derivative to base the new Tracker on.</param>
		/// <returns>A new FormManagementTracker&lt;T&gt; object based on the specified FormManagementObject derivative class .</returns>
		public static dynamic CreateHandleInstance(Type formType)
		{
			if (!FormManagementObject.IsManagedForm(formType))
				throw FormManagementObject.InvalidType(formType);

			Type formManagementHandleType = typeof(FormManagementHandle<>);
			Type newHandleType = formManagementHandleType.MakeGenericType(formType);
			dynamic result = Activator.CreateInstance(newHandleType);
			return result;
		}
		#endregion
	}

	/// <summary>
	///
	/// </summary>
	/// <typeparam name="T"></typeparam>
	internal class FormManagementHandle<T> : FormManagementHandleBase where T : FormManagementObject
	{
		#region Constructors
		public FormManagementHandle() : base() =>
			this._allowMultiples = FormManagementObject.AllowMultipleInstances(typeof(T));

		protected FormManagementHandle(Guid id) : base(id) =>
			this._allowMultiples = FormManagementObject.AllowMultipleInstances(typeof(T));
		#endregion

		#region Accessors
		new public T Form
		{
			get
			{
				T form = Common.Forms.GetTracker(this).Form;
				if (form is null) throw new ArgumentOutOfRangeException("Error: There is no active instance of this form in the library.");
				return form;
			}
		}

		public Type FormType =>
			typeof(T);

		public FormWindowState WindowState =>
			(this.Form as FormManagementObject).WindowState;
		#endregion

		#region Methods
		public T Instantiate() =>
			base.Instantiate<T>();
		#endregion
	}

	/// <summary>Provides a common "base" class for all derivative generic FormManagementHandleCollection<T> classes.</summary>
	internal class FormManagementHandleCollectionBase : IEnumerator<FormManagementHandleBase>
	{
		protected Type _myType = typeof(FormManagementHandleBase);
		protected List<FormManagementHandleBase> _handles = new List<FormManagementHandleBase>();
		protected int _position = 0;

		#region Constructors
		public FormManagementHandleCollectionBase(Type containedType)
		{
			if (!FormManagementObject.IsManagedForm(containedType))
				throw FormManagementObject.InvalidType(containedType);

			this._myType = containedType;
		}

		public FormManagementHandleCollectionBase(string containedTypeName)
		{
			if (!FormManagementObject.IsManagedForm(containedTypeName))
				throw FormManagementObject.InvalidType(containedTypeName);

			this._myType = FormManagementObject.ParseTypeName(containedTypeName);
		}
		#endregion

		#region Accessors
		public int Count =>
			this._handles.Count;

		public FormManagementHandleBase[] Instances =>
			this._handles.ToArray();

		public FormManagementHandleBase this[int index]
		{
			get => ((index >= 0) && (index < this.Count)) ? this._handles[index] : null;
			set { if ((index >= 0) && (index < this.Count)) { this._handles[index] = value; } }
		}

		public FormManagementHandleBase this[string uid]
		{
			get
			{
				int i = this.IndexOf(uid);
				return (i < 0) ? null : this._handles[i];
			}
		}

		FormManagementHandleBase IEnumerator<FormManagementHandleBase>.Current =>
			this[this._position];

		object IEnumerator.Current =>
			this._handles[this._position];
		#endregion

		#region Methods
		protected int IndexOf(string uid)
		{
			uid = uid.Trim();
			if (string.IsNullOrWhiteSpace(uid) || (uid.Length == 0)) return -1;

			int i = -1; while ((++i < _handles.Count) && !_handles[i].Id.Equals(uid, StringComparison.InvariantCultureIgnoreCase)) ;
			return (i < this.Count) ? i : -1;
		}

		protected bool HasHandle<T>(FormManagementHandle<T> handle) where T : FormManagementObject =>
			(this.IndexOf(handle.Id) >= 0);

		public virtual bool HasHandle(FormManagementHandleBase handle) => throw new NotImplementedException();

		public virtual bool HasHandle(string uid) => throw new NotImplementedException();

		protected List<FormManagementHandle<T>> Upgrade<T>() where T : FormManagementObject
		{
			List<FormManagementHandle<T>> items = new List<FormManagementHandle<T>>();
			foreach (FormManagementHandleBase obj in this._handles)
				items.Add(obj as FormManagementHandle<T>);

			return items;
		}

		public bool AddHandle(FormManagementHandleBase handle)
		{
			if (handle.IsNotNull())
			{
				if ((this.Count == 0) || !this.HasHandle(handle))
				{
					this._handles.Add(handle);
					return true;
				}
			}
			return false;
		}

		public bool AddHandles(FormManagementHandleBase[] handles)
		{
			bool result = true;
			if (handles.Length > 0)
				foreach (FormManagementHandleBase handle in handles)
					result &= this.AddHandle(handle);

			return result;
		}

		public bool RemoveHandle(FormManagementHandleBase handle) =>
			(handle is null) ? true : RemoveHandle(handle.Id);

		public bool RemoveHandle(string uid)
		{
			int i = IndexOf(uid);
			if (i >= 0)
				this._handles.RemoveAt(i);

			return (i >= 0);
		}

		protected FormManagementHandle<T>[] ToArray<T>() where T : FormManagementObject =>
			Upgrade<T>().ToArray();

		public FormManagementHandleBase[] ToArray() =>
			_handles.ToArray();

		//IEnumerator Support
		public IEnumerator<FormManagementHandleBase> GetEnumerator() =>
			new List<FormManagementHandleBase>().GetEnumerator();

		bool IEnumerator.MoveNext() => (++this._position) < this._handles.Count;

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

		#region Static Methods
		public static bool IsHandleCollection(object obj) =>
			IsHandleCollectionForm(obj.GetType());

		public static bool IsHandleCollectionForm(Type test) =>
			(test == typeof(Object)) ? false : ((test == typeof(FormManagementHandleBase)) || IsHandleCollectionForm(test.BaseType));

		public static ArgumentException InvalidType(object obj) =>
			InvalidType(obj.GetType().Name);

		public static ArgumentException InvalidType(Type type) =>
			InvalidType(type.Name);

		public static ArgumentException InvalidType(string name) =>
			new ArgumentException("The specified source object is not a valid FormManagementHandle type (\"" + name + "\")");

		/// <summary>Creates a new FormManagementTracker&lt;T&gt; class from the <i>name</i> of the desired FormManagementObject derivative class.</summary>
		/// <param name="typeName">A string specifying the name of the desired FormManagermentObject derivative to base the new Tracker on.</param>
		/// <returns>A new FormManagementTracker&lt;T&gt; object based on the specified FormManagementObject class's name.</returns>
		public static dynamic CreateTrackerInstance(string typeName)
		{
			if (!FormManagementObject.IsManagedForm(typeName))
				throw FormManagementObject.InvalidType(typeName);

			Type formManagementType = Type.GetType(typeName);
			return CreateTrackerInstance(formManagementType);
		}

		/// <summary>Creates a new FormManagementTracker&lt;T&gt; class from the <i>type</i> of the desired FormManagementObject derivative class.</summary>
		/// <param name="formType">A Type object specifying the desired FormManagermentObject derivative to base the new Tracker on.</param>
		/// <returns>A new FormManagementTracker&lt;T&gt; object based on the specified FormManagementObject derivative class .</returns>
		public static dynamic CreateTrackerInstance(Type formType)
		{
			if (!FormManagementObject.IsManagedForm(formType))
				throw FormManagementObject.InvalidType(formType);

			Type formManagementHandleTrackerType = typeof(FormManagementHandleTracker<>);
			Type newTrackerType = formManagementHandleTrackerType.MakeGenericType(formType);
			dynamic result = Activator.CreateInstance(newTrackerType);
			return result;
		}
		#endregion
		#endregion
	}

	/// <summary>Class to manage a collection of FormManagementHandle objects.</summary>
	/// <typeparam name="T">FormManagementObject derivatives.</typeparam>
	internal class FormManagementHandleCollection<T> : FormManagementHandleCollectionBase, IEnumerator<FormManagementHandle<T>> where T : FormManagementObject
	{
		//protected List<FormManagementHandle<T>> _handles = new List<FormManagementHandle<T>>();

		#region Constructors
		public FormManagementHandleCollection() : base(typeof(T)) { }

		public FormManagementHandleCollection(FormManagementHandle<T> handle) : base(typeof(T)) =>
			this.AddHandle(handle);

		public FormManagementHandleCollection(FormManagementHandle<T>[] handles) : base(typeof(T)) =>
			this.AddHandles(handles);

		#endregion

		#region Accessors
		new public FormManagementHandle<T>[] Instances =>
			this.ToArray<T>();

		new public FormManagementHandle<T> this[int index]
		{
			get => ((index >= 0) && (index < this.Count)) ? (this._handles[index] as FormManagementHandle<T>) : null;
			set { if ((index >= 0) && (index < this.Count)) { this._handles[index] = value; } }
		}

		new public FormManagementHandle<T> this[string uid]
		{
			get
			{
				int i = this.IndexOf(uid);
				return (i < 0) ? null : (this._handles[i] as FormManagementHandle<T>);
			}
		}

		FormManagementHandle<T> IEnumerator<FormManagementHandle<T>>.Current =>
			this[this._position];

		object IEnumerator.Current =>
			(this._handles[this._position] as FormManagementHandle<T>);
		#endregion

		#region Methods
		public bool AddHandle(FormManagementHandle<T> handle) =>
			base.AddHandle(handle);

		public bool AddHandles(FormManagementHandle<T>[] handles) =>
			base.AddHandles(handles);

		public bool RemoveHandle(FormManagementHandle<T> handle) =>
			base.RemoveHandle(handle);

		new public FormManagementHandle<T>[] ToArray() =>
			base.ToArray<T>();

		public override string ToString()
		{
			string result = "";
			foreach (FormManagementHandle<T> h in this._handles)
				result += h.ToString() + "\r\n";

			return result;
		}

		//IEnumerator Support
		new public IEnumerator<FormManagementHandle<T>> GetEnumerator() =>
			new List<FormManagementHandle<T>>().GetEnumerator();

		bool IEnumerator.MoveNext() => (++this._position) < this._handles.Count;

		void IEnumerator.Reset() => this._position = 0;

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected override void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects).
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.
				base.Dispose(disposing);
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
		#endregion
	}

	/// <summary>This class is the Form controller that manages the form itself, as well as all of the handles that reference it.</summary>
	/// <typeparam name="T">FormManagementObject derivatives.</typeparam>
	internal class FormManagementHandleTracker<T> : FormManagementHandleCollection<T>, IEnumerator<FormManagementHandle<T>> where T : FormManagementObject
	{
		protected T _form;

		#region Constructors
		public FormManagementHandleTracker() : base()
		{
			if (!FormManagementObject.IsManagedForm(Type)) throw FormManagementObject.InvalidType(Type);

			this._form = new FormManagementHandle<T>().Instantiate();
		}

		public FormManagementHandleTracker(FormManagementHandle<T> handle) : base(handle) =>
			this._form = handle.Instantiate();

		public FormManagementHandleTracker(FormManagementHandle<T>[] handles) : base()
		{
			if (handles.Length == 0)
				throw new ArgumentOutOfRangeException("You cannot instantiate this object with an empty array.");

			this._form = handles[0].Instantiate();
			this.AddInstances(handles);
		}

		public FormManagementHandleTracker(FormManagementHandleCollection<T> collection) : base()
		{
			if (collection.Count == 0)
				throw new ArgumentOutOfRangeException("You cannot instantiate this object with an empty collection.");

			this._form = collection[0].Instantiate();
			this.AddInstances(collection.ToArray());
		}
		#endregion

		#region Accessors
		public TypeInfo Type => (TypeInfo)typeof(T);

		public T Form => this._form;

		public string Name => this._form.Name;

		protected FormManagementObject FmoForm => (this._form as FormManagementObject);

		public string Id => this.FmoForm.TrackerId;

		public bool IsOpen =>
			!(_form is null) && (_form.WindowState != FormWindowState.Minimized) && _form.Visible && (_form.Width > 0) && (_form.Height > 0);
		#endregion

		#region Methods
		protected bool HasHandle(FormManagementHandle<T> handle) =>
			base.HasHandle(handle);

		new protected bool HasHandle(string uid) =>
			base.HasHandle(uid);

		new protected bool AddHandle(FormManagementHandle<T> handle) =>
			base.AddHandle(handle);

		new protected bool AddHandles(FormManagementHandle<T>[] handles) =>
			base.AddHandles(handles);

		new protected bool RemoveHandle(FormManagementHandle<T> handle) =>
			base.RemoveHandle(handle);

		new protected bool RemoveHandle(string uid) =>
			base.RemoveHandle(uid);

		public bool HasInstance(FormManagementHandle<T> handle) =>
			base.HasHandle(handle);

		public bool HasInstance(string uid) =>
			base.HasHandle(uid);

		public bool AddInstance(FormManagementHandle<T> handle)
		{
			//if (handle.FormType != this.Type)
			//	throw new ArgumentException("You cannot add an object of type \"" + handle.FormType.Name + "\" to a Tracker for type \"" + this._type.Name + "\".");

			return base.AddHandle(handle);
		}

		public bool AddInstances(FormManagementHandle<T>[] handles)
		{
			bool result = true;
			foreach (FormManagementHandle<T> fh in handles)
				result &= this.AddInstance(fh);

			return result;
		}

		public bool AddInstances(FormManagementHandleCollection<T> collection) =>
			this.AddInstances(collection.ToArray());

		public bool RemoveInstance(FormManagementHandle<T> handle) =>
			base.RemoveHandle(handle);

		public bool RemoveInstance(string uid) =>
			base.RemoveHandle(uid);

		public bool Activate(FormManagementHandle<T> handle)
		{
			if (!this.HasHandle(handle))
				throw new ArgumentOutOfRangeException("The FormManagementHandle \"" + handle.Id + "\" doesn't exist in this collection.");

			return Activate();
 		}

		public bool Activate()
		{
			if (this._form is null) return false;

			if (this.FmoForm.WindowState == FormWindowState.Minimized)
				this.FmoForm.WindowState = FormWindowState.Normal;

			this.FmoForm.Show();
			this.FmoForm.Activate();
			return true;
		}

		public bool Hide(FormManagementHandle<T> handle)
		{
			if (!this.HasHandle(handle))
				throw new ArgumentOutOfRangeException("The FormManagementHandle \"" + handle.Id + "\" doesn't exist in this collection.");

			return Hide();
		}

		public bool Hide()
		{
			if (this._form is null) return false;

			if (this.FmoForm.WindowState != FormWindowState.Minimized)
				this.FmoForm.Hide();

			return true;
		}

		public void Close()
		{
			foreach (FormManagementHandle<T> handle in this)
				this.RemoveHandle(handle);

			(this._form as FormManagementObject).Close(true);
			this._form = null;
		}

		public FormManagementHandle<T> CreateHandle(bool withAdd = false)
		{
			FormManagementHandle<T> newHandle = FormManagementHandleBase.CreateHandleInstance(typeof(T));
			if (withAdd)
				this.AddHandle(newHandle);
			return newHandle;
		}

		public override string ToString() => base.ToString() + "(" + this._form.GetType().Name + ")";

		protected override void Dispose(bool disposing)
		{
			if (this._form.IsNotNull())
				this.Close();

			base.Dispose(disposing);
		}
		#endregion
	}

	internal class FormManagementQueue
	{
		protected List<dynamic> _activeTrackers;

		#region Constructors
		public FormManagementQueue() =>
			this._activeTrackers = new List<dynamic>();

		//~FormManagementQueue() => this.CloseAllForms();
		#endregion

		#region Accessors
		/// <summary>Gets the value of the FormManagementObjectTracker with the specified index.</summary>
		/// <param name="index">The index within the current FormManagementTracker list to return.</param>
		/// <returns>The FormManagementObject&lt;T&gt; object at the specified index (or null if the specified index is out of range).</returns>
		protected dynamic this[int index]
		{
			get
			{
				if ((index < 0) || (index >= this.Count)) throw new ArgumentOutOfRangeException();
				return this._activeTrackers[index];
			}
			set
			{
				if (!FormManagementHandleCollectionBase.IsHandleCollection(value))
					throw (ArgumentException)FormManagementHandleCollectionBase.InvalidType(value.GetType().Name);

				this._activeTrackers[index] = value;
			}
		}

		/// <summary>Reports the current number of known FormManagementTracker&lt;T&gt; objects in the class.</summary>
		public int Count =>
			this._activeTrackers.Count;

		/// <summary>Attempts to locate a known tracker with the specified Id and return it.</summary>
		/// <param name="trackerId">A string specifying the TrackerId to search for.</param>
		/// <returns>A FormManagementTracker&lt;T&gt; object corresponding to the specified Id, otherwise NULL.</returns>
		public dynamic this[string trackerId]
		{
			get
			{
				int i = IndexOfTracker(trackerId);
				return (i >= 0) ? this._activeTrackers[i] : null;
			}
		}
		#endregion

		#region Methods
		/// <summary>
		/// Searches the library for the first instance of a form of the specified type.
		/// NOTE: Forms of types that AllowMultiple instances may occur more than once in the Library, so this could
		/// return the wrong form if used in that circumstance! To guarantee receiving a specific form, you should
		/// use IndexOfHandle instead!
		/// </summary>
		/// <seealso cref="IndexOfHandle(FormManagementHandleBase)"/>
		/// <typeparam name="T">A FormManagementObject drivative class.</typeparam>
		/// <returns>An index from the Library corresponding to a stored form of the matching type, or -1 if not found.</returns>
		protected int[] IndexOfType<T>() where T : FormManagementObject
		{
			List<int> indexList = new List<int>();
			for (int i = 0; i < this.Count; i++)
				if (this[i] is T)
					indexList.Add(i);

			return (indexList.Count < 1) ? new int[] { -1 } : indexList.ToArray();
		}

		/// <summary>Returns the index of the first tracker instance index from the list that matches the specified type.</summary>
		/// <typeparam name="T">A FormManagementObject derivative class.</typeparam>
		/// <returns>The index id of a known tracker of the specified type, if found; otherwise -1</returns>
		protected int FirstIndexOfType<T>() where T : FormManagementObject
		{
			int i = -1; while ((++i < this.Count) && !(this._activeTrackers is T)) ;
			return (i<this.Count) ? i : -1;
		}

		/// <summary>Returns the index of the first tracker instance index from the list that matches the specified type.</summary>
		/// <param name="byType">A FormManagementObject derivative class Type.</param>
		/// <returns>The index id of a known tracker of the specified type, if found; otherwise -1</returns>
		protected int FirstIndexOfType(Type byType) =>
			FirstIndexOfType(byType.Name);

		/// <summary>Returns the index of the first tracker instance index from the list that matches the specified type.</summary>
		/// <param name="byName">A string specifying the FormManagementObject derivative class to look for.</param>
		/// <returns>The index id of a known tracker of the specified type, if found; otherwise -1</returns>
		protected int FirstIndexOfType(string byName)
		{
			int i = -1; while ((++i < this.Count) && !(this._activeTrackers.GetType().Name.Equals(byName,StringComparison.InvariantCultureIgnoreCase))) ;
			return (i < this.Count) ? i : -1;
		}

		/// <summary>Searches the library for the form corresponding to the specified Handle.</summary>
		/// <param name="handleId">A string containing the HandleId of the precise form to be retrieved.</param>
		/// <returns>An index from the Library corresponding to the requested form, or -1 if not found.</returns>
		protected int[] IndexOfHandle(string handleId)
		{
			List<int> indexList = new List<int>();
			for (int i = 0; i < this.Count; i++)
				if (this[i].HasInstance(handleId))
					indexList.Add(i);

			return (indexList.Count < 1) ? new int[] { -1 } : indexList.ToArray();
		}

		/// <summary>Searches the library for the form corresponding to the specified Handle.</summary>
		/// <param name="handle">A FormManagementHandle specifying the precise form to be retrieved.</param>
		/// <returns>An index from the Library corresponding to the requested form, or -1 if not found.</returns>
		protected int[] IndexOfHandle(FormManagementHandleBase handle) =>
			IndexOfHandle(handle.Id);

		/// <summary>Returns the index of the first tracker instance in the list possessing the specified handle.</summary>
		/// <param name="handleId">A string specifying the Id of the handle to search for.</param>
		/// <returns>If an active tracker contains the specified handle, that tracker's index, otherwise -1</returns>
		protected int FirstIndexOfHandle(FormManagementHandleBase handle) =>
			FirstIndexOfHandle(handle.Id);

		/// <summary>Returns the index of the first tracker instance in the list possessing the specified handle.</summary>
		/// <param name="handle">A FormManagementHandleBase (or derivative) whose handle is being searched for.</param>
		/// <returns>If an active tracker contains the specified handle, that tracker's index, otherwise -1</returns>
		protected int FirstIndexOfHandle(string handleId)
		{
			int i = -1; while ((++i< this.Count) && !this[i]._activeTrackers.HasInstance(handleId)) ;
			return (i<this.Count) ? i : -1;
		}

		/// <summary>Searches the library for the form corresponding to the specified tracker Guid.</summary>
		/// <param name="trackerId">A Guid value corresponding to the Tracker Id that we're searching for.</param>
		/// <returns>The index of the requested tracker, otherwise -1.</returns>
		protected int IndexOfTracker(string trackerId)
		{
			int i = -1; while ((++i < this.Count) && !this[i].Id.Equals(trackerId, StringComparison.InvariantCultureIgnoreCase)) ;
			return (i < this._activeTrackers.Count) ? i : -1;
		}

		/// <summary>Reports whether or not there is a form of the matching Type already in the Library.</summary>
		/// <param name="byType">The form Type to look for.</param>
		/// <returns>TRUE if a stored form of the specified Type was found, otherwise FALSE.</returns>
		public bool HasType<T>() where T : FormManagementObject =>
			(FirstIndexOfType<T>() >= 0);

		/// <summary>Reports whether or not there is a form of the matching Type already in the Library.</summary>
		/// <param name="byType">A FormManagementObject derived type to look for.</param>
		/// <returns>TRUE if a stored form of the specified Type was found, otherwise FALSE.</returns>
		public bool HasType(Type byType) =>
			(FirstIndexOfType(byType) >= 0);

		/// <summary>Reports whether or not there is a form matching the specified Type name in the Library.</summary>
		/// <param name="byName">The name of the FormManagementObject type look for.</param>
		/// <returns>TRUE if a stored FormManagementObject derivative of the specified Type was found, otherwise FALSE.</returns>
		public bool HasType(string byName) =>
			(FirstIndexOfType(byName) >= 0);

		/// <summary>Creates an array of all FormManagementHandle&lt;T&gt; objects in the library matching the specified type.</summary>
		/// <typeparam name="T">A FormManagementObject derivative class.</typeparam>
		/// <returns>An Array of all FormManagementHandle&lt;T&gt; objects currently known to the Library.</returns>
		public FormManagementHandleCollection<T> Collection<T>() where T : FormManagementObject
		{
			//if (!FormManagementHandle.IsManagedForm(typeof(T))) throw FormManagementHandle.InvalidType(typeof(T));
			FormManagementHandleCollection<T> forms = new FormManagementHandleCollection<T>();
			foreach (FormManagementHandleTracker<T> fmt in this.Trackers<T>())
				forms.AddHandles(fmt.Instances);

			return forms;
		}

		/// <summary>Collates an array of trackers from the known trackers list that match the specified type.</summary>
		/// <typeparam name="T">A FormManagementObject derivative class.</typeparam>
		/// <returns>An array of active FormManagementHandleTracker&lt;T&gt; objects from the known trackers collection.</returns>
		public FormManagementHandleTracker<T>[] Trackers<T>() where T : FormManagementObject
		{
			List<FormManagementHandleTracker<T>> collection = new List<FormManagementHandleTracker<T>>();
			foreach (dynamic tracker in this._activeTrackers)
				if (tracker is T) collection.Add(tracker);

			return collection.ToArray();
		}

		/// <summary>Collates an array of trackers from the known trackers list that match the specified type and have the specified handle.</summary>
		/// <typeparam name="T">A FormManagementObject derivative class.</typeparam>
		/// <returns>An array of active FormManagementHandleTracker&lt;T&gt; objects, with the specified Handle, from the known trackers collection.</returns>
		public FormManagementHandleTracker<T>[] Trackers<T>(string handleId) where T : FormManagementObject
		{
			List<FormManagementHandleTracker<T>> collection = new List<FormManagementHandleTracker<T>>();
			foreach (FormManagementHandleTracker<T> tracker in this.Trackers<T>())
				if (tracker.HasInstance(handleId))
					collection.Add(tracker);

			return collection.ToArray();
		}

		/// <summary>Collates an array of trackers from the known trackers list that manage the specified FormManagementHandle.</summary>
		/// <param name="handle">A FormManagementHandleBase (or derivative) class that's being searched for.</param>
		/// <returns>An array of FormManagementHandle derivative classes that manage the specified FormManagementHandle Id.</returns>
		public dynamic[] Trackers(FormManagementHandleBase handle) =>
			Trackers(handle.Id);

		/// <summary>Collates an array of trackers from the known trackers list that manage the specified FormManagementHandle id.</summary>
		/// <param name="handleId">A string containing the Handle Id that's being searched for.</param>
		/// <returns>An array of FormManagementHandle derivative classes that manage the specified FormManagementHandle Id.</returns>
		public dynamic[] Trackers(string handleId)
		{
			List<dynamic> collection = new List<dynamic>();
			foreach (dynamic fmht in this._activeTrackers)
				if (fmht.HasInstance(handleId))
					collection.Add(fmht);

			return collection.ToArray();
		}

		/// <summary>Collates an array of trackers from the known trackers list that correspond to the supplied FormManagementObject class.</summary>
		/// <param name="byType">A FormManagementObject (or derivative) class type.</param>
		/// <returns>An array of FormManagementTracker&lt;T&gt; classes the manage the specified FormManagementObject type.</returns>
		public dynamic[] Trackers(Type byType)
		{
			byType = FormManagementHandleCollectionBase.CreateTrackerInstance(byType).GetType();
			List<dynamic> collection = new List<dynamic>();
			foreach (dynamic fmht in this._activeTrackers)
				if (fmht.GetType() == byType)
					collection.Add(fmht);

			return collection.ToArray();
		}

		/// <summary>Collates an array of trackers from the known trackers list that correspond to the supplied FormManagementObject class.</summary>
		/// <param name="byType">A FormManagementObject (or derivative) object.</param>
		/// <returns>An array of FormManagementTracker&lt;T&gt; classes the manage objects of the specified FormManagementObject.</returns>
		public dynamic[] Trackers(FormManagementObject form) =>
			Trackers(form.GetType());

		/// <summary>Returns the entire known FormManagementHandleTracker list as an array.</summary>
		public dynamic[] Trackers() =>
			this._activeTrackers.ToArray();

		/// <summary>Activates a Form associated with the specified handle.</summary>
		/// <param name="handle">A FormManagement handle specifying the form to Activate.</param>
		/// <typeparam name="T">A FormManagementObject derivative class.</typeparam>
		/// <returns>TRUE if a form was found with a matching handle and was activated, otherwise FALSE.</returns>
		public bool ActivateForm<T>(FormManagementHandle<T> handle) where T : FormManagementObject
		{
			bool result = true;
			foreach (FormManagementHandleTracker<T> tracker in this.Trackers<T>())
				result &= tracker.Activate();

			return result;
		}

		/// <summary>Activates the first Form in the collection that matches the specified type.</summary>
		/// <param name="byType">The C# Type of the form to activate.</param>
		/// <returns>TRUE if a form was found of the specified type and was activated, otherwise FALSE.</returns>
		public bool ActivateForm<T>() where T : FormManagementObject
		{
			int i = FirstIndexOfType<T>();
			return (i < 0) ? false : this[i].Activate();
		}

		/// <summary>Hides all Forms associated to the specified handle.</summary>
		/// <param name="handle">A FormManagement handle specifying the form to Hide.</param>
		/// <returns>TRUE if a form was found with a matching handle and was hidden, otherwise FALSE.</returns>
		public bool HideForm(FormManagementHandleBase handle)
		{
			bool result = true;
			int[] indexes = IndexOfHandle(handle);
			if (indexes[0] >= 0)
				foreach (int i in indexes)
					result &= this[i].Hide();

			return result;
		}

		/// <summary>Hides the first Form in the collection that matches the specified type.</summary>
		/// <param name="byType">The C# Type of the form to hide.</param>
		/// <returns>TRUE if a form was found of the specified type and was hidden, otherwise FALSE.</returns>
		public bool HideForm<T>() where T : FormManagementObject
		{
			int i = FirstIndexOfType<T>();
			if (i >= 0)
				return this[i].Hide();

			return false;
		}

		/// <summary>Requests a new FormManagementHandle from the collection for a Form of the specified type.</summary>
		/// <param name="withOpen">If set to TRUE, the form is requested is also opened when the new FormManagementHandle is generated.</param>
		/// <returns>A new FormManagementHandle object that refers to an instance of the requested type.</returns>
		public dynamic RequestHandle<T>(bool withOpen = false) where T : FormManagementObject =>
			RequestHandle(typeof(T), withOpen);

		/// <summary>Requests a new FormManagementHandle from the collection for a Form of the specified type.</summary>
		/// <param name="byType">A FormManagementObject derivative Type.</param>
		/// <param name="withOpen">If set to TRUE, the form is requested is also opened when the new FormManagementHandle is generated.</param>
		/// <returns>A new FormManagementHandle object that refers to an instance of the requested type.</returns>
		public dynamic RequestHandle(Type byType, bool withOpen = false)
		{
			if (!FormManagementObject.IsManagedForm(byType))
				throw FormManagementObject.InvalidType(byType);

			dynamic fmht = FormManagementHandleCollectionBase.CreateTrackerInstance(byType);
			int i = this.FirstIndexOfType(byType);
			if ((i < 0) || fmht.AllowMultiples)
				this._activeTrackers.Add(fmht);
			else
				this[i].AddInstance(fmht);

			if (withOpen) { fmht.Form.Show(); fmht.Form.Activate(); }
			return fmht;
		}

		/// <summary>Requests a new FormManagementHandle from the collection for a Form with the specified Uid.</summary>
		/// <param name="uid">A String specifying the Uid to identify the desired Form.</param>
		/// <returns>A new FormManagementHandle object that refers to an instance with the specidifed Uid.</returns>
		public dynamic RequestHandle(string uid) =>
			this[uid];

		/// <summary>Attempts to add a FormManagementHandle object, that was created independently, to the collection.</summary>
		/// <typeparam name="T">A FormManagementObject derived type.</typeparam>
		/// <param name="source">A FormManagementHandle that is to be added to the collection.</param>
		/// <param name="withOpen">If set to TRUE, will cause the specified form to be opened (Shown) as well.</param>
		/// <returns>The same FormManagementHandle object that was submitted to the collection, or null.</returns>
		public FormManagementHandle<T> AddHandle<T>(FormManagementHandle<T> source, bool withOpen = false) where T : FormManagementObject
		{
			FormManagementHandleTracker<T> fmht;
			int i = this.FirstIndexOfType<T>();
			if ((i < 0) || source.AllowMultiples)
			{
				fmht = new FormManagementHandleTracker<T>(source);
				this._activeTrackers.Add(fmht);
			}
			else
				this[i].AddInstance(source);

			if (withOpen) { source.Form.Show(); source.Form.Activate(); }
			return source;
		}

		/// <summary>Attempts to add a FormManagementHandle object, that was created independently, to the collection.</summary>
		/// <param name="source">A FormManagementHandle that is to be added to the collection.</param>
		/// <param name="withOpen">If set to TRUE, will cause the specified form to be opened (Shown) as well.</param>
		/// <returns>The same FormManagementHandle object that was submitted to the collection, or null.</returns>
		public dynamic AddHandle(dynamic source, bool withOpen = false)
		{
			if (source.GetType().Name.IndexOf("FormManagementHandle")==0)
			{
				dynamic fmht = FormManagementHandleCollectionBase.CreateTrackerInstance(source.FormType);
				int i = this.FirstIndexOfType(source.FormType);
				if ((i < 0) || fmht.AllowMultiples)
				{
					fmht.AddInstance(source);
					this._activeTrackers.Add(fmht);
				}
				else
				{
					this[i].AddInstance(source);
					fmht = this[i];
				}

				if (withOpen) { fmht.Form.Show(); source.Form.Activate(); }
				return fmht;
			}
			throw (ArgumentException)FormManagementHandleCollectionBase.InvalidType(source.GetType());
		}

		/// <summary>Returns a FormManagementTracker&lt;T&gt; object with the requested TrackerId.</summary>
		/// <param name="trackerId">A string specifying the TrackerId to search for.</param>
		/// <returns>The matching FormManagementTracker&lt;T&gt; if one is found, otherwise NULL.</returns>
		public dynamic GetTracker(string trackerId)
		{
			int i = this.IndexOfTracker(trackerId);
			return (i < 0) ? null : this[i];
		}

		/// <summary>Returns the first FormManagementTracker&lt;T&gt; object that manages the specified FormManagementObject class.</summary>
		/// <param name="formType">A FormManagementObject type that's being sought.</param>
		/// <returns>A FormManagementTracker&lt;T&gt;, if a Tracker of the specified FormManagementObject type is located, otherwise NULL.</returns>
		public dynamic GetTracker(Type formType)
		{
			int i = FirstIndexOfType(formType);
			return (i < 0) ? null : this[i];
		}

		/// <summary>Returns a FormManagementTracker&lt;T&gt; object that manages the requested FormManagementHandle.</summary>
		/// <param name="handle">A FormManagementHandleBase (or derivative) class specifying the Handle to search for.</param>
		/// <returns>The matching FormManagementTracker&lt;T&gt; if one is found, otherwise NULL.</returns>
		public dynamic GetTracker(FormManagementHandleBase handle)
		{
			dynamic[] trackers = this.Trackers(handle);
			return (trackers.Length > 0) ? trackers[0] : null;
		}

		/// <summary>Returns the first FormManagementTracker&lt;T&gt;, that manages the specified FormManagementObject type, from the known Trackers list.</summary>
		/// <typeparam name="T">A FormManagementObject derivative class.</typeparam>
		/// <returns>The first FormManagementTracker&lt;T&gt; object in the known trackers list that manages the specified FormManagementObject type.</returns>
		public FormManagementHandleTracker<T> GetTracker<T>() where T : FormManagementObject
		{
			FormManagementHandleTracker<T>[] trackers = Trackers<T>();
			return (trackers.Length > 0) ? trackers[0] : null;
		}

		/// <summary>Facilitates adding a foreign FormManagementTracker&lt;T&gt; object to the known ActiveTrackers list.</summary>
		/// <param name="newTracker">The FormManagementTracker&lt;T&gt; to integrate into the current Trackers list.</param>
		protected void AddTracker(dynamic newTracker)
		{
			if (newTracker.GetType().Name.IndexOf("FormManagementTracker") == 0)
			{
				dynamic fmo = newTracker.Form;
				if (newTracker.AllowMultiples || !HasType(fmo.GetType().Name))
					this._activeTrackers.Add(newTracker);
				else
				{
					int i = FirstIndexOfType(fmo.GetType().Name);
					this._activeTrackers[i].AddInstances(newTracker.ToArray());
				}
			}
			else
				throw (ArgumentException)FormManagementHandleCollectionBase.InvalidType(newTracker.GetType());
		}

		/// <summary>Forces the Closure of a specified tracker. This also results in the associated FormManagementObject being force-closed as well.</summary>
		/// <param name="trackerId">The TrackerId specifying which tracker to close.</param>
		/// <returns>TRUE if the specified Tracker was found and closed, otherwise FALSE.</returns>
		public bool CloseTracker(string trackerId)
		{
			int i = this.IndexOfTracker(trackerId);
			if (i > 0) this[i].Close();
			return (i > 0);
		}

		/// <summary>Closes all known Trackers of type &lt;T&gt; from the list.</summary>
		/// <typeparam name="T">A FormManagementObject derivative class.</typeparam>
		/// <returns>TRUE if any trackers of the specified type were found and closed.</returns>
		public bool CloseTrackers<T>() where T : FormManagementObject
		{
			bool result = false;
			int i = 0;
			while (i < this.Count)
				if (this._activeTrackers[i] is FormManagementHandleTracker<T>)
				{
					this._activeTrackers[i].Close();
					this._activeTrackers.RemoveAt(i);
					result = true;
				}
				else
					i++;

			return result;
		}

		/// <summary>Collates an array of open FormManagementTracking&lt;T&gt; classes from the list of known trackers.</summary>
		/// <typeparam name="T">A FormManagementObject derivative class.</typeparam>
		/// <returns>An array of open FormManagementTracking&lt;T&gt; classes.</returns>
		public T[] GetOpenForms<T>() where T : FormManagementObject
		{
			List<T> work = new List<T>();
			foreach (FormManagementHandleTracker<T> tracker in this.Trackers<T>())
				if ((tracker is T) && tracker.IsOpen)
					work.Add(tracker.Form);

			return work.ToArray();
		}

		/// <summary>Collates an array of open FormManagementTracking&lt;T&gt; classes from the list of known trackers.</summary>
		/// <returns>An array of open FormManagementTracking&lt;T&gt; classes.</returns>
		public dynamic[] GetOpenForms()
		{
			List<dynamic> trackers = new List<dynamic>();

			foreach (dynamic tracker in this._activeTrackers)
				if (tracker.IsOpen)
					trackers.Add(tracker);

			return trackers.ToArray();
		}

		/// <summary>Collates an array of open FormManagementTracking&lt;T&gt; classes from the list of known trackers.</summary>
		/// <param name="typeName">A string specifying the name of the FormManagementObject (or derivative) class to collect.</param>
		/// <returns>An array of open FormManagementTracking&lt;T&gt; classes.</returns>
		public dynamic[] GetOpenForms(string typeName)
		{
			List<dynamic> trackers = new List<dynamic>();

			foreach (dynamic tracker in this._activeTrackers)
				if (tracker.IsOpen && tracker.Form.GetType().Name.Equals(typeName,StringComparison.InvariantCultureIgnoreCase))
					trackers.Add(tracker);

			return trackers.ToArray();
		}

		/// <summary>Collates an array of FormManagementObjects from the list of known trackers by their Type name.</summary>
		/// <param name="formType">A Type value specifying the FormManagementObject (or derivative) class to collect.</param>
		/// <returns>An array of FormManagementObjects whose managed type name matches the specified string.</returns>
		public dynamic[] GetOpenForms(Type formType)
			=> GetOpenForms(formType.Name);

		/// <summary>
		/// Removes a specific instance of a FormManagementHandle from the collection and, if it is the last known instance of that
		/// form, also removes the tracker for that form from the known trackers list.
		/// </summary>
		/// <param name="handle"></param>
		/// <returns></returns>
		public bool CloseInstance(FormManagementHandleBase handle) =>
			CloseInstance(handle.Id);

		/// <summary>
		/// Closes all instances of the specified FormManagementHandle in the known Trackers List. Any trackers having no remaining
		/// handles afterwards are also removed from the known trackers list.
		/// </summary>
		/// <param name="handleId">A string specifying the FormManagementHandle id to purge from the known Trackers.</param>
		/// <returns>TRUE if <b>any</b> instances were found and removed, otherwise FALSE.</returns>
		public bool CloseInstance(string handleId)
		{
			int[] indexes = this.IndexOfHandle(handleId);
			bool result = true;
			if (indexes[0] >= 0)
				foreach (int i in indexes)
				{
					result &= this[i].RemoveInstance(handleId);

					if (this[i].Count == 0) // If all instances are removed, remove the form from the library.
						this._activeTrackers.RemoveAt(i);

					return result;
				}

			return false;
		}
		#endregion

		#region Static Methods
		public static TypeInfo[] GetAllFormPrototypes()
		{
			// Get a collection of all Assemblies in the current collection.
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			List<TypeInfo> classes = new List<TypeInfo>();

			// Search through the list for the "Cobblestone" assembly.
			int i = -1; while ((++i < assemblies.Length) && (assemblies[i].FullName.Substring(0, 12) != "Cobblestone,")) ;
			if (i < assemblies.Length)
			{
				// We found it, now iterate through all of the classes it contains and retain any from the
				// "Cobblestone.Forms" namespace...
				Assembly cobblestone = assemblies[i];
				foreach (TypeInfo ti in cobblestone.DefinedTypes)
					if ((ti.Namespace == "Cobblestone.Forms") && !ti.IsAbstract && FormManagementObject.IsManagedForm(ti))
						classes.Add(ti);
			}
			// Send back whatever we found...
			return classes.ToArray();
		}

		/// <summary>Facilitates converting the name of a Form as a string into an actual C# Type.</summary>
		/// <param name="name">A string containing the name of the form you want to convert to a Type.</param>
		/// <returns>A C# Type object referencing the class withe the specified name.</returns>
		public static Type GetTypeByName(string name)
		{
			System.Reflection.TypeInfo[] formTypes = GetAllFormPrototypes();
			int i = -1; while ((++i < formTypes.Length) && !name.Equals(formTypes[i].Name, StringComparison.InvariantCultureIgnoreCase)) ;
			return (i < formTypes.Length) ? (formTypes[i] as Type) : null;
		}
		#endregion
	}
	#endregion
#else
	#region FormManagement Objects without using Generics
	internal class FormManagementHandle
	{
		protected Guid _myGuid;
		protected bool _allowMultiples;
		protected Type _myFormType;

		#region Constructors
		public FormManagementHandle(Type formType) =>
			Initialise(formType, Guid.NewGuid());

		public FormManagementHandle(Type formType, Guid uid) =>
			Initialise(formType, uid);

		private void Initialise(Type formType, Guid uid)
		{
			if (!FormManagementObject.IsManagedForm(formType))
				throw FormManagementObject.InvalidType(formType);

			this._myGuid = uid;
			this._myFormType = formType;
			this._allowMultiples = FormManagementObject.AllowMultipleInstances(formType);
		}
		#endregion

		#region Operators
		public static bool operator ==(FormManagementHandle left, Guid right)
		{
			if (left is null) return (right == Guid.Empty);
			return (left._myGuid == right);
		}

		public static bool operator ==(FormManagementHandle left, string right)
		{
			if (left is null) return ((right is null) || (right == string.Empty) || (right == "") || (right.Length == 0));
			if ((right is null) || (right == string.Empty) || (right == "") || (right.Length == 0)) return false;
			return ((left._myGuid.ToString() == right) || left.FormName.Equals(right, StringComparison.InvariantCultureIgnoreCase));
		}

		public static bool operator ==(FormManagementHandle left, Type right)
		{
			if (left is null) return (right is null);
			if (right is null) return false;
			return (left.FormName == right.Name);
		}

		public static bool operator ==(FormManagementHandle left, FormManagementHandle right)
		{
			if (left is null) return (right is null);
			if (right is null) return false;
			return ((left.FormName == right.FormName) && (left.Id == right.Id));
		}

		public static bool operator !=(FormManagementHandle left, Guid right) => !(left == right);

		public static bool operator !=(FormManagementHandle left, string right) => !(left == right);

		public static bool operator !=(FormManagementHandle left, Type right) => !(left == right);

		public static bool operator !=(FormManagementHandle left, FormManagementHandle right) => !(left == right);
		#endregion

		#region Accessors
		public string Id =>
			this.ToString();

		public dynamic Form
		{
			get
			{
				if (Common.Forms is null)
					throw new ArgumentOutOfRangeException("Error: The Common.Forms collection has not been instantiated.");

				dynamic form = Common.Forms.GetTracker(this).Form;

				if (form is null)
					throw new ArgumentOutOfRangeException("Error: There is no active instance of this form in the library.");

				return form;
			}
		}

		public bool AllowMultiples =>
			this._allowMultiples;

		public Type FormType =>
			this._myFormType;

		public string FormName =>
			this._myFormType.Name;

		public FormWindowState WindowState =>
			(this.Form as FormManagementObject).WindowState;
		#endregion

		#region Methods
		public override string ToString() =>
			this._myGuid.ToString();

		protected T Convert<T>(object input) where T : FormManagementObject =>
			(T)System.Convert.ChangeType(input, typeof(T));

		public T Instantiate<T>() where T : FormManagementObject
		{
			T form = this.Convert<T>(Activator.CreateInstance(typeof(T))); //, typeof(T)));
			string name = FormManagementHandle.CreateName(typeof(T), (form as FormManagementObject).AllowMultiples);
			(form as FormManagementObject).Name = name;
			return form;
		}

		public dynamic CreateTracker()
		{
			Type formManagementHandleTrackerType = typeof(FormManagementTracker<>);
			Type newTrackerType = formManagementHandleTrackerType.MakeGenericType(this._myFormType);
			dynamic result = Activator.CreateInstance(newTrackerType);
			result.AddHandle(this);
			return result;
		}

		public FormManagementHandleCollection CreateCollection() =>
			new FormManagementHandleCollection(this);

		public override bool Equals(object obj) =>
			base.Equals(obj);

		public override int GetHashCode() =>
			base.GetHashCode();
		#endregion

		#region Static Methods
		public static string CreateName(Type type, bool randomize)
		{
			string name = type.Name;
			if (randomize)
			{
				string unixTimestamp = DateTime.Now.UnixTimeStamp().ToString();
				string ms = DateTime.Now.Millisecond.ToString();
				name += "(" + unixTimestamp.PadLeft(16, '0') + ms.PadLeft(4, '0') + ")";
			}
			return name;
		}

		/// <summary>Creates a new FormManagementTracker&lt;T&gt; class from the <i>name</i> of the desired FormManagementObject derivative class.</summary>
		/// <param name="typeName">A string specifying the name of the desired FormManagermentObject derivative to base the new Tracker on.</param>
		/// <returns>A new FormManagementTracker&lt;T&gt; object based on the specified FormManagementObject class's name.</returns>
		public static FormManagementHandle CreateHandleInstance(string typeName)
		{
			Type formManagementType = Type.GetType(typeName);
			return new FormManagementHandle(formManagementType);
		}

		/// <summary>Creates a new FormManagementTracker&lt;T&gt; class from the <i>type</i> of the desired FormManagementObject derivative class.</summary>
		/// <param name="formType">A Type object specifying the desired FormManagermentObject derivative to base the new Tracker on.</param>
		/// <returns>A new FormManagementTracker&lt;T&gt; object based on the specified FormManagementObject derivative class .</returns>
		public static FormManagementHandle CreateHandleInstance(Type formType) =>
			new FormManagementHandle(formType);
		#endregion
	}

	/// <summary>Provides a common "base" class for all derivative generic FormManagementHandleCollection<T> classes.</summary>
	internal class FormManagementHandleCollection : IEnumerator<FormManagementHandle>
	{
		private Type _myType = null;
		protected List<FormManagementHandle> _handles = new List<FormManagementHandle>();
		private int _position = 0;

		#region Constructors
		public FormManagementHandleCollection(Type formType)
		{
			if (!FormManagementObject.IsManagedForm(formType))
				throw FormManagementObject.InvalidType(formType);

			this._myType = formType;
		}

		public FormManagementHandleCollection(string containedTypeName)
		{
			if (!FormManagementObject.IsManagedForm(containedTypeName))
				throw FormManagementObject.InvalidType(containedTypeName);

			this._myType = FormManagementObject.ParseTypeName(containedTypeName);
		}

		public FormManagementHandleCollection(FormManagementHandle seed)
		{
			this._myType = seed.FormType;
			this._handles.Add(seed);
		}

		public FormManagementHandleCollection() { }
		#endregion

		#region Accessors
		public int Count =>
			(this._handles is null) ? 0 : this._handles.Count;

		public Type FormType
		{
			get => this._myType;
			protected set { if ((this._myType is null) && FormManagementObject.IsManagedForm(value)) { this._myType = value; } }
		}

		public FormManagementHandle[] Handles =>
			this._handles.ToArray();

		public FormManagementHandle this[int index]
		{
			get => ((index >= 0) && (index < this.Count)) ? this._handles[index] : null;
			set { if ((index >= 0) && (index < this.Count)) { this._handles[index] = value; } }
		}

		public FormManagementHandle this[string uid]
		{
			get
			{
				int i = this.IndexOf(uid);
				return (i < 0) ? null : this._handles[i];
			}
		}

		FormManagementHandle IEnumerator<FormManagementHandle>.Current =>
			this[this._position];

		object IEnumerator.Current =>
			this[this._position];
		#endregion

		#region Methods
		protected int IndexOf(string uid)
		{
			uid = uid.Trim();
			if (string.IsNullOrWhiteSpace(uid) || (uid.Length == 0)) return -1;

			int i = -1; while ((++i < _handles.Count) && !_handles[i].Id.Equals(uid, StringComparison.InvariantCultureIgnoreCase)) ;
			return (i < this.Count) ? i : -1;
		}

		protected int IndexOf(FormManagementHandle handle)
		{
			int i = -1; while ((++i < this.Count) && (this._handles[i] != handle)) ;
			return (i < this.Count) ? i : -1;
		}

		public bool HasHandle(FormManagementHandle handle) =>
			(this.IndexOf(handle) >= 0);

		public bool HasHandle(Guid uid) =>
			(this.IndexOf(uid.ToString()) >= 0);

		public bool HasHandle(string uid) =>
			(this.IndexOf(uid) >= 0);

		public bool AddHandle(FormManagementHandle handle)
		{
			if (!(handle is null) && (handle == this._myType))
			{
				if ((this.Count == 0) || !this.HasHandle(handle))
				{
					this._handles.Add(handle);
					return true;
				}
			}
			return false;
		}

		public bool AddHandles(FormManagementHandle[] handles)
		{
			bool result = true;
			if (handles.Length > 0)
				foreach (FormManagementHandle handle in handles)
					result &= this.AddHandle(handle);

			return result;
		}

		public bool AddHandles(FormManagementHandleCollection handles) =>
			AddHandles(handles.ToArray());

		public bool RemoveHandle(FormManagementHandle handle)
		{
			if (handle is null) return false;

			int i = IndexOf(handle);
			if (i >= 0)
				this._handles.RemoveAt(i);

			return (i >= 0);
		}

		public bool RemoveHandle(string uid)
		{
			int i = IndexOf(uid);
			if (i >= 0)
				this._handles.RemoveAt(i);

			return (i >= 0);
		}

		public dynamic CreateTracker()
		{
			dynamic tracker = CreateTrackerInstance(this._myType);
			tracker.AddHandles(this);
			return tracker;
		}

		public FormManagementHandle[] ToArray() =>
			(this._handles is null) ? new FormManagementHandle[]{} : this._handles.ToArray();

		//IEnumerator Support
		public IEnumerator<FormManagementHandle> GetEnumerator() =>
			new List<FormManagementHandle>().GetEnumerator();

		bool IEnumerator.MoveNext() => (++this._position) < this._handles.Count;

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
		#endregion

		#region Static Methods
		public static bool IsHandleCollection(object obj) =>
			IsHandleCollectionForm(obj.GetType());

		public static bool IsHandleCollectionForm(Type test) =>
			(test == typeof(Object)) ? false : ((test == typeof(FormManagementHandleCollection)) || IsHandleCollectionForm(test.BaseType));

		public static ArgumentException InvalidType(object obj) =>
			InvalidType(obj.GetType().Name);

		public static ArgumentException InvalidType(Type type) =>
			InvalidType(type.Name);

		public static ArgumentException InvalidType(string name) =>
			new ArgumentException("The specified source object is not a valid FormManagementHandle type (\"" + name + "\")");

		/// <summary>Creates a new FormManagementTracker&lt;T&gt; class from the <i>name</i> of the desired FormManagementObject derivative class.</summary>
		/// <param name="typeName">A string specifying the name of the desired FormManagermentObject derivative to base the new Tracker on.</param>
		/// <returns>A new FormManagementTracker&lt;T&gt; object based on the specified FormManagementObject class's name.</returns>
		public static dynamic CreateTrackerInstance(string typeName)
		{
			if (!FormManagementObject.IsManagedForm(typeName))
				throw FormManagementObject.InvalidType(typeName);

			Type formManagementType = Type.GetType(typeName);
			return CreateTrackerInstance(formManagementType);
		}

		/// <summary>Creates a new FormManagementTracker&lt;T&gt; class from the <i>type</i> of the desired FormManagementObject derivative class.</summary>
		/// <param name="formType">A Type object specifying the desired FormManagermentObject derivative to base the new Tracker on.</param>
		/// <returns>A new FormManagementTracker&lt;T&gt; object based on the specified FormManagementObject derivative class .</returns>
		public static dynamic CreateTrackerInstance(Type formType)
		{
			if (!FormManagementObject.IsManagedForm(formType))
				throw FormManagementObject.InvalidType(formType);

			Type formManagementHandleTrackerType = typeof(FormManagementTracker<>);
			Type newTrackerType = formManagementHandleTrackerType.MakeGenericType(formType);
			dynamic result = Activator.CreateInstance(newTrackerType);
			return result;
		}
		#endregion
	}

	internal class FormManagementTracker<T> : FormManagementHandleCollection, IEnumerator<FormManagementHandle> where T : FormManagementObject, new()
	{
		private int _position = 0;
		protected T _myForm;

		#region Constructors
		public FormManagementTracker() : base()
		{
			this.FormType = typeof(T);
			this._myForm = new T();
		}

		public FormManagementTracker(FormManagementHandle handle) : base(typeof(T))
		{
			this._myForm = new T();
			this.AddHandle(handle);
		}

		public FormManagementTracker(FormManagementHandleCollection handles) : base(typeof(T))
		{
			this._myForm = new T();
			this.AddHandles(handles.ToArray());
		}
		#endregion

		#region Operators
		#endregion

		#region Accessors
		public T Form =>
			this._myForm;

		public string Name =>
			(this._myForm is null) ? "" : this._myForm.Name;

		public Type MyFormType =>
			typeof(T);

		public bool AllowMultiples =>
			Form.AllowMultiples;

		public string Id =>
			(this._myForm is null) ? "" : this._myForm.TrackerId;

		/// <summary>Reports TRUE if the form should be visible somewhere on the desktop, otherwise FALSE.</summary>
		public bool IsVisible =>
			!(_myForm is null) && (_myForm.WindowState != FormWindowState.Minimized) && _myForm.Visible && (_myForm.Width > 0) && (_myForm.Height > 0);

		FormManagementHandle IEnumerator<FormManagementHandle>.Current =>
			this[this._position];

		object IEnumerator.Current =>
			this[this._position];
		#endregion

		#region Methods
		public bool Activate(FormManagementHandle handle)
		{
			if (!this.HasHandle(handle))
				throw new ArgumentOutOfRangeException("The FormManagementHandle \"" + handle.Id + "\" doesn't exist in this collection.");

			return Activate();
		}

		public bool Activate()
		{
			if (this._myForm is null) return false;

			if (this._myForm.WindowState == FormWindowState.Minimized)
				this._myForm.WindowState = FormWindowState.Normal;

			this._myForm.Show();
			this._myForm.Activate();
			return true;
		}

		public bool Hide(FormManagementHandle handle)
		{
			if (!this.HasHandle(handle))
				throw new ArgumentOutOfRangeException("The FormManagementHandle \"" + handle.Id + "\" doesn't exist in this collection.");

			return Hide();
		}

		public bool Hide()
		{
			if (this._myForm is null) return false;

			if (this._myForm.WindowState != FormWindowState.Minimized)
				this._myForm.Hide();

			return true;
		}

		public void Close()
		{
			this._handles = null;
			this._myForm.Close(true);
			this._myForm = null;
		}

		//IEnumerator Support
		new public IEnumerator<FormManagementHandle> GetEnumerator() =>
			new List<FormManagementHandle>().GetEnumerator();

		bool IEnumerator.MoveNext() => (++this._position) < this._handles.Count;

		void IEnumerator.Reset() => this._position = 0;

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected override void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects).
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				this._handles = null;
				this._myForm.Close(true);
				this._myForm = null;

				base.Dispose(disposing);
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
		#endregion
	}

	internal class FormManagementQueue
	{
		protected List<dynamic> _trackers = new List<dynamic>();

		#region Constructors
		public FormManagementQueue() { }
		#endregion

		#region Accessors
		public int Count =>
			this._trackers.Count;

		public dynamic this[int index] =>
			((index>=0) && (index<Count)) ? this._trackers[index] : null;
		#endregion

		#region Methods
		protected int FindFirstTrackerByHandle(FormManagementHandle handle)
		{
			dynamic[] trackers = Trackers(handle.FormType);
			int i = -1;
			if (trackers.Length > 0)
				while ((++i < trackers.Length) && !trackers[i].HasHandle(handle)) ;

			return (i < trackers.Length) ? i : -1;
		}

		protected int[] FindTrackersByHandle(FormManagementHandle handle)
		{
			dynamic[] trackers = Trackers(handle.FormType);
			List<int> indexes = new List<int>();
			if (trackers.Length > 0)
				for (int i = 0; i < trackers.Length; i++)
					if (trackers[i].HasHandle(handle))
						indexes.Add(i);

			return indexes.ToArray();
		}

		protected int FindFirstTrackerByType(Type formType)
		{
			dynamic[] trackers = Trackers(formType);
			int i = -1;
			if (trackers.Length > 0)
				while ((++i < trackers.Length) && (trackers[i].FormType != formType)) ;

			return (i < trackers.Length) ? i : -1;
		}

		protected int[] FindTrackersByType(Type formType)
		{
			dynamic[] trackers = Trackers(formType);
			List<int> indexes = new List<int>();
			if (trackers.Length > 0)
				for (int i = 0; i < trackers.Length; i++)
					if (trackers[i].FormType == formType)
						indexes.Add(i);

			return indexes.ToArray();
		}

		protected int FindFirstTrackerById(string trackerId)
		{
			int i = -1;
			if (this._trackers.Count > 0)
				while ((++i < this._trackers.Count) && !this._trackers[i].Id.Equals(trackerId)) ;

			return (i < this._trackers.Count) ? i : -1;
		}

		public bool HasTracker(Type byType) =>
			(this.FindFirstTrackerByType(byType) >= 0);

		public bool HasTracker(FormManagementHandle handle) =>
			HasTracker(handle.FormType);

		public bool HasTracker(string trackerId) =>
			(this.FindFirstTrackerById(trackerId) >= 0);

		public dynamic Trackers(Type byType)
		{
			FormManagementObject.ValidateType(byType);

			List<dynamic> results = new List<dynamic>();
			foreach (dynamic tracker in this._trackers)
				if (tracker.FormType == byType)
					results.Add(tracker);

			return results.ToArray();
		}

		public FormManagementHandle AddHandle(Type byType, bool withOpen = false) =>
			AddHandle(new FormManagementHandle(byType), withOpen);

		public FormManagementHandle AddHandle(FormManagementHandle handle, bool withOpen = false)
		{
			if (handle is null) return null;
			dynamic tracker = handle.CreateTracker();

			if (handle.AllowMultiples || !HasTracker(handle))
				this._trackers.Add(tracker);
			else
			{
				int i = this.FindFirstTrackerByHandle(handle);
				this[i].AddHandle(handle);
				tracker = this[i];
			}

			if (withOpen)
				tracker.Form.Activate();

			return handle;
		}

		/// <summary>Activates a Form associated with the specified handle.</summary>
		/// <param name="handle">A FormManagement handle specifying the form to Activate.</param>
		/// <typeparam name="T">A FormManagementObject derivative class.</typeparam>
		/// <returns>TRUE if a form was found with a matching handle and was activated, otherwise FALSE.</returns>
		public bool ActivateForm(FormManagementHandle handle)
		{
			bool result = true;
			int[] indexes = FindTrackersByHandle(handle);
			foreach (int i in indexes)
				result &= this[i].Activate();

			return result;
		}

		/// <summary>Hides all Forms associated to the specified handle.</summary>
		/// <param name="handle">A FormManagement handle specifying the form to Hide.</param>
		/// <returns>TRUE if a form was found with a matching handle and was hidden, otherwise FALSE.</returns>
		public bool HideForm(FormManagementHandle handle)
		{
			bool result = true;
			int[] indexes = FindTrackersByHandle(handle);
			foreach (int i in indexes)
				result &= this[i].Hide();

			return result;
		}

		public bool RemoveHandle(FormManagementHandle handle)
		{
			int[] indexes = FindTrackersByHandle(handle);
			foreach (int i in indexes)
			{
				this[i].RemoveHandle(handle);
				if (this[i].Count == 0)
					this.CloseTracker(this[i].Id);
			}
			return (indexes.Length > 0);
		}

		/// <summary>Collates an array of open FormManagementTracking&lt;T&gt; classes from the list of known trackers.</summary>
		/// <returns>An array of open FormManagementTracking&lt;T&gt; classes.</returns>
		public dynamic[] GetOpenForms()
		{
			List<dynamic> trackers = new List<dynamic>();

			foreach (dynamic tracker in this._trackers)
				if (tracker.IsVisible)
					trackers.Add(tracker);

			return trackers.ToArray();
		}

		/// <summary>Collates an array of open FormManagementTracking&lt;T&gt; classes from the list of known trackers.</summary>
		/// <param name="typeName">A string specifying the name of the FormManagementObject (or derivative) class to collect.</param>
		/// <returns>An array of open FormManagementTracking&lt;T&gt; classes.</returns>
		public dynamic[] GetOpenForms(string typeName)
		{
			List<dynamic> trackers = new List<dynamic>();

			foreach (dynamic tracker in this._trackers)
				if (tracker.IsVisible && tracker.Form.GetType().Name.Equals(typeName, StringComparison.InvariantCultureIgnoreCase))
					trackers.Add(tracker);

			return trackers.ToArray();
		}

		/// <summary>Collates an array of FormManagementObjects from the list of known trackers by their Type name.</summary>
		/// <param name="formType">A Type value specifying the FormManagementObject (or derivative) class to collect.</param>
		/// <returns>An array of FormManagementObjects whose managed type name matches the specified string.</returns>
		public dynamic[] GetOpenForms(Type formType)
			=> GetOpenForms(formType.Name);

		protected bool CloseTracker(int index)
		{
			if ((index >= 0) && (index < Count))
			{
				this[index].Close();
				this._trackers.RemoveAt(index);
				return true;
			}
			return false;
		}

		public bool CloseTracker(string trackerId) =>
			CloseTracker(this.FindFirstTrackerById(trackerId));

		public void CloseTrackers(Type formType)
		{
			int[] indexes = FindTrackersByType(formType);
			foreach (int i in indexes)
				this.CloseTracker(i);
		}

		/// <summary>Returns a FormManagementTracker&lt;T&gt; object with the requested TrackerId.</summary>
		/// <param name="trackerId">A string specifying the TrackerId to search for.</param>
		/// <returns>The matching FormManagementTracker&lt;T&gt; if one is found, otherwise NULL.</returns>
		public dynamic GetTracker(string trackerId)
		{
			int i = this.FindFirstTrackerById(trackerId);
			return (i < 0) ? null : this[i];
		}

		public dynamic GetTracker(Type byType)
		{
			int i = this.FindFirstTrackerByType(byType);
			return (i < 0) ? null : this[i];
		}

		public dynamic GetTracker(FormManagementHandle byHandle)
		{
			int i = this.FindFirstTrackerByHandle(byHandle);
			return (i < 0) ? null : this[i];
		}

		public dynamic[] ToArray() =>
			this._trackers.ToArray();
		#endregion

		#region Static Methods
		public static TypeInfo[] GetAllFormPrototypes()
		{
			// Get a collection of all Assemblies in the current collection.
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			List<TypeInfo> classes = new List<TypeInfo>();

			// Search through the list for the "Cobblestone" assembly.
			int i = -1; while ((++i < assemblies.Length) && (assemblies[i].FullName.Substring(0, 12) != "Cobblestone,")) ;
			if (i < assemblies.Length)
			{
				// We found it, now iterate through all of the classes it contains and retain any from the
				// "Cobblestone.Forms" namespace...
				Assembly cobblestone = assemblies[i];
				foreach (TypeInfo ti in cobblestone.DefinedTypes)
					if ((ti.Namespace == "Cobblestone.Forms") && !ti.IsAbstract && FormManagementObject.IsManagedForm(ti))
						classes.Add(ti);
			}
			// Send back whatever we found...
			return classes.ToArray();
		}

		/// <summary>Facilitates converting the name of a Form as a string into an actual C# Type.</summary>
		/// <param name="name">A string containing the name of the form you want to convert to a Type.</param>
		/// <returns>A C# Type object referencing the class withe the specified name.</returns>
		public static Type GetTypeByName(string name)
		{
			System.Reflection.TypeInfo[] formTypes = GetAllFormPrototypes();
			int i = -1; while ((++i < formTypes.Length) && !name.Equals(formTypes[i].Name, StringComparison.InvariantCultureIgnoreCase)) ;
			return (i < formTypes.Length) ? (formTypes[i] as Type) : null;
		}
		#endregion
	}
	#endregion
#endif
}
