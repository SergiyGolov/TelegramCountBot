using Telegram.Bot;
using Microsoft.Data.Sqlite;
using System.Text;
using Telegram.Bot.Types.Enums;
using System.Text.RegularExpressions;
using System.Numerics;
using System.Globalization;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Data.Common;

public class TelegramCountBot
{
    private TelegramBotClient bot_client = null!;
    private bool debug = true;
    private string bot_token = "";
    private HashSet<long> allowed_user_ids = new HashSet<long>();
    private long admin_user_id = 0;
    protected List<BotCommand> commands = [];

    public enum Value_type
    {
        Non_init,
        Real,
        Integer
    }

    protected string sqlite_db_name = "";
    protected string sqlite_table_name = "";
    protected string bot_appsettings_name = "";
    protected string what_to_count = "";
    protected Value_type value_type;
    protected decimal? allowed_value_multiple = null;

    protected void init()
    {
        var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json");
        var config = configuration.Build();

        string? bot_token_env = config[$"AppConfig:{bot_appsettings_name}:BOT_TOKEN"];
        string? admin_telegram_user_id_env = config[$"AppConfig:{bot_appsettings_name}:ADMIN_TELEGRAM_USER_ID"];

        ArgumentException.ThrowIfNullOrEmpty(bot_token_env);
        ArgumentException.ThrowIfNullOrEmpty(admin_telegram_user_id_env);
        ArgumentException.ThrowIfNullOrEmpty(sqlite_db_name);
        ArgumentException.ThrowIfNullOrEmpty(sqlite_table_name);
        ArgumentException.ThrowIfNullOrEmpty(bot_appsettings_name);
        ArgumentException.ThrowIfNullOrEmpty(what_to_count);

        if (value_type == Value_type.Non_init)
        {
            throw new ArgumentNullException();
        }
        else if (value_type == Value_type.Real)
        {
            ArgumentNullException.ThrowIfNull(allowed_value_multiple);
        }

        admin_user_id = long.Parse(admin_telegram_user_id_env!);
        allowed_user_ids.Add(admin_user_id);
        bot_token = bot_token_env;

        bot_client = new TelegramBotClient(bot_token!);
        init_db();
        init_commands();

        //needed for normalize_string(string accentedStr)
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private void init_commands()
    {
        string add_single_regex = @"^\w+?\s+\-*\d+\.*\d*$";
        string add_multiple_regex = @"^\w+(\s*,\s*\w+)+\s+\-*\d+\.*\d*$";
        Func<string, long, string> add_single_fun = add_single<float>;
        Func<string, long, string> add_multiple_fun = add_multiple<float>;
        if (value_type == Value_type.Integer)
        {
            add_single_regex = @"^\w+?\s+\-*\d+$";
            add_multiple_regex = @"^\w+(\s*,\s*\w+)+\s+\-*\d+$";
            add_single_fun = add_single<int>;
            add_multiple_fun = add_multiple<int>;
        }

        commands = [];
        commands.Add(new BotCommand("", (s) => s.StartsWith("/add_user"), add_user));
        string description = "<b>/help</b>: List all possible commands";
        commands.Add(new BotCommand(description, (s) => s.StartsWith("/help"), show_help));
        description = $"<b>/list</b>: List sum of all saved {what_to_count}";
        commands.Add(new BotCommand(description, (s) => s.StartsWith("/list"), list_all));
        description = "<b>/start</b>: /help + /list";
        commands.Add(new BotCommand(description, (s) => s.StartsWith("/start"), show_start));
        description = $"<b>[name] [number]</b>: Adds [number] to the sum of the {what_to_count} for [name] ([number] can also be negative)";
        commands.Add(new BotCommand(description, (s) => Regex.IsMatch(s, add_single_regex), add_single_fun));
        description = $"<b>[name A], [name B]... [number]</b>: Adds [number] to the sum of the {what_to_count} for each name in a list of comma separated names ([number] can also be negative)";
        commands.Add(new BotCommand(description, (s) => Regex.IsMatch(s, add_multiple_regex), add_multiple_fun));
    }

    protected DbConnection create_DB_Connection()
    {
        return new SqliteConnection($"Data Source=db/{sqlite_db_name}");
    }

    protected void add_parameter_with_value(DbCommand command, string parameter_name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = parameter_name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private void init_db()
    {
        string folderName = "db";
        if (!Directory.Exists(folderName))
        {
            Directory.CreateDirectory(folderName);
        }

        using var connection = create_DB_Connection();
        connection.Open();

        var command = connection.CreateCommand();

        string db_value_type = "real";
        if (value_type == Value_type.Integer)
        {
            db_value_type = "integer";
        }
        command.CommandText =
        $@"
                create table if not exists {sqlite_table_name}(
                    telegram_user_id integer not null,
                    name text not null,
                    value {db_value_type} not null
                );
            ";
        command.ExecuteNonQuery();

        command = connection.CreateCommand();
        command.CommandText = $"create index if not exists idx on {sqlite_table_name}(telegram_user_id,name);";
        command.ExecuteNonQuery();


        command = connection.CreateCommand();
        command.CommandText =
        $@"
                create table if not exists {sqlite_table_name}_allowedUsers(
                    telegram_user_id text not null
                );
            ";

        command.ExecuteNonQuery();

        command = connection.CreateCommand();
        command.CommandText = $"select telegram_user_id from {sqlite_table_name}_allowedUsers;";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            string user_id = reader.GetString(0);
            allowed_user_ids.Add(long.Parse(user_id));
        }
    }

    private string show_help()
    {
        return "<b>Available commands:</b>\n\n" + String.Join("\n\n", commands.Select(c => c.get_description()).Where(d => d != "").ToArray());
    }

    private async Task<string> show_start(long telegram_user_id)
    {
        await send_message_async(telegram_user_id, show_help());
        await send_message_async(telegram_user_id, list_all(telegram_user_id));
        return "";
    }

    public async Task<Telegram.Bot.Types.Message> send_message_async(long receiver_id, string message)
    {
        return await bot_client.SendTextMessageAsync(receiver_id, message, parseMode: ParseMode.Html);
    }

    protected string list_all(long telegram_user_id)
    {
        string response = "";
        using var connection = create_DB_Connection();

        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = $"select name,value from {sqlite_table_name} where telegram_user_id = $telegram_user_id order by value desc;";
        add_parameter_with_value(command, "$telegram_user_id", telegram_user_id);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            string name = reader.GetString(0);
            string value = "";
            if (value_type == Value_type.Real)
            {
                value = reader.GetFloat(1).ToString();
            }
            else
            {
                value = reader.GetInt16(1).ToString();

            }
            response += $"\n{name}: {value}";
        }

        if (response == "")
        {
            response = $"There are no {what_to_count} yet";
        }

        response = $"<b>List of all {what_to_count}:</b>\n" + response;


        return response;
    }

