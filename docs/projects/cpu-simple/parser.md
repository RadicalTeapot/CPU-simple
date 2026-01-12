# Parser

Following the lexer, the parser converts a stream of tokens into an abstract syntax tree (AST).

This is the step where the syntax of the program is validated, i.e. what was written. 
The meaning of the program content and whether it is allowed is left for the analyser.

## Parsing process

The parsing process turns the stream of token into a tree, where nodes are related to one another.
At the root of the tree is the program node, which a list contains statement nodes.
The content of those statement nodes is defined in the language EBNF syntax.

This is achieved by a line-oriented (since the code is broken into lines) recursive-descent.
This means that each branch from the tree is built from the root to the leaf(ves) before parsing the next branch, i.e., parse a whole statement (a line) by parsing its contents (directives, instructions, labels, ...) and then move on to the next.
And for each content of a statement, parse its contents (identifiers, memory addresses, immediate numbers, ...), all the way down to the leaves.

## AST nodes

Most nodes are small and simple, containing a (the) lexer token value as it's value (still as a string, meaning comes later) along with its size in tokens and the location in the source code for debugging purposes.
As noted above all nodes but the leaf nodes contains references to their sub-nodes, creating them when during the parsing process.