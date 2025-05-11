using System.Net.Http.Headers;
using System.Runtime.CompilerServices;

namespace NetXpertExtensions.Classes
{
	/// <summary>Implements a semi-thread-safe wrapper to facilitate submitting simple requests to a webserver and returning the results.</summary>
	/// <remarks>
	/// Public Static Methods:<br/>
	/// <seealso cref="Get(string, bool)"/> - Takes a string containing the URL to query and a boolean value to specify whether exceptions should be quietly captured or allowed to occur.<br/>
	/// Returns a string containing the raw text of the webserver's response.<br/>
	/// <seealso cref="Get(Uri, bool)"/> - Takes a <seealso cref="Uri"/> object containing the URL to query and a boolean value to specify whether exceptions should be quietly captured or allowed to occur.<br/>
	/// Returns a string containing the raw text of the webserver's response.<br/>
	/// <seealso cref="BinGet(string)"/> - Takes a string containing a URL to query and returns the result as a binary (byte) array.<br/>
	/// <seealso cref="BinGet(Uri)"/> - Takes a <seealso cref="Uri"/> object containing a URL to query and returns the result as a binary (byte) array.<br/><br/>
	/// If <seealso cref="Exception"/> suppression is used, and one occurs, the returned string will contain the <seealso cref="Exception.Message"/>.
	/// Exception capture/suppression is <i>not</i> supported on <b><u>BinGet</u></b> calls because there's no meaningful way to encapsulate the error in a byte array.
	/// </remarks>
	public static class Http
	{
		#region Accessors
		private static HttpClient? _client { get; set; } = null; // = new HttpClient() { Timeout = TimeSpan.FromMilliseconds( 10000 ) };

		private static object _lock = new();

		public static int Timeout
		{
			get { lock ( _lock ) { return _client is null ? -1 : (int)_client.Timeout.TotalMilliseconds; } }
			set
			{
				int t = Math.Max( 1000, Math.Min( 30000, value ) );
				lock ( _lock ) { Reset( TimeSpan.FromMilliseconds( t ) ); }
			}
		}
		#endregion

		#region Client Kludges
		/// <summary>Used to retrieve webserver data via simple <b>HTTP GET</b> requests.</summary>
		/// <param name="url">A string containing the complete web <b>URL</b> to submit.</param>
		/// <returns>Whatever <seealso cref="HttpClient"/> receives after attempting the supplied query (as a <seealso cref="string"/>).</returns>
		/// <exception cref="InvalidOperationException">Returned if the supplied <i>url</i> string is null, empty or whitespace.</exception>
		private static string HttpClientKludge1a( string url )
		{
			if ( string.IsNullOrWhiteSpace( url ) )
				throw new InvalidOperationException( "You must supply a url to interrogate for this function to work." );

			Uri uri;
			try { uri = new Uri( url ); }
			catch ( UriFormatException e ) { return $"{e.Message}\r\n{url}"; }

			return HttpClientKludge1Worker( uri ).Result;
		}

		/// <summary>Used to retrieve webserver data via simple <b>HTTP GET</b> requests.</summary>
		/// <param name="url">A <seealso cref="Uri"/> object specifying the web query to perform.</param>
		/// <returns>Whatever <seealso cref="HttpClient"/> receives after attempting the supplied query (as a <seealso cref="string"/>).</returns>
		private static string HttpClientKludge1b( Uri url ) { lock ( _lock ) { return HttpClientKludge1Worker( url ).Result; } }

		/// <summary>Used to retrieve webserver data via simple <b>HTTP GET</b> requests.</summary>
		/// <param name="url">A <seealso cref="Uri"/> object specifying the web query to perform.</param>
		/// <returns>Whatever <seealso cref="HttpClient"/> receives after attempting the supplied query (as a <seealso cref="string"/>).</returns>
		private static async Task<string> HttpClientKludge1Worker( Uri url )
		{
			if ( _client is null ) Reset();
			return await _client.GetStringAsync( url ).ConfigureAwait( false );
		}

		/// <summary>Used to retrieve binary webserver data via simple <b>HTTP GET</b> requests.</summary>
		/// <param name="url">A string containing the complete web <b>URL</b> to submit.</param>
		/// <returns>A byte array containing whatever <seealso cref="HttpClient"/> receives after attempting the supplied query (as a <seealso cref="byte"/> array).</returns>
		/// <exception cref="InvalidOperationException">Thrown if the supplied url is null, empty or whitespace.</exception>
		private static byte[] HttpClientKludge2a( string url )
		{
			if ( string.IsNullOrWhiteSpace( url ) )
				throw new InvalidOperationException( "You must supply a url to interrogate for this function to work." );

			Uri uri = new( url );

			return HttpClientKludge2Worker( uri ).Result;
		}

