using Assembler;
using CPU;
using Spectre.Console;

namespace TUI
{
    public class ApplicationState
    {
        public event Action<CpuInspector>? CpuStateUpdate;
        public event Action<bool>? RunningStateChanged;
        public event Action<int>? TickRateChanged;

        public void UpdateCpuState(CpuInspector inspector)
        {
            CpuStateUpdate?.Invoke(inspector);
        }

        public bool IsRunning {
            get => _isRunning;
            set
            {
                _isRunning = value;
                RunningStateChanged?.Invoke(_isRunning);
            }
        }

        public int TickRate
        {
            get => _tickRate;
            set
            {
                _tickRate = Math.Max(value, MinTickRate);
                TickRateChanged?.Invoke(_tickRate);
            }
        }

        private bool _isRunning = false;
        private int _tickRate = 0;

        private const int MinTickRate = 100;
    }

    public class CpuRunner
    {
        public CpuRunner(string program, ApplicationState applicationState)
        {
            _cpu = new CPU.CPU(new Config())
            {
                ProgressInspector = new Progress<CpuInspector>(inspector => applicationState.UpdateCpuState(inspector))
            };
            _cpu.LoadProgram(AssembleProgram(program));
            _cpu.Reset();
        }

        public void Step() => _cpu.Step();
        public void Reset() => _cpu.Reset();

        //public void Run(int tickRateMs, CancellationToken cancellationToken)
        //{
        //    while (!cancellationToken.IsCancellationRequested)
        //    {
        //        Step();
        //        if (tickRateMs > 0)
        //        {
        //            Thread.Sleep(tickRateMs);
        //        }
        //    }
        //}

        static byte[] AssembleProgram(string program)
        {
            const int memorySize = 240;
            var tokens = new Lexer().Tokenize(program);
            var programNode = Parser.ParseProgram(tokens);
            var emitNodes = new Analyser(memorySize).Run(programNode);
            var outputBytes = new Emitter(memorySize).Emit(emitNodes);
            return outputBytes;
        }

        private readonly CPU.CPU _cpu;
    }

    public static class KeyHandler
    {
        public static void HandleKey(ConsoleKey key, ApplicationState applicationState, 
            out bool stepRequested, out bool resetRequested, out bool quitRequested)
        {
            stepRequested = false;
            resetRequested = false;
            quitRequested = false;

            if (key == ConsoleKey.Q)
            {
                quitRequested = true;
            }
            else if (key == ConsoleKey.Spacebar)
            {
                applicationState.IsRunning = false;
                stepRequested = true;
            }
            else if (key == ConsoleKey.Enter)
            {
                applicationState.IsRunning = !applicationState.IsRunning;
            }
            else if (key == ConsoleKey.Add || key == ConsoleKey.OemPlus)
            {
                applicationState.TickRate = applicationState.TickRate - 100;
            }
            else if (key == ConsoleKey.Subtract || key == ConsoleKey.OemMinus)
            {
                applicationState.TickRate = applicationState.TickRate + 100;
            }
            else if (key == ConsoleKey.R)
            {
                resetRequested = true;
            }
        }
    }

    public static class Program
    {
        public static void Main(string[] args)
        {
            var applicationState = new ApplicationState();

            var program = "nop";
            var cpuRunner = new CpuRunner(program, applicationState);

            var layout = CreateLayout(applicationState);

            applicationState.IsRunning = false;
            applicationState.TickRate = 100;

            AnsiConsole.Live(layout)
                .Start(ctx =>
                {
                    while (true)
                    {
                        if (Console.KeyAvailable)
                        {
                            var key = Console.ReadKey(true).Key;
                            KeyHandler.HandleKey(key, applicationState, out bool stepRequested, out bool resetRequested, out bool quitRequested);

                            if (quitRequested)
                            {
                                return;
                            }

                            if (stepRequested)
                            {
                                cpuRunner.Step();
                            }

                            if (resetRequested)
                            {
                                cpuRunner.Reset();
                            }
                        }

                        if (applicationState.IsRunning)
                        {
                            cpuRunner.Step();
                        }

                        ctx.Refresh();
                        Thread.Sleep(applicationState.TickRate); // TODO: Decouple from CPU speed (and run CPU in separate thread)
                    }
                });
        }

        private static Layout CreateLayout(ApplicationState applicationState)
        {
            var layout = new Layout("root")
                .SplitRows(
                    new Layout("header").Ratio(1),
                    new Layout("body").Ratio(8)
                );

            layout["body"]
                .SplitColumns(
                    new Layout("cpu").Ratio(6),
                    new Layout("address").Ratio(1),
                    new Layout("data").Ratio(2)
                );

            layout["cpu"]
                .SplitRows(
                    new Layout("info").Ratio(1),
                    new Layout("registers").Ratio(3),
                    new Layout("current-instruction").Ratio(1),
                    new Layout("instruction-logs").Ratio(3),
                    new Layout("output-and-stack").Ratio(6)
                );

            layout["output-and-stack"]
                .SplitColumns(
                    new Layout("output").Ratio(5),
                    new Layout("stack").Ratio(1)
                );

            CreateHeaderPanel(layout["header"], applicationState);
            CreateCpuInfoPanel(layout["info"], applicationState);

            return layout;
        }

