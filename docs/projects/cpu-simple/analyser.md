# Analyser

The analyser is responsible for converting the AST into meaningful analysed nodes, that can be validated and used to emit the machine code.

This is where the labels are solved and code is analysed for correctness (e.g., if the operands of an instruction have the correct count and type). 
This is done in multiple passes while keeping track of location counters and program sections.

