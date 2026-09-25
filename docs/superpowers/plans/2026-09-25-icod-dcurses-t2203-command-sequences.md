# Icod.DCurses T2203 Bounded Command Sequences Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Work inline; do not dispatch subagents.

**Goal:** Add opt-in, bounded multi-key command composition to the existing interaction router without changing direct `Route(CursesInputEvent)` behavior.

**Architecture:** The router owns one bounded pending-prefix state. Regions, scopes and the router global owner store immutable sequence registrations beside their existing single-key dictionaries. `ProcessCommandSequence` selects the first eligible owner with a matching single binding or sequence prefix, retains only that owner's candidates, and returns immutable pending/completed/mismatch/fallback data without executing commands.

**Tech Stack:** C# 13; .NET 8, 9 and 10; xUnit; Staging PR CI on Windows/Linux/macOS x64/ARM64. No Python. The local workspace has no `dotnet`, so exact-head PR CI supplies observed RED and GREEN build/test evidence.

**Spec:** `docs/superpowers/specs/2026-09-25-icod-dcurses-t2203-command-sequences-design.md`

## Global Constraints

- Preserve every published 2.1 API and keep `AssemblyVersion` at `2.0.0.0`.
- Keep `Version` and `PackageVersion` at `2.2.0-alpha.1` during T2203.
- Keep `Icod.Terminal 1.18.0` as the sole direct production dependency; never add direct TermInfo or protocol access.
- Keep direct `Route(CursesInputEvent)` behavior and its ordinary matching path unchanged.
- Sequence lengths are 2–8; per-region/per-scope/global limits are 128/128/512; router total is 4,096.
- Applications retain event-loop, timeout, cancellation-policy, drawing and command-execution ownership.
- All stored gesture lists and all returned lists are detached read-only copies.
- Use only C#, PowerShell 5.1-compatible PowerShell, cmd and sh.
- PR checks run Staging only. Release validation is reserved for an authorized push to `main`.
- Do not merge, tag, publish or create a NuGet release as part of this plan.

## Review Focus

1. **Caller mutates a source list after binding:** the registered sequence and discovery snapshot remain unchanged (Task 2, `BindingAndRegistrationCopyGestureLists`).
2. **Single-key/prefix conflict is registered in either order:** the second operation fails without partial mutation (Task 2, `SameOwnerSingleAndSequenceConflictInBothOrders`).
3. **A mismatch is also another sequence's first gesture:** it is routed once as ordinary fallback and does not silently start another sequence (Task 3, `MismatchFallsBackOnceWithoutRestart`).
4. **Focus or modal context changes while pending:** the prefix is cleared before any later input can complete it (Task 4, `RoutingContextChangesInvalidatePendingSequence`).
5. **Text/key normalization differs:** Unicode text, Space key equivalence, modifiers, phases and function-key numbers use `CursesKeyGesture.Matches` semantics (Task 3, `SequenceMatchingUsesSemanticGestures`).

---

## File structure

### New production files

- `src/CursesCommandSequenceBinding.cs` — public detached immutable binding snapshot.
- `src/CursesCommandSequenceResultKind.cs` — four public processing outcomes.
- `src/CursesCommandSequenceResult.cs` — public immutable result with internal factories.
- `src/CursesCommandSequenceRegistration.cs` — internal copied registration plus equality/prefix helpers.
- `src/CursesInteractionRegion.Sequences.cs` — region storage and bind/unbind operations.
- `src/CursesInteractionScope.Sequences.cs` — scope storage and bind/unbind operations.
- `src/CursesInteractionRouter.Sequences.cs` — global storage, pending state, processing and capacity.
- `src/CursesInteractionRouter.SequenceDiscovery.cs` — deterministic effective-sequence snapshots.

### Modified production files

- `src/CursesInteractionRegion.Bindings.cs` — reject first-gesture conflicts and invalidate pending state after mutation.
- `src/CursesInteractionScope.Bindings.cs` — same scope behavior.
- `src/CursesInteractionRouter.Routing.cs` — same global behavior; ordinary `Route` remains otherwise unchanged.
- `src/CursesInteractionRouter.cs` — publish limits/property and invalidate on focus, eligibility, resize, region removal and disposal.
- `src/CursesInteractionRouter.Scopes.cs` — invalidate on activation/deactivation and scope removal.
- `Icod.DCurses.csproj` — expand accurate alpha release notes without changing versions or dependencies.

### New and modified tests/evidence

