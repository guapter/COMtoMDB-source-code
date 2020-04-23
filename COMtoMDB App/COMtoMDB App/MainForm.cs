using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
using System.Threading;


namespace test_program_barier
{

    public partial class MainForm : Form
    {
        public static ListBox listBox { get; set; }
        public static TextBox textBox { get; set; }
        public static RS232port Port;
        
        public MainForm()
        {
            InitializeComponent();
            listBox = listBox1;
            textBox = textBox1;
        }
  
        private void MainForm_Load(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(COMtoMDB_App.Properties.Settings.Default.MBDPath))
            { DBExchange.PathToBD = COMtoMDB_App.Properties.Settings.Default.MBDPath; }
            if (!String.IsNullOrEmpty(COMtoMDB_App.Properties.Settings.Default.PortNumber))
            { RS232port.PortNumber = COMtoMDB_App.Properties.Settings.Default.PortNumber;}
            if (COMtoMDB_App.Properties.Settings.Default.BaudRate > 0)
            { RS232port.BaudRate = COMtoMDB_App.Properties.Settings.Default.BaudRate;}
            if (COMtoMDB_App.Properties.Settings.Default.Timeouts > 0)
            { RS232port.Timeouts = COMtoMDB_App.Properties.Settings.Default.Timeouts;}
            DBExchange.ConnectToDB();
            DBExchange.LoadTable();
            Port = new RS232port();
            Port.OpenPort();
            
        }
        
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            COMtoMDB_App.Properties.Settings.Default.MBDPath = DBExchange.PathToBD;
            COMtoMDB_App.Properties.Settings.Default.PortNumber = RS232port.PortNumber;
            COMtoMDB_App.Properties.Settings.Default.BaudRate = RS232port.BaudRate;
            COMtoMDB_App.Properties.Settings.Default.Timeouts = RS232port.Timeouts;
            RS232port.Continue = false;
            DBExchange.myConnection.Close();
            Environment.Exit(0);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DBExchange.AddLine(textBox1.Text, false);
        }

        private void PortSettingsToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            Form PortSettingsForm = new PortSettingsForm();
            PortSettingsForm.ShowDialog();
        }

        private void GeneralSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Form GeneralSettingsForm = new Form();
            //GeneralSettingsForm.ShowDialog();
            Form GeneralSettingsForm = new GeneralSettingsForm();
            GeneralSettingsForm.ShowDialog();
        }

        public void TerminalSend()
        {
            RS232port.RS232.WriteLine(tbTerminal.Text);
            lbTerminal.Items.Insert(0, DateTime.Now.ToString("HH:mm:ss") + ">  " + tbTerminal.Text);
            tbTerminal.Clear();
            
        }


        public static class DBExchange
        {
            public static string PathToBD { get; set; }
            public static OleDbConnection myConnection;
            public static string connectString;
            

            public static void ConnectToDB()
            {
                if (!String.IsNullOrEmpty(PathToBD) && !String.IsNullOrWhiteSpace(PathToBD))
                {
                    try
                    {
                        connectString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + PathToBD;
                        myConnection = new OleDbConnection(connectString);
                        myConnection.Open();
                    }
                    catch (Exception E) { MessageBox.Show(E.Message, "Соединение с базой данных установить не удалось!", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                }
                else
                {
                    try
                    {
                        connectString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=db1testwater.mdb";
                        myConnection = new OleDbConnection(connectString);
                        myConnection.Open();
                    }
                    catch (Exception E)
                    {
                        Form GenSettingsForm = new GeneralSettingsForm();
                        GenSettingsForm.ShowDialog();
                    }
                }
            }

            public static void AddLine(string InsertData, bool Mute = true)
            {
                
                if (!String.IsNullOrWhiteSpace(InsertData) && !String.IsNullOrEmpty(InsertData))
                {
                    int RowCount = -1;
                    string QueryToDB = "INSERT INTO measurement VALUES ('" + DateTime.Now + "','" + InsertData + "')";
                    OleDbCommand command = new OleDbCommand(QueryToDB, myConnection);
                    try { RowCount = command.ExecuteNonQuery(); }
                    catch (OleDbException E)
                    {
                        MessageBox.Show("OleDbException:" + E.Message, "Ошибка добавления записи в базу данных", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    if (!Mute)
                    {
                        if (RowCount > 0) { MessageBox.Show("Success"); LoadTable(); } else { MessageBox.Show("Fail"); }
                    }
                }
                //TextBox textBox1 = new TextBox();
                //textBox1.Clear();
                textBox.Clear();

            }
            public static void LoadTable()
            {
                
                string QueryToDB = "SELECT Time, Result FROM measurement ORDER BY Time";
                OleDbCommand command = new OleDbCommand(QueryToDB, myConnection);
                OleDbDataReader reader = command.ExecuteReader();
                
                listBox.Items.Clear();
                while (reader.Read())
                {
                    listBox.Items.Add(reader[0].ToString() + " " + reader[1].ToString() + " ");
                }
                reader.Close();
                //int visibleItems = listBox.ClientSize.Height / listBox.ItemHeight;
                //int padding = 3;
                //bool atEnd = listBox.TopIndex >= Math.Max(listBox.Items.Count - visibleItems - 3, 0);
                //if (atEnd) { listBox.TopIndex = Math.Max(listBox.Items.Count - visibleItems, 0); }
                
            }
        }

        public class RS232port
        {
            public static SerialPort RS232 { get; set; }
            public static string PortNumber = "COM3";
            public static int BaudRate = 4800;
            public static int Timeouts = 500;
            public static string RecievedData { get; set; }
            public static bool Continue;
            public static Thread readThread;

            public void OpenPort()
            {
                StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;
                string[] ports = SerialPort.GetPortNames();
                if (Array.IndexOf(ports, PortNumber) > -1)
                {
                    RS232 = new SerialPort(PortNumber, BaudRate, Parity.None, 8, StopBits.One)
                    {
                        Handshake = Handshake.None,
                        ReadTimeout = Timeouts,
                        WriteTimeout = Timeouts
                    };
                    RS232.Open();
                    Continue = true;
                    readThread = new Thread(Read);
                    readThread.Name = "Reading Port Thread";
                    readThread.Start();    
                }
                else
                {
                    MessageBox.Show("Укажите существующий COM порт или подключите устройство","COM порт не найден", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Form PortSettingsForm = new PortSettingsForm();
                    PortSettingsForm.ShowDialog();
                }
            }
            public void Read()
            {
                while (Continue && RS232.IsOpen)
                {
                    try
                    {
                        RecievedData = RS232.ReadLine();
                        //DBExchange.AddLine(RecievedData, true);
                        //MainForm.textBox.Invoke(new Action(() => { DBExchange.AddLine(RecievedData, true); }));
                        MainForm.listBox.Invoke(new Action(() => { DBExchange.AddLine(RecievedData, true); DBExchange.LoadTable(); }));
                    }
                    catch (TimeoutException) {  }
                }
                RS232port.RS232.Close();
            }
        }

        private void tbTerminal_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                TerminalSend();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            TerminalSend();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //DBExchange.LoadTable();
            //tbPortStatus.Clear();
            string PortStatus;
            if (RS232port.RS232 != null)
            {
                if (RS232port.RS232.IsOpen && RS232port.Continue)
                {
                    PortStatus = "Соединение установлено";
                    tbPortStatus.ForeColor = System.Drawing.Color.DarkGreen;
                }
                else
                {
                    tbPortStatus.ForeColor = Color.DarkRed;
                    string[] ports = SerialPort.GetPortNames();
                    if (Array.IndexOf(ports, RS232port.PortNumber) > -1)
                    { PortStatus = "Соединение отсутствует"; }
                    else { PortStatus = "Соединение отсутствует,\nпорт не существует"; }
                }
                tbPortStatus.Text = PortStatus + "\r\n\r\nПорт: " + RS232port.PortNumber + "\r\nСкорость: " + RS232port.BaudRate;
            }
        }

        private void textBox1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                DBExchange.AddLine(textBox1.Text, false);
            }
        }

        private void оПрограммеToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Program made by Guapter");
        }
    }
}
