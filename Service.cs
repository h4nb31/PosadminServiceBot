using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

public class BotService : IHostedService
{
   
    //Токен рабочего бота 7073838502:AAFaKEO5d5hGUcoqqH2xIYK3rnSzcbAwCa8
    private readonly string _botToken = "7073838502:AAFaKEO5d5hGUcoqqH2xIYK3rnSzcbAwCa8"; 
    private TelegramBotClient? _botClient;
    private CancellationTokenSource? _cts;  //Токен Отмены

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _botClient = new TelegramBotClient(_botToken);
        _cts = new CancellationTokenSource();

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[]
            {
                UpdateType.Message,  //Принимает сообщения
                UpdateType.CallbackQuery //Для inline кнопок
            },
            ThrowPendingUpdates = true
        };

        _botClient.StartReceiving(
            updateHandler: Handlers.HandleUpdateAsync,
            pollingErrorHandler: Handlers.HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: _cts.Token
        );

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cts.Cancel();
        return Task.CompletedTask;
    }
}
