using System.Text.RegularExpressions;
using Assembler;
using Assembler.Analysis;
using Assembler.Lexeme;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using DiagnosticSeverity = OmniSharp.Extensions.LanguageServer.Protocol.Models.DiagnosticSeverity;

namespace LanguageServer;

public partial class DocumentAnalyser
{
    public AnalysisResult Analyse(string text)
    {
        var tokens = default(List<Token>?);
        var programNode = default(Parser.ProgramNode?);
        var symbols = default(IList<Symbol>?);
        var diagnostics = new List<Diagnostic>();

        // Stage 1: Lexer
        try
        {
            tokens = new Lexer().Tokenize(text);
        }
        catch (LexerException ex)
        {
            var offset = GetLeadingWhitespaceCount(text, ex.Line);
            diagnostics.Add(ToDiagnostic(ex.Message, ex.Line, ex.Column + offset, 1));
            return new AnalysisResult(null, null, null, diagnostics);
        }

        // The Lexer trims leading whitespace from lines before tokenizing, so token
        // columns are relative to the trimmed line. Adjust them to match the original
        // source positions so that LSP features (hover, completion, diagnostics) align
        // with the editor's cursor positions.
        var lineOffsets = ComputeLineOffsets(text);
        tokens = AdjustTokenColumns(tokens, lineOffsets);

        // Stage 2: Parser
        try
        {
            programNode = Parser.ParseProgram(tokens);
        }
        catch (AggregateException ex)
        {
            foreach (var inner in ex.InnerExceptions)
            {
                if (inner is ParserException pe)
                {
                    diagnostics.Add(ToDiagnostic(pe.Message, pe.Line, pe.Column, 1));
                }
            }
            // tokens still available, but AST is null
            return new AnalysisResult(tokens, null, null, diagnostics);
        }

        // Stage 3: Analyser
        try
        {
            var analyser = new Analyser();
            analyser.Run(programNode);
            symbols = analyser.GetSymbols();
        }
        catch (AggregateException ex)
        {
            foreach (var inner in ex.InnerExceptions)
            {
                if (inner is AnalyserException ae)
                {
                    diagnostics.Add(ToDiagnostic(ae.Message, ae.Line, ae.Column, 1));
                }
            }
            // tokens + AST available, symbols null
            return new AnalysisResult(tokens, programNode, null, diagnostics);
        }
        catch (ParserException ex)
        {
            // ResolveLabels() can throw ParserException for unresolved labels
            diagnostics.Add(ToDiagnostic(ex.Message, ex.Line, ex.Column, 1));
            return new AnalysisResult(tokens, programNode, null, diagnostics);
        }

        return new AnalysisResult(tokens, programNode, symbols, diagnostics);
    }

    private static Diagnostic ToDiagnostic(string message, int line, int column, int length)
    {
        // Strip the " at line N, column M" suffix that the assembler bakes into messages
        var cleanMessage = LocationSuffixRegex().Replace(message, "");

        return new Diagnostic
        {
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                new Position(line, column),
                new Position(line, column + length)),
            Severity = DiagnosticSeverity.Error,
            Source = "csasm",
            Message = cleanMessage
        };
    }

    private static int[] ComputeLineOffsets(string text)
    {
        var lines = text.Split('\n');
        var offsets = new int[lines.Length];
        for (var i = 0; i < lines.Length; i++)
        {
            var count = 0;
            while (count < lines[i].Length && lines[i][count] is ' ' or '\t')
                count++;
            offsets[i] = count;
        }
        return offsets;
    }

    private static int GetLeadingWhitespaceCount(string text, int line)
    {
        var lines = text.Split('\n');
        if (line < 0 || line >= lines.Length)
            return 0;
        var count = 0;
        while (count < lines[line].Length && lines[line][count] is ' ' or '\t')
            count++;
        return count;
    }

    private static List<Token> AdjustTokenColumns(List<Token> tokens, int[] lineOffsets)
    {
        var adjusted = new List<Token>(tokens.Count);
        foreach (var token in tokens)
        {
            var offset = token.Line >= 0 && token.Line < lineOffsets.Length ? lineOffsets[token.Line] : 0;
            adjusted.Add(new Token(token.Type, token.Lexeme, token.Line, token.Column + offset));
        }
        return adjusted;
    }

    [GeneratedRegex(@"\s+at line \d+, column \d+$")]
    private static partial Regex LocationSuffixRegex();
}

public record AnalysisResult(
    List<Token>? Tokens,
    Parser.ProgramNode? Program,
    IList<Symbol>? Symbols,
    List<Diagnostic> Diagnostics);
