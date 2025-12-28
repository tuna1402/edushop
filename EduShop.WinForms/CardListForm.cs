using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using EduShop.Core.Common;
using EduShop.Core.Models;
using EduShop.Core.Services;

namespace EduShop.WinForms;

public class CardListForm : Form
{
    private readonly CardService _cardService;
    private readonly UserContext _currentUser;

    private TextBox _txtKeyword = null!;
    private ComboBox _cboStatus = null!;
    private Button _btnSearch = null!;
    private Button _btnReset = null!;

    private DataGridView _grid = null!;
    private Button _btnNew = null!;
    private Button _btnEdit = null!;
    private Button _btnDeactivate = null!;
    private Button _btnClose = null!;

    private ContextMenuStrip _ctxMenu = null!;

    private List<Card> _cards = new();

    private class CardRow
    {
        public long CardId { get; set; }
        public string CardName { get; set; } = "";
        public string? CardCompany { get; set; }
        public string? Last4Digits { get; set; }
        public string? OwnerName { get; set; }
        public string? OwnerType { get; set; }
        public int? BillingDay { get; set; }
        public string Status { get; set; } = "";
        public string? Memo { get; set; }
    }

    public CardListForm(CardService cardService, UserContext currentUser)
    {
        _cardService = cardService;
        _currentUser = currentUser;

        Text = "카드 관리";
        Width = 1000;
        Height = 600;
        StartPosition = FormStartPosition.CenterParent;

        InitializeControls();
        ReloadData();
    }

    private void InitializeControls()
    {
        var lblKeyword = new Label
        {
            Text = "검색",
            Left = 10,
            Top = 15,
            Width = 40
        };
        _txtKeyword = new TextBox
        {
            Left = lblKeyword.Right + 5,
            Top = 12,
            Width = 220
        };

        var lblStatus = new Label
        {
            Text = "상태",
            Left = _txtKeyword.Right + 20,
            Top = 15,
            Width = 40
        };
        _cboStatus = new ComboBox
        {
            Left = lblStatus.Right + 5,
            Top = 12,
            Width = 120,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cboStatus.Items.Add("");
        _cboStatus.Items.Add("ACTIVE");
        _cboStatus.Items.Add("INACTIVE");
        _cboStatus.SelectedIndex = 0;

        _btnSearch = new Button
        {
            Text = "조회",
            Left = _cboStatus.Right + 20,
            Top = 10,
            Width = 80
        };
        _btnSearch.Click += (_, _) => ReloadData();

        _btnReset = new Button
        {
            Text = "초기화",
            Left = _btnSearch.Right + 10,
            Top = 10,
            Width = 80
        };
        _btnReset.Click += (_, _) => ResetFilters();

        _grid = new DataGridView
        {
            Left = 10,
            Top = 45,
            Width = ClientSize.Width - 20,
            Height = ClientSize.Height - 110,
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
            HeaderText = "ID",
            DataPropertyName = "CardId",
            Width = 60
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "카드명",
            DataPropertyName = "CardName",
            Width = 180
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "카드사",
            DataPropertyName = "CardCompany",
            Width = 120
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "끝 4자리",
            DataPropertyName = "Last4Digits",
            Width = 80
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "소유자",
            DataPropertyName = "OwnerName",
            Width = 120
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "구분",
            DataPropertyName = "OwnerType",
            Width = 80
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "결제일",
            DataPropertyName = "BillingDay",
            Width = 70
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "상태",
            DataPropertyName = "Status",
            Width = 80
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "메모",
            DataPropertyName = "Memo",
            Width = 200
        });

        _grid.DoubleClick += (_, _) => EditSelected();
        _grid.KeyDown += GridOnKeyDown;

        _ctxMenu = new ContextMenuStrip();
        _ctxMenu.Items.Add("카드 수정", null, (_, _) => EditSelected());
        _ctxMenu.Items.Add("카드 비활성", null, (_, _) => DeactivateSelected());
        _grid.ContextMenuStrip = _ctxMenu;

        _btnNew = new Button
        {
            Text = "신규",
            Left = 10,
            Top = ClientSize.Height - 45,
            Width = 80,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnNew.Click += (_, _) => CreateNew();

        _btnEdit = new Button
        {
            Text = "수정",
            Left = _btnNew.Right + 10,
            Top = ClientSize.Height - 45,
            Width = 80,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnEdit.Click += (_, _) => EditSelected();

        _btnDeactivate = new Button
        {
            Text = "비활성",
            Left = _btnEdit.Right + 10,
            Top = ClientSize.Height - 45,
            Width = 80,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnDeactivate.Click += (_, _) => DeactivateSelected();

        _btnClose = new Button
        {
            Text = "닫기",
            Left = ClientSize.Width - 90,
            Top = ClientSize.Height - 45,
            Width = 80,
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom
        };
        _btnClose.Click += (_, _) => Close();

        Controls.AddRange(new Control[]
        {
            lblKeyword, _txtKeyword,
            lblStatus, _cboStatus,
            _btnSearch, _btnReset,
            _grid,
            _btnNew, _btnEdit, _btnDeactivate, _btnClose
        });
    }

    private void ReloadData()
    {
        _cards = _cardService.GetAll();

        IEnumerable<Card> query = _cards;

        var keyword = _txtKeyword.Text.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(c =>
                (c.CardName ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                (c.CardCompany ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                (c.OwnerName ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                (c.Last4Digits ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        if (_cboStatus.SelectedIndex > 0)
        {
            var status = _cboStatus.SelectedItem?.ToString() ?? "";
            query = query.Where(c => c.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        var rows = query
            .OrderByDescending(c => c.CardId)
            .Select(c => new CardRow
            {
                CardId = c.CardId,
                CardName = c.CardName,
                CardCompany = c.CardCompany,
                Last4Digits = c.Last4Digits,
                OwnerName = c.OwnerName,
                OwnerType = c.OwnerType,
                BillingDay = c.BillingDay,
                Status = c.Status,
                Memo = c.Memo
            })
            .ToList();

        _grid.DataSource = rows;
    }

    private void ResetFilters()
    {
        _txtKeyword.Text = "";
        _cboStatus.SelectedIndex = 0;
        ReloadData();
    }

    private Card? GetSelected()
    {
        if (_grid.CurrentRow?.DataBoundItem is not CardRow row)
            return null;

        return _cards.FirstOrDefault(c => c.CardId == row.CardId);
    }

    private void CreateNew()
    {
        using var dlg = new CardDetailForm(_cardService, _currentUser, null);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            ReloadData();
        }
    }

    private void EditSelected()
    {
        var selected = GetSelected();
        if (selected == null) return;

        using var dlg = new CardDetailForm(_cardService, _currentUser, selected);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            ReloadData();
        }
    }

    private void DeactivateSelected()
    {
        var selected = GetSelected();
        if (selected == null) return;

        if (selected.Status.Equals("INACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("이미 비활성 상태입니다.", "안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"카드 [{selected.CardName}] 상태를 INACTIVE로 변경하시겠습니까?",
            "확인",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        _cardService.ChangeStatus(selected.CardId, "INACTIVE", _currentUser);
        ReloadData();
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
            DeactivateSelected();
            e.Handled = true;
        }
    }
}