- `tests/Icod.DCurses.Tests/src/CursesCommandSequenceContractTests.cs`
- `tests/Icod.DCurses.Tests/src/CursesCommandSequenceRoutingTests.cs`
- `tests/Icod.DCurses.Tests/src/CursesCommandSequenceContextTests.cs`
- `tests/Icod.DCurses.Tests/src/CursesCommandSequenceDiscoveryTests.cs`
- `tests/Icod.DCurses.Tests/src/PublicApiFingerprintTests.cs`
- `tests/Icod.DCurses.Tests/src/PublicTwoTwoDevelopmentIdentityTests.cs`
- `docs/Public-API-Fingerprint-2.2.json`
- `docs/Public-API-Baseline-2.2.md`
- `docs/T2203-Bounded-Command-Sequences-Gate.md`
- `docs/2.2-Interaction-API-Design.md`
- `Icod.DCurses-2.2.0-Development-Roadmap.md`
- `Icod.DCurses-Development-Roadmap.md`

---

### Task 1: Frozen public-contract RED

**Files:**
- Create: `tests/Icod.DCurses.Tests/src/CursesCommandSequenceContractTests.cs`
- Modify after CI: `docs/superpowers/plans/2026-09-25-icod-dcurses-t2203-command-sequences.md`

**Interfaces:**
- Consumes: the approved T2203 specification and existing `CursesKeyGesture`, `CursesCommand`, `CursesInteractionRegion`, `CursesInteractionScope`, `CursesInteractionRouter`.
- Produces: compile-time use of every frozen public type, constant, property and method for Tasks 2–5.

- [x] **Step 1: Add the GPL test header, `using System.Text;`, `using Xunit;`, namespace and helpers.**

```csharp
private static CursesKeyGesture Character( char value ) {
	return CursesKeyGesture.ForCharacter( new Rune( value ) );
}

private static CursesInteractionRegion RegisterFocusable(
	CursesInteractionRouter router
) {
	return router.RegisterRegion(
		new CursesInteractionRegionOptions(
			new CursesRectangle( 0, 0, 4, 4 )
		) {
			IsFocusable = true
		}
	);
}
```

- [x] **Step 2: Add one test that references the complete frozen surface.**

```csharp
[Fact]
public void FrozenSequenceSurfaceIsAvailable() {
	CursesKeyGesture[] gestures = [ Character( 'g' ), Character( 'g' ) ];
	CursesCommand command = new( "go-top" );
	CursesCommandSequenceBinding binding = new( gestures, command );
	Assert.Equal( 8, CursesInteractionRouter.MaximumCommandSequenceLength );
	Assert.Equal( 128, CursesInteractionRouter.MaximumRegionCommandSequenceBindings );
	Assert.Equal( 128, CursesInteractionRouter.MaximumScopeCommandSequenceBindings );
	Assert.Equal( 512, CursesInteractionRouter.MaximumGlobalCommandSequenceBindings );
	Assert.Equal( 4096, CursesInteractionRouter.MaximumCommandSequenceBindings );
	Assert.Equal( gestures, binding.Gestures.ToArray() );
	Assert.Same( command, binding.Command );
	Assert.Equal( 0, (int)CursesCommandSequenceResultKind.Fallback );
	Assert.Equal( 1, (int)CursesCommandSequenceResultKind.Pending );
	Assert.Equal( 2, (int)CursesCommandSequenceResultKind.Completed );
	Assert.Equal( 3, (int)CursesCommandSequenceResultKind.Mismatch );

	CursesScreen screen = new( 20, 10 );
	using CursesInteractionRouter router = new( screen );
	using CursesInteractionRegion region = RegisterFocusable( router );
	using CursesInteractionScope scope = router.RegisterScope();
	region.BindGestureSequence( gestures, command );
	Assert.True( region.UnbindGestureSequence( gestures ) );
	scope.BindGestureSequence( gestures, command );
	Assert.True( scope.UnbindGestureSequence( gestures ) );
	router.BindGlobalGestureSequence( gestures, command );
	Assert.False( router.HasPendingCommandSequence );
	Assert.Single( router.GetEffectiveGestureSequenceBindings() );
	CursesInputEvent input = CursesInputEvent.FromText( new Rune( 'g' ) );
	CursesCommandSequenceResult result = router.ProcessCommandSequence( input );
	Assert.Equal( CursesCommandSequenceResultKind.Pending, result.Kind );
	Assert.Same( input, result.Input );
	Assert.Single( result.MatchedGestures );
	Assert.Null( result.Command );
	Assert.Null( result.Fallback );
	Assert.True( router.CancelPendingCommandSequence() );
	Assert.True( router.UnbindGlobalGestureSequence( gestures ) );
}
```

