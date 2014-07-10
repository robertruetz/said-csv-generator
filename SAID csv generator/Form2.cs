using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SAID_csv_generator
{
    public partial class Form2 : Form
    {
        public delegate void AdviseParentEventHandler(DataTable t);
        public event AdviseParentEventHandler AdviseParent;

        //variable declaration
        string[] temporary;
        List<CopyHolder> tempList = new List<CopyHolder>();
        string[] colTitles = { "File ID", "Script", "Script 2" };

        public Form2()
        {
            InitializeComponent();
        }

        private void sendButton_Click(object sender, EventArgs e)
        {
            if (dataGridView1.DataSource != null)
            {
                //pulls the data out of the grid view and passes it back to form1
                DataTable outMesa = new DataTable();

                foreach (DataGridViewColumn column in dataGridView1.Columns)
                {
                    DataColumn dCol = new DataColumn();
                    dCol.ColumnName = column.HeaderText;
                    //dCol.DataType = column.CellType;
                    outMesa.Columns.Add(dCol);
                }

                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    DataRow dRow = outMesa.Rows.Add();
                    for (int i = 0; i < dataGridView1.ColumnCount; i++)
                    {
                        dRow[i] = row.Cells[i].Value;
                    }

                }

                SendInfoToForm1(outMesa);

                this.Close();
            }

            else
                MessageBox.Show("No script items found. Try again.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        public void SendInfoToForm1(DataTable info)
        {
            if (AdviseParent != null)
            {
                AdviseParent(info);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            dataGridView1.DataSource = null;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (Clipboard.ContainsText(TextDataFormat.Text))
            {
                //clearing data in case of second attempt
                tempList.Clear();

                if (Clipboard.GetText().Contains('\t'))
                {
                    temporary = Clipboard.GetText().Trim().Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                    
                    DataTable newTable = new DataTable();

                    //count how many columns are needed and add them.
                    string[] cnt = temporary[0].Split('\t');
                    for (int foo = 0; foo < cnt.Length; foo++)
                        newTable.Columns.Add();

                    //add our data to each row.
                    foreach (string s in temporary)
                    {
                        string[] r = s.Trim().Split('\t');
                        newTable.Rows.Add(r);
                    }

                    dataGridView1.DataSource = newTable;

                    dataGridView1.AutoGenerateColumns = true;
                    dataGridView1.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

                    for (int ccc = 0; ccc < dataGridView1.Columns.Count; ccc++)
                    {
                        DataGridViewColumn col = dataGridView1.Columns[ccc];
                        col.HeaderText = colTitles[ccc];
                        col.Width = 300;
                        col.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                    }

                    dataGridView1.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells);
                }

                else
                    MessageBox.Show("The text you tried to paste doesn't look like a script.\r\n Try again.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

            }
            else
                MessageBox.Show("You must copy your script from Word first", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            
        }

        static DataTable ConvertListToDataTable(List<CopyHolder> list)
        {
            DataTable mesa = new DataTable();

            for (int x = 0; x < 2; x++)
            {
                mesa.Columns.Add();
            }

            foreach (CopyHolder file in list)
            {
                string[] temp = { file.fileID, file.script1 };
                mesa.Rows.Add(temp);
            }

            return mesa;
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            //dataGridView1.DataSource = null;
        }

        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {

        }
    }

    public class CopyHolder
    {
        public string fileID;
        public string script1;

        public CopyHolder(string fID, string scrpt1)
        {
            this.fileID = fID;
            this.script1 = scrpt1;
        }
    }
}
