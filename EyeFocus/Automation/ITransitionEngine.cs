using System;
using System.Threading;
using System.Threading.Tasks;
using EyeFocus.Models;

namespace EyeFocus.Automation
{
    public interface ITransitionEngine : IDisposable
    {
        Task TransitionToProfileAsync(DisplayProfile targetProfile, int durationSeconds, CancellationToken cancellationToken = default);
        void CancelActiveTransition();
        bool IsTransitionActive { get; }
        event EventHandler<DisplayProfile>? TransitionStepCompleted;
        event EventHandler? TransitionFinished;
    }
}
