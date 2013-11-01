using System;
using System.Windows.Forms;

namespace Beanulator
{
    public partial class FormBrowse : Form
    {
        public FormBrowse()
        {
            InitializeComponent();
        }

        private void Open(string filename)
        {
        }

        #region File

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            base.Close();
        }

        private void buttonRecent1_Click(object sender, EventArgs e) { Open(""); }
        private void buttonRecent2_Click(object sender, EventArgs e) { Open(""); }
        private void buttonRecent3_Click(object sender, EventArgs e) { Open(""); }
        private void buttonRecent4_Click(object sender, EventArgs e) { Open(""); }
        private void buttonRecent5_Click(object sender, EventArgs e) { Open(""); }

        #endregion
        #region Tools

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var form = new FormOptions())
            {
                form.ShowDialog(this);
            }
        }

        #endregion
    }
}