using System;
using System.Drawing;
using System.Windows.Forms;

namespace EduShop.WinForms;

public class ExtendSubscriptionForm : Form
{
    private readonly int _selectedCount;
    private NumericUpDown _numMonths = null!;
    private Button _btnOk = null!;
    private Button _btnCancel = null!;

    public int Months => (int)_numMonths.Value;

    public ExtendSubscriptionForm(int selectedCount)
    {
        _selectedCount = selectedCount;

        Text = "구독 기간 연장";
        Width = 320;
        Height = 170;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;

        InitializeControls();
    }

    private void InitializeControls()
    {
        var lblInfo = new Label
        {
            Text = _selectedCount > 0
                ? $"선택된 계정: {_selectedCount}개"
                : "선택된 계정 없음",
            Left = 15,
            Top = 15,
            AutoSize = true
        };

        var lblMonths = new Label
        {
            Text = "연장 개월 수",
            Left = 15,
            Top = lblInfo.Bottom + 20,
            AutoSize = true
        };

        _numMonths = new NumericUpDown
        {
            Left = lblMonths.Right + 10,
            Top = lblMonths.Top - 3,
            Width = 80,
            Minimum = 1,
            Maximum = 36,
            Value = 1,
            TextAlign = HorizontalAlignment.Right
        };

        _btnOk = new Button
        {
            Text = "확인",
            DialogResult = DialogResult.OK,
            Left = Width - 200,
            Top = lblMonths.Bottom + 25,
            Width = 80
        };

        _btnCancel = new Button
        {
            Text = "취소",
            DialogResult = DialogResult.Cancel,
            Left = _btnOk.Right + 10,
            Top = _btnOk.Top,
            Width = 80
        };

        Controls.Add(lblInfo);
        Controls.Add(lblMonths);
        Controls.Add(_numMonths);
        Controls.Add(_btnOk);
        Controls.Add(_btnCancel);

        AcceptButton = _btnOk;
        CancelButton = _btnCancel;
    }
}
