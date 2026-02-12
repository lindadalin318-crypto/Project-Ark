using NUnit.Framework;
using ProjectArk.Combat.Enemy;

namespace ProjectArk.Combat.Tests
{
    /// <summary>
    /// Unit tests for <see cref="StateMachine"/>.
    /// Covers transition ordering (OnExit before OnEnter), no-op same-state, null guard.
    /// </summary>
    [TestFixture]
    public class StateMachineTests
    {
        // ──────────────────── Test Double ────────────────────

        private class RecordingState : IState
        {
            public int EnterCount;
            public int ExitCount;
            public int UpdateCount;
            public string Name;

            // Track global order of operations
            public static int GlobalCounter;
            public int EnterOrder;
            public int ExitOrder;

            public RecordingState(string name) { Name = name; }

            public void OnEnter()
            {
                EnterCount++;
                EnterOrder = ++GlobalCounter;
            }

            public void OnUpdate(float deltaTime)
            {
                UpdateCount++;
            }

            public void OnExit()
            {
                ExitCount++;
                ExitOrder = ++GlobalCounter;
            }
        }

        [SetUp]
        public void SetUp()
        {
            RecordingState.GlobalCounter = 0;
        }

        // ──────────────────── Tests ────────────────────

        [Test]
        public void Initialize_CallsOnEnter()
        {
            var sm = new StateMachine();
            var idle = new RecordingState("Idle");

            sm.Initialize(idle);

            Assert.AreEqual(1, idle.EnterCount);
            Assert.AreSame(idle, sm.CurrentState);
        }

        [Test]
        public void Tick_CallsOnUpdate()
        {
            var sm = new StateMachine();
            var idle = new RecordingState("Idle");
            sm.Initialize(idle);

            sm.Tick(0.016f);
            sm.Tick(0.016f);

            Assert.AreEqual(2, idle.UpdateCount);
        }

        [Test]
        public void TransitionTo_CallsExitThenEnter()
        {
            var sm = new StateMachine();
            var idle = new RecordingState("Idle");
            var chase = new RecordingState("Chase");
            sm.Initialize(idle);

            sm.TransitionTo(chase);

            Assert.AreEqual(1, idle.ExitCount);
            Assert.AreEqual(1, chase.EnterCount);
            Assert.AreSame(chase, sm.CurrentState);

            // Exit of idle happened BEFORE enter of chase
            Assert.Less(idle.ExitOrder, chase.EnterOrder);
        }

        [Test]
        public void TransitionTo_SameState_NoOp()
        {
            var sm = new StateMachine();
            var idle = new RecordingState("Idle");
            sm.Initialize(idle);

            sm.TransitionTo(idle);

            // Should not have called exit or re-entered
            Assert.AreEqual(1, idle.EnterCount);
            Assert.AreEqual(0, idle.ExitCount);
        }

        [Test]
        public void TransitionTo_Null_NoException()
        {
            var sm = new StateMachine();
            var idle = new RecordingState("Idle");
            sm.Initialize(idle);

            // Should not throw, just log a warning
            Assert.DoesNotThrow(() => sm.TransitionTo(null));
            Assert.AreSame(idle, sm.CurrentState);
        }

        [Test]
        public void MultipleTransitions_CorrectOrder()
        {
            var sm = new StateMachine();
            var a = new RecordingState("A");
            var b = new RecordingState("B");
            var c = new RecordingState("C");
            sm.Initialize(a);

            sm.TransitionTo(b);
            sm.TransitionTo(c);

            Assert.AreEqual(1, a.ExitCount);
            Assert.AreEqual(1, b.EnterCount);
            Assert.AreEqual(1, b.ExitCount);
            Assert.AreEqual(1, c.EnterCount);
            Assert.AreSame(c, sm.CurrentState);
        }

        [Test]
        public void NestedStateMachine_IndependentTick()
        {
            var outer = new StateMachine { DebugName = "Outer" };
            var inner = new StateMachine { DebugName = "Inner" };

            var outerState = new RecordingState("OuterState");
            var innerState = new RecordingState("InnerState");

            outer.Initialize(outerState);
            inner.Initialize(innerState);

            outer.Tick(0.016f);
            inner.Tick(0.016f);
            inner.Tick(0.016f);

            Assert.AreEqual(1, outerState.UpdateCount);
            Assert.AreEqual(2, innerState.UpdateCount);
        }
    }
}
