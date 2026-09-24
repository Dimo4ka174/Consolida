using System.Globalization;

namespace Application.DataAccessLayer.Service.Common
{
    public class RussianNumberToWordsConverter
    {
        private static readonly string[] Units = { "", "один", "два", "три", "четыре", "пять", "шесть", "семь", "восемь", "девять" };
        private static readonly string[] UnitsFemale = { "", "одна", "две", "три", "четыре", "пять", "шесть", "семь", "восемь", "девять" };
        private static readonly string[] Teens = { "десять", "одиннадцать", "двенадцать", "тринадцать", "четырнадцать", "пятнадцать", "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать" };
        private static readonly string[] Tens = { "", "десять", "двадцать", "тридцать", "сорок", "пятьдесят", "шестьдесят", "семьдесят", "восемьдесят", "девяносто" };
        private static readonly string[] Hundreds = { "", "сто", "двести", "триста", "четыреста", "пятьсот", "шестьсот", "семьсот", "восемьсот", "девятьсот" };

        private static readonly string[] ThousandsForms = { "тысяча", "тысячи", "тысяч" };
        private static readonly string[] MillionsForms = { "миллион", "миллиона", "миллионов" };
        private static readonly string[] BillionsForms = { "миллиард", "миллиарда", "миллиардов" };
        private static readonly string[] RublesForms = { "рубль", "рубля", "рублей" };
        private static readonly string[] KopecksForms = { "копейка", "копейки", "копеек" };

        public string Convert(decimal amount)
        {
            if (amount == 0)
                return "ноль рублей 00 копеек";

            var rubles = (long)Math.Floor(amount);
            var kopecks = (long)Math.Round((amount - rubles) * 100);

            // Обработка случая, когда копейки округляются до 100
            if (kopecks == 100)
            {
                rubles++;
                kopecks = 0;
            }

            var rublesText = ConvertInteger(rubles) + " " + GetRublesForm(rubles);
            var kopecksText = $"{kopecks:00} {GetKopecksForm(kopecks)}";

            return $"{rublesText} {kopecksText}";
        }

        public string FormatMoney(decimal amount, bool includeWords = true)
        {
            var nfi = new NumberFormatInfo
            {
                NumberGroupSeparator = " ",
                NumberDecimalSeparator = ",",
                NumberDecimalDigits = 2
            };

            var formattedAmount = amount.ToString("N", nfi);

            if (!includeWords)
                return formattedAmount;

            var words = Convert(amount);
            return $"{formattedAmount} РУБ ({words})";
        }

        private string ConvertInteger(long number)
        {
            if (number == 0)
                return "ноль";

            var parts = new List<string>();

            // Обрабатываем миллиарды
            var billions = number / 1_000_000_000;
            if (billions > 0)
            {
                parts.Add(ConvertTriplet((int)billions, false, false));
                parts.Add(GetScaleForm((int)billions, BillionsForms));
                number %= 1_000_000_000;
            }

            // Обрабатываем миллионы
            var millions = number / 1_000_000;
            if (millions > 0)
            {
                parts.Add(ConvertTriplet((int)millions, false, false));
                parts.Add(GetScaleForm((int)millions, MillionsForms));
                number %= 1_000_000;
            }

            // Обрабатываем тысячи
            var thousands = number / 1000;
            if (thousands > 0)
            {
                parts.Add(ConvertTriplet((int)thousands, true, false)); // Для тысяч используем женский род
                parts.Add(GetScaleForm((int)thousands, ThousandsForms));
                number %= 1000;
            }

            // Обрабатываем единицы
            if (number > 0 || parts.Count == 0) // Если всё число было 0, то "ноль" уже обработан
            {
                parts.Add(ConvertTriplet((int)number, false, false));
            }

            return string.Join(" ", parts.Where(p => !string.IsNullOrEmpty(p)));
        }

        private string ConvertTriplet(int number, bool isFemaleForThousands, bool isFemaleForUnits)
        {
            var units = isFemaleForThousands ? UnitsFemale : Units;
            var result = new List<string>();

            var hundreds = number / 100;
            if (hundreds > 0)
                result.Add(Hundreds[hundreds]);

            var tensUnits = number % 100;
            if (tensUnits >= 20)
            {
                var tens = tensUnits / 10;
                result.Add(Tens[tens]);

                var unit = tensUnits % 10;
                if (unit > 0)
                    result.Add(isFemaleForUnits ? UnitsFemale[unit] : units[unit]);
            }
            else if (tensUnits >= 10)
            {
                result.Add(Teens[tensUnits - 10]);
            }
            else if (tensUnits > 0)
            {
                result.Add(isFemaleForUnits ? UnitsFemale[tensUnits] : units[tensUnits]);
            }

            return string.Join(" ", result);
        }

        private string GetScaleForm(int number, string[] forms)
        {
            // Для чисел от 11 до 19 всегда форма множественного числа
            if (number % 100 >= 11 && number % 100 <= 19)
                return forms[2];

            switch (number % 10)
            {
                case 1:
                    return forms[0];
                case 2:
                case 3:
                case 4:
                    return forms[1];
                default:
                    return forms[2];
            }
        }

        private string GetRublesForm(long number)
        {
            return GetScaleForm((int)(number % 100), RublesForms);
        }

        private string GetKopecksForm(long number)
        {
            return GetScaleForm((int)(number % 100), KopecksForms);
        }
    }
}