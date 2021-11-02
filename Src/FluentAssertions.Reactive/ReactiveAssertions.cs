using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using FluentAssertions.Common;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using FluentAssertions.Specialized;
using JetBrains.Annotations;
using Microsoft.Reactive.Testing;

namespace FluentAssertions.Reactive
{
    /// <summary>
    /// Provides methods to assert an <see cref="IObservable{T}"/> observed by a <see cref="FluentTestObserver{TPayload}"/>
    /// </summary>
    /// <typeparam name="TPayload"></typeparam>
    public class ReactiveAssertions<TPayload> : ReferenceTypeAssertions<IObservable<TPayload>, ReactiveAssertions<TPayload>>
    {
        private readonly IExtractExceptions extractor = new AggregateExceptionExtractor();
        public FluentTestObserver<TPayload> Observer { get; }

        protected internal ReactiveAssertions(FluentTestObserver<TPayload> observer): base(observer.Subject)
        {
            Observer = observer;
        }

        protected override string Identifier => "Subscription";

        /// <summary>
        /// Asserts that at least <paramref name="numberOfNotifications"/> notifications were pushed to the <see cref="FluentTestObserver{TPayload}"/> within the specified <paramref name="timeout"/>.<br />
        /// This includes any previously recorded notifications since it has been created or cleared.
        /// </summary> 
        /// <param name="numberOfNotifications">the number of notifications the observer should have recorded by now</param>
        /// <param name="timeout">the maximum time to wait for the notifications to arrive</param>
        /// <param name="because"></param>
        /// <param name="becauseArgs"></param>
        public AndWhichConstraint<ReactiveAssertions<TPayload>, IEnumerable<TPayload>> Push(int numberOfNotifications, TimeSpan timeout,
            string because = "", params object[] becauseArgs)
        {
            IList<TPayload> notifications = new List<TPayload>();
            var assertion = Execute.Assertion
                .WithExpectation($"Expected observable to push at least {numberOfNotifications} {(numberOfNotifications == 1 ? "notification" : "notifications")}, ")
                .BecauseOf(because, becauseArgs);

            try
            {
                notifications = Observer.RecordedNotificationStream
                    .Select(r => r.Value)
                    .Dematerialize()
                    .Take(numberOfNotifications)
                    .Timeout(timeout)
                    .Catch<TPayload, TimeoutException>(exception => Observable.Empty<TPayload>())
                    .ToList()
                    .ToTask()
                    .ExecuteInDefaultSynchronizationContext();
            }
            catch (Exception e)
            {
                if(e is AggregateException aggregateException)
                    e = aggregateException.InnerException;
                assertion.FailWith("but it failed with a {0}.", e);
            }

            assertion
                .ForCondition(notifications.Count >= numberOfNotifications)
                .FailWith("but {0} were received within {1}.", notifications.Count, timeout);

            return new AndWhichConstraint<ReactiveAssertions<TPayload>, IEnumerable<TPayload>>(this, notifications);
        }

        /// <inheritdoc cref="Push(int,TimeSpan,string,object[])"/>
        public async Task<AndWhichConstraint<ReactiveAssertions<TPayload>, IEnumerable<TPayload>>> PushAsync(int numberOfNotifications, TimeSpan timeout,
            string because = "", params object[] becauseArgs)
        {
            IList<TPayload> notifications = new List<TPayload>();
            var assertion = Execute.Assertion
                .WithExpectation($"Expected observable to push at least {numberOfNotifications} {(numberOfNotifications == 1 ? "notification" : "notifications")}, ")
                .BecauseOf(because, becauseArgs);

            try
            {
                notifications = await Observer.RecordedNotificationStream
                    .Select(r => r.Value)
                    .Dematerialize()
                    .Take(numberOfNotifications)
                    .Timeout(timeout)
                    .Catch<TPayload, TimeoutException>(exception => Observable.Empty<TPayload>())
                    .ToList()
                    .ToTask().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                if (e is AggregateException aggregateException)
                    e = aggregateException.InnerException;
                assertion.FailWith("but it failed with a {0}.", e);
            }

            assertion
                .ForCondition(notifications.Count >= numberOfNotifications)
                .FailWith("but {0} were received within {1}.", notifications.Count, timeout);

            return new AndWhichConstraint<ReactiveAssertions<TPayload>, IEnumerable<TPayload>>(this, notifications);
        }

