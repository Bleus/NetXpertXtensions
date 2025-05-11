using System.Windows.Forms;

namespace NetXpertExtensions.Controls
{
	partial class LabelledInputControl<T>
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
			this.Size = new System.Drawing.Size( 300, 23 );
			this.label1 = new System.Windows.Forms.Label();
			this.textBox1 = new T();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.label1.AutoSize = true;
			this.label1.ForeColor = base.ForeColor;
			this.label1.BackColor = base.BackColor;
			this.label1.Location = new System.Drawing.Point(0, 7);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(38, 15);
			this.label1.TabIndex = 0;
			this.label1.Text = "label1";
			// 
			// textBox1
			// 
			this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBox1.Location = new System.Drawing.Point(47, 4);
			this.textBox1.Margin = new System.Windows.Forms.Padding(0);
			this.textBox1.Name = $"{this.textBox1.GetType().Name}1";
			this.textBox1.Size = new System.Drawing.Size(258, 23);
			this.textBox1.TabIndex = 1;
			// 
			// TextBoxWithLabel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.BackColor = System.Drawing.Color.Transparent;
			this.Controls.Add(this.textBox1);
			this.Controls.Add(this.label1);
			this.Margin = new System.Windows.Forms.Padding(0);
			this.Name = "LabelledInputControl";
			this.Size = new System.Drawing.Size(308, 28);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		protected Label label1;
		protected T textBox1;
	}
}
