﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

namespace NetXpertCodeLibrary.ConfigManagement
{
	/// <summary>A static class that provides simple routines for reading/writing to Windows' Registry.</summary>
	/// <remarks>
	/// Functions:<br/>
	///		private static RegistryKey ConvertToKey(RegistryHive? hive = null, RegistryView? view = null)<br/>
	///		public static RegistryKey[] ReadAllSubKeys(string subKey = "", RegistryKey hive = null);<br/>
	///		public static RegistryKey[] ReadAllSubKeys(string subKey = "", RegistryHive? hive = null, RegistryView? view = null);<br/>
	///		public static string[] GetSubKeyNames(string subKey = "", RegistryKey hive = null);<br/>
	///		public static string[] GetSubKeyNames(string subKey = "", RegistryHive? hive = null, RegistryView? view = null);<br/>
	///		public static bool DeleteSubKeyTree(string subKey = "", RegistryKey hive = null);<br/>
	///		public static bool DeleteSubKeyTree(string subKey = "", RegistryHive? hive = null, RegistryView? view = null);<br/>
	///		public static int SubKeyCount(string subKey = "", RegistryKey hive = null);<br/>
	///		public static int SubKeyCount(string subKey = "", RegistryHive? hive = null, RegistryView? view = null);<br/>
	///		public static int ValueCount(string subKey = "", RegistryKey hive = null);<br/>
	///		public static int ValueCount(string subKey = "", RegistryHive? hive = null, RegistryView? view = null);<br/>
	///		public static bool HasSubKeys(string subKey = "", RegistryKey hive = null);<br/>
	///		public static bool HasSubKeys(string subKey = "", RegistryHive? hive = null, RegistryView? view = null);<br/>
	///<br/>
	///		public static bool KeyExists(string subKey = "", RegistryHive? hive = null, bool createIfNot = false, RegistryView? view = null);<br/>
	///		public static bool ValueExists(string valueName, string subKey = "", RegistryHive? hive = null, RegistryView? view = null);<br/>
	///		public static RegistryKey CreateKey(string subKey = "", RegistryHive? hive = null, RegistryView? view = null);<br/>
	///		public static RegistryKey FetchKey(string subKey = "", RegistryHive? hive = null, RegistryView? view = null);<br/>
	///		public static bool SetValue(string name, object value, string subKey = "", RegistryValueKind type = RegistryValueKind.String, RegistryHive? hive = null, RegistryView? view = null);<br/>
	///		public static object GetValue(string name, string subKey = "", RegistryHive? hive = null, RegistryView? view = null);<br/>
	///		public static string GetValueAsString(string name, string subKey = "", RegistryHive? hive = null, RegistryView? view = null);<br/>
	///		public static RegistryHive ParseHiveAbbr(string value);<br/>
	///		public static string RegistryHiveToAbbr(RegistryHive value, bool dressItUp = false);<br/>
	/// </remarks>
	public static class RegMgmt
	{
		/// <summary>Specifies the default value to be used in these routines when a RegistryHive value isn't specified.</summary>
		public static RegistryHive DEFAULT_REGISTRY_HIVE = RegistryHive.LocalMachine;

		/// <summary>Specifies the default value to be used in these routines when a RegistryView value isn't specified.</summary>
		public static RegistryView DEFAULT_REGISTRY_VIEW = RegistryView.Default;

		/// <summary>Specifies the default value to use when a subKey value isn't specified.</summary>
		public static string DEFAULT_SUBKEY = $"SOFTWARE\\NetXpert\\{Application.ProductName}";

		/// <summary>Holds any error information generated by any of these routines during their operation.</summary>
		/// <remarks>
		/// This value is cleared whenever one of these registry functions is called so any value that it contains must have been
		/// generated on the last call. This provides a simple mechanism for determining if any errors occurred by checking if the
		/// length of this string is greater than 0.
		/// </remarks>
		public static string LastError = "";

