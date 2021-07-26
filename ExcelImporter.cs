using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SMSSender
{
    public partial class ExcelImporter : Form
    {
        public DataTable DataSource { get; set; }
        public Boolean exportMode = false;

        public ExcelImporter(DataTable dt)
        {
            this.DataSource = dt;
            InitializeComponent();
        }

        private void ExcelImporter_Load(object sender, EventArgs e)
        {
            dataGridView1.DataSource = DataSource;
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            DataSource.Clear();
            using (OpenFileDialog dialog = new OpenFileDialog { Filter = "All files (*.*)|*.*" })
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    DataTable dt1 = ExcelToDataTable(dialog.FileName, "Sheet1").Tables[0];
                    dataGridView1.FillDataSource(DataSource, dt1);
                }
            }
        }

        public DataSet ExcelToDataTable(string filename, string sheetname)
        {
            string ConnectionString = "";
            FileInfo fi = new FileInfo(filename);
            OleDbConnection conn = null;
            if (fi.Extension.Equals(".xls") || fi.Extension.Equals(".csv"))
            {
                ConnectionString = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + filename + ";Extended Properties='Excel 8.0';";
                conn = new OleDbConnection(ConnectionString);
                conn.Open();
            }
            else if (fi.Extension.Equals(".xlsx"))
            {
                openConnection(conn, "12.0", filename);
            }

            try
            {
                OleDbDataAdapter oda = new OleDbDataAdapter("select * from [" + sheetname + "$]", conn);
                DataSet ds = new DataSet();
                oda.Fill(ds);
                return ds;

            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }

            finally
            {
                conn.Close();
            }
        }


        public void openConnection(OleDbConnection conn, String version, String filename)
        {

            try
            {
                String ConnectionString = @"Provider=Microsoft.ACE.OLEDB." + version + ";Data Source=" + filename + ";Extended Properties='Excel " + version + "';";
                conn = new OleDbConnection(ConnectionString);
                conn.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                if (version.Equals("20.0"))
                {
                    return;
                }
                String new_version = (Int32.Parse(version.Substring(0, 2)) + 1) + ".0";
                openConnection(conn, new_version, filename);
            }
        }



        private void btnClear_Click(object sender, EventArgs e)
        {
            DataSource.Clear();
        }



        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void dataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.V && e.Modifiers == Keys.Control)
            {
                DataTable dt = ControlExtentions.StringToDataTable(Clipboard.GetText());
                dataGridView1.FillDataSource(DataSource, dt);
            }
            //else if (e.KeyCode == Keys.Delete)
            //{
            //    DeleteSelectedRows(gridView1);
            //}
        }
    }
}
