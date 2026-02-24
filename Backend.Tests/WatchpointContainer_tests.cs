using CPU.microcode;

namespace Backend.Tests
{
    [TestFixture]
    internal class WatchpointContainer_tests
    {
        private WatchpointContainer _container = null!;

        [SetUp]
        public void SetUp()
        {
            _container = new WatchpointContainer();
        }

        [Test]
        public void Add_IncrementsCount()
        {
            var wp = new AddressWatchpoint(_container.NextId(), BusDirection.Write, 0x0C);
            _container.Add(wp);
            Assert.That(_container.Count, Is.EqualTo(1));
        }

        [Test]
        public void Remove_DecrementsCount()
        {
            var wp = new AddressWatchpoint(_container.NextId(), BusDirection.Write, 0x0C);
            _container.Add(wp);
            _container.Remove(wp.Id);
            Assert.That(_container.Count, Is.EqualTo(0));
        }

        [Test]
        public void Clear_RemovesAll()
        {
            _container.Add(new AddressWatchpoint(_container.NextId(), BusDirection.Write, 0x0C));
            _container.Add(new AddressWatchpoint(_container.NextId(), BusDirection.Read, 0x0D));
            _container.Clear();
            Assert.That(_container.Count, Is.EqualTo(0));
        }

        [Test]
        public void GetAll_ReturnsAllWatchpoints()
        {
            _container.Add(new AddressWatchpoint(_container.NextId(), BusDirection.Write, 0x0C));
            _container.Add(new PhaseWatchpoint(_container.NextId(), MicroPhase.MemoryWrite));
            Assert.That(_container.GetAll(), Has.Length.EqualTo(2));
        }

        [Test]
        public void NextId_NeverResets()
        {
            var id1 = _container.NextId();
            var id2 = _container.NextId();
            _container.Clear();
            var id3 = _container.NextId();
            Assert.Multiple(() =>
            {
                Assert.That(id2, Is.GreaterThan(id1));
                Assert.That(id3, Is.GreaterThan(id2));
            });
        }

        [Test]
        public void Check_OnWriteMatches_BusWriteToTargetAddress()
        {
            var wp = new AddressWatchpoint(_container.NextId(), BusDirection.Write, 0x0C);
            _container.Add(wp);
            var trace = MakeTrace(bus: new BusAccess(0x0C, 0x42, BusDirection.Write, BusType.Memory));
            var match = _container.Check([trace]);
            Assert.That(match, Is.Not.Null);
            Assert.That(match!.Id, Is.EqualTo(wp.Id));
        }

        [Test]
        public void Check_OnReadMatches_BusReadFromTargetAddress()
        {
            var wp = new AddressWatchpoint(_container.NextId(), BusDirection.Read, 0x0C);
            _container.Add(wp);
            var trace = MakeTrace(bus: new BusAccess(0x0C, 0x42, BusDirection.Read, BusType.Memory));
            var match = _container.Check([trace]);
            Assert.That(match, Is.Not.Null);
            Assert.That(match!.Id, Is.EqualTo(wp.Id));
        }

        [Test]
        public void Check_OnWrite_DoesNotMatchStackAccess()
        {
            var wp = new AddressWatchpoint(_container.NextId(), BusDirection.Write, 0x0C);
            _container.Add(wp);
            var trace = MakeTrace(bus: new BusAccess(0x0C, 0x42, BusDirection.Write, BusType.Stack));
            var match = _container.Check([trace]);
            Assert.That(match, Is.Null);
        }

        [Test]
        public void Check_OnWrite_DoesNotMatchDifferentAddress()
        {
            var wp = new AddressWatchpoint(_container.NextId(), BusDirection.Write, 0x0C);
            _container.Add(wp);
            var trace = MakeTrace(bus: new BusAccess(0x0D, 0x42, BusDirection.Write, BusType.Memory));
            var match = _container.Check([trace]);
            Assert.That(match, Is.Null);
        }

        [Test]
        public void Check_OnWrite_DoesNotMatchRead()
        {
            var wp = new AddressWatchpoint(_container.NextId(), BusDirection.Write, 0x0C);
            _container.Add(wp);
            var trace = MakeTrace(bus: new BusAccess(0x0C, 0x42, BusDirection.Read, BusType.Memory));
            var match = _container.Check([trace]);
            Assert.That(match, Is.Null);
        }

        [Test]
        public void Check_OnPhaseMatches_TargetNextPhase()
        {
            var wp = new PhaseWatchpoint(_container.NextId(), MicroPhase.MemoryWrite);
            _container.Add(wp);
            var trace = MakeTrace(nextPhase: MicroPhase.MemoryWrite);
            var match = _container.Check([trace]);
            Assert.That(match, Is.Not.Null);
            Assert.That(match!.Id, Is.EqualTo(wp.Id));
        }

        [Test]
        public void Check_OnPhase_DoesNotMatchDifferentPhase()
        {
            var wp = new PhaseWatchpoint(_container.NextId(), MicroPhase.MemoryWrite);
            _container.Add(wp);
            var trace = MakeTrace(nextPhase: MicroPhase.MemoryRead);
            var match = _container.Check([trace]);
            Assert.That(match, Is.Null);
        }

        [Test]
        public void Check_NoMatch_ReturnsNull()
        {
            _container.Add(new AddressWatchpoint(_container.NextId(), BusDirection.Write, 0x0C));
            var trace = MakeTrace(bus: null);
            var match = _container.Check([trace]);
            Assert.That(match, Is.Null);
        }

        [Test]
        public void Check_EmptyContainer_ReturnsNull()
        {
            var trace = MakeTrace(bus: new BusAccess(0x0C, 0x42, BusDirection.Write, BusType.Memory));
            var match = _container.Check([trace]);
            Assert.That(match, Is.Null);
        }

        private static TickTrace MakeTrace(
            BusAccess? bus = null,
            MicroPhase nextPhase = MicroPhase.Done)
        {
            return new TickTrace(
                TickNumber: 1,
                Type: bus != null ? TickType.Bus : TickType.Internal,
                NextPhase: nextPhase,
                PcBefore: 0, PcAfter: 0,
                SpBefore: 0, SpAfter: 0,
                Instruction: "NOP",
                RegisterChanges: [],
                ZeroFlagBefore: false, ZeroFlagAfter: false,
                CarryFlagBefore: false, CarryFlagAfter: false,
                Bus: bus
            );
        }
    }
}
