using Emulator.Commands.StateCommands;
using Emulator.IO;
using NUnit.Framework.Internal;
using PPU.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Emulator.Tests
{
    [TestFixture]
    internal class Integration_tests
    {
        [Test]
        [Ignore("This test is meant to be run manually, as it will open a window and display the output of the example program.")]
        public void Run_8bitGraphicalExample_WithPpu()
        {
            // This test is meant to be run manually, as it will open a window and display the output of the example program.
            // This assumes that the example program is compiled and available at the specified path.
            var progData = File.ReadAllBytes("../../../../data/cpu_programs/bin/8-bit-graphics-example.bin");
            var chrData = File.ReadAllBytes("../../../../data/ppu_chrrom/default.rom");
            var ppuConfig = PpuConfig.Minimal8Bit;
            var context = new EmulatorApplication.EmulatorContext(
                new ConsoleLogger(), new ConsoleInput(), new ConsoleOutput(),
                new CPU.Config(), ppuConfig, 4,
                chrData, progData
            );
            var application = new EmulatorApplication(context);
            application.Run();
            Assert.Pass("Emulator ran successfully. Please verify the output visually.");
        }
    }
}
