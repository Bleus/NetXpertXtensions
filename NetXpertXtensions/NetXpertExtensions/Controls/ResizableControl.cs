using System;
using System.Drawing;
using System.Windows.Forms;

namespace NetXpertExtensions.Controls
{
	#nullable disable

	public abstract class ResizableControl : UserControl
	{
		#region Properties
		private bool mMouseDown = false;
		private EdgeEnum mEdge = EdgeEnum.None;
		private readonly int mWidth = 4;
		private bool mOutlineDrawn = false;

		private enum EdgeEnum { None, Right, Left, Top, Bottom, TopLeft };

		private System.ComponentModel.IContainer components = null;
		#endregion

		#region Constructors
		public ResizableControl() : base() 
		{
			components = new System.ComponentModel.Container();
			this.MouseDown += this.ResizableControl_MouseDown;
			this.MouseUp += this.ResizableControl_MouseUp;
			this.MouseMove += this.ResizableControl_MouseMove;
			this.MouseLeave += this.ResizableControl_MouseLeave;
		}
		#endregion

		#region Methods
		private void ResizableControl_MouseLeave( object sender, EventArgs e )
		{
			Control c = sender as Control;
			mEdge = EdgeEnum.None;
			c.Refresh();
		}

		private void ResizableControl_MouseMove( object sender, MouseEventArgs e )
		{
			Control c = sender as Control;
			Graphics g = c.CreateGraphics();
			switch( mEdge )
			{
				case EdgeEnum.TopLeft:
					g.FillRectangle( Brushes.Fuchsia, 0, 0, mWidth * 4, mWidth * 4 );
					mOutlineDrawn = true;
					break;
				case EdgeEnum.Left:
					g.FillRectangle( Brushes.Fuchsia, 0, 0, mWidth, c.Height );
					mOutlineDrawn = true;
					break;
				case EdgeEnum.Right:
					g.FillRectangle( Brushes.Fuchsia, c.Width - mWidth, 0, c.Width, c.Height );
					mOutlineDrawn = true;
					break;
				case EdgeEnum.Top:
					g.FillRectangle( Brushes.Fuchsia, 0, 0, c.Width, mWidth );
					mOutlineDrawn = true;
					break;
				case EdgeEnum.Bottom:
					g.FillRectangle( Brushes.Fuchsia, 0, c.Height - mWidth, c.Width, mWidth );
					mOutlineDrawn = true;
					break;
				case EdgeEnum.None:
					if ( mOutlineDrawn )
					{
						c.Refresh();
						mOutlineDrawn = false;
					}
					break;
			}

			if ( mMouseDown && mEdge != EdgeEnum.None )
			{
				c.SuspendLayout();
				switch (mEdge)
				{
					case EdgeEnum.TopLeft:
						c.SetBounds( c.Left + e.X, c.Top + e.Y, c.Width, c.Height );
						break;
					case EdgeEnum.Left:
						c.SetBounds( c.Left + e.X, c.Top, c.Width - e.X, c.Height );
						break;
					case EdgeEnum.Right:
						c.SetBounds( c.Left, c.Top, c.Width - (c.Width - e.X), c.Height );
						break;
					case EdgeEnum.Top:
						c.SetBounds( c.Left, c.Top + e.Y, c.Width, c.Height - e.Y );
						break;
					case EdgeEnum.Bottom:
						c.SetBounds( c.Left, c.Top, c.Width, c.Height - (c.Height - e.Y) );
						break;
				}
				c.ResumeLayout();
			}
			else
			{
				if ( e.X <= (mWidth * 4) && e.Y <= (mWidth * 4) ) // top-left corner
				{
					c.Cursor = Cursors.SizeAll;
					mEdge = EdgeEnum.TopLeft;
					return;
				}
				if ( e.X <= mWidth ) // left edge
				{
					c.Cursor = Cursors.VSplit;
					mEdge = EdgeEnum.Left;
					return;
				}
				if ( e.X > c.Width - (mWidth + 1) ) // right edge
				{
					c.Cursor = Cursors.VSplit;
					mEdge = EdgeEnum.Right;
					return;
				}
				if ( e.Y <= mWidth ) // top edge
				{
					c.Cursor = Cursors.HSplit;
					mEdge = EdgeEnum.Top;
					return;
				}
				if ( e.Y > c.Height - (mWidth + 1) ) // bottom edge
				{
					c.Cursor = Cursors.HSplit;
					mEdge = EdgeEnum.Bottom;
					return;
				}
				
				c.Cursor = Cursors.Default;
				mEdge = EdgeEnum.None;
			}
		}

		private void ResizableControl_MouseUp( object sender, MouseEventArgs e ) =>
			mMouseDown = false;

		private void ResizableControl_MouseDown( object sender, MouseEventArgs e ) =>
			mMouseDown = e.Button == MouseButtons.Left;
	}
	#endregion
}
