using Assembler.Lexeme;
using CPU.opcodes;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace LanguageServer.Handlers;

public class CompletionHandler : CompletionHandlerBase
{
    private readonly DocumentStore _store;
    private readonly TokenLocator _locator;

    private static readonly string[] Registers = ["r0", "r1", "r2", "r3"];
    private static readonly string[] DirectiveNames = ["text", "data", "irq", "byte", "short", "zero", "org", "string"];

    private static readonly HashSet<string> NoOperandOpcodes =
        ["nop", "hlt", "clc", "sec", "clz", "sez", "sei", "cli", "ret", "rti"];
    private static readonly HashSet<string> SingleMemoryOpcodes =
        ["jmp", "jcc", "jcs", "jzc", "jzs", "cal"];
    private static readonly HashSet<string> SingleRegisterOpcodes =
        ["pop", "pek", "psh", "lsh", "rsh", "lrt", "rrt", "inc", "dec"];
    private static readonly HashSet<string> RegisterImmediateOpcodes =
        ["ldi", "adi", "sbi", "cpi", "ani", "ori", "xri", "bti"];
    private static readonly HashSet<string> RegisterMemoryOpcodes =
        ["lda", "sta", "ada", "sba", "cpa", "ana", "ora", "xra", "bta", "ldx", "stx"];
    private static readonly HashSet<string> TwoRegisterOpcodes =
        ["mov", "add", "sub", "cmp", "and", "or", "xor"];

    public CompletionHandler(DocumentStore store, TokenLocator locator)
    {
        _store = store;
        _locator = locator;
    }

    public override Task<CompletionItem> Handle(CompletionItem request, CancellationToken cancellationToken)
    {
        return Task.FromResult(request);
    }

    public override Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
    {
        var doc = _store.GetDocument(request.TextDocument.Uri);
        var items = new List<CompletionItem>();

        var line = request.Position.Line;
        var col = request.Position.Character;

        // Determine context from document text
        var lineText = GetLineText(doc?.Text, line);
        var textBeforeCursor = col <= lineText.Length ? lineText[..col] : lineText;
        var trimmed = textBeforeCursor.TrimStart();

        // After a dot → directive names
        if (trimmed.EndsWith('.') || (trimmed.Contains('.') && !trimmed.Contains(' ')))
        {
            items.AddRange(GetDirectiveCompletions());
            return Task.FromResult(new CompletionList(items));
        }

        // If line is empty or cursor is at the start → mnemonics + section directives
        if (string.IsNullOrWhiteSpace(trimmed) || !trimmed.Contains(' '))
        {
            items.AddRange(GetMnemonicCompletions());
            items.AddRange(GetSectionDirectiveCompletions());
            // Also offer labels if they look like they're typing a label definition
            if (doc?.Symbols is not null)
            {
                items.AddRange(GetLabelCompletions(doc));
            }
            return Task.FromResult(new CompletionList(items));
        }

        // We're in an operand position — figure out the mnemonic
        var mnemonic = doc?.Tokens is not null
            ? _locator.GetMnemonicForLine(doc.Tokens, line)
            : GetFirstWord(trimmed);

        if (mnemonic is not null)
        {
            items.AddRange(GetOperandCompletions(mnemonic, doc, textBeforeCursor));
        }

        return Task.FromResult(new CompletionList(items));
    }

    private static string GetLineText(string? fullText, int line)
    {
        if (fullText is null) return string.Empty;
        var lines = fullText.Split('\n');
        return line < lines.Length ? lines[line] : string.Empty;
    }

