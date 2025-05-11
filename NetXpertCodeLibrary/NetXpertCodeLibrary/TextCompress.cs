using System.IO;
using System.IO.Compression;
using System.Text;

namespace NetXpertCodeLibrary
{
	/// <summary>Facilitates compressing and uncompressing raw text via GZipStream.</summary>
	/// <remarks>Code found on StackOverflow, written by "xanatos"</remarks>
	/// <see href="https://stackoverflow.com/questions/7343465/compression-decompression-string-with-c-sharp"/>
	public static class TextCompress
	{
		// Garnered from StackOverflow: 
		// https://stackoverflow.com/questions/7343465/compression-decompression-string-with-c-sharp

		/// <summary>Compresses a supplied string and returns the result as an array of bytes.</summary>
		public static byte[] Compress( string str ) =>
			Compress( Encoding.UTF8.GetBytes( str ) );

		/// <summary>Takes a supplied binary data set (as an array of bytes), compresses it, and returns the result as an array of bytes.</summary>
		public static byte[] Compress( byte[] data )
		{
			if ( data.Length > 0 )
			{
				using var msi = new MemoryStream( data );
				using var mso = new MemoryStream();
				using ( var gs = new GZipStream( mso, CompressionMode.Compress ) )
					msi.CopyTo( gs );

				return mso.ToArray();
			}

			return new byte[] { };
		}

		/// <summary>Uncompresses the supplied binary data and returns the result as a string.</summary>
		public static string TextUncompress( byte[] bytes ) =>
			Encoding.UTF8.GetString( BinaryUncompress( bytes ) );

		/// <summary>Uncompresses the supplied binary data and returns the result as a byte array (binary representation).</summary>
		public static byte[] BinaryUncompress( byte[] bytes )
        {
			if ( bytes.Length > 0 )
			{
				using MemoryStream msi = new MemoryStream( bytes );
					using MemoryStream mso = new MemoryStream();
						using ( GZipStream gs = new GZipStream( msi, CompressionMode.Decompress ) )
							gs.CopyTo( mso );

				return mso.ToArray();
			}

			return new byte[] { };
		}
    }
}