		/// <summary>Used to retrieve binary webserver data via simple <b>HTTP GET</b> requests.</summary>
		/// <param name="url">A <seealso cref="Uri"/> object specifying the web query to perform.</param>
		/// <returns>A byte array containing whatever <seealso cref="HttpClient"/> receives after attempting the supplied query (as a <seealso cref="byte"/> array).</returns>
		private static byte[] HttpClientKludge2b( Uri url ) => HttpClientKludge2Worker( url ).Result;

		/// <summary>Used to retrieve binary webserver data via simple <b>HTTP GET</b> requests.</summary>
		/// <param name="url">A <seealso cref="Uri"/> object specifying the web query to perform.</param>
		/// <returns>A byte array containing whatever <seealso cref="HttpClient"/> receives after attempting the supplied query (as a <seealso cref="byte"/> array).</returns>
		private static async Task<byte[]> HttpClientKludge2Worker( Uri url )
		{
			if ( _client is null ) Reset();

			HttpResponseMessage response;
			lock ( _lock ) { response = _client.GetAsync( url ).Result; }
			return await response.Content.ReadAsByteArrayAsync(); 
		}

		// ******************************************************************************************

		/// <summary>Submits data to a specified web resource via <b>HTTP POST</b>.</summary>
		/// <param name="url">A <seealso cref="Uri"/> object specifying the web resource to which the data will be submitted.</param>
		/// <param name="data">A <seealso cref="HttpContent"/> object containing the data to submit.</param>
		/// <returns>Whatever <seealso cref="HttpClient"/> receives after attempting the supplied operation (as a <seealso cref="string"/>).</returns>
		private static async Task<string> HttpClientKludge3( Uri url, HttpContent data )
		{
			if ( _client is null ) Reset();
			HttpResponseMessage result;
			lock ( _lock ) { result = _client.PostAsync( url, data ).Result; }
			return await result.Content.ReadAsStringAsync();
		}
		#endregion

		#region GET functions
		/// <summary>Attempts to interrogate a website via the supplied URL and stores the result in a <i>string</i>.</summary>
		/// <param name="url">A string containing a fully-formed, proper URL to retrieve.</param>
		/// <param name="captureExceptions">If <b>TRUE</b>, any Exceptions generated by the operation will be suppressed with their Message returned as the result string, otherwise they're thrown normally.</param>
		/// <returns>
		/// A <seealso cref="string"/> containing the raw data received from the specified server.<br/>
		/// If <seealso cref="Exception"/> suppression is active, and one is raised by this call, the relevant <seealso cref="Exception.Message"/>
		/// will be returned instead.
		/// </returns>
		/// <remarks>If the desired data is binary (i.e. images, multi-media, compressed files etc.), use <seealso cref="BinGet(string)"/> instead!</remarks>
		public static string Get( string url, bool captureExceptions = true )
		{
			string result;
			try { result = HttpClientKludge1a( url ); }
			catch ( AggregateException e )
			{
				if ( !captureExceptions ) throw;
				result = e.InnerException is null ? e.Message : e.InnerException.Message;
			}
			return result;
		}

		/// <summary>Attempts to interrogate a website via the supplied URL and stores the result in a <i>string</i>.</summary>
		/// <param name="url">A <seealso cref="Uri"/> object containing the fully-formed, proper URL to dereference.</param>
		/// <param name="captureExceptions">If <b>TRUE</b>, any <seealso cref="Exception"/>s generated by the operation will be 
		/// suppressed with their Message returned as the result string, otherwise they're thrown normally.</param>
		/// <returns>
		/// A <seealso cref="string"/> containing the raw data received from the specified server.<br/>
		/// If <seealso cref="Exception"/> suppression is active, and one is raised by this call, the relevant <seealso cref="Exception.Message"/>
		/// will be returned instead.
		/// </returns>
		/// <remarks>If the desired data is binary (i.e. images, multi-media, compressed files etc.), use <seealso cref="BinGet(Uri)"/> instead!</remarks>
		public static string Get( Uri url, bool captureExceptions = true )
		{
			string result;
			try { result = HttpClientKludge1b( url ); }
			catch ( AggregateException e )
			{
				if ( !captureExceptions ) { throw; }
				result = e.InnerException is null ? e.Message : e.InnerException.Message;
			}
			catch { throw; }
			return result;
		}

		/// <summary>Attempts to interrogate a website via the supplied URL and stores the result in a <i>byte array</i>.</summary>
		/// <param name="url">A <seealso cref="Uri"/> object containing the fully-formed, proper URL to dereference.</param>
		/// <returns>The data returned after submitting the request, in/as a <i>byte array</i>.</returns>
		/// <remarks>If the expected/desired data is plain text based (i.e. text, html. xml. javascript, css etc.), use <seealso cref="Get(Uri,bool)"/> instead!</remarks>
		// NOTE: There's no point in capturing exceptions here as the Message can't be reasonably returned as a byte array anyway...
		public static byte[] BinGet( Uri url ) => HttpClientKludge2b( url );