    private static string? GetFirstWord(string text)
    {
        var parts = text.TrimStart().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        // Skip past label: if present
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].EndsWith(':'))
                continue;
            return parts[i];
        }
        return null;
    }

    private static List<CompletionItem> GetDirectiveCompletions()
    {
        var items = new List<CompletionItem>();
        foreach (var name in DirectiveNames)
        {
            var desc = DirectiveDescriptions.Entries.TryGetValue(name, out var d) ? d.Description : "";
            items.Add(new CompletionItem
            {
                Label = name,
                Kind = CompletionItemKind.Keyword,
                Detail = desc,
                InsertText = name
            });
        }
        return items;
    }

    private static List<CompletionItem> GetMnemonicCompletions()
    {
        var items = new List<CompletionItem>();
        foreach (var code in Enum.GetValues<OpcodeBaseCode>())
        {
            var name = code.ToString().ToLower();
            var desc = InstructionDescriptions.Entries.TryGetValue(name, out var d) ? d.Description : "";
            items.Add(new CompletionItem
            {
                Label = name,
                Kind = CompletionItemKind.Keyword,
                Detail = desc,
                InsertText = name
            });
        }
        return items;
    }

    private static List<CompletionItem> GetSectionDirectiveCompletions()
    {
        return
        [
            new CompletionItem
            {
                Label = ".text",
                Kind = CompletionItemKind.Keyword,
                Detail = "Switch to text (code) section",
                InsertText = ".text"
            },
            new CompletionItem
            {
                Label = ".data",
                Kind = CompletionItemKind.Keyword,
                Detail = "Switch to data section",
                InsertText = ".data"
            },
            new CompletionItem
            {
                Label = ".irq",
                Kind = CompletionItemKind.Keyword,
                Detail = "Switch to IRQ handler section",
                InsertText = ".irq"
            }
        ];
    }

    private List<CompletionItem> GetOperandCompletions(string mnemonic, DocumentState? doc, string textBeforeCursor)
    {
        var items = new List<CompletionItem>();
        var lowerMnemonic = mnemonic.ToLower();

        if (NoOperandOpcodes.Contains(lowerMnemonic))
        {
            // No operands expected
            return items;
        }

        // Check if we're in a bracket context
        var inBracket = textBeforeCursor.Contains('[') && !textBeforeCursor.Contains(']');

        if (SingleMemoryOpcodes.Contains(lowerMnemonic))
        {
            // Labels only
            if (doc?.Symbols is not null)
                items.AddRange(GetLabelCompletions(doc));
        }
        else if (SingleRegisterOpcodes.Contains(lowerMnemonic))
        {
            items.AddRange(GetRegisterCompletions());
        }
        else if (RegisterImmediateOpcodes.Contains(lowerMnemonic))
        {
            // First operand: register, second operand: #immediate
            if (!HasComma(textBeforeCursor))
            {
                items.AddRange(GetRegisterCompletions());
            }
            // After comma, user types #value — not much to complete
        }
        else if (RegisterMemoryOpcodes.Contains(lowerMnemonic))
        {
            if (!HasComma(textBeforeCursor))
            {
                items.AddRange(GetRegisterCompletions());
            }
            else
            {
                // Labels for memory address, registers for indexed addressing
                if (doc?.Symbols is not null)
                    items.AddRange(GetLabelCompletions(doc));
                if (inBracket)
                    items.AddRange(GetRegisterCompletions());
            }
        }
        else if (TwoRegisterOpcodes.Contains(lowerMnemonic))
        {
            items.AddRange(GetRegisterCompletions());
        }

        return items;
    }

    private static bool HasComma(string text) => text.Contains(',');

    private static List<CompletionItem> GetRegisterCompletions()
    {
        return Registers.Select(r => new CompletionItem
        {
            Label = r,
            Kind = CompletionItemKind.Variable,
            Detail = "General purpose 8-bit register",
            InsertText = r
        }).ToList();
    }

    private static List<CompletionItem> GetLabelCompletions(DocumentState doc)
    {
        if (doc.Symbols is null) return [];
        return doc.Symbols.Select(s => new CompletionItem
        {
            Label = s.Name,
            Kind = s.Kind == Assembler.Analysis.SymbolKind.Function
                ? CompletionItemKind.Function
                : CompletionItemKind.Variable,
            Detail = $"{s.Kind} at 0x{s.Address:X2}",
            InsertText = s.Name
        }).ToList();
    }

    protected override CompletionRegistrationOptions CreateRegistrationOptions(
        CompletionCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new CompletionRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("csasm"),
            TriggerCharacters = new Container<string>("."),
            ResolveProvider = false
        };
    }
}
