using System;
using System.IO;
using System.Threading;
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
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                txtSource.Text = folderBrowserDialog1.SelectedPath;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                txtDest.Text = folderBrowserDialog1.SelectedPath;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            comboBox1.Enabled = false;
            new Thread(() => CBackup.doBackup(txtSource.Text, txtDest.Text)).Start();
            timer1.Enabled = true;
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
                comboBox1.Enabled = true;
            }
        }

        string[] arrstrPresets;

        private void Form1_Load(object sender, EventArgs e)
        {
            if (File.Exists("presets.txt"))
            {
                arrstrPresets = File.ReadAllLines("presets.txt");
                for (int i = 0; i < arrstrPresets.Length; i += 3)
                {
                    comboBox1.Items.Add(arrstrPresets[i]);
                }
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            txtSource.Text = arrstrPresets[comboBox1.SelectedIndex * 3 + 1];
            txtDest.Text = arrstrPresets[comboBox1.SelectedIndex * 3 + 2];
        }
    }
}
