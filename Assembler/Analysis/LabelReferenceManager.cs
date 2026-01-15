using Assembler.Analysis.EmitNode;
using Assembler.AST;
using System.Diagnostics;

namespace Assembler.Analysis
{
    internal class LabelReferenceManager
    {
        public void LocateLabel(LabelNode labelNode, Section section)
        {
            var emitNodeContainer = GetOrCreateEmitNodeContainer(labelNode.Label);

            try
            {
                emitNodeContainer.Locate(section, section.LocationCounter);
            }
            catch (InvalidOperationException ex)
            {
                throw new ParserException(ex.Message, labelNode.Span.Line, labelNode.Span.StartColumn);
            }
        }

        public LabelReferenceEmitNode CreateAndRegisterEmitNode(LabelReferenceNode labelReferenceNode, int offset = 0)
        {
            var labelRefEmitNode = new LabelReferenceEmitNode(labelReferenceNode, offset, labelReferenceNode.Span);
            GetOrCreateEmitNodeContainer(labelReferenceNode.Label).EmitNodes.Add(labelRefEmitNode);
            
            return labelRefEmitNode;
        }

        public void ResolveLabels()
        {
            foreach (var emitNodeContainer in _containers.Values)
            {
                emitNodeContainer.ResolveEmitNodes();
            }
        }

        public IList<Symbol> GetAllSymbols()
        {
            var symbols = new List<Symbol>();
            foreach (var emitNodeContainer in _containers.Values)
            {
                symbols.Add(emitNodeContainer.AsSymbol());
            }
            return symbols;
        }

        private EmitNodeContainer GetOrCreateEmitNodeContainer(string labelName)
        {
            if (!_containers.TryGetValue(labelName, out var emitNodeContainer))
            {
                emitNodeContainer = new EmitNodeContainer(labelName);
                _containers.Add(labelName, emitNodeContainer);
            }
            return emitNodeContainer;
        }

        private class EmitNodeContainer(string labelName)
        {
            public string LabelName { get; } = labelName;
            public List<LabelReferenceEmitNode> EmitNodes { get; } = [];

            public void Locate(Section section, int sectionLocationCounter)
            {
                if (_isLocated)
                {
                    throw new InvalidOperationException($"Label '{LabelName}' location counter has already been set.");
                }

                _locationCounter = sectionLocationCounter;
                _section = section;
                _isLocated = true;
            }

            public void ResolveEmitNodes()
            {
                if (EmitNodes.Count == 0)
                {
                    return; // No emit nodes to resolve
                }

                if (!_isLocated)
                {
                    var firstEmitNode = EmitNodes[0];
                    throw new ParserException($"Label '{LabelName}' location counter has not been set.",
                        firstEmitNode.LabelRefNode.Span.Line, firstEmitNode.LabelRefNode.Span.StartColumn); // Show location of first reference
                }

                foreach (var emitNode in EmitNodes)
                {
                    emitNode.Resolve(_locationCounter + _section?.StartAddress ?? 0);
                }
            }

            public Symbol AsSymbol()
            {
                if (!_isLocated)
                {
                    throw new InvalidOperationException($"Label '{LabelName}' location counter has not been set.");
                }

                Debug.Assert(_section != null, "Section should not be null when creating symbol for located label.");
                var symbolKind = _section.SectionType == Section.Type.Text ? SymbolKind.Function : SymbolKind.Variable;
                return new Symbol(LabelName, _locationCounter + _section.StartAddress, symbolKind);
            }

            private int _locationCounter;
            private Section? _section;
            private bool _isLocated = false;
        }

        private readonly Dictionary<string, EmitNodeContainer> _containers = [];
    }
}