        private static void CreateHeaderPanel(Layout layout, ApplicationState applicationState)
        {
            var headerPanel = new Layout("header-content")
                .SplitColumns(
                    new Layout("state").Ratio(2),
                    new Layout("instructions")
                );

            var stateTable = new Table()
                .HideHeaders()
                .Expand()
                .Border(TableBorder.None);

            stateTable.AddColumn("status-name", col => col.RightAligned());
            stateTable.AddColumn("status-value");
            stateTable.AddRow("Running", "[red]No[/]");
            stateTable.AddEmptyRow();
            stateTable.AddRow("Tick rate", "[green]0ms[/]");

            applicationState.RunningStateChanged += isRunning =>
            {
                stateTable.UpdateCell(0, 1, new Markup(isRunning ? "[green]Yes[/]" : "[red]No[/]"));
            };
            applicationState.TickRateChanged += tickRate =>
            {
                stateTable.UpdateCell(2, 1, new Markup($"[green]{tickRate}ms[/]"));
            };

            headerPanel["state"].Update(stateTable);

            var instructionsTable = new Table()
                .HideHeaders()
                .Border(TableBorder.None);

            instructionsTable.AddColumn("names-1");
            instructionsTable.AddColumn("values-1");
            instructionsTable.AddColumn("names-2");
            instructionsTable.AddColumn("values-2");
            instructionsTable.AddRow("Step", "[blue]space[/]", "Slow down", "[blue]-[/]");
            instructionsTable.AddRow("Run", "[blue]enter[/]", "Reset", "[blue]r[/]");
            instructionsTable.AddRow("Speed up", "[blue]+[/]", "Quit", "[blue]q[/]");

            headerPanel["instructions"].Update(instructionsTable);

            layout.Update(
                new Panel(headerPanel)
                    .Header("CPU Emulator")
                    .Border(BoxBorder.Rounded)
                    .HeaderAlignment(Justify.Center)
                    .Expand()
            );
        }

        private static void CreateCpuInfoPanel(Layout layout, ApplicationState applicationState)
        {
            var panelLayout = new Layout()
                .SplitColumns(
                    new Layout("state").Ratio(1),
                    new Layout("cycle").Ratio(1),
                    new Layout("PC").Ratio(1),
                    new Layout("SP").Ratio(1),
                    new Layout("flags").Ratio(2)
                );

            var stateTable = new Table()
                .HideHeaders()
                .Expand()
                .Border(TableBorder.None)
                .AddColumn("state")
                .AddRow("[red]Stopped[/]");
            applicationState.RunningStateChanged += isRunning =>
            {
                var stateText = isRunning ? "[green]Running[/]" : "[red]Stopped[/]";
                stateTable.UpdateCell(0, 0, new Markup(stateText).Centered());
            };
            var statePanel = new Panel(stateTable)
                .Header("State")
                .Border(BoxBorder.Rounded)
                .HeaderAlignment(Justify.Left)
                .Expand();
            panelLayout["state"].Update(statePanel);

            var cycleTable = new Table()
                .HideHeaders()
                .Expand()
                .Border(TableBorder.None)
                .AddColumn("cycle")
                .AddRow("0x00 (0)");
            applicationState.CpuStateUpdate += inspector =>
            {
                cycleTable.UpdateCell(0, 0, $"0x{inspector.Cycle:X2} ({inspector.Cycle})");
            };
            var cyclePanel = new Panel(cycleTable)
                .Header("Cycle")
                .Border(BoxBorder.Rounded)
                .HeaderAlignment(Justify.Left)
                .Expand();
            panelLayout["cycle"].Update(cyclePanel);

            var pcTable = new Table()
                .HideHeaders()
                .Expand()
                .Border(TableBorder.None)
                .AddColumn("PC")
                .AddRow("0x00 (0)");
            applicationState.CpuStateUpdate += inspector =>
            {
                pcTable.UpdateCell(0, 0, $"0x{inspector.PC:X2} ({inspector.PC})");
            };
            var pcPanel = new Panel(pcTable)
                .Header("PC")
                .Border(BoxBorder.Rounded)
                .HeaderAlignment(Justify.Left)
                .Expand();
            panelLayout["PC"].Update(pcPanel);

            var spTable = new Table()
                .HideHeaders()
                .Expand()
                .Border(TableBorder.None)
                .AddColumn("SP")
                .AddRow("0x00 (0)");
            applicationState.CpuStateUpdate += inspector =>
            {
                spTable.UpdateCell(0, 0, $"0x{inspector.SP:X2} ({inspector.SP})");
            };
            var spPanel = new Panel(spTable)
                .Header("SP")
                .Border(BoxBorder.Rounded)
                .HeaderAlignment(Justify.Left)
                .Expand();
            panelLayout["SP"].Update(spPanel);

            var flagsTable = new Table()
                .HideHeaders()
                .Expand()
                .Border(TableBorder.None)
                .AddColumn("Z")
                .AddColumn("C")
                .AddRow("Z(0)", "C(0)");
            applicationState.CpuStateUpdate += inspector =>
            {
                flagsTable.UpdateCell(0, 0, inspector.ZeroFlag ? "Z(1)" : "Z(0)");
                flagsTable.UpdateCell(0, 1, inspector.CarryFlag ? "C(1)" : "C(0)");
            };
            var flagsPanel = new Panel(flagsTable)
                .Header("Flags")
                .Border(BoxBorder.Rounded)
                .HeaderAlignment(Justify.Left)
                .Expand();
            panelLayout["flags"].Update(flagsPanel);

            layout.Update(panelLayout);
        }
    }
}