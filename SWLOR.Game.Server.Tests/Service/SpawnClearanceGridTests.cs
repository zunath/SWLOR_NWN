using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Tests.Service;

public class SpawnClearanceGridTests
{
    private const float Radius = 6f;

    [Test]
    public void HasObjectWithin_RaisedFloor_KeepsTheFullRadius()
    {
        // The Ruined Farmlands floor sits at Z 5 with the burnt barn's origin on it. Measured from Z 0 this point
        // was 6.3 m away and stayed spawnable; measured from the ground it is 3.9 m away.
        var grid = new SpawnClearanceGrid(new[] { new Vector3(73.9f, 95.9f, 5f) }, Radius);

        grid.HasObjectWithin(new Vector3(74f, 92f, 5f)).Should().BeTrue();
        grid.HasObjectWithin(new Vector3(74f, 92f, 0f)).Should().BeFalse();
    }

    [Test]
    public void HasObjectWithin_FindsTheNearbyObjectAmongDistantOnes()
    {
        // A distant object must not hide a near one; the old bake only measured its single nearest object.
        var grid = new SpawnClearanceGrid(new[]
        {
            new Vector3(55f, 142f, 0f),
            new Vector3(48.05f, 133.93f, 5f),
            new Vector3(120f, 20f, 5f),
        }, Radius);

        grid.HasObjectWithin(new Vector3(52f, 136f, 5f)).Should().BeTrue();
    }

    [Test]
    public void HasObjectWithin_ChecksNeighboringCells()
    {
        var grid = new SpawnClearanceGrid(new[] { new Vector3(11.9f, 0f, 0f) }, Radius);

        grid.HasObjectWithin(new Vector3(6.1f, 0f, 0f)).Should().BeTrue();
        grid.HasObjectWithin(new Vector3(5.8f, 0f, 0f)).Should().BeFalse();
    }

    [Test]
    public void HasObjectWithin_IncludesTheExactRadius()
    {
        var grid = new SpawnClearanceGrid(new[] { new Vector3(10f, 10f, 0f) }, Radius);

        grid.HasObjectWithin(new Vector3(16f, 10f, 0f)).Should().BeTrue();
        grid.HasObjectWithin(new Vector3(16.01f, 10f, 0f)).Should().BeFalse();
    }

    [Test]
    public void HasObjectWithin_EmptyArea_IsAlwaysClear()
    {
        var grid = new SpawnClearanceGrid(Array.Empty<Vector3>(), Radius);

        grid.HasObjectWithin(new Vector3(0f, 0f, 0f)).Should().BeFalse();
    }
}