		/// <summary>Retrieves all subkeys found in a specified Registry location in an array.</summary>
		/// <param name="subKey">A string specifying the SubKey to dereference. (Defaults to the value in RegistryAccess.DEFAULT_SUBKEY)</param>
		/// <param name="hive">A <seealso cref="RegistryKey"/> value specifying which hive to use. (Defaults to the value in RegistryAccess.DEFAULT_REGISTRY_KEY)</param>
		/// <returns>
		/// An array of <seealso cref="RegistryKey"/> objects for each SubKey it contains.
		/// If it does not exist, cannot be accessed, or contains no subkeys, the array will be empty.
		/// </returns>
		/// <remarks>Any errors encountered in the process are stored in RegistryKey.LastError.</remarks>
		public static RegistryKey[] ReadAllSubKeys(string subKey = "", RegistryKey hive = null)
		{
			// Clear the "Last_Error" string.
			LastError = "";

			// Validate the incoming subKey string for the default value
			subKey = string.IsNullOrEmpty(subKey.Trim()) ? DEFAULT_SUBKEY : subKey.Trim();

			List<RegistryKey> keys = new List<RegistryKey>();
			try
			{
				RegistryKey key = ((hive is null) ? Convert() : hive).OpenSubKey(subKey, false);
				if (key is null)
					LastError = "The Requested Key was not found!";
				else
				{
					string[] keyNames = key.GetSubKeyNames();
					foreach (string keyName in keyNames)
						keys.Add(key.OpenSubKey(keyName));
				}
			}
			catch (Exception e) { LastError = e.Message; }

			return keys.ToArray();
		}

		/// <summary>Facilitates setting the ACL permissions on a specified <see cref="RegistryKey"/>.</summary>
		/// <param name="subkey">A string specifying the subkey to modify.</param>
		/// <param name="access">A <seealso cref="RegistrySecurity"/> object specifying the permission to set.</param>
		/// <param name="hive">A <seealso cref="RegistryHive"/> value specifying which hive to use. (Defaults to DEFAULT_REGISTRY_HIVE)</param>
		/// <param name="view">A <seealso cref="RegistryView"/> value specifying which view to reference. (Defaults to DEFAULT_REGISTRY_VIEW)</param>
		public static void SetAcls( string subkey, System.Security.AccessControl.RegistrySecurity access, RegistryHive? hive = null, RegistryView? view = null )
		{
			LastError = "";
			if ( KeyExists( subkey, hive, false, view ) )
			{
				try
				{
					RegistryKey key = FetchKey( subkey, hive, view );
					key.SetAccessControl( access );
				}
				catch (Exception e) { LastError = e.Message; }
			}
			else
				LastError = "The Requested Key was not found!";
		}

		/// <summary>Retrieves all subkeys found in a specified Registry location in an array.</summary>
		/// <param name="subKey">A string specifying the SubKey to dereference. (Defaults to DEFAULT_SUBKEY)</param>
		/// <param name="hive">A <seealso cref="RegistryHive"/> value specifying which hive to use. (Defaults to DEFAULT_REGISTRY_HIVE)</param>
		/// <param name="view">A <seealso cref="RegistryView"/> value specifying which view to reference. (Defaults to DEFAULT_REGISTRY_VIEW)</param>
		/// <returns>
		/// An array of RegistryKey objects for each SubKey it contains.
		/// If it does not exist, cannot be accessed, or contains no subkeys, the array will be empty.
		/// </returns>
		/// <remarks>Any errors encountered in the process are stored in RegistryKey.LastError.</remarks>
		public static RegistryKey[] ReadAllSubKeys( string subKey = "", RegistryHive? hive = null, RegistryView? view = null ) =>
			ReadAllSubKeys( subKey, Convert( hive, view ) );

		/// <summary>Returns a list of subKey names contained by the provided subKey.</summary>
		/// <param name="subKey">A string specifying the SubKey to dereference. (Defaults to DEFAULT_SUBKEY)</param>
		/// <param name="hive">A <seealso cref="RegistryKey"/> value specifying which hive to use. (Defaults to DEFAULT_REGISTRY_HIVE)</param>
		/// <returns>
		/// An array of strings containing the name of each SubKey in the specified source.
		/// If the source does not exist, cannot be accessed, or contains no subkeys, the array will be empty.
		/// </returns>
		public static string[] GetSubKeyNames( string subKey = "", RegistryKey hive = null )
		{
			List<string> names = new();
			RegistryKey[] keys = ReadAllSubKeys( subKey, hive );
			foreach ( RegistryKey key in keys )
				names.Add( key.Name );

			return names.ToArray();
		}

