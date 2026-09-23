namespace DB.Authorization
{
    public static class PermissionTranslations
    {
        public static readonly Dictionary<string, string> GroupTranslations = new()
        {
            { "Roles", "Роли" },
            { "Users", "Пользователи" },
            { "Cities", "Города" },
            { "Companies", "Компании" },
            { "Customers", "Клиенты" },
        };
    }
}
