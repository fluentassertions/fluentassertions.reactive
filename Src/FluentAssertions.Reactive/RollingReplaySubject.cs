using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace FluentAssertions.Reactive
{
    /// <summary>
    /// Clearable <see cref="ReplaySubject{T}"/> taken from James World: https://gist.github.com/james-world/c46f09f32e2d4f338b07
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RollingReplaySubject
    {
        protected class NopSubject<TSource> : ISubject<TSource>
        {
            public static readonly NopSubject<TSource> Default = new NopSubject<TSource>();

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }

            public void OnNext(TSource value)
            {
            }

            public IDisposable Subscribe(IObserver<TSource> observer)
            {
                return Disposable.Empty;
            }
        }
    }

    public class RollingReplaySubject<TSource> : RollingReplaySubject, ISubject<TSource>
    {
        private readonly ReplaySubject<IObservable<TSource>> _subjects;
        private readonly IObservable<TSource> _concatenatedSubjects;
        private ISubject<TSource> _currentSubject;
        private readonly object _gate = new object();

        public RollingReplaySubject()
        {
            _subjects = new ReplaySubject<IObservable<TSource>>(1);
            _concatenatedSubjects = _subjects.Concat();
            _currentSubject = new ReplaySubject<TSource>();
            _subjects.OnNext(_currentSubject);
        }
        
        public void Clear()
        {
            lock (_gate)
            {
                _currentSubject.OnCompleted();
                _currentSubject = new ReplaySubject<TSource>();
                _subjects.OnNext(_currentSubject);
            }
        }

        public void OnNext(TSource value)
        {
            lock (_gate)
            {
                _currentSubject.OnNext(value);
            }
        }

        public void OnError(Exception error)
        {
            lock (_gate)
            {
                _currentSubject.OnError(error);
                _currentSubject = NopSubject<TSource>.Default;
            }
        }

        public void OnCompleted()
        {
            lock (_gate)
            {
                _currentSubject.OnCompleted();
                _subjects.OnCompleted();
                _currentSubject = NopSubject<TSource>.Default;
            }
        }

        public IDisposable Subscribe(IObserver<TSource> observer)
        {
            return _concatenatedSubjects.Subscribe(observer);
        }

        public IEnumerable<TSource> GetSnapshot()
        {
            var snapshot = new List<TSource>();
            using (this.Subscribe(item => snapshot.Add(item)))
            {
                // Deliberately empty; subscribing will add everything to the list.
            }
            return snapshot;
        }

        public void Dispose()
        {
            OnCompleted();
            _subjects?.Dispose();
        }
    }
}
