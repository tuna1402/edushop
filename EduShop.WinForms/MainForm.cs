using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using EduShop.Core.Common;
using EduShop.Core.Models;
using EduShop.Core.Services;

namespace EduShop.WinForms;

public class MainForm : Form
{
    private readonly ProductService _service;
    private readonly UserContext _currentUser = new() { UserId = "admin", UserName = "사장" };

    private TextBox _txtNameFilter = null!;
    private ComboBox _cboStatus = null!;
    private Button _btnSearch = null!;
    private DataGridView _grid = null!;
    private Button _btnNew = null!;
    private Button _btnToggle = null!;
    private Button _btnLogs = null!;
    private Button _btnClose = null!;

    private List<Product> _currentList = new();

    public MainForm(ProductService service)
    {
        _service = service;

        Text = "EduShop 상품 관리";
        Width = 1000;
        Height = 600;
        StartPosition = FormStartPosition.CenterScreen;

        InitializeControls();
        LoadProducts();
    }

    private void InitializeControls()
    {
        // 상단 필터
        var lblName = new Label
        {
            Text = "상품명",
            Left = 10,
            Top = 15,
            AutoSize = true
        };
        _txtNameFilter = new TextBox
        {
            Left = lblName.Right + 5,
            Top = 10,
            Width = 200
        };

        var lblStatus = new Label
        {
            Text = "상태",
            Left = _txtNameFilter.Right + 20,
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

        // 그리드
        _grid = new DataGridView
        {
            Left = 10,
            Top = 45,
            Width = ClientSize.Width - 20,
            Height = ClientSize.Height - 100,
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
            HeaderText = "ID",
            DataPropertyName = "ProductId",
            Width = 60
        });
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
            HeaderText = "월(KRW)",
            DataPropertyName = "MonthlyFeeKrw",
            Width = 80
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "도매가",
            DataPropertyName = "WholesalePrice",
            Width = 80
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

        _grid.CellDoubleClick += (_, _) =>
        {
            MessageBox.Show("지금은 더블클릭 수정은 없고,\n[신규]/[판매중/중지]/[로그] 버튼만 동작합니다.", "안내");
        };

        // 하단 버튼들
        _btnNew = new Button
        {
            Text = "신규",
            Left = 10,
            Top = ClientSize.Height - 45,
            Width = 80,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnNew.Click += (_, _) => NewProduct();

        _btnToggle = new Button
        {
            Text = "판매중/중지",
            Left = _btnNew.Right + 10,
            Top = ClientSize.Height - 45,
            Width = 100,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnToggle.Click += (_, _) => ToggleStatus();

        _btnLogs = new Button
        {
            Text = "로그",
            Left = _btnToggle.Right + 10,
            Top = ClientSize.Height - 45,
            Width = 80,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnLogs.Click += (_, _) => ShowLogs();

        _btnClose = new Button
        {
            Text = "닫기",
            Width = 80,
            Top = ClientSize.Height - 45,
            Left = ClientSize.Width - 90,
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom
        };
        _btnClose.Click += (_, _) => Close();

        Controls.Add(lblName);
        Controls.Add(_txtNameFilter);
        Controls.Add(lblStatus);
        Controls.Add(_cboStatus);
        Controls.Add(_btnSearch);
        Controls.Add(_grid);
        Controls.Add(_btnNew);
        Controls.Add(_btnToggle);
        Controls.Add(_btnLogs);
        Controls.Add(_btnClose);
    }

    private void LoadProducts()
    {
        var all = _service.GetAll();

        var nameFilter = _txtNameFilter.Text?.Trim();
        var statusFilter = _cboStatus.SelectedItem?.ToString();

        IEnumerable<Product> query = all;

        if (!string.IsNullOrWhiteSpace(nameFilter))
        {
            query = query.Where(p =>
                p.ProductName.Contains(nameFilter, StringComparison.OrdinalIgnoreCase) ||
                p.ProductCode.Contains(nameFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (statusFilter == "판매중")
            query = query.Where(p => p.Status == "ACTIVE");
        else if (statusFilter == "판매중지")
            query = query.Where(p => p.Status == "INACTIVE");

        _currentList = query.ToList();
        _grid.DataSource = _currentList;
    }

    private Product? GetSelected()
    {
        if (_grid.CurrentRow?.DataBoundItem is Product p)
            return p;
        return null;
    }

    private void NewProduct()
    {
        using var dlg = new ProductDetailForm();
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Product != null)
        {
            try
            {
                _service.Create(dlg.Product, _currentUser);
                LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"저장 중 오류: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void ToggleStatus()
    {
        var selected = GetSelected();
        if (selected == null)
        {
            MessageBox.Show("상태를 변경할 상품을 선택하세요.");
            return;
        }

        var newStatus = selected.Status == "ACTIVE" ? "INACTIVE" : "ACTIVE";
        var text = newStatus == "ACTIVE" ? "판매중" : "판매중지";

        if (MessageBox.Show(
                $"{selected.ProductName} 을(를) {text} 상태로 변경하시겠습니까?",
                "확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
        {
            try
            {
                _service.ChangeStatus(selected.ProductId, newStatus, _currentUser);
                LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"상태 변경 중 오류: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void ShowLogs()
    {
        var selected = GetSelected();
        if (selected == null)
        {
            MessageBox.Show("로그를 볼 상품을 선택하세요.");
            return;
        }

        using var dlg = new ProductLogForm(_service, selected);
        dlg.ShowDialog(this);
    }
}
