using System.Linq;
using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.HuttballService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Huttball follows the DM ruleset: opposed d20 rolls with role bonuses, shoves at 2+, armour breaks at 6+,
/// dodges out of opponents' squares, four-square passes with interceptions, and ten turns per half.
/// </summary>
public class HuttballMatchTests
{
    private const uint HomeBruiser = 1;
    private const uint HomeCatcher = 2;
    private const uint HomeThrower = 3;
    private const uint HomeBruiser2 = 4;
    private const uint AwayCatcher = 11;
    private const uint AwayBlocker = 12;
    private const uint AwayBruiser = 13;

    private ScriptedHuttballDice _dice = null!;

    [SetUp]
    public void SetUp()
    {
        _dice = new ScriptedHuttballDice();
    }

    private HuttballMatch CreateMatch(int width = 10, int length = 20)
    {
        return new HuttballMatch(width, length, _dice);
    }

    private static void Add(HuttballMatch match, uint creature, HuttballTeam team, HuttballRole role, int x, int y)
    {
        match.AddPlayer(creature, $"P{creature}", team, role).Succeeded.Should().BeTrue();
        match.Place(creature, new HuttballSquare(x, y)).Succeeded.Should().BeTrue();
    }

    private static void Kickoff(HuttballMatch match, int x = 0, int y = 0)
    {
        var result = match.StartDrive(new HuttballSquare(x, y));
        result.Succeeded.Should().BeTrue(result.Error);
    }

    [Test]
    public void RolesMatchTheRuleset()
    {
        HuttballRules.GetStats(HuttballRole.Bruiser).Should().Be(new HuttballRoleStats(5, 0, 3, false));
        HuttballRules.GetStats(HuttballRole.Catcher).Should().Be(new HuttballRoleStats(0, 2, 5, false));
        HuttballRules.GetStats(HuttballRole.Thrower).Should().Be(new HuttballRoleStats(0, 5, 3, false));
        HuttballRules.GetStats(HuttballRole.Blocker).Should().Be(new HuttballRoleStats(5, 0, 2, true));
    }

    [TestCase(1, HuttballInjury.KnockedDown)]
    [TestCase(10, HuttballInjury.KnockedDown)]
    [TestCase(11, HuttballInjury.Injured)]
    [TestCase(15, HuttballInjury.Injured)]
    [TestCase(16, HuttballInjury.SevereInjury)]
    [TestCase(19, HuttballInjury.SevereInjury)]
    [TestCase(20, HuttballInjury.Maimed)]
    public void InjuryTableMatchesTheRuleset(int roll, HuttballInjury expected)
    {
        HuttballRules.GetInjury(roll).Should().Be(expected);
    }

    [Test]
    public void SetupKeepsEachTeamInItsOwnHalf()
    {
        var match = CreateMatch();
        match.AddPlayer(HomeCatcher, "Home", HuttballTeam.Home, HuttballRole.Catcher);
        match.AddPlayer(AwayCatcher, "Away", HuttballTeam.Away, HuttballRole.Catcher);

        match.Place(HomeCatcher, new HuttballSquare(3, 10)).Succeeded.Should().BeFalse("row 11 is the Away half");
        match.Place(HomeCatcher, new HuttballSquare(3, 9)).Succeeded.Should().BeTrue();
        match.Place(AwayCatcher, new HuttballSquare(3, 9)).Succeeded.Should().BeFalse();
        match.Place(AwayCatcher, new HuttballSquare(3, 10)).Succeeded.Should().BeTrue();
    }

    [Test]
    public void WinningABlockByTwoShovesTheLoserBack()
    {
        var match = CreateMatch();
        Add(match, HomeBruiser, HuttballTeam.Home, HuttballRole.Bruiser, 5, 9);
        Add(match, AwayCatcher, HuttballTeam.Away, HuttballRole.Catcher, 5, 10);
        Kickoff(match);

        _dice.Queue(10, 10); // 10+5 vs 10: wins by 5, a shove but no armour break.
        match.Block(HomeBruiser, AwayCatcher).Succeeded.Should().BeTrue();

        var defender = match.GetPlayer(AwayCatcher);
        defender.Square.Should().Be(new HuttballSquare(5, 11));
        defender.Status.Should().Be(HuttballPlayerStatus.Standing);
        _dice.Remaining.Should().Be(0);
    }

