class TelegramScrabbleBot : TelegramCountBot
{
    public TelegramScrabbleBot() : base()
    {
        sqlite_db_name = "TelegramScrabbleBot.db";
        sqlite_table_name = "TelegramScrabbleBot";
        bot_appsettings_name = "ScrabbleBot";
        what_to_count = "scrabble points";
        value_type = Value_type.Integer;
        init();
        string description = "<b>/reset</b>: Resets the points";
        commands.Add(new BotCommand(description, (s) => s.StartsWith("/reset"), reset_points));
    }

    private string reset_points(long telegram_user_id)
    {
        using var connection = create_DB_Connection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = $@"
                    delete from {sqlite_table_name}
                        where telegram_user_id = $telegram_user_id;
                    ";
        add_parameter_with_value(command, "$telegram_user_id", telegram_user_id);
        command.ExecuteNonQuery();
        return "The points have ben reset";
    }
}