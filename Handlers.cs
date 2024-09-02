using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using System.Net.NetworkInformation;
using System.Runtime.ConstrainedExecution;

//Отдельный класс для сопоставления сотрудников и их рабочего времени
class Employee
{
    public enum DayOfWeekEnum
    {
        Sunday,
        Monday,
        Tuesday,
        Wednesday,
        Thursday,
        Friday,
        Saturday
    }
    public string Name { get; set; }
    private TimeSpan StartTime { get; set; }
    private TimeSpan EndTime { get; set; }
    private HashSet<DayOfWeekEnum> DaysOff { get; set; }

    public Employee(string name, TimeSpan startTime, TimeSpan endTime, IEnumerable<DayOfWeekEnum> dayOff) 
    {
        Name = name;
        StartTime = startTime;
        EndTime = endTime;
        DaysOff = new HashSet<DayOfWeekEnum>(dayOff);
    }

    public bool IsAvailable(DateTime currentTime)
    {
        //Проверка на выходной
        if(DaysOff.Contains((DayOfWeekEnum)currentTime.DayOfWeek))
        {
            return false;
        }

        //Проверка Рассписания
        return currentTime.TimeOfDay >= StartTime && currentTime.TimeOfDay <= EndTime;
    }
}
class Handlers
{
    //Словарь с ID чатов
    private static Dictionary<string,string> Chat_ids = new Dictionary<string,string>()
    {
        {"Test_chat","-4278844400"}, 
        {"Work_chat","-1001902270586"}
    };

    //Словарь с именами сотрудников и их user_name в телеграме
    private static Dictionary<string, string> EmployeeName = new Dictionary<string, string>()
    {
        {"Евгений","@Salomatin_Evgeny_Posadmin"},
        {"Виктория","@victoria_kazakova_POSADMIN"},
        {"Марина","@Marina_POSADMIN"},
        {"Руслан","@AlmakaevPosadmin"},
        {"Кирилл","@POSADMIN_Kirill"},
        {"Владислав","@vladislav_sidorov_posadmin"},
        {"Андрей","@POSADMIN_Sidorenko"},
        {"Дмитрий","@dpetelin_posdamin"},
        {"Константин","@Konstantin_Pushkin_POSADMIN"}
    };

    //Списов сотрудников с сопоставленым временем
    public static List<Employee> employees = new List<Employee>()
    {
        new Employee(EmployeeName["Евгений"], new TimeSpan(10,0,0), new TimeSpan(23,59,0), new List<Employee.DayOfWeekEnum> { Employee.DayOfWeekEnum.Sunday, Employee.DayOfWeekEnum.Saturday}),
        new Employee(EmployeeName["Виктория"], new TimeSpan(10,0,0), new TimeSpan(19,0,0), new List < Employee.DayOfWeekEnum > { Employee.DayOfWeekEnum.Tuesday, Employee.DayOfWeekEnum.Wednesday}),
        new Employee(EmployeeName["Марина"], new TimeSpan(17,0,0), new TimeSpan(23,59,0), new List < Employee.DayOfWeekEnum > { Employee.DayOfWeekEnum.Monday, Employee.DayOfWeekEnum.Tuesday}),
        new Employee(EmployeeName["Руслан"], new TimeSpan(10,0,0), new TimeSpan(19,0,0), new List < Employee.DayOfWeekEnum > { Employee.DayOfWeekEnum.Sunday, Employee.DayOfWeekEnum.Saturday}),
        new Employee(EmployeeName["Кирилл"], new TimeSpan(14,0,0), new TimeSpan(23,55,0), new List < Employee.DayOfWeekEnum > { Employee.DayOfWeekEnum.Thursday, Employee.DayOfWeekEnum.Friday}),
        new Employee(EmployeeName["Владислав"], new TimeSpan(10,0,0), new TimeSpan(19,0,0), new List < Employee.DayOfWeekEnum > { Employee.DayOfWeekEnum.Monday, Employee.DayOfWeekEnum.Tuesday}),
        new Employee(EmployeeName["Андрей"], new TimeSpan(14,0,0), new TimeSpan(23,0,0), new List < Employee.DayOfWeekEnum > { Employee.DayOfWeekEnum.Sunday, Employee.DayOfWeekEnum.Saturday}),
        new Employee(EmployeeName["Дмитрий"], new TimeSpan(17,0,0), new TimeSpan(23,59,0), new List < Employee.DayOfWeekEnum > { Employee.DayOfWeekEnum.Sunday, Employee.DayOfWeekEnum.Wednesday}),
        new Employee(EmployeeName["Константин"], new TimeSpan(0,0,0), new TimeSpan(8,0,0), new List < Employee.DayOfWeekEnum > { Employee.DayOfWeekEnum.Sunday, Employee.DayOfWeekEnum.Monday}),
    };

    public static string ConstantPing = "@amir_edygov_posadmin\n";
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

            DateTime currentTime = DateTime.Now; //Забираем текущее время

            List<string> AvailebalEmployees = new List<string>();

            foreach (var employee in employees)
            {
                if (employee.IsAvailable(currentTime))
                {
                    AvailebalEmployees.Add(employee.Name);
                }
            }

            string empResult = string.Join("\n", AvailebalEmployees);
            string FinalString = ConstantPing + empResult + "\n\n";

            // Сообщение "повтор" @pos_chatService_bot
            if (messageText.Contains("@pos_chatService_bot")){
                Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: Chat_ids["Work_chat"],  //Чат для тестов
                messageThreadId: 27,
                text: FinalString + RespondText,
                //replyToMessageId: message.MessageId,
                replyMarkup: new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithUrl(
                        text: "ссылка на сообщение",
                        url: messageLink)),
                cancellationToken: cancellationToken);
                return;
            }

            //Для проверки что бот работает
            if (messageText.Contains("/test") && userInfo.Id == 6017481524)
            {
                Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                messageThreadId: TopicId,
                text: "Status is OK",
                cancellationToken: cancellationToken);
                return;
            }
            else if (userInfo.Id != 6017481524)
            {
                Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: Chat_ids["Test_chat"],
                text: $"User: {userInfo.Username}|  with no access tried to use command /test",
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