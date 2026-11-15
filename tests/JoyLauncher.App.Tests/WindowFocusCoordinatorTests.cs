using JoyLauncher.App.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JoyLauncher.App.Tests;

[TestClass]
public sealed class WindowFocusCoordinatorTests
{
    private static readonly IntPtr TargetWindow = new(101);
    private static readonly IntPtr OwnerWindow = new(202);

    [TestMethod]
    public void FocusFromDifferentThread_AttachesAndDetachesInputQueues()
    {
        var api = new FakeWindowFocusApi
        {
            CurrentThreadId = 10,
            TargetThreadId = 20,
            FocusSucceeds = true
        };
        var coordinator = new WindowFocusCoordinator(api);

        var focused = coordinator.TryFocus(TargetWindow, OwnerWindow, out var diagnostic);

        Assert.IsTrue(focused, diagnostic);
        CollectionAssert.AreEqual(
            new[] { (10u, 20u, true), (10u, 20u, false) },
            api.AttachCalls.ToArray());
        Assert.AreEqual(TargetWindow, api.FocusedWindow);
        Assert.AreEqual(1, api.SetFocusCallCount);
    }

    [TestMethod]
    public void FocusWithinSameThread_DoesNotAttachInputQueues()
    {
        var api = new FakeWindowFocusApi
        {
            CurrentThreadId = 10,
            TargetThreadId = 10,
            FocusSucceeds = true
        };
        var coordinator = new WindowFocusCoordinator(api);

        var focused = coordinator.TryFocus(TargetWindow, OwnerWindow, out var diagnostic);

        Assert.IsTrue(focused, diagnostic);
        Assert.AreEqual(0, api.AttachCalls.Count);
        Assert.AreEqual(TargetWindow, api.FocusedWindow);
    }

    [TestMethod]
    public void FailedInputQueueAttachment_DoesNotAttemptToSetFocus()
    {
        var api = new FakeWindowFocusApi
        {
            CurrentThreadId = 10,
            TargetThreadId = 20,
            AttachSucceeds = false
        };
        var coordinator = new WindowFocusCoordinator(api);

        var focused = coordinator.TryFocus(TargetWindow, OwnerWindow, out var diagnostic);

        Assert.IsFalse(focused);
        Assert.AreEqual(0, api.SetFocusCallCount);
        Assert.IsTrue(diagnostic.Contains("AttachThreadInput", StringComparison.OrdinalIgnoreCase), diagnostic);
        CollectionAssert.AreEqual(new[] { (10u, 20u, true) }, api.AttachCalls.ToArray());
    }

    [TestMethod]
    public void FocusFailure_StillDetachesInputQueues()
    {
        var api = new FakeWindowFocusApi
        {
            CurrentThreadId = 10,
            TargetThreadId = 20,
            FocusSucceeds = false
        };
        var coordinator = new WindowFocusCoordinator(api);

        var focused = coordinator.TryFocus(TargetWindow, OwnerWindow, out var diagnostic);

        Assert.IsFalse(focused);
        Assert.IsTrue(diagnostic.Contains("focus", StringComparison.OrdinalIgnoreCase), diagnostic);
        CollectionAssert.AreEqual(
            new[] { (10u, 20u, true), (10u, 20u, false) },
            api.AttachCalls.ToArray());
    }

    private sealed class FakeWindowFocusApi : IWindowFocusApi
    {
        public uint CurrentThreadId { get; init; }
        public uint TargetThreadId { get; init; }
        public bool AttachSucceeds { get; init; } = true;
        public bool DetachSucceeds { get; init; } = true;
        public bool FocusSucceeds { get; init; }
        public int SetFocusCallCount { get; private set; }
        public IntPtr FocusedWindow { get; private set; }
        public List<(uint From, uint To, bool Attach)> AttachCalls { get; } = [];

        public uint GetCurrentThreadId() => CurrentThreadId;

        public uint GetWindowThreadId(IntPtr window) => TargetThreadId;

        public bool AttachThreadInput(uint fromThreadId, uint toThreadId, bool attach)
        {
            AttachCalls.Add((fromThreadId, toThreadId, attach));
            return attach ? AttachSucceeds : DetachSucceeds;
        }

        public bool SetForegroundWindow(IntPtr window) => true;

        public IntPtr SetFocus(IntPtr window)
        {
            SetFocusCallCount++;
            if (FocusSucceeds)
                FocusedWindow = window;

            // SetFocus returns the previous focus HWND, which can legitimately be null.
            return IntPtr.Zero;
        }

        public IntPtr GetFocus() => FocusedWindow;

        public int GetLastError() => 5;
    }
}
