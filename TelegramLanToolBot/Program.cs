using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputMessageContents;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramLanToolBot
{
    class Program
    {
        private static TelegramBotClient _bot;

        static void Main(string[] args)
        {
            //Console.WriteLine(args[0]);
            
            if (args.Length == 0)
            {
                Console.WriteLine("Не указан API_ID, приложение будет закрыто.");
                Console.ReadLine();
                return;
            }
            try
            {
                _bot = new TelegramBotClient(args[0]);
                _bot.OnCallbackQuery += BotOnCallbackQueryReceived;
                _bot.OnMessage += BotOnMessageReceived;
                _bot.OnMessageEdited += BotOnMessageReceived;
                _bot.OnInlineQuery += BotOnInlineQueryReceived;
                _bot.OnInlineResultChosen += BotOnChosenInlineResultReceived;
                _bot.OnReceiveError += BotOnReceiveError;

                var me = _bot.GetMeAsync().Result;

                Console.Title = me.Username;

                _bot.StartReceiving();
                Console.WriteLine("Сервис запущен.");
                Console.ReadLine();
                _bot.StopReceiving();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
            }

        }

        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            Debugger.Break();
        }

        private static void BotOnChosenInlineResultReceived(object sender, ChosenInlineResultEventArgs chosenInlineResultEventArgs)
        {
            Console.WriteLine($"Received choosen inline result: {chosenInlineResultEventArgs.ChosenInlineResult.ResultId}");
        }

        private static async void BotOnInlineQueryReceived(object sender, InlineQueryEventArgs inlineQueryEventArgs)
        {
            InlineQueryResult[] results = {
                new InlineQueryResultLocation
                {
                    Id = "1",
                    Latitude = 40.7058316f, // displayed result
                    Longitude = -74.2581888f,
                    Title = "New York",
                    InputMessageContent = new InputLocationMessageContent // message if result is selected
                    {
                        Latitude = 40.7058316f,
                        Longitude = -74.2581888f,
                    }
                },

                new InlineQueryResultLocation
                {
                    Id = "2",
                    Longitude = 52.507629f, // displayed result
                    Latitude = 13.1449577f,
                    Title = "Berlin",
                    InputMessageContent = new InputLocationMessageContent // message if result is selected
                    {
                        Longitude = 52.507629f,
                        Latitude = 13.1449577f
                    }
                }
            };

            await _bot.AnswerInlineQueryAsync(inlineQueryEventArgs.InlineQuery.Id, results, isPersonal: true, cacheTime: 0);
        }

        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;


            if (message == null || message.Type != MessageType.TextMessage) return;

            var text = message.Text.Split(' ');
            if (message.Text.StartsWith("/wol")) // команда будить компьютер
            {
                switch (text.Length)
                {
                    case 1:
                    case 2:
                        await _bot.SendTextMessageAsync(message.Chat.Id, "Пример использования: /wol 1.2.3.4 01:02:03:04:05:06 7");
                        break;
                    default:
                        if (!WakeOnLan.ValidateMac(text[2]))
                            await _bot.SendTextMessageAsync(message.Chat.Id, "Неверный MAC адрес");
                        else
                        {
                            try
                            {
                                WakeOnLan.Up(text[1], text[2], Convert.ToInt32(text[3]));
                                Console.WriteLine($"Пакет отправлен на {text[1]}. Отправитель - Ид: {message.From.Id}, Имя: {message.From.FirstName} ");
                                await _bot.SendTextMessageAsync(message.Chat.Id, "Пакет отправлен!");
                            }
                            catch (Exception)
                            {
                                await _bot.SendTextMessageAsync(message.Chat.Id, "Произошла ошибка :(");
                            }
                        }
                        break;
                }
            }
            if (message.Text.StartsWith("/ping")) // команда пинга
            {
                switch (text.Length)
                {
                    case 1:
                        await _bot.SendTextMessageAsync(message.Chat.Id, "Пример использования: /ping 1.2.3.4");
                        break;
                    default:
                        try
                        {
                            var msg = LocalPing(text[1]);
                            Console.WriteLine($"Пинг хоста. Выполнил - Ид: {message.From.Id}, Имя: {message.From.FirstName} ");
                            await _bot.SendTextMessageAsync(message.Chat.Id, msg);
                        }
                        catch (Exception)
                        {
                            await _bot.SendTextMessageAsync(message.Chat.Id, "Произошла ошибка :(");
                        }

                        break;
                }
            }
            //else if (message.Text.StartsWith("/keyboard")) // send custom keyboard
            //{
            //    var keyboard = new ReplyKeyboardMarkup(new[]
            //    {
            //        new [] // first row
            //        {
            //            new KeyboardButton("1.1"),
            //            new KeyboardButton("1.2"),  
            //        },
            //        new [] // last row
            //        {
            //            new KeyboardButton("2.1"),
            //            new KeyboardButton("2.2"),  
            //        }
            //    });

            //    await Bot.SendTextMessageAsync(message.Chat.Id, "Choose",
            //        replyMarkup: keyboard);
            //}
            //else if (message.Text.StartsWith("/photo")) // send a photo
            //{
            //    await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.UploadPhoto);

            //    const string file = @"<FilePath>";

            //    var fileName = file.Split('\\').Last();

            //    using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            //    {
            //        var fts = new FileToSend(fileName, fileStream);

            //        await Bot.SendPhotoAsync(message.Chat.Id, fts, "Nice Picture");
            //    }
            //}
            else if (message.Text.StartsWith("/GetCode")) // команда на получение своего ИД
            {
                var usage = $"Твой код для привязки - {message.From.Id} " +
                            "Передай его админу!";
                await _bot.SendTextMessageAsync(message.Chat.Id, usage, replyMarkup: new ReplyKeyboardRemove());
            }
            //else if (message.Text.StartsWith("/request")) // request location or contact
            //{
            //    var keyboard = new ReplyKeyboardMarkup(new[]
            //    {
            //        new KeyboardButton("Location")
            //        {
            //            RequestLocation = true
            //        },
            //        new KeyboardButton("Contact")
            //        {
            //            RequestContact = true
            //        },
            //    });

            //    await _bot.SendTextMessageAsync(message.Chat.Id, "Who or Where are you?", replyMarkup: keyboard);
            //}

            else
            {
                var usage = "Я особо интелектуальный робот - будильщик компьютеров!!! " +
                            "\nИспользование:" +
                            "\n/wol <IP MAC port> - для пробуждения компьютера " +
                            "\nПример использования: /wol 1.2.3.4 01:02:03:04:05:06 7 " +
                            "\n/GetCode - получить свой ИД, для передачи Админу " +
                            "\n/ping <IP> - пинг заданного ип ";


                await _bot.SendTextMessageAsync(message.Chat.Id, usage, replyMarkup: new ReplyKeyboardRemove());
            }
        }

        private static string LocalPing(string ip)
        {
            // Ping's the local machine.
            var pingSender = new Ping();
            var reply = pingSender.Send(IPAddress.Parse(ip));

            var str = new StringBuilder();

            if (reply != null && reply.Status == IPStatus.Success)
            {

                str.AppendLine($"Адрес: {reply.Address}");
                str.AppendLine($"RoundTrip time: {reply.RoundtripTime}");
                str.AppendLine($"Время жизни: {reply.Options.Ttl}");
                str.AppendLine($"Отсутствие фрагментации: {reply.Options.DontFragment}");
                str.AppendLine($"Размер буфера: {reply.Buffer.Length}");
            }
            else
            {
                if (reply != null) str.AppendLine(reply.Status.ToString());
            }
            return str.ToString();
        }

        private static async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            await _bot.AnswerCallbackQueryAsync(callbackQueryEventArgs.CallbackQuery.Id,
                $"Received {callbackQueryEventArgs.CallbackQuery.Data}");
        }
    }

    internal static class WakeOnLan
    {
        public static void Up(string ip, string mac, int? port = null)
        {
            var client = new UdpClient();
            var data = new byte[102];

            for (var i = 0; i <= 5; i++) // первые шесть байт - нулевые
                data[i] = 0xff;

            var macDigits = GetMacDigits(mac);
            if (macDigits.Length != 6)
                throw new ArgumentException("Введён некоректный MAC адрес!");

            const int start = 6;
            for (var i = 0; i < 16; i++) // создаем нужную последовательность байт для пакета
            for (var x = 0; x < 6; x++)
                data[start + i * 6 + x] = (byte)Convert.ToInt32(macDigits[x], 16);

            client.Send(data, data.Length, ip, port ?? 7); // отправляем пакет
        }

        private static string[] GetMacDigits(string mac) // парсим MAC
        {
            return mac.Split(mac.Contains("-") ? '-' : ':');
        }

        public static bool ValidateMac(string mac) // простая проверка на валидность MAC адреса
        {
            return GetMacDigits(mac).Length == 6;
        }
    }
}
