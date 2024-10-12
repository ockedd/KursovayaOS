using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

class ClientInfo
{
    public StreamWriter Writer { get; set; }
    public StreamReader Reader { get; set; }
    public string PartnerUserName { get; set; }
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

            clients[userName] = new ClientInfo { Writer = writer, Reader = reader, PartnerUserName = null }; // инициализация без партнёра
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
            clients[userName].PartnerUserName = partnerUserName;
            clients[partnerUserName].PartnerUserName = userName;
            await clients[userName].Writer.WriteLineAsync($"Вы подключены к клиенту {partnerUserName}");
            await clients[partnerUserName].Writer.WriteLineAsync($"Вы подключены к клиенту {userName}");
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
            await clients[partnerUserName].Writer.WriteLineAsync($"Сообщение от {userName}: {message}");
        }
        else
        {
            await clients[userName].Writer.WriteLineAsync("ERROR: Нет подключенного партнёра.");
        }
    }
    }



