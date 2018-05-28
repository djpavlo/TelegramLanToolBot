using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using ApiAiSDK;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramLanToolBot
{
    internal class Program
    {
        private static ApiAi _apiAi;

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
                //_bot.OnInlineQuery += BotOnInlineQueryReceived;
                _bot.OnInlineResultChosen += BotOnChosenInlineResultReceived;
                _bot.OnReceiveError += BotOnReceiveError;

                var config = new AIConfiguration("87286ae9955d43d7ada5c23346787606", SupportedLanguage.Russian);
                _apiAi = new ApiAi(config);


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

        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;


            if (message == null || message.Type != MessageType.Text) return;

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
            else if (message.Text.StartsWith("/keyboard")) // send custom keyboard
            {
                var keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new [] // first row
                    {
                        new KeyboardButton("List"),
                        new KeyboardButton("Ping"),
                    },
                    new [] // last row
                    {
                        new KeyboardButton("WOL"),
                        new KeyboardButton("TraceRt"),
                    }
                });

                await _bot.SendTextMessageAsync(message.Chat.Id, "Choose",
                    replyMarkup: keyboard);
            }
            else if (message.Text.StartsWith("/GetCode")) // команда на получение своего ИД
            {
                var usage = $"Твой код для привязки - {message.From.Id} " +
                            "Передай его админу!";
                await _bot.SendTextMessageAsync(message.Chat.Id, usage, replyMarkup: new ReplyKeyboardRemove());
            }
            else if (message.Text.StartsWith("/request")) // request location or contact
            {
                var keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new KeyboardButton("Location")
                    {
                        RequestLocation = true
                    },
                    new KeyboardButton("Contact")
                    {
                        RequestContact = true
                    },
                });

                await _bot.SendTextMessageAsync(message.Chat.Id, "Who or Where are you?", replyMarkup: keyboard);
            }

            else
            {
                try
                {
                    var response = _apiAi.TextRequest(message.Text);
                    var msg = string.IsNullOrEmpty(response.Result.Fulfillment.Speech)
                        ? "Я Вас не совсем понял!"
                        : response.Result.Fulfillment.Speech;

                    await _bot.SendTextMessageAsync(message.Chat.Id,
                        msg,
                        disableWebPagePreview: true, replyMarkup: new ReplyKeyboardRemove());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }


                //var usage = "Я особо интелектуальный робот - будильщик компьютеров!!! " +
                //            "\nИспользование:" +
                //            "\n/wol <IP MAC port> - для пробуждения компьютера " +
                //            "\nПример использования: /wol 1.2.3.4 01:02:03:04:05:06 7 " +
                //            "\n/GetCode - получить свой ИД, для передачи Админу " +
                //            "\n/ping <IP> - пинг заданного ип ";


                //await _bot.SendTextMessageAsync(message.Chat.Id, usage, replyMarkup: new ReplyKeyboardRemove());
            }
        }
        // метод локального пинга
        private static string LocalPing(string ip)
        {
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


        //отработка прилепленных кнопок под сообщениями
        private static async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            //await _bot.EditMessageTextAsync(callbackQueryEventArgs.CallbackQuery.From.Id,
            //    callbackQueryEventArgs.CallbackQuery.Message.MessageId,
            //    "Страница" + callbackQueryEventArgs.CallbackQuery.Data,
            //    replyMarkup: GeneratePagination(15, int.Parse(callbackQueryEventArgs.CallbackQuery.Data)));


            //await _bot.AnswerCallbackQueryAsync(callbackQueryEventArgs.CallbackQuery.Id, $"Received {callbackQueryEventArgs.CallbackQuery.Data}");


            //await _bot.SendTextMessageAsync(callbackQueryEventArgs.CallbackQuery.From.Id,
            //    "Сделайте выбор", replyMarkup: GeneratePagination(5, int.Parse(callbackQueryEventArgs.CallbackQuery.Data)),
            //    replyToMessageId: callbackQueryEventArgs.CallbackQuery.Message.MessageId);

            //await _bot.SendTextMessageAsync(callbackQueryEventArgs.CallbackQuery.From.Id, $"Received {callbackQueryEventArgs.CallbackQuery.Data}", replyMarkup: new ReplyKeyboardRemove());
        }

    }
    //класс пробуждения компьютера по сети
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
