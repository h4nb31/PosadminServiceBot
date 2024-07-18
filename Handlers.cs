using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;

class Handlers
{
    
    public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {

            // Обабатывает только обновления Message
            if (update.Message is not { } message)
                return;
            // Обробатывает только текстовые сообщения
            if (message.Text is not {} messageText)
                return;

            //var FullChatInfo = message.Chat;
            var chatId = message.Chat.Id;
            var TopicId = message.MessageThreadId;
            var userInfo = message.From ?? throw new ArgumentNullException(nameof(message.From) + "is null");


            //Сообщение для запроса
            string ProccessedM = messageText.Replace("@pos_chatService_bot", "").Trim();
            string RespondText = $"Поступил Запрос:\n\nЧат:  {message.Chat.Title}\nТекст:  {(ProccessedM !=""?ProccessedM:"Отсутствует")}";
            
            
            //формирование ссылки
            string absChatId = (chatId.ToString()).Substring(3); //Убираем -100 у ID чата
            string messageLink = $"https://t.me/c/{absChatId}/{message.MessageId}"; //ссылка на сообщение чата для кнопки
            
            Console.WriteLine($"\nReceived: '{messageText}'\nChat: '{chatId}\nTopic id: {TopicId}'\nUser: '{userInfo.Id} - {userInfo.FirstName} {userInfo.LastName}: @{userInfo.Username}'"); //Лог в консоль

            // Сообщение "повтор"
            if(messageText.Contains("@pos_chatService_bot")){
                Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: "-1001902270586",
                messageThreadId: 27,
                text: RespondText,
                //replyToMessageId: message.MessageId,
                replyMarkup: new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithUrl(
                        text: "ссылка на сообщение",
                        url: messageLink)),
                cancellationToken: cancellationToken);
                return;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine(ex.ToString());
        }
    }

    public static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(ErrorMessage);
        return Task.CompletedTask;
    }
}