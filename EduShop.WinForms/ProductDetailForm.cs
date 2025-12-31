using System;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using EduShop.Core.Common;
using EduShop.Core.Models;
using EduShop.Core.Services;

namespace EduShop.WinForms;

public partial class ProductDetailForm : Form
{
    private readonly ProductService _productService;
    private readonly UserContext    _currentUser;

    private Product? _product;

    // 입력 컨트롤
    private TextBox       _txtCode = null!;
    private TextBox       _txtName = null!;
    private TextBox       _txtPlan = null!;
    private ComboBox      _cboDuration = null!;
    private TextBox       _txtPurchaseUsd = null!;
    private TextBox       _txtPurchaseKrw = null!;
    private TextBox       _txtSaleKrw = null!;
    private ComboBox      _cboStatus = null!;
    private TextBox       _txtRemark = null!;
    private Button        _btnSave = null!;
    private Button        _btnCancel = null!;

    // 로그
    private DataGridView  _gridLogs = null!;

    public ProductDetailForm(ProductService productService, UserContext currentUser, Product? product)
    {
        _productService = productService;
        _currentUser    = currentUser;
        _product        = product;

        Text          = _product == null ? "상품 등록" : "상품 수정";
        Width         = 900;
        Height        = 600;
        StartPosition = FormStartPosition.CenterParent;

        InitializeControls();
        BindProduct();
        LoadLogs();
    }