		/// <summary>Returns a list of subKey names contained by the provided subKey.</summary>
		/// <param name="subKey">A string specifying the SubKey to dereference. (Defaults to the value in DEFAULT_SUBKEY)</param>
		/// <param name="hive">A <seealso cref="RegistryHive"/> value specifying which hive to use. (Defaults to DEFAULT_REGISTRY_HIVE)</param>
		/// <param name="view">A <seealso cref="RegistryView"/> value specifying which Registry view to reference. (Defaults to DEFAULT_REGISTRY_VIEW)</param>
		/// <returns>
		/// An array of strings containing the name of each SubKey in the specified source.
		/// If the source does not exist, cannot be accessed, or contains no subkeys, the array will be empty.
		/// </returns>
		public static string[] GetSubKeyNames(string subKey = "", RegistryHive? hive = null, RegistryView? view = null) =>
			GetSubKeyNames(subKey, Convert(hive, view));

		/// <summary>Deletes a specified subkey and all of the key/values beneath it.</summary>
		/// <param name="subKey">A string specifying the SubKey to dereference. (Defaults to the value in RegistryAccess.DEFAULT_SUBKEY)</param>
		/// <param name="hive">A <seealso cref="RegistryKey"/> value specifying which hive to use. (Defaults to the value in RegistryAccess.DEFAULT_REGISTRY_KEY)</param>
		/// <returns><b>TRUE</b> if the operation was successful, otherwise <b>FALSE</b>.</returns>
		/// <remarks>Any errors encountered in the process are stored in RegistryKey.LastError.</remarks>
		public static bool DeleteSubKeyTree(string subKey = "", RegistryKey hive = null)
		{
			// Clear the "Last_Error" string.
			LastError = "";

			// Validate the incoming subKey string for the default value
			subKey = string.IsNullOrEmpty(subKey.Trim()) ? DEFAULT_SUBKEY : subKey.Trim();
			try
			{
				RegistryKey key = ((hive is null) ? Convert() : hive).OpenSubKey(subKey);
				if (key is null)
					LastError = "The Requested Key was not found!";
				else
				{
					key.DeleteSubKeyTree(subKey);
					return true;
				}
			}
			catch (Exception e) { LastError = e.Message; }
			return false;
		}

		/// <summary>Deletes a specified subkey and all of the key/values beneath it.</summary>
		/// <param name="subKey">A string specifying the SubKey to dereference. (Defaults to the value in DEFAULT_SUBKEY)</param>
		/// <param name="hive">A <seealso cref="RegistryHive"/> value specifying which hive to use. (Defaults to DEFAULT_REGISTRY_HIVE)</param>
		/// <param name="view">A <seealso cref="RegistryView"/> value specifying which view to reference. (Defaults to DEFAULT_REGISTRY_VIEW)</param>
		/// <returns><b>TRUE</b> if the operation was successful, otherwise <b>FALSE</b>.</returns>
		/// <remarks>Any errors encountered in the process are stored in <see cref="RegistryKey.LastError"/>.</remarks>
		public static bool DeleteSubKeyTree(string subKey = "", RegistryHive? hive = null, RegistryView? view = null) =>
			DeleteSubKeyTree(subKey, Convert(hive, view));

		/// <summary>Returns the number of subkeys found in the specified RegistryKey + subkey</summary>
		/// <param name="subKey">A string specifying the SubKey to dereference. (Defaults to the value in RegistryAccess.DEFAULT_SUBKEY)</param>
		/// <param name="hive">A <seealso cref="RegistryKey"/> value specifying which hive to use. (Defaults to the value in RegistryAccess.DEFAULT_REGISTRY_KEY)</param>
		/// <returns>A count of the number of keys/subkeys found in the requested registry location. Or -1 if the request failed for any reason.</returns>
		/// <remarks>Any errors encountered in the process are stored in <see cref="RegistryKey.LastError"/>.</remarks>
		public static int SubKeyCount(string subKey = "", RegistryKey hive = null)
		{
			// Clear the "Last_Error" string.
			LastError = "";

			// Validate the incoming subKey string for the default value
			subKey = string.IsNullOrEmpty(subKey.Trim()) ? DEFAULT_SUBKEY : subKey.Trim();
			try
			{
				RegistryKey key = ((hive is null) ? Convert() : hive).OpenSubKey(subKey);
				return (key is null) ? -1 : key.SubKeyCount;
			}
			catch (Exception e) { LastError = e.Message; }
			return -1;
		}

