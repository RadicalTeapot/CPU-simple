# Lexer

This is the first part of the assembler. It's responsibility is to clean and split the provided source code string into lexically valid tokens.

It outputs a stream of tokens.

## Source code cleanup

At this stage, the source code is split into lines. For each line comments and beginning / ending whitespace are trimmed. Empty lines are then removed (the original line number is kept for debugging purposes) and finally cast to lowercase.

## Tokens

Those define "units" of the language:

- Single char elements, e.g., hash, comma, dot, ...
- Multi char elements, e.g., hex number, identifier, ...

Each token is a separate lexical entity and has an intrinsic meaning. For example, a immediate hex value written `#0x01`, is split in two tokens:

- The hash symbol, meaning that this is an immediate value
- The hex number `0x01`

In the case of the number, the prefix `0x` is an integral part of the number itself and doesn't have meaning by itself, so it part of the number rather than a separate token.