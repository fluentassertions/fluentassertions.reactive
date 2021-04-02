using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace FluentAssertions.Reactive
{
    /// <summary>
    /// Clearable <see cref="ReplaySubject{T}"/> taken from James World: https://stackoverflow.com/a/28945444/4340541
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RollingReplaySubject<T> : ISubject<T>, IDisposable
    {
        private readonly ReplaySubject<IObservable<T>> subjects;
        private readonly IObservable<T> concatenatedSubjects;
        private ISubject<T> currentSubject;

        public RollingReplaySubject()
        {
            subjects = new ReplaySubject<IObservable<T>>(1);
            concatenatedSubjects = subjects.Concat();
            currentSubject = new ReplaySubject<T>();
            subjects.OnNext(currentSubject);
        }

        public void Clear()
        {
            currentSubject.OnCompleted();
            currentSubject = new ReplaySubject<T>();
            subjects.OnNext(currentSubject);
        }

        public void OnNext(T value) => currentSubject.OnNext(value);

        public void OnError(Exception error) => currentSubject.OnError(error);

        public void OnCompleted()
        {
            currentSubject.OnCompleted();
            subjects.OnCompleted();
            // a quick way to make the current ReplaySubject unreachable
            // except to in-flight observers, and not hold up collection
            currentSubject = new Subject<T>();
        }

        public IDisposable Subscribe(IObserver<T> observer) => concatenatedSubjects.Subscribe(observer);

        public IEnumerable<T> GetSnapshot()
        {
            var snapshot = new List<T>();
            using (this.Subscribe(item => snapshot.Add(item)))
            {
                // Deliberately empty; subscribing will add everything to the list.
            }
            return snapshot;
        }

        public void Dispose()
        {
            OnCompleted();
            subjects?.Dispose();
        }
    }
}