		/// <summary>Returns the number of subkeys found in the specified RegistryKey + subkey</summary>
		/// <param name="subKey">A string specifying the SubKey to dereference. (Defaults to the value in RegistryAccess.DEFAULT_SUBKEY)</param>
		/// <param name="hive">A <seealso cref="RegistryKey"/> value specifying which hive to use. (Defaults to HKEY_LOCAL_MACHINE)</param>
		/// <param name="view">A <seealso cref="RegistryView"/> value specifying which view to reference.</param>
		/// <returns>A count of the number of keys/subkeys found in the requested registry location. Or -1 if the request failed for any reason.</returns>
		/// <remarks>Any errors encountered in the process are stored in RegistryKey.LastError.</remarks>
		public static int SubKeyCount(string subKey = "", RegistryHive? hive = null, RegistryView? view = null) =>
			SubKeyCount(subKey, Convert(hive, view));

		/// <summary>Returns the number of value entries found in the specified RegistryKey + subkey.</summary>
		/// <param name="subKey">A string specifying the SubKey to dereference. (Defaults to the value in RegistryAccess.DEFAULT_SUBKEY)</param>
		/// <param name="hive">A <seealso cref="RegistryKey"/> value specifying which hive to use. (Defaults to the value in RegistryAccess.DEFAULT_REGISTRY_KEY)</param>
		/// <returns>A count of the number of keys/values found in the requested registry location. Or -1 if the request failed for any reason.</returns>
		/// <remarks>Any errors encountered in the process are stored in RegistryKey.LastError.</remarks>
		public static int ValueCount(string subKey = "", RegistryKey hive = null)
		{
			LastError = ""; // Clear the "Last_Error" string.

			// Validate the incoming subKey string for the default value
			subKey = string.IsNullOrEmpty(subKey.Trim()) ? DEFAULT_SUBKEY : subKey.Trim();
			try
			{
				RegistryKey key = ((hive is null) ? Convert() : hive).OpenSubKey(subKey, false);
				// If the RegistryKey exists...
				return (key is null) ? -1 : key.ValueCount;
			}
			catch (Exception e) { LastError = e.Message; }
			return -1;
		}

		/// <summary>Returns the number of value entries found in the specified RegistryKey + subkey.</summary>
		/// <param name="subKey">A string specifying the SubKey to dereference. (Defaults to the value in RegistryAccess.DEFAULT_SUBKEY)</param>
		/// <param name="hive">A <seealso cref="RegistryHive"/> value specifying which hive to use. (Defaults to HKEY_LOCAL_MACHINE)</param>
		/// <param name="view">A <seealso cref="RegistryView"/> value specifying which view to reference.</param>
		/// <returns>A count of the number of keys/values found in the requested registry location. Or -1 if the request failed for any reason.</returns>
		/// <remarks>Any errors encountered in the process are stored in RegistryKey.LastError.</remarks>
		public static int ValueCount(string subKey = "", RegistryHive? hive = null, RegistryView? view = null) =>
			ValueCount(subKey, Convert(hive, view));

		/// <summary>Provides a means to quickly check for the existence of subkeys within a specified registry key+subkey.</summary>
		/// <param name="subKey">A string specifying the SubKey to dereference. (Defaults to the value in RegistryAccess.DEFAULT_SUBKEY)</param>
		/// <param name="hive">A <seealso cref="RegistryKey"/> value specifying which hive to use. (Defaults to the value in RegistryAccess.DEFAULT_REGISTRY_KEY)</param>
		/// <returns><b>TRUE</b> if the specified registry entry contains subkeys, otherwise <b>FALSE</b>.</returns>
		/// <remarks>Any errors encountered in the process are stored in RegistryKey.LastError.</remarks>
		public static bool HasSubKeys(string subKey = "", RegistryKey hive = null) => SubKeyCount(subKey, hive) > 0;

		/// <summary>Provides a means to quickly check for the existence of subkeys within a specified registry key+subkey.</summary>
		/// <param name="subKey">A string specifying the SubKey to dereference. (Defaults to the value in RegistryAccess.DEFAULT_SUBKEY)</param>
		/// <param name="hive">A <seealso cref="RegistryHive"/> value specifying which Hive to use. (Defaults to HKEY_LOCAL_MACHINE)</param>
		/// <param name="view">A <seealso cref="RegistryView"/> value specifying which Registry view to reference.</param>
		/// <returns><b>TRUE</b> if the specified registry entry contains subkeys, otherwise <b>FALSE</b>.</returns>
		/// <remarks>Any errors encountered in the process are stored in RegistryKey.LastError.</remarks>
		public static bool HasSubKeys(string subKey = "", RegistryHive? hive = null, RegistryView? view = null) =>
			SubKeyCount(subKey, Convert(hive, view)) > 0;

