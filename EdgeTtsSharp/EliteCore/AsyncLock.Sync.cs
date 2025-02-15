// ReSharper disable InconsistentlySynchronizedField

// ReSharper disable SuspiciousLockOverSynchronizationPrimitive
// ReSharper disable once CheckNamespace

namespace EliteCore.Utility;

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

[SuppressMessage("ReSharper", "ExplicitCallerInfoArgument")]
internal partial class AsyncLock
{
    /*LOCK*/

    public TRes LockSync<TRes>(Func<TRes> body,
                               [CallerMemberName] string memberName = "",
                               [CallerFilePath] string sourceFilePath = "",
                               [CallerLineNumber] int sourceLineNumber = 0)
    {
        return this.LockSync(_ => body.Invoke(), memberName: memberName, sourceFilePath: sourceFilePath, sourceLineNumber: sourceLineNumber);
    }

    public TRes LockSync<TRes>(Func<CancellationToken, TRes> body,
                               CancellationToken ct = default,
                               [CallerMemberName] string memberName = "",
                               [CallerFilePath] string sourceFilePath = "",
                               [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (this.IsDebug)
        {
            lock (this.semaphore)
            {
                if ((this.semaphore.CurrentCount > 0) && this.CurrentTaskAlreadyHolding())
                {
                    this.onSameTaskTriesToEnterError?.Invoke(new StackTrace().ToString());
                }
            }
        }

        this.semaphore.Wait(ct);
        try
        {
            this.taskHolding = Task.CurrentId;
            if (this.IsDebug)
            {
                this.HoldingAt = $"[{memberName}] at [{sourceFilePath}]:[{sourceLineNumber}]";
            }

            return body.Invoke(ct);
        }
        finally
        {
            this.taskHolding = 0;
            if (this.IsDebug)
            {
                this.HoldingAt = null;
            }

            this.semaphore.Release();
        }
    }

    public void LockSync(Action body, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
    {
        this.LockSync(_ => body.Invoke(), memberName: memberName, sourceFilePath: sourceFilePath, sourceLineNumber: sourceLineNumber);
    }

    public void LockSync(Action<CancellationToken> body,
                         CancellationToken ct = default,
                         [CallerMemberName] string memberName = "",
                         [CallerFilePath] string sourceFilePath = "",
                         [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (this.IsDebug)
        {
            lock (this.semaphore)
            {
                if ((this.semaphore.CurrentCount > 0) && this.CurrentTaskAlreadyHolding())
                {
                    this.onSameTaskTriesToEnterError?.Invoke(new StackTrace().ToString());
                }
            }
        }

        this.semaphore.Wait(ct);
        try
        {
            this.taskHolding = Task.CurrentId;
            if (this.IsDebug)
            {
                this.HoldingAt = $"[{memberName}] at [{sourceFilePath}]:[{sourceLineNumber}]";
            }

            body.Invoke(ct);
        }
        finally
        {
            this.taskHolding = 0;
            if (this.IsDebug)
            {
                this.HoldingAt = null;
            }

            this.semaphore.Release();
        }
    }


    public bool TryLockSync(Action<CancellationToken> body,
                            int waitForMillis = 0,
                            CancellationToken ct = default,
                            [CallerMemberName] string memberName = "",
                            [CallerFilePath] string sourceFilePath = "",
                            [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (this.IsDebug)
        {
            lock (this.semaphore)
            {
                if ((this.semaphore.CurrentCount > 0) && this.CurrentTaskAlreadyHolding())
                {
                    this.onSameTaskTriesToEnterError?.Invoke(new StackTrace().ToString());
                }
            }
        }

        if (this.semaphore.Wait(waitForMillis, ct))
        {
            try
            {
                this.taskHolding = Task.CurrentId;
                if (this.IsDebug)
                {
                    this.HoldingAt = $"[{memberName}] at [{sourceFilePath}]:[{sourceLineNumber}]";
                }

                body.Invoke(ct);
            }
            finally
            {
                this.taskHolding = 0;
                if (this.IsDebug)
                {
                    this.HoldingAt = null;
                }

                this.semaphore.Release();
            }

            return true;
        }

        return false;
    }

    public bool TryLockSync(Action body,
                            int waitForMillis = 0,
                            [CallerMemberName] string memberName = "",
                            [CallerFilePath] string sourceFilePath = "",
                            [CallerLineNumber] int sourceLineNumber = 0)
    {
        return this.TryLockSync(_ => body.Invoke(), waitForMillis, memberName: memberName, sourceFilePath: sourceFilePath, sourceLineNumber: sourceLineNumber);
    }
}