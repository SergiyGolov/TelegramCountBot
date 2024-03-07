public class BotCommand
{

    private string description;
    private Func<string, bool> test_fun;
    private Func<string, string>? return_fun_string = null;
    private Func<string>? return_fun = null;
    private Func<string, long, string>? return_fun_string_long = null;
    private Func<long, string>? return_fun_long = null;
    private Func<string, long, Task<string>>? return_fun_string_long_task = null;
    private Func<long, Task<string>>? return_fun_long_task = null;

    public BotCommand(string description, Func<string, bool> test_fun, Func<string, string> return_fun)
    {
        this.description = description;
        this.test_fun = test_fun;
        this.return_fun_string = return_fun;
    }

    public BotCommand(string description, Func<string, bool> test_fun, Func<string> return_fun)
    {
        this.description = description;
        this.test_fun = test_fun;
        this.return_fun = return_fun;
    }

    public BotCommand(string description, Func<string, bool> test_fun, Func<string, long, Task<string>> return_fun)
    {
        this.description = description;
        this.test_fun = test_fun;
        this.return_fun_string_long_task = return_fun;
    }

    public BotCommand(string description, Func<string, bool> test_fun, Func<long, Task<string>> return_fun)
    {
        this.description = description;
        this.test_fun = test_fun;
        this.return_fun_long_task = return_fun;
    }

    public BotCommand(string description, Func<string, bool> test_fun, Func<string, long, string> return_fun)
    {
        this.description = description;
        this.test_fun = test_fun;
        this.return_fun_string_long = return_fun;
    }

    public BotCommand(string description, Func<string, bool> test_fun, Func<long, string> return_fun)
    {
        this.description = description;
        this.test_fun = test_fun;
        this.return_fun_long = return_fun;
    }

    public string get_description()
    {
        return this.description;
    }

    public async Task<(bool, string)> handle_command(string command, long telegram_user_id)
    {
        bool valid_command = test_fun(command);
        if (!valid_command)
        {
            return (valid_command, "");
        }
        else
        {
            if (return_fun_string_long is not null)
            {
                return (valid_command, return_fun_string_long!(command, telegram_user_id));
            }
            else if (return_fun_long is not null)
            {
                return (valid_command, return_fun_long!(telegram_user_id));
            }
            else if (return_fun_string is not null)
            {
                return (valid_command, return_fun_string!(command));
            }
            else if (return_fun_string_long_task is not null)
            {
                return (valid_command, await return_fun_string_long_task!(command, telegram_user_id));
            }
            else if (return_fun_long_task is not null)
            {
                return (valid_command, await return_fun_long_task!(telegram_user_id));
            }
            else
            {
                return (valid_command, return_fun!());
            }
        }
    }
}