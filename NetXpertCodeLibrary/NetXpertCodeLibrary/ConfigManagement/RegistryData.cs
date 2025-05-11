using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using NetXpertExtensions;
using StripOuterOptions = NetXpertExtensions.NetXpertExtensions.StripOuterOptions;
//using NetXpertCodeLibrary.ConsoleFunctions;

namespace NetXpertCodeLibrary.ConfigManagement
{
	/// <summary>Stores and manages a single Registry Value.</summary>
	public class RegistryValue
	{
		#region Properties
		protected RegistryHive _hive = RegistryHive.LocalMachine;
		protected RegistryView _view = RegistryView.Default;
		protected RegistryValueKind _type = RegistryValueKind.String;
		protected dynamic _value;
		protected string _name;
		protected string _subKey;
		#endregion

		#region Constructors
		public RegistryValue(string name, dynamic value, RegistryValueKind type = RegistryValueKind.String, RegistryHive hive = RegistryHive.LocalMachine, RegistryView view = RegistryView.Default)
		{
			this._type = type;
			this._name = name;
			this._value = value;
			this._hive = hive;
			this._view = view;
			this._subKey = "";
		}

		public RegistryValue(string name = "")
		{
			this._type = RegistryValueKind.None;
			this._name = name;
			this._hive = RegistryHive.LocalMachine;
			this._view = RegistryView.Default;
			this._subKey = "";
			this._value = null;
		}
		#endregion

		#region Accessors
		public RegistryValueKind Kind
		{
			get => this._type;
			set => this._type = (this._type == RegistryValueKind.None) ? value : this._type;
		}

		public string Name
		{
			get => this._name;
			set => this._name = value; // TODO: Validity checking this value!
		}

		public dynamic Value
		{
			get => this._value;
			set => this._value = value;
		}

		public string SubKey
		{
			get => this._subKey;
			set => this._subKey = value;
		}

		public RegistryHive Hive
		{
			get => this._hive;
			set => this._hive = value;
		}

		public RegistryView View
		{
			get => this._view;
			set => this._view = value;
		}
		#endregion

		#region Methods
		public void Fetch()
		{
			if (RegMgmt.ValueExists(this.Name, this.SubKey, this.Hive, this.View))
			{
				this.Kind = RegMgmt.GetValueType(this.Name, this.SubKey, this.Hive, this.View);
				this.Value = RegMgmt.GetValue(this.Name, this.SubKey, this.Hive, this.View);
			}

		}

		/// <summary>If the key already exists, this attempts to update the registry using the settings of this object.</summary>
		/// <param name="subKey">Optional: if provided, allows changing the subkey target for the write, but the alternate key
		/// must also exist PRIOR to this call.</param>
		/// <returns>True if the operation was a success, otherwise False.</returns>
		public bool Write(string subKey = null)
		{
			if (subKey is null) subKey = this.SubKey;
			if (RegMgmt.KeyExists(subKey) && RegMgmt.ValueExists(this.Name))
			{
				this.SubKey = subKey;
				return RegMgmt.SetValue(this.Name, this.Value, subKey, this.Kind, this.Hive, this.View);
			}
			return false;
		}

		public override string ToString() => this.Name + " {" + this._type.ToString() + "} = \"" + this.Value.ToString() + "\";";
		#endregion

		#region Static Methods
		public static RegistryValue Fetch(string subKey, string name, RegistryHive hive = RegistryHive.LocalMachine, RegistryView view = RegistryView.Default)
		{
			RegistryValue result = new(name);
			if (RegMgmt.ValueExists(name, subKey, hive, view))
			{
				result.Hive = hive;
				result.View = view;
				result.Kind = RegMgmt.GetValueType(name, subKey, hive, view);
				result.Value = RegMgmt.GetValue(name, subKey, hive, view);
				result.SubKey = subKey;
				return result;
			}
			return null;
		}
		#endregion
	}

	/// <summary>Extends the RegistryValue class to add support for storing an entire registry subkey (+tree)</summary>
	public class RegistrySubKey : RegistryValue, IEnumerator<RegistryValue>
	{
		#region Properties
		protected RegistryEntryCollection _subKeys = new();
		private int _position = 0;
		#endregion

		#region Constructors
		public RegistrySubKey(string name, dynamic value, RegistryValueKind type = RegistryValueKind.String, RegistryHive hive = RegistryHive.LocalMachine, RegistryView view = RegistryView.Default) :
			base(name, null, type, hive, view)
		{
			this._value = value;
			this._subKeys = new RegistryEntryCollection();
		}

		public RegistrySubKey(string name) : base(name) { }
		#endregion

