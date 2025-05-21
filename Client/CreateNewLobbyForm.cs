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
    public partial class CreateNewLobbyForm : Form
    {
        public string LobbyName => LobbyNameTextBox.Text;
        public string EntryCode => EntryCodeTextBox.Text;

        public CreateNewLobbyForm()
        {
            InitializeComponent();
        }

        private void CreateButton_Click(object sender, EventArgs e)
        {
            // Validate username
            if (!LobbyName.All(char.IsLetterOrDigit))
            {
                MessageBox.Show("Illegal lobby name - alphanumeric chracters only!", "Create Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LobbyNameTextBox.Clear();
                EntryCodeTextBox.Clear();

                return;
            }
            
            // Validate password
            if (!EntryCode.All(char.IsDigit))
            {
                MessageBox.Show("Illegal entry code - digits only!", "Create Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                EntryCodeTextBox.Clear();

                return;
            }

            DialogResult = DialogResult.OK;
            Close();

            return;
        }
    }
}
