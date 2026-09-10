namespace Icod.DCurses.Tests;

using Icod.DCurses.Internal;

public sealed class CursesPanelOrderTests {
	[Fact]
	public void AddAppendsPanelsFromBottomToTop() {
		CursesPanelOrder<PanelStub> order = new();
		PanelStub first = new( 1 );
		PanelStub second = new( 2 );
		PanelStub third = new( 3 );

		order.Add( first );
		order.Add( second );
		order.Add( third );

		Assert.Equal(
			[
				first,
				second,
				third
			],
			order.SnapshotBottomToTop()
		);
	}

	[Fact]
	public void AddUsesReferenceIdentityRatherThanValueEquality() {
		CursesPanelOrder<PanelStub> order = new();
		PanelStub first = new( 7 );
		PanelStub equalButDistinct = new( 7 );

		order.Add( first );
		order.Add( equalButDistinct );

		PanelStub[] snapshot = order.SnapshotBottomToTop();
		Assert.Equal( 2, snapshot.Length );
		Assert.Same( first, snapshot[ 0 ] );
		Assert.Same( equalButDistinct, snapshot[ 1 ] );
	}

	[Fact]
	public void AddRejectsDuplicateReference() {
		CursesPanelOrder<PanelStub> order = new();
		PanelStub panel = new( 1 );
		order.Add( panel );

		Assert.Throws<InvalidOperationException>(
			() => order.Add( panel )
		);
	}

	[Fact]
	public void RemoveDeletesOnlyTheRequestedReference() {
		CursesPanelOrder<PanelStub> order = new();
		PanelStub first = new( 1 );
		PanelStub second = new( 1 );
		order.Add( first );
		order.Add( second );

		Assert.True( order.Remove( first ) );
		Assert.False( order.Remove( first ) );

		PanelStub remaining = Assert.Single( order.SnapshotBottomToTop() );
		Assert.Same( second, remaining );
	}

	[Fact]
	public void MoveToTopAndBottomPreserveRelativeOrderOfOtherPanels() {
		CursesPanelOrder<PanelStub> order = CreateFourPanelOrder(
			out PanelStub first,
			out PanelStub second,
			out PanelStub third,
			out PanelStub fourth
		);

		order.MoveToTop( second );
		Assert.Equal(
			[
				first,
				third,
				fourth,
				second
			],
			order.SnapshotBottomToTop()
		);

		order.MoveToBottom( fourth );
		Assert.Equal(
			[
				fourth,
				first,
				third,
				second
			],
			order.SnapshotBottomToTop()
		);
	}

	[Fact]
	public void MoveAbovePlacesPanelImmediatelyAboveSibling() {
		CursesPanelOrder<PanelStub> order = CreateFourPanelOrder(
			out PanelStub first,
			out PanelStub second,
			out PanelStub third,
			out PanelStub fourth
		);

		order.MoveAbove(
			first,
			third
		);

		Assert.Equal(
			[
				second,
				third,
				first,
				fourth
			],
			order.SnapshotBottomToTop()
		);
	}

	[Fact]
	public void MoveBelowPlacesPanelImmediatelyBelowSibling() {
		CursesPanelOrder<PanelStub> order = CreateFourPanelOrder(
			out PanelStub first,
			out PanelStub second,
			out PanelStub third,
			out PanelStub fourth
		);

		order.MoveBelow(
			fourth,
			second
		);

		Assert.Equal(
			[
				first,
				fourth,
				second,
				third
			],
			order.SnapshotBottomToTop()
		);
	}

	[Fact]
	public void MovementRejectsUnknownAndSelfRelativePanels() {
		CursesPanelOrder<PanelStub> order = new();
		PanelStub member = new( 1 );
		PanelStub unknown = new( 2 );
		order.Add( member );

		Assert.Throws<InvalidOperationException>(
			() => order.MoveToTop( unknown )
		);
		Assert.Throws<InvalidOperationException>(
			() => order.MoveToBottom( unknown )
		);
		Assert.Throws<InvalidOperationException>(
			() => order.MoveAbove(
				member,
				unknown
			)
		);
		Assert.Throws<InvalidOperationException>(
			() => order.MoveBelow(
				unknown,
				member
			)
		);
		Assert.Throws<ArgumentException>(
			() => order.MoveAbove(
				member,
				member
			)
		);
		Assert.Throws<ArgumentException>(
			() => order.MoveBelow(
				member,
				member
			)
		);
	}

	private static CursesPanelOrder<PanelStub> CreateFourPanelOrder(
		out PanelStub first,
		out PanelStub second,
		out PanelStub third,
		out PanelStub fourth
	) {
		first = new PanelStub( 1 );
		second = new PanelStub( 2 );
		third = new PanelStub( 3 );
		fourth = new PanelStub( 4 );

		CursesPanelOrder<PanelStub> order = new();
		order.Add( first );
		order.Add( second );
		order.Add( third );
		order.Add( fourth );
		return order;
	}

	private sealed record PanelStub( int Value );
}
