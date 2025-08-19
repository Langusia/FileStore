using System.Globalization;

namespace Credo.Core.Shared.Helpers
{
    public class NumberHelper
    {
        #region Numbers

        private static bool IsEven(long number)
        {
            return (number % 2 == 0);
        }

        public enum CurrencyEnum
        {
            NULL,
            GEL,
            USD
        }

        private enum NumbersEnum
        {
            ნული = 0,
            ერთი = 1,
            ორი = 2,
            სამი = 3,
            ოთხი = 4,
            ხუთი = 5,
            ექვსი = 6,
            შვიდი = 7,
            რვა = 8,
            ცხრა = 9,
            ათი = 10,
            თერთმეტი = 11,
            თორმეტი = 12,
            ცამეტი = 13,
            თოთხმეტი = 14,
            თხუთმეტი = 15,
            თექვსმეტი = 16,
            ჩვიდმეტი = 17,
            თვრამეტი = 18,
            ცხრამეტი = 19,
            ოცი = 20,
            ოცდაათი = 30,
            ორმოცი = 40,
            ორმოცდაათი = 50,
            სამოცი = 60,
            სამოცდაათი = 70,
            ოთხმოცი = 80,
            ოთხმოცდაათი = 90,
            ასი = 100,
            ათასი = 1000,
            მილიონი = 1000000,
            მილიარდი = 1000000000
        }

        private static string ProcessDigits(int digit)
        {
            if (digit.ToString().Length == 1)
            {
                if (digit == 0)
                {
                    return "";
                }
                else
                {
                    return DigitFromEnum(digit);
                }
            }

            if (digit.ToString().Length == 2 && digit <= 20)
                return DigitFromEnum(digit);

            int leftNumber = Convert.ToInt32(digit.ToString().Substring(0, 1));
            int rightNumber = Convert.ToInt32(digit.ToString().Substring(1));

            string zeroz = "";
            string da = null;
            for (int i = 1; i <= digit.ToString().Length - 1; i++)
            {
                zeroz += 0;
            }


            switch (digit)
            {
                case 1: // TODO: to 20
                    return DigitFromEnum(digit);
                case 20:
                    return "ოცი";
                case 30:
                    return "ოცდაათი";
                case 40:
                    return "ორმოცი";
                case 50:
                    return "ორმოცდაათი";
                case 60:
                    return "სამოცი";
                case 70:
                    return "სამოცდაათი";
                case 80:
                    return "ოთხმოცი";
                case 90:
                    return "ოთხმოცდაათი";
                case 100:
                    return "ასი";
                case 200:
                    return "ორასი";
                case 300:
                    return "სამასი";
                case 400:
                    return "ოთხასი";
                case 500:
                    return "ხუთასი";
                case 600:
                    return "ექვსასი";
                case 700:
                    return "შვიდასი";
                case 800:
                    return "რვაასი";
                case 900:
                    return "ცხრაასი";
                case 1000:
                    return "ათასი";
                case 10000:
                    return "ათი ათასი";
                case 100000:
                    return "ასი ათასი";
                case 1000000:
                    return "მილიონი";
                case 10000000:
                    return "ათი მილიონი";
                case 100000000:
                    return "ასი მილიონი";
                case 1000000000:
                    return "მილიარდი";
                default:

                    if (digit.ToString().Length > 2)
                    {
                        da = "";
                    }
                    else
                    {
                        da = "და";
                        if (IsEven(leftNumber) == false && leftNumber != 1)
                        {
                            string p1 = DigitFromEnum(Convert.ToInt32($"{leftNumber - 1}{zeroz}")).TrimEnd('ი');
                            return p1 + da + DigitFromEnum(Convert.ToInt32($"{1}{rightNumber}"));
                        }
                        else
                        {
                            return DigitFromEnum(Convert.ToInt32($"{leftNumber}{zeroz}")).Trim('ი') + da + DigitFromEnum(rightNumber);
                        }
                    }

                    const char trimSymbol = 'ი';
                    //TrimSymbol = IIf(Digit.ToString.Length = 4, "", "ი")
                    string leftSide = DigitFromEnum(leftNumber);
                    leftSide = (leftSide.EndsWith("ა") ? leftSide : leftSide.Trim(trimSymbol));
                    leftSide = (leftNumber == 1 ? "" : leftSide);
                    string middleSide = DigitFromEnum(Convert.ToInt32($"{1}{zeroz}"));
                    middleSide = (middleSide.EndsWith("ა") ? middleSide : middleSide.TrimEnd(trimSymbol));
                    return leftSide + middleSide + " " + da + ProcessDigits(rightNumber);
            }
        }

        private static string DigitFromEnum(int digit)
        {
            return Enum.GetName(typeof(NumbersEnum), digit);
        }

