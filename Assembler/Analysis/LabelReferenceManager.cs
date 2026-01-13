using Assembler.Analysis.EmitNode;
using Assembler.AST;

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
            var labelRefEmitNode = new LabelReferenceEmitNode(labelReferenceNode, offset);
            GetOrCreateEmitNodeContainer(labelReferenceNode.Label).EmitNodes.Add(labelRefEmitNode);
            
            return labelRefEmitNode;
        }

        public void ResolveLabels()
        {
            foreach (var emitNodeContainer in containers.Values)
            {
                emitNodeContainer.ResolveEmitNodes();
            }
        }

        private EmitNodeContainer GetOrCreateEmitNodeContainer(string labelName)
        {
            if (!containers.TryGetValue(labelName, out var emitNodeContainer))
            {
                emitNodeContainer = new EmitNodeContainer(labelName);
                containers.Add(labelName, emitNodeContainer);
            }
            return emitNodeContainer;
        }

        private class EmitNodeContainer(string labelName)
        {
            public string LabelName { get; } = labelName;
            public List<LabelReferenceEmitNode> EmitNodes { get; } = [];

            public void Locate(Section section, int sectionLocationCounter)
            {
                if (isLocated)
                {
                    throw new InvalidOperationException($"Label '{LabelName}' location counter has already been set.");
                }

                locationCounter = sectionLocationCounter;
                this.section = section;
                isLocated = true;
            }

            public void ResolveEmitNodes()
            {
                if (EmitNodes.Count == 0)
                {
                    return; // No emit nodes to resolve
                }

                if (!isLocated)
                {
                    var firstEmitNode = EmitNodes[0];
                    throw new ParserException($"Label '{LabelName}' location counter has not been set.",
                        firstEmitNode.LabelRefNode.Span.Line, firstEmitNode.LabelRefNode.Span.StartColumn); // Show location of first reference
                }

                foreach (var emitNode in EmitNodes)
                {
                    emitNode.Resolve(locationCounter + section?.StartAddress ?? 0);
                }
            }

            private int locationCounter;
            private Section? section;
            private bool isLocated = false;
        }

        private readonly Dictionary<string, EmitNodeContainer> containers = [];
    }
}
