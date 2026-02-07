using System.Collections.Concurrent;
using Assembler;
using Assembler.Analysis;
using Assembler.Lexeme;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace LanguageServer;

public class DocumentStore
{
    private readonly ConcurrentDictionary<DocumentUri, DocumentState> _documents = new();
    private readonly DocumentAnalyser _analyser;

    public DocumentStore(DocumentAnalyser analyser)
    {
        _analyser = analyser;
    }

    public DocumentState UpdateDocument(DocumentUri uri, string text)
    {
        var result = _analyser.Analyse(text);
        var state = new DocumentState(text, result.Tokens, result.Program, result.Symbols, result.Diagnostics);
        _documents[uri] = state;
        return state;
    }

    public void RemoveDocument(DocumentUri uri)
    {
        _documents.TryRemove(uri, out _);
    }

    public DocumentState? GetDocument(DocumentUri uri)
    {
        _documents.TryGetValue(uri, out var state);
        return state;
    }
}

public record DocumentState(
    string Text,
    List<Token>? Tokens,
    Parser.ProgramNode? Program,
    IList<Symbol>? Symbols,
    List<Diagnostic> Diagnostics);
