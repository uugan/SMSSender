using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SMSSender
{
    public static class ControlExtentions
    {
        /// <summary>
        /// Delegate calling like control.InvokeIfNeeded
        /// </summary>
        /// <param name="control">Component</param>
        /// <param name="doit">Delegate action</param>
        public static void InvokeIfNeeded(this Control control, Action doit)
        {
            if (control.InvokeRequired)
                control.Invoke(doit);
            else
                doit();
        }
        /// <summary>
        /// Delegate calling like control.InvokeIfNeeded
        /// </summary>
        /// <typeparam name="T">Delegate-н param type</typeparam>
        /// <param name="control">Component</param>
        /// <param name="doit">Delegate action</param>
        /// <param name="arg">Delegate action args</param>
        public static void InvokeIfNeeded<T>(this Control control, Action<T> doit, T arg)
        {
            if (control.InvokeRequired)
                control.Invoke(doit, arg);
            else
                doit(arg);
        }
        public static DataTable StringToDataTable(String source)
        {

            DataTable dt = new DataTable();
            string[] pastedRows = Regex.Split(source, "\r\n");
            int i = 0;
            foreach (string pastedRow in pastedRows)
            {
                if (pastedRow.Length == 0)
                    continue;

                string[] pastedRowCells = pastedRow.Split(new char[] { '\t' });
                if (i == 0)
                {
                    int j = 0;
                    foreach (string str in pastedRowCells)
                    {
                        dt.Columns.Add(new DataColumn { ColumnName = str });
                        j++;
                    }
                }
                else
                {
                    DataRow dr = dt.NewRow();
                    int j = 0;
                    foreach (string str in pastedRowCells)
                    {
                        dr[j] = str;
                        j++;
                    }
                    dt.Rows.Add(dr);
                }

                i++;

            }
            return dt;
        }
        public static void FillDataSource(this DataGridView dgrview, DataTable DataSource ,DataTable dtNew)
        {
            try
            {
                dgrview.DataSource = DataSource;
                foreach (DataRow dr in dtNew.Rows)
                {
                    DataRow dr1 = DataSource.NewRow();
                    foreach (DataColumn dc in dtNew.Columns)
                    {
                        try
                        {
                            dr1[dc.ColumnName] = dr[dc.ColumnName];
                        }
                        catch (ArgumentException ex)
                        {
                            Console.WriteLine(ex.Message);
                            DataSource.Columns.Add(new DataColumn { ColumnName = dc.ColumnName });
                            dr1[dc.ColumnName] = dr[dc.ColumnName];
                            dgrview.Columns.Add(dc.ColumnName, dc.ColumnName);
                            dgrview.Columns[dc.ColumnName].Visible = true;
                        }
                    }

                    DataSource.Rows.Add(dr1);

                }
            }
            catch (NoNullAllowedException ex)
            {
                MessageBox.Show("Please fill all given columns: " + ex.Message);
                DataSource.Clear();
                return;
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show("File columns are not fit with table columns: " + ex.Message);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error occurred: " + ex.Message);
                return;
            }
        }

    }
}
