namespace LanguageServer;

public static class DirectiveDescriptions
{
    public static readonly Dictionary<string, (string Description, string Syntax)> Entries = new(StringComparer.OrdinalIgnoreCase)
    {
        ["text"] = ("Switch to the text (code) section. Instructions are only allowed in the text section.", ".text"),
        ["data"] = ("Switch to a new data section. Data directives (.byte, .short, .zero, .string) are only allowed in data sections.", ".data"),
        ["irq"] = ("Switch to the IRQ handler section. Code placed here runs when an interrupt is serviced. Fixed address near end of memory.", ".irq"),
        ["byte"] = ("Emit a single byte value.", ".byte value"),
        ["short"] = ("Emit a 16-bit (2-byte) value in little-endian order.", ".short value"),
        ["zero"] = ("Emit N zero bytes.", ".zero count"),
        ["org"] = ("Set the origin address (location counter) within the current section.", ".org address"),
        ["string"] = ("Emit a null-terminated ASCII string.", ".string \"text\""),
    };
}
