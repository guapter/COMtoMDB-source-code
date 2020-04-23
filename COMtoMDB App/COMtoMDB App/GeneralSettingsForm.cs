using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace test_program_barier
{
    public partial class GeneralSettingsForm : Form
    {
        public static string fileName { get; set; }
        public GeneralSettingsForm()
        {
            InitializeComponent();
        }

        private void GeneralSettingsForm_Load(object sender, EventArgs e)
        {

        }

        private void Browse_Click(object sender, EventArgs e)
        {
            OpenFileDialog Fd = new OpenFileDialog
            {
                Title = "Выберите файл",
                InitialDirectory = @"C:\",
                Filter = "База данных Access (*.mdb)|*.mdb"
            };
            if (Fd.ShowDialog() == DialogResult.OK)
            {
                fileName = Fd.FileName;
                tbFIlePath.Text = fileName;
            }
        }

        private void bAccept_Click(object sender, EventArgs e)
        {
            MainForm.DBExchange.PathToBD = fileName;
            Close();
        }

        private void bCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
       
    }
}
