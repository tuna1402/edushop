using System;
using System.Windows.Forms;
using EduShop.Core.Models;

namespace EduShop.WinForms;

public class ProductDetailForm : Form
{
    public Product? Product { get; private set; }

    private TextBox _txtCode = null!;
    private TextBox _txtName = null!;
    private TextBox _txtPlan = null!;
    private CheckBox _chkYearly = null!;
    private TextBox _txtMinMonth = null!;
    private TextBox _txtMaxMonth = null!;
    private TextBox _txtMonthKrw = null!;
    private TextBox _txtWholesale = null!;
    private TextBox _txtRetail = null!;
    private TextBox _txtPurchase = null!;
    private ComboBox _cboStatus = null!;
    private TextBox _txtRemark = null!;
    private Button _btnSave = null!;
    private Button _btnCancel = null!;

    public ProductDetailForm()
    {
        Text = "상품 등록";
        Width = 500;
        Height = 500;
        StartPosition = FormStartPosition.CenterParent;

        InitializeControls();
    }

    private void InitializeControls()
    {
        int labelWidth = 90;
        int leftLabel = 20;
        int leftInput = leftLabel + labelWidth + 5;
        int top = 20;
        int lineHeight = 28;

        Label MakeLabel(string text)
        {
            var lbl = new Label
            {
                Text = text,
                Left = leftLabel,
                Top = top + 4,
                Width = labelWidth
            };
            Controls.Add(lbl);
            return lbl;
        }

        _txtCode = new TextBox { Left = leftInput, Top = top, Width = 150 };
        MakeLabel("상품코드");
        Controls.Add(_txtCode);
        top += lineHeight;

        _txtName = new TextBox { Left = leftInput, Top = top, Width = 250 };
        MakeLabel("상품명");
        Controls.Add(_txtName);
        top += lineHeight;

        _txtPlan = new TextBox { Left = leftInput, Top = top, Width = 150 };
        MakeLabel("플랜명");
        Controls.Add(_txtPlan);
        top += lineHeight;

        _chkYearly = new CheckBox { Left = leftInput, Top = top + 4, Width = 20, Checked = true };
        MakeLabel("연 구독 가능");
        Controls.Add(_chkYearly);
        top += lineHeight;

        _txtMinMonth = new TextBox { Left = leftInput, Top = top, Width = 50, Text = "1" };
        MakeLabel("최소개월");
        Controls.Add(_txtMinMonth);
        top += lineHeight;

        _txtMaxMonth = new TextBox { Left = leftInput, Top = top, Width = 50, Text = "12" };
        MakeLabel("최대개월");
        Controls.Add(_txtMaxMonth);
        top += lineHeight;

        _txtMonthKrw = new TextBox { Left = leftInput, Top = top, Width = 100, Text = "0" };
        MakeLabel("월 구독료(원)");
        Controls.Add(_txtMonthKrw);
        top += lineHeight;

        _txtWholesale = new TextBox { Left = leftInput, Top = top, Width = 100, Text = "0" };
        MakeLabel("도매가");
        Controls.Add(_txtWholesale);
        top += lineHeight;

        _txtRetail = new TextBox { Left = leftInput, Top = top, Width = 100, Text = "0" };
        MakeLabel("소매가");
        Controls.Add(_txtRetail);
        top += lineHeight;

        _txtPurchase = new TextBox { Left = leftInput, Top = top, Width = 100, Text = "0" };
        MakeLabel("매입가");
        Controls.Add(_txtPurchase);
        top += lineHeight;

        _cboStatus = new ComboBox
        {
            Left = leftInput,
            Top = top,
            Width = 120,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cboStatus.Items.AddRange(new[] { "판매중", "판매중지" });
        _cboStatus.SelectedIndex = 0;
        MakeLabel("상태");
        Controls.Add(_cboStatus);
        top += lineHeight;

        _txtRemark = new TextBox
        {
            Left = leftInput,
            Top = top,
            Width = 250,
            Height = 60,
            Multiline = true
        };
        MakeLabel("비고");
        Controls.Add(_txtRemark);
        top += 70;

        _btnSave = new Button
        {
            Text = "저장",
            Left = Width - 200,
            Top = top + 10,
            Width = 80,
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom
        };
        _btnSave.Click += (_, _) => Save();
        Controls.Add(_btnSave);

        _btnCancel = new Button
        {
            Text = "취소",
            Left = Width - 110,
            Top = top + 10,
            Width = 80,
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom
        };
        _btnCancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        Controls.Add(_btnCancel);
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(_txtCode.Text))
        {
            MessageBox.Show("상품코드를 입력하세요.");
            _txtCode.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtName.Text))
        {
            MessageBox.Show("상품명을 입력하세요.");
            _txtName.Focus();
            return;
        }

        if (!int.TryParse(_txtMinMonth.Text, out var minMonth) || minMonth <= 0)
        {
            MessageBox.Show("최소개월을 올바르게 입력하세요.");
            _txtMinMonth.Focus();
            return;
        }

        if (!int.TryParse(_txtMaxMonth.Text, out var maxMonth) || maxMonth < minMonth)
        {
            MessageBox.Show("최대개월은 최소개월 이상이어야 합니다.");
            _txtMaxMonth.Focus();
            return;
        }

        if (!long.TryParse(_txtMonthKrw.Text, out var monthKrw) || monthKrw < 0 ||
            !long.TryParse(_txtWholesale.Text, out var wholesale) || wholesale < 0 ||
            !long.TryParse(_txtRetail.Text, out var retail) || retail < 0 ||
            !long.TryParse(_txtPurchase.Text, out var purchase) || purchase < 0)
        {
            MessageBox.Show("금액 필드를 올바르게 입력하세요.");
            return;
        }

        var status = _cboStatus.SelectedItem?.ToString() switch
        {
            "판매중"   => "ACTIVE",
            "판매중지" => "INACTIVE",
            _          => "ACTIVE"
        };

        Product = new Product
        {
            ProductCode     = _txtCode.Text.Trim(),
            ProductName     = _txtName.Text.Trim(),
            PlanName        = string.IsNullOrWhiteSpace(_txtPlan.Text) ? null : _txtPlan.Text.Trim(),
            YearlyAvailable = _chkYearly.Checked,
            MinMonth        = minMonth,
            MaxMonth        = maxMonth,
            MonthlyFeeKrw   = monthKrw,
            WholesalePrice  = wholesale,
            RetailPrice     = retail,
            PurchasePrice   = purchase,
            Status          = status,
            Remark          = string.IsNullOrWhiteSpace(_txtRemark.Text) ? null : _txtRemark.Text.Trim()
        };

        DialogResult = DialogResult.OK;
        Close();
    }
}
