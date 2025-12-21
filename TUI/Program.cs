using Spectre.Console;

public static class Program
{
    public static void Main(string[] args)
    {
        var layout = new Layout("root")
            .SplitRows(
                new Layout("header").Ratio(1),
                new Layout("body").Ratio(8).SplitColumns(
                    new Layout("cpu").Ratio(6).SplitRows(
                        new Layout("info").Ratio(1).SplitColumns(
                            new Layout("state").Ratio(1),
                            new Layout("cycle").Ratio(1),
                            new Layout("PC").Ratio(1),
                            new Layout("SP").Ratio(1),
                            new Layout("flags").Ratio(2)
                            ),
                        new Layout("registers").Ratio(3),
                        new Layout("current-instruction").Ratio(1),
                        new Layout("instruction-logs").Ratio(3),
                        new Layout("output-and-stack").Ratio(6).SplitColumns(
                            new Layout("output").Ratio(5),
                            new Layout("stack").Ratio(1)
                        )
                    ),
                    new Layout("address").Ratio(1),
                    new Layout("data").Ratio(2)
                )
            );

        var headerPanelContent = new Layout("header-content")
            .SplitRows(
                new Layout("state").Ratio(5).MinimumSize(1),
                new Layout("instructions").Ratio(1).MinimumSize(1).Update(
                    new Markup("Step [blue]space[/] | Run [blue]enter[/] | Speed up [blue]+[/] | Slow down [blue]-[/] | Reset [blue]r[/] | Quit [blue]q[/]").Centered()
                )
            );
        layout["header"].Update(
            new Panel(headerPanelContent)
                .Header("CPU Emulator")
                .Border(BoxBorder.Rounded)
                .HeaderAlignment(Justify.Center)
                .Expand()
        );

        layout["body"]["cpu"]["info"]["state"].Update(
            new Panel(Align.Center(new Markup("[green]RUN[/]"), VerticalAlignment.Middle)).Header("State").Border(BoxBorder.Rounded).Expand()
        );

        layout["body"]["cpu"]["info"]["cycle"].Update(
            new Panel(Align.Center(new Text("0"), VerticalAlignment.Middle)).Header("Cycle").Border(BoxBorder.Rounded).Expand()
        );

        layout["body"]["cpu"]["info"]["PC"].Update(
            new Panel(Align.Center(new Text("0"), VerticalAlignment.Middle)).Header("PC").Border(BoxBorder.Rounded).Expand()
        );

        layout["body"]["cpu"]["info"]["SP"].Update(
            new Panel(Align.Center(new Text("0"), VerticalAlignment.Middle)).Header("SP").Border(BoxBorder.Rounded).Expand()
        );

        AnsiConsole.Live(layout)
            .AutoClear(false)
            .Start(ctx =>
            {
                while (true)
                {
                    headerPanelContent["state"].Update(new Markup("Running [green]Yes[/] | Tick rate: [green]10ms[/]").Centered());
                    ctx.Refresh();
                    Thread.Sleep(1000);
                }
            });
    }
}