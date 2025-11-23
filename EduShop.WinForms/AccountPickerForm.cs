using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using EduShop.Core.Models;
using EduShop.Core.Services;

namespace EduShop.WinForms;

public class AccountPickerForm : Form
{
    private readonly AccountService _accountService;
    private readonly long? _productId;
    private readonly long? _currentOrderId;

    private DataGridView _grid = null!;
    private TextBox _txtEmail = null!;
    private ComboBox _cboStatus = null!;
    private Button _btnSearch = null!;

    public List<long> SelectedAccountIds { get; } = new();

    private List<Account> _accounts = new();

    public AccountPickerForm(AccountService accountService, long? productId, long? currentOrderId)
    {
        _accountService = accountService;
        _productId      = productId;
        _currentOrderId = currentOrderId;

        Text = "계정 선택";
        Width = 900;
        Height = 520;
        StartPosition = FormStartPosition.CenterParent;

        InitializeControls();
        LoadAccounts();
    }

    private void InitializeControls()
    {
        var lblEmail = new Label
        {
            Text = "이메일",
            Left = 10,
            Top = 15,
            Width = 50
        };
        _txtEmail = new TextBox
        {
            Left = lblEmail.Right + 5,
            Top = 10,
            Width = 220
        };

        var lblStatus = new Label
        {
            Text = "상태",
            Left = _txtEmail.Right + 10,
            Top = 15,
            Width = 40
        };
        _cboStatus = new ComboBox
        {
            Left = lblStatus.Right + 5,
            Top = 10,
            Width = 160,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cboStatus.Items.Add("");
        foreach (var item in AccountStatusHelper.GetAll())
        {
            _cboStatus.Items.Add(item.Code);
        }
        _cboStatus.SelectedIndex = 0;

        _btnSearch = new Button
        {
            Text = "검색",
            Left = _cboStatus.Right + 10,
            Top = 8,
            Width = 70
        };
        _btnSearch.Click += (_, _) => ApplyFilter();

        _grid = new DataGridView
        {
            Left = 10,
            Top = 40,
            Width = ClientSize.Width - 20,
            Height = ClientSize.Height - 110,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoGenerateColumns = false,
            MultiSelect = true
        };

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "계정ID",
            Name = "AccountId",
            DataPropertyName = "AccountId",
            Width = 80
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "이메일",
            Name = "Email",
            DataPropertyName = "Email",
            Width = 220
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "상태",
            Name = "Status",
            DataPropertyName = "Status",
            Width = 120
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "상품",
            Name = "ProductId",
            DataPropertyName = "ProductId",
            Width = 80
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "시작일",
            Name = "StartDate",
            DataPropertyName = "StartDate",
            Width = 100,
            DefaultCellStyle = { Format = "yyyy-MM-dd" }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "만료일",
            Name = "EndDate",
            DataPropertyName = "EndDate",
            Width = 100,
            DefaultCellStyle = { Format = "yyyy-MM-dd" }
        });

        var btnOk = new Button
        {
            Text = "선택 완료",
            Left = ClientSize.Width - 190,
            Top = ClientSize.Height - 60,
            Width = 80,
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom
        };
        btnOk.Click += (_, _) => ConfirmSelection();

        var btnCancel = new Button
        {
            Text = "취소",
            Left = ClientSize.Width - 100,
            Top = ClientSize.Height - 60,
            Width = 80,
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom
        };
        btnCancel.Click += (_, _) => DialogResult = DialogResult.Cancel;

        Controls.Add(lblEmail);
        Controls.Add(_txtEmail);
        Controls.Add(lblStatus);
        Controls.Add(_cboStatus);
        Controls.Add(_btnSearch);
        Controls.Add(_grid);
        Controls.Add(btnOk);
        Controls.Add(btnCancel);
    }

    private void LoadAccounts()
    {
        _accounts = _accountService.GetAssignableAccountsForOrder(_productId, _currentOrderId);
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var emailFilter = _txtEmail.Text.Trim();
        var statusFilter = _cboStatus.SelectedItem?.ToString();

        var filtered = _accounts.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(emailFilter))
        {
            filtered = filtered.Where(a => a.Email.Contains(emailFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            filtered = filtered.Where(a => a.Status.Equals(statusFilter, StringComparison.OrdinalIgnoreCase));
        }

        var rows = filtered
            .Select(a => new
            {
                a.AccountId,
                a.Email,
                Status = AccountStatusHelper.ToDisplay(a.Status),
                a.ProductId,
                StartDate = a.SubscriptionStartDate,
                EndDate = a.SubscriptionEndDate
            })
            .ToList();

        _grid.DataSource = rows;
    }

    private void ConfirmSelection()
    {
        if (_grid.SelectedRows.Count == 0)
        {
            MessageBox.Show("계정을 선택하세요.", "안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var ids = _grid.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(r => (long)r.Cells["AccountId"].Value)
            .ToList();

        SelectedAccountIds.Clear();
        SelectedAccountIds.AddRange(ids);

        DialogResult = DialogResult.OK;
    }
}
