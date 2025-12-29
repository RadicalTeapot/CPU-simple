# Parser and tokenizer

## Notes

As per the language specifications, implementation should be:
- a clean tokeniser: `. # [ ] , + - : ;` identifiers, strings
- a simple parser with almost no lookahead
- semantic validation that’s easy to phase:
    - collect labels + section structure
    - resolve addresses + emit bytes
    - enforce reserved stack region #F0–#FF