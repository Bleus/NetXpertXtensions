using System;
using Microsoft.Win32;

namespace NetXpertCodeLibrary.ConfigManagement
{
	/// <summary>Provides a specialised implementation of IniLineItem designed to manage line items containing co-ordinate values.</summary>
	/// <remarks>Format: ( a, b )</remarks>
	public class IniRegistryItem : IniLineItem
	{
		public IniRegistryItem(string key, RegistryKey registryKey, bool encrypt = false, bool enable = true)
			: base(key, "", encrypt, registryKey.Name, enable)
		{
			if (registryKey.ValueCount > 0)
				foreach (string itemName in registryKey.GetValueNames())
					if (itemName.Equals(key, StringComparison.OrdinalIgnoreCase))
						base.Value = registryKey.GetValue(itemName).ToString();
		}

		public IniRegistryItem(string key, string value = "", bool encrypt = false, string comment = "", bool enable = true)
			: base(key, value, encrypt, comment, enable) {  }

		public bool Save()
		{
			return true;
		}
	}
}