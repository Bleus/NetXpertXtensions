using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace NetXpertCodeLibrary.ConfigManagement
{
	/// <summary>Manages a Registry Subkey/Value tree as an IniFile.</summary>
	/// <remarks>
	/// Typical expected schema for configuration items...
	/// [HKLM]
	///		Software										-- RegistryKey
	///			NetXpert
	///				{Application.ProductName}				-- SubKey1
	///					{GroupName1}						-- SubKey1.1
	///						{Value1«string»} = {Data}		-- SubKey1.1-Value1
	///						{Value2«string»} = {Data}		-- SubKey1.1-Value2
	///						{Value3«string»} = {Data}		-- SubKey1.1-Value3
	///							:
	///					{GroupName2}						-- SubKey1.2
	///						{Value1«string»} = {Data}
	///						{Value2«string»} = {Data}
	///						{Value3«string»} = {Data}
	///							:
	/// </remarks>
	public class IniRegistryFile : IniFileBase, IEnumerator<IniGroupItem>
	{
		protected RegistryHive _defaultRegistryHive = RegMgmt.DEFAULT_REGISTRY_HIVE;

		protected RegistryView _defaultRegistryView = RegMgmt.DEFAULT_REGISTRY_VIEW;

		protected string _defaultSubKey = RegMgmt.DEFAULT_SUBKEY;

		private int _position = 0;

		public IniRegistryFile() : base() { }

		public IniRegistryFile(RegistryHive defaultRegistryHive, RegistryView defaultRegistryView, string defaultSubKey) : base()
		{
			this._defaultRegistryHive = defaultRegistryHive;
			this._defaultRegistryView = defaultRegistryView;
			this._defaultSubKey = defaultSubKey.Trim();
		}

		public IniRegistryFile(string subKey, RegistryHive hive = RegistryHive.LocalMachine, RegistryView view = RegistryView.Default) : base()
		{
			if ((subKey is null) || (subKey.Trim().Length == 0))
				throw new ArgumentException("The provided 'subKey' value cannot be null, empty, or whitespace.");

			this.Load(subKey, hive, view);
		}

		public override bool Load(string fileName = "") => 
			Load((fileName == "") ? this._defaultSubKey : fileName);

		public override string ToString() => "";

		public bool Load(string subKey, RegistryHive? hive = null, RegistryView? view = null)
		{
			// Obtain a list of subkeys (Groups) under the root subkey...
			string[] keys = RegMgmt.GetSubKeyNames(subKey, Convert(hive), Convert(view));
			foreach (string key in keys)
			{
				string keyName = subKey + (subKey.EndsWith("\\") ? "" : "\\") + key;
				RegistryKey group = RegMgmt.FetchKey(keyName, hive, view);
				if (!(group is null))
				{
					string hiveAbbr = RegMgmt.RegistryHiveToAbbr(hive);
					IniGroupItem newGroup = new IniGroupItem(hiveAbbr + ":" + key);
					if (group.ValueCount > 0)
						foreach (string valueName in group.GetValueNames())
							newGroup.Add( new IniLineItem(valueName, RegMgmt.GetValueAsString(valueName, keyName, hive, view), false, hiveAbbr + ":" + keyName) );
				}
			}
			return true; // temporary
		}

		public override bool Save(string fileName = "") => true;

		#region Functions to facilitate using default values for RegistryHive and RegistryView calls.
		protected RegistryHive Convert(RegistryHive? hive = null) =>
			(hive is null) ? this._defaultRegistryHive : (RegistryHive)hive;

		protected RegistryView Convert(RegistryView? view = null) =>
			(view is null) ? this._defaultRegistryView : (RegistryView)view;

		protected RegistryKey Convert(RegistryHive? hive = null, RegistryView? view = null) =>
			RegistryKey.OpenBaseKey(Convert(hive), Convert(view));
		#endregion
	}
}