        public static string NumberToWord(int digit)
        {
            switch (digit)
            {
                case 0:
                    return "ნული";
                case 10:
                    return "ათი";
                case 20:
                    return "ოცი";
                case 30:
                    return "ოცდაათი";
                case 40:
                    return "ორმოცი";
                case 50:
                    return "ორმოცდაათი";
                case 60:
                    return "სამოცი";
                case 70:
                    return "სამოცდაათი";
                case 80:
                    return "ოთხმოცი";
                case 90:
                    return "ოთხმოცდაათი";
                case 100:
                    return "ასი";
                case 1000:
                    return "ათასი";
                case 10000:
                    return "ათი ათასი";
                case 100000:
                    return "ასი ათასი";
                case 1000000:
                    return "მილიონი";
                case 10000000:
                    return "ათი მილიონი";
                case 100000000:
                    return "ასი მილიონი";
                case 1000000000:
                    return "მილიარდი";
            }

            return ProcessNumberToWord(digit);
        }

        private static string ProcessNumberToWord(int digit)
        {
            string result = "";
            string result1 = "";
            string result2 = "";

            switch (digit.ToString().Length)
            {
                case 1:
                    result = ProcessDigits(digit);
                    result = result.EndsWith("ა") ? result : (result.EndsWith("ი") ? result : result + "ი");
                    return result;
                case 2:
                    result = ProcessDigits(digit);
                    result = result.EndsWith("ა") ? result : (result.EndsWith("ი") ? result : result + "ი");
                    return result;
                case 3:
                    result = ProcessDigits(digit);
                    result = result.TrimEnd(' ');
                    result = result.EndsWith("ა") ? result : (result.EndsWith("ი") ? result : result + "ი");
                    return result;
                case 4:
                    if (digit.ToString().StartsWith("1"))
                    {
                        result = ProcessDigits(digit);
                        result = result.TrimEnd(' ');
                        result = (result.EndsWith("ა") ? result : (result.EndsWith("ი") ? result : result + "ი"));

                        return result;
                    }
                    else
                    {
                        result1 = ProcessDigits(Convert.ToInt32(digit.ToString().Substring(0, 1)));
                        result1 = (result1.EndsWith("ა") ? result1 : (result1.EndsWith("ი") ? result1 : result1 + "ი"));
                        result1 = result1.Replace(" ", "");

                        result2 = ProcessDigits(Convert.ToInt32($"{1}{digit.ToString().Substring(1)}"));
                        result2 = result2.TrimEnd(' ');
                        result2 = (result2.EndsWith("ა") ? result2 : (result2.EndsWith("ი") ? result2 : result2 + "ი"));
                        return result1 + ' ' + result2;
                    }

                case 5:

                    result1 = ProcessDigits(Convert.ToInt32($"{digit.ToString().Substring(0, 2)}"));
                    result1 = (result1.EndsWith("ა") ? result1 : (result1.EndsWith("ი") ? result1 : result1 + "ი"));
                    result1 = result1.Replace(" ", "");

                    result2 = ProcessDigits(Convert.ToInt32($"{1}{digit.ToString().Substring(2)}"));
                    result2 = (result2.EndsWith("ა") ? result2 : (result2.EndsWith("ი") ? result2 : result2 + "ი"));

                    return result1 + ' ' + result2;

                case 6:

                    result1 = ProcessDigits(Convert.ToInt32($"{digit.ToString().Substring(0, 3)}"));
                    result1 = result1.TrimEnd(' ');
                    result1 = (result1.EndsWith("ა") ? result1 : (result1.EndsWith("ი") ? result1 : result1 + "ი"));


                    result2 = ProcessDigits(Convert.ToInt32($"{1}{digit.ToString().Substring(3)}"));
                    result2 = result2.TrimEnd(' ');
                    result2 = (result2.EndsWith("ა") ? result2 : (result2.EndsWith("ი") ? result2 : result2 + "ი"));

                    return result1 + ' ' + result2;
                case 7:
                    if (digit.ToString().Substring(1) == "000000")
                    {
                        if (digit.ToString().StartsWith("1"))
                        {
                            result1 = ProcessDigits(digit);
                            result1 = (result1.EndsWith("ა") ? result1 : (result1.EndsWith("ი") ? result1 : result1 + "ი"));
                            return result1;
                        }
                        else
                        {
                            result1 = ProcessDigits(Convert.ToInt32(digit.ToString().Substring(0, 1))) + ' ' + DigitFromEnum(Convert.ToInt32($"{1}{digit.ToString().Substring(1)}"));
                            result1 = result1.EndsWith("ა") ? result1 : (result1.EndsWith("ი") ? result1 : result1 + "ი");
                            return result1;
                        }
                    }


                    if (digit.ToString().StartsWith("1"))
                    {
                        result1 = DigitFromEnum(Convert.ToInt32($"{1}000000"));
                        result1 = result1.TrimEnd('ი');

                        return result1 + ' ' + ProcessNumberToWord(Convert.ToInt32(digit.ToString().Substring(1)));
                    }
                    else
                    {
                        result1 = ProcessDigits(Convert.ToInt32(digit.ToString().Substring(0, 1))) + ' ' + DigitFromEnum(Convert.ToInt32($"{1}000000"));
                        result1 = result1.TrimEnd('ი');

                        return result1 + ' ' + ProcessNumberToWord(Convert.ToInt32(digit.ToString().Substring(1)));
                    }

                case 8:

                    if (digit.ToString().Substring(2) == "000000")
                    {
                        result1 = ProcessDigits(Convert.ToInt32(digit.ToString().Substring(0, 2))) + ' ' + DigitFromEnum(Convert.ToInt32($"{1}{digit.ToString().Substring(2)}"));
                        result1 = result1.EndsWith("ა") ? result1 : (result1.EndsWith("ი") ? result1 : result1 + "ი");
                        return result1;
                    }
                    else
                    {
                        result1 = ProcessDigits(Convert.ToInt32(digit.ToString().Substring(0, 2))) + ' ' + DigitFromEnum(Convert.ToInt32($"{1}000000"));
                        result1 = result1.TrimEnd('ი');

                        return result1 + ' ' + ProcessNumberToWord(Convert.ToInt32(digit.ToString().Substring(2)));
                    }

                case 9:

                    if (digit.ToString().Substring(3) == "000000")
                    {
                        result1 = ProcessDigits(Convert.ToInt32(digit.ToString().Substring(0, 3))) + ' ' + DigitFromEnum(Convert.ToInt32($"{1}{digit.ToString().Substring(3)}"));
                        result1 = result1.EndsWith("ა") ? result1 : (result1.EndsWith("ი") ? result1 : result1 + "ი");
                        return result1;
                    }
                    else
                    {
                        result1 = ProcessDigits(Convert.ToInt32(digit.ToString().Substring(0, 3))) + ' ' + DigitFromEnum(Convert.ToInt32($"{1}000000"));
                        result1 = result1.TrimEnd('ი');

                        return result1 + ' ' + ProcessNumberToWord(Convert.ToInt32(digit.ToString().Substring(3)));
                    }

                case 10:
                    break;
                //მილიარდი
            }

            return "";
        }

