using Client1;
using Client1.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Mail;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
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
                var textBox = new System.Windows.Forms.TextBox { Dock = DockStyle.Fill, };
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

                    ShowUserNameDialog();

                    // Здесь мы отправляем имя пользователя
                    await writer.WriteLineAsync($"SETNAME {UserName}");

                    label5.Text = "Ваше имя: " + UserName;
                    Thread receiveThread = new Thread(ReceiveMessages);
                    receiveThread.IsBackground = true;
                    receiveThread.Start();
                    break; // Выйдем из цикла, если имя успешно отправлено
                }
                catch (SocketException)
                {
                    richTextBox1.AppendText("Сервер не доступен.Повторная попытка через 5 секунд...\n");
                    await Task.Delay(5000);
                    richTextBox1.Clear();// Ждем 5 секунд перед новой попыткой
                    richTextBox1.AppendText("Подключение...");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка подключения: " + ex.Message);
                    break; // В случае другой ошибки, выходим из цикла 
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
                        if (message.StartsWith("CONNECT_REQUEST "))

                        {

                            string requestingUserName = message.Substring("CONNECT_REQUEST ".Length);

                            var result = MessageBox.Show($"Пользователь {requestingUserName} хочет подключиться. Принять?",

                                                          "Запрос на подключение",

                                                          MessageBoxButtons.YesNo);


                            if (result == DialogResult.Yes)

                            {

                                await writer.WriteLineAsync($"SELECT {requestingUserName}");

                            }

                            else

                            {

                                await writer.WriteLineAsync($"REJECT {requestingUserName}");

                            }

                        }
                        else if (message.StartsWith("ERROR: Имя занято или некорректно"))
                        {
                            label5.Text = "Ваше имя: ";
                            MessageBox.Show("Имя занято", "Имя", MessageBoxButtons.OK);
                            ShowUserNameDialog();
                            await writer.WriteLineAsync($"SETNAME {UserName}");
                            label5.Text = "Ваше имя: " + UserName;

                        }
                        else if (message.StartsWith("FILESENT "))
                        {
                            // Сообщение о том, что файл будет отправлен
                            string fileName = message.Substring("FILESENT ".Length);
                            // Начинаем процесс получения файла
                            await ReceiveFile(fileName);
                        }
                        else if (message.StartsWith("CONNECTED "))
                        {
                            listBox2.Items.Clear(); // Очищаем перед добавлением новых клиентов
                            string clientsList = message.Substring("CONNECTED ".Length);
                            string[] names = clientsList.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            foreach (string name in names)
                            {
                                if (name == UserName)
                                {
                                    listBox2.Items.Add(name + "(Вы)");
                                }
                                else
                                {
                                    listBox2.Items.Add(name);
                                }
                            }
                        }

                        else
                        {
                            // Обработка обычных текстовых сообщений
                            this.BeginInvoke((Action)(() =>
                            {
                                if(message.StartsWith("Вы подключены "))
                                {
                                    label4.Text = message.Substring("Вы подключены к клиенту".Length);
                                }

                                if(message.StartsWith("Клиент "))
                                {
                                    label4.Text = "";
                                }
                                if (message.StartsWith("Получен файл: "))
                                {
                                    string fileName = message.Substring("Получен файл: ".Length);
                                    listBox1.Items.Add(fileName); // Добавление имени файла в ListBox
                                    richTextBox1.AppendText(message + Environment.NewLine);
                                }
                                else
                                {
                                    richTextBox1.AppendText(message + Environment.NewLine);
                                }

                            }));
                        }
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

        private async Task ReceiveFile(string fileName)
        {
            byte[] sizeBuffer = new byte[8]; // буфер для размера файла
            await stream.ReadAsync(sizeBuffer, 0, sizeBuffer.Length); // чтение размера файла
            long fileSize = BitConverter.ToInt64(sizeBuffer, 0);
            string savePath = Path.Combine("ReceivedFiles", fileName); // Путь для сохранения файла
            Directory.CreateDirectory("ReceivedFiles"); // Создаём директорию, если её нет
            using (var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] buffer = new byte[4096]; // буфер для данных
                long totalRead = 0;
                while (totalRead < fileSize)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break; // Если нет больше данных, прерываем
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalRead += bytesRead;
                }
            }
            richTextBox1.AppendText($"Файл {fileName} был успешно сохранен." + Environment.NewLine);
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


        private async void textbox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
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
            if (label4.Text == "")
            {
                MessageBox.Show($"Ошибка: Вы не подключены к клиенту", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
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

            await writer.WriteLineAsync($"REQUEST {partnerUserName}");
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
            }
        }

        private async void button6_Click(object sender, EventArgs e)
        {

            if (client != null)

            {

                try

                {

                    // Отправьте команду на сервер для отключения от партнёра

                    await writer.WriteLineAsync("DISCONNECTFROMPART");

                    await writer.FlushAsync();


                    // Уведомляем пользователя

                    richTextBox1.AppendText("Вы отключились от партнера\n");
                    label4.Text = "";

                }

                catch (Exception ex)

                {

                    Console.WriteLine("Ошибка при отправке команды отключения: " + ex.Message);

                }

            }

        }

        private async void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            Name = listBox2.SelectedItem.ToString();
            await writer.WriteLineAsync($"REQUEST {Name}");
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }
    }
}

