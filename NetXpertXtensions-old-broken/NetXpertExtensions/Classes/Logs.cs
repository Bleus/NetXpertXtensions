using System.CodeDom;
using System.Collections;

namespace NetXpertExtensions.Classes
{
	public class BasicLogEntry
	{
		#region Properties
		protected DateTime _created = DateTime.Now;

		protected string _message = string.Empty;
		#endregion

		#region Constructors
		protected BasicLogEntry() { }

		public BasicLogEntry( string message ) =>
			this.Message = message;

		public BasicLogEntry( string message, DateTime created )
		{
			this.Created = created;
			this.Message = message;
		}

		public BasicLogEntry( KeyValuePair<DateTime,string> message)
		{
			this.Created = message.Key;
			this.Message = message.Value;
		}
		#endregion

		#region Accessors
		public DateTime Created
		{
			get => this._created;
			protected set => this._created = DateTime.Now.Min( value );
		}

		public string Message
		{
			get => string.IsNullOrWhiteSpace(this._message) ? string.Empty : this._message;
			protected set
			{
				if ( string.IsNullOrEmpty( value ) )
					throw new ArgumentException( "The log's message cannot be null, empty or white-space." );

				this._message = value;
			}
		}
		#endregion

		#region Operators
		public static implicit operator BasicLogEntry( string message ) => new( message );
		public static implicit operator BasicLogEntry( KeyValuePair<DateTime, string>? source ) => 
			source is null ? new() : new( ((KeyValuePair<DateTime, string>)source).Value, ((KeyValuePair<DateTime, string>)source).Key );
		public static implicit operator string( BasicLogEntry log ) => log is null ? string.Empty : log.Message;
		public static implicit operator DateTime( BasicLogEntry log ) => log is null ? DateTime.Now : log.Created;
		public static implicit operator KeyValuePair<DateTime,string>( BasicLogEntry log ) => 
			log is null ? new(DateTime.Now,"") : new(log.Created,log.Message);

		public static bool operator >(BasicLogEntry left, BasicLogEntry right)
		{
			if ( left is null || right is null ) return false;
			return left.Created > right.Created;
		}

		public static bool operator <(BasicLogEntry left, BasicLogEntry right) 
		{
			if ( left is null || right is null ) return false;
			return left.Created < right.Created;
		}

		public static bool operator >( BasicLogEntry left, DateTime right )
		{
			if ( left is null ) return false;
			return left.Created > right;
		}

		public static bool operator <( BasicLogEntry left, DateTime right )
		{
			if ( left is null ) return false;
			return left.Created < right;
		}

		public static bool operator ==( BasicLogEntry left, BasicLogEntry right)
		{
			if ( left is null ) return right is null;
			if ( right is null ) return false;
			return (left.Created == right.Created) && left.Message.Equals(right.Message,StringComparison.CurrentCultureIgnoreCase);
		}

		public static bool operator !=( BasicLogEntry left, BasicLogEntry right) => !(left == right);

		public static bool operator ==( BasicLogEntry left, DateTime right )
		{
			if ( left is null ) return false;
			return left.Created == right;
		}

		public static bool operator !=( BasicLogEntry left, DateTime right ) => !(left == right);

		public static bool operator ==( BasicLogEntry left, string right )
		{
			if ( left is null ) return string.IsNullOrEmpty(right);
			return left.Message.Equals( right, StringComparison.CurrentCultureIgnoreCase );
		}

		public static bool operator !=( BasicLogEntry left, string right ) => !(left == right);
		#endregion

		#region Methods
		public override string ToString() =>
			$"{this._created} -- \x22{this.Message}\x22";

		public string ToString( string format, IFormatProvider formatProvider ) =>
			$"{this._created.ToString( format, formatProvider )} -- \x22{this.Message}\x22";

		public string ToString( string format ) =>
			$"{this._created.ToString( format )} -- \x22{this.Message}\x22";

		public string ToString( IFormatProvider formatProvider ) =>
			$"{this._created.ToString( formatProvider )} -- \x22{this.Message}\x22";

