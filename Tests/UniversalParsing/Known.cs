namespace Tests.UniversalParsing;

public static class Known
{
    public static bool IsKnownTag(this string value)
    {
        var strip = value.Replace("&nbsp;", "");
        return !string.IsNullOrWhiteSpace(strip) &&
               Tags.Any(t => Eq(strip, t));
    }

    private static bool Eq(string value, string tag) =>
        value.StartsWith(tag) &&
        value[tag.Length..].Trim() is "" or ":";

    private static readonly string[] Tags =
    {
        "Название",
        "Выпущено",
        "Озвучивает",
        "Описание",

        "Год выпуска",
        "Фамилия автора",
        "Фамилии авторов",
        "Фамилия авторов",
        "Aвтор",
        "Автор",
        "Автора",
        "Авторы",
        "Имя автора",
        "Имена авторов",
        "Исполнитель",
        "Исполнители",
        "Исполнитель и звукорежиссёр",
        "Цикл",
        "Цикл/серия",
        "Серия",
        "Теги",
        "Номер книги",
        "Жанр",
        "Время звучания"
    };
}