- [x] **Step 3: Perform local static verification.** `git diff --check` passed and the targeted `rg` inventory found every frozen type/limit family. The workspace still has no `dotnet` executable.

Run:

```sh
git diff --check
rg -n "CursesCommandSequence|Maximum.*CommandSequence" tests/Icod.DCurses.Tests/src/CursesCommandSequenceContractTests.cs
```

Expected: no whitespace errors; every frozen public name appears.

- [ ] **Step 4: Commit and push the tests-only RED checkpoint.**

```sh
git add tests/Icod.DCurses.Tests/src/CursesCommandSequenceContractTests.cs docs/superpowers/plans/2026-09-25-icod-dcurses-t2203-command-sequences.md
git commit -m "test: freeze T2203 command sequence contract"
```

Push the exact tree to `2.2.0-roadmap` using the configured repository connector.

- [ ] **Step 5: Observe the expected Staging failure and record exact evidence.**

Expected: compile failure on .NET 8/9/10 for missing `CursesCommandSequenceBinding`, `CursesCommandSequenceResultKind`, sequence constants and owner/router methods. Record workflow, job, exact head and compiler identifiers in this plan and the PR body. A failure for any unrelated reason blocks Task 2 and is diagnosed first.

---

### Task 2: Immutable values, registration rules and bounded storage

**Files:**
- Create: `src/CursesCommandSequenceBinding.cs`
- Create: `src/CursesCommandSequenceResultKind.cs`
- Create: `src/CursesCommandSequenceResult.cs`
- Create: `src/CursesCommandSequenceRegistration.cs`
- Create: `src/CursesInteractionRegion.Sequences.cs`
- Create: `src/CursesInteractionScope.Sequences.cs`
- Create: `src/CursesInteractionRouter.Sequences.cs`
- Modify: `src/CursesInteractionRegion.Bindings.cs`
- Modify: `src/CursesInteractionScope.Bindings.cs`
- Modify: `src/CursesInteractionRouter.Routing.cs`
- Modify: `src/CursesInteractionRouter.cs`
- Test: `tests/Icod.DCurses.Tests/src/CursesCommandSequenceContractTests.cs`

**Interfaces:**
- Consumes: `CursesKeyGesture.IsBindable`, `CursesKeyGesture.Matches`, existing owner disposal checks and total-binding validation pattern.
- Produces: copied internal `CursesCommandSequenceRegistration` values; all frozen public members; minimal correct fallback processing for non-prefix input; owner enumeration used by Tasks 3 and 5.

- [ ] **Step 1: Add failing validation, copy and conflict tests.**

```csharp
[Fact]
public void BindingAndRegistrationCopyGestureLists() {
	CursesKeyGesture[] source = [ Character( 'g' ), Character( 'g' ) ];
	CursesCommand command = new( "go-top" );
	CursesCommandSequenceBinding binding = new( source, command );
	CursesScreen screen = new( 20, 10 );
	using CursesInteractionRouter router = new( screen );
	router.BindGlobalGestureSequence( source, command );
	source[0] = Character( 'x' );
	Assert.Equal( Character( 'g' ), binding.Gestures[0] );
	Assert.Equal(
		Character( 'g' ),
		router.GetEffectiveGestureSequenceBindings()[0].Gestures[0]
	);
	Assert.Throws<NotSupportedException>(
		() => ((IList<CursesKeyGesture>)binding.Gestures).Clear()
	);
}

[Fact]
public void SameOwnerSingleAndSequenceConflictInBothOrders() {
	CursesKeyGesture g = Character( 'g' );
	CursesKeyGesture[] sequence = [ g, Character( 'd' ) ];
	CursesScreen screen = new( 20, 10 );
	using CursesInteractionRouter first = new( screen );
	first.BindGlobalGesture( g, new CursesCommand( "single" ) );
	Assert.Throws<InvalidOperationException>(
		() => first.BindGlobalGestureSequence( sequence, new CursesCommand( "sequence" ) )
	);
	Assert.Empty( first.GetEffectiveGestureSequenceBindings() );

	using CursesInteractionRouter second = new( screen );
	second.BindGlobalGestureSequence( sequence, new CursesCommand( "sequence" ) );
	Assert.Throws<InvalidOperationException>(
		() => second.BindGlobalGesture( g, new CursesCommand( "single" ) )
	);
	Assert.Single( second.GetEffectiveGestureSequenceBindings() );
}
```

Also add:

- `BindingRejectsNullShortLongAndDefaultGestures`;
- `DuplicateAndProperPrefixConflictsDoNotMutateOwner`;
- `SharedNonterminalPrefixIsAllowed`;
- `UnbindUsesExactSequenceAndInvalidArgumentsDoNotMutate`;
- `PerOwnerAndRouterSequenceCapacitiesFailBeforeMutation`.

