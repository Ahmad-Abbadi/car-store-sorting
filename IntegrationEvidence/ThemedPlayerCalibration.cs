using UnityEngine;
using UnityEditor;

/// <summary>Fixed authoring calibration: the camera can see over the top tray lip.</summary>
public static class ThemedPlayerCalibration
{
    public static void Apply(Player player, float worldScale)
    {
        if(player==null||player.Look==null||player.LookTransform==null)throw new System.InvalidOperationException("Player viewing references missing");
        var settings=new SerializedObject(player.Look);
        var pivot=settings.FindProperty("pitchPivot").objectReferenceValue as Transform;
        if(pivot==null)throw new System.InvalidOperationException("Player pitch pivot missing");
        if(!player.LookTransform.IsChildOf(pivot))throw new System.InvalidOperationException("View must follow the calibrated pivot");
        float eyeHeight=player.LookTransform.position.y-player.transform.position.y;
        pivot.position+=Vector3.up*(2.9f*worldScale-eyeHeight);
        pivot.localRotation=Quaternion.Euler(20,0,0);
        if(player.Motor!=null)player.Motor.SetCapsuleDimensions(.35f*worldScale,3f*worldScale,1.5f*worldScale);
        var original=PrefabUtility.GetCorrespondingObjectFromSource(player.Character);
        if(original==null)throw new System.InvalidOperationException("Original movement configuration missing");
        player.Character.MaxStableMoveSpeed=original.MaxStableMoveSpeed*worldScale;
        player.Character.MaxAirMoveSpeed=original.MaxAirMoveSpeed*worldScale;
        player.Character.Gravity=original.Gravity*worldScale;
        player.Character.JumpUpSpeed=original.JumpUpSpeed*worldScale;
        EditorUtility.SetDirty(player.Character);PrefabUtility.RecordPrefabInstancePropertyModifications(player.Character);
        // Reuse the template's camera-fixed socket so carrying follows pitch and the calibrated eye.
        var handler=new SerializedObject(player.ItemHandler);
        var socket=handler.FindProperty("cameraFixedSocket").objectReferenceValue as Transform;
        if(socket==null||!socket.IsChildOf(pivot))throw new System.InvalidOperationException("Camera-fixed carry socket missing");
        socket.localPosition=Vector3.zero;socket.localRotation=Quaternion.identity;
        EditorUtility.SetDirty(socket);PrefabUtility.RecordPrefabInstancePropertyModifications(socket);
        EditorUtility.SetDirty(pivot);PrefabUtility.RecordPrefabInstancePropertyModifications(pivot);
        if(player.Motor!=null){EditorUtility.SetDirty(player.Motor);PrefabUtility.RecordPrefabInstancePropertyModifications(player.Motor);PrefabUtility.RecordPrefabInstancePropertyModifications(player.Motor.Capsule);}
    }
}
