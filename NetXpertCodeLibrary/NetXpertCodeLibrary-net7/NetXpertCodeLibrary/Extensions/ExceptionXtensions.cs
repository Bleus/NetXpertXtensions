using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetXpertCodeLibrary.Extensions
{
	public static partial class NetXpertExtensions
	{
		public static ExceptionStackTrace StackTraceDetails( this Exception e ) => new ExceptionStackTrace( e );
	}
}
