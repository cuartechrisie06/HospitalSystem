using System;
using System.Drawing;
using System.Windows.Forms;
using HospitalSystem.Data;
using HospitalSystem.Models;

namespace HospitalSystem.Forms
{
    public class LoginForm : Form
    {
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button btnLogin;
        private Label lblTitle;
        private Label lblError;
        private Panel panelCard;

        public LoginForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Hospital System - Login";
            this.Size = new Size(420, 480);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(240, 242, 245);

            // Card panel
            panelCard = new Panel();
            panelCard.Size = new Size(340, 360);
            panelCard.Location = new Point(40, 40);
            panelCard.BackColor = Color.White;
            panelCard.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(panelCard);

            // Title
            lblTitle = new Label();
            lblTitle.Text = "Hospital System";
            lblTitle.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(30, 64, 175);
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(70, 30);
            panelCard.Controls.Add(lblTitle);

            Label lblSubtitle = new Label();
            lblSubtitle.Text = "Sign in to continue";
            lblSubtitle.Font = new Font("Segoe UI", 9F);
            lblSubtitle.ForeColor = Color.Gray;
            lblSubtitle.AutoSize = true;
            lblSubtitle.Location = new Point(100, 65);
            panelCard.Controls.Add(lblSubtitle);

            // Username
            Label lblUser = new Label();
            lblUser.Text = "Username";
            lblUser.Font = new Font("Segoe UI", 9F);
            lblUser.Location = new Point(30, 110);
            lblUser.AutoSize = true;
            panelCard.Controls.Add(lblUser);

            txtUsername = new TextBox();
            txtUsername.Location = new Point(30, 130);
            txtUsername.Size = new Size(270, 28);
            txtUsername.Font = new Font("Segoe UI", 11F);
            panelCard.Controls.Add(txtUsername);

            // Password
            Label lblPass = new Label();
            lblPass.Text = "Password";
            lblPass.Font = new Font("Segoe UI", 9F);
            lblPass.Location = new Point(30, 175);
            lblPass.AutoSize = true;
            panelCard.Controls.Add(lblPass);

            txtPassword = new TextBox();
            txtPassword.Location = new Point(30, 195);
            txtPassword.Size = new Size(270, 28);
            txtPassword.Font = new Font("Segoe UI", 11F);
            txtPassword.UseSystemPasswordChar = true;
            panelCard.Controls.Add(txtPassword);

            // Error label
            lblError = new Label();
            lblError.Text = "";
            lblError.ForeColor = Color.Firebrick;
            lblError.Font = new Font("Segoe UI", 9F);
            lblError.Location = new Point(30, 235);
            lblError.Size = new Size(270, 20);
            panelCard.Controls.Add(lblError);

            // Login button
            btnLogin = new Button();
            btnLogin.Text = "Sign In";
            btnLogin.Location = new Point(30, 270);
            btnLogin.Size = new Size(270, 40);
            btnLogin.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnLogin.BackColor = Color.FromArgb(37, 99, 235);
            btnLogin.ForeColor = Color.White;
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Cursor = Cursors.Hand;
            btnLogin.Click += BtnLogin_Click;
            panelCard.Controls.Add(btnLogin);

            // Demo hint
            Label lblHint = new Label();
            lblHint.Text = "Demo: admin / admin   or   nurse / nurse";
            lblHint.Font = new Font("Segoe UI", 8F);
            lblHint.ForeColor = Color.Gray;
            lblHint.AutoSize = true;
            lblHint.Location = new Point(50, 325);
            panelCard.Controls.Add(lblHint);

            // Enter key support
            this.AcceptButton = btnLogin;
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                lblError.Text = "Please enter username and password.";
                return;
            }

            User user = HospitalData.Authenticate(username, password);
            if (user == null)
            {
                lblError.Text = "Invalid username or password.";
                txtPassword.Clear();
                txtPassword.Focus();
                return;
            }

            HospitalData.CurrentUser = user;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
