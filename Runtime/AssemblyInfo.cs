using System.Runtime.CompilerServices;

// FastIoC generated injectors live in Assembly-CSharp and assign `internal` fields
// of this assembly directly, without reflection.
[assembly: InternalsVisibleTo("Assembly-CSharp")]

// The tests reach the engine's internal pieces.
[assembly: InternalsVisibleTo("UIScroller.Tests.Editor")]
[assembly: InternalsVisibleTo("UIScroller.Tests.PlayMode")]

// The inspector uses the same Content layout check as the runtime.
[assembly: InternalsVisibleTo("UIScroller.Editor")]
