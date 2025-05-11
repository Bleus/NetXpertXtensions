using System;
using System.Collections.Generic;
using NetXpertExtensions;

namespace NetXpertCodeLibrary.ConsoleFunctions
{
	public delegate void ConsoleCancelEventHandler( object sender, ref ConsoleCancelEventArgs e );

	public class ConsoleCancelEventCollection
	{
		#region Properties
		protected List<KeyValuePair<string, ConsoleCancelEventHandler>> _handlers =
			new List<KeyValuePair<string, ConsoleCancelEventHandler>>();
		#endregion

		#region Constructors
		public ConsoleCancelEventCollection() =>
			Console.CancelKeyPress += this.ProcessEvents;

		public ConsoleCancelEventCollection( string name, ConsoleCancelEventHandler handler )
		{
			this.Add( name, handler );
			Console.CancelKeyPress += this.ProcessEvents;
		}
		#endregion

		#region Accessors
		public int Count => _handlers.Count;

		public ConsoleCancelEventHandler this[ int index ] =>
			index.InRange( Count, 0, NetXpertExtensions.Classes.Range.BoundaryRule.Loop ) ? _handlers[ index ].Value : null;

		public ConsoleCancelEventHandler this[ string name ]
		{
			get
			{
				int i = IndexOf( name );
				return (i < 0) ? null : _handlers[ i ].Value;
			}
		}
		#endregion

		#region Methods
		protected int IndexOf( string name )
		{
			int i = -1;
			if ( !string.IsNullOrWhiteSpace( name ) )
				while ( (++i < Count) && !this._handlers[ i ].Key.Equals( name, StringComparison.OrdinalIgnoreCase ) ) ;
			return (i < Count) ? i : -1;
		}

		public void Add( string name, ConsoleCancelEventHandler handler ) =>
			this.Add( new KeyValuePair<string, ConsoleCancelEventHandler>( name, handler ) );

		///<summary>Adds a new handler to the collection.</summary>
		///<remarks>New handlers are added to the front of the collection and will be executed in LIFO precedence.</remarks>
		protected void Add( KeyValuePair<string, ConsoleCancelEventHandler> value )
		{
			if ( !string.IsNullOrWhiteSpace( value.Key ) )
			{
				int i = IndexOf( value.Key );
				if ( i < 0 )
					this._handlers.Insert( 0, value );
				else
					this._handlers[ i ] = value;
			}
		}

		public void Remove( string name )
		{
			if ( !string.IsNullOrWhiteSpace( name ) )
			{
				int i = IndexOf( name );
				if ( i >= 0 )
					this._handlers.RemoveAt( i );
			}
		}

		/// <summary>Attaches to the ConcoleCancelKeyPress event when this object is created.</summary>
		/// <remarks>Because new events are inserted at the front of the collection, this routine will 
		/// process them in reverse order (last-in-first-out)</remarks>
		public void ProcessEvents( object sender, ConsoleCancelEventArgs e )
		{
			if ( this.Count > 0 )
				for ( int i = 0; i < Count; i++ )
					this[ i ]( sender, ref e );

			//e.Cancel = true; // Prevent CTRL-C from terminating the application.
		}
		#endregion

		/// <summary>Adding this routine to the collection prevents CTRL-C from killing the entire application.</summary>
		public static void DefaultHandler( object sender, ref ConsoleCancelEventArgs e )
		{
			e.Cancel = true;
			Con.Tec( "{,rn}{F1}&laquo;-break-&raquo;{,rn} {,rn}" );
		}
	}
}
