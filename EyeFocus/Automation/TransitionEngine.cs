using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EyeFocus.Display;
using EyeFocus.Models;
using EyeFocus.Storage;

namespace EyeFocus.Automation
{
    public class TransitionEngine : ITransitionEngine
    {
        private readonly IDisplayEngine _displayEngine;
        private CancellationTokenSource? _activeCts;
        private readonly object _lock = new();

        public bool IsTransitionActive { get; private set; }

        public event EventHandler<DisplayProfile>? TransitionStepCompleted;
        public event EventHandler? TransitionFinished;

        public TransitionEngine(IDisplayEngine displayEngine)
        {
            _displayEngine = displayEngine;
        }

        public async Task TransitionToProfileAsync(DisplayProfile targetProfile, int durationSeconds, CancellationToken cancellationToken = default)
        {
            CancelActiveTransition();

            if (durationSeconds <= 0)
            {
                _displayEngine.ApplyProfile(targetProfile);
                TransitionFinished?.Invoke(this, EventArgs.Empty);
                return;
            }

            CancellationTokenSource cts;
            lock (_lock)
            {
                _activeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts = _activeCts;
                IsTransitionActive = true;
            }

            var token = cts.Token;

            // Capture starting parameters from display engine
            int startKelvin = _displayEngine.CurrentKelvin;
            int startBrightness = _displayEngine.CurrentBrightness;
            int startDim = _displayEngine.CurrentSoftwareDim;
            int startR = 100, startG = 100, startB = 100;

            int targetKelvin = targetProfile.Kelvin;
            int targetBrightness = targetProfile.Brightness;
            int targetDim = targetProfile.SoftwareDim;
            int targetR = targetProfile.Red;
            int targetG = targetProfile.Green;
            int targetB = targetProfile.Blue;

            // Adaptive frame rate
            int intervalMs = durationSeconds switch
            {
                <= 5 => 33,    // ~30 FPS
                <= 60 => 66,   // ~15 FPS
                _ => 200       // ~5 FPS
            };

            var sw = Stopwatch.StartNew();
            double totalDurationMs = durationSeconds * 1000.0;
            var lastHwUpdate = DateTime.UtcNow;

            LogService.Debug($"Starting transition from {startKelvin}K/{startBrightness}% to {targetKelvin}K/{targetBrightness}% over {durationSeconds}s.");

            try
            {
                while (sw.ElapsedMilliseconds < totalDurationMs && !token.IsCancellationRequested)
                {
                    double linearT = Math.Clamp(sw.ElapsedMilliseconds / totalDurationMs, 0.0, 1.0);
                    // Smoothstep interpolation: 3t^2 - 2t^3
                    double smoothT = linearT * linearT * (3.0 - 2.0 * linearT);

                    int curKelvin = (int)Math.Round(startKelvin + (targetKelvin - startKelvin) * smoothT);
                    int curBrightness = (int)Math.Round(startBrightness + (targetBrightness - startBrightness) * smoothT);
                    int curDim = (int)Math.Round(startDim + (targetDim - startDim) * smoothT);
                    int curR = (int)Math.Round(startR + (targetR - startR) * smoothT);
                    int curG = (int)Math.Round(startG + (targetG - startG) * smoothT);
                    int curB = (int)Math.Round(startB + (targetB - startB) * smoothT);

                    var intermediateProfile = new DisplayProfile
                    {
                        Name = targetProfile.Name,
                        Kelvin = curKelvin,
                        Brightness = curBrightness,
                        SoftwareDim = curDim,
                        Red = curR,
                        Green = curG,
                        Blue = curB,
                        Contrast = targetProfile.Contrast
                    };

                    _displayEngine.ApplyProfile(intermediateProfile);
                    TransitionStepCompleted?.Invoke(this, intermediateProfile);

                    await Task.Delay(intervalMs, token).ConfigureAwait(false);
                }

                if (!token.IsCancellationRequested)
                {
                    // Apply exact final profile values
                    _displayEngine.ApplyProfile(targetProfile);
                    LogService.Debug("Transition completed successfully.");
                }
            }
            catch (OperationCanceledException)
            {
                LogService.Debug("Transition canceled.");
            }
            finally
            {
                lock (_lock)
                {
                    if (_activeCts == cts)
                    {
                        IsTransitionActive = false;
                        _activeCts = null;
                    }
                }
                TransitionFinished?.Invoke(this, EventArgs.Empty);
            }
        }

        public void CancelActiveTransition()
        {
            lock (_lock)
            {
                if (_activeCts != null)
                {
                    _activeCts.Cancel();
                    _activeCts.Dispose();
                    _activeCts = null;
                    IsTransitionActive = false;
                }
            }
        }

        public void Dispose()
        {
            CancelActiveTransition();
        }
    }
}