    [Test]
    public void WinningABlockBySixBreaksArmourAndRollsInjury()
    {
        var match = CreateMatch();
        Add(match, HomeBruiser, HuttballTeam.Home, HuttballRole.Bruiser, 5, 9);
        Add(match, AwayCatcher, HuttballTeam.Away, HuttballRole.Catcher, 5, 10);
        Kickoff(match);

        _dice.Queue(15, 2, 12); // 15+5 vs 2: wins by 18. Injury roll 12: out until halftime.
        match.Block(HomeBruiser, AwayCatcher);

        var defender = match.GetPlayer(AwayCatcher);
        defender.Status.Should().Be(HuttballPlayerStatus.Injured);
        defender.Square.Should().BeNull();
    }

    [Test]
    public void ATiedBlockChangesNothing()
    {
        var match = CreateMatch();
        Add(match, HomeBruiser, HuttballTeam.Home, HuttballRole.Bruiser, 5, 9);
        Add(match, AwayCatcher, HuttballTeam.Away, HuttballRole.Catcher, 5, 10);
        Kickoff(match);

        _dice.Queue(10, 15);
        match.Block(HomeBruiser, AwayCatcher);

        match.GetPlayer(HomeBruiser).Square.Should().Be(new HuttballSquare(5, 9));
        match.GetPlayer(AwayCatcher).Square.Should().Be(new HuttballSquare(5, 10));
        match.GetPlayer(HomeBruiser).HasActed.Should().BeTrue();
    }

    [Test]
    public void TheDefenderCanWinABlock()
    {
        var match = CreateMatch();
        Add(match, HomeCatcher, HuttballTeam.Home, HuttballRole.Catcher, 5, 9);
        Add(match, AwayBruiser, HuttballTeam.Away, HuttballRole.Bruiser, 5, 10);
        Kickoff(match);

        _dice.Queue(5, 12, 4); // 5 vs 12+5: the attacker loses by 12, is shoved and knocked down.
        match.Block(HomeCatcher, AwayBruiser);

        var attacker = match.GetPlayer(HomeCatcher);
        attacker.Square.Should().Be(new HuttballSquare(5, 8));
        attacker.Status.Should().Be(HuttballPlayerStatus.KnockedDown);
        attacker.IsDone.Should().BeTrue();
    }

    [Test]
    public void LeavingABlockerTakesAgilityAgainstItsStrength()
    {
        var match = CreateMatch();
        Add(match, HomeCatcher, HuttballTeam.Home, HuttballRole.Catcher, 5, 9);
        Add(match, AwayBlocker, HuttballTeam.Away, HuttballRole.Blocker, 5, 10);
        Kickoff(match);

        _dice.Queue(10, 8); // 10+2 vs 8+5: tripped.
        var result = match.Move(HomeCatcher, new HuttballSquare(5, 6));

        result.Messages.Should().Contain(m => m.Contains("STR 8+5=13"));
        var mover = match.GetPlayer(HomeCatcher);
        mover.Square.Should().Be(new HuttballSquare(5, 9));
        mover.Status.Should().Be(HuttballPlayerStatus.KnockedDown);
        mover.IsDone.Should().BeTrue();
    }

    [Test]
    public void ADodgeTieGoesToTheMover()
    {
        var match = CreateMatch();
        Add(match, HomeCatcher, HuttballTeam.Home, HuttballRole.Catcher, 5, 9);
        Add(match, AwayCatcher, HuttballTeam.Away, HuttballRole.Catcher, 5, 10);
        Kickoff(match);

        _dice.Queue(9, 9);
        match.Move(HomeCatcher, new HuttballSquare(5, 6));

        var mover = match.GetPlayer(HomeCatcher);
        mover.Square.Should().Be(new HuttballSquare(5, 6));
        mover.MovementLeft.Should().Be(2);
    }

