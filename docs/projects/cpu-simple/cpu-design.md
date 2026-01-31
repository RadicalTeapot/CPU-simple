# CPU design

## Registers and ISA

Why 4 registers only?

> The reason why I choose to have only 4 GPR was to keep the number of bytes used by the ISA as low as possible (initially only one byte per instruction, but now up to two byte for the 8-bit version and three for the 16-bit version).
> 4 GPR means that I can encode instructions targeting two registers (arguably some of the most commonly used ones in a program) into a single byte, with the high nibble setting representing the opcode and the low nibble representing the two register numbers.

This lead to 16 possible opcodes when using encoding an instruction in a single byte. However the ISA has more than 16 op-codes, how did this evolution happended?

> I did start with a naive "high nibble = opcode" and hit a wall indeed. While I was pondering about how to expand while keeping to 2 byte at most for all instructions, I had a look at the 6502 microprocessor ISA (since this is quite a close real-world analog for my CPU) and it's addressing modes and that's when I got the idea that I can avoid "wasting" bits in my ISA and make it more dense by being less rigid about how instructions are grouped.
> I was not really motivated by needing new instructions as I (naively) thought I could emulate those in software, although I can now see why this would have been a wrong choice to make.

Instruction encoding density does matter at lot indeed. Since the 6502 was a source of inspiration, how where the boundary between native and emulated instructions drawn? Was it a specific use case that forced the evolution, or more of a theoretical realization?

> I think the biggest factor was program ROM space. In the current architecture (even if I were to use the 16bit addressing space variant of the CPU) space is at a premium and emulating instructions in software is really wasteful and would go against the very reason why I want to keep instruction size to be 2 bytes maximum.
> This is my first go at designing a CPU and an instruction set so in the end it's defintively more of a theoretical realization (i.e., spending time thinking about the problem and discovering implications of choices).

## 8-bit addressing space limitations

An 8-bit PC gives you 256 bytes of addressable program space. That's extremely tight—a few loops, some subroutines, and you're out.
Why was this a choice and why is there a 16-bit version? Also how is the max instruction size handled in the 8 vs 16-bit version?

> I started with a 8-bit PC to keep the addressing logic simple. Not having to deal with byte vs word depending on the context and no bit manipulation logic in the CPU emulator was a nice starting point for the project.
> However I did know from the start that 256 bytes for ROM (and even less if you factor in the stack space) is not really useful, hence why I have a 16-bit PC variant.
> I still keep the 8-bit option around for the challenge of trying to write useful software in such a contsrained environment though.
> Regarding the instruction size goal, I think I need to clarify indeed, 2 bytes max is for the 8-bit PC version and 3 bytes are used in the 16-bit version.
> I still want to explore bank switching too.