using System;
using System.Globalization;
using System.Windows.Forms;
using EduShop.Core.Common;
using EduShop.Core.Models;
using EduShop.Core.Services;

namespace EduShop.WinForms;

public class CardDetailForm : Form
{
    private readonly CardService _cardService;
    private readonly UserContext _currentUser;
    private Card? _card;

    private TextBox _txtName = null!;
    private TextBox _txtCompany = null!;
    private TextBox _txtLast4 = null!;
    private TextBox _txtOwner = null!;
    private TextBox _txtOwnerType = null!;
    private TextBox _txtBillingDay = null!;
    private ComboBox _cboStatus = null!;
    private TextBox _txtMemo = null!;
    private Button _btnSave = null!;
    private Button _btnCancel = null!;

    public CardDetailForm(CardService cardService, UserContext currentUser, Card? card)
    {
        _cardService = cardService;
        _currentUser = currentUser;
        _card = card;

        Text = _card == null ? "카드 등록" : "카드 수정";
        Width = 520;
        Height = 430;
        StartPosition = FormStartPosition.CenterParent;

        InitializeControls();
        BindCard();
    }

    private void InitializeControls()
    {
        int leftLabel = 10;
        int leftInput = 120;
        int top = 15;
        int rowHeight = 28;

        var lblName = new Label { Text = "카드명", Left = leftLabel, Top = top + 4, Width = 90 };
        _txtName = new TextBox { Left = leftInput, Top = top, Width = 350 };
        top += rowHeight;

        var lblCompany = new Label { Text = "카드사", Left = leftLabel, Top = top + 4, Width = 90 };
        _txtCompany = new TextBox { Left = leftInput, Top = top, Width = 200 };
        top += rowHeight;

        var lblLast4 = new Label { Text = "끝 4자리", Left = leftLabel, Top = top + 4, Width = 90 };
        _txtLast4 = new TextBox { Left = leftInput, Top = top, Width = 120 };
        top += rowHeight;

        var lblOwner = new Label { Text = "소유자", Left = leftLabel, Top = top + 4, Width = 90 };
        _txtOwner = new TextBox { Left = leftInput, Top = top, Width = 200 };
        top += rowHeight;

        var lblOwnerType = new Label { Text = "소유자 구분", Left = leftLabel, Top = top + 4, Width = 90 };
        _txtOwnerType = new TextBox { Left = leftInput, Top = top, Width = 200 };
        top += rowHeight;

        var lblBillingDay = new Label { Text = "결제일", Left = leftLabel, Top = top + 4, Width = 90 };
        _txtBillingDay = new TextBox { Left = leftInput, Top = top, Width = 80 };
        var lblBillingHint = new Label { Text = "(1~31)", Left = _txtBillingDay.Right + 10, Top = top + 4, Width = 60 };
        top += rowHeight;

        var lblStatus = new Label { Text = "상태", Left = leftLabel, Top = top + 4, Width = 90 };
        _cboStatus = new ComboBox
        {
            Left = leftInput,
            Top = top,
            Width = 120,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cboStatus.Items.Add("ACTIVE");
        _cboStatus.Items.Add("INACTIVE");
        _cboStatus.SelectedIndex = 0;
        top += rowHeight;

        var lblMemo = new Label { Text = "메모", Left = leftLabel, Top = top + 4, Width = 90 };
        _txtMemo = new TextBox
        {
            Left = leftInput,
            Top = top,
            Width = 350,
            Height = 80,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical
        };
        top += 90;

        _btnSave = new Button
        {
            Text = "저장",
            Left = Width - 220,
            Top = top,
            Width = 90,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        _btnSave.Click += (_, _) => SaveCard();

        _btnCancel = new Button
        {
            Text = "닫기",
            Left = _btnSave.Right + 10,
            Top = top,
            Width = 90,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        _btnCancel.Click += (_, _) => Close();

        Controls.AddRange(new Control[]
        {
            lblName, _txtName,
            lblCompany, _txtCompany,
            lblLast4, _txtLast4,
            lblOwner, _txtOwner,
            lblOwnerType, _txtOwnerType,
            lblBillingDay, _txtBillingDay, lblBillingHint,
            lblStatus, _cboStatus,
            lblMemo, _txtMemo,
            _btnSave, _btnCancel
        });
    }

    private void BindCard()
    {
        if (_card == null) return;

        _txtName.Text = _card.CardName;
        _txtCompany.Text = _card.CardCompany ?? "";
        _txtLast4.Text = _card.Last4Digits ?? "";
        _txtOwner.Text = _card.OwnerName ?? "";
        _txtOwnerType.Text = _card.OwnerType ?? "";
        _txtBillingDay.Text = _card.BillingDay?.ToString(CultureInfo.InvariantCulture) ?? "";
        _txtMemo.Text = _card.Memo ?? "";

        var statusIndex = _cboStatus.Items.IndexOf(_card.Status);
        _cboStatus.SelectedIndex = statusIndex >= 0 ? statusIndex : 0;
    }

    private void SaveCard()
    {
        var cardName = _txtName.Text.Trim();
        if (string.IsNullOrWhiteSpace(cardName))
        {
            MessageBox.Show("카드명을 입력하세요.", "필수", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtName.Focus();
            return;
        }

        int? billingDay = null;
        var billingText = _txtBillingDay.Text.Trim();
        if (!string.IsNullOrWhiteSpace(billingText))
        {
            if (!int.TryParse(billingText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                || parsed < 1 || parsed > 31)
            {
                MessageBox.Show("결제일은 1~31 사이 숫자로 입력하세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtBillingDay.Focus();
                return;
            }

            billingDay = parsed;
        }

        var status = _cboStatus.SelectedItem?.ToString() ?? "ACTIVE";

        if (_card == null)
        {
            var newCard = new Card
            {
                CardName = cardName,
                CardCompany = _txtCompany.Text.Trim(),
                Last4Digits = _txtLast4.Text.Trim(),
                OwnerName = _txtOwner.Text.Trim(),
                OwnerType = _txtOwnerType.Text.Trim(),
                BillingDay = billingDay,
                Status = status,
                Memo = _txtMemo.Text.Trim()
            };

            _cardService.Create(newCard, _currentUser);
        }
        else
        {
            _card.CardName = cardName;
            _card.CardCompany = _txtCompany.Text.Trim();
            _card.Last4Digits = _txtLast4.Text.Trim();
            _card.OwnerName = _txtOwner.Text.Trim();
            _card.OwnerType = _txtOwnerType.Text.Trim();
            _card.BillingDay = billingDay;
            _card.Status = status;
            _card.Memo = _txtMemo.Text.Trim();

            _cardService.Update(_card, _currentUser);
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
