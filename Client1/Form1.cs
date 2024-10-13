using Client1;
using Client1.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Mail;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace Client1
{

    public partial class Form1 : Form
    {
        private TcpClient client;
        private NetworkStream stream;
        private StreamReader reader;
        private StreamWriter writer;
        private string UserName { get; set; }
        private List<string> receivedFiles = new List<string>();
        private bool userConfirmed = false;


        public Form1()
        {
            InitializeComponent();

            ShowUserNameDialog();

            StartClient();
        }
        private void ShowUserNameDialog()
        {
            using (var userNameForm = new Form())
            {
                userNameForm.ShowInTaskbar = false;
                bool _userConfirmed = false;
                userNameForm.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
                userNameForm.TopMost = true;
                userNameForm.Text = "Введите имя пользователя";
                var textBox = new System.Windows.Forms.TextBox { Dock = DockStyle.Fill,  };
                var button = new System.Windows.Forms.Button { Text = "Подтвердить", Dock = DockStyle.Bottom };
                button.Click += (sender, e) =>
                {
                    string inputUserName = textBox.Text.Trim();
                    if (string.IsNullOrEmpty(inputUserName))
                    {
                        MessageBox.Show("Имя пользователя не может быть пустым.");
                    }
                    else
                    {
                        UserName = inputUserName;
                        _userConfirmed = true;
                        userNameForm.DialogResult = DialogResult.OK;
                        userNameForm.Close();
                    }
                };
                textBox.KeyDown += (sender, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        string inputUserName = textBox.Text.Trim();
                        if (string.IsNullOrEmpty(inputUserName))
                        {
                            MessageBox.Show("Имя пользователя не может быть пустым.");
                        }
                        else
                        {
                            UserName = inputUserName;
                            _userConfirmed = true;
                            userNameForm.DialogResult = DialogResult.OK;
                            userNameForm.Close();
                        }
                    }
                };
                userNameForm.FormClosing += (sender, e) =>
                {
                    if (!_userConfirmed) // Если имя пользователя не подтверждено, закрываем всё приложение
                    {
                        Application.Exit();
                    }
                };
                userNameForm.Controls.Add(textBox);
                userNameForm.Controls.Add(button);
                userNameForm.ShowDialog();
            }
        }

        private async void StartClient()
        {
            while (true)
            {
                try
                {
                    client = new TcpClient("127.0.0.1", 5000);
                    stream = client.GetStream();
                    reader = new StreamReader(stream);
                    writer = new StreamWriter(stream) { AutoFlush = true };

                    // Здесь мы отправляем имя пользователя
                    await writer.WriteLineAsync($"SETNAME {UserName}");

                    // Запускаем поток для получения сообщений
                    Thread receiveThread = new Thread(ReceiveMessages);
                    receiveThread.IsBackground = true;
                    receiveThread.Start();
                    break; // Выйдем из цикла, если имя успешно отправлено
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка подключения: " + ex.Message);
                }
            }
        }



        private static readonly object _lock = new object();


        private async void ReceiveMessages()

        {

            try

            {

                while (true)

                {

                    string message = await reader.ReadLineAsync();

                    if (message != null)

                    {

                        this.BeginInvoke((Action)(async () =>

                        {

                           
                            // Если сообщение содержит информацию о полученном файле

                            if (message.StartsWith("Получен файл: "))

                            {

                                string fileName = message.Substring("Получен файл: ".Length);

                                listBox1.Items.Add(fileName); // Добавление имени файла в ListBox

                            }

                                richTextBox1.AppendText(message + Environment.NewLine);
                            

                        }));

                    }

                    else

                    {

                        break;

                    }

                }

            }

            catch (Exception ex)

            {

                MessageBox.Show("Ошибка получения сообщения: " + ex.Message);

            }

        }


       


        private async void SendDisconnectMessage()
        {
            if (client != null)
            {
                try
                {
                    await writer.WriteLineAsync("DISCONNECT");
                    await writer.FlushAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка при отправке команды отключения: " + ex.Message);
                }
                finally
                {
                    writer?.Close();
                    reader?.Close();
                    stream?.Close();
                    client?.Close();
                    Application.Exit();
                }
            }
        }



        private async void button1_Click(object sender, EventArgs e)
        {
            string message = textBox1.Text;

            if (!string.IsNullOrWhiteSpace(message))
            {
                // Отправляем сообщение на сервер
                await writer.WriteLineAsync($"MESSAGE {message}");
                richTextBox1.AppendText($"Вы: {message}\n");
                // Используем команду MESSAGE
                textBox1.Clear();
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SendDisconnectMessage();
        }
        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private async void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            richTextBox1.ReadOnly = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SendDisconnectMessage();
            this.Close();
        }

        private async void button4_Click(object sender, EventArgs e)

        {

            using (OpenFileDialog openFileDialog = new OpenFileDialog())

            {

                openFileDialog.InitialDirectory = "c:\\";

                openFileDialog.Filter = "All files (*.*)|*.*";

                openFileDialog.Title = "Выберите файл для отправки";


                if (openFileDialog.ShowDialog() == DialogResult.OK)

                {

                    string filePath = openFileDialog.FileName;

                    FileInfo fileInfo = new FileInfo(filePath);

                    const long maxFileSize = 50 * 1024 * 1024; // 50 МБ


                    if (fileInfo.Length > maxFileSize)

                    {

                        MessageBox.Show($"Ошибка: Файл слишком большой. Максимально допустимый размер - 50 МБ.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        return;

                    }


                    string fileName = Path.GetFileName(filePath);

                    await writer.WriteLineAsync($"SEND_FILE {fileName}");


                    // Отправка размера файла

                    byte[] sizeBuffer = BitConverter.GetBytes(fileInfo.Length);

                    await stream.WriteAsync(sizeBuffer, 0, sizeBuffer.Length); // отправляем размер файла


                    // Отправка файла

                    byte[] buffer = new byte[4096]; // буфер для передачи данных


                    using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))

                    {

                        int bytesRead;

                        while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)

                        {

                            await stream.WriteAsync(buffer, 0, bytesRead); // отправляем данные на сервер

                        }

                    }


                    
                }

            }

        }
        private async void button5_Click(object sender, EventArgs e)
        {
            string partnerUserName = textBox2.Text;

            await writer.WriteLineAsync($"SELECT {partnerUserName}");
            textBox2.Clear();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {
            
        }




        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }



        private void label2_Click_1(object sender, EventArgs e)
        {

        }

        private async void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                string fileName = listBox1.SelectedItem.ToString();

                // Отправляем имя файла серверу
                await writer.WriteLineAsync($"REQUEST_FILE {fileName}");


                // Здесь можно добавить дополнительную логику, если необходимо
            }
        }


    }
}