		#region Accessors
		public RegistryValue this[int index]
		{
			get => this._subKeys[index];
			set => this._subKeys[index] = value;
		}

		public RegistryValue this[string name]
		{
			get => name.Trim().Equals("default",StringComparison.OrdinalIgnoreCase) ? this.Default : this._subKeys[name];
			set
			{
				if (name.Trim().Equals("default", StringComparison.OrdinalIgnoreCase))
					this.Default = value.Value;
				else
					this._subKeys[name] = value;
			}
		}

		public RegistryValue Default
		{
			get
			{
				RegistryValue def = new("default");
				def.Value = RegMgmt.GetDefaultValue(this._subKey, this.Hive, this.View);
				def.Kind = RegMgmt.GetDefaultValueKind(this._subKey, this.Hive, this.View);
				return def;
			}
			set => RegMgmt.SetValue("", value.Value, this.SubKey);
		}

		public int Count => this._subKeys.Count;

		public int KeyCount => this._subKeys.KeyCount;

		public int ValueCount => this._subKeys.ValueCount;

		public RegistryValue[] Keys => this._subKeys.Keys;

		public RegistryValue[] Values => this._subKeys.Values;

		new protected dynamic Value => Default.Value;

		new protected RegistryValueKind Kind => Default.Kind;

		// IEnumerator support
		RegistryValue IEnumerator<RegistryValue>.Current =>
			this[this._position];

		object IEnumerator.Current =>
			this[this._position];
		#endregion

		#region Methods
		public override string ToString() => this.Name + " {" + this._subKey + "}";

		public string ToString(int indent = 0, int increment = 5)
		{
			string response = "{".PadLeft(indent, ' ') + this._view.ToString() + "} [" + RegMgmt.RegistryHiveToAbbr(this._hive) + "::" + this._subKey + "]", values = "";
			indent += increment;
			if (this._subKeys.Count > 0)
			{
				foreach (RegistryValue key in this._subKeys)
					if (key.GetType() == typeof(RegistrySubKey))
						response += "\r\n" + (key as RegistrySubKey).ToString(indent);
					else
					{
						string value = key.Value.ToString();
						if ( value == "System.String[]" )
							value = new IniMultiString( (string[])key.Value );

						values += 
							"\r\n".PadRight(indent, ' ') +
							(key.Name + "<" + ((key.Value is null) ? "null" : key.Value.GetType().Name) + ">:").PadRight(25, ' ') +
							((key.Value is null) ? "" : value.Wrap(StripOuterOptions.DoubleQuotes)) + ";";
						// ArgData.QuoteWrap(value) ??
					}
			}
			else
				values = "\r\n".PadRight(indent, ' ') + "« No Values in this Key »";
			return response + values; // + "\r\n";
		}

		/// <summary>Can be used to instantiate a new RegistrySubKey object directly from a specified Registry Key</summary>
		/// <param name="rootPath">The Path and name of the key to obtain.</param>
		/// <param name="hive">Which Hive to use.</param>
		/// <param name="view">Which View to use.</param>
		/// <returns>
		/// If the specified key exists and the user has access to it, a new RegistrySubKey object containing its contents
		/// otherwise NULL.
		/// </returns>
		//public static RegistrySubKey Fetch(string rootPath, RegistryHive? hive = null, RegistryView? view = null)
		//{
		//	RegistryHive h = (hive is null) ? RegistryHive.LocalMachine : (RegistryHive)hive;
		//	RegistryView v = (view is null) ? RegistryView.Default : (RegistryView)view;

		//	RegistrySubKey subkey = (RegistrySubKey)RegistrySubKey.Fetch(rootPath, h, v);
		//	foreach (string s in RegMgmt.GetSubKeyNames(rootPath, h, v))
		//		subkey._subKeys.Add(Fetch(rootPath + "\\" + s, h, v));

		//	if (RegMgmt.ValueCount(rootPath, hive, view) > 0)
		//	{
		//		RegistryKey key = RegMgmt.FetchKey(rootPath, hive, view);
		//		foreach (string s in key.GetValueNames())
		//			subkey._subKeys.Add(RegistryValue.Fetch(rootPath, s, h, v));
		//	}
		//	return subkey;
		//}

