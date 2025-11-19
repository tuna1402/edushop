using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using EduShop.Core.Common;
using EduShop.Core.Models;
using EduShop.Core.Services;

namespace EduShop.WinForms;

public class CustomerListForm : Form
{
    private readonly CustomerService _customerService;
    private readonly UserContext     _currentUser;

    private TextBox      _txtName = null!;
    private Button       _btnSearch = null!;
    private Button       _btnReset = null!;
    private DataGridView _grid = null!;
    private Button       _btnNew = null!;
    private Button       _btnEdit = null!;
    private Button       _btnDelete = null!;
    private Button       _btnClose = null!;

    private List<Customer> _customers = new();

    private class CustomerRow
    {
        public long   CustomerId   { get; set; }
        public string CustomerName { get; set; } = "";
        public string? ContactName { get; set; }
        public string? Phone       { get; set; }
        public string? Email       { get; set; }
        public string? Address     { get; set; }
        public string? Memo        { get; set; }
    }

    public CustomerListForm(CustomerService customerService, UserContext currentUser)
    {
        _customerService = customerService;
        _currentUser     = currentUser;

        Text = "고객 관리";
        Width = 900;
        Height = 600;
        StartPosition = FormStartPosition.CenterParent;

        InitializeControls();
        ReloadData();
    }

    private void InitializeControls()
    {
        var lblName = new Label
        {
            Text = "고객명",
            Left = 10,
            Top  = 15,
            Width = 60
        };
        _txtName = new TextBox
        {
            Left = lblName.Right + 5,
            Top  = 10,
            Width = 200
        };

        _btnSearch = new Button
        {
            Text = "조회",
            Left = _txtName.Right + 10,
            Top  = 8,
            Width = 80
        };
        _btnSearch.Click += (_, _) => ReloadData();

        _btnReset = new Button
        {
            Text = "초기화",
            Left = _btnSearch.Right + 10,
            Top  = 8,
            Width = 80
        };
        _btnReset.Click += (_, _) => ResetFilters();

        _grid = new DataGridView
        {
            Left = 10,
            Top  = 45,
            Width  = ClientSize.Width - 20,
            Height = ClientSize.Height - 110,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect   = false,
            AutoGenerateColumns = false
        };

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "ID",
            DataPropertyName = "CustomerId",
            Width = 60
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "고객명",
            DataPropertyName = "CustomerName",
            Width = 200
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "담당자",
            DataPropertyName = "ContactName",
            Width = 120
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "전화",
            DataPropertyName = "Phone",
            Width = 120
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "이메일",
            DataPropertyName = "Email",
            Width = 180
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "메모",
            DataPropertyName = "Memo",
            Width = 200
        });

        _grid.DoubleClick += (_, _) => EditSelected();
        _grid.KeyDown += GridOnKeyDown;

        _btnNew = new Button
        {
            Text = "신규",
            Left = 10,
            Top  = ClientSize.Height - 45,
            Width = 80,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnNew.Click += (_, _) => CreateNew();

        _btnEdit = new Button
        {
            Text = "수정",
            Left = _btnNew.Right + 10,
            Top  = ClientSize.Height - 45,
            Width = 80,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnEdit.Click += (_, _) => EditSelected();

        _btnDelete = new Button
        {
            Text = "삭제",
            Left = _btnEdit.Right + 10,
            Top  = ClientSize.Height - 45,
            Width = 80,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnDelete.Click += (_, _) => DeleteSelected();

        _btnClose = new Button
        {
            Text = "닫기",
            Left = ClientSize.Width - 100,
            Top  = ClientSize.Height - 45,
            Width = 80,
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom
        };
        _btnClose.Click += (_, _) => Close();

        Controls.Add(lblName);
        Controls.Add(_txtName);
        Controls.Add(_btnSearch);
        Controls.Add(_btnReset);
        Controls.Add(_grid);
        Controls.Add(_btnNew);
        Controls.Add(_btnEdit);
        Controls.Add(_btnDelete);
        Controls.Add(_btnClose);
    }

    private void GridOnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            EditSelected();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Delete)
        {
            DeleteSelected();
            e.Handled = true;
        }
    }

    private void ResetFilters()
    {
        _txtName.Text = "";
        ReloadData();
    }

    private void ReloadData()
    {
        _customers = _customerService.GetAll();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        IEnumerable<Customer> query = _customers;

        var name = _txtName.Text.Trim();
        if (!string.IsNullOrEmpty(name))
        {
            query = query.Where(c =>
                c.CustomerName.Contains(name, StringComparison.OrdinalIgnoreCase));
        }

        var list = query
            .OrderBy(c => c.CustomerName)
            .ThenBy(c => c.CustomerId)
            .Select(c => new CustomerRow
            {
                CustomerId   = c.CustomerId,
                CustomerName = c.CustomerName,
                ContactName  = c.ContactName,
                Phone        = c.Phone,
                Email        = c.Email,
                Address      = c.Address,
                Memo         = c.Memo
            })
            .ToList();

        _grid.DataSource = list;
    }

    private Customer? GetSelected()
    {
        if (_grid.CurrentRow?.DataBoundItem is not CustomerRow row) return null;
        return _customers.FirstOrDefault(c => c.CustomerId == row.CustomerId);
    }

    private void CreateNew()
    {
        using var dlg = new CustomerEditForm(_customerService, _currentUser, null);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            ReloadData();
        }
    }

    private void EditSelected()
    {
        var c = GetSelected();
        if (c == null) return;

        using var dlg = new CustomerEditForm(_customerService, _currentUser, c.CustomerId);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            ReloadData();
        }
    }

    private void DeleteSelected()
    {
        var c = GetSelected();
        if (c == null) return;

        var result = MessageBox.Show(
            $"고객 [{c.CustomerName}] 을(를) 삭제(비활성) 하시겠습니까?",
            "확인",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
            return;

        _customerService.SoftDelete(c.CustomerId, _currentUser);
        ReloadData();
    }
}