    [Test]
    public void MovementRoutesAroundOpponentsWhenItCan()
    {
        var match = CreateMatch();
        Add(match, HomeCatcher, HuttballTeam.Home, HuttballRole.Catcher, 0, 9);
        Add(match, AwayCatcher, HuttballTeam.Away, HuttballRole.Catcher, 2, 10);
        Kickoff(match);

        var result = match.Move(HomeCatcher, new HuttballSquare(4, 9));

        result.Succeeded.Should().BeTrue(result.Error);
        result.Path.Should().HaveCount(4);
        result.Path.Take(3).Should().OnlyContain(square => !square.IsAdjacentTo(new HuttballSquare(2, 10)),
            "leaving a square next to the opponent would need a dodge");
        _dice.Remaining.Should().Be(0);
    }

    [Test]
    public void MovementStopsAtTheRolesLimit()
    {
        var match = CreateMatch();
        Add(match, HomeBruiser, HuttballTeam.Home, HuttballRole.Bruiser, 0, 0);
        Add(match, AwayCatcher, HuttballTeam.Away, HuttballRole.Catcher, 9, 19);
        Kickoff(match, 9, 9);

        match.Move(HomeBruiser, new HuttballSquare(0, 4)).Succeeded.Should().BeFalse("a Bruiser moves 3");
        match.Move(HomeBruiser, new HuttballSquare(0, 3)).Succeeded.Should().BeTrue();
    }

    [Test]
    public void AGoodPassIsCaught()
    {
        var match = CreateMatch();
        Add(match, HomeThrower, HuttballTeam.Home, HuttballRole.Thrower, 5, 5);
        Add(match, HomeCatcher, HuttballTeam.Home, HuttballRole.Catcher, 5, 9);
        Add(match, AwayCatcher, HuttballTeam.Away, HuttballRole.Catcher, 0, 19);
        Kickoff(match, 5, 5);
        match.BallCarrier.Creature.Should().Be(HomeThrower);

        _dice.Queue(5, 8); // Throw 5+5=10, catch 8+2=10.
        match.Pass(HomeThrower, HomeCatcher).Succeeded.Should().BeTrue();

        match.BallCarrier.Creature.Should().Be(HomeCatcher);
    }

    [Test]
    public void PassesReachFourSquares()
    {
        var match = CreateMatch();
        Add(match, HomeThrower, HuttballTeam.Home, HuttballRole.Thrower, 5, 4);
        Add(match, HomeCatcher, HuttballTeam.Home, HuttballRole.Catcher, 5, 9);
        Add(match, AwayCatcher, HuttballTeam.Away, HuttballRole.Catcher, 0, 19);
        Kickoff(match, 5, 4);

        match.Pass(HomeThrower, HomeCatcher).Succeeded.Should().BeFalse();
        match.BallCarrier.Creature.Should().Be(HomeThrower);
    }

    [Test]
    public void AFumbledCatchBouncesTheBall()
    {
        var match = CreateMatch();
        Add(match, HomeThrower, HuttballTeam.Home, HuttballRole.Thrower, 5, 5);
        Add(match, HomeCatcher, HuttballTeam.Home, HuttballRole.Catcher, 5, 9);
        Add(match, AwayCatcher, HuttballTeam.Away, HuttballRole.Catcher, 0, 19);
        Kickoff(match, 5, 5);

        _dice.Queue(15, 3, 3); // Good throw, catch 3+2=5 misses, d8 face 3 bounces east.
        match.Pass(HomeThrower, HomeCatcher);

        match.BallCarrier.Should().BeNull();
        match.LooseBall.Should().Be(new HuttballSquare(6, 9));
    }

    [Test]
    public void AnOpponentOnThePassLineCanIntercept()
    {
        var match = CreateMatch();
        Add(match, HomeThrower, HuttballTeam.Home, HuttballRole.Thrower, 5, 6);
        Add(match, HomeCatcher, HuttballTeam.Home, HuttballRole.Catcher, 8, 9);
        Add(match, AwayCatcher, HuttballTeam.Away, HuttballRole.Catcher, 6, 11);
        Kickoff(match, 5, 6);

        // Home passes the turn; Away steps onto the passing lane at (7, 8) without passing an opponent.
        match.EndTeamTurn();
        var move = match.Move(AwayCatcher, new HuttballSquare(7, 8));
        move.Succeeded.Should().BeTrue(move.Error);
        match.EndTeamTurn();

        // Home's turn 2: throw 12+5=17, the interceptor rolls 16+2=18.
        _dice.Queue(12, 16);
        match.Pass(HomeThrower, HomeCatcher);

        match.BallCarrier.Creature.Should().Be(AwayCatcher);
    }