    // ─────────────────────────────────────
    // UI 구성
    // ─────────────────────────────────────
    private void InitializeControls()
    {
        int leftLabel = 10;
        int leftInput = 110;
        int top       = 15;
        int rowHeight = 28;

        // 상품코드
        var lblCode = new Label
        {
            Text  = "상품코드",
            Left  = leftLabel,
            Top   = top + 4,
            Width = 90
        };
        _txtCode = new TextBox
        {
            Left  = leftInput,
            Top   = top,
            Width = 200
        };
        top += rowHeight;

        // 상품명
        var lblName = new Label
        {
            Text  = "상품명",
            Left  = leftLabel,
            Top   = top + 4,
            Width = 90
        };
        _txtName = new TextBox
        {
            Left  = leftInput,
            Top   = top,
            Width = 400
        };
        top += rowHeight;

        // 플랜명
        var lblPlan = new Label
        {
            Text  = "플랜명",
            Left  = leftLabel,
            Top   = top + 4,
            Width = 90
        };
        _txtPlan = new TextBox
        {
            Left  = leftInput,
            Top   = top,
            Width = 250
        };
        top += rowHeight;

        // 기간(개월)
        var lblDuration = new Label
        {
            Text  = "기간(개월)",
            Left  = leftLabel,
            Top   = top + 4,
            Width = 90
        };
        _cboDuration = new ComboBox
        {
            Left  = leftInput,
            Top   = top,
            Width = 120,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cboDuration.Items.AddRange(new object[] { 1, 2, 3, 4, 5, 6, 12 });
        _cboDuration.SelectedIndex = 0;
        top += rowHeight;

        // 매입가 USD / KRW
        var lblPurchaseUsd = new Label
        {
            Text  = "매입가(USD)",
            Left  = leftLabel,
            Top   = top + 4,
            Width = 90
        };
        _txtPurchaseUsd = new TextBox
        {
            Left  = leftInput,
            Top   = top,
            Width = 100
        };

        var lblPurchaseKrw = new Label
        {
            Text  = "매입가(원)",
            Left  = _txtPurchaseUsd.Right + 20,
            Top   = top + 4,
            Width = 90
        };
        _txtPurchaseKrw = new TextBox
        {
            Left  = lblPurchaseKrw.Right + 5,
            Top   = top,
            Width = 120
        };
        top += rowHeight;

        // 판매가(원)
        var lblSaleKrw = new Label
        {
            Text  = "판매가(원)",
            Left  = leftLabel,
            Top   = top + 4,
            Width = 90
        };
        _txtSaleKrw = new TextBox
        {
            Left  = leftInput,
            Top   = top,
            Width = 120
        };
        top += rowHeight;

        // 상태
        var lblStatus = new Label
        {
            Text  = "상태",
            Left  = leftLabel,
            Top   = top + 4,
            Width = 90
        };
        _cboStatus = new ComboBox
        {
            Left  = leftInput,
            Top   = top,
            Width = 140,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cboStatus.Items.Add(new StatusOption("ACTIVE", "판매중"));
        _cboStatus.Items.Add(new StatusOption("INACTIVE", "품절"));
        _cboStatus.SelectedIndex = 0;
        top += rowHeight;

        // 메모
        var lblRemark = new Label
        {
            Text  = "메모",
            Left  = leftLabel,
            Top   = top + 4,
            Width = 90
        };
        _txtRemark = new TextBox
        {
            Left       = leftInput,
            Top        = top,
            Width      = 500,
            Height     = 70,
            Multiline  = true,
            ScrollBars = ScrollBars.Vertical
        };
        top += 80;

        // 저장/취소 버튼
        _btnSave = new Button
        {
            Text   = "저장",
            Left   = Width - 220,
            Top    = top,
            Width  = 80,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnSave.Click += (_, _) => SaveProduct();

        _btnCancel = new Button
        {
            Text   = "취소",
            Left   = Width - 130,
            Top    = top,
            Width  = 80,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnCancel.Click += (_, _) => DialogResult = DialogResult.Cancel;

        // 로그 영역
        var lblLogs = new Label
        {
            Text  = "상품 로그",
            Left  = 10,
            Top   = top + 35,
            Width = 100
        };

        _gridLogs = new DataGridView
        {
            Left   = 10,
            Top    = lblLogs.Bottom + 5,
            Width  = ClientSize.Width - 20,
            Height = ClientSize.Height - (lblLogs.Bottom + 15),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect   = false,
            AutoGenerateColumns = false
        };

        _gridLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "일시",
            DataPropertyName = "EventTime",
            Width = 140,
            DefaultCellStyle = { Format = "yyyy-MM-dd HH:mm" }
        });
        _gridLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "액션",
            DataPropertyName = "ActionType",
            Width = 120
        });
        _gridLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "내용",
            DataPropertyName = "Description",
            Width = 350
        });
        _gridLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "작업자",
            DataPropertyName = "UserName",
            Width = 80
        });

        Controls.Add(lblCode);
        Controls.Add(_txtCode);
        Controls.Add(lblName);
        Controls.Add(_txtName);
        Controls.Add(lblPlan);
        Controls.Add(_txtPlan);
        Controls.Add(lblDuration);
        Controls.Add(_cboDuration);
        Controls.Add(lblPurchaseUsd);
        Controls.Add(_txtPurchaseUsd);
        Controls.Add(lblPurchaseKrw);
        Controls.Add(_txtPurchaseKrw);
        Controls.Add(lblSaleKrw);
        Controls.Add(_txtSaleKrw);
        Controls.Add(lblStatus);
        Controls.Add(_cboStatus);
        Controls.Add(lblRemark);
        Controls.Add(_txtRemark);
        Controls.Add(_btnSave);
        Controls.Add(_btnCancel);
        Controls.Add(lblLogs);
        Controls.Add(_gridLogs);
    }

    // ─────────────────────────────────────
    // 데이터 바인딩
    // ─────────────────────────────────────
    private void BindProduct()
    {
        if (_product == null)
        {
            _cboDuration.SelectedIndex = 0;
            _cboStatus.SelectedIndex = 0;
            return;
        }

        _txtCode.Text       = _product.ProductCode;
        _txtName.Text       = _product.ProductName;
        _txtPlan.Text       = _product.PlanName;
        _txtPurchaseUsd.Text = _product.PurchasePriceUsd?.ToString(CultureInfo.InvariantCulture) ?? "";
        _txtPurchaseKrw.Text = _product.PurchasePriceKrw?.ToString(CultureInfo.InvariantCulture) ?? "";
        _txtSaleKrw.Text     = _product.SalePriceKrw.ToString(CultureInfo.InvariantCulture);
        _txtRemark.Text     = _product.Remark ?? "";

        for (int i = 0; i < _cboDuration.Items.Count; i++)
        {
            if (_cboDuration.Items[i] is int value && value == _product.DurationMonths)
            {
                _cboDuration.SelectedIndex = i;
                break;
            }
        }

        var statusItem = _cboStatus.Items.Cast<StatusOption?>()
            .FirstOrDefault(item => item?.Value == _product.Status);
        _cboStatus.SelectedItem = statusItem ?? _cboStatus.Items[0];
    }

    private void LoadLogs()
    {
        if (_product == null || _product.ProductId <= 0)
        {
            _gridLogs.DataSource = null;
            return;
        }

        var logs = _productService.GetLogsForProduct(_product.ProductId);

        var view = logs
            .OrderByDescending(l => l.EventTime)
            .Select(l => new
            {
                l.EventTime,
                l.ActionType,
                l.Description,
                l.UserName
            })
            .ToList();

        _gridLogs.DataSource = view;
    }

    // ─────────────────────────────────────
    // 저장
    // ─────────────────────────────────────
    private void SaveProduct()
    {
        var code = _txtCode.Text.Trim();
        var name = _txtName.Text.Trim();

        if (string.IsNullOrWhiteSpace(code))
        {
            MessageBox.Show("상품코드를 입력하세요.", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _txtCode.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("상품명을 입력하세요.", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _txtName.Focus();
            return;
        }

        if (_cboDuration.SelectedItem is not int durationMonths)
        {
            MessageBox.Show("기간(개월) 옵션을 선택하세요.", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _cboDuration.Focus();
            return;
        }

        var purchaseUsd = ParseNullableDouble(_txtPurchaseUsd.Text);
        var purchaseKrw = ParseNullableLong(_txtPurchaseKrw.Text);
        var saleKrw = ParseLong(_txtSaleKrw.Text);
        var status = (_cboStatus.SelectedItem as StatusOption)?.Value ?? "ACTIVE";

        if (saleKrw <= 0)
        {
            MessageBox.Show("판매가(원)를 입력하세요.", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _txtSaleKrw.Focus();
            return;
        }

        if (_product == null)
        {
            var p = new Product
            {
                ProductCode     = code,
                ProductName     = name,
                PlanName        = _txtPlan.Text.Trim(),
                DurationMonths  = durationMonths,
                PurchasePriceUsd = purchaseUsd,
                PurchasePriceKrw = purchaseKrw,
                SalePriceKrw    = saleKrw,
                Status          = status,
                Remark          = _txtRemark.Text
            };

            _productService.Create(p, _currentUser);

            MessageBox.Show("상품이 등록되었습니다.", "완료",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            _product.ProductCode     = code;
            _product.ProductName     = name;
            _product.PlanName        = _txtPlan.Text.Trim();
            _product.DurationMonths  = durationMonths;
            _product.PurchasePriceUsd = purchaseUsd;
            _product.PurchasePriceKrw = purchaseKrw;
            _product.SalePriceKrw    = saleKrw;
            _product.Status          = status;
            _product.Remark          = _txtRemark.Text;

            // 이전 단계에서 만든 ProductService.Update(...) 메서드에 맞춰 호출
            _productService.Update(_product, _currentUser);

            MessageBox.Show("상품 정보가 수정되었습니다.", "완료",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    // ─────────────────────────────────────
    // 숫자 파싱 유틸
    // ─────────────────────────────────────
    private static long ParseLong(string text)
    {
        var t = text.Trim();
        if (string.IsNullOrEmpty(t)) return 0;

        if (long.TryParse(t, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
            return v;
        if (long.TryParse(t, NumberStyles.Any, CultureInfo.CurrentCulture, out v))
            return v;

        return 0;
    }

    private static double? ParseNullableDouble(string text)
    {
        var t = text.Trim();
        if (string.IsNullOrEmpty(t)) return null;

        if (double.TryParse(t, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
            return v;
        if (double.TryParse(t, NumberStyles.Any, CultureInfo.CurrentCulture, out v))
            return v;

        return null;
    }

    private static long? ParseNullableLong(string text)
    {
        var t = text.Trim();
        if (string.IsNullOrEmpty(t)) return null;

        if (long.TryParse(t, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
            return v;
        if (long.TryParse(t, NumberStyles.Any, CultureInfo.CurrentCulture, out v))
            return v;

        return null;
    }

    private sealed class StatusOption
    {
        public StatusOption(string value, string text)
        {
            Value = value;
            Text = text;
        }

        public string Value { get; }
        public string Text { get; }
        public override string ToString() => Text;
    }
}