Use 32 regions × 128 registrations to reach the 4,096 total boundary; reuse gesture sequences across different owners and generate distinct two-rune sequences within each owner.

- [ ] **Step 2: Confirm the registration tests are part of the observed Task 1 RED before production changes.**

Add these tests to the tests-only head before beginning Step 3. The expected
compiler failures remain the missing sequence constructors and registration
members. Do not start a redundant workflow solely to distinguish each missing
member after the exact-head contract RED has established that no production
surface exists.

- [ ] **Step 3: Implement copied public values and internal registration helpers.**

```csharp
internal sealed class CursesCommandSequenceRegistration {
	internal CursesCommandSequenceRegistration(
		IReadOnlyList<CursesKeyGesture> gestures,
		CursesCommand command
	) {
		this.Gestures = ValidateAndCopy( gestures );
		this.Command = command
			?? throw new ArgumentNullException( nameof( command ) );
	}

	internal CursesKeyGesture[] Gestures { get; }
	internal CursesCommand Command { get; }

	internal static CursesKeyGesture[] ValidateAndCopy(
		IReadOnlyList<CursesKeyGesture> gestures
	) {
		ArgumentNullException.ThrowIfNull( gestures );
		if ( gestures.Count is < 2
			or > CursesInteractionRouter.MaximumCommandSequenceLength ) {
			throw new ArgumentOutOfRangeException( nameof( gestures ) );
		}
		CursesKeyGesture[] copy = new CursesKeyGesture[gestures.Count];
		for ( int index = 0; index < copy.Length; ++index ) {
			if ( !gestures[index].IsBindable ) {
				throw new ArgumentException(
					"Every sequence gesture must be created by a CursesKeyGesture factory.",
					nameof( gestures )
				);
			}
			copy[index] = gestures[index];
		}
		return copy;
	}
}
```

`CursesCommandSequenceBinding` copies through the same validation rule and exposes `Array.AsReadOnly(copy)`. `CursesCommandSequenceResult` uses private construction plus internal `FallbackResult`, `PendingResult`, `CompletedResult` and `MismatchResult` factories that enforce the property-nullability matrix.

- [ ] **Step 4: Implement per-owner storage and registration-order-independent checks.**

Each owner stores:

```csharp
private readonly List<CursesCommandSequenceRegistration> commandSequences = [];
internal int CommandSequenceCount => this.commandSequences.Count;
internal IReadOnlyList<CursesCommandSequenceRegistration> CommandSequences =>
	this.commandSequences;
```

Before mutation, compare exact sequences, proper prefixes in both directions and the first gesture against the owner's single-key dictionary. Call `owner.EnsureCommandSequenceCapacity()` only after argument/conflict validation and before insertion. After successful bind or unbind, call `owner.ClearPendingCommandSequence()`.

- [ ] **Step 5: Add the five public limits and total-capacity calculation.**

`EnsureCommandSequenceCapacity()` sums global, live region and live scope counts using checked bounded addition and rejects when the existing count is already 4,096. It does not share counters with `MaximumGestureBindings`.

- [ ] **Step 6: Supply minimal correct router behavior required by the frozen surface.**

Until Task 3 adds sequence matching:

```csharp
public CursesCommandSequenceResult ProcessCommandSequence(
	CursesInputEvent input
) {
	ArgumentNullException.ThrowIfNull( input );
	this.ThrowIfDisposed();
	return CursesCommandSequenceResult.FallbackResult(
		input,
		this.Route( input )
	);
}
```

`HasPendingCommandSequence` is false, `CancelPendingCommandSequence()` returns false, and the initial discovery implementation returns detached registered global sequences so Task 2 copy/conflict tests are observable. Task 3 replaces the minimal processing behavior; Task 5 completes precedence-aware discovery.

- [ ] **Step 7: Run local static verification and commit.**

```sh
git diff --check
rg -n "Maximum(Command|RegionCommand|ScopeCommand|GlobalCommand)Sequence" src tests/Icod.DCurses.Tests/src
rg -n "Bind.*GestureSequence|Unbind.*GestureSequence" src tests/Icod.DCurses.Tests/src
git add src tests/Icod.DCurses.Tests/src/CursesCommandSequenceContractTests.cs
git commit -m "feat: add bounded command sequence registrations"
```

- [ ] **Step 8: Push and observe GREEN on the registration batch.**

All Staging jobs must compile on .NET 8/9/10 and pass the new copy, validation, prefix-conflict and capacity tests. Record the exact head and workflow. The direct `Route` regression suite must remain green.

