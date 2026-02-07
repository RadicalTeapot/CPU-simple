namespace LanguageServer.Tests;

public class DocumentAnalyserTests
{
    private DocumentAnalyser _analyser = null!;

    [SetUp]
    public void Setup()
    {
        _analyser = new DocumentAnalyser();
    }

    [Test]
    public void Analyse_ValidSource_NoDiagnostics()
    {
        var source = ".text\nnop\nhlt\n";
        var result = _analyser.Analyse(source);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Tokens, Is.Not.Null);
        Assert.That(result.Program, Is.Not.Null);
        Assert.That(result.Symbols, Is.Not.Null);
    }

    [Test]
    public void Analyse_ValidSourceWithLabel_HasSymbols()
    {
        var source = ".text\nstart: nop\njmp [start]\nhlt\n";
        var result = _analyser.Analyse(source);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Symbols, Is.Not.Null);
        Assert.That(result.Symbols!.Count, Is.GreaterThan(0));
        Assert.That(result.Symbols!.Any(s => s.Name == "start"), Is.True);
    }

    [Test]
    public void Analyse_LexerError_SingleDiagnostic_TokensNull()
    {
        var source = ".text\n$$$invalid\n";
        var result = _analyser.Analyse(source);

        Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(result.Tokens, Is.Null);
        Assert.That(result.Program, Is.Null);
        Assert.That(result.Symbols, Is.Null);
    }

    [Test]
    public void Analyse_ParserError_DiagnosticsPresent_TokensAvailable_AstNull()
    {
        // Two commas in a row is a parser error
        var source = ".text\nldi r0, ,\n";
        var result = _analyser.Analyse(source);

        Assert.That(result.Diagnostics, Has.Count.GreaterThan(0));
        Assert.That(result.Tokens, Is.Not.Null);
        Assert.That(result.Program, Is.Null);
        Assert.That(result.Symbols, Is.Null);
    }

    [Test]
    public void Analyse_AnalyserError_DiagnosticsPresent_TokensAndAstAvailable_SymbolsNull()
    {
        // Referencing an undefined label is an analyser error
        var source = ".text\njmp [nonexistent]\nhlt\n";
        var result = _analyser.Analyse(source);

        Assert.That(result.Diagnostics, Has.Count.GreaterThan(0));
        Assert.That(result.Tokens, Is.Not.Null);
        Assert.That(result.Program, Is.Not.Null);
        Assert.That(result.Symbols, Is.Null);
    }

    [Test]
    public void Analyse_DiagnosticMessage_DoesNotContainLocationSuffix()
    {
        var source = ".text\n$$$invalid\n";
        var result = _analyser.Analyse(source);

        Assert.That(result.Diagnostics, Has.Count.GreaterThan(0));
        var message = result.Diagnostics[0].Message;
        Assert.That(message, Does.Not.Contain("at line"));
        Assert.That(message, Does.Not.Contain("column"));
    }

    [Test]
    public void Analyse_EmptySource_NoDiagnostics()
    {
        var source = "";
        var result = _analyser.Analyse(source);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Tokens, Is.Not.Null);
    }

    [Test]
    public void Analyse_LeadingWhitespace_TokenColumnsAdjusted()
    {
        // "   nop" — 3 spaces before nop. The Lexer trims the line, so without
        // adjustment the token column would be 0. After adjustment it should be 3.
        var source = ".text\n   nop\nhlt\n";
        var result = _analyser.Analyse(source);

        Assert.That(result.Diagnostics, Is.Empty);
        var nopToken = result.Tokens!.First(t => t.Lexeme == "nop");
        Assert.That(nopToken.Column, Is.EqualTo(3));
    }

    [Test]
    public void Analyse_LeadingWhitespace_MultipleTokensOnLine()
    {
        // "    ldi r0, #0x05" — 4 spaces of indent
        var source = ".text\n    ldi r0, #0x05\nhlt\n";
        var result = _analyser.Analyse(source);

        Assert.That(result.Diagnostics, Is.Empty);
        var ldiToken = result.Tokens!.First(t => t.Lexeme == "ldi");
        var r0Token = result.Tokens!.First(t => t.Lexeme == "r0");
        Assert.That(ldiToken.Column, Is.EqualTo(4));
        Assert.That(r0Token.Column, Is.EqualTo(8));
    }
}
