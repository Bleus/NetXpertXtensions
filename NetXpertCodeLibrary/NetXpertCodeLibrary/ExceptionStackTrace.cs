using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace NetXpertCodeLibrary
{
	public sealed class ExceptionStackTraceLine
	{
		#region Properties
		private int _line = -1;
		private string _module = "";
		private string _procName = "";
		private string _procParams = "";
		#endregion

		#region Constructors
		public ExceptionStackTraceLine() { }

		public ExceptionStackTraceLine( string stackLine ) =>
			Parse( stackLine );
		#endregion

		#region Accessors
		public int AtLine => _line;

		public string Module => _module;

		public string Procedure => _procName;

		public string Parameters => _procParams;

		public string ModulePath =>
			Path.GetDirectoryName( Module );

		public string ModuleFile =>
			Path.GetFileName( Module );
		#endregion

		#region Methods
		public void Parse( string s )
		{
			/*
				at System.Windows.Forms.Clipboard.SetText(String text, TextDataFormat format)

				at CobblestoneBaseApplets.CMD_Connect.PerformWork() in E:\brett\Documents\Visual Studio 2015\Projects\Cobblestone\CobbleShell\Applets\CobblestoneBaseApplets\Applets\CMD_Connect.cs:line 156
				at NetXpertCodeLibrary.ConsoleFunctions.AppletFoundation.Main(Ranks asRank) in E:\brett\Documents\Visual Studio 2015\Projects\NetXpertXtensions\NetXpertCodeLibrary\NetXpertCodeLibrary\ConsoleFunctions\AppletFoundation.cs:line 1009
				at NetXpertCodeLibrary.ConsoleFunctions.AppletFoundation.Main(ArgumentCollection args, Ranks asRank) in E:\brett\Documents\Visual Studio 2015\Projects\NetXpertXtensions\NetXpertCodeLibrary\NetXpertCodeLibrary\ConsoleFunctions\AppletFoundation.cs:line 944
				at CallSite.Target(Closure , CallSite , Object , ArgumentCollection , Ranks )
				at System.Dynamic.UpdateDelegates.UpdateAndExecute3[T0,T1,T2,TRet](CallSite site, T0 arg0, T1 arg1, T2 arg2)
				at CallSite.Target(Closure , CallSite , Object , ArgumentCollection , Ranks )
				at NetXpertCodeLibrary.ConsoleFunctions.CommandLineInterface.CommandProcessor() in E:\brett\Documents\Visual Studio 2015\Projects\NetXpertXtensions\NetXpertCodeLibrary\NetXpertCodeLibrary\ConsoleFunctions\CommandLineInterface.cs:line 647	 
			*/

			if ( !string.IsNullOrWhiteSpace( s ) )
			{
				// Can't parse the function name and parameters together because of a Regex runaway recursion!
				Regex work = new Regex( @"^(?:[\s]*at[\s]+)(?<func>(?:[.]?[\w<\[\]>,]+)+)", RegexOptions.IgnoreCase );
				if ( work.IsMatch( s ) )
				{
					Match match = work.Match( s );
					if ( match.Groups[ "func" ].Success )
					{
						this._procName = match.Groups[ "func" ].Value;

						work = new Regex( @"(?:[\w]+)(?<params>[(][\w \[\]{}.,]*[)])(?:[\s]+in |$)", RegexOptions.IgnoreCase );
						if ( work.IsMatch( s ) )
						{
							match = work.Match( s );
							if ( match.Groups[ "params" ].Success ) this._procParams = match.Groups[ "params" ].Value;
						}
					}
				}

				work = new Regex( @"(?:[\s]+in[\s]+)(?<file>(?:[a-z]:)?[\w_ \\.\/:]+)(?::line (?<line>[\d]+))$" );
				if ( work.IsMatch( s ) )
				{
					Match match = work.Match( s );
					if ( match.Groups["file"].Success )
						this._module = match.Groups[ "file" ].Value;

					this._line = match.Groups[ "line" ].Success ? int.Parse( match.Groups[ "line" ].Value ) : -1;
				}
			}
		}

		public override string ToString() => $"{this._procName}{this._procParams}{(AtLine > 0 ? $" § {ModuleFile} Line: {AtLine}" : "")}";
		#endregion
	}

	public class ExceptionStackTrace : IEnumerator<ExceptionStackTraceLine>
	{
		int _position = 0;
		List<ExceptionStackTraceLine> _data = new List<ExceptionStackTraceLine>();

		#region Constructors
		public ExceptionStackTrace() { }

		public ExceptionStackTrace( Exception e ) =>
			Parse( e );
		#endregion

		#region Accessors
		public int Count =>
			_data.Count;

		public ExceptionStackTraceLine this[int index] => _data[ index ];

		ExceptionStackTraceLine IEnumerator<ExceptionStackTraceLine>.Current => this[ this._position ];

		object IEnumerator.Current => this[ this._position ];
		#endregion

		#region Methods
		public void Parse( Exception e )
		{
			string work = e.StackTrace;
			if ( !string.IsNullOrWhiteSpace( work ) )
			{
				string[] lines = work.Split( new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries ); // Regex.Split( e.StackTrace, @"^[\s]+at ", RegexOptions.Multiline );
				foreach ( string s in lines ) // .Split( new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries ) )
					this._data.Add( new ExceptionStackTraceLine( s ) );
			}
		}

		public ExceptionStackTraceLine[] ToArray() => _data.ToArray();
		#endregion

		#region IEnumerator Support
		public IEnumerator<ExceptionStackTraceLine> GetEnumerator() => this._data.GetEnumerator();

		bool IEnumerator.MoveNext() => (++this._position) < this.Count;

		void IEnumerator.Reset() => this._position = 0;

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose( bool disposing )
		{
			if ( !disposedValue )
			{
				if ( disposing )
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
}
