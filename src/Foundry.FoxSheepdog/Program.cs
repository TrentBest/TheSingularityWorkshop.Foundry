using System.Text;
using TheSingularityWorkshop.FSM_API;
using Engine = TheSingularityWorkshop.FSM_API.FSM_API;

namespace Foundry.FoxSheepdog;

internal static class Program
{
    private const string FoxName = "Foundry.CI.FoxSheepdog.Fox.v1";
    private const string DogName = "Foundry.CI.FoxSheepdog.Dog.v1";
    private const string Group = "Foundry.CI.FoxSheepdog";
    private static readonly List<TraceEvent> Trace = new();
    private static FoxContext? _fox;
    private static DogContext? _dog;

    private static int Main(string[] args)
    {
        var outputDirectory = args.Length > 0 ? args[0] : "artifacts";
        Directory.CreateDirectory(outputDirectory);

        try
        {
            EnsureCleanStart();
            BuildDefinitions();
            _fox = new FoxContext();
            _dog = new DogContext();

            _fox.Handle = Engine.Create.CreateInstance(FoxName, _fox, Group);
            _dog.Handle = Engine.Create.CreateInstance(DogName, _dog, Group);
            Record(0, "WORLD", "Ready", "Fox=Walking;Dog=Sleeping;Scenario=deterministic");

            // The fixed sensor inputs model the same story as SimpleDemos without
            // Console.KeyAvailable, wall-clock sleeps, or an unbounded interactive loop.
            for (var tick = 1; tick <= 6; tick++)
            {
                ApplyScenarioInputs(tick);
                Engine.Interaction.Update(Group);
                Record(tick, "WORLD", "Observed",
                    $"Fox={_fox.Handle.CurrentState}@{_fox.Position};Dog={_dog.Handle.CurrentState}@{_dog.Position}");
            }

            AssertState("Fox", "Mangled", _fox.Handle.CurrentState);
            AssertState("Dog", "Mangling", _dog.Handle.CurrentState);
            AssertContains("Fox", "Walking");
            AssertContains("Fox", "Jumping");
            AssertContains("Fox", "Fleeing");
            AssertContains("Dog", "Sleeping");
            AssertContains("Dog", "Awake");
            AssertContains("Dog", "Chasing");
            AssertContains("Dog", "Mangling");
            AssertContains("Fox", "Mangled");

            var tracePath = Path.Combine(outputDirectory, "fox-sheepdog.trace");
            var svgPath = Path.Combine(outputDirectory, "fox-sheepdog.svg");
            File.WriteAllLines(tracePath, Trace.Select(item => item.ToTsv()), new UTF8Encoding(false));
            File.WriteAllText(svgPath, BuildSvg(), new UTF8Encoding(false));

            Console.WriteLine("PASS: real FSM_API definitions and instances executed.");
            Console.WriteLine("PASS: fox exercised Walking, Jumping, Fleeing, and Mangled.");
            Console.WriteLine("PASS: dog exercised Sleeping, Awake, Chasing, and Mangling.");
            Console.WriteLine("PASS: dog-to-fox cross-FSM transition reached Mangled.");
            Console.WriteLine($"EVIDENCE: {tracePath}");
            Console.WriteLine($"EVIDENCE: {svgPath}");
            Console.WriteLine($"TRACE_EVENTS: {Trace.Count}");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"FAIL: {exception}");
            return 1;
        }
        finally
        {
            CleanupOwnedDefinition(FoxName);
            CleanupOwnedDefinition(DogName);
        }
    }

    private static void EnsureCleanStart()
    {
        if (Engine.Interaction.Exists(FoxName, Group))
            throw new InvalidOperationException($"Probe-owned FSM '{FoxName}' already exists.");
        if (Engine.Interaction.Exists(DogName, Group))
            throw new InvalidOperationException($"Probe-owned FSM '{DogName}' already exists.");
    }

    private static void BuildDefinitions()
    {
        Engine.Create.CreateFiniteStateMachine(FoxName, -1, Group)
            .State("Walking", ctx => OnFoxEnter(ctx, "Walking"), null, null)
            .State("Jumping", ctx => OnFoxEnter(ctx, "Jumping"), null, null)
            .State("Fleeing", ctx => OnFoxEnter(ctx, "Fleeing"), null, null)
            .State("Mangled", ctx => OnFoxEnter(ctx, "Mangled"), null, null)
            .WithInitialState("Walking")
            .Transition("Walking", "Jumping", ctx => ((FoxContext)ctx).Tick == 1)
            .Transition("Jumping", "Walking", ctx => ((FoxContext)ctx).Tick == 2)
            .Transition("Walking", "Fleeing", ctx => ((FoxContext)ctx).Tick == 3)
            .BuildDefinition();

        Engine.Create.CreateFiniteStateMachine(DogName, -1, Group)
            .State("Sleeping", ctx => OnDogEnter(ctx, "Sleeping"), null, null)
            .State("Awake", ctx => OnDogEnter(ctx, "Awake"), null, null)
            .State("Chasing", ctx => OnDogEnter(ctx, "Chasing"), null, null)
            .State("Mangling", ctx => OnDogEnter(ctx, "Mangling"), null, null)
            .WithInitialState("Sleeping")
            .Transition("Sleeping", "Awake", ctx => ((DogContext)ctx).Tick == 3)
            .Transition("Awake", "Chasing", ctx => ((DogContext)ctx).Tick == 4)
            .Transition("Chasing", "Mangling", ctx => ((DogContext)ctx).Tick == 5)
            .BuildDefinition();
    }

    private static void ApplyScenarioInputs(int tick)
    {
        if (_fox is null || _dog is null) throw new InvalidOperationException("Agents not initialized.");
        _fox.Tick = tick;
        _dog.Tick = tick;
        _fox.Position += _fox.Handle.CurrentState == "Fleeing" ? 2 : 1;
        _dog.Position += _dog.Handle.CurrentState == "Chasing" ? 3 : 0;
    }

    private static void OnFoxEnter(IStateContext context, string state)
    {
        if (context is FoxContext fox)
            Record(fox.Tick, "Fox", state, $"position={fox.Position};reason={Reason(fox.Tick, state)}");
    }

    private static void OnDogEnter(IStateContext context, string state)
    {
        if (context is DogContext dog)
        {
            Record(dog.Tick, "Dog", state, $"position={dog.Position};reason={Reason(dog.Tick, state)}");
            if (state == "Mangling")
            {
                if (_fox?.Handle is null)
                    throw new InvalidOperationException("Dog reached Mangling before the fox handle was available.");
                _fox.Handle.TransitionTo("Mangled");
            }
        }
    }

    private static string Reason(int tick, string state) => (state, tick) switch
    {
        ("Walking", 0) => "initial state",
        ("Jumping", 1) => "obstacle detected",
        ("Walking", 2) => "jump completed",
        ("Fleeing", 3) => "collision response",
        ("Awake", 3) => "collision woke dog",
        ("Chasing", 4) => "fox visible",
        ("Mangling", 5) => "dog caught fox",
        ("Mangled", 5) => "dog requested cross-FSM transition",
        _ => "state entered"
    };

    private static void AssertState(string agent, string expected, string actual)
    {
        if (!StringComparer.Ordinal.Equals(expected, actual))
            throw new InvalidOperationException($"{agent}: expected final state '{expected}', got '{actual}'.");
    }

    private static void AssertContains(string agent, string state)
    {
        if (!Trace.Any(item => item.Agent == agent && item.State == state))
            throw new InvalidOperationException($"Expected trace event {agent}/{state} was not observed.");
    }

    private static void Record(int tick, string agent, string state, string detail) =>
        Trace.Add(new TraceEvent(tick, agent, state, detail));

    private static string BuildSvg()
    {
        var rows = Trace.Where(item => item.Agent != "WORLD").ToArray();
        var body = new StringBuilder();
        var y = 156;
        foreach (var item in rows)
        {
            body.AppendLine($"<text x=\"48\" y=\"{y}\" class=\"tick\">T{item.Tick}</text>");
            body.AppendLine($"<text x=\"112\" y=\"{y}\" class=\"agent\">{Escape(item.Agent)}</text>");
            body.AppendLine($"<text x=\"260\" y=\"{y}\" class=\"state\">{Escape(item.State)}</text>");
            body.AppendLine($"<text x=\"470\" y=\"{y}\" class=\"detail\">{Escape(item.Detail)}</text>");
            y += 32;
        }

        var height = Math.Max(420, y + 48);
        return $"""
<svg xmlns="http://www.w3.org/2000/svg" width="1100" height="{{{height}}}" viewBox="0 0 1100 {{height}}" role="img" aria-labelledby="title desc">
<title id="title">Fox and sheepdog — CI execution trace</title>
<desc id="desc">Generated from state-entry events emitted by the real FSM_API execution. This is a trace visualization, not a live animation.</desc>
<rect width="1100" height="{{{height}}}" rx="20" fill="#0b1421"/>
<text x="48" y="48" fill="#f1f6fc" font-size="25" font-weight="750">FOX + SHEEPDOG · VERIFIED CI TRACE</text>
<text x="48" y="78" fill="#a9bfd3" font-size="14">State entries emitted by FSM_API · deterministic six-tick scenario</text>
<path d="M40 104 H1060" stroke="#2d4155" stroke-width="2"/>
<text x="48" y="132" class="head">TICK</text><text x="112" y="132" class="head">AGENT</text><text x="260" y="132" class="head">STATE</text><text x="470" y="132" class="head">OBSERVED DETAIL</text>
{{body}}
<style>.head{fill:#88a2bb;font-size:12px;font-weight:700;letter-spacing:1px}.tick{fill:#88a2bb;font:14px ui-monospace,monospace}.agent{fill:#74d6b3;font-size:14px;font-weight:700}.state{fill:#f1f6fc;font-size:15px;font-weight:700}.detail{fill:#b7c9da;font-size:13px}</style>
</svg>
""";
    }

    private static string Escape(string value) => value
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal)
        .Replace("\"", "&quot;", StringComparison.Ordinal);

    private static void CleanupOwnedDefinition(string name)
    {
        if (Engine.Interaction.Exists(name, Group))
            Engine.Interaction.DestroyFiniteStateMachine(name, Group);
    }

    private sealed class FoxContext : IStateContext
    {
        public bool IsValid { get; set; } = true;
        public string Name { get; set; } = "CI fox";
        public int Tick { get; set; }
        public int Position { get; set; }
        public FSMHandle Handle { get; set; } = null!;
    }

    private sealed class DogContext : IStateContext
    {
        public bool IsValid { get; set; } = true;
        public string Name { get; set; } = "CI sheepdog";
        public int Tick { get; set; }
        public int Position { get; set; } = 3;
        public FSMHandle Handle { get; set; } = null!;
    }

    private sealed record TraceEvent(int Tick, string Agent, string State, string Detail)
    {
        public string ToTsv() => $"{Tick}\t{Agent}\t{State}\t{Detail.Replace("\t", " ")}";
    }
}