		/// <summary>Endeavours to create a ConfigIniFile object from a specified registry key.</summary>
		/// <param name="fileName">A string specifying the filename to create.</param>
		/// <param name="readOnly">If TRUE, sets the resulting IniFile object to be read-only.</param>
		public IniFile ToIniFile(string fileName, bool readOnly = true)
		{
			IniFile result = new( fileName, new string[] { "ROOT:" + this._subKey } );

			IniGroupItem ParseSubKey( RegistryKey key )
			{
				IniGroupItem group = new( Regex.Replace( key.Name, @"^HKEY_[A-Z_]+\\", "" ).Replace( this._subKey, "" ), this.Hive );
				//RegistryKey _key = RegMgmt.FetchKey( key.SubKey, this._hive );
				foreach ( string valueName in key.GetValueNames() )
				{
					string v = "", 
						vN = Regex.Replace( valueName, @"(<[a-z0-9]>|[^\w])", "", RegexOptions.IgnoreCase );

					if ( key.GetValueKind( valueName ) == RegistryValueKind.MultiString )
					{
						v = "[ `" + string.Join( "`, `", (string[])key.GetValue( valueName ) ) + "` ]";
						vN += "<M>" + (valueName.EndsWith( "?" ) ? "?" : "");
						group.Add( new IniMultiStringItem( vN, v, valueName.EndsWith( "?" ) ) );
					}
					else
						group.Add( new IniLineItem( vN + (valueName.EndsWith( "?" ) ? "?" : ""), key.GetValue( valueName ).ToString(), valueName.EndsWith( "?" ) ) );
				}

				foreach ( string subKeyName in key.GetSubKeyNames() )
				{
					RegistryKey subKey = key.OpenSubKey( subKeyName );
					result.Add( ParseSubKey( subKey ) );
				}

				return group;
			}

			foreach ( RegistrySubKey key in this.Keys )
			{
				RegistryKey k = RegMgmt.FetchKey( key.SubKey, this.Hive );
				result.Add( ParseSubKey( k ) );
			}

			result.ReadOnly = readOnly;
			return result;
		}

		//IEnumerator Support
		public IEnumerator<RegistryValue> GetEnumerator() =>
		this._subKeys.GetEnumerator();

		bool IEnumerator.MoveNext() => (++this._position) < this.Count;

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
		public static RegistrySubKey Fetch(string subKey, RegistryHive hive = RegistryHive.LocalMachine, RegistryView view = RegistryView.Default)
		{
			if (RegMgmt.KeyExists(subKey, hive, false, view))
			{
				RegistryKey key = RegMgmt.FetchKey(subKey, hive, view);
				if (RegMgmt.LastError.Length > 0) throw new InvalidOperationException(RegMgmt.LastError);

				RegistrySubKey me = new(Path.GetFileName(key.Name));

				me.Default.Kind = RegMgmt.GetDefaultValueKind(subKey, hive, view);
				me.Default.Value = RegMgmt.GetDefaultValue(subKey, hive, view);
				me.SubKey = subKey;
				me.Hive = hive;
				me.View = view;

				if (key.SubKeyCount > 0)
					foreach (string keyName in key.GetSubKeyNames())
						me._subKeys.Add(Fetch(subKey + keyName, hive, view));

				if (key.ValueCount > 0)
					foreach (string valueName in key.GetValueNames())
						me._subKeys.Add(RegistryValue.Fetch(subKey, valueName, hive, view));

				return me;
			}
			return null;
		}

		public static bool CreateKeyValue(RegistryValue value, string subKey = null)
		{
			if (subKey is null) subKey = value.SubKey;
			if (RegMgmt.KeyExists( subKey, value.Hive, false, value.View ))
			{
				RegistryKey key = RegMgmt.FetchKey(subKey, value.Hive, value.View, true);
				if (!(key is null))
				{
					try { key.SetValue(value.Name, value.Value, value.Kind); }
					catch (Exception) { return false; }
					return true;
				}
			}
			return false;
		}

		public static bool CreateKeyValue(string subKey, string name, object value, RegistryValueKind kind = RegistryValueKind.String, RegistryHive hive = RegistryHive.LocalMachine, RegistryView view = RegistryView.Default) =>
			CreateKeyValue(new RegistryValue(name, value, kind, hive, view), subKey);
		#endregion
	}

	/// <summary>Stores and manages a collection of RegistryValue classes.</summary>
	public class RegistryEntryCollection : IEnumerator<RegistryValue>
	{
		#region Properties
		protected List<RegistryValue> _collection = new();
		private int _position = 0;
		#endregion

		#region Constructors
		public RegistryEntryCollection() { }

		public RegistryEntryCollection(RegistryValue[] values) => this.AddRange(values);

		public RegistryEntryCollection(RegistryEntryCollection values) => this.AddRange(values.ToArray());
		#endregion

		#region Accessors
		public RegistryValue this[int index]
		{
			get => this._collection[index];
			set => this._collection[index] = value;
		}

