#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class PlayerWallGeometryTests
{
    private const float Tolerance = 0.0001f;
    private static readonly Vector2 BodyPosition = Vector2.zero;
    private static readonly Bounds PlayerBounds = new Bounds(
        new Vector3(0.05f, 0.35f, 0f),
        new Vector3(0.4f, 0.7f, 0f));

    [TestCase(1, 0.5f, 0.49f)]
    [TestCase(-1, -0.5f, -0.49f)]
    public void HangPosition_PlacesFacingColliderEdgeAtWallSkin(
        int wallDirection,
        float wallSurfaceX,
        float expectedFacingEdgeX)
    {
        Vector2 target = PlayerWallGeometry.CalculateHangPosition(
            BodyPosition,
            PlayerBounds,
            wallSurfaceX,
            1f,
            wallDirection,
            0.01f,
            0.15f);
        Bounds shiftedBounds = ShiftBounds(PlayerBounds, target - BodyPosition);
        float facingEdgeX = wallDirection > 0 ? shiftedBounds.max.x : shiftedBounds.min.x;

        Assert.That(facingEdgeX, Is.EqualTo(expectedFacingEdgeX).Within(Tolerance));
        Assert.That(shiftedBounds.max.y, Is.EqualTo(1.15f).Within(Tolerance));
    }

    [TestCase(1, 0.5f, 0.51f)]
    [TestCase(-1, -0.5f, -0.51f)]
    public void ClimbPosition_PlacesWholeColliderBeyondAndAboveLedge(
        int wallDirection,
        float wallSurfaceX,
        float expectedTrailingEdgeX)
    {
        Vector2 target = PlayerWallGeometry.CalculateClimbPosition(
            BodyPosition,
            PlayerBounds,
            wallSurfaceX,
            1f,
            wallDirection,
            0.01f);
        Bounds shiftedBounds = ShiftBounds(PlayerBounds, target - BodyPosition);
        float trailingEdgeX = wallDirection > 0 ? shiftedBounds.min.x : shiftedBounds.max.x;

        Assert.That(trailingEdgeX, Is.EqualTo(expectedTrailingEdgeX).Within(Tolerance));
        Assert.That(shiftedBounds.min.y, Is.EqualTo(1.01f).Within(Tolerance));
    }

    private static Bounds ShiftBounds(Bounds bounds, Vector2 delta)
    {
        bounds.center += new Vector3(delta.x, delta.y, 0f);
        return bounds;
    }
}
#endif
