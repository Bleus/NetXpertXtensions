using System;
using System.Drawing;
using System.Windows.Forms;
using NetXpertCodeLibrary.Extensions;
using NetXpertCodeLibrary.WinForms;

namespace NetXpertCodeLibrary.ConsoleFunctions
{
	public partial class BackgroundForm : Form
	{
		private ThreadedFormCollection _forms;

		#region Constructors and Destructor
		public BackgroundForm()
		{
			this._forms = new ThreadedFormCollection();
			InitializeComponent();
			this.Move += this.BackgroundForm_Move;
			this.VisibleChanged += this.BackgroundForm_VisibleChanged;
			//this.FormClosing += this.BackgroundForm_FormClosing;
		}

		// Destructor - ensure that all open forms are closed-out before allowing this to be closed.
		~BackgroundForm() { if ((this._forms != null) && (this._forms.Count > 0)) { this.Stop(); } }
		#endregion

		#region Methods
		public ThreadedFormBase GetFormByUid( string uid ) => _forms[ uid ];

		public ThreadedFormBase GetFormByHandle( ThreadedHandle handle ) => _forms[ handle ];

		private void BackgroundForm_FormClosing(object sender, FormClosingEventArgs e) => this.Stop();

		// Prevents being made visible.
		private void BackgroundForm_VisibleChanged(object sender, EventArgs e) => base.Visible = false;

		// Prevents form from being moved.
		private void BackgroundForm_Move(object sender, EventArgs e) => base.Location = new Point (0,0);

		public T Instantiate<T>() =>
			(T)(ThreadedHandle.IsThreadedForm(typeof(T)) ? Activator.CreateInstance(typeof(T)) : null);

		public dynamic Instantiate(ThreadedHandle handle) =>
			Instantiate(handle.ManagedType);

		public dynamic Instantiate(Type type)
		{
			if (!ThreadedHandle.IsThreadedForm(type))
				throw new ArgumentException("The provided type (\"" + type.Name + "\") is not derived from ThreadedFormBase.");

			return Convert.ChangeType(Activator.CreateInstance(type), type);
		}

		public dynamic Show(ThreadedFormBase form)
		{
			if (!_forms.HasForm(form.ThreadedHandle) || form.AllowMultipleInstances)
			{
				this._forms.Add(form);
				form.Show();
				return form;
			}
			return null;
		}

		public dynamic Show(ThreadedHandle handle)
		{
			dynamic form = _forms.HasForm(handle) ? _forms[handle] : null;
			if (form is null)
			{
				form = Instantiate(handle.ManagedType);
				this._forms.Add(form);
			}

			form.Show();
			return form;
		}

		public dynamic Show(Type t)
		{
			if (!ThreadedHandle.IsThreadedForm(t))
				throw new ArgumentException("The provided type (\"" + t.Name + "\") is not derived from ThreadedFormBase.");

			return Show(new ThreadedHandle(t));
		}

		public bool Close(ThreadedHandle handle) =>
			this._forms.Remove(handle);

		public ThreadedHandle FindFormHandle<T>() where T : ThreadedFormBase
		{
			int i = -1; while ((++i < _forms.Count) && (_forms[ i ].GetType() != typeof( T ))) ;
			return (i < _forms.Count) ? _forms[ i ].ThreadedHandle : null;
		}

		public void Stop()
		{
			// Forcibly close all open forms, and remove them from the Queue...
			while (_forms.Count > 0) { _forms[0].Close(); _forms.RemoveAt(0); }

			Application.ExitThread();
			this.Invoke((MethodInvoker)delegate
			{
				// close the form on the forms thread
				this.Close();
			});
		}

		public void ModifyAttribute(string uid, string attributeName, object value = null)
		{
			if ( !string.IsNullOrWhiteSpace( uid ) )
			{
				dynamic form = GetFormByUid( uid );
				if ( !(form is null) )
					switch ( attributeName.ToLowerInvariant() )
					{
						case "windowstate":
							form.WindowState = (FormWindowState)value;
							return;
						case "unminimize()":
						case "restore()":
							form.RestoreMinimized();
							return;
						case "location":
							form.Location = (Point)value;
							return;
						case "size":
							form.Size = (Size)value;
							return;
						case "text":
						case "title":
							form.Text = value.ToString();
							return;
						case "hide()":
							form.Hide();
							return;
						case "visible":
							form.Visible = (bool)value;
							return;
						case "show()":
							form.Show();
							return;
					}
			}

			throw new Exception( "Unrecognized Background Form Attribute Requested (\"" + attributeName + "\")." );
		}

		public object GetAttribute(string uid, string attributeName)
		{
			if ( !string.IsNullOrWhiteSpace( uid ) )
			{
				dynamic form = GetFormByUid( uid );
				switch ( attributeName.ToLowerInvariant() )
				{
					case "windowstate":
						return (FormWindowState)form.WindowState;
					case "location":
						return (Point2)form.Location;
					case "size":
						return (Size)form.Size;
					case "title":
					case "text":
						return (string)form.Text;
					case "visible":
						return (bool)form.Visible;
				}
			}

			throw new Exception( "Unrecognized Background Form Attribute Requested (\"" + attributeName + "\")." );
		}

		public object GetAttribute( ThreadedHandle handle, string attributeName ) =>
			GetAttribute( handle.Uid, attributeName );

		public void ModifyAttribute( ThreadedHandle handle, string attributeName, object value = null ) =>
			ModifyAttribute( handle.Uid, attributeName, value );
		#endregion

		#region Hidden Functions / Accessors
		new protected void Show() => base.Show();

		new protected DialogResult ShowDialog(IWin32Window owner = null) => base.ShowDialog(owner);

		new protected DialogResult ShowDialog() => base.ShowDialog();

		new protected bool Visible
		{
			get => base.Visible;
			set => base.Visible = false;
		}
		#endregion

		#region Overridden Accessors
		new FormWindowState WindowState
		{
			get => base.WindowState;
			set => base.WindowState = FormWindowState.Normal;
		}

		new Point Location
		{
			get => base.Location;
			set => base.Location = new Point(0, 0);
		}

		new Size Size
		{
			get => base.Size;
			set => base.Size = new Size(256, 64);
		}

		new Size MinimumSize
		{
			get => base.MinimumSize;
			set => base.MinimumSize = new Size(256, 64);
		}

		new Size MaximumSize
		{
			get => base.MaximumSize;
			set => base.MaximumSize = new Size(256, 64);
		}
		#endregion
	}
}