        /// <summary>
        /// Asserts that at least <paramref name="numberOfNotifications"/> notifications are pushed to the <see cref="FluentTestObserver{TPayload}"/> within the next 1 second.<br />
        /// This includes any previously recorded notifications since it has been created or cleared. 
        /// </summary>
        /// <param name="numberOfNotifications">the number of notifications the observer should have recorded by now</param>
        /// <param name="because"></param>
        /// <param name="becauseArgs"></param>
        public AndWhichConstraint<ReactiveAssertions<TPayload>, IEnumerable<TPayload>> Push(int numberOfNotifications, string because = "", params object[] becauseArgs)
            => Push(numberOfNotifications, TimeSpan.FromSeconds(10), because, becauseArgs);

        /// <inheritdoc cref="Push(int,string,object[])"/>
        public Task<AndWhichConstraint<ReactiveAssertions<TPayload>, IEnumerable<TPayload>>> PushAsync(int numberOfNotifications, string because = "", params object[] becauseArgs)
            => PushAsync(numberOfNotifications, TimeSpan.FromSeconds(10), because, becauseArgs);

        /// <summary>
        /// Asserts that at least 1 notification is pushed to the <see cref="FluentTestObserver{TPayload}"/> within the next 1 second.<br />
        /// This includes any previously recorded notifications since it has been created or cleared. 
        /// </summary>
        public AndWhichConstraint<ReactiveAssertions<TPayload>, IEnumerable<TPayload>> Push(string because = "", params object[] becauseArgs)
            => Push(1, TimeSpan.FromSeconds(1), because, becauseArgs);

        /// <inheritdoc cref="Push(string,object[])"/>
        public Task<AndWhichConstraint<ReactiveAssertions<TPayload>, IEnumerable<TPayload>>> PushAsync(string because = "", params object[] becauseArgs)
            => PushAsync(1, TimeSpan.FromSeconds(1), because, becauseArgs);

        /// <summary>
        /// Asserts that the <see cref="FluentTestObserver{TPayload}"/> does not receive any notifications within the specified <paramref name="timeout"/>.<br />
        /// This includes any previously recorded notifications since it has been created or cleared. 
        /// </summary>
        public AndConstraint<ReactiveAssertions<TPayload>> NotPush(TimeSpan timeout,
            string because = "", params object[] becauseArgs)
        {
            bool anyNotifications = Observer.RecordedNotificationStream
                .Any(recorded => recorded.Value.Kind == NotificationKind.OnNext)
                .Timeout(timeout)
                .Catch(Observable.Return(false))
                .ToTask()
                .ExecuteInDefaultSynchronizationContext();

            Execute.Assertion
                .ForCondition(!anyNotifications)
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected observable to not push any notifications{reason}, but it did.");
            
            return new AndConstraint<ReactiveAssertions<TPayload>>(this);
        }

        /// <summary>
        /// Asserts that the <see cref="FluentTestObserver{TPayload}"/> does not receive any notifications within the next 100 milliseconds.<br />
        /// This includes any previously recorded notifications since it has been created or last cleared. 
        /// </summary>
        public AndConstraint<ReactiveAssertions<TPayload>> NotPush(string because = "", params object[] becauseArgs)
            => NotPush(TimeSpan.FromMilliseconds(100), because, becauseArgs);

        /// <summary>
        /// Asserts that the <see cref="IObservable{T}"/> observed by the <see cref="FluentTestObserver{TPayload}"/> fails within the specified <paramref name="timeout"/>. 
        /// </summary>
        public ExceptionAssertions<TException> Throw<TException>(TimeSpan timeout, string because = "", params object[] becauseArgs)
            where TException : Exception
        {
            var notifications = GetRecordedNotifications(timeout).ExecuteInDefaultSynchronizationContext();
            return Throw<TException>(notifications, because, becauseArgs);
        }

        /// <inheritdoc cref="Throw"/>
        public async Task<ExceptionAssertions<TException>> ThrowAsync<TException>(TimeSpan timeout,
            string because = "", params object[] becauseArgs)
            where TException : Exception
        {
            var notifications = await GetRecordedNotifications(timeout).ConfigureAwait(false);
            return Throw<TException>(notifications, because, becauseArgs);
        }

        /// <summary>
        /// Asserts that the <see cref="IObservable{T}"/> observed by the <see cref="FluentTestObserver{TPayload}"/> fails within the next 1 second. 
        /// </summary>
        public ExceptionAssertions<TException> Throw<TException>(string because = "", params object[] becauseArgs)
            where TException : Exception
            => Throw<TException>(TimeSpan.FromSeconds(1), because, becauseArgs);

