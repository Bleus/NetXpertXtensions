using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Cobblestone.Classes;
using Cobblestone.Classes.Settings;

namespace NetXpertCodeLibrary.WinForms
{
	internal partial class AboutForm : Form
	{
		public AboutForm()
		{
			InitializeComponent();
			if (!(Common.Main is null))
			{
				this._about.BackColor = Common.Main.BackColor;
				_about.WriteLn("The Cobblestone&trade; Property Management Application");
				Common.Main.PopulateAboutBox(this._about);
			}
		}

		private void pictureBox2_Click(object sender, EventArgs e) =>
			this.Close();
	}
}
