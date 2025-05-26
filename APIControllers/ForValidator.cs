namespace ArtonitRESTAPI.APIControllers
{
    public class ForValidator
    {
        //Текст ошибки при проверке на ввод Id_cardtype от 1 до 4
        public const string Text1 = "Тип карты должен быть от 1 до 4";

        // Текст ошибки для Id_cardtype == 1
        public const string IdCardType1Format = "Для типа 1 Id_card должен состоять только из цифр 0–9 и букв A–F";

        // Текст ошибки для Id_cardtype == 2
        public const string IdCardType2Format = "Для типа 2 Id_card должен состоять только из цифр 0–9, букв A–F и буквы P";
        public const string IdPepPositive = "Id_pep должен быть положительным";


        //Данный метод задает условия для проверки
        public static class Conditions
        {
            public const string IdCardType1Regex = "^[0-9A-F]+$";
            public const string IdCardType2Regex = "^[0-9A-FP]+$";

            
            public const int CardTypeMin = 1;
            public const int CardTypeMax = 4;
            public const int ValidPep = 0;
        }
    }
}

