using System;
using System.Windows.Forms;

namespace BackupTool
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.ShowDialog();
            txtSource.Text = folderBrowserDialog1.SelectedPath;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.ShowDialog();
            txtDest.Text = folderBrowserDialog1.SelectedPath;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            timer1.Enabled = true;
            MessageBox.Show(CBackup.doBackup(txtSource.Text, txtDest.Text));
            timer1.Enabled = false;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // Reset
            button3_Click(null, null);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            progressBar1.Maximum = CBackup.Total;
            if (progressBar1.Maximum <= 0)
            {
                progressBar1.Value = 0;
            }
            else if (CBackup.Current > progressBar1.Maximum)
            {
                progressBar1.Value = progressBar1.Maximum;
            }
            else
            {
                progressBar1.Value = (CBackup.Current <= 0) ? 1 : CBackup.Current;
            }
        }
    }
}