		/// <summary>Tests for the existence of a specified Key in the registry.</summary>
		/// <param name="subKey">A string specifying the key to test.</param>
		/// <param name="hive">A <seealso cref="RegistryHive"/> value specifying which Hive to search in (default is HKEY_LOCAL_MACHINE).</param>
		/// <param name="createIfNot">A bool value that, if set to <b>TRUE</b>, will cause the specified key to be created if it doesn't exist (default to <b>FALSE</b>).</param>
		/// <param name="view">A <seealso cref="RegistryHive"/> value specifying which View to use.</param>
		/// <returns><b>TRUE</b> if the specified key exists, otherwise <b>FALSE</b>.</returns>
		public static bool KeyExists(string subKey = "", RegistryHive? hive = null, bool createIfNot = false, RegistryView? view = null)
		{
			LastError = ""; // Clear the LastError value.
			RegistryKey test = Convert(hive, view);
			try
			{
				var reg = test.OpenSubKey(subKey, false);
				if ((reg is null) && createIfNot)
					try { reg = test.CreateSubKey(subKey); } catch (Exception e) { LastError = e.Message; return false; }
				LastError = (reg is null) ? $"Could not find the requested key (\"{RegistryHiveToAbbr(hive, true)}{subKey}\") in the specified view ({view})." : "";
				return (reg is not null);
			}
			catch(Exception e) { LastError = e.Message; }
			return false;
		}

		/// <summary>Tests for the existence of a specified value within a specified key.</summary>
		/// <param name="valueName">The Name assigned to the value to look for.</param>
		/// <param name="subKey">A string specifying the key to test.</param>
		/// <param name="hive">A <seealso cref="RegistryHive"/> value specifying which hive to search in (default is HKEY_LOCAL_MACHINE).</param>
		/// <param name="view">A <seealso cref="RegistryView"/> value specifying which view to use.</param>
		/// <returns><b>TRUE</b> if a key was found in the specified location, otherwise <b>FALSE</b>.</returns>
		public static bool ValueExists(string valueName, string subKey = "", RegistryHive? hive = null, RegistryView? view = null)
		{
			LastError = ""; // Clear the LastError value.
			RegistryKey key = FetchKey(subKey, hive, view);
			if (key is null)
				LastError = $"Could not find the requested key (\"{RegistryHiveToAbbr(hive, true)}{subKey}\") in the specified view ({view}).";
			else
				if ( key.GetValue(valueName) is not null )
				LastError = $"Could not find the requested value (\"{valueName}\") in the specified key / hive / view (\"{RegistryHiveToAbbr(hive, true)}{subKey}\", {view}).";
			else
				return true;
			return false;
		}

		/// <summary>Reports whether or not the specified SubKey has a default value.</summary>
		/// <param name="subKey">The subkey who's default value we're looking for.</param>
		/// <param name="hive">A <seealso cref="RegistryHive"/> value specifying which hive to search in (default is HKEY_LOCAL_MACHINE).</param>
		/// <param name="view">A <seealso cref="RegistryVikew"/> value specifying which view to use.</param>
		/// <returns><b>TRUE</b> if the specified subKey has a default value.</returns>
		public static bool HasDefaultValue(string subKey = "", RegistryHive? hive = null, RegistryView? view = null)
		{
			// Validate the incoming subKey string for the default value
			subKey = string.IsNullOrEmpty(subKey.Trim()) ? DEFAULT_SUBKEY : subKey.Trim();

			LastError = ""; // Clear the LastError value.
			try
			{
				RegistryKey key = FetchKey(subKey, hive, view);
				return (key is not null) || (key.GetValue("") is not null);
			}
			catch (Exception e) { LastError = e.Message; }
			return false;
		}

