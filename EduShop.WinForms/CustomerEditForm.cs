using System;
using System.Windows.Forms;
using EduShop.Core.Common;
using EduShop.Core.Models;
using EduShop.Core.Services;

namespace EduShop.WinForms;

public class CustomerEditForm : Form
{
    private readonly CustomerService _customerService;
    private readonly UserContext     _currentUser;

    private Customer? _customer;

    private TextBox _txtName = null!;
    private TextBox _txtContact = null!;
    private TextBox _txtPhone = null!;
    private TextBox _txtEmail = null!;
    private TextBox _txtAddress = null!;
    private TextBox _txtMemo = null!;
    private Button  _btnSave = null!;
    private Button  _btnCancel = null!;

    public CustomerEditForm(CustomerService customerService, UserContext currentUser, long? customerId)
    {
        _customerService = customerService;
        _currentUser     = currentUser;

        if (customerId.HasValue)
        {
            _customer = _customerService.Get(customerId.Value);
        }

        Text = _customer == null ? "고객 등록" : "고객 수정";
        Width = 600;
        Height = 450;
        StartPosition = FormStartPosition.CenterParent;

        InitializeControls();
        BindCustomer();
    }

    private void InitializeControls()
    {
        int leftLabel = 10;
        int leftInput = 110;
        int top       = 15;
        int rowHeight = 28;

        var lblName = new Label
        {
            Text  = "고객명",
            Left  = leftLabel,
            Top   = top + 4,
            Width = 90
        };
        _txtName = new TextBox
        {
            Left  = leftInput,
            Top   = top,
            Width = 350
        };
        top += rowHeight;

        var lblContact = new Label
        {
            Text  = "담당자",
            Left  = leftLabel,
            Top   = top + 4,
            Width = 90
        };
        _txtContact = new TextBox
        {
            Left  = leftInput,
            Top   = top,
            Width = 250
        };
        top += rowHeight;

        var lblPhone = new Label
        {
            Text  = "전화",
            Left  = leftLabel,
            Top   = top + 4,
            Width = 90
        };
        _txtPhone = new TextBox
        {
            Left  = leftInput,
            Top   = top,
            Width = 250
        };
        top += rowHeight;

        var lblEmail = new Label
        {
            Text  = "이메일",
            Left  = leftLabel,
            Top   = top + 4,
            Width = 90
        };
        _txtEmail = new TextBox
        {
            Left  = leftInput,
            Top   = top,
            Width = 350
        };
        top += rowHeight;

        var lblAddress = new Label
        {
            Text  = "주소",
            Left  = leftLabel,
            Top   = top + 4,
            Width = 90
        };
        _txtAddress = new TextBox
        {
            Left  = leftInput,
            Top   = top,
            Width = 350
        };
        top += rowHeight;

        var lblMemo = new Label
        {
            Text  = "메모",
            Left  = leftLabel,
            Top   = top + 4,
            Width = 90
        };
        _txtMemo = new TextBox
        {
            Left     = leftInput,
            Top      = top,
            Width    = 400,
            Height   = 120,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical
        };
        top += 130;

        _btnSave = new Button
        {
            Text = "저장",
            Left = Width - 220,
            Top  = top,
            Width = 80,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnSave.Click += (_, _) => SaveCustomer();

        _btnCancel = new Button
        {
            Text = "취소",
            Left = Width - 130,
            Top  = top,
            Width = 80,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnCancel.Click += (_, _) => DialogResult = DialogResult.Cancel;

        Controls.Add(lblName);
        Controls.Add(_txtName);
        Controls.Add(lblContact);
        Controls.Add(_txtContact);
        Controls.Add(lblPhone);
        Controls.Add(_txtPhone);
        Controls.Add(lblEmail);
        Controls.Add(_txtEmail);
        Controls.Add(lblAddress);
        Controls.Add(_txtAddress);
        Controls.Add(lblMemo);
        Controls.Add(_txtMemo);
        Controls.Add(_btnSave);
        Controls.Add(_btnCancel);
    }

    private void BindCustomer()
    {
        if (_customer == null) return;

        _txtName.Text    = _customer.CustomerName;
        _txtContact.Text = _customer.ContactName ?? "";
        _txtPhone.Text   = _customer.Phone ?? "";
        _txtEmail.Text   = _customer.Email ?? "";
        _txtAddress.Text = _customer.Address ?? "";
        _txtMemo.Text    = _customer.Memo ?? "";
    }

    private void SaveCustomer()
    {
        var name = _txtName.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("고객명을 입력하세요.", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _txtName.Focus();
            return;
        }

        if (_customer == null)
        {
            var c = new Customer
            {
                CustomerName = name,
                ContactName  = _txtContact.Text.Trim(),
                Phone        = _txtPhone.Text.Trim(),
                Email        = _txtEmail.Text.Trim(),
                Address      = _txtAddress.Text.Trim(),
                Memo         = _txtMemo.Text
            };

            _customerService.Create(c, _currentUser);
            MessageBox.Show("고객이 등록되었습니다.", "완료",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            _customer.CustomerName = name;
            _customer.ContactName  = _txtContact.Text.Trim();
            _customer.Phone        = _txtPhone.Text.Trim();
            _customer.Email        = _txtEmail.Text.Trim();
            _customer.Address      = _txtAddress.Text.Trim();
            _customer.Memo         = _txtMemo.Text;

            _customerService.Update(_customer, _currentUser);
            MessageBox.Show("고객 정보가 수정되었습니다.", "완료",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
