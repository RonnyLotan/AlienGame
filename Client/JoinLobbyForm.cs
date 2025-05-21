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
    public partial class JoinLobbyForm : Form
    {
        public string LobbyName => LobbyNameTextBox.Text;
        public string EntryCode => EntryCodeTextBox.Text;

        public bool IsCreateNewClicked = false;

        public JoinLobbyForm()
        {
            InitializeComponent();
        }

        private void EnterButton_Click(object sender, EventArgs e)
        {
            // Validate username
            if (!LobbyName.All(char.IsLetterOrDigit))
            {
                MessageBox.Show("Illegal lobby name - alphanumeric chracters only!", "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LobbyNameTextBox.Clear();
                EntryCodeTextBox.Clear();

                return;
            }

            // Validate password
            if (!EntryCode.All(char.IsDigit))
            {
                MessageBox.Show("Illegal entry code - digits only!", "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                EntryCodeTextBox.Clear();

                return; 
            }

            DialogResult = DialogResult.OK; 
            
            Close();

            return;
        }

        private void CreateNewButton_Click(object sender, EventArgs e)
        {
            IsCreateNewClicked = true;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
