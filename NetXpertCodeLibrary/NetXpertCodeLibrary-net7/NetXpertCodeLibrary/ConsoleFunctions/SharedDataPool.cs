using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using NetXpertCodeLibrary.Extensions;
using NetXpertExtensions;

namespace NetXpertCodeLibrary.ConsoleFunctions
{

	/// <summary>
	/// This struct is used by the PoolDataItem/SharedData classes to provide for diagnostic investigation of the collection without
	/// impacting the data sanctity of the pool.
	/// </summary>
	public struct PoolDataDiagItem
	{
		public string Id;
		public string Cmd;
		public int Size;
		public Type Type;
		public DateTime Created;
		public TimeSpan Age;
		public TimeSpan MaxAge;
		public int Records;
		public int MaxRetrieves;
		public string Owner;

		public ConsoleListLine ToListLine()
		{
			string[] results = new string[7] { "", "", "", "", "", "", "" };
			TimeSpan ttd = MaxAge - Age;

			results[0] = Id;
			results[1] = (Type is null) ? "Unknown(NULL)" : ((Type.Name.Length > 20) ? Type.Name.Substring(0,17) + "..." : Type.Name.PadRight(20,' '));
			results[2] = Cmd.PadLeft(14,' ');
			results[3] = Created.ToString("HH:mm:ss");
			results[4] = Math.Floor(ttd.TotalHours).ToString().PadLeft(3, ' ') + "H" + ttd.Minutes.ToString().PadLeft(2, '0') + ":" + ttd.Seconds.ToString().PadLeft(2, '0') + "." + Math.Round((decimal)ttd.Milliseconds).ToString("###").PadRight(3,'0');
			//results[5] = Math.Floor(MaxAge.TotalHours).ToString().PadLeft(2,' ') + ":" + MaxAge.Minutes.ToString().PadLeft(2, '0') + ":" + MaxAge.Seconds.ToString().PadLeft(2, '0');
			results[5] = $"{{{Records.ToString().PadCenter(4, ' ')}}}";
			results[6] = (MaxRetrieves == int.MaxValue) ? " -1" : MaxRetrieves.ToString().PadLeft(3,' ');
			return new ConsoleListLine(results);
		}

		public static int GetRecordCount(dynamic obj)
		{
			if (obj is null) return 0;
			if (((object)obj).GetType().GetProperty("Count") != null) return obj.Count;
			if (((object)obj).GetType().GetProperty("Length") != null) return obj.Length;
			return 1;
		}
	}

	public class PoolDataItem
	{
		#region Properties
		protected string _cmd;
		protected XmlDocument _doc = null;
		protected dynamic _item;
		protected Type _type;
		protected string _id;
		protected DateTime _createdAt;
		protected TimeSpan _maxAge;
		protected int _maxRetrieves;
		protected UserInfo _owner;
		#endregion

		#region Constructors
		public PoolDataItem(string id, dynamic item, UserInfo user, TimeSpan maxAge, Type type = null, string cmd = "", int maxRetrieves = -1, string xml = "")
		{
			this.Id = id;
			this.Cmd = cmd;
			this._item = item;
			this.MaxAge = maxAge;
			this._createdAt = DateTime.Now;
			this._type = (type is null) ? item.GetType() : type;
			this.MaxRetrieves = maxRetrieves;
			this._owner = user;
			this._doc = new XmlDocument();

			if (xml.IsXml())
				this._doc.LoadXml(xml);
		}

		public PoolDataItem(string id, dynamic item, UserInfo user, Type type = null, string cmd = "", int maxRetrieves = -1, string xml = "")
		{
			this.Id = id;
			this.Cmd = cmd;
			this._item = item;
			this._createdAt = DateTime.Now;
			this.MaxRetrieves = maxRetrieves;
			this.MaxAge = new TimeSpan(0, 0, 0);
			this._type = (type is null) ? item.GetType() : type;
			this._owner = user;
			this._doc = new XmlDocument();

			if (xml.IsXml())
				this._doc.LoadXml(xml);
		}
		#endregion

		#region Accessors
		public string Id
		{
			get => this._id;
			protected set
			{
				if (string.IsNullOrEmpty(value) || (value.Trim().Length == 0))
					throw new ArgumentException("You cannot assign a null or empty value for the Id of this object.");
				this._id = value;
			}
		}

		public string Cmd
		{
			get => this._cmd;
			protected set =>
				this._cmd = (value is null) ? "" : value.Filter("abcdefghijklmnopqrstuvwxyz0123456789_");
		}

