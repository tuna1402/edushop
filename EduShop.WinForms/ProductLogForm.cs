using System.Windows.Forms;
using EduShop.Core.Models;
using EduShop.Core.Services;

namespace EduShop.WinForms;

public class ProductLogForm : Form
{
    private readonly ProductService _service;
    private readonly Product _product;

    private DataGridView _grid = null!;

    public ProductLogForm(ProductService service, Product product)
    {
        _service = service;
        _product = product;

        Text = $"변경 이력 - [{_product.ProductCode}] {_product.ProductName}";
        Width = 800;
        Height = 400;
        StartPosition = FormStartPosition.CenterParent;

        InitializeControls();
        LoadLogs();
    }

    private void InitializeControls()
    {
        _grid = new DataGridView
        {
            Left = 10,
            Top = 10,
            Width = ClientSize.Width - 20,
            Height = ClientSize.Height - 20,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoGenerateColumns = false
        };

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "일시",
            DataPropertyName = "EventTime",
            Width = 150
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "사용자",
            DataPropertyName = "UserName",
            Width = 100
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "작업",
            DataPropertyName = "ActionType",
            Width = 150
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "내용",
            DataPropertyName = "Description",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });

        Controls.Add(_grid);
    }

    private void LoadLogs()
    {
        var logs = _service.GetLogsForProduct(_product.ProductId);
        _grid.DataSource = logs;
    }
}
