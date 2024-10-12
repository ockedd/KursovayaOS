using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client1
{
    public partial class Form2 : Form

    {

        public string UserName { get; private set; }
        private bool _userConfirmed = false;
        public Form2()

        {

            InitializeComponent();

        }



        private void button1_Click_1(object sender, EventArgs e)
        {
            CheckUserName();
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_userConfirmed) // Если имя пользователя не подтверждено, закрываем всё приложение

            {

                Application.Exit();

            }
        }

        private void textBox1_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                CheckUserName();
            }

        }
        private async void CheckUserName()
        {
            string inputUserName = textBox1.Text.Trim();

            if (string.IsNullOrEmpty(inputUserName))

            {

                MessageBox.Show("Имя пользователя не может быть пустым.");

                return;

            }

            else
            {
                UserName = textBox1.Text.Trim();
                _userConfirmed = true;

                this.DialogResult = DialogResult.OK;

                this.Close();

            }

        }
    }
}