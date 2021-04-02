# FluentAssertions.Reactive

[![Nuget](https://img.shields.io/nuget/dt/FluentAssertions.Reactive)](https://www.nuget.org/packages/FluentAssertions.Reactive/)
[![Nuget](https://img.shields.io/nuget/vpre/FluentAssertions.Reactive)](https://www.nuget.org/packages/FluentAssertions.Reactive)

Extensions for the [Fluent Assertions](https://www.fluentassertions.com) for testing the behaviour of observables.

## Example Usage

```csharp
var observable = Observable.Empty<Unit>();

// observe the sequence
using var observer = observable.Observe();

// assert the behaviour of the sequence
observer.Should().Complete();

```

More examples can be found in the [unit tests](https://github.com/fluentassertions/fluentassertions.reactive/blob/master/Tests/FluentAssertions.Reactive.Specs/ReactiveAssertionSpecs.cs)


