using System.IO;
using static System.Net.Mime.MediaTypeNames;

namespace NetXpertExtensions
{
	public static partial class NetXpertExtensions
	{
		public static string MimeType( this FileInfo source )
		{
			string ext = source.Extension.ToLowerInvariant().TrimStart('.');
			switch ( ext )
			{
				case "arc":
					return "application/x-freearc";
				case "asp":
				case "css":
				case "htm":
				case "html":
				case "php":
				//case ".xml": moved to 'application/xml'
				case "csv":
					return $"text/{ext}";
				case "js":
					return "text/javascript";
				case "txt":
				case "text":
					return "text/plain";
				case "bmp":
				case "gif":
				case "jpg":
				case "jpeg":
				case "png":
				case "tif":
				case "tiff":
				case "webp":
					return $"image/{ext}";
				case "ico":
					return "image/x-icon";
				case "gz":
				case "zip":
					return "application/x-compressed";
				case "gtar":
				case "gzip":
					return $"application/x-{ext}";
				case "xls":
				case "xlsx":
					return "application/excel";
				case "doc":
				case "docx":
					return "application/msword";
				case "ppt":
				case "pptx":
					return "application/ms-powerpoint";
				case "vsd":
					return "application/visio";
				case "avi":
					return "video/avi";
				case "mp4":
				case "mpg":
				case "mpeg":
					return "video/mpeg";
				case "mkv":
					return "video/x-matroska";
				case "aac":
				case "mp3":
				case "ogg":
				case "opus":
				case "weba":
				case "wav":
					return $"audio/{ext}";
				case "pdf":
				case "json":
				case "rtf":
				case "xml":
					return $"application/{ext}";
				case "ttf":
				case "woff":
					return $"font/{ext}";
				default: break;
			}
			return "application/octet-stream";
		}
	}
}
