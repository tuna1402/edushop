using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using EduShop.Core.Common;
using EduShop.Core.Models;
using EduShop.Core.Services;

namespace EduShop.WinForms;

public class SaleDetailForm : Form
{
    private readonly SalesService   _salesService;
    private readonly AccountService _accountService;
    private readonly UserContext    _currentUser;

    private readonly long _saleId;

    private Label _lblHeader = null!;
    private DataGridView _gridItems = null!;
    private DataGridView _gridAccounts = null!;
    private Button _btnAddAccount = null!;
    private Button _btnRemoveAccount = null!;
    private Button _btnClose = null!;

    private SaleHeader? _currentSale;
    private List<SaleItem> _currentItems = new();

    public SaleDetailForm(SalesService salesService, AccountService accountService, UserContext currentUser, long saleId)
    {
        _salesService   = salesService;
        _accountService = accountService;
        _currentUser    = currentUser;
        _saleId         = saleId;

        Text = "주문/견적 상세";
        Width = 900;
        Height = 650;
        StartPosition = FormStartPosition.CenterParent;

        InitializeControls();
        LoadSale();
    }

    private void InitializeControls()
    {
        _lblHeader = new Label
        {
            Left = 10,
            Top = 10,
            Width = ClientSize.Width - 20,
            AutoSize = false
        };

        _gridItems = new DataGridView
        {
            Left = 10,
            Top = 40,
            Width = ClientSize.Width - 20,
            Height = 220,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoGenerateColumns = false
        };

        _gridItems.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "코드",
            DataPropertyName = "ProductCode",
            Width = 100
        });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "상품명",
            DataPropertyName = "ProductName",
            Width = 260
        });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "단가",
            DataPropertyName = "UnitPrice",
            Width = 90
        });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "수량",
            DataPropertyName = "Quantity",
            Width = 70
        });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "금액",
            DataPropertyName = "LineTotal",
            Width = 100
        });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "마진",
            DataPropertyName = "LineProfit",
            Width = 100
        });

        _gridAccounts = new DataGridView
        {
            Left = 10,
            Top = _gridItems.Bottom + 10,
            Width = ClientSize.Width - 20,
            Height = 250,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoGenerateColumns = false,
            MultiSelect = true
        };

        _gridAccounts.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "이메일",
            DataPropertyName = "Email",
            Width = 200
        });
        _gridAccounts.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "상태",
            DataPropertyName = "Status",
            Width = 100
        });
        _gridAccounts.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "시작일",
            DataPropertyName = "StartDate",
            Width = 90,
            DefaultCellStyle = { Format = "yyyy-MM-dd" }
        });
        _gridAccounts.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "만료일",
            DataPropertyName = "EndDate",
            Width = 90,
            DefaultCellStyle = { Format = "yyyy-MM-dd" }
        });
        _gridAccounts.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "납품일",
            DataPropertyName = "DeliveryDate",
            Width = 90,
            DefaultCellStyle = { Format = "yyyy-MM-dd" }
        });
        _gridAccounts.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "메모",
            DataPropertyName = "Memo",
            Width = 220
        });

        _btnAddAccount = new Button
        {
            Text = "계정 추가",
            Width = 90,
            Left = 10,
            Top = ClientSize.Height - 40,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnAddAccount.Click += (_, _) => AddAccounts();

        _btnRemoveAccount = new Button
        {
            Text = "선택 계정 해제",
            Width = 110,
            Left = _btnAddAccount.Right + 10,
            Top = ClientSize.Height - 40,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnRemoveAccount.Click += (_, _) => RemoveAccounts();

        _btnClose = new Button
        {
            Text = "닫기",
            Width = 80,
            Left = ClientSize.Width - 90,
            Top = ClientSize.Height - 40,
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom
        };
        _btnClose.Click += (_, _) => Close();

        Controls.Add(_lblHeader);
        Controls.Add(_gridItems);
        Controls.Add(_gridAccounts);
        Controls.Add(_btnAddAccount);
        Controls.Add(_btnRemoveAccount);
        Controls.Add(_btnClose);
    }

    private void LoadSale()
    {
        _currentSale = _salesService.GetSale(_saleId);
        if (_currentSale == null)
        {
            MessageBox.Show("주문/견적 정보를 찾을 수 없습니다.", "안내",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
            return;
        }

        var headerText = $"번호: {_currentSale.SaleId} / 일자: {_currentSale.SaleDate:yyyy-MM-dd} / 고객: {_currentSale.CustomerName ?? "(무기명)"}";
        _lblHeader.Text = headerText;

        _currentItems = _salesService.GetSaleItems(_saleId);
        _gridItems.DataSource = null;
        _gridItems.DataSource = _currentItems;

        LoadAccounts();
    }

    private void LoadAccounts()
    {
        var accounts = _accountService.GetByOrderId(_saleId);
        var rows = accounts.Select(a => new AccountRow
        {
            AccountId   = a.AccountId,
            Email       = a.Email,
            Status      = AccountStatusHelper.ToDisplay(a.Status),
            StartDate   = a.SubscriptionStartDate,
            EndDate     = a.SubscriptionEndDate,
            DeliveryDate = a.DeliveryDate,
            Memo        = a.Memo
        }).ToList();

        _gridAccounts.DataSource = null;
        _gridAccounts.DataSource = rows;
    }

    private void AddAccounts()
    {
        if (_currentSale == null)
        {
            MessageBox.Show("주문/견적 정보를 불러온 후에 계정을 배정할 수 있습니다.", "안내",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        long? productId = null;
        var productIds = _currentItems
            .Where(i => i.ProductId.HasValue)
            .Select(i => i.ProductId!.Value)
            .Distinct()
            .ToList();

        if (productIds.Count == 1)
            productId = productIds[0];

        using var dlg = new AccountPickerForm(_accountService, productId, _saleId);
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        if (dlg.SelectedAccountIds.Count == 0)
            return;

        try
        {
            _accountService.AssignToOrder(
                _saleId,
                _currentSale.SaleId.ToString(),
                null,
                dlg.SelectedAccountIds,
                _currentUser);

            LoadAccounts();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"계정 배정 중 오류: {ex.Message}", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RemoveAccounts()
    {
        if (_gridAccounts.SelectedRows.Count == 0)
        {
            MessageBox.Show("해제할 계정을 선택하세요.", "안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var ids = _gridAccounts.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(r => ((AccountRow)r.DataBoundItem).AccountId)
            .ToList();

        if (ids.Count == 0)
            return;

        var confirm = MessageBox.Show(
            $"선택한 {ids.Count}개 계정을 이 주문에서 해제하시겠습니까?",
            "계정 해제 확인",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
            return;

        try
        {
            _accountService.UnassignFromOrder(_saleId, ids, _currentUser);
            LoadAccounts();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"계정 해제 중 오류: {ex.Message}", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private class AccountRow
    {
        public long AccountId { get; set; }
        public string Email { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? Memo { get; set; }
    }
}