        public static string DecimalToWord(decimal number, string currency = null, string lang = "GEO")
        {
            var result = "";

            var leftSideText = "";
            var rightSideText = "";

            var numberStr = "";
            if (currency != null)
            {
                number = Math.Round(number, 2);
                numberStr = number.ToString("0.00", StandardCultureGet());
            }
            else
                numberStr = number.ToString("0.00000000", StandardCultureGet());


            var splittedNumber = numberStr.Split(new[] { "." }, StringSplitOptions.None);
            var leftNumber = splittedNumber[0];
            var rightNumber = numberStr.Contains('.') ? splittedNumber[1] : null;

            switch (currency)
            {
                case "GEL":
                    leftSideText = "ლარი";
                    rightSideText = "თეთრი";

                    break;
                case "USD":
                    leftSideText = "აშშ დოლარი";
                    rightSideText = "ცენტი";
                    break;
                case "EUR":
                    leftSideText = "ევრო";
                    rightSideText = "ევროცენტი";
                    break;
                case "RUB":
                    leftSideText = "რუსული რუბლი";
                    rightSideText = "კაპიკი";
                    break;


                //case CurrencyEnum.NULL:
                //    if (rightNumber != null && Convert.ToInt32(rightNumber) > 0)
                //    {
                //        leftSideText = "მთელი";
                //        foreach (var rightNumberChar in rightNumber.ToCharArray())
                //        {
                //            if (rightNumberChar == '0')
                //                rightSideText += "ნულ ";
                //            else
                //                break;
                //        }
                //        rightSideText = rightSideText.TrimEnd(' ');
                //    }

                //    break;
            }

            if (rightNumber != null && Convert.ToInt32(rightNumber) > 0)
                result = NumberToWord(Convert.ToInt32(leftNumber)) + (string.IsNullOrWhiteSpace(leftSideText) ? "" : $" {leftSideText}") + $" და {NumberToWord(Convert.ToInt32(rightNumber))}" +
                         (string.IsNullOrWhiteSpace(rightSideText) ? "" : $" {rightSideText}");
            else
                result = NumberToWord(Convert.ToInt32(leftNumber)) + (string.IsNullOrWhiteSpace(leftSideText) ? "" : $" {leftSideText}");

            return result;
        }

        #endregion

        public static CultureInfo StandardCultureGet(string cultureName = "ka-GE")
        {
            var culture = CultureInfo.CreateSpecificCulture(cultureName);

            culture.DateTimeFormat.ShortDatePattern = "yyyy-MM-dd";
            culture.DateTimeFormat.LongDatePattern = "yyyy-MM-dd";
            culture.DateTimeFormat.ShortTimePattern = "HH:mm:ss";
            culture.DateTimeFormat.LongTimePattern = "HH:mm:ss";
            culture.NumberFormat.NumberDecimalSeparator = ".";
            culture.NumberFormat.CurrencyDecimalSeparator = ".";
            culture.NumberFormat.PercentDecimalSeparator = ".";

            return culture;
        }
    }
}