        /// <inheritdoc cref="Throw(string,object[])"/>
        public Task<ExceptionAssertions<TException>> ThrowAsync<TException>(string because = "", params object[] becauseArgs)
            where TException : Exception
            => ThrowAsync<TException>(TimeSpan.FromSeconds(1), because, becauseArgs);

        /// <summary>
        /// Asserts that the <see cref="IObservable{T}"/> observed by the <see cref="FluentTestObserver{TPayload}"/> completes within the specified <paramref name="timeout"/>. 
        /// </summary>
        public AndConstraint<ReactiveAssertions<TPayload>> Complete(TimeSpan timeout,
            string because = "", params object[] becauseArgs)
        {
            var notifications = GetRecordedNotifications(timeout).ExecuteInDefaultSynchronizationContext();

            return Complete(timeout, because, becauseArgs, notifications);
        }


        /// <inheritdoc cref="Complete(System.TimeSpan,string,object[])"/>
        public async Task<AndConstraint<ReactiveAssertions<TPayload>>> CompleteAsync(TimeSpan timeout,
            string because = "", params object[] becauseArgs)
        {
            var notifications = await GetRecordedNotifications(timeout).ConfigureAwait(false);

            return Complete(timeout, because, becauseArgs, notifications);
        }

        /// <summary>
        /// Asserts that the <see cref="IObservable{T}"/> observed by the <see cref="FluentTestObserver{TPayload}"/> completes within the next 1 second. 
        /// </summary>
        public AndConstraint<ReactiveAssertions<TPayload>> Complete(string because = "", params object[] becauseArgs)
            => Complete(TimeSpan.FromSeconds(1), because, becauseArgs);

        /// <inheritdoc cref="Complete(string,object[])"/>
        public Task<AndConstraint<ReactiveAssertions<TPayload>>> CompleteAsync(string because = "", params object[] becauseArgs)
            => CompleteAsync(TimeSpan.FromSeconds(1), because, becauseArgs);

        /// <summary>
        /// Asserts that the <see cref="IObservable{T}"/> observed by the <see cref="FluentTestObserver{TPayload}"/> does not complete within the specified <paramref name="timeout"/>. 
        /// </summary>
        public AndConstraint<ReactiveAssertions<TPayload>> NotComplete(TimeSpan timeout,
            string because = "", params object[] becauseArgs)
        {
            bool completed = Observer.RecordedNotificationStream
                .Any(recorded => recorded.Value.Kind == NotificationKind.OnCompleted)
                .Timeout(timeout)
                .Catch(Observable.Return(false))
                .ToTask()
                .ExecuteInDefaultSynchronizationContext();

            Execute.Assertion
                .ForCondition(!completed)
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected observable to not complete{reason}, but it did.");
            
            return new AndConstraint<ReactiveAssertions<TPayload>>(this);
        }

        /// <summary>
        /// Asserts that the <see cref="IObservable{T}"/> observed by the <see cref="FluentTestObserver{TPayload}"/> does not complete within the next 100 milliseconds. 
        /// </summary>
        public AndConstraint<ReactiveAssertions<TPayload>> NotComplete(string because = "", params object[] becauseArgs)
            => NotComplete(TimeSpan.FromMilliseconds(100), because, becauseArgs);


        /// <summary>
        /// Asserts that at least one notification matching <paramref name="predicate"/> was pushed to the <see cref="FluentTestObserver{TPayload}"/>
        /// within the specified <paramref name="timeout"/>.<br />
        /// This includes any previously recorded notifications since it has been created or cleared.
        /// </summary>
        /// <param name="predicate">A predicate to match the items in the collection against.</param>
        /// <param name="timeout">the maximum time to wait for the notification to arrive</param>
        /// <param name="because">
        /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
        /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
        /// </param>
        /// <param name="becauseArgs">
        /// Zero or more objects to format using the placeholders in <paramref name="because"/>.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is <c>null</c>.</exception>
        public AndConstraint<ReactiveAssertions<TPayload>> PushMatch([NotNull] Expression<Func<TPayload, bool>> predicate, TimeSpan timeout, string because = "", params object[] becauseArgs)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            IList<TPayload> notifications = new List<TPayload>();
            AssertionScope assertion = Execute.Assertion
                .WithExpectation("Expected {context:observable} {0} to push an item matching {1}{reason}", Subject, predicate.Body)
                .BecauseOf(because, becauseArgs);

