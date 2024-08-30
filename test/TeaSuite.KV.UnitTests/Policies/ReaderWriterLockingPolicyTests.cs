using System.Reflection;

namespace TeaSuite.KV.Policies;

public sealed class ReaderWriterLockingPolicyTests
{
    private readonly ReaderWriterLockingPolicy policy;

    public ReaderWriterLockingPolicyTests()
    {
        policy = new ReaderWriterLockingPolicy();
    }

    [Fact]
    public void AcquireReadLockIsKeptUntilDisposed()
    {
        ReaderWriterLockSlim rwlock = GetLock();
        Assert.False(rwlock.IsReadLockHeld);
        Assert.Equal(0, rwlock.WaitingReadCount);

        IDisposable? disposable = policy.AcquireReadLock();
        Assert.NotNull(disposable);
        Assert.True(rwlock.IsReadLockHeld);
        Assert.Equal(1, rwlock.CurrentReadCount);

        disposable.Dispose();
        Assert.False(rwlock.IsReadLockHeld);
        Assert.Equal(0, rwlock.WaitingReadCount);
    }

    [Fact]
    public void AcquireWriteLockIsKeptUntilDisposed()
    {
        ReaderWriterLockSlim rwlock = GetLock();
        Assert.False(rwlock.IsWriteLockHeld);
        Assert.Equal(0, rwlock.WaitingWriteCount);

        IDisposable? disposable = policy.AcquireWriteLock();
        Assert.NotNull(disposable);
        Assert.True(rwlock.IsWriteLockHeld);

        disposable.Dispose();
        Assert.False(rwlock.IsWriteLockHeld);
        Assert.Equal(0, rwlock.WaitingWriteCount);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public async Task AcquiredReadLockBlocksWrites(int numReadLocks)
    {
        long readers = 0;
        long writers = 0;

        IDisposable?[] readLocks = new IDisposable?[numReadLocks];
        Task[] readTasks = new Task[numReadLocks];
        SemaphoreSlim isReading = new(0, numReadLocks);
        SemaphoreSlim finishReading = new(0, numReadLocks);
        for (int i = 0; i < numReadLocks; i++)
        {
            readTasks[i] = Task.Run(() =>
            {
                using (policy.AcquireReadLock())
                {
                    Interlocked.Increment(ref readers);
                    isReading.Release();
                    finishReading.Wait();
                };
                Interlocked.Decrement(ref readers);
            });
        }

        for (int i = 0; i < numReadLocks; i++)
        {
            isReading.Wait();
        }
        Assert.Equal(numReadLocks, Interlocked.Read(ref readers));
        Assert.Equal(0, Interlocked.Read(ref writers));

        ManualResetEventSlim startWrite = new(false);
        ManualResetEventSlim isWriting = new(false);
        ManualResetEventSlim finishWrite = new(false);
        Task writeTask = Task.Run(() =>
        {
            startWrite.Wait();
            using (policy.AcquireWriteLock())
            {
                Interlocked.Increment(ref writers);
                Assert.Equal(0, Interlocked.Read(ref readers));
                isWriting.Set();
                finishWrite.Wait();
            }
            Interlocked.Decrement(ref writers);
        });

        startWrite.Set();
        finishReading.Release(numReadLocks);
        isWriting.Wait();
        Assert.Equal(1, Interlocked.Read(ref writers));
        finishWrite.Set();

        await writeTask;
        await Task.WhenAll(readTasks);

        Assert.Equal(0, Interlocked.Read(ref writers));
        Assert.Equal(0, Interlocked.Read(ref readers));
    }

    private ReaderWriterLockSlim GetLock()
    {
        Type type = typeof(ReaderWriterLockingPolicy);
        FieldInfo? field = type.GetField(
            "rwlock", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        object? rawValue = field.GetValue(policy);
        Assert.NotNull(rawValue);

        return (ReaderWriterLockSlim)rawValue;
    }
}