    /// <summary>
    /// Replace diacritics with their ASCII counterpart (e.g. é,è,ê -> e) and make the first letter uppercase
    /// </summary>
    /// <param name="accentedStr">initial name (e.g. hervé)</param>
    /// <returns>normalized text (e.g. Herve)</returns>
    private string normalize_string(string accentedStr)
    {
        accentedStr = char.ToUpper(accentedStr[0]) + accentedStr[1..];
        //source: https://stackoverflow.com/a/2086575
        byte[] tempBytes;
        tempBytes = System.Text.Encoding.GetEncoding("ISO-8859-8").GetBytes(accentedStr);
        string asciiStr = System.Text.Encoding.UTF8.GetString(tempBytes);
        return asciiStr;
    }

    /// <summary>
    /// Used to handle messages in the following form: "a,b,c x" which will add x to the count for a,b and c
    /// </summary>
    /// <param name="message">messages containing comma</param>
    /// <param name="telegram_user_id">chat id</param>
    /// <returns>response listing the sum of counts of the names specified in the message after updating their counts</returns>
    private string add_multiple<T>(string message, long telegram_user_id) where T : INumber<T>
    {
        string response = "";
        string[] splitted_messages_by_commas = message.Split(',');
        string[] last_name_splitted = splitted_messages_by_commas[splitted_messages_by_commas.Length - 1].Trim().Split();

        T value = parse_T<T>(last_name_splitted[last_name_splitted.Length - 1]);

        string[] names = splitted_messages_by_commas[..^1];
        names = [.. names, last_name_splitted[0]];

        names = names.Select(n => n.Trim()).ToArray();

        HashSet<string> names_set = [.. names];

        if (names_set.Count != names.Length)
        {
            string error = "Error, you have the same name multiple times in your list";
            throw new Exception(error);
        }

        foreach (string name in names)
        {
            string single_message = $"{name} {value}";
            response += $"\n{add_single<T>(single_message, telegram_user_id)}";
        }

        return response;
    }

    private T parse_T<T>(string s) where T : INumber<T>
    {
        return T.Parse(s, CultureInfo.InvariantCulture);

    }

    private string add_single<T>(string message, long telegram_user_id) where T : INumber<T>
    {
        string response = "";
        (string name, T value) = extract_name_value_from_message<T>(message);
        name = normalize_string(name);
        if (value_type == Value_type.Real)
        {
            value = T.CreateChecked(normalize_value(Convert.ToSingle(value)));
        }
        T new_value = update_value(name, value, telegram_user_id);
        response = $"Sum {what_to_count} {name}: {new_value}";
        return response;
    }