    [Test]
    public void CarryingTheBallIntoTheFarEndZoneScores()
    {
        var match = CreateMatch(10, 8);
        Add(match, HomeCatcher, HuttballTeam.Home, HuttballRole.Catcher, 2, 3);
        Add(match, AwayCatcher, HuttballTeam.Away, HuttballRole.Catcher, 9, 7);
        Kickoff(match, 2, 3);

        match.Move(HomeCatcher, new HuttballSquare(2, 7)).Succeeded.Should().BeTrue();

        match.GetScore(HuttballTeam.Home).Should().Be(1);
        match.Phase.Should().Be(HuttballPhase.Setup);
        match.ActiveTeam.Should().Be(HuttballTeam.Away, "the team scored on moves next");
        match.Turn.Should().Be(1);
        match.BallCarrier.Should().BeNull();
        match.LooseBall.Should().BeNull();
    }

    [Test]
    public void ASpottedDirtyMoveSpendsABribe()
    {
        var match = SetUpDirtyMove();

        _dice.Queue(12, 3); // Injury 12: out until halftime. Referee 3: spotted.
        match.DirtyMove(HomeBruiser2, AwayCatcher).Succeeded.Should().BeTrue();

        match.GetPlayer(AwayCatcher).Status.Should().Be(HuttballPlayerStatus.Injured);
        match.GetPlayer(HomeBruiser2).Status.Should().Be(HuttballPlayerStatus.Standing);
        match.GetBribes(HuttballTeam.Home).Should().Be(0);
    }

    [Test]
    public void ASpottedDirtyMoveWithoutABribeSendsThePlayerOff()
    {
        var match = SetUpDirtyMove();
        match.SetBribes(HuttballTeam.Home, 0);

        _dice.Queue(4, 14); // Injury 4: stays down. Referee 14: spotted.
        match.DirtyMove(HomeBruiser2, AwayCatcher);

        match.GetPlayer(AwayCatcher).Status.Should().Be(HuttballPlayerStatus.KnockedDown);
        var attacker = match.GetPlayer(HomeBruiser2);
        attacker.Status.Should().Be(HuttballPlayerStatus.SentOff);
        attacker.Square.Should().BeNull();
    }

    [Test]
    public void AnUnseenDirtyMoveCostsNothing()
    {
        var match = SetUpDirtyMove();

        _dice.Queue(4, 15);
        match.DirtyMove(HomeBruiser2, AwayCatcher);

        match.GetPlayer(HomeBruiser2).Status.Should().Be(HuttballPlayerStatus.Standing);
        match.GetBribes(HuttballTeam.Home).Should().Be(1);
    }

    /// <summary>
    /// The Home Bruiser knocks the Away Catcher down where it stands (pinned by its own Blocker), leaving the
    /// second Home Bruiser next to the downed Catcher with its action unused.
    /// </summary>
    private HuttballMatch SetUpDirtyMove()
    {
        var match = CreateMatch();
        Add(match, HomeBruiser, HuttballTeam.Home, HuttballRole.Bruiser, 5, 9);
        Add(match, HomeBruiser2, HuttballTeam.Home, HuttballRole.Bruiser, 4, 9);
        Add(match, AwayCatcher, HuttballTeam.Away, HuttballRole.Catcher, 5, 10);
        Add(match, AwayBlocker, HuttballTeam.Away, HuttballRole.Blocker, 5, 11);
        Kickoff(match);

        _dice.Queue(18, 1, 2); // Wins by 22, pinned, injury 2: knocked down.
        match.Block(HomeBruiser, AwayCatcher);
        match.GetPlayer(AwayCatcher).Status.Should().Be(HuttballPlayerStatus.KnockedDown);
        match.GetPlayer(AwayCatcher).Square.Should().Be(new HuttballSquare(5, 10));
        return match;
    }

