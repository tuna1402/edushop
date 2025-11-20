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
    private TextBox       _txtMonthlyUsd = null!;
    private TextBox       _txtMonthlyKrw = null!;
    private TextBox       _txtWholesale = null!;
    private TextBox       _txtRetail = null!;
    private TextBox       _txtPurchase = null!;
    private CheckBox      _chkYearly = null!;
    private NumericUpDown _numMinMonth = null!;
    private NumericUpDown _numMaxMonth = null!;
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

        // 월 구독료 USD / KRW
        var lblMonthlyUsd = new Label
        {
            Text  = "월 구독료(USD)",
            Left  = leftLabel,
            Top   = top + 4,
            Width = 90
        };
        _txtMonthlyUsd = new TextBox
        {
            Left  = leftInput,
            Top   = top,
            Width = 100
        };

        var lblMonthlyKrw = new Label
        {
            Text  = "월 구독료(원)",
            Left  = _txtMonthlyUsd.Right + 20,
            Top   = top + 4,
            Width = 90
        };
        _txtMonthlyKrw = new TextBox
        {
            Left  = lblMonthlyKrw.Right + 5,
            Top   = top,
            Width = 120
        };
        top += rowHeight;

        // 도매가 / 소매가 / 매입가
        var lblWholesale = new Label
        {
            Text  = "도매가",
            Left  = leftLabel,
            Top   = top + 4,
            Width = 90
        };
        _txtWholesale = new TextBox
        {
            Left  = leftInput,
            Top   = top,
            Width = 100
        };

        var lblRetail = new Label
        {
            Text  = "소매가",
            Left  = _txtWholesale.Right + 20,
            Top   = top + 4,
            Width = 60
        };
        _txtRetail = new TextBox
        {
            Left  = lblRetail.Right + 5,
            Top   = top,
            Width = 100
        };

        var lblPurchase = new Label
        {
            Text  = "매입가",
            Left  = _txtRetail.Right + 20,
            Top   = top + 4,
            Width = 60
        };
        _txtPurchase = new TextBox
        {
            Left  = lblPurchase.Right + 5,
            Top   = top,
            Width = 100
        };
        top += rowHeight;

        // 연 구독 / 기간(개월)
        _chkYearly = new CheckBox
        {
            Text  = "연 구독 가능",
            Left  = leftInput,
            Top   = top + 2,
            Width = 120
        };

        var lblMinMonth = new Label
        {
            Text  = "기간(개월)",
            Left  = _chkYearly.Right + 20,
            Top   = top + 4,
            Width = 70
        };
        _numMinMonth = new NumericUpDown
        {
            Left  = lblMinMonth.Right + 5,
            Top   = top,
            Width = 60,
            Minimum = 1,
            Maximum = 120,
            Value   = 1
        };
        var lblTilde = new Label
        {
            Text  = "~",
            Left  = _numMinMonth.Right + 5,
            Top   = top + 4,
            Width = 15
        };
        _numMaxMonth = new NumericUpDown
        {
            Left  = lblTilde.Right + 5,
            Top   = top,
            Width = 60,
            Minimum = 1,
            Maximum = 120,
            Value   = 12
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
        _cboStatus.Items.Add("ACTIVE");
        _cboStatus.Items.Add("INACTIVE");
        _cboStatus.Items.Add("STOPPED");
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
        Controls.Add(lblMonthlyUsd);
        Controls.Add(_txtMonthlyUsd);
        Controls.Add(lblMonthlyKrw);
        Controls.Add(_txtMonthlyKrw);
        Controls.Add(lblWholesale);
        Controls.Add(_txtWholesale);
        Controls.Add(lblRetail);
        Controls.Add(_txtRetail);
        Controls.Add(lblPurchase);
        Controls.Add(_txtPurchase);
        Controls.Add(_chkYearly);
        Controls.Add(lblMinMonth);
        Controls.Add(_numMinMonth);
        Controls.Add(lblTilde);
        Controls.Add(_numMaxMonth);
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
            _numMinMonth.Value   = 1;
            _numMaxMonth.Value   = 12;
            _chkYearly.Checked   = true;
            _cboStatus.SelectedItem = "ACTIVE";
            return;
        }

        _txtCode.Text       = _product.ProductCode;
        _txtName.Text       = _product.ProductName;
        _txtPlan.Text       = _product.PlanName;
        _txtMonthlyUsd.Text = _product.MonthlyFeeUsd.ToString() ?? "";
        _txtMonthlyKrw.Text = _product.MonthlyFeeKrw.ToString() ?? "";
        _txtWholesale.Text  = _product.WholesalePrice.ToString(CultureInfo.InvariantCulture);
        _txtRetail.Text     = _product.RetailPrice.ToString(CultureInfo.InvariantCulture);
        _txtPurchase.Text   = _product.PurchasePrice.ToString(CultureInfo.InvariantCulture);
        _chkYearly.Checked  = _product.YearlyAvailable;
        _numMinMonth.Value  = _product.MinMonth;
        _numMaxMonth.Value  = _product.MaxMonth;
        _txtRemark.Text     = _product.Remark ?? "";

        var idx = _cboStatus.Items.IndexOf(_product.Status);
        _cboStatus.SelectedIndex = idx >= 0 ? idx : 0;
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

        int minMonth = (int)_numMinMonth.Value;
        int maxMonth = (int)_numMaxMonth.Value;

        if (minMonth <= 0 || maxMonth <= 0 || minMonth > maxMonth)
        {
            MessageBox.Show("기간(개월) 범위를 확인하세요.", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _numMinMonth.Focus();
            return;
        }

        double monthlyUsd  = ParseDouble(_txtMonthlyUsd.Text);
        long   monthlyKrw  = ParseLong(_txtMonthlyKrw.Text);
        long   wholesale   = ParseLong(_txtWholesale.Text);
        long   retail      = ParseLong(_txtRetail.Text);
        long   purchase    = ParseLong(_txtPurchase.Text);
        bool   yearlyAvail = _chkYearly.Checked;
        string status      = (_cboStatus.SelectedItem as string) ?? "ACTIVE";

        if (_product == null)
        {
            var p = new Product
            {
                ProductCode     = code,
                ProductName     = name,
                PlanName        = _txtPlan.Text.Trim(),
                MonthlyFeeUsd   = monthlyUsd,
                MonthlyFeeKrw   = monthlyKrw,
                WholesalePrice  = wholesale,
                RetailPrice     = retail,
                PurchasePrice   = purchase,
                YearlyAvailable = yearlyAvail,
                MinMonth        = minMonth,
                MaxMonth        = maxMonth,
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
            _product.MonthlyFeeUsd   = monthlyUsd;
            _product.MonthlyFeeKrw   = monthlyKrw;
            _product.WholesalePrice  = wholesale;
            _product.RetailPrice     = retail;
            _product.PurchasePrice   = purchase;
            _product.YearlyAvailable = yearlyAvail;
            _product.MinMonth        = minMonth;
            _product.MaxMonth        = maxMonth;
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
    private static double ParseDouble(string text)
    {
        var t = text.Trim();
        if (string.IsNullOrEmpty(t)) return 0;

        if (double.TryParse(t, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
            return v;
        if (double.TryParse(t, NumberStyles.Any, CultureInfo.CurrentCulture, out v))
            return v;

        return 0;
    }

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
}