		/// <summary>Returns the default value from the specified key (if it exists, NULL otherwise).</summary>
		/// <param name="subKey">The subkey who's default value we're looking for.</param>
		/// <param name="hive">A <seealso cref="RegistryHive"/> value specifying which Hive to search in (default is HKEY_LOCAL_MACHINE).</param>
		/// <param name="view">A <seealso cref="RegistryView"/> value specifying which View to use.</param>
		/// <returns>The default value of the subkey, if one exists, otherwise <i>NULL</i>.</returns>
		public static dynamic GetDefaultValue(string subKey = "", RegistryHive? hive = null, RegistryView? view = null)
		{
			// Validate the incoming subKey string for the default value
			subKey = string.IsNullOrEmpty(subKey.Trim()) ? DEFAULT_SUBKEY : subKey.Trim();

			if (HasDefaultValue(subKey, hive, view))
				return FetchKey(subKey, hive, view).GetValue("");

			return null;
		}

		/// <summary>Returns the data type of the subkey's default value, if one exists, otherwise RegistryValueKind.None.</summary>
		/// <param name="subKey">The subkey who's default value we're looking for.</param>
		/// <param name="hive">A <seealso cref="RegistryHive"/> value specifying which Hive to search in (default is HKEY_LOCAL_MACHINE).</param>
		/// <param name="view">A <seealso cref="RegistryView"/> value specifying which View to use.</param>
		/// <returns>The data type of the subkey's default value, if one exists, otherwise RegistryValueKind.None.</returns>
		public static RegistryValueKind GetDefaultValueKind(string subKey = "", RegistryHive? hive = null, RegistryView? view = null)
		{
			if (HasDefaultValue(subKey, hive, view))
			{
				RegistryKey key = FetchKey(subKey, hive, view);
				return key.GetValueKind("");
			}
			LastError = "The requested subkey could not be found.";
			return RegistryValueKind.None;
		}

		/// <summary>
		/// Attempts to creates a specified registry key and returns a <seealso cref="RegistryKey"/> value from it.
		/// If the key already exists, then nothing is created and the existing RegistryKey value is returned.
		/// </summary>
		/// <param name="subKey">A string specifying the Key to create.</param>
		/// <param name="hive">A <seealso cref="RegistryHive"/> value specifying which Hive to search in (default is HKEY_LOCAL_MACHINE).</param>
		/// <param name="view">A <seealso cref="RegistryView"/> value specifying which View to use.</param>
		/// <returns>A <seealso cref="RegistryKey"/> object if the specified Key was created, or already existed, otherwise NULL.</returns>
		public static RegistryKey CreateKey(string subKey = "", RegistryHive? hive = null, RegistryView? view = null)
		{
			// Validate the incoming subKey string for the default value
			subKey = string.IsNullOrEmpty(subKey.Trim()) ? DEFAULT_SUBKEY : subKey.Trim();

			LastError = ""; // Clear the LastError value.
			try
			{
				RegistryKey test = Convert(hive, view);
				return (!KeyExists(subKey, hive, false, view)) ? test.CreateSubKey(subKey, true) : test.OpenSubKey(subKey, true);
			}
			catch (Exception e) { LastError = e.Message; }
			return null;
		}

		/// <summary>Retrieves a specified <seealso cref="RegistryKey"/> object.</summary>
		/// <param name="subKey">A string specifying the Key to retrieve.</param>
		/// <param name="hive">A <seealso cref="RegistryHive"/> value specifying which Hive to search in (default is HKEY_LOCAL_MACHINE).</param>
		/// <param name="view">A <seealso cref="RegistryView"/> value specifying which View to use.</param>
		/// <returns>A populated <seealso cref="RegistryKey"/> object if the specified Key was found, otherwise NULL.</returns>
		public static RegistryKey FetchKey(string subKey = "", RegistryHive? hive = null, RegistryView? view = null, bool writeable = false)
		{
			// Validate the incoming subKey string for the default value
			subKey = string.IsNullOrEmpty(subKey.Trim()) ? DEFAULT_SUBKEY : subKey.Trim();

			LastError = ""; // Clear the LastError value.
			try { return (KeyExists(subKey, hive, false, view)) ? Convert(hive, view).OpenSubKey(subKey, writeable) : null; }
			catch (Exception e) { LastError = e.Message; }
			return null;
		}

