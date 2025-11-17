using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using EduShop.Core.Models;
using EduShop.Core.Services;

namespace EduShop.WinForms;

public class ProductPickerForm : Form
{
    private readonly ProductService _service;

    private TextBox _txtKeyword = null!;
    private ComboBox _cboStatus = null!;
    private Button _btnSearch = null!;
    private DataGridView _grid = null!;
    private Button _btnSelect = null!;
    private Button _btnCancel = null!;

    private List<Product> _currentList = new();

    public Product? SelectedProduct { get; private set; }

    public ProductPickerForm(ProductService service)
    {
        _service = service;

        Text = "상품 선택";
        Width = 800;
        Height = 500;
        StartPosition = FormStartPosition.CenterParent;

        InitializeControls();
        LoadProducts();
    }

    private void InitializeControls()
    {
        var lblKeyword = new Label
        {
            Text = "검색",
            Left = 10,
            Top = 15,
            AutoSize = true
        };
        _txtKeyword = new TextBox
        {
            Left = lblKeyword.Right + 5,
            Top = 10,
            Width = 200
        };

        var lblStatus = new Label
        {
            Text = "상태",
            Left = _txtKeyword.Right + 20,
            Top = 15,
            AutoSize = true
        };
        _cboStatus = new ComboBox
        {
            Left = lblStatus.Right + 5,
            Top = 10,
            Width = 120,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cboStatus.Items.AddRange(new[] { "전체", "판매중", "판매중지" });
        _cboStatus.SelectedIndex = 0;

        _btnSearch = new Button
        {
            Text = "검색",
            Left = _cboStatus.Right + 20,
            Top = 9,
            Width = 80
        };
        _btnSearch.Click += (_, _) => LoadProducts();

        _grid = new DataGridView
        {
            Left = 10,
            Top = 45,
            Width = ClientSize.Width - 20,
            Height = ClientSize.Height - 90,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoGenerateColumns = false
        };

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "코드",
            DataPropertyName = "ProductCode",
            Width = 100
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "상품명",
            DataPropertyName = "ProductName",
            Width = 250
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "플랜",
            DataPropertyName = "PlanName",
            Width = 100
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "소매가",
            DataPropertyName = "RetailPrice",
            Width = 80
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "상태",
            DataPropertyName = "Status",
            Width = 80
        });

        _grid.CellDoubleClick += (_, _) => SelectCurrent();

        _btnSelect = new Button
        {
            Text = "선택",
            Left = ClientSize.Width - 190,
            Top = ClientSize.Height - 35,
            Width = 80,
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom
        };
        _btnSelect.Click += (_, _) => SelectCurrent();

        _btnCancel = new Button
        {
            Text = "취소",
            Left = ClientSize.Width - 100,
            Top = ClientSize.Height - 35,
            Width = 80,
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom
        };
        _btnCancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        Controls.Add(lblKeyword);
        Controls.Add(_txtKeyword);
        Controls.Add(lblStatus);
        Controls.Add(_cboStatus);
        Controls.Add(_btnSearch);
        Controls.Add(_grid);
        Controls.Add(_btnSelect);
        Controls.Add(_btnCancel);
    }

    private void LoadProducts()
    {
        var all = _service.GetAll();

        var keyword = _txtKeyword.Text?.Trim();
        var statusFilter = _cboStatus.SelectedItem?.ToString();

        IEnumerable<Product> query = all;

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(p =>
                p.ProductName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                p.ProductCode.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        if (statusFilter == "판매중")
            query = query.Where(p => p.Status == "ACTIVE");
        else if (statusFilter == "판매중지")
            query = query.Where(p => p.Status == "INACTIVE");

        _currentList = query.ToList();
        _grid.DataSource = _currentList;
    }

    private void SelectCurrent()
    {
        if (_grid.CurrentRow?.DataBoundItem is Product p)
        {
            SelectedProduct = p;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
