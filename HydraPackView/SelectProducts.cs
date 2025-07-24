using HydraPackView.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HydraPackView
{
    public partial class SelectProducts : Form
    {
        private readonly DatabaseService _databaseService;
        private readonly long _documentId;
        private bool _checkAll = true;

        public List<string> ReturnProducts { get; private set; } = new List<string>();

        public SelectProducts(DatabaseService databaseService, long documentId)
        {
            InitializeComponent();
            _databaseService = databaseService;
            _documentId = documentId;
        }

        private async void SelectProducts_Load(object sender, EventArgs e)
        {
            try
            {
                var table = await _databaseService.GetPackingDataTable(_documentId);

                if (!ProductsDataGridView.Columns.Contains("Select"))
                {
                    DataGridViewCheckBoxColumn checkboxColumn = new DataGridViewCheckBoxColumn
                    {
                        Name = "Select",
                        HeaderText = "",
                        Width = 30,
                        Frozen = true
                    };
                    ProductsDataGridView.Columns.Insert(0, checkboxColumn);
                }

                ProductsDataGridView.DataSource = table;

                ProductsDataGridView.Columns["Id"].Visible = false;
                ProductsDataGridView.Columns["Kod"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                ProductsDataGridView.Columns["Nazwa"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                ProductsDataGridView.Columns["Il. spak."].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                ProductsDataGridView.Columns["Data spak."].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Wystąpił błąd przy ładowaniu pozycji. Zawołaj administratora: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SearchButton_Click(object sender, EventArgs e)
        {
            var selectedRows = ProductsDataGridView.Rows
                .Cast<DataGridViewRow>()
                .Where(row => row.Cells["Select"].Value is bool b && b)
                .ToList();

            foreach (DataGridViewRow row in selectedRows)
            {
                ReturnProducts.Add(row.Cells["Id"].Value.ToString());
            }

            this.DialogResult = DialogResult.OK;
            Close();
        }

        private void SelectAllButton_Click(object sender, EventArgs e)
        {
            SelectAll();
            if (_checkAll)
                SelectAllButton.Text = "Zaznacz wszystkie";
            else
                SelectAllButton.Text = "Odznacz wszystkie";
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            Close();
        }

        private void ProductsDataGridView_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (ProductsDataGridView.Columns[e.ColumnIndex].Name == "Select")
            {
                SelectAll();
            }
        }

        private void SelectAll()
        {
            foreach (DataGridViewRow row in ProductsDataGridView.Rows)
            {
                row.Cells["Select"].Value = _checkAll;
            }
            _checkAll = !_checkAll;
        }
    }
}