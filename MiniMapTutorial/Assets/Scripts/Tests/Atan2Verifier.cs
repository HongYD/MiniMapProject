using UnityEngine;

// Attach this to any GameObject in a scene and press Play.
public class Atan2Verifier : MonoBehaviour
{
 void Start()
 {
 Test("Up (0,1)", new Vector2(0f,1f),90f,0f);
 Test("Right (1,0)", new Vector2(1f,0f),0f,90f);
Test("Down (0,-1)", new Vector2(0f, -1f), -90f,180f);
 Test("Down (-1,0)", new Vector2(-1f, 0f), -90f,180f);
 }

 private void Test(string name, Vector2 v, float expectedYXDeg, float expectedXYDeg)
 {
 // Mathf.Atan2(y, x) бк angle from +X axis, CCW positive
 float radYX = Mathf.Atan2(v.y, v.x);
 float degYX = radYX * Mathf.Rad2Deg;

 // Swapped arguments: Atan2(x, y) бк effectively angle from +Y axis
 float radXY = Mathf.Atan2(v.x, v.y);
 float degXY = radXY * Mathf.Rad2Deg;

 Debug.Log(
 $"[{name}]\n" +
 $" v = ({v.x}, {v.y})\n" +
 $" atan2(y, x) -> {degYX:F2}бу ({radYX:F4} rad),0..360 = {To360(degYX):F2}бу | expected {expectedYXDeg:F2}бу (0..360 {To360(expectedYXDeg):F2}бу)\n" +
 $" atan2(x, y) -> {degXY:F2}бу ({radXY:F4} rad),0..360 = {To360(degXY):F2}бу | expected {expectedXYDeg:F2}бу (0..360 {To360(expectedXYDeg):F2}бу)"
 );
 }

 private float To360(float deg)
 {
 deg %=360f;
 if (deg <0f) deg +=360f;
 return deg;
 }
}
