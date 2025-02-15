// ReSharper disable InconsistentlySynchronizedField

// ReSharper disable SuspiciousLockOverSynchronizationPrimitive
// ReSharper disable CheckNamespace

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
    private readonly Action<string>? onSameTaskTriesToEnterError;
    private readonly SemaphoreSlim semaphore;
    private int? taskHolding;

    public AsyncLock()
    {
        this.CheckDebug();
        this.semaphore = new SemaphoreSlim(1, 1);
    }

    public AsyncLock(Action<string> onSameTaskTriesToEnterError)
    {
        this.CheckDebug();
        this.semaphore = new SemaphoreSlim(1, 1);
        this.onSameTaskTriesToEnterError = onSameTaskTriesToEnterError;
    }

    public bool IsDebug { get; set; }

    public string? HoldingAt { get; private set; }

    [Conditional("DEBUG")]
    private void CheckDebug()
    {
        this.IsDebug = true;
    }

    private bool CurrentTaskAlreadyHolding()
    {
        return (Task.CurrentId == this.taskHolding) && (Task.CurrentId != null);
    }

    /*LOCK*/

    public async ValueTask<TRes> Lock<TRes>(Func<Task<TRes>> body,
                                            [CallerMemberName] string memberName = "",
                                            [CallerFilePath] string sourceFilePath = "",
                                            [CallerLineNumber] int sourceLineNumber = 0)
    {
        return await this.Lock(async _ => await body.Invoke(), memberName: memberName, sourceFilePath: sourceFilePath, sourceLineNumber: sourceLineNumber);
    }

    public async ValueTask<TRes> Lock<TRes>(Func<CancellationToken, Task<TRes>> body,
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

        await this.semaphore.WaitAsync(ct);
        //await Task.Yield();
        try
        {
            this.taskHolding = Task.CurrentId;
            if (this.IsDebug)
            {
                this.HoldingAt = $"[{memberName}] at [{sourceFilePath}]:[{sourceLineNumber}]";
            }

            return await body.Invoke(ct);
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

    public async ValueTask Lock(Func<Task> body,
                                [CallerMemberName] string memberName = "",
                                [CallerFilePath] string sourceFilePath = "",
                                [CallerLineNumber] int sourceLineNumber = 0)
    {
        await this.Lock(async _ => await body.Invoke(), memberName: memberName, sourceFilePath: sourceFilePath, sourceLineNumber: sourceLineNumber);
    }

    public async ValueTask Lock(Func<CancellationToken, Task> body,
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

        await this.semaphore.WaitAsync(ct);
        //await Task.Yield();
        try
        {
            this.taskHolding = Task.CurrentId;
            if (this.IsDebug)
            {
                this.HoldingAt = $"[{memberName}] at [{sourceFilePath}]:[{sourceLineNumber}]";
            }

            await body.Invoke(ct);
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

    public async ValueTask<TRes> Lock<TRes>(Func<TRes> body,
                                            [CallerMemberName] string memberName = "",
                                            [CallerFilePath] string sourceFilePath = "",
                                            [CallerLineNumber] int sourceLineNumber = 0)
    {
        return await this.Lock(_ => body.Invoke(), memberName: memberName, sourceFilePath: sourceFilePath, sourceLineNumber: sourceLineNumber);
    }

    public async ValueTask<TRes> Lock<TRes>(Func<CancellationToken, TRes> body,
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

        await this.semaphore.WaitAsync(ct);
        //await Task.Yield();
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

    public async ValueTask Lock(Action body, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
    {
        await this.Lock(_ => body.Invoke(), memberName: memberName, sourceFilePath: sourceFilePath, sourceLineNumber: sourceLineNumber);
    }

    public async ValueTask Lock(Action<CancellationToken> body,
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

        await this.semaphore.WaitAsync(ct);
        //await Task.Yield();
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

    public async ValueTask<bool> TryLock(Func<Task> body,
                                         int waitForMillis = 0,
                                         [CallerMemberName] string memberName = "",
                                         [CallerFilePath] string sourceFilePath = "",
                                         [CallerLineNumber] int sourceLineNumber = 0)
    {
        return await this.TryLock(async _ => await body.Invoke(), waitForMillis, memberName: memberName, sourceFilePath: sourceFilePath, sourceLineNumber: sourceLineNumber);
    }

    public async ValueTask<bool> TryLock(Func<CancellationToken, Task> body,
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

        if (await this.semaphore.WaitAsync(waitForMillis, ct))
        {
            //await Task.Yield();
            try
            {
                this.taskHolding = Task.CurrentId;
                if (this.IsDebug)
                {
                    this.HoldingAt = $"[{memberName}] at [{sourceFilePath}]:[{sourceLineNumber}]";
                }

                await body.Invoke(ct);
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

    public async ValueTask<bool> TryLock(Action body,
                                         int waitForMillis = 0,
                                         [CallerMemberName] string memberName = "",
                                         [CallerFilePath] string sourceFilePath = "",
                                         [CallerLineNumber] int sourceLineNumber = 0)
    {
        return await this.TryLock(_ => body.Invoke(), waitForMillis, memberName: memberName, sourceFilePath: sourceFilePath, sourceLineNumber: sourceLineNumber);
    }

    public async ValueTask<bool> TryLock(Action<CancellationToken> body,
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

        if (await this.semaphore.WaitAsync(waitForMillis, ct))
        {
            //await Task.Yield();
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
}