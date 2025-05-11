namespace NetXpertCodeLibrary.WinFormsControls.Controls
{
	partial class BasicTransparentPanel
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing )
		{
			if ( disposing && (components != null) )
			{
				components.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.roundedPanel1 = new NetXpertExtensions.Controls.RoundedPanel();
			this.SuspendLayout();
			// 
			// roundedPanel1
			// 
			this.roundedPanel1.BackColor = System.Drawing.Color.LightGray;
			this.roundedPanel1.BorderColor = System.Drawing.Color.Empty;
			this.roundedPanel1.BorderWidth = 10;
			this.roundedPanel1.Location = new System.Drawing.Point(0, 0);
			this.roundedPanel1.Name = "roundedPanel1";
			this.roundedPanel1.Radius = 5;
			this.roundedPanel1.Size = new System.Drawing.Size(200, 100);
			this.roundedPanel1.TabIndex = 0;
			this.ResumeLayout(false);

		}

		#endregion

		private NetXpertExtensions.Controls.RoundedPanel roundedPanel1;
	}
}
