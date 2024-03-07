public class TelegramDebtBot : TelegramCountBot
{

    public TelegramDebtBot() : base()
    {
        sqlite_db_name = "TelegramDebtBot.db";
        sqlite_table_name = "TelegramDebtBot";
        bot_appsettings_name = "DebtBot";
        what_to_count = "debt";
        value_type = Value_type.Real;
        allowed_value_multiple = 0.05M;
        init();
    }
}