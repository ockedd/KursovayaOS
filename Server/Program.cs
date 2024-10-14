using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;

class ClientInfo
{
    public StreamWriter Writer { get; set; }
    public StreamReader Reader { get; set; }
    public string PartnerUserName { get; set; }
    public Stream Stream { get; set; }
}

class Program
{
    private static TcpListener listener;
    private static ConcurrentDictionary<string, ClientInfo> clients = new ConcurrentDictionary<string, ClientInfo>();

    static async Task Main()
    {
        listener = new TcpListener(IPAddress.Any, 5000);
        listener.Start();
        Console.WriteLine("Сервер запущен...");

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            _ = HandleClientAsync(client);
        }
    }

    private static async Task HandleClientAsync(TcpClient client)
    {
        string userName = null;
        using (var stream = client.GetStream())
        using (var reader = new StreamReader(stream))
        using (var writer = new StreamWriter(stream) { AutoFlush = true })
        {
            while (userName == null)
            {
                string line1 = await reader.ReadLineAsync();
                string[] commandParts = line1.Split(new[] { ' ' }, 2);
                string command = commandParts[0].ToUpperInvariant();
                string message = commandParts.Length > 1 ? commandParts[1] : string.Empty;

                if (command == "SETNAME")
                {
                    userName = await SetNameAsync(writer, message);
                }
            }

            clients[userName] = new ClientInfo { Writer = writer, Reader = reader, PartnerUserName = null, Stream = stream }; // инициализация без партнёра
            Console.WriteLine($"Клиент {userName} подключен.");

            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                string[] commandParts = line.Split(new[] { ' ' }, 2);
                string command = commandParts[0].ToUpperInvariant();
                string message = commandParts.Length > 1 ? commandParts[1] : string.Empty;

                switch (command)
                {
                    case "SELECT":
                        await SelectPartnerAsync(userName, message);
                        break;

                    case "MESSAGE":
                        await SendMessageAsync(userName, message);
                        break;

                    case "SEND_FILE":
                        await HandleSendFileAsync(userName, message, stream);
                        break;

                    case "REQUEST_FILE":
                        await HandleRequestFileAsync(userName, message);
                        break;
                    case "DISCONNECT":
                        Console.WriteLine($"Клиент {userName} отключился.");
                        await NotifyPartnerDisconnection(userName);
                        clients.TryRemove(userName, out _);
                        return;

                    default:
                        await writer.WriteLineAsync("ERROR: Неправильная команда.");
                        break;
                }
            }
        }
    }



    private static async Task HandleRequestFileAsync(string userName, string message)
    {
        string fileName = message;
        string filePath = Path.Combine("Files", fileName);
        if (File.Exists(filePath))
        {
            await SendFileAsync(userName, filePath);
        }
        else
        {
            await clients[userName].Writer.WriteLineAsync("ERROR: Файл не найден.");
        }
    }


    private static async Task SendFileAsync(string userName, string filePath)
    {
        var partnerUserName = clients[userName].PartnerUserName;
        if (partnerUserName != null && clients.ContainsKey(partnerUserName))
        {
            await clients[userName].Writer.WriteLineAsync($"FILESENT {Path.GetFileName(filePath)}");
            string fileName = Path.GetFileName(filePath);
            FileInfo fileInfo = new FileInfo(filePath);
            // Отправка размера файла
            byte[] sizeBuffer = BitConverter.GetBytes(fileInfo.Length);
            await clients[userName].Stream.WriteAsync(sizeBuffer, 0, sizeBuffer.Length); // отправляем размер файла
            // Отправка файла
            byte[] buffer = new byte[4096]; // буфер для передачи данных
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                int bytesRead;
                while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {                
                    await clients[userName].Stream.WriteAsync(buffer, 0, bytesRead); // отправляем данные на сервер
                }
            }
        }
        else
        {
            await clients[userName].Writer.WriteLineAsync("ERROR: Нет подключенного партнёра.");
        }
    }



    private static async Task HandleSendFileAsync(string userName, string message, Stream stream)
    {
        var partnerUserName = clients[userName].PartnerUserName;

            string fileName = message;

            // Чтение размера файла из потока данных
            byte[] sizeBuffer = new byte[8];
            await stream.ReadAsync(sizeBuffer, 0, sizeBuffer.Length);

            long fileSize = BitConverter.ToInt64(sizeBuffer, 0);

            // Подготовка для сохранения файла
            var buffer = new byte[4096]; // Размер буфера для передачи

            using (var ms = new MemoryStream())
            {
                int bytesRead;
                long totalRead = 0;

                while (totalRead < fileSize && (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await ms.WriteAsync(buffer, 0, bytesRead);
                    totalRead += bytesRead;
                }

                // Сохранить файл
                ms.Position = 0; // Сбросить позицию обратно на начало потока
                await ReceiveFileAsync(userName, fileName, ms);
            }
            await clients[userName].Writer.WriteLineAsync($"Файл {fileName} был отправлен.\n");
        

    }


    private static async Task ReceiveFileAsync(string userName, string fileName, Stream fileStream)
    {
        int bufferSize = 52428800; // 50 МБ
        string filePath = Path.Combine("Files", fileName);
        // Проверка наличия файла с тем же именем и добавление уникального идентификатор
        int fileCounter = 1;
        while (File.Exists(filePath))
        {
            break;
        }
        Directory.CreateDirectory("Files");
        using (var fileStreamToSave = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            // Использование CopyToAsync для надежной записи всех данных
            await fileStream.CopyToAsync(fileStreamToSave, bufferSize);
            await fileStreamToSave.FlushAsync(); // Сбросить данные в файл
        }
        await NotifyFileReceived(userName, fileName); // Уведомить о получении файла
    }



    private static async Task NotifyFileReceived(string userName, string fileName)
    {
        var partnerUserName = clients[userName].PartnerUserName;
        if (partnerUserName != null && clients.ContainsKey(partnerUserName))
        {
            await clients[partnerUserName].Writer.WriteLineAsync($"Получен файл: {fileName}");
        }
    }

    private static async Task<string> SetNameAsync(StreamWriter writer, string name)
    {
        string userName = name.Trim();
        if (string.IsNullOrWhiteSpace(userName) || clients.ContainsKey(userName))
        {
            await writer.WriteLineAsync("ERROR: Имя занято или некорректно");
            return null; // имя не задано
        }
        return userName; // возвращаем установленное имя
    }

    private static async Task SelectPartnerAsync(string userName, string partnerUserName)
    {
        if (userName.Equals(partnerUserName, StringComparison.OrdinalIgnoreCase))
        {
            await clients[userName].Writer.WriteLineAsync("ERROR: Вы не можете выбрать себя в качестве партнёра.");
            return;
        }

        if (clients.ContainsKey(partnerUserName))
        {
            var selectedPartner = clients[partnerUserName];

            // Проверяем, есть ли уже избранный партнер у второго клиента
            if (selectedPartner.PartnerUserName != null)
            {
                await clients[userName].Writer.WriteLineAsync("ERROR: Партнёр уже подключен к другому клиенту.");
                return;
            }

            clients[userName].PartnerUserName = partnerUserName;
            selectedPartner.PartnerUserName = userName;

            await clients[userName].Writer.WriteLineAsync($"Вы подключены к клиенту {partnerUserName}");
            await selectedPartner.Writer.WriteLineAsync($"Вы подключены к клиенту {userName}");
        }
        else
        {
            await clients[userName].Writer.WriteLineAsync("ERROR: Партнёр не найден.");
        }
    }


    private static async Task NotifyPartnerDisconnection(string userName)
    {
        var partnerUserName = clients[userName].PartnerUserName;
        if (partnerUserName != null && clients.ContainsKey(partnerUserName))
        {
            await clients[partnerUserName].Writer.WriteLineAsync($"Клиент {userName} отключился.");
        }
    }

    private static async Task SendMessageAsync(string userName, string message)
    {
        var partnerUserName = clients[userName].PartnerUserName;
        if (partnerUserName != null && clients.ContainsKey(partnerUserName))
        {
            await clients[partnerUserName].Writer.WriteLineAsync($"{userName}: {message}");
        }
        else
        {
            await clients[userName].Writer.WriteLineAsync("ERROR: Нет подключенного партнёра.");
        }
    }
    }



