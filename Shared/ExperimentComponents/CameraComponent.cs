using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ExperimentFramework;

[DisplayName("Camera")]
[IconString("\xE722")]
public abstract class CameraComponent : ExperimentComponentClass
{
    public abstract double Exposure { get; set; }
    public abstract double MinExposure { get; }
    public abstract double MaxExposure { get; }
    public abstract double Framerate { get; }
    public abstract double BufferUsage { get; }
    public abstract int SensorWidth { get; }
    public abstract int SensorHeight { get; }
    public abstract event FrameAvailableHandler? FrameAvailable;
    public abstract (int Width, int Height) ROI { get; set; }

    private double[]? referenceFrame;
    public double[]? ReferenceFrame { get => referenceFrame; set { referenceFrame = value; OnReferenceFrameChange?.Invoke(this); } }
    public event Action<CameraComponent>? OnReferenceFrameChange;
}

public class CameraFrame : IDisposable
{
    public int InUseCount;
    private Action OnDispose;
    private Func<bool> AvailabilityCheck;
    public bool IsAvailable => AvailabilityCheck();

    public readonly IntPtr ImagePtr;
    public readonly int Width;
    public readonly int Height;
    public readonly int BitsPerPixel;
    public readonly double dt;

    public CameraFrame(IntPtr image, int width, int height, int bitsPerPixel, int inUseCount, Action onDispose, Func<bool> availabilityCheck, double dt)
    {
        ImagePtr = image;
        Width = width;
        Height = height;
        BitsPerPixel = bitsPerPixel;
        InUseCount = inUseCount;
        OnDispose = onDispose;
        AvailabilityCheck = availabilityCheck;
        this.dt = dt;
    }

    private ulong FrameSum = ulong.MaxValue;
    public ulong GetFrameSum()
    {
        if (!IsAvailable)
        {
            throw new Exception("Frame is not available");
        }
        if (FrameSum != ulong.MaxValue)
        {
            return FrameSum;
        }
        unsafe
        {
            var buffer = new Span<ushort>(ImagePtr.ToPointer(), Width * Height);
            ulong sum = 0;
            for (var i = 0; i < Width * Height; i++)
            {
                sum += buffer[i];
            }
            return FrameSum = sum;
        }
    }

    public void Dispose()
    {
        var used = Interlocked.Decrement(ref InUseCount);
        if (used == 0)
        {
            OnDispose();
        }
        else if (used < 0 && Debugger.IsAttached)
        {
            throw new Exception("The camera frame has been disposed too often.");
        }
    }
}

public delegate void FrameAvailableHandler(CameraFrame frame);

[DisplayName("Debug")]
public class FakeCamera : CameraComponent
{
    private bool IsRunning = true;
    private int UsedBuffers = 0;
    private double InitialPhase;
    public FakeCamera()
    {
        var bitCount = 12;
        InitialPhase = DateTime.UtcNow.Second + DateTime.UtcNow.Millisecond / 1000.0;
        var previousTime = DateTime.UtcNow;
        Task.Run(() =>
        {
            while (IsRunning)
            {
                if (FrameAvailable != null)
                {
                    var listenerCount = FrameAvailable.GetInvocationList().Length;
                    if (listenerCount > 0)
                    {
                        var lineBuffer = new double[SensorWidth];
                        var phase = (DateTime.UtcNow.Second + DateTime.UtcNow.Millisecond / 1000.0 - InitialPhase) * 5 * 2*Math.PI;
                        for (var x = 0; x < SensorWidth; x++)
                        {
                            lineBuffer[x] = (Math.Sin(x * 5.0 / SensorWidth + phase) + 1) * (1 << bitCount) / 2;
                        }

                        var imageArray = new ushort[SensorWidth * SensorHeight];
                        var handle = GCHandle.Alloc(imageArray, GCHandleType.Pinned);
                        var image = new Span<ushort>(imageArray);
                        for (var y = 0; y < SensorHeight; y++)
                        {
                            var ry2 = Math.Pow(y / (float)SensorHeight * 2.0 - 1, 2);
                            for (var x = 0; x < SensorWidth; x++)
                            {
                                var r2 = Math.Pow(x / (float)SensorWidth * 2.0 - 1, 2) + ry2;
                                image[y * SensorWidth + x] = (ushort)(lineBuffer[x] * (1 - Math.Min(r2, 1)) + 0.5);
                            }
                        }
                        //image[0] = ushort.MaxValue;
                        var now = DateTime.UtcNow;
                        var dt = (now - previousTime).TotalSeconds;
                        previousTime = now;
                        Interlocked.Increment(ref UsedBuffers);
                        listenerCount = FrameAvailable.GetInvocationList().Length;
                        var frame = new CameraFrame(handle.AddrOfPinnedObject(), SensorWidth, SensorHeight, bitCount, listenerCount, () =>
                        {
                            handle.Free();
                            Interlocked.Decrement(ref UsedBuffers);
                        }, () => IsRunning, dt);
                        FrameAvailable.Invoke(frame);
                    }
                }
                Thread.Sleep(1);
            }
        });
    }
    public override void Dispose()
    {
        IsRunning = false;
        base.Dispose();
    }
    public override double Exposure { get; set; }
    public override double MinExposure { get => 0.01; }
    public override double MaxExposure { get => 1000; }
    public override double Framerate { get => 1000 / Exposure; }
    public override double BufferUsage { get => UsedBuffers / 250.0; }
    public override int SensorWidth { get => 128; }
    public override int SensorHeight { get => 128; }
    public override event FrameAvailableHandler? FrameAvailable;
    public override (int Width, int Height) ROI { get => (128, 128); set { } }
}