		/// <summary>Creates a <seealso cref="RegistryValue"/> within a specified <seealso cref="RegistryKey"/> (which must exist!)</summary>
		/// <param name="name">The name of the value key to create/set/update.</param>
		/// <param name="value">An object containing the value to set (must accord itself with the "type").</param>
		/// <param name="subKey">A string specifying the Registry (sub)key in which to set this value.</param>
		/// <param name="type">A <seealso cref="RegistryValueKind"/> designation for the value (default = string).</param>
		/// <param name="hive">The hive in which to work (default is HKEY_LOCAL_MACHINE).</param>
		/// <param name="view">A <seealso cref="RegistryView"/> value specifying which View to use.</param>
		/// <returns><b>TRUE</b> if the operation succeeded, otherwise <b>FALSE</b>.</returns>
		public static bool SetValue(string name, object value, string subKey = "", RegistryValueKind type = RegistryValueKind.String, RegistryHive? hive = null, RegistryView? view = null)
		{
			// Validate the incoming subKey string for the default value
			subKey = string.IsNullOrEmpty(subKey.Trim()) ? DEFAULT_SUBKEY : subKey.Trim();

			LastError = ""; // Clear the LastError value.
			try
			{
				RegistryKey test = FetchKey(subKey, hive, view, true);
				if ( test is not null )
				{
					test.SetValue(name, value, type);
					return true;
				}
			}
			catch (Exception e) { LastError = e.Message; }
			return false;
		}

		/// <summary>Retrieves the value data from a specified Subkey + Value.</summary>
		/// <param name="name">The name of the value to dereference.</param>
		/// <param name="subKey">A string specifying the subkey from which to get the value.</param>
		/// <param name="hive">The hive in which to work (default is HKEY_LOCAL_MACHINE).</param>
		/// <param name="view">A <seealso cref="RegistryView"/> value specifying which View to use.</param>
		/// <returns>An object containing the registry value according to its defined type.</returns>
		public static object GetValue(string name, string subKey = "", RegistryHive? hive = null, RegistryView? view = null)
		{
			// Validate the incoming subKey string for the default value
			subKey = string.IsNullOrEmpty(subKey.Trim()) ? DEFAULT_SUBKEY : subKey.Trim();

			try { if (ValueExists(name, subKey, hive, view)) { return FetchKey(subKey, hive, view).GetValue(name); } }
			catch (Exception e) { LastError = e.Message; }
			return null;
		}

		/// <summary>Retrieves the value data from a specified Subkey + Value.</summary>
		/// <param name="name">The name of the value to dereference.</param>
		/// <param name="subKey">A string specifying the subkey from which to get the value.</param>
		/// <param name="hive">The hive in which to work (default is HKEY_LOCAL_MACHINE).</param>
		/// <param name="view">A <seealso cref="RegistryView"/> value specifying which View to use.</param>
		/// <returns>A String representation of the requested registry value.</returns>
		public static string GetValueAsString(string name, string subKey = "", RegistryHive? hive = null, RegistryView? view = null)
		{
			object result = GetValue(name, subKey, hive, view);
			return (result is null) ? "" : result.ToString();
		}

		/// <summary>Retrieves the ValueKind of a specified subKey value.</summary>
		/// <param name="name">The name of the value to be interrogated.</param>
		/// <param name="subKey">A string specifying the subkey in which the Value lives.</param>
		/// <param name="hive">The <seealso cref="RegistryHive"/> to reference.</param>
		/// <param name="view">Which Registry to use.</param>
		/// <returns>If the value is found, it's <seealso cref="RegistryValueKind"/>, otherwise RegistryValueKind.None.</returns>
		/// <remarks>
		/// Receiving a return value of RegistryValueKind.None does NOT necessarily indicate that the key couldn't be found,
		/// always check the LastError value if you need to know!
		/// </remarks>
		public static RegistryValueKind GetValueType(string name, string subKey = "", RegistryHive? hive = null, RegistryView? view = null)
		{
			// Validate the incoming subKey string for the default value
			subKey = string.IsNullOrEmpty(subKey.Trim()) ? DEFAULT_SUBKEY : subKey.Trim();

			LastError = ""; // Clear the LastError value.
			if (ValueExists(name, subKey, hive, view))
			{
				RegistryKey key = FetchKey(subKey, hive, view);
				return key.GetValueKind(name);
			}
			LastError = $"The requested value (\"{subKey}[{name}]\") was not found.";
			return RegistryValueKind.None;
		}