		public dynamic Data
		{
			get => (this._maxRetrieves-- > 0) ? this._item : null;
			protected set { this._item = value; this._type = value.GetType(); }
		}

		public Type Type
		{
			get => this._type;
			protected set => this._type = (value is null) ? this._item.GetType() : value;
		}

		public DateTime CreatedAt => this._createdAt;

		public int MaxRetrieves
		{
			get => this._maxRetrieves;
			protected set => this._maxRetrieves = (value < 1) ? int.MaxValue : value;
		}

		public TimeSpan Age => DateTime.Now - this._createdAt;

		public TimeSpan MaxAge
		{
			get => this._maxAge;
			protected set
			{
				int compare = TimeSpan.Compare(value, new TimeSpan(0, 0, 0));
				switch (compare)
				{
					case 1:
						this._maxAge = value;
						break;
					default:
						this._maxAge = new TimeSpan(23, 59, 59);
						break;
				}
			}
		}

		/// <summary>Returns the size of this object in bytes.</summary>
		public int Size => System.Runtime.InteropServices.Marshal.ReadInt32(this.GetType().TypeHandle.Value, 4);

		public bool Expired => (this.Age > this.MaxAge) || (this.MaxRetrieves < 0);
		#endregion

		#region Methods
		public override string ToString() => "{" + this._id + "}: " + this._type.Name;

		public PoolDataDiagItem Diagnostics()
		{
			PoolDataDiagItem diags = new PoolDataDiagItem();

			diags.Id = this._id;
			diags.Size = this.Size;
			diags.Type = this._type;
			diags.Cmd = this._cmd;
			diags.Age = this.Age;
			diags.MaxAge = this._maxAge;
			diags.MaxRetrieves = this._maxRetrieves;
			diags.Created = this._createdAt;
			diags.Records = PoolDataDiagItem.GetRecordCount(this._item);
			diags.Owner = this._owner.UserName;

			return diags;
		}
		#endregion

		#region Static Methods
		/// <summary>Creates a new PoolDataItem when a pre-existing UID isn't available.</summary>
		/// <param name="item">The data to store.</param>
		/// <param name="maxRetrieves">Specifies the maximum number of times this data can be recovered.</param>
		/// <param name="cmd">An optional string specifying the command name to associate with the item.</param>
		/// <param name="xml">Optional XML source to store with it. </param>
		/// <returns>A new PoolDataItem with a new UID, containing the provided data.</returns>
		public static PoolDataItem CreateItem(dynamic item, UserInfo user, string cmd = "", string xml = "", int maxRetrieves = -1) =>
			CreateItem(item, user, item.GetType(), cmd, xml, maxRetrieves);

		/// <summary>Creates a new PoolDataItem when a pre-existing UID isn't available.</summary>
		/// <param name="item">The data to store.</param>
		/// <param name="type">A Type object </param>
		/// <param name="cmd">An optional string specifying the command name to associate with the item.</param>
		/// <param name="xml">Optional XML source to store with it. </param>
		/// <returns>A new PoolDataItem with a new UID, containing the provided data.</returns>
		public static PoolDataItem CreateItem(dynamic item, UserInfo user, Type type, string cmd = "", string xml = "", int maxRetrieves = -1)
		{
			Guid newId = new Guid();
			return new PoolDataItem(newId.ToString(), item, user, type, cmd, maxRetrieves, xml);
		}
		#endregion
	}

	public sealed class PoolDataDiagItemCollection : IEnumerator<PoolDataDiagItem>
	{
		#region Properties
		List<PoolDataDiagItem> _items = new List<PoolDataDiagItem>();
		int _position = 0;
		#endregion

		public PoolDataDiagItemCollection() { }

		#region Accessors
		public int Count => this._items.Count;

		public PoolDataDiagItem this[int index] =>
			this._items[index];

		public PoolDataDiagItem this[string uid] =>
			this[ IndexOf( uid )];

		// IEnumerator Support Accessors...
		PoolDataDiagItem IEnumerator<PoolDataDiagItem>.Current => this[ this._position ];

		object IEnumerator.Current => this[ this._position ];
		#endregion

		#region Methods
		private int IndexOf( string uid )
		{
			int i = -1; while ((++i < Count) && !uid.Equals( this[ i ].Id, StringComparison.OrdinalIgnoreCase )) ;
			return (i < Count) ? i : -1;
		}

