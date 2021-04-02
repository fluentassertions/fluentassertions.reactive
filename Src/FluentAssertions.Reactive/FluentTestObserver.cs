using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;

namespace FluentAssertions.Reactive
{
    /// <summary>
    /// Observer for testing <see cref="Observable"/>s using the FluentAssertions framework
    /// </summary>
    /// <typeparam name="TPayload"></typeparam>
    public class FluentTestObserver<TPayload> : IObserver<TPayload>, IDisposable
    {
        private readonly IDisposable subscription;
        private readonly IScheduler observeScheduler;
        private readonly RollingReplaySubject<Recorded<Notification<TPayload>>> rollingReplaySubject = new RollingReplaySubject<Recorded<Notification<TPayload>>>();

        /// <summary>
        /// The observable which is observed by this instance
        /// </summary>
        public IObservable<TPayload> Subject { get; }

        /// <summary>
        /// The stream of recorded <see cref="Notification{T}"/>s
        /// </summary>
        public IObservable<Recorded<Notification<TPayload>>> RecordedNotificationStream => rollingReplaySubject.AsObservable();

        /// <summary>
        /// The recorded <see cref="Notification{T}"/>s
        /// </summary>
        public IEnumerable<Recorded<Notification<TPayload>>> RecordedNotifications =>
            rollingReplaySubject.GetSnapshot();

        /// <summary>
        /// The recorded messages
        /// </summary>
        public IEnumerable<TPayload> RecordedMessages =>
            RecordedNotifications.GetMessages();
        
        /// <summary>
        /// The exception 
        /// </summary>
        public Exception Error =>
            RecordedNotifications
                .Where(r => r.Value.Kind == NotificationKind.OnError)
                .Select(r => r.Value.Exception)
                .FirstOrDefault();
        
        /// <summary>
        /// The recorded messages
        /// </summary>
        public bool Completed =>
            RecordedNotifications
                .Any(r => r.Value.Kind == NotificationKind.OnCompleted);

        /// <summary>
        /// Creates a new <see cref="FluentTestObserver{TPayload}"/> which subscribes to the supplied <see cref="IObservable{T}"/>
        /// </summary>
        /// <param name="subject">the <see cref="IObservable{T}"/> under test</param>
        public FluentTestObserver(IObservable<TPayload> subject)
        {
            Subject = subject;
            observeScheduler = new EventLoopScheduler();
            subscription = new CompositeDisposable(); subject.ObserveOn(observeScheduler).Subscribe(this);
        }

        /// <summary>
        /// Creates a new <see cref="FluentTestObserver{TPayload}"/> which subscribes to the supplied <see cref="IObservable{T}"/>
        /// </summary>
        /// <param name="subject">the <see cref="IObservable{T}"/> under test</param>
        /// <param name="scheduler">the scheduler used for observing the sequence</param>
        public FluentTestObserver(IObservable<TPayload> subject, IScheduler scheduler)
        {
            Subject = subject;
            observeScheduler = scheduler;
            subscription = subject.ObserveOn(scheduler).Subscribe(this);
        }

        /// <summary>
        /// Creates a new <see cref="FluentTestObserver{TPayload}"/> which subscribes to the supplied <see cref="IObservable{T}"/>
        /// </summary>
        /// <param name="subject">the <see cref="IObservable{T}"/> under test</param>
        /// <param name="testScheduler">the test scheduler used to control notification timing</param>
        public FluentTestObserver(IObservable<TPayload> subject, TestScheduler testScheduler)
        {
            Subject = subject;
            observeScheduler = testScheduler;
            subscription = subject.ObserveOn(Scheduler.CurrentThread).Subscribe(this);
        }

        /// <summary>
        /// Clears the recorded notifications and messages as well as the recorded notifications stream buffer
        /// </summary>
        public void Clear() => rollingReplaySubject.Clear();

        /// <inheritdoc />
        public void OnNext(TPayload value)
        {
            rollingReplaySubject.OnNext(
                new Recorded<Notification<TPayload>>(observeScheduler.Now.UtcTicks, Notification.CreateOnNext(value)));
        }

        /// <inheritdoc />
        public void OnError(Exception exception) =>
            rollingReplaySubject.OnNext(new Recorded<Notification<TPayload>>(observeScheduler.Now.UtcTicks, Notification.CreateOnError<TPayload>(exception)));

        /// <inheritdoc />
        public void OnCompleted() =>
            rollingReplaySubject.OnNext(new Recorded<Notification<TPayload>>(observeScheduler.Now.UtcTicks, Notification.CreateOnCompleted<TPayload>()));
        
        /// <inheritdoc />
        public void Dispose()
        {
            subscription?.Dispose();
            rollingReplaySubject?.Dispose();
        }

        /// <summary>
        /// Returns an <see cref="ReactiveAssertions{TPayload}"/> object that can be used to assert the observed <see cref="IObservable{T}"/>
        /// </summary>
        /// <returns></returns>
        public ReactiveAssertions<TPayload> Should() => new ReactiveAssertions<TPayload>(this);
    }
}
