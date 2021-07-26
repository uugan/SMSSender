using GsmComm.GsmCommunication;
using GsmComm.PduConverter;
using GsmComm.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SMSSender
{
    public partial class FrmMain : Form
    {



        LiveModem _modem;
        Thread thrd;
        Dictionary<string, string> com_ports = new Dictionary<string, string>();
        public FrmMain()
        {
            InitializeComponent();
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            string[] ports = SerialPort.GetPortNames();
            try
            {
                ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("root\\CIMV2",
                    "SELECT * FROM Win32_PnPEntity");

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    if (queryObj["Caption"] != null && queryObj["Caption"].ToString().Contains("(COM"))
                    {
                        string port_caption = queryObj["Caption"].ToString();
                        com_ports.Add(queryObj["Caption"].ToString(), port_caption.Substring(port_caption.IndexOf("(COM") + 1).Replace(")", ""));
                        cmbComPort.Items.Add(queryObj["Caption"]);
                    }

                }
            }
            catch (ManagementException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void start()
        {
            DataTable dt = dataGridView1.DataSource as DataTable;
            if (dt == null || dt.Rows.Count == 0)
            {
                MessageBox.Show("Sms list is empty!");
                return;
            }
            btnStart.InvokeIfNeeded(() => btnStart.Enabled = false);
            label5.InvokeIfNeeded(() => label5.Text = "Status: Running");
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                _modem.sendSms(dt.Rows[i]["PHONENO"].ToString(), dt.Rows[i]["SMS"].ToString());
                dt.Rows[i]["STATUS"] = "SENT";
                lstLog.InvokeIfNeeded(() => lstLog.Items.Add("SENT> " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " : " + dt.Rows[i]["SMS"].ToString()));
            }
            btnStart.InvokeIfNeeded(() => btnStart.Enabled = true);
            label5.InvokeIfNeeded(() => label5.Text = "Status: Stopped.");
        }


        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_modem != null && _modem.isConnected)
            {
                _modem.disconnect(); // stopping receiver
            }
            try
            {
                if (thrd != null && thrd.IsAlive)
                {
                    thrd.Join();
                    thrd.Abort();

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error occurred:" + ex.Message);
            }
        }



        private void btnConnect_Click(object sender, EventArgs e)
        {

            if (com_ports.ContainsKey(cmbComPort.Text) && txtSMSC.Text != "")
            {
                _modem = new LiveModem(com_ports[cmbComPort.Text], (int)numBaudRate.Value, (int)numTimeout.Value, txtSMSC.Text);
                string ret = _modem.connect();
                _modem.OnSmsReceived += _modem_OnSmsReceived;
                _modem.OnSmsSent += _modem_OnSmsSent;

                if (ret == "000")
                {
                    label1.Text = "Status: Connected";
                    btnSend.Enabled = true;
                    if (!_modem.isRunning)
                    {
                        thrd = new Thread(() =>
                        {
                            _modem.startReceive();
                        });
                        thrd.Start();
                    }

                }
                else
                {
                    label1.Text = "Status: Error occurred>" + ret;
                    btnSend.Enabled = false;
                }
            }
            else
            {
                label1.Text = "Status: Error occurred> Serial port is not found or SMSC is empty.";
                btnSend.Enabled = false;
            }

        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (_modem.isConnected)
            {
                _modem.sendSms(txtPhoneno.Text, txtSms.Text);
            }
        }

        private void btnImport_Click_1(object sender, EventArgs e)
        {

            DataTable dt = new DataTable();
            dt.Columns.Add("PHONENO");
            dt.Columns.Add("SMS");
            dt.Columns.Add("STATUS");
            ExcelImporter frm = new ExcelImporter(dt);
            if (frm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                dt = frm.DataSource;
                dataGridView1.DataSource = dt;
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {

            if (_modem != null && _modem.isConnected)
            {

                Task.Factory.StartNew(() => start());
            }
            else
            {
                label5.Text = "Status: Not connected!";
            }
        }

        private void btnSMSDelete_Click(object sender, EventArgs e)
        {
            if (_modem != null && _modem.isConnected)
            {
                if (MessageBox.Show("Delete all sms from inbox?", "", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                {
                    if (_modem.clearsms() == "000")
                    {
                        MessageBox.Show("Deleted.");
                    }
                }
            }
        }

        private void btnExport_Click_1(object sender, EventArgs e)
        {
            using (SaveFileDialog dialog = new SaveFileDialog { Filter = "Excel|*.xlsx" })
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {

                        DataTable dt = ((DataTable)dataGridView1.DataSource);
                        CreateCSVFile(ref dt, dialog.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error occurred:" + ex.Message);
                    }
                }
            }
        }
        public void CreateCSVFile(ref DataTable dt, string strFilePath)
        {
            try
            {
                // Create the CSV file to which grid data will be exported.
                StreamWriter sw = new StreamWriter(strFilePath, false);
                // First we will write the headers.
                int iColCount = dt.Columns.Count;
                for (int i = 0; i < iColCount; i++)
                {
                    sw.Write(dt.Columns[i]);
                    if (i < iColCount - 1)
                    {
                        sw.Write(",");
                    }
                }
                sw.Write(sw.NewLine);

                foreach (DataRow dr in dt.Rows)
                {
                    for (int i = 0; i < iColCount; i++)
                    {
                        if (!Convert.IsDBNull(dr[i]))
                        {
                            sw.Write(dr[i].ToString());
                        }
                        if (i < iColCount - 1)
                        {
                            sw.Write(",");
                        }
                    }

                    sw.Write(sw.NewLine);
                }
                sw.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        /* sms listener */
        void _modem_OnSmsReceived(object sender, SmsEventArg e)
        {
            lstLog.InvokeIfNeeded(() => lstLog.Items.Add(e.smsInfo.ToString()));
        }

        void _modem_OnSmsSent(object sender, SmsEventArg e)
        {
            lstLog.InvokeIfNeeded(() => lstLog.Items.Add(e.smsInfo.ToString()));
        }

        private void txtSms_TextChanged(object sender, EventArgs e)
        {
            label2.Text = txtSms.Text.Length + "/160";
        }

        private void dataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.V && e.Modifiers == Keys.Control)
            {
                DataTable dt = ControlExtentions.StringToDataTable(Clipboard.GetText());

                DataTable DataSource = new DataTable();
                DataSource.Columns.Add("PHONENO");
                DataSource.Columns.Add("SMS");
                DataSource.Columns.Add("STATUS");
                dataGridView1.FillDataSource(DataSource, dt);
            }
        }




    }
}
