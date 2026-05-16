namespace Medital_Application.Helpers;

public static class NumberToWords
{
    private static readonly string[] Ones = {
        "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
        "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen",
        "Seventeen", "Eighteen", "Nineteen"
    };

    private static readonly string[] Tens = {
        "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"
    };

    public static string Convert(long number)
    {
        if (number == 0) return "Zero Rupees Only";
        if (number < 0) return "Minus " + Convert(-number);

        var result = "";
        if (number >= 10000000)
        {
            result += Convert(number / 10000000) + " Crore ";
            number %= 10000000;
        }
        if (number >= 100000)
        {
            result += Convert(number / 100000) + " Lakh ";
            number %= 100000;
        }
        if (number >= 1000)
        {
            result += Convert(number / 1000) + " Thousand ";
            number %= 1000;
        }
        if (number >= 100)
        {
            result += Ones[number / 100] + " Hundred ";
            number %= 100;
        }
        if (number >= 20)
        {
            result += Tens[number / 10] + " ";
            number %= 10;
        }
        if (number > 0)
            result += Ones[number] + " ";

        return (result.Trim() + " Rupees Only").Trim();
    }
}
