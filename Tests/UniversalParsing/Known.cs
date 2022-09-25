using Tests.Utilities;

namespace Tests.UniversalParsing;

public static class Known
{
    public static bool IsKnownTag(this string value)
    {
        var input = value.Replace("&nbsp;", "");
        return Tags.HasPrefixOver(input) is > 0 and var i &&
               input[i..].Trim() is "" or ":";
    }
    

    private static readonly TrieSet Tags =new()
    {
        "Название",
        "Выпущено",
        "Озвучивает",
        "Описание",

        "Год выпуска",
        "Фамилия автора",
        "Фамилии авторов",
        "Фамилия авторов",
        "Фамилии и имена авторов",
        "Фамилия автора сценария",
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