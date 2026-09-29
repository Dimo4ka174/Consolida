using Application.DataAccessLayer.Service.Common;
using FluentAssertions;

namespace Consolida.UnitTests.Services;

public class RussianNumberToWordsConverterTests
{
    private readonly RussianNumberToWordsConverter _sut = new();

    // такConvert — целые рубли

    [Fact]
    public void Convert_Zero_ReturnsZeroRubles()
    {
        _sut.Convert(0m).Should().Be("ноль рублей 00 копеек");
    }

    [Theory]
    [InlineData(1, "один рубль 00 копеек")]
    [InlineData(2, "два рубля 00 копеек")]
    [InlineData(3, "три рубля 00 копеек")]
    [InlineData(4, "четыре рубля 00 копеек")]
    [InlineData(5, "пять рублей 00 копеек")]
    [InlineData(10, "десять рублей 00 копеек")]
    [InlineData(11, "одиннадцать рублей 00 копеек")]
    [InlineData(21, "двадцать один рубль 00 копеек")]
    [InlineData(22, "двадцать два рубля 00 копеек")]
    [InlineData(25, "двадцать пять рублей 00 копеек")]
    [InlineData(100, "сто рублей 00 копеек")]
    [InlineData(111, "сто одиннадцать рублей 00 копеек")]
    [InlineData(1000, "одна тысяча рублей 00 копеек")]
    [InlineData(2000, "две тысячи рублей 00 копеек")]
    [InlineData(5000, "пять тысяч рублей 00 копеек")]
    [InlineData(21000, "двадцать одна тысяча рублей 00 копеек")]
    [InlineData(1000000, "один миллион рублей 00 копеек")]
    [InlineData(2000000, "два миллиона рублей 00 копеек")]
    [InlineData(5000000, "пять миллионов рублей 00 копеек")]
    public void Convert_WholeRubles_ReturnsExpected(decimal amount, string expected)
    {
        _sut.Convert(amount).Should().Be(expected);
    }

    // Convert — с копейками

    [Theory]
    [InlineData(1.01, "один рубль 01 копейка")]
    [InlineData(1.02, "один рубль 02 копейки")]
    [InlineData(1.05, "один рубль 05 копеек")]
    [InlineData(1.11, "один рубль 11 копеек")]
    [InlineData(1.21, "один рубль 21 копейка")]
    [InlineData(1.22, "один рубль 22 копейки")]
    [InlineData(1.25, "один рубль 25 копеек")]
    [InlineData(1.50, "один рубль 50 копеек")]
    public void Convert_WithKopecks_ReturnsExpected(decimal amount, string expected)
    {
        _sut.Convert(amount).Should().Be(expected);
    }

    // Convert — округление копеек до 100

    [Fact]
    public void Convert_KopecksRoundUpTo100_IncrementsRubles()
    {
        // 1.995 → 1 рубль, (0.995 * 100) = 99.5 → Math.Round(99.5) = 100 (ToEven)
        // Значит: 2 рубля 00 копеек.
        _sut.Convert(1.995m).Should().Be("два рубля 00 копеек");
    }

    // FormatMoney

    [Fact]
    public void FormatMoney_WithWords_ContainsNumberRubAndWords()
    {
        var result = _sut.FormatMoney(1234.56m);

        result.Should().Contain("1 234,56");
        result.Should().Contain("РУБ");
        result.Should().Contain("одна тысяча двести тридцать четыре");
        result.Should().Contain("56 копеек");
    }

    [Fact]
    public void FormatMoney_WithoutWords_ContainsOnlyNumber()
    {
        var result = _sut.FormatMoney(1234.56m, includeWords: false);

        result.Should().Be("1 234,56");
    }

    [Fact]
    public void FormatMoney_UsesSpaceAsGroupSeparator()
    {
        var result = _sut.FormatMoney(1000000m, includeWords: false);

        result.Should().Be("1 000 000,00");
    }

    [Fact]
    public void FormatMoney_UsesCommaAsDecimalSeparator()
    {
        var result = _sut.FormatMoney(1.5m, includeWords: false);

        result.Should().Be("1,50");
    }
}
