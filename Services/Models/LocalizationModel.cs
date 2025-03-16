namespace Services.Models
{
    public static class Localization
    {
        public static Dictionary<string, Dictionary<string, string>> Texts = new()
        {
            {
                "ru", new Dictionary<string, string>
                {
                    { "DonateMessage", "Спасибо за желание поддержать автора! 🎉\nВы можете сделать это по ссылке ниже:" },
                    { "DonateButton", "Задонатить автору" },
                    { "HelpMessage", "Ваш выбор:" },
                    { "HelpButton", "Помощь (donate)" },
                    { "SubscriptionsButton", "Мои подписки" },
                    { "StopNotificationsButton", "Stop уведомления" },
                    { "StartNotificationsButton", "Start уведомления" }
                }
            },
            {
                "en", new Dictionary<string, string>
                {
                    { "DonateMessage", "Thank you for wanting to support the author! 🎉\nYou can do so via the link below:" },
                    { "DonateButton", "Donate to the author" },
                    { "HelpMessage", "Your choice:" },
                    { "HelpButton", "Help (donate)" },
                    { "SubscriptionsButton", "My subscriptions" },
                    { "StopNotificationsButton", "Stop notifications" },
                    { "StartNotificationsButton", "Start notifications" }
                }
            }
        };
    }
}
