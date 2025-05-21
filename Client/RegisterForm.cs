using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class RegisterForm : Form
    {
        public string Username => UsernameTextBox.Text;
        public string Email => EmailTextBox.Text;
        public string Password => PasswordTextBox.Text;

        public RegisterForm()
        {
            InitializeComponent();
        }

        private void RegisterButton_Click(object sender, EventArgs e)
        {
            // Validate username
            if (!Username.All(char.IsLetterOrDigit))
            {
                MessageBox.Show("Illegal Username - alphanumeric chracters only!", "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UsernameTextBox.Clear();
                PasswordTextBox.Clear();

                return;
            }
            
            // Validate username
            if (!Email.All(c => char.IsLetterOrDigit(c) || c == '@' || c == '.' || c == '_') || Email.Count(c => c == '@') != 1)
            {
                MessageBox.Show("Illegal email - alphanumeric chracters only (and '@', '_' or '.')!", "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                EmailTextBox.Clear();
                PasswordTextBox.Clear();

                return;
            }

            // Validate password
            if (!Password.All(char.IsLetterOrDigit))
            {
                MessageBox.Show("Illegal Password - alphanumeric chracters only!", "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                PasswordTextBox.Clear();

                return;
            }

            DialogResult = DialogResult.OK;
            Close();

            return;
        }
    }
}
