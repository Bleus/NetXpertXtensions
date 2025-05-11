//using NetXpertCodeLibrary.ConsoleFunctions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace NetXpertCodeLibrary.WinForms
{
	public class ThreadedHandle
	{
		#region Properties
		protected Guid _uid;
		protected Type _type;
		#endregion

		#region Constructors
		public ThreadedHandle(Type formType)
		{
			if (!IsThreadedForm(formType))
				throw new ArgumentException("The provided Type for this handle (\"" + formType.Name + "\") isn't derived from `ThreadedFormBase`.");

			this._type = formType;
			this._uid = new Guid();
		}
		#endregion

		#region Operators
		public static bool operator !=(ThreadedHandle left, ThreadedHandle right) => !(left == right);
		public static bool operator ==(ThreadedHandle left, ThreadedHandle right)
		{
			if (left is null) return (right is null);
			if (right is null) return false;
			if (left.AllowMultipleInstances && right.AllowMultipleInstances)
				return left._type == right._type;

			return (left._type == right._type) && (left._uid == right._uid);
		}

		public static bool operator !=(ThreadedHandle left, Type right) => !(left == right);
		public static bool operator ==(ThreadedHandle left, Type right)
		{
			if (left is null) return (right is null);
			if (right is null) return false;
			return left.AllowMultipleInstances && (left._type == right);
		}
		#endregion

		#region Accessors
		public string Uid
		{
			get => this._uid.ToString();
			protected set => this._uid = Guid.Parse(value);
		}

		public bool AllowMultipleInstances
		{
			get
			{
				ThreadedFormBase form = this.CreateFormInstance();
				return form.AllowMultipleInstances;
			}
		}

		public Type ManagedType => this._type;
		#endregion

		#region Methods
		public override bool Equals(object obj) => base.Equals(obj);

		public override int GetHashCode() => base.GetHashCode();

		public dynamic CreateFormInstance()
		{
			dynamic result = Activator.CreateInstance(this._type);
			(result as ThreadedFormBase).ThreadedHandle.Uid = this.Uid;
			return result;
		}
		#endregion

		#region Static Methods
		public static bool IsThreadedForm(dynamic test) => IsThreadedForm(test.GetType());

		public static bool IsThreadedForm(Type test) =>
			(test.BaseType == typeof(Object)) ? false : (test.BaseType == typeof(ThreadedFormBase) || IsThreadedForm(test.BaseType));
		#endregion
	}

	[TypeDescriptionProvider(typeof(VS_AbstractionOverride.AbstractControlDescriptionProvider<ThreadedFormBase, Form>))]
	public abstract class ThreadedFormBase : Form
	{
		#region Properties
		protected ThreadedHandle _handle;
		#endregion

		#region Constructors
		/// <summary>The type value expected here is the Type of the descendent class that is calling this constructor.</summary>
		public ThreadedFormBase(Type parentType, bool allowMultipleInstances = true) : base() =>
			this._handle = new ThreadedHandle(parentType);
		#endregion

		#region Accessors
		public string Uid => this._handle.Uid;

		public ThreadedHandle ThreadedHandle => this._handle;

		public bool AllowMultipleInstances => AllowMultipleInstancesSetting();

		//public WindowInfo Info => new WindowInfo( this, this._handle );

		/// <summary>Provides easy access to the Screen information of whatever display the form is currently on.</summary>
		protected Screen Screen => Screen.FromHandle( this.Handle );
		#endregion

		#region Methods
		protected abstract bool AllowMultipleInstancesSetting();
		#endregion
	}

	public class ThreadedFormCollection : IEnumerator<ThreadedFormBase>
	{
		#region Properties
		protected List<ThreadedFormBase> _forms;
		private int _position = 0;
		#endregion

		#region Constructors
		public ThreadedFormCollection() => this._forms = new List<ThreadedFormBase>();
		#endregion

		#region Accessors
		public int Count => this._forms.Count;

		public ThreadedFormBase this[int index]
		{
			get => ((index >= 0) && (index < Count)) ? this._forms [index] : null;
			set { if ((index >= 0) && (index < Count)) { this[index] = value; } }
		}

		public ThreadedFormBase this[ThreadedHandle index]
		{
			get
			{
				int i = IndexOf(index);
				return (i < 0) ? null : this[i];
			}
		}

		public ThreadedFormBase this[string uid]
		{
			get
			{
				int i = IndexOf( uid );
				return (i < 0) ? null : _forms[ i ];
			}
		}

		ThreadedFormBase IEnumerator<ThreadedFormBase>.Current => this[this._position];

		object IEnumerator.Current => this._forms[this._position];
		#endregion

		#region Methods
		public int IndexOf(ThreadedHandle handle)
		{
			int i = -1; while ((++i < Count) && (this[i].ThreadedHandle != handle)) ;
			return (i < Count) ? i : -1;
		}

		public int IndexOf(string uid)
		{
			int i = -1; while ( (++i < Count) && !_forms[ i ].Uid.Equals( uid, StringComparison.OrdinalIgnoreCase ) ) ;
			return (i < Count) ? i : -1;
		}

		public bool Add(ThreadedFormBase form)
		{
			if (form.AllowMultipleInstances || (IndexOf(form.ThreadedHandle) < 0))
			{
				form.Closed += this.FormClosedNotifier;
				this._forms.Add(form);
				return true;
			}

			return false;
		}

		public bool Add(ThreadedHandle handle) =>
			this.Add( handle.CreateFormInstance() );

		public bool Add(Type formType)
		{
			if (ThreadedHandle.IsThreadedForm(formType))
			{
				ThreadedHandle handle = new ThreadedHandle(formType);
				return this.Add(handle.CreateFormInstance());
			}
			return false;
		}

		public bool Remove(ThreadedHandle handle)
		{
			int i = IndexOf(handle);
			if (i>=0)
			{
				ThreadedFormBase form = this._forms[ i ];
				this._forms.RemoveAt( i );
				form.Close();
				return true;
			}
			return false;
		}

		public bool RemoveAt(int i)
		{
			if ((i >= 0) && (i < Count))
			{
				this._forms.RemoveAt(i);
				return true;
			}
			return false;
		}

		public bool HasForm(ThreadedHandle handle) => (IndexOf(handle) >= 0);

		public void Show(ThreadedHandle handle)
		{
			int i = IndexOf(handle);
			if (i >= 0) this[i].Show();
		}

		public void Show(ThreadedHandle handle, IWin32Window parent)
		{
			int i = IndexOf(handle);
			if (i >= 0) this[i].Show(parent);
		}

		public DialogResult ShowDialog(ThreadedHandle handle)
		{
			int i = IndexOf(handle);
			return (i < 0) ? DialogResult.None : this[i].ShowDialog();
		}

		public DialogResult ShowDialog(ThreadedHandle handle, IWin32Window parentForm)
		{
			int i = IndexOf(handle);
			return (i < 0) ? DialogResult.None : this[i].ShowDialog(parentForm);
		}

		public void Hide(ThreadedHandle handle)
		{
			int i = IndexOf(handle);
			if (i >= 0) this[i].Hide();
		}

		private void FormClosedNotifier(object sender, EventArgs e)
		{
			ThreadedHandle handle = (sender as ThreadedFormBase).ThreadedHandle;
			int i = IndexOf(handle);
			if (i >= 0) this._forms.RemoveAt(i);
		}
		#endregion

		//IEnumerator Support
		public IEnumerator<ThreadedFormBase> GetEnumerator() => this._forms.GetEnumerator();

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

	public class VS_AbstractionOverride
	{
		public class AbstractControlDescriptionProvider<TAbstract, TBase> : TypeDescriptionProvider
		{
			public AbstractControlDescriptionProvider()
				: base(TypeDescriptor.GetProvider(typeof(TAbstract))) { }

			public override Type GetReflectionType(Type objectType, object instance)
			{
				if (objectType == typeof(TAbstract))
					return typeof(TBase);

				return base.GetReflectionType(objectType, instance);
			}

			public override object CreateInstance(IServiceProvider provider, Type objectType, Type[] argTypes, object[] args)
			{
				if (objectType == typeof(TAbstract))
					objectType = typeof(TBase);

				return base.CreateInstance(provider, objectType, argTypes, args);
			}
		}
	}
}
