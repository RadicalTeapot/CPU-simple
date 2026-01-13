using Spectre.Console;

namespace TUI
{
    public class HeaderPanel
    {
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                _table.UpdateCell(0, 1, new Markup(_isRunning ? "[green]Yes[/]" : "[red]No[/]"));
            }
        }

        public int TickRateMs
        {
            get => _tickRateMs;
            set
            {
                _tickRateMs = value;
                _table.UpdateCell(2, 1, new Markup($"[green]{_tickRateMs} ms[/]"));
            }
        }

        public HeaderPanel(Layout layout)
        {
            _table = new Table()
                .HideHeaders()
                .Expand()
                .Border(TableBorder.None);

            _table.AddColumn("status-name", col => col.RightAligned());
            _table.AddColumn("status-value");
            _table.AddRow("Running", "[red]No[/]");
            _table.AddEmptyRow();
            _table.AddRow("Tick rate", "[green]0ms[/]");

            layout.Update(_table);
        }

        private readonly Table _table;
        private bool _isRunning = false;
        private int _tickRateMs = 0;
    }

    public static class Program
    {
        public static void Main(string[] args)
        {
            var layout = CreateLayout();
            var headerPanel = CreateHeaderPanel(layout);
            headerPanel.IsRunning = false;
            headerPanel.TickRateMs = 0;

            AnsiConsole.Live(layout)
                .Start(ctx =>
                {
                    while (true)
                    {
                        if (Console.KeyAvailable)
                        {
                            var key = Console.ReadKey(true).Key;
                            if (key == ConsoleKey.Q)
                            {
                                break;
                            }
                            else if (key == ConsoleKey.Spacebar)
                            {
                                headerPanel.IsRunning = false;
                            }
                            else if (key == ConsoleKey.Enter)
                            {
                                headerPanel.IsRunning = !headerPanel.IsRunning;
                            }
                            else if (key == ConsoleKey.Add || key == ConsoleKey.OemPlus)
                            {
                                headerPanel.TickRateMs = Math.Max(100, headerPanel.TickRateMs - 100);
                            }
                            else if (key == ConsoleKey.Subtract || key == ConsoleKey.OemMinus)
                            {
                                headerPanel.TickRateMs += 100;
                            }
                            else if (key == ConsoleKey.R)
                            {
                                headerPanel.IsRunning = false;
                                headerPanel.TickRateMs = 0;
                            }
                        }

                        ctx.Refresh();
                        Thread.Sleep(100);
                    }
                });
        }

        private static Layout CreateLayout()
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

            layout["info"]
                .SplitColumns(
                    new Layout("state").Ratio(1),
                    new Layout("cycle").Ratio(1),
                    new Layout("PC").Ratio(1),
                    new Layout("SP").Ratio(1),
                    new Layout("flags").Ratio(2)
                );

            layout["output-and-stack"]
                .SplitColumns(
                    new Layout("output").Ratio(5),
                    new Layout("stack").Ratio(1)
                );

            return layout;
        }

        private static HeaderPanel CreateHeaderPanel(Layout layout)
        {
            var headerPanel = new Layout("header-content")
                .SplitColumns(
                    new Layout("state").Ratio(2),
                    new Layout("instructions")
                );

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

            layout["header"].Update(
                new Panel(headerPanel)
                    .Header("CPU Emulator")
                    .Border(BoxBorder.Rounded)
                    .HeaderAlignment(Justify.Center)
                    .Expand()
            );

            return new HeaderPanel(headerPanel["state"]);
        }
    }
}