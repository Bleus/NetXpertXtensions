using System.Collections;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace NetXpertExtensions
{
	#nullable disable

	public static partial class MiscellaneousExtensions
	{
		/// <summary>Provides a means to extract all column names from a row (handy for iterative processing of columns).</summary>
		/// <returns>An array containing all of the column names in the row.</returns>
		public static string[] GetNames(this DataGridViewCellCollection cells)
		{
			List<string> colNames = new();
			foreach (DataGridViewCell cell in cells)
				colNames.Add(cell.OwningColumn.Name);

			return colNames.ToArray();
		}

		/// <summary>Takes a <seealso cref="ulong"/> value and converts it to a formatted-string representation.</summary>
		/// <param name="useLongSuffix">Optional parameter that, if set to <b>TRUE</b> will cause the file size units to be full words, instead of abbreviations.</param>
		/// <returns>The supplied value, parsed into a formatted string.</returns>
		public static string FileSizeToString<T>( this T value, bool useLongSuffix = false ) where T : INumber<T>
		{
			ulong fileSize = Convert.ToUInt64(value);
			static Decimal Divide( ulong a, byte p ) => Decimal.Divide( (Decimal)a, (Decimal)Math.Pow( 2, p ) );

			string[] sfx =
				useLongSuffix ?
					new string[] { " bytes", " kilobytes", " megabytes", " gigabytes", "terabytes" } :
					new string[] { " B", " KB", " MB", " GB", "TB" };

			return fileSize switch
			{
				// 1.5TB+: Report as Terabytes with one decimal
				>= 0x19000000000 => Divide( fileSize, 40 ).ToString( "N1" ) + sfx[ 4 ], // 2^40 = 1,099,511,627,776 = 0x10000000000
																						// 1TB+: Report as Terabytes with two decimals
				>= 0x0F000000000 => Divide( fileSize, 40 ).ToString( "N2" ) + sfx[ 4 ], // 2^40 = 1,099,511,627,776 = 0x10000000000
																						// 100GB+: Report as Gigabytes with no decimals.
				>= 0x01900000000 => Divide( fileSize, 30 ).ToString( "N0" ) + sfx[ 3 ], // 2^30 = 1,073,741,824 = 0x40000000
																						// 10GB+: Report as Gigabytes with one decimal.
				>= 0x00280000000 => Divide( fileSize, 30 ).ToString( "N1" ) + sfx[ 3 ], // 2^30 = 1,073,741,824 = 0x40000000			};
																						// 2GB+ : Report as Gigabytes with two decimals.
				>= 0x00080000000 => Divide( fileSize, 30 ).ToString( "N2" ) + sfx[ 3 ], // 2^30 = 1,073,741,824 = 0x40000000
																						// Over 1GB: Report as Megabytes with no decimals.
				> 0x00040000000 => Math.Round( Divide( fileSize, 20 ) ).ToString( "N0" ) + sfx[ 2 ], // 2^20 = 1,048,576 = 0x100000
																									 // 100MB+: Report as Megabytes with one decimal.
				> 0x00006400000 => Divide( fileSize, 20 ).ToString( "N1" ) + sfx[ 2 ], // 2^20 = 1,048,576 = 0x100000
																					   // Over 2MB: Report as Megabytes with two decimals.
				> 0x00000200000 => Divide( fileSize, 20 ).ToString( "N2" ) + sfx[ 2 ], // 2^20 = 1,048,576 = 0x100000
																					   // Over 1MB: Report as Kilobytes with no decimals.
				> 0x00000100000 => Math.Round( Divide( fileSize, 10 ) ).ToString( "N0" ) + sfx[ 1 ], // 2^10 = 1024 = 0x400
																									 // Over 100kb: Report as Kilobytes with one decimal.
				> 0x00000019000 => Divide( fileSize, 10 ).ToString( "N1" ) + sfx[ 1 ], // 2^10 = 1024 = 0x400
																					   // Over 2kb: Report as Kilobytes with two decimals.
				> 0x00000000800 => Divide( fileSize, 10 ).ToString( "N2" ) + sfx[ 1 ], // 2^10 = 1024 = 0x400
																					   // Just report the number of bytes...
				_ => fileSize.ToString() + sfx[ 0 ],
			};
		}

		/// <summary>Extends the <seealso cref="TimeSpan"/> Class to add parsing into a variable-length string showing only the relevant information.</summary>
		/// <param name="showMs">If set to <b>TRUE</b>, causes the milliseconds value to be shown, otherwise it is ignored (Default: <b>FALSE</b>)</param>
		/// <param name="useLongWords">If set to <b>TRUE</b>, uses the full words for the result, otherwise just uses abbreviations.</param>
		/// <returns>A string showing the TimeSpan value broken out into days/hours/minutes/seconds/milliseconds as appropriate.</returns>
		public static string ToLongString(this TimeSpan value, bool showMs = false, bool useLongWords = false)
		{
			string[] sfx = useLongWords ? new string[] { " days", " hours", " minutes", " seconds", " milliseconds" } : new string[] { "d", "h", "m", "s", "ms" };
			string result = "";
			if (value.TotalHours > 24)
				result += value.Days.ToString() + sfx[0];

			if (value.Hours > 0)
				result += ((result.Length>0) ? " " : "") + value.Hours.ToString() + sfx[1];

			if (value.Minutes > 0)
				result += ((result.Length > 0) ? " " : "") + value.Minutes.ToString() + sfx[2];

			if (value.Seconds > 0)
				result += ((result.Length > 0) ? " " : "") + value.Seconds.ToString() + sfx[3];

			if ( (result.Length == 0) || (showMs && (value.Milliseconds > 0)) )
				result += ((result.Length > 0) ? " " : "") + value.Hours.ToString() + sfx[4];

			return result;
		}

		/// <summary>Takes a <seealso cref="TimeSpan"/> and presents its "total" value in an accurate and succinct way.</summary>
		public static string TotalToString(this TimeSpan value)
		{
			//if (value.TotalMilliseconds < 10)
			//	return value.TotalMilliseconds.ToString( "N2" ) + " ms";

			//if (value.TotalMilliseconds < 100)
			//	return value.TotalMilliseconds.ToString( "N1" ) + " ms";

			if (value.TotalMilliseconds < 1000)
				return value.TotalMilliseconds.ToString( "N0" ) + " ms";

			if (value.TotalSeconds < 10)
				return value.TotalSeconds.ToString( "N1" ) + " s";

			if (value.TotalSeconds < 60)
				return value.TotalSeconds.ToString( "N1" ) + " s";

			if (value.TotalSeconds < 150)
				return value.TotalSeconds.ToString( "N0" ) + " s";

			if (value.TotalMinutes < 15)
				return value.TotalMinutes.ToString( "N0" ).PadLeft( 3, ' ' ) + "m " +
					Math.Truncate( value.TotalSeconds % 60 ).ToString("N1").PadLeft( 4, '0' ) + " s";

			if (value.TotalMinutes < 60)
				return value.TotalMinutes.ToString( "N0" ).PadLeft( 3, ' ' ) + "m " +
					Math.Truncate( value.TotalSeconds % 60 ).ToString( "N0" ).PadLeft( 2, '0' ) + " s";

			if (value.TotalMinutes < 180)
				return value.TotalMinutes.ToString( "N0" ).PadLeft( 3, ' ' ) + "m";

			return value.TotalHours.ToString("N0") + "h " +
				(value.TotalMinutes % 60).ToString( "N0" ) + "m";
		}

		/// <summary>Facilitates quick and easy access to the EntryAssembly's Version information.</summary>
		/// <returns>The EntryAssembly's Version information.</returns>
		public static Version AssemblyVersion() => Assembly.GetEntryAssembly().GetName().Version;

		/// <summary>Adds a function to dereference just the base name portion of an Assembly from the FullName.</summary>
		/// <returns>The extracted base name if able, otherwise just the FullName.</returns>
		public static string ExtractName(this Assembly root)
		{
			string pattern = /* language=regex */ @"^(?<assy>(?:[a-zA-Z\d]+[.]?)+)(?>,).*$", work = root.FullName;
			MatchCollection matches = Regex.Matches(work, pattern, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
			if (matches.Count > 0)
				return matches[0].Groups["assy"].Value;

			if (work.IndexOf(",") > 3)
				return work.Substring(0, work.IndexOf(','));

			return root.FullName;
		}

		/// <summary>Extends the Object class to add the ability to test any object's ancestry.</summary>
		/// <typeparam name="T">The ancestor class to verify.</typeparam>
		/// <param name="type">The local object class</param>
		/// <returns><b>TRUE</b> if the calling object's class has <i>typeof(<typeparamref name="T"/>)</i> as an ancestor class.</returns>
		public static bool IsDerivedFrom<T>( this object obj ) => IsDerivedFromWorker<T>( obj.GetType() );

		/// <summary>Extends the Type class to add an ability to test the Class ancestry of the calling Type.</summary>
		/// <typeparam name="T">The ancestor class to look for.</typeparam>
		/// <returns>TRUE if the specified Class is an ancestor of the calling Type.</returns>
		public static bool HasAncestor<T>( this Type type ) => IsDerivedFromWorker<T>( type );

		/// <summary>Working portion of Object.IsDerivedFrom[T]() and Type.HasAncestor[T]() extension methods.</summary>
		private static bool IsDerivedFromWorker<T>( Type t ) => typeof(T) == t ||
			((t.BaseType == typeof(Object)) ? typeof(T)==typeof(Object) : (t.BaseType == typeof(T) ?  true : IsDerivedFromWorker<T>( t.BaseType )));

		/// <summary>Facilitates loading a text file from the specified EntryAssembly's Resources.</summary>
		/// <param name="name">The full name of the resource to retrieve (i.e. ApplicationNamespace.folderName.FileName.xml)</param>
		/// <returns>The requested file in a string.</returns>
		public static string FetchInternalResourceFile(string name)
		{
			var assembly = Assembly.GetEntryAssembly();
			using (Stream stream = assembly.GetManifestResourceStream(name))
			using (StreamReader reader = new(stream))
				return reader.ReadToEnd();
		}

		/// <summary>Facilitates loading a binary file from the specified EntryAssembly's Resources.</summary>
		/// <param name="name">The full name of the resource to retrieve (i.e. ApplicationNamespace.folderName.FileName.bin)</param>
		/// <returns>The contents of the requested file in a byte array.</returns>
		public static byte[] FetchInternalBinaryResourceFile( string name )
		{
			var assembly = Assembly.GetEntryAssembly();
			try
			{
				using ( Stream stream = assembly.GetManifestResourceStream( name ) )
				using ( MemoryStream reader = new() )
				{
					stream.CopyTo( reader );
					return reader.ToArray();
				}
			}
			catch ( Exception inner )
			{
				throw new FileNotFoundException( $"The requested resource (\"{name}\") was not found.", inner );
			}
		}

		/// <summary>Searches all loaded assemblies in a project for the specified embedded resource text file.</summary>
		/// <returns>If the specified resource is found, its contents as a string, otherwise throws a DllNotFoundException.</returns>
		/// <exception cref="DllNotFoundException"></exception>
		public static byte[] FetchAssemblyBinaryResourceFile( string name )
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			int i = -1; while ( (++i < assemblies.Length) && !Regex.IsMatch( name, $"^({assemblies[ i ].ExtractName()})", RegexOptions.IgnoreCase ) ) ;
			if ( i < assemblies.Length )
			{
				try
				{
					using ( Stream stream = assemblies[ i ].GetManifestResourceStream( name ) )
					using ( MemoryStream reader = new() )
					{
						stream.CopyTo( reader );
						return reader.ToArray();
					}
				}
				catch ( Exception inner )
				{
					throw new FileNotFoundException( $"The requested resource (\"{name}\") was not found.", inner );
				}
			}
			throw new DllNotFoundException( $"The requested assembly resource (\"{name}\") could not be found." );
		}

		/// <summary>Searches all loaded assemblies in a project for the specified embedded resource text file.</summary>
		/// <returns>If the specified resource is found, its contents as a string, otherwise throws a DllNotFoundException.</returns>
		/// <exception cref="DllNotFoundException"></exception>
		public static string FetchAssemblyResourceFile( string name )
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			int i = -1; while ( (++i < assemblies.Length) && !Regex.IsMatch( name, $"^({assemblies[ i ].ExtractName()})", RegexOptions.IgnoreCase ) ) ;
			if ( i < assemblies.Length )
			{
				try
				{
					using ( Stream stream = assemblies[ i ].GetManifestResourceStream( name ) )
					using ( StreamReader reader = new System.IO.StreamReader( stream, true ) )
						return reader.ReadToEnd();
				}
				catch ( Exception inner )
				{
					throw new FileNotFoundException( $"The requested resource (\"{name}\") was not found.", inner );
				}
			}
			throw new DllNotFoundException( $"The requested assembly resource (\"{name}\") could not be found." );
		}

		/// <summary>Searches all loaded assemblies in a project for the specified embedded resource text file.</summary>
		/// <param name="encoding">What <seealso cref="System.Text.Encoding"/> mechanism should be used when loading the file.</param>
		/// <returns>If the specified resource is found, its contents as a string, otherwise throws a DllNotFoundException.</returns>
		/// <remarks></remarks>
		/// <exception cref="DllNotFoundException"></exception>
		public static string FetchAssemblyResourceFile(string name, System.Text.Encoding encoding )
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			int i = -1; while ((++i < assemblies.Length) && !Regex.IsMatch(name, $"^({assemblies[i].ExtractName()})", RegexOptions.IgnoreCase)) ;
			if (i < assemblies.Length)
			{
				try {
					using (Stream stream = assemblies[i].GetManifestResourceStream(name))
					using (StreamReader reader = new(stream, encoding))
						return reader.ReadToEnd();
				}
				catch (Exception inner)
				{
					throw new FileNotFoundException( $"The requested resource (\"{name}\") was not found.", inner );
				}
			}
			throw new DllNotFoundException( $"The requested assembly resource (\"{name}\") could not be found." );
		}

		// "CobblestoneCommon.XmlFormating.xsl"
		//public static string FetchDllResourceFile(string name)
		//{
		//	string[] parts = name.Split(new char[] { '.' }, 2);
		//	if (parts.Length == 2)
		//	{
		//		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		//		int i = -1; while ((++i < assemblies.Length) && !Regex.IsMatch(assemblies[i].FullName, @"^" + parts[0] + @", .*$", RegexOptions.IgnoreCase) );
		//		if (i < assemblies.Length)
		//		{
		//			using (System.IO.Stream stream = assemblies[i].GetManifestResourceStream(name))
		//			using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
		//				return reader.ReadToEnd();
		//		}
		//		throw new DllNotFoundException("The requested DLL (\"" + parts[0] + "\") could not be found in the loaded Assemblies.");
		//	}
		//	throw new ArgumentException("The provided item name (\"" + name + "\") is not in properly dotted notation.");
		//}

		/// <summary>Returns the path to the current executable file.</summary>
		public static string LocalExecutablePath
		{
			get
			{
				string data = AppDomain.CurrentDomain.BaseDirectory; // Assembly.GetEntryAssembly().GetName().CodeBase;
				data = data.Substring(data.IndexOf("///")+3); // Get rid of https: file: etc headers...

				data = Path.GetDirectoryName(data);
				if (!data.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
					data += Path.DirectorySeparatorChar;

				return data;
			}
		}

		public static string ExecutableName => System.Diagnostics.Process.GetCurrentProcess().ProcessName;

		/// <summary>Provides a mechanism to quickly and easily fill an array with a specified value.</summary>
		/// <param name="value">The value to fill the array with.</param>
		/// <param name="size">The size of the array to return.</param>
		/// <param name="preserveContents">If set to TRUE, any existing contents of the original array are retained, otherwise they're overwritten.</param>
		/// <returns>An array of the specified type and length whose values have been set to the specified value.</returns>
		public static T[] Fill<T>(this T[] source, T value = default(T), int size = -1, bool preserveContents = false )
		{
			List<T> result = new();
			if ( size < 1 ) size = source.Length;
			if ( size > 0 )
			{
				if ( preserveContents ) result.AddRange( source );
				while (result.Count < size) result.Add( value );
			}
			while ( result.Count > size ) result.RemoveAt( result.Count - 1 );

			return result.ToArray();
		}

		/// <summary>Returns a new array consisting of the source array shortened to the specified "size".</summary>
		/// <param name="size">The maximum length of the returned array. If the source is shorter than this, the entire array is returned.</param>
		/// <returns>An array of maximum length "size" containing as many entries as will fit from the source array.</returns>
		/// <remarks>The resulting array WILL be shorter in length than "size" if there are insufficient records in the source array!</remarks>
		public static T[] Truncate<T>( this T[] source, int size ) =>
			size >= source.Length ? source: new List<T>( source ).ToArray( size );

		/// <summary>Returns a new Array&lt;T&gt; object containing the elements of the source Array in reverse order.</summary>
		/// <typeparam name="T">The datatype managed by the array.</typeparam>
		public static T[] ReverseOrder<T>( this T[] source )
		{
			if ( (source is null) || (source.Count() == 0) ) return Array.Empty<T>();
			T[] result = new T[ source.Length ];
			if ( source.Count() == 1 )
				result[ 0 ] = source[ 0 ];
			else
			{
				for ( int i = 0; i < source.Length; )
					result[ i++ ] = source[ source.Length - i ];
			}
			return result;
		}

		/// <summary>Copies a specified section out of an array into a new array.</summary>
		/// <param name="start">The index within the array to start copying from.<br/>If this value is negative, 
		/// the 'start' position is offset from the length of the array by the specified value.</param>
		/// <param name="size">If this value is positive, specifies the length of the subset to collect. If this value 
		/// is negative, it represents the distance from the end of the list at which to stop.</param>
		/// <returns>An array containing all of the records from the source within the specified scope.</returns>
		/// <remarks>If no other values are specified, this function defaults to starting at the beginning, and finishing at the end (i.e. it just copies the entire array).</remarks>
		public static T[] Copy<T>( this T[] source, int start = 0, int size = int.MaxValue)
		{
			if ( (start == 0) && (size >= source.Length) )
				return new List<T>(source).ToArray();

			if ( start < 0 ) start += source.Length;

			start = Math.Max( start, 0 ); // 'start' can never be less than zero!

			if ( start > source.Length ) // 'start' can never lie beyond the end of the array!
				throw new ArgumentOutOfRangeException( $"The specified 'start' value ({start}) exceeds the length of the array! ({source.Length})" );


			if ( size < 0 ) size = (size + source.Length) - start;
			if ( size + start > source.Length ) size = source.Length - start;

			T[] result = new T[size];
			if (size > 0) Array.Copy( source, start, result, 0, result.Length ); // result.Length == size

			return result;
		}

		/// <summary>
		/// Adds an option to the "List" collective class to facilitate returning an array of only the first "size" elsments
		/// from the list in the resulting array (rather than all of it).</summary>
		/// <typeparam name="T">The defined generic Type of the List.</typeparam>
		/// <param name="size">How many entries to return. If the size is bigger than the original list, all elements are returned.<br/><br/>
		/// <b>NOTE</b>: If this is a negative value, the returned elements will be taken from the <i>end</i> of <paramref name="source"/> instead.</param>
		/// <returns>An array of type T containing the lesser of all the items from the source list, or the number specified by "size".</returns>
		/// <remarks><b>NOTE</b>: If the <paramref name="size"/> value is negative, the returned array is taken from the <i>end</i> of <paramref name="source"/> instead.</remarks>
		public static T[] ToArray<T>( this List<T> source, int size )
		{
			if ( size == 0 ) return Array.Empty<T>();
			if ( Math.Abs( size ) >= source.Count ) return (size < 0 ? source.ToArray().Reverse() : source).ToArray();

			List<T> result = new();
			if ( size > 0 )
			{
				for ( int i = 0; i < size; ) result.Add( source[ i++ ] );
			}
			else
			{
				// Remember: if we're here, it means that 'size' is a NEGATIVE number!
				for ( int i = 0; i > size; ) result.Add( source[ source.Count - --i ] );
			}
			return result.ToArray();
		}

		/// <summary>
		/// Adds an option to the <seealso cref="List{T}"/> collective class to facilitate returning an array of only the 
		/// first <paramref name="size"/> elements from the list in the resulting array (rather than all of it).</summary>
		/// <typeparam name="T">The defined generic <seealso cref="Type"/> of the List.</typeparam>
		/// <param name="size">
		/// How many entries to return. If <paramref name="size"/> is lies beyond the end of the original collection, the 
		/// returned array will be padded with the value specified in <paramref name="fillValue"/>.
		/// </param>
		/// <param name="fillValue">The value to use for excess results if the requested array size is larger than the source list./// </param>
		/// <returns>An array of type T, of length <paramref name="size"/> containing the items from the source list and, padded 
		/// with <paramref name="fillValue"/> if required.</returns>
		public static T[] ToArray<T>( this List<T> source, int size, T fillValue) =>
			size > source.Count ? source.ToArray().Fill( fillValue, size, true ) : source.ToArray( size );

		/// <summary>Facilitates encoding a byte[] object into a Base64 encoded string.</summary>
		public static string ToBase64String( this byte[] source )
		{
			if ( (source is null) || (source.Length == 0) ) return "";

			long length = (long)(Math.Ceiling( ((4.0m / 3.0m) * (decimal)source.Length) / 4.0m ) * 4.0m);
			char[] result = new char[length]; // [ (length % 4) > 0 ? 4 - (length % 4) : 0 ];
			Convert.ToBase64CharArray( source, 0, source.Length, result, 0 );
			return new string( result );
		}

		/// <summary>Facilitates converting a Base64 encoded string into a byte array.</summary>
		public static byte[] FromBase64String( this string source ) =>
			Convert.FromBase64String( source );

		private class GenericComparer<T> : IComparer<T>
		{
			public int Compare( T left, T right ) => left.Equals( right ) ? 0 : 1;
		}

		public static bool Contains<T>( this T[] source, T value, IComparer<T> comparer = null )
		{
			int i = -1;
			while ( (++i < source.Length) && (comparer.Compare( source[ i ], value ) == 0) ) ;

			return i < source.Length;
		}

		public static bool Contains<T,U>( this T source, U value, IComparer<U> comparer = null) where T : IEnumerable<U>
		{
			comparer ??= new GenericComparer<U>();
			foreach ( var v in source )
				if ( comparer.Compare( v, value ) == 1 ) return true;

			return false;
		}

		/// <summary>
		/// Facilitates invoking a control from a separate thread.<br/><br/>
		/// <example><b>Example</b>:<code>controlName.Invoke( t => t.text = "A" );</code></example>
		/// </summary>
		/// <param name="del">A function delegate to execute against the control..</param>
		/// <remarks>Source reference: <seealso cref="https://stackoverflow.com/a/36983936/1542024"/></remarks>
		public static void Invoke<TControlType>( this TControlType control, Action<TControlType> del )
				where TControlType : Control
		{
			if ( control.InvokeRequired )
				control.Invoke( new Action( () => del( control ) ) );
			else
				del( control );
		}

		[DllImport( "user32.dll", SetLastError = true )]
		[return: MarshalAs( UnmanagedType.Bool )]
		private static extern bool GetWindowPlacement( IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl );

		private struct WINDOWPLACEMENT
		{
			public int length;
			public int flags;
			public int showCmd;
			public System.Drawing.Point ptMinPosition;
			public System.Drawing.Point ptMaxPosition;
			public System.Drawing.Rectangle rcNormalPosition;
		}

		/// <summary>Correctly restores a minimized window (Form element) to it's proper pre-minimized state.</summary>
		/// <param name="performUnminimize">If set to FALSE, just the proper FormWindowState is returned and no action is taken.</param>
		/// <returns>The un-minimized FormWindowState for the window.</returns>
		public static FormWindowState Unminimize( this Form form, bool performUnminimize = true )
		{
			const int WPF_RESTORETOMAXIMIZED = 0x2;

			FormWindowState result = form.WindowState;
			WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
			placement.length = Marshal.SizeOf( placement );
			GetWindowPlacement( form.Handle, ref placement );

			result =(placement.flags & WPF_RESTORETOMAXIMIZED) == WPF_RESTORETOMAXIMIZED ? FormWindowState.Maximized : FormWindowState.Normal;

			if ( performUnminimize ) form.WindowState = result;

			return result;
		}

		/// <summary>Given a string, endeavors to validate it as a Windows path+filename.</summary>
		/// <param name="fullFilePathandName">The value to validate.</param>
		/// <param name="checkExists">If set to true, also checks to see if the file actually exists.</param>
		/// <returns><b>TRUE</b> if the supplied string meets the specification for a valid Windows path + filename. 
		/// If the optional <i>checkExists</i> flag is set, the file must also exist.</returns>
		public static bool ValidateWindowsFilePath( string fullFilePathandName, bool checkExists = false )
		{
			if ( !string.IsNullOrWhiteSpace( fullFilePathandName ) && (fullFilePathandName.IndexOfAny(Path.GetInvalidPathChars()) < 0) )
			{
				// Basic pattern check; will incorrectly validate paths with multiple periods, and rejects paths with '+' signs...
				Match m = Regex.Match( fullFilePathandName, @"^(?<drive>[a-z]:)?(?<path>(?:[\\]?(?:[\w !#()-]+|[.]{1,2})+)*[\\])?(?<filename>(?:[.]?[\w !#()-]+)+)?[.]?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking, TimeSpan.FromSeconds(10) );
				if ( m.Success )
				{
					// Though it's technically valid to do so, I prefer to reject filenames with grouped periods...
					if ( m.Groups[ "filename" ].Success && Regex.IsMatch( m.Groups[ "filename" ].Value, @"[.]{2,}" ) ) return false;

					try { return !checkExists || File.Exists( fullFilePathandName ); }
					catch { }
				}
			}
			return false;
		}

		/// <summary>Facilitates directly modifying a <seealso cref="Color"/>.</summary>
		/// <param name="red">The amount by which to modify the Red Attribute.</param>
		/// <param name="green">The amount by which to modify the Green Attribute.</param>
		/// <param name="blue">The amount by which to modify the Blue Attribute.</param>
		/// <param name="alpha">The amount by which to modify the Alpha channel Attribute; by default the Alpha channel is left unchanged..</param>
		/// <returns>A new <seealso cref="Color"/> object representing the calling color modified by the provided values.</returns>
		public static Color Modify( this Color color, int red, int green, int blue, int alpha = 0 )
		{
			return Color.FromArgb(
				(byte)(color.A + alpha).LimitToRange( 0, 255 ),
				(byte)(color.R + red).LimitToRange( 0, 255 ),
				(byte)(color.G + green).LimitToRange( 0, 255 ),
				(byte)(color.B + blue).LimitToRange( 0, 255 )
			);
		}

		/// <summary>Facilitates directly modifying a <seealso cref="Color"/>'s channel values by a fixed amount.</summary>
		/// <param name="red">The amount by which to modify each of the Attributes.</param>
		/// <param name="value">The amount by which each channel will be modified.</param>
		/// <param name="alpha">The amount by which to modify the Alpha channel Attribute; by default the Alpha channel is left unchanged,
		/// (i.e. '0') but if <i>Null</i> is passed, <paramref name="value"/> is passed instead.</param>
		/// <returns>A new <seealso cref="Color"/> object representing the calling color modified by the provided values.</returns>
		public static Color Modify( this Color color, int value, int? alpha = 0 ) =>
			color.Modify( value, value, value, alpha is null ? value : (int)alpha );

		/// <summary>Derives a <seealso cref="Color"/> object by averaging two others' attributes.</summary>
		/// <returns>A <seealso cref="Color"/> object that represents the average of the calling object and a provided one.</returns>
		public static Color BlendWith( this Color color, Color with )
		{
			int a = (int)color.A + (int)with.A,
				r = (int)color.R + (int)with.R,
				g = (int)color.G + (int)with.G,
				b = (int)color.B + (int)with.B;

			return Color.FromArgb( a / 2, r / 2, g / 2, b / 2 );
		}

		public static bool IsNumericType( this object o ) => o is not null && o.GetType().IsNumericType();

		public static bool IsNumericType( this Type t ) =>
			t is not null && 
			(
				t == typeof( int ) ||
				t == typeof( float ) ||
				t == typeof( double ) ||
				t == typeof( decimal ) ||
				t == typeof( uint ) ||
				t == typeof( long ) ||
				t == typeof( ulong ) ||
				t == typeof( short ) ||
				t == typeof( ushort ) ||
				t == typeof( byte ) ||
				t == typeof( Int128 ) || 
				t == typeof( UInt128 ) ||
				t == typeof( sbyte )
			);
	}
}
