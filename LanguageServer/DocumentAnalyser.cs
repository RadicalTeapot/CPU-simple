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
            diagnostics.Add(ToDiagnostic(ex.Message, ex.Line, ex.Column, 1));
            return new AnalysisResult(null, null, null, diagnostics);
        }

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

    [GeneratedRegex(@"\s+at line \d+, column \d+$")]
    private static partial Regex LocationSuffixRegex();
}

public record AnalysisResult(
    List<Token>? Tokens,
    Parser.ProgramNode? Program,
    IList<Symbol>? Symbols,
    List<Diagnostic> Diagnostics);