            try
            {
                Func<TPayload, bool> func = predicate.Compile();
                notifications = Observer.RecordedNotificationStream
                    .Select(r => r.Value)
                    .Dematerialize()
                    .Where(func)
                    .Take(1)
                    .Timeout(timeout)
                    .Catch<TPayload, TimeoutException>(exception => Observable.Empty<TPayload>())
                    .ToList()
                    .ToTask()
                    .ExecuteInDefaultSynchronizationContext();
            }
            catch (Exception e)
            {
                if (e is AggregateException aggregateException)
                    e = aggregateException.InnerException;
                assertion.FailWith(", but it failed with a {0}.", e);
            }
            
            assertion
                .ForCondition(notifications.Any())
                .FailWith(" within {0}.", timeout);

            return new AndConstraint<ReactiveAssertions<TPayload>>(this);
        }

        /// <inheritdoc cref="PushMatch"/>
        public async Task<AndConstraint<ReactiveAssertions<TPayload>>> PushMatchAsync([NotNull] Expression<Func<TPayload, bool>> predicate, TimeSpan timeout,
            string because = "", params object[] becauseArgs)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            IList<TPayload> notifications = new List<TPayload>();
            AssertionScope assertion = Execute.Assertion
                .WithExpectation("Expected {context:observable} {0} to push an item matching {1}{reason}", Subject, predicate.Body)
                .BecauseOf(because, becauseArgs);

            try
            {
                Func<TPayload, bool> func = predicate.Compile();
                notifications = await Observer.RecordedNotificationStream
                    .Select(r => r.Value)
                    .Dematerialize()
                    .Where(func)
                    .Take(1)
                    .Timeout(timeout)
                    .Catch<TPayload, TimeoutException>(exception => Observable.Empty<TPayload>())
                    .ToList()
                    .ToTask().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                if (e is AggregateException aggregateException)
                    e = aggregateException.InnerException;
                assertion.FailWith(", but it failed with a {0}.", e);
            }

            assertion
                .ForCondition(notifications.Any())
                .FailWith(" within {0}.", timeout);

            return new AndWhichConstraint<ReactiveAssertions<TPayload>, IEnumerable<TPayload>>(this, notifications);
        }

        protected Task<IList<Recorded<Notification<TPayload>>>> GetRecordedNotifications(TimeSpan timeout) =>
            Observer.RecordedNotificationStream
                .TakeUntil(recorded => recorded.Value.Kind == NotificationKind.OnError)
                .TakeUntil(recorded => recorded.Value.Kind == NotificationKind.OnCompleted)
                .Timeout(timeout)
                .Catch(Observable.Empty<Recorded<Notification<TPayload>>>())
                .ToList()
                .ToTask();

        protected ExceptionAssertions<TException> Throw<TException>(IList<Recorded<Notification<TPayload>>> notifications, string because, object[] becauseArgs)
            where TException : Exception
        {
            var exception = notifications
                .Where(r => r.Value.Kind == NotificationKind.OnError)
                .Select(r => r.Value.Exception)
                .FirstOrDefault();

            TException[] expectedExceptions = extractor.OfType<TException>(exception).ToArray();

            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .WithExpectation("Expected observable to throw a <{0}>{reason}, ", typeof(TException))
                .ForCondition(exception != null)
                .FailWith("but no exception was thrown.")
                .Then
                .ForCondition(expectedExceptions.Any())
                .FailWith("but found <{0}>: {1}{2}.",
                    exception?.GetType(),
                    Environment.NewLine,
                    exception)
                .Then
                .ClearExpectation();

            return new ExceptionAssertions<TException>(expectedExceptions);
        }

        protected AndConstraint<ReactiveAssertions<TPayload>> Complete(TimeSpan timeout, string because, object[] becauseArgs, IList<Recorded<Notification<TPayload>>> notifications)
        {
            var exception = notifications
                .Where(r => r.Value.Kind == NotificationKind.OnError)
                .Select(r => r.Value.Exception)
                .FirstOrDefault();

            Execute.Assertion
                .WithExpectation("Expected observable to complete within {0}{reason}, ", timeout)
                .BecauseOf(because, becauseArgs)
                .ForCondition(exception is null)
                .FailWith("but it failed with <{0}>: {1}{2}.",
                    exception?.GetType(),
                    Environment.NewLine,
                    exception)
                .Then
                .ForCondition(notifications.Any(r => r.Value.Kind == NotificationKind.OnCompleted))
                .FailWith("but it did not.");

            return new AndConstraint<ReactiveAssertions<TPayload>>(this);
        }
    }
}