		/// <summary>Attempts to parse a supplied string into the correct RegistryHive enumeration.</summary>
		/// <param name="value">A string containing the text to parse (i.e. "HKLM", "HKCU", "HKCC" etc).</param>
		/// <returns>The appropriate</returns>
		/// <exception cref="InvalidOperationException"></exception>
		public static RegistryHive ParseHiveAbbr( string value ) =>
			Regex.Replace( value, @"[^acdefghiklmnoprstuy]", "", RegexOptions.IgnoreCase ).ToLower() switch
			{
				"hkeylocalmachine" or "localmachine" or "hklm" => RegistryHive.LocalMachine,
				"hkeycurrentuser" or "currentuser" or "hkcu" => RegistryHive.CurrentUser,
				"hkeycurrentconfig" or "currentconfig" or "hkcc" => RegistryHive.CurrentConfig,
				"hkeyclassesroot" or "classesroot" or "classes" or "hkcr" => RegistryHive.ClassesRoot,
				"hkeyperformancedata" or "performancedata" or "performance" or "hkpd" => RegistryHive.PerformanceData,
				"hku" or "hkus" or "hkusr" or "hkuser" or "hkusers" => RegistryHive.Users,
				_ => throw new InvalidOperationException( "The value provided (\"" + value + "\") does not conform to an accepted RegistryHive abbreviation." )
				//				case "hkeydynamicdata":
				//				case "dynamicdata":
				//				case "dynamic":
				//				case "hkdd":
				//					return RegistryHive.DynData;
			};

		/// <summary>Converts a <seealso cref="RegistryHive"/> enum into an appropriate abbreviated string value.</summary>
		/// <param name="value">The <seealso cref="RegistryHive"/> value to parse.</param>
		/// <param name="dressItUp">
		/// If set to <b>TRUE</b> the returned value will be enclosed in brace brackets and be followed by a colon
		/// (i.e. "RegistryHive.LocalMachine" => "{HKLM}:").
		/// </param>
		/// <returns>A string containing an appropriate designation for the supplied <seealso cref="RegistryHive"/> value.</returns>
		public static string RegistryHiveToAbbr(RegistryHive? value, bool dressItUp = false)
		{
			string hiveText = "HK" + (Convert( value ) switch {
				// NOTE: Hivetext automatically prepends 'HK' when the specified string is only 2-characters...
				RegistryHive.LocalMachine => "LM",
				RegistryHive.CurrentUser => "CU",
				RegistryHive.CurrentConfig => "CC",
				RegistryHive.ClassesRoot => "CR",
				//				RegistryHive.DynData => "DD",
				RegistryHive.PerformanceData => "PD",
				RegistryHive.Users => "USERS",
				_ => $"UNK({value})",
			});
			return dressItUp ? $"{{{hiveText}}}:" : hiveText;
		}

		#region Private Functions
		/// <summary>Facilitates converting RegistryHive? to RegistryHive and incorporates the DEFAULT_REGISTRY_HIVE value when the originator is NULL.</summary>
		/// <param name="hive">A RegistryHive? value to parse.</param>
		/// <returns>A proper RegistryHive value derived from the source.</returns>
		private static RegistryHive Convert(RegistryHive? hive) => (hive is null) ? DEFAULT_REGISTRY_HIVE : (RegistryHive)hive;

		/// <summary>Facilitates converting RegistryView? to RegistryView and incorporates the DEFAULT_REGISTRY_VIEW value when the originator is NULL.</summary>
		/// <param name="view">A RegistryView? value to parse.</param>
		/// <returns>A proper RegistryView value derived from the source.</returns>
		private static RegistryView Convert(RegistryView? view) => (view is null) ? DEFAULT_REGISTRY_VIEW : (RegistryView)view;

		/// <summary>Creates a new RegistryKey BaseKey from the supplied values, leveraging the DEFAULT_REGISTRY_HIVE and DEFAULT_REGISTRY_VIEW static values.</summary>
		/// <param name="hive">A RegistryHive value (or NULL) to use. If NULL (the default) is used, DEFAULT_REGISTRY_HIVE is substituted.</param>
		/// <param name="view">A RegistryView value (or NULL) to use. If NULL (the default) is used, DEFAULT_REGISTRY_VIEW is substituted.</param>
		/// <returns>A new RegistryKey value derived from the supplied values.</returns>
		private static RegistryKey Convert(RegistryHive? hive = null, RegistryView? view = null) =>
			RegistryKey.OpenBaseKey(Convert(hive), Convert(view));
		#endregion
	}
}