    public async Task<string> handle_message(string message, long telegram_user_id)
    {
        string response = "";
        if (!allowed_user_ids.Contains(telegram_user_id))
        {
            response = $"You are not allowed to use this bot, please forward the following message to the owner of the bot:";
            await send_message_async(telegram_user_id, response);

            response = $"/add_user {telegram_user_id}";
            await send_message_async(telegram_user_id, response);

            return "";
        }

        bool valid_command = false;
        try
        {
            foreach (BotCommand c in commands)
            {
                (valid_command, response) = await c.handle_command(message, telegram_user_id);
                if (valid_command)
                {
                    break;
                }
            }
            if (!valid_command)
            {
                response = "Error, invalid command";
            }
        }
        catch (Exception e)
        {
            if (debug)
            {
                Console.WriteLine(message);
                Console.WriteLine(e.GetType());
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
            response = e.Message;
        }

        return response;
    }

    private async Task<string> add_user(string message, long telegram_user_id)
    {
        if (admin_user_id != telegram_user_id)
        {
            string error = "Error, only the bot owner is allowed to add a new user";
            throw new Exception(error);
        }

        long new_user_id = long.Parse(message.Split()[1]);

        if (allowed_user_ids.Contains(new_user_id))
        {
            throw new Exception($"Error, {new_user_id} is already in the allowed users list");
        }

        try
        {
            await send_message_async(new_user_id, "The bot owner added you in the allowed users list");
            await send_message_async(new_user_id, show_help());
        }
        catch (Telegram.Bot.Exceptions.ApiRequestException)
        {
            throw new Exception($"Error, {new_user_id} is an invalid telegram user id or the user hasn't opened a chat with the bot yet.");
        }

        allowed_user_ids.Add(new_user_id);

        using (var connection = create_DB_Connection())
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = $"insert into {sqlite_table_name}_allowedUsers values($telegram_user_id);";
            add_parameter_with_value(command, "$telegram_user_id", new_user_id);
            command.ExecuteNonQuery();
        }

        return $"{new_user_id} added to allowed users list";

    }

    private float normalize_value(float value)
    {
        decimal decimal_value = Convert.ToDecimal(value);

        if (decimal_value % allowed_value_multiple != 0)
        {
            string error = "Error, your value must be a multiple of 0.05";
            throw new Exception(error);
        }
        return (float)decimal_value;
    }

    private (string, T) extract_name_value_from_message<T>(string message) where T : INumber<T>
    {
        T value = T.CreateChecked(0);
        string name = " ";
        String error = "";
        string[] split_message = message.Split();
        if (split_message.Length < 2)
        {
            error = "Error, your message must contain a name with a number separated with a whitespace";
            throw new Exception(error);
        }
        name = split_message[0];
        try
        {
            value = parse_T<T>(split_message[split_message.Length - 1]);
        }
        catch (System.FormatException)
        {
            error = "Error, your message didn't contain a valid number";
            throw new Exception(error);
        }
        return (name, value);
    }

    private T update_value<T>(string name, T value, long telegram_user_id) where T : INumber<T>
    {

        using var connection = create_DB_Connection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = $"select value from {sqlite_table_name} where telegram_user_id = $telegram_user_id and name = $name;";
        add_parameter_with_value(command, "$telegram_user_id", telegram_user_id);
        add_parameter_with_value(command, "$name", name);

        var result = command.ExecuteScalar();

        if (result is null)
        {
            command = connection.CreateCommand();
            command.CommandText = $"insert into {sqlite_table_name} values($telegram_user_id,$name,$value);";
            add_parameter_with_value(command, "$telegram_user_id", telegram_user_id);
            add_parameter_with_value(command, "$name", name);
            add_parameter_with_value(command, "$value", value);
            command.ExecuteNonQuery();
            return value;
        }
        else
        {
            T old_value = parse_T<T>(result.ToString()!);

            T new_value;
            if (value_type == Value_type.Real)
            {
                new_value = T.CreateChecked(Convert.ToDecimal(old_value) + Convert.ToDecimal(value));
            }
            else
            {
                new_value = old_value + value;
            }

            if (new_value == T.CreateChecked(0))
            {
                command = connection.CreateCommand();
                command.CommandText = $@"
                    delete from {sqlite_table_name}
                        where telegram_user_id = $telegram_user_id and name = $name;
                    ";
                add_parameter_with_value(command, "$telegram_user_id", telegram_user_id);
                add_parameter_with_value(command, "$name", name);
                command.ExecuteNonQuery();
            }
            else
            {
                command = connection.CreateCommand();
                command.CommandText = $@"
                    update {sqlite_table_name}
                        set value = $value
                        where telegram_user_id = $telegram_user_id and name = $name;
                    ";
                add_parameter_with_value(command, "$telegram_user_id", telegram_user_id);
                add_parameter_with_value(command, "$name", name);
                add_parameter_with_value(command, "$value", new_value);
                command.ExecuteNonQuery();
            }

            return new_value;
        }
    }

}