		public bool HasItem(string uid) =>
			IndexOf( uid ) >= 0;

		public void Add( PoolDataDiagItem item )
		{
			int i = IndexOf( item.Id );
			if (i < 0)
				this._items.Add( item );
			else
				this._items[ i ] = item;
		}

		public void AddRange( PoolDataDiagItem[] items )
		{
			foreach (PoolDataDiagItem item in items)
				this.Add( item );
		}

		public PoolDataDiagItem[] ToArray() =>
			this._items.ToArray();

		//IEnumerator Support
		public IEnumerator<PoolDataDiagItem> GetEnumerator() =>
			this._items.GetEnumerator();

		bool IEnumerator.MoveNext() => (++this._position) < this.Count;

		void IEnumerator.Reset() => this._position = 0;

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		public void Dispose(bool disposing)
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
			Dispose( true );
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
		#endregion
	}

	/// <summary>Provides the rudimentary infrastructure for managing a collection of PoolDataItems</summary>
	public class PoolData : IEnumerator<PoolDataItem>
	{
		#region Properties
		protected List<PoolDataItem> _pool = new List<PoolDataItem>();
		private int _position = 0;
		#endregion

		#region Constructors
		public PoolData() { }
		#endregion

		#region Accessors
		public PoolDataItem this[int index]
		{
			get => this._pool[index];
			set => this._pool[index] = value;
		}

		public PoolDataItem this[string id]
		{
			get { int i = IndexOf(id); return (i < 0) ? null : this[i]; }
		}

		public int Count => this._pool.Count;

		// IEnumerator Support Accessors...
		PoolDataItem IEnumerator<PoolDataItem>.Current => this[this._position];

		object IEnumerator.Current => this[this._position];
		#endregion

		#region Methods
		protected int IndexOf(string id)
		{
			int i = -1; while ((++i < Count) && !this[i].Id.Equals(id, StringComparison.OrdinalIgnoreCase)) ;
			return (i < Count) ? i : -1;
		}

		public bool HasItem(string id) => (IndexOf(id) >= 0);

		public bool HasItem(PoolDataItem item) => HasItem(item.Id);

		public void Add(PoolDataItem newItem)
		{
			int i = IndexOf(newItem.Id);
			if (i < 0)
				this._pool.Add(newItem);
			else
				this._pool[i] = newItem;
		}

		public void Add(AppletFoundation applet) =>
			this.Add(applet.CreatePoolItem());

		public void Remove(string id)
		{
			int i = IndexOf(id);
			if (i >= 0)
				this._pool.RemoveAt(i);
		}

		public void Remove(PoolDataItem item) => Remove(item.Id);

		public void RemoveAt(int index)
		{
			if ((index >= 0) && (index < Count)) this._pool.RemoveAt(index);
		}
		#endregion

		//IEnumerator Support
		public IEnumerator<PoolDataItem> GetEnumerator() =>
			this._pool.GetEnumerator();

		bool IEnumerator.MoveNext() => (++this._position) < this._pool.Count;

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

	/// <summary>Provides an upper-level management class for a pool of shared PoolDataItems.</summary>
	public class SharedData
	{
		#region Properties
		public PoolData _pool = new PoolData();
		#endregion

		#region Constructors
		public SharedData() { }
		#endregion

		#region Accessors
		public int Count => this._pool.Count;

		public PoolDataItem this[string uid] => this._pool[uid];
		#endregion

		#region Methods
		/// <summary>Returns a list of all Uids currently being managed in the pool.</summary>
		/// <returns>An array of strings specifying the Id's of all items stored in the pool.</returns>
		public string[] GetUids()
		{
			List<string> idList = new List<string>();
			foreach (PoolDataItem item in this._pool)
				idList.Add(item.Id);
			return idList.ToArray();
		}

		/// <summary>Find all stored Uid's of a given type.</summary>
		/// <param name="byType">A Type object that specifies the data types to collect Id's for.</param>
		/// <returns>An array of strings specifying the Id's of stored items whose data conforms to the specified Type.</returns>
		public string[] GetUids(Type byType)
		{
			List<string> idList = new List<string>();
			foreach (PoolDataItem item in this._pool)
				if (item.Type == byType)
					idList.Add(item.Id);

			return idList.ToArray();
		}

		/// <summary>Find all stored Uid's of a given type.</summary>
		/// <typeparam name="T">The type of (Data) object to collect the Id's for.</typeparam>
		/// <returns>An array of strings specifying the Id's of stored items whose data conforms to the specified Type.</returns>
		public string[] GetUids<T>() => GetUids(typeof(T));

