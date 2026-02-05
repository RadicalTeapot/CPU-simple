/// <reference types="tree-sitter-cli/dsl" />
// @ts-check

module.exports = grammar({
  name: "cpu_simple_asm",

  // Whitespace (but not newlines) is handled automatically
  extras: ($) => [/[ \t]+/],

  rules: {
    // Root: sequence of lines separated by newlines
    source_file: ($) => repeat($.line),

    // A line is either a statement or empty, followed by newline
    line: ($) => seq(optional($.statement), /\r?\n/),

    // Statement must have at least one component
    // Possible combinations: header?, label?, (directive|instruction)?, comment?
    // At least one must be present
    statement: ($) =>
      choice(
        // Has header directive
        seq(
          $.header_directive,
          optional($.label_definition),
          optional(choice($.directive, $.instruction)),
          optional($.comment)
        ),
        // No header, has label
        seq(
          $.label_definition,
          optional(choice($.directive, $.instruction)),
          optional($.comment)
        ),
        // No header, no label, has directive/instruction
        seq(choice($.directive, $.instruction), optional($.comment)),
        // Comment only
        $.comment
      ),

    // Section directives: .text, .data
    header_directive: ($) =>
      seq(".", choice(/[tT][eE][xX][tT]/, /[dD][aA][tT][aA]/)),

    // Data directives with operands
    directive: ($) =>
      seq(
        $.directive_name,
        optional(
          choice(
            seq($.immediate_value, optional(seq(",", $.immediate_value))),
            $.string_literal
          )
        )
      ),

    directive_name: ($) =>
      seq(
        ".",
        choice(
          /[bB][yY][tT][eE]/,
          /[sS][hH][oO][rR][tT]/,
          /[zZ][eE][rR][oO]/,
          /[oO][rR][gG]/,
          /[sS][tT][rR][iI][nN][gG]/
        )
      ),

    // Label definition: identifier followed by colon
    label_definition: ($) => seq($.identifier, ":"),

    // Instruction: mnemonic with optional operands
    instruction: ($) =>
      seq($.mnemonic, optional(seq($.operand, optional(seq(",", $.operand))))),

    mnemonic: ($) => $.identifier,

    operand: ($) =>
      choice($.register, $.immediate_value, $.identifier, $.memory_address),

    // Immediate value: # followed by hex number
    immediate_value: ($) => seq("#", $.hex_number),

    // Memory address: [ memory_operand ]
    memory_address: ($) => seq("[", $.memory_operand, "]"),

    // Memory operand variants:
    // - #0xNN (direct hex)
    // - label (identifier)
    // - label+/-#0xNN (identifier with offset)
    // - rN (register)
    // - rN+#0xNN (register with offset)
    memory_operand: ($) =>
      choice(
        seq($.identifier, choice("+", "-"), $.immediate_value),
        seq($.register, "+", $.immediate_value),
        $.immediate_value,
        $.identifier,
        $.register
      ),

    // Register: r followed by digits (case insensitive), priority 2 to match before identifier
    register: ($) => prec(2, /[rR][0-9]+/),

    // Identifier: starts with letter or underscore, followed by alphanumerics or underscores
    identifier: ($) => /[a-zA-Z_][a-zA-Z0-9_]*/,

    // Hex number: 0x followed by hex digits
    hex_number: ($) => /0[xX][0-9a-fA-F]+/,

    // String literal: quoted string with escape support for \" and \\
    string_literal: ($) => /"([^"\\]|\\"|\\\\)*"/,

    // Comment: ; followed by anything to end of line
    comment: ($) => /;[^\r\n]*/,
  },
});