    [Test]
    public void TenTurnsMakeAHalfAndHalftimeBringsBackTheInjured()
    {
        var match = CreateMatch();
        Add(match, HomeBruiser, HuttballTeam.Home, HuttballRole.Bruiser, 5, 9);
        Add(match, AwayCatcher, HuttballTeam.Away, HuttballRole.Catcher, 5, 10);
        Add(match, AwayBlocker, HuttballTeam.Away, HuttballRole.Blocker, 0, 19);
        Kickoff(match);

        _dice.Queue(15, 1, 13); // Away Catcher injured until halftime.
        match.Block(HomeBruiser, AwayCatcher);

        for (var call = 0; call < HuttballRules.TurnsPerHalf * 2 - 1; call++)
            match.EndTeamTurn().Succeeded.Should().BeTrue();
        match.Half.Should().Be(1);
        match.Turn.Should().Be(HuttballRules.TurnsPerHalf);
        match.ActiveTeam.Should().Be(HuttballTeam.Away);

        match.EndTeamTurn();

        match.Half.Should().Be(2);
        match.Turn.Should().Be(1);
        match.Phase.Should().Be(HuttballPhase.Setup);
        match.ActiveTeam.Should().Be(HuttballTeam.Away, "Away kicks off the second half");
        var returned = match.GetPlayer(AwayCatcher);
        returned.Status.Should().Be(HuttballPlayerStatus.Standing);
        returned.Square.Should().BeNull("returning players take their places again");

        match.Place(AwayCatcher, new HuttballSquare(5, 12));
        Kickoff(match);
        for (var call = 0; call < HuttballRules.TurnsPerHalf * 2; call++)
            match.EndTeamTurn();

        match.Phase.Should().Be(HuttballPhase.Finished);
    }

    [Test]
    public void APlayerKnockedDownStandsUpWithTheirMovement()
    {
        var match = CreateMatch();
        Add(match, HomeCatcher, HuttballTeam.Home, HuttballRole.Catcher, 5, 9);
        Add(match, AwayBlocker, HuttballTeam.Away, HuttballRole.Blocker, 5, 10);
        Kickoff(match);

        _dice.Queue(1, 20); // Tripped leaving the Blocker.
        match.Move(HomeCatcher, new HuttballSquare(5, 7));
        match.ActiveTeam.Should().Be(HuttballTeam.Away, "the turn ends once every Home player is done");
        match.EndTeamTurn();
        match.ActiveTeam.Should().Be(HuttballTeam.Home);

        match.Move(HomeCatcher, new HuttballSquare(5, 8)).Succeeded.Should().BeFalse("they must stand first");
        match.StandUp(HomeCatcher).Succeeded.Should().BeTrue();
        var player = match.GetPlayer(HomeCatcher);
        player.Status.Should().Be(HuttballPlayerStatus.Standing);
        player.MovementLeft.Should().Be(0);
        player.HasActed.Should().BeFalse("standing leaves the action");
    }

    [Test]
    public void AKnockedDownCarrierDropsTheBall()
    {
        var match = CreateMatch();
        Add(match, HomeCatcher, HuttballTeam.Home, HuttballRole.Catcher, 5, 9);
        Add(match, AwayBlocker, HuttballTeam.Away, HuttballRole.Blocker, 5, 10);
        Kickoff(match, 5, 9);

        _dice.Queue(1, 20, 7); // Tripped; the ball bounces west (d8 face 7).
        match.Move(HomeCatcher, new HuttballSquare(5, 7));

        match.BallCarrier.Should().BeNull();
        match.LooseBall.Should().Be(new HuttballSquare(4, 9));
    }

    [Test]
    public void SquaresLineUpWithTheBattleGrid()
    {
        var pitch = HuttballPitch.FromGrid(new Vector3(100f, 50f, 2f), new BattleGridSettings(2f, 1, 1, 0.2f));

        pitch.Width.Should().Be(20);
        pitch.Length.Should().Be(20);
        pitch.GetSquareCenter(new HuttballSquare(0, 0)).Should().Be(new Vector3(81f, 31f, 2f));
        pitch.GetSquareCenter(new HuttballSquare(19, 19)).Should().Be(new Vector3(119f, 69f, 2f));
        pitch.GetSquare(new Vector3(81.9f, 31.1f, 0f)).Should().Be(new HuttballSquare(0, 0));
        pitch.GetSquare(new Vector3(100.1f, 49.9f, 0f)).Should().Be(new HuttballSquare(10, 9));
        pitch.GetSquare(new Vector3(70f, 50f, 0f)).X.Should().BeNegative("positions off the pitch map off it");
    }
}