		/// <summary>Retrieves an item with the specified id from the pool.</summary>
		/// <param name="id">The id of the item to retrieve.</param>
		/// <param name="removeAfter">If set removes the object from the pool after recovery.</param>
		/// <returns>Failure to find the requested item returns NULL, otherwise success returns the requested object.</returns>
		public dynamic RetrieveItem(string id, bool removeAfter = true)
		{
			dynamic result = null;
			if (this._pool.HasItem(id))
			{
				if (this._pool[id].MaxRetrieves > 0)
				{
					result = this._pool[id];
					if (removeAfter || (result.MaxRetrieves == 0)) 
						this._pool.Remove(id);
				}
			}
			return result;
		}

		/// <summary>
		/// Retrieves an item of the specified type from the pool. If the Id is not specified, retrieves the first found item,
		/// otherwise it searches for an object of the specified type with a matching Id. In all cases, failure returns NULL and
		/// success returns the requested object AND removes that object from the pool.
		/// </summary>
		/// <typeparam name="T">Specifies the Data type being searched for.</typeparam>
		/// <param name="removeAfter">If set to TRUE (default) the pool item is deleted after retrieval.</param>
		/// <param name="id">(Optional) Specifies the Id of the object being requested.</param>
		/// <returns>If an object matching the specified critera is found, that item, otherwise NULL.</returns>
		public T RetrieveItem<T>(bool removeAfter = true, string id = "")
		{
			string[] ids = GetUids<T>();
			if (string.IsNullOrEmpty(id.Trim()) || (ids.Length == 0))
				return (T)((ids.Length > 0) ? RetrieveItem(ids[0], removeAfter) : null);

			int i = -1; while ((++i < ids.Length) && !ids[i].Equals(id, StringComparison.OrdinalIgnoreCase)) ;
			return (T)((i < 0) ? null : RetrieveItem(ids[i], removeAfter));
		}

		/// <summary>Adds a new item to the Pool.</summary>
		/// <typeparam name="T">The Type of the data being stored.</typeparam>
		/// <param name="data">The actual data.</param>
		/// <param name="id">The unique Id to associate with the data.</param>
		/// <param name="xml">An optional XML string to correlate with it.</param>
		/// <param name="cmd">An optional string specifying a command string to associate with the new object.</param>
		/// <param name="maxRetrievals">An integeer specifying the number of times the data can be recovered.</param>
		/// <returns>The UID of the new shared memory item.</returns>
		public string AddItem<T>(T data, string id, UserInfo user, string xml = "", string cmd = "", int maxRetrievals = -1) where T : class
		{
			if ((data == null) || (maxRetrievals == 0)) return "{NoData}";

			PoolDataItem pdi = new PoolDataItem(id, data, user, typeof(T), cmd, maxRetrievals, xml);
			this._pool.Add(pdi);
			return pdi.Id;
		}

		/// <summary>Adds a new item to the Pool.</summary>
		/// <param name="sourceApplet">An Applet from which to extract the data.</param>
		/// <returns>The UID of the new shared memory item.</returns>
		public string AddItem(dynamic sourceApplet)
		{
			if ((sourceApplet.Data != null) && Applets.IsApplet(sourceApplet))
			{
				PoolDataItem pdi = sourceApplet.CreatePoolItem();
				if (pdi.MaxRetrieves > 0)
				{
					this._pool.Add(pdi);
					return pdi.Id;
				}
			}
			return "{NoData}";
		}

		/// <summary>Removes all expired items from the pool.</summary>
		public void Prune()
		{
			int i = 0;
			while (i < Count)
			{
				if (this._pool[i].Expired)
					this._pool.RemoveAt(i);
				else
					i++;
			}
		}

		/// <summary>Unceremoniously removes all existing shared data objects.</summary>
		public int Clear()
		{
			int i = Count;
			this._pool = new PoolData();
			return i;
		}

		/// <summary>Retrieves a non-volatile summary of diagnostic information about the contents of the shared pool.</summary>
		/// <returns>An array of PoolDataDiagItem objects relating to the contents of the pool.</returns>
		public PoolDataDiagItemCollection Diagnostics()
		{
			PoolDataDiagItemCollection items = new PoolDataDiagItemCollection();
			foreach (PoolDataItem pI in this._pool)
				items.Add(pI.Diagnostics());
			return items;
		}
		#endregion
	}
}
