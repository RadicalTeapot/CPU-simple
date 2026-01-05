using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Assembler
{
    public class AnalyserException : Exception
    {
        public int Line { get; }
        public int Column { get; }
        public AnalyserException(string message, int line, int column)
            : base($"{message} at line {line}, column {column}")
        {
            Line = line;
            Column = column;
        }
    }

    internal class Section
    {
        public int LocationCounter { get; set; } = 0;
        public Dictionary<string, int> Labels { get; } = [];
    }

    internal class Analyser
    {
        private Section TextSection;
        private Section DataSection;
        private Section currentSection;
        private int textSectionCounter;
        private int dataSectionCounter;

        public Analyser()
        {
            TextSection = new Section();
            DataSection = new Section();
            currentSection = TextSection;
            textSectionCounter = 0;
            dataSectionCounter = 0;
        }

        public void Run(ProgramNode program)
        {
            TextSection = new Section();
            DataSection = new Section();
            currentSection = TextSection;
            textSectionCounter = 0;
            dataSectionCounter = 0;

            foreach (var statement in program.Statements)
            {
                AnalyseStatement(statement);
            }
        }

        private void AnalyseStatement(StatementNode statement)
        {
            AnalyseHeaderDirective(statement);

            if (statement.Label != null)
            {
                currentSection.Labels[statement.Label.Label] = currentSection.LocationCounter;
            }

            AnalysePostDirective(statement);

            if (statement.Instruction != null)
            {
                if (currentSection == DataSection)
                {
                    throw new AnalyserException("Instructions are not allowed in the data section", statement.Instruction.Span.Line, statement.Instruction.Span.Start);
                }
                // Handle instructions
            }

            currentSection.LocationCounter++;
        }
        private void AnalyseHeaderDirective(StatementNode statement)
        {
            if (statement.HeaderDirective != null)
            {
                switch (statement.HeaderDirective.Directive)
                {
                    case "data":
                        if (dataSectionCounter > 0)
                        {
                            throw new AnalyserException("Multiple data section directives are not allowed",
                                statement.HeaderDirective.Span.Line, statement.HeaderDirective.Span.Start);
                        }
                        currentSection = DataSection;
                        dataSectionCounter++;
                        break;
                    case "text":
                        if (textSectionCounter > 0)
                        {
                            throw new AnalyserException("Multiple text section directives are not allowed",
                                statement.HeaderDirective.Span.Line, statement.HeaderDirective.Span.Start);
                        }
                        currentSection = TextSection;
                        textSectionCounter++;
                        break;
                    case "org":
                        break;
                    default:
                        throw new AnalyserException($"Invalid header directive: {statement.HeaderDirective.Directive}",
                                statement.HeaderDirective.Span.Line, statement.HeaderDirective.Span.Start);
                }
            }
        }
        
        private void AnalysePostDirective(StatementNode statement)
        {
            if (statement.PostDirective != null)
            {
                switch (statement.PostDirective.Directive)
                {
                    case "byte":
                        if (currentSection == TextSection)
                        {
                            throw new AnalyserException("'byte' directive is not allowed in the text section",
                                statement.PostDirective.Span.Line, statement.PostDirective.Span.Start);
                        }
                        // Handle byte directive
                        break;
                    case "short":
                        if (currentSection == TextSection)
                        {
                            throw new AnalyserException("'short' directive is not allowed in the text section",
                                statement.PostDirective.Span.Line, statement.PostDirective.Span.Start);
                        }
                        // Handle short directive
                        break;
                    case "zero":
                        if (currentSection == TextSection)
                        {
                            throw new AnalyserException("'zero' directive is not allowed in the text section",
                                statement.PostDirective.Span.Line, statement.PostDirective.Span.Start);
                        }
                        // Handle zero directive
                        break;
                    case "string":
                        if (currentSection == TextSection)
                        {
                            throw new AnalyserException("'string' directive is not allowed in the text section",
                                statement.PostDirective.Span.Line, statement.PostDirective.Span.Start);
                        }
                        // Handle string directive
                        break;
                    case "data":
                    case "text":
                    default:
                        throw new AnalyserException($"Invalid post-directive: {statement.PostDirective.Directive}",
                           statement.PostDirective.Span.Line, statement.PostDirective.Span.Start);
                }
            }
        }
    }
}