		/// <summary>Attempts to interrogate a website via the supplied URL and stores the result in a <i>byte array</i>.</summary>
		/// <param name="url">A <seealso cref="string"/> object containing the fully-formed, proper URL to dereference.</param>
		/// <returns>The data returned after submitting the request, in/as a <i>byte array</i>.</returns>
		/// <remarks>If the expected/desired data is plain text based (i.e. text, html. xml. javascript, css etc.), use <seealso cref="Get(string,bool)"/> instead!</remarks>
		// NOTE: There's no point in capturing exceptions here as the Message can't be reasonably returned as a byte array anyway...
		public static byte[] BinGet( string url ) => HttpClientKludge2a( url );
		#endregion

		#region Post functions
		private static string Post( Uri uri, HttpContent content, bool captureExceptions = true )
		{
			string result;
			try { result = HttpClientKludge3( uri, content ).Result; }
			catch ( AggregateException e )
			{
				if ( !captureExceptions ) throw;
				result = e.InnerException is null ? e.Message : e.InnerException.Message;
			}
			return result;
		}

		public static string Post( Uri uri, Dictionary<string, string> data, bool captureExceptions = true)
		{
			MultipartFormDataContent content = new();
			foreach ( var d in data )
			{
				var httpContent = new StringContent( d.Value );
				httpContent.Headers.Add( "Content-Type", "text/plain; charset=utf-8" );
				content.Add( httpContent, d.Key );
			}

			return Post( uri, content, captureExceptions );
		}

		public static string Post( Uri uri, Dictionary<string, byte[]> data, bool captureExceptions = true )
		{
			MultipartFormDataContent content = new();
			foreach ( var d in data )
			{
				var httpContent = new ByteArrayContent( d.Value );
				httpContent.Headers.Add( "Content-Type", "application/octet-stream" );
				httpContent.Headers.ContentLength = d.Value.Length;
				httpContent.Headers.ContentType = 
					new MediaTypeHeaderValue( "application/octet-stream" );

				content.Add( httpContent, d.Key );
				//content.Headers.ContentLength = d.Value.Length;
			}

			return Post( uri, content, captureExceptions );
		}

		public static string Post( Uri uri, KeyValuePair<string, byte[]> data, bool captureExceptions = true ) =>
			Post(uri, new Dictionary<string, byte[]>(new[] { data } ), captureExceptions );

		public static string Post( Uri uri, KeyValuePair<string, string> data, bool captureExceptions = true ) =>
			Post( uri, new Dictionary<string, string>( new[] { data } ), captureExceptions );

		public static string Post( Uri uri, string key, string value ) =>
			Post( uri, new KeyValuePair<string,string>(key,value ) );

		public static string Post( Uri uri, string key, byte[] value ) =>
			Post( uri, new KeyValuePair<string, byte[]>( key, value ) );

		//public static string Post( string url, Dictionary<string, string> values, bool captureExceptions = true )
		//{
		//	if ( (values is null) || (values.Count == 0) ) return "";

		//	var data = new FormUrlEncodedContent( values );
		//	string result;
		//	try { result = HttpClientKludge2( url, data ).Result; }
		//	catch ( AggregateException e )
		//	{
		//		if ( !captureExceptions ) throw;
		//		result = e.InnerException is null ? e.Message : e.InnerException.Message;
		//	}
		//	return result;
		//}

		public static string Post( string url, KeyValuePair<string, string> data, bool captureExceptions = true ) =>
			Post( new Uri(url), data, captureExceptions );

		public static string Post( string url, string fieldName, string fieldValue, bool captureExceptions = true ) =>
			Post( new Uri(url), new KeyValuePair<string, string>(fieldName, fieldValue), captureExceptions );
		#endregion

		/// <summary>Disposes of the existing <seealso cref="HttpClient"/> object and re-initializes this class with a new one using the specified <seealso cref="TimeSpan"/> for the default Timeout..</summary>
		/// <param name="timeout">Specifies the timeout value to assign to the new <seealso cref="HttpClient"/> object.</param>
		public static void Reset( TimeSpan timeout )
		{
			if ( _client is not null )
			{
				_client.CancelPendingRequests();
				_client.Dispose();
			}
			_client = new HttpClient() { Timeout = timeout };
			_client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue() { NoCache = true };
		}

		/// <summary>Disposes of the existing <seealso cref="HttpClient"/> object and re-initializes this class with a new one.</summary>
		public static void Reset() => Reset( _client is null ? TimeSpan.FromMilliseconds(10000) : _client.Timeout );
	}
}