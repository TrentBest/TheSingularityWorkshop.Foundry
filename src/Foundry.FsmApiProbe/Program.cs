using System;
using TheSingularityWorkshop.FSM_API;
using Engine = TheSingularityWorkshop.FSM_API.FSM_API;

namespace TheSingularityWorkshop.Foundry.FsmApiProbe
{
    internal static class Program
    {
        private const string FsmName = "Foundry.CI.FsmApiProbe.v1";
        private const string ProcessingGroup = "Foundry.CI.Probe";

        private static int Main()
        {
            try
            {
                // This executable runs in a fresh process. It never resets global FSM_API state.
                if (Engine.Interaction.Exists(FsmName, ProcessingGroup))
                {
                    throw new InvalidOperationException(
                        $"The probe-owned FSM '{FsmName}' unexpectedly already exists.");
                }

                Engine.Create.CreateFiniteStateMachine(
                        FsmName, processRate: -1, processingGroup: ProcessingGroup)
                    .State("Ready", null, null, null)
                    .State("Complete", null, null, null)
                    .WithInitialState("Ready")
                    .Transition("Ready", "Complete", _ => true)
                    .BuildDefinition();

                var context = new ProbeContext();
                var handle = Engine.Create.CreateInstance(
                    FsmName, context, ProcessingGroup);

                if (handle.CurrentState != "Ready")
                {
                    throw new InvalidOperationException(
                        $"Expected initial state 'Ready', got '{handle.CurrentState}'.");
                }

                Engine.Interaction.Update(ProcessingGroup);

                if (handle.CurrentState != "Complete")
                {
                    throw new InvalidOperationException(
                        $"Expected transitioned state 'Complete', got '{handle.CurrentState}'.");
                }

                Console.WriteLine("PASS: actual FSM_API definition created.");
                Console.WriteLine("PASS: instance started in Ready.");
                Console.WriteLine("PASS: FSM_API update evaluated transition to Complete.");
                Console.WriteLine("PASS: probe uses no global reset and only cleans up its owned definition.");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"FAIL: {exception}");
                return 1;
            }
            finally
            {
                // Cleanup is deliberately scoped to this probe's known name and processing group.
                if (Engine.Interaction.Exists(FsmName, ProcessingGroup))
                {
                    Engine.Interaction.DestroyFiniteStateMachine(FsmName, ProcessingGroup);
                }
            }
        }

        private sealed class ProbeContext : IStateContext
        {
            public bool IsValid { get; set; } = true;
            public string Name { get; set; } = "Foundry CI FSM API probe";
        }
    }
}
