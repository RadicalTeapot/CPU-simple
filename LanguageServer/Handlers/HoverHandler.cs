using Assembler.Lexeme;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace LanguageServer.Handlers;

public class HoverHandler : HoverHandlerBase
{
    private readonly DocumentStore _store;
    private readonly TokenLocator _locator;

    public HoverHandler(DocumentStore store, TokenLocator locator)
    {
        _store = store;
        _locator = locator;
    }

    public override Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
    {
        var doc = _store.GetDocument(request.TextDocument.Uri);
        if (doc?.Tokens is null)
            return Task.FromResult<Hover?>(null);

        var pos = request.Position;
        var found = _locator.FindTokenAt(doc.Tokens, pos.Line, pos.Character);
        if (found is null)
            return Task.FromResult<Hover?>(null);

        var (token, index) = found.Value;
        var content = GetHoverContent(doc, token, index);
        if (content is null)
            return Task.FromResult<Hover?>(null);

        return Task.FromResult<Hover?>(new Hover
        {
            Contents = new MarkedStringsOrMarkupContent(new MarkupContent
            {
                Kind = MarkupKind.Markdown,
                Value = content
            }),
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                new Position(token.Line, token.Column),
                new Position(token.Line, token.Column + token.Length))
        });
    }

    private string? GetHoverContent(DocumentState doc, Token token, int index)
    {
        switch (token.Type)
        {
            case TokenType.Identifier:
                return GetIdentifierHover(doc, token, index);
            case TokenType.Register:
                return GetRegisterHover(token);
            case TokenType.HexNumber:
                return GetHexHover(token);
            default:
                return null;
        }
    }

    private string? GetIdentifierHover(DocumentState doc, Token token, int index)
    {
        // Directive context (after dot)
        if (_locator.IsDirectiveContext(doc.Tokens!, index))
        {
            if (DirectiveDescriptions.Entries.TryGetValue(token.Lexeme, out var directive))
            {
                return $"**Directive: {directive.Syntax}**\n\n{directive.Description}";
            }
            return null;
        }

        // Instruction mnemonic
        if (_locator.IsInstructionMnemonic(doc.Tokens!, index))
        {
            if (InstructionDescriptions.Entries.TryGetValue(token.Lexeme, out var instr))
            {
                return $"**{instr.Syntax}**\n\n{instr.Description}";
            }
            return null;
        }

        // Label definition or reference — look up in symbol table
        if (doc.Symbols is not null)
        {
            var symbol = doc.Symbols.FirstOrDefault(s =>
                string.Equals(s.Name, token.Lexeme, StringComparison.OrdinalIgnoreCase));
            if (symbol is not null)
            {
                var kindStr = symbol.Kind.ToString().ToLower();
                return $"**{symbol.Name}** ({kindStr})\n\nAddress: 0x{symbol.Address:X2}";
            }
        }

        return null;
    }

    private static string GetRegisterHover(Token token)
    {
        return $"**Register {token.Lexeme}**\n\nGeneral purpose 8-bit register.";
    }

    private static string? GetHexHover(Token token)
    {
        if (int.TryParse(token.Lexeme.AsSpan(2), System.Globalization.NumberStyles.HexNumber, null, out var value))
        {
            return $"**{token.Lexeme}**\n\nDecimal: {value}";
        }
        return null;
    }

    protected override HoverRegistrationOptions CreateRegistrationOptions(
        HoverCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new HoverRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("csasm")
        };
    }
}
