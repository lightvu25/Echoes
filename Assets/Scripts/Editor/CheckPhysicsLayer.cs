using UnityEditor;
using UnityEngine;
public class CheckPhysicsLayer {
    public static void Check() {
        bool collides = Physics2D.GetIgnoreLayerCollision(9, 13);
        bool collides17 = Physics2D.GetIgnoreLayerCollision(17, 13);
        Debug.Log("Item (9) ignores OneWayPlatform (13): " + collides);
        Debug.Log("Item (17) ignores OneWayPlatform (13): " + collides17);
    }
}
