using System;
using System.Windows.Forms;
using System.Threading;

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
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            new Thread(() => CBackup.doBackup(txtSource.Text, txtDest.Text)).Start();
            timer1.Enabled = true;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // Reset
            //button3_Click(null, null);
            MessageBox.Show("Fonction non-implémentée.");
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

            if (CBackup.Finish)
            {
                timer1.Enabled = false;
                MessageBox.Show(CBackup.Message);
                button1.Enabled = true;
                button2.Enabled = true;
                button3.Enabled = true;
            }
        }
    }
}
