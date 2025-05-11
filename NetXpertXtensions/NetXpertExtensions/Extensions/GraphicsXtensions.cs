using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace NetXpertExtensions
{
	#nullable disable

	public static class GraphicsXtensions
	{
		public static GraphicsPath GenerateRoundedRectangle(
			this Graphics graphics,
			RectangleF rectangle,
			float radius )
		{
			float diameter;
			GraphicsPath path = new();
			if ( radius <= 0.0F )
			{
				path.AddRectangle( rectangle );
				path.CloseFigure();
				return path;
			}
			else
			{
				if ( radius >= (Math.Min( rectangle.Width, rectangle.Height )) / 2.0 )
					return graphics.GenerateCapsule( rectangle );
				diameter = radius * 2.0F;
				SizeF sizeF = new( diameter, diameter );
				RectangleF arc = new( rectangle.Location, sizeF );
				path.AddArc( arc, 180, 90 );
				arc.X = rectangle.Right - diameter;
				path.AddArc( arc, 270, 90 );
				arc.Y = rectangle.Bottom - diameter;
				path.AddArc( arc, 0, 90 );
				arc.X = rectangle.Left;
				path.AddArc( arc, 90, 90 );
				path.CloseFigure();
			}
			return path;
		}

		public static GraphicsPath GenerateCapsule(
			this Graphics graphics,
			RectangleF baseRect )
		{
			float diameter;
			RectangleF arc;
			GraphicsPath path = new();
			try
			{
				if ( baseRect.Width > baseRect.Height )
				{
					diameter = baseRect.Height;
					SizeF sizeF = new( diameter, diameter );
					arc = new RectangleF( baseRect.Location, sizeF );
					path.AddArc( arc, 90, 180 );
					arc.X = baseRect.Right - diameter;
					path.AddArc( arc, 270, 180 );
				}
				else if ( baseRect.Width < baseRect.Height )
				{
					diameter = baseRect.Width;
					SizeF sizeF = new( diameter, diameter );
					arc = new RectangleF( baseRect.Location, sizeF );
					path.AddArc( arc, 180, 180 );
					arc.Y = baseRect.Bottom - diameter;
					path.AddArc( arc, 0, 180 );
				}
				else path.AddEllipse( baseRect );
			}
			catch { path.AddEllipse( baseRect ); }
			finally { path.CloseFigure(); }
			return path;
		}

		/// <summary>
		/// Draws a rounded rectangle specified by a pair of coordinates, a width, a height and the radius
		/// for the arcs that make the rounded edges.
		/// </summary>
		/// <param name="pen">System.Drawing.Pen that determines the color, width and style of the rectangle.</param>
		/// <param name="x">The x-coordinate of the upper-left corner of the rectangle to draw.</param>
		/// <param name="y">The y-coordinate of the upper-left corner of the rectangle to draw.</param>
		/// <param name="width">Width of the rectangle to draw.</param>
		/// <param name="height">Height of the rectangle to draw.</param>
		/// <param name="radius">The radius of the arc used for the rounded edges.</param>
		public static void DrawRoundedRectangle(
			this Graphics graphics,
			Pen pen,
			float x,
			float y,
			float width,
			float height,
			float radius )
		{
			RectangleF rectangle = new( x, y, width, height );
			GraphicsPath path = graphics.GenerateRoundedRectangle( rectangle, radius );
			SmoothingMode old = graphics.SmoothingMode;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			graphics.DrawPath( pen, path );
			graphics.SmoothingMode = old;
		}

		/// <summary>
		/// Draws a rounded rectangle specified by a pair of coordinates, a width, a height and the radius
		/// for the arcs that make the rounded edges.
		/// </summary>
		/// <param name="pen">System.Drawing.Pen that determines the color, width and style of the rectangle.</param>
		/// <param name="x">The x-coordinate of the upper-left corner of the rectangle to draw.</param>
		/// <param name="y">The y-coordinate of the upper-left corner of the rectangle to draw.</param>
		/// <param name="width">Width of the rectangle to draw.</param>
		/// <param name="height">Height of the rectangle to draw.</param>
		/// <param name="radius">The radius of the arc used for the rounded edges.</param>
		public static void DrawRoundedRectangle(
			this Graphics graphics,
			Pen pen,
			int x,
			int y,
			int width,
			int height,
			int radius )
		{
			graphics.DrawRoundedRectangle(
				pen,
				Convert.ToSingle( x ),
				Convert.ToSingle( y ),
				Convert.ToSingle( width ),
				Convert.ToSingle( height ),
				Convert.ToSingle( radius ) );
		}

		/// <summary>
		/// Draws a rounded rectangle specified by a pair of coordinates, a width, a height and the radius
		/// for the arcs that make the rounded edges.
		/// </summary>
		/// <param name="pen">System.Drawing.Pen that determines the color, width and style of the rectangle.</param>
		/// <param name="rect">A Rectangle object specifying the location and size of the desired rounded rectangle.</param>
		/// <param name="radius">The radius of the arc used for the rounded edges.</param>
		public static void DrawRoundedRectangle( this Graphics graphics, Pen pen, Rectangle rect, int radius ) =>
			graphics.DrawRoundedRectangle( pen, rect.Location.X, rect.Location.Y, rect.Width, rect.Height, radius );

		/// <summary>
		/// Fills the interior of a rounded rectangle specified by a pair of coordinates, a width, a height
		/// and the radius for the arcs that make the rounded edges.
		/// </summary>
		/// <param name="brush">System.Drawing.Brush that determines the characteristics of the fill.</param>
		/// <param name="x">The x-coordinate of the upper-left corner of the rectangle to fill.</param>
		/// <param name="y">The y-coordinate of the upper-left corner of the rectangle to fill.</param>
		/// <param name="width">Width of the rectangle to fill.</param>
		/// <param name="height">Height of the rectangle to fill.</param>
		/// <param name="radius">The radius of the arc used for the rounded edges.</param>
		public static void FillRoundedRectangle(
			this Graphics graphics,
			Brush brush,
			float x,
			float y,
			float width,
			float height,
			float radius )
		{
			RectangleF rectangle = new( x, y, width, height );
			GraphicsPath path = graphics.GenerateRoundedRectangle( rectangle, radius );
			SmoothingMode old = graphics.SmoothingMode;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			graphics.FillPath( brush, path );
			graphics.SmoothingMode = old;
		}

		/// <summary>
		/// Fills the interior of a rounded rectangle specified by a pair of coordinates, a width, a height
		/// and the radius for the arcs that make the rounded edges.
		/// </summary>
		/// <param name="brush">System.Drawing.Brush that determines the characteristics of the fill.</param>
		/// <param name="x">The x-coordinate of the upper-left corner of the rectangle to fill.</param>
		/// <param name="y">The y-coordinate of the upper-left corner of the rectangle to fill.</param>
		/// <param name="width">Width of the rectangle to fill.</param>
		/// <param name="height">Height of the rectangle to fill.</param>
		/// <param name="radius">The radius of the arc used for the rounded edges.</param>
		public static void FillRoundedRectangle(
			this Graphics graphics,
			Brush brush,
			int x,
			int y,
			int width,
			int height,
			int radius )
		{
			graphics.FillRoundedRectangle(
				brush,
				Convert.ToSingle( x ),
				Convert.ToSingle( y ),
				Convert.ToSingle( width ),
				Convert.ToSingle( height ),
				Convert.ToSingle( radius ) );
		}

		/// <summary>
		/// Draws a rounded rectangle specified by a pair of coordinates, a width, a height and the radius
		/// for the arcs that make the rounded edges.
		/// </summary>
		/// <param name="pen">System.Drawing.Pen that determines the color, width and style of the rectangle.</param>
		/// <param name="rect">A Rectangle object specifying the location and size of the desired rounded rectangle.</param>
		/// <param name="radius">The radius of the arc used for the rounded edges.</param>
		public static void FillRoundedRectangle( this Graphics graphics, Brush brush, Rectangle rect, int radius ) =>
			graphics.FillRoundedRectangle( brush, rect.Location.X, rect.Location.Y, rect.Width, rect.Height, radius );

		/// <summary>Extends the Point class to provide a simple mechanism to create a new Point from an existing one modified by specified amounts.</summary>
		/// <param name="x">A value to add to the parent Point's X co-ordinate.</param>
		/// <param name="y">A value to add to the parent Point's Y co-ordinate.</param>
		/// <returns>A new Point object who's values represent those of the original modified by the specified values.</returns>
		public static Point Add( this Point point, int x, int y = 0 ) =>
			new( point.X + x, point.Y + y );

		/// <summary>Extends the Point class to provide a simple mechanism to create a new Point from an existing one modified by the values of a second specified Point.</summary>
		/// <param name="addPoint">A Point object whose X and Y coordinates will be added to the base object's values.</param>
		/// <returns>A new Point object who's values represent the sum of the original object's coordinates modified by those of the specified Point object.</returns>
		public static Point Add( this Point point, Point addPoint ) =>
			point.Add( addPoint.X, addPoint.Y );

		/// <summary>Extends the Point class to provide a simple mechanism to create a new Point from an existing one modified by the values of a second specified Point.</summary>
		/// <param name="size">A Size object whose width and height are added to the base Point object.</param>
		/// <returns>A new Point object representing the result of adding the supplied Size object to the calling Point object.</returns>
		public static Point Add( this Point point, Size size ) =>
			point.Add( size.Width, size.Height );

		/// <summary>Creates a derivative Point object containing the highest X and Y values between the base object and the provided values.</summary>
		/// <param name="x">An X value to compare against.</param>
		/// <param name="y">A Y value to compare against.</param>
		/// <returns>A derivative Point object containing the highest X and Y values between the base object and the provided values.</returns>
		public static Point Max( this Point point, int x, int y ) =>
			new( Math.Max( point.X, x ), Math.Max( point.Y, y ) );

		/// <summary>Creates a derivative Point object containing the highest X and Y values between the base object and the provided values.</summary>
		/// <param name="max">A Point object whose values will be compared against those of the base.</param>
		/// <returns>A derivative Point object containing the highest X and Y values between the base object and the provided values.</returns>
		public static Point Max( this Point point, Point max ) =>
			point.Max( max.X, max.Y );

		/// <summary>Creates a derivative Point object containing the lowest X and Y values between the base object and the provided values.</summary>
		/// <param name="x">An X value to compare against.</param>
		/// <param name="y">A Y value to compare against.</param>
		/// <returns>A derivative Point object containing the lowest X and Y values between the base object and the provided values.</returns>
		public static Point Min( this Point point, int x, int y ) =>
			point.Min( x, y );

		/// <summary>Creates a derivative Point object containing the lowest X and Y values between the base object and the provided values.</summary>
		/// <param name="max">A Point object whose values will be compared against those of the base.</param>
		/// <returns>A derivative Point object containing the lowest X and Y values between the base object and the provided values.</returns>
		public static Point Min( this Point point, Point min ) =>
			point.Min( min );

		/// <summary>Extends the Rectangle Class to provide a means to obtain the bottom-right corner location.</summary>
		/// <returns>A Point object containing the co-ordinates for the bottom-right corner of the Rectangle.</returns>
		public static Point End( this Rectangle area ) =>
			area.Location.Add( area.Size );

		/// <summary>Extends the Rectangle class to provide a mechanism to test if a given point resides within it.</summary>
		/// <param name="x">An x co-ordinate value to test.</param>
		/// <param name="y">A y co-ordinate value to test.</param>
		/// <returns>TRUE if the provided co-ordinates lie within the bounds of this Rectangle.</returns>
		public static bool Contains( this Rectangle area, int x, int y ) =>
			area.Contains( new Point( x, y ) );

		/// <summary>Extends the Rectangle class to provide a mechanism to test if a set of given points all reside within it.</summary>
		/// <param name="points">An array of Point objects to test.</param>
		/// <returns>TRUE if ALL supplied Points reside within the Rectangle.</returns>
		public static bool ContainsAll( this Rectangle area, Point[] points )
		{
			int i = -1; while ( (++i < points.Length) && area.Contains( points[ i ] ) ) ;
			return (i == points.Length);
		}

		/// <summary>Extends the Rectangle class to provide a mechanism to test if any one from set of given points resides within it.</summary>
		/// <param name="points">An array of Point objects to test.</param>
		/// <returns>TRUE if ANY of the supplied Points reside within the Rectangle.</returns>
		public static bool ContainsAny( this Rectangle area, Point[] points )
		{
			int i = -1; while ( (++i < points.Length) && !area.Contains( points[ i ] ) ) ;
			return (i < points.Length);
		}

		/// <summary>Creates a new Rectangle object from a Size object and a provided Point object.</summary>
		/// <param name="x">An int value specifying the X co-ordinate to use in the new Rectangle's location.</param>
		/// <param name="y">An int value specifying the Y co-ordinate to use in the new Rectangle's location.</param>
		/// <returns>A new Rectangle object with "location" as it's Point, and a Size derived from the calling object's values.</returns>
		public static Rectangle ToRectangle( this Size s, int x = 0, int y = 0 ) =>
				s.ToRectangle( new( x, y ) );

		/// <summary>Creates a new Rectangle object from a Size object and a provided Point object.</summary>
		/// <param name="location">A Point2 object to use for the new Rectangle's location values.</param>
		/// <returns>A new Rectangle object with "location" as it's Point, and a Size derived from the calling object's values.</returns>
		public static Rectangle ToRectangle( this Size s, Point location ) =>
			location.ToRectangle( s );

		/// <summary>Creates a new Rectangle object from a Point object using a provided Size value.</summary>
		/// <param name="size">A Size object specifying the width and height of the new Rectangle.</param>
		/// <returns>A new Rectangle object with the calling Point as the home location, and the Size defined by the supplied value.</returns>
		public static Rectangle ToRectangle( this Point p, Size size ) =>
			p.ToRectangle( size );

		/// <summary>Creates a new Rectangle object from a Point2 source using supplied width and height values.</summary>
		/// <param name="width">An int value specifying the width for the Rectangle.</param>
		/// <param name="height">An int value specifying the height for the Rectangle. Defaults to 1 if not provided.</param>
		/// <returns>A new Rectangle object with the calling Point2 as the home location, and the Size defined by the supplied values.</returns>
		public static Rectangle ToRectangle( this Point p, int width, int height = 1 ) =>
			new( p, new Size( width, height ) );

		/// <summary>Reports on whether another rectangle and this one overlap eachother.</summary>
		/// <param name="RectB">A Rectangle to test for overlap.</param>
		/// <returns>TRUE if any corner of either rectangle lies within the bounds of the other.</returns>
		public static bool Overlaps( this Rectangle RectA, Rectangle RectB ) =>
			(RectA.Left < RectB.Right) && (RectA.Right > RectB.Left) && (RectA.Top < RectB.Bottom) && (RectA.Bottom > RectB.Top) ||
			(RectB.Left < RectA.Right) && (RectB.Right > RectA.Left) && (RectB.Top < RectA.Bottom) && (RectB.Bottom > RectA.Top);

		/// <summary>Returns a new Size object comprised of the Maximum values taken from the source and the supplied "maxValue".</summary>
		/// <param name="maxValue">A Size object that specifies the maximum allowed Width and Height values.</param>
		public static Size Maximum( this Size source, Size maxValue ) => source.Maximum( maxValue.Width, maxValue.Height );

		/// <summary>Returns a new Size object comprised of the Maximum values taken from the source and the supplied "maxValue".</summary>
		/// <param name="width">The minimum width value to return.</param>
		/// <param name="height">The minimum width value to return.</param>
		public static Size Maximum( this Size source, int width, int height ) =>
			new( Math.Max( source.Width, width ), Math.Max( source.Height, height ) );

		/// <summary>Returns a new Size object comprised of the Minimum values taken from the source and the supplied "minValue".</summary>
		/// <param name="minValue">A Size object that specifies the minimum allowed Width and Height values.</param>
		public static Size Minimum( this Size source, Size minValue ) => source.Minimum( minValue.Width, minValue.Height );

		/// <summary>Returns a new Size object comprised of the Minimum values taken from the source and the supplied "minValue".</summary>
		/// <param name="width">The minimum width value to return.</param>
		/// <param name="height">The minimum width value to return.</param>
		public static Size Minimum( this Size source, int width, int height ) =>
			new( Math.Min( source.Width, width ), Math.Min( source.Height, height ) );

		/// <summary>Reports on whether a supplied pair of co-ordinates falls within the bounds of the calling Size object.</summary>
		/// <param name="point">The Point2 object to test.</param>
		/// <returns>TRUE if the provided point lies within the bounds of the parent Size object.</returns>
		public static bool Contains( this Size source, Point point ) =>
			new Rectangle( new Point( 0, 0 ), source ).Contains( point );

		/// <summary>Reports on whether a supplied pair of co-ordinates falls within the bounds of the calling Size object.</summary>
		/// <param name="point">The Point2 object to test.</param>
		/// <returns>TRUE if the provided point lies within the bounds of the parent Size object.</returns>
		public static bool Contains( this Size source, int x, int y ) =>
			new Rectangle( new Point( 0, 0 ), source ).Contains( new Point( x, y ) );

		/// <summary>Reports on whether every one of a supplied array of co-ordinates falls within the bounds of the calling Size object.</summary>
		/// <param name="point">An array of Point2 objects to test.</param>
		/// <returns>TRUE if ALL of the provided points lie within the bounds of the calling Size object.</returns>
		public static bool ContainsAll( this Size source, Point[] points )
		{
			bool result = true;
			if ( (points is not null) && (points.Length > 0) )
			{
				int i = -1; while ( (++i < points.Length) && result )
					result = source.Contains( points[ i ] );
			}
			return result;
		}

		/// <summary>Reports on whether any one of a supplied array of co-ordinates falls within the bounds of the calling Size object.</summary>
		/// <param name="point">An array of Point2 objects to test.</param>
		/// <returns>TRUE if ANY of the provided points lie within the bounds of the calling Size object.</returns>
		public static bool ContainsAny( this Size source, Point[] points )
		{
			bool result = false;
			if ( (points is not null) && (points.Length > 0) )
			{
				int i = -1; while ( (++i < points.Length) && !result )
					result = source.Contains( points[ i ] );
			}
			return result;
		}

		/// <summary>Produces a resized copy of the source Bitmap object.</summary>
		/// <param name="size">The required size of the new Bitmap.</param>
		/// <param name="withBrush">What fill (brush) to apply to areas of the new Bitmap that are not covered by the resized original.</param>
		/// <param name="maintainAspect">If TRUE, maintains the aspect ratio of the original image, otherwise the image is stretched to conform to the new size.</param>
		/// <remarks>NOTE: The default 'fill' brush is 'SolidBrush( Color.Transparent )'.</remarks>
		public static Bitmap ResizeTo( this Image source, Size size, Brush withBrush = null, bool maintainAspect = true )
		{
			size = new Size( Math.Abs( size.Width ), Math.Abs( size.Height ) );
			if ( withBrush is null ) withBrush = new SolidBrush( Color.Transparent );

			Bitmap result = new( size.Width, size.Height );
			using ( Graphics g = Graphics.FromImage( result ) )
			{
				g.InterpolationMode = InterpolationMode.High;
				g.CompositingQuality = CompositingQuality.HighQuality;
				g.SmoothingMode = SmoothingMode.AntiAlias;

				Size scale;
				if ( maintainAspect )
				{
					float scaleFactor = Math.Min( size.Width / source.Width, size.Height / source.Height );
					scale = new Size( (int)(source.Width * scaleFactor), (int)(source.Height * scaleFactor) );
				}
				else
				{
					SizeF scaleFactor = new( size.Width / source.Width, size.Height / source.Height );
					scale = new Size( (int)(source.Width * scaleFactor.Width), (int)(source.Height * scaleFactor.Height) );
				}

				g.FillRectangle( withBrush, new RectangleF( 0, 0, size.Width, size.Height ) );
				g.DrawImage( source, (size.Width - scale.Width) / 2, (size.Height - scale.Height) / 2, scale.Width, scale.Height );
			}
			return result;
		}

		/// <summary>Produces a resized copy of the source Bitmap object.</summary>
		/// <param name="width">The required width of the new Bitmap.</param>
		/// <param name="height">The required height of the new Bitmap.</param>
		/// <param name="withBrush">What fill (brush) to apply to areas of the new Bitmap that are not covered by the resized original.</param>
		/// <param name="maintainAspect">If TRUE, maintains the aspect ratio of the original image, otherwise the image is stretched to conform to the new size.</param>
		/// <remarks>NOTE: The default 'fill' brush is 'SolidBrush( Color.Transparent )'.</remarks>
		public static Bitmap ResizeTo( this Image source, int width, int height, Brush withBrush = null, bool maintainAspect = true ) =>
			source.ResizeTo( new Size( width, height ), withBrush, maintainAspect );

		/// <summary>Produces a resized copy of the source Bitmap object.</summary>
		/// <param name="width">The required width of the new Bitmap.</param>
		/// <param name="height">The required height of the new Bitmap.</param>
		/// <param name="fillColor">What fill color to use in the areas of the new Bitmap that are not covered by the resized image.</param>
		/// <param name="maintainAspect">If TRUE, maintains the aspect ratio of the original image, otherwise the image is stretched to conform to the new size.</param>
		/// <remarks>NOTE: The default 'fill' brush is 'SolidBrush( Color.Transparent )'.</remarks>
		public static Bitmap ResizeTo( this Image source, int width, int height, Color fillColor, bool maintainAspect = true ) =>
			source.ResizeTo( new Size( width, height ), new SolidBrush( fillColor ), maintainAspect );

		/// <summary>Returns the source Bitmap object as a byte array.</summary>
		public static byte[] ToByteArray( this Bitmap source ) =>
			(byte[])new ImageConverter().ConvertTo( source, typeof( byte[] ) );

		/// <summary>Facilitates creating a new Bitmap object from an existing byte array.</summary>
		public static Bitmap CreateBitmap( this byte[] rawImage, Size size )
		{
			Bitmap result = new( size.Width, size.Height );

			if ( (rawImage is not null) && (rawImage.Length > 0) && (size.Width > 0) && (size.Height > 0) )
				using ( var stream = new MemoryStream( rawImage ) ) result = new Bitmap( stream );

			return result;
		}

		/// <summary>Facilitates filling a bitmap with a specified brush.</summary>
		/// <param name="brush">A System.Drawing.Brush object that is used to fill the resulting bitmap.</param>
		/// <returns>A new bitmap object of the same size as the calling one, filled by the specified Brush.</returns>
		public static Bitmap Fill( this Bitmap source, Brush brush )
		{
			Bitmap result = new( source.Width, source.Height );
			Graphics g = Graphics.FromImage( result );
			g.FillRectangle( brush, 0, 0, source.Width, source.Height );
			return result;
		}

		/// <summary>Facilitates filling a bitmap filled with a specified color (SolidBrush).</summary>
		/// <param name="color">A Color value that fills the resulting bitmap.</param>
		/// <returns>A new bitmap object of the same size as the calling one, filled by the specified Color.</returns>
		public static Bitmap Fill( this Bitmap source, Color color ) =>
			source.Fill( new SolidBrush( color ) );

		/// <summary>Resize the image to the specified width and height.</summary>
		/// <param name="width">The width to resize to.</param>
		/// <param name="height">The height to resize to.</param>
		/// <returns>The resized image.</returns>
		/*
		public static Bitmap HQResizeTo( this Image image, int width, int height )
		{
			var destRect = new Rectangle( 0, 0, width, height );
			var destImage = new Bitmap( width, height );

			destImage.SetResolution( image.HorizontalResolution, image.VerticalResolution );

			using ( var graphics = Graphics.FromImage( destImage ) )
			{
				graphics.CompositingMode = CompositingMode.SourceCopy;
				graphics.CompositingQuality = CompositingQuality.HighQuality;
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.SmoothingMode = SmoothingMode.HighQuality;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

				using ( var wrapMode = new ImageAttributes() )
				{
					wrapMode.SetWrapMode( WrapMode.TileFlipXY );
					graphics.DrawImage( image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode );
				}
			}

			return destImage;
		}
		*/
	}
}