---

### Task 3: Processing state machine, precedence and mismatch replay

**Files:**
- Modify: `src/CursesInteractionRouter.Sequences.cs`
- Modify: `src/CursesCommandSequenceRegistration.cs`
- Modify: `src/CursesInteractionRegion.Sequences.cs`
- Modify: `src/CursesInteractionScope.Sequences.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesCommandSequenceRoutingTests.cs`

**Interfaces:**
- Consumes: copied registrations from Task 2, existing owner order, `CursesKeyGesture.Matches(CursesInputEvent)`, `Route(CursesInputEvent)`.
- Produces: real `ProcessCommandSequence`, bounded pending candidates, exactly-once mismatch fallback and explicit cancellation.

- [ ] **Step 0: Create the test file with the GPL header, `using System.Text;`, `using Xunit;`, and the exact `Character`/`RegisterFocusable` helpers from Task 1.**

- [ ] **Step 1: Add failing pending/completion and precedence tests.**

```csharp
[Fact]
public void RegionSequenceWinsOverLowerGlobalSingle() {
	CursesScreen screen = new( 20, 10 );
	using CursesInteractionRouter router = new( screen );
	using CursesInteractionRegion region = RegisterFocusable( router );
	CursesKeyGesture g = Character( 'g' );
	CursesCommand command = new( "go-top" );
	region.BindGestureSequence( [ g, g ], command );
	router.BindGlobalGesture( g, new CursesCommand( "global-single" ) );
	Assert.True( router.Focus( region ) );

	CursesCommandSequenceResult pending = router.ProcessCommandSequence(
		CursesInputEvent.FromText( new Rune( 'g' ) )
	);
	Assert.Equal( CursesCommandSequenceResultKind.Pending, pending.Kind );
	Assert.True( router.HasPendingCommandSequence );
	CursesCommandSequenceResult completed = router.ProcessCommandSequence(
		CursesInputEvent.FromText( new Rune( 'g' ) )
	);

	Assert.Equal( CursesCommandSequenceResultKind.Completed, completed.Kind );
	Assert.Same( command, completed.Command );
	Assert.Null( completed.Fallback );
	Assert.False( router.HasPendingCommandSequence );
}

[Fact]
public void MismatchFallsBackOnceWithoutRestart() {
	CursesScreen screen = new( 20, 10 );
	using CursesInteractionRouter router = new( screen );
	CursesKeyGesture g = Character( 'g' );
	CursesKeyGesture x = Character( 'x' );
	router.BindGlobalGestureSequence( [ g, g ], new CursesCommand( "go-top" ) );
	router.BindGlobalGestureSequence( [ x, x ], new CursesCommand( "other-sequence" ) );
	CursesCommand fallback = new( "ordinary-x" );
	using CursesInteractionScope scope = router.RegisterScope();
	scope.BindGesture( x, fallback );
	using CursesInteractionScopeLease lease = router.ActivateScope( scope );
	Assert.Equal(
		CursesCommandSequenceResultKind.Pending,
		router.ProcessCommandSequence( CursesInputEvent.FromText( new Rune( 'g' ) ) ).Kind
	);

	CursesCommandSequenceResult mismatch = router.ProcessCommandSequence(
		CursesInputEvent.FromText( new Rune( 'x' ) )
	);

	Assert.Equal( CursesCommandSequenceResultKind.Mismatch, mismatch.Kind );
	Assert.Single( mismatch.MatchedGestures );
	Assert.Same( fallback, mismatch.Fallback!.Command );
	Assert.False( router.HasPendingCommandSequence );
}
```

Add `HigherSingleBindingPreventsLowerSequenceStart`, `SharedPrefixSelectsTheCompletedCommand`, `NonKeyboardInputFallsBackOrMismatches`, and `CancelIsExplicitAndIdempotent`.

- [ ] **Step 2: Add failing semantic-gesture coverage.**

`SequenceMatchingUsesSemanticGestures` registers and completes separate sequences that cover:

- `ForCharacter(new Rune(' '))` matched by a normalized `CursesKey.Space` key event;
- a non-ASCII rune delivered as text;
- Control+character;
- Release-phase named key;
- numbered function key.

Use the internal test-accessible `CursesInputEvent.FromText` and `FromKey` factories already used by routing tests.

- [ ] **Step 3: Push the routing RED checkpoint.**

Expected: assertions fail because Task 2 always returns `Fallback`; compilation remains green. Record the exact failed test names and head.

- [ ] **Step 4: Implement owner classification and pending state.**

Store only bounded state:

```csharp
private object? pendingCommandSequenceOwner;
private CursesKeyGesture[] pendingCommandSequenceGestures = [];
private CursesCommandSequenceRegistration[] pendingCommandSequenceCandidates = [];
```

At the first keyboard event, enumerate focused region, eligible scope chain and global owner. For each owner, check its single-key bindings and sequences before advancing to the next owner. Use `gesture.Matches(input)`, never reconstruct a gesture from the event. When a sequence owner wins, copy its matching candidate references and the first registered gesture into pending state.

- [ ] **Step 5: Implement extension, completion and mismatch.**

```csharp
private CursesCommandSequenceResult ContinuePendingCommandSequence(
	CursesInputEvent input
) {
	CursesKeyGesture[] abandoned = this.pendingCommandSequenceGestures;
	List<CursesCommandSequenceRegistration> matches = [];
	CursesCommandSequenceRegistration? completed = null;
	foreach ( CursesCommandSequenceRegistration candidate
		in this.pendingCommandSequenceCandidates ) {
		if ( candidate.Gestures.Length <= abandoned.Length
			|| !candidate.Gestures[abandoned.Length].Matches( input ) ) {
			continue;
		}
		matches.Add( candidate );
		if ( candidate.Gestures.Length == abandoned.Length + 1 ) {
			completed = candidate;
		}
	}
	if ( 0 == matches.Count ) {
		this.ClearPendingCommandSequence();
		return CursesCommandSequenceResult.MismatchResult(
			input,
			abandoned,
			this.Route( input )
		);
	}
	CursesKeyGesture[] extended = [
		.. abandoned,
		matches[0].Gestures[abandoned.Length]
	];
	if ( completed is not null ) {
		this.ClearPendingCommandSequence();
		return CursesCommandSequenceResult.CompletedResult(
			input,
			extended,
			completed.Command
		);
	}
	this.SetPendingCommandSequence( extended, matches.ToArray() );
	return CursesCommandSequenceResult.PendingResult( input, extended );
}
```

`MismatchResult` calls `Route(input)` once and never feeds `input` back into sequence-start classification.

- [ ] **Step 6: Verify and commit the state machine.**

```sh
git diff --check
rg -n "ProcessCommandSequence|ContinuePending|MismatchResult|CancelPending" src
git add src tests/Icod.DCurses.Tests/src/CursesCommandSequenceRoutingTests.cs
git commit -m "feat: process bounded command sequences"
```

- [ ] **Step 7: Push and observe GREEN on routing.**

Require all new routing tests and the complete existing direct-routing suite to pass on all three TFMs. Record exact head/workflow and confirm no test relies on a timer or callback.

---

### Task 4: Routing-context invalidation

**Files:**
- Modify: `src/CursesInteractionRouter.cs`
- Modify: `src/CursesInteractionRouter.Scopes.cs`
- Modify: `src/CursesInteractionRouter.Routing.cs`
- Modify: `src/CursesInteractionRegion.Bindings.cs`
- Modify: `src/CursesInteractionScope.Bindings.cs`
- Modify: `src/CursesInteractionRegion.Sequences.cs`
- Modify: `src/CursesInteractionScope.Sequences.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesCommandSequenceContextTests.cs`

**Interfaces:**
- Consumes: `ClearPendingCommandSequence()` from Task 3 and all existing routing-context mutation points.
- Produces: immediate invalidation on every spec-listed context mutation without synthesizing results.

- [ ] **Step 0: Create the test file with the GPL header, `using System.Text;`, `using Xunit;`, and a local `Character(char)` helper.**

- [ ] **Step 1: Add a failing focus/scope invalidation theory.**

```csharp
[Fact]
public void RoutingContextChangesInvalidatePendingSequence() {
	CursesScreen screen = new( 20, 10 );
	using CursesInteractionRouter router = new( screen );
	using CursesInteractionRegion first = router.RegisterRegion(
		new CursesInteractionRegionOptions(
			new CursesRectangle( 0, 0, 4, 4 )
		) {
			IsFocusable = true
		}
	);
	using CursesInteractionRegion second = router.RegisterRegion(
		new CursesInteractionRegionOptions(
			new CursesRectangle( 5, 0, 4, 4 )
		) {
			IsFocusable = true
		}
	);
	first.BindGestureSequence(
		[ Character( 'g' ), Character( 'g' ) ],
		new CursesCommand( "go-top" )
	);
	Assert.True( router.Focus( first ) );
	Assert.Equal(
		CursesCommandSequenceResultKind.Pending,
		router.ProcessCommandSequence( CursesInputEvent.FromText( new Rune( 'g' ) ) ).Kind
	);
	Assert.True( router.HasPendingCommandSequence );

	Assert.True( router.Focus( second ) );

	Assert.False( router.HasPendingCommandSequence );
	Assert.NotEqual(
		CursesCommandSequenceResultKind.Completed,
		router.ProcessCommandSequence( CursesInputEvent.FromText( new Rune( 'g' ) ) ).Kind
	);
}
```

Add focused cases for `ClearFocus`, `MoveFocus`, scope activation/deactivation, region disable, panel eligibility change, screen resize, single bind/unbind, sequence bind/unbind and region disposal.

- [ ] **Step 2: Add lazy repair and disposal tests.**

`LazyFocusRepairClearsPendingBeforeProcessing` makes the focused region ineligible without an explicit focus call, then verifies `ProcessCommandSequence` cannot complete the old prefix. `DisposedRouterRejectsSequenceMembers` verifies the property, process, cancel, global bind/unbind and discovery query throw `ObjectDisposedException`.

- [ ] **Step 3: Push the context RED checkpoint.**

Expected: at least focus/scope/eligibility mutation tests fail because Task 3 retains pending state. Record exact failed tests and head.

- [ ] **Step 4: Centralize idempotent invalidation.**

```csharp
internal void ClearPendingCommandSequence() {
	this.pendingCommandSequenceOwner = null;
	this.pendingCommandSequenceGestures = [];
	this.pendingCommandSequenceCandidates = [];
}
```

Call it only after successful binding mutations, before or immediately after successful focus/scope context mutations, on eligibility notifications and resize, and at the beginning of router disposal. Failed validation, duplicate binds and missing unbinds must not clear pending state.

- [ ] **Step 5: Make lazy focus repair observable to invalidation.**

In `RepairFocusIfNeeded`, retain the original focused-region reference. Clear pending state only when repair actually changes that reference. Preserve current traversal selection exactly.

- [ ] **Step 6: Verify and commit invalidation.**

```sh
git diff --check
rg -n "ClearPendingCommandSequence" src/CursesInteractionRouter*.cs src/CursesInteractionRegion*.cs src/CursesInteractionScope*.cs
git add src tests/Icod.DCurses.Tests/src/CursesCommandSequenceContextTests.cs
git commit -m "fix: invalidate command prefixes with routing context"
```

- [ ] **Step 7: Push and observe GREEN on context behavior.**

Require all context tests plus existing focus, scope, panel, resize and disposal suites to pass on .NET 8/9/10.

---

### Task 5: Effective sequence discovery, API fingerprint and T2203 gate

**Files:**
- Create: `src/CursesInteractionRouter.SequenceDiscovery.cs`
- Modify: `src/CursesInteractionRouter.Sequences.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesCommandSequenceDiscoveryTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/PublicApiFingerprintTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/PublicTwoTwoDevelopmentIdentityTests.cs`
- Modify: `docs/Public-API-Fingerprint-2.2.json`
- Modify: `docs/Public-API-Baseline-2.2.md`
- Modify: `Icod.DCurses.csproj`
- Create: `docs/T2203-Bounded-Command-Sequences-Gate.md`
- Modify: `docs/2.2-Interaction-API-Design.md`
- Modify: `Icod.DCurses-2.2.0-Development-Roadmap.md`
- Modify: `Icod.DCurses-Development-Roadmap.md`

**Interfaces:**
- Consumes: all owner registrations, union first-gesture precedence and semantic gesture ordering from Tasks 2–4.
- Produces: complete `GetEffectiveGestureSequenceBindings()`, measured additive API baseline, package notes and exact-head acceptance evidence.

- [ ] **Step 0: Create the discovery test file with the GPL header, `using System.Text;`, `using Xunit;`, and the exact `Character`/`RegisterFocusable` helpers from Task 1.**

- [ ] **Step 1: Add failing precedence, ordering and detachment tests.**

```csharp
[Fact]
public void DiscoveryUsesFirstOwnerAndSingleBindingPrecedence() {
	CursesScreen screen = new( 20, 10 );
	using CursesInteractionRouter router = new( screen );
	using CursesInteractionRegion region = RegisterFocusable( router );
	CursesKeyGesture g = Character( 'g' );
	CursesCommand local = new( "local-sequence" );
	region.BindGestureSequence( [ g, Character( 'd' ) ], local );
	router.BindGlobalGesture( g, new CursesCommand( "global-single" ) );
	router.BindGlobalGestureSequence(
		[ Character( 'x' ), Character( 'x' ) ],
		new CursesCommand( "global-sequence" )
	);
	Assert.True( router.Focus( region ) );

	IReadOnlyList<CursesCommandSequenceBinding> bindings =
		router.GetEffectiveGestureSequenceBindings();

	Assert.Equal(
		new[] { "local-sequence", "global-sequence" },
		bindings.Select( binding => binding.Command.Name )
	);
}
```

Also add:

- `HigherSingleBindingHidesLowerSequence`;
- `ModalScopeHidesAncestorSequences`;
- `DiscoveryOrderIsOwnerThenLexicographicAndIndependentOfRegistrationOrder`;
- `SnapshotAndNestedGestureListsAreReadOnlyAndDetached`;
- `DiscoveryIsEmptyWithoutSequencesAndThrowsAfterDispose`.

- [ ] **Step 2: Implement precedence-aware deterministic discovery.**

Track claimed first gestures while enumerating owners. For each owner, first claim all of its matching single-key gestures, then emit and claim its sequence first gestures. When multiple sequences in the same owner share a first gesture, emit all of them. Sort that owner's registrations lexicographically using the same key tuple as T2202 for each gesture:

```csharp
( gesture.Key,
  gesture.Character?.Value ?? -1,
  gesture.Modifiers,
  gesture.Phase,
  gesture.FunctionKeyNumber ?? -1 )
```

Use sequence length only after all shared gesture positions compare equal. Construct a new `CursesCommandSequenceBinding` for every emitted entry and return `Array.AsReadOnly(entries.ToArray())`.

- [ ] **Step 3: Push the discovery implementation with the old fingerprint intact.**

Expected: functional sequence tests pass; `PublicApiMatchesCurrentDevelopmentFingerprint` fails and prints the compiler-derived SHA-256, exported type count, contract line count and exact exported types on every TFM. Treat any other failure as a product defect.

- [ ] **Step 4: Update the 2.2 development fingerprint only from matching CI evidence.**

Require .NET 8/9/10 to report identical fingerprint values. Update `docs/Public-API-Fingerprint-2.2.json` with those measured values. Keep `docs/Public-API-Fingerprint-2.1.json` byte-for-byte unchanged and retain `PublishedTwoOneExportedTypesRemainPresent`.

- [ ] **Step 5: Update identity and package notes without version churn.**

Keep `Version`/`PackageVersion` `2.2.0-alpha.1`, `AssemblyVersion` `2.0.0.0` and `Icod.Terminal 1.18.0`. Expand `PackageReleaseNotes` to mention both implemented binding discovery and bounded opt-in command sequences, and update `DevelopmentIdentityAndNotesDescribeImplementedDiscovery` so it requires both phrases.

- [ ] **Step 6: Run static and repository-boundary checks, then commit.**

```sh
git diff --check
git diff --exit-code origin/main -- docs/Public-API-Fingerprint-2.1.json
rg -n "2.2.0-alpha.1|2.0.0.0|Icod.Terminal.*1.18.0" Icod.DCurses.csproj tests/Icod.DCurses.Tests/src
rg -n "Icod.TermInfo" src Icod.DCurses.csproj
git add src tests docs Icod.DCurses.csproj Icod.DCurses-2.2.0-Development-Roadmap.md Icod.DCurses-Development-Roadmap.md
git commit -m "feat: complete bounded command sequence discovery"
```

Expected: no direct production TermInfo match and no change to the published 2.1 fingerprint.

- [ ] **Step 7: Push and qualify the exact executable head.**

Require all seven Staging jobs green. Record per-TFM test counts from representative Windows, Linux and macOS jobs, confirm macOS x64 remains sequential, and record the exact commit/workflow.

- [ ] **Step 8: Qualify the package artifact.**

Record artifact ID and digest. Confirm the candidate contains `Icod.DCurses.2.2.0-alpha.1.nupkg` and symbols, fresh package-only consumers build/run on .NET 8/9/10, the live Linux pseudo-terminal refresh passes, and the nuspec has only direct `Icod.Terminal 1.18.0`.

- [ ] **Step 9: Record the accepted T2203 gate.**

Write `docs/T2203-Bounded-Command-Sequences-Gate.md` with RED heads, GREEN exact head, workflows, fingerprint delta, package provenance, residual T2204/T2205 deferrals and no-merge/no-publication status. Update both roadmaps and the PR body. This documentation-only checkpoint does not require waiting for a redundant workflow before continuing to the next approved tranche.

- [ ] **Step 10: Commit and push the gate documentation.**

```sh
git diff --check
git add docs Icod.DCurses-2.2.0-Development-Roadmap.md Icod.DCurses-Development-Roadmap.md
git commit -m "docs: accept T2203 command sequence gate"
```

Keep PR #34 draft and unmerged.
