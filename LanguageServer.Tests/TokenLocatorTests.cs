using Assembler;
using Assembler.Lexeme;

namespace LanguageServer.Tests;

public class TokenLocatorTests
{
    private TokenLocator _locator = null!;

    [SetUp]
    public void Setup()
    {
        _locator = new TokenLocator();
    }

    private static List<Token> Tokenize(string source)
    {
        return new Lexer().Tokenize(source);
    }

    [Test]
    public void FindTokenAt_ExactPosition_ReturnsToken()
    {
        var tokens = Tokenize("nop");
        var result = _locator.FindTokenAt(tokens, 0, 0);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Token.Lexeme, Is.EqualTo("nop"));
    }

    [Test]
    public void FindTokenAt_MidSpan_ReturnsToken()
    {
        var tokens = Tokenize("nop");
        var result = _locator.FindTokenAt(tokens, 0, 1);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Token.Lexeme, Is.EqualTo("nop"));
    }

    [Test]
    public void FindTokenAt_Whitespace_ReturnsNull()
    {
        var tokens = Tokenize("nop   hlt");
        var result = _locator.FindTokenAt(tokens, 0, 4);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void IsDirectiveContext_AfterDot_ReturnsTrue()
    {
        var tokens = Tokenize(".text");
        // Tokens: Dot, Identifier("text"), EndOfLine, EndOfFile
        // Find the "text" identifier token
        var textIndex = tokens.FindIndex(t => t.Lexeme == "text");
        Assert.That(textIndex, Is.GreaterThan(0));
        Assert.That(_locator.IsDirectiveContext(tokens, textIndex), Is.True);
    }

    [Test]
    public void IsDirectiveContext_NotAfterDot_ReturnsFalse()
    {
        var tokens = Tokenize("nop");
        var nopIndex = tokens.FindIndex(t => t.Lexeme == "nop");
        Assert.That(_locator.IsDirectiveContext(tokens, nopIndex), Is.False);
    }

    [Test]
    public void IsInstructionMnemonic_FirstIdentifier_ReturnsTrue()
    {
        var tokens = Tokenize("nop");
        var nopIndex = tokens.FindIndex(t => t.Lexeme == "nop");
        Assert.That(_locator.IsInstructionMnemonic(tokens, nopIndex), Is.True);
    }

    [Test]
    public void IsInstructionMnemonic_AfterLabel_ReturnsTrue()
    {
        var tokens = Tokenize("start: nop");
        var nopIndex = tokens.FindIndex(t => t.Lexeme == "nop");
        Assert.That(_locator.IsInstructionMnemonic(tokens, nopIndex), Is.True);
    }

    [Test]
    public void IsLabelDefinition_FollowedByColon_ReturnsTrue()
    {
        var tokens = Tokenize("start: nop");
        var startIndex = tokens.FindIndex(t => t.Lexeme == "start");
        Assert.That(_locator.IsLabelDefinition(tokens, startIndex), Is.True);
    }

    [Test]
    public void IsLabelDefinition_NotFollowedByColon_ReturnsFalse()
    {
        var tokens = Tokenize("nop");
        var nopIndex = tokens.FindIndex(t => t.Lexeme == "nop");
        Assert.That(_locator.IsLabelDefinition(tokens, nopIndex), Is.False);
    }

    [Test]
    public void GetMnemonicForLine_ReturnsCorrectMnemonic()
    {
        var tokens = Tokenize("start: ldi r0, #0x05");
        var mnemonic = _locator.GetMnemonicForLine(tokens, 0);
        Assert.That(mnemonic, Is.EqualTo("ldi"));
    }

    [Test]
    public void GetMnemonicForLine_NoMnemonic_ReturnsNull()
    {
        var tokens = Tokenize(".data");
        var mnemonic = _locator.GetMnemonicForLine(tokens, 0);
        Assert.That(mnemonic, Is.Null);
    }
}