		public override bool Equals( object? obj )
		{
			if ( obj is not null )
			{
				if ( obj.IsDerivedFrom<BasicLogEntry>() )
					return this.Created == (((BasicLogEntry)obj).Created) && ((BasicLogEntry)obj).Message.Equals( this.Message, StringComparison.CurrentCultureIgnoreCase );

				if ( obj.IsDerivedFrom<DateTime>() )
					return (DateTime)obj == this.Created;

				if ( obj.IsDerivedFrom<string>() )
					return this.Message.Equals( obj.ToString(), StringComparison.CurrentCultureIgnoreCase );
			}
			return false;
		}

		public override int GetHashCode() => base.GetHashCode();
		#endregion
	}

	public abstract class LogCollectionFoundation<T> : IEnumerable<T>, IEnumerator<T> where T : BasicLogEntry
	{
		#region Properties
		protected readonly List<T> _logs = new();
		private int _position = -1;
		private bool disposedValue;
		#endregion

		#region Constructors
		protected LogCollectionFoundation() { }

		protected LogCollectionFoundation( IEnumerable<T> logs ) => this.AddRange( logs );

		protected LogCollectionFoundation( params T[] logs ) => this.AddRange( logs );
		#endregion

		#region Accessors
		public int Count => this._logs.Count;

		public T Current => this._logs[this._position];

		object IEnumerator.Current => Current;

		T IEnumerator<T>.Current => this._logs[this._position];

		public T this[ int index ]
		{
			get => this._logs[ index ];
			set => this._logs[ index ] = value;
		}
		#endregion

		#region Methods
		public void Add( T log )
		{
			if ( log is not null )
			{
				int i = -1;
				while ( (++i < this._logs.Count) && (this._logs[ i ].Created < log.Created) );
				if ( i < this._logs.Count )
					this._logs.Insert( i, log );
				else
					this._logs.Add( log );

				//this._logs.Add( log );
				//this._logs.Sort( ( a, b ) => a > b ? 1 : -1 );
			}
		}

		public void Add( KeyValuePair<DateTime, string> log ) =>
			this.Add( (T)new BasicLogEntry( log ) );

		public void Add( string message, DateTime? when = null ) =>
			this.Add( (T)new BasicLogEntry( message, when is null ? DateTime.Now : (DateTime)when ) );

		public void AddRange( IEnumerable<T> logs )
		{
			foreach ( var l in logs ) this.Add( l );
		}

		public void AddRange( IEnumerable<string> messages )
		{
			foreach ( var m in messages ) this.Add( m );
		}

		public T? RemoveAt(int index)
		{
			if ( !index.InRange( Count ) ) 
				throw new IndexOutOfRangeException( $"'{index}' lies outside the bounds of this collection (0..{Count})." );

			T log = this[ index ];
			this._logs.RemoveAt( index );
			return log;
		}

		public void Clear() => this._logs.Clear();

		public T[] ToArray() => this._logs.ToArray();

		public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)this._logs).GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this._logs).GetEnumerator();

		public bool MoveNext() => this._position++ < Count;

		public void Reset() => this._position = 0;

		bool IEnumerator.MoveNext() => MoveNext();

		void IEnumerator.Reset() => Reset();
		#endregion

		#region IDisposable
		void Dispose( bool disposing )
		{
			if ( !disposedValue )
			{
				if ( disposing )
				{
					// TODO: dispose managed state (managed objects)
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				disposedValue = true;
			}
		}

		// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		// ~LogCollection()
		// {
		//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		//     Dispose(disposing: false);
		// }

		void IDisposable.Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose( disposing: true );
			GC.SuppressFinalize( this );
		}
		#endregion

		#region Operators
		public static implicit operator StringCollection( LogCollectionFoundation<T> logs )
		{
			StringCollection result = new();
			if ( logs is not null )
				result.AddRange( logs.ToArray(), false );

			return result;
		}
		#endregion
	}

	public class BasicLogCollection : LogCollectionFoundation<BasicLogEntry>
	{
		#region Constructors
		public BasicLogCollection() : base() { }

		public BasicLogCollection( IEnumerable<BasicLogEntry> logs ) : base( logs ) { }

		public BasicLogCollection( params BasicLogEntry[] logs ) : base( logs ) { }
		#endregion

		#region Operators
		public static implicit operator BasicLogCollection( StringCollection messages )
		{
			BasicLogCollection logs = new BasicLogCollection();
			if ( messages is not null )
				foreach ( string s in messages )
					logs.Add( s );

			return logs;
		}
		#endregion
	}
}
