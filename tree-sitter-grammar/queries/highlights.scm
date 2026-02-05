; Comments
(comment) @comment

; Registers
(register) @variable.builtin

; Directive names
(directive_name) @keyword.directive
(header_directive) @keyword.directive

; Mnemonics (instructions)
(mnemonic) @function

; Numbers
(hex_number) @number

; Strings
(string_literal) @string

; Labels
(label_definition
  (identifier) @label)

; Memory brackets
(memory_address
  "[" @punctuation.bracket
  "]" @punctuation.bracket)

; Operators in memory operands
(memory_operand
  "+" @operator)
(memory_operand
  "-" @operator)

; Immediate value prefix
(immediate_value
  "#" @punctuation.special)

; Identifiers (general - labels used as operands)
(operand
  (identifier) @variable)

; Memory operand identifiers (labels)
(memory_operand
  (identifier) @variable)