		public RegistryValue this[string name]
		{
			get
			{
				int i = IndexOf(name);
				return (i < 0) ? null : this[i];
			}
			set
			{
				int i = IndexOf(name);
				if (i >= 0) this[i] = value;
			}
		}

		public int Count => this._collection.Count;

		public RegistryValue[] Values
		{
			get
			{
				RegistryEntryCollection results = new();
				foreach (dynamic p in this)
					if (p.GetType() == typeof(RegistryValue))
						results.Add(p);
				return results.ToArray();
			}
		}

		public int KeyCount => Keys.Length;

		public RegistryValue[] Keys
		{
			get
			{
				RegistryEntryCollection results = new();
				foreach (dynamic p in this)
					if (p.GetType() == typeof(RegistrySubKey))
						results.Add(p);
				return results.ToArray();
			}
		}

		public int ValueCount => Values.Length;

		// IEnumerator support
		RegistryValue IEnumerator<RegistryValue>.Current =>
			this[this._position];

		object IEnumerator.Current =>
			this[this._position];
		#endregion

		#region Methods
		protected int IndexOf(string name)
		{
			int i = -1; while ((++i < Count) && !this._collection[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ;
			return (i < Count) ? i : -1;
		}

		public void Add(RegistryValue source) =>
			this._collection.Add(source);

		public void AddRange(RegistryValue[] source)
		{
			foreach (RegistryValue v in source)
				this.Add(v);
		}

		public void AddRange(RegistryEntryCollection source) =>
			this.AddRange(source.ToArray());

		public bool Remove(string name)
		{
			int i = IndexOf(name);
			if (i>=0)
			{
				this._collection.RemoveAt(i);
				return true;
			}
			return false;
		}

		public RegistryValue[] ToArray() => this._collection.ToArray();

		//IEnumerator Support
		public IEnumerator<RegistryValue> GetEnumerator() =>
			this._collection.GetEnumerator();

		bool IEnumerator.MoveNext() => (++this._position) < this.Count;

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
	}

	/// <summary>Stores / Manages a single registry key value.</summary>
	/// <remarks>This isn't used anywhere, it's just preliminary work on a grand registry-as file-tree management system.</remarks>
	public class RegistryValueEntry
	{
		#region Properties
		protected string _name = "";
		protected RegistryValueKind _kind = RegistryValueKind.Unknown;
		protected string[] _value = new string[] { "" };
		#endregion

		#region Constructors
		public RegistryValueEntry() { }

		public RegistryValueEntry( string name, string value, RegistryValueKind kind = RegistryValueKind.String )
		{
			this.Name = name;
			this._value[0] = value;
			this._kind = kind;
		}

		public RegistryValueEntry( string name, object value, RegistryValueKind kind)
		{
			this.Name = name;
			this._value[0] = value.ToString();
			this._kind = kind;
		}

		public RegistryValueEntry( string name, string[] values )
		{
			this.Name = name;
			this._value = values;
			this._kind = RegistryValueKind.MultiString;
		}
		#endregion

		#region Accessors
		public string Name
		{
			get => this._name;
			set
			{
				if ( !string.IsNullOrWhiteSpace( value ) && Regex.IsMatch( value, @"^[a-zA-Z][\w]*$" ) )
					this._name = value;
			}
		}

		public string Value
		{
			get => _value[0];
			set => _value = new string[] { value };
		}

		/// <summary>Reports the C# equivalent Type for the defined _kind of this object.</summary>
		public Type NativeType => ConvertType( _kind );

		public dynamic DataAsNativeType
		{
			get =>
				_kind switch
				{
					RegistryValueKind.MultiString => _value,
					RegistryValueKind.DWord => UInt32.Parse( Value ),
					RegistryValueKind.QWord => UInt64.Parse( Value ),
					_ => Value,
				};
		}
		#endregion

		#region Methods
		public static Type ConvertType( RegistryValueKind regKind ) =>
			regKind switch
			{
				RegistryValueKind.MultiString => typeof( string[ ] ),
				RegistryValueKind.DWord => typeof( UInt32 ),
				RegistryValueKind.QWord => typeof( UInt64 ),
				_ => typeof(string)
			};

		//public static Type ConvertType( RegistryValueKind regKind )
		//{ 
		//	switch (regKind)
		//	{
		//		case RegistryValueKind.MultiString:
		//			return typeof( string[] );
		//		case RegistryValueKind.DWord:
		//			return typeof( UInt32 );
		//		case RegistryValueKind.QWord:
		//			return typeof( UInt64 );
		//		case RegistryValueKind.Unknown:
		//		case RegistryValueKind.String:
		//		case RegistryValueKind.ExpandString:
		//		default:
		//			return typeof( string );
		//	}
		//}
		#endregion